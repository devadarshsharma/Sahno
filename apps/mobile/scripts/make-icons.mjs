// Composes the app icons from the Sahno symbol (assets/images/splash-symbol.png)
// on the navy the brand vault names as the primary icon background. Run with
// `node scripts/make-icons.mjs` from apps/mobile after changing the symbol.
import { createRequire } from 'node:module';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const Jimp = require('jimp-compact');

const here = path.dirname(fileURLToPath(import.meta.url));
const images = path.join(here, '..', 'assets', 'images');

const NAVY = 0x0b1b2aff;
const SIZE = 1024;

async function main() {
  const symbol = await Jimp.read(path.join(images, 'splash-symbol.png'));

  // iOS / general icon: navy square, symbol at 62% of the height. iOS masks
  // the corners itself, so the square carries no rounding of its own.
  await compose(SIZE, NAVY, symbol, 0.62).writeAsync(path.join(images, 'icon.png'));

  // Android adaptive: the launcher crops to a circle/squircle from the inner
  // 66%, so the foreground symbol is kept to ~50% of the height, on
  // transparent, over a plain navy background layer.
  await compose(SIZE, 0x00000000, symbol, 0.5).writeAsync(
    path.join(images, 'android-icon-foreground.png'),
  );
  await new Jimp(SIZE, SIZE, NAVY).writeAsync(path.join(images, 'android-icon-background.png'));

  // Themed (monochrome) icon: the symbol as a white silhouette.
  const silhouette = symbol.clone();
  silhouette.scan(0, 0, silhouette.bitmap.width, silhouette.bitmap.height, (x, y, index) => {
    silhouette.bitmap.data[index] = 255;
    silhouette.bitmap.data[index + 1] = 255;
    silhouette.bitmap.data[index + 2] = 255;
  });
  await compose(SIZE, 0x00000000, silhouette, 0.5).writeAsync(
    path.join(images, 'android-icon-monochrome.png'),
  );

  // Android status-bar (small) notification icon: white silhouette on
  // transparent — Android tints it, so colour would be lost anyway.
  await compose(96, 0x00000000, silhouette, 0.9).writeAsync(
    path.join(images, 'notification-icon.png'),
  );

  // Web favicon: the same navy square, small.
  await compose(SIZE, NAVY, symbol, 0.7)
    .resize(64, 64)
    .writeAsync(path.join(images, 'favicon.png'));

  console.log('icons written to', images);
}

function compose(size, background, symbol, heightFraction) {
  const canvas = new Jimp(size, size, background);
  const scaled = symbol.clone().resize(Jimp.AUTO, Math.round(size * heightFraction));
  const x = Math.round((size - scaled.bitmap.width) / 2);
  const y = Math.round((size - scaled.bitmap.height) / 2);
  return canvas.composite(scaled, x, y);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
