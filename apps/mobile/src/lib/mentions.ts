/**
 * Mentions in event chat (D-112). The body carries "@Name" exactly as the
 * person picked it; the message carries who each name means. These helpers
 * keep the two in step while typing, and find the names again for display.
 */

/** Longest "@query" we treat as a mention still being typed. */
const MAX_QUERY = 30;

/**
 * The "@..." being typed at the cursor, if any: an "@" at the start or after
 * a space, with no line break or second "@" between it and the cursor.
 */
export function mentionQueryAt(text: string, cursor: number): { start: number; query: string } | null {
  const before = text.slice(0, cursor);
  const at = before.lastIndexOf('@');
  if (at < 0) {
    return null;
  }
  if (at > 0 && !/\s/.test(before[at - 1])) {
    return null;
  }
  const query = before.slice(at + 1);
  if (query.length > MAX_QUERY || /[\n@]/.test(query)) {
    return null;
  }
  return { start: at, query };
}

/** Replaces the "@query" at the cursor with "@Name " and says where the cursor goes. */
export function insertMention(
  text: string,
  start: number,
  cursor: number,
  name: string,
): { text: string; cursor: number } {
  const after = text.slice(cursor);
  // A space after the name to carry on typing, unless one is already there.
  const inserted = /^\s/.test(after) ? `@${name}` : `@${name} `;
  return {
    text: text.slice(0, start) + inserted + after,
    cursor: start + inserted.length,
  };
}

/** The people picked while writing whose "@Name" is still in the text at send. */
export function mentionsStillIn(body: string, picked: ReadonlyMap<string, string>): string[] {
  return [...picked.entries()].filter(([, name]) => body.includes(`@${name}`)).map(([userId]) => userId);
}

export type BodySegment = { text: string; mention: boolean };

/**
 * The body cut into plain text and "@Name" runs, for the names given. Longer
 * names are tried first, so "@Ali Khan" is not read as "@Ali" and " Khan".
 */
export function splitMentions(body: string, names: readonly string[]): BodySegment[] {
  const tokens = [...new Set(names.filter((name) => name.trim().length > 0))]
    .sort((a, b) => b.length - a.length)
    .map((name) => `@${name}`);
  if (tokens.length === 0) {
    return [{ text: body, mention: false }];
  }

  const segments: BodySegment[] = [];
  let plain = '';
  let index = 0;
  while (index < body.length) {
    const token = tokens.find((candidate) => body.startsWith(candidate, index));
    if (token) {
      if (plain) {
        segments.push({ text: plain, mention: false });
        plain = '';
      }
      segments.push({ text: token, mention: true });
      index += token.length;
    } else {
      plain += body[index];
      index += 1;
    }
  }
  if (plain) {
    segments.push({ text: plain, mention: false });
  }
  return segments;
}
