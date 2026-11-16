/**
 * Manually run screenshot retention (e.g. to clear a backlog).
 *
 *   npx ts-node --transpile-only src/scripts/purge-old-screenshots.ts --days=60            # dry run (default)
 *   npx ts-node --transpile-only src/scripts/purge-old-screenshots.ts --days=60 --execute  # really delete
 */
import { purgeOldScreenshots, formatGb } from '../services/retention.service';
import { prisma } from '../lib/prisma';

const arg = (name: string) => process.argv.find((a) => a.startsWith(`--${name}=`))?.split('=')[1];

async function main() {
  const retentionDays = parseInt(arg('days') ?? '60', 10);
  const execute = process.argv.includes('--execute');
  const maxBatches = arg('max-batches') ? parseInt(arg('max-batches')!, 10) : Infinity;

  const started = Date.now();
  const r = await purgeOldScreenshots({ retentionDays, dryRun: !execute, maxBatches });
  console.log(JSON.stringify({
    mode: execute ? 'EXECUTE' : 'DRY RUN',
    cutoff: r.cutoff.toISOString(),
    candidates: r.candidates,
    candidateSize: formatGb(r.candidateBytes),
    deletedFiles: r.deletedFiles,
    deletedSize: formatGb(r.deletedBytes),
    failed: r.failed,
    seconds: Math.round((Date.now() - started) / 1000),
  }, null, 2));
}

main()
  .catch((e) => { console.error(e); process.exitCode = 1; })
  .finally(() => prisma.$disconnect());
