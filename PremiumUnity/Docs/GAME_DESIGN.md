# Native game design revision — 2026-10-03

The game uses an ink, parchment and amber palette. The board carries the colour; surfaces and controls stay quiet. Tile colours follow a warm progression into restrained sage/ink at high values. Numbers choose the stronger text contrast, with every configured tile above 4.5:1. Noto Sans regular/bold establishes a clear hierarchy. Launcher, adaptive, monochrome, iOS icon-set and portrait artwork are rendered from the same tile monogram.

## One native UI kit

`PremiumGame` constructs the real uGUI view. `UIBaker` derives scenes/prefabs from that view, replacing the old separate mock design. Preview data uses an in-memory store and never overwrites player saves. `PremiumPreviewLayout` calls the same positioning function as runtime. Touch controls use at least 72 design units on the 540-unit phone width, equivalent to 48 points on a 360-point viewport. Physical-device DPI/accessibility testing remains necessary.

## Complete offline loop

Home/live resume preview; 4×4, 5×5 and 6×6 selection; saved-board replacement confirmation; gameplay; pause with restart; rules with scrolling; records/statistics; settings; clear-progress confirmation; restart confirmation; victory/endless; loss/retry; one-time continuation confirmation; About with version/font credits. Settings include sound, haptics, reduced motion, large text, high contrast and English/Spanish. Optional reward-provider states remain available when a native SDK is configured.

Home preview reflects the current grid dimension and board. Sound controls stay synchronized. Result cards show actual score, move count and best tile. Selecting a new board preserves the old board until confirmation; cancelling returns to the level selector. Restart confirmation from pause preserves navigation history.

## Motion and responsiveness

120ms smoothstep slides followed by 100ms merge/spawn feedback; 160ms Swap/Delete motion; brief Undo pulse, score lift, button press and 200ms panel reveal. Victory uses a single celebration mesh. Reduced motion resolves gameplay instantly and suppresses effects. One extra swipe may be buffered during a move; pause/background/result clears it. Board input is gated during power animation.

Target frame rate is 60fps. Ghost tiles are prewarmed, resized when reused for another board dimension and retained across board rebuilds. Moves reuse the existing ghost list. Stable buttons/panels skip repeated transform writes; home rebuilds only when needed. These are implementation optimizations, not measured mobile frame-time guarantees.

## Verification and release boundary

See `game-design-checks.log` and `Previews/AllScreens/` for final verification evidence. Native macOS smoke checks cover all board sizes, settings, power animations, save/lifecycle, confirmation/cancel, About routing and buffered swipes. Final native smoke run passed 623 checks. Android IL2CPP ARM64 development and non-development APK builds both passed. The installable preview APK is `Builds/Android/2048-Premium-preview.apk` (15,262,476 bytes); manifest verification confirms the Unity native activity, ARM64, target SDK 36, minimum SDK 25 and vibration permission. iOS module/export, physical Android/iOS runs, low-end-device performance and store signing/submission remain release work.

Platform icon assignment follows Unity's [SetPlatformIcons API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/playersettings/setplatformicons).

## Tile colour identity
Each value from 2 to 8192 has a distinct curated tone: ivory, sage, sand, gold, peach, terracotta, rose, plum, blue, teal, amber, violet and navy. Higher keep-going values use deterministic unique hues. High-contrast mode preserves these backgrounds and selects black/white labels. Normal labels fall back to black/white if needed to reach 4.5:1 contrast. See `tile-colour-checks.log`.

## Animation regression fix
Pooled moving tiles were below rebuilt board cells, hiding movement after new-game/restart. AcquireGhost now moves each visual to the front. Slides present an unchanged source frame before interpolation, preventing a first-frame jump; merge and spawn effects start only at contact. Home has a staggered fade/lift entrance and a subtle brand float, with reduced-motion and modal/background handling. Layout changes rebase entry positions.

The native `-premiumMotionSmoke` runner exercises the actual view: source position, render order, intermediate frames, merge pulse, spawn growth, scale settlement, all three dimensions and all four directions, home entry and reduced motion. Original runs reproduced both failures; the corrected matrix passes. QA uses isolated storage and is excluded from release builds.

Final restored-animation verification: 623 native gameplay checks and 431 frame-level motion checks passed. All 14 scenes were rebuilt. The Android non-development preview APK was rebuilt with this fix; the older development APK is from the previous revision.

## Gameplay feedback polish
Fast 120ms slides and 100ms settle timing retained. Merge audio now plays at tile contact, accompanied by a short amber outline pulse from a single reusable graphic. Score counts up through the settle phase, then returns to the exact committed value. Highlights stop on navigation, background, reduced motion and board rebuild. No new per-move particle objects or textures are created.

## Level targets and completion
Level 1 (4×4) completes when a 2048 tile forms; Level 2 (5×5) requires 4096; Level 3 (6×6) requires 8192. Total score is not the win condition. A newly reached target takes precedence over a blocked board. The completion dialog shows the actual target and offers Next level for levels 1/2; endless play remains optional when moves remain. Level 3 offers replay and optional endless play.
