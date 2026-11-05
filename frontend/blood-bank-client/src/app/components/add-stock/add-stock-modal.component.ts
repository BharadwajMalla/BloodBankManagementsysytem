import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BloodInventoryService } from '../../core/services/blood-inventory.service';
import { AuthService } from '../../core/services/auth.service';
import { BloodGroup } from '../../core/models/blood-inventory.model';

@Component({
  selector: 'app-add-stock-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-stock-modal.component.html',
  styleUrl: './add-stock-modal.component.css'
})
export class AddStockModalComponent {
  private fb = inject(FormBuilder);
  private inventoryService = inject(BloodInventoryService);
  private authService = inject(AuthService);

  @Output() close = new EventEmitter<void>();
  @Output() stockAdded = new EventEmitter<void>();

  public isSubmitting = false;
  public errorMessage: string | null = null;
  public bloodGroups: BloodGroup[] = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  public stockForm: FormGroup = this.fb.group({
    bloodGroup: ['O-', [Validators.required]],
    quantityUnits: [10, [Validators.required, Validators.min(1), Validators.max(10000)]],
    bloodBankName: [this.getDefaultBankName(), [Validators.required, Validators.minLength(2)]]
  });

  private getDefaultBankName(): string {
    const user = this.authService.currentUserSignal();
    return user?.fullName || 'Metropolitan Blood Center';
  }

  selectBloodGroup(bg: BloodGroup): void {
    this.stockForm.patchValue({ bloodGroup: bg });
  }

  onBackdropClick(event: MouseEvent): void {
    this.closeModal();
  }

  closeModal(): void {
    this.close.emit();
  }

  onSubmit(): void {
    if (this.stockForm.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    this.inventoryService.addStock(this.stockForm.value).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.stockAdded.emit();
        this.closeModal();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to replenish blood stock.';
      }
    });
  }
}
