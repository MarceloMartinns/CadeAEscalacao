import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/client';

/** The TanStack Query cache (ADR-018). */
export function createQueryClient() {
    return new QueryClient({
        defaultOptions: {
            queries: {
                staleTime: 30_000,
                // Errors answered by the API (403, 404...) do not get better by trying again.
                retry: (failureCount, error) => !(error instanceof ApiError) && failureCount < 2,
            },
        },
    });
}
