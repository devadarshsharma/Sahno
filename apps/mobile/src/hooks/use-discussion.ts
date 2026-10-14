import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useMemo } from 'react';

import { ApiError } from '@/api/client';
import {
  listDiscussion,
  markDiscussionRead,
  postDiscussionMessage,
  type DiscussionMessage,
  type DiscussionQuote,
} from '@/api/discussion';
import { useActiveOrg } from '@/hooks/use-organisations';
import { dismissChatNotifications } from '@/notifications/chat-actions';
import { useSession } from '@/providers/auth-provider';
import { newClientId, useChatOutbox } from '@/stores/chat-outbox';

/**
 * The thread for one engagement, oldest first. Access follows the engagement,
 * so anyone who can open the booking can read this — there is no separate
 * permission to check on the client.
 */
export function useDiscussion(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'discussion'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listDiscussion(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
    // Opening a chat always asks for what is new (D-106). The copy from the
    // last visit shows meanwhile, but the inbox may already be showing a
    // message this copy predates — trusting a 30-second-old thread is what
    // made "there is a message, but the chat does not have it".
    staleTime: 0,
    refetchOnMount: 'always',
  });
}

/**
 * Runs a discussion change and refreshes just the thread. Nothing here moves
 * readiness or the engagement list, so there is no reason to invalidate them.
 */
export function useDiscussionMutation<TArgs>(
  engagementId: string,
  action: (
    accessToken: string,
    organisationId: string,
    args: TArgs,
  ) => Promise<unknown>,
) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (args: TArgs) => {
      const accessToken = await session.getAccessToken();
      return action(accessToken, active!.id, args);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active?.id, 'engagements', engagementId, 'discussion'],
      }),
  });
}

/** How long a send may hang on a dead connection before it counts as failed. */
const SEND_TIMEOUT_MS = 20_000;

/** One send at a time per chat, so messages arrive in the order they were written. */
const sendQueues = new Map<string, Promise<void>>();

/**
 * Sending messages in one chat (D-087). A message joins the thread at once,
 * with a clock, and is posted in the background; once the server has it, it
 * becomes an ordinary message with a tick. If it fails it stays in the
 * thread as "Not sent" — tap to retry — and goes again by itself when the
 * thread next loads, which is the first sign the connection is back.
 */
export function useChatSender(engagementId: string) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();
  const organisationId = active?.id ?? null;
  const getAccessToken = session.getAccessToken;

  const all = useChatOutbox((state) => state.messages);
  const outgoing = useMemo(
    () =>
      all.filter(
        (message) => message.engagementId === engagementId && message.organisationId === organisationId,
      ),
    [all, engagementId, organisationId],
  );

  const deliver = useCallback(
    (clientId: string) => {
      const queued = sendQueues.get(engagementId) ?? Promise.resolve();
      const next = queued.then(async () => {
        const outbox = useChatOutbox.getState();
        const message = outbox.messages.find((row) => row.clientId === clientId);
        if (!message) {
          return;
        }

        outbox.update(clientId, { state: 'sending' });
        const timeout = new AbortController();
        const timer = setTimeout(() => timeout.abort(), SEND_TIMEOUT_MS);
        try {
          const accessToken = await getAccessToken();
          const posted = await postDiscussionMessage(
            accessToken,
            message.organisationId,
            message.engagementId,
            message.body,
            message.replyTo?.id ?? null,
            message.clientId,
            timeout.signal,
          );

          const key = ['org', message.organisationId, 'engagements', message.engagementId, 'discussion'];
          queryClient.setQueryData<DiscussionMessage[]>(key, (thread) =>
            thread && !thread.some((row) => row.id === posted.id)
              ? [...thread, { ...posted, replyTo: message.replyTo, reactions: [] }]
              : thread,
          );
          useChatOutbox.getState().remove(clientId);
          void queryClient.invalidateQueries({ queryKey: key });
          void queryClient.invalidateQueries({ queryKey: ['org', message.organisationId, 'chats'] });
        } catch (error) {
          useChatOutbox.getState().update(clientId, {
            state: 'failed',
            retryable: !(error instanceof ApiError) || error.status >= 500,
          });
        } finally {
          clearTimeout(timer);
        }
      });
      sendQueues.set(engagementId, next);
    },
    [engagementId, getAccessToken, queryClient],
  );

  const send = useCallback(
    (body: string, replyTo: DiscussionQuote | null) => {
      if (organisationId === null) {
        return;
      }
      const clientId = newClientId();
      useChatOutbox.getState().add({
        clientId,
        organisationId,
        engagementId,
        body,
        replyTo,
        queuedAtUtc: new Date().toISOString(),
        state: 'sending',
        retryable: true,
      });
      deliver(clientId);
    },
    [deliver, engagementId, organisationId],
  );

  const discard = useCallback((clientId: string) => useChatOutbox.getState().remove(clientId), []);

  // The thread loading is the sign the connection works: send again whatever
  // failed for want of one.
  const thread = useDiscussion(engagementId);
  useEffect(() => {
    if (!thread.isSuccess) {
      return;
    }
    useChatOutbox
      .getState()
      .messages.filter(
        (message) =>
          message.engagementId === engagementId &&
          message.organisationId === organisationId &&
          message.state === 'failed' &&
          message.retryable,
      )
      .forEach((message) => deliver(message.clientId));
  }, [thread.isSuccess, thread.dataUpdatedAt, deliver, engagementId, organisationId]);

  return { outgoing, send, retry: deliver, discard };
}

/**
 * While a chat is on screen, what is in it has been read. Marks it so on
 * opening, and again whenever a newer message arrives while it is open, then
 * refreshes the event list (the unread bubbles) and the bell (the same rows).
 */
export function useMarkDiscussionRead(engagementId: string) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();
  const thread = useDiscussion(engagementId);

  const organisationId = active?.id ?? null;
  const latestMessageId = thread.data?.at(-1)?.id ?? null;
  const getAccessToken = session.getAccessToken;

  useEffect(() => {
    if (organisationId === null || !thread.isSuccess) {
      return;
    }

    let cancelled = false;
    void (async () => {
      try {
        const accessToken = await getAccessToken();
        await markDiscussionRead(accessToken, organisationId, engagementId);
        // Read here, so nothing about this chat needs to wait in the tray.
        void dismissChatNotifications(engagementId).catch(() => undefined);
        if (cancelled) {
          return;
        }
        // The list (not the thread under it), the Chat inbox, and the bell.
        await Promise.all([
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'engagements'],
            exact: true,
          }),
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'chats'],
          }),
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'notifications'],
          }),
        ]);
      } catch {
        // A bubble left on is the worst outcome; the next open clears it.
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [organisationId, engagementId, latestMessageId, thread.isSuccess, getAccessToken, queryClient]);
}
