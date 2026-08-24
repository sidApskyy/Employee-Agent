import rateLimit from 'express-rate-limit';
import { AuthRequest } from './auth.middleware';

export const rateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 100,
  message: 'Too many requests from this IP, please try again later.',
  standardHeaders: true,
  legacyHeaders: false,
  // Routes that have their own auth or dedicated rate limits should not be globally IP-limited.
  skip: (req) =>
    req.path.startsWith('/api/crm') ||
    req.path.startsWith('/api/storage') ||
    req.path.startsWith('/api/agent') ||
    req.path.startsWith('/api/auth'),
});

// IMPORTANT: keyed by authenticated employeeId, NOT source IP.
// Multiple employees behind the same office NAT/public IP would otherwise
// share a single counter, causing mass 429s (and multi-hour upload backlogs)
// even under normal usage. Requires `authenticate` middleware to run BEFORE
// this limiter on the route so req.user is populated.
//
// Per employee: screenshot every 10s = ~6 uploads/min = ~90 per 15 min.
// Ceiling set well above that to allow for retries/bursts.
export const uploadRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 500,
  message: 'Upload rate limit exceeded. Please slow down.',
  standardHeaders: true,
  legacyHeaders: false,
  keyGenerator: (req) => {
    const authReq = req as AuthRequest;
    return authReq.user?.employeeId ?? authReq.user?.deviceId ?? req.ip ?? 'unknown';
  },
});
