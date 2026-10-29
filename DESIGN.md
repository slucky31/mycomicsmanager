# Design System: MyComicsManager

The visual rules of the app. The values live in `Web/wwwroot/css/theme.css` (CSS variables `--mcm-*`) and in
`Web/Infrastructure/AppTheme.cs` (the MudBlazor theme). A component's scoped style (`*.razor.css`) uses these
variables instead of writing a value again.

## Overview

A personal tool to manage a collection of comics, used on a phone, a tablet and a desktop. It is an "operate"
surface: the covers carry the colour and the character, the interface around them stays quiet, familiar and
consistent from one screen to the other.

## Colors

- **Primary** (`--mud-palette-primary`, #594AE2): primary actions, current selection, links. Pressed:
  `--mcm-primary-strong`; tint behind a selected item: `--mcm-primary-soft`.
- **Text** (`--mcm-text`, #1B1D24) and **muted text** (`--mcm-text-muted`, #5C6070, also for placeholders).
- **Surfaces**: white cards (`--mcm-surface`) on `--mcm-surface-muted`; `--mcm-surface-sunken` for placeholders and neutral chips.
- **Lines**: `--mcm-border`, `--mcm-border-strong`.
- **States**: error, success, info and warning each have a colour, a stronger colour for text on a tint, and a
  tint (`--mcm-error`, `--mcm-error-strong`, `--mcm-error-soft`…).
- **Library colours** (`LibraryColors`): free for borders, tints and icons. Written as text, they are mixed with
  the text colour (`color-mix(in srgb, var(--library-color) 45%, var(--mcm-text))`) so that every one of them
  reaches 4.5:1.

## Typography

Roboto (MudBlazor), one family. Use the role, never a pixel value:

| Role | Size | Use |
|---|---|---|
| `--mcm-font-caption` | 12px | badges, metadata, the smallest text allowed |
| `--mcm-font-small` | 13px | secondary text, section labels |
| `--mcm-font-label` | 14px | labels, chips, small buttons |
| `--mcm-font-body` | 15px | body text |
| `--mcm-font-body-l` | 16px | inputs, titles in lists |
| `--mcm-font-subtitle` | 18px | card titles |
| `--mcm-font-title` | 20px | title of a page bar (with a back arrow) |
| `--mcm-font-title-l` | 22px | the same title from the tablet on |
| `--mcm-font-page` | 28px | title of a top-level page (Libraries, Statistics, Settings, Feeds) |
| `--mcm-font-page-l` | 32px | the same title, or a book's title, on a large screen |
| `--mcm-font-display` | 40px | big numbers and a book's title on a large screen |
| `--mcm-font-display-l`, `--mcm-font-hero` | 52px, 64px | the headline number of Statistics, the icon of a form state |

Weights: 400 for text, 500 for titles and emphasis, 700 for figures and badges. Buttons are in sentence case
(`AppTheme`), section labels in small capitals (uppercase, 0.06em tracking).

## Layout

- Spacing steps: `--mcm-space-1` to `--mcm-space-6` (4, 8, 12, 16, 24, 32px). Gutters: 16px on a phone, 28px from
  600px, 48px from 1440px.
- Breakpoints: 600px (tablet), 840px / 960px (side panel, side menu), 1440px (large screen).
- Grids of covers (Covers view, series page, list of series): 3 columns on a phone, then
  `repeat(auto-fill, minmax(var(--mcm-cover-m-min), 1fr))`.

## Covers

All 2:3, in four sizes (`--mcm-cover-*`):

| Size | Width | Where | Radius |
|---|---|---|---|
| XS | 40px | action menus, search results, Find details, missing volumes | 4px |
| S | 72px | list view, edit header, library cards | 6px |
| M | grid cell (`--mcm-cover-reading`, 112px, in the row of the Reading page on a phone) | Covers view, series page, list of series, books in progress | 6px |
| L | 124px, then 240px from 600px | a book's page and the book forms | 8px |

`CoverImage.Sized(link, CoverSize)` asks Cloudinary for the cover at the size it is shown.

## Elevation & Depth

Flat cards with a 1px border (`--mcm-border`) on a muted background. Shadows only on covers
(`--mcm-cover-shadow`, `--mcm-cover-shadow-l` for L) and on floating menus.

## Shapes

Radius steps: `--mcm-radius-s` 4px, `--mcm-radius-m` 8px, `--mcm-radius-l` 12px, `--mcm-radius-xl` 16px (cards),
`--mcm-radius-pill` (chips, pill buttons).

## Components

- **Buttons**: pill-shaped, primary filled for the main action of a screen, outlined or text for the others.
- **Chips**: pill, 1px border; filled with the primary colour when selected.
- **Page bar**: back arrow, title (`--mcm-font-title`) and subtitle in muted text; top-level pages use a large
  title (`--mcm-font-page`) instead.
- **Action sheet**: a bottom sheet on a phone, a menu anchored to its button from the tablet on, with an XS cover
  in its header for a book.

## Do and don't

- Do reuse a variable of `theme.css`; add one there when a value is needed in more than one component.
- Do keep text at 12px or more and at 4.5:1 or more.
- Don't write a hard-coded colour, font size or cover size in a component.
