export interface AdminUserResponse {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  active: boolean;
  created: string;
  verified?: string | null;
  dob?: string | null;
  phoneNumber?: string | null;
  mfaEnabled?: boolean;
  updated?: string | null;
}
