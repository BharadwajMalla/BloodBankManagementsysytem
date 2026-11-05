import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { BloodBankService } from './blood-bank.service';
import { AuthService } from './auth.service';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface AppNotification {
  id: string;
  type: 'request' | 'stock' | 'expiry' | 'approval';
  title: string;
  message: string;
  targetTab: 'requests' | 'stock' | 'collection' | 'discard';
  urgency: 'critical' | 'warning' | 'info';
  timestamp: Date;
  read: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private bloodBankService = inject(BloodBankService);
  private authService = inject(AuthService);
  private router = inject(Router);

  public notifications = signal<AppNotification[]>([]);
  public activeTabRequest = signal<string | null>(null);

  public unreadCount = computed(() => {
    return this.notifications().filter(n => !n.read).length;
  });

  private dismissedIds = new Set<string>();
  private readIds = new Set<string>();
  private pollInterval: any = null;

  constructor() {
    this.loadStateFromStorage();
    this.refresh();

    if (typeof window !== 'undefined') {
      this.pollInterval = setInterval(() => {
        if (this.authService.isAuthenticated()) {
          this.refresh();
        }
      }, 45000);
    }
  }

  private loadStateFromStorage(): void {
    if (typeof localStorage === 'undefined') return;
    try {
      const read = localStorage.getItem('bb_read_notifs');
      if (read) this.readIds = new Set(JSON.parse(read));
      const dismissed = localStorage.getItem('bb_dismissed_notifs');
      if (dismissed) this.dismissedIds = new Set(JSON.parse(dismissed));
    } catch {
      // ignore
    }
  }

  private saveStateToStorage(): void {
    if (typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem('bb_read_notifs', JSON.stringify(Array.from(this.readIds)));
      localStorage.setItem('bb_dismissed_notifs', JSON.stringify(Array.from(this.dismissedIds)));
    } catch {
      // ignore
    }
  }

  public refresh(): void {
    if (!this.authService.isAuthenticated()) {
      this.notifications.set([]);
      return;
    }

    if (this.authService.isBloodBank()) {
      this.loadBloodBankNotifications();
    }
  }

  private loadBloodBankNotifications(): void {
    forkJoin({
      requests: this.bloodBankService.getRequests('Pending').pipe(catchError(() => of([]))),
      stock: this.bloodBankService.getStock().pipe(catchError(() => of([]))),
      collections: this.bloodBankService.getComponentCollections().pipe(catchError(() => of([]))),
      discards: this.bloodBankService.getComponentDiscards().pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ requests, stock, collections, discards }) => {
        const notifs: AppNotification[] = [];

        // 1. Pending blood component requests
        requests.forEach(req => {
          const id = `req-${req.id}`;
          if (this.dismissedIds.has(id)) return;
          const urgency: 'critical' | 'warning' | 'info' =
            req.urgency === 'Critical' ? 'critical' : req.urgency === 'Urgent' ? 'warning' : 'info';
          notifs.push({
            id,
            type: 'request',
            title: `Component Request: ${req.patientName || req.hospitalName || 'Patient'}`,
            message: `${req.quantityUnits} unit(s) of ${req.bloodGroup} ${req.componentType} requested (${req.urgency})`,
            targetTab: 'requests',
            urgency,
            timestamp: req.createdAt ? new Date(req.createdAt) : new Date(),
            read: this.readIds.has(id)
          });
        });

        // 2. Low stock & depleted stock alerts (quantity <= 2 units)
        stock.forEach(item => {
          if (item.quantityUnits <= 2) {
            const id = `stock-${item.bloodGroup}-${item.componentType}`;
            if (this.dismissedIds.has(id)) return;
            const isZero = item.quantityUnits === 0;
            notifs.push({
              id,
              type: 'stock',
              title: isZero ? `Stock Depleted: ${item.bloodGroup} ${item.componentType}` : `Low Stock: ${item.bloodGroup} ${item.componentType}`,
              message: isZero ? `0 units available in depository!` : `Only ${item.quantityUnits} unit(s) remaining in stock.`,
              targetTab: 'stock',
              urgency: isZero ? 'critical' : 'warning',
              timestamp: item.lastUpdated ? new Date(item.lastUpdated) : new Date(),
              read: this.readIds.has(id)
            });
          }
        });

        // 3. Components near expiry (within 7 days or already expired)
        const now = Date.now();
        collections.forEach(col => {
          if (col.status === 'Available' && col.expiryDate) {
            const expiryTime = new Date(col.expiryDate).getTime();
            const daysLeft = Math.ceil((expiryTime - now) / (1000 * 60 * 60 * 24));
            if (daysLeft <= 7) {
              const id = `expiry-${col.bagBarcode}`;
              if (this.dismissedIds.has(id)) return;
              const isExpired = daysLeft <= 0;
              notifs.push({
                id,
                type: 'expiry',
                title: isExpired ? `Expired Bag: ${col.bagBarcode}` : `Near Expiry: ${col.bagBarcode}`,
                message: isExpired
                  ? `${col.bloodGroup} ${col.componentType} has expired! Please review for discard.`
                  : `${col.bloodGroup} ${col.componentType} expires in ${daysLeft} day(s) (${new Date(col.expiryDate).toLocaleDateString()}).`,
                targetTab: 'collection',
                urgency: isExpired || daysLeft <= 2 ? 'critical' : 'warning',
                timestamp: col.collectionDate ? new Date(col.collectionDate) : new Date(),
                read: this.readIds.has(id)
              });
            }
          }
        });

        // 4. Component discard approvals pending
        discards.forEach(discard => {
          if (discard.approvalStatus === 'Pending') {
            const id = `discard-${discard.id}`;
            if (this.dismissedIds.has(id)) return;
            notifs.push({
              id,
              type: 'approval',
              title: `Discard Review: ${discard.bagBarcode}`,
              message: `Approval requested for bag discard. Reason: ${discard.reason}`,
              targetTab: 'discard',
              urgency: 'warning',
              timestamp: discard.createdAt ? new Date(discard.createdAt) : new Date(),
              read: this.readIds.has(id)
            });
          }
        });

        // Sort: Critical first, then Warning, then Info, then newest timestamp
        const priorityOrder: Record<string, number> = { critical: 3, warning: 2, info: 1 };
        notifs.sort((a, b) => {
          const pA = priorityOrder[a.urgency] || 0;
          const pB = priorityOrder[b.urgency] || 0;
          if (pA !== pB) return pB - pA;
          return b.timestamp.getTime() - a.timestamp.getTime();
        });

        this.notifications.set(notifs);
      }
    });
  }

  public markAsRead(id: string): void {
    this.readIds.add(id);
    this.saveStateToStorage();
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, read: true } : n)
    );
  }

  public markAllAsRead(): void {
    this.notifications().forEach(n => this.readIds.add(n.id));
    this.saveStateToStorage();
    this.notifications.update(list =>
      list.map(n => ({ ...n, read: true }))
    );
  }

  public dismiss(id: string): void {
    this.dismissedIds.add(id);
    this.saveStateToStorage();
    this.notifications.update(list => list.filter(n => n.id !== id));
  }

  public navigateToNotification(notification: AppNotification): void {
    this.markAsRead(notification.id);
    this.activeTabRequest.set(notification.targetTab);
    if (!this.router.url.includes('/dashboard')) {
      this.router.navigate(['/dashboard']);
    }
  }
}
