# Expedition activation verification — 2026-10-03

E1 rules/service foundation is complete. The latest campaign and Daily UI already present in this workspace was also checked and corrected. The verified source was synced to the open project at `/Users/chandanvishwakarma/2048/PremiumUnity`.

## Verified

- Native Home → Expedition Journey → visible mission Play button → mission gameplay → lock opening → target 16 → mission completion. First mission completes after two authored Left slides, with 3 stars and its own actions/tiles in results.
- Home Daily button starts a Daily mission.
- Separate Classic and Expedition resume routes, native save reload, corruption fallback, Classic session/inventory isolation and lifecycle gates.
- Shared pooled slide/merge/spawn animation; source positions survive the first frame. Mission HUD uses Actions/Stars and a vector lock icon.
- Home button sizes meet the existing 72-unit minimum. Daily positioning and journey top anchors were corrected; the first Play button is asserted inside its clipped viewport.
- Editor-only baked preview canvas is excluded from players. This resolves the reproduced native scene-loading crash while keeping editable previews. Icon sprite initialization happens before graphic rebuild, removing recursive uGUI rebuild errors.

## Evidence

| Check | Result |
|---|---|
| Classic plain C# rules | 8,772,314 assertions passed |
| Expedition plain C# rules/campaign | 20,148 checks passed |
| Daily deterministic rules | 2,284 checks passed |
| Final native Unity smoke | 737 checks passed; `PREMIUM_SMOKE_SUCCESS` |
| Native motion regression | 445 checks passed; `PREMIUM_MOTION_SUCCESS` |
| Open Unity editor | Updated project imported; Expedition/Daily buttons visibly present in Android Simulator Play Mode |

Evidence: [checks](expedition-checks.log), [Home](Previews/Expedition/home.png), [Journey](Previews/Expedition/journey.png), [Mission](Previews/Expedition/mission.png), [Completion](Previews/Expedition/complete.png), [Daily](Previews/Expedition/daily.png).

These are desktop native and editor checks. Physical Android/iOS touch, frame-time, battery, signing and store acceptance remain pending in E9/E10. No APK was built. No commit was created.
