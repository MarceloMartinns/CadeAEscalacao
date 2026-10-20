import { useState, type FormEvent } from 'react';
import { Alert, Anchor, Badge, Button, Card, Group, Modal, NumberInput, Stack, Text, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { nextMatchDate, toIsoDate, formatDate } from '@/app/dates';
import { dayOfWeekLabels, formatMatchDate, lineupStatusLabels, listStatusLabels } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { groupKeys, useGroup, useGroupLists, useGroupSeasons, useIsPresident } from './hooks';

type Group = Schemas['GroupResponse'];
type ListSummary = Schemas['WeeklyListSummaryResponse'];

/** The group: the next match, the previous ones and the seasons. The president runs the week from here. */
export function GroupPage() {
    const { groupId = '' } = useParams();
    const group = useGroup(groupId);
    const isPresident = useIsPresident(group.data);

    if (group.isPending) {
        return <PageLoader />;
    }

    if (group.isError) {
        return <ErrorMessage error={group.error} onRetry={() => void group.refetch()} />;
    }

    return (
        <Stack>
            <Anchor component={Link} to="/grupos" size="sm">
                ← Meus grupos
            </Anchor>
            <Group justify="space-between" align="flex-start" wrap="nowrap">
                <Stack gap={2}>
                    <Title order={2}>{group.data.name}</Title>
                    <Text size="sm" c="dimmed">
                        {dayOfWeekLabels[group.data.matchDay]} · {group.data.playersPerTeam} +{' '}
                        {group.data.benchPlayersPerTeam} · {group.data.members.length}{' '}
                        {group.data.members.length === 1 ? 'membro' : 'membros'}
                    </Text>
                </Stack>
                <Button component={Link} to={`/grupos/${groupId}/membros`} variant="light" size="xs">
                    Membros
                </Button>
            </Group>
            <Matches group={group.data} isPresident={isPresident} />
            <Seasons groupId={groupId} isPresident={isPresident} />
        </Stack>
    );
}

function Matches({ group, isPresident }: { group: Group; isPresident: boolean }) {
    const lists = useGroupLists(group.id);
    const [opening, setOpening] = useState(false);

    if (lists.isPending) {
        return <PageLoader />;
    }

    if (lists.isError) {
        return <ErrorMessage error={lists.error} onRetry={() => void lists.refetch()} />;
    }

    const [latest, ...previous] = lists.data;
    const hasOpenList = lists.data.some((list) => list.status === 'Open');

    return (
        <Stack>
            <Group justify="space-between">
                <Title order={3}>Próximo jogo</Title>
                {isPresident && !hasOpenList && (
                    <Button size="xs" onClick={() => setOpening(true)}>
                        Abrir lista
                    </Button>
                )}
            </Group>
            {latest ? (
                <NextMatchCard list={latest} groupId={group.id} isPresident={isPresident} />
            ) : (
                <Text c="dimmed" size="sm">
                    Nenhuma lista ainda. {isPresident ? 'Abra a lista do próximo jogo.' : 'O presidente abre a lista da semana.'}
                </Text>
            )}
            {previous.length > 0 && (
                <>
                    <Title order={4}>Jogos anteriores</Title>
                    {previous.map((list) => (
                        <PreviousMatch key={list.id} list={list} />
                    ))}
                </>
            )}
            <OpenListModal group={group} opened={opening} onClose={() => setOpening(false)} />
        </Stack>
    );
}

function NextMatchCard({ list, groupId, isPresident }: { list: ListSummary; groupId: string; isPresident: boolean }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [confirmingCancel, setConfirmingCancel] = useState(false);
    const refreshLists = () => queryClient.invalidateQueries({ queryKey: groupKeys.lists(groupId) });

    const close = useMutation({
        mutationFn: () => unwrap(api.POST('/api/lists/{id}/close', { params: { path: { id: list.id } } })),
        onSuccess: async () => {
            notifications.show({ color: 'green', message: 'Lista fechada.' });
            await refreshLists();
        },
    });
    const cancel = useMutation({
        mutationFn: () => unwrap(api.POST('/api/lists/{id}/cancel', { params: { path: { id: list.id } } })),
        onSuccess: async () => {
            setConfirmingCancel(false);
            notifications.show({ color: 'green', message: 'Jogo cancelado.' });
            await refreshLists();
        },
    });
    const startLineup = useMutation({
        mutationFn: () => unwrap(api.POST('/api/lists/{listId}/lineup', { params: { path: { listId: list.id } } })),
        onSuccess: async (lineup) => {
            await refreshLists();
            await navigate(`/escalacoes/${lineup.id}`);
        },
    });
    const failed = [close, cancel, startLineup].find((mutation) => mutation.isError);

    return (
        <Card withBorder>
            <Stack gap="sm">
                <Group justify="space-between">
                    <Text fw={600} tt="capitalize">
                        {formatMatchDate(list.matchDate)}
                    </Text>
                    <Badge color={list.status === 'Open' ? 'green' : list.status === 'Cancelled' ? 'red' : 'gray'} variant="light">
                        {listStatusLabels[list.status]}
                    </Badge>
                </Group>
                {list.lineupStatus && !list.matchId && (
                    <Text size="sm" c="dimmed">
                        {lineupStatusLabels[list.lineupStatus]}
                    </Text>
                )}

                <Group gap="xs">
                    <Button component={Link} to={`/listas/${list.id}`} variant="light" size="sm">
                        Ver lista
                    </Button>
                    {list.lineupId && (
                        <Button component={Link} to={`/escalacoes/${list.lineupId}`} variant="light" size="sm">
                            Ver escalação
                        </Button>
                    )}
                    {list.matchId && (
                        <Button component={Link} to={`/partidas/${list.matchId}`} variant="light" size="sm">
                            Ver resultado
                        </Button>
                    )}
                </Group>

                {isPresident && list.status !== 'Cancelled' && !list.matchId && (
                    <Group gap="xs">
                        {list.status === 'Open' && (
                            <Button size="sm" loading={close.isPending} onClick={() => close.mutate()}>
                                Fechar lista
                            </Button>
                        )}
                        {list.status === 'Closed' && !list.lineupId && (
                            <Button size="sm" loading={startLineup.isPending} onClick={() => startLineup.mutate()}>
                                Iniciar escalação
                            </Button>
                        )}
                        <Button size="sm" color="red" variant="subtle" onClick={() => setConfirmingCancel(true)}>
                            Cancelar jogo
                        </Button>
                    </Group>
                )}

                {failed && (
                    <Alert color="red" role="alert">
                        {errorMessage(failed.error, {
                            409: 'A escalação já foi iniciada ou a lista mudou. Recarregue a página.',
                            422: 'Não foi possível nesta situação da lista.',
                        })}
                    </Alert>
                )}
            </Stack>

            <Modal opened={confirmingCancel} onClose={() => setConfirmingCancel(false)} title="Cancelar o jogo?" centered>
                <Stack>
                    <Text size="sm">O jogo de {formatMatchDate(list.matchDate)} será cancelado para todo o grupo.</Text>
                    <Group justify="flex-end">
                        <Button variant="default" onClick={() => setConfirmingCancel(false)}>
                            Voltar
                        </Button>
                        <Button color="red" loading={cancel.isPending} onClick={() => cancel.mutate()}>
                            Cancelar jogo
                        </Button>
                    </Group>
                </Stack>
            </Modal>
        </Card>
    );
}

function PreviousMatch({ list }: { list: ListSummary }) {
    const link = list.matchId ? `/partidas/${list.matchId}` : list.lineupId ? `/escalacoes/${list.lineupId}` : `/listas/${list.id}`;
    return (
        <Card withBorder padding="sm" component={Link} to={link}>
            <Group justify="space-between">
                <Text size="sm" tt="capitalize">
                    {formatMatchDate(list.matchDate)}
                </Text>
                <Text size="sm" c="dimmed">
                    {list.matchId ? 'Ver resultado' : listStatusLabels[list.status]}
                </Text>
            </Group>
        </Card>
    );
}

function OpenListModal({ group, opened, onClose }: { group: Group; opened: boolean; onClose: () => void }) {
    const queryClient = useQueryClient();
    const [matchDate, setMatchDate] = useState(() => nextMatchDate(group.matchDay));

    const openList = useMutation({
        mutationFn: (date: string) =>
            unwrap(api.POST('/api/groups/{groupId}/lists', { params: { path: { groupId: group.id } }, body: { matchDate: date } })),
        onSuccess: async () => {
            onClose();
            notifications.show({ color: 'green', message: 'Lista aberta. Avise o grupo!' });
            await queryClient.invalidateQueries({ queryKey: groupKeys.lists(group.id) });
        },
    });

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        openList.mutate(matchDate);
    }

    return (
        <Modal opened={opened} onClose={onClose} title="Abrir lista" centered>
            <form onSubmit={handleSubmit}>
                <Stack>
                    <TextInput
                        label="Data do jogo"
                        type="date"
                        required
                        value={matchDate}
                        onChange={(event) => setMatchDate(event.currentTarget.value)}
                    />
                    <Text size="sm" c="dimmed">
                        Os mensalistas entram como "não respondeu" e têm a vaga garantida até a lista fechar.
                    </Text>
                    {openList.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(openList.error, { 409: 'Já existe uma lista para esse dia.' })}
                        </Alert>
                    )}
                    <Button type="submit" loading={openList.isPending}>
                        Abrir lista
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}

