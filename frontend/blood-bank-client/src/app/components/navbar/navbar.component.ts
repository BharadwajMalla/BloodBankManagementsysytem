import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AppNotification, NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  public authService = inject(AuthService);
  public notificationService = inject(NotificationService);
  private elementRef = inject(ElementRef);

  public showNotifications = false;

  public toggleNotifications(event: MouseEvent): void {
    event.stopPropagation();
    this.showNotifications = !this.showNotifications;
    if (this.showNotifications) {
      this.notificationService.refresh();
    }
  }

  public onSelectNotification(notif: AppNotification): void {
    this.notificationService.navigateToNotification(notif);
    this.showNotifications = false;
  }

  public onDismissNotification(event: MouseEvent, notifId: string): void {
    event.stopPropagation();
    this.notificationService.dismiss(notifId);
  }

  public onMarkAllRead(event: MouseEvent): void {
    event.stopPropagation();
    this.notificationService.markAllAsRead();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.showNotifications = false;
    }
  }

  onLogout(): void {
    this.authService.logout();
  }
}
