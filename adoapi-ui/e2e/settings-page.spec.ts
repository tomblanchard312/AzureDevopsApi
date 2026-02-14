import { test, expect } from '@playwright/test';

test.describe('Settings Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('networkidle');
  });

  test('shows the Settings heading', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByRole('heading', { name: /settings/i })).toBeVisible();
  });

  test('shows API Configuration section', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByText('API Configuration')).toBeVisible();
  });

  test('shows Application Settings section', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    await expect(page.getByText('Application Settings')).toBeVisible();
  });

  test('can update API URL field', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const apiUrlField = page.getByLabel('API URL');
    await apiUrlField.clear();
    await apiUrlField.fill('https://my-api.example.com');
    await expect(apiUrlField).toHaveValue('https://my-api.example.com');
  });

  test('can toggle notification switch', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const notificationSwitch = page.getByLabel('Enable notifications');
    await expect(notificationSwitch).toBeChecked();
    await notificationSwitch.click();
    await expect(notificationSwitch).not.toBeChecked();
  });

  test('can toggle auto-save switch', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const autoSaveSwitch = page.getByLabel('Auto-save changes');
    await expect(autoSaveSwitch).not.toBeChecked();
    await autoSaveSwitch.click();
    await expect(autoSaveSwitch).toBeChecked();
  });

  test('Save Settings button is visible and clickable', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const saveButton = page.getByRole('button', { name: /save settings/i });
    await expect(saveButton).toBeVisible();
    await expect(saveButton).toBeEnabled();
  });

  test('shows success message after saving settings', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const saveButton = page.getByRole('button', { name: /save settings/i });
    await saveButton.click();

    await expect(page.getByText('Settings saved successfully!')).toBeVisible();
  });

  test('settings persist in localStorage after save', async ({ page }) => {
    const configMessage = page.getByText('Configuration Required');
    const isConfigPage = await configMessage.isVisible().catch(() => false);

    if (isConfigPage) {
      test.skip();
      return;
    }

    const apiUrlField = page.getByLabel('API URL');
    await apiUrlField.clear();
    await apiUrlField.fill('https://test-api.example.com');

    const saveButton = page.getByRole('button', { name: /save settings/i });
    await saveButton.click();

    // Verify localStorage was updated
    const storedSettings = await page.evaluate(() => localStorage.getItem('app_settings'));
    expect(storedSettings).toBeTruthy();
    const parsed = JSON.parse(storedSettings!);
    expect(parsed.apiUrl).toBe('https://test-api.example.com');
  });
});
