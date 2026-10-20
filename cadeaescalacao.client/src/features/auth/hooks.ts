import { useSyncExternalStore } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap } from '@/api/client';
import { getSessionStatus, subscribeToSession } from '@/api/session';

/** 'unknown' while the app has not tried the cookie yet, then 'signedIn' or 'signedOut' (ADR-020). */
export function useSessionStatus() {
    return useSyncExternalStore(subscribeToSession, getSessionStatus);
}

export const meQueryKey = ['me'] as const;

/** The signed-in account and its player (null until the profile is created). */
export function useMe() {
    return useQuery({
        queryKey: meQueryKey,
        queryFn: () => unwrap(api.GET('/api/me')),
    });
}
