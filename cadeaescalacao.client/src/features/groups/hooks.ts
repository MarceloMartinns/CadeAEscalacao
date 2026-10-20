import { useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '@/api/client';
import { useMe } from '@/features/auth/hooks';

type Group = Schemas['GroupResponse'];

export const groupKeys = {
    mine: ['groups'] as const,
    group: (groupId: string) => ['group', groupId] as const,
    lists: (groupId: string) => ['group', groupId, 'lists'] as const,
    seasons: (groupId: string) => ['group', groupId, 'seasons'] as const,
};

export function useMyGroups() {
    return useQuery({
        queryKey: groupKeys.mine,
        queryFn: () => unwrap(api.GET('/api/groups')),
    });
}

/** The group with its members' names (ADR-021). Cached: the other screens use it to show names. */
export function useGroup(groupId: string) {
    return useQuery({
        queryKey: groupKeys.group(groupId),
        queryFn: () => unwrap(api.GET('/api/groups/{id}', { params: { path: { id: groupId } } })),
        // Some screens learn the group only after their own data arrives.
        enabled: groupId !== '',
    });
}

/** The group's lists, newest first, with lineup and match (ADR-021). */
export function useGroupLists(groupId: string) {
    return useQuery({
        queryKey: groupKeys.lists(groupId),
        queryFn: () => unwrap(api.GET('/api/groups/{groupId}/lists', { params: { path: { groupId } } })),
    });
}

export function useGroupSeasons(groupId: string) {
    return useQuery({
        queryKey: groupKeys.seasons(groupId),
        queryFn: () => unwrap(api.GET('/api/groups/{groupId}/seasons', { params: { path: { groupId } } })),
    });
}

/** Name of a player of the group, for the IDs of lists, lineups, matches and rankings. */
export function usePlayerName(groupId: string) {
    const group = useGroup(groupId);
    const members = group.data?.members;
    return useCallback(
        (playerId: string) => members?.find((member) => member.playerId === playerId)?.name ?? 'Jogador',
        [members],
    );
}

/** True when the signed-in player is the president of the group (ADR-016). The API checks it again. */
export function useIsPresident(group: Group | undefined) {
    const me = useMe();
    return !!group && me.data?.player?.id === group.presidentId;
}

/** After a change that returns the group: put it in the cache, so every screen sees it at once. */
export function useGroupMutation<TVariables>(
    groupId: string,
    mutationFn: (variables: TVariables) => Promise<Group>,
) {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn,
        onSuccess: (group) => queryClient.setQueryData(groupKeys.group(groupId), group),
    });
}
