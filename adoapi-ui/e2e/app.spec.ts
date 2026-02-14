import { test, expect } from '@playwright/test';

test.describe('Application Shell', () => {
  test('should load the application', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/AzureDevopsApi/i);
  });

  test('should show auth configuration message or redirect to /docs', async ({ page }) => {
    await page.goto('/');

    // The app either shows the "Configuration Required" screen (no env vars)
    // or redirects to /docs after auth
    const configMessage = page.getByText('Configuration Required');
    const docsHeading = page.getByText('Documentation Assistant');

    await expect(configMessage.or(docsHeading)).toBeVisible({ timeout: 10_000 });
  });

  test('should display the app bar with correct title', async ({ page }) => {
    await page.goto('/docs');
    // Wait for either the config screen or the app layout
    const appTitle = page.getByText('AzureDevopsApi Docs Assistant');
    const configMessage = page.getByText('Configuration Required');

    const visible = await configMessage.isVisible().catch(() => false);
    if (!visible) {
      await expect(appTitle).toBeVisible();
    }
  });
});
