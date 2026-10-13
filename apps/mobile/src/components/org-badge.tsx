import { StyleSheet, View } from 'react-native';

import { Text } from '@/components/ui';
import { nameColour } from '@/lib/chat';
import { fontFamilies } from '@/theme';

/**
 * Whose an event is, when a list mixes organisations (D-098): the
 * organisation's name on a pill in a colour fixed to that organisation, so the
 * same group looks the same everywhere it appears.
 */
export function OrgBadge({ organisationId, name }: { organisationId: string; name: string }) {
  const colour = nameColour(organisationId);

  return (
    <View
      style={[styles.badge, { borderColor: colour }]}
      accessibilityLabel={`From ${name}`}
    >
      <View style={[styles.dot, { backgroundColor: colour }]} />
      <Text style={[styles.text, { color: colour }]} numberOfLines={1}>
        {name}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 4,
    maxWidth: '100%',
    paddingHorizontal: 6,
    paddingVertical: 1,
    borderRadius: 10,
    borderWidth: 1,
  },
  dot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  text: {
    fontSize: 11,
    lineHeight: 15,
    fontFamily: fontFamilies.uiMedium,
    flexShrink: 1,
  },
});
