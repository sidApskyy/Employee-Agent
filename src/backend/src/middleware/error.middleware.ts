import { Request, Response, NextFunction } from 'express';
import { errorResponse } from '../utils/response.util';

export const errorHandler = (
  err: Error,
  req: Request,
  res: Response,
  next: NextFunction
) => {
  console.error('Error:', err);

  if (err.name === 'JsonWebTokenError') {
    return res.status(401).json(errorResponse('Invalid token'));
  }

  if (err.name === 'TokenExpiredError') {
    return res.status(401).json(errorResponse('Token expired'));
  }

  return res.status(500).json(errorResponse('Internal server error', 500));
};

export const notFoundHandler = (req: Request, res: Response) => {
  res.status(404).json(errorResponse('Route not found', 404));
};
