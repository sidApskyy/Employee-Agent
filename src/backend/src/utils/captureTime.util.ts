// The desktop agent names capture files as `{unixSeconds}_{correlationId}.{ext}`
// (see StoragePathHelper.GenerateFileName). The leading segment is the real
// capture time and stays correct even when an upload is retried days later.

const FILENAME_TS_REGEX = /^(\d{9,10})_[0-9a-z-]+\.[a-z0-9]+$/i;

export const parseCaptureTimeFromFileName = (name: string): Date | null => {
  const base = name.split(/[\\/]/).pop() ?? name;
  const match = base.match(FILENAME_TS_REGEX);
  if (!match) return null;

  const seconds = parseInt(match[1], 10);
  const date = new Date(seconds * 1000);
  return Number.isNaN(date.getTime()) ? null : date;
};

export const resolveCaptureTime = (
  capturedAt: unknown,
  originalName: string
): { time: Date; estimated: boolean } => {
  if (capturedAt) {
    const parsed = new Date(String(capturedAt));
    if (!Number.isNaN(parsed.getTime())) {
      return { time: parsed, estimated: false };
    }
  }

  // Agent sent no capturedAt (older agent version or backlog upload):
  // recover the real capture time from the filename instead of stamping now.
  const fromName = parseCaptureTimeFromFileName(originalName);
  if (fromName) {
    return { time: fromName, estimated: true };
  }

  return { time: new Date(), estimated: true };
};
