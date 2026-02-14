import { test, expect } from '@playwright/test';

// These tests validate navigation when the app is loaded with auth bypassed
// or when running without MSAL configuration (shows config screen).
// Use the auth fixture (see auth.setup.ts) for authenticated tests.

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    // Wait for auth resolution
    await page.waitForLoadState('networkidle');
  });

  test('root path redirects to /docs', async ({ page }) => {
    // If auth is configured and passed, we should land on /docs
    // If not configured, we'll see the config page
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (!isConfigPage) {
      await expect(page).toHaveURL(/\/docs/);
    }
  });

  test('navigation drawer contains Documentation and Settings links', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // Desktop: drawer is permanently visible
    const docsLink = page.getByRole('link', { name: /documentation/i });
    const settingsLink = page.getByRole('link', { name: /settings/i });

    await expect(docsLink).toBeVisible();
    await expect(settingsLink).toBeVisible();
  });

  test('can navigate to Settings page', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await page.getByRole('link', { name: /settings/i }).click();
    await expect(page).toHaveURL(/\/settings/);
    await expect(page.getByRole('heading', { name: /settings/i })).toBeVisible();
  });

  test('can navigate back to Docs page from Settings', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await page.getByRole('link', { name: /settings/i }).click();
    await expect(page).toHaveURL(/\/settings/);

    await page.getByRole('link', { name: /documentation/i }).click();
    await expect(page).toHaveURL(/\/docs/);
  });

  test('unknown routes show no content but keep layout', async ({ page }) => {
    await page.goto('/nonexistent-route');
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // Layout should still be present (app bar)
    const appTitle = page.getByText('AzureDevopsApi Docs Assistant');
    await expect(appTitle).toBeVisible();
  });
});

test.describe('Mobile Navigation', () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test('mobile drawer toggle works', async ({ page }) => {
    await page.goto('/docs');
    await page.waitForLoadState('networkidle');

    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // On mobile, the drawer is hidden by default. Click hamburger menu.
    const menuButton = page.getByLabel('open drawer');
    await expect(menuButton).toBeVisible();
    await menuButton.click();

    // After clicking, the drawer should show navigation items
    const docsLink = page.getByRole('link', { name: /documentation/i });
    await expect(docsLink).toBeVisible();
  });
});
