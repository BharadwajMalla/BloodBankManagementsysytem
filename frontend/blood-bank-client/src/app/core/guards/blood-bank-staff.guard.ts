import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const bloodBankStaffGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  return authService.isBloodBank() || inject(Router).parseUrl('/dashboard');
};
