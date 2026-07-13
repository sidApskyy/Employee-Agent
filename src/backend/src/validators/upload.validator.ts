import { Request, Response, NextFunction } from 'express';
import { errorResponse } from '../utils/response.util';

const ALLOWED_MIME_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_FILE_SIZE = 52_428_800; // 50 MB
const SHA256_REGEX = /^[a-f0-9]{64}$/i;

export const validateUpload = (req: Request, res: Response, next: NextFunction) => {
  const file = (req as any).file as Express.Multer.File | undefined;

  if (!file) {
    return res.status(400).json(errorResponse('No file provided'));
  }

  if (!ALLOWED_MIME_TYPES.includes(file.mimetype)) {
    return res.status(400).json(errorResponse(`Unsupported file type: ${file.mimetype}`));
  }

  if (file.size > MAX_FILE_SIZE) {
    return res.status(400).json(errorResponse(`File too large: ${file.size} bytes (max ${MAX_FILE_SIZE})`));
  }

  const { jobId, correlationId, employeeId, deviceId, checksum } = req.body;

  if (!jobId || !correlationId || !employeeId || !deviceId) {
    return res.status(400).json(errorResponse('Missing required fields: jobId, correlationId, employeeId, deviceId'));
  }

  if (checksum && !SHA256_REGEX.test(checksum)) {
    return res.status(400).json(errorResponse('Invalid checksum format. Expected SHA-256 hex string'));
  }

  next();
};

export const validateCompleteUpload = (req: Request, res: Response, next: NextFunction) => {
  const { jobId, uploadId, s3ObjectKey } = req.body;
  if (!jobId || !uploadId || !s3ObjectKey) {
    return res.status(400).json(errorResponse('Missing required fields: jobId, uploadId, s3ObjectKey'));
  }
  next();
};
