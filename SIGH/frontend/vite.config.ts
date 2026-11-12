/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// Em desenvolvimento o front chama "/api" na própria origem e o Vite repassa para a API .NET.
// Assim o cookie HttpOnly do refresh token (Path=/api/v1/auth) é de mesma origem e não há CORS.
const API_TARGET = 'http://localhost:5000';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rollupOptions: {
      output: {
        // React/router e axios/react-query em chunks próprios (estáveis, ficam em cache entre deploys).
        // O MUI não é forçado num chunk único: assim os componentes usados só por páginas lazy
        // continuam nos chunks dessas páginas e não entram na carga inicial.
        manualChunks(id) {
          if (!id.includes('node_modules')) return undefined;
          if (/node_modules\/(react|react-dom|scheduler|react-router|react-router-dom|cookie|set-cookie-parser)\//.test(id))
            return 'vendor-react';
          if (/node_modules\/(axios|@tanstack)\//.test(id)) return 'vendor-data';
          return undefined;
        },
      },
    },
  },
  server: {
    port: 3000,
    strictPort: true,
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: false,
      },
    },
  },
  preview: {
    port: 3000,
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: false,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true,
  },
});
