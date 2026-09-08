// https://docs.expo.dev/guides/using-eslint/
const path = require('path');
const { defineConfig } = require('eslint/config');
const expoConfig = require('eslint-config-expo/flat');

module.exports = defineConfig([
  expoConfig,
  {
    // pnpm hoists dependencies to the workspace root, so the import resolver
    // must look there as well as in the app's own node_modules.
    settings: {
      'import/resolver': {
        node: {
          moduleDirectory: [
            'node_modules',
            path.resolve(__dirname, '../../node_modules'),
          ],
        },
      },
    },
  },
  {
    ignores: ['dist/*'],
  },
]);
