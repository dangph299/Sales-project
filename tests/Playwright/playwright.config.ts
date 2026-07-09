import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './specs',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: process.env.SALES_API_URL ?? 'http://localhost:5000',
    extraHTTPHeaders: { Accept: 'application/json' },
    trace: 'retain-on-failure'
  },
  outputDir: 'test-results'
});
