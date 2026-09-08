import { useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import Animated, { FadeInDown, FadeInUp } from 'react-native-reanimated';

import { SahnoSymbol } from '@/components/brand';
import { Button, Screen, Text } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

/**
 * First-time journey (D-056): a warm welcome, then the create/join choice.
 * There is deliberately no role question — creating makes you the Owner,
 * joining makes you a Member; role lives on each membership (D-044).
 */
export default function Onboarding() {
  const router = useRouter();
  const session = useSession();
  const [step, setStep] = useState<'welcome' | 'choose'>('welcome');
  const firstName = session.user?.name?.split(' ')[0];

  if (step === 'welcome') {
    return (
      <View style={styles.welcomeScreen}>
        <StatusBar style="light" />
        <View style={styles.welcomeBody}>
          <Animated.View entering={FadeInDown.duration(500)} style={styles.welcomeLogo}>
            <SahnoSymbol size={84} />
            <Text style={styles.wordmark}>Sahno</Text>
          </Animated.View>

          <Animated.View
            entering={FadeInDown.delay(250).duration(500)}
            style={styles.illustration}
          >
            <Text style={styles.illustrationEmoji}>👋</Text>
          </Animated.View>

          <Animated.View entering={FadeInDown.delay(450).duration(500)}>
            <Text variant="title" style={styles.welcomeTitle}>
              Welcome to Sahno{firstName ? `, ${firstName}` : ''}!
            </Text>
            <Text style={styles.welcomeSubtitle}>
              You&apos;re all set. Let&apos;s help you get started.
            </Text>
          </Animated.View>
        </View>

        <Animated.View entering={FadeInUp.delay(650).duration(500)}>
          <Button
            label="Get Started"
            onPress={() => setStep('choose')}
            style={styles.getStarted}
          />
        </Animated.View>
      </View>
    );
  }

  return (
    <Screen>
      <View style={styles.chooseContainer}>
        <Animated.View
          entering={FadeInDown.duration(400)}
          style={styles.chooseHeader}
        >
          <View style={styles.chooseIllustration}>
            <Text style={styles.chooseIllustrationEmoji}>🎪</Text>
          </View>
          <Text variant="title" style={styles.centeredText}>
            What would you like to do?
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
            You can always do the other one later.
          </Text>
        </Animated.View>

        <View style={styles.options}>
          <Animated.View entering={FadeInDown.delay(200).duration(400)}>
            <OptionCard
              emoji="👥"
              tint={colors.tealSoft}
              title="Join an organisation"
              subtitle="I have an invite from my organiser"
              onPress={() => router.push('/join')}
            />
          </Animated.View>
          <Animated.View entering={FadeInDown.delay(350).duration(400)}>
            <OptionCard
              emoji="✨"
              tint={colors.orangeSoft}
              title="Create an organisation"
              subtitle="Start and organise a group"
              onPress={() => router.push('/create-organisation')}
            />
          </Animated.View>
        </View>

        <Animated.View entering={FadeInUp.delay(500).duration(400)}>
          <Button label="Sign out" variant="ghost" onPress={session.signOut} />
        </Animated.View>
      </View>
    </Screen>
  );
}

function OptionCard({
  emoji,
  tint,
  title,
  subtitle,
  onPress,
}: {
  emoji: string;
  tint: string;
  title: string;
  subtitle: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${title}. ${subtitle}`}
      onPress={onPress}
      style={({ pressed }) => [
        styles.option,
        pressed ? styles.optionPressed : null,
      ]}
    >
      <View style={[styles.optionEmoji, { backgroundColor: tint }]}>
        <Text variant="heading">{emoji}</Text>
      </View>
      <View style={styles.optionText}>
        <Text variant="subheading">{title}</Text>
        <Text variant="bodySmall" color="secondary">
          {subtitle}
        </Text>
      </View>
      <Text variant="heading" color="muted">
        ›
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  welcomeScreen: {
    flex: 1,
    backgroundColor: colors.navy,
    paddingHorizontal: spacing.xl,
    paddingTop: 72,
    paddingBottom: spacing.xxl,
  },
  welcomeBody: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xl,
  },
  welcomeLogo: {
    alignItems: 'center',
    gap: spacing.sm,
  },
  wordmark: {
    fontFamily: fontFamilies.bold,
    fontSize: 30,
    lineHeight: 36,
    color: colors.offWhite,
  },
  illustration: {
    width: 108,
    height: 108,
    borderRadius: 54,
    backgroundColor: '#16283A',
    alignItems: 'center',
    justifyContent: 'center',
  },
  illustrationEmoji: {
    fontSize: 52,
    lineHeight: 64,
  },
  welcomeTitle: {
    color: colors.offWhite,
    textAlign: 'center',
  },
  welcomeSubtitle: {
    color: 'rgba(250, 247, 242, 0.7)',
    textAlign: 'center',
    marginTop: spacing.sm,
  },
  getStarted: {
    backgroundColor: colors.tealText,
    borderColor: 'transparent',
  },
  chooseContainer: {
    flex: 1,
    justifyContent: 'center',
    gap: spacing.xl,
  },
  chooseHeader: {
    alignItems: 'center',
    gap: spacing.sm,
  },
  chooseIllustration: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.surface.subtle,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.sm,
  },
  chooseIllustrationEmoji: {
    fontSize: 40,
    lineHeight: 50,
  },
  centeredText: {
    textAlign: 'center',
  },
  options: {
    gap: spacing.md,
  },
  option: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.lg,
    padding: spacing.lg,
    borderRadius: radii.lg,
    backgroundColor: colors.surface.raised,
    ...shadows.sm,
  },
  optionPressed: {
    backgroundColor: colors.surface.subtle,
  },
  optionEmoji: {
    width: 48,
    height: 48,
    borderRadius: radii.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  optionText: {
    flex: 1,
    gap: 2,
  },
});
