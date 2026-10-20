import createClient from 'openapi-fetch';
import type { components, paths } from './schema';
import { getAccessToken, refreshSession } from './session';

export type Schemas = components['schemas'];

/**
 * Typed client of the API, generated from the OpenAPI document (ADR-019).
 * Every request carries the access token. On 401 the session is renewed once and the request
 * is repeated; if the renewal fails, the session ends and the screens go to /login (ADR-020).
 */
export const api = createClient<paths>({
    baseUrl: window.location.origin,
    fetch: fetchWithSession,
});

async function fetchWithSession(request: Request): Promise<Response> {
    // The body can be read only once, so keep a copy for the retry.
    const retry = request.clone();
    const response = await globalThis.fetch(withToken(request, getAccessToken()));
    if (response.status !== 401 || new URL(request.url).pathname.startsWith('/api/auth/')) {
        return response;
    }

    const token = await refreshSession();
    return token ? globalThis.fetch(withToken(retry, token)) : response;
}

function withToken(request: Request, token: string | null): Request {
    if (token) {
        request.headers.set('Authorization', `Bearer ${token}`);
    }

    return request;
}

/** An error answer of the API, with its Problem Details (ADR-013). */
export class ApiError extends Error {
    readonly status: number;
    readonly problem: Schemas['ProblemDetails'] | undefined;

    constructor(status: number, problem: Schemas['ProblemDetails'] | undefined) {
        super(problem?.detail ?? problem?.title ?? `HTTP ${status}`);
        this.name = 'ApiError';
        this.status = status;
        this.problem = problem;
    }
}

/**
 * The data of an openapi-fetch answer, or an ApiError. TanStack Query expects a promise that
 * fails on error, and openapi-fetch returns { data, error } instead.
 */
export async function unwrap<R extends { data?: unknown; error?: unknown; response: Response }>(
    call: Promise<R>,
): Promise<Exclude<R['data'], undefined>> {
    const { data, error, response } = await call;
    if (!response.ok) {
        throw new ApiError(response.status, error as Schemas['ProblemDetails'] | undefined);
    }

    return data as Exclude<R['data'], undefined>;
}

const defaultMessages: Record<number, string> = {
    401: 'Sua sessão terminou. Entre de novo.',
    403: 'Você não tem permissão para fazer isso.',
    404: 'Não encontramos o que você procurou.',
    409: 'Alguém mudou isso ao mesmo tempo. Recarregue a página e tente de novo.',
    429: 'Muitas tentativas. Espere um minuto e tente de novo.',
};

/**
 * A message in Portuguese for the screens. The API's own messages are in English (ADR-009),
 * so each screen passes its texts for the statuses it expects.
 */
export function errorMessage(error: unknown, messages: Record<number, string> = {}): string {
    if (error instanceof ApiError) {
        return messages[error.status] ?? defaultMessages[error.status] ?? 'Algo deu errado. Tente de novo.';
    }

    return 'Sem conexão com o servidor. Verifique a internet e tente de novo.';
}
