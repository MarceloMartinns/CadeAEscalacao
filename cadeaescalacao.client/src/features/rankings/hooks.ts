import { useQuery } from '@tanstack/react-query';
import { api, unwrap } from '@/api/client';

export function useRanking(seasonId: string) {
    return useQuery({
        queryKey: ['ranking', seasonId],
        queryFn: () => unwrap(api.GET('/api/seasons/{seasonId}/ranking', { params: { path: { seasonId } } })),
    });
}
