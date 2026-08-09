import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthTokens, LoginRequest, RegisterRequest, UserRole } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly accessToken = signal<string | null>(localStorage.getItem('clinichub.access-token'));
  private readonly refreshToken = signal<string | null>(localStorage.getItem('clinichub.refresh-token'));

  readonly isAuthenticated = computed(() => !!this.accessToken());
  readonly role = computed<UserRole | null>(() => this.readRole(this.accessToken()));

  login(request: LoginRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${environment.apiUrl}/auth/login`, request).pipe(tap((tokens) => this.persist(tokens)));
  }

  register(request: RegisterRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/register`, request);
  }

  confirmEmail(token: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/confirm-email`, { token });
  }

  refresh(): Observable<AuthTokens> {
    const refreshToken = this.refreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('Sessão expirada.'));
    }

    return this.http.post<AuthTokens>(`${environment.apiUrl}/auth/refresh`, { refreshToken }).pipe(
      tap((tokens) => this.persist(tokens)),
      catchError((error) => {
        this.logout();
        return throwError(() => error);
      })
    );
  }

  getAccessToken(): string | null {
    return this.accessToken();
  }

  logout(): void {
    localStorage.removeItem('clinichub.access-token');
    localStorage.removeItem('clinichub.refresh-token');
    this.accessToken.set(null);
    this.refreshToken.set(null);
    void this.router.navigate(['/login']);
  }

  private persist(tokens: AuthTokens): void {
    localStorage.setItem('clinichub.access-token', tokens.accessToken);
    localStorage.setItem('clinichub.refresh-token', tokens.refreshToken);
    this.accessToken.set(tokens.accessToken);
    this.refreshToken.set(tokens.refreshToken);
  }

  private readRole(token: string | null): UserRole | null {
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
      return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? payload.role ?? null;
    } catch {
      return null;
    }
  }
}
