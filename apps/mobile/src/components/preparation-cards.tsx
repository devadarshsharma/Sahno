import { useState } from 'react';
import { Linking, Pressable, StyleSheet, View } from 'react-native';

import {
  createRehearsal,
  createResource,
  deleteRehearsal,
  deleteResource,
  type EngagementResource,
  type Rehearsal,
  type ResourceAudience,
  type ResourceKind,
} from '@/api/preparation';
import {
  createResponsibility,
  deleteResponsibility,
  setResponsibilityProgress,
  updateResponsibility,
  type Responsibility,
} from '@/api/responsibilities';
import {
  Button,
  Card,
  DateField,
  Text,
  TextInput,
  TimeField,
  formatIsoDate,
  formatTime,
} from '@/components/ui';
import type { Participant } from '@/api/availability';
import { useAvailability } from '@/hooks/use-availability';
import {
  usePreparationMutation,
  useRehearsals,
  useResources,
  useResponsibilities,
} from '@/hooks/use-preparation';
import { colors, radii, spacing } from '@/theme';

/**
 * Who is doing or bringing what (D-047 §3).
 *
 * Everyone on the event reads the whole list, because a job list only stops
 * duplicated and dropped work if the group can see it. What each person may
 * change differs: an organiser writes and assigns the work, and the person
 * holding a job says whether it is done.
 */
