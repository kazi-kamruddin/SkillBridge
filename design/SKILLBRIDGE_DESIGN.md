---
name: SkillBridge Design System
description: Playful Neo-Brutalism with Editorial Discipline — canonical design tokens, dual-theme palette, surface hierarchy, and component contract for SkillBridge (ASP.NET Core 10 MVC + Tailwind CSS v4).
version: 1.0.0
baselineCommit: 23509f8
---

# SkillBridge Design System (`SKILLBRIDGE_DESIGN.md`)

> **Creative North Star:** *"Warm Studio Notebook (Light) / Deep Navy Workshop (Dark)"* — Bold, charming, trustworthy, structured, and carefully crafted peer-to-peer learning community.

This document is the single source of truth for SkillBridge's visual refinement inside `SkillBridge.Core/`. Every token, contrast ratio, border rule, shadow offset, and component spec maps directly to SkillBridge's existing `--sb-*` CSS custom properties, `@theme` Tailwind v4 (`tw:`) tokens, and Razor MVC structure.

---

## 1. CSS Cascade & Consolidation Architecture

SkillBridge currently loads stylesheets in `_Layout.cshtml` (`L24-27`) in this order:
1. `@RenderSection("Styles", required: false)` (view-specific `<style>` or `wwwroot/Styles/*.css` or `skillbridge-about.css`)
2. `~/Content/skillbridge-refresh.css`
3. `~/Content/skillbridge-overhaul.css`
4. `~/Content/tailwind-app.css` (compiled from `Styles/tailwind.css` `@theme`, `@layer base`, `@layer utilities` with `prefix(tw)` & `!important`, and unlayered `@import "./skillbridge-app.css"`).

### Architectural Rules
- **Single Source of Truth for Tokens:** Define all semantic theme tokens once in `SkillBridge.Core/Styles/skillbridge-app.css` under `:root` (light) and `body[data-theme="dark"], html[data-theme="dark"]` (dark), mapped into Tailwind v4 `@theme` inside `SkillBridge.Core/Styles/tailwind.css`.
- **Reconcile Legacy Overrides:**
  - Eliminate competing surface overrides in `skillbridge-refresh.css` (`#151e22` / `#213035`) and `skillbridge-overhaul.css` (`#25383c` / `#33484b` / light `--sb-ink: #a8b8b7`) by pointing `--bg-color`, `--card-bg`, `--text-color`, `--text-muted`, `--border-color`, `--primary-color`, and `--accent-color` cleanly to canonical `--sb-*` semantic tokens.
  - Replace hardcoded `border: 1px solid var(--border-color)` and soft `box-shadow: var(--shadow)` leaks on `.card`, `.sb-page-heading`, and `.filter-section` with unified neo-brutalist border + offset-shadow tokens.
  - Eliminate per-view conflicting inline styles (`Interactions/Sessions.cshtml`, `Interactions/Index.cshtml`, `Profile/ChangePassword.cshtml`, `Notifications/Index.cshtml`) and fix `wwwroot/Content/skillbridge-about.css` dark-mode card overrides.
- **Test Contract Compatibility:** Preserve the six baseline dark-mode token values asserted by `tests/e2e/comprehensive-audit.spec.js` (`--sb-paper: #0f1416`, `--sb-card: #182024`, `--sb-line: #3d525a`, `--sb-sage: #1a3328`, `--sb-blue: #1d2a45`, `--sb-coral: #3b221a`) while introducing extended semantic tokens for raised surfaces, controls, accessible links, auth panels, amber badges, and crisp offset shadows.

---

## 2. Color Tokens (Light & Dark)

### Primitive & Semantic Color Table

