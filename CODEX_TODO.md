# Unified Work Plan (Single Source of Truth)

Last updated: 2026-03-05

## Status

- Phase 1: implementation complete (manual gameplay validation deferred)
- Phase 2: implementation complete (manual gameplay validation deferred)
- Phase 3: implementation complete (manual gameplay validation deferred)
- Phase 4A: implementation complete (manual gameplay validation deferred)
- Phase 4B: implementation complete (manual gameplay validation deferred)
- Phase 5: implementation complete (manual gameplay validation deferred)
- Phase 6: implementation complete (manual gameplay validation deferred)
- Phase 7: implementation complete (manual gameplay validation deferred)
- Phase 8: planned (city hub + story spine)

## Phase 1 - Tactical Combat Foundation

Goal:
- Make combat less random, more readable, and more skill-based.

Scope:
- Enemy intent/pattern readability.
- Elite modifiers.
- Camera follow for larger play space.
- Enemy perception (sight/FOV + line-of-sight + leash/timeout).
- Enemy state machine: Patrol -> Investigate -> Chase -> Search -> Return.
- Enemy movement pressure so the player can choose loot-first or fight-first.

Checklist:
- [x] Implement dead-zone camera.
- [x] Implement smooth follow interpolation.
- [x] Clamp camera to map bounds.
- [x] Implement detection rules (radius + FOV + LOS).
- [x] Implement chase timeout and leash distance.
- [x] Implement search memory window and return-to-patrol.
- [x] Implement 5-state enemy FSM and transitions.
- [x] Add a debug indicator for active enemy state.
- [x] Add contested loot interaction under enemy pressure.
- [x] Add counterplay (interrupted pickup and/or guaranteed loot drop on kill).

Acceptance Criteria:
- [ ] Enemy can fail to detect player outside FOV or behind walls.
- [ ] Enemy never chases forever across the full floor.
- [ ] All five AI states are observable in normal play.
- [ ] No stuck AI state after LOS break or combat end.
- [ ] Player can reliably choose either loot-first or fight-first.
- [ ] Camera never shows out-of-bounds map space.
- [ ] Camera movement is stable (no visible jitter).

Validation note:
- Manual gameplay validation pass is deferred by user request and will be executed after planned implementation phases.

Progress notes (2026-03-04):
- Implemented `GameTuning` runtime tuning values for camera and enemy perception/chase.
- Added camera dead-zone + smoothing + world-bounds clamping.
- Added enemy world movement with state flow:
  - Patrol -> Investigate -> Chase -> Search -> Return
- Added line-of-sight + FOV + proximity detection and leash/timeout chase break.
- Added `F1` debug overlay for state, vision range/FOV, and leash visualization.
- Added D&D-inspired low-level guaranteed enemy drops (common/uncommon/rare style).
- Added contested loot flow:
  - Hold `E` to secure loot.
  - Pickup can be interrupted by nearby/chasing enemies.
  - Enemy kills always produce a loot drop.
- Expanded equipment slot structure (Wrath/PF-style baseline):
  - Added broader slot model (Main/Off Hand, Armor, Head, Goggles, Neck, Cloak, Shirt, Bracers, Gloves, Belt, Knee Pads, Boots, Ring).
  - Ring now supports two simultaneous equip positions (Ring 1 / Ring 2) with deterministic auto-swap when both are occupied.
  - Save/load now persists equipped slot index (including ring position) to keep load behavior stable.
- Immersion update:
  - Removed visible "Loot Shard" wording from player-facing UI/messages.
  - Shifted rewards toward item-based loot language and supply drops.
- System update:
  - Removed internal shard currency fields from runtime and save schema.
  - Enemies now carry loot kits from the same item pool.
  - Enemies can consume eligible loot in combat (once per fight), and only unused loot drops.
- Build + self-checks pass after this update.

## Phase 2 - Build Identity and Replay (Current Priority)

Goal:
- Increase replay value through meaningful build variation.

Scope:
- Relic/passive synergies that change playstyle.
- Three early archetype path options.
- Milestone progression checkpoints with meaningful unlock choices.

