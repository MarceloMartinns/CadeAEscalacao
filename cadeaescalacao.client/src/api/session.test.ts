import { delay, http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { me, problem, tokens } from '@/test/fakeApi';
import { server } from '@/test/server';
import { api } from './client';
import { getAccessToken, getSessionStatus, refreshSession, signIn } from './session';

describe('session (ADR-020)', () => {
    it('sends the access token in every request', async () => {
        let authorization: string | null = null;
        server.use(
            http.get('/api/me', ({ request }) => {
                authorization = request.headers.get('Authorization');
                return HttpResponse.json(me('Zé'));
            }),
        );
        signIn('token-1');

        await api.GET('/api/me');

        expect(authorization).toBe('Bearer token-1');
    });

    it('renews the session only once when several requests get 401 together', async () => {
        let refreshes = 0;
        let refreshBody: unknown;
        server.use(
            http.post('/api/auth/refresh', async ({ request }) => {
                refreshes++;
                refreshBody = await request.json();
                await delay(20);
                return HttpResponse.json(tokens('new-token'));
            }),
            http.get('/api/me', ({ request }) =>
                request.headers.get('Authorization') === 'Bearer new-token'
                    ? HttpResponse.json(me('Zé'))
                    : problem(401, 'Unauthorized'),
            ),
        );
        signIn('expired-token');

        const answers = await Promise.all([api.GET('/api/me'), api.GET('/api/me'), api.GET('/api/me')]);

        // A second refresh would reuse a rotated token and the API would revoke the session (ADR-015).
        expect(refreshes).toBe(1);
        expect(refreshBody).toEqual({ useCookie: true });
        expect(answers.map((answer) => answer.response.status)).toEqual([200, 200, 200]);
        expect(getAccessToken()).toBe('new-token');
    });

    it('repeats a request with its body after renewing the session', async () => {
        server.use(
            http.post('/api/auth/refresh', () => HttpResponse.json(tokens('new-token'))),
            http.post('/api/me', async ({ request }) => {
                if (request.headers.get('Authorization') !== 'Bearer new-token') {
                    return problem(401, 'Unauthorized');
                }

                const { name } = (await request.json()) as { name: string };
                return HttpResponse.json(
                    { player: { id: '1', name }, token: { accessToken: 'with-player', expiresIn: 1800 } },
                    { status: 201 },
                );
            }),
        );
        signIn('expired-token');

        const { data } = await api.POST('/api/me', { body: { name: 'Zé' } });

        expect(data?.player.name).toBe('Zé');
    });

    it('ends the session when it cannot be renewed', async () => {
        server.use(
            http.post('/api/auth/refresh', () => problem(401, 'Invalid refresh token')),
            http.get('/api/me', () => problem(401, 'Unauthorized')),
        );
        signIn('expired-token');

        const { response } = await api.GET('/api/me');

        expect(response.status).toBe(401);
        expect(getSessionStatus()).toBe('signedOut');
        expect(getAccessToken()).toBeNull();
    });

    it('shares one refresh between callers, like the two effects of StrictMode', async () => {
        let refreshes = 0;
        server.use(
            http.post('/api/auth/refresh', async () => {
                refreshes++;
                await delay(20);
                return HttpResponse.json(tokens('cookie-token'));
            }),
        );

        const [first, second] = await Promise.all([refreshSession(), refreshSession()]);

        expect(refreshes).toBe(1);
        expect(first).toBe('cookie-token');
        expect(second).toBe('cookie-token');
        expect(getSessionStatus()).toBe('signedIn');
    });
});
