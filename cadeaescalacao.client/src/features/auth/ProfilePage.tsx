import { useState, type FormEvent } from 'react';
import { Alert, Button, Stack, Text, TextInput, Title } from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Navigate, useNavigate } from 'react-router';
import { api, errorMessage, unwrap, type Schemas } from '@/api/client';
import { signIn } from '@/api/session';
import { ErrorMessage, PageLoader } from '@/components/feedback';
import { meQueryKey, useMe } from './hooks';

/** First access: the person says the name the group will see (POST /api/me, ADR-014). */
export function ProfilePage() {
    const me = useMe();

    if (me.isPending) {
        return <PageLoader />;
    }

    if (me.isError) {
        return <ErrorMessage error={me.error} onRetry={() => void me.refetch()} />;
    }

    if (me.data.player) {
        return <Navigate to="/grupos" replace />;
    }

    return <ProfileForm me={me.data} />;
}

function ProfileForm({ me }: { me: Schemas['MeResponse'] }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [name, setName] = useState('');

    const createProfile = useMutation({
        mutationFn: (playerName: string) => unwrap(api.POST('/api/me', { body: { name: playerName } })),
        onSuccess: async (profile) => {
            // The new access token already has the player_id (ADR-015).
            signIn(profile.token.accessToken);
            queryClient.setQueryData<Schemas['MeResponse']>(meQueryKey, { ...me, player: profile.player });
            await navigate('/grupos', { replace: true });
        },
    });

    function handleSubmit(event: FormEvent) {
        event.preventDefault();
        createProfile.mutate(name.trim());
    }

    return (
        <form onSubmit={handleSubmit}>
            <Stack>
                <Title order={2}>Bem-vindo!</Title>
                <Text c="dimmed">Como o pessoal da pelada te chama? O nome aparece na lista e na escalação.</Text>
                <TextInput
                    label="Nome"
                    autoComplete="nickname"
                    required
                    maxLength={100}
                    value={name}
                    onChange={(event) => setName(event.currentTarget.value)}
                />
                {createProfile.isError && (
                    <Alert color="red" role="alert">
                        {errorMessage(createProfile.error, { 409: 'Esta conta já tem um perfil. Recarregue a página.' })}
                    </Alert>
                )}
                <Button type="submit" loading={createProfile.isPending} fullWidth>
                    Salvar
                </Button>
            </Stack>
        </form>
    );
}
