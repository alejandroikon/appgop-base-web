import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AuthUser,
  LoginCredentials,
  TokenResponseDTO,
  UserProfileDTO,
  LoginRequestDTO,
  RefreshTokenRequestDTO,
} from '@shared/models';
import { API } from '@core/http/api-endpoints';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  login(credentials: LoginCredentials): Observable<TokenResponseDTO> {
    return this.http.post<TokenResponseDTO>(API.auth.login, {
      email: credentials.email.trim().toLowerCase(),
      password: credentials.password,
    } satisfies LoginRequestDTO);
  }

  refresh(refreshToken: string): Observable<TokenResponseDTO> {
    return this.http.post<TokenResponseDTO>(API.auth.refresh, {
      refreshToken,
    } satisfies RefreshTokenRequestDTO);
  }

  me(): Observable<UserProfileDTO> {
    return this.http.get<UserProfileDTO>(API.auth.me);
  }

  saveTokens(accessToken: string, refreshToken: string): void {
    sessionStorage.setItem('accessToken', accessToken);
    sessionStorage.setItem('refreshToken', refreshToken);
  }

  saveUser(user: AuthUser): void {
    sessionStorage.setItem('user', JSON.stringify(user));
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken');
  }

  clearSession(): void {
    sessionStorage.clear();
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

  forgotPassword(_email: string): Observable<void> {
    return new Observable((subscriber) => {
      setTimeout(() => {
        subscriber.next();
        subscriber.complete();
      }, 800);
    });
  }
}
