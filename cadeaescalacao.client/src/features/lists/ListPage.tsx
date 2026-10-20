import { useState } from 'react';
import { Accordion, Alert, Anchor, Badge, Button, Card, Group, Modal, Progress, Stack, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Link, useParams } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { formatMatchDate, listStatusLabels, positionLabels, type Position } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { PositionPicker } from '@/components/PositionPicker';
import { useMe } from '@/features/auth/hooks';
import { useGroup, usePlayerName } from '@/features/groups/hooks';
import { useListMutation, useWeeklyList } from './hooks';

type WeeklyList = Schemas['WeeklyListResponse'];
type Entry = Schemas['WeeklyListEntryResponse'];

const listErrors = {
    403: 'Só os membros do grupo podem entrar nesta lista.',
    422: 'Não foi possível: a lista pode ter sido fechada. Recarregue a página.',
};

/** The weekly list: confirm, change positions, leave or wait for a spot. */
export function ListPage() {
    const { listId = '' } = useParams();
    const list = useWeeklyList(listId);

    if (list.isPending) {
        return <PageLoader />;
    }

    if (list.isError) {
        return <ErrorMessage error={list.error} onRetry={() => void list.refetch()} />;
    }

    return <ListView list={list.data} />;
}

function ListView({ list }: { list: WeeklyList }) {
    const group = useGroup(list.groupId);
    const nameOf = usePlayerName(list.groupId);
    const filled = list.capacity - list.openSpots;

    return (
        <Stack>
            <Anchor component={Link} to={`/grupos/${list.groupId}`} size="sm">
                ← {group.data?.name ?? 'Grupo'}
            </Anchor>
            <Group justify="space-between" align="flex-start">
                <Title order={2} tt="capitalize">
                    {formatMatchDate(list.matchDate)}
                </Title>
                <Badge color={list.status === 'Open' ? 'green' : list.status === 'Cancelled' ? 'red' : 'gray'} variant="light">
                    {listStatusLabels[list.status]}
                </Badge>
            </Group>
            <Stack gap={4}>
                <Progress value={(filled / list.capacity) * 100} aria-label="Vagas preenchidas" />
                <Text size="sm" c="dimmed">
                    {filled} de {list.capacity} vagas · {list.openSpots} {list.openSpots === 1 ? 'aberta' : 'abertas'}
                </Text>
            </Stack>

            <MyPresence list={list} />

            <EntrySection title={`Confirmados (${list.playing.length})`} entries={list.playing} nameOf={nameOf} />
            {list.pending.length > 0 && (
                <EntrySection
                    title={`Não responderam (${list.pending.length})`}
                    description="Mensalistas com a vaga guardada até responderem."
                    entries={list.pending}
                    nameOf={nameOf}
                />
            )}
            {list.waiting.length > 0 && (
                <EntrySection
                    title={`Na espera (${list.waiting.length})`}
                    description="Sobem nesta ordem quando abrir vaga."
                    entries={list.waiting}
                    nameOf={nameOf}
                    numbered
                />
            )}
            {list.declined.length > 0 && (
                <Accordion variant="contained">
                    <Accordion.Item value="declined">
                        <Accordion.Control>Fora ({list.declined.length})</Accordion.Control>
                        <Accordion.Panel>
                            {list.declined.map((entry) => (
                                <Text key={entry.playerId} size="sm" py={2}>
                                    {nameOf(entry.playerId)}
                                </Text>
                            ))}
                        </Accordion.Panel>
                    </Accordion.Item>
                </Accordion>
            )}
        </Stack>
    );
}

