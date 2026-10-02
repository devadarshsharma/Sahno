import type { DiscussionMessage } from '@/api/discussion';

/**
 * Name colours, one per person, readable on white and on the soft teal of
 * your own bubbles. Picked from the author's id so a person keeps their
 * colour in every chat and on every phone.
 */
const NAME_COLOURS = [
  '#1F6A65',
  '#B4530A',
  '#7A3E9D',
  '#2F6DB5',
  '#B23A48',
  '#3C7D3A',
  '#8A6D1F',
  '#5A4FCF',
];

export function nameColour(userId: string): string {
  let hash = 0;
  for (let index = 0; index < userId.length; index++) {
    hash = (hash * 31 + userId.charCodeAt(index)) | 0;
  }
  return NAME_COLOURS[Math.abs(hash) % NAME_COLOURS.length];
}

/** What the thread list renders: messages, and a separator where the day changes. */
export type ChatItem =
  | {
      kind: 'message';
      key: string;
      message: DiscussionMessage;
      /** First of a run from the same person: their name is shown. */
      startsRun: boolean;
    }
  | { kind: 'day'; key: string; label: string };

/** Consecutive messages from one person this close together read as one run. */
const RUN_GAP_MS = 5 * 60 * 1000;

/**
 * Turns a thread (oldest first) into list items NEWEST first, for an inverted
 * list that opens at the latest message. A day separator follows the oldest
 * message of its day in this order, which puts it above that day on screen.
 */
export function chatItems(thread: DiscussionMessage[]): ChatItem[] {
  const items: ChatItem[] = [];

  thread.forEach((message, index) => {
    const previous = index > 0 ? thread[index - 1] : null;
    const day = dayKey(message.postedAtUtc);

    if (!previous || dayKey(previous.postedAtUtc) !== day) {
      items.push({ kind: 'day', key: `day-${day}`, label: dayLabel(message.postedAtUtc) });
    }

    const startsRun =
      !previous ||
      previous.authorUserId !== message.authorUserId ||
      dayKey(previous.postedAtUtc) !== day ||
      new Date(message.postedAtUtc).getTime() - new Date(previous.postedAtUtc).getTime() > RUN_GAP_MS;

    items.push({ kind: 'message', key: message.id, message, startsRun });
  });

  return items.reverse();
}

function dayKey(isoUtc: string): string {
  const date = new Date(isoUtc);
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

/** "Today", "Yesterday", the weekday this week, then the date. */
function dayLabel(isoUtc: string): string {
  const date = new Date(isoUtc);
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  const dayMs = 24 * 60 * 60 * 1000;
  const at = new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();

  if (at === startOfToday) {
    return 'Today';
  }
  if (at === startOfToday - dayMs) {
    return 'Yesterday';
  }
  if (at > startOfToday - 7 * dayMs) {
    return date.toLocaleDateString(undefined, { weekday: 'long' });
  }
  return date.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: date.getFullYear() === now.getFullYear() ? undefined : 'numeric',
  });
}

/** "10:48 AM" — the time inside a bubble. */
export function messageTime(isoUtc: string): string {
  return new Date(isoUtc).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

/** Whose a message is, as a label: "You", their name, or "Someone". */
export function authorName(message: Pick<DiscussionMessage, 'isYours' | 'authorDisplayName'>): string {
  return message.isYours ? 'You' : (message.authorDisplayName ?? 'Someone');
}

/** What a removed message says in its place. */
export function removedText(message: DiscussionMessage): string {
  if (message.wasModerated) {
    return 'Removed by an organiser';
  }
  return message.isYours ? 'You deleted this message' : 'This message was deleted';
}