function Seasons({ groupId, isPresident }: { groupId: string; isPresident: boolean }) {
    const seasons = useGroupSeasons(groupId);
    const [creating, setCreating] = useState(false);

    return (
        <Stack>
            <Group justify="space-between">
                <Title order={3}>Temporadas</Title>
                {isPresident && (
                    <Button size="xs" variant="light" onClick={() => setCreating(true)}>
                        Nova temporada
                    </Button>
                )}
            </Group>
            {seasons.isError && <ErrorMessage error={seasons.error} onRetry={() => void seasons.refetch()} />}
            {seasons.data?.length === 0 && (
                <Text c="dimmed" size="sm">
                    Nenhuma temporada ainda. O ranking aparece quando houver uma.
                </Text>
            )}
            {seasons.data?.map((season) => (
                <Card key={season.id} withBorder padding="sm" component={Link} to={`/temporadas/${season.id}/ranking`}>
                    <Group justify="space-between">
                        <Text fw={600}>{season.name}</Text>
                        <Text size="sm" c="dimmed">
                            {formatDate(season.start)} a {formatDate(season.end)}
                        </Text>
                    </Group>
                </Card>
            ))}
            <CreateSeasonModal groupId={groupId} opened={creating} onClose={() => setCreating(false)} />
        </Stack>
    );
}

