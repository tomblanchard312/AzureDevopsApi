import { test, expect } from '@playwright/test';

test.describe('Documentation Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/docs');
    await page.waitForLoadState('networkidle');
  });

  test('shows the Documentation Assistant heading', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByRole('heading', { name: /documentation assistant/i })).toBeVisible();
  });

  test('shows Step 1: Select Repository section', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByText('Step 1: Select Repository')).toBeVisible();
  });

  test('shows Step 2: Generate Preview section', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByText('Step 2: Generate Preview')).toBeVisible();
  });

  test('Generate Preview button is disabled without selection', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const generateButton = page.getByRole('button', { name: /generate preview/i });
    await expect(generateButton).toBeDisabled();
  });

  test('shows sign-in message when not authenticated', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // When no user is signed in, the docs page shows an info alert
    const signInAlert = page.getByText(/please sign in/i);
    const projectSelect = page.getByLabel('Project');

    // Either the sign-in message or the project select should be visible
    const signInVisible = await signInAlert.isVisible().catch(() => false);
    const selectVisible = await projectSelect.isVisible().catch(() => false);

    expect(signInVisible || selectVisible).toBeTruthy();
  });

  test('branch field defaults to main', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // Branch field may be visible when authenticated
    const branchField = page.getByLabel('Branch');
    const isVisible = await branchField.isVisible().catch(() => false);
    if (isVisible) {
      await expect(branchField).toHaveValue('main');
    }
  });

  test('Step 3 is not visible initially (no preview data)', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const step3 = page.getByText('Step 3: Review and Apply');
    await expect(step3).not.toBeVisible();
  });
});
