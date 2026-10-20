import createClient from 'openapi-fetch';
import type { paths } from './schema';

/**
 * The browser session (ADR-020).
 * - The access token lives only in this module's memory: a reload loses it.
 * - The refresh token lives in an HttpOnly cookie that JavaScript cannot read; the API reads it
 *   on POST /api/auth/refresh.
 */

export type SessionStatus = 'unknown' | 'signedIn' | 'signedOut';

let accessToken: string | null = null;
let status: SessionStatus = 'unknown';
let refreshing: Promise<string | null> | null = null;
const listeners = new Set<() => void>();

/** Client without the session logic, for the /api/auth endpoints themselves. */
export const authClient = createClient<paths>({
    baseUrl: window.location.origin,
    fetch: (request) => globalThis.fetch(request),
});

export function getAccessToken(): string | null {
    return accessToken;
}

export function getSessionStatus(): SessionStatus {
    return status;
}

/** For React's useSyncExternalStore: re-renders when the person signs in or out. */
export function subscribeToSession(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/** Starts the session with a token from the login or from POST /api/me. */
export function signIn(token: string): void {
    accessToken = token;
    setStatus('signedIn');
}

/** Forgets the access token. The cookie is removed by the API (see signOut). */
export function clearSession(): void {
    accessToken = null;
    setStatus('signedOut');
}

/**
 * Gets a new access token with the cookie. Every caller waits for the SAME request: the refresh
 * token is rotated, and using it twice would revoke the whole session (ADR-015). This also covers
 * React's StrictMode, which runs effects twice in development.
 */
export function refreshSession(): Promise<string | null> {
    refreshing ??= requestNewToken().finally(() => {
        refreshing = null;
    });
    return refreshing;
}

/** Logout: the API revokes the refresh token and deletes the cookie. */
export async function signOut(): Promise<void> {
    try {
        await authClient.POST('/api/auth/logout', { body: { useCookie: true } });
    } finally {
        clearSession();
    }
}

async function requestNewToken(): Promise<string | null> {
    try {
        const { data } = await authClient.POST('/api/auth/refresh', { body: { useCookie: true } });
        if (data) {
            signIn(data.accessToken);
            return data.accessToken;
        }
    } catch {
        // Network error: same as an invalid session.
    }

    clearSession();
    return null;
}

function setStatus(next: SessionStatus): void {
    status = next;
    listeners.forEach((listener) => listener());
}

/** Only for the tests: back to the state of a page that was just opened. */
export function resetSessionForTests(): void {
    accessToken = null;
    status = 'unknown';
    refreshing = null;
    listeners.clear();
}
