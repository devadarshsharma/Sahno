import { Ionicons } from '@expo/vector-icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

import { setDefaultOrganisation } from '@/api/organisations';
import { RecentlyDeleted } from '@/components/recently-deleted';
import { Button, Screen, showAlert, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { colors, radii, spacing } from '@/theme';

/**
 * Organisation switcher (D-044): pick the active context — and, for people in
 * more than one, which one the app opens in (D-099).
 */
export default function SwitchOrganisation() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { organisations, active, switchTo } = useActiveOrg();

  const makeDefault = useMutation({
    mutationFn: async (organisationId: string) => {
      const accessToken = await session.getAccessToken();
      await setDefaultOrganisation(accessToken, organisationId);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['organisations'] }),
    onError: () => showAlert('Could not change your default', 'Check your connection and try again.'),
  });

  // A default only means something with a choice to make.
  const choosing = organisations.length > 1;

  return (
    <Screen
      hero={{
        title: 'Your organisations',
        subtitle:
          'Switching changes everything — bookings, members, and permissions are strictly per organisation.',
      }}
    >

      <View style={styles.list}>
        {organisations.map((organisation) => {
          const isActive = organisation.id === active?.id;
          const isDefault = organisation.isDefault === true;
          return (
            <View key={organisation.id} style={[styles.row, isActive ? styles.rowActive : null]}>
              <Pressable
                accessibilityRole="button"
                accessibilityState={{ selected: isActive }}
                onPress={() => {
                  switchTo(organisation.id);
                  router.back();
                }}
                style={styles.rowMain}
              >
                <View style={styles.rowText}>
                  <Text variant="subheading">{organisation.name}</Text>
                  <Text variant="caption" color="muted">
                    {organisation.role}
                  </Text>
                </View>
                {isActive ? (
                  <Text variant="label" color="accent">
                    Active
                  </Text>
                ) : null}
              </Pressable>

              {choosing ? (
                isDefault ? (
                  <View style={styles.default} accessibilityLabel="Opens in this organisation">
                    <Ionicons name="star" size={14} color={colors.orange} />
                    <Text variant="caption" color="secondary">
                      Default
                    </Text>
                  </View>
                ) : (
                  <Pressable
                    accessibilityRole="button"
                    accessibilityLabel={`Open the app in ${organisation.name} from now on`}
                    disabled={makeDefault.isPending}
                    onPress={() => makeDefault.mutate(organisation.id)}
                    hitSlop={8}
                    style={({ pressed }) => [styles.default, pressed ? styles.pressed : null]}
                  >
                    <Ionicons name="star-outline" size={14} color={colors.text.muted} />
                    <Text variant="caption" color="muted">
                      Make default
                    </Text>
                  </Pressable>
                )
              ) : null}
            </View>
          );
        })}
      </View>

      {choosing ? (
        <Text variant="caption" color="muted" style={styles.hint}>
          Sahno opens in your default organisation. It follows your account, on any phone.
        </Text>
      ) : null}

      <Button
        label="Join another organisation"
        variant="secondary"
        onPress={() => router.push('/join')}
      />
      <Button
        label="Create a new organisation"
        variant="ghost"
        onPress={() => router.push('/create-organisation')}
      />

      {/* Out of the way at the bottom, and only for what the person owns (D-101). */}
      {organisations
        .filter((organisation) => organisation.role === 'Owner')
        .map((organisation) => (
          <Pressable
            key={organisation.id}
            accessibilityRole="button"
            onPress={() =>
              router.push({ pathname: '/delete-organisation', params: { organisationId: organisation.id } })
            }
            hitSlop={8}
            style={({ pressed }) => [styles.danger, pressed ? styles.pressed : null]}
          >
            <Ionicons name="trash-outline" size={16} color={colors.text.error} />
            <Text variant="bodySmall" color="error">
              Delete {organisation.name}
            </Text>
          </Pressable>
        ))}

      <RecentlyDeleted />
    </Screen>
  );
}

const styles = StyleSheet.create({
  list: {
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  row: {
    minHeight: 64,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.raised,
    gap: spacing.xs,
  },
  rowActive: {
    borderColor: colors.border.focus,
    borderWidth: 2,
  },
  rowMain: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 44,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  default: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 4,
    paddingVertical: 2,
  },
  pressed: {
    opacity: 0.6,
  },
  hint: {
    marginBottom: spacing.xl,
  },
  danger: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xs,
    minHeight: 44,
    marginTop: spacing.md,
  },
});
