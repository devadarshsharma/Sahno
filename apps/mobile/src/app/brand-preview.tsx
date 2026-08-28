import { useRouter } from 'expo-router';
import { useState, type ReactNode } from 'react';
import { StyleSheet, View } from 'react-native';

import { SahnoLogo, SahnoSymbol } from '@/components/brand';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import {
  colors,
  palette,
  radii,
  spacing,
  textVariants,
  type RadiusKey,
  type SpacingKey,
  type TextVariant,
} from '@/theme';

/**
 * TEMPORARY brand-preview screen for validating the v0.1 visual direction on
 * real devices (BRAND_VAULT validation gates). Safe to delete: remove this
 * file and the "Brand preview" button in `src/app/index.tsx`.
 */

const TYPE_RAMP: TextVariant[] = [
  'display',
  'title',
  'heading',
  'subheading',
  'body',
  'bodySmall',
  'label',
  'button',
  'caption',
];

const PALETTE_SWATCHES: { name: string; value: string; onDark: boolean }[] = [
  { name: 'Deep navy', value: palette.navy, onDark: true },
  { name: 'Energetic orange', value: palette.orange, onDark: false },
  { name: 'Confident teal', value: palette.teal, onDark: true },
  { name: 'Teal (text-safe)', value: palette.tealText, onDark: true },
  { name: 'Warm off-white', value: palette.offWhite, onDark: false },
  { name: 'White', value: palette.white, onDark: false },
];

const SPACING_KEYS: SpacingKey[] = ['xxs', 'xs', 'sm', 'md', 'lg', 'xl', 'xxl', 'xxxl'];
const RADIUS_KEYS: RadiusKey[] = ['sm', 'md', 'lg', 'xl'];

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <View style={styles.section}>
      <Text variant="heading">{title}</Text>
      {children}
    </View>
  );
}

