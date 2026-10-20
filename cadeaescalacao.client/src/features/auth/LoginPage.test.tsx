import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { meAnswer, myGroupsAnswer, noSession, problem, tokens } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

async function requestCode(email = 'ze@example.com') {
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText(/e-mail/i), email);
    await user.click(screen.getByRole('button', { name: 'Receber código' }));
    await screen.findByText(/enviamos um código/i);
    return user;
}

async function typeCode(user: ReturnType<typeof userEvent.setup>, code: string) {
    const [firstDigit] = screen.getAllByLabelText('Código de acesso');
    await user.click(firstDigit);
    await user.keyboard(code);
}

describe('LoginPage', () => {
    it('sends the code to the e-mail and asks for it', async () => {
        let body: unknown;
        server.use(
            http.post('/api/auth/email/start', async ({ request }) => {
                body = await request.json();
                return new HttpResponse(null, { status: 202 });
            }),
        );
        renderApp('/login');

        await requestCode();

        expect(body).toEqual({ email: 'ze@example.com' });
        expect(screen.getByText('ze@example.com')).toBeInTheDocument();
    });

    it('signs in with the right code and asks for the name on the first access', async () => {
        let verifyBody: unknown;
        server.use(
            http.post('/api/auth/email/start', () => new HttpResponse(null, { status: 202 })),
            http.post('/api/auth/email/verify', async ({ request }) => {
                verifyBody = await request.json();
                return HttpResponse.json(tokens('login-token'));
            }),
            meAnswer(null),
        );
        renderApp('/login');

        const user = await requestCode();
        await typeCode(user, '123456');

        expect(await screen.findByRole('heading', { name: 'Bem-vindo!' })).toBeInTheDocument();
        // The refresh token goes to the HttpOnly cookie, not to JavaScript (ADR-020).
        expect(verifyBody).toEqual({ email: 'ze@example.com', code: '123456', useCookie: true });
    });

    it('shows a message for a wrong code', async () => {
        server.use(
            http.post('/api/auth/email/start', () => new HttpResponse(null, { status: 202 })),
            http.post('/api/auth/email/verify', () => problem(400, 'Invalid code')),
        );
        renderApp('/login');

        const user = await requestCode();
        await typeCode(user, '000000');

        expect(await screen.findByRole('alert')).toHaveTextContent('Código inválido ou expirado');
    });

    it('explains the wait after too many requests', async () => {
        server.use(http.post('/api/auth/email/start', () => problem(429, 'Too Many Requests')));
        renderApp('/login');
        const user = userEvent.setup();

        await user.type(await screen.findByLabelText(/e-mail/i), 'ze@example.com');
        await user.click(screen.getByRole('button', { name: 'Receber código' }));

        expect(await screen.findByRole('alert')).toHaveTextContent('Espere um minuto');
    });

    it('signs in straight from the e-mail link', async () => {
        let verifyBody: unknown;
        server.use(
            http.post('/api/auth/email/verify', async ({ request }) => {
                verifyBody = await request.json();
                return HttpResponse.json(tokens('login-token'));
            }),
            meAnswer('Zé'),
            myGroupsAnswer(),
        );

        const { router } = renderApp('/login#email=ze%40example.com&code=111111');

        expect(await screen.findByRole('heading', { name: 'Meus grupos' })).toBeInTheDocument();
        expect(verifyBody).toEqual({ email: 'ze@example.com', code: '111111', useCookie: true });
        // The code does not stay in the address.
        expect(router.state.location.hash).toBe('');
    });

    it('asks for a new code when the link has expired', async () => {
        server.use(http.post('/api/auth/email/verify', () => problem(400, 'Invalid code')));

        renderApp('/login#email=ze%40example.com&code=222222');

        expect(await screen.findByRole('alert')).toHaveTextContent('O link expirou');
        expect(screen.getByLabelText(/e-mail/i)).toHaveValue('ze@example.com');
    });

    it('sends to the login whoever opens a page without a session, and back afterwards', async () => {
        server.use(
            noSession,
            http.post('/api/auth/email/start', () => new HttpResponse(null, { status: 202 })),
            http.post('/api/auth/email/verify', () => HttpResponse.json(tokens('login-token'))),
            meAnswer('Zé'),
            myGroupsAnswer(),
        );
        const { router } = renderApp('/grupos');

        const user = await requestCode();
        await typeCode(user, '333333');

        await waitFor(() => expect(router.state.location.pathname).toBe('/grupos'));
        expect(await screen.findByRole('heading', { name: 'Meus grupos' })).toBeInTheDocument();
    });
});
