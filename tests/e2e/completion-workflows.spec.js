const fs = require('node:fs');
const { test, expect } = require('@playwright/test');

const credentialFile = process.env.SKILLBRIDGE_TEST_ACCOUNTS_FILE;
const accounts = credentialFile ? JSON.parse(fs.readFileSync(credentialFile, 'utf8').replace(/^\uFEFF/, '')) : null;
const allowWrites = process.env.SKILLBRIDGE_MUTATING_TESTS === '1';

async function signIn(page, account) {
  await page.goto('/Account/Login');
  await page.locator('input[name="Email"]').fill(account.email);
  await page.locator('input[name="Password"]').fill(account.password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page).not.toHaveURL(/\/Account\/Login/i);
}

test('guest is redirected to sign in from /Messages/Inbox', async ({ page }) => {
  await page.goto('/Messages/Inbox');
  await expect(page).toHaveURL(/\/Account\/Login/i);
});

test.describe('Phase 1-3 completion member workflows', () => {
  test.skip(!accounts, 'Set SKILLBRIDGE_TEST_ACCOUNTS_FILE to an ignored local credentials file.');

  test('explore search, open community posting in All Communities, and Community reply notification', async ({ browser }) => {
    test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
    test.setTimeout(120_000);

    const author = await browser.newPage();
    await signIn(author, accounts.accountA);

    // Explore discovery: server search + client filter + visible AdditionalMembers / Knock readiness.
    await author.goto('/Explore');
    await author.locator('#memberSearchInput').fill('Python');
    await author.getByRole('button', { name: 'Search', exact: true }).click();
    await expect(author).toHaveURL(/q=Python/i);
    await expect(author.locator('main')).toBeVisible();

    // Open community posting into a community outside user skills (<optgroup label="All Communities">).
    await author.goto('/Communities');
    const allCommunityOption = author.locator('#feed-community-select optgroup[label="All Communities"] option').first();
    const targetCommunityId = await allCommunityOption.getAttribute('value');
    expect(targetCommunityId).toBeTruthy();

    const postTitle = `Open community discussion ${Date.now()}`;
    await author.locator('#feed-community-select').selectOption(targetCommunityId);
    await author.locator('#feed-post-title').fill(postTitle);
    await author.locator('#feed-post-content').fill('Checking open community participation and reply notification delivery.');
    await author.getByRole('button', { name: 'Publish Post' }).click();

    const postLink = author.getByRole('link', { name: postTitle }).first();
    await expect(postLink).toBeVisible();
    await postLink.click();
    await expect(author).toHaveURL(/\/Communities\/PostDetails\//i);
    const postUrl = author.url();

    // Second member comments on the open post without needing membership in that skill.
    const commenter = await browser.newPage();
    await signIn(commenter, accounts.accountB);
    await commenter.goto(postUrl);
    await commenter.locator('.comment-form [name="Content"]').fill('Reply from another member verifying open commenting and Community notifications.');
    await commenter.locator('.comment-form').getByRole('button', { name: 'Submit' }).click();
    await expect(commenter.locator('.comment-card').filter({ hasText: 'Reply from another member verifying open commenting' })).toBeVisible();

    // Post author receives a "Community" notification linking directly back to PostDetails.
    await author.goto('/Notifications');
    const replyNotification = author.locator('.notification-item').filter({ hasText: postTitle }).first();
    await expect(replyNotification).toBeVisible();
    const updateLink = replyNotification.getByRole('link', { name: 'View update' });
    await expect(updateLink).toHaveAttribute('href', /\/Communities\/PostDetails\//i);

    // Clean up temporary community post so disposable feed stays tidy.
    author.once('dialog', dialog => dialog.accept());
    await author.goto(postUrl);
    await author.getByRole('button', { name: 'Remove post' }).click();
    await expect(author).toHaveURL(/\/Communities/i);

    await commenter.close();
    await author.close();
  });

  test('Messages/Inbox search, Chat partner safety actions, single SignalR negotiate, and 320px dark/light layout', async ({ page }) => {
    test.setTimeout(90_000);
    await signIn(page, accounts.accountA);

    for (const width of [1280, 320]) {
      await page.setViewportSize({ width, height: 700 });
      for (const theme of ['light', 'dark']) {
        await page.goto('/Messages/Inbox');
        await page.evaluate(mode => {
          document.documentElement.setAttribute('data-theme', mode);
          document.body.setAttribute('data-theme', mode);
        }, theme);

        await expect(page.getByRole('heading', { name: 'Your Conversations' })).toBeVisible();
        const inboxWidth = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(inboxWidth, `/Messages/Inbox overflows at ${width}px (${theme})`).toBeLessThanOrEqual(width + 1);
      }
    }

    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/Messages/Inbox');
    if (await page.locator('.sb-inbox-item').count() > 0) {
      const firstPartner = (await page.locator('.sb-inbox-item a[class*="tw:text-base"]').first().innerText()).trim();
      await page.locator('#inboxSearch').fill('unlikely-search-token-xyz');
      await expect(page.locator('#inboxEmptyFilter')).toBeVisible();
      await page.locator('#inboxSearch').fill(firstPartner);
      const expectedMatches = await page.locator('.sb-inbox-item').filter({ hasText: firstPartner }).count();
      await expect(page.locator('.sb-inbox-item:visible')).toHaveCount(expectedMatches);

      await page.locator('.sb-inbox-item:visible').first().getByRole('link', { name: 'Open chat' }).click();
      await expect(page).toHaveURL(/\/Messages\/Chat\//i);

      // Verify shared layout SignalR hub connection starts without duplicating negotiate calls.
      await page.waitForFunction(() => performance.getEntriesByType('resource').some(entry => entry.name.includes('/chatHub/negotiate')));
      const negotiateCount = await page.evaluate(() =>
        performance.getEntriesByType('resource').filter(entry => entry.name.includes('/chatHub/negotiate')).length
      );
      expect(negotiateCount).toBe(1);

      // Verify "All conversations" button inside #messagePane and safety controls in .sb-chat-profile.
      await expect(page.locator('#messagePane').getByRole('link', { name: 'All conversations' })).toBeVisible();
      const profileRail = page.locator('.sb-chat-profile');
      await expect(profileRail.getByRole('link', { name: 'View profile' })).toBeVisible();
      await expect(profileRail.getByRole('button', { name: 'Block member' })).toBeVisible();
      await profileRail.locator('summary', { hasText: 'Report member' }).click();
      await expect(profileRail.locator('#chat-report-reason')).toBeVisible();

      // Verify 320px dark/light zero overflow on Chat view as well.
      await page.setViewportSize({ width: 320, height: 700 });
      for (const theme of ['light', 'dark']) {
        await page.evaluate(mode => {
          document.documentElement.setAttribute('data-theme', mode);
          document.body.setAttribute('data-theme', mode);
        }, theme);
        const chatWidth = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(chatWidth, `/Messages/Chat overflows at 320px (${theme})`).toBeLessThanOrEqual(321);
      }
    }
  });

  test('one-way mentorship renders a single exchange track without silent teaching promotion', async ({ browser }) => {
    test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
    test.setTimeout(150_000);

    const teacher = await browser.newPage();
    await signIn(teacher, accounts.accountA);
    await teacher.goto('/Profile');
    const teacherName = (await teacher.locator('main h1').first().innerText()).trim();

    const learner = await browser.newPage();
    await signIn(learner, accounts.accountB);
    await learner.goto('/Explore');
    const teacherCard = learner.locator('.person-card').filter({ has: learner.getByRole('heading', { name: teacherName, exact: true }) }).first();
    test.skip(await teacherCard.count() === 0, 'Both disposable accounts must see each other in Explore.');
    await teacherCard.getByRole('link', { name: 'View Profile' }).click();

    const openProposal = learner.locator('#skillsToTeachList li').filter({ has: learner.locator('summary') }).first();
    test.skip(await openProposal.count() === 0,
      'No open teaching skill request slot remains between disposable accounts for 1-way mentorship test.');

    await openProposal.locator('summary').click();
    await openProposal.locator('[name="goal"]').fill('Learn fundamentals in a 1-way mentorship.');
    await openProposal.locator('[name="pace"]').selectOption({ label: 'Weekly' });
    await openProposal.getByRole('button', { name: 'Send proposal' }).click();
    await expect(openProposal.getByRole('status')).toHaveText('Proposal sent.');

    await teacher.goto('/Profile/Requests');
    await teacher.getByRole('link', { name: 'View proposal' }).first().click();

    const reciprocalSelect = teacher.locator('#reciprocalSkill');
    if (await reciprocalSelect.count()) {
      await reciprocalSelect.selectOption('');
    }
    await teacher.getByRole('button', { name: 'Accept and start exchange' }).click();
    await expect(teacher).toHaveURL(/\/Profile\/Requests/i, { timeout: 30_000 });

    await learner.goto('/Interactions');
    await expect(learner.locator('.interaction-card').first()).toBeVisible();
    await learner.locator('.interaction-card').first().getByRole('link', { name: 'View Details' }).click();
    await expect(learner.locator('.exchange-track')).toHaveCount(1);

    await teacher.close();
    await learner.close();
  });

  test('UpdateProfile renders side-by-side Red/Blue skill board, inline stage drawer, 3 MB avatar guard, and 320px dark/light layout', async ({ page }) => {
    test.setTimeout(90_000);
    await signIn(page, accounts.accountA);
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/Profile/UpdateProfile');
    if (/\/CompleteProfile/i.test(page.url())) {
      const expand = page.locator('details').filter({ hasText: 'Add more details and skills now' });
      if (await expand.count()) await expand.locator('summary').click();
    }

    const learnPanel = page.locator('#skillsToLearnContainer');
    const knowPanel = page.locator('#skillsIKnowContainer');
    const divider = page.locator('.sb-skill-divider');
    await expect(learnPanel).toBeVisible();
    await expect(knowPanel).toBeVisible();
    await expect(divider).toBeVisible();

    const [learnBox, knowBox] = await Promise.all([learnPanel.boundingBox(), knowPanel.boundingBox()]);
    expect(Math.abs(learnBox.y - knowBox.y)).toBeLessThan(20);
    expect(learnBox.x + learnBox.width).toBeLessThan(knowBox.x);

    // Three skills per row in the desktop Programming category grid.
    const firstLearnGridYs = await learnPanel.locator('.sb-skill-grid').first().locator('.sb-skill-cell').evaluateAll(els =>
      els.slice(0, 3).map(e => Math.round(e.getBoundingClientRect().top))
    );
    expect(new Set(firstLearnGridYs).size).toBe(1);

    // Toggle Learn skill -> highlighted Red & count increments.
    const learnPython = learnPanel.locator('.skill-learn').filter({ hasText: /^\s*Python\s*$/ });
    const learnInitialPressed = await learnPython.getAttribute('aria-pressed');
    await learnPython.click();
    await expect(learnPython).toHaveAttribute('aria-pressed', learnInitialPressed === 'true' ? 'false' : 'true');
    if (learnInitialPressed === 'true') await learnPython.click();
    await expect(learnPython).toHaveAttribute('aria-pressed', 'true');
    await expect.poll(() => learnPython.evaluate(el => getComputedStyle(el).backgroundColor)).toMatch(/^rgb\((201, 59, 43|177, 49, 34)\)$/);

    // Toggle Teach skill -> highlighted Blue, drawer opens inline directly under that skill's row, stages 1..3 Blue.
    const knowPython = knowPanel.locator('.skill-know').filter({ hasText: /^\s*Python\s*$/ });
    if (await knowPython.getAttribute('aria-pressed') === 'true') {
      await knowPython.click();
      await expect(knowPython).toHaveAttribute('aria-pressed', 'false');
    }
    await knowPython.click();
    await expect(knowPython).toHaveAttribute('aria-pressed', 'true');
    const knowCard = knowPanel.locator('.sb-skill-know-card').filter({ has: page.locator('.skill-know').filter({ hasText: /^\s*Python\s*$/ }) });
    await expect.poll(() => knowCard.evaluate(el => getComputedStyle(el).backgroundColor)).toMatch(/^rgb\((50, 77, 178|41, 65, 156)\)$/);

    const drawer = knowPanel.locator('.sb-stage-drawer');
    await expect(drawer).toBeVisible();
    const [cardBox, drawerBox] = await Promise.all([knowCard.boundingBox(), drawer.boundingBox()]);
    expect(drawerBox.y).toBeGreaterThan(cardBox.y + cardBox.height - 2);
    expect(drawerBox.y - (cardBox.y + cardBox.height)).toBeLessThan(80);

    // Stage 1 is pre-selected; selecting Stage 3 highlights stages 1..3 in blue & updates trigger summary.
    await expect(drawer.locator('.stage-option').nth(0)).toHaveClass(/is-selected/);
    await expect(drawer.locator('.stage-option').nth(1)).not.toHaveClass(/is-selected/);
    await drawer.locator('.stage-option[data-stage="3"]').click();
    for (let i = 0; i < 3; i++) {
      await expect(drawer.locator('.stage-option').nth(i)).toHaveClass(/is-selected/);
    }
    await expect(drawer.locator('.stage-option').nth(3)).not.toHaveClass(/is-selected/);
    const trigger = knowCard.locator('.sb-stage-trigger');
    await expect(trigger).toContainText(/Stages 1[–-]3 of/);

    // Close via Escape and verify trigger regains focus.
    await drawer.locator('.stage-option[data-stage="3"]').press('Escape');
    await expect(drawer).toHaveCount(0);
    await expect(trigger).toBeFocused();

    // 3 MB avatar client-side guard & live circular preview check.
    const oversizedBuf = Buffer.alloc(3 * 1024 * 1024 + 64, 1);
    await page.locator('#ProfileImage').setInputFiles({ name: 'too-big.png', mimeType: 'image/png', buffer: oversizedBuf });
    await expect(page.locator('[data-sb-avatar-error]')).toBeVisible();
    await expect(page.locator('[data-sb-avatar-error]')).toContainText(/exceeds the 3 MB limit/i);

    const tinyPng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL/nwAAAABJRU5ErkJggg==', 'base64');
    await page.locator('#ProfileImage').setInputFiles({ name: 'avatar-ok.png', mimeType: 'image/png', buffer: tinyPng });
    await expect(page.locator('[data-sb-avatar-error]')).toBeHidden();
    await expect(page.locator('[data-sb-avatar-name]')).toHaveText('avatar-ok.png');
    await expect(page.locator('[data-sb-avatar-img]')).toHaveAttribute('src', /^blob:/);

    // Check dark mode + 320px zero horizontal overflow.
    for (const vp of [{ width: 1280, height: 800 }, { width: 320, height: 700 }]) {
      await page.setViewportSize(vp);
      for (const theme of ['light', 'dark']) {
        await page.evaluate(mode => {
          document.documentElement.setAttribute('data-theme', mode);
          document.body.setAttribute('data-theme', mode);
        }, theme);
        const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(scrollWidth, `Profile form overflows at ${vp.width}px (${theme})`).toBeLessThanOrEqual(vp.width + 1);
      }
    }
  });
});
