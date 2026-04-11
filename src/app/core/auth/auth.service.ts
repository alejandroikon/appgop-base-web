import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthUser, LoginCredentials } from '@shared/models';
import { mockLogin } from './auth.mock';

@Injectable({ providedIn: 'root' })
export class AuthService {

  login(credentials: LoginCredentials): Observable<AuthUser> {
    return mockLogin(credentials.email, credentials.password);
  }

  forgotPassword(_email: string): Observable<void> {
    // Mock: siempre retorna éxito tras delay (implementado en auth.mock o inline)
    return new Observable((subscriber) => {
      setTimeout(() => {
        subscriber.next();
        subscriber.complete();
      }, 800);
    });
  }

  saveSession(user: AuthUser): void {
    sessionStorage.setItem('token', `mock-token-${user.id}`);
    sessionStorage.setItem('user', JSON.stringify(user));
  }

  clearSession(): void {
    sessionStorage.clear();
  }

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  getStoredUser(): AuthUser | null {
    const raw = sessionStorage.getItem('user');
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
