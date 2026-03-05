# Player Hunting Plan

Last updated: 2026-03-05

## Purpose

Build a clear path from "solid tactical prototype" to "game people follow, recommend, and support."

This plan focuses on:
- Animation and visual readability.
- Combat feel and presentation polish.
- Demo quality and retention.
- Community and Patreon readiness.

## Current Baseline (What We Already Have)

- Playable one-floor tactical RPG run.
- Multi-enemy initiative combat with movement, targeting legality, spell targeting mode, and reinforcement joins.
- Build systems (classes, spells, feats, loot, equipment, route choices).
- Save/load and internal phase self-check coverage.

## Outcome Targets

## Product Targets

- Combat is immediately readable and satisfying (hits, casts, damage, reactions).
- 10-20 minute demo loop is polished and replayable.
- Players can describe the game hook in one sentence.

## Audience Targets

- Repeatable short-form clips that look good without explanation.
- Early community loop (Discord + changelogs + playable drops).
- Patreon value structure ready after player trust is established.

## Core Workstreams

## Workstream A - Sprite Animation System

Scope:
- Player and enemy animation states.
- Facing direction consistency.
- Event-driven combat animation triggers.

Implementation:
- Add animation state machine per actor:
  - `Idle`
  - `Move`
  - `Attack`
  - `Cast`
  - `HitReact`
  - `Death`
- Define per-state timing profiles (windup, impact, recovery).
- Keep fallback behavior when a state strip is missing.

Acceptance criteria:
- Every basic combat action has a visible animation response.
- No instant pose pop when transitioning states.
- Missing clips do not break runtime.

## Workstream B - Spell VFX and Telegraph Layer

Scope:
- Spell cast, travel, impact, and AOE readability.
- Clear legal-target feedback during target mode.

Implementation:
- Build a lightweight VFX event system:
  - `CastStart`
  - `ProjectileTravel`
  - `Impact`
  - `LingeringArea` (when needed)
- Add color and shape language by damage type.
- Add pre-impact telegraph for high-impact spells.
- Add hit confirmation VFX and optional screen shake for heavy spells.

Acceptance criteria:
- Spell type is recognizable from visuals alone.
- Impact moment is obvious and synchronized with combat log result.
- Telegraphs improve decision-making, not clutter.

## Workstream C - Combat Juice and Feedback

Scope:
- Make tactical combat feel responsive and weighty.

Implementation:
- Add floating damage numbers and crit styling.
- Add brief hit stop / impact flash for melee and heavy spell impacts.
- Add status effect icons and on-apply feedback.
- Add turn ownership indicators (whose turn is active).

Acceptance criteria:
- Players can parse action outcome in under 1 second.
- Crit and miss moments feel distinct.
- No readability loss during multi-enemy rounds.

## Workstream D - Audio Foundation (SFX + Music)

Scope:
- Core SFX and minimum music pass tied to gameplay states.

Implementation:
- Add SFX categories:
  - footsteps
  - melee hit
  - spell cast
  - spell impact
  - loot pickup
  - UI confirm/back
  - death
- Add music layers:
  - exploration
  - combat
  - boss
- Add volume controls split by channel:
  - master
  - music
  - SFX

Acceptance criteria:
- Major gameplay events have matching audio feedback.
- Audio can be balanced in settings without clipping.
- Combat and boss moments feel meaningfully different.

## Workstream E - Vertical Slice Quality Gate

Scope:
- Convert current floor into a streamable/demo-ready slice.

Implementation:
- Fix polish issues found in manual playtest passes.
- Add "first 3 minute" onboarding clarity:
  - objective readability
  - controls clarity
  - early reward clarity
- Ensure one full run has:
  - tactical pressure
  - route decision
  - boss resolution

Acceptance criteria:
- New player can start and finish a run without external help.
- No blocker bugs in start-to-finish slice.
- Combat pacing remains engaging across the run.

## Workstream F - Player Acquisition + Patreon Readiness

Scope:
- Build audience loop after product baseline is polished.

Implementation:
- Content cadence:
  - weekly devlog post
  - one short gameplay clip per week
  - one monthly build summary
- Community loop:
  - Discord channel structure for feedback and bug reports
  - changelog transparency for each build
- Patreon setup (after slice polish):
  - free member tier
  - supporter tier with early build access and design votes
  - credit/supporter recognition

Acceptance criteria:
- Community feedback appears consistently across releases.
- Update cadence is predictable.
- Patreon launch has clear value, not just donation ask.

## Workstream G - City Hub and Story Spine

Scope:
- Add a city loop that follows dungeon runs and anchors progression.

Implementation:
- Add return-to-city flow after retreat/segment completion.
- Add baseline city interactions:
  - heal/rest
  - shop/vendor
  - stash/loadout prep
  - depart back to dungeon
- Add short story beats tied to run milestones.
- Add NPC progression flags and service unlocks.

Acceptance criteria:
- City loop is fast, readable, and useful between dungeon runs.
- At least one city story beat triggers after meaningful dungeon progress.
- City progression/services persist correctly across save/load.

## Execution Phases

## Phase PH-1 - Animation Architecture

Checklist:
- Define actor animation states and transitions.
- Build fallback handling for missing clips.
- Integrate animation triggers into combat actions.

## Phase PH-2 - Spell VFX + Combat Feedback

Checklist:
- Implement cast/travel/impact VFX events.
- Add damage/crit visual feedback and hit reactions.
- Add turn readability cues.

## Phase PH-3 - Audio + Feel Pass

Checklist:
- Add minimum SFX coverage for core actions.
- Add exploration/combat/boss music transitions.
- Tune audio mix and options.

## Phase PH-4 - Vertical Slice Lock

Checklist:
- Run structured manual gameplay validation.
- Fix blocker and high-impact polish issues.
- Confirm demo-quality 10-20 minute run stability.

## Phase PH-5 - City Hub and Story Spine

Checklist:
- Add city transition flow and return loop.
- Implement baseline city services (`Heal`, `Shop`, `Stash`, `Depart`).
- Add first city story beat chain tied to dungeon milestones.
- Persist city unlock and NPC progression flags.

## Phase PH-6 - Community Pipeline

Checklist:
- Start consistent update cadence.
- Ship public-facing devlogs and clips.
- Launch Patreon only after polish threshold is met.

## Manual Validation Protocol (When We Start Testing)

- Run at least 5 full runs with different classes.
- Track:
  - dead turns
  - unreadable moments
  - pacing dips
  - repeated frustration points
- Label issues by severity:
  - blocker
  - high
  - medium
  - low

## Non-Negotiables

- Readability over visual noise.
- Performance stability over effect quantity.
- Fallback-safe assets (no hard crash when missing animation/VFX).
- One authoritative plan update per major decision.

## Deferred Until After This Plan

- Large story campaign expansion.
- Advanced meta systems beyond basic retention loop.
- Major economy/crafting overhaul.
