import Svg, { Circle, Path } from 'react-native-svg';

import { palette } from '@/theme';

/**
 * The Sahno S mark: an abstract human figure formed by a comma-headed orange
 * hook, a teal counter-hook, and a separate orange dot (BRAND_VAULT selected
 * v0.1 direction). Geometry was traced from
 * docs/brand/references/sahno-brand-direction-v0.1.png and is a candidate for
 * visual validation — not a locked production master.
 *
 * Guardrails: flat colour only (no gradients/effects); the orange and teal
 * forms must stay clearly separated; do not stretch, rotate, or recolour.
 */

const VIEW_BOX_WIDTH = 72;
const VIEW_BOX_HEIGHT = 116;

export type SahnoSymbolProps = {
  /** Rendered height in dp. Width scales to keep the mark's proportions. */
  size?: number;
};

export function SahnoSymbol({ size = 48 }: SahnoSymbolProps) {
  return (
    <Svg
      width={(size * VIEW_BOX_WIDTH) / VIEW_BOX_HEIGHT}
      height={size}
      viewBox="-2 -1 72 116"
      accessibilityRole="image"
      accessibilityLabel="Sahno symbol"
    >
      <Circle cx={27.5} cy={8} r={9.5} fill={palette.orange} />
      <Path
        d="M 48 36 C 40 28, 28 26, 19 31 C 10 36, 7 46, 8 58 C 9 68, 16 77, 28 79"
        fill="none"
        stroke={palette.orange}
        strokeWidth={16}
        strokeLinecap="round"
      />
      <Circle cx={50} cy={35} r={11} fill={palette.orange} />
      <Path
        d="M 40 58 C 49 62, 56 70, 58 80 C 60 90, 55 99, 44 102 C 36 104, 24 104, 17 101"
        fill="none"
        stroke={palette.teal}
        strokeWidth={16}
        strokeLinecap="round"
      />
      <Circle cx={40} cy={58} r={9.5} fill={palette.teal} />
      <Circle cx={16} cy={101} r={11} fill={palette.teal} />
    </Svg>
  );
}
