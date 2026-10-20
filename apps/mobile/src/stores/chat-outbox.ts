import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import type { DiscussionQuote } from '@/api/discussion';

/**
 * A chat message written on this phone that the server has not yet
 * confirmed (D-087). It shows in the thread straight away with a clock; if
 * sending fails it stays, marked "Not sent", until it is retried or
 * discarded — a dropped connection never loses what somebody typed.
 *
 * `clientId` becomes the message's id on the server, which is what makes a
 * retry safe: if the first attempt did get through, the server recognises
 * the id and does not post it twice.
 */
export type OutgoingMessage = {
  clientId: string;
  organisationId: string;
  engagementId: string;
  body: string;
  replyTo: DiscussionQuote | null;
  /** Who the "@Name"s in the body are (D-112). Absent from messages queued by older builds. */
  mentionedUserIds?: string[];
  queuedAtUtc: string;
  state: 'sending' | 'failed';
  /**
   * Worth retrying on its own when the connection comes back: no answer, or
   * the server's fault. A refusal (lost access, say) waits for the person.
   */
  retryable: boolean;
};

type ChatOutboxState = {
  messages: OutgoingMessage[];
  add: (message: OutgoingMessage) => void;
  update: (clientId: string, changes: Partial<OutgoingMessage>) => void;
  remove: (clientId: string) => void;
};

export const useChatOutbox = create<ChatOutboxState>()(
  persist(
    (set) => ({
      messages: [],
      add: (message) => set((state) => ({ messages: [...state.messages, message] })),
      update: (clientId, changes) =>
        set((state) => ({
          messages: state.messages.map((message) =>
            message.clientId === clientId ? { ...message, ...changes } : message,
          ),
        })),
      remove: (clientId) =>
        set((state) => ({ messages: state.messages.filter((message) => message.clientId !== clientId) })),
    }),
    {
      name: 'sahno.chat-outbox',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({ messages: state.messages }),
      // Whatever was mid-send when the app closed is unconfirmed: it shows as
      // not sent, and goes again the next time its chat is open.
      merge: (persisted, current) => ({
        ...current,
        messages: ((persisted as Partial<ChatOutboxState> | undefined)?.messages ?? []).map((message) =>
          message.state === 'sending' ? { ...message, state: 'failed' as const, retryable: true } : message,
        ),
      }),
    },
  ),
);

/** A random v4 UUID. Not a secret — only unique enough to name one message. */
export function newClientId(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const random = (Math.random() * 16) | 0;
    return (char === 'x' ? random : (random & 0x3) | 0x8).toString(16);
  });
}
