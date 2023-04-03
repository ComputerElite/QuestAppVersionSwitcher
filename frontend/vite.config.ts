import { defineConfig } from 'vite';
import solidPlugin from 'vite-plugin-solid';

export default defineConfig({
  plugins: [solidPlugin()],
  server: {
    port: 3000,
    proxy: {
      // string shorthand: http://localhost:5173/foo -> http://localhost:4567/foo
      '/api': {
        target: 'http://localhost:50002/',
      }
    },
  },
  build: {
    target: 'esnext',
  },
  resolve: {
    alias: {
      '@': '/src',
    }
  },
  css: {
      preprocessorOptions: {
        scss: {
          additionalData: `@import "@/assets/global.scss"; `
        }
      }
  }
});
