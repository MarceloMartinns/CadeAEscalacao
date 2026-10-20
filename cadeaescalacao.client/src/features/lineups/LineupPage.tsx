import { useState } from 'react';
import { Alert, Anchor, Badge, Button, Card, Group, Modal, SegmentedControl, Select, SimpleGrid, Stack, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Link, useParams } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { formatMatchDate, lineupStatusLabels, positionLabels, positions, type Position } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { useMe } from '@/features/auth/hooks';
import { useGroup, useIsPresident, usePlayerName } from '@/features/groups/hooks';
import { useWeeklyList } from '@/features/lists/hooks';
import { RecordResult } from '@/features/matches/RecordResult';
import { useLineupMutation, useLineup } from './hooks';

type Lineup = Schemas['LineupResponse'];
type Team = Schemas['Team'];

const lineupErrors = {
    403: 'Só quem tem esse papel na escalação pode fazer isso.',
    409: 'A escalação mudou ao mesmo tempo. Recarregue a página.',
    422: 'Não foi possível nesta etapa da escalação. Recarregue a página.',
};

/** Who does what in this lineup (ADR-016) and the helpers every part of the screen needs. */
interface LineupContext {
    lineup: Lineup;
    nameOf: (playerId: string) => string;
    positionsOf: (playerId: string) => Position[];
    isPresident: boolean;
    isCoach: boolean;
    isPartner: boolean;
}

/** The lineup: the president picks the partner of the day, the coach splits the teams, the partner chooses one. */
export function LineupPage() {
    const { lineupId = '' } = useParams();
    const lineup = useLineup(lineupId);

    if (lineup.isPending) {
        return <PageLoader />;
    }

    if (lineup.isError) {
        return <ErrorMessage error={lineup.error} onRetry={() => void lineup.refetch()} />;
    }

    return <LineupView lineup={lineup.data} />;
}

function LineupView({ lineup }: { lineup: Lineup }) {
    const me = useMe();
    const group = useGroup(lineup.groupId);
    const list = useWeeklyList(lineup.weeklyListId);
    const nameOf = usePlayerName(lineup.groupId);
    const isPresident = useIsPresident(group.data);
    const myId = me.data?.player?.id;

    const context: LineupContext = {
        lineup,
        nameOf,
        positionsOf: (playerId) =>
            (list.data?.playing.find((entry) => entry.playerId === playerId)?.positions ?? []) as Position[],
        isPresident,
        isCoach: myId === lineup.coachId,
        isPartner: !!myId && myId === lineup.partnerId,
    };

    return (
        <Stack>
            <Anchor component={Link} to={`/grupos/${lineup.groupId}`} size="sm">
                ← {group.data?.name ?? 'Grupo'}
            </Anchor>
            <Group justify="space-between" align="flex-start">
                <Stack gap={2}>
                    <Title order={2}>Escalação</Title>
                    {list.data && (
                        <Text size="sm" c="dimmed" tt="capitalize">
                            {formatMatchDate(list.data.matchDate)}
                        </Text>
                    )}
                </Stack>
                <Badge variant="light" color={lineup.status === 'Completed' ? 'green' : 'yellow'}>
                    {lineupStatusLabels[lineup.status!]}
                </Badge>
            </Group>
            <Text size="sm">
                Professor: <strong>{nameOf(lineup.coachId)}</strong> · Dupla:{' '}
                <strong>{lineup.partnerId ? nameOf(lineup.partnerId) : 'a definir'}</strong>
            </Text>
            <LineupStep context={context} />
        </Stack>
    );
}

function LineupStep({ context }: { context: LineupContext }) {
    const { lineup, nameOf, isPresident, isCoach, isPartner } = context;
    const [redoing, setRedoing] = useState(false);

    switch (lineup.status) {
        case 'WaitingForPartner':
            return isPresident ? (
                <PartnerOfTheDay context={context} />
            ) : (
                <Alert color="yellow">
                    A dupla de {nameOf(lineup.coachId)} não joga hoje. O presidente vai escolher a dupla do dia.
                </Alert>
            );

        case 'Drafting':
            return isCoach ? (
                <TeamBuilder context={context} />
            ) : (
                <Alert color="yellow">{nameOf(lineup.coachId)} está montando os times.</Alert>
            );

        case 'WaitingForPartnerChoice':
            if (isCoach && redoing) {
                return <TeamBuilder context={context} onDone={() => setRedoing(false)} />;
            }

            return (
                <Stack>
                    <Teams context={context} />
                    {isPartner && <ChooseTeam context={context} />}
                    {!isPartner && (
                        <Alert color="yellow">{nameOf(lineup.partnerId!)} vai escolher o time. O professor fica com o outro.</Alert>
                    )}
                    {isCoach && (
                        <Button variant="default" onClick={() => setRedoing(true)}>
                            Refazer times
                        </Button>
                    )}
                </Stack>
            );

        default:
            return (
                <Stack>
                    <Teams context={context} />
                    <RecordResult lineup={lineup} canRecord={isCoach || isPresident} />
                </Stack>
            );
    }
}

