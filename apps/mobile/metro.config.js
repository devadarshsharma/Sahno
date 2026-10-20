// Expo's default Metro config, with Sentry's serializer added so each bundle
// carries debug ids: a crash report's stack then maps back to the source
// files, once source maps are uploaded at build time (D-117).
const { getSentryExpoConfig } = require('@sentry/react-native/metro');

module.exports = getSentryExpoConfig(__dirname);