Checklist:
- [x] Add at least 3 early build archetype options.
- [x] Ensure relics modify gameplay behavior, not only stats.
- [x] Add progression checkpoints every defined interval.

Acceptance Criteria:
- [ ] Different archetypes lead to clearly different combat decisions.
- [ ] At least one relic synergy can enable a distinct build strategy.
- [ ] Progression choices feel consequential, not cosmetic.

Progress notes (2026-03-04):
- Implemented first Phase 2 slice: three early archetype path choices at the first reward node.
  - `Path of the Vanguard`: +1 melee, +2 defense.
  - `Path of the Arcanist`: +2 spell damage, +1 Mana Draught.
  - `Path of the Skirmisher`: +3% crit, +5% flee, +1 Sharpening Oil.
- Updated combat-edge reward scaling by chosen archetype for future reward nodes.
- Added run archetype persistence to save/load.
- Implemented second Phase 2 slice: milestone relic checkpoint after archetype choice.
  - `Bloodwake Emblem`: first melee hit each combat restores HP (stronger with Vanguard).
  - `Astral Conduit`: first spell each combat gains burst damage (stronger with Arcanist).
  - `Veilstrider Charm`: first flee attempt each combat gains bonus chance (stronger with Skirmisher).
- Added relic persistence to save/load and surfaced archetype/relic labels in diagnostics/victory summary.
- Implemented third Phase 2 slice: interval-based doctrine checkpoints.
  - Checkpoint interval is defined (`every 2 claimed reward nodes`).
  - Each interval grants a meaningful doctrine choice:
    - `Execution Doctrine`: on-kill sustain scaling by rank.
    - `Arc Doctrine`: limited per-combat spell-slot waivers scaling by rank.
    - `Escape Doctrine`: limited per-combat failed-flee retaliation blocks scaling by rank.
  - Doctrine ranks persist in save/load and are shown in run diagnostics/summary.
- Completed Phase 2 item 1 polish/validation pass (code-level):
  - Doctrine ranks are now effect-capped at `3/3` with overflow checkpoint picks converting into supply rewards instead of dead choices.
  - Milestone/relic combat effects now use centralized tuning constants across reward previews and runtime execution.
  - Build identity visibility improved in reward choice, HUD, combat overlay, and character sheet.
  - Save-load restore now clamps doctrine ranks to valid runtime bounds.
- Verification (2026-03-04):
  - `dotnet build -c Release` passed.
  - `dotnet run -c Release -- --phase7-checks` passed (`11/11`).

## Phase 3 - One-Floor Mini-Run Depth

Goal:
- Add depth and route strategy without adding more floors yet.

Scope:
- Keep one floor, but split it into 3 connected zones.
- Add branch route decisions.
- Add at least one risk/reward event.
- Preserve run variety across exploration, event interaction, and combat.

Checklist:
- [x] Build 3 connected zones with different pacing.
- [x] Add at least one meaningful branch path.
- [x] Add at least one risk/reward event.
- [x] Ensure zone flow supports route planning.

Acceptance Criteria:
- [ ] One-floor run lasts about 10-15 minutes.
- [ ] Every run includes at least one meaningful route decision.
- [ ] Event outcomes can alter resources or build direction.

Progress notes (2026-03-04):
- Started Phase 3 implementation slice 1:
  - Added macro-zone tracking on the same floor:
    - `Entry Frontier`
    - `Branching Depths`
    - `Sanctum Ring`
  - Added an explicit branch choice event (`Forked Descent`) after early run identity setup:
    - `Upper Catacombs Route`: +20% kill XP, enemies gain +1 attack.
    - `Lower Shrine Route`: +1 defense, +6% flee, +1 Health Potion, but -10% kill XP.
  - Added a route-tied risk/reward event (`Oath Crucible`) in the selected branch zone:
    - `Take Blood Oath`: HP sacrifice for offensive run bonuses.
    - `Stabilize The Cache`: supply + sustain boost with mobility/crit tradeoff.
  - Surfaced zone/route info in HUD, diagnostics, character sheet, and victory summary.
  - Persisted Phase 3 route/event modifiers in save/load (`schema v10`).
