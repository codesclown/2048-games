# Verification

Verified with Unity 6000.3.25f1 on macOS.

- Pure C# rules/session tests: **8,772,314 assertions passed** across 4×4, 5×5 and 6×6.
- Unity editor package import, runtime/editor compilation and project configuration: passed.
- Native macOS development build: passed.
- Native app integration runner: **PREMIUM_SMOKE_SUCCESS**, including actual UI button handlers, animated movement/scoring, tool unlock, Undo, Swap/Delete cell selection, all level dimensions, target win, keep going, game over, one-time continuation, instructions fitting, score dialog, home navigation and resume.
- Native Home, level-selector, gameplay and loss-dialog visual layouts: inspected in the running window.
- Native macOS release build: passed; development-only QA runner excluded.

The editor's headless Play-mode harness did not advance frames reliably, so native integration verification ran in the built app instead. The final build lives at `Builds/2048Premium.app`.

Live JioGames ads/profile/leaderboard were not verified or shipped: the original browser DroidHandler bridge requires a native platform SDK. Offline continuation is implemented and labeled honestly. A real rewarded-video integration can use `RewardProvider`.

## 2026-10-02 — Mobile UI foundation revision

- Rules/session suite rerun: **8,772,314 assertions passed**.
- Unity 6000.3.25f1 native development build: **PREMIUM_BUILD_SUCCESS** (`/tmp/premium-ui-verified.log`).
- Updated native integration runner: **PREMIUM_SMOKE_SUCCESS** (`/tmp/premium-ui-smoke.log`). Covers manager-backed sessions, UI handlers, animation, all three dimensions, tool selections, win/continue/loss, instructions, scores and resume.
- Home/game screenshots inspected at 600 × 960; retained in `Docs/Previews`.
- Added native GameManager/GameController separation, vector control icons, coherent palette, larger touch controls, contextual cancel, button/dialog motion and background pause behaviour.
- Mobile safe-area/responsive code remains a baseline. Physical touch-area, notch/tablet, lifecycle and animation verification on Android/iOS are **pending**.
- This Unity install includes MacStandaloneSupport only. Android/iOS compilation, signing and real device tests were **not performed**. See `PHASES.md` for release exit criteria.
- Updated native release build: **PREMIUM_BUILD_SUCCESS** (`/tmp/premium-ui-release.log`); copied to `Builds/2048Premium.app`. This is an unsigned macOS preview, not an Android/iOS release artifact.

## Phase 1 completion — 2026-10-02

Phase 1 native foundations are complete. Android/iOS compilation and physical device testing remain explicitly assigned to Phases 5 and 6.

- Pure C# rules/session suite: **8,772,314 assertions passed** after the manager/navigation/persistence refactor.
- Final Unity development build: **PREMIUM_BUILD_SUCCESS**, no C# errors/warnings (`/tmp/premium-phase1-build.log`).
- Phase 1 services: **PHASE1_SERVICE_SUCCESS: 100 checks**. Covers real native file atomic replacement, corrupt slot/temp recovery, checksum rejection, migration including integer mute preference, failed migration/write retry, future-version protection, reset, dialog history, state gates, all dimensions, inventory/history/score, rewards and save deduplication.
- Native UI/controller integration: **PREMIUM_SMOKE_SUCCESS** (`/tmp/premium-phase1-smoke.log`). Includes tiny/outside/cancelled gestures, an actual controller swipe to animated scoring, background mid-animation, overlapping pause/focus notifications, explicit resume, persisted swipe and one manager/controller, followed by existing full gameplay/UI flows.
- Retained evidence: `Docs/phase1-checks.log` contains the passing service and integration checks.
- QA intentionally simulates migration/commit failures; the corresponding warning messages are expected and are followed by successful retry checks.
- The smoke harness alone runs in the background to keep desktop verification frames advancing; release gameplay retains native background suspension and explicit Resume.
- Two independent schema-v2 files under `Application.persistentDataPath/Saves` use atomic temporary-file replacement and checksum-protected revisions. Migration reads legacy PlayerPrefs, rendering writes nothing, and committed actions/settings/lifecycle boundaries save only changed payloads.
- Physical Android/iOS file-system, rewarded SDK and lifecycle checks remain pending; native desktop success does not substitute for device QA.
- Final Phase 1 release preview: **PREMIUM_BUILD_SUCCESS** (`/tmp/premium-phase1-release.log`), copied to `Builds/2048Premium.app`; development-only QA excluded. This remains an unsigned macOS verification artifact.
