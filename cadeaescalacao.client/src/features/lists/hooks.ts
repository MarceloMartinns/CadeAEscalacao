import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '@/api/client';
import { groupKeys } from '@/features/groups/hooks';

type WeeklyList = Schemas['WeeklyListResponse'];

export const listKeys = {
    list: (listId: string) => ['list', listId] as const,
};

/** How often an open list is reloaded, so everyone sees the spots change. */
const openListRefreshMs = 30_000;

export function useWeeklyList(listId: string) {
    return useQuery({
        queryKey: listKeys.list(listId),
        queryFn: () => unwrap(api.GET('/api/lists/{id}', { params: { path: { id: listId } } })),
        refetchInterval: (query) => (query.state.data?.status === 'Open' ? openListRefreshMs : false),
    });
}

/** A change made by the signed-in player. The API answers with the whole list, which goes to the cache. */
export function useListMutation<TVariables>(listId: string, mutationFn: (variables: TVariables) => Promise<WeeklyList>) {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn,
        onSuccess: async (list) => {
            queryClient.setQueryData(listKeys.list(listId), list);
            await queryClient.invalidateQueries({ queryKey: groupKeys.lists(list.groupId) });
        },
    });
}
