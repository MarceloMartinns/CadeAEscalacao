import { useState, type FormEvent } from 'react';
import { Alert, Anchor, Button, Group, NumberInput, SegmentedControl, Select, Stack, TextInput, Title } from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { dayOfWeekLabels } from '@/app/labels';
import { groupKeys } from './hooks';

type Format = 'society' | 'futsal' | 'other';

const formats: Record<Exclude<Format, 'other'>, { playersPerTeam: number; benchPlayersPerTeam: number }> = {
    society: { playersPerTeam: 7, benchPlayersPerTeam: 1 },
    futsal: { playersPerTeam: 5, benchPlayersPerTeam: 1 },
};

const dayOptions = Object.entries(dayOfWeekLabels).map(([value, label]) => ({ value, label }));

/** Whoever creates the group becomes its president and first regular member (ADR-017). */
export function CreateGroupPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [name, setName] = useState('');
    const [matchDay, setMatchDay] = useState<string | null>(null);
    const [format, setFormat] = useState<Format>('society');
    const [playersPerTeam, setPlayersPerTeam] = useState<number | string>(6);
    const [benchPlayersPerTeam, setBenchPlayersPerTeam] = useState<number | string>(0);

    const createGroup = useMutation({
        mutationFn: (body: Schemas['CreateGroupRequest']) => unwrap(api.POST('/api/groups', { body })),
        onSuccess: async (group) => {
            queryClient.setQueryData(groupKeys.group(group.id), group);
            await queryClient.invalidateQueries({ queryKey: groupKeys.mine });
            await navigate(`/grupos/${group.id}`, { replace: true });
        },
    });

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        const size = format === 'other'
            ? { playersPerTeam: Number(playersPerTeam), benchPlayersPerTeam: Number(benchPlayersPerTeam) }
            : formats[format];
        createGroup.mutate({ name: name.trim(), matchDay: matchDay as Schemas['DayOfWeek'], ...size });
    }

    return (
        <form onSubmit={handleSubmit}>
            <Stack>
                <Anchor component={Link} to="/grupos" size="sm">
                    ← Meus grupos
                </Anchor>
                <Title order={2}>Criar grupo</Title>
                <TextInput
                    label="Nome do grupo"
                    placeholder="Society de terça"
                    required
                    maxLength={100}
                    value={name}
                    onChange={(event) => setName(event.currentTarget.value)}
                />
                <Select
                    label="Dia do jogo"
                    placeholder="Escolha o dia"
                    required
                    data={dayOptions}
                    value={matchDay}
                    onChange={setMatchDay}
                    allowDeselect={false}
                />
                <Stack gap={4}>
                    <SegmentedControl
                        aria-label="Formato do jogo"
                        fullWidth
                        value={format}
                        onChange={(value) => setFormat(value as Format)}
                        data={[
                            { value: 'society', label: 'Society 7 + 1' },
                            { value: 'futsal', label: 'Futsal 5 + 1' },
                            { value: 'other', label: 'Outro' },
                        ]}
                    />
                </Stack>
                {format === 'other' && (
                    <Group grow>
                        <NumberInput label="Na linha" min={1} max={11} required value={playersPerTeam} onChange={setPlayersPerTeam} />
                        <NumberInput label="Reservas" min={0} max={11} required value={benchPlayersPerTeam} onChange={setBenchPlayersPerTeam} />
                    </Group>
                )}
                {createGroup.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(createGroup.error, { 400: 'Confira os campos do grupo.' })}
                    </Alert>
                )}
                <Button type="submit" loading={createGroup.isPending} disabled={!matchDay} fullWidth>
                    Criar grupo
                </Button>
            </Stack>
        </form>
    );
}
