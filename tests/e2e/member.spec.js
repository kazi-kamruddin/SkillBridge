const fs = require('node:fs');
const { test, expect } = require('@playwright/test');

const credentialFile = process.env.SKILLBRIDGE_TEST_ACCOUNTS_FILE;
const accounts = credentialFile ? JSON.parse(fs.readFileSync(credentialFile, 'utf8')) : null;
const allowWrites = process.env.SKILLBRIDGE_MUTATING_TESTS === '1';

test.skip(!accounts, 'Set SKILLBRIDGE_TEST_ACCOUNTS_FILE to an ignored local credentials file.');

for (const key of ['accountA', 'accountB']) {
  test(`${key} can sign in and access member pages`, async ({ page }) => {
    const account = accounts[key];
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(account.email);
    await page.locator('input[name="Password"]').fill(account.password);
    await page.getByRole('button', { name: 'Sign In' }).click();

    await expect(page).not.toHaveURL(/\/Account\/Login/i);
    const firstDestination = new URL(page.url()).pathname;
    expect(firstDestination).toMatch(/^\/(?:CompleteProfile|Home)?(?:\/Index)?$/i);

    const failures = [];
    for (const path of ['/Profile', '/Profile/UpdateProfile', '/Explore', '/Explore/Skills', '/Interactions', '/Interactions/History', '/Messages', '/Notifications', '/SavedProfiles', '/Communities']) {
      const response = await page.goto(path);
      if (response.status() !== 200) failures.push(`${path}: HTTP ${response.status()}`);
      if (/\/Account\/Login/i.test(page.url())) failures.push(`${path}: redirected to login`);
    }
    expect(failures, `${key} member routes`).toEqual([]);
  });
}

test('exchange pages show profile names without exposing login emails', async ({ browser }) => {
  test.setTimeout(90_000);
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    for (const path of ['/', '/Interactions', '/Profile/Requests', '/Notifications', '/SavedProfiles']) {
      await page.goto(path);
      const text = await page.locator('body').innerText();
      expect(text, `${key} ${path} should hide login addresses`).not.toMatch(/[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}/);
    }
    await page.goto('/Profile/Requests');
    await page.getByRole('link', { name: 'View proposal' }).first().click();
    expect(await page.locator('body').innerText()).not.toMatch(/[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}/);
    await page.close();
  }
});

test('member avatar stays consistent between navigation and profile', async ({ browser }) => {
  test.setTimeout(60_000);
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Profile');
    const navigationAvatar = await page.locator('nav.navbar img[alt="Profile Image"]').getAttribute('src');
    const profileAvatar = await page.locator('main img[alt="Profile Picture"]').getAttribute('src');
    expect(navigationAvatar).toBeTruthy();
    expect(profileAvatar).toBeTruthy();
    expect(new URL(navigationAvatar, page.url()).href).toBe(new URL(profileAvatar, page.url()).href);
    await page.close();
  }
});

