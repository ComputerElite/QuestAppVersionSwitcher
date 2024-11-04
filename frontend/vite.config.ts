import { defineConfig } from "vite";
import solidPlugin from "vite-plugin-solid";
import devtools from "solid-devtools/vite";
import legacy from "@vitejs/plugin-legacy";

export default defineConfig({
  base: "./",
  plugins: [
    // Allows q1 browser to work? Needs to be tested
    legacy({
      targets: ["defaults", "not IE 11", "chrome >= 92"],
      renderModernChunks: false,
    }),
    solidPlugin(),
    devtools({
      autoname: true,
      locator: {
        targetIDE: "vscode",
        key: "Ctrl",
        jsxLocation: true,
        componentLocation: true,
      },
    }),
  ],
  server: {
    port: 3000,
    proxy: {
      // string shorthand: http://localhost:5173/foo -> http://localhost:4567/foo
      "/api": {
        target: "http://127.0.0.1:50002/",
      },
    },
  },
  resolve: {
    alias: {
      "@": "/src",
    },
  },
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: `@import "@/assets/global.scss"; `,
        // WARNING: If you update to dart-sass 3.0 you may need to remove this silencing
        silenceDeprecations: ["legacy-js-api", "import", "global-builtin"],
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        entryFileNames: `assets/[name].js`,
        chunkFileNames: `assets/[name].js`,
        assetFileNames: `assets/[name].[ext]`,
      },
    },
  },
});
