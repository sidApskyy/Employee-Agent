import { prisma } from '../lib/prisma';
import crypto from 'crypto';

export class EmployeeRepository {
  async findByEmail(email: string) {
    return prisma.employee.findUnique({
      where: { email: email.toLowerCase().trim() },
    });
  }

  async findById(id: string) {
    return prisma.employee.findUnique({ where: { id } });
  }

  async create(data: {
    email: string;
    passwordHash: string;
    firstName: string;
    lastName: string;
    companyId: string;
    role?: string;
  }) {
    return prisma.employee.create({
      data: {
        ...data,
        email: data.email.toLowerCase().trim(),
      },
    });
  }

  async storeRefreshToken(employeeId: string, rawToken: string, deviceId?: string) {
    const tokenHash = crypto.createHash('sha256').update(rawToken).digest('hex');
    const expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000); // 7 days

    await prisma.refreshToken.create({
      data: { employeeId, tokenHash, deviceId, expiresAt },
    });

    return tokenHash;
  }

  async findRefreshToken(rawToken: string) {
    const tokenHash = crypto.createHash('sha256').update(rawToken).digest('hex');
    return prisma.refreshToken.findUnique({
      where: { tokenHash },
      include: { employee: true },
    });
  }

  async revokeRefreshToken(rawToken: string) {
    const tokenHash = crypto.createHash('sha256').update(rawToken).digest('hex');
    await prisma.refreshToken.updateMany({
      where: { tokenHash },
      data: { revokedAt: new Date() },
    });
  }

  async revokeAllForEmployee(employeeId: string) {
    await prisma.refreshToken.updateMany({
      where: { employeeId, revokedAt: null },
      data: { revokedAt: new Date() },
    });
  }
}
