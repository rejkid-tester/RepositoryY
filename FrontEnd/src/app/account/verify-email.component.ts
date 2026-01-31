import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { VerifyEmailRequest } from '../requests/verify-email-request';
import { AccountModalService } from './account-modal.service';

@Component({
  standalone: true,
  selector: 'app-verify-email',
  imports: [CommonModule],
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.css']
})
export class VerifyEmailComponent implements OnInit {
  loading = true;
  success = false;
  message = '';
  private token = '';
  private readonly redirectDelayMs = 3000;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService
    , private modalService: AccountModalService
  ) {}

  ngOnInit(): void {
    // debugger; // Removed leftover debugger statement
    const token = this.route.snapshot.queryParamMap.get('token');
    const dob = this.route.snapshot.queryParamMap.get('DOB');
    
    if (!token) {
      this.loading = false;
      this.success = false;
      this.message = 'No verification token provided. Please check your email link.';
      return;
    }

    this.token = token;
    
    const request: VerifyEmailRequest = { token };
    if (dob) {
      request.dob = dob;
    }
    
    this.userService.verifyEmail(request).subscribe({
      next: (res: any) => {
        this.loading = false;
        this.success = true;
        this.message = res?.message || 'Your email has been verified successfully. You can now log in.';
        try { this.modalService.hideLogin(); } catch {}
        try {
          setTimeout(() => {
            try { this.modalService.showLogin(); } catch {}
            try { this.router.navigate(['/account/login']); } catch {}
          }, this.redirectDelayMs);
        } catch {}
      },
      error: (err: any) => {
        console.error('Verification failed:', err);
        this.loading = false;
        this.success = false;
        this.message = err?.error?.message || 'Email verification failed. The link may be invalid or expired.';
      }
    });
  }

  gotoLogin(): void {
    try { this.modalService.showLogin(); } catch {}
    this.router.navigate(['/account/login']);
  }

  // resendVerification(): void {
  //   // Navigate to a resend verification page or show a form
  //   this.router.navigate(['/account/resend-verification']);
  // }
}
