# 2048 Premium — Unity

Native Android/iOS Unity 2048 game. The parent web prototype is reference material only. Open **this `PremiumUnity` folder** in Unity Hub using **Unity 6000.3.25f1**, open `Assets/Scenes/Premium.unity`, and press **Play**. The project configures its build scene, legacy input, display size, portrait orientation and provisional Android/iOS identifiers on first editor load. You can rerun `2048 Premium → Configure project`.

## Included

- Ink, parchment and muted amber UI kit, coherent tile progression, native procedural toolbar icons, rounded touch controls and a live home preview for all board sizes.
- Level selector: 4×4 / 2048, 5×5 / 4096, 6×6 / 8192; new-board replacement confirmation, restart confirmation, pause/menu, home navigation and saved-game resume.
- Separate score/best cards, floating score gains, short slide/spawn/merge, Swap/Delete and screen animations with one queued swipe, win and game-over dialogs, keep going, play again and home actions.
- Arrows, WASD and HJKL; single-finger swipe or mouse drag on the board. Modifier shortcuts are ignored; R opens restart confirmation; Escape opens menu or cancels selection/dialogs. Multitouch cancels the swipe.
- Sound toggle with native generated slide, merge, click and win tones; mute preference persists.
- Power-ups unlock after **2 valid moves**, matching the active unified configuration in the web source. Starting Undo/Swap/Delete: **2/1/1**, **2/2/2**, **3/3/3** by level. Each type has a lifetime cap of **4/5/6**.
- Undo restores a board and score snapshot. Swap requires two different, occupied, orthogonally adjacent tiles. Delete targets an exact cell and cannot remove the last remaining tile. Cancel selection spends nothing. Power-ups never spawn extra tiles or refund consumed stock through Undo.
- After using a tool, a held 128 / 256 / 512 tile can replenish Undo / Swap / Delete on successful moves, with a 1-second cooldown and the lifetime cap. No earnings from repeated rendering or invalid input.
- Up to 15 action snapshots; one loss continuation rewinds up to five actions, retains the current score, and keeps earned/used power-ups, matching the web continuation policy.
- High scores per level, games played/won and win rate; confirmed reset of saved scores/game. Winning is recorded once per session, including after undo/replay.
- Persistent session, undo history, power-up stock, reward usage and statistics. Invalid saves are rejected. UI fits the current safe area and updates after resize/rotation.

## Rewarded video / platform integration

The web game's `window.DroidHandler` / JioGames wrapper is a browser-host interface, not a native Unity SDK. This Unity project does **not** claim to ship live ads or JioGames leaderboard submission.

Without a configured SDK, the game honestly offers **Continue: rewind 5 moves**, with a confirmation and one use per game. For native rewarded video, implement `RewardProvider` with your advertising SDK, attach the component to the scene, and assign it to `PremiumGame.rewardProvider`. `IsReady` controls availability and `Show(Action<bool>)` must call back on Unity's main thread, reporting true only after the SDK confirms a completed reward. Unavailable, canceled, failed or timed-out ads grant no reward; duplicate callbacks are ignored. Native JioGames publishing, interstitial/banner ads, account/profile and leaderboard require the native SDK and credentials for your target platform.

## Rules verification

From this folder:

```sh
mcs -out:/tmp/premium-rules.exe Assets/Scripts/Board.cs Assets/Scripts/Session.cs Assets/Scripts/BoardMovement.cs Assets/Scripts/IGameSession.cs Assets/Scripts/ExpeditionSession.cs Tests/RulesTests.cs
mono /tmp/premium-rules.exe
```

Tests cover all grid sizes and movement directions, merge precedence, score/targets, valid-move counting, unlock configuration, stock use, invalid selection, exact-cell delete, undo, replenishment/cooldown/caps, continuation and seeded simulations with animation path conservation. C# runtime/editor scripts also compile against the installed Unity 6.3 engine/uGUI assemblies. Unity editor import/compilation and a macOS native app build passed. The native app integration check passed Home/Game navigation, animated moves and scoring, power-up unlock, Undo, Swap/Delete selection, all three board sizes, 8192 win/keep-going, loss/continuation, instructions fit, High Scores and saved-game Resume. Home, level selector, gameplay and loss-dialog layouts were also inspected in the running app.

## Play the included macOS app

