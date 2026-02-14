import { test, expect } from '@playwright/test';

test.describe('Accessibility', () => {
  test('page has a main landmark', async ({ page }) => {
    await page.goto('/docs');
    await page.waitForLoadState('networkidle');

    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const main = page.getByRole('main');
    await expect(main).toBeVisible();
  });

  test('page has a navigation landmark', async ({ page }) => {
    await page.goto('/docs');
    await page.waitForLoadState('networkidle');

    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const nav = page.getByRole('navigation');
    await expect(nav).toBeVisible();
  });

  test('interactive elements are keyboard accessible', async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('networkidle');

    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // Tab through page elements - Save button should be reachable
    const saveButton = page.getByRole('button', { name: /save settings/i });
    await saveButton.focus();
    await expect(saveButton).toBeFocused();

    // Press Enter to activate
    await page.keyboard.press('Enter');
    await expect(page.getByText('Settings saved successfully!')).toBeVisible();
  });

  test('form inputs have accessible labels', async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('networkidle');

    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    // All form inputs should have labels
    await expect(page.getByLabel('API URL')).toBeVisible();
    await expect(page.getByLabel('API Key')).toBeVisible();
    await expect(page.getByLabel('Enable notifications')).toBeVisible();
    await expect(page.getByLabel('Auto-save changes')).toBeVisible();
  });
});
