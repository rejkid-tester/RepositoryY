import { Component, ElementRef, EventEmitter, Input, Output, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  standalone: true,
  selector: 'app-mfa-dialog',
  imports: [CommonModule, FormsModule],
  templateUrl: './mfa-dialog.component.html',
  styleUrls: ['./mfa-dialog.component.css']
})
export class MfaDialogComponent implements OnChanges {
  @Input() visible: boolean = false;
  @Output() verify = new EventEmitter<string>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild('codeInput') codeInput?: ElementRef<HTMLInputElement>;

  code: string = '';
  error: string = '';
  loading: boolean = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible']?.currentValue === true) {
      setTimeout(() => {
        try { this.codeInput?.nativeElement.focus(); } catch {}
      });
    }
  }

  onVerify(): void {
    this.error = '';
    if (!this.code || this.code.trim().length === 0) {
      this.error = 'Please enter the code';
      return;
    }
    if (this.code.trim().length !== 6) {
      this.error = 'Code must be 6 digits';
      return;
    }
    this.verify.emit(this.code.trim());
  }

  onCancel(): void {
    this.cancelled.emit();
  }

}
