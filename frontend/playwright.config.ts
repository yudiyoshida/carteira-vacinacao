import { defineConfig, devices } from '@playwright/test';

// No webServer: the smoke runs against the Compose already up (TechSpec §11.4).
export default defineConfig({
  testDir: './e2e',
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
