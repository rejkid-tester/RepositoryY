import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { UserService } from '../services/user.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_FORMATS, NativeDateAdapter, DateAdapter } from '@angular/material/core';

class HyphenDateAdapter extends NativeDateAdapter {
  override format(date: Date, displayFormat: any): string {
    const dd = this._to2digit(date.getDate());
    const mm = this._to2digit(date.getMonth() + 1);
    const yyyy = date.getFullYear();
    return `${dd}-${mm}-${yyyy}`;
  }

  override parse(value: any): Date | null {
    if (!value && value !== 0) return null;
    if (value instanceof Date) return value;
    if (typeof value === 'number') {
      const d = new Date(value);
      return isNaN(d.getTime()) ? null : d;
    }
    const str = String(value).trim();
    // Accept dd-mm-yyyy or dd/mm/yyyy or dd-mm-yy (two-digit year)
    let m = str.match(/^(\d{2})[-\/](\d{2})[-\/](\d{2,4})$/);
    if (m) {
      const d = Number(m[1]);
      const mo = Number(m[2]) - 1;
      let y = Number(m[3]);
      // Handle two-digit years: map 00-49 -> 2000-2049, 50-99 -> 1950-1999
      if (m[3].length === 2) {
        y = (y >= 50) ? (1900 + y) : (2000 + y);
      }
      const date = new Date(y, mo, d);
      return isNaN(date.getTime()) ? null : date;
    }
    m = str.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (m) {
      const y = Number(m[1]);
      const mo = Number(m[2]) - 1;
      const d = Number(m[3]);
      const date = new Date(y, mo, d);
      return isNaN(date.getTime()) ? null : date;
    }
    // Fallback to native Date parser
    const fallback = new Date(str);
    return isNaN(fallback.getTime()) ? null : fallback;
  }

  private _to2digit(n: number) {
    return (`0${n}`).slice(-2);
  }
}

@Component({
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule],
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
  ,
  providers: [
    { provide: DateAdapter, useClass: HyphenDateAdapter },
    {
      provide: MAT_DATE_FORMATS,
      useValue: {
        parse: { dateInput: 'DD-MM-YYYY' },
        display: {
          dateInput: 'DD-MM-YYYY',
          monthYearLabel: { year: 'numeric', month: 'short' },
          dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
          monthYearA11yLabel: { year: 'numeric', month: 'long' }
        }
      }
    }
  ]
})
export class ForgotPasswordComponent implements OnInit {
  @Input() visible: boolean = true;
  @Output() cancelled = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<{ email: string; dob: string }>();

  model = { email: '', dob: '' };
  // `dobDate` is the Date selected via material datepicker.
  dobDate: Date | null = null;
  loading = false;
  error = '';
  success = '';

  constructor(private userService: UserService) {}
  ngOnInit(): void {
    // Initialize component state and guard against stray values
    this.model.dob = '';
    this.dobDate = null;
    console.log('[ForgotPassword] ngOnInit dobDate:', this.dobDate, 'model.dob:', this.model.dob);

  }

  onCancel(): void {
    this.reset();
    this.cancelled.emit();
    this.visible = false;
  }

  onSubmit(): void {
    this.error = '';
    if (!this.model.email) {
      this.error = 'Please enter your email.';
      return;
    }
    console.log('[ForgotPassword] onSubmit dobDate:', this.dobDate, 'model.dob:', this.model.dob);
    // DOB is optional but included if provided
    this.loading = true;
    try {
      document && (document.body.style.cursor = 'wait');
    } catch (e) {
      // ignore if running in non-browser environment
    }
    const req = { email: this.model.email, dob: this.model.dob || undefined };
    this.userService.forgotPassword(req)
      .pipe(finalize(() => {
        this.loading = false;
        try {
          document && (document.body.style.cursor = '');
        } catch (e) {
          // ignore
        }
      }))
      .subscribe({
        next: (msg : string) => {
          this.success = 'Please check your email for password reset instructions';
          this.submitted.emit({ email: this.model.email, dob: this.model.dob });
        },
        error: (err) => {
          this.error = err?.error?.message || 'Failed to submit request.';
        }
      });
  }

  onDateChange(): void {
    console.log('[ForgotPassword] onDateChange dobDate:', this.dobDate);
    if (this.dobDate) {
      // Build ISO yyyy-mm-dd from components to avoid toISOString edge-cases
      const yyyy = String(this.dobDate.getFullYear()).padStart(4, '0');
      const mm = String(this.dobDate.getMonth() + 1).padStart(2, '0');
      const dd = String(this.dobDate.getDate()).padStart(2, '0');
      console.log('[ForgotPassword] dob year:', this.dobDate.getFullYear());
      this.model.dob = `${dd}-${mm}-${yyyy}`;
    } else {
      this.model.dob = '';
    }
  }

  private reset(): void {
    this.model = { email: '', dob: '' };
    this.dobDate = null;
    this.loading = false;
    this.error = '';
    this.success = '';
    try {
      document && (document.body.style.cursor = '');
    } catch (e) {
      // ignore
    }
    //this.visible = false;
  }

}
