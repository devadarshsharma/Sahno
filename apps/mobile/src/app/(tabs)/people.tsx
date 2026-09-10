import { useRouter } from 'expo-router';
import { useEffect } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Member } from '@/api/members';
import { Button, Screen, Text } from '@/components/ui';
import { sortedForDisplay, useMembers } from '@/hooks/use-members';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSeenPeople } from '@/stores/seen-people';
import { colors, radii, shadows, spacing } from '@/theme';

/**
 * The member directory (D-018, Slice 3). Everyone sees who is in the group and
 * what they do; contact details appear only where the API has released them,
 * so a missing detail is left out entirely rather than shown as blank.
 */
export default function People() {
  const router = useRouter();
  const { active } = useActiveOrg();
  const membersQuery = useMembers();
  const markPeopleSeen = useSeenPeople((state) => state.markPeopleSeen);

  // Looking at People is the acknowledgement that clears the "recently
  // joined" notice on Home.
  useEffect(() => {
    markPeopleSeen();
  }, [markPeopleSeen]);

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  if (membersQuery.isPending) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (membersQuery.isError) {
    return (
      <Screen>
        <View style={styles.centered}>
          <Text variant="heading" color="error">
            Could not load your people
          </Text>
          <Text color="secondary" style={styles.centeredText}>
            Check your connection and try again.
          </Text>
          <Button label="Try again" onPress={() => membersQuery.refetch()} />
        </View>
      </Screen>
    );
  }

  const members = sortedForDisplay(membersQuery.data);
  const refresh = () => membersQuery.refetch();

  return (
    <Screen
      scroll
      onRefresh={refresh}
      refreshing={membersQuery.isRefetching}
    >
      <View style={styles.header}>
        <Text variant="title">People</Text>
        <Text color="secondary" variant="bodySmall">
          {members.length === 1
            ? 'Just you so far.'
            : `${members.length} people in ${active?.name}.`}
        </Text>
      </View>

      <View style={styles.list}>
        {members.map((member) => (
          <MemberRow
            key={member.membershipId}
            member={member}
            onPress={() =>
              router.push({
                pathname: '/member/[membershipId]',
                params: { membershipId: member.membershipId },
              })
            }
          />
        ))}
      </View>

      {isOrganiser ? (
        <Button
          label="Invite members"
          variant="secondary"
          onPress={() => router.push('/invitations')}
          style={styles.invite}
        />
      ) : null}
    </Screen>
  );
}

function MemberRow({
  member,
  onPress,
}: {
  member: Member;
  onPress: () => void;
}) {
  const name = member.displayName ?? 'Member';
  const contact = member.email ?? member.phoneNumber;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${name}${member.function ? `, ${member.function}` : ''}. ${member.role}.`}
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
    >
      <View style={styles.avatar}>
        <Text variant="subheading" color="accent">
          {initialOf(name)}
        </Text>
      </View>

      <View style={styles.rowText}>
        <View style={styles.nameLine}>
          <Text variant="subheading" numberOfLines={1} style={styles.name}>
            {name}
          </Text>
          {member.isYou ? (
            <Text variant="caption" color="muted">
              you
            </Text>
          ) : null}
        </View>
        {member.function ? (
          <Text variant="bodySmall" color="secondary" numberOfLines={1}>
            {member.function}
          </Text>
        ) : null}
        {contact ? (
          <Text variant="caption" color="muted" numberOfLines={1}>
            {contact}
          </Text>
        ) : null}
      </View>

      {member.role === 'Member' ? null : <RoleBadge role={member.role} />}
    </Pressable>
  );
}

function RoleBadge({ role }: { role: 'Owner' | 'Admin' }) {
  return (
    <View
      style={[
        styles.badge,
        role === 'Owner' ? styles.badgeOwner : styles.badgeAdmin,
      ]}
    >
      <Text variant="caption" style={styles.badgeText}>
        {role}
      </Text>
    </View>
  );
}

function initialOf(name: string): string {
  return name.trim().charAt(0).toUpperCase() || '?';
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
  list: {
    gap: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radii.lg,
    backgroundColor: colors.surface.raised,
    ...shadows.sm,
  },
  rowPressed: {
    backgroundColor: colors.surface.subtle,
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.tealSoft,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  nameLine: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: spacing.sm,
  },
  name: {
    flexShrink: 1,
  },
  badge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: radii.full,
  },
  badgeOwner: {
    backgroundColor: colors.orangeSoft,
  },
  badgeAdmin: {
    backgroundColor: colors.tealSoft,
  },
  badgeText: {
    color: colors.text.primary,
  },
  invite: {
    marginTop: spacing.xl,
  },
});
