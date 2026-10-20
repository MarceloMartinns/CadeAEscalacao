import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Anchor, Button, Center, Container, Group, Loader, PinInput, Stack, Text, TextInput, Title } from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { errorMessage, unwrap } from '@/api/client';
import { authClient, signIn } from '@/api/session';
import { useSessionStatus } from './hooks';
import { parseLoginLink, type LoginLink } from './loginLink';

/** What the login page may receive in the navigation state. */
interface LoginState {
    /** Where the person was going before being sent to the login. */
    from?: string;
    email?: string;
    linkFailed?: boolean;
}

/** Codes already sent from a link. React's StrictMode runs effects twice, and a code works only once. */
const usedLinkCodes = new Set<string>();

function useVerifyCode() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ email, code }: LoginLink) =>
            unwrap(authClient.POST('/api/auth/email/verify', { body: { email, code, useCookie: true } })),
        onSuccess: (tokens) => {
            queryClient.clear();
            signIn(tokens.accessToken);
        },
    });
}

const codeErrors = {
    400: 'Código inválido ou expirado. Confira o e-mail ou peça um novo código.',
};

/** Passwordless login: the person types the e-mail, receives a 6-digit code and types it (ADR-014). */
export function LoginPage() {
    const status = useSessionStatus();
    const location = useLocation();
    const state = (location.state ?? {}) as LoginState;
    const link = parseLoginLink(location.hash);

    if (link) {
        return <LoginFromLink link={link} from={state.from} />;
    }

    if (status === 'signedIn') {
        return <Navigate to={state.from ?? '/grupos'} replace />;
    }

    return (
        <Container size="xs" py="xl">
            <Stack gap="lg">
                <Stack gap={4}>
                    <Title order={1}>Cadê a escalação?</Title>
                    <Text c="dimmed">Entre com o seu e-mail. Não precisa de senha.</Text>
                </Stack>
                <LoginForm initialEmail={state.email} linkFailed={state.linkFailed} from={state.from} />
            </Stack>
        </Container>
    );
}

function LoginForm({ initialEmail, linkFailed, from }: { initialEmail?: string; linkFailed?: boolean; from?: string }) {
    const navigate = useNavigate();
    const [email, setEmail] = useState(initialEmail ?? '');
    const [codeSentTo, setCodeSentTo] = useState<string | null>(null);
    const [code, setCode] = useState('');

    const requestCode = useMutation({
        mutationFn: (address: string) => unwrap(authClient.POST('/api/auth/email/start', { body: { email: address } })),
        onSuccess: (_, address) => {
            setCodeSentTo(address);
            setCode('');
        },
    });
    const verify = useVerifyCode();

    function handleRequestCode(event: FormEvent) {
        event.preventDefault();
        verify.reset();
        requestCode.mutate(email.trim());
    }

    function handleCodeComplete(value: string) {
        if (codeSentTo) {
            verify.mutate(
                { email: codeSentTo, code: value },
                {
                    onSuccess: () => void navigate(from ?? '/grupos', { replace: true }),
                    onError: () => setCode(''),
                },
            );
        }
    }

    if (!codeSentTo) {
        return (
            <form onSubmit={handleRequestCode}>
                <Stack>
                    {linkFailed && (
                        <Alert color="yellow" role="alert">
                            O link expirou ou já foi usado. Peça um novo código.
                        </Alert>
                    )}
                    <TextInput
                        label="E-mail"
                        type="email"
                        autoComplete="email"
                        inputMode="email"
                        required
                        value={email}
                        onChange={(event) => setEmail(event.currentTarget.value)}
                    />
                    {requestCode.isError && (
                        <Alert color="red" role="alert">
                            {errorMessage(requestCode.error, { 400: 'Confira o e-mail digitado.' })}
                        </Alert>
                    )}
                    <Button type="submit" loading={requestCode.isPending} fullWidth>
                        Receber código
                    </Button>
                </Stack>
            </form>
        );
    }

    return (
        <Stack>
            <Text>
                Enviamos um código de 6 dígitos para <strong>{codeSentTo}</strong>. Digite o código ou toque no link do
                e-mail.
            </Text>
            <Center>
                <PinInput
                    length={6}
                    type="number"
                    inputMode="numeric"
                    oneTimeCode
                    autoFocus
                    size="lg"
                    ariaLabel="Código de acesso"
                    value={code}
                    onChange={setCode}
                    onComplete={handleCodeComplete}
                    disabled={verify.isPending}
                    error={verify.isError}
                />
            </Center>
            {verify.isPending && (
                <Center>
                    <Loader size="sm" aria-label="Conferindo o código" />
                </Center>
            )}
            {verify.isError && (
                <Alert color="red" role="alert">
                    {errorMessage(verify.error, codeErrors)}
                </Alert>
            )}
            <Group justify="space-between">
                <Anchor component="button" type="button" size="sm" onClick={() => setCodeSentTo(null)}>
                    Usar outro e-mail
                </Anchor>
                <Anchor
                    component="button"
                    type="button"
                    size="sm"
                    disabled={requestCode.isPending}
                    onClick={() => {
                        verify.reset();
                        requestCode.mutate(codeSentTo);
                    }}
                >
                    Reenviar código
                </Anchor>
            </Group>
        </Stack>
    );
}

/** Opened from the e-mail link: sends the code at once and clears it from the address. */
function LoginFromLink({ link, from }: { link: LoginLink; from?: string }) {
    const navigate = useNavigate();
    const { mutateAsync } = useVerifyCode();
    const { email, code } = link;

    useEffect(() => {
        if (usedLinkCodes.has(code)) {
            return;
        }

        usedLinkCodes.add(code);
        mutateAsync({ email, code }).then(
            () => navigate(from ?? '/grupos', { replace: true }),
            () => navigate('/login', { replace: true, state: { email, linkFailed: true, from } }),
        );
    }, [email, code, from, mutateAsync, navigate]);

    return (
        <Center py="xl">
            <Stack align="center">
                <Loader aria-label="Entrando" />
                <Text c="dimmed">Entrando...</Text>
            </Stack>
        </Center>
    );
}
