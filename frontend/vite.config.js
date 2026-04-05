import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const target = env.VITE_PROXY_TARGET || 'http://localhost:5122'

  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target,
          changeOrigin: true,
        },
        '/health': {
          target,
          changeOrigin: true,
        },
        '/hubs': {
          target,
          changeOrigin: true,
          ws: true,
        },
      },
    },
  }
})
