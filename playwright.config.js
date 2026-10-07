const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  use: {
    baseURL: process.env.SKILLBRIDGE_BASE_URL || 'https://localhost:44364',
    ignoreHTTPSErrors: true,
    ...devices['Desktop Chrome'],
  },
  reporter: 'list',
});
