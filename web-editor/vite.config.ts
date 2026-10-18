import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    watch: process.env.VITE_DOCKER_POLLING === 'true'
      ? { usePolling: true, interval: 250 }
      : undefined,
  },
})
