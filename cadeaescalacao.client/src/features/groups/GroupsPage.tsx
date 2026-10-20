import { Badge, Button, Card, Group, Stack, Text, Title } from '@mantine/core';
import { Link } from 'react-router';
import { dayOfWeekLabels } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { useMe } from '@/features/auth/hooks';
import { useMyGroups } from './hooks';

/** The groups of the signed-in player (GET /api/groups). */
export function GroupsPage() {
    const me = useMe();
    const groups = useMyGroups();

    return (
        <Stack>
            <Group justify="space-between">
                <Title order={2}>Meus grupos</Title>
                <Button component={Link} to="/grupos/novo" variant="light" size="sm">
                    Criar grupo
                </Button>
            </Group>

            {groups.isPending && <PageLoader />}
            {groups.isError && <ErrorMessage error={groups.error} onRetry={() => void groups.refetch()} />}

            {groups.data?.length === 0 && (
                <Card withBorder>
                    <Text>Você ainda não está em nenhum grupo.</Text>
                    <Text c="dimmed" size="sm" mt="xs">
                        Peça ao presidente do grupo para te adicionar com o e-mail <strong>{me.data?.email}</strong>, ou
                        crie um grupo novo.
                    </Text>
                </Card>
            )}

            {groups.data?.map((group) => (
                <Card key={group.id} withBorder component={Link} to={`/grupos/${group.id}`}>
                    <Group justify="space-between" wrap="nowrap">
                        <Stack gap={2}>
                            <Text fw={600}>{group.name}</Text>
                            <Text size="sm" c="dimmed">
                                {dayOfWeekLabels[group.matchDay]} · {group.playersPerTeam} + {group.benchPlayersPerTeam}
                            </Text>
                        </Stack>
                        {group.presidentId === me.data?.player?.id && <Badge variant="light">Presidente</Badge>}
                    </Group>
                </Card>
            ))}
        </Stack>
    );
}
