import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll, vi } from 'vitest';
import { resetSessionForTests } from '@/api/session';
import { server } from './server';

// jsdom does not implement these browser APIs, which Mantine uses.
Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
    })),
});

class ResizeObserverStub {
    observe() {}
    unobserve() {}
    disconnect() {}
}

window.ResizeObserver = ResizeObserverStub;
Element.prototype.scrollIntoView = vi.fn();

// A request without an answer in the test is a mistake in the test.
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }));

afterEach(() => {
    cleanup();
    server.resetHandlers();
    resetSessionForTests();
});

afterAll(() => server.close());
