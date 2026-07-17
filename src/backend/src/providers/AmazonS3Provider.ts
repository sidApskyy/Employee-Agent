import {
  S3Client,
  PutObjectCommand,
  DeleteObjectCommand,
  HeadBucketCommand,
  HeadObjectCommand,
  GetObjectCommand,
  ListObjectsV2Command,
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

  async runStartupDiagnostics(): Promise<void> {
    console.log('[S3 Diagnostics] Bucket:', this.bucket);
    console.log('[S3 Diagnostics] Region:', config.s3Region);
    console.log('[S3 Diagnostics] Access Key:', this.maskAccessKey(config.s3AccessKeyId));

    let diagnosticFailure: unknown;

    try {
      const headBucketResponse = await this.client.send(new HeadBucketCommand({ Bucket: this.bucket }));
      console.log('[S3 Diagnostics] HeadBucket result:', {
        httpStatusCode: headBucketResponse.$metadata.httpStatusCode,
        requestId: headBucketResponse.$metadata.requestId,
        extendedRequestId: headBucketResponse.$metadata.extendedRequestId,
      });
    } catch (error) {
      this.logAwsError('HeadBucket failed', error);
      diagnosticFailure = error;
    }

    try {
      const listObjectsResponse = await this.client.send(
        new ListObjectsV2Command({ Bucket: this.bucket, MaxKeys: 5 })
      );
      console.log('[S3 Diagnostics] ListObjectsV2 result:', {
        httpStatusCode: listObjectsResponse.$metadata.httpStatusCode,
        requestId: listObjectsResponse.$metadata.requestId,
        extendedRequestId: listObjectsResponse.$metadata.extendedRequestId,
        keyCount: listObjectsResponse.KeyCount,
        contents: listObjectsResponse.Contents?.map((object) => ({
          key: object.Key,
          size: object.Size,
          lastModified: object.LastModified?.toISOString(),
          eTag: object.ETag,
        })) ?? [],
      });
    } catch (error) {
      this.logAwsError('ListObjectsV2 failed', error);
      diagnosticFailure ??= error;
    }

    if (diagnosticFailure) {
      throw diagnosticFailure;
    }
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
    } catch (error) {
      this.logAwsError(`HeadObject failed for key ${key}`, error);
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

    try {
      const signedUrl = await getSignedUrl(this.client, command, { expiresIn: expiresInSeconds });
      const credential = new URL(signedUrl).searchParams.get('X-Amz-Credential');
      console.log('[S3 Diagnostics] Presigned URL result:', {
        bucket: this.bucket,
        key,
        expirationSeconds: expiresInSeconds,
        credential,
        generatedUrl: signedUrl,
      });
      return signedUrl;
    } catch (error) {
      this.logAwsError(`GetSignedUrl failed for key ${key}`, error);
      throw error;
    }
  }

  private maskAccessKey(accessKeyId: string): string {
    return accessKeyId.length <= 6 ? accessKeyId : `${accessKeyId.slice(0, 6)}*******`;
  }

  private logAwsError(operation: string, error: unknown): void {
    const awsError = error as {
      name?: string;
      code?: string;
      message?: string;
      stack?: string;
      $metadata?: { httpStatusCode?: number; requestId?: string; extendedRequestId?: string };
    };

    console.error(`[S3 Diagnostics] ${operation}`, {
      awsErrorName: awsError.name,
      awsErrorCode: awsError.code,
      message: awsError.message,
      httpStatus: awsError.$metadata?.httpStatusCode,
      requestId: awsError.$metadata?.requestId,
      extendedRequestId: awsError.$metadata?.extendedRequestId,
      stackTrace: awsError.stack,
    });
  }

  private buildObjectUrl(key: string): string {
    return `https://${this.bucket}.s3.${config.s3Region}.amazonaws.com/${key}`;
  }
}
