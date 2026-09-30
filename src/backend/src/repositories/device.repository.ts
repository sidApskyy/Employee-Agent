import { prisma } from '../lib/prisma';

export class DeviceRepository {
  async findByFingerprint(fingerprint: string): Promise<any> {
    return prisma.employeeDevice.findUnique({
      where: { fingerprint },
    });
  }

  async findById(deviceId: string): Promise<any> {
    return prisma.employeeDevice.findUnique({
      where: { id: deviceId },
    });
  }

  async create(data: any): Promise<any> {
    return prisma.employeeDevice.create({
      data,
    });
  }

  async updateLastSeen(deviceId: string): Promise<void> {
    // updateMany: never throws when the device row doesn't exist (e.g. the
    // agent sent a fingerprint instead of the device UUID) — a heartbeat
    // should fail soft, not 500.
    await prisma.employeeDevice.updateMany({
      where: {
        OR: [{ id: deviceId }, { machineGuid: deviceId }, { fingerprint: deviceId }],
      },
      data: {
        lastSeenAt: new Date(),
        isOnline: true,
      },
    });
  }

  async setOffline(deviceId: string): Promise<void> {
    await prisma.employeeDevice.update({
      where: { id: deviceId },
      data: {
        isOnline: false,
      },
    });
  }
}
