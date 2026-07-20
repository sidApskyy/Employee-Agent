import rateLimit from 'express-rate-limit';

export const rateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 100,
  message: 'Too many requests from this IP, please try again later.',
  standardHeaders: true,
  legacyHeaders: false,
  skip: (req) => req.path.startsWith('/api/crm'),
});

// Upload endpoints: 30 employees × 2 uploads/min × 15 min = 900. Set ceiling at 1000.
export const uploadRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 1000,
  message: 'Upload rate limit exceeded. Please slow down.',
  standardHeaders: true,
  legacyHeaders: false,
});