export default function BrandPreview() {
  const router = useRouter();
  const [loadingDemo, setLoadingDemo] = useState(false);
  const [inputValue, setInputValue] = useState('');

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Brand preview</Text>
        <Text color="secondary">
          v0.1 candidates for visual validation — geometry and colour values
          are not locked.
        </Text>
        <Button label="Back" variant="secondary" onPress={() => router.back()} />
      </View>

      <Section title="Primary lockup">
        <Card>
          <SahnoLogo size={64} tagline />
        </Card>
      </Section>

      <Section title="Symbol on light and dark">
        <View style={styles.row}>
          <View style={[styles.symbolTile, { backgroundColor: colors.surface.canvas }]}>
            <SahnoSymbol size={72} />
          </View>
          <View style={[styles.symbolTile, { backgroundColor: colors.surface.dark }]}>
            <SahnoSymbol size={72} />
          </View>
        </View>
        <View style={[styles.darkPanel]}>
          <SahnoLogo size={40} onDark />
        </View>
        <Text variant="caption" color="muted">
          Small sizes (legibility gate: 16 / 24 / 32 / 48 px)
        </Text>
        <View style={styles.rowEnd}>
          <SahnoSymbol size={16} />
          <SahnoSymbol size={24} />
          <SahnoSymbol size={32} />
          <SahnoSymbol size={48} />
        </View>
      </Section>

      <Section title="Typography">
        <Card style={styles.gapMd}>
          {TYPE_RAMP.map((variant) => (
            <Text key={variant} variant={variant}>
              {variant} · {textVariants[variant].fontSize}px
            </Text>
          ))}
          <Text variant="caption" color="muted">
            Bricolage Grotesque for body copy is under validation; it may be
            paired with a quieter UI family if it tires at small sizes.
          </Text>
        </Card>
      </Section>

      <Section title="Colour palette">
        <View style={styles.paletteGrid}>
          {PALETTE_SWATCHES.map((swatch) => (
            <View
              key={swatch.name}
              style={[styles.swatch, { backgroundColor: swatch.value }]}
            >
              <Text
                variant="label"
                color={swatch.onDark ? 'inverse' : 'primary'}
              >
                {swatch.name}
              </Text>
              <Text
                variant="caption"
                color={swatch.onDark ? 'inverse' : 'secondary'}
              >
                {swatch.value}
              </Text>
            </View>
          ))}
        </View>
        <Text variant="caption" color="muted">
          Orange is decorative only — it does not pass text-contrast
          requirements on light backgrounds.
        </Text>
      </Section>

      <Section title="Buttons">
        <Card style={styles.gapMd}>
          <Button label="Primary" onPress={() => {}} />
          <Button label="Secondary" variant="secondary" onPress={() => {}} />
          <Button label="Ghost" variant="ghost" onPress={() => {}} />
          <Button label="Disabled" disabled onPress={() => {}} />
          <Button
            label={loadingDemo ? 'Loading' : 'Tap to show loading'}
            loading={loadingDemo}
            onPress={() => {
              setLoadingDemo(true);
              setTimeout(() => setLoadingDemo(false), 2000);
            }}
          />
          <Text variant="caption" color="muted">
            Press and hold any button to see its pressed state.
          </Text>
        </Card>
      </Section>

      <Section title="Cards">
        <Card shadow="none" style={styles.gapMd}>
          <Text variant="subheading">Flat card</Text>
          <Text color="secondary">No elevation, raised surface colour.</Text>
        </Card>
        <Card style={styles.gapMd}>
          <Text variant="subheading">Default card</Text>
          <Text color="secondary">Small shadow, 16px radius, 16px padding.</Text>
        </Card>
        <Card shadow="lg" style={styles.gapMd}>
          <Text variant="subheading">Elevated card</Text>
          <Text color="secondary">Large shadow for prominent surfaces.</Text>
        </Card>
      </Section>

      <Section title="Text inputs">
        <Card style={styles.gapMd}>
          <TextInput
            label="Event name"
            placeholder="e.g. Wedding reception"
            value={inputValue}
            onChangeText={setInputValue}
            helperText="Tap to see the focus state."
          />
          <TextInput
            label="Venue"
            placeholder="TBC"
            helperText="Optional — can stay TBC."
          />
          <TextInput
            label="Contact email"
            value="not-an-email"
            error="Enter a valid email address."
          />
          <TextInput label="Disabled field" value="Read only" disabled />
        </Card>
      </Section>

      <Section title="Spacing scale">
        <Card style={styles.gapMd}>
          {SPACING_KEYS.map((key) => (
            <View key={key} style={styles.rowEnd}>
              <Text variant="label" style={styles.scaleLabel}>
                {key} · {spacing[key]}
              </Text>
              <View style={[styles.spacingBar, { width: spacing[key] * 4 }]} />
            </View>
          ))}
        </Card>
      </Section>

      <Section title="Corner radii">
        <View style={styles.row}>
          {RADIUS_KEYS.map((key) => (
            <View key={key} style={[styles.radiusTile, { borderRadius: radii[key] }]}>
              <Text variant="caption" color="inverse">
                {key}
              </Text>
              <Text variant="caption" color="inverse">
                {radii[key]}
              </Text>
            </View>
          ))}
        </View>
      </Section>
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    gap: spacing.md,
    marginBottom: spacing.xl,
  },
  section: {
    gap: spacing.md,
    marginBottom: spacing.xxl,
  },
  row: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  rowEnd: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.lg,
  },
  gapMd: {
    gap: spacing.md,
  },
  symbolTile: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: spacing.xl,
    borderRadius: radii.lg,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border.default,
  },
  darkPanel: {
    backgroundColor: colors.surface.dark,
    borderRadius: radii.lg,
    padding: spacing.xl,
    alignItems: 'center',
  },
  paletteGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  swatch: {
    width: '47%',
    borderRadius: radii.md,
    padding: spacing.lg,
    gap: spacing.xxs,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border.default,
  },
  scaleLabel: {
    width: 80,
  },
  spacingBar: {
    height: 16,
    backgroundColor: colors.teal,
    borderRadius: radii.sm / 2,
  },
  radiusTile: {
    flex: 1,
    aspectRatio: 1,
    backgroundColor: colors.navy,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