test('test members can add reciprocal skills and appear in discovery', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(90_000);
  const pairs = [
    { key: 'accountA', teach: 'Java', learn: 'AutoCAD' },
    { key: 'accountB', teach: 'AutoCAD', learn: 'Java' },
  ];

  for (const pair of pairs) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[pair.key].email);
    await page.locator('input[name="Password"]').fill(accounts[pair.key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Profile/UpdateProfile');

    const teaching = page.locator('.skill-know').filter({ hasText: new RegExp(`^\\s*${pair.teach}\\s*$`) });
    const learning = page.locator('.skill-learn').filter({ hasText: new RegExp(`^\\s*${pair.learn}\\s*$`) });
    if (await teaching.getAttribute('aria-pressed') === 'false') await teaching.click();
    if (await learning.getAttribute('aria-pressed') === 'false') await learning.click();
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await expect(page).toHaveURL(/\/Profile(?:\/Index)?$/i);
    await expect(page.getByRole('heading', { name: 'Teaching Skills' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Seeking to Learn' })).toBeVisible();
    await page.close();
  }

  const page = await browser.newPage();
  await page.goto('/Account/Login');
  await page.locator('input[name="Email"]').fill(accounts.accountA.email);
  await page.locator('input[name="Password"]').fill(accounts.accountA.password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.goto('/Explore');
  await expect(page.locator('.person-card').first()).toBeVisible();
  await page.close();
});

test('test members can save a match and start an exchange proposal', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(150_000);
  async function signIn(key) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    return page;
  }

  const receiver = await signIn('accountB');
  await receiver.goto('/Profile');
  const receiverName = await receiver.locator('#title').innerText();

  const requester = await signIn('accountA');
  await requester.goto('/Explore');
  const match = requester.locator('.person-card').filter({ has: requester.getByRole('heading', { name: receiverName, exact: true }) });
  await match.getByRole('link', { name: 'View Profile' }).click();
  if (await requester.getByRole('button', { name: 'Save member' }).isVisible()) {
    await requester.getByRole('button', { name: 'Save member' }).click();
    await expect(requester.getByRole('button', { name: 'Remove from saved' })).toBeVisible();
  }
  const profileUrl = requester.url();
  await requester.goto('/SavedProfiles');
  await expect(requester.locator(`a[href="${new URL(profileUrl).pathname}"]`).first()).toBeVisible();
  await requester.goto(profileUrl);

  const autocad = requester.locator('#skillsToTeachList li').filter({ hasText: /AutoCAD/ });
  await autocad.locator('summary').click();
  await autocad.locator('[name="goal"]').fill('Test exchange: learn introductory AutoCAD drawing.');
  await autocad.locator('[name="pace"]').selectOption({ label: 'Weekly' });
  await autocad.getByRole('button', { name: 'Send proposal' }).click();
  await expect(autocad.getByRole('status')).toHaveText('Proposal sent.');

  await receiver.goto('/Profile/Requests');
  await receiver.getByRole('link', { name: 'View proposal' }).first().click();
  await expect(receiver.getByRole('button', { name: 'Accept and start exchange' })).toBeVisible();
  await receiver.getByRole('button', { name: 'Accept and start exchange' }).click();
  await expect(receiver).toHaveURL(/\/Profile\/Requests/i);
  await receiver.goto('/Interactions');
  await expect(receiver.locator('.interaction-card').first()).toBeVisible();

  await requester.close();
  await receiver.close();
});

test('accepted test exchange opens its plan and members can chat', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(120_000);
  const pages = {};
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    pages[key] = page;
  }

  await pages.accountB.goto('/Interactions');
  await expect(pages.accountB.locator('.interaction-card').first()).toBeVisible();
  await pages.accountB.locator('.interaction-card').first().getByRole('link', { name: 'View Details' }).click();
  await expect(pages.accountB.getByRole('heading', { name: 'Your exchange plan' })).toBeVisible();
  await expect(pages.accountB.locator('.exchange-track')).toHaveCount(2);

  await pages.accountA.goto('/Explore');
  const receiverName = await pages.accountB.goto('/Profile').then(() => pages.accountB.locator('#title').innerText());
  const match = pages.accountA.locator('.person-card').filter({ has: pages.accountA.getByRole('heading', { name: receiverName, exact: true }) });
  await match.getByRole('button', { name: 'Knock' }).click();
  await expect(pages.accountA).toHaveURL(/\/Messages\/Chat\//i);
  await pages.accountA.locator('#messageText').fill('Test message: confirming our SkillBridge exchange.');
  await pages.accountA.locator('#sendMessageForm').getByRole('button', { name: 'Send' }).click();
  await expect(pages.accountA.locator('#messageList')).toContainText('Test message: confirming our SkillBridge exchange.');

  await pages.accountB.goto('/Messages');
  await expect(pages.accountB.locator('#messageList')).toContainText('Test message: confirming our SkillBridge exchange.');
  await expect.poll(async () => {
    return pages.accountB.evaluate(async () => {
      const response = await fetch('/Messages/UnreadCount');
      if (!response.ok) throw new Error(`UnreadCount returned HTTP ${response.status}`);
      return (await response.json()).count;
    });
  }, { timeout: 20_000 }).toBe(0);
  await pages.accountA.reload();
  await expect(pages.accountA.locator('#messageList .read-state').last()).toHaveText('Read');

  await pages.accountA.close();
  await pages.accountB.close();
});

