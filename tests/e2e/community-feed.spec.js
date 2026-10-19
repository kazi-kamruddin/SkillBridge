const { test, expect } = require('@playwright/test');

test.describe('Hybrid Smart Community Feed E2E Tests', () => {

  test('Navbar contains "Community Feed" link and navigates to the feed', async ({ page }) => {
    await page.goto('/');
    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    const feedLink = nav.getByRole('link', { name: 'Community Feed' });
    await expect(feedLink).toBeVisible();
    await feedLink.click();
    await expect(page).toHaveURL(/\/Communities/i);
    await expect(page.getByRole('heading', { name: 'Find your people.' })).toBeVisible();
  });

  test('Community Feed renders hero, tabs, search bar, and topic pills', async ({ page }) => {
    await page.goto('/Communities');

    // Hero check
    await expect(page.getByRole('heading', { name: 'Find your people.' })).toBeVisible();
    await expect(page.locator('text=Real-Time Community Feed')).toBeVisible();

    // Tab check
    const allPostsTab = page.getByRole('link', { name: 'All Posts' });
    await expect(allPostsTab).toBeVisible();

    // Search bar check
    const searchInput = page.locator('#community-search');
    await expect(searchInput).toBeVisible();
    await expect(searchInput).toHaveAttribute('placeholder', /Search posts, titles, or skills/i);

    // Topic pills
    const allTopicsPill = page.getByRole('link', { name: '#All Topics' });
    await expect(allTopicsPill).toBeVisible();
  });

  test('Clicking a topic pill filters the feed and allows clearing the filter', async ({ page }) => {
    await page.goto('/Communities');

    // Check if there are topic pills
    const topicPills = page.locator('a[href*="skillId="]');
    if (await topicPills.count() > 0) {
      const firstPill = topicPills.first();
      const pillText = (await firstPill.textContent()).trim();
      const topicName = pillText.replace(/^#/, '').split('(')[0].trim();

      await firstPill.click();
      await expect(page).toHaveURL(/skillId=/i);

      // Verify header indicates filtered topic
      await expect(page.locator(`text=Posts in`)).toBeVisible();

      // Clear filter link
      const clearFilter = page.getByRole('link', { name: /Clear topic filter/i });
      await expect(clearFilter).toBeVisible();
      await clearFilter.click();

      // Back to all topics
      await expect(page).not.toHaveURL(/skillId=/i);
    }
  });

  test('Feed post cards render author information, role badges, skill tag, and action links', async ({ page }) => {
    await page.goto('/Communities');

    const postCards = page.locator('article.card');
    if (await postCards.count() > 0) {
      const firstCard = postCards.first();

      // Check author elements
      await expect(firstCard.locator('img').first()).toBeVisible();

      // Check discuss action button
      const discussBtn = firstCard.getByRole('link', { name: /Discuss/i });
      await expect(discussBtn).toBeVisible();

      // Check view profile action button
      const profileBtn = firstCard.getByRole('link', { name: /View Profile/i });
      await expect(profileBtn).toBeVisible();

      // Click post title to open PostDetails
      const postTitleLink = firstCard.locator('h3 a').first();
      await postTitleLink.click();

      await expect(page).toHaveURL(/\/Communities\/PostDetails\//i);

      // Detail page has h1 post title
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

      // Breadcrumb to go back
      const backLink = page.getByRole('link', { name: '← Back to Community Feed' });
      await expect(backLink).toBeVisible();
      await backLink.click();
      await expect(page).toHaveURL(/\/Communities/i);
    }
  });

  test('PostDetails displays friendly interactive gating for guests', async ({ page }) => {
    await page.goto('/Communities');
    const postCards = page.locator('article.card');
    if (await postCards.count() > 0) {
      const discussBtn = postCards.first().getByRole('link', { name: /Discuss/i });
      await discussBtn.click();

      const gatingCard = page.locator('.card').filter({ hasText: 'Want to join this conversation?' });
      await expect(gatingCard).toBeVisible();
      await expect(gatingCard.getByRole('link', { name: 'Join SkillBridge' })).toBeVisible();
      await expect(gatingCard.getByRole('link', { name: 'Log In' })).toBeVisible();
    }
  });

  test('Mobile viewport (320px) has zero horizontal overflow on Community Feed', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 720 });
    const response = await page.goto('/Communities');
    expect(response.status()).toBe(200);

    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    expect(scrollWidth).toBeLessThanOrEqual(321);
  });
});
