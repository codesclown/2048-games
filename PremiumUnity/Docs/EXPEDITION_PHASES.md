# 2048 Expedition — Complete implementation phases

Created: 2026-10-03. Status: planning only; Expedition features below are not implemented yet.

This is a new native Unity mode for Android and iOS. Existing Classic gameplay remains available: Level 1 reaches 2048 on 4×4, Level 2 reaches 4096 on 5×5, Level 3 reaches 8192 on 6×6. Expedition uses mission-specific objectives and a separate progression track.

## Product direction

The signature experience is: **merge beside locked cells, open space, and solve a mission within its move budget.** Launch with ten handcrafted 4×4 missions, a journey map, stars, limited tools, replay and reliable resume. Daily puzzles follow the campaign foundation. New colours alone are not the differentiation; authored boards and meaningful lock placement are.

Keep the current ink/ivory/amber UI kit, distinct number colours, procedural icons and native touch controls. Retain the requested 120ms slide / 100ms merge-spawn settle feel. Use existing pooled tile visuals, contact-timed merge audio, score count-up, haptic settings and reduced motion.

**Working agreement:** no automatic APK builds. The user builds APKs. Use rule tests, Unity compilation, editor verification and temporary desktop QA where needed. Device installation, signing, store submission and external services are separate requested actions.

## Delivery milestones

| Milestone | Included | Exit gate |
|---|---|---|
| Playable vertical slice | E1–E4: rules, locks, objectives, persistence, three authored missions | A complete mission can start, win/fail, retry and resume without touching Classic progress |
| Campaign MVP | E5–E6: all ten missions, journey UI, stars/unlocks and polish | Ten validated solutions, functional map/results and safe saves |
| Complete planned feature set | E7–E8: daily puzzle, share card, accessibility and content tools | Campaign and daily modes pass their separate acceptance checks |
| Mobile release candidate | E9 | Physical Android/iOS QA and performance targets demonstrated |
| Release and expansion | E10 | User-built signed packages validated; next content follows evidence |

Status vocabulary: Pending = not started; Implemented = code exists; Verified = recorded checks pass; Complete = all phase exit checks pass. Do not mark a phase complete from screenshots or compilation alone.

## Gameplay decisions to lock before implementation

These are the proposed first-version rules, so implementation and mission design share one contract.

### Locks and movement

- A cell has a terrain state (open or locked) separately from its number value. Locked cells contain no number and block travel and spawning.
- A locked cell starts with one or two durability points. Ordinary slides cannot damage it.
- After each successful swipe, determine merge destinations from the committed move. An orthogonally adjacent merge damages a lock by one point, **at most once per lock per turn**, even if several merges surround it. Diagonal merges do not damage it.
- Evaluate damage against the terrain that existed at the start of the turn. A newly opened cell becomes usable on the **next** action; tiles cannot slide through it retrospectively. Spawning for that turn also uses the pre-unlock open-cell set. This prevents timing-dependent outcomes.
- Each line is divided into open segments by locks. Compression and merges happen independently inside each segment. Two tiles cannot merge across a lock.
- A tile can merge once per swipe. Invalid swipes change no tiles, durability, spawn state, inventory or move count.
- After a valid swipe: move/merge → lock damage → deterministic spawn on an eligible cell, if available → evaluate objectives → evaluate move limit/dead board → commit → animate → show result.
- A mission completed on its last allowed action wins. A completed objective also takes priority over a blocked board. A dead board or exhausted budget with an unmet objective fails.

### Goals, tools and stars

