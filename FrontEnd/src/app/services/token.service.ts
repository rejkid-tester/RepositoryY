import { Injectable } from '@angular/core';
import { parseJwtPayload } from '../helpers/jwt.utils';
import { TokenResponse } from '../responses/token-response';

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  constructor() {
    this.startTestingTimer(() => {
      this.isLoggedIn();
    });
  }
  private testingTokenInterval: number | undefined;

  startTestingTimer(callback: () => void): void {
    this.clearTestingTimer();
    this.testingTokenInterval = window.setInterval(() => {
      try { callback(); } catch {}
    }, 5 * 1000) as unknown as number;
  }

  clearTestingTimer(): void {
    if (this.testingTokenInterval) {
      clearInterval(this.testingTokenInterval);
      this.testingTokenInterval = undefined;
    }
  }
      
  saveSession(tokenResponse: TokenResponse) {
    window.localStorage.setItem('AT', tokenResponse.accessToken);
    window.localStorage.removeItem('RT');
    if (tokenResponse.userId) {
      window.localStorage.setItem('ID', tokenResponse.userId.toString());
      window.localStorage.setItem('FN', tokenResponse.firstName);
    }
  }

  getSession(): TokenResponse | null {
    if (window.localStorage.getItem('AT')) {
      const tokenResponse: TokenResponse = {
        accessToken: window.localStorage.getItem('AT') || '',
        refreshToken: '',
        firstName: window.localStorage.getItem('FN') || '',
        userId: +(window.localStorage.getItem('ID') || 0),
      };

      return tokenResponse;
    }
    return null;
  }

  logout() {
    window.localStorage.clear();
    window.localStorage.removeItem('RT');
  }

  isLoggedIn(): boolean {
    const session = this.getSession();
    if (!session?.accessToken) {
      return false;
    }
    const payload = parseJwtPayload(session.accessToken);
    if (!payload?.exp) {
      return false;
    }
    console.info("Time gap is: " + (payload.exp  - Date.now()/1000)+" seconds");
    const valid = Date.now() < (payload.exp * 1000);
    return valid;
  }
}
