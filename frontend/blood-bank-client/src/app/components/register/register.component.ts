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
      <div class="max-w-md w-full space-y-8 bg-white p-8 sm:p-10 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-100">
        <div class="text-center">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-rose-50 text-rose-600 mb-4 shadow-sm border border-rose-100">
            <svg class="w-8 h-8 fill-current text-rose-600" viewBox="0 0 24 24">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
            </svg>
          </div>
          <h2 class="text-2xl font-black tracking-tight text-slate-900">Create Portal Account</h2>
          <p class="mt-2 text-sm text-slate-500">Register with Blood Bank A Management System</p>
        </div>

        @if (errorMessage) {
          <div class="p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm flex items-start space-x-2">
            <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
            </svg>
            <span>{{ errorMessage }}</span>
          </div>
        }

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="mt-8 space-y-4">
          <!-- Role Selector -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Select Your Portal Role *
            </label>
            <select
              formControlName="role"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm bg-white font-medium text-slate-800"
            >
              <option value="Hospital">Hospital (Request Blood & Components from Blood Bank A)</option>
              <option value="Donor">Donor (Donate at Camps & Request Blood)</option>
              <option value="BloodBank">Blood Bank A Operator (Manage Stock, Camps, Donors, Requests)</option>
            </select>
          </div>

          <!-- Full Name -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              {{ registerForm.get('role')?.value === 'Hospital' ? 'Contact Person Name *' : 'Full Name *' }}
            </label>
            <input
              type="text"
              formControlName="fullName"
              placeholder="e.g. Dr. Jane Smith"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <!-- Email -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Email Address *
            </label>
            <input
              type="email"
              formControlName="email"
              placeholder="contact@entity.org"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <!-- Password -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Password (Min 6 characters) *
            </label>
            <input
              type="password"
              formControlName="password"
              placeholder="••••••••"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <!-- Phone -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">
              Contact Phone
            </label>
            <input
              type="text"
              formControlName="phone"
              placeholder="+1-555-0100"
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 transition-all text-sm"
            />
          </div>

          <!-- If Hospital: Hospital Name & Reg Number -->
          @if (registerForm.get('role')?.value === 'Hospital') {
            <div class="space-y-3 p-3.5 bg-emerald-50 rounded-xl border border-emerald-200">
              <span class="block text-xs font-bold text-emerald-900 uppercase">Hospital Requisition Credentials</span>
              <div>
                <label class="block text-[11px] font-bold text-emerald-800 mb-1">Hospital / Medical Center Name</label>
                <input
                  type="text"
                  formControlName="hospitalName"
                  placeholder="e.g. St. Jude Memorial Hospital"
                  class="w-full px-3 py-2 rounded-lg border border-emerald-300 bg-white text-sm"
                />
              </div>
              <div>
                <label class="block text-[11px] font-bold text-emerald-800 mb-1">Hospital Registration / License #</label>
                <input
                  type="text"
                  formControlName="hospitalRegNumber"
                  placeholder="e.g. HOSP-REG-1049"
                  class="w-full px-3 py-2 rounded-lg border border-emerald-300 bg-white text-sm"
                />
              </div>
            </div>
          }

          <!-- If Donor: Blood Group -->
          @if (registerForm.get('role')?.value === 'Donor') {
            <div class="p-3.5 bg-sky-50 rounded-xl border border-sky-200">
              <label class="block text-xs font-bold text-sky-900 uppercase mb-1">Donor Blood Group</label>
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
            class="w-full py-3.5 px-4 bg-gradient-to-r from-rose-600 to-red-600 hover:from-rose-700 hover:to-red-700 text-white font-bold rounded-xl shadow-lg shadow-rose-600/30 transition-all transform active:scale-[0.99] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2 text-sm mt-2"
          >
            @if (isLoading) {
              <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Creating account...</span>
            } @else {
              <span>Register Account</span>
            }
          </button>
        </form>

        <div class="text-center text-xs text-slate-500">
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
  public errorMessage: string | null = null;

  public registerForm: FormGroup = this.fb.group({
    role: ['Hospital', [Validators.required]],
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phone: [''],
    hospitalName: [''],
    hospitalRegNumber: [''],
    bloodGroup: ['O+']
  });

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Registration failed.';
      }
    });
  }
}
