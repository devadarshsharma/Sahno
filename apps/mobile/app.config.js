const fs = require('node:fs');
const path = require('node:path');

/**
 * Layers per-machine bits over app.json. The Firebase config that Android
 * push rides on (google-services.json) is never committed, so it is wired in
 * only where it exists — a machine without it still builds and runs, just
 * without push tokens on Android.
 */
module.exports = ({ config }) => {
  const googleServices = path.join(__dirname, 'google-services.json');

  return {
    ...config,
    android: {
      ...config.android,
      ...(fs.existsSync(googleServices)
        ? { googleServicesFile: './google-services.json' }
        : {}),
    },
  };
};
