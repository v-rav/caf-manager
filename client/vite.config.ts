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
    rolldownOptions: {
      output: {
        // Stable vendor chunks: browsers keep them cached across app deploys, and charts load only where used.
        codeSplitting: {
          groups: [
            { name: 'vendor-react', test: /node_modules[\\/](react|react-dom|react-router|react-router-dom|scheduler)[\\/]/ },
            { name: 'vendor-charts', test: /node_modules[\\/](chart\.js|react-chartjs-2|@kurkle)[\\/]/ },
            { name: 'vendor-fluent', test: /node_modules[\\/](@fluentui|@griffel|tabster|keyborg|@floating-ui|@swc)[\\/]/ },
            { name: 'vendor', test: /node_modules/ },
          ],
        },
      },
    },
  },
})
