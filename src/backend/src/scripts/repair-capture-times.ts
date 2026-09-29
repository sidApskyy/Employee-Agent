// Repairs uploaded_files.capture_time_utc for rows where the agent sent no
// capturedAt and the value was stamped at upload time instead of capture time.
// The real capture time is embedded in the filename inside s3_object_key:
// {unixSeconds}_{correlationId}.{ext} — the same timestamp the agent generated.
//
// Usage:
//   node dist/scripts/repair-capture-times.js           # dry run (default)
//   node dist/scripts/repair-capture-times.js --apply   # writes fixes

import 'dotenv/config';
import { prisma } from '../lib/prisma';
import { parseCaptureTimeFromFileName } from '../utils/captureTime.util';

const APPLY = process.argv.includes('--apply');
const BATCH = 500;
const TOLERANCE_MS = 60 * 1000; // only fix rows off by more than a minute

const main = async () => {
  let cursor: string | undefined;
  let scanned = 0;
  let mismatched = 0;
  let repaired = 0;
  let unparseable = 0;

  for (;;) {
    const files = await prisma.uploadedFile.findMany({
      select: { id: true, s3ObjectKey: true, captureTimeUtc: true, uploadedAt: true },
      orderBy: { id: 'asc' },
      take: BATCH,
      ...(cursor ? { skip: 1, cursor: { id: cursor } } : {}),
    });

    if (files.length === 0) break;
    cursor = files[files.length - 1].id;

    for (const file of files) {
      scanned++;
      const realCapture = parseCaptureTimeFromFileName(file.s3ObjectKey);
      if (!realCapture) {
        unparseable++;
        continue;
      }

      const drift = Math.abs(file.captureTimeUtc.getTime() - realCapture.getTime());
      if (drift <= TOLERANCE_MS) continue;

      mismatched++;
      console.log(
        `[mismatch] id=${file.id} stored=${file.captureTimeUtc.toISOString()} real=${realCapture.toISOString()} uploaded=${file.uploadedAt.toISOString()}`
      );

      if (APPLY) {
        await prisma.uploadedFile.update({
          where: { id: file.id },
          data: { captureTimeUtc: realCapture },
        });
        repaired++;
      }
    }
  }

  console.log(`\nscanned=${scanned} mismatched=${mismatched} unparseableNames=${unparseable} repaired=${repaired}${APPLY ? '' : ' (dry run — pass --apply to write)'}`);
};

main()
  .catch((err) => {
    console.error('repair failed:', err);
    process.exitCode = 1;
  })
  .finally(() => prisma.$disconnect());
