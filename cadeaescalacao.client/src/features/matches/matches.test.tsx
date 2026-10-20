import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { Schemas } from '@/api/client';
import { group, groupId, joaoId, pedroId, playerId, problem, sessionFromCookie } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

const lineupId = '0199f0a0-0000-7000-8000-0000000000u1';
const listId = '0199f0a0-0000-7000-8000-0000000000l1';
const matchId = '0199f0a0-0000-7000-8000-0000000000m1';
const seasonId = '0199f0a0-0000-7000-8000-0000000000s1';

// Completed lineup: Zé (coach) and Pedro on team A, João (partner) on team B.
const completed: Schemas['LineupResponse'] = {
    id: lineupId,
    groupId,
    weeklyListId: listId,
    status: 'Completed',
    coachId: playerId,
    partnerId: joaoId,
    players: [playerId, joaoId, pedroId],
    teamA: [pedroId, playerId],
    teamB: [joaoId],
};

const match: Schemas['MatchResponse'] = {
    id: matchId,
    groupId,
    lineupId,
    date: '2026-10-20',
    goalsA: 3,
    goalsB: 1,
    teamA: completed.teamA,
    teamB: completed.teamB,
};

function signedInAs(player: { id: string; name: string }) {
    return http.get('/api/me', () => HttpResponse.json({ userId: 'u', email: 'x@example.com', player }));
}

function lineupAnswers(matchIdOfList: string | null = null) {
    return [
        sessionFromCookie(),
        http.get('/api/groups/:id', () => HttpResponse.json(group())),
        http.get('/api/groups/:groupId/lists', () =>
            HttpResponse.json([
                { id: listId, matchDate: '2026-10-20', status: 'Closed', lineupId, lineupStatus: 'Completed', matchId: matchIdOfList },
            ]),
        ),
        http.get('/api/lineups/:id', () => HttpResponse.json(completed)),
        http.get('/api/lists/:id', () =>
            HttpResponse.json({
                id: listId, groupId, matchDate: '2026-10-20', status: 'Closed', capacity: 4, openSpots: 1,
                playing: [], pending: [], waiting: [], declined: [],
            }),
        ),
        http.get('/api/matches/:id', () => HttpResponse.json(match)),
    ];
}

describe('RecordResult', () => {
    it('lets the coach record the score and shows the match', async () => {
        let body: unknown;
        server.use(
            signedInAs({ id: playerId, name: 'Zé' }),
            http.post('/api/lineups/:lineupId/match', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(match, { status: 201 });
            }),
            ...lineupAnswers(),
        );
        const { router } = renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        const goalsA = await screen.findByLabelText(/Gols do Time A/);
        await user.clear(goalsA);
        await user.type(goalsA, '3');
        const goalsB = screen.getByLabelText(/Gols do Time B/);
        await user.clear(goalsB);
        await user.type(goalsB, '1');
        await user.click(screen.getByRole('button', { name: 'Registrar resultado' }));

        expect(await screen.findByText('Você venceu! +3 pontos')).toBeInTheDocument();
        expect(router.state.location.pathname).toBe(`/partidas/${matchId}`);
        expect(body).toEqual({ goalsA: 3, goalsB: 1 });
    });

    it('does not show the form to the other members', async () => {
        server.use(signedInAs({ id: pedroId, name: 'Pedro' }), ...lineupAnswers());

        renderApp(`/escalacoes/${lineupId}`);

        expect(await screen.findByText(/Aguardando o professor ou o presidente/)).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Registrar resultado' })).not.toBeInTheDocument();
    });

    it('explains when the result was already recorded', async () => {
        server.use(
            signedInAs({ id: playerId, name: 'Zé' }),
            http.post('/api/lineups/:lineupId/match', () => problem(409, 'Conflict')),
            ...lineupAnswers(),
        );
        renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Registrar resultado' }));

        expect(await screen.findByText(/já foi registrado/)).toBeInTheDocument();
    });

    it('links to the match once it exists', async () => {
        server.use(signedInAs({ id: playerId, name: 'Zé' }), ...lineupAnswers(matchId));

        renderApp(`/escalacoes/${lineupId}`);

        expect(await screen.findByRole('link', { name: 'Ver resultado' })).toHaveAttribute('href', `/partidas/${matchId}`);
        expect(screen.queryByRole('button', { name: 'Registrar resultado' })).not.toBeInTheDocument();
    });
});

describe('MatchPage', () => {
    it('shows the score, the teams and the result for the player', async () => {
        server.use(signedInAs({ id: joaoId, name: 'João' }), ...lineupAnswers(matchId));

        renderApp(`/partidas/${matchId}`);

        expect(await screen.findByLabelText('Time A 3 x 1 Time B')).toBeInTheDocument();
        expect(await within(screen.getByRole('region', { name: 'Time B' })).findByText('João')).toBeInTheDocument();
        expect(screen.getByText(/Derrota: 0 pontos/)).toBeInTheDocument();
    });
});

describe('RankingPage', () => {
    it('shows the ranking with names and highlights the signed-in player', async () => {
        server.use(
            signedInAs({ id: pedroId, name: 'Pedro' }),
            ...lineupAnswers(),
            http.get('/api/seasons/:seasonId/ranking', () =>
                HttpResponse.json({
                    seasonId,
                    groupId,
                    name: '2026/2',
                    start: '2026-07-01',
                    end: '2026-12-31',
                    entries: [
                        { position: 1, playerId: playerId, playerName: 'Zé', points: 3, attendances: 1, wins: 1, draws: 0, losses: 0 },
                        { position: 1, playerId: pedroId, playerName: 'Pedro', points: 3, attendances: 1, wins: 1, draws: 0, losses: 0 },
                        // Left the group: not a member anymore, but still in the ranking.
                        { position: 3, playerId: 'former', playerName: 'Lucas', points: 0, attendances: 1, wins: 0, draws: 0, losses: 1 },
                    ],
                }),
            ),
        );

        renderApp(`/temporadas/${seasonId}/ranking`);

        expect(await screen.findByRole('heading', { name: 'Ranking 2026/2' })).toBeInTheDocument();
        expect(screen.getByText('01/07/2026 a 31/12/2026')).toBeInTheDocument();
        expect(screen.getByText('Lucas')).toBeInTheDocument();
        const rows = screen.getAllByRole('row');
        const mine = rows.find((row) => row.getAttribute('aria-current') === 'true')!;
        expect(within(mine).getByText('Pedro')).toBeInTheDocument();
        expect(await screen.findByRole('link', { name: '← Terça' })).toHaveAttribute('href', `/grupos/${groupId}`);
    });
});