| Token | Light Value | Dark Value | Role & Context | Verified Contrast |
|---|---|---|---|---|
| `--sb-paper` | `#fffdf8` | `#0f1416` | Page canvas anchor (paired with radial washes + SVG motif) | Primary text `15.4:1` (dark), `14.8:1` (light) |
| `--sb-card` (`--card-bg`) | `#fffdf8` | `#182024` | Standard card, panel, modal, and dropdown surface | Primary text `13.7:1` (dark), `14.8:1` (light) |
| `--sb-surface-raised` | `#f7f4eb` | `#1c262d` | Hero panels (`.sb-page-heading`), filter cards, secondary sections | Subtle navy-slate step above `--sb-card` (`1.25:1` luminance step) |
| `--sb-surface-sunken` | `#f3efe4` | `#12191e` | Form inputs (`input`, `textarea`, `select`), inset preview wells | Crisp inset field distinction inside `#182024` cards |
| `--sb-ink` (`--text-color`) | `#17242a` | `#edeae2` | Warm ink (light) / soft warm cream (dark) for headings & body | `14.8:1` on light paper, `13.7:1` on dark card |
| `--sb-muted` (`--text-muted`) | `#4b5c63` | `#9eb2b9` | Secondary labels, timestamps, captions, helper copy | `6.8:1` (light paper), `7.5:1` (dark card), `8.4:1` (dark paper) |
| `--sb-line` | `#17242a` | `#3d525a` | Primary structural borders (light) / subtle hairlines & dividers (dark) | Structural frame; baseline test contract preserved |
| `--sb-border-card` | `#17242a` | `#4c6573` | Card & hero outer frames in dark mode (eliminates white-wire vs invisible borders) | Visible cool-slate outline without harsh glare |
| `--sb-border-control` | `#17242a` | `#577380` | Interactive borders: buttons, inputs, checkboxes, switches | `3.28:1` vs `#182024`, `3.68:1` vs `#0f1416` (**WCAG 1.4.11 >= 3:1**) |
| `--sb-border-focus` | `#243b94` | `#8eaaff` | Keyboard `:focus-visible` indicator ring | `8.25:1` (dark), `9.1:1` (light) |
| `--sb-sage` | `#dfebe1` | `#1a3328` | Teaching/matching surface tint, hero headers, modal header | `#edeae2` text on `#1a3328` = `10.9:1` |
| `--sb-sage-deep` | `#2f6e5d` | `#244637` | Completed milestone / positive CTA fill | Dark `#ffffff` or `#edeae2` text `8.4:1` (dark), `#ffffff` `5.9:1` (light) |
| `--sb-blue` | `#e4e9fb` | `#1d2a45` | Deep navy contextual card surface ("Grow together", 1-way badge) | `#edeae2` on `#1d2a45` = `10.8:1` |
| `--sb-blue-strong` | `#2b44a8` | `#2f4db3` | Primary CTA button fill (`btn-primary`, selected teach stages) | White `#ffffff` text = `8.3:1` (light), `7.41:1` (dark) |
| `--sb-blue-link` (`--primary-color`) | `#2942a3` | `#8eaaff` | Inline links, outline-primary button text, eyebrow section marks | `8.6:1` (light), `7.35:1` on dark card, `8.25:1` on dark paper |
| `--sb-coral` | `#e8b4a0` | `#3b221a` | Secondary pastel highlight (light) & muted brick card token (dark baseline) | `#17242a` (light `8.67:1`), `#edeae2` (dark `11.25:1`) |
| `--sb-coral-rich` | `#e8b4a0` | `#47251f` | Warm terracotta card fill in dark mode (CTA banner "Curiosity looks good on you") | `11.25:1` with `#edeae2`; softer warm terracotta over muddy brown |
| `--sb-accent-text` (`--accent-color`) | `#a83820` | `#f29b7d` | Eyebrow text (`.sb-eyebrow`), footer bar underline, step numbers | `6.35:1` (light paper), `5.26:1` (light sage), `7.68:1` (dark card) |
| `--sb-gold` (`tw:bg-sb-yellow`) | `#f5d76e` | `#5e4718` | Topic pills, `#SkillName` tags, "Yellow" in-progress milestone stage | `#17242a` (`11.2:1` light), `#f6edd9` (`7.9:1` dark) |
| `--sb-aside-blue` | `#2f49ac` | `#22356b` | Login right promotional panel | `#f4f1e9` text = `10.4:1` (dark); `#ffffff` = `7.8:1` (light) |
| `--sb-aside-coral` | `#b83728` | `#6d2c25` | Register left promotional panel (deep controlled terracotta in dark mode) | `#f4f1e9` text = `9.13:1` (dark); `#ffffff` = `5.81:1` (light) |
| `--sb-danger` | `#b42318` | `#4f2120` | Error banners & destructive surfaces | `#fff` (`6.4:1` light), `#ffd7d2` ink (`9.8:1` dark) |
| `--sb-danger-text` | `#9f2217` | `#ffa89e` | Form validation / inline error text | `7.1:1` (light), `8.5:1` (dark) |
| `--sb-shadow-ink` | `#17242a` | `#05080c` | Deep solid offset shadow core | Crisp dark drop step |
| `--sb-shadow-slate` | `#17242a` | `#263744` | Elevated dark card offset block shadow (visible geometric step on `#0f1416`) | `1.51:1` distinct tonal block edge against `#0f1416`, zero white glare |

