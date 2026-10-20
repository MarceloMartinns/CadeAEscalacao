import { setupServer } from 'msw/node';

/**
 * Fake API for the tests (ADR-018): MSW answers the HTTP requests, so the screens use the real
 * client and session code. Each test adds the answers it needs with server.use(...).
 */
export const server = setupServer();