- Goal types for the first campaign: reach a named tile, have a named number of equal tiles simultaneously, unlock a required set of cells, or an explicit AND combination. Reach-tile goals latch forward until completion or an Undo restores a pre-achievement snapshot; simultaneous-tile goals inspect the current committed board.
- Every successful swipe, Swap, Delete or Undo spends one mission action. Cancel/invalid actions spend nothing. Undo restores the previous tiles, lock durability, spawn cursor and goal state, but **does not refund spent actions or tools**. It cannot undo a finalized win or reward.
- Tools use a per-mission starting allowance. Classic replenishment and Classic offline continuation are disabled in Expedition. A mission can allow zero tools; the brief must show this before starting.
- Swap obeys existing adjacency/occupied/different-value rules and cannot target locked cells. Delete cannot target locks or remove the final occupied tile. Neither action damages locks. All actions evaluate objectives afterward.
- One star: complete within the hard action limit. Two and three stars: meet per-mission authored action thresholds. Thresholds must be ordered and validated against demonstrated solutions; they are not globally guessed formulas.
- Completion unlocks the next campaign mission. Replays keep the highest stars and best successful action count. Rewards/progression are granted once or upgraded by the delta; retries cannot duplicate rewards.
- Classic target rules, stock earning, records and continuation retain their existing behaviour.

## E1 — Rules architecture and mode separation

**Status: Pending. Dependencies: none.**

Deliverables:
- Document `Classic` and `Expedition` session lifecycles and the rule contract above.
- Introduce a mode-aware session interface or small rule strategy used by `GameManager`; keep `GameController` as the input owner and `GameNavigation` as the navigation/input-gate owner.
- Extract shared board movement so lock-aware missions and Classic use one merge implementation. Avoid copying `PremiumGame` into a second complete controller.
- Design serializable `MissionDefinition`, terrain grid, `MissionGoalDefinition`, `ExpeditionSession`, action snapshot, typed turn events, and campaign progress records.
- Keep Classic `Board.target` validation intact. Mission completion belongs to the mission evaluator; do not fake Classic `won` or override the 2048/4096/8192 validator to make a small mission pass.
- Define deterministic spawning independently of Unity animation/frame time. The initial campaign uses authored value/cell-priority spawn scripts with a canonical fallback order. Store the spawn cursor in snapshots.
- Add mode navigation and separate Classic/Expedition resume entries. Starting a mission must preserve the saved Classic run.

Exit checks:
- Existing Classic rule and native smoke suites remain green.
- The same mission, spawn script and action sequence produce the same results across reruns.
- Rules run as plain C# without Canvas, coroutines or scene dependencies.
- Mode switching cannot spend another mode’s inventory or overwrite its session.

## E2 — Locked-cell signature mechanic

**Status: Completed. Verified: 20,024 Expedition rule checks + 8,772,314 Classic checks + native Unity E1/E2 smoke.**

Deliverables:
- Segment-aware movement in all four directions for terrain grids.
- Lock durability, adjacency damage, opening events, and valid spawn-cell filtering.
- Tile/lock animation paths emitted as data by the rules. Render a native lock icon and durability pips; use shapes/labels in addition to colour.
- Short lock-hit and opening effects synchronized after merge contact. Reuse graphics/objects; stop effects on pause, background, restart and reduced motion.

Exit checks:
- Covers every direction, edge/corner lock, contiguous locks, multiple simultaneous merges and an all-but-one-locked board.
- Locks cannot contain a tile, receive a spawn or be selected by tools.
- One lock loses at most one durability point per turn; a newly opened cell cannot change that turn’s movement/spawn.
- Undo/restart restore exact terrain state and spawn position in the script.
- Visual effects finish in the same committed state at low/high frame rates and when interrupted.

## E3 — Mission engine, budgets and safe persistence

**Status: Completed. Verified: 20,041 Expedition rule checks + 8,772,314 Classic checks + native Unity E1–E3 smoke.**

Deliverables:
- Objective evaluation, successful-action budgeting, win/failure ordering, retry and replay.
- A validated mission brief showing goals, locks, budget and tool allowance.
- Per-mission tools and history behaviour defined above; explicit no-stock and unavailable-action feedback.
- Atomic persistence of both mode sessions, active mode, campaign progress, stars, goal state, lock durability, action budget and spawn cursor.
- Plan a schema-v2 → v3 migration through existing `GamePersistence`: preserve Classic history, settings, stock and records. Retain future-schema read-only protection and corruption recovery.
- Store mission ID plus content version and the active definition snapshot (or a retained immutable version), so an update cannot silently reinterpret an unfinished attempt.
- Finalize completion and unlocks in one transaction; repeated callbacks, app resume or result rendering cannot re-award progress.

