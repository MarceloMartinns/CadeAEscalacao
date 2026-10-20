import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '@/api/client';
import { groupKeys } from '@/features/groups/hooks';

type Lineup = Schemas['LineupResponse'];

export const lineupKeys = {
    lineup: (lineupId: string) => ['lineup', lineupId] as const,
};

/** How often an unfinished lineup is reloaded, so the partner sees the teams once the coach publishes them. */
const unfinishedLineupRefreshMs = 15_000;

export function useLineup(lineupId: string) {
    return useQuery({
        queryKey: lineupKeys.lineup(lineupId),
        queryFn: () => unwrap(api.GET('/api/lineups/{id}', { params: { path: { id: lineupId } } })),
        refetchInterval: (query) => (query.state.data && query.state.data.status !== 'Completed' ? unfinishedLineupRefreshMs : false),
    });
}

/** A step of the lineup. The answer goes to the cache; the group screen and the rotation change too. */
export function useLineupMutation<TVariables>(lineupId: string, mutationFn: (variables: TVariables) => Promise<Lineup>) {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn,
        onSuccess: async (lineup) => {
            queryClient.setQueryData(lineupKeys.lineup(lineupId), lineup);
            await Promise.all([
                queryClient.invalidateQueries({ queryKey: groupKeys.lists(lineup.groupId) }),
                // When the partner chooses, the coach goes to the end of the rotation.
                queryClient.invalidateQueries({ queryKey: groupKeys.group(lineup.groupId), exact: true }),
            ]);
        },
    });
}
