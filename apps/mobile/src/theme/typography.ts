/**
 * Sahno typography tokens.
 *
 * Bricolage Grotesque is the selected wordmark/display direction
 * (BRAND_VAULT) and carries headings and buttons. Body and dense UI text use
 * Instrument Sans — the quieter pairing BRAND_VAULT proposed and the family
 * the organiser mockups are set in. Candidate pairing pending on-device
 * validation.
 */

export const fontFamilies = {
  // Display family — headings, buttons, the wordmark.
  regular: 'BricolageGrotesque_400Regular',
  medium: 'BricolageGrotesque_500Medium',
  semiBold: 'BricolageGrotesque_600SemiBold',
  bold: 'BricolageGrotesque_700Bold',
  // UI family — body copy, labels, captions, dense operational text.
  uiRegular: 'InstrumentSans_400Regular',
  uiMedium: 'InstrumentSans_500Medium',
  uiSemiBold: 'InstrumentSans_600SemiBold',
} as const;

export type FontFamilyKey = keyof typeof fontFamilies;

type TextVariantStyle = {
  fontFamily: (typeof fontFamilies)[FontFamilyKey];
  fontSize: number;
  lineHeight: number;
  letterSpacing?: number;
};

export const textVariants = {
  display: {
    fontFamily: fontFamilies.bold,
    fontSize: 36,
    lineHeight: 42,
    letterSpacing: -0.5,
  },
  title: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 28,
    lineHeight: 34,
    letterSpacing: -0.25,
  },
  heading: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 22,
    lineHeight: 28,
  },
  subheading: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 17,
    lineHeight: 24,
  },
  body: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 16,
    lineHeight: 24,
  },
  bodySmall: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 14,
    lineHeight: 20,
  },
  label: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 14,
    lineHeight: 20,
  },
  button: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 16,
    lineHeight: 22,
  },
  caption: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 12,
    lineHeight: 16,
  },
} as const satisfies Record<string, TextVariantStyle>;

export type TextVariant = keyof typeof textVariants;
