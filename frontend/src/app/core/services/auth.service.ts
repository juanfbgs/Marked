import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '@core/models/auth.model';
import { environment } from '@envs/environment.development';
import { catchError, finalize, Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.API_URL}/auth`;

  private readonly _currentUser = signal<User | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);

  constructor() {
    this.loadUserFromToken();
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, data).pipe(
      tap((res) => {
        this.saveTokens(res);
        this.setUserFromToken(res.accessToken);
      }),
    );
  }

  register(data: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/register`, data);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(
      tap({ finalize: () => this.clearAuth() }),
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refresh_token');
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, { refreshToken }).pipe(
      tap((res) => {
        this.saveTokens(res);
        this.setUserFromToken(res.accessToken);
      }),
    );
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  private loadUserFromToken(): void {
    const token = localStorage.getItem('access_token');
    if (token) this.setUserFromToken(token);
  }

  private setUserFromToken(token: string): void {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this._currentUser.set({
        id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        username: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
      });
    } catch {
      this._currentUser.set(null);
    }
  }

  private saveTokens(res: AuthResponse): void {
    localStorage.setItem('access_token', res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
  }

  private clearAuth(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }

}
