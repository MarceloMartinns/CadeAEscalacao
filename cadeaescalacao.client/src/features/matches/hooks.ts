import { useQuery } from '@tanstack/react-query';
import { api, unwrap } from '@/api/client';

export function useMatch(matchId: string) {
    return useQuery({
        queryKey: ['match', matchId],
        queryFn: () => unwrap(api.GET('/api/matches/{id}', { params: { path: { id: matchId } } })),
    });
}
