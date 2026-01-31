import { Injectable } from '@angular/core';
import { AbstractControl, FormControl } from '@angular/forms';
import { Constants } from './constants';

@Injectable()
export class TimeHandler {
    constructor() { }

    // Validates that the control value is a valid date (Date object or ISO-8601 string).
    static dateValidator(control: FormControl) {
        if (!control || control.value == null || control.value === '') {
            return null;
        }

        const val = control.value;

        if (val instanceof Date) {
            return isNaN(val.getTime()) ? { dateValidator: true } : null;
        }

        if (typeof val === 'string') {
            // Allow plain date (YYYY-MM-DD) or ISO datetime strings.
            const isoDateRegex = /^\d{4}-\d{2}-\d{2}(T.*)?$/;
            if (!isoDateRegex.test(val)) {
                return { dateValidator: true };
            }
            const parsed = new Date(val);
            return isNaN(parsed.getTime()) ? { dateValidator: true } : null;
        }

        return { dateValidator: true };
    }

    // Validates a date-time value (accepts Date or any parsable date-time string).
    static dateTimeValidator(control: AbstractControl) {
        if (!control || control.value == null || control.value === '') {
            return null;
        }

        const val = control.value;
        if (val instanceof Date) {
            return isNaN(val.getTime()) ? { dateValidator: true } : null;
        }

        if (typeof val === 'string') {
            const parsed = new Date(val);
            return isNaN(parsed.getTime()) ? { dateValidator: true } : null;
        }

        return { dateValidator: true };
    }
}