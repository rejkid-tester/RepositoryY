import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { ResetPasswordRequest } from '../requests/reset-password-request';

@Component({
  standalone: true,
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fp-overlay">
      <div class="fp-modal">
        <div class="fp-header">
          <h3>Reset Password</h3>
          <p class="fp-sub">Set a new password using the link you received</p>
        </div>

        <div *ngIf="loading" class="fp-loading-overlay" aria-hidden="true">
          <div class="fp-spinner" role="status" aria-label="Loading"></div>
        </div>

        <form *ngIf="!success" (ngSubmit)="onSubmit()" #resetForm="ngForm" class="fp-form">
          <div *ngIf="!token" class="fp-error">Invalid or missing reset token.</div>

          <div *ngIf="token">
            <label for="password">New Password</label>
            <input id="password" name="password" type="password" class="fp-input" [(ngModel)]="model.password" required minlength="6" [disabled]="loading" placeholder="Enter new password" />

            <label for="confirmPassword">Confirm Password</label>
            <input id="confirmPassword" name="confirmPassword" type="password" class="fp-input" [(ngModel)]="model.confirmPassword" required [disabled]="loading" placeholder="Confirm new password" />

            <div *ngIf="error" class="fp-error">{{ error }}</div>

            <div class="fp-actions">
              <button type="submit" class="btn-primary" [disabled]="!resetForm.form.valid || loading">{{ loading ? 'Resetting...' : 'Reset Password' }}</button>
              <button type="button" class="btn-secondary" (click)="gotoLogin()" [disabled]="loading">Cancel</button>
            </div>
          </div>
        </form>

        <div *ngIf="success" class="fp-success fp-form">
          <div class="fp-success">{{ message }}</div>
          <div class="fp-actions" style="margin-top:1rem;">
            <button (click)="gotoLogin()" class="btn-primary">Go to Login</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
  `
    /* Reuse modal-style from other account components */
    .fp-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0,0,0,0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10000;
    }

    .fp-modal {
      background: #fff;
      padding: 0;
      border-radius: 8px;
      width: 420px;
      overflow: hidden;
      box-shadow: 0 12px 40px rgba(0,0,0,0.22);
      position: relative;
    }

    .fp-header {
      padding: 1rem 1.25rem;
      background: linear-gradient(90deg,#6a11cb 0%,#2575fc 100%);
      color: #fff;
    }

    .fp-header h3 { margin:0; font-weight:600 }
    .fp-header .fp-sub { margin:0.25rem 0 0; font-size:0.9rem; opacity:0.95 }

    .fp-form { padding: 1rem 1.25rem; background: linear-gradient(180deg,#fff 0%, #fbfdff 100%); }
    .fp-form label { display:block; margin-top:0.5rem; margin-bottom:0.25rem; color:#333; font-weight:600 }
    .fp-input { width:100%; padding:0.6rem 0.75rem; border-radius:6px; border:1px solid #e0e6f0; box-sizing:border-box; }
    .fp-mat { display:block; }

    .fp-actions { display:flex; justify-content:flex-end; gap:0.6rem; margin-top:0.75rem }
    .btn-primary { background: linear-gradient(90deg,#6a11cb,#2575fc); color:#fff; border:none; padding:0.55rem 0.9rem; border-radius:6px; cursor:pointer; font-weight:600; box-shadow: 0 6px 18px rgba(37,117,252,0.25); }
    .btn-primary[disabled] { opacity:0.7; cursor:default; box-shadow:none; }
    .btn-secondary { background:transparent; border:1px solid #d6dbe8; padding:0.45rem 0.8rem; border-radius:6px; cursor:pointer }

    .fp-error { color:#a00; margin-top:0.5rem; background: #ffecec; border:1px solid #f5c6cb; padding:0.5rem; border-radius:4px }
    .fp-success { background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 0.75rem; border-radius: 4px; margin: 0.5rem 0; line-height: 1.5; }

    /* Loading overlay inside modal */
    .fp-loading-overlay {
      position: absolute;
      inset: 0;
      background: rgba(255,255,255,0.7);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 50;
      pointer-events: auto;
    }
    .fp-spinner { width: 42px; height: 42px; border-radius: 50%; border: 5px solid rgba(0,0,0,0.08); border-top-color: #2575fc; animation: spin 1s linear infinite; }
    @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
    .reset-password {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 1rem;
    }

    .reset-card {
      background: white;
      padding: 2.5rem;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.2);
      max-width: 450px;
      width: 100%;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .icon {
      font-size: 3rem;
      text-align: center;
      margin-bottom: 1rem;
    }

    h2 {
      color: #333;
      text-align: center;
      margin-bottom: 2rem;
      font-size: 1.75rem;
      font-weight: 600;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    label {
      display: block;
      margin-bottom: 0.5rem;
      color: #555;
      font-weight: 500;
      font-size: 0.95rem;
    }

    input {
      width: 100%;
      padding: 0.875rem;
      border: 2px solid #e1e8ed;
      border-radius: 8px;
      font-size: 1rem;
      transition: all 0.3s;
      box-sizing: border-box;
    }

    input:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    input:disabled {
      background: #f5f5f5;
      cursor: not-allowed;
    }

    .error-message {
      background: #fee;
      color: #c00;
      padding: 0.875rem;
      border-radius: 6px;
      margin-bottom: 1rem;
      font-size: 0.95rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .btn-primary, .btn-secondary {
      flex: 1;
      padding: 0.875rem;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
    }

    .btn-primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .btn-primary:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(102, 126, 234, 0.5);
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: white;
      color: #667eea;
      border: 2px solid #667eea;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #f8f9fa;
    }

    .success-state, .error-state {
      text-align: center;
      padding: 2rem 0;
    }

    .success-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .success-state p, .error-state p {
      color: #333;
      margin-bottom: 1.5rem;
      font-size: 1.1rem;
    }
  `]
})
export class ResetPasswordComponent implements OnInit {
  token: string | null = null;
  model = { password: '', confirmPassword: '' };
  loading = false;
  success = false;
  error = '';
  message = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    // Accept token from either path param or query param
    this.token = this.route.snapshot.paramMap.get('token') || this.route.snapshot.queryParamMap.get('token');
    // Accept optional DOB via query string (recommended: yyyy-mm-dd)
    const dob = this.route.snapshot.queryParamMap.get('dob');
    if (dob) {
      // attach dob to model or keep local for request
      (this as any).dob = dob;
    }
  }

  onSubmit(): void {
    this.error = '';

    if (this.model.password !== this.model.confirmPassword) {
      this.error = 'Passwords do not match';
      return;
    }

    if (!this.token) {
      this.error = 'Invalid reset token';
      return;
    }

    this.loading = true;

    const request: ResetPasswordRequest = {
      token: this.token,
      password: this.model.password,
      confirmPassword: this.model.confirmPassword
    };
    // include dob if present
    if ((this as any).dob) {
      (request as any).dob = (this as any).dob;
    }

    this.userService.resetPassword(request).subscribe({
      next: (res: any) => {
        this.loading = false;
        this.success = true;
        this.message = res?.message || 'Password reset successful! You can now log in.';
        setTimeout(() => this.gotoLogin(), 2000);
      },
      error: (err: any) => {
        this.loading = false;
        this.error = err?.error?.message || 'Password reset failed. The link may be invalid or expired.';
      }
    });
  }

  gotoLogin(): void {
    this.router.navigate(['/account/login']);
  }
}
