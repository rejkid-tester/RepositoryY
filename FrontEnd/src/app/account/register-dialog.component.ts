import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { UserService } from '../services/user.service';
import { RegisterRequest } from '../requests/register-request';
import { AccountModalService } from './account-modal.service';

export interface RegisterPayload {
  title: 'Mr' | 'Mrs' | 'Miss' | 'Ms' | '';
  firstName: string;
  lastName: string;
  email: string;
  dob: string; // ISO date
  phoneNumber?: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

@Component({
  standalone: true,
  selector: 'app-register-dialog',
  imports: [CommonModule, FormsModule],
  templateUrl: './register-dialog.component.html',
  styleUrls: ['./register-dialog.component.css']
})
export class RegisterDialogComponent {

  @Input() visible: boolean = true;
  @Output() cancelled = new EventEmitter<void>();
  @Output() registered = new EventEmitter<RegisterPayload>();

  payload: RegisterPayload = {
    title: '',
    firstName: '',
    lastName: '',
    email: '',
    dob: '',
    phoneNumber: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false
  };
  constructor(private userService: UserService, private router: Router, private modalService: AccountModalService) {
    //debugger; // This will force the debugger to stop here
    console.log('RegisterDialogComponent initialized');
  }
  errorMessage: string = '';
  successMessage: string = '';
  loading: boolean = false;
  private readonly redirectDelayMs = 3000;

  onCancel(): void {
    this.reset();
    this.cancelled.emit();
    this.modalService.showLogin();
    try { this.router.navigate(['/account/login']); } catch {}
  }

  onRegister(): void {
    this.errorMessage = '';
    this.successMessage = '';
    if (!this.payload.firstName || !this.payload.lastName) {
      this.errorMessage = 'Please provide your name.';
      return;
    }
    if (!this.payload.email) {
      this.errorMessage = 'Please provide an email address.';
      return;
    }
    if (!this.payload.dob) {
      this.errorMessage = 'Please provide your date of birth.';
      return;
    }
    if (!this.payload.phoneNumber) {
      this.errorMessage = 'Please provide a phone number.';
      return;
    }
    if (!this.payload.password) {
      this.errorMessage = 'Please provide a password.';
      return;
    }
    if (this.payload.password !== this.payload.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }
    if (!this.payload.acceptTerms) {
      this.errorMessage = 'You must accept the terms.';
      return;
    }

    this.loading = true;

    const registerRequest: RegisterRequest = {
      email: this.payload.email,
      phoneNumber: this.payload.phoneNumber,
      password: this.payload.password,
      confirmPassword: this.payload.confirmPassword,
      firstName: this.payload.firstName,
      lastName: this.payload.lastName,
      dob: new Date(this.payload.dob),
      ts: new Date()
    };

    this.userService.register(registerRequest).subscribe({
      next: (res: any) => {
        // Show success message about email verification
        this.successMessage = 'Registration successful, please check your email: ' + registerRequest.email + ' for verification instructions';
        this.loading = false;
        // Immediately return user to login screen after successful registration
        //try { this.onCancel(); } catch {}
        
        // Don't emit or reset yet - let user see the success message
        // this.registered.emit(this.payload);
        // this.reset();
      },
      error: (err: any) => {
        console.error('Registration error:', err);
        
        // Handle different error response formats
        let errorMsg = 'Registration failed. Please try again.';
        
        if (err?.error) {
          // If error is a string (e.g., from backend text response)
          if (typeof err.error === 'string') {
            try {
              const parsed = JSON.parse(err.error);
              errorMsg = parsed.message || parsed.error || err.error;
            } catch {
              errorMsg = err.error;
            }
          }
          // If error is an object with message property
          else if (err.error.message) {
            errorMsg = err.error.message;
          }
          // If error has validation errors array
          else if (err.error.errors && Array.isArray(err.error.errors)) {
            errorMsg = err.error.errors.map((e: any) => e.msg || e.message).join(', ');
          }
          // If error object has other properties
          else if (err.error.error) {
            errorMsg = err.error.error;
          }
        }
        // Fallback to err.message or status text
        else if (err.message) {
          errorMsg = err.message;
        }
        else if (err.statusText) {
          errorMsg = `${err.statusText} (${err.status})`;
        }
        
        this.errorMessage = errorMsg;
        this.loading = false;
      }
    });
  }

  private reset(): void {
    this.payload = {
      title: '',
      firstName: '',
      lastName: '',
      email: '',
      dob: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
      acceptTerms: false
    };
    this.errorMessage = '';
    this.visible = false;
  }

}
