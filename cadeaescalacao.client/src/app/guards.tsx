import { useEffect } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { refreshSession } from '@/api/session';
import { useMe, useSessionStatus } from '@/features/auth/hooks';
import { ErrorMessage, PageLoader } from '@/components/feedback';

/**
 * Pages for signed-in people. When the app opens, the session comes back from the cookie
 * (POST /api/auth/refresh); without it, the person goes to /login and returns here afterwards.
 */
export function RequireSession() {
    const status = useSessionStatus();
    const location = useLocation();

    useEffect(() => {
        if (status === 'unknown') {
            void refreshSession();
        }
    }, [status]);

    if (status === 'unknown') {
        return <PageLoader />;
    }

    if (status === 'signedOut') {
        return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
    }

    return <Outlet />;
}

/** Pages that need the player: whoever has no profile yet creates it first (POST /api/me). */
export function RequireProfile() {
    const me = useMe();

    if (me.isPending) {
        return <PageLoader />;
    }

    if (me.isError) {
        return <ErrorMessage error={me.error} onRetry={() => void me.refetch()} />;
    }

    if (!me.data.player) {
        return <Navigate to="/perfil" replace />;
    }

    return <Outlet />;
}