- Continued Phase 3 implementation slice 2:
  - Added route-based enemy pacing:
    - Run starts with entry pack only.
    - Choosing a route now mobilizes route-specific enemy packs (Upper vs Lower).
    - Entering `Sanctum Ring` triggers a sanctum wave spawn (including boss).
  - Added a second branch-specific outcome at sanctum entry:
    - Upper route: extra sanctum reinforcements.
    - Lower route: emergency supply + sustain package on entry.
  - Persisted route/sanctum wave state flags in save/load (`schema v10`) so wave spawns do not duplicate after load.
- Continued Phase 3 implementation slice 3 (pacing gate + objective clarity):
  - Added sanctum unlock pacing gate so boss wave only spawns after:
    - route selected,
    - route event resolved,
    - at least `8` enemy defeats,
    - at least `2` claimed primary reward nodes.
  - Added persistent kill-progress tracking for pacing (`Phase3EnemiesDefeated`).
  - Added explicit objective guidance to HUD + character sheet so route/run progression is visible at all times.
  - Persisted new pacing progress field in save/load (`schema v11`).
- Continued Phase 3 implementation slice 4 (physical branch commitment):
  - Added route-dependent sealed corridor locks so the opposite branch path is physically blocked after route selection.
  - Added visible corridor lock overlays in world rendering to communicate blocked paths.
  - Added route-specific pre-sanctum objective reward granted once unlock requirements are met:
    - Upper route: offensive/magic prep bundle.
    - Lower route: defensive/sustain prep bundle.
  - Persisted pre-sanctum reward grant state in save/load (`schema v12`) to prevent duplicate grants after loading.
- Verification:
  - `dotnet build -c Release` passed.
  - `dotnet run -c Release -- --phase7-checks` passed (`11/11`).

## Phase 4A - Accessibility Foundation (After Phase 3)

Goal:
- Add accessibility support without tying it to gameplay penalties.

Scope:
- Accessibility settings:
  - Color-blind presets.
  - High-contrast mode.
  - UI readability controls (text size/important highlights).

Checklist:
- [x] Add accessibility menu section in settings.
- [x] Add at least one color-blind preset and one high-contrast preset.

Acceptance Criteria:
- [x] Accessibility options are independent from difficulty and always available.

Progress notes (2026-03-04):
- Added a dedicated `Accessibility` submenu under pause `Settings`.
- Added accessibility runtime settings:
  - Color profile: `Default` and `Deuteranopia-Friendly`.
  - `High Contrast UI`: on/off.
- Added live palette application path so accessibility toggles affect UI immediately.
- Persisted accessibility settings in save/load snapshots and bumped save schema to `v13`.
- Extended Phase 7 save round-trip self-check coverage to include the new accessibility fields.
- Verification:
  - `dotnet build -c Release` passed.
  - `dotnet run -c Release -- --phase7-checks` passed (`11/11`).

## Phase 4B - Optional Conditions and Recovery (After 4A)

Goal:
- Add optional long-term condition depth with clear recovery paths and no forced baseline penalties.

Scope:
- Optional condition/injury system:
  - Character-creation origin conditions (optional).
  - Dungeon-acquired major conditions (rare, high-impact).
  - High-tier healing or equivalent services to remove major conditions.
- Blind mage concept (optional archetype/condition path):
  - No normal color vision presentation.
  - Vision assisted by magic sense.
  - If no magic resource is available, fallback "bat resonance"/echolocation style sensing.

Checklist:
- [x] Add core condition data model (apply, persist, cure).
- [x] Add condition source rules (creation-selected and dungeon-acquired).
- [x] Add high-tier cure path (spell/service/item) with clear cost.
- [x] Prototype blind mage perception mode with no-color presentation and magic/resonance sensing.
- [x] Add clear UI indicators for active conditions and recovery options.

Acceptance Criteria:
- [x] Optional conditions can be enabled/disabled by mode toggle.
- [x] Blind mage mode remains playable in normal combat/exploration loops.
- [x] Recovery path is understandable, costly, and not RNG-locked.
- [x] Save/load preserves condition and cure-state correctly.

