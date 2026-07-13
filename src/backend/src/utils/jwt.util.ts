import jwt from 'jsonwebtoken';
import crypto from 'crypto';
import { config } from '../config';

export interface TokenPayload {
  employeeId: string;
  companyId: string;
  deviceId?: string;
}

export const generateAccessToken = (payload: TokenPayload): string => {
  return jwt.sign(payload, config.jwtSecret, { expiresIn: '1h' });
};

// Refresh tokens are opaque random bytes — validated via DB hash lookup, not JWT verification.
export const generateRefreshToken = (): string => {
  return crypto.randomBytes(48).toString('base64url');
};

export const verifyAccessToken = (token: string): TokenPayload => {
  return jwt.verify(token, config.jwtSecret) as TokenPayload;
};
