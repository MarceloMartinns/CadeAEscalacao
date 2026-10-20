import { http, HttpResponse } from 'msw';
import type { Schemas } from '@/api/client';

// Answers of the fake API used by many tests. Each test can still add its own with server.use(...).

export const playerId = '0199f0a0-0000-7000-8000-000000000001';

export function tokens(accessToken: string): Schemas['TokenResponse'] {
    return { accessToken, expiresIn: 1800, refreshToken: null };
}

export function me(name: string | null): Schemas['MeResponse'] {
    return {
        userId: '0199f0a0-0000-7000-8000-0000000000aa',
        email: 'ze@example.com',
        player: name === null ? null : { id: playerId, name },
    };
}

export function problem(status: number, title = 'Error') {
    return HttpResponse.json({ status, title }, { status, headers: { 'Content-Type': 'application/problem+json' } });
}

/** The cookie still holds a valid session: the app comes back signed in. */
export const sessionFromCookie = (accessToken = 'cookie-token') =>
    http.post('/api/auth/refresh', () => HttpResponse.json(tokens(accessToken)));

/** No cookie (or an expired one): the app goes to the login. */
export const noSession = http.post('/api/auth/refresh', () => problem(401, 'Invalid refresh token'));

export const meAnswer = (name: string | null) => http.get('/api/me', () => HttpResponse.json(me(name)));

export const joaoId = '0199f0a0-0000-7000-8000-000000000002';
export const pedroId = '0199f0a0-0000-7000-8000-000000000003';
export const groupId = '0199f0a0-0000-7000-8000-0000000000g1';

/** "Terça": Zé (the signed-in player) is the president; João is a regular and Pedro a substitute. */
export function group(overrides: Partial<Schemas['GroupResponse']> = {}): Schemas['GroupResponse'] {
    return {
        id: groupId,
        name: 'Terça',
        playersPerTeam: 7,
        benchPlayersPerTeam: 1,
        matchDay: 'Tuesday',
        presidentId: playerId,
        members: [
            { playerId, name: 'Zé', type: 'Regular' },
            { playerId: joaoId, name: 'João', type: 'Regular' },
            { playerId: pedroId, name: 'Pedro', type: 'Substitute' },
        ],
        pairs: [{ firstPlayerId: playerId, secondPlayerId: joaoId }],
        coachRotation: [playerId, joaoId],
        ...overrides,
    };
}

export const myGroupsAnswer = (groups: Schemas['GroupSummaryResponse'][] = []) =>
    http.get('/api/groups', () => HttpResponse.json(groups));
