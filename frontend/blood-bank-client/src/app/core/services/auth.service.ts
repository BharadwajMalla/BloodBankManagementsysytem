import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, ResetPasswordRequest, SendOtpResponse, User, UserRole } from '../models/user.model';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'blood_bank_jwt_token';
  private readonly USER_KEY = 'blood_bank_current_user';

  private tokenSignal = signal<string | null>(this.getStoredToken());
  public currentUserSignal = signal<User | null>(this.getStoredUser());

  // Exposed reactive signals
  public userRoleSignal = computed<UserRole | null>(() => this.currentUserSignal()?.role ?? null);
  public isAuthenticated = computed<boolean>(() => !!this.tokenSignal() && !this.isTokenExpired());

  // Role Checks
  public isBloodBank = computed<boolean>(() => 
    this.userRoleSignal() === 'BloodBank' || this.userRoleSignal() === 'Admin'
  );
  public isDonor = computed<boolean>(() => this.userRoleSignal() === 'Donor');
  public isHospital = computed<boolean>(() => this.userRoleSignal() === 'Hospital');

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.initSession();
  }

  public register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/Auth/register`, payload).pipe(
      tap((response) => this.handleAuthSuccess(response))
    );
  }

  public sendRegistrationOtp(email: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>(`${environment.apiUrl}/Auth/send-registration-otp`, { email });
  }

  public sendForgotPasswordOtp(email: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>(`${environment.apiUrl}/Auth/forgot-password/send-otp`, { email });
  }

  public resetPassword(payload: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/Auth/forgot-password/reset`, payload);
  }

  public login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/Auth/login`, payload).pipe(
      tap((response) => this.handleAuthSuccess(response))
    );
  }

  public logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.tokenSignal.set(null);
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  public getToken(): string | null {
    return this.tokenSignal();
  }

  public hasRole(allowedRoles: UserRole[]): boolean {
    const role = this.userRoleSignal();
    return !!role && allowedRoles.includes(role);
  }

  private handleAuthSuccess(response: AuthResponse): void {
    if (response.token && response.user) {
      localStorage.setItem(this.TOKEN_KEY, response.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
      this.tokenSignal.set(response.token);
      this.currentUserSignal.set(response.user);
    }
  }

  private initSession(): void {
    const token = this.getStoredToken();
    const user = this.getStoredUser();

    if (token && !this.isTokenExpired(token)) {
      this.tokenSignal.set(token);
      this.currentUserSignal.set(user);
    } else {
      this.logout();
    }
  }

  private getStoredToken(): string | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem(this.TOKEN_KEY);
    }
    return null;
  }

  private getStoredUser(): User | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      const data = localStorage.getItem(this.USER_KEY);
      if (data) {
        try {
          return JSON.parse(data) as User;
        } catch {
          return null;
        }
      }
    }
    return null;
  }

  public isTokenExpired(tokenToCheck?: string | null): boolean {
    const token = tokenToCheck ?? this.tokenSignal();
    if (!token) return true;

    try {
      const parts = token.split('.');
      if (parts.length !== 3) return true;
      const payload = JSON.parse(atob(parts[1]));
      if (!payload.exp) return false;
      const expDate = new Date(payload.exp * 1000);
      return expDate <= new Date();
    } catch {
      return true;
    }
  }
}
