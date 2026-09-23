import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The production build is emitted straight into the API's wwwroot so the whole
// portal ships as a single App Service deployment package.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/health': 'http://localhost:5080',
    },
  },
  build: {
    outDir: '../src/CafPortal.Api/wwwroot',
    emptyOutDir: true,
  },
})