function EntrySection({
    title,
    description,
    entries,
    nameOf,
    numbered = false,
}: {
    title: string;
    description?: string;
    entries: Entry[];
    nameOf: (playerId: string) => string;
    numbered?: boolean;
}) {
    return (
        <Stack gap="xs">
            <Stack gap={0}>
                <Title order={3}>{title}</Title>
                {description && (
                    <Text size="sm" c="dimmed">
                        {description}
                    </Text>
                )}
            </Stack>
            {entries.length === 0 ? (
                <Text size="sm" c="dimmed">
                    Ninguém ainda.
                </Text>
            ) : (
                <Card withBorder padding="xs">
                    {entries.map((entry, index) => (
                        <Group key={entry.playerId} justify="space-between" py={6} wrap="nowrap">
                            <Text truncate>
                                {numbered && `${index + 1}. `}
                                {nameOf(entry.playerId)}
                            </Text>
                            <Group gap={4} wrap="nowrap">
                                {entry.positions.map((position) => (
                                    <Badge key={position} size="sm" variant="light" color="gray">
                                        {positionLabels[position as Position]}
                                    </Badge>
                                ))}
                            </Group>
                        </Group>
                    ))}
                </Card>
            )}
        </Stack>
    );
}

/** What the signed-in player can do now, depending on where they are on the list. */
function MyPresence({ list }: { list: WeeklyList }) {
    const me = useMe();
    const group = useGroup(list.groupId);
    const playerId = me.data?.player?.id;
    const member = group.data?.members.find((m) => m.playerId === playerId);

    if (!playerId || !member) {
        return null;
    }

    if (list.status !== 'Open') {
        const playing = list.playing.some((entry) => entry.playerId === playerId);
        return (
            <Alert color="gray">
                {list.status === 'Cancelled'
                    ? 'O jogo foi cancelado.'
                    : playing
                      ? 'A lista fechou. Você está entre os confirmados.'
                      : 'A lista fechou. Você não está entre os confirmados.'}
            </Alert>
        );
    }

    const pending = list.pending.find((entry) => entry.playerId === playerId);
    const playing = list.playing.find((entry) => entry.playerId === playerId);
    const waitingIndex = list.waiting.findIndex((entry) => entry.playerId === playerId);

    if (pending) {
        return <PendingCard list={list} />;
    }

    if (playing) {
        return <ConfirmedCard list={list} entry={playing} />;
    }

    if (waitingIndex >= 0) {
        return <WaitingCard list={list} entry={list.waiting[waitingIndex]} place={waitingIndex + 1} />;
    }

    const declined = list.declined.some((entry) => entry.playerId === playerId);
    // A regular who joined the group after the list opened still gets a reserved spot.
    const goesToWaiting = list.openSpots === 0 && (declined || member.type === 'Substitute');
    return <JoinCard list={list} goesToWaiting={goesToWaiting} />;
}

function useSignUp(listId: string) {
    return useListMutation(listId, (positions: Position[]) =>
        unwrap(api.POST('/api/lists/{id}/entries', { params: { path: { id: listId } }, body: { positions } })),
    );
}

function useWithdraw(listId: string) {
    return useListMutation(listId, () =>
        unwrap(api.DELETE('/api/lists/{id}/entries/me', { params: { path: { id: listId } } })),
    );
}

function PendingCard({ list }: { list: WeeklyList }) {
    const [positions, setPositions] = useState<Position[]>([]);
    const signUp = useSignUp(list.id);
    const withdraw = useWithdraw(list.id);
    const failed = signUp.isError ? signUp : withdraw.isError ? withdraw : null;

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>Você vai jogar?</Text>
                <Text size="sm" c="dimmed">
                    Sua vaga de mensalista está guardada até você responder.
                </Text>
                <PositionPicker value={positions} onChange={setPositions} />
                <Group grow>
                    <Button variant="default" loading={withdraw.isPending} onClick={() => withdraw.mutate()}>
                        Não vou
                    </Button>
                    <Button disabled={positions.length === 0} loading={signUp.isPending} onClick={() => signUp.mutate(positions)}>
                        Vou jogar
                    </Button>
                </Group>
                {failed && (
                    <Alert color="red" role="alert">
                        {errorMessage(failed.error, listErrors)}
                    </Alert>
                )}
            </Stack>
        </Card>
    );
}

