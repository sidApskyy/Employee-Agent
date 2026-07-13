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
    await prisma.employeeDevice.update({
      where: { id: deviceId },
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
