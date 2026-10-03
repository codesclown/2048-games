# Native game UI kit

All runtime screens use `PremiumTheme`, `PremiumControl`, `PremiumText` and `PremiumLayout`. Baked scenes and prefabs are generated from the same `PremiumGame` view.

- Midnight background, slate surfaces, amber primary actions; parchment tile values with contrast-aware text.
- 540 × 960 logical minimum, a home composition fitted between the top and bottom safe-area edges, and native safe-area anchors. Taller displays proportionally expand card heights and inter-section spacing.
- 72-point minimum navigation targets; 76-point home action cards with 12-point gaps. Tool controls use 14-point names, explicit availability, and stock badges.
- Board fits between the level caption and tool dock, including selection/cancel mode. Expedition's scroll viewport fills the available safe content region.
- Home has a staggered entrance; Classic/Daily/Expedition gameplay and Journey map have page entrances. Every dialog fades and settles; all buttons have press feedback. Slide/settle durations are 110/100 ms with one queued input. Reduced motion renders the final state immediately.
- Board cells, terrain icons and moving tile ghosts are reused when the board mode and dimensions match. Labels skip unchanged formatting. Navigation does not reconstruct pages for unrelated state events.

Open this exact checkout with `OpenUnity.command`. Code edits affect Play mode after compilation. The active generated preview refreshes after source compilation, scene opening and Play stop. `2048 Premium → Refresh Current Preview` forces a refresh; Bake All Screens & Prefabs updates the full artifact set.

Verification: `2048 Premium → Verify Responsive UI` checks four content heights, resume variants, touch sizes, board/tool clearance and cell reuse. `SceneChecker.RebuildAndVerify` renders the fourteen baked screens. Development players accept `-premiumSmoke` and `-premiumMotionSmoke` for interaction and animation verification.

Desktop checks do not establish physical Android/iOS frame rates.

Verified on 3 October 2026: Unity compilation/macOS build, 14 rendered screens, responsive layout checks, 8,772,314 rules assertions, native integration and final motion suites all passed. Logs: `ui-layout-checks.log`, `ui-integration-checks.log`, `ui-motion-checks.log`. The active editor uses `/Users/chandanvishwakarma/2048-games/PremiumUnity`.

Home design: enlarged 156-point live board preview, prominent Classic action, paired Expedition/Daily cards with progress and supporting copy, and a three-column statistics row. Responsive verification checks paired cards horizontally and the full page vertically with and without saved-game Resume.

Editor parity: both views use the same source hierarchy and safe-area padding. The Premium scene reads saved progress through a read-only preview store; preview commands never write player saves. Generated scene canvases refresh after compilation, scene opening and returning to Edit mode. Stale hierarchy/scale replacement and a live Play → Stop cycle were verified.

Gameplay header/tool refresh: 100-point score cards with 44-point numbers and score/record icons; 104-point tool controls with 16-point names and 28-point inventory badges. Responsive board fit reserves clearance for the enlarged controls and selection Cancel. Selected inventory numbers use amber on the dark badge.

Dialogs use content-based heights, 28-point outer padding, 12-point row gaps, an 8-point heading gap and 24 points after supporting copy. Wrapped descriptions expand their text box. The full-screen backdrop covers safe-area insets while controls remain inside the safe region. Dialog verification checks 13 dialog types, stable repeated layout, large text and Spanish, and fit at 320×568, 390×844, 430×932, 768×1024, 1024×768 and 1920×1080. These are editor checks; physical-device validation remains separate.
