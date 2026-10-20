import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { Schemas } from '@/api/client';
import { group, groupId, joaoId, meAnswer, myGroupsAnswer, playerId, problem, sessionFromCookie } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

const listId = '0199f0a0-0000-7000-8000-0000000000l1';

function openList(overrides: Partial<Schemas['WeeklyListSummaryResponse']> = {}): Schemas['WeeklyListSummaryResponse'] {
    return { id: listId, matchDate: '2026-10-20', status: 'Open', lineupId: null, lineupStatus: null, matchId: null, ...overrides };
}

function groupPageAnswers(options: { group?: Schemas['GroupResponse']; lists?: Schemas['WeeklyListSummaryResponse'][] } = {}) {
    return [
        sessionFromCookie(),
        meAnswer('Zé'),
        http.get('/api/groups/:id', () => HttpResponse.json(options.group ?? group())),
        http.get('/api/groups/:groupId/lists', () => HttpResponse.json(options.lists ?? [])),
        http.get('/api/groups/:groupId/seasons', () => HttpResponse.json([])),
    ];
}

describe('GroupsPage', () => {
    it('lists the groups and marks where the player is president', async () => {
        server.use(
            sessionFromCookie(),
            meAnswer('Zé'),
            myGroupsAnswer([
                { id: groupId, name: 'Terça', matchDay: 'Tuesday', playersPerTeam: 7, benchPlayersPerTeam: 1, presidentId: playerId },
                { id: 'other', name: 'Futsal', matchDay: 'Thursday', playersPerTeam: 5, benchPlayersPerTeam: 1, presidentId: joaoId },
            ]),
        );

        renderApp('/grupos');

        const tuesday = (await screen.findByText('Terça')).closest('a')!;
        expect(tuesday).toHaveAttribute('href', `/grupos/${groupId}`);
        expect(within(tuesday).getByText('Presidente')).toBeInTheDocument();
        expect(within(screen.getByText('Futsal').closest('a')!).queryByText('Presidente')).not.toBeInTheDocument();
    });

    it('tells who has no group how to join one', async () => {
        server.use(sessionFromCookie(), meAnswer('Zé'), myGroupsAnswer([]));

        renderApp('/grupos');

        expect(await screen.findByText('Você ainda não está em nenhum grupo.')).toBeInTheDocument();
        expect(screen.getByText('ze@example.com')).toBeInTheDocument();
    });
});

describe('CreateGroupPage', () => {
    it('creates a society group and opens it', async () => {
        let body: unknown;
        server.use(
            ...groupPageAnswers(),
            myGroupsAnswer([]),
            http.post('/api/groups', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(group({ name: 'Society de terça' }), { status: 201 });
            }),
        );
        const { router } = renderApp('/grupos/novo');
        const user = userEvent.setup();

        await user.type(await screen.findByLabelText(/nome do grupo/i), 'Society de terça');
        await user.click(screen.getByRole('combobox', { name: /dia do jogo/i }));
        await user.click(await screen.findByRole('option', { name: 'Terça' }));
        await user.click(screen.getByRole('button', { name: 'Criar grupo' }));

        await waitFor(() => expect(router.state.location.pathname).toBe(`/grupos/${groupId}`));
        expect(body).toEqual({ name: 'Society de terça', matchDay: 'Tuesday', playersPerTeam: 7, benchPlayersPerTeam: 1 });
    });
});