test('test exchange supports meeting planning and milestone notes', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(120_000);
  const pages = {};
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Interactions');
    await page.locator('.interaction-card').first().getByRole('link', { name: 'View Details' }).click();
    pages[key] = page;
  }

  await pages.accountA.getByText('Propose a meeting time').click();
  const meetingLocal = await pages.accountA.evaluate(() => {
    const date = new Date(Date.now() + 3 * 60 * 60 * 1000);
    const pad = n => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  });
  await pages.accountA.locator('#meetingLocal').fill(meetingLocal);
  await pages.accountA.locator('#meetingFormat').selectOption('Online');
  await pages.accountA.locator('#meetingNote').fill('Test SkillBridge video call');
  await pages.accountA.locator('#meetingForm').getByRole('button', { name: 'Send proposal' }).click();
  await expect(pages.accountA.locator('#meeting-heading')).toBeVisible();
  await expect(pages.accountA.locator('.meeting-time').first()).toBeVisible();

  await pages.accountB.reload();
  await pages.accountB.getByRole('button', { name: 'Accept time' }).click();
  await expect(pages.accountB.getByText('Confirmed', { exact: true }).first()).toBeVisible();

  const stage = pages.accountA.locator('.exchange-track .stage').first();
  await stage.getByText(/Milestone notes/).click();
  await stage.locator('[name="whatWeCovered"]').fill('Test note: reviewed the first milestone.');
  await stage.locator('[name="nextStep"]').fill('Practice before our next session.');
  await stage.getByRole('button', { name: 'Save note' }).click();
  await pages.accountA.locator('.exchange-track .stage').first().getByText(/Milestone notes/).click();
  await expect(pages.accountA.locator('.stage .small').filter({ hasText: 'Test note: reviewed the first milestone.' }).first()).toBeVisible();
  await pages.accountB.reload();
  await pages.accountB.locator('.exchange-track .stage').first().getByText(/Milestone notes/).click();
  await expect(pages.accountB.locator('.stage .small').filter({ hasText: 'Test note: reviewed the first milestone.' }).first()).toBeVisible();

  await pages.accountA.close();
  await pages.accountB.close();
});

test('test exchange partners can propose and accept a milestone rename', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(120_000);
  const pages = {};
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Interactions');
    await page.locator('.interaction-card').first().getByRole('link', { name: 'View Details' }).click();
    pages[key] = page;
  }

  const proposerTrack = pages.accountA.locator('.exchange-track').filter({ has: pages.accountA.getByRole('heading', { name: 'Java', exact: true }) });
  await proposerTrack.getByText('Suggest changes to upcoming milestones').click();
  const firstTitle = proposerTrack.locator('.plan-title').first();
  const originalTitle = await firstTitle.inputValue();
  const proposedTitle = `${originalTitle.replace(/ \(test\)$/, '')} (test)`;
  await firstTitle.fill(proposedTitle);
  await proposerTrack.getByRole('button', { name: 'Send plan proposal' }).click();
  await expect(pages.accountA.getByText('Waiting for your partner to review these changes')).toBeVisible();

  await pages.accountB.reload();
  const receiverTrack = pages.accountB.locator('.exchange-track').filter({ has: pages.accountB.getByRole('heading', { name: 'Java', exact: true }) });
  await expect(receiverTrack.getByText('Your partner suggested this plan')).toBeVisible();
  await receiverTrack.getByRole('button', { name: 'Accept plan' }).click();
  await expect(receiverTrack.getByText(proposedTitle, { exact: false }).first()).toBeVisible();
  await pages.accountA.reload();
  await expect(pages.accountA.locator('.exchange-track').filter({ has: pages.accountA.getByRole('heading', { name: 'Java', exact: true }) }).getByText(proposedTitle, { exact: false }).first()).toBeVisible();

  await pages.accountA.close();
  await pages.accountB.close();
});

