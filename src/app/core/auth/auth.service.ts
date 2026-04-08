import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly store   = inject(Store);

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  // TODO: definir roles según especificación funcional de autenticación
  getUserRole(): string {
    return sessionStorage.getItem('role') ?? '';
  }

  logout(): void {
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }
}
