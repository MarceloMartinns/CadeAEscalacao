import { Navigate, type RouteObject } from 'react-router';
import { LoginPage } from '@/features/auth/LoginPage';
import { ProfilePage } from '@/features/auth/ProfilePage';
import { CreateGroupPage } from '@/features/groups/CreateGroupPage';
import { GroupPage } from '@/features/groups/GroupPage';
import { GroupsPage } from '@/features/groups/GroupsPage';
import { MembersPage } from '@/features/groups/MembersPage';
import { LineupPage } from '@/features/lineups/LineupPage';
import { ListPage } from '@/features/lists/ListPage';
import { MatchPage } from '@/features/matches/MatchPage';
import { RankingPage } from '@/features/rankings/RankingPage';
import { AppLayout } from './AppLayout';
import { RequireProfile, RequireSession } from './guards';
import { NotFoundPage } from './NotFoundPage';

/** Routes of the app (ADR-018). Shared by the browser router and by the tests' memory router. */
export const routes: RouteObject[] = [
    { path: '/login', element: <LoginPage /> },
    {
        element: <RequireSession />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/perfil', element: <ProfilePage /> },
                    {
                        element: <RequireProfile />,
                        children: [
                            { path: '/', element: <Navigate to="/grupos" replace /> },
                            { path: '/grupos', element: <GroupsPage /> },
                            { path: '/grupos/novo', element: <CreateGroupPage /> },
                            { path: '/grupos/:groupId', element: <GroupPage /> },
                            { path: '/grupos/:groupId/membros', element: <MembersPage /> },
                            { path: '/listas/:listId', element: <ListPage /> },
                            { path: '/escalacoes/:lineupId', element: <LineupPage /> },
                            { path: '/partidas/:matchId', element: <MatchPage /> },
                            { path: '/temporadas/:seasonId/ranking', element: <RankingPage /> },
                        ],
                    },
                    { path: '*', element: <NotFoundPage /> },
                ],
            },
        ],
    },
];
