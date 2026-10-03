# Premium 2048 — Native Android / iOS delivery phases

This is a native Unity game using uGUI, C# rules and native touch input. Android and iOS are the release targets. A macOS player is only a development verification target. No WebView, HTML UI or browser bridge is required.

Status vocabulary: **Complete** means implementation and verification passed; **Implemented** means code exists; **Verified** means a recorded check passed; **Pending** means required release work remains. Premium quality is a release criterion, not a claim that device QA is complete.

## Phase 1 — Native foundations and gameplay ownership

**Complete — implementation and native desktop integration verified on 2026-10-02.** Physical Android/iOS build and device validation remain in Phases 5 and 6.

- `GameManager` owns the live session, new game, move dispatch, Undo/Swap/Delete, continuation, settings, reset and score/win accounting. The view never mutates gameplay rules.
- `GameController` owns board-only single-finger swipe, DPI fallback/clamping, tap targeting, multi-touch cancellation and lifecycle cancellation. Desktop keys/mouse compile only for editor/standalone testing.
- `GameNavigation` owns Loading, Menu, Playing, Animating, Selecting, Rewarding, Paused and Result states, screen routing, dialog history and input gates. Nested help/scores return to their parent menu.
- Committed actions publish `GameManager.Changed`; navigation publishes `Changed`. UI rendering and animation completion do not write saves. Invalid moves, cancelled selections and repeated unchanged lifecycle saves do not write.
- `GamePersistence` saves session, history, inventory, statistics and audio preference in one schema-v2 transaction. Two independent native files alternate revisions, use checksums and atomically replace a temporary file. A corrupt newest record or leftover temporary file falls back to the previous valid commit.
- Legacy v1 PlayerPrefs data and the integer sound preference migrate together. Legacy data stays available if migration cannot commit. Future-version records are preserved against older-app overwrites; failed writes keep the live game and retry on the next action/lifecycle boundary.
- Reset clears both slots, temporary files and legacy keys so old progress cannot reappear.
- Backgrounding cancels selection, resolves tile visuals, pauses gameplay and saves. Overlapping focus/pause notifications remain suspended until both clear; foreground requires explicit Resume.
- Reward callbacks use request/session guards; failure, timeout and duplicate completion cannot grant a second continuation.
- Singleton/duplicate-component guards retain one active game root, manager and controller. Native smoke checks confirm one manager and one controller in the running scene.
- `Board` and `Session` stay independent C# rules across 4×4, 5×5 and 6×6.
- Provisional Android/iOS identifiers and portrait orientation are configured through the project setup tool.

**Exit evidence:** rules suite, dedicated Phase 1 service checks and real native UI/controller smoke checks cover all dimensions, valid/invalid gestures, animation input gates, Undo/Swap/Delete, win/keep-going/loss, one-time continuation, save migration/recovery, navigation and background/foreground. See `VERIFICATION.md` for the current recorded results. Native rewarded-SDK and physical mobile lifecycle behaviour are still Phase 5/6 checks.

## Phase 2 — Premium UI and icon system

**Complete — implementation and native integration verified on 2026-10-02.**

- **Art-directed brand and licensed bundled typography:** Art-directed monogram tile mark and "2048" wordmark in champagne gold. Bundled SIL Open Font License fonts (`NotoSans-Regular.ttf`, `NotoSans-Bold.ttf`) with multi-tier fallbacks (LegacyRuntime and OS fonts).
- **Home and level cards with difficulty previews:** Unified cards for Classic (4×4, Target 2048), Advanced (5×5, Target 4096), and Expert (6×6, Target 8192) with mini grid previews, gold start tiles, target indicators, and active "CURRENT BOARD" selection frame badges.
- **Unified controls, states, and destructive-action hierarchy:** Consistent `PremiumControl` styling across Primary, Secondary, and Destructive (`#713A48`) tones. Distinct selected, locked, ready, and empty state badges. Destructive actions ("Clear scores", "Restart") clearly isolated with secondary dismiss buttons.
- **Native vector icon system:** 12 procedural vector UI icons (`Menu`, `Sound`, `Mute`, `Restart`, `Undo`, `Swap`, `Delete`, `Settings`, `Play`, `Check`, `Lock`, `Trophy`) generated via `VertexHelper` mesh without platform font or emoji dependencies.
- **App icon and launch artwork assets:** Platform-ready launcher and store assets configured: Android adaptive icons (foreground, background, monochrome) and iOS complete app icon set (1024×1024 marketing + 13 device resolutions in `AppIcon.appiconset`), plus bundled 512×512 app icon and portrait launch splash.
- **Accessibility pass:** Full accessibility compliance suite:
  - **Localisation:** Complete English and Spanish strings catalog (`Assets/Resources/Text/strings.json`) with formatted runtime parameter translation.
  - **Readability & Contrast:** WCAG AA contrast ratio compliance (all tiles ≥ 4.5:1, ink-to-text ≥ 7:1 AAA). Dedicated High Contrast mode with monochrome/duotone tiles and deep ink text.
  - **Large Text:** 1.15× proportional scale with automatic bounding box handling and best-fit limits.
  - **Colour-independent selection cues:** Numbered step badges ("1") and selection frames for Swap/Delete targeting; explicit text status badges ("SELECTED", "LOCKED", "READY", "EMPTY", "ON", "OFF").
  - **Reduced Motion:** Instant move execution without animation lag, instant score gains, suppressed dialog zoom, and clamped scroll physics.

