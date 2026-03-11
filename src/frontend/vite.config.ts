import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5128',
      '/hubs': {
        target: 'http://localhost:5128',
        ws: true,
      },
    },
  },
})
