# Public Profile Page Overrides

> **PROJECT:** TERRA Public Portfolio
> **Generated:** 2026-09-14 10:34:30
> **Page Type:** Public profile / portfolio

> ⚠️ **IMPORTANT:** Rules in this file **override** the Master file (`design-system/MASTER.md`).
> Only deviations from the Master are documented here. For all other rules, refer to the Master.

---

## Page-Specific Rules

### Layout Overrides

- **Max Width:** 1400px or full-width
- **Grid:** 12-column grid for data flexibility

### Spacing Overrides

- **Content Density:** High — optimize for information display

### Typography Overrides

- Latin: Manrope (self-hosted)
- Arabic: Noto Sans Arabic (self-hosted)
- Oversized editorial headings with compact metadata labels

### Color Overrides

- Canvas: `#F2F5EF`
- Ink / dark stage: `#14221B`
- Brand accent: `#245C42`
- Signal accent: `#D8EE75`
- Project tones: brand green, slate blue, and warm brown

### Component Overrides

- Persistent side index on desktop with active-section state and reading progress
- Dark editorial hero with featured project signal
- Featured project plus two-column bento project grid
- Compact “More context” panel for education, certifications, and languages
- Timeline-style professional experience rows

---

## Page-Specific Components

- `side-index`
- `scroll-progress`
- `hero-stage`
- `hero-facts`
- `project-entry-featured` and `project-entry-row`
- `details-section`

---

## Motion & Accessibility

- Smooth anchor scrolling and a non-blocking reading-progress indicator
- IntersectionObserver reveal: 18px travel, 440–680ms, staggered project/experience entries
- No scroll-jacking, scrubbed parallax, or layout-shifting hover effects
- `prefers-reduced-motion` renders the full page immediately and disables image scaling
- Arabic RTL and English LTR layouts share the same information order