### Dark-Mode Background Anchor
- Preserve `#0f1416` (`--sb-paper`) + radial washes (`rgba(65, 130, 100, 0.13)` sage wash, `rgba(43, 62, 134, 0.16)` deep navy atmospheric wash, `rgba(180, 90, 68, 0.09)` terracotta wash) + `url("/images/skill-pattern-dark.svg")` `420px × 420px`.
- Footer and floating navbar remain anchored in deep navy-ink (`#111a24` / `#131e28` with `2px` border) so chrome frames the canvas cohesively without hard patch seams behind `.sb-brand`.

---

## 3. Surface Hierarchy (Light & Dark)

| Tier | Surface Purpose | Light Mode | Dark Mode | Elevation / Separation Mechanism |
|---|---|---|---|---|
| **L0: Canvas** | `html`, `body` | `#fffdf8` + radial washes + SVG pattern | `#0f1416` + radial navy/sage washes + SVG pattern | Base layer; sticky-flex column layout keeps footer anchored to bottom (`min-height: 100dvh`) |
| **L1: Standard Card** | Content cards, posts, directory items, match cards | `#fffdf8` | `#182024` | `2px solid var(--sb-border-card)` + `4px 4px 0 var(--sb-shadow-slate)` |
| **L2: Raised / Feature Panel** | `.sb-page-heading`, `.sb-hero`, `.filter-section`, modals | `#f7f4eb` / `#dfebe1` / `#e4e9fb` | `#1c262d` / `#1a3328` / `#1d2a45` | `2px solid var(--sb-border-card)` + `6px 6px 0 var(--sb-shadow-slate)` + subtle 45deg `✳` watermark |
| **L3: Sunken Well / Input** | Text fields, `<select>`, `<textarea>`, preview wells | `#ffffff` (crisp) / `#f3efe4` | `#12191e` | `2px solid var(--sb-border-control)`; no drop shadow at rest; inset clarity |
| **L4: Chrome (Navbar & Footer)** | Floating pill `.sb-nav`, `footer` | `#17242a` ink pill + `#f4f0e5` active pill | `#131e28` navy-slate pill + `1.5px solid #4c6573` + crisp shadow; active pill `#243652` with `#e2e9ff` ring | Distinct navigation frame without glaring pure-white active pills in dark mode |

---

## 4. Border & Shadow Grammar

To project **editorial neo-brutalist discipline** (+10–15% restrained intentional character):
- **Border Width Hierarchy:**
  - `2px solid`: Primary cards, hero sections, buttons, inputs, modals, alerts, badges in active states.
  - `1px solid`: Internal list dividers, table rows, compact skill pills inside dense match cards (reduces visual noise in multi-chip grids).
  - `2px dashed var(--sb-border-card)`: Empty-state panels (`.sb-empty-panel`, `.empty-state`, `.no-results`).