Progress notes (2026-03-04):
- Implemented Phase 4B condition framework:
  - Core runtime major-condition model with apply/cure flows.
  - Save/load persistence for condition states and related settings (`schema v14`).
- Added optional condition source rules:
  - Character creation origin condition selection in Identity (`None`, `Arcane Blindness`, `Crushed Limb`).
  - Dungeon-acquired rare major condition trigger from severe enemy hits.
- Added high-tier recovery path:
  - Pause Settings -> Accessibility -> `Purge Major Condition`.
  - Deterministic cost: `2 Health Potion + 2 Mana Draught + 1 Sharpening Oil`.
- Implemented blind mage prototype:
  - Arcane Blindness condition applies no-color-style screen filter.
  - Magic-sense overlay when mana is available.
  - Resonance fallback when mana is empty.
  - Mechanical penalties/recovery tied to mana state.
- Added condition UI coverage:
  - HUD condition summary + recovery hint.
  - Combat panel condition status.
  - Character sheet major-condition section with effects and cure cost.
  - Creation summary origin condition visibility.
- Verification:
  - `dotnet build -c Release` passed.
  - `dotnet run -c Release -- --phase7-checks` passed (`11/11`).

## Phase 5 - Feat and Armor Expansion (Next Priority)

Goal:
- Expand build depth with a larger feat ecosystem and D&D-style armor training/equipment rules.

Scope:
- Raise feat catalog to at least `60` total feats.
- Add feat pick at character creation.
- Replace feat progression with interval progression:
  - feat at level `1` (creation),
  - then level `4`,
  - then every `4` levels after (`8`, `12`, `16`, ...).
- Keep class bonus feats as a future additive system (not in this phase).
- Add armor categories and equip restrictions:
  - `Unarmored`, `Light`, `Medium`, `Heavy`.
- Add armor-focused feats:
  - `Light Armor Training`
  - `Medium Armor Training` (requires Light)
  - `Heavy Armor Training` (requires Medium)
  - `Unarmored Defense` (works only when armor slot is empty)
- Add more armor items to loot tables so training choices matter in real runs.
- Update UI and save/load validations for new feat and armor states.

Checklist:
- [x] Add a `Feats` step to character creation and require selecting one starting feat before final confirmation.
- [x] Update feat progression logic to grant feats at levels `4 + every 4 levels` after creation.
- [x] Add feat tier/prerequisite gates so high-power feats unlock later.
- [x] Expand `FeatBook` from current `12` to minimum `60` feats.
- [x] Implement armor category metadata for armor-slot equipment.
- [x] Enforce armor training checks during equip attempts with clear user-facing block reasons.
- [x] Implement armor training and unarmored feats in runtime stat/combat calculations.
- [x] Add armor pieces across low-level loot tables and ensure slot/category consistency.
- [x] Update character sheet and inventory views to show armor category, training, and active armor-state bonuses.
- [x] Extend phase self-checks for feat progression, feat prerequisites, armor gating, and save/load round-trip.

Acceptance Criteria:
- [x] Character creation always grants exactly one starting feat choice.
- [x] All classes gain feat picks at level 4 and each 4 levels after.
- [x] Feat catalog count is at least `60`.
- [x] Armor equip rules prevent untrained use of restricted categories.
- [x] Unarmored bonuses only apply while no armor is equipped.
- [x] Save/load preserves all new feat and armor state without regression.

Implementation order:
- Pass A: feat progression + creation feat step + core prerequisite framework.
- Pass B: armor categories + training feats + equip gating + UI messaging.
- Pass C: feat catalog expansion to `60+` + loot integration + self-check coverage.

Progress notes (2026-03-05):
- Started Pass A implementation:
  - Added a dedicated `Feats` section to character creation (`Identity/Class/Stats/Spells/Feats/Review`).
  - Enforced exactly one starting feat pick before `Start Adventure` is allowed.
  - Added creation feat selection UI, readiness checks, and summary panel updates.
  - Updated feat progression to `level 4 + every 4 levels` (4/8/12/...).
  - Added feat prerequisite framework (`RequiredFeatIds`) and wired prerequisite validation into `Player.CanLearnFeat`.
  - Added initial high-tier gates to existing feats (sample chains for martial, defensive, crit, and caster tracks).
- Completed Pass B implementation:
  - Added armor category metadata (`Light` / `Medium` / `Heavy`) for armor-slot gear.
  - Added class/feat armor training rules and runtime checks that block invalid armor equips with explicit reasons.
  - Added armor training feats (`Light Armor Training`, `Medium Armor Training`, `Heavy Armor Training`, `Unarmored Defense`) and integrated prerequisite chain checks.
  - Added armor-state feat effects to combat calculations (defense/flee) with unarmored-only handling.
  - Added armor items (`Leather Jerkin`, `Brigandine Coat`, `Plate Harness`) to inventory and low-level loot tables.
  - Updated inventory and character sheet UI to surface armor category, training state, and active armor-style bonuses.
  - Added new phase checks for armor training rules and armor feat-chain validation.
- Completed Pass C implementation:
  - Expanded feat catalog to `67` total feats (60+ target met) across armor, melee, defense, crit, mobility, and caster tracks.
  - Removed level-based feat gating in the active catalog so feats are level-1 available by default.
  - Kept feat-family progression through prerequisite chains (`RequiredFeatIds`) instead of level locks.
  - Refactored feat runtime effects to data-driven bonus fields (melee/spell/defense/crit/flee/HP/MP/armor bypass) used directly in player calculations.
  - Added feat catalog self-check coverage for:
    - minimum feat count,
    - no level-gated feats in current scope,
    - valid prerequisite references.
- Verification:
- `dotnet build -c Release` passed.
- `dotnet run -c Release -- --phase7-checks` passed (`15/15`).

## Phase 6 - Floor Identity and Modular Visual Equipment (Planned)

Goal:
- Improve dungeon realism and readability by separating enemy families across floor progression.
- Add true equipment-visible character visuals (paper-doll layering) once modular art is available.

Scope:
- Floor-by-floor enemy placement plan:
  - Avoid placing all enemy types on the same early floor.
  - Assign enemy families by floor theme/tier and encounter role.
  - Reserve boss/support enemy sets for deeper floors and escalation bands.
- Encounter progression rules:
  - Early floors: basic enemy families and clear telegraphed threats.
  - Mid floors: mixed packs and synergy-based enemy behavior.
  - Deep floors: elite variants and specialized counters.
- Visual equipment system (asset-gated):
  - Use layered body + gear rendering for equipped items (paper-doll style).
  - Support at minimum: `Armor`, `Head`, `Cloak`, `MainHand`, `OffHand`.
  - Keep fallback behavior when a specific overlay layer is missing.

Checklist:
- [ ] Define enemy-family-to-floor matrix (Floor 1+).
- [x] Rework spawn tables so Floor 1 contains only entry-tier enemy families.
- [ ] Add per-floor difficulty and composition targets (trash/skirmisher/bruiser/caster/elite ratios).
- [x] Add boss lane ownership per floor so each boss is not front-loaded.
- [ ] Define modular sprite naming contract for paper-doll layers and animation sets.
- [ ] Integrate layered sprite draw pipeline for player equipment (behind/in-front ordering).
- [ ] Add graceful fallback to base full-body sprite when layer assets are missing.

Acceptance Criteria:
- [ ] Floor 1 no longer uses the full enemy catalog.
- [ ] Enemy identity feels floor-themed and progression-authentic.
- [ ] Player can visually identify at least equipped armor/head/cloak in-world.
- [ ] Missing modular sprites never break runtime rendering.

Blocked-by:
- Modular equipment art assets are required before full paper-doll implementation begins.

Progress notes (2026-03-05):
- Floor 1 encounter identity shifted to a goblin-family roster:
  - `Goblin Grunt`
  - `Goblin Skirmisher`
  - `Goblin Slinger`
  - `Goblin Supervisor`
  - floor boss: `Goblin General`
- Reworked all active Floor 1 spawn packs (entry/route/sanctum/reinforcements) to goblin-family keys only.
- Added pack-chain combat behavior for goblin encounters:
  - fights can chain into nearby goblin allies,
  - encounter size is capped to a 1-3 hostile flow.
