import { Ionicons } from '@expo/vector-icons';
import { Tabs } from 'expo-router';

import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, fontFamilies } from '@/theme';

/**
 * Primary navigation (D-042). One tab system for everyone; the Events tab is
 * labelled "Bookings" for organisers per the role-appropriate terminology in
 * D-039/D-042. Capabilities inside each tab derive from the active
 * membership's role — never from a mode switch (D-043).
 */
export default function TabsLayout() {
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.tealText,
        tabBarInactiveTintColor: colors.text.muted,
        tabBarStyle: {
          backgroundColor: colors.surface.raised,
          borderTopColor: colors.border.default,
        },
        tabBarLabelStyle: {
          fontFamily: fontFamilies.uiMedium,
          fontSize: 11,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: 'Home',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="home-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="events"
        options={{
          title: isOrganiser ? 'Bookings' : 'Events',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="calendar-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="people"
        options={{
          title: 'People',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="people-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="chat"
        options={{
          title: 'Chat',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="chatbubble-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="more"
        options={{
          title: 'More',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="menu-outline" size={size} color={color} />
          ),
        }}
      />
    </Tabs>
  );
}