test('private chat images load for both members and require sign-in', async ({ browser }) => {
  test.skip(!allowWrites || process.env.SKILLBRIDGE_MEDIA_TESTS !== '1', 'Enable explicit media testing on the deployed app.');
  test.setTimeout(120_000);
  const image = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL/nwAAAABJRU5ErkJggg==', 'base64');
  const pages = {};
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Messages');
    pages[key] = page;
  }

  await pages.accountA.locator('#chatImage').setInputFiles({ name: 'skillbridge-test.png', mimeType: 'image/png', buffer: image });
  const [send] = await Promise.all([
    pages.accountA.waitForResponse(response => /\/Messages\/Send$/i.test(new URL(response.url()).pathname), { timeout: 45_000 }),
    pages.accountA.locator('#sendMessageForm').getByRole('button', { name: 'Send' }).click(),
  ]);
  expect(send.status()).toBe(200);
  await pages.accountB.reload();
  const imageLink = pages.accountB.locator('#messageList a[href*="/Messages/Image/"]').last();
  await expect(imageLink).toBeVisible();
  const imagePath = await imageLink.getAttribute('href');
  const imageResponse = await pages.accountB.goto(new URL(imagePath, pages.accountB.url()).toString());
  expect(imageResponse.status()).toBe(200);
  expect(imageResponse.headers()['content-type']).toMatch(/^image\/png/i);

  const guest = await browser.newPage();
  await guest.goto(new URL(imagePath, pages.accountB.url()).toString());
  await expect(guest).toHaveURL(/\/Account\/Login/i);
  await guest.close();
  await pages.accountA.close();
  await pages.accountB.close();
});

test('community images work in a post and comment', async ({ browser }) => {
  test.skip(!allowWrites || process.env.SKILLBRIDGE_MEDIA_TESTS !== '1', 'Enable explicit media testing on the deployed app.');
  test.setTimeout(120_000);
  const image = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL/nwAAAABJRU5ErkJggg==', 'base64');
  const title = `SkillBridge media test ${Date.now()}`;
  const author = await browser.newPage();
  await author.goto('/Account/Login');
  await author.locator('input[name="Email"]').fill(accounts.accountA.email);
  await author.locator('input[name="Password"]').fill(accounts.accountA.password);
  await author.getByRole('button', { name: 'Sign In' }).click();
  await author.goto('/Communities');
  const javaCommunity = author.locator('.sb-community-card-known').filter({ has: author.locator('strong').getByText('Java', { exact: true }) });
  await javaCommunity.click();
  const communityUrl = author.url();
  await author.getByRole('link', { name: 'Create Post' }).click();
  await author.locator('[name="Title"]').fill(title);
  await author.locator('[name="Content"]').fill('Temporary image upload check for SkillBridge.');
  await author.locator('#Image').setInputFiles({ name: 'skillbridge-post-test.png', mimeType: 'image/png', buffer: image });
  await author.getByRole('button', { name: 'Create Post' }).click();
  await expect(author).toHaveURL(communityUrl);
  await author.getByRole('link', { name: new RegExp(title) }).click();
  const postUrl = author.url();
  await expect(author.getByRole('img', { name: 'Image attached to this post' })).toBeVisible();

  const commenter = await browser.newPage();
  await commenter.goto('/Account/Login');
  await commenter.locator('input[name="Email"]').fill(accounts.accountB.email);
  await commenter.locator('input[name="Password"]').fill(accounts.accountB.password);
  await commenter.getByRole('button', { name: 'Sign In' }).click();
  await commenter.goto(postUrl);
  await commenter.locator('.comment-form [name="Content"]').fill('Temporary comment image upload check.');
  await commenter.locator('#comment-image').setInputFiles({ name: 'skillbridge-comment-test.png', mimeType: 'image/png', buffer: image });
  await commenter.locator('.comment-form').getByRole('button', { name: 'Submit' }).click();
  await expect(commenter.getByRole('img', { name: 'Image attached to this comment' })).toBeVisible();

  const guest = await browser.newPage();
  await guest.goto(postUrl);
  await expect(guest.getByRole('heading', { name: title })).toBeVisible();
  await expect(guest.getByRole('img', { name: 'Image attached to this post' })).toBeVisible();
  await guest.close();

  author.once('dialog', dialog => dialog.accept());
  await author.goto(postUrl);
  await author.getByRole('button', { name: 'Remove your post' }).click();
  await expect(author).toHaveURL(communityUrl);
  await commenter.close();
  await author.close();
});

