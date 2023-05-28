import { defineConfig } from 'vite';
import solidPlugin from 'vite-plugin-solid';
import devtools from 'solid-devtools/vite';

export default defineConfig({
  base: "./",
  plugins: [
    
    solidPlugin(),
    devtools({
      autoname: true,
      locator: {
        targetIDE: 'vscode',
        key: 'Ctrl',
        jsxLocation: true,
        componentLocation: true,
      },
    }),
  ],
  server: {
    port: 3000,
    proxy: {
      // string shorthand: http://localhost:5173/foo -> http://localhost:4567/foo
      '/api': {
        target: 'http://127.0.0.1:5002/',
      }
    },
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
  },
  build: {
    target: 'esnext',
    rollupOptions: {
      output: {
        entryFileNames: `assets/[name].js`,
        chunkFileNames: `assets/[name].js`,
        assetFileNames: `assets/[name].[ext]`
      }
    }
  }
});
