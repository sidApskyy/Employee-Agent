import crypto from 'crypto';
import { NextFunction, Request, Response } from 'express';
import { config } from '../config';
import { errorResponse } from '../utils/response.util';

export const authenticateCrmScreenshotService = (
  req: Request,
  res: Response,
  next: NextFunction
): void => {
  const providedKey = req.header('x-crm-screenshot-key');
  const expectedKey = config.crmScreenshotApiKey;

  if (!providedKey || !expectedKey) {
    res.status(401).json(errorResponse('CRM screenshot service authentication failed'));
    return;
  }

  const providedBuffer = Buffer.from(providedKey);
  const expectedBuffer = Buffer.from(expectedKey);

  if (
    providedBuffer.length !== expectedBuffer.length ||
    !crypto.timingSafeEqual(providedBuffer, expectedBuffer)
  ) {
    res.status(401).json(errorResponse('CRM screenshot service authentication failed'));
    return;
  }

  next();
};
