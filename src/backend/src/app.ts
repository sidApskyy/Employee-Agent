import express, { Application } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import { config } from './config';
import agentRoutes from './routes/agent.routes';
import storageRoutes from './routes/storage.routes';
import employeeRoutes from './routes/employee.routes';
import screenshotRoutes from './routes/screenshot.routes';
import { rateLimiter } from './middleware/rate-limit.middleware';
import { errorHandler, notFoundHandler } from './middleware/error.middleware';

export const createApp = (): Application => {
  const app = express();

  // Middleware
  app.use(helmet());
  app.use(cors());
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));
  app.use(rateLimiter);

  // Routes
  app.use('/api/agent', agentRoutes);
  app.use('/api/storage', storageRoutes);
  app.use('/api/employees', employeeRoutes);
  app.use('/api', screenshotRoutes);

  // Error handling
  app.use(notFoundHandler);
  app.use(errorHandler);

  return app;
};
