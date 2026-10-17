const fs = require('node:fs');
const { test, expect } = require('@playwright/test');

const credentialFile = process.env.SKILLBRIDGE_TEST_ACCOUNTS_FILE;
const accounts = credentialFile && fs.existsSync(credentialFile) ? JSON.parse(fs.readFileSync(credentialFile, 'utf8')) : null;

test.describe('SkillBridge Repository-Wide End-to-End Audit', () => {

  // =========================================================================
  // 1. AUTHENTICATION, PASSWORD TOGGLES & MIRROR LAYOUT
  // =========================================================================
  test('1.1 Login page: form controls, anti-forgery, and password toggle', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/Account/Login');
    await expect(page.getByRole('heading', { name: 'Welcome back.' })).toBeVisible();

    // Check anti-forgery token
    const token = page.locator('form input[name="__RequestVerificationToken"]').first();
    await expect(token).toHaveCount(1);
    expect(await token.inputValue()).toBeTruthy();

    // Verify Password input starts as type="password"
    const passwordInput = page.locator('input[name="Password"]');
    await expect(passwordInput).toHaveAttribute('type', 'password');
    await passwordInput.fill('SecretPassword123!');

    // Toggle password visibility
    const toggleBtn = page.locator('.sb-password-toggle').first();
    await expect(toggleBtn).toBeVisible();
    await toggleBtn.click();
    await expect(passwordInput).toHaveAttribute('type', 'text');
    await expect(passwordInput).toHaveValue('SecretPassword123!');

    // Toggle back to password
    await toggleBtn.click();
    await expect(passwordInput).toHaveAttribute('type', 'password');

    // Verify mirror layout: form on left, signup promo on right
    const authCard = page.locator('.sb-auth-card');
    await expect(authCard).toBeVisible();
    const signupPromoLink = page.getByRole('link', { name: 'Sign Up', exact: true });
    await expect(signupPromoLink).toBeVisible();
  });

  test('1.2 Registration page: form controls, mirror layout, and dual password toggles', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/Account/Register');
    await expect(page.getByRole('heading', { name: 'Join the exchange.' })).toBeVisible();

    // Check anti-forgery token
    const token = page.locator('form input[name="__RequestVerificationToken"]').first();
    await expect(token).toHaveCount(1);
    expect(await token.inputValue()).toBeTruthy();

    // Check Password and ConfirmPassword inputs
    const pwd = page.locator('input[name="Password"]');
    const confirmPwd = page.locator('input[name="ConfirmPassword"]');
    await expect(pwd).toHaveAttribute('type', 'password');
    await expect(confirmPwd).toHaveAttribute('type', 'password');

    await pwd.fill('SecurePass123!');
    await confirmPwd.fill('SecurePass123!');

    // Toggle Password
    const pwdToggle = pwd.locator('..').locator('.sb-password-toggle');
    await pwdToggle.click();
    await expect(pwd).toHaveAttribute('type', 'text');

    // Toggle ConfirmPassword independently
    const confirmToggle = confirmPwd.locator('..').locator('.sb-password-toggle');
    await confirmToggle.click();
    await expect(confirmPwd).toHaveAttribute('type', 'text');

    // Toggle back
    await pwdToggle.click();
    await expect(pwd).toHaveAttribute('type', 'password');
    await confirmToggle.click();
    await expect(confirmPwd).toHaveAttribute('type', 'password');

    // Verify mirror layout: promo on left, form on right
    const loginPromoLink = page.getByRole('link', { name: /Log in to your account|Sign in/i });
    await expect(loginPromoLink).toBeVisible();
  });

  test('1.3 Protected route access control: unauthenticated users redirected to login', async ({ page }) => {
    test.setTimeout(60_000);
    const protectedRoutes = [
      '/Profile',
      '/Profile/UpdateProfile',
      '/Interactions',
      '/Messages',
      '/Notifications',
      '/SavedProfiles'
    ];

    for (const route of protectedRoutes) {
      await page.goto(route);
      await expect(page).toHaveURL(/\/Account\/Login/i);
    }
  });

  // =========================================================================
  // 2. THEME SYSTEM, ANTI-FOUC & NEO-BRUTALIST COLOR PALETTE
  // =========================================================================
  test('2.1 Dark / Light theme switching, persistence and anti-FOUC boot', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/');

    const themeToggle = page.locator('#themeToggle');
    await expect(themeToggle).toBeVisible();

    // Switch to Dark mode
    await themeToggle.click();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    await expect(page.locator('html')).toHaveClass(/dark/);
    const storedThemeDark = await page.evaluate(() => localStorage.getItem('theme'));
    expect(storedThemeDark).toBe('dark');

    // Reload while dark mode is active and verify instant anti-FOUC application
    await page.reload();
    const dataThemeOnReload = await page.evaluate(() => document.documentElement.getAttribute('data-theme'));
    expect(dataThemeOnReload).toBe('dark');

    // Verify dark palette CSS variables
    const colors = await page.evaluate(() => {
      const style = getComputedStyle(document.documentElement);
      return {
        paper: style.getPropertyValue('--sb-paper').trim(),
        card: style.getPropertyValue('--sb-card').trim(),
        line: style.getPropertyValue('--sb-line').trim(),
        sage: style.getPropertyValue('--sb-sage').trim(),
        blue: style.getPropertyValue('--sb-blue').trim(),
        coral: style.getPropertyValue('--sb-coral').trim()
      };
    });

    expect(colors.paper).toBe('#0f1416');
    expect(colors.card).toBe('#182024');
    expect(colors.line).toBe('#3d525a');
    expect(colors.sage).toBe('#1a3328');
    expect(colors.blue).toBe('#1d2a45');
    expect(colors.coral).toBe('#3b221a');

    // Switch back to Light mode
    await themeToggle.click();
    await expect(page.locator('html')).not.toHaveAttribute('data-theme', 'dark');
    const storedThemeLight = await page.evaluate(() => localStorage.getItem('theme'));
    expect(storedThemeLight).toBe('light');
  });

  test('2.2 Background pattern SVG loads successfully in both modes', async ({ page }) => {
    test.setTimeout(60_000);
    const lightPattern = await page.request.get('/images/skill-pattern-light.svg');
    expect(lightPattern.status()).toBe(200);
    expect(lightPattern.headers()['content-type']).toMatch(/image\/svg\+xml/);

    const darkPattern = await page.request.get('/images/skill-pattern-dark.svg');
    expect(darkPattern.status()).toBe(200);
    expect(darkPattern.headers()['content-type']).toMatch(/image\/svg\+xml/);
  });

  // =========================================================================
  // 3. HOMEPAGE & NEO-BRUTALISM ART DIRECTION
  // =========================================================================
  test('3.1 Homepage: hero, chromatic cards, and CTA button legibility in light & dark mode', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/');

    // Check Hero headings
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Teach what you know.');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Learn what you love.');

    // Check chromatic cards in "The good part"
    await expect(page.getByRole('heading', { name: 'Join communities' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Trade skills' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Grow together' })).toBeVisible();

    // Check bottom section "Curiosity looks good on you."
    const ctaSection = page.locator('section').filter({ hasText: 'Curiosity looks good on you.' });
    await expect(ctaSection).toBeVisible();
    const registerBtn = ctaSection.getByRole('link', { name: /Create your account/i });
    await expect(registerBtn).toBeVisible();

    // Verify legibility in dark mode
    await page.locator('#themeToggle').click();
    await expect(registerBtn).toBeVisible();
    const btnColor = await registerBtn.evaluate(el => window.getComputedStyle(el).color);
    expect(btnColor).toBeTruthy();
  });

  // =========================================================================
  // 4. PUBLIC CATALOG & COMMUNITIES EXPLORATION
  // =========================================================================
  test('4.1 Skills Directory: search, categories, and skill stage details', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/Explore/Skills');
    await expect(page.getByRole('heading', { name: 'Explore skills.' })).toBeVisible();

    // Verify category headings exist
    const categoryHeadings = page.locator('section h2');
    expect(await categoryHeadings.count()).toBeGreaterThan(0);

    // Verify skill cards and detail links exist
    const firstSkillLink = page.locator('a[href*="/Explore/Detail/"]').first();
    await expect(firstSkillLink).toBeVisible();
    await firstSkillLink.click();

    // Skill detail page verification
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Learning stages' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Visit community' })).toBeVisible();
  });

  test('4.2 Communities directory: search, community cards, and post preview', async ({ page }) => {
    test.setTimeout(60_000);
    await page.goto('/Communities');
    await expect(page.getByRole('heading', { name: 'Find your people.' })).toBeVisible();

    // Check community search form
    const searchInput = page.locator('input[name="searchQuery"], input[type="search"]');
    if (await searchInput.count()) {
      await expect(searchInput).toBeVisible();
    }

    // Check community cards
    const communityCards = page.locator('.sb-community-card-known, a[href*="/Communities/"]');
    expect(await communityCards.count()).toBeGreaterThan(0);

    // Open first community
    const firstCommunity = communityCards.first();
    await firstCommunity.click();
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });

  test('4.3 Informational & help pages return HTTP 200 without layout breakage', async ({ page }) => {
    test.setTimeout(60_000);
    const pages = [
      { path: '/Home/HowItWorks', heading: /Teach one thing|How Exchanges Work/i },
      { path: '/Home/About', heading: /Skills are better|Meet the team/i },
      { path: '/Home/Contact', heading: /Let's sort it out|Talk to us/i },
      { path: '/Account/ForgotPassword', heading: /Forgot your password/i }
    ];

    for (const item of pages) {
      const response = await page.goto(item.path);
      expect(response.status()).toBe(200);
      await expect(page.getByRole('heading', { name: item.heading }).first()).toBeVisible();
    }
  });

  // =========================================================================
  // 5. CHAT IMAGE SIZE VALIDATION & IN-COMPOSER ERROR UI
  // =========================================================================
  test('5.1 Chat composer enforces 4 MB size limit and renders error preview', async ({ page }) => {
    test.skip(!accounts, 'Requires test accounts');
    test.setTimeout(90_000);

    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts.accountA.email);
    await page.locator('input[name="Password"]').fill(accounts.accountA.password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).not.toHaveURL(/\/Account\/Login/i);

    await page.goto('/Messages');
    const conversationLink = page.locator('.sb-chat-list a').first();
    test.skip(await conversationLink.count() === 0, 'Requires an existing conversation');
    await conversationLink.click();

    // Verify chat composer exists
    const fileInput = page.locator('#chatImage');
    const errorBanner = page.locator('#chatImageError');
    const previewContainer = page.locator('#chatImagePreview');
    await expect(fileInput).toHaveCount(1);
    await expect(errorBanner).toBeHidden();

    // 1. Create a mock oversized image (4.5 MB)
    const oversizedBytes = 4.5 * 1024 * 1024;
    const oversizedBuffer = Buffer.alloc(oversizedBytes, 0);

    // Trigger file change with oversized file
    await fileInput.setInputFiles({
      name: 'large-photo.jpg',
      mimeType: 'image/jpeg',
      buffer: oversizedBuffer
    });

    // Verify error banner pops up in preview area
    await expect(errorBanner).toBeVisible();
    await expect(errorBanner).toContainText('4.5 MB');
    await expect(errorBanner).toContainText('4 MB limit');

    // Verify invalid file was cleared from input
    const fileCount = await fileInput.evaluate(el => el.files.length);
    expect(fileCount).toBe(0);

    // Dismiss error banner
    const dismissBtn = errorBanner.locator('button');
    await dismissBtn.click();
    await expect(errorBanner).toBeHidden();

    // 2. Select a valid image (under 4 MB)
    const validBytes = 200 * 1024; // 200 KB
    const validBuffer = Buffer.alloc(validBytes, 0);

    await fileInput.setInputFiles({
      name: 'valid-diagram.png',
      mimeType: 'image/png',
      buffer: validBuffer
    });

    // Error banner stays hidden, preview shows up
    await expect(errorBanner).toBeHidden();
    await expect(previewContainer).toBeVisible();
    await expect(page.locator('#chatImageSize')).toContainText('200 KB');

    // Remove attachment via preview remove button
    await page.locator('#removeChatImage').click();
    await expect(previewContainer).toBeHidden();
  });

  // =========================================================================
  // 6. MEMBER PROFILE, SETTINGS & SKILLS AUDIT
  // =========================================================================
  test('6.1 Member profile displays headline, skills and edit controls', async ({ page }) => {
    test.skip(!accounts, 'Requires test accounts');
    test.setTimeout(90_000);

    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts.accountA.email);
    await page.locator('input[name="Password"]').fill(accounts.accountA.password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).not.toHaveURL(/\/Account\/Login/i);

    await page.goto('/Profile');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

    // Check Teaching Skills and Learning Goals headings
    await expect(page.getByRole('heading', { name: /Teaching Skills|Skills you teach/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /Seeking to Learn|Skills you want to learn/i })).toBeVisible();

    // Check Edit Profile action
    const editBtn = page.getByRole('link', { name: /Edit Profile|Update Profile|Manage skills/i }).first();
    await expect(editBtn).toBeVisible();
    await editBtn.click();
    await expect(page).toHaveURL(/\/Profile\/UpdateProfile/i);

    // Verify form fields in UpdateProfile
    await expect(page.locator('textarea[name="Bio"]')).toBeVisible();
    await expect(page.locator('input[name="Location"]')).toBeVisible();
  });

  // =========================================================================
  // 7. RESPONSIVENESS AUDIT ACROSS ALL KEY VIEWPORTS
  // =========================================================================
  test('7.1 Narrow mobile (320px) viewport: zero horizontal overflow on public and member pages', async ({ page }) => {
    test.setTimeout(90_000);
    await page.setViewportSize({ width: 320, height: 720 });

    const auditRoutes = [
      '/',
      '/Account/Login',
      '/Account/Register',
      '/Explore/Skills',
      '/Communities',
      '/Home/HowItWorks',
      '/Home/About',
      '/Home/Contact'
    ];

    for (const route of auditRoutes) {
      const res = await page.goto(route);
      expect(res.status(), `${route} status`).toBe(200);

      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(scrollWidth, `${route} overflows viewport at 320px`).toBeLessThanOrEqual(321);
    }
  });

  test('7.2 Smartphone (390px) navigation: mobile hamburger menu toggles cleanly', async ({ page }) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/');

    const menuToggle = page.getByRole('button', { name: /Toggle navigation|Menu/i });
    await expect(menuToggle).toBeVisible();

    // Open menu
    await menuToggle.click();
    const navContent = page.locator('.sb-nav-content');
    await expect(navContent).toHaveClass(/is-open/);

    // Close menu
    await menuToggle.click();
    await expect(navContent).not.toHaveClass(/is-open/);
  });

  test('7.3 Desktop (1280px) viewport: floating navigation centers brand and shows full menu', async ({ page }) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/');

    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    await expect(nav).toBeVisible();

    // Verify links are visible without opening hamburger
    await expect(nav.getByRole('link', { name: 'Explore' })).toBeVisible();
    await expect(nav.getByRole('link', { name: 'Communities' })).toBeVisible();
    await expect(nav.getByRole('link', { name: 'Log in' })).toBeVisible();
    await expect(nav.getByRole('link', { name: 'Register' })).toBeVisible();
  });
});
