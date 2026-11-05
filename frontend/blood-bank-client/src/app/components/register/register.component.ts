import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  public isLoading = false;
  public isSendingOtp = false;
  public otpSent = false;
  public otpMessage: string | null = null;
  public previewOtp: string | null = null;
  public errorMessage: string | null = null;
  public showPassword = false;
  public accountType: 'BloodBank' | 'Hospital' | 'Donor' = 'Donor';

  // Strong password regex: min 8, 1 uppercase, 1 lowercase, 1 number, 1 special character
  private readonly passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#])[A-Za-z\d@$!%*?&_\-#]{8,}$/;

  // Indian mobile number regex: optional +91 or 0, followed by 10 digits starting with 6, 7, 8, or 9
  private readonly indianMobileRegex = /^(\+91[\-\s]?|0)?[6-9]\d{9}$/;

  public registerForm: FormGroup = this.fb.group({
    accountType: ['Donor', [Validators.required]],
    role: ['Donor', [Validators.required]],
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    otpCode: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]],
    password: ['', [Validators.required, Validators.pattern(this.passwordRegex)]],
    phone: ['', [Validators.required, Validators.pattern(this.indianMobileRegex)]],
    hospitalName: [''],
    hospitalRegNumber: [''],
    bloodGroup: ['O+']
  });

  public onAccountTypeChange(event: Event): void {
    const accountType = (event.target as HTMLSelectElement).value as 'BloodBank' | 'Hospital' | 'Donor';
    this.accountType = accountType;
    const role = accountType === 'BloodBank' ? 'Technician' : accountType;
    this.registerForm.patchValue({ role });
  }

  public get selectedAccountType(): 'BloodBank' | 'Hospital' | 'Donor' {
    return this.registerForm.get('accountType')?.value ?? 'Donor';
  }

  get passwordVal(): string {
    return this.registerForm.get('password')?.value || '';
  }
  get isMinLength(): boolean {
    return this.passwordVal.length >= 8;
  }
  get hasUpper(): boolean {
    return /[A-Z]/.test(this.passwordVal);
  }
  get hasLower(): boolean {
    return /[a-z]/.test(this.passwordVal);
  }
  get hasDigit(): boolean {
    return /\d/.test(this.passwordVal);
  }
  get hasSpecial(): boolean {
    return /[@$!%*?&_\-#]/.test(this.passwordVal);
  }

  toggleShowPassword(): void {
    this.showPassword = !this.showPassword;
  }

  sendEmailOtp(): void {
    const email = this.registerForm.get('email')?.value?.trim();
    if (!email) {
      this.errorMessage = 'Please enter your email address first.';
      return;
    }

    this.isSendingOtp = true;
    this.errorMessage = null;
    this.otpMessage = null;

    this.authService.sendRegistrationOtp(email).subscribe({
      next: (res) => {
        this.isSendingOtp = false;
        this.otpSent = true;
        this.otpMessage = res.message || `Verification OTP sent to ${email}`;
        if (res.previewOtp) {
          this.previewOtp = res.previewOtp;
        }
      },
      error: (err) => {
        this.isSendingOtp = false;
        this.errorMessage = err.error?.message || 'Failed to dispatch verification OTP. Please try again.';
      }
    });
  }

  autoFillOtp(): void {
    if (this.previewOtp) {
      this.registerForm.patchValue({ otpCode: this.previewOtp });
    }
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Registration failed. Please check your verification code.';
      }
    });
  }
}
