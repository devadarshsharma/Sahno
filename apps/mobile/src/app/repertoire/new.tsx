import { useRouter } from 'expo-router';
import { StyleSheet } from 'react-native';

import { PieceForm } from '@/components/piece-form';
import { Card, Screen } from '@/components/ui';
import { spacing } from '@/theme';

/** Add a piece to the repertoire, then open it. */
export default function NewPiece() {
  const router = useRouter();

  return (
    <Screen
      scroll
      hero={{
        title: 'New piece',
        subtitle: 'Type the words once. Every set list from now on just picks it.',
      }}
    >
      <Card style={styles.card}>
        <PieceForm
          onSaved={(piece) => router.replace(`/repertoire/${piece.id}`)}
          onCancel={() => router.back()}
        />
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
  },
});
