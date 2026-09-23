# Design system

The Version 2 interface uses a light, restrained operational palette. CSS tokens live in `frontend/web/src/app/globals.css`; new components should use these tokens rather than hard-coded colors. The existing overview, topology, deployment, and event layouts retain their behavior while adopting this palette.

| Role | Token | Value |
| --- | --- | --- |
| Page background | `--color-bg` | `#F8FAFC` |
| Surface | `--color-surface` | `#FFFFFF` |
| Subtle surface | `--color-surface-subtle` | `#F1F5F9` |
| Border / strong border | `--color-border` / `--color-border-strong` | `#E2E8F0` / `#CBD5E1` |
| Text / secondary / muted | `--color-text` / `--color-text-secondary` / `--color-text-muted` | `#0F172A` / `#475569` / `#64748B` |
| Primary / hover | `--color-primary` / `--color-primary-hover` | `#2563EB` / `#1D4ED8` |
| Success | `--color-success` | `#15803D` |
| Warning | `--color-warning` | `#B45309` |
| Danger | `--color-danger` | `#B91C1C` |
| Info / unknown | `--color-info` / `--color-unknown` | `#1D4ED8` / `#64748B` |

The semantic colors have pale background companions. Statuses must also have readable text labels, as in the topology nodes and event severity badges. Focus outlines use primary blue and must stay visible on white surfaces. Cards use `--radius-lg` and `--shadow-card`; graph nodes use `--shadow-node`. Typography is a compact system sans stack with clear heading hierarchy. The board and forms introduced in later slices should reuse these primitives.

The palette was the first Version 2 slice and did not change the database model.

The board reuses these neutrals and semantic colors. Tag colors are selected from named tokens; priority always has a readable text label. The work-item panel uses the same surface, border, focus, and spacing conventions as the project forms.
