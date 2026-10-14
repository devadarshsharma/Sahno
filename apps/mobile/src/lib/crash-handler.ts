import { Alert } from 'react-native';

import { describeError } from '@/components/crash-screen';

type GlobalHandler = (error: unknown, isFatal?: boolean) => void;

declare const ErrorUtils: {
  getGlobalHandler: () => GlobalHandler;
  setGlobalHandler: (handler: GlobalHandler) => void;
};

let installed = false;

/**
 * Errors outside drawing — in a tap handler, a timer, a callback — never reach
 * an error boundary, and in a release build React Native treats them as fatal
 * and closes the app with no word (D-110). This keeps the app open and says
 * what happened instead. In development the usual red screen still appears.
 *
 * Only fatal errors are intercepted; everything else goes to React Native's
 * own handler as before.
 */
export function installCrashHandler(): void {
  if (installed || __DEV__ || typeof ErrorUtils === 'undefined') {
    return;
  }
  installed = true;

  const previous = ErrorUtils.getGlobalHandler();
  let showing = false;

  ErrorUtils.setGlobalHandler((error, isFatal) => {
    if (!isFatal) {
      previous(error, isFatal);
      return;
    }
    // One alert at a time: an error in a loop should not stack a hundred.
    if (showing) {
      return;
    }
    showing = true;
    Alert.alert(
      'Something went wrong',
      `That did not work. If it keeps happening, take a screenshot of this and send it to us.\n\n${describeError(error).slice(0, 600)}`,
      [{ text: 'OK', onPress: () => (showing = false) }],
      { cancelable: true, onDismiss: () => (showing = false) },
    );
  });
}
