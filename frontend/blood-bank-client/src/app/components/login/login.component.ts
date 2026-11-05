import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public isLoading = false;
  public errorMessage: string | null = null;
  public successMessage: string | null = null;
  public showPassword = false;

  // Forgot password state
  public isForgotPassword = false;
  public isSendingOtp = false;
  public otpSent = false;
  public isResetting = false;
  public forgotError: string | null = null;
  public forgotSuccessMsg: string | null = null;
  public previewOtp: string | null = null;
  public showResetPassword = false;

  // Strong password regex: min 8, 1 upper, 1 lower, 1 digit, 1 special char
  private readonly passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#])[A-Za-z\d@$!%*?&_\-#]{8,}$/;

  public loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  public forgotForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    otpCode: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]],
    newPassword: ['', [Validators.required, Validators.pattern(this.passwordRegex)]],
    confirmPassword: ['', [Validators.required]]
  });

  // Password checklist getters
  get newPasswordVal(): string {
    return this.forgotForm.get('newPassword')?.value || '';
  }
  get isMinLength(): boolean {
    return this.newPasswordVal.length >= 8;
  }
  get hasUpper(): boolean {
    return /[A-Z]/.test(this.newPasswordVal);
  }
  get hasLower(): boolean {
    return /[a-z]/.test(this.newPasswordVal);
  }
  get hasDigit(): boolean {
    return /\d/.test(this.newPasswordVal);
  }
  get hasSpecial(): boolean {
    return /[@$!%*?&_\-#]/.test(this.newPasswordVal);
  }

  toggleShowPassword(): void {
    this.showPassword = !this.showPassword;
  }

  openForgotPassword(): void {
    this.isForgotPassword = true;
    this.forgotError = null;
    this.forgotSuccessMsg = null;
    this.otpSent = false;
    this.previewOtp = null;
    const currentEmail = this.loginForm.get('email')?.value || '';
    this.forgotForm.reset({ email: currentEmail, otpCode: '', newPassword: '', confirmPassword: '' });
  }

  cancelForgotPassword(): void {
    this.isForgotPassword = false;
    this.forgotError = null;
    this.forgotSuccessMsg = null;
    this.otpSent = false;
  }

  sendForgotOtp(): void {
    const email = this.forgotForm.get('email')?.value?.trim();
    if (!email) {
      this.forgotError = 'Please enter your registered email address first.';
      return;
    }

    this.isSendingOtp = true;
    this.forgotError = null;
    this.forgotSuccessMsg = null;

    this.authService.sendForgotPasswordOtp(email).subscribe({
      next: (res) => {
        this.isSendingOtp = false;
        this.otpSent = true;
        this.forgotSuccessMsg = res.message || 'Verification OTP sent to your email!';
        if (res.previewOtp) {
          this.previewOtp = res.previewOtp;
        }
      },
      error: (err) => {
        this.isSendingOtp = false;
        this.forgotError = err.error?.message || 'Failed to dispatch OTP. Please check the email entered.';
      }
    });
  }

  autoFillResetOtp(): void {
    if (this.previewOtp) {
      this.forgotForm.patchValue({ otpCode: this.previewOtp });
    }
  }

  onResetPasswordSubmit(): void {
    if (this.forgotForm.invalid) return;

    const { email, otpCode, newPassword, confirmPassword } = this.forgotForm.value;

    if (newPassword !== confirmPassword) {
      this.forgotError = 'Passwords do not match.';
      return;
    }

    this.isResetting = true;
    this.forgotError = null;

    this.authService.resetPassword({ email, otpCode, newPassword }).subscribe({
      next: (res) => {
        this.isResetting = false;
        this.isForgotPassword = false;
        this.successMessage = res.message || 'Password reset successfully! You can now log in with your new password.';
        this.loginForm.patchValue({ email, password: '' });
      },
      error: (err) => {
        this.isResetting = false;
        this.forgotError = err.error?.message || 'Password reset failed. Invalid or expired OTP.';
      }
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.isLoading = false;
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Login failed. Please verify your credentials.';
      }
    });
  }
}