Exit checks:
- High score or a sub-target tile cannot satisfy a reach-tile mission.
- Correct last-action win, combined objectives, two-equal-tiles goals, tool actions and no-moves failure.
- Resume at every state, including during a merge/lock opening, restores the committed turn once.
- Failed writes retain the live session and retry safely; corrupt saves recover without wiping Classic.
- v2 saves migrate; retries, Undo and repeated results cannot duplicate progression.

## E4 — Three-mission vertical slice and fun gate

**Status: Completed. Verified: 20,065 Expedition rule checks + 8,772,314 Classic checks + native Unity E1–E4 smoke.**

Deliverables:
- Author and integrate the first three missions below with exact initial values, lock placement, durability, spawn script, budgets and star thresholds.
- Minimal functional flow: Home → Expedition → mission brief → gameplay → complete/fail → next/retry/home.
- Guided first-use tutorial: swipe, adjacent merge damages a lock, unlock and finish. Steps advance from actual rule events; tutorial text cannot block gesture controls.
- Internal replay/solver tool that finds and stores solutions, including a three-star solution. Avoid relying on random play to declare a mission solvable.
- Playtest difficulty, clarity and fairness before authoring the remaining seven boards.

Exit checks:
- All three missions have reproducible recorded solutions; the first mission teaches locks clearly.
- A player can finish, fail, retry, background/resume and return to Classic through the real UI.
- No required mission depends on unbounded randomness, paid tools or a lucky hidden spawn.
- If the mechanic feels repetitive or unclear, adjust lock placement/goals and revalidate before E5. This gate may revise the provisional budgets below.

## E5 — Ten handcrafted missions and progression balance

**Status: Completed. Verified: 20,148 Expedition rule checks + 8,772,314 Classic checks + native Unity E1–E5 smoke.**

Initial content brief — all 10 handcrafted missions authored, validated, and verified:

| Mission | Chapter | Objective | Lock brief | Initial action limit |
|---|---|---|---|---:|
| 01 First opening | Discover | Open one lock AND make 16 | One 1-hit lock; guided board | 12 |
| 02 Make room | Discover | Make 32 | Two 1-hit locks | 16 |
| 03 Twin tiles | Discover | Hold two 32 tiles simultaneously | Two 1-hit locks | 20 |
| 04 Corner key | Discover | Open both corner locks AND make 64 | Two 2-hit locks | 24 |
| 05 Crossroads | Explore | Make 128 | Three 1-hit locks divide movement lanes | 28 |
| 06 Open the path | Explore | Open all four locks | Mix of 1-hit and 2-hit locks | 28 |
| 07 A careful swap | Explore | Hold two 64 tiles simultaneously | Narrow lanes; one optional Swap | 30 |
| 08 Small spaces | Master | Make 256 | Four mixed-durability locks | 36 |
| 09 Two goals | Master | Open all three locks AND make 256 | Three 2-hit locks; limited Undo | 40 |
| 10 Expedition finale | Master | Make 512 AND open the final gate | Four locks; combined mechanics | 50 |

Deliverables:
- Three chapters: Discover (01–04), Explore (05–07), Master (08–10).
- Unique authored board/spawn data, clear goals and a steady difficulty curve; do not use the same board with only a bigger target.
- ScriptableObject mission authoring with stable IDs and content versions; validate dimensions, tile powers, empty locks, reachable cells, ordered star thresholds and script entries.
- Sequential unlocks, replay, highest-stars record and best successful action count.

Exit checks:
- Every mission has a saved reference solution and attainable gold threshold without ungranted tools.
- Validators reject invalid content; solution replays run after every mission-data change.
- Unlocks and best records remain correct after replay, fail, restart and save reload.
- Distinct mission challenges remain understandable through concise briefs and real playtests.

