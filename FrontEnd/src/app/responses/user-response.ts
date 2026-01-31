export interface UserResponse {
    email: string;
    firstName: string;
    lastName: string;
    creationDate: Date;
    // MFA properties
    mfaEnabled: boolean;
    phoneNumber?: string;
}
