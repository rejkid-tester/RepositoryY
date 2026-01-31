import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, map, Observable, tap, throwError } from 'rxjs';
import { RegisterRequest } from '../requests/register-request';
import { TokenService } from './token.service';
import { TokenResponse } from '../responses/token-response';
import { UserResponse } from '../responses/user-response';
import { Router } from '@angular/router';
import { environment } from '../environments/environment';
import { VerifyEmailRequest } from '../requests/verify-email-request';
import { ForgotPasswordRequest } from '../requests/forgot-password-request';
import { ResetPasswordRequest } from '../requests/reset-password-request';
import { AdminUserResponse } from '../responses/admin-user-response';
import { parseJwtPayload } from '../helpers/jwt.utils';

const BASE_URL = `${environment.apiUrl}/users`;

export interface MfaRequiredResponse {
  mfaRequired: true;
  mfaSessionId?: string;
  phoneMasked?: string;
  channels?: string[];
}

@Injectable({ providedIn: 'root' })
export class UserService {

  private tokenSubject: BehaviorSubject<TokenResponse | null>;
  public token: Observable<TokenResponse | null>;
  private refreshTokenTimeout: number | undefined;
  private tokenService = inject(TokenService);

  constructor(
    private router: Router,
    private http: HttpClient
  ) {
    this.tokenSubject = new BehaviorSubject<TokenResponse | null>(null);
    this.token = this.tokenSubject.asObservable();
    const existingSession = this.tokenService.getSession();
    if (existingSession) {
      this.tokenSubject.next(existingSession);
    }
  }

  public get tokenValue(): TokenResponse | null {
    return this.tokenSubject.value;
  }



  // login may return a TokenResponse or an MfaRequiredResponse.
  login(email: string, password: string, dob: string): Observable<TokenResponse | MfaRequiredResponse> {
    return this.http.post<TokenResponse | MfaRequiredResponse>(`${BASE_URL}/login`, { email, password, dob }, { withCredentials: true })
      .pipe(tap(resp => {
        // If server returned a token, set up session side-effects
        if ((resp as any)?.accessToken) {
          const token = resp as TokenResponse;
          this.tokenSubject.next(token);
          try { this.tokenService.saveSession(token); } catch { }
          this.startRefreshTokenTimer();
        }
      }));
  }

  // Verify an MFA code. Server expects email and code.
  verifyMfa(email: string, code: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${BASE_URL}/verify-mfa`, { email, code }, { withCredentials: true })
      .pipe(tap(token => {
        this.tokenSubject.next(token);
        try { this.tokenService.saveSession(token); } catch { }
        this.startRefreshTokenTimer();
      }));
  }

  // Enable MFA for the current user
  enableMfa(phoneNumber: string): Observable<{ success: boolean; message?: string; error?: string }> {
    return this.http.post<{ success: boolean; message?: string; error?: string }>(
      `${BASE_URL}/enable-mfa`,
      { phoneNumber },
      { withCredentials: true }
    );
  }

  // Disable MFA for the current user
  disableMfa(): Observable<{ success: boolean; message?: string; error?: string }> {
    return this.http.post<{ success: boolean; message?: string; error?: string }>(
      `${BASE_URL}/disable-mfa`,
      {},
      { withCredentials: true }
    );
  }

  register(registerRequest: RegisterRequest): Observable<string> {
    return this.http.post(`${BASE_URL}/register`, registerRequest, { responseType: 'text' });
  }

  logout(): Observable<any> {
    this.stopRefreshTokenTimer();
    return this.http.post(`${BASE_URL}/logout`, null, { withCredentials: true });
  }

  getUserInfo(): Observable<UserResponse> {
    return this.http.get<UserResponse>(`${BASE_URL}/info`, { withCredentials: true });
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<any> {
    return this.http.post(`${environment.apiUrl}/users/forgot-password`, { email: request.email, dob: request.dob }, { withCredentials: true });
  }

  resetPassword(request: ResetPasswordRequest): Observable<any> {
    return this.http.post(`${environment.apiUrl}/users/reset-password`, request, {
      responseType: 'json'
    });
  }

  verifyEmail(request: VerifyEmailRequest): Observable<any> {
    return this.http.post(`${environment.apiUrl}/users/confirm_email`, request, {
      responseType: 'json'
    });
  }

  getAllUsers(): Observable<AdminUserResponse[]> {
    return this.http.get<AdminUserResponse[]>(`${BASE_URL}/all`, { withCredentials: true });
  }

  refreshToken(): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${environment.apiUrl}/users/refresh-token`, {}, { withCredentials: true })
      .pipe(map((tokenResponse) => {
        const current = (this.tokenValue ?? this.tokenService.getSession() ?? {}) as TokenResponse;
        const merged: TokenResponse = {
          ...current,
          ...tokenResponse,
          userId: tokenResponse.userId ?? current.userId ?? 0,
          refreshToken: ''
        };
        this.tokenSubject.next(merged);
        try { this.tokenService.saveSession(merged); } catch { }
        this.startRefreshTokenTimer();
        return merged;
      }));
  }

  private startRefreshTokenTimer() {
    this.stopRefreshTokenTimer();

    const session = this.tokenValue;
    if (!session?.accessToken) return;

    const payload = parseJwtPayload(session.accessToken);
    const expMs = payload?.exp ? payload.exp * 1000 : 0;
    if (!expMs) return;

    // refresh 60 seconds before expiry
    const timeout = Math.max(0, expMs - Date.now() - (60 * 1000));
    console.info("Setting refresh token timeout to " + timeout / 1000 / 60 + " minutes");
    this.refreshTokenTimeout = window.setTimeout(() => {
      this.refreshToken().subscribe({
        error: (err) => {
          console.error('Auto refresh failed', err);
          this.performLogoutCleanup();
        }
      });
    }, timeout) as unknown as number;
  }

  private stopRefreshTokenTimer() {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
      this.refreshTokenTimeout = undefined;
    }
  }

  private performLogoutCleanup() {
    this.stopRefreshTokenTimer();
    this.tokenSubject.next(null);
    try { this.router.navigate(['/login']); } catch { }
  }

}
