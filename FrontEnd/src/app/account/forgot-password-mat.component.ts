import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, first } from 'rxjs/operators';
//import { AccountService, AlertService } from '../_services';
import { Constants } from '../helpers/constants';
import { TimeHandler } from '../helpers/time.handler';
import { BrowserModule } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { CommonModule } from '@angular/common';
import { MatNativeDateModule } from '@angular/material/core';

@Component(
    {
        standalone: true,
        templateUrl: 'forgot-password-mat.component.html',
        selector: 'app-forgot-password-mat',
        imports: [
            CommonModule,
            FormsModule,
            MatFormFieldModule,
            MatInputModule,
            MatButtonModule,
            MatDatepickerModule,
            MatNativeDateModule,
            ReactiveFormsModule
        ],
        providers : [MatDatepickerModule]
    })
export class ForgotPasswordMatComponent implements OnInit {
    DATE_FORMAT = Constants.dateFormat;
    @Input() visible: boolean = true;
    @Output() cancelled = new EventEmitter<void>();
    @Output() submitted = new EventEmitter<{ email: string; dob: string }>();

    form!: FormGroup;
    loading = false;
    model = { email: '', dob: '' };
    error = '';

    constructor(
        private formBuilder: FormBuilder,
        //private accountService: AccountService,
        //private alertService: AlertService
    ) { }

    ngOnInit() {
        this.form = this.formBuilder.group({
            email: ['', [Validators.required, Validators.email]],
            dob: ['', [Validators.required, TimeHandler.dateValidator]],
        });
        this.form.get('dob')!.setValue(new Date());
    }

    // convenience getter for easy access to form fields
    get f() { return this.form.controls; }

    onCancel(): void {
        this.reset();
        this.cancelled.emit();
    }

    onSubmit(): void {
        // reset alerts on submit
        //this.alertService.clear();

        this.error = '';
        if (!this.model.email) {
            this.error = 'Please enter your email.';
            return;
        }
        // DOB is optional but included if provided
        this.loading = true;
        // emit payload to parent; parent handles remote call
        this.submitted.emit({ email: this.model.email, dob: this.model.dob });
        // stop local loading; parent may show progress state
        this.loading = false;
    }

    private reset(): void {
        this.model = { email: '', dob: '' };
        this.loading = false;
        this.error = '';
        this.visible = false;
    }
}