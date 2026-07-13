import { Request, Response } from 'express';
import { successResponse, errorResponse } from '../utils/response.util';
import { HeartbeatRequest, HeartbeatResponse } from '../dtos/heartbeat.dto';
import { DeviceRepository } from '../repositories/device.repository';

export class HeartbeatController {
  private deviceRepository: DeviceRepository;

  constructor() {
    this.deviceRepository = new DeviceRepository();
  }

  async heartbeat(req: Request, res: Response) {
    try {
      const body: HeartbeatRequest = req.body;

      // Update device last seen time
      await this.deviceRepository.updateLastSeen(body.deviceId);

      const response: HeartbeatResponse = {
        nextHeartbeatIntervalSeconds: 60,
        configVersion: body.configVersion,
        configChanged: false,
        isBlocked: false,
        requiresLogout: false,
      };

      return res.json(successResponse(response));
    } catch (error) {
      console.error('Heartbeat error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }
}