test('community text post and comment can be created and removed', async ({ page, browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(90_000);
  const title = `SkillBridge text test ${Date.now()}`;
  await page.goto('/Account/Login');
  await page.locator('input[name="Email"]').fill(accounts.accountA.email);
  await page.locator('input[name="Password"]').fill(accounts.accountA.password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.goto('/Communities');
  const javaCommunity = page.locator('.sb-community-card-known').filter({ has: page.locator('strong').getByText('Java', { exact: true }) });
  await javaCommunity.click();
  const communityUrl = page.url();
  await page.getByRole('link', { name: 'Create Post' }).click();
  await page.locator('[name="Title"]').fill(title);
  await page.locator('[name="Content"]').fill('Temporary text-only post check.');
  await page.getByRole('button', { name: 'Create Post' }).click();
  await expect(page).toHaveURL(communityUrl);
  await page.getByRole('link', { name: new RegExp(title) }).click();
  const postUrl = page.url();
  const commenter = await browser.newPage();
  await commenter.goto('/Account/Login');
  await commenter.locator('input[name="Email"]').fill(accounts.accountB.email);
  await commenter.locator('input[name="Password"]').fill(accounts.accountB.password);
  await commenter.getByRole('button', { name: 'Sign In' }).click();
  await commenter.goto(postUrl);
  await commenter.locator('.comment-form [name="Content"]').fill('Temporary comment check.');
  await commenter.locator('.comment-form').getByRole('button', { name: 'Submit' }).click();
  await expect(commenter.locator('.comment-card').filter({ hasText: 'Temporary comment check.' })).toBeVisible();
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Remove your post' }).click();
  await expect(page).toHaveURL(communityUrl);
  await commenter.close();
});

test('both partners can complete milestones and rate their exchange', async ({ browser }) => {
  test.skip(!allowWrites, 'Set SKILLBRIDGE_MUTATING_TESTS=1 for disposable-account writes.');
  test.setTimeout(150_000);
  const pages = {};
  for (const key of ['accountA', 'accountB']) {
    const page = await browser.newPage();
    await page.goto('/Account/Login');
    await page.locator('input[name="Email"]').fill(accounts[key].email);
    await page.locator('input[name="Password"]').fill(accounts[key].password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.goto('/Interactions');
    await page.locator('.interaction-card').first().getByRole('link', { name: 'View Details' }).click();
    pages[key] = page;
  }
  const planUrl = pages.accountA.url();

  for (const key of ['accountA', 'accountB']) {
    const page = pages[key];
    await page.goto(planUrl);
    const stages = await page.locator('.stage').evaluateAll(elements => elements.map(element => ({
      skill: element.dataset.skill,
      stage: element.dataset.stage,
    })));
    expect(stages).toHaveLength(2);
    for (const entry of stages) {
      const milestone = page.locator(`.stage[data-skill="${entry.skill}"][data-stage="${entry.stage}"]`);
      const confirm = milestone.getByRole('button', { name: 'Confirm milestone' });
      if (await confirm.count()) {
        await confirm.click();
        await expect(confirm).toHaveCount(0);
      }
    }
  }

  await pages.accountA.goto(planUrl);
  await expect(pages.accountA.getByRole('button', { name: 'Complete exchange' })).toBeVisible();
  await pages.accountA.getByRole('button', { name: 'Complete exchange' }).click();
  await expect(pages.accountA).toHaveURL(/\/Interactions(?:\/Index)?$/i, { timeout: 20_000 });

  for (const [key, rating] of [['accountA', '9'], ['accountB', '8']]) {
    const page = pages[key];
    await page.goto('/Interactions/History');
    await page.getByRole('link', { name: 'View exchange record' }).first().click();
    await expect(page.getByRole('link', { name: 'Rate this exchange' })).toBeVisible();
    await page.getByRole('link', { name: 'Rate this exchange' }).click();
    await page.locator('#RatingValue').fill(rating);
    await page.locator('#Comment').fill('Test exchange feedback.');
    await page.getByRole('button', { name: 'Submit Rating' }).click();
    await expect(page.locator('#rating-result')).toContainText('Rating submitted successfully!');
  }
  await pages.accountA.goto('/Interactions/History');
  await pages.accountA.getByRole('link', { name: 'View exchange record' }).first().click();
  await expect(pages.accountA.getByText('Your rating: 9/10')).toBeVisible();
  await expect(pages.accountA.getByText("Partner's rating: 8/10")).toBeVisible();

  await pages.accountA.close();
  await pages.accountB.close();
});
