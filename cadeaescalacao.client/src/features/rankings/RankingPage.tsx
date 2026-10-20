import { Anchor, Stack, Table, Text, Title } from '@mantine/core';
import { Link, useParams } from 'react-router';
import { formatDate } from '@/app/dates';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { useMe } from '@/features/auth/hooks';
import { useGroup } from '@/features/groups/hooks';
import { useRanking } from './hooks';

/** The season ranking (points 3/1/0 and attendances), computed by the API. */
export function RankingPage() {
    const { seasonId = '' } = useParams();
    const ranking = useRanking(seasonId);
    const group = useGroup(ranking.data?.groupId ?? '');
    const me = useMe();

    if (ranking.isPending) {
        return <PageLoader />;
    }

    if (ranking.isError) {
        return <ErrorMessage error={ranking.error} onRetry={() => void ranking.refetch()} />;
    }

    const { data } = ranking;

    return (
        <Stack>
            <Anchor component={Link} to={`/grupos/${data.groupId}`} size="sm">
                ← {group.data?.name ?? 'Grupo'}
            </Anchor>
            <Stack gap={2}>
                <Title order={2}>Ranking {data.name}</Title>
                <Text size="sm" c="dimmed">
                    {formatDate(data.start)} a {formatDate(data.end)}
                </Text>
            </Stack>

            {data.entries.length === 0 ? (
                <Text c="dimmed">Nenhum jogo nesta temporada ainda.</Text>
            ) : (
                <Table striped highlightOnHover verticalSpacing={6} horizontalSpacing={6} fz="sm">
                    <Table.Thead>
                        <Table.Tr>
                            <Table.Th>#</Table.Th>
                            <Table.Th>Jogador</Table.Th>
                            <Table.Th ta="right" title="Pontos">Pts</Table.Th>
                            <Table.Th ta="right" title="Jogos">J</Table.Th>
                            <Table.Th ta="right" title="Vitórias">V</Table.Th>
                            <Table.Th ta="right" title="Empates">E</Table.Th>
                            <Table.Th ta="right" title="Derrotas">D</Table.Th>
                        </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                        {data.entries.map((entry) => {
                            const isMe = entry.playerId === me.data?.player?.id;
                            return (
                                <Table.Tr
                                    key={entry.playerId}
                                    bg={isMe ? 'var(--mantine-color-green-light)' : undefined}
                                    aria-current={isMe ? 'true' : undefined}
                                    fw={isMe ? 700 : undefined}
                                >
                                    <Table.Td>{entry.position}</Table.Td>
                                    <Table.Td>{entry.playerName || 'Jogador'}</Table.Td>
                                    <Table.Td ta="right" fw={700}>
                                        {entry.points}
                                    </Table.Td>
                                    <Table.Td ta="right">{entry.attendances}</Table.Td>
                                    <Table.Td ta="right">{entry.wins}</Table.Td>
                                    <Table.Td ta="right">{entry.draws}</Table.Td>
                                    <Table.Td ta="right">{entry.losses}</Table.Td>
                                </Table.Tr>
                            );
                        })}
                    </Table.Tbody>
                </Table>
            )}
            <Text size="xs" c="dimmed">
                Vitória vale 3 pontos, empate 1 e derrota 0. J = jogos na temporada.
            </Text>
        </Stack>
    );
}