function JoinCard({ list, goesToWaiting }: { list: WeeklyList; goesToWaiting: boolean }) {
    const [positions, setPositions] = useState<Position[]>([]);
    const signUp = useSignUp(list.id);

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>{goesToWaiting ? 'A lista está cheia' : 'Você ainda não está na lista'}</Text>
                {goesToWaiting && (
                    <Text size="sm" c="dimmed">
                        Você entra na espera e sobe se abrir vaga.
                    </Text>
                )}
                <PositionPicker value={positions} onChange={setPositions} />
                <Button disabled={positions.length === 0} loading={signUp.isPending} onClick={() => signUp.mutate(positions)}>
                    {goesToWaiting ? 'Entrar na espera' : 'Vou jogar'}
                </Button>
                {signUp.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(signUp.error, listErrors)}
                    </Alert>
                )}
            </Stack>
        </Card>
    );
}

function ConfirmedCard({ list, entry }: { list: WeeklyList; entry: Entry }) {
    const [positions, setPositions] = useState<Position[]>(entry.positions as Position[]);
    const [confirmingLeave, setConfirmingLeave] = useState(false);
    const changePositions = useListMutation(list.id, (body: Schemas['ChangePositionsRequest']) =>
        unwrap(api.PUT('/api/lists/{id}/entries/me', { params: { path: { id: list.id } }, body })),
    );
    const withdraw = useWithdraw(list.id);
    const changed = [...positions].sort().join() !== [...entry.positions].sort().join();
    const failed = changePositions.isError ? changePositions : withdraw.isError ? withdraw : null;

    function leave() {
        withdraw.mutate(undefined, { onSuccess: () => setConfirmingLeave(false) });
    }

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>Você está confirmado ✅</Text>
                <PositionPicker value={positions} onChange={setPositions} />
                <Group grow>
                    <Button variant="default" color="red" onClick={() => (entry.hasReservedSpot ? setConfirmingLeave(true) : leave())} loading={withdraw.isPending && !confirmingLeave}>
                        Sair da lista
                    </Button>
                    <Button
                        disabled={!changed || positions.length === 0}
                        loading={changePositions.isPending}
                        onClick={() =>
                            changePositions.mutate(
                                { positions },
                                { onSuccess: () => notifications.show({ color: 'green', message: 'Posições salvas.' }) },
                            )
                        }
                    >
                        Salvar posições
                    </Button>
                </Group>
                {failed && (
                    <Alert color="red" role="alert">
                        {errorMessage(failed.error, listErrors)}
                    </Alert>
                )}
            </Stack>
            <Modal opened={confirmingLeave} onClose={() => setConfirmingLeave(false)} title="Sair da lista?" centered>
                <Stack>
                    <Text size="sm">
                        Você perde a vaga garantida de mensalista. Se voltar depois, só joga se houver vaga.
                    </Text>
                    <Group justify="flex-end">
                        <Button variant="default" onClick={() => setConfirmingLeave(false)}>
                            Voltar
                        </Button>
                        <Button color="red" loading={withdraw.isPending} onClick={leave}>
                            Sair da lista
                        </Button>
                    </Group>
                </Stack>
            </Modal>
        </Card>
    );
}

function WaitingCard({ list, entry, place }: { list: WeeklyList; entry: Entry; place: number }) {
    const withdraw = useWithdraw(list.id);

    return (
        <Card withBorder>
            <Stack>
                <Text fw={600}>Você é o {place}º da espera</Text>
                <Text size="sm" c="dimmed">
                    Se abrir vaga, você sobe para os confirmados. Posições:{' '}
                    {entry.positions.map((position) => positionLabels[position as Position]).join(', ')}.
                </Text>
                <Button variant="default" loading={withdraw.isPending} onClick={() => withdraw.mutate()}>
                    Sair da espera
                </Button>
                {withdraw.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(withdraw.error, listErrors)}
                    </Alert>
                )}
            </Stack>
        </Card>
    );
}
