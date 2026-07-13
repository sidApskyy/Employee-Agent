import { Request, Response } from 'express';
import { successResponse, errorResponse } from '../utils/response.util';
import { ConfigurationResponse } from '../dtos/config.dto';

export class ConfigController {
  async getConfig(req: Request, res: Response) {
    try {
      const env = process.env.NODE_ENV || 'production';
      const apiUrl = process.env.API_URL || `http://localhost:${process.env.PORT || 3000}`;

      const response: ConfigurationResponse = {
        configVersion: process.env.CONFIG_VERSION || '1.0.0',
        apiUrl,
        environment: env,
        agentVersion: process.env.AGENT_VERSION || '1.0.0',
        retryCount: parseInt(process.env.AGENT_RETRY_COUNT || '3'),
        timeoutSeconds: parseInt(process.env.AGENT_TIMEOUT_SECONDS || '30'),
        loggingLevel: process.env.AGENT_LOG_LEVEL || 'Information',
        heartbeatIntervalSeconds: parseInt(process.env.HEARTBEAT_INTERVAL_SECONDS || '60'),
        features: {
          screenshotsEnabled: process.env.FEATURE_SCREENSHOTS !== 'false',
          applicationMonitoringEnabled: process.env.FEATURE_APP_MONITORING === 'true',
          websiteMonitoringEnabled: process.env.FEATURE_WEBSITE_MONITORING === 'true',
          idleDetectionEnabled: process.env.FEATURE_IDLE_DETECTION === 'true',
          usbMonitoringEnabled: process.env.FEATURE_USB_MONITORING === 'true',
        },
      };

      return res.json(successResponse(response));
    } catch (error) {
      console.error('Config error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }
}
