# Native UI kit refresh

The shared Unity UI now uses midnight blue surfaces, warm amber primary actions and seafoam daily accents. Home, game, Journey, Daily and dialogs inherit the same theme.

- Raised buttons and cards use cached nine-sliced surfaces with restrained bevels and shadows.
- Rounded selection outlines follow control edges; full-screen overlays fill the viewport.
- Home mode cards have distinct vector emblems and clearer title/subtitle hierarchy.
- The board has an inset frame, bevelled tiles and stronger score typography.
- Every numbered tile retains its distinct colour and contrast-aware text.
- Existing home, button, slide, spawn and merge animations remain; move timings were not changed.
- Spanish localization includes the new Play Classic label.

## Verification — 3 October 2026

- 14 native scene previews regenerated at 1080 × 2340.
- 738 native UI, service and integration checks passed.
- 445 native animation checks passed.
- Rounded selection geometry is checked for bounds and outline area.
- Updated assets synced to `/Users/chandanvishwakarma/2048/PremiumUnity`; the open Unity scene was reloaded and visually checked.
- A temporary macOS player was used for QA. No APK was generated. Physical Android/iOS verification remains pending.

Previews: [Home](Previews/AllScreens/01_HomeScreen_preview.png), [Game](Previews/AllScreens/02_GameScreen_4x4_preview.png), [Settings](Previews/AllScreens/06_Dialog_Settings_preview.png).
