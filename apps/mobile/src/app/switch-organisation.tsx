import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

import { Button, Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, radii, spacing } from '@/theme';

/** Organisation switcher (D-044): pick the active context. */
export default function SwitchOrganisation() {
  const router = useRouter();
  const { organisations, active, switchTo } = useActiveOrg();

  return (
    <Screen>
      <View style={styles.header}>
        <Text variant="title">Your organisations</Text>
        <Text color="secondary">
          Switching changes everything — bookings, members, and permissions are
          strictly per organisation.
        </Text>
      </View>

      <View style={styles.list}>
        {organisations.map((organisation) => {
          const isActive = organisation.id === active?.id;
          return (
            <Pressable
              key={organisation.id}
              accessibilityRole="button"
              accessibilityState={{ selected: isActive }}
              onPress={() => {
                switchTo(organisation.id);
                router.back();
              }}
              style={[styles.row, isActive ? styles.rowActive : null]}
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
          );
        })}
      </View>

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
      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  list: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 64,
    paddingHorizontal: spacing.lg,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.raised,
  },
  rowActive: {
    borderColor: colors.border.focus,
    borderWidth: 2,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
});
