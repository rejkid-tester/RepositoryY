import { Component, OnInit, inject } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { TokenService } from '../services/token.service';

@Component({
  standalone: true,
  imports: [RouterModule],
  template: `
    <div class="account-layout">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    .account-layout {
      position: fixed;
      inset: 0;
      width: 100%;
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f5f5f5;
      z-index: 1000;
    }
  `]
})
export class AccountLayoutComponent implements OnInit {
  private router = inject(Router);
  private tokenService = inject(TokenService);

  ngOnInit(): void {
    // Redirect away from auth pages if already logged in, but allow profile/verify-email
    if (this.tokenService.isLoggedIn()) {
      const url = this.router.url || '';
      const allow = url.includes('/account/verify-email') || url.includes('/account/profile') || url.includes('verify-email');
      if (!allow) {
        // Delay redirect to avoid interfering with initial route resolution/rendering.
        setTimeout(() => this.router.navigate(['/profile']));
      }
    }
  }
}
