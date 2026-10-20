import { Anchor, Stack, Text, Title } from '@mantine/core';
import { Link } from 'react-router';

export function NotFoundPage() {
    return (
        <Stack>
            <Title order={2}>Página não encontrada</Title>
            <Text c="dimmed">O endereço pode estar errado ou a página mudou de lugar.</Text>
            <Anchor component={Link} to="/grupos">
                Voltar para os grupos
            </Anchor>
        </Stack>
    );
}