- **Shadow Hierarchy (`@theme` Tailwind & CSS tokens):**
  - `--sb-shadow-sm`: `3px 3px 0 var(--sb-shadow-slate)` — buttons, icon tiles, small callout pills, alerts, active drawer options.
  - `--sb-shadow-md` (`--sb-card-shadow`): `5px 5px 0 var(--sb-shadow-slate)` — standard cards, auth split-panel, dropdown menus.
  - `--sb-shadow-lg`: `7px 7px 0 var(--sb-shadow-slate)` — hero cards, card `:hover` lift (`transform: translate(-1px, -1px)`), modals.
- **Pressed / Tactile Rule:**
  - Interactive buttons & click cards shift *into* their shadow on hover (`transform: translate(2px, 2px); box-shadow: 1px 1px 0`) and flatten on `:active` (`transform: translate(3px, 3px); box-shadow: none`).
  - Decorative/informative non-clickable hero containers stay anchored (no spurious hover bounce).
- **Zero Forbidden Effects:** No Gaussian blur shadows on standard cards (`filter: blur` / `rgba(..., 0.15)` generic SaaS shadows prohibited outside `.sb-nav` viewport elevation if needed, and even `.sb-nav` shifts to a crisp `0 6px 0 var(--sb-shadow-ink)` / clean tactile border-shadow). No rainbow text gradients or glossy progress bars.

---

## 5. Typography Scale & Line Heights

Preserve SkillBridge's clean geometric/system grotesque hierarchy with tighter editorial contrast:
- **Font Family:** `system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`; monospace code/ID accents (`ui-monospace, SFMono-Regular, Menlo, Consolas, monospace`).
- **Scale & Metrics:**
  - **Hero Display (`h1` on Landing / Profile):** `clamp(2.2rem, 4.6vw, 4.25rem)`, weight `900`, `line-height: 1.04`, `letter-spacing: -0.04em`.
  - **Page H1 (`h1` inside `.sb-page-heading` / `.main-content`):** `clamp(1.95rem, 3.6vw, 3.25rem)`, weight `900`, `line-height: 1.08`, `letter-spacing: -0.03em`.
  - **Section H2:** `clamp(1.45rem, 2.2vw, 2.25rem)`, weight `850`, `line-height: 1.15`, `letter-spacing: -0.02em`.
  - **Card Title H3 / H4:** `1.15rem – 1.35rem`, weight `800`, `line-height: 1.25`.
  - **Body Copy:** `1rem` (`16px`), `line-height: 1.6`, max prose measure `66ch`.
  - **Eyebrow / Kicker (`.sb-eyebrow`):** `0.75rem`, weight `850`, uppercase, `letter-spacing: 0.11em`, color `var(--sb-accent-text)`.
  - **Badge / Meta Label:** `0.75rem – 0.8125rem`, weight `750`, tabular numerals (`font-variant-numeric: tabular-nums`) on ratings (`9.7`), notification counts, and step indices (`01 /`).

---

## 6. Spacing & Grid Rules

- **Page Container:** Max width `1400px` (`1460px` for `/Messages` split chat pane), fluid horizontal padding `clamp(16px, 3vw, 40px)`, top clearance for fixed floating pill navbar (`112px` desktop, `88px` compact mobile).
- **Sticky Footer Flex:** `body { min-height: 100dvh; display: flex; flex-direction: column; }`, `main.main-content { flex: 1 0 auto; }`, `footer { margin-top: auto; }`. Eliminates floating mid-viewport footers on sparse pages (`/Interactions`, `/Messages` empty state, `/Notifications`).
- **Section Rhythm:** Vertical section spacing `clamp(2.25rem, 4.5vw, 4rem)`. Uniform grid gaps (`1rem – 1.5rem`).
- **Breakpoints & Mobile Floor (`>= 320px`):**
  - Desktop `>= 1200px`: Full floating top navbar with centered `.sb-brand`, multi-column grids (`2–3 cols`).
  - Tablet `768px – 1199px`: Drawer toggle (`#navbarContent.is-open`) anchored below floating header bar; 2-column grids; `.sb-auth-card` remains split side-by-side above `768px`.
  - Mobile `< 768px` & Ultra-Compact `320px`: Single-column stacks, full-width tappable CTA buttons (`min-height: 44px`), `.sb-chat` single-column stack / message view, horizontal-scroll-safe filter groups and wrapped topic chips (`overflow-x: hidden` never masking text; flex-wrap + `min-width: 0`).

