# Premium 2048 — Native Android / iOS delivery phases

This is a native Unity game using uGUI, C# rules and native touch input. Android and iOS are the release targets. A macOS player is only a development verification target. No WebView, HTML UI or browser bridge is required.

Status vocabulary: **Implemented** means code exists; **Verified** means a recorded check passed; **Pending** means required release work remains. Premium quality is a release criterion, not a claim that device QA is complete.

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

**Implemented in this revision**
- Restrained navy/slate background, coherent tile palette and champagne accents.
- Native vector menu, speaker/mute, restart, undo, swap and delete icons; no emoji or platform font glyph dependence.
- Larger main touch controls; contextual selection cancellation rather than an always-visible control.
- Full safe-area modal blocker, consistent score cards and touch-first instructions.

**Pending**
- Art-directed logo and licensed bundled typography with fallback coverage.
- Home/level cards with difficulty previews; unify spacing, typography, selected/disabled/locked states and destructive-action hierarchy.
- App icon and launch artwork: Android adaptive foreground/background plus monochrome icon, iOS app icon assets. Runtime toolbar icons are not launcher icons.
- Accessibility pass: readability, contrast, large text, localised strings, colour-independent selection cues and reduced-motion setting.

**Exit check:** review screenshots of every screen, modal and tool state on actual phones. No missing icons, clipping, ellipsis that hides meaning, or unreadable tile values.

## Phase 3 — Responsive mobile layout

**Implemented baseline**
- Canvas fits a 600 × 960 reference composition inside `Screen.safeArea`; recalculates when screen size/safe area changes.
- Board stays square for 4×4, 5×5 and 6×6; tile text adapts to tile size/value.
- Native portrait configuration; modal input blocker stretches across the safe region.

**Pending**
- Replace fixed-coordinate composition with explicit compact-phone, tall-phone and tablet layout rules. Current proportional fit is a baseline, not proof of perfect responsiveness.
- Guarantee at least 44-point iOS / 48-dp Android interactive areas on each supported device, including dialog and secondary buttons.
- Validate notches, Dynamic Island, gesture navigation, Android system bars, cutouts, tablets, split-screen and system keyboard interruption.
- Define supported aspect-ratio range and deliberate tablet layout, then test all supported sizes.

**Exit check:** no overlap or offscreen controls at compact 16:9, tall 19.5:9/20:9 and tablet 4:3. Every action is reachable with the system safe areas applied.

## Phase 4 — Animation, audio and tactile polish

**Implemented**
- Existing slide, merge pop, spawn scale and floating score animation retained.
- Added unscaled button press/release easing and dialog fade/scale reveal.
- Native sound toggle icon tracks mute state; existing local audio preference retained.

**Pending**
- Home/game transitions, level selection feedback, undo/swap/delete animation and win celebration with input gates and cancellation.
- Curated licensed audio; native Android/iOS haptics adapter and persistent toggle.
- Reduced motion, no-flash and audio-interruption behaviour; synchronise feedback with successful actions only.
- Pool tile ghosts and generated visual assets; profile animation on low/mid-range hardware.

**Exit check:** repeat rapid input, pause mid-slide, restart during animation, open/close dialogs and resize. No stuck input, orphan tiles, overshoot or state mismatch; sustained target frame rate on supported hardware.

## Phase 5 — Native platform builds and integrations

**Pending — mobile platform modules absent on this machine**
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

Phase 1 is complete. Prioritise Phase 3 physical touch/layout validation next. Complete Phase 2 art and Phase 4 motion polish next. Run Phase 5 real mobile builds early enough to catch platform issues; finish Phase 6 QA before Phase 7 submission. No phase is complete merely because it appears in this document.
