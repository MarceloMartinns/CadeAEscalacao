import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { getAccessToken } from '@/api/session';
import { meAnswer, myGroupsAnswer, playerId, sessionFromCookie } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

describe('ProfilePage', () => {
    it('creates the profile and uses the new token, which has the player', async () => {
        server.use(
            sessionFromCookie(),
            meAnswer(null),
            myGroupsAnswer(),
            http.post('/api/me', async ({ request }) => {
                const { name } = (await request.json()) as { name: string };
                return HttpResponse.json(
                    { player: { id: playerId, name }, token: { accessToken: 'token-with-player', expiresIn: 1800 } },
                    { status: 201 },
                );
            }),
        );
        const { router } = renderApp('/grupos');
        const user = userEvent.setup();

        await user.type(await screen.findByLabelText(/nome/i), 'Zé');
        await user.click(screen.getByRole('button', { name: 'Salvar' }));

        expect(await screen.findByRole('heading', { name: 'Meus grupos' })).toBeInTheDocument();
        expect(router.state.location.pathname).toBe('/grupos');
        expect(getAccessToken()).toBe('token-with-player');
    });

    it('skips the page for whoever already has a profile', async () => {
        server.use(sessionFromCookie(), meAnswer('Zé'), myGroupsAnswer());

        const { router } = renderApp('/perfil');

        await waitFor(() => expect(router.state.location.pathname).toBe('/grupos'));
    });
});

describe('AppLayout', () => {
    it('signs out and goes back to the login', async () => {
        let loggedOut = false;
        server.use(
            sessionFromCookie(),
            meAnswer('Zé'),
            myGroupsAnswer(),
            http.post('/api/auth/logout', () => {
                loggedOut = true;
                return new HttpResponse(null, { status: 204 });
            }),
        );
        const { router } = renderApp('/grupos');
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Sair' }));

        await waitFor(() => expect(router.state.location.pathname).toBe('/login'));
        expect(loggedOut).toBe(true);
        expect(getAccessToken()).toBeNull();
        expect(await screen.findByRole('button', { name: 'Receber código' })).toBeInTheDocument();
    });
});
