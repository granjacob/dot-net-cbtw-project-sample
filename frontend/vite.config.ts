import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      "/api/auth": { target: "http://localhost:5001", changeOrigin: true },
      "/api/requests": { target: "http://localhost:5001", changeOrigin: true },
      "/api/notifications": { target: "http://localhost:5002", changeOrigin: true },
      "/hubs": { target: "http://localhost:5002", changeOrigin: true, ws: true }
    }
  },
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    css: true
  }
});
