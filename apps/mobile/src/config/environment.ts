const apiUrl = process.env.EXPO_PUBLIC_API_URL;

if (!apiUrl) {
  throw new Error(
    'EXPO_PUBLIC_API_URL is required. Copy .env.example to .env and provide the API address.',
  );
}

export const environment = {
  apiUrl: apiUrl.replace(/\/+$/, ''),
} as const;