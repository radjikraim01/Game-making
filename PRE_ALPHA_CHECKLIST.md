# Pre-Alpha Checklist

Purpose:

- Define the minimum bar for calling this game `pre-alpha`.
- This is not a polish checklist.
- This is the minimum bar for a rough but real external playtest build.

## Pre-Alpha Definition

The game is `pre-alpha` when:

1. The core loop is playable from start to finish for the current slice.
2. Major visible systems work honestly enough to be tested.
3. External testers can play without constant developer intervention.
4. Feedback can focus on design quality, not constant breakage.

## Required Slice

- [ ] One complete playable floor
- [ ] Clear start state
- [ ] Clear boss encounter
- [ ] Clear victory state
- [ ] Clear defeat state
- [x] Save/load works during the slice

## Required Core Systems

- [ ] Character creation is readable and usable
- [x] Movement works reliably
- [x] Combat works reliably
- [x] Spellcasting works reliably for visible spells
- [ ] Inventory works reliably
- [ ] Equipment works reliably
- [ ] Loot works reliably
- [x] Level-up works reliably
- [x] Enemy behavior works reliably

## UI / UX Minimum

- [ ] Text is readable in normal window size
- [ ] Text is readable in maximized window size
- [x] Combat UI does not hide critical battlefield information
- [x] Action menus are fully visible and not clipped
- [ ] Character creation can be completed without confusion
- [ ] Level-up can be completed without confusion
- [ ] Inventory/equipment layout is understandable

## Honesty Rules

- [x] No major visible system is mostly fake
- [x] Visible spells do what their current descriptions claim
- [ ] Disabled/future systems are hidden instead of exposed half-finished
- [ ] Player-facing labels do not break tone unnecessarily

## Content Minimum

- [ ] Enough enemy variety to judge combat
- [ ] Enough loot to judge progression
- [x] Enough spells/feats to judge build direction
- [ ] Dungeon layout is coherent enough to judge exploration
- [ ] Boss encounter is distinct enough to judge the slice

## Stability Minimum

- [ ] No crash in a normal full-floor run
- [ ] No frequent softlocks
- [ ] No progression blockers in the current slice
- [ ] No constant UI corruption or unreadable states

## Balance Minimum

- [ ] Early combat is not trivialized by a small number of overtuned options
- [ ] Basic enemies do not feel meaningless because of one-shot spam
- [ ] Player can lose if they play badly
- [ ] Goblin encounters are still threatening enough to test tactics

Current known balance warning:

- Some cantrips/spells are overtuned enough to one-shot goblins too easily.
- This means combat balance is not yet at pre-alpha quality even if the systems run.

## External Test Standard

The build counts as pre-alpha when at least one outside tester can:

1. Start the game
2. Make a character
3. Play through the floor
4. Reach combat, loot, and progression naturally
5. Finish or fail the slice
6. Give feedback about fun, clarity, balance, and pacing

If the outside tester mostly reports:

- confusion
- broken flow
- unreadable UI
- fake mechanics
- constant intervention needed

then the build is still below pre-alpha.

## Likely Remaining Gaps Right Now

1. Live UI readability still needs real user confirmation.
2. Combat balance still needs work — cantrips/spells one-shot goblins.
3. Spell tuning still needs live gameplay validation.
4. Slice needs one clean external test run from start to finish.
5. Boss encounter, victory/defeat states not yet confirmed implemented.

## Exit Condition

We call the game `pre-alpha` when:

- the slice is rough but real
- testers can complete it
- feedback is mainly about quality and design, not basic functionality failure
