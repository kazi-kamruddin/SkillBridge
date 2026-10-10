const { test, expect } = require('@playwright/test');

test('guest can open the home, login, and registration pages', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /Teach what you know.*Learn what you love/i })).toBeVisible();

  await page.goto('/Account/Login');
  await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();
  await expect(page.locator('form[role="form"] input[name="__RequestVerificationToken"]')).toHaveCount(1);

  await page.goto('/Account/Register');
  await expect(page.getByRole('heading', { name: 'Join the exchange.' })).toBeVisible();
  await expect(page.locator('form[role="form"] input[name="__RequestVerificationToken"]')).toHaveCount(1);
});

test('guest is redirected to login from protected pages', async ({ page }) => {
  for (const path of ['/Interactions', '/Interactions/History', '/Notifications', '/Messages', '/Profile', '/Profile/Requests', '/Moderation']) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/Account\/Login/i);
  }
});

test('guest can follow a skill into its detail and community pages', async ({ page }) => {
  await page.goto('/Explore/Skills');
  await expect(page.getByRole('heading', { name: 'Explore skills.' })).toBeVisible();
  const skillLink = page.locator('a[href*="/Explore/Detail/"]').first();
  await expect(skillLink).toBeVisible();
  await skillLink.click();
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Learning stages' })).toBeVisible();
  await page.getByRole('link', { name: 'Visit community' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

  await page.goto('/Communities');
  await expect(page.getByRole('heading', { name: 'Find your people.' })).toBeVisible();
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

test('floating navigation centers the brand and opens on mobile', async ({ page }) => {
  await page.goto('/');
  const navigation = page.getByRole('navigation', { name: 'Main navigation' });
  const navBox = await navigation.boundingBox();
  const brandBox = await navigation.getByRole('link', { name: 'SkillBridge home' }).boundingBox();
  expect(navBox).not.toBeNull();
  expect(brandBox).not.toBeNull();
  expect(Math.abs((brandBox.x + brandBox.width / 2) - (navBox.x + navBox.width / 2))).toBeLessThan(3);
  await expect(navigation).toHaveCSS('position', 'fixed');

  await page.setViewportSize({ width: 390, height: 844 });
  const menu = navigation.getByRole('button', { name: 'Toggle navigation' });
  await expect(menu).toBeVisible();
  await menu.click();
  await expect(navigation.getByRole('link', { name: 'Explore' })).toBeVisible();
});

test('public help pages provide working destinations', async ({ page }) => {
  await page.goto('/Home/Contact');
  await expect(page.getByRole('heading', { name: "Let's sort it out." })).toBeVisible();
  await page.getByRole('link', { name: 'Request a reset link' }).click();
  await expect(page.getByRole('heading', { name: /Forgot your password/i })).toBeVisible();
  await expect(page.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);
});

test('guest can read the exchange guide', async ({ page }) => {
  await page.goto('/Home/HowItWorks');
  await expect(page.getByRole('heading', { name: /Teach one thing.*Learn another/i })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Browse skills' })).toHaveAttribute('href', /Explore\/Skills/i);
});

test('about page lets visitors browse the team on desktop and mobile', async ({ page }) => {
  await page.goto('/Home/About');
  const portraits = page.locator('.sb-team-card');
  await expect(portraits).toHaveCount(3);
  await expect(portraits.nth(0)).toHaveAttribute('aria-pressed', 'true');
  await portraits.nth(1).click();
  await expect(portraits.nth(1)).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('.sb-team-counter')).toHaveText('02 / 03');
  await page.getByRole('button', { name: 'Next developer' }).click();
  await expect(portraits.nth(2)).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Our mission' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Our vision' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Our history' })).toBeVisible();
  await expect(page.locator('.sb-supervisor-card')).toHaveCount(2);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('button', { name: 'Kazi', exact: true }).click();
  await expect(portraits.nth(1)).toHaveAttribute('aria-pressed', 'true');
  expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(0);
});
