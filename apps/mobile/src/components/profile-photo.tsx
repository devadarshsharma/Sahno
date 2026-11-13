import { Ionicons } from '@expo/vector-icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { manipulateAsync, SaveFormat } from 'expo-image-manipulator';
import * as ImagePicker from 'expo-image-picker';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { removeAvatar, uploadAvatar } from '@/api/avatars';
import { Avatar } from '@/components/avatar';
import { showAlert, showToast, Text } from '@/components/ui';
import { useAvatarVersions } from '@/hooks/use-avatars';
import { useSession } from '@/providers/auth-provider';
import { colors, spacing } from '@/theme';

/** Photos are sent square and this many pixels across — about 25 KB (D-129). */
const PHOTO_SIZE = 256;

/**
 * Your own photo, on More (D-129): tap to take one or choose one; cropped
 * square on the phone, shrunk, then sent. Seen by everyone you share an
 * organisation with.
 */
export function ProfilePhoto({ userId, name }: { userId: string; name: string | null }) {
  const session = useSession();
  const queryClient = useQueryClient();
  const versions = useAvatarVersions();
  const hasPhoto = versions.has(userId);

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['org'], predicate: (query) => query.queryKey[2] === 'avatars' });

  const upload = useMutation({
    mutationFn: async (uri: string) => {
      const small = await manipulateAsync(uri, [{ resize: { width: PHOTO_SIZE, height: PHOTO_SIZE } }], {
        compress: 0.8,
        format: SaveFormat.JPEG,
      });
      return uploadAvatar(await session.getAccessToken(), small.uri);
    },
    onSuccess: async () => {
      await refresh();
      showToast('Photo updated');
    },
    onError: () => showAlert('Could not save your photo', 'Check your connection and try again.'),
  });

  const remove = useMutation({
    mutationFn: async () => removeAvatar(await session.getAccessToken()),
    onSuccess: async () => {
      await refresh();
      showToast('Photo removed');
    },
    onError: () => showAlert('Could not remove your photo', 'Check your connection and try again.'),
  });

  async function pick(from: 'camera' | 'library') {
    const permission =
      from === 'camera'
        ? await ImagePicker.requestCameraPermissionsAsync()
        : await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      showAlert(
        from === 'camera' ? 'Camera not allowed' : 'Photos not allowed',
        'You can allow it for Sahno in your phone’s Settings.',
      );
      return;
    }

    const options: ImagePicker.ImagePickerOptions = { mediaTypes: ['images'], allowsEditing: true, aspect: [1, 1], quality: 1 };
    const result =
      from === 'camera' ? await ImagePicker.launchCameraAsync(options) : await ImagePicker.launchImageLibraryAsync(options);
    if (!result.canceled && result.assets[0]) {
      upload.mutate(result.assets[0].uri);
    }
  }

  function choose() {
    showAlert('Profile photo', 'Everyone in your organisations sees it.', [
      { text: 'Take a photo', onPress: () => void pick('camera') },
      { text: 'Choose from library', onPress: () => void pick('library') },
      ...(hasPhoto ? [{ text: 'Remove photo', style: 'destructive' as const, onPress: () => remove.mutate() }] : []),
      { text: 'Cancel', style: 'cancel' as const },
    ]);
  }

  const busy = upload.isPending || remove.isPending;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={hasPhoto ? 'Change your photo' : 'Add a photo'}
      disabled={busy}
      onPress={choose}
      style={({ pressed }) => [styles.row, pressed ? styles.pressed : null]}
    >
      <View>
        <Avatar userId={userId} name={name} size={64} />
        <View style={styles.badge}>
          {busy ? (
            <ActivityIndicator size="small" color={colors.offWhite} />
          ) : (
            <Ionicons name="camera" size={14} color={colors.offWhite} />
          )}
        </View>
      </View>
      <Text variant="bodySmall" color="accent">
        {hasPhoto ? 'Change photo' : 'Add a photo'}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    alignSelf: 'flex-start',
  },
  pressed: {
    opacity: 0.7,
  },
  badge: {
    position: 'absolute',
    right: -2,
    bottom: -2,
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: colors.tealText,
    borderWidth: 2,
    borderColor: colors.surface.raised,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
