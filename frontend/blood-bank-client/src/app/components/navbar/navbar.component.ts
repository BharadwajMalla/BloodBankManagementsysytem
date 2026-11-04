import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header class="bg-white/90 backdrop-blur-md border-b border-slate-200 sticky top-0 z-40">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16 items-center">
          <!-- Logo & Brand -->
          <div class="flex items-center space-x-3 cursor-pointer" routerLink="/dashboard">
            <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-rose-600 to-red-600 flex items-center justify-center shadow-md shadow-rose-500/20 text-white font-black text-xl">
              <svg class="w-6 h-6 fill-current text-white" viewBox="0 0 24 24">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/>
              </svg>
            </div>
            <div>
              <span class="text-xl font-extrabold tracking-tight bg-gradient-to-r from-rose-600 to-red-600 bg-clip-text text-transparent">
                Blood Bank A
              </span>
              <span class="hidden sm:inline-block ml-1.5 px-2 py-0.5 text-[10px] font-bold tracking-widest uppercase bg-rose-50 text-rose-700 border border-rose-200 rounded-full">
                Single Facility System
              </span>
            </div>
          </div>

          <!-- Navigation -->
          <nav class="flex items-center space-x-3">
            @if (authService.isAuthenticated()) {
              <a
                routerLink="/dashboard"
                routerLinkActive="bg-slate-100 text-rose-600 font-semibold"
                class="px-3.5 py-2 text-sm font-medium text-slate-700 hover:text-rose-600 rounded-lg transition-colors flex items-center space-x-1.5"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/>
                </svg>
                <span>Portal Dashboard</span>
              </a>

              <div class="flex items-center space-x-2 pl-3 border-l border-slate-200">
                <div class="text-right hidden sm:block">
                  <p class="text-xs font-semibold text-slate-900 leading-tight">
                    {{ authService.currentUserSignal()?.fullName }}
                  </p>
                  <span
                    [ngClass]="{
                      'bg-rose-50 text-rose-700 border-rose-200': authService.isBloodBank(),
                      'bg-emerald-50 text-emerald-700 border-emerald-200': authService.isHospital(),
                      'bg-sky-50 text-sky-700 border-sky-200': authService.isDonor()
                    }"
                    class="inline-block mt-0.5 px-2 py-0.5 text-[10px] font-bold tracking-wide uppercase border rounded-md"
                  >
                    {{ authService.userRoleSignal() }}
                  </span>
                </div>

                <button
                  (click)="onLogout()"
                  title="Sign out"
                  class="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
                  </svg>
                </button>
              </div>
            } @else {
              <a routerLink="/login" class="px-4 py-2 text-sm font-medium text-slate-600 hover:text-rose-600">
                Sign In
              </a>
              <a routerLink="/register" class="px-4 py-2 text-sm font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-lg shadow-sm">
                Register
              </a>
            }
          </nav>
        </div>
      </div>
    </header>
  `
})
export class NavbarComponent {
  public authService = inject(AuthService);

  onLogout(): void {
    this.authService.logout();
  }
}
