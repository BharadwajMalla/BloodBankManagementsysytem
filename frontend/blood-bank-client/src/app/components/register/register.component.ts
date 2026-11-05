import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-slate-50">
      <div class="max-w-lg w-full space-y-6 bg-white p-8 sm:p-10 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-100">

        <!-- Header -->
        <div class="text-center">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-rose-50 text-rose-600 mb-3 shadow-sm border border-rose-100">
            <svg class="w-8 h-8 fill-current text-rose-600" viewBox="0 0 24 24">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
            </svg>
          </div>
          <h2 class="text-2xl font-black tracking-tight text-slate-900">Create Portal Account</h2>
          <p class="mt-1 text-sm text-slate-500">Join the Blood Bank Management Network</p>
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

        <!-- OTP Feedback Banner -->
        @if (otpMessage) {
          <div class="p-3.5 rounded-xl bg-blue-50 border border-blue-200 text-blue-800 text-xs sm:text-sm space-y-1">
            <div class="flex items-center font-bold">
              <svg class="w-4 h-4 mr-1.5 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
              </svg>
              <span>{{ otpMessage }}</span>
            </div>
            @if (previewOtp) {
              <div class="mt-1 bg-white p-2 rounded-lg border border-blue-200 text-slate-800 font-mono text-xs flex items-center justify-between">
                <span>Verification OTP: <strong class="text-rose-600 text-sm tracking-widest">{{ previewOtp }}</strong></span>
                <button type="button" (click)="autoFillOtp()" class="text-[11px] font-bold text-rose-600 underline">Auto-fill</button>
              </div>
            }
          </div>
        }

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="mt-6 space-y-4">
          <!-- Role Selector -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Select Your Portal Role *
            </label>
            <select
              formControlName="role"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm bg-white font-medium text-slate-800"
            >
              <option value="Donor">Donor (Donate Blood & Book Appointments)</option>
              <option value="Hospital">Hospital (Requisition Blood & Inventory Units)</option>
              <option value="BloodBank">Blood Bank Operator (Manage Bank Stocks & Bags)</option>
            </select>
          </div>

          <!-- Full Name -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              {{ registerForm.get('role')?.value === 'Hospital' ? 'Contact Person / Representative Name *' : 'Full Name *' }}
            </label>
            <input
              type="text"
              formControlName="fullName"
              placeholder="e.g. Ramesh Kumar"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
            />
          </div>

          <!-- Email Address & Send OTP Button -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Email Address (Verification Required) *
            </label>
            <div class="flex space-x-2">
              <input
                type="email"
                formControlName="email"
                placeholder="ramesh@example.com"
                class="flex-1 px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
              />
              <button
                type="button"
                (click)="sendEmailOtp()"
                [disabled]="isSendingOtp || registerForm.get('email')?.invalid"
                class="px-4 py-2.5 bg-slate-800 hover:bg-slate-900 text-white font-bold text-xs rounded-xl transition-all disabled:opacity-40 disabled:cursor-not-allowed flex items-center space-x-1"
              >
                @if (isSendingOtp) {
                  <svg class="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                }
                <span>{{ otpSent ? 'Resend OTP' : 'Send OTP' }}</span>
              </button>
            </div>
            <p class="text-[11px] text-slate-500 mt-1">A 6-digit verification code will be sent to this email.</p>
          </div>

          <!-- 6-digit Email OTP -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              6-Digit Email Verification Code *
            </label>
            <input
              type="text"
              maxlength="6"
              formControlName="otpCode"
              placeholder="e.g. 123456"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-300 tracking-widest font-mono text-center text-lg font-bold focus:outline-none focus:ring-2 focus:ring-rose-500 text-slate-800"
            />
            @if (registerForm.get('otpCode')?.touched && registerForm.get('otpCode')?.invalid) {
              <p class="text-[11px] text-rose-600 font-semibold mt-1">Please enter the 6-digit OTP code received on your email.</p>
            }
          </div>

          <!-- Password with Eye Icon Toggle and Requirements Checklist -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Password *
            </label>
            <div class="relative">
              <input
                [type]="showPassword ? 'text' : 'password'"
                formControlName="password"
                placeholder="Create a strong password"
                class="w-full px-4 py-2.5 pr-12 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
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

          <!-- Phone: Indian Mobile Number Validation Only -->
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="block text-xs font-bold uppercase tracking-wider text-slate-700">
                Indian Mobile Number *
              </label>
              <span class="text-[10px] text-slate-400 font-medium">No verification required</span>
            </div>
            <div class="relative">
              <input
                type="tel"
                formControlName="phone"
                placeholder="e.g. 9876543210 or +919876543210"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 text-sm"
              />
            </div>
            @if (registerForm.get('phone')?.touched && registerForm.get('phone')?.invalid) {
              <p class="text-[11px] text-rose-600 font-semibold mt-1">
                Please enter a valid 10-digit Indian mobile number (starts with 6, 7, 8, or 9; optional +91 or 0 prefix).
              </p>
            } @else {
              <p class="text-[11px] text-slate-500 mt-1">Accepts 10-digit Indian numbers (e.g. 9876543210, +91 9876543210).</p>
            }
          </div>

          <!-- If Hospital: Hospital Name & Reg Number -->
          @if (registerForm.get('role')?.value === 'Hospital') {
            <div class="space-y-3 p-3.5 bg-emerald-50 rounded-xl border border-emerald-200">
              <span class="block text-xs font-bold text-emerald-900 uppercase">Hospital Requisition Credentials</span>
              <div>
                <label class="block text-[11px] font-bold text-emerald-800 mb-1">Hospital / Medical Center Name *</label>
                <input
                  type="text"
                  formControlName="hospitalName"
                  placeholder="e.g. Apollo Multi-Specialty Hospital"
                  class="w-full px-3 py-2 rounded-lg border border-emerald-300 bg-white text-sm"
                />
              </div>
              <div>
                <label class="block text-[11px] font-bold text-emerald-800 mb-1">Hospital Registration / License #</label>
                <input
                  type="text"
                  formControlName="hospitalRegNumber"
                  placeholder="e.g. HOSP-AP-2024-991"
                  class="w-full px-3 py-2 rounded-lg border border-emerald-300 bg-white text-sm"
                />
              </div>
            </div>
          }

          <!-- If Donor: Blood Group -->
          @if (registerForm.get('role')?.value === 'Donor') {
            <div class="p-3.5 bg-sky-50 rounded-xl border border-sky-200">
              <label class="block text-xs font-bold text-sky-900 uppercase mb-1">Donor Blood Group *</label>
              <select
                formControlName="bloodGroup"
                class="w-full px-3 py-2 rounded-lg border border-sky-300 bg-white text-sm"
              >
                <option value="O+">O+ Positive</option>
                <option value="O-">O- Negative (Universal)</option>
                <option value="A+">A+ Positive</option>
                <option value="A-">A- Negative</option>
                <option value="B+">B+ Positive</option>
                <option value="B-">B- Negative</option>
                <option value="AB+">AB+ Positive</option>
                <option value="AB-">AB- Negative</option>
              </select>
            </div>
          }

          <button
            type="submit"
            [disabled]="registerForm.invalid || isLoading"
            class="w-full py-3.5 px-4 bg-gradient-to-r from-rose-600 to-red-600 hover:from-rose-700 hover:to-red-700 text-white font-bold rounded-xl shadow-lg shadow-rose-600/30 transition-all transform active:scale-[0.99] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2 text-sm mt-3"
          >
            @if (isLoading) {
              <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Creating verified account...</span>
            } @else {
              <span>Verify & Register Account</span>
            }
          </button>
        </form>

        <div class="text-center text-xs text-slate-500 pt-2 border-t border-slate-100">
          Already registered?
          <a routerLink="/login" class="font-bold text-rose-600 hover:text-rose-700 transition-colors ml-1">
            Sign in
          </a>
        </div>
      </div>
    </div>
  `
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

  // Strong password regex: min 8, 1 uppercase, 1 lowercase, 1 number, 1 special character
  private readonly passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#])[A-Za-z\d@$!%*?&_\-#]{8,}$/;

  // Indian mobile number regex: optional +91 or 0, followed by 10 digits starting with 6, 7, 8, or 9
  private readonly indianMobileRegex = /^(\+91[\-\s]?|0)?[6-9]\d{9}$/;

  public registerForm: FormGroup = this.fb.group({
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