**Exit evidence:** 214 Phase 2 service checks and end-to-end integration smoke runner pass with zero errors. See `VERIFICATION.md` and `Docs/phase2-checks.log`. Physical Android/iOS device QA remains scheduled for Phase 6.

## Phase 3 — Responsive mobile layout

**Complete — implementation and native integration verified on 2026-10-02.**

- **Explicit device layout engine (`PremiumLayout.cs`):** Dedicated profile categorization for `CompactPhone` ($1.70 \le \text{aspect} < 1.95$, e.g. 16:9), `TallPhone` ($\text{aspect} \ge 1.95$, e.g. 19.5:9, 20:9, 21:9), and `Tablet` ($\text{aspect} < 1.70$, e.g. 4:3, 16:10, 3:2).
- **Guaranteed interactive touch targets ($\ge 48\times48$ pt/dp):** Every button across the game (main buttons $500\times80$, action powers $155\times80$, modal buttons $456\times66$–$456\times132$, icon controls $74\times74$, close button $180\times56$, cancel selection $240\times52$) strictly satisfies and exceeds the 44pt iOS / 48dp Android minimum touch target standard.
- **Safe area & cutout isolation:** Dynamic safe anchor computation isolates notches, cutouts, Dynamic Island, and bottom gesture navigation pills (`anchorMin`/`anchorMax` clamped inside `Screen.safeArea`), ensuring zero clipping or occluded controls.
- **Mathematical board geometry & squareness:** Clean cell metrics calculation for $4\times4, 5\times5, 6\times6$ grids ($496\text{px}$ content area, $10\text{px}$ spacing, centered in $520\text{px}$ board frame). Cell coordinates verified strictly bounded without overlap.
- **Responsive scale matrix:** Verified content bounds across all standard aspect ratios: Compact 16:9 ($1080\times1920$), Tall iPhone 15 Pro ($1179\times2556$ with $132\text{px}$ top notch and $102\text{px}$ home bar), Tall Android 20:9 ($1080\times2400$), iPad 4:3 ($1536\times2048$), and Android Tablet 16:10 ($1200\times1920$).

**Exit evidence:** 272 Phase 3 service checks, 214 Phase 2 checks, 100 Phase 1 checks, and end-to-end integration smoke runner pass with zero errors. See `VERIFICATION.md` and `Docs/phase3-checks.log`. Physical Android/iOS device QA remains scheduled for Phase 6.

## Phase 4 — Animation, audio and tactile polish

**Complete — implementation and native integration verified on 2026-10-02.**

- **Zero-allocation ghost tile pooling (`ghostPool` / `activeGhosts`):**
  - High-performance slide animations reuse pre-instantiated RectTransforms via LIFO pool without runtime allocations or GC pressure.
  - Clean lifecycle recycling guarantees zero orphan or lingering tiles upon rapid swiping, pause interruption, or level restarts.
- **Tactile feedback engine (`PremiumHaptics.cs`):**
  - Native tactile impact triggers (`Light`, `Medium`, `Heavy`, `Success`, `Warning`, `Error`) with iOS `UIImpactFeedbackGenerator`/`UINotificationFeedbackGenerator` and Android `Vibrator` fallback.
  - Respects global `Haptics` setting and `ReducedMotion` preference.
- **Audio & tactile feedback synchronization:**
  - Harmonic procedural tones for slide, merge pop (pitch-scaled with tile exponent), power activation (Undo board pulse, Swap, Delete), win fanfare, and button presses.
  - Audio interruption handling (`OnApplicationFocus`/`OnApplicationPause`) immediately stops audio sources on backgrounding.
- **Schema v2 persistence for tactile preferences:**
  - `haptics` boolean field persisted and verified in atomic alternating save transactions.
  - Full English/Spanish localized settings item in `strings.json` ("Haptic feedback" / "Vibración háptica").
- **Animation gating and interruption safety:**
  - Board input strictly locked during animation coroutines (`State == GameState.Animating`).
  - `FinishAnimation()` completely clears animating states, halts active coroutines, resets grid scale to $1.0$, and recycles all ghost tiles cleanly.
- **Reduced motion integrity:**
  - Moves and score changes resolve immediately without running coroutines when `ReducedMotion` is active.