function PartnerOfTheDay({ context }: { context: LineupContext }) {
    const { lineup, nameOf } = context;
    const [partnerId, setPartnerId] = useState<string | null>(null);
    const setPartner = useLineupMutation(lineup.id, (playerId: string) =>
        unwrap(api.POST('/api/lineups/{id}/partner', { params: { path: { id: lineup.id } }, body: { playerId } })),
    );
    const options = lineup.players
        .filter((playerId) => playerId !== lineup.coachId)
        .map((playerId) => ({ value: playerId, label: nameOf(playerId) }))
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'));

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>Escolha a dupla do dia</Text>
                <Text size="sm" c="dimmed">
                    A dupla de {nameOf(lineup.coachId)} não joga hoje. Quem você escolher fica com o time que preferir, e o
                    professor vai para o outro.
                </Text>
                <Select label="Dupla do dia" data={options} value={partnerId} onChange={setPartnerId} searchable />
                {setPartner.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(setPartner.error, lineupErrors)}
                    </Alert>
                )}
                <Button disabled={!partnerId} loading={setPartner.isPending} onClick={() => partnerId && setPartner.mutate(partnerId)}>
                    Confirmar dupla
                </Button>
            </Stack>
        </Card>
    );
}

type Filter = 'all' | Position;

/** The coach splits the players into A and B, with a filter by position to balance the teams. */
function TeamBuilder({ context, onDone }: { context: LineupContext; onDone?: () => void }) {
    const { lineup, nameOf, positionsOf } = context;
    const toAssign = lineup.players.filter((playerId) => playerId !== lineup.coachId && playerId !== lineup.partnerId);
    const [teams, setTeams] = useState<Record<string, Team>>(() =>
        Object.fromEntries([
            ...lineup.teamA.map((playerId) => [playerId, 'A'] as const),
            ...lineup.teamB.map((playerId) => [playerId, 'B'] as const),
        ]),
    );
    const [filter, setFilter] = useState<Filter>('all');
    const assignTeams = useLineupMutation(lineup.id, (body: Schemas['AssignTeamsRequest']) =>
        unwrap(api.PUT('/api/lineups/{id}/teams', { params: { path: { id: lineup.id } }, body })),
    );

    const teamA = toAssign.filter((playerId) => teams[playerId] === 'A');
    const teamB = toAssign.filter((playerId) => teams[playerId] === 'B');
    const unassigned = toAssign.length - teamA.length - teamB.length;
    const balanced = Math.abs(teamA.length - teamB.length) <= 1;
    const shown = toAssign
        .filter((playerId) => filter === 'all' || positionsOf(playerId).includes(filter))
        .sort((a, b) => nameOf(a).localeCompare(nameOf(b), 'pt-BR'));
    const countIn = (team: string[], position: Position) => team.filter((playerId) => positionsOf(playerId).includes(position)).length;

    function publish() {
        assignTeams.mutate(
            { teamA, teamB },
            {
                onSuccess: () => {
                    notifications.show({ color: 'green', message: 'Escalação publicada. Agora a dupla escolhe o time.' });
                    onDone?.();
                },
            },
        );
    }

    return (
        <Stack>
            <Text size="sm" c="dimmed">
                Divida os jogadores entre os times A e B. Você e a sua dupla ficam de fora: depois, a dupla escolhe um time
                e você vai para o outro.
            </Text>

            <Card withBorder padding="sm">
                <Group justify="space-between">
                    <Text fw={600}>Time A: {teamA.length}</Text>
                    <Text fw={600}>Time B: {teamB.length}</Text>
                </Group>
                {positions.map((position) => (
                    <Group key={position} justify="space-between">
                        <Text size="xs" c="dimmed">
                            {positionLabels[position]}: {countIn(teamA, position)}
                        </Text>
                        <Text size="xs" c="dimmed">
                            {positionLabels[position]}: {countIn(teamB, position)}
                        </Text>
                    </Group>
                ))}
            </Card>

            <SegmentedControl
                aria-label="Filtrar por posição"
                fullWidth
                size="xs"
                value={filter}
                onChange={(value) => setFilter(value as Filter)}
                data={[{ value: 'all', label: 'Todos' }, ...positions.map((position) => ({ value: position, label: positionLabels[position] }))]}
            />

            <Card withBorder padding="xs">
                {shown.length === 0 && (
                    <Text size="sm" c="dimmed" p="xs">
                        Ninguém nesta posição.
                    </Text>
                )}
                {shown.map((playerId) => (
                    <Group key={playerId} justify="space-between" py={6} wrap="nowrap">
                        <Stack gap={0} style={{ minWidth: 0 }}>
                            <Text truncate>{nameOf(playerId)}</Text>
                            <Text size="xs" c="dimmed">
                                {positionsOf(playerId).map((position) => positionLabels[position]).join(', ')}
                            </Text>
                        </Stack>
                        <SegmentedControl
                            aria-label={`Time de ${nameOf(playerId)}`}
                            size="xs"
                            value={teams[playerId] ?? ''}
                            onChange={(team) => setTeams((current) => ({ ...current, [playerId]: team as Team }))}
                            data={['A', 'B']}
                        />
                    </Group>
                ))}
            </Card>

            {(unassigned > 0 || !balanced) && (
                <Text size="sm" c="dimmed">
                    {unassigned > 0
                        ? `Falta escolher o time de ${unassigned} ${unassigned === 1 ? 'jogador' : 'jogadores'}.`
                        : 'Os times precisam ter o mesmo tamanho (no máximo um a mais).'}
                </Text>
            )}
            {assignTeams.isError && (
                <Alert color="red" role="alert">
                    {errorMessage(assignTeams.error, lineupErrors)}
                </Alert>
            )}
            <Group grow>
                {onDone && (
                    <Button variant="default" onClick={onDone}>
                        Voltar
                    </Button>
                )}
                <Button disabled={unassigned > 0 || !balanced} loading={assignTeams.isPending} onClick={publish}>
                    Publicar escalação
                </Button>
            </Group>
        </Stack>
    );
}

