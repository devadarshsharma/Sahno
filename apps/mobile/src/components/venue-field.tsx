import { Ionicons } from '@expo/vector-icons';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { Keyboard, Pressable, StyleSheet, View } from 'react-native';

import { suggestPlaces } from '@/api/places';
import { Text, TextInput } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { colors, radii, spacing } from '@/theme';

const MIN_CHARS = 3;
const DEBOUNCE_MS = 300;
/** Long enough for a tap on a suggestion to land before the blur saves. */
const BLUR_GRACE_MS = 250;

/** The phone's country ("AU" from "en-AU"), to bias suggestions towards it. */
function deviceRegion(): string | null {
  const locale = Intl.DateTimeFormat().resolvedOptions().locale ?? '';
  const region = locale.split(/[-_]/).find((part, index) => index > 0 && /^[A-Za-z]{2}$/.test(part));
  return region ? region.toUpperCase() : null;
}

function newSessionToken(): string {
  return `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`;
}

/**
 * A venue field that suggests real places as you type (D-102). Still free
 * text: "Ali's place" or a hall nobody has mapped are as good as a picked
 * address, and with suggestions off on the server it is a plain field.
 *
 * Picking a suggestion calls `onPick`. A field that saves on blur passes both:
 * the blur is held back briefly, and dropped if a pick follows, so tapping a
 * suggestion saves the venue once, not the half-typed text first.
 */
export function VenueField({
  label,
  placeholder,
  value,
  onChangeText,
  onBlur,
  onPick,
  helperText,
  error,
}: {
  label: string;
  placeholder?: string;
  value: string;
  onChangeText: (text: string) => void;
  onBlur?: () => void;
  onPick?: (text: string) => void;
  helperText?: string;
  error?: string;
}) {
  const session = useSession();
  const [focused, setFocused] = useState(false);
  // Only what the person typed is searched — not the saved venue the field
  // opened with, and not the line a pick just filled in.
  const [typed, setTyped] = useState<string | null>(null);
  const [debounced, setDebounced] = useState<string | null>(null);
  const [sessionToken, setSessionToken] = useState(newSessionToken);
  const blurTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  // A pick already saved; the blur that follows the keyboard closing is not
  // a second edit.
  const justPicked = useRef(false);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(typed), DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [typed]);

  useEffect(
    () => () => {
      if (blurTimer.current) {
        clearTimeout(blurTimer.current);
      }
    },
    [],
  );

  const query = debounced?.trim() ?? '';
  const suggestions = useQuery({
    queryKey: ['places', query],
    queryFn: async ({ signal }) =>
      suggestPlaces(await session.getAccessToken(), query, sessionToken, deviceRegion(), signal),
    enabled: focused && query.length >= MIN_CHARS && session.status === 'authenticated',
    staleTime: 10 * 60 * 1000,
    // A failed lookup is no suggestions; the field still works.
    retry: false,
  });

  const rows = focused && typed !== null && query.length >= MIN_CHARS ? (suggestions.data ?? []).slice(0, 5) : [];

  const pick = (text: string) => {
    if (blurTimer.current) {
      // The blur came first: this pick replaces its save.
      clearTimeout(blurTimer.current);
      blurTimer.current = null;
      setFocused(false);
    } else {
      justPicked.current = true;
    }
    setTyped(null);
    setDebounced(null);
    // One search session ends with a pick; the next starts afresh.
    setSessionToken(newSessionToken());
    onChangeText(text);
    onPick?.(text);
    Keyboard.dismiss();
  };

  return (
    <View>
      <TextInput
        label={label}
        placeholder={placeholder}
        value={value}
        onChangeText={(text) => {
          justPicked.current = false;
          setTyped(text);
          onChangeText(text);
        }}
        onFocus={() => setFocused(true)}
        onBlur={() => {
          if (justPicked.current) {
            justPicked.current = false;
            setFocused(false);
            return;
          }
          const finish = () => {
            setFocused(false);
            onBlur?.();
          };
          // Suggestions on screen: keep them, and the save, a moment longer
          // in case this blur is the start of a tap on one.
          if (rows.length === 0) {
            finish();
            return;
          }
          blurTimer.current = setTimeout(() => {
            blurTimer.current = null;
            finish();
          }, BLUR_GRACE_MS);
        }}
        helperText={rows.length > 0 ? undefined : helperText}
        error={error}
        autoCorrect={false}
      />

      {rows.length > 0 ? (
        <View style={styles.list} accessibilityRole="list" accessibilityLabel="Suggested places">
          {rows.map((place, index) => (
            <Pressable
              key={`${place.text}-${index}`}
              accessibilityRole="button"
              accessibilityLabel={`Use ${place.text}`}
              onPress={() => pick(place.text)}
              style={({ pressed }) => [
                styles.row,
                index > 0 ? styles.rowDivider : null,
                pressed ? styles.rowPressed : null,
              ]}
            >
              <Ionicons name="location-outline" size={16} color={colors.tealText} />
              <View style={styles.rowText}>
                <Text variant="bodySmall" numberOfLines={1}>
                  {place.name}
                </Text>
                {place.address ? (
                  <Text variant="caption" color="muted" numberOfLines={1}>
                    {place.address}
                  </Text>
                ) : null}
              </View>
            </Pressable>
          ))}
          {/* Google's terms ask for attribution where its suggestions show without a map. */}
          <Text variant="caption" color="muted" style={styles.attribution}>
            Google Maps
          </Text>
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  list: {
    marginTop: spacing.xs,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.raised,
    overflow: 'hidden',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minHeight: 48,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
  },
  rowDivider: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
  },
  rowPressed: {
    backgroundColor: colors.surface.subtle,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  attribution: {
    textAlign: 'right',
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.xs,
  },
});
