const { test, expect } = require('@playwright/test');

test('guest can open the home, login, and registration pages', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Bridge Your Skills, Build Connections' })).toBeVisible();

  await page.goto('/Account/Login');
  await expect(page.getByRole('heading', { name: 'Sign In' })).toBeVisible();
  await expect(page.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);

  await page.goto('/Account/Register');
  await expect(page.getByRole('heading', { name: 'Register' })).toBeVisible();
  await expect(page.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);
});

test('guest is redirected to login from protected pages', async ({ page }) => {
  for (const path of ['/Explore', '/Interactions', '/Notifications', '/Messages']) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/Account\/Login/i);
  }
});