function ChooseTeam({ context }: { context: LineupContext }) {
    const { lineup, nameOf } = context;
    const [team, setTeam] = useState<Team | null>(null);
    const choose = useLineupMutation(lineup.id, (chosen: Team) =>
        unwrap(api.POST('/api/lineups/{id}/choice', { params: { path: { id: lineup.id } }, body: { team: chosen } })),
    );
    const other = team === 'A' ? 'B' : 'A';

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>Você é a dupla: escolha o seu time</Text>
                <Group grow>
                    <Button onClick={() => setTeam('A')}>Ficar com o Time A</Button>
                    <Button onClick={() => setTeam('B')}>Ficar com o Time B</Button>
                </Group>
            </Stack>
            <Modal opened={team !== null} onClose={() => setTeam(null)} title={`Ficar com o Time ${team}?`} centered>
                <Stack>
                    <Text size="sm">
                        {nameOf(lineup.coachId)} vai para o Time {other}. Depois disso a escalação não muda mais.
                    </Text>
                    {choose.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(choose.error, lineupErrors)}
                        </Alert>
                    )}
                    <Group justify="flex-end">
                        <Button variant="default" onClick={() => setTeam(null)}>
                            Voltar
                        </Button>
                        <Button loading={choose.isPending} onClick={() => team && choose.mutate(team, { onSuccess: () => setTeam(null) })}>
                            Confirmar
                        </Button>
                    </Group>
                </Stack>
            </Modal>
        </Card>
    );
}

function Teams({ context }: { context: LineupContext }) {
    const { lineup } = context;
    return (
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
            <TeamCard name="Time A" players={lineup.teamA} context={context} />
            <TeamCard name="Time B" players={lineup.teamB} context={context} />
        </SimpleGrid>
    );
}

function TeamCard({ name, players, context }: { name: string; players: string[]; context: LineupContext }) {
    const { lineup, nameOf, positionsOf } = context;
    return (
        <Card withBorder padding="sm" component="section" aria-label={name}>
            <Title order={3} size="h4" mb="xs">
                {name}
            </Title>
            {players.map((playerId) => (
                <Group key={playerId} justify="space-between" py={4} wrap="nowrap">
                    <Text size="sm" truncate>
                        {nameOf(playerId)}
                    </Text>
                    <Group gap={4} wrap="nowrap">
                        {playerId === lineup.coachId && <Badge size="xs">Professor</Badge>}
                        {playerId === lineup.partnerId && <Badge size="xs" color="grape">Dupla</Badge>}
                        {positionsOf(playerId).map((position) => (
                            <Badge key={position} size="xs" variant="light" color="gray">
                                {positionLabels[position]}
                            </Badge>
                        ))}
                    </Group>
                </Group>
            ))}
        </Card>
    );
}
