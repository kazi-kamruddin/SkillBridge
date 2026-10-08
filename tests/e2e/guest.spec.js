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
  for (const path of ['/Interactions', '/Notifications', '/Messages', '/Profile', '/Moderation']) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/Account\/Login/i);
  }
});

test('guest navigation offers browsing and keeps private areas out of view', async ({ page }) => {
  await page.goto('/');
  const navigation = page.locator('nav.navbar');
  await expect(navigation.getByRole('link', { name: 'Explore' })).toBeVisible();
  await expect(navigation.getByRole('link', { name: 'Communities' })).toBeVisible();
  await expect(navigation.getByRole('link', { name: 'Interactions' })).toHaveCount(0);
  await expect(navigation.getByRole('link', { name: 'Messages' })).toHaveCount(0);
  await expect(navigation.getByRole('link', { name: 'Reports' })).toHaveCount(0);
});

test('public help pages provide working destinations', async ({ page }) => {
  await page.goto('/Home/Contact');
  await expect(page.getByRole('heading', { name: 'Contact SkillBridge' })).toBeVisible();
  await page.getByRole('link', { name: 'Request a reset link' }).click();
  await expect(page.getByRole('heading', { name: /Forgot your password/i })).toBeVisible();
  await expect(page.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);
});

test('guest can read the exchange guide', async ({ page }) => {
  await page.goto('/Home/HowItWorks');
  await expect(page.getByRole('heading', { name: 'How an exchange works' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Browse skills' })).toHaveAttribute('href', /Explore\/Skills/i);
});
