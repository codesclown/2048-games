# Screen fixes — 2026-10-03

All ten baked scenes and screen/dialog prefabs rebuilt. Fonts enlarged for phone readability; fixed anchors keep preview tiles square. Rounded sprites persist as imported assets and caches respect Unity destroyed-object references. Safe-area padding adapts to the simulator/device. Baked preview canvas hides during Play, leaving the runtime UI. Bold labels stay bold across refresh/localization. Tile numbers select the stronger contrast colour; gradient endpoints adjusted where required. Platform icon arrays use the required slot count.

Verification: ten scenes rendered at 1080 × 2340; home and runtime UI checked in Unity Device Simulator. Native macOS development build and the complete phase 1–4/gameplay smoke suite passed. This verifies native compilation and runtime behaviour; Android/iOS physical-device builds were not run in this check.

Evidence: `Previews/AllScreens/`, `screen-fix-checks.log`. Rebuild and verify with `Premium2048.EditorTools.SceneChecker.RebuildAndVerify`, or use the Unity `2048 Premium` bake and verify menu commands.
