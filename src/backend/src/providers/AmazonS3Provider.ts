import {
  S3Client,
  PutObjectCommand,
  DeleteObjectCommand,
  HeadObjectCommand,
  GetObjectCommand,
} from '@aws-sdk/client-s3';
import { getSignedUrl } from '@aws-sdk/s3-request-presigner';
import { config } from '../config';

export interface S3UploadResult {
  bucket: string;
  key: string;
  url: string;
  etag: string;
  sizeBytes: number;
}

export class AmazonS3Provider {
  private readonly client: S3Client;
  private readonly bucket: string;

  constructor() {
    this.bucket = config.s3BucketName;
    this.client = new S3Client({
      region: config.s3Region,
      credentials: {
        accessKeyId: config.s3AccessKeyId,
        secretAccessKey: config.s3SecretAccessKey,
      },
    });
  }

  async uploadFile(
    key: string,
    body: Buffer,
    contentType: string,
    metadata: Record<string, string> = {}
  ): Promise<S3UploadResult> {
    const command = new PutObjectCommand({
      Bucket: this.bucket,
      Key: key,
      Body: body,
      ContentType: contentType,
      Metadata: metadata,
      ServerSideEncryption: 'AES256',
    });

    const response = await this.client.send(command);

    return {
      bucket: this.bucket,
      key,
      url: this.buildObjectUrl(key),
      etag: response.ETag?.replace(/"/g, '') ?? '',
      sizeBytes: body.length,
    };
  }

  async deleteFile(key: string): Promise<void> {
    await this.client.send(
      new DeleteObjectCommand({ Bucket: this.bucket, Key: key })
    );
  }

  async objectExists(key: string): Promise<boolean> {
    try {
      await this.client.send(
        new HeadObjectCommand({ Bucket: this.bucket, Key: key })
      );
      return true;
    } catch {
      return false;
    }
  }

  buildObjectKey(companyId: string, employeeId: string, capturedAt: Date, fileName: string): string {
    const y = capturedAt.getUTCFullYear();
    const mo = String(capturedAt.getUTCMonth() + 1).padStart(2, '0');
    const d = String(capturedAt.getUTCDate()).padStart(2, '0');
    const h = String(capturedAt.getUTCHours()).padStart(2, '0');
    const mi = String(capturedAt.getUTCMinutes()).padStart(2, '0');
    return `${companyId}/${employeeId}/${y}/${mo}/${d}/${h}/${mi}/${fileName}`;
  }

  async getSignedUrl(key: string, expiresInSeconds = 300): Promise<string> {
    const command = new GetObjectCommand({ Bucket: this.bucket, Key: key });
    return getSignedUrl(this.client, command, { expiresIn: expiresInSeconds });
  }

  private buildObjectUrl(key: string): string {
    return `https://${this.bucket}.s3.${config.s3Region}.amazonaws.com/${key}`;
  }
}
