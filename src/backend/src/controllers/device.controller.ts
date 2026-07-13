import { Request, Response } from 'express';
import { successResponse, errorResponse } from '../utils/response.util';
import { RegisterDeviceRequest, RegisterDeviceResponse } from '../dtos/register-device.dto';
import { DeviceRepository } from '../repositories/device.repository';

export class DeviceController {
  private deviceRepository: DeviceRepository;

  constructor() {
    this.deviceRepository = new DeviceRepository();
  }

  async registerDevice(req: Request, res: Response) {
    try {
      const body: RegisterDeviceRequest = req.body;

      // Check if device already exists
      const existingDevice = await this.deviceRepository.findByFingerprint(body.fingerprint);
      if (existingDevice) {
        const response: RegisterDeviceResponse = {
          deviceId: existingDevice.id,
          deviceName: existingDevice.deviceName,
          configVersion: existingDevice.configVersion,
          isBlocked: existingDevice.isBlocked,
          registeredAt: existingDevice.registeredAt.toISOString(),
        };
        return res.json(successResponse(response));
      }

      // Create new device
      const device = await this.deviceRepository.create({
        employeeId: body.employeeId,
        companyId: body.companyId,
        deviceName: body.deviceName,
        computerName: body.computerName,
        machineGuid: body.machineGuid,
        fingerprint: body.fingerprint,
        osVersion: body.osVersion,
        windowsUsername: body.windowsUsername,
        processor: body.processor,
        ramGb: body.ramGb,
        diskSizeGb: body.diskSizeGb,
        macAddress: body.macAddress,
        agentVersion: body.agentVersion,
        configVersion: '1.0.0',
      });

      const response: RegisterDeviceResponse = {
        deviceId: device.id,
        deviceName: device.deviceName,
        configVersion: device.configVersion,
        isBlocked: device.isBlocked,
        registeredAt: device.registeredAt.toISOString(),
      };

      return res.json(successResponse(response));
    } catch (error) {
      console.error('Device registration error:', error);
      return res.status(500).json(errorResponse('Internal server error', 500));
    }
  }
}
