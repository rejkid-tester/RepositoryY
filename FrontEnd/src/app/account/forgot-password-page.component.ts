import { Component } from '@angular/core';
import { UserService } from '../services/user.service';
import { Router } from '@angular/router';
import { ForgotPasswordComponent } from './forgot-password.component';
import { ForgotPasswordRequest } from '../requests/forgot-password-request';

@Component({
  standalone: true,
  imports: [ForgotPasswordComponent],
  selector: 'app-forgot-password-page',
  template: `
    <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; display: flex; align-items: center; justify-content: center; padding: 20px; overflow-y: auto; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
      <div class="row w-100">
        <div class="col-12 col-sm-10 col-md-8 col-lg-6 col-xl-4 mx-auto">
          @if (message) {
            <div class="alert alert-success alert-dismissible fade show mb-3" role="alert">
              {{ message }}
            </div>
          }
          @if (error) {
            <div class="alert alert-danger alert-dismissible fade show mb-3" role="alert">
              {{ error }}
            </div>
          }
          <app-forgot-password (submitted)="onSubmitted($event)" (cancelled)="onCancelled()"></app-forgot-password>
        </div>
      </div>
    </div>
  `
})
export class ForgotPasswordPageComponent {
  message = '';
  error = '';
  loading = false;

  constructor(private userService: UserService, private router: Router) {}

  onSubmitted(payload: { email: string; dob: string }) {
    this.message = '';
    this.error = '';
    this.loading = true;
    
    const request: ForgotPasswordRequest = {
      email: payload.email,
      dob: payload.dob || undefined
    };
    
    this.userService.forgotPassword(request).subscribe({
      next: (res: any) => {
        this.loading = false;
        this.message = res?.message || 'If the account exists, a password reset email has been sent.';
        setTimeout(() => { 
          try { this.router.navigate(['/']); } catch {} 
        }, 3000);
      },
      error: (err: any) => {
        this.loading = false;
        this.error = err?.error?.message || err?.message || 'Request failed';
      }
    });
  }

  onCancelled() {
    try { this.router.navigate(['/']); } catch {}
  }
}
