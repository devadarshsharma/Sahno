/**
 * Sahno typography tokens.
 *
 * Bricolage Grotesque is the selected wordmark/display direction
 * (BRAND_VAULT). Using it for body copy is still under validation; if it
 * tires at small sizes, body variants may move to a quieter family such as
 * Instrument Sans without touching component call sites.
 */

export const fontFamilies = {
  regular: 'BricolageGrotesque_400Regular',
  medium: 'BricolageGrotesque_500Medium',
  semiBold: 'BricolageGrotesque_600SemiBold',
  bold: 'BricolageGrotesque_700Bold',
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
    fontFamily: fontFamilies.regular,
    fontSize: 16,
    lineHeight: 24,
  },
  bodySmall: {
    fontFamily: fontFamilies.regular,
    fontSize: 14,
    lineHeight: 20,
  },
  label: {
    fontFamily: fontFamilies.medium,
    fontSize: 14,
    lineHeight: 20,
  },
  button: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 16,
    lineHeight: 22,
  },
  caption: {
    fontFamily: fontFamilies.regular,
    fontSize: 12,
    lineHeight: 16,
  },
} as const satisfies Record<string, TextVariantStyle>;

export type TextVariant = keyof typeof textVariants;
