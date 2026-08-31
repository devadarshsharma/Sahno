import { StatusBar } from 'expo-status-bar';
import { useEffect, useState } from 'react';
import {
  Dimensions,
  KeyboardAvoidingView,
  Pressable,
  TextInput as RNTextInput,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import Animated, {
  Easing,
  useAnimatedStyle,
  useSharedValue,
  withDelay,
  withTiming,
} from 'react-native-reanimated';

import {
  ContinueWithAppleButton,
  ContinueWithGoogleButton,
} from '@/components/auth/provider-buttons';
import { SahnoSymbol } from '@/components/brand';
import { Button, Text } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { colors, fontFamilies, radii, spacing } from '@/theme';

type PendingAction = 'google' | 'apple' | 'send-code' | 'verify-code' | null;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Dark-surface colours local to the authentication intro. The product dark
 * theme is still an open brand decision (BRAND_VAULT); these are candidates
 * scoped to this screen only.
 */
const dark = {
  background: colors.navy,
  field: '#16283A',
  fieldBorder: '#31465C',
  text: colors.offWhite,
  textDim: 'rgba(250, 247, 242, 0.64)',
  error: '#FFB4AB',
  cta: colors.tealText,
};

const INTRO_HOLD_MS = 900;
const INTRO_MOVE_MS = 600;

export default function SignIn() {
  const session = useSession();
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [codeSentTo, setCodeSentTo] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingAction>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);

  const busy = pending !== null;
  const errorMessage = actionError ?? session.error;

  // Intro: the mark starts centred (continuing the OS splash), holds briefly,
  // then glides to the top while the login controls rise into place beneath.
  const intro = useSharedValue(0);
  const screenHeight = Dimensions.get('window').height;
  const logoCenterOffset = screenHeight / 2 - 220;

  useEffect(() => {
    intro.value = withDelay(
      INTRO_HOLD_MS,
      withTiming(1, {
        duration: INTRO_MOVE_MS,
        easing: Easing.out(Easing.cubic),
      }),
    );
  }, [intro]);

  const logoStyle = useAnimatedStyle(() => ({
    transform: [
      { translateY: (1 - intro.value) * logoCenterOffset },
      { scale: 1 - intro.value * 0.25 },
    ],
  }));

  const contentStyle = useAnimatedStyle(() => ({
    opacity: intro.value,
    transform: [{ translateY: (1 - intro.value) * 30 }],
  }));

  async function run(action: PendingAction, work: () => Promise<void>) {
    setPending(action);
    setActionError(null);
    try {
      await work();
    } catch (error) {
      setActionError(
        error instanceof Error
          ? error.message
          : 'Something went wrong while signing you in. Please try again.',
      );
    } finally {
      setPending(null);
    }
  }

  function handleSendCode() {
    const trimmed = email.trim();
    if (!EMAIL_PATTERN.test(trimmed)) {
      setEmailError('Enter a valid email address.');
      return;
    }
    setEmailError(null);
    run('send-code', async () => {
      await session.sendEmailCode(trimmed);
      setCodeSentTo(trimmed);
      setCode('');
    });
  }

  function handleVerifyCode() {
    if (!codeSentTo) {
      return;
    }
    run('verify-code', () =>
      session.continueWithEmailCode(codeSentTo, code.trim()),
    );
  }

  return (
    <KeyboardAvoidingView
      style={styles.screen}
      // Android runs edge-to-edge on SDK 57, so the OS no longer resizes the
      // window for the keyboard — both platforms need active padding.
      behavior="padding"
    >
      <StatusBar style="light" />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <Animated.View style={[styles.logoBlock, logoStyle]}>
        <SahnoSymbol size={96} />
        <Text style={styles.wordmark} accessibilityRole="header">
          Sahno
        </Text>
        <Text style={styles.tagline}>Make it happen, together.</Text>
      </Animated.View>

      <Animated.View style={[styles.content, contentStyle]}>
        <Text variant="heading" style={styles.welcome}>
          Welcome to Sahno
        </Text>
        <Text variant="bodySmall" style={styles.subtitle}>
          Plan, coordinate and run events together.
        </Text>

        {errorMessage ? (
          <Text
            variant="bodySmall"
            accessibilityLiveRegion="polite"
            style={styles.error}
          >
            {errorMessage}
          </Text>
        ) : null}

        <View style={styles.actions}>
          <ContinueWithGoogleButton
            onPress={() => run('google', session.continueWithGoogle)}
            loading={pending === 'google'}
            disabled={busy}
          />
          <ContinueWithAppleButton
            onDark
            onPress={() => run('apple', session.continueWithApple)}
            loading={pending === 'apple'}
            disabled={busy}
          />

          <View style={styles.divider}>
            <View style={styles.dividerLine} />
            <Text variant="caption" style={styles.dividerText}>
              or
            </Text>
            <View style={styles.dividerLine} />
          </View>

          {codeSentTo === null ? (
            <>
              <RNTextInput
                accessibilityLabel="Email address"
                placeholder="Email address"
                placeholderTextColor={dark.textDim}
                value={email}
                onChangeText={setEmail}
                autoCapitalize="none"
                autoComplete="email"
                keyboardType="email-address"
                editable={!busy}
                style={[styles.input, emailError ? styles.inputError : null]}
              />
              {emailError ? (
                <Text variant="caption" style={styles.error}>
                  {emailError}
                </Text>
              ) : null}
              <Button
                label="Continue with email"
                onPress={handleSendCode}
                loading={pending === 'send-code'}
                disabled={busy}
                style={styles.cta}
              />
            </>
          ) : (
            <>
              <Text variant="bodySmall" style={styles.subtitle}>
                We sent a one-time code to {codeSentTo}. Enter it below to
                continue.
              </Text>
              <RNTextInput
                accessibilityLabel="One-time code"
                placeholder="One-time code"
                placeholderTextColor={dark.textDim}
                value={code}
                onChangeText={setCode}
                autoCapitalize="none"
                autoComplete="one-time-code"
                keyboardType="number-pad"
                editable={!busy}
                style={styles.input}
              />
              <Button
                label="Continue"
                onPress={handleVerifyCode}
                loading={pending === 'verify-code'}
                disabled={busy || code.trim().length === 0}
                style={styles.cta}
              />
              <View style={styles.linkRow}>
                <TextLink
                  label="Resend code"
                  onPress={handleSendCode}
                  disabled={busy}
                />
                <TextLink
                  label="Use a different email"
                  onPress={() => {
                    setCodeSentTo(null);
                    setCode('');
                    setActionError(null);
                  }}
                  disabled={busy}
                />
              </View>
            </>
          )}
        </View>

        <Text variant="caption" style={styles.terms}>
          Continuing creates your Sahno account or signs you back in — use the
          method you originally chose. By continuing you agree to our Terms of
          Service and Privacy Policy.
        </Text>
      </Animated.View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

function TextLink({
  label,
  onPress,
  disabled,
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ disabled: disabled ?? false }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [styles.link, pressed ? { opacity: 0.6 } : null]}
    >
      <Text variant="label" style={styles.linkText}>
        {label}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: dark.background,
  },
  scrollContent: {
    paddingHorizontal: spacing.xl,
    paddingTop: 72,
    paddingBottom: spacing.xxl,
  },
  logoBlock: {
    alignItems: 'center',
    gap: spacing.sm,
  },
  wordmark: {
    fontFamily: fontFamilies.bold,
    fontSize: 32,
    lineHeight: 38,
    color: dark.text,
  },
  tagline: {
    fontFamily: fontFamilies.medium,
    fontSize: 13,
    lineHeight: 18,
    color: colors.tealSoft,
  },
  content: {
    marginTop: spacing.xxl,
  },
  welcome: {
    color: dark.text,
    textAlign: 'center',
  },
  subtitle: {
    color: dark.textDim,
    textAlign: 'center',
    marginTop: spacing.xs,
  },
  error: {
    color: dark.error,
    textAlign: 'center',
    marginTop: spacing.md,
  },
  actions: {
    marginTop: spacing.xl,
    gap: spacing.md,
  },
  divider: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    marginVertical: spacing.xs,
  },
  dividerLine: {
    flex: 1,
    height: StyleSheet.hairlineWidth,
    backgroundColor: dark.fieldBorder,
  },
  dividerText: {
    color: dark.textDim,
  },
  input: {
    minHeight: 48,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: dark.fieldBorder,
    backgroundColor: dark.field,
    color: dark.text,
    paddingHorizontal: spacing.lg,
    fontFamily: fontFamilies.regular,
    fontSize: 16,
  },
  inputError: {
    borderColor: dark.error,
  },
  cta: {
    backgroundColor: dark.cta,
    borderColor: 'transparent',
  },
  linkRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: spacing.xl,
  },
  link: {
    minHeight: 44,
    justifyContent: 'center',
  },
  linkText: {
    color: colors.tealSoft,
  },
  terms: {
    color: dark.textDim,
    textAlign: 'center',
    marginTop: spacing.xl,
  },
});