Use `BuildVerifier.BuildMac` to generate `/tmp/2048Premium.app` on this Mac. This is an unsigned local macOS build. The full Unity source is included for editing and building for other platforms.

## Build

File → Build Profiles → choose a platform → Build. `Assets/Scenes/Premium.unity` is already registered. Install the corresponding Unity Hub platform module and SDK for Android/iOS. Store signing and publishing are not configured.

## Source structure

- `Board.cs`: dimension-independent movement rules and animation paths.
- `Session.cs`: history, powers, limited continuation and stats data.
- `GameManager.cs`: authoritative gameplay commands, settings, score/win accounting and committed-action events.
- `GameNavigation.cs`: screen/dialog routing, explicit game states and input gates.
- `GamePersistence.cs`: versioned native save transactions, atomic file replacement, corruption recovery and legacy migration.
- `GameController.cs`: native board swipe/tap input and lifecycle cancellation.
- `PremiumGame.cs`: event-driven native UI rendering, animations and audio.
- `PremiumIcon.cs` / `PremiumMotion.cs`: vector controls and touch/dialog feedback.
- `RewardProvider.cs`: optional real SDK adapter contract.
- `Assets/Editor/ProjectSetup.cs`: project configuration and scene creation.

The original HTML/CSS/JS files stay in the parent folder for reference. The native UI is built at runtime by `PremiumGame`.

## Native integration verification

`Assets/Editor/BuildVerifier.cs` builds a development macOS player into `/tmp/2048Premium.app`. Run it with `-batchmode -premiumSmoke` for the development-only integration runner. The runner uses the separate `Premium2048.QA` preference namespace and is excluded from release builds. `BuildVerifier.BuildReleaseMac` creates the release player.

## Mobile premium delivery plan

The offline gameplay loop, fourteen screens, UI kit and phase 1–4 foundations are implemented and desktop integration verified. See [PHASES.md](PHASES.md) for completed changes, pending work and phase exit checks. See `Docs/GAME_DESIGN.md` for this design revision. Physical device responsiveness, iOS export/signing and store release remain pending. Matching launcher assets and the Android ARM64 development build command are included.

## Save format and Phase 1 checks

Schema-v2 saves alternate two checksum-protected files in `Application.persistentDataPath/Saves`. Each commit contains session, undo history, inventory, statistics and sound preference; temporary files are flushed and atomically replaced. Legacy `.session.v1`, `.stats.v1` and integer `.sound` PlayerPrefs migrate automatically. A corrupt newest slot falls back to the prior valid slot. Reset removes backup and legacy progress. Rendering never commits a save; unchanged lifecycle saves are deduplicated.

The development-only `PhaseOneVerification` checks native file recovery, v1 migration, invalid/interrupted saves, future-schema protection, state gates, all board dimensions, tools, reward idempotency and persistence. `PremiumSmokeRunner` also checks actual controller gestures and overlapping focus/pause notifications. These run with `-premiumSmoke`, use separate QA storage, and are excluded from the release player.

## Rebuild native screen previews

`2048 Premium → Bake All Screens & Prefabs` generates all fourteen scenes and their prefabs from `PremiumGame`. `PremiumPreviewLayout` uses the runtime positioning function, so simulator previews adapt to compact/tall phones and tablets. `SceneChecker.RebuildAndVerify` rebuilds and captures the complete screen set in `Docs/Previews/AllScreens`. Editor previews use isolated in-memory demo data and hide during Play.

`BuildVerifier.BuildAndroidDevelopment` builds an IL2CPP ARM64 APK into `Builds/Android/2048-Premium-dev.apk` with a development signing key. It preserves custom release-signing preferences and SDK/backend settings. It does not publish to a store.

`BuildVerifier.BuildAndroidPreview` builds the non-development, debug-signed phone-testing APK at `Builds/Android/2048-Premium-preview.apk`. Android ARM64 builds and 623 native smoke checks passed for this revision. Store signing and physical-device performance validation remain pending.

`-premiumMotionSmoke` on a development player verifies real slide/merge frames across all board sizes/directions, pooled tile draw order, home entrance and reduced motion. See `Docs/motion-checks.log` for the restored-animation regression evidence.

## Expedition roadmap
See [Docs/EXPEDITION_PHASES.md](Docs/EXPEDITION_PHASES.md) for the planned E1–E10 mission/locked-cell mode. This is a design and implementation plan; Expedition features are not implemented yet.