describe('GroupPage', () => {
    it('shows the president the buttons to run the week', async () => {
        server.use(...groupPageAnswers({ lists: [openList()] }));

        renderApp(`/grupos/${groupId}`);

        expect(await screen.findByRole('button', { name: 'Fechar lista' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Cancelar jogo' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Ver lista' })).toHaveAttribute('href', `/listas/${listId}`);
    });

    it('does not show the president buttons to the other members', async () => {
        server.use(...groupPageAnswers({ group: group({ presidentId: joaoId }), lists: [openList()] }));

        renderApp(`/grupos/${groupId}`);

        expect(await screen.findByRole('link', { name: 'Ver lista' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Fechar lista' })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Abrir lista' })).not.toBeInTheDocument();
    });

    it('opens the list of the next match day', async () => {
        let body: unknown;
        let lists: Schemas['WeeklyListSummaryResponse'][] = [];
        server.use(
            // The first answer that matches wins, so this one comes before the defaults.
            http.get('/api/groups/:groupId/lists', () => HttpResponse.json(lists)),
            ...groupPageAnswers(),
            http.post('/api/groups/:groupId/lists', async ({ request }) => {
                body = await request.json();
                lists = [openList({ matchDate: (body as { matchDate: string }).matchDate })];
                return HttpResponse.json({}, { status: 201 });
            }),
        );
        renderApp(`/grupos/${groupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Abrir lista' }));
        const dialog = await screen.findByRole('dialog', { name: 'Abrir lista' });
        await user.click(within(dialog).getByRole('button', { name: 'Abrir lista' }));

        expect(await screen.findByRole('button', { name: 'Fechar lista' })).toBeInTheDocument();
        // The suggested date is the group's next match day (Tuesday).
        expect(new Date(`${(body as { matchDate: string }).matchDate}T12:00`).getDay()).toBe(2);
    });

    it('explains when there is already a list for the date', async () => {
        server.use(
            ...groupPageAnswers(),
            http.post('/api/groups/:groupId/lists', () => problem(409, 'Conflict')),
        );
        renderApp(`/grupos/${groupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Abrir lista' }));
        const dialog = await screen.findByRole('dialog', { name: 'Abrir lista' });
        await user.click(within(dialog).getByRole('button', { name: 'Abrir lista' }));

        expect(await within(dialog).findByRole('alert')).toHaveTextContent('Já existe uma lista para esse dia.');
    });

    it('closes the list and then offers to start the lineup', async () => {
        let lists = [openList()];
        server.use(
            // The first answer that matches wins, so this one comes before the defaults.
            http.get('/api/groups/:groupId/lists', () => HttpResponse.json(lists)),
            ...groupPageAnswers(),
            http.post('/api/lists/:id/close', () => {
                lists = [openList({ status: 'Closed' })];
                return HttpResponse.json({});
            }),
        );
        renderApp(`/grupos/${groupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Fechar lista' }));

        expect(await screen.findByRole('button', { name: 'Iniciar escalação' })).toBeInTheDocument();
    });
});

describe('MembersPage', () => {
    it('shows members, pairs and the coach rotation by name', async () => {
        server.use(...groupPageAnswers());

        renderApp(`/grupos/${groupId}/membros`);

        expect(await screen.findByText('Zé e João')).toBeInTheDocument();
        expect(screen.getByText('Pedro')).toBeInTheDocument();
        expect(screen.getAllByText('Suplente')).toHaveLength(1);
    });

    it('adds a member by e-mail', async () => {
        let body: unknown;
        server.use(
            ...groupPageAnswers(),
            http.post('/api/groups/:id/members/by-email', async ({ request }) => {
                body = await request.json();
                const updated = group();
                return HttpResponse.json({
                    ...updated,
                    members: [...updated.members, { playerId: 'new', name: 'Lucas', type: 'Substitute' }],
                });
            }),
        );
        renderApp(`/grupos/${groupId}/membros`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Adicionar membro' }));
        const dialog = await screen.findByRole('dialog', { name: 'Adicionar membro' });
        await user.type(within(dialog).getByLabelText(/e-mail da pessoa/i), 'lucas@example.com');
        await user.click(within(dialog).getByText('Suplente'));
        await user.click(within(dialog).getByRole('button', { name: 'Adicionar' }));

        expect(await screen.findByText('Lucas')).toBeInTheDocument();
        expect(body).toEqual({ email: 'lucas@example.com', type: 'Substitute' });
    });

    it('explains that the person must create the profile first', async () => {
        server.use(
            ...groupPageAnswers(),
            http.post('/api/groups/:id/members/by-email', () => problem(404, 'Not Found')),
        );
        renderApp(`/grupos/${groupId}/membros`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Adicionar membro' }));
        const dialog = await screen.findByRole('dialog', { name: 'Adicionar membro' });
        await user.type(within(dialog).getByLabelText(/e-mail da pessoa/i), 'ninguem@example.com');
        await user.click(within(dialog).getByRole('button', { name: 'Adicionar' }));

        expect(await within(dialog).findByRole('alert')).toHaveTextContent('criar o perfil primeiro');
    });
});
