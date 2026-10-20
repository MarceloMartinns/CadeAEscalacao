import { useState, type FormEvent } from 'react';
import { Alert, Button, Card, Group, NumberInput, Stack, Text } from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { groupKeys, useGroupLists } from '@/features/groups/hooks';

/**
 * Under the final teams: the coach of the week or the president records the score (ADR-016).
 * The group's lists tell whether the match already exists (ADR-021).
 */
export function RecordResult({ lineup, canRecord }: { lineup: Schemas['LineupResponse']; canRecord: boolean }) {
    const lists = useGroupLists(lineup.groupId);
    const matchId = lists.data?.find((list) => list.lineupId === lineup.id)?.matchId;

    if (lists.isPending) {
        return null;
    }

    if (matchId) {
        return (
            <Button component={Link} to={`/partidas/${matchId}`} variant="light">
                Ver resultado
            </Button>
        );
    }

    if (!canRecord) {
        return (
            <Text size="sm" c="dimmed">
                Aguardando o professor ou o presidente registrar o resultado.
            </Text>
        );
    }

    return <ScoreForm lineup={lineup} />;
}

function ScoreForm({ lineup }: { lineup: Schemas['LineupResponse'] }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [goalsA, setGoalsA] = useState<number | string>(0);
    const [goalsB, setGoalsB] = useState<number | string>(0);

    const record = useMutation({
        mutationFn: (body: Schemas['ScoreRequest']) =>
            unwrap(api.POST('/api/lineups/{lineupId}/match', { params: { path: { lineupId: lineup.id } }, body })),
        onSuccess: async (match) => {
            await Promise.all([
                queryClient.invalidateQueries({ queryKey: groupKeys.lists(lineup.groupId) }),
                queryClient.invalidateQueries({ queryKey: ['ranking'] }),
            ]);
            await navigate(`/partidas/${match.id}`);
        },
    });

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        record.mutate({ goalsA: Number(goalsA), goalsB: Number(goalsB) });
    }

    return (
        <Card withBorder component="form" onSubmit={handleSubmit}>
            <Stack>
                <Text fw={600}>Resultado</Text>
                <Group grow align="flex-end">
                    <NumberInput label="Gols do Time A" min={0} max={99} required value={goalsA} onChange={setGoalsA} />
                    <NumberInput label="Gols do Time B" min={0} max={99} required value={goalsB} onChange={setGoalsB} />
                </Group>
                {record.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(record.error, {
                            409: 'O resultado deste jogo já foi registrado. Recarregue a página.',
                            403: 'Só o professor da vez ou o presidente registram o resultado.',
                        })}
                    </Alert>
                )}
                <Button type="submit" loading={record.isPending}>
                    Registrar resultado
                </Button>
            </Stack>
        </Card>
    );
}
