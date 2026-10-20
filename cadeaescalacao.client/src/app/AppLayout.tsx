import { AppShell, Button, Container, Group, Text, Title } from '@mantine/core';
import { Link, Outlet, useNavigate } from 'react-router';
import { signOut } from '@/api/session';
import { useMe } from '@/features/auth/hooks';

/** Mobile-first layout: a short header and one column, also on a computer (ADR-018). */
export function AppLayout() {
    const me = useMe();
    const navigate = useNavigate();

    async function handleSignOut() {
        await signOut();
        await navigate('/login', { replace: true });
    }

    return (
        <AppShell header={{ height: 56 }} padding="md">
            <AppShell.Header>
                <Container size="xs" h="100%">
                    <Group h="100%" justify="space-between" wrap="nowrap">
                        <Title order={1} size="h4">
                            <Link to="/grupos" style={{ color: 'inherit', textDecoration: 'none' }}>
                                Cadê a escalação?
                            </Link>
                        </Title>
                        <Group gap="xs" wrap="nowrap">
                            {me.data?.player && (
                                <Text size="sm" c="dimmed" truncate maw={120}>
                                    {me.data.player.name}
                                </Text>
                            )}
                            <Button variant="subtle" size="compact-sm" onClick={() => void handleSignOut()}>
                                Sair
                            </Button>
                        </Group>
                    </Group>
                </Container>
            </AppShell.Header>
            <AppShell.Main>
                <Container size="xs" px={0}>
                    <Outlet />
                </Container>
            </AppShell.Main>
        </AppShell>
    );
}