---

## 7. Component Specifications

### Navigation Shell (`.sb-nav`) & Footer
- `.sb-brand`: `background: transparent` (eliminates `#17242a` square patch artifact over pill background); preserves geometric `SkillBridge ✳` alignment (`position: fixed` bar centered within `3px` tolerance tested by `guest.spec.js`).
- Active nav item in Dark Mode: Soft luminous navy-tint pill (`#233452` fill, `2px solid #8eaaff` border, `#eef2ff` ink) instead of jarring cream-white `#f4f0e5` brick in dark bar. Light Mode retains `#f4f0e5` crisp cream pill on `#17242a` bar.
- Footer links, section headers (`h4:after` terracotta rule), and copyright preserve `var(--footer-bg)` synced cleanly with dark-navy chrome (`#111922`) + top `3px` warm terracotta-coral rule (`#d97154` / `#f29b7d`).

### Buttons & Interactive Links
- `.btn-primary`: `background: var(--sb-blue-strong); color: #ffffff; border: 2px solid var(--sb-border-control); box-shadow: var(--sb-shadow-sm);` (hover presses cleanly down-right).
- `.btn-outline-primary`, `.btn-outline-secondary`: Dark mode renders `background: var(--sb-card); color: var(--sb-blue-link)` (`#8eaaff`, `7.35:1`) with `border-color: var(--sb-border-control)` (`#577380`, `3.28:1`). Hover lifts fill gently (`#212f48`) without jarring white flash.
- `.btn-success`: `background: var(--sb-sage-deep); color: #ffffff; border: 2px solid var(--sb-border-control)`.
- `.btn-danger` / `.btn-outline-danger`: Uses `var(--sb-danger)` fill + `#ffffff`/`#ffd7d2` text rather than muddy brown-red or harsh `#ff4b2b`.
- **Touch Target Discipline:** All primary standalone buttons, mobile drawer nav links, input fields, theme toggle (`44px × 44px` on `<1200px`), and pill chips enforce comfortable hit geometry (`>= 44px` height on mobile controls; `>= 24px` compliance with spacing elsewhere).

### Auth Split Card (`.sb-auth-card`: Login & Register)
- Preserve exact DOM structure, form fields, `asp-for` bindings, `__RequestVerificationToken`, `.sb-password-toggle`, and `.sb-auth-switch-link` sliding transition.
- **Aside Panels:** Use `.sb-auth-aside-blue` (`#2f49ac` light / `#22356b` dark) and `.sb-auth-aside-red` (`#b83728` light / `#6d2c25` dark) with crisp inner editorial framing (`border-top`/`border-left` divider `2px solid var(--sb-border-card)`).
- **`.sb-auth-symbol` Badge:** Light paper/cream circle `#fffdf8` (`#17242a` ink icon + `2px solid #17242a` border + `3px 3px 0 #17242a` shadow in BOTH light & dark mode — **14.1:1** contrast instead of broken `1.2:1`!).
- Form inputs sit on `--sb-surface-sunken` (`#12191e` dark / `#ffffff` light) paired with `--sb-sage` icon addons for tactile clarity.

