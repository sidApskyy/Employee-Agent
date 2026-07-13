import crypto from 'crypto';
import path from 'path';
import { AmazonS3Provider } from '../providers/AmazonS3Provider';
import { StorageRepository } from '../repositories/storage.repository';

export interface UploadFileInput {
  jobId: string;
  correlationId: string;
  employeeId: string;
  companyId: string;
  deviceId: string;
  fileBuffer: Buffer;
  originalName: string;
  contentType: string;
  checksum: string;
  fileSize: number;
  capturedAt: Date;
}

export interface UploadFileResult {
  uploadId: string;
  s3ObjectKey: string;
  s3Url: string;
  status: string;
  checksumVerified: boolean;
}

export class StorageService {
  private readonly s3: AmazonS3Provider;
  private readonly repo: StorageRepository;

  constructor() {
    this.s3 = new AmazonS3Provider();
    this.repo = new StorageRepository();
  }

  async uploadScreenshot(input: UploadFileInput): Promise<UploadFileResult> {
    const serverChecksum = crypto
      .createHash('sha256')
      .update(input.fileBuffer)
      .digest('hex')
      .toLowerCase();

    const checksumVerified =
      input.checksum.toLowerCase() === serverChecksum;

    const fileName = path.basename(input.originalName);
    const s3Key = this.s3.buildObjectKey(
      input.companyId,
      input.employeeId,
      input.capturedAt,
      fileName
    );

    const s3Result = await this.s3.uploadFile(s3Key, input.fileBuffer, input.contentType, {
      jobId: input.jobId,
      employeeId: input.employeeId,
      deviceId: input.deviceId,
      checksum: serverChecksum,
    });

    const record = await this.repo.createUploadedFile({
      jobId: input.jobId,
      correlationId: input.correlationId,
      employeeId: input.employeeId,
      deviceId: input.deviceId,
      s3Bucket: s3Result.bucket,
      s3ObjectKey: s3Result.key,
      s3Url: s3Result.url,
      fileSize: s3Result.sizeBytes,
      checksum: serverChecksum,
      checksumVerified,
      metadata: { originalChecksum: input.checksum, etag: s3Result.etag },
    });

    await this.repo.createAuditLog({
      uploadId: record.id,
      action: 'upload',
      status: 'success',
      message: `Uploaded to S3: ${s3Key}`,
    });

    await this.repo.upsertStorageUsage(
      input.employeeId,
      input.companyId,
      input.capturedAt,
      s3Result.sizeBytes
    );

    return {
      uploadId: record.id,
      s3ObjectKey: s3Result.key,
      s3Url: s3Result.url,
      status: 'uploaded',
      checksumVerified,
    };
  }

  async confirmComplete(jobId: string): Promise<void> {
    const record = await this.repo.findByJobId(jobId);
    if (!record) throw new Error(`Upload record not found for jobId: ${jobId}`);

    await this.repo.markCompleted(record.id);
    await this.repo.createAuditLog({
      uploadId: record.id,
      action: 'complete',
      status: 'completed',
    });
  }

  async getStorageUsage(employeeId: string, from: Date, to: Date) {
    return this.repo.getStorageUsage(employeeId, from, to);
  }

  async listFiles(employeeId: string, limit = 50, offset = 0) {
    return this.repo.listUploadedFiles(employeeId, limit, offset);
  }
}
