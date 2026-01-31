export interface TokenResponse {
    accessToken: string;
    refreshToken: string;
    firstName: string;
    secondName?: string;
    email?: string;
    userId: number;
    mfaRequired?: boolean;
}
