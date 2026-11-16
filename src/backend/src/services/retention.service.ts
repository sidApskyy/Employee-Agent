import { prisma } from '../lib/prisma';
import { AmazonS3Provider } from '../providers/AmazonS3Provider';

// Screenshot retention: removes screenshots older than N days from S3 and
// their uploaded_files rows (upload_audits cascade). The S3 object is always
// deleted first; a row is only removed once its object is confirmed gone, so
// a failure can never leave an image in S3 that nothing points to.

export interface RetentionOptions {
  retentionDays: number;
  batchSize?: number;
  dryRun?: boolean;
  maxBatches?: number;
  pauseMs?: number;
}

export interface RetentionResult {
  cutoff: Date;
  candidates: number;
  candidateBytes: number;
  deletedFiles: number;
  deletedBytes: number;
  failed: number;
  dryRun: boolean;
}

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

export async function purgeOldScreenshots(
  options: RetentionOptions,
  s3: AmazonS3Provider = new AmazonS3Provider()
): Promise<RetentionResult> {
  const { retentionDays, batchSize = 500, dryRun = false, maxBatches = Infinity, pauseMs = 250 } = options;
  if (!Number.isFinite(retentionDays) || retentionDays < 7) {
    throw new Error(`Refusing to run screenshot retention with retentionDays=${retentionDays} (minimum 7)`);
  }

  const cutoff = new Date(Date.now() - retentionDays * 24 * 60 * 60 * 1000);
  // Only rows stored in the bucket this backend can delete from
  const where = { captureTimeUtc: { lt: cutoff }, s3Bucket: s3.bucketName };

  const stats = await prisma.uploadedFile.aggregate({ where, _count: { _all: true }, _sum: { fileSize: true } });
  const result: RetentionResult = {
    cutoff,
    candidates: stats._count._all,
    candidateBytes: stats._sum.fileSize ?? 0,
    deletedFiles: 0,
    deletedBytes: 0,
    failed: 0,
    dryRun,
  };
  if (dryRun || result.candidates === 0) return result;

  const failedKeys = new Set<string>();
  for (let batch = 0; batch < maxBatches; batch++) {
    const rows = await prisma.uploadedFile.findMany({
      where,
      select: { id: true, s3ObjectKey: true, fileSize: true },
      orderBy: { captureTimeUtc: 'asc' },
      take: Math.min(batchSize, 1000),
    });
    if (rows.length === 0) break;

    const { deleted, failed } = await s3.deleteFiles(rows.map((r) => r.s3ObjectKey));
    const gone = new Set(deleted);
    const removable = rows.filter((r) => gone.has(r.s3ObjectKey));

    if (removable.length > 0) {
      await prisma.uploadedFile.deleteMany({ where: { id: { in: removable.map((r) => r.id) } } });
      result.deletedFiles += removable.length;
      result.deletedBytes += removable.reduce((sum, r) => sum + (r.fileSize ?? 0), 0);
    }
    const newFailures = failed.filter((f) => !failedKeys.has(f.key));
    newFailures.forEach((f) => failedKeys.add(f.key));
    if (newFailures.length) console.warn(`[Retention] S3 refused ${newFailures.length} deletions, e.g. ${newFailures[0].key} (${newFailures[0].code})`);

    // Nothing could be deleted in this batch — stop instead of retrying the same rows forever
    if (removable.length === 0) break;
    if (pauseMs) await sleep(pauseMs);
  }

  result.failed = failedKeys.size;
  return result;
}

const formatGb = (bytes: number) => `${(bytes / 1024 ** 3).toFixed(2)} GB`;

/** Daily background job. Disabled when SCREENSHOT_RETENTION_DAYS=0. */
export function startScreenshotRetentionJob(retentionDays: number): void {
  if (!retentionDays) {
    console.log('[Retention] Screenshot retention disabled (SCREENSHOT_RETENTION_DAYS=0)');
    return;
  }
  let running = false;
  const run = async () => {
    if (running) return;
    running = true;
    try {
      const r = await purgeOldScreenshots({ retentionDays });
      if (r.deletedFiles || r.failed) {
        console.log(`[Retention] Deleted ${r.deletedFiles} screenshots (${formatGb(r.deletedBytes)}) older than ${retentionDays} days; ${r.failed} failed`);
      }
    } catch (error) {
      console.error('[Retention] Screenshot retention run failed:', error);
    } finally {
      running = false;
    }
  };
  setTimeout(run, 5 * 60 * 1000);
  setInterval(run, 24 * 60 * 60 * 1000);
  console.log(`[Retention] Screenshot retention active: keeping ${retentionDays} days`);
}

export { formatGb };