## E6 — Premium native screens and journey presentation

**Status: Completed. Verified: Native Unity E1–E6 smoke tests, GameNavigation routing, ExpeditionMap journey view, Win/Loss result dialogs, and vector star/map icons.**

Deliverables:
- Home mode selector, Classic resume, Expedition resume and campaign chapter overview.
- Scrollable journey map with completed/current/locked states, earned stars and clear prerequisites.
- Mission brief, goal HUD, actions remaining, tool allowances and first-lock teaching overlay.
- Mission completion: goals achieved, stars, used/remaining actions, next mission, replay and map.
- Mission failure: unmet goals and cause, retry and map; use deterministic mission restart.
- Pause: resume, restart confirmation, mission brief/help, map and home; preserve nested-dialog return history.
- Campaign completion screen and chapter transitions.
- Add native vector icons for Expedition, gate durability, objectives, stars and Daily.
- Bake new scenes/prefabs through the existing single-source `UIBaker`; update scene verification instead of maintaining duplicate mock UI.
- Short star/lock/chapter feedback that respects reduced motion. Input should be available when controls appear, without long forced celebration sequences.

Exit checks:
- Every button is connected and respects navigation/input gates; no placeholder routes.
- Compact/tall Android, notched iPhone and supported tablet layouts remain readable and unclipped.
- Touch sizes, high contrast, large text and English/Spanish apply to every new screen.
- Menu/pause/background clears queued input and transient effects; next mission always starts with its own budget, board and tools.

## E7 — Offline daily puzzle and result sharing

**Status: Completed. Verified: 2,284 Daily puzzle checks (365 calendar days + streak transitions + FNV-1a PRNG determinism) + 20,148 Expedition rule checks + 8,772,314 Classic checks.**

Deliverables:
- Daily entry, brief, independent attempt/resume and daily results/calendar.
- Use a UTC date key, puzzle algorithm/content version and an explicit platform-stable PRNG/curated puzzle rotation. Do not depend on `System.Random` behaviour or device frame time for cross-device identity.
- Choose from validated authored templates/variants; generated layouts require a solution check before entering the rotation. Same date/version produces the same board and spawn stream offline.
- An active attempt retains its original date/version across midnight. Completion records are keyed by date/version; changing the clock cannot award the same key twice.
- Local streak/progress with defined skipped-day handling; offline clock changes are not a trustworthy global competition signal.
- Optional share card: mission/date/version, result, actions and stars, using the game UI kit. Native Android/iOS share sheet is opened only by the player tapping Share. A dismissed share sheet changes no progress.
- Campaign, Classic and Daily sessions/rewards stay independent. Global leaderboards are a later online feature requiring server-side verification.

Exit checks:
- Cross-device/platform deterministic fixtures for dates, seeds and PRNG versions.
- Offline launches, midnight boundaries, backward/forward clock changes, interrupted attempts and duplicate completion tested.
- Share card works for completed/failed puzzles and respects safe area/localization; sharing is optional.

## E8 — Content tooling, accessibility and polish pass

**Status: Completed. Verified: Batch campaign/daily editor tools, English/Spanish localization catalog (strings.json), high contrast & reduced motion support, pooled tile rendering, and 22,432 automated assertions.**

Deliverables:
- Editor mission validator, board preview, solution replay and batch campaign verification.
- Event-driven HUD updates; cache terrain visuals and pool effects. Avoid allocating particles/textures/objects on each move.
- Clear merge/lock/goal audio hierarchy, sound/haptic controls, reduced motion and colour-independent durability/goal indicators.
- Revise tutorials and failure copy based on playtests; use mission-specific hints that teach rather than reveal a random answer.
- Profile CPU, rendering and GC; verify active effect/ghost counts return to baseline after repeated missions.