function CreateSeasonModal({ groupId, opened, onClose }: { groupId: string; opened: boolean; onClose: () => void }) {
    const queryClient = useQueryClient();
    const [name, setName] = useState('');
    const [start, setStart] = useState(() => toIsoDate(new Date()));
    const [months, setMonths] = useState<number | string>(6);

    const createSeason = useMutation({
        mutationFn: (body: Schemas['CreateSeasonRequest']) =>
            unwrap(api.POST('/api/groups/{groupId}/seasons', { params: { path: { groupId } }, body })),
        onSuccess: async () => {
            onClose();
            notifications.show({ color: 'green', message: 'Temporada criada.' });
            await queryClient.invalidateQueries({ queryKey: groupKeys.seasons(groupId) });
        },
    });

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        createSeason.mutate({ name: name.trim(), start, durationInMonths: Number(months) });
    }

    return (
        <Modal opened={opened} onClose={onClose} title="Nova temporada" centered>
            <form onSubmit={handleSubmit}>
                <Stack>
                    <TextInput
                        label="Nome"
                        placeholder="2026/2"
                        required
                        maxLength={100}
                        value={name}
                        onChange={(event) => setName(event.currentTarget.value)}
                    />
                    <TextInput
                        label="Início"
                        type="date"
                        required
                        value={start}
                        onChange={(event) => setStart(event.currentTarget.value)}
                    />
                    <NumberInput label="Duração (meses)" min={1} max={24} required value={months} onChange={setMonths} />
                    {createSeason.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(createSeason.error, { 409: 'Já existe uma temporada nesse período.' })}
                        </Alert>
                    )}
                    <Button type="submit" loading={createSeason.isPending}>
                        Criar temporada
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