- Updated Floor 1 loot profile to goblin-themed low-tier supplies and adjusted rarity logic by goblin tier (grunt/skirmisher/supervisor/general).
- Updated boss objective/combat text and sanctum-wave boss detection to `Goblin General`.

## Phase 7 - Tactical Dungeon Encounter Combat (Current Priority)

Goal:
- Shift combat from single-target duels to tactical dungeon encounters with movement, positioning, and legal targeting.

Scope:
- Keep combat on dungeon tiles (no detached combat scene).
- Multi-unit encounters with initiative order.
- Turn economy with movement caps and action limits.
- Targeting rules with strict range + line-of-sight + wall blocking.
- Reinforcement joins when nearby enemies detect ongoing combat.
- Spell targeting mode with legal-target preview and confirmation.

Locked design decisions:
- Movement is capped per turn (no infinite movement).
- Baseline race movement:
  - `Dwarf`: 5 tiles
  - `Human`: 6 tiles
  - `Elf`: 7 tiles
- Heavy armor movement penalty target: `-1 tile` (with minimum floor).
- Walls/blocked tiles prevent targeting and movement.
- Story expansion is deferred until combat system and floor progression stabilize.

Implementation order:
- Pass 1: encounter scaffolding (combat roster, encounter context, initial join logic).
- Pass 2: initiative + turn ownership model.
- Pass 3: movement points and reachable-tile movement.
- Pass 4: unified target validation and enemy selection UX.
- Pass 5: spell target mode with legality highlights (support layer complete).
- Pass 6: reinforcement joins and tactical AI movement/attack decisions.
- Pass 7: validation and gameplay tuning pass.

Checklist:
- [x] Capture tactical-combat design and constraints in this plan.
- [x] Start encounter scaffolding implementation in runtime code.
- [x] Add encounter initiative timeline and turn transitions.
- [x] Add per-turn movement budget and race-based movement model.
- [x] Add legal-target validator for melee/ranged/spell actions.
- [x] Add spell target mode support layer and confirmation validation flow.
- [x] Add deterministic tactical enemy movement/attack decision helpers.
- [x] Add reinforcement join checks during ongoing combat rounds.
- [x] Add self-check coverage for LOS/range/movement constraints.

Acceptance Criteria:
- [x] Player can move during combat within per-turn movement limits.
- [x] Targets behind walls cannot be attacked unless explicitly allowed by ability rules.
- [x] Encounters can include multiple enemies from the same local group.
- [x] Nearby hostiles can join an active encounter under explicit detection rules.

Progress notes (2026-03-05):
- Completed Phase 7 Pass 1 + Pass 2 scaffolding:
  - Added encounter roster context and reset/rebuild flows.
  - Added initiative order model with deterministic seed-based rolls and stable tie-breaking.
  - Added encounter turn cursor helpers (advance, prune, player/enemy cursor targeting).
  - Surfaced encounter turn diagnostics (`EncTurn`, `EncCurrent`) for runtime visibility.
- Added Phase 7 self-check coverage for initiative core:
  - deterministic ordering consistency with same seed,
  - explicit tie-break ordering behavior,
  - turn index advancement wrap-around and edge-case normalization.
- Completed Phase 7 Pass 3 movement implementation:
  - Added per-turn movement budget model using race baseline (`Dwarf 5`, `Human 6`, `Elf 7`) with heavy armor penalty floor.
  - Added reachable-tile computation and in-combat movement mode with move-point spend + world overlay.
  - Added movement diagnostics and self-check coverage for movement budgets and blocked-tile reachable sets.
- Completed Phase 7 Pass 4 target validation + target selection UX:
  - Added unified encounter target validator (`range + LOS + alive`) used by melee, spells, and enemy attacks.
  - Added combat target cycling (`LEFT/RIGHT`) and surfaced target legality in combat and spell UI.
  - Added LOS/range/dead-target self-check coverage in the phase check suite.