### Cards, Directory Items & Topic Pills
- Unify `.card`, `.sb-page-heading`, `.sb-profile-hero`, `.sb-profile-panel`, `.sb-saved-card`, `.filter-section`, `.post-card`, and `.sb-community-card` under shared `--sb-card` / `--sb-surface-raised`, `--sb-border-card`, and `--sb-shadow-md` geometry.
- **Topic & Skill Badges (`tw:bg-sb-yellow` repair):** Define `--sb-gold` (`#f5d76e` light / `#5e4718` dark) and register `--color-sb-yellow: var(--sb-gold)` in `tailwind.css` `@theme` so active `#Topic` filters & `#Skill` tags gain real neo-brutalist color with `> 7.9:1` contrast, AND fix `Communities/Index.cshtml` dark heading link (`tw:text-sb-blue-link`).
- **Ratings Badge:** Replace Bootstrap `#007bff` blue with structured neo-brutalist rating pill (`2px solid var(--sb-border-control)`, tabular font, blue for `>= 7.5`, neutral slate/amber for mid/new `0.0`).
- **About Team & Values Cards:** Fix `skillbridge-about.css` `body[data-theme="dark"] .sb-about-values article:nth-child(2|3)` so they transition cleanly to `--sb-blue` (`#1d2a45`) & `--sb-sage` (`#1a3328`) with `--sb-ink` (`#edeae2`) text — solving the `1.07:1` contrast catastrophe! Fix `.sb-team-card` overlay text contrast by strengthening the bottom gradient scrim (`rgba(11, 22, 28, .92)`).

### Progress Bars & Stage Statuses
- Fix conflicting classes `tw:bg-sb-blue-strong bg-danger tw:bg-sb-coral` on `Profile/Index` & `Profile/PublicProfile` progress bars -> clean neo-brutalist progress track (`--sb-surface-sunken`) + vivid cobalt/sage fill (`var(--sb-blue-strong)`).
- Unify `Interactions/Sessions` `.stage.Red`, `.stage.Yellow`, `.stage.Green` across both light & dark modes with explicit accessible pairs + text/icon indicator alongside color (not color-only).

---

## 8. Interaction State & Motion Rules

- **Hover:** Interactive cards & links translate (`+2px, +2px` into shadow for buttons; `-2px, -2px` away with deeper `7px 7px 0` shadow for clickable cards). Transitions use crisp neo-brutalist easing (`150ms cubic-bezier(0.2, 0.8, 0.2, 1)`).
- **Focus (`:focus-visible`):** Universal `outline: 3px solid var(--sb-border-focus); outline-offset: 2px;` — NEVER `outline: none` without equal/higher contrast replacement.
- **Active / Selected:** Distinct fill + solid border shift + optional checkmark/asterisk marker (never color-only state).
- **Disabled:** Reduced fill contrast paired with muted border (`opacity: 0.55; box-shadow: none; cursor: not-allowed;`).
- **Theme Switch Transition:** Keep `_Layout.cshtml` synchronous `<head>` anti-FOUC script untouched; ensure `data-theme="dark"` + `.dark` class update atomically without jarring flash across body/nav/cards.
- **Reduced Motion:** `@media (prefers-reduced-motion: reduce)` preserves functional state transitions cleanly while disabling pulsing badges, sliding/bouncing transforms, and parallax/card-lift motion (`transition-duration: 0.01ms` ONLY on motion properties or instant state swaps — never breaking disclosure/visibility states).

---

## 9. Accessibility & Invariant Protection Checklist

1. **Contrast (WCAG 2.2 AA):** Normal text >= `4.5:1`; large headings (>= 18.66px bold / >= 24px regular) >= `3.0:1`; UI control borders & focus indicators >= `3.0:1`.
2. **Color-Blind & Non-Color Cues:** All states (Red/Yellow/Green stages, form validation errors, active filters, verified skills) combine color with label, badge text, or icon.
3. **Preserved Functional Contracts:**
   - All `asp-for`, `name`, `id`, `__RequestVerificationToken`, `[data-*]` attributes (`data-sb-*`, `data-ui-*`, `data-utc`, `data-skill-id`, `data-auth-target`).
   - All Playwright selectors: `#themeToggle`, `.sb-nav`, `.sb-brand`, `.sb-nav-content`, `.sb-password-toggle`, `.sb-auth-card`, `.sb-team-card`, `#chatImage`, `#chatImageError`, `#chatImagePreview`, `.stage-option`, `.skill-learn`, `.skill-know`, `.sb-community-card-known`, `#community-search`, `article.card`.
   - Zero changes to SignalR, encryption, controllers, EF models, database, or routing.
