// Repairs uploaded_files.capture_time_utc for rows where the agent sent no
// capturedAt and the value was stamped at upload time instead of capture time.
// The real capture time is embedded in the filename inside s3_object_key:
// {unixSeconds}_{correlationId}.{ext} — the same timestamp the agent generated.
//
// Paginates by id cursor (constant-time batches) and writes each batch in one
// UPDATE ... FROM (VALUES ...) to stay under pooler statement timeouts.
// Saves progress to .repair-cursor so a dropped connection can resume.
//
// Usage:
//   node dist/scripts/repair-capture-times.js           # dry run (default)
//   node dist/scripts/repair-capture-times.js --apply   # writes fixes

import 'dotenv/config';
import fs from 'fs';
import path from 'path';
import { prisma } from '../lib/prisma';
import { parseCaptureTimeFromFileName } from '../utils/captureTime.util';

const APPLY = process.argv.includes('--apply');
const BATCH = 5000;

const parseArg = (prefix: string): string | undefined => {
  const arg = process.argv.find((a) => a.startsWith(prefix + '='));
  return arg ? arg.slice(prefix.length + 1) : undefined;
};

const parseSince = (): Date | undefined => {
  const raw = parseArg('--uploaded-since');
  if (!raw) return undefined;
  if (raw.endsWith('h')) {
    const hours = parseFloat(raw.slice(0, -1));
    if (!Number.isNaN(hours)) return new Date(Date.now() - hours * 60 * 60 * 1000);
  }
  const d = new Date(raw);
  return Number.isNaN(d.getTime()) ? undefined : d;
};

const since = parseSince();
const TOLERANCE_MS = 60 * 1000;
const CURSOR_FILE = path.join(__dirname, '.repair-cursor');

const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

const withRetry = async <T>(fn: () => Promise<T>): Promise<T> => {
  for (let attempt = 1; ; attempt++) {
    try {
      return await fn();
    } catch (err) {
      if (attempt >= 5) throw err;
      console.warn(`batch failed (attempt ${attempt}), retrying in ${attempt * 2}s...`);
      await sleep(attempt * 2000);
    }
  }
};

const main = async () => {
  let cursor: string | undefined;
  if (since) {
    console.log('uploaded-since filter active: ignoring cursor file');
  } else if (APPLY && fs.existsSync(CURSOR_FILE)) {
    cursor = fs.readFileSync(CURSOR_FILE, 'utf8').trim() || undefined;
    if (cursor) console.log(`resuming from cursor ${cursor}`);
  }

  let scanned = 0;
  let mismatched = 0;
  let repaired = 0;
  let unparseable = 0;

  if (since) console.log(`restricting to rows uploaded since ${since.toISOString()}`);

  for (;;) {
    const files = await withRetry(() =>
      prisma.uploadedFile.findMany({
        where: since ? { uploadedAt: { gte: since } } : undefined,
        select: { id: true, s3ObjectKey: true, captureTimeUtc: true },
        orderBy: { id: 'asc' },
        take: BATCH,
        ...(cursor ? { skip: 1, cursor: { id: cursor } } : {}),
      })
    );

    if (files.length === 0) break;
    cursor = files[files.length - 1].id;

    const fixes: { id: string; ts: number }[] = [];
    for (const file of files) {
      scanned++;
      const realCapture = parseCaptureTimeFromFileName(file.s3ObjectKey);
      if (!realCapture) {
        unparseable++;
        continue;
      }
      if (Math.abs(file.captureTimeUtc.getTime() - realCapture.getTime()) <= TOLERANCE_MS) {
        continue;
      }
      mismatched++;
      fixes.push({ id: file.id, ts: Math.floor(realCapture.getTime() / 1000) });
    }

    if (APPLY && fixes.length > 0) {
      const values = fixes.map((f) => `('${f.id}', ${f.ts})`).join(',');
      repaired += await withRetry(() =>
        prisma.$executeRawUnsafe(`
          UPDATE rdcs.uploaded_files AS f
          SET capture_time_utc = to_timestamp(v.ts)
          FROM (VALUES ${values}) AS v(id, ts)
          WHERE f.id = v.id
        `)
      );
    }

    // Only advance the resume cursor after this batch's write succeeded
    if (APPLY) fs.writeFileSync(CURSOR_FILE, cursor);

    if (scanned % 50000 < BATCH) {
      console.log(`scanned=${scanned} mismatched=${mismatched} repaired=${repaired}`);
    }
  }

  if (APPLY && fs.existsSync(CURSOR_FILE)) fs.unlinkSync(CURSOR_FILE);

  console.log(
    `\ndone: scanned=${scanned} mismatched=${mismatched} unparseableNames=${unparseable} repaired=${repaired}` +
      (APPLY ? '' : ' (dry run — pass --apply to write)')
  );
};

main()
  .catch((err) => {
    console.error('repair failed:', err);
    process.exitCode = 1;
  })
  .finally(() => prisma.$disconnect());