export function ResponsibilitiesCard({
  engagementId,
  isOrganiser,
}: {
  engagementId: string;
  isOrganiser: boolean;
}) {
  const jobsQuery = useResponsibilities(engagementId);
  const [adding, setAdding] = useState(false);
  const [assigning, setAssigning] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const lineupQuery = useAvailability(engagementId);

  const progress = usePreparationMutation<{
    responsibilityId: string;
    isDone: boolean;
  }>((accessToken, organisationId, args) =>
    setResponsibilityProgress(
      accessToken,
      organisationId,
      engagementId,
      args.responsibilityId,
      args.isDone,
      null,
    ),
  );

  const remove = usePreparationMutation<string>(
    (accessToken, organisationId, responsibilityId) =>
      deleteResponsibility(
        accessToken,
        organisationId,
        engagementId,
        responsibilityId,
      ),
  );

  // Handing a job out later is the common case, not an edge one: an organiser
  // writes the list first and works out who is doing what afterwards. The
  // title travels with the change because the API takes the whole job.
  const assign = usePreparationMutation<{
    job: Responsibility;
    assignedUserId: string | null;
  }>((accessToken, organisationId, args) =>
    updateResponsibility(accessToken, organisationId, engagementId, args.job.id, {
      title: args.job.title,
      detail: args.job.detail,
      assignedUserId: args.assignedUserId,
    }),
  );

  if (!jobsQuery.isSuccess) {
    return null;
  }

  const jobs = jobsQuery.data;
  const outstanding = jobs.filter((job) => !job.isDone).length;
  const unassigned = jobs.filter((job) => job.assignedUserId === null).length;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Responsibilities</Text>
      <Text color="secondary" variant="bodySmall">
        {summariseJobs(jobs.length, outstanding, unassigned)}
      </Text>

      {jobs.length > 0 ? (
        <View style={styles.rows}>
          {jobs.map((job) => (
            <ResponsibilityRow
              key={job.id}
              job={job}
              // The tick is only yours to move if the job is yours. An
              // organiser may also record it, because they are often the one
              // who gets told in person.
              canTick={job.isYours || isOrganiser}
              canManage={isOrganiser}
              assigning={assigning === job.id}
              lineup={(lineupQuery.data?.participants ?? []).filter(
                (participant) => participant.isActive,
              )}
              busy={progress.isPending || remove.isPending || assign.isPending}
              onToggle={() =>
                progress.mutate(
                  { responsibilityId: job.id, isDone: !job.isDone },
                  { onError: () => setError('Could not save that.') },
                )
              }
              onStartAssigning={() =>
                setAssigning((current) => (current === job.id ? null : job.id))
              }
              onAssign={(assignedUserId) =>
                assign.mutate(
                  { job, assignedUserId },
                  {
                    onSuccess: () => setAssigning(null),
                    onError: () => setError('Could not hand that over.'),
                  },
                )
              }
              onRemove={() =>
                remove.mutate(job.id, {
                  onError: () => setError('Could not remove that.'),
                })
              }
            />
          ))}
        </View>
      ) : null}

      {isOrganiser && adding ? (
        <AddResponsibility
          engagementId={engagementId}
          onDone={() => setAdding(false)}
          onError={setError}
        />
      ) : null}

      {isOrganiser && !adding ? (
        <Button
          label="Add a job"
          variant="secondary"
          onPress={() => setAdding(true)}
        />
      ) : null}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function summariseJobs(
  total: number,
  outstanding: number,
  unassigned: number,
): string {
  if (total === 0) {
    return 'Nothing written down yet.';
  }

  // Unassigned is the part an organiser has to act on, so it is named
  // separately rather than folded into "still to do".
  const done = `${total - outstanding} of ${total} done`;
  return unassigned > 0
    ? `${done} · ${unassigned} still to hand out`
    : `${done}.`;
}

function ResponsibilityRow({
  job,
  canTick,
  canManage,
  assigning,
  lineup,
  busy,
  onToggle,
  onStartAssigning,
  onAssign,
  onRemove,
}: {
  job: Responsibility;
  canTick: boolean;
  canManage: boolean;
  assigning: boolean;
  lineup: Participant[];
  busy: boolean;
  onToggle: () => void;
  onStartAssigning: () => void;
  onAssign: (assignedUserId: string | null) => void;
  onRemove: () => void;
}) {
  return (
    <View style={styles.rowGroup}>
      <View style={styles.row}>
        <Pressable
          accessibilityRole="checkbox"
          accessibilityState={{ checked: job.isDone, disabled: !canTick }}
          accessibilityLabel={job.title}
          disabled={!canTick || busy}
          onPress={onToggle}
          style={[
            styles.tick,
            job.isDone ? styles.tickDone : styles.tickOutstanding,
          ]}
        >
          <Text variant="caption" style={styles.tickMark}>
            {job.isDone ? '✓' : ''}
          </Text>
        </Pressable>

        <View style={styles.rowBody}>
          <Text variant="bodySmall" color={job.isDone ? 'muted' : 'primary'}>
            {job.title}
          </Text>
          {/* An organiser taps the name to hand the job over. For everyone
              else it is just the answer to "whose is this". */}
          <Text
            variant="caption"
            color={job.assignedUserId ? 'secondary' : 'error'}
            onPress={canManage && !busy ? onStartAssigning : undefined}
            suppressHighlighting
          >
            {assigneeLabel(job)}
            {job.note ? ` · ${job.note}` : ''}
          </Text>
        </View>

        {canManage ? (
          <Text
            variant="caption"
            color="muted"
            onPress={busy ? undefined : onRemove}
            suppressHighlighting
          >
            Remove
          </Text>
        ) : null}
      </View>

      {assigning ? (
        <View style={styles.chips}>
          <Chip
            label="Nobody yet"
            selected={job.assignedUserId === null}
            onPress={() => onAssign(null)}
          />
          {lineup.map((participant) => (
            <Chip
              key={participant.userId}
              label={participant.displayName ?? 'Someone'}
              selected={job.assignedUserId === participant.userId}
              onPress={() => onAssign(participant.userId)}
            />
          ))}
        </View>
      ) : null}
    </View>
  );
}

function assigneeLabel(job: Responsibility): string {
  if (job.isYours) {
    return 'You';
  }

  if (job.assignedUserId === null) {
    return 'Nobody yet';
  }

  return job.assignedDisplayName ?? 'Someone on the event';
}

/**
 * A new job. The assignee is optional on purpose — an organiser writing out
 * the jobs the night before knows the list long before they know who is taking
 * each one, and a required name would either stop them writing it down or
 * produce assignments nobody meant.
 */
function AddResponsibility({
  engagementId,
  onDone,
  onError,
}: {
  engagementId: string;
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [title, setTitle] = useState('');
  const [assignedUserId, setAssignedUserId] = useState<string | null>(null);
  const lineupQuery = useAvailability(engagementId);

  const create = usePreparationMutation<{
    title: string;
    assignedUserId: string | null;
  }>((accessToken, organisationId, args) =>
    createResponsibility(accessToken, organisationId, engagementId, args),
  );

  const lineup = (lineupQuery.data?.participants ?? []).filter(
    (participant) => participant.isActive,
  );

  return (
    <View style={styles.form}>
      <TextInput
        label="What needs doing"
        placeholder="e.g. Bring the harmonium"
        value={title}
        onChangeText={setTitle}
      />

      <View>
        <Text variant="label" color="secondary">
          Who is doing it
        </Text>
        <View style={styles.chips}>
          <Chip
            label="Nobody yet"
            selected={assignedUserId === null}
            onPress={() => setAssignedUserId(null)}
          />
          {lineup.map((participant) => (
            <Chip
              key={participant.userId}
              label={participant.displayName ?? 'Someone'}
              selected={assignedUserId === participant.userId}
              onPress={() => setAssignedUserId(participant.userId)}
            />
          ))}
        </View>
        {lineup.length === 0 ? (
          <Text variant="caption" color="muted">
            Nobody is on this event yet, so a job can only be written down for
            now.
          </Text>
        ) : null}
      </View>

      <Button
        label="Add"
        loading={create.isPending}
        onPress={() => {
          if (title.trim().length === 0) {
            onError('A job needs a title.');
            return;
          }

          create.mutate(
            { title: title.trim(), assignedUserId },
            {
              onSuccess: () => {
                setTitle('');
                setAssignedUserId(null);
                onDone();
              },
              onError: () => onError('Could not add that.'),
            },
          );
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

/**
 * Rehearsals linked to this event (D-047 §4). Everyone on the lineup sees
 * them: a rehearsal nobody can see is a meeting nobody attends.
 */
export function RehearsalsCard({
  engagementId,
  isOrganiser,
}: {
  engagementId: string;
  isOrganiser: boolean;
}) {
  const rehearsalsQuery = useRehearsals(engagementId);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const remove = usePreparationMutation<string>(
    (accessToken, organisationId, rehearsalId) =>
      deleteRehearsal(accessToken, organisationId, engagementId, rehearsalId),
  );

  if (!rehearsalsQuery.isSuccess) {
    return null;
  }

  const rehearsals = rehearsalsQuery.data;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Rehearsals</Text>
      <Text color="secondary" variant="bodySmall">
        {rehearsals.length === 0
          ? 'None booked yet.'
          : `${rehearsals.length} booked before the event.`}
      </Text>

      {rehearsals.length > 0 ? (
        <View style={styles.rows}>
          {rehearsals.map((rehearsal) => (
            <View key={rehearsal.id} style={styles.row}>
              <View style={styles.rowBody}>
                <Text variant="bodySmall">
                  {rehearsal.title ?? 'Rehearsal'}
                </Text>
                <Text variant="caption" color="secondary">
                  {describeRehearsal(rehearsal)}
                </Text>
                {rehearsal.notes ? (
                  <Text variant="caption" color="muted">
                    {rehearsal.notes}
                  </Text>
                ) : null}
              </View>
              {isOrganiser ? (
                <Text
                  variant="caption"
                  color="muted"
                  onPress={
                    remove.isPending
                      ? undefined
                      : () =>
                          remove.mutate(rehearsal.id, {
                            onError: () => setError('Could not remove that.'),
                          })
                  }
                  suppressHighlighting
                >
                  Remove
                </Text>
              ) : null}
            </View>
          ))}
        </View>
      ) : null}

      {isOrganiser && adding ? (
        <AddRehearsal
          engagementId={engagementId}
          onDone={() => setAdding(false)}
          onError={setError}
        />
      ) : null}

      {isOrganiser && !adding ? (
        <Button
          label="Book a rehearsal"
          variant="secondary"
          onPress={() => setAdding(true)}
        />
      ) : null}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function describeRehearsal(rehearsal: Rehearsal): string {
  const parts = [formatIsoDate(rehearsal.date)];

  if (rehearsal.startTime) {
    const start = formatTime(rehearsal.startTime);
    parts.push(
      rehearsal.endTime ? `${start} – ${formatTime(rehearsal.endTime)}` : start,
    );
  }

  if (rehearsal.venue) {
    parts.push(rehearsal.venue);
  }

  return parts.join(' · ');
}

function AddRehearsal({
  engagementId,
  onDone,
  onError,
}: {
  engagementId: string;
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [date, setDate] = useState<string | null>(null);
  const [startTime, setStartTime] = useState<string | null>(null);
  const [venue, setVenue] = useState('');
  const [notes, setNotes] = useState('');

  const create = usePreparationMutation<{
    date: string;
    startTime: string | null;
    venue: string | null;
    notes: string | null;
  }>((accessToken, organisationId, args) =>
    createRehearsal(accessToken, organisationId, engagementId, args),
  );

  return (
    <View style={styles.form}>
      <DateField label="Date" value={date} onChange={setDate} />
      <TimeField label="Start time" value={startTime} onChange={setStartTime} />
      <TextInput
        label="Where"
        placeholder="e.g. Dural hall"
        value={venue}
        onChangeText={setVenue}
      />
      <TextInput
        label="Notes"
        placeholder="What you are working on, what to bring"
        value={notes}
        onChangeText={setNotes}
        multiline
      />

      <Button
        label="Book it"
        loading={create.isPending}
        onPress={() => {
          if (date === null) {
            // Without a date it is an intention, not a rehearsal — and the
            // readiness item would tick for something nobody could attend.
            onError('A rehearsal needs a date.');
            return;
          }

          create.mutate(
            {
              date,
              startTime,
              venue: venue.trim() || null,
              notes: notes.trim() || null,
            },
            {
              onSuccess: onDone,
              onError: () => onError('Could not book that.'),
            },
          );
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

/**
 * Repertoire, notes, and links (D-047 §5, D-023).
 *
 * A member's copy of this list has already had the Admins-only rows removed by
 * the API, so nothing here needs hiding on the client. The badge exists to
 * tell an organiser which of their own rows the group cannot see.
 */
export function ResourcesCard({
  engagementId,
  isOrganiser,
}: {
  engagementId: string;
  isOrganiser: boolean;
}) {
  const resourcesQuery = useResources(engagementId);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const remove = usePreparationMutation<string>(
    (accessToken, organisationId, resourceId) =>
      deleteResource(accessToken, organisationId, engagementId, resourceId),
  );

  if (!resourcesQuery.isSuccess) {
    return null;
  }

  const resources = resourcesQuery.data;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Repertoire and resources</Text>
      <Text color="secondary" variant="bodySmall">
        {resources.length === 0
          ? 'Nothing attached yet.'
          : `${resources.length} attached.`}
      </Text>

      {resources.length > 0 ? (
        <View style={styles.rows}>
          {resources.map((resource) => (
            <ResourceRow
              key={resource.id}
              resource={resource}
              canRemove={isOrganiser}
              busy={remove.isPending}
              onRemove={() =>
                remove.mutate(resource.id, {
                  onError: () => setError('Could not remove that.'),
                })
              }
            />
          ))}
        </View>
      ) : null}

      {isOrganiser && adding ? (
        <AddResource
          engagementId={engagementId}
          onDone={() => setAdding(false)}
          onError={setError}
        />
      ) : null}

      {isOrganiser && !adding ? (
        <Button
          label="Attach a note or link"
          variant="secondary"
          onPress={() => setAdding(true)}
        />
      ) : null}

      {isOrganiser ? (
        <Text variant="caption" color="muted">
          File uploads arrive once Sahno has somewhere to keep them. A link to
          a file you already have in Drive or Dropbox works today.
        </Text>
      ) : null}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function ResourceRow({
  resource,
  canRemove,
  busy,
  onRemove,
}: {
  resource: EngagementResource;
  canRemove: boolean;
  busy: boolean;
  onRemove: () => void;
}) {
  const [expanded, setExpanded] = useState(false);

  return (
    <View style={styles.row}>
      <View style={styles.rowBody}>
        <Pressable
          accessibilityRole={resource.kind === 'Link' ? 'link' : 'button'}
          onPress={() => {
            if (resource.kind === 'Link' && resource.url) {
              Linking.openURL(resource.url);
              return;
            }
            setExpanded((value) => !value);
          }}
        >
          <Text variant="bodySmall" color={resource.kind === 'Link' ? 'accent' : 'primary'}>
            {resource.kind === 'Link' ? '↗ ' : ''}
            {resource.title}
          </Text>
        </Pressable>

        {resource.kind === 'Link' && resource.url ? (
          <Text variant="caption" color="muted" numberOfLines={1}>
            {resource.url}
          </Text>
        ) : null}

        {resource.kind === 'Note' && resource.body ? (
          <Text
            variant="caption"
            color="secondary"
            numberOfLines={expanded ? undefined : 2}
          >
            {resource.body}
          </Text>
        ) : null}

        {resource.audience === 'AdminsOnly' ? (
          <View style={styles.badge}>
            <Text variant="caption" color="secondary">
              Not shown to members
            </Text>
          </View>
        ) : null}
      </View>

      {canRemove ? (
        <Text
          variant="caption"
          color="muted"
          onPress={busy ? undefined : onRemove}
          suppressHighlighting
        >
          Remove
        </Text>
      ) : null}
    </View>
  );
}

function AddResource({
  engagementId,
  onDone,
  onError,
}: {
  engagementId: string;
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [kind, setKind] = useState<ResourceKind>('Note');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [url, setUrl] = useState('');
  const [audience, setAudience] = useState<ResourceAudience>('Participants');

  const create = usePreparationMutation<{
    kind: ResourceKind;
    title: string;
    body: string | null;
    url: string | null;
    audience: ResourceAudience;
  }>((accessToken, organisationId, args) =>
    createResource(accessToken, organisationId, engagementId, args),
  );

  return (
    <View style={styles.form}>
      <View style={styles.chips}>
        <Chip
          label="Note"
          selected={kind === 'Note'}
          onPress={() => setKind('Note')}
        />
        <Chip
          label="Link"
          selected={kind === 'Link'}
          onPress={() => setKind('Link')}
        />
      </View>

      <TextInput
        label="Title"
        placeholder={kind === 'Note' ? 'e.g. Running order' : 'e.g. Reference recording'}
        value={title}
        onChangeText={setTitle}
      />

      {kind === 'Note' ? (
        <TextInput
          label="Note"
          placeholder="Qaul first, then the two new pieces."
          value={body}
          onChangeText={setBody}
          multiline
        />
      ) : (
        <TextInput
          label="Address"
          placeholder="https://"
          value={url}
          onChangeText={setUrl}
          autoCapitalize="none"
          keyboardType="url"
        />
      )}

      <View>
        <Text variant="label" color="secondary">
          Who can see it
        </Text>
        <View style={styles.chips}>
          <Chip
            label="Everyone on the event"
            selected={audience === 'Participants'}
            onPress={() => setAudience('Participants')}
          />
          <Chip
            label="Organisers only"
            selected={audience === 'AdminsOnly'}
            onPress={() => setAudience('AdminsOnly')}
          />
        </View>
      </View>

      <Button
        label="Attach"
        loading={create.isPending}
        onPress={() => {
          if (title.trim().length === 0) {
            onError('That needs a title.');
            return;
          }

          if (kind === 'Note' && body.trim().length === 0) {
            onError('A note needs something in it.');
            return;
          }

          if (kind === 'Link' && url.trim().length === 0) {
            onError('A link needs an address.');
            return;
          }

          create.mutate(
            {
              kind,
              title: title.trim(),
              body: kind === 'Note' ? body.trim() : null,
              url: kind === 'Link' ? url.trim() : null,
              audience,
            },
            {
              onSuccess: onDone,
              // The API refuses anything that is not http(s), and its wording
              // is what the person needs to read.
              onError: (failure) =>
                onError(
                  failure instanceof Error && failure.message
                    ? failure.message
                    : 'Could not attach that.',
                ),
            },
          );
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

function Chip({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[styles.chip, selected ? styles.chipSelected : null]}
    >
      <Text variant="caption" color={selected ? 'inverse' : 'secondary'}>
        {label}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  rows: {
    gap: spacing.md,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
  },
  rowGroup: {
    gap: spacing.xs,
  },
  rowBody: {
    flex: 1,
    gap: 2,
  },
  form: {
    gap: spacing.md,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
  chips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
    marginTop: spacing.xs,
  },
  chip: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: colors.border.strong,
    minHeight: 34,
    justifyContent: 'center',
  },
  chipSelected: {
    backgroundColor: colors.interactive.primary,
    borderColor: colors.interactive.primary,
  },
  tick: {
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
  },
  tickMark: {
    color: colors.text.inverse,
    lineHeight: 14,
  },
  tickDone: {
    backgroundColor: colors.tealText,
    borderColor: colors.tealText,
  },
  tickOutstanding: {
    backgroundColor: 'transparent',
    borderColor: colors.border.strong,
  },
  badge: {
    alignSelf: 'flex-start',
    marginTop: 2,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: radii.full,
    backgroundColor: colors.surface.subtle,
  },
});
