import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// Separate from vite.config.ts, which creates the HTTPS certificate when it loads (ADR-018).
export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    test: {
        environment: 'jsdom',
        setupFiles: ['./src/test/setup.ts'],
        css: false,
    }
});
