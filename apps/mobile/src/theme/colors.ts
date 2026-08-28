/**
 * Sahno colour tokens.
 *
 * Brand values are v0.1 CANDIDATES sampled from
 * docs/brand/references/sahno-brand-direction-v0.1.png. Per
 * docs/brand/BRAND_VAULT.md, production values must come from the approved
 * vector master and pass contrast validation before being treated as locked.
 */

// Brand colour families (BRAND_VAULT "Working colour direction").
export const palette = {
  // Deep navy — trust, wordmark, high-contrast text, primary dark surface.
  navy: '#0B1B2A',
  navySoft: '#16283A',

  // Energetic orange — the mark's upper form and the dot. Fails WCAG contrast
  // as text on light backgrounds; use for the mark and large accents only.
  orange: '#F97B0A',
  orangeSoft: '#FDE4CC',

  // Confident teal — the mark's lower form. `teal` is decorative; `tealText`
  // is a darkened variant that passes 4.5:1 on off-white for tagline-style text.
  teal: '#2B8E88',
  tealText: '#1F6A65',
  tealSoft: '#D8ECEA',

  // Warm off-white — primary light canvas.
  offWhite: '#FAF7F2',
  white: '#FFFFFF',
} as const;

/**
 * Semantic UI colours. Error/focus greys are UI semantics, not brand colours:
 * BRAND_VAULT keeps status colours as semantic decisions, and no brand family
 * expresses an error state at accessible contrast.
 */
export const colors = {
  ...palette,

  text: {
    primary: palette.navy,
    secondary: '#44566A',
    muted: '#6B7A8C',
    inverse: palette.offWhite,
    accent: palette.tealText,
    error: '#B3261E',
  },

  surface: {
    canvas: palette.offWhite,
    raised: palette.white,
    dark: palette.navy,
    subtle: '#F1EBE2',
  },

  border: {
    default: '#DCD4C8',
    strong: '#B9AFA0',
    focus: palette.tealText,
    error: '#B3261E',
  },

  interactive: {
    primary: palette.navy,
    primaryPressed: '#1D3550',
    disabledBackground: '#D8D2C9',
    disabledText: '#8A8378',
  },
} as const;

export type Palette = typeof palette;
export type Colors = typeof colors;
