import { defineConfig } from '@playwright/test'

const baseURL = process.env.E2E_FRONTEND_URL || 'http://localhost:5173'
const port = new URL(baseURL).port

export default defineConfig({
  testDir: './tests',
  workers: 1,
  timeout: 180_000,
  webServer: { command: 'npm run dev -- --port ' + port, url: baseURL, reuseExistingServer: true },
  use: {
    baseURL,
    channel: process.env.E2E_BROWSER || 'msedge',
    trace: 'retain-on-failure',
  },
})
