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
      <div class="max-w-md w-full space-y-8 bg-white p-8 sm:p-10 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-100">
        <!-- Header -->
        <div class="text-center">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-rose-50 text-rose-600 mb-4 shadow-sm border border-rose-100">
            <svg class="w-8 h-8 fill-current text-rose-600" viewBox="0 0 24 24">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
            </svg>
          </div>
          <h2 class="text-2xl font-black tracking-tight text-slate-900">Blood Bank A Portal</h2>
          <p class="mt-2 text-sm text-slate-500">Sign in to your designated portal account</p>
        </div>

        <!-- 3 Quick Demo Account Buttons -->
        <div class="bg-slate-50 p-3 rounded-xl border border-slate-200/80">
          <span class="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-2 text-center">
            One-Click Login Profiles:
          </span>
          <div class="grid grid-cols-3 gap-2">
            <button
              type="button"
              (click)="fillDemo('BloodBank')"
              class="px-2 py-2 text-xs font-bold rounded-lg bg-rose-100 hover:bg-rose-200 text-rose-800 transition-colors flex flex-col items-center"
            >
              <span>Blood Bank A</span>
              <span class="text-[9px] font-normal text-rose-600">Admin</span>
            </button>
            <button
              type="button"
              (click)="fillDemo('Hospital')"
              class="px-2 py-2 text-xs font-bold rounded-lg bg-emerald-100 hover:bg-emerald-200 text-emerald-800 transition-colors flex flex-col items-center"
            >
              <span>Hospital</span>
              <span class="text-[9px] font-normal text-emerald-600">Requisitions</span>
            </button>
            <button
              type="button"
              (click)="fillDemo('Donor')"
              class="px-2 py-2 text-xs font-bold rounded-lg bg-sky-100 hover:bg-sky-200 text-sky-800 transition-colors flex flex-col items-center"
            >
              <span>Donor</span>
              <span class="text-[9px] font-normal text-sky-600">Camps & Req</span>
            </button>
          </div>
        </div>

        <!-- Error Alert -->
        @if (errorMessage) {
          <div class="p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm flex items-start space-x-2">
            <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
            </svg>
            <span>{{ errorMessage }}</span>
          </div>
        }

        <!-- Login Form -->
        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="mt-8 space-y-4">
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Email Address
            </label>
            <input
              type="email"
              formControlName="email"
              placeholder="user@portal.org"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Password
            </label>
            <input
              type="password"
              formControlName="password"
              placeholder="••••••••"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <button
            type="submit"
            [disabled]="loginForm.invalid || isLoading"
            class="w-full py-3.5 px-4 bg-gradient-to-r from-rose-600 to-red-600 hover:from-rose-700 hover:to-red-700 text-white font-bold rounded-xl shadow-lg shadow-rose-600/30 transition-all transform active:scale-[0.99] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2 text-sm mt-2"
          >
            @if (isLoading) {
              <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Authenticating...</span>
            } @else {
              <span>Sign In to Blood Bank A</span>
            }
          </button>
        </form>

        <div class="text-center text-xs text-slate-500">
          Need an account?
          <a routerLink="/register" class="font-bold text-rose-600 hover:text-rose-700 transition-colors ml-1">
            Register here
          </a>
        </div>
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

  public loginForm: FormGroup = this.fb.group({
    email: ['admin@bloodbankA.org', [Validators.required, Validators.email]],
    password: ['BloodBank@123', [Validators.required, Validators.minLength(6)]]
  });

  fillDemo(role: 'BloodBank' | 'Hospital' | 'Donor'): void {
    if (role === 'BloodBank') {
      this.loginForm.setValue({
        email: 'admin@bloodbankA.org',
        password: 'BloodBank@123'
      });
    } else if (role === 'Hospital') {
      this.loginForm.setValue({
        email: 'cityhospital@hospital.org',
        password: 'Hospital@123'
      });
    } else if (role === 'Donor') {
      this.loginForm.setValue({
        email: 'john.donor@gmail.com',
        password: 'Donor@123'
      });
    }
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
