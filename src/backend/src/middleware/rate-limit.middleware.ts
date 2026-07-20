import rateLimit from 'express-rate-limit';

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

// Upload endpoints: 30 employees × 2 uploads/min × 15 min = 900. Set ceiling at 1000.
export const uploadRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 1000,
  message: 'Upload rate limit exceeded. Please slow down.',
  standardHeaders: true,
  legacyHeaders: false,
});
