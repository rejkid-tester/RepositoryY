import { Component, EventEmitter, Input, Output, ChangeDetectorRef, NgZone, OnInit, OnDestroy } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { MfaDialogComponent } from './mfa-dialog.component';
import { LoginRequest } from '../requests/login-request';
import { Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { AccountModalService } from './account-modal.service';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, MfaDialogComponent],
  selector: 'app-login-prompt',
  templateUrl: './login-prompt.component.html',
  styleUrls: ['./login-prompt.component.css']
})
export class LoginPromptComponent implements OnInit {
  @Input() visible: boolean = true;
  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<LoginRequest>();
  @Output() register = new EventEmitter<void>();
  @Output() forgotPassword = new EventEmitter<void>();

  errorMessage = '';
  loading: boolean = false;
  // MFA UI state
  showMfa: boolean = false;
  mfaEmail: string = '';
  private subs: Subscription[] = [];

  constructor(
    private userService: UserService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
    , private modalService: AccountModalService
  ) {
    console.log('LoginPromptComponent initialized');
  }
  ngOnInit(): void {
    // Initialize the component
    // Reset login form and error state
    this.login = { email: '', password: '', dob: '' };
    this.errorMessage = '';
    this.loading = false;

    // Sync visibility with shared modal service
    this.subs.push(this.modalService.loginVisible$.subscribe(v => {
      this.visible = v;
      try { this.cdr.detectChanges(); } catch {}
    }));
  }
  login: LoginRequest = { email: '', password: '', dob: '' };

  onCancel(): void {
    this.visible = false;
    this.modalService.hideLogin();
    this.cancelled.emit();
    try { this.router.navigate(['/']); } catch {}
  }

  onSubmit(): void {
    // clear previous errors and show loading state
    this.errorMessage = '';
    this.loading = true;
    this.submitted.emit(this.login);
    // call login with dob from form (or empty string if not provided)
    this.userService.login(this.login.email, this.login.password, this.login.dob || '').subscribe({
      next: (data: any) => {
        // Check if MFA is required via mfaRequired property
        if (data?.mfaRequired === true) {
          this.loading = false;
          // SMS already sent by server during login
          this.mfaEmail = this.login.email;
          this.showMfa = true;
          try { this.cdr.detectChanges(); } catch {}
          return;
        }

        // Normal login with token
        if (data?.accessToken) {
          console.log('Login successful:', data);
          this.loading = false;
          this.ngZone.run(() => {
            this.modalService.hideLogin();
            try { this.cdr.detectChanges(); } catch {}
            try { this.router.navigate(['/profile']); } catch {}
          });
          return;
        }

        // Unexpected response
        console.log('Login response (no token, no MFA):', data);
        this.errorMessage = 'Unexpected login response';
        this.loading = false;
        this.ngZone.run(() => { try { this.cdr.detectChanges(); } catch {} });
      },
      error: (err: any) => {
        console.error('Login failed:', err);
        this.errorMessage = err?.error?.error || err?.error?.message || err?.message || 'Login failed';
        this.loading = false;
        this.ngZone.run(() => { try { this.cdr.detectChanges(); } catch {} });
      }
    });
  }

  onRegister(): void {
    this.register.emit();
    this.router.navigate(['account/register']);
  }

  onForgotPassword(): void {
    this.forgotPassword.emit();
    try { this.router.navigate(['account/forgot-password']); } catch {}
  }

  // MFA dialog handlers
  onMfaVerify(code: string): void {
    this.errorMessage = '';
    this.loading = true;
    // Use email instead of sessionId to verify MFA code
    this.userService.verifyMfa(this.mfaEmail, code).subscribe({
      next: (token: any) => {
        this.loading = false;
        this.showMfa = false;
        this.mfaEmail = '';
        this.ngZone.run(() => {
          try { this.cdr.detectChanges(); } catch {}
          try { this.modalService.hideLogin(); } catch {}
          try { this.router.navigate(['/profile']); } catch {}
        });
      },
      error: (err: any) => {
        console.error('MFA verification failed', err);
        const msg = err?.error?.error || err?.error?.message || err?.message || 'Invalid code';
        this.errorMessage = msg;
        this.loading = false;
        // If the server indicates the code expired, close the MFA dialog so
        // the login error message is visible and user can retry login.
        try {
          if (/expire/i.test(msg)) {
            this.showMfa = false;
            this.mfaEmail = '';
          }
        } catch {}
        try { this.cdr.detectChanges(); } catch {}
      }
    });
  }

  onMfaCancelled(): void {
    this.showMfa = false;
    this.mfaEmail = '';
    this.loading = false;
    try { this.cdr.detectChanges(); } catch {}
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

}
