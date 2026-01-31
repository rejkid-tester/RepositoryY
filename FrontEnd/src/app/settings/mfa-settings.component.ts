import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../services/user.service';

@Component({
  standalone: true,
  selector: 'app-mfa-settings',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mfa-settings">
      <h3>Two-Factor Authentication</h3>
      <p class="description">Add an extra layer of security to your account with SMS verification.</p>
      
      <div *ngIf="!mfaEnabled" class="mfa-disabled">
        <p><strong>Status:</strong> Disabled</p>
        <form (ngSubmit)="onEnableMfa()" #mfaForm="ngForm">
          <div class="form-group">
            <label for="phoneNumber">
              Phone Number <small>(with country code, e.g., +61412345678)</small>
            </label>
            <input 
              type="tel" 
              id="phoneNumber"
              [(ngModel)]="phoneNumber" 
              name="phoneNumber"
              placeholder="+61412345678"
              pattern="\\+[0-9]{10,15}"
              required 
              [disabled]="loading" />
          </div>
          <button type="submit" [disabled]="loading || !mfaForm.valid" class="btn-primary">
            {{ loading ? 'Enabling...' : 'Enable MFA' }}
          </button>
        </form>
      </div>
      
      <div *ngIf="mfaEnabled" class="mfa-enabled">
        <p><strong>Status:</strong> ✅ Enabled</p>
        <p>Your account is protected with two-factor authentication via SMS.</p>
        <button (click)="onDisableMfa()" [disabled]="loading" class="btn-danger">
          {{ loading ? 'Disabling...' : 'Disable MFA' }}
        </button>
      </div>
      
      <div *ngIf="message" class="alert alert-success">{{ message }}</div>
      <div *ngIf="error" class="alert alert-error">{{ error }}</div>
    </div>
  `,
  styles: [`
    .mfa-settings {
      max-width: 600px;
      padding: 20px;
    }

    .description {
      color: #666;
      margin-bottom: 20px;
    }

    .form-group {
      margin-bottom: 15px;
    }

    label {
      display: block;
      margin-bottom: 5px;
      font-weight: 500;
    }

    label small {
      font-weight: normal;
      color: #666;
    }

    input[type="tel"] {
      width: 100%;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }

    input[type="tel"]:focus {
      outline: none;
      border-color: #007bff;
    }

    button {
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .btn-danger {
      background-color: #dc3545;
      color: white;
    }

    .btn-danger:hover:not(:disabled) {
      background-color: #c82333;
    }

    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .alert {
      padding: 12px;
      margin-top: 15px;
      border-radius: 4px;
    }

    .alert-success {
      background-color: #d4edda;
      color: #155724;
      border: 1px solid #c3e6cb;
    }

    .alert-error {
      background-color: #f8d7da;
      color: #721c24;
      border: 1px solid #f5c6cb;
    }

    .mfa-enabled p,
    .mfa-disabled p {
      margin: 10px 0;
    }
  `]
})
export class MfaSettingsComponent implements OnInit {
  mfaEnabled = false;
  phoneNumber = '';
  loading = false;
  message = '';
  error = '';

  constructor(private userService: UserService) {}

  ngOnInit() {
    // Load user's MFA status from user info
    this.loadMfaStatus();
  }

  loadMfaStatus() {
    // Get user info to check if MFA is enabled
    this.userService.getUserInfo().subscribe({
      next: (user: any) => {
        // Assuming user object has mfaEnabled property
        this.mfaEnabled = user?.mfaEnabled || false;
      },
      error: (err) => {
        console.error('Failed to load user info', err);
      }
    });
  }

  onEnableMfa() {
    if (!this.phoneNumber) {
      this.error = 'Please enter a valid phone number';
      return;
    }
    
    this.loading = true;
    this.error = '';
    this.message = '';
    
    this.userService.enableMfa(this.phoneNumber).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.mfaEnabled = true;
          this.message = res.message || 'MFA enabled successfully! You will receive SMS codes on login.';
          this.phoneNumber = '';
        } else {
          this.error = res.error || 'Failed to enable MFA';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.error || err?.error?.message || 'Failed to enable MFA';
      }
    });
  }

  onDisableMfa() {
    if (!confirm('Are you sure you want to disable two-factor authentication?')) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';
    
    this.userService.disableMfa().subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.mfaEnabled = false;
          this.message = res.message || 'MFA disabled successfully';
        } else {
          this.error = res.error || 'Failed to disable MFA';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.error || err?.error?.message || 'Failed to disable MFA';
      }
    });
  }
}
