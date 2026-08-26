import { useQuery } from '@tanstack/react-query';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { getHealth } from '@/api/health';

export default function Index() {
  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => getHealth(signal),
  });

  return (
    <View style={styles.container}>
      <Text style={styles.brand}>Sahno</Text>
      <Text style={styles.subtitle}>Make it happen, together.</Text>

      <View style={styles.statusContainer}>
        {healthQuery.isPending ? (
          <>
            <ActivityIndicator />
            <Text style={styles.statusText}>Connecting to Sahno API…</Text>
          </>
        ) : null}

        {healthQuery.isError ? (
          <>
            <Text style={styles.errorTitle}>Could not connect</Text>
            <Text style={styles.errorMessage}>
              {healthQuery.error.message}
            </Text>

            <Pressable
              accessibilityRole="button"
              onPress={() => healthQuery.refetch()}
              style={styles.button}
            >
              <Text style={styles.buttonText}>Try again</Text>
            </Pressable>
          </>
        ) : null}

        {healthQuery.isSuccess ? (
          <>
            <Text style={styles.successTitle}>API connected</Text>
            <Text style={styles.statusText}>
              Status: {healthQuery.data.status}
            </Text>
          </>
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
    backgroundColor: '#F7FAF9',
  },
  brand: {
    fontSize: 36,
    fontWeight: '600',
    color: '#174F52',
    textAlign: 'center',
  },
  subtitle: {
    marginTop: 8,
    color: '#52666B',
    textAlign: 'center',
  },
  statusContainer: {
    marginTop: 32,
    padding: 20,
    gap: 12,
    borderRadius: 16,
    backgroundColor: '#FFFFFF',
  },
  statusText: {
    color: '#52666B',
    textAlign: 'center',
  },
  successTitle: {
    color: '#28716C',
    fontSize: 20,
    fontWeight: '600',
    textAlign: 'center',
  },
  errorTitle: {
    color: '#A13D32',
    fontSize: 20,
    fontWeight: '600',
    textAlign: 'center',
  },
  errorMessage: {
    color: '#6E4A46',
    textAlign: 'center',
  },
  button: {
    alignItems: 'center',
    marginTop: 8,
    padding: 12,
    borderRadius: 12,
    backgroundColor: '#174F52',
  },
  buttonText: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
});