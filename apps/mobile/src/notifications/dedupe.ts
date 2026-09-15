/**
 * One notification can reach a foregrounded app twice: over the live
 * connection the moment it commits, and as a push a second or two later. The
 * first one to arrive shows; the other is recognised by its id and dropped.
 * Ids are kept for a few minutes only — the set never has to be cleared.
 */
const TTL_MS = 5 * 60 * 1000;
const seen = new Map<string, number>();

export function markShown(notificationId: string): void {
  prune();
  seen.set(notificationId, Date.now());
}

export function wasShown(notificationId: string | undefined | null): boolean {
  if (!notificationId) {
    return false;
  }
  prune();
  return seen.has(notificationId);
}

function prune(): void {
  const cutoff = Date.now() - TTL_MS;
  for (const [id, at] of seen) {
    if (at < cutoff) {
      seen.delete(id);
    }
  }
}
