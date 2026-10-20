import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { AppProviders } from '@/app/AppProviders';
import { createQueryClient } from '@/app/queryClient';
import { routes } from '@/app/routes';

/** The whole app (routes, guards, providers) starting at a path, as if opened in the browser. */
export function renderApp(path: string) {
    const router = createMemoryRouter(routes, { initialEntries: [path] });
    const queryClient = createQueryClient();
    queryClient.setDefaultOptions({ queries: { ...queryClient.getDefaultOptions().queries, retry: false } });

    const result = render(
        <AppProviders queryClient={queryClient}>
            <RouterProvider router={router} />
        </AppProviders>,
    );

    return { ...result, router };
}