**Exit evidence:** 23 Phase 4 service checks (100 Phase 1 + 214 Phase 2 + 272 Phase 3 + 23 Phase 4 = 609 checks) and live UI gameplay/gesture smoke runner pass with `PREMIUM_SMOKE_SUCCESS`. See `VERIFICATION.md` and `Docs/phase4-checks.log`.

## Phase 5 — Native platform builds and integrations

**In progress — Android module, SDK/NDK and OpenJDK are installed, and development plus non-development IL2CPP ARM64 APK builds passed; iOS module and physical-device release checks remain pending.**
- Install Android/iOS Unity platform modules and required SDK/toolchains.
- Android: IL2CPP ARM64 release build, store-required target/min SDK, AAB, signing keystore, adaptive icons and manifest checks.
- iOS: IL2CPP device build, Xcode export, bundle identifier ownership, signing/provisioning, icons, deployment target and archive.
- Keep app identifiers provisional until publishing account ownership is confirmed.
- Integrate actual native rewarded SDK through `RewardProvider` only if ads are part of the product. Validate success, cancellation, failure, timeout and duplicate callbacks on device.
- Add native platform leaderboard/account services only if required. Browser `DroidHandler` is not a mobile Unity integration.

**Exit check:** signed installable Android build and iOS device/TestFlight build run without browser dependencies. Background/foreground, offline play, audio interruptions and save/resume pass on physical devices.

## Phase 6 — QA, performance and accessibility

**Verified baseline:** pure C# rules suite passes 8,772,314 assertions for all board dimensions after this revision. See `VERIFICATION.md` for current Unity build/integration status.

**Pending**
- Device matrix: compact/tall Android, low/mid-range Android, notched iPhone and iPad if supported.
- Cold/warm launch, corrupted save, reinstall/update policy, long sessions, high tile values, all tool inventories and one-time continuation.
- Memory/GC, CPU/GPU, battery, suspend/resume, release IL2CPP stripping and crash logs.
- Screen reader/navigation feasibility, contrast, localisation expansion and reduced-motion review.

**Exit check:** no known critical/high-severity defects; documented device evidence and performance budgets. Desktop smoke checks alone do not satisfy mobile QA.

## Phase 7 — Store-ready release

**Pending**
- Final app icon, launch experience, screenshots, descriptions and age/content ratings.
- Privacy disclosures and platform data-safety forms based on the SDKs actually shipped.
- Version/build numbering, release signing, save migration, store metadata and rollback plan.
- Android internal testing and iOS TestFlight acceptance before store submission.

**Exit check:** reviewed signed release artifacts and device QA evidence. Publishing requires the owner's store accounts and release authorization.

## Priority order

Phases 1, 2, 3, and 4 are complete. Next prioritize Phase 5 (native Android/iOS platform builds and mobile SDK toolchains) when platform modules are available, followed by Phase 6 comprehensive device QA and Phase 7 store release.

## 2026-10-03 — Design and completeness revision

- Replaced mixed neon/gradient styling with the ink/parchment/amber kit and matching launcher artwork.
- Rebuilt all fourteen scenes and prefabs from the real runtime view; shared responsive preview positioning replaces the separate scene design.
- Added saved-board replacement confirmation, clear-progress/continuation preview scenes and About/version/font credits. Pause includes Restart.
- Live home preview supports every grid size; sound icons stay synchronized; results show score, moves and best tile.
- Added Swap/Delete transitions, prewarmed ghost pooling, lightweight victory celebration and one-swipe buffering with lifecycle cancellation.
- Enlarged controls to 72 virtual units at the 540-unit phone reference width; device QA is still required.
- Verification and current Android build outcome are recorded in `Docs/GAME_DESIGN.md` and `Docs/game-design-checks.log`.

The older phase entries record their historical evidence. Current haptics use Unity's vibration fallback; fine-grained iOS/Android impact behaviour needs a platform bridge/device review. Mobile frame time, signed iOS export and store publication are not established by desktop smoke tests.

### Animation regression correction
Restored visible smooth slides after board rebuild/restart by correcting pooled tile draw order. Source-frame jumps removed; 160ms slide and 160ms merge/spawn settle. Home staggered entrance and restrained brand motion added. All 12 dimension/direction combinations, home and reduced motion verified by the native `-premiumMotionSmoke` runner.

Slide timing tuned on request: 120ms movement + 100ms merge/spawn settle, retaining smoothstep interpolation and source-frame/render-order fixes.

## Expedition feature roadmap
The separate [2048 Expedition phase plan](Docs/EXPEDITION_PHASES.md) defines E1–E10 for the mission/locked-cell mode, ten authored missions, journey progression, daily puzzles, native UI and release QA. All Expedition phases are currently **Pending**; existing Classic phase completion does not imply Expedition is implemented. Start with E1 architecture and mode separation. No automatic APK builds.
