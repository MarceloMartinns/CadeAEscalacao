import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { Schemas } from '@/api/client';
import { group, groupId, joaoId, meAnswer, pedroId, playerId, problem, sessionFromCookie } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

type Entry = Schemas['WeeklyListEntryResponse'];
type WeeklyList = Schemas['WeeklyListResponse'];

const listId = '0199f0a0-0000-7000-8000-0000000000l1';

function entry(id: string, overrides: Partial<Entry> = {}): Entry {
    return { playerId: id, type: 'Regular', status: 'Confirmed', positions: ['Midfielder'], hasReservedSpot: true, signUpOrder: 1, ...overrides };
}

function weeklyList(overrides: Partial<WeeklyList> = {}): WeeklyList {
    return {
        id: listId,
        groupId,
        matchDate: '2026-10-20',
        status: 'Open',
        capacity: 4,
        openSpots: 2,
        playing: [entry(joaoId, { positions: ['Defender', 'Forward'] })],
        pending: [entry(playerId, { status: 'Pending', positions: [] })],
        waiting: [],
        declined: [],
        ...overrides,
    };
}

/** The list answers with `list`, and the changes answer with `afterChange`. */
function listAnswers(list: WeeklyList) {
    return [
        sessionFromCookie(),
        meAnswer('Zé'),
        http.get('/api/groups/:id', () => HttpResponse.json(group())),
        http.get('/api/groups/:groupId/lists', () => HttpResponse.json([])),
        http.get('/api/lists/:id', () => HttpResponse.json(list)),
    ];
}

describe('ListPage', () => {
    it('shows who plays, who did not answer and the open spots, by name', async () => {
        server.use(...listAnswers(weeklyList()));

        renderApp(`/listas/${listId}`);

        expect(await screen.findByRole('heading', { name: 'Confirmados (1)' })).toBeInTheDocument();
        expect(await screen.findByText('João')).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Não responderam (1)' })).toBeInTheDocument();
        expect(screen.getByText('2 de 4 vagas · 2 abertas')).toBeInTheDocument();
    });

    it('confirms a pending regular with the chosen positions', async () => {
        let body: unknown;
        server.use(
            http.post('/api/lists/:id/entries', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(
                    weeklyList({ playing: [entry(joaoId), entry(playerId, { positions: ['Goalkeeper'] })], pending: [], openSpots: 2 }),
                );
            }),
            ...listAnswers(weeklyList()),
        );
        renderApp(`/listas/${listId}`);
        const user = userEvent.setup();

        expect(await screen.findByText('Você vai jogar?')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Vou jogar' })).toBeDisabled();
        await user.click(screen.getByRole('checkbox', { name: 'Goleiro' }));
        await user.click(screen.getByRole('button', { name: 'Vou jogar' }));

        expect(await screen.findByText(/Você está confirmado/)).toBeInTheDocument();
        expect(body).toEqual({ positions: ['Goalkeeper'] });
    });

    it('lets a pending regular say they will not play', async () => {
        let withdrew = false;
        server.use(
            http.delete('/api/lists/:id/entries/me', () => {
                withdrew = true;
                return HttpResponse.json(weeklyList({ pending: [], declined: [entry(playerId, { status: 'Declined', hasReservedSpot: false })] }));
            }),
            ...listAnswers(weeklyList()),
        );
        renderApp(`/listas/${listId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Não vou' }));

        expect(await screen.findByText('Você ainda não está na lista')).toBeInTheDocument();
        expect(withdrew).toBe(true);
    });

    it('saves new positions of a confirmed player', async () => {
        let body: unknown;
        server.use(
            http.put('/api/lists/:id/entries/me', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(weeklyList({ playing: [entry(playerId, { positions: ['Midfielder', 'Forward'] })], pending: [] }));
            }),
            ...listAnswers(weeklyList({ playing: [entry(playerId)], pending: [] })),
        );
        renderApp(`/listas/${listId}`);
        const user = userEvent.setup();

        expect(await screen.findByRole('button', { name: 'Salvar posições' })).toBeDisabled();
        await user.click(screen.getByRole('checkbox', { name: 'Ataque' }));
        await user.click(screen.getByRole('button', { name: 'Salvar posições' }));

        expect(await screen.findByText('Posições salvas.')).toBeInTheDocument();
        expect(body).toEqual({ positions: ['Midfielder', 'Forward'] });
    });

    it('asks a regular to confirm before giving up the reserved spot', async () => {
        let withdrew = false;
        server.use(
            http.delete('/api/lists/:id/entries/me', () => {
                withdrew = true;
                return HttpResponse.json(weeklyList({ pending: [], declined: [entry(playerId, { status: 'Declined', hasReservedSpot: false })] }));
            }),
            ...listAnswers(weeklyList({ playing: [entry(playerId)], pending: [] })),
        );
        renderApp(`/listas/${listId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Sair da lista' }));
        const dialog = await screen.findByRole('dialog', { name: 'Sair da lista?' });
        expect(withdrew).toBe(false);
        await user.click(within(dialog).getByRole('button', { name: 'Sair da lista' }));

        expect(await screen.findByText('Você ainda não está na lista')).toBeInTheDocument();
        expect(withdrew).toBe(true);
    });

    it('tells a substitute that a full list means waiting for a spot', async () => {
        server.use(
            http.get('/api/me', () =>
                HttpResponse.json({ userId: 'u', email: 'pedro@example.com', player: { id: pedroId, name: 'Pedro' } }),
            ),
            ...listAnswers(weeklyList({ openSpots: 0 })),
        );

        renderApp(`/listas/${listId}`);

        expect(await screen.findByText('A lista está cheia')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Entrar na espera' })).toBeInTheDocument();
    });

    it('shows the place in the waiting line', async () => {
        server.use(
            http.get('/api/me', () =>
                HttpResponse.json({ userId: 'u', email: 'pedro@example.com', player: { id: pedroId, name: 'Pedro' } }),
            ),
            ...listAnswers(
                weeklyList({
                    openSpots: 0,
                    waiting: [entry('someone', { type: 'Substitute' }), entry(pedroId, { type: 'Substitute', hasReservedSpot: false })],
                }),
            ),
        );

        renderApp(`/listas/${listId}`);

        expect(await screen.findByText('Você é o 2º da espera')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Sair da espera' })).toBeInTheDocument();
    });

    it('only shows a closed list', async () => {
        server.use(...listAnswers(weeklyList({ status: 'Closed', playing: [entry(playerId)], pending: [] })));

        renderApp(`/listas/${listId}`);

        expect(await screen.findByText('A lista fechou. Você está entre os confirmados.')).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Sair da lista' })).not.toBeInTheDocument();
    });

    it('explains when the list closed in the meantime', async () => {
        server.use(
            http.post('/api/lists/:id/entries', () => problem(422, 'Unprocessable')),
            ...listAnswers(weeklyList()),
        );
        renderApp(`/listas/${listId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('checkbox', { name: 'Meio' }));
        await user.click(screen.getByRole('button', { name: 'Vou jogar' }));

        expect(await screen.findByText(/a lista pode ter sido fechada/)).toBeInTheDocument();
    });
});
