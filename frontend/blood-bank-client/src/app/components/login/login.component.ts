import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-slate-50">
      <div class="max-w-md w-full space-y-6 bg-white p-8 sm:p-10 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-100">

        <!-- ======================= STANDARD LOGIN VIEW ======================= -->
        @if (!isForgotPassword) {
          <!-- Header -->
          <div class="text-center">
            <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-rose-50 text-rose-600 mb-3 shadow-sm border border-rose-100">
              <svg class="w-8 h-8 fill-current text-rose-600" viewBox="0 0 24 24">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
              </svg>
            </div>
            <h2 class="text-2xl font-black tracking-tight text-slate-900">Sign In to Portal</h2>
            <p class="mt-1 text-sm text-slate-500">Access your Blood Bank Management workspace</p>
          </div>

          <!-- Error Alert -->
          @if (errorMessage) {
            <div class="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs sm:text-sm flex items-start space-x-2">
              <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
              </svg>
              <span>{{ errorMessage }}</span>
            </div>
          }

          <!-- Success Alert (e.g. after password reset) -->
          @if (successMessage) {
            <div class="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs sm:text-sm flex items-start space-x-2">
              <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-emerald-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
              </svg>
              <span>{{ successMessage }}</span>
            </div>
          }

          <!-- Login Form (Clean, empty defaults) -->
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="mt-6 space-y-4">
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
                Email Address
              </label>
              <input
                type="email"
                formControlName="email"
                placeholder="Enter your registered email"
                class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
              />
            </div>

            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700">
                  Password
                </label>
                <button
                  type="button"
                  (click)="openForgotPassword()"
                  class="text-xs font-bold text-rose-600 hover:text-rose-700 transition-colors"
                >
                  Forgot Password?
                </button>
              </div>

              <!-- Password Input with Show/Hide Eye Toggle -->
              <div class="relative">
                <input
                  [type]="showPassword ? 'text' : 'password'"
                  formControlName="password"
                  placeholder="Enter your password"
                  class="w-full px-4 py-3 pr-12 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
                />
                <button
                  type="button"
                  (click)="toggleShowPassword()"
                  class="absolute inset-y-0 right-0 pr-3.5 flex items-center text-slate-400 hover:text-slate-600 transition-colors focus:outline-none"
                  [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'"
                >
                  @if (showPassword) {
                    <!-- Eye Off Icon -->
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                    </svg>
                  } @else {
                    <!-- Eye Icon -->
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  }
                </button>
              </div>
            </div>

            <button
              type="submit"
              [disabled]="loginForm.invalid || isLoading"
              class="w-full py-3.5 px-4 bg-gradient-to-r from-rose-600 to-red-600 hover:from-rose-700 hover:to-red-700 text-white font-bold rounded-xl shadow-lg shadow-rose-600/30 transition-all transform active:scale-[0.99] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2 text-sm mt-3"
            >
              @if (isLoading) {
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>Authenticating...</span>
              } @else {
                <span>Sign In</span>
              }
            </button>
          </form>

          <div class="text-center text-xs text-slate-500 pt-2 border-t border-slate-100">
            Need an account?
            <a routerLink="/register" class="font-bold text-rose-600 hover:text-rose-700 transition-colors ml-1">
              Register here
            </a>
          </div>
        }

        <!-- ======================= FORGOT PASSWORD VIEW ======================= -->
        @if (isForgotPassword) {
          <!-- Header -->
          <div class="text-center">
            <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-amber-50 text-amber-600 mb-3 shadow-sm border border-amber-100">
              <svg class="w-8 h-8 fill-none stroke-current" viewBox="0 0 24 24" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
            </div>
            <h2 class="text-2xl font-black tracking-tight text-slate-900">Reset Password</h2>
            <p class="mt-1 text-xs sm:text-sm text-slate-500">
              {{ otpSent ? 'Enter the 6-digit OTP sent to your email and your new password' : 'Enter your registered email to receive an email OTP code' }}
            </p>
          </div>

          <!-- Error Alert -->
          @if (forgotError) {
            <div class="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs sm:text-sm flex items-start space-x-2">
              <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
              </svg>
              <span>{{ forgotError }}</span>
            </div>
          }

          <!-- Info / OTP Code Preview Box -->
          @if (forgotSuccessMsg) {
            <div class="p-3.5 rounded-xl bg-blue-50 border border-blue-200 text-blue-800 text-xs sm:text-sm space-y-1">
              <div class="flex items-center font-bold">
                <svg class="w-4 h-4 mr-1.5 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                </svg>
                <span>{{ forgotSuccessMsg }}</span>
              </div>
              @if (previewOtp) {
                <div class="mt-1 bg-white p-2 rounded-lg border border-blue-200 text-slate-800 font-mono text-xs flex items-center justify-between">
                  <span>Verification Code: <strong class="text-rose-600 text-sm tracking-widest">{{ previewOtp }}</strong></span>
                  <button type="button" (click)="autoFillResetOtp()" class="text-[11px] font-bold text-rose-600 underline">Auto-fill</button>
                </div>
              }
            </div>
          }

          <form [formGroup]="forgotForm" (ngSubmit)="onResetPasswordSubmit()" class="mt-4 space-y-4">
            <!-- Email Field -->
            <div>
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
                Registered Email *
              </label>
              <div class="flex space-x-2">
                <input
                  type="email"
                  formControlName="email"
                  placeholder="name@portal.org"
                  [readonly]="otpSent"
                  class="flex-1 px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 text-sm disabled:bg-slate-100"
                />
                <button
                  type="button"
                  (click)="sendForgotOtp()"
                  [disabled]="isSendingOtp || forgotForm.get('email')?.invalid"
                  class="px-4 py-2.5 bg-slate-800 hover:bg-slate-900 text-white font-bold text-xs rounded-xl transition-all disabled:opacity-40 disabled:cursor-not-allowed flex items-center space-x-1"
                >
                  @if (isSendingOtp) {
                    <svg class="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  }
                  <span>{{ otpSent ? 'Resend' : 'Send OTP' }}</span>
                </button>
              </div>
            </div>

            <!-- OTP and New Password Fields (Visible once OTP is sent) -->
            @if (otpSent) {
              <!-- 6-digit OTP -->
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
                  6-Digit Email OTP *
                </label>
                <input
                  type="text"
                  maxlength="6"
                  formControlName="otpCode"
                  placeholder="e.g. 123456"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-300 tracking-widest font-mono text-center text-lg font-bold focus:outline-none focus:ring-2 focus:ring-rose-500 text-slate-800"
                />
              </div>

              <!-- New Password with Eye Icon -->
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
                  New Password *
                </label>
                <div class="relative">
                  <input
                    [type]="showResetPassword ? 'text' : 'password'"
                    formControlName="newPassword"
                    placeholder="Min 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special char"
                    class="w-full px-4 py-2.5 pr-12 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
                  />
                  <button
                    type="button"
                    (click)="showResetPassword = !showResetPassword"
                    class="absolute inset-y-0 right-0 pr-3.5 flex items-center text-slate-400 hover:text-slate-600 transition-colors focus:outline-none"
                  >
                    @if (showResetPassword) {
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                      </svg>
                    } @else {
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                    }
                  </button>
                </div>

                <!-- Password Requirements Checklist -->
                <div class="mt-2 text-[11px] text-slate-500 space-y-1 bg-slate-50 p-2.5 rounded-lg border border-slate-200">
                  <span class="font-bold block text-slate-700">Password Requirements:</span>
                  <div class="flex items-center space-x-1" [class.text-emerald-600]="isMinLength" [class.font-bold]="isMinLength">
                    <span>{{ isMinLength ? '✓' : '•' }} At least 8 characters</span>
                  </div>
                  <div class="flex items-center space-x-1" [class.text-emerald-600]="hasUpper" [class.font-bold]="hasUpper">
                    <span>{{ hasUpper ? '✓' : '•' }} At least 1 uppercase letter (A-Z)</span>
                  </div>
                  <div class="flex items-center space-x-1" [class.text-emerald-600]="hasLower" [class.font-bold]="hasLower">
                    <span>{{ hasLower ? '✓' : '•' }} At least 1 lowercase letter (a-z)</span>
                  </div>
                  <div class="flex items-center space-x-1" [class.text-emerald-600]="hasDigit" [class.font-bold]="hasDigit">
                    <span>{{ hasDigit ? '✓' : '•' }} At least 1 number (0-9)</span>
                  </div>
                  <div class="flex items-center space-x-1" [class.text-emerald-600]="hasSpecial" [class.font-bold]="hasSpecial">
                    <span>{{ hasSpecial ? '✓' : '•' }} At least 1 special symbol (&#64;$!%*?&amp;_#-)</span>
                  </div>
                </div>
              </div>

              <!-- Confirm Password -->
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
                  Confirm New Password *
                </label>
                <input
                  type="password"
                  formControlName="confirmPassword"
                  placeholder="Re-type new password"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
                />
                @if (forgotForm.get('confirmPassword')?.touched && forgotForm.value.newPassword !== forgotForm.value.confirmPassword) {
                  <p class="text-[11px] text-red-600 font-semibold mt-1">Passwords do not match.</p>
                }
              </div>

              <button
                type="submit"
                [disabled]="forgotForm.invalid || forgotForm.value.newPassword !== forgotForm.value.confirmPassword || isResetting"
                class="w-full py-3 px-4 bg-gradient-to-r from-rose-600 to-red-600 hover:from-rose-700 hover:to-red-700 text-white font-bold rounded-xl shadow-lg shadow-rose-600/30 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2 text-sm mt-2"
              >
                @if (isResetting) {
                  <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  <span>Resetting Password...</span>
                } @else {
                  <span>Update Password</span>
                }
              </button>
            }
          </form>

          <div class="text-center pt-3 border-t border-slate-100">
            <button
              type="button"
              (click)="cancelForgotPassword()"
              class="text-xs font-bold text-slate-600 hover:text-slate-900 transition-colors inline-flex items-center space-x-1"
            >
              <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
              </svg>
              <span>Back to Sign In</span>
            </button>
          </div>
        }

      </div>
    </div>
  `
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
