import { Ionicons } from '@expo/vector-icons';
import * as Clipboard from 'expo-clipboard';
import { useState } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Button, Text } from '@/components/ui';
import { colors, radii, spacing } from '@/theme';

/** The details worth sending: what broke and where in the code it was. */
export function describeError(error: unknown): string {
  if (error instanceof Error) {
    const stack = error.stack?.split('\n').slice(0, 8).join('\n');
    return [`${error.name}: ${error.message}`, stack].filter(Boolean).join('\n');
  }
  return String(error);
}

/**
 * What shows instead of the app closing when a screen fails to draw (D-110).
 * It depends on nothing but the theme — no session, no queries — because it
 * may be standing in for the very providers that broke. The error is shown
 * and can be copied, so a tester can send exactly what happened.
 */
export function CrashScreen({
  error,
  onRetry,
  onBack,
}: {
  error: unknown;
  onRetry: () => void;
  onBack?: () => void;
}) {
  const insets = useSafeAreaInsets();
  const [copied, setCopied] = useState(false);
  const details = describeError(error);

  return (
    <ScrollView
      style={styles.screen}
      contentContainerStyle={[
        styles.content,
        { paddingTop: insets.top + spacing.xl, paddingBottom: insets.bottom + spacing.xl },
      ]}
    >
      <Ionicons name="alert-circle-outline" size={44} color={colors.tealText} />
      <Text variant="title">Something went wrong</Text>
      <Text color="secondary">
        This screen hit an error. Nothing you saved is lost. Try again, and if it keeps
        happening, copy the details below and send them to us.
      </Text>

      <View style={styles.actions}>
        <Button label="Try again" onPress={onRetry} />
        {onBack ? <Button label="Back" variant="secondary" onPress={onBack} /> : null}
      </View>

      <View style={styles.details}>
        <Text variant="caption" color="muted" selectable>
          {details}
        </Text>
      </View>
      <Button
        label={copied ? 'Copied' : 'Copy error details'}
        variant="ghost"
        onPress={() => {
          void Clipboard.setStringAsync(details).then(() => setCopied(true));
        }}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  content: {
    paddingHorizontal: spacing.xl,
    gap: spacing.md,
  },
  actions: {
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
  details: {
    marginTop: spacing.md,
    padding: spacing.md,
    borderRadius: radii.md,
    backgroundColor: colors.surface.subtle,
  },
});
