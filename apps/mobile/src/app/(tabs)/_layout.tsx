import { Ionicons } from '@expo/vector-icons';
import { Tabs } from 'expo-router';

import { BADGE_COLOR } from '@/components/ui';
import { useEngagements } from '@/hooks/use-engagements';
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

  // Every unread chat message across the organisation's events, on the Chat
  // tab, so a new message is noticed from anywhere in the app.
  const engagements = useEngagements();
  const unreadChat = (engagements.data ?? []).reduce(
    (total, engagement) => total + (engagement.unreadMessages ?? 0),
    0,
  );

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
          tabBarBadge: unreadChat > 0 ? (unreadChat > 99 ? '99+' : unreadChat) : undefined,
          tabBarBadgeStyle: {
            backgroundColor: BADGE_COLOR,
            color: colors.offWhite,
            fontFamily: fontFamilies.bold,
            fontSize: 11,
          },
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
