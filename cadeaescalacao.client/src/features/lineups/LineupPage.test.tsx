import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { Schemas } from '@/api/client';
import { group, groupId, joaoId, pedroId, playerId, sessionFromCookie } from '@/test/fakeApi';
import { renderApp } from '@/test/renderApp';
import { server } from '@/test/server';

type Lineup = Schemas['LineupResponse'];

const lineupId = '0199f0a0-0000-7000-8000-0000000000u1';
const listId = '0199f0a0-0000-7000-8000-0000000000l1';
const lucasId = '0199f0a0-0000-7000-8000-000000000004';
const anaId = '0199f0a0-0000-7000-8000-000000000005';

// Zé (signed in) is the president and the coach; João is his pair.
const members: Schemas['GroupMemberResponse'][] = [
    ...group().members,
    { playerId: lucasId, name: 'Lucas', type: 'Substitute' },
    { playerId: anaId, name: 'Ana', type: 'Substitute' },
];

function lineup(overrides: Partial<Lineup> = {}): Lineup {
    return {
        id: lineupId,
        groupId,
        weeklyListId: listId,
        status: 'Drafting',
        coachId: playerId,
        partnerId: joaoId,
        players: [playerId, joaoId, pedroId, lucasId, anaId],
        teamA: [],
        teamB: [],
        ...overrides,
    };
}

const positionsById: Record<string, string[]> = {
    [playerId]: ['Midfielder'],
    [joaoId]: ['Defender'],
    [pedroId]: ['Goalkeeper'],
    [lucasId]: ['Forward'],
    [anaId]: ['Defender', 'Midfielder'],
};

function lineupAnswers(current: Lineup, signedInAs: { id: string; name: string } = { id: playerId, name: 'Zé' }) {
    return [
        sessionFromCookie(),
        http.get('/api/me', () => HttpResponse.json({ userId: 'u', email: 'x@example.com', player: signedInAs })),
        http.get('/api/groups/:id', () => HttpResponse.json(group({ members }))),
        http.get('/api/groups/:groupId/lists', () => HttpResponse.json([])),
        http.get('/api/lineups/:id', () => HttpResponse.json(current)),
        http.get('/api/lists/:id', () =>
            HttpResponse.json({
                id: listId,
                groupId,
                matchDate: '2026-10-20',
                status: 'Closed',
                capacity: 6,
                openSpots: 1,
                playing: current.players.map((id, index) => ({
                    playerId: id,
                    type: 'Regular',
                    status: 'Confirmed',
                    positions: positionsById[id],
                    hasReservedSpot: false,
                    signUpOrder: index,
                })),
                pending: [],
                waiting: [],
                declined: [],
            }),
        ),
    ];
}

async function chooseTeam(user: ReturnType<typeof userEvent.setup>, name: string, team: 'A' | 'B') {
    const control = await screen.findByRole('radiogroup', { name: `Time de ${name}` });
    await user.click(within(control).getByText(team));
}

describe('LineupPage', () => {
    it('lets the coach split the players, filtering by position, and publish', async () => {
        let body: unknown;
        server.use(
            http.put('/api/lineups/:id/teams', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(lineup({ status: 'WaitingForPartnerChoice', teamA: [pedroId, anaId], teamB: [lucasId] }));
            }),
            ...lineupAnswers(lineup()),
        );
        renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        // Only the goalkeepers.
        await user.click(await screen.findByRole('radio', { name: 'Goleiro' }));
        expect(screen.getByRole('radiogroup', { name: 'Time de Pedro' })).toBeInTheDocument();
        expect(screen.queryByRole('radiogroup', { name: 'Time de Lucas' })).not.toBeInTheDocument();
        await user.click(screen.getByRole('radio', { name: 'Todos' }));

        await chooseTeam(user, 'Pedro', 'A');
        await chooseTeam(user, 'Lucas', 'B');
        expect(screen.getByRole('button', { name: 'Publicar escalação' })).toBeDisabled();
        expect(screen.getByText('Falta escolher o time de 1 jogador.')).toBeInTheDocument();
        await chooseTeam(user, 'Ana', 'A');
        await user.click(screen.getByRole('button', { name: 'Publicar escalação' }));

        expect(await screen.findByText(/João vai escolher o time/)).toBeInTheDocument();
        expect(body).toEqual({ teamA: [pedroId, anaId], teamB: [lucasId] });
    });

    it('does not let the coach publish unbalanced teams', async () => {
        server.use(...lineupAnswers(lineup()));
        renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        await chooseTeam(user, 'Pedro', 'A');
        await chooseTeam(user, 'Lucas', 'A');
        await chooseTeam(user, 'Ana', 'A');

        expect(screen.getByText('Os times precisam ter o mesmo tamanho (no máximo um a mais).')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Publicar escalação' })).toBeDisabled();
    });

    it('shows the other members that the coach is building the teams', async () => {
        server.use(...lineupAnswers(lineup(), { id: pedroId, name: 'Pedro' }));

        renderApp(`/escalacoes/${lineupId}`);

        expect(await screen.findByText('Zé está montando os times.')).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Publicar escalação' })).not.toBeInTheDocument();
    });

    it('lets the president pick the partner of the day', async () => {
        let body: unknown;
        server.use(
            http.post('/api/lineups/:id/partner', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json(lineup({ coachId: joaoId, partnerId: lucasId }));
            }),
            ...lineupAnswers(lineup({ status: 'WaitingForPartner', partnerId: null, coachId: joaoId }), { id: playerId, name: 'Zé' }),
        );
        renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('combobox', { name: 'Dupla do dia' }));
        await user.click(await screen.findByRole('option', { name: 'Lucas' }));
        await user.click(screen.getByRole('button', { name: 'Confirmar dupla' }));

        expect(await screen.findByText('João está montando os times.')).toBeInTheDocument();
        expect(body).toEqual({ playerId: lucasId });
    });

    it('lets the partner choose a team', async () => {
        let body: unknown;
        const published = lineup({ status: 'WaitingForPartnerChoice', teamA: [pedroId, anaId], teamB: [lucasId] });
        server.use(
            http.post('/api/lineups/:id/choice', async ({ request }) => {
                body = await request.json();
                return HttpResponse.json({ ...published, status: 'Completed', teamA: [pedroId, anaId, playerId], teamB: [lucasId, joaoId] });
            }),
            ...lineupAnswers(published, { id: joaoId, name: 'João' }),
        );
        renderApp(`/escalacoes/${lineupId}`);
        const user = userEvent.setup();

        await user.click(await screen.findByRole('button', { name: 'Ficar com o Time B' }));
        const dialog = await screen.findByRole('dialog', { name: 'Ficar com o Time B?' });
        expect(within(dialog).getByText(/Zé vai para o Time A/)).toBeInTheDocument();
        await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }));

        const teamB = await screen.findByRole('region', { name: 'Time B' });
        expect(within(teamB).getByText('Dupla')).toBeInTheDocument();
        expect(body).toEqual({ team: 'B' });
    });

    it('shows the final teams with coach and partner', async () => {
        server.use(
            ...lineupAnswers(lineup({ status: 'Completed', teamA: [pedroId, anaId, playerId], teamB: [lucasId, joaoId] }), {
                id: pedroId,
                name: 'Pedro',
            }),
        );

        renderApp(`/escalacoes/${lineupId}`);

        const teamA = await screen.findByRole('region', { name: 'Time A' });
        expect(await within(teamA).findByText('Pedro')).toBeInTheDocument();
        expect(within(teamA).getByText('Professor')).toBeInTheDocument();
        expect(await within(teamA).findByText('Goleiro')).toBeInTheDocument();
    });
});
