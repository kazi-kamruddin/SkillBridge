const { test, expect } = require('@playwright/test');

test.describe('SkillBridge Complete End-to-End User Experience Simulation', () => {

  test('Journey 1: Discovery & Catalog Exploration', async ({ page }) => {
    // 1. Visitor lands on Homepage
    await page.goto('/');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Teach what you know.');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Learn what you love.');

    // Verify key chromatic feature cards are visible
    await expect(page.getByRole('heading', { name: 'Join communities' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Trade skills' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Grow together' })).toBeVisible();

    // 2. Theme toggle test
    const themeBtn = page.locator('#themeToggle');
    await expect(themeBtn).toBeVisible();
    await themeBtn.click();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    await themeBtn.click();
    await expect(page.locator('html')).not.toHaveAttribute('data-theme', 'dark');

    // 3. Navigation to Explore and Skills Catalog
    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    const exploreLink = nav.getByRole('link', { name: 'Explore' });
    await expect(exploreLink).toBeVisible();
    await exploreLink.click();
    await expect(page).toHaveURL(/\/Explore/i);

    // Browse Skills Catalog
    await page.goto('/Explore/Skills');
    await expect(page.getByRole('heading', { name: 'Explore skills.' })).toBeVisible();

    // 4. Explore skills list and navigate to detail page
    const skillCards = page.locator('a[href*="/Explore/Detail/"]');
    expect(await skillCards.count()).toBeGreaterThan(0);
    const firstSkill = skillCards.first();
    const skillName = (await firstSkill.innerText()).trim();

    await firstSkill.click();
    await expect(page).toHaveURL(/\/Explore\/Detail\//i);

    // 5. Verify skill detail page elements
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Learning stages' })).toBeVisible();
    const visitCommunityBtn = page.getByRole('link', { name: 'Visit community' });
    await expect(visitCommunityBtn).toBeVisible();
  });

  test('Journey 2: Community Feed Exploration, Filtering & Search', async ({ page }) => {
    // 1. Visit Community Feed via navbar
    await page.goto('/');
    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    const communityFeedLink = nav.getByRole('link', { name: 'Community Feed' });
    await expect(communityFeedLink).toBeVisible();
    await communityFeedLink.click();

    await expect(page).toHaveURL(/\/Communities/i);
    await expect(page.getByRole('heading', { name: 'Find your people.' })).toBeVisible();

    // 2. Feed tab switching
    const allPostsTab = page.getByRole('link', { name: 'All Posts' });
    const mySkillsTab = page.getByRole('link', { name: 'My Skills' });
    await expect(allPostsTab).toBeVisible();
    await expect(mySkillsTab).toBeVisible();

    // Test clicking My Skills tab as guest
    await mySkillsTab.click();
    await expect(page).toHaveURL(/tab=my-skills/i);
    // Guest gets friendly guidance callout
    await expect(page.locator('text=Log in to follow discussions tailored to your teaching skills')).toBeVisible();

    // Switch back to All Posts
    await allPostsTab.click();
    await expect(page).not.toHaveURL(/tab=my-skills/i);

    // 3. Topic filter pills
    const allTopicsPill = page.getByRole('link', { name: '#All Topics' });
    await expect(allTopicsPill).toBeVisible();

    const topicPills = page.locator('a[href*="skillId="]');
    if (await topicPills.count() > 0) {
      const targetPill = topicPills.first();
      await targetPill.click();
      await expect(page).toHaveURL(/skillId=/i);
      await expect(page.locator('text=Posts in')).toBeVisible();

      // Clear the filter
      const clearBtn = page.getByRole('link', { name: /Clear topic filter/i });
      await expect(clearBtn).toBeVisible();
      await clearBtn.click();
      await expect(page).not.toHaveURL(/skillId=/i);
    }

    // 4. Feed search functionality
    const searchInput = page.locator('#community-search');
    await expect(searchInput).toBeVisible();
    await searchInput.fill('Skill');
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/[?&]q=Skill/i);

    // 5. Expandable category archive
    const archiveDetails = page.locator('details.sb-community-category');
    if (await archiveDetails.count() > 0) {
      const firstArchive = archiveDetails.first();
      await expect(firstArchive).toBeVisible();
      const communityCard = firstArchive.locator('a[href*="/Communities/Landing/"]').first();
      await expect(communityCard).toBeVisible();
    }
  });

  test('Journey 3: Post Details, Breadcrumb & Interactive Guest Gating', async ({ page }) => {
    await page.goto('/Communities');

    const postCards = page.locator('article.card');
    if (await postCards.count() > 0) {
      const firstCard = postCards.first();
      const discussBtn = firstCard.getByRole('link', { name: /Discuss/i });
      await expect(discussBtn).toBeVisible();

      // Click to open post details
      await discussBtn.click();
      await expect(page).toHaveURL(/\/Communities\/PostDetails\//i);

      // Verify post heading
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

      // Verify breadcrumb back navigation
      const breadcrumb = page.getByRole('link', { name: '← Back to Community Feed' });
      await expect(breadcrumb).toBeVisible();

      // Verify guest commenting gating card
      const gatingCard = page.locator('.card').filter({ hasText: 'Want to join this conversation?' });
      await expect(gatingCard).toBeVisible();
      const joinLink = gatingCard.getByRole('link', { name: 'Join SkillBridge' });
      const loginLink = gatingCard.getByRole('link', { name: 'Log In' });
      await expect(joinLink).toBeVisible();
      await expect(loginLink).toBeVisible();

      // Verify breadcrumb returns cleanly to the feed
      await breadcrumb.click();
      await expect(page).toHaveURL(/\/Communities/i);
    }
  });

  test('Journey 4: Informational Pages, About Team Carousel & Contact Reset', async ({ page }) => {
    // 1. How It Works
    await page.goto('/Home/HowItWorks');
    await expect(page.getByRole('heading', { name: /Teach one thing.*Learn another/i })).toBeVisible();
    const browseSkillsCta = page.getByRole('link', { name: 'Browse skills' });
    await expect(browseSkillsCta).toBeVisible();

    // 2. About page with interactive developer carousel
    await page.goto('/Home/About');
    await expect(page.getByRole('heading', { name: /Skills are better/i })).toBeVisible();
    const teamCards = page.locator('.sb-team-card');
    await expect(teamCards).toHaveCount(3);
    await teamCards.nth(1).click();
    await expect(teamCards.nth(1)).toHaveAttribute('aria-pressed', 'true');
    await expect(page.locator('.sb-team-counter')).toHaveText('02 / 03');

    // 3. Contact page with password reset link
    await page.goto('/Home/Contact');
    await expect(page.getByRole('heading', { name: "Let's sort it out." })).toBeVisible();
    const resetLink = page.getByRole('link', { name: 'Request a reset link' });
    await expect(resetLink).toBeVisible();
    await resetLink.click();
    await expect(page).toHaveURL(/\/Account\/ForgotPassword/i);
    await expect(page.getByRole('heading', { name: /Forgot your password/i })).toBeVisible();
  });

  test('Journey 5: End-to-End Responsive Simulation (320px, 390px, 1280px)', async ({ page }) => {
    const viewports = [
      { name: 'Ultra-Compact Phone', width: 320, height: 640 },
      { name: 'Standard Phone', width: 390, height: 844 },
      { name: 'Desktop Workstation', width: 1280, height: 800 }
    ];

    const criticalRoutes = [
      '/',
      '/Explore/Skills',
      '/Communities',
      '/Home/HowItWorks',
      '/Home/About',
      '/Home/Contact',
      '/Account/Login',
      '/Account/Register'
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });

      for (const route of criticalRoutes) {
        const response = await page.goto(route);
        expect(response.status(), `Route ${route} at ${vp.name}`).toBe(200);

        // Verify zero horizontal scroll overflow
        const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(scrollWidth, `${route} overflows at ${vp.name} (${vp.width}px)`).toBeLessThanOrEqual(vp.width + 1);
      }
    }
  });

});
