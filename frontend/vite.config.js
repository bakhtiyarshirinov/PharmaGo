import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.VITE_PROXY_TARGET || 'http://localhost:5122',
        changeOrigin: true,
      },
      '/health': {
        target: process.env.VITE_PROXY_TARGET || 'http://localhost:5122',
        changeOrigin: true,
      },
      '/hubs': {
        target: process.env.VITE_PROXY_TARGET || 'http://localhost:5122',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
