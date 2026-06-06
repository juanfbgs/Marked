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
  private readonly _isChecking = signal(true);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly isChecking = this._isChecking.asReadonly();

  constructor() {
    const token = localStorage.getItem('access_token');
    if (token) this.fetchCurrentUser();
    else this._isChecking.set(false);
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, data).pipe(
      tap((res) => this.handleAuthResponse(res)),
    );
  }

  register(data: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/register`, data);
  }

  logout(): void {
    this.http.post(`${this.baseUrl}/logout`, {}).pipe(
      finalize(() => this.clearAuth()),
    ).subscribe();
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refresh_token');
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, { refreshToken }).pipe(
      tap((res) => this.handleAuthResponse(res)),
    );
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  private handleAuthResponse(res: AuthResponse): void {
    localStorage.setItem('access_token', res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    this.fetchCurrentUser();
  }

  private clearAuth(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    this._currentUser.set(null);
    this._isChecking.set(false);
    this.router.navigate(['/login']);
  }

  private fetchCurrentUser(): void {
    this.http.get<User>(`${environment.API_URL}/user/profile`).pipe(
      catchError(() => of(null)),
      tap({ next: (user) => this._currentUser.set(user) }),
      finalize(() => this._isChecking.set(false)),
    ).subscribe();
  }
}
