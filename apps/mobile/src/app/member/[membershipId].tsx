import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Switch, View } from 'react-native';

import {
  changeMemberRole,
  removeMember,
  setMemberFinancialAccess,
  setMemberInternalNotes,
  transferOwnership,
  updateOwnMembership,
  type Member,
} from '@/api/members';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useMemberMutation, useMembers } from '@/hooks/use-members';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, spacing } from '@/theme';

/**
 * One member, and whatever the caller is actually allowed to do about them.
 * The API is the authority (D-063); this screen only offers actions it would
 * accept, so nobody is shown a control that will refuse them.
 */
export default function MemberDetail() {
  const { membershipId } = useLocalSearchParams<{ membershipId: string }>();
  const router = useRouter();
  const { active } = useActiveOrg();
  const membersQuery = useMembers();

  const member = membersQuery.data?.find(
    (row) => row.membershipId === membershipId,
  );

  if (membersQuery.isPending) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (!member || !active) {
    return (
      <Screen>
        <View style={styles.centered}>
          <Text variant="heading">Member not found</Text>
          <Text color="secondary" style={styles.centeredText}>
            They may have been removed from {active?.name ?? 'this organisation'}.
          </Text>
          <Button label="Back" variant="secondary" onPress={() => router.back()} />
        </View>
      </Screen>
    );
  }

  const callerRole = active.role;
  const isOwner = callerRole === 'Owner';
  const isOrganiser = isOwner || callerRole === 'Admin';

  // Mirrors of the server rules, so the UI never offers a refused action.
  const targetIsOwner = member.role === 'Owner';
  const targetIsAdmin = member.role === 'Admin';
  const canManageThem = isOrganiser && !member.isYou && !targetIsOwner;
  const canChangeRole = canManageThem && (isOwner || !targetIsAdmin);
  const canRemove = canChangeRole;
  const canSetFinances = isOwner && targetIsAdmin;
  const canTransfer = isOwner && !member.isYou;
  const canWriteNotes = canManageThem;

  return (
    <Screen
      scroll
      onRefresh={() => membersQuery.refetch()}
      refreshing={membersQuery.isRefetching}
    >
      <View style={styles.header}>
        <Text variant="title">{member.displayName ?? 'Member'}</Text>
        <Text color="secondary">
          {member.role}
          {member.function ? ` · ${member.function}` : ''}
        </Text>
      </View>

      <ContactCard member={member} />

      {member.isYou ? (
        <OwnProfileCard member={member} organisationName={active.name} />
      ) : null}

      {canChangeRole || canSetFinances || canTransfer ? (
        <RoleCard
          member={member}
          canChangeRole={canChangeRole}
          canSetFinances={canSetFinances}
          canTransfer={canTransfer}
        />
      ) : null}

      {canWriteNotes ? <NotesCard member={member} /> : null}

      {canRemove ? (
        <RemoveCard member={member} onRemoved={() => router.back()} />
      ) : null}

      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

function ContactCard({ member }: { member: Member }) {
  const hasContact = member.email !== null || member.phoneNumber !== null;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Contact</Text>
      {member.email ? <Text>{member.email}</Text> : null}
      {member.phoneNumber ? <Text>{member.phoneNumber}</Text> : null}
      {hasContact ? null : (
        <Text color="muted" variant="bodySmall">
          {member.isYou
            ? 'You have not added contact details yet.'
            : 'This person keeps their contact details private.'}
        </Text>
      )}
    </Card>
  );
}

/** What the person controls about themselves here (D-018). */
function OwnProfileCard({
  member,
  organisationName,
}: {
  member: Member;
  organisationName: string;
}) {
  const [functionText, setFunctionText] = useState(member.function ?? '');
  const [error, setError] = useState<string | null>(null);

  const update = useMemberMutation<{
    function?: string | null;
    sharesContactDetails?: boolean;
  }>((accessToken, organisationId, args) =>
    updateOwnMembership(accessToken, organisationId, args),
  );

  function run(args: { function?: string | null; sharesContactDetails?: boolean }) {
    setError(null);
    update.mutate(args, {
      onError: () => setError('That did not save. Try again.'),
    });
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Your profile here</Text>

      <TextInput
        label="What you do"
        placeholder="e.g. Tabla player"
        value={functionText}
        onChangeText={setFunctionText}
        helperText="Everyone in the group can see this."
      />
      <Button
        label="Save"
        variant="secondary"
        onPress={() => run({ function: functionText })}
        loading={update.isPending}
      />

      <View style={styles.switchRow}>
        <View style={styles.switchText}>
          <Text>Share my contact details</Text>
          <Text color="muted" variant="caption">
            Off by default. Organisers of {organisationName} can always reach
            you; this decides whether other members can.
          </Text>
        </View>
        <Switch
          accessibilityLabel="Share my contact details"
          value={member.sharesContactDetails}
          onValueChange={(value) => run({ sharesContactDetails: value })}
          trackColor={{ true: colors.tealText, false: colors.border.strong }}
        />
      </View>

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function RoleCard({
  member,
  canChangeRole,
  canSetFinances,
  canTransfer,
}: {
  member: Member;
  canChangeRole: boolean;
  canSetFinances: boolean;
  canTransfer: boolean;
}) {
  const [error, setError] = useState<string | null>(null);

  const role = useMemberMutation<'Admin' | 'Member'>(
    (accessToken, organisationId, next) =>
      changeMemberRole(accessToken, organisationId, member.membershipId, next),
  );
  const finances = useMemberMutation<boolean>(
    (accessToken, organisationId, next) =>
      setMemberFinancialAccess(
        accessToken,
        organisationId,
        member.membershipId,
        next,
      ),
  );
  const transfer = useMemberMutation<void>((accessToken, organisationId) =>
    transferOwnership(accessToken, organisationId, member.membershipId),
  );

  const name = member.displayName ?? 'this member';

  function confirmTransfer() {
    Alert.alert(
      'Transfer ownership?',
      `${name} becomes the Owner and you become an Admin. Only they will be able to hand it back.`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Transfer',
          style: 'destructive',
          onPress: () =>
            transfer.mutate(undefined, {
              onError: () => setError('Could not transfer ownership.'),
            }),
        },
      ],
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Role</Text>

      {canChangeRole ? (
        <Button
          label={member.role === 'Admin' ? 'Remove admin' : 'Make an admin'}
          variant="secondary"
          loading={role.isPending}
          onPress={() =>
            role.mutate(member.role === 'Admin' ? 'Member' : 'Admin', {
              onError: () => setError('Could not change that role.'),
            })
          }
        />
      ) : null}

      {canSetFinances ? (
        <View style={styles.switchRow}>
          <View style={styles.switchText}>
            <Text>Can manage finances</Text>
            <Text color="muted" variant="caption">
              Off by default. Only you can grant this.
            </Text>
          </View>
          <Switch
            accessibilityLabel="Can manage finances"
            value={member.canManageFinances}
            onValueChange={(value) =>
              finances.mutate(value, {
                onError: () => setError('Could not change financial access.'),
              })
            }
            trackColor={{ true: colors.tealText, false: colors.border.strong }}
          />
        </View>
      ) : null}

      {canTransfer ? (
        <Button
          label="Transfer ownership"
          variant="secondary"
          loading={transfer.isPending}
          onPress={confirmTransfer}
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

function NotesCard({ member }: { member: Member }) {
  const [notes, setNotes] = useState(member.internalNotes ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = useMemberMutation<string>((accessToken, organisationId, value) =>
    setMemberInternalNotes(
      accessToken,
      organisationId,
      member.membershipId,
      value,
    ),
  );

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Private notes</Text>
      <TextInput
        label="Notes"
        placeholder="Only organisers can read these."
        value={notes}
        onChangeText={setNotes}
        multiline
        numberOfLines={4}
        style={styles.notes}
        helperText="Never shown to members, including this one."
      />
      <Button
        label="Save notes"
        variant="secondary"
        loading={save.isPending}
        onPress={() =>
          save.mutate(notes, {
            onError: () => setError('Could not save those notes.'),
          })
        }
      />
      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function RemoveCard({
  member,
  onRemoved,
}: {
  member: Member;
  onRemoved: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const remove = useMemberMutation<void>((accessToken, organisationId) =>
    removeMember(accessToken, organisationId, member.membershipId),
  );

  const name = member.displayName ?? 'this member';

  // Removal is confirmed rather than immediate (Slice 3): it cannot be undone
  // from here, and the person would have to be invited again.
  function confirmRemove() {
    Alert.alert(
      `Remove ${name}?`,
      'They lose access to this organisation straight away. You can invite them again later.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: () =>
            remove.mutate(undefined, {
              onSuccess: onRemoved,
              onError: () => setError('Could not remove them.'),
            }),
        },
      ],
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Remove from organisation</Text>
      <Button
        label={`Remove ${name}`}
        variant="secondary"
        loading={remove.isPending}
        onPress={confirmRemove}
      />
      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
  },
  centeredText: {
    textAlign: 'center',
  },
  header: {
    gap: spacing.xs,
    marginBottom: spacing.xl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  switchText: {
    flex: 1,
    gap: 2,
  },
  notes: {
    minHeight: 96,
    textAlignVertical: 'top',
  },
});
