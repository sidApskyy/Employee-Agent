import { prisma } from '../lib/prisma';

export class StorageRepository {
  async createUploadedFile(data: {
    jobId: string;
    correlationId: string;
    employeeId: string;
    deviceId: string;
    s3Bucket: string;
    s3ObjectKey: string;
    s3Url: string;
    fileSize: number;
    checksum: string;
    checksumVerified: boolean;
    metadata?: Record<string, any>;
  }) {
    return prisma.uploadedFile.create({ data });
  }

  async findByJobId(jobId: string) {
    return prisma.uploadedFile.findUnique({ where: { jobId } });
  }

  async markCompleted(id: string) {
    return prisma.uploadedFile.update({
      where: { id },
      data: { uploadStatus: 'completed', completedAt: new Date() },
    });
  }

  async markFailed(id: string, reason: string) {
    return prisma.uploadedFile.update({
      where: { id },
      data: { uploadStatus: 'failed', metadata: { failureReason: reason } },
    });
  }

  async createAuditLog(data: {
    uploadId: string;
    action: string;
    status: string;
    message?: string;
  }) {
    return prisma.uploadAudit.create({ data });
  }

  async upsertStorageUsage(employeeId: string, companyId: string, date: Date, sizeBytes: number) {
    const day = new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
    return prisma.storageUsage.upsert({
      where: { employeeId_date: { employeeId, date: day } },
      create: { employeeId, companyId, date: day, fileCount: 1, totalSizeBytes: sizeBytes },
      update: { fileCount: { increment: 1 }, totalSizeBytes: { increment: sizeBytes } },
    });
  }

  async getStorageUsage(employeeId: string, from: Date, to: Date) {
    return prisma.storageUsage.findMany({
      where: { employeeId, date: { gte: from, lte: to } },
      orderBy: { date: 'asc' },
    });
  }

  async listUploadedFiles(employeeId: string, limit = 50, offset = 0) {
    return prisma.uploadedFile.findMany({
      where: { employeeId },
      orderBy: { uploadedAt: 'desc' },
      take: limit,
      skip: offset,
    });
  }
}
