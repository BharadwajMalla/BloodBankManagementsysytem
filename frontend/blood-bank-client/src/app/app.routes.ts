import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent,
    title: 'Sign In | BloodBank SaaS'
  },
  {
    path: 'register',
    component: RegisterComponent,
    title: 'Register | BloodBank SaaS'
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    title: 'Live Inventory Dashboard | BloodBank SaaS'
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
