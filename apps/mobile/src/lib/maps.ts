import { Linking, Platform } from 'react-native';

/**
 * Opens a venue in the phone's maps app, searched by its name or address as
 * typed. Apple Maps on iOS; on Android the `geo:` link lets the person pick
 * Google Maps, Waze or whatever they use. Falls back to Google Maps on the web.
 */
export async function openInMaps(place: string): Promise<void> {
  const query = encodeURIComponent(place.trim());
  const native = Platform.OS === 'ios' ? `maps:0,0?q=${query}` : `geo:0,0?q=${query}`;

  try {
    await Linking.openURL(native);
  } catch {
    await Linking.openURL(`https://www.google.com/maps/search/?api=1&query=${query}`).catch(() => undefined);
  }
}
