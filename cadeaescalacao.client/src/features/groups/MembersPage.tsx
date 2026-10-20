import { useState, type FormEvent } from 'react';
import { Alert, Anchor, Badge, Button, Card, Group, List, Modal, SegmentedControl, Select, Stack, Text, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Link, useParams } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { playerTypeLabels } from '@/app/labels';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { useGroup, useGroupMutation, useIsPresident, usePlayerName } from './hooks';

type Group = Schemas['GroupResponse'];

/** Members, pairs and coach rotation. The president adds members by e-mail and creates pairs (ADR-021). */
export function MembersPage() {
    const { groupId = '' } = useParams();
    const group = useGroup(groupId);
    const isPresident = useIsPresident(group.data);
    const nameOf = usePlayerName(groupId);
    const [adding, setAdding] = useState(false);
    const [pairing, setPairing] = useState(false);

    if (group.isPending) {
        return <PageLoader />;
    }

    if (group.isError) {
        return <ErrorMessage error={group.error} onRetry={() => void group.refetch()} />;
    }

    const members = [...group.data.members].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'));

    return (
        <Stack>
            <Anchor component={Link} to={`/grupos/${groupId}`} size="sm">
                ← {group.data.name}
            </Anchor>

            <Group justify="space-between">
                <Title order={2}>Membros</Title>
                {isPresident && (
                    <Button size="xs" onClick={() => setAdding(true)}>
                        Adicionar membro
                    </Button>
                )}
            </Group>
            <Card withBorder padding="xs">
                {members.map((member) => (
                    <Group key={member.playerId} justify="space-between" py={6} wrap="nowrap">
                        <Text truncate>{member.name}</Text>
                        <Group gap={4} wrap="nowrap">
                            {member.playerId === group.data.presidentId && (
                                <Badge variant="light" color="yellow">
                                    Presidente
                                </Badge>
                            )}
                            <Badge variant="light" color={member.type === 'Regular' ? 'green' : 'gray'}>
                                {playerTypeLabels[member.type]}
                            </Badge>
                        </Group>
                    </Group>
                ))}
            </Card>

            <Group justify="space-between">
                <Title order={3}>Duplas</Title>
                {isPresident && (
                    <Button size="xs" variant="light" onClick={() => setPairing(true)}>
                        Criar dupla
                    </Button>
                )}
            </Group>
            {group.data.pairs.length === 0 ? (
                <Text size="sm" c="dimmed">
                    Nenhuma dupla. Quando o professor da vez tem dupla, é ela que escolhe o time.
                </Text>
            ) : (
                <List spacing={4}>
                    {group.data.pairs.map((pair) => (
                        <List.Item key={`${pair.firstPlayerId}-${pair.secondPlayerId}`}>
                            {nameOf(pair.firstPlayerId)} e {nameOf(pair.secondPlayerId)}
                        </List.Item>
                    ))}
                </List>
            )}

            <Title order={3}>Rodízio de professor</Title>
            <Text size="sm" c="dimmed">
                O primeiro da fila monta os times na próxima escalação e depois vai para o fim.
            </Text>
            <List type="ordered" spacing={4}>
                {group.data.coachRotation.map((playerId) => (
                    <List.Item key={playerId}>{nameOf(playerId)}</List.Item>
                ))}
            </List>

            <AddMemberModal group={group.data} opened={adding} onClose={() => setAdding(false)} />
            <CreatePairModal group={group.data} opened={pairing} onClose={() => setPairing(false)} />
        </Stack>
    );
}

function AddMemberModal({ group, opened, onClose }: { group: Group; opened: boolean; onClose: () => void }) {
    const [email, setEmail] = useState('');
    const [type, setType] = useState<Schemas['PlayerType']>('Regular');

    const addMember = useGroupMutation(group.id, (body: Schemas['AddMemberByEmailRequest']) =>
        unwrap(api.POST('/api/groups/{id}/members/by-email', { params: { path: { id: group.id } }, body })),
    );

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        addMember.mutate(
            { email: email.trim(), type },
            {
                onSuccess: () => {
                    notifications.show({ color: 'green', message: `${email.trim()} entrou no grupo.` });
                    setEmail('');
                    onClose();
                },
            },
        );
    }

    return (
        <Modal opened={opened} onClose={onClose} title="Adicionar membro" centered>
            <form onSubmit={handleSubmit}>
                <Stack>
                    <TextInput
                        label="E-mail da pessoa"
                        description="A pessoa precisa ter entrado no app e criado o perfil."
                        type="email"
                        inputMode="email"
                        required
                        value={email}
                        onChange={(event) => setEmail(event.currentTarget.value)}
                    />
                    <SegmentedControl
                        aria-label="Tipo de membro"
                        fullWidth
                        value={type}
                        onChange={(value) => setType(value as Schemas['PlayerType'])}
                        data={[
                            { value: 'Regular', label: playerTypeLabels.Regular },
                            { value: 'Substitute', label: playerTypeLabels.Substitute },
                        ]}
                    />
                    {addMember.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(addMember.error, {
                                400: 'Confira o e-mail digitado.',
                                404: 'Não encontramos ninguém com esse e-mail. A pessoa precisa entrar no app e criar o perfil primeiro.',
                                422: 'Essa pessoa já é membro do grupo.',
                            })}
                        </Alert>
                    )}
                    <Button type="submit" loading={addMember.isPending}>
                        Adicionar
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}

function CreatePairModal({ group, opened, onClose }: { group: Group; opened: boolean; onClose: () => void }) {
    const [first, setFirst] = useState<string | null>(null);
    const [second, setSecond] = useState<string | null>(null);
    const options = group.members
        .map((member) => ({ value: member.playerId, label: member.name }))
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'));

    const createPair = useGroupMutation(group.id, (body: Schemas['CreatePairRequest']) =>
        unwrap(api.POST('/api/groups/{id}/pairs', { params: { path: { id: group.id } }, body })),
    );

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        if (first && second) {
            createPair.mutate(
                { firstPlayerId: first, secondPlayerId: second },
                {
                    onSuccess: () => {
                        notifications.show({ color: 'green', message: 'Dupla criada.' });
                        setFirst(null);
                        setSecond(null);
                        onClose();
                    },
                },
            );
        }
    }

    return (
        <Modal opened={opened} onClose={onClose} title="Criar dupla" centered>
            <form onSubmit={handleSubmit}>
                <Stack>
                    <Select label="Primeiro jogador" data={options} value={first} onChange={setFirst} searchable required />
                    <Select
                        label="Segundo jogador"
                        data={options.filter((option) => option.value !== first)}
                        value={second}
                        onChange={setSecond}
                        searchable
                        required
                    />
                    {createPair.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(createPair.error, { 422: 'Um dos dois já está em outra dupla.' })}
                        </Alert>
                    )}
                    <Button type="submit" loading={createPair.isPending} disabled={!first || !second}>
                        Criar dupla
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
