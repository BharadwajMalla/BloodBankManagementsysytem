export type UserRole = 'BloodBank' | 'Donor' | 'Hospital' | 'Admin' | 'TechnicalSupervisor' | 'Technician';

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  phone?: string;
  bloodGroup?: string;
  hospitalName?: string;
  hospitalRegNumber?: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: UserRole;
  phone?: string;
  otpCode: string;
  bloodGroup?: string;
  hospitalName?: string;
  hospitalRegNumber?: string;
}

export interface ResetPasswordRequest {
  email: string;
  otpCode: string;
  newPassword: string;
}

export interface SendOtpResponse {
  message: string;
  previewOtp?: string;
}
