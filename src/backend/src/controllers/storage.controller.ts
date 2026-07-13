import { Response } from 'express';
import { AuthRequest } from '../middleware/auth.middleware';
import { StorageService } from '../services/storage.service';
import { AmazonS3Provider } from '../providers/AmazonS3Provider';
import { successResponse, errorResponse } from '../utils/response.util';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();
const s3 = new AmazonS3Provider();

export class StorageController {
  private readonly storageService: StorageService;

  constructor(storageService?: StorageService) {
    this.storageService = storageService ?? new StorageService();
  }

  async uploadScreenshot(req: AuthRequest, res: Response) {
    try {
      const file = (req as any).file as Express.Multer.File;
      const {
        jobId,
        correlationId,
        employeeId,
        deviceId,
        checksum = '',
        fileSize,
        capturedAt,
      } = req.body;

      const companyId = req.user?.companyId ?? 'unknown';

      const result = await this.storageService.uploadScreenshot({
        jobId,
        correlationId,
        employeeId: employeeId ?? req.user?.employeeId,
        companyId,
        deviceId: deviceId ?? req.user?.deviceId,
        fileBuffer: file.buffer,
        originalName: file.originalname,
        contentType: file.mimetype,
        checksum,
        fileSize: parseInt(fileSize ?? file.size, 10),
        capturedAt: capturedAt ? new Date(capturedAt) : new Date(),
      });

      return res.status(201).json(successResponse(result, 'Screenshot uploaded successfully'));
    } catch (error: any) {
      console.error('[StorageController] uploadScreenshot error:', error);
      return res.status(500).json(errorResponse(error.message ?? 'Upload failed'));
    }
  }

  async completeUpload(req: AuthRequest, res: Response) {
    try {
      const { jobId } = req.body;
      await this.storageService.confirmComplete(jobId);
      return res.status(200).json(successResponse({ jobId }, 'Upload marked as completed'));
    } catch (error: any) {
      console.error('[StorageController] completeUpload error:', error);
      return res.status(500).json(errorResponse(error.message ?? 'Complete upload failed'));
    }
  }

  async getStorageUsage(req: AuthRequest, res: Response) {
    try {
      const employeeId = req.query.employeeId as string ?? req.user?.employeeId ?? '';
      const from = new Date((req.query.from as string) ?? new Date(Date.now() - 30 * 86400_000).toISOString());
      const to = new Date((req.query.to as string) ?? new Date().toISOString());

      const usage = await this.storageService.getStorageUsage(employeeId, from, to);
      return res.status(200).json(successResponse(usage));
    } catch (error: any) {
      return res.status(500).json(errorResponse(error.message ?? 'Failed to retrieve usage'));
    }
  }

  async listFiles(req: AuthRequest, res: Response) {
    try {
      const employeeId = req.query.employeeId as string ?? req.user?.employeeId ?? '';
      const limit = parseInt(req.query.limit as string ?? '50', 10);
      const offset = parseInt(req.query.offset as string ?? '0', 10);

      const files = await this.storageService.listFiles(employeeId, limit, offset);
      return res.status(200).json(successResponse(files));
    } catch (error: any) {
      return res.status(500).json(errorResponse(error.message ?? 'Failed to list files'));
    }
  }

  async viewScreenshot(req: AuthRequest, res: Response) {
    try {
      const { id } = req.params;

      const record = await prisma.uploadedFile.findUnique({ where: { id } });
      if (!record || !record.s3ObjectKey) {
        return res.status(404).json(errorResponse('Screenshot not found'));
      }

      const signedUrl = await s3.getSignedUrl(record.s3ObjectKey, 300);
      return res.status(200).json(successResponse({ url: signedUrl, expiresInSeconds: 300 }));
    } catch (error: any) {
      console.error('[StorageController] viewScreenshot error:', error);
      return res.status(500).json(errorResponse(error.message ?? 'Failed to generate view URL'));
    }
  }
}
