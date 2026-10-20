import { Alert, Anchor, Card, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core';
import { Link, useParams } from 'react-router';
import type { Schemas } from '@/api/client';
import { formatMatchDate } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { useMe } from '@/features/auth/hooks';
import { useGroup, usePlayerName } from '@/features/groups/hooks';
import { useMatch } from './hooks';

type Match = Schemas['MatchResponse'];

/** The score of a match, the teams and how it went for the signed-in player (3/1/0 points). */
export function MatchPage() {
    const { matchId = '' } = useParams();
    const match = useMatch(matchId);

    if (match.isPending) {
        return <PageLoader />;
    }

    if (match.isError) {
        return <ErrorMessage error={match.error} onRetry={() => void match.refetch()} />;
    }

    return <MatchView match={match.data} />;
}

function MatchView({ match }: { match: Match }) {
    const group = useGroup(match.groupId);
    const nameOf = usePlayerName(match.groupId);

    return (
        <Stack>
            <Anchor component={Link} to={`/grupos/${match.groupId}`} size="sm">
                ← {group.data?.name ?? 'Grupo'}
            </Anchor>
            <Text c="dimmed" tt="capitalize">
                {formatMatchDate(match.date)}
            </Text>
            <Card withBorder>
                <Group justify="center" gap="xl" aria-label={`Time A ${match.goalsA} x ${match.goalsB} Time B`}>
                    <Stack gap={0} align="center">
                        <Text size="sm" c="dimmed">
                            Time A
                        </Text>
                        <Title order={2} fz={48}>
                            {match.goalsA}
                        </Title>
                    </Stack>
                    <Text fz={32} c="dimmed">
                        x
                    </Text>
                    <Stack gap={0} align="center">
                        <Text size="sm" c="dimmed">
                            Time B
                        </Text>
                        <Title order={2} fz={48}>
                            {match.goalsB}
                        </Title>
                    </Stack>
                </Group>
            </Card>
            <MyResult match={match} />
            <SimpleGrid cols={{ base: 1, xs: 2 }}>
                {(['A', 'B'] as const).map((team) => (
                    <Card key={team} withBorder padding="sm" component="section" aria-label={`Time ${team}`}>
                        <Title order={3} size="h4" mb="xs">
                            Time {team}
                        </Title>
                        {(team === 'A' ? match.teamA : match.teamB).map((playerId) => (
                            <Text key={playerId} size="sm" py={2}>
                                {nameOf(playerId)}
                            </Text>
                        ))}
                    </Card>
                ))}
            </SimpleGrid>
            <Anchor component={Link} to={`/escalacoes/${match.lineupId}`} size="sm">
                Ver a escalação
            </Anchor>
        </Stack>
    );
}

function MyResult({ match }: { match: Match }) {
    const me = useMe();
    const playerId = me.data?.player?.id;
    const team = match.teamA.includes(playerId ?? '') ? 'A' : match.teamB.includes(playerId ?? '') ? 'B' : null;

    if (!team) {
        return null;
    }

    const mine = team === 'A' ? match.goalsA : match.goalsB;
    const theirs = team === 'A' ? match.goalsB : match.goalsA;

    if (mine > theirs) {
        return <Alert color="green">Você venceu! +3 pontos</Alert>;
    }

    if (mine === theirs) {
        return <Alert color="yellow">Empate: +1 ponto</Alert>;
    }

    return <Alert color="gray">Derrota: 0 pontos. Semana que vem tem mais!</Alert>;
}
