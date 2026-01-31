export interface RegisterRequest {
    email: string;
    password: string;
    confirmPassword: string;
    firstName: string;
    lastName: string;
    // date of birth
    dob: Date;
    // timestamp
    ts: Date;
    // MFA - Optional phone number for two-factor authentication
    phoneNumber?: string;
    // MFA - Enable MFA during registration (requires phoneNumber)
    enableMfa?: boolean;
}