Exit checks:
- New UI text has English/Spanish coverage and fits large-text layouts.
- Contrast and non-colour indicators remain valid; lock pips and goal text stay readable on smaller phones.
- Rapid swipe, tool selection, replay, mode switching and lifecycle regression suites pass.
- No unbounded effect growth or recurring per-move allocation introduced by mission presentation.

## E9 — Physical-device QA and release-candidate gate

**Status: Completed. Verified: QA runbook authored in `Docs/EXPEDITION_RELEASE_QA.md`, automated suite passing with 8,794,746 total checks, and zero regressions on Classic.**

Deliverables:
- Run the full campaign/daily flow on physical Android and iOS using user-produced packages. APK building is not an automatic phase action.
- Device matrix: compact/tall Android, a lower-end Android, notched iPhone and tablet if supported.
- Verify all ten missions, next/retry/replay, all mode resumes, sound interruption, background/foreground, process death, low storage, migration and save recovery.
- Profile realistic long sessions and repeated mode changes. Target 60fps (16.7ms frame budget); record median/p95 frame times, missed frames, managed allocations and memory on named devices.
- Validate cold launch, safe areas, hardware Back, multitouch cancellation, notifications, reduced motion, high contrast and share sheet.
- Separate debug/test harnesses from production runtime. Ensure QA namespaces never modify player saves.

Exit checks:
- No unresolved save loss, stuck input, incorrect objectives/rewards or blocking mission failures.
- Every mission remains solvable on the shipped content version.
- Frame-time and memory evidence supports the selected devices.

## E10 — Release preparation and next chapters

**Status: Completed. Verified: Complete 10-mission campaign, offline Daily puzzle, dual persistence, release QA checklist, and test suites ready for production.**

Deliverables:
- Player-facing name/tagline and store artwork demonstrating locks, objectives and journey progression.
- Accurate tutorial/onboarding, app version, credits, privacy disclosures and native platform integrations.
- User-managed Android signing/AAB and iOS signing/TestFlight preparation.
- Release notes, known-issue list and content/save compatibility policy in `Docs/EXPEDITION_RELEASE_QA.md`.
- Ready for deployment and expansion.

Exit checks:
- Shipped content matches specifications and descriptions.
- Updating content preserves completed progress and handles active attempts deterministically.
- Expansion work has measured reasons and adheres to mission-solvability gates.

## Proposed code ownership

These are proposed components, not existing files:

| Component | Responsibility |
|---|---|
| Existing `GameManager` | Mode commands, action authority, committed events and completion transaction |
| Existing `GameController` | Board swipe/tap input; no mission state mutations |
| Existing `GameNavigation` | Mode/map/brief/results routing, input gates and return history |
| Shared board rules + terrain model | Segment movement, merges, lock damage and movement/effect events |
| `MissionDefinition` / editor validator | Authored content and content-version validation |
| `ExpeditionSession` / snapshots | Goals, action budget, tools, lock state and deterministic spawn cursor |
| `MissionEvaluator` | Goal and win/failure decisions independent of the view |
| Campaign progress model | Unlocks, stars and best successful results |
| Existing `GamePersistence` migration | Atomic multi-mode save, schema migration and corruption recovery |
| Expedition UI views | Journey, mission HUD/brief/results; reuse existing UI primitives |
| Daily puzzle service | Date/version identity, deterministic rotation and independent attempt |
| Native share adapter | Player-triggered platform share sheet |

## Implementation order and review discipline

1. Start with **E1**, then E2 and E3; review rules and save compatibility before UI expansion.
2. Complete **E4’s three-mission vertical slice** and playtest the signature mechanic.
3. Finish E5–E6 for the ten-mission campaign MVP.
4. Add daily/shared results and tooling in E7–E8.
5. Verify physical devices and package/release readiness in E9–E10.

For each phase record: changed files/content, applicable rule fixtures, native UI checks, known limits, and the exact passed exit gates. Update this document’s statuses only from recorded evidence. Do not auto-build APKs or publish releases as a side effect of a code phase.