- Completed Phase 7 Pass 5 support layer for explicit spell targeting mode:
  - Added `EncounterSpellTargetingRules` with explicit mode helpers (`Disabled/SelectTarget/ConfirmTarget`) and deterministic target-index cycling for integration.
  - Added `EncounterSpellTargetingRangePolicy` for level-based spell range resolution (`cantrip=5`, `L1=6`, `L2=7`, `L3+=8`).
  - Extended phase self-check coverage with explicit mode, range policy, and spell-target validation behavior checks.
- Completed Phase 7 Pass 6 tactical enemy decision helper slice:
  - Added `EncounterEnemyTactics` deterministic movement helper for enemy step pathing toward a target under movement budget + blocker constraints.
  - Added tactical enemy attack feasibility helper that enforces alive-target + range + LOS gating.
  - Extended Phase 7 self-checks with tactical movement and attack-feasibility validation cases.
- Completed Phase 7 Pass 6 runtime integration:
  - Added enemy-turn phase loop that advances by initiative ownership until player turn returns.
  - Added reinforcement join checks during active combat rounds using ally-family + LOS + join-distance constraints.
  - Added tactical enemy turn behavior in runtime: enemies attempt legal repositioning before attack resolution.
  - Added enemy-phase safety cap to prevent infinite turn loops per player action.
- Completed Phase 7 Pass 7 validation+tuning slice:
  - Added reinforcement helper self-check coverage for positive join behavior (`ally match + in range + LOS`).
  - Added reinforcement helper self-check coverage for blocked join behavior (`no ally match`, `out of range`, `no LOS`, `encounter cap full`).
  - Validation run: `dotnet build -c Release` and `dotnet run -c Release -- --phase7-checks`.

Locked decisions (2026-03-05):
- Minimum feat count target for this phase is `60`.
- Feat progression baseline is creation pick + every 4 levels.
- Class bonus feats are postponed to a later phase.
- This document remains the single authoritative plan to avoid oversight/redundant work.

## Phase 8 - City Hub and Story Spine (Planned)

Goal:
- Add a city-return loop that anchors progression and delivers story beats between dungeon runs.

Scope:
- Automatic return to city after run exit/retreat milestones.
- City services:
  - healing/rest
  - vendor/shop
  - stash/inventory management
  - next-run preparation
- Story delivery in city:
  - short scene beats between runs
  - NPC dialogue progression flags
  - boss/faction breadcrumbs tied to dungeon progress
- City progression:
  - unlockable services/NPCs by milestone.

Checklist:
- [ ] Add city map/state and transition flow from dungeon to city and back.
- [ ] Add baseline city services (`Heal`, `Shop`, `Stash`, `Depart`).
- [ ] Add first narrative scene chain tied to Floor 1 completion milestones.
- [ ] Add NPC progression flags and save/load persistence.
- [ ] Add city objective UI so player always knows next step.

Acceptance Criteria:
- [ ] Player can complete dungeon segment and return to city without friction.
- [ ] City interaction changes next dungeon run readiness (supplies/build options).
- [ ] At least one story beat fires in city after dungeon progress.
- [ ] City progression state persists correctly across save/load.

Progress notes (2026-03-05):
- Added by user request as the next major expansion track after tactical-combat baseline completion.

## Global Design Rules

- Collaboration rule (user priority):
  - Be careful with plan consistency to avoid oversights and redundant work.
  - Keep exactly one authoritative plan file updated immediately after decisions.
  - Align implementation to the user's vision before adding new structure.
- Every floor must contain:
  - 1 real player choice (autonomy).
  - 1 mastery test with clear feedback (competence).
  - 1 progression reward that changes playstyle.
- Features should follow mechanics -> dynamics -> aesthetics (MDA).
- Scope control rule:
  - No Phase 4 implementation starts before Phase 3 acceptance criteria are met.
  - Phase 4B can start only after Phase 4A baseline accessibility settings are complete.
  - New ideas are added to this file first, then prioritized before implementation.
- Not in scope until later:
  - More floors.
  - Large narrative systems.
  - Complex crafting/economy.

## Research-Backed Principles Used

- Support multiple motivation types (achievement, social, immersion).
- Keep activity variety (exploration, interaction/events, combat).
- Keep progression meaningful through capability changes, not only number growth.
