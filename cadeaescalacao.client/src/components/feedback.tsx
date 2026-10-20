import { Alert, Button, Center, Loader, Stack, Text } from '@mantine/core';
import { errorMessage } from '@/api/client';

export function PageLoader() {
    return (
        <Center py="xl">
            <Loader aria-label="Carregando" />
        </Center>
    );
}

/** An error while loading a screen, with a button to try again. */
export function ErrorMessage({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
    return (
        <Alert color="red" title="Não foi possível carregar" role="alert">
            <Stack gap="xs" align="flex-start">
                <Text size="sm">{errorMessage(error)}</Text>
                {onRetry && (
                    <Button size="xs" variant="light" color="red" onClick={onRetry}>
                        Tentar de novo
                    </Button>
                )}
            </Stack>
        </Alert>
    );
}
