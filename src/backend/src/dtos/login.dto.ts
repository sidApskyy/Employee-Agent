export interface LoginRequest {
  email: string;
  password: string;
  clientVersion: string;
  environment: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
  employeeId: string;
  companyId: string;
  deviceId: string | null;
  configVersion: string;
  requiresDeviceRegistration: boolean;
}
