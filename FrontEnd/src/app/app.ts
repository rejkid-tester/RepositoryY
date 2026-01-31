import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { TokenService } from './services/token.service';
import { UserService } from './services/user.service';
import { AccountModalService } from './account/account-modal.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrls: ['./app.css']
})
export class App implements OnInit, OnDestroy {
  readonly title = 'Register App';
  isLoggedIn = false;
  showShell = false;

  private tokenService = inject(TokenService);
  private userService = inject(UserService);
  private router = inject(Router);
  private modalService = inject(AccountModalService);
  private subscriptions = new Subscription();

  ngOnInit() {
    try { this.syncLoginState(); } catch {}
    try { this.updateShellVisibility(); } catch {}

    this.subscriptions.add(
      this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe((event) => {
        const navEnd = event as NavigationEnd;
        try { this.updateShellVisibility(navEnd.urlAfterRedirects); } catch {}
      })
    );
  }

  ngOnDestroy(): void {
    try { this.subscriptions.unsubscribe(); } catch {}
  }

  private updateShellVisibility(url?: string): void {
    const current = url ?? this.router.url ?? '';
    this.showShell = !current.startsWith('/account');
  }

  private syncLoginState(): void {
    const loggedIn = this.tokenService.isLoggedIn();
    this.isLoggedIn = loggedIn;
    if (!loggedIn) {
      try { this.modalService.showLogin(); } catch {}
    }
  }

  logout(): void {
    try { this.tokenService.logout(); } catch {}
    this.isLoggedIn = false;
    try { this.modalService.showLogin(); } catch {}
    try { this.router.navigate(['/account/login']); } catch {}
    return;
  }
}
