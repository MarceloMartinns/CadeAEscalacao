import { useEffect, useState, type ReactNode } from 'react';
import { createTheme, MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { subscribeToSession, getSessionStatus } from '@/api/session';
import { createQueryClient } from './queryClient';

const theme = createTheme({
    primaryColor: 'green',
    defaultRadius: 'md',
});

/** Theme, notifications and the TanStack Query cache (ADR-018). */
export function AppProviders({ children, queryClient }: { children: ReactNode; queryClient?: QueryClient }) {
    const [client] = useState(() => queryClient ?? createQueryClient());

    // Data of one person must not show up for the next one who signs in on the same phone.
    useEffect(
        () => subscribeToSession(() => {
            if (getSessionStatus() === 'signedOut') {
                client.clear();
            }
        }),
        [client],
    );

    return (
        // In the tests (Vitest), Mantine turns off transitions and portals, which jsdom cannot run.
        <MantineProvider theme={theme} env={import.meta.env.MODE === 'test' ? 'test' : 'default'}>
            <Notifications position="top-center" />
            <QueryClientProvider client={client}>{children}</QueryClientProvider>
        </MantineProvider>
    );
}
