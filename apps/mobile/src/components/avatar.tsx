import { Image } from 'expo-image';
import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { avatarUrl } from '@/api/avatars';
import { Text } from '@/components/ui';
import { useAvatarVersions, useImageToken } from '@/hooks/use-avatars';
import { initials, nameColour } from '@/lib/chat';
import { fontFamilies } from '@/theme';

/**
 * Somebody's photo, or their initials in their colour until there is one —
 * or if it will not load (D-129). One component, so a photo shows the same
 * everywhere: the members list, a member's page, Who's going, More.
 */
export function Avatar({
  userId,
  name,
  size = 40,
  version: givenVersion,
}: {
  userId: string;
  name: string | null;
  size?: number;
  /** Known already (your own, just changed); otherwise looked up. */
  version?: number | null;
}) {
  const versions = useAvatarVersions();
  const token = useImageToken();
  const [failed, setFailed] = useState<string | null>(null);

  const version = givenVersion === undefined ? versions.get(userId) : givenVersion;
  const source = version && token ? avatarUrl(userId, version) : null;
  const round = { width: size, height: size, borderRadius: size / 2 };

  if (source && failed !== source) {
    return (
      <Image
        source={{ uri: source, headers: { Authorization: `Bearer ${token}` } }}
        style={round}
        contentFit="cover"
        cachePolicy="disk"
        transition={120}
        accessibilityLabel={name ? `${name}'s photo` : 'Photo'}
        onError={() => setFailed(source)}
      />
    );
  }

  return (
    <View style={[styles.initials, round, { backgroundColor: nameColour(userId) }]}>
      <Text style={[styles.initialsText, { fontSize: Math.round(size * 0.38) }]}>{initials(name)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  initials: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  initialsText: {
    fontFamily: fontFamilies.uiMedium,
    color: '#FFFFFF',
  },
});
