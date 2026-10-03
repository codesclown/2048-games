# 2048 Expedition — QA & Release Candidate Guide

This document defines the physical QA checklist, performance guidelines, device matrix, and release verification for **2048 Expedition Mode**.

---

## 1. System Architecture & Features Summary

| Feature | Details |
|---|---|
| **Modes** | Classic (4×4 Level 1 to 2048, 5×5 Level 2 to 4096, 6×6 Level 3 to 8192) & Expedition (10-Mission Handcrafted Campaign + Offline Daily Puzzle) |
| **Mechanic** | Orthogonal adjacent merge lock cracking (1-hit & 2-hit locks), action budgets, 3-star rating, deterministic spawn priority scripts |
| **Daily Puzzle** | Offline UTC-date deterministic generation (FNV-1a PRNG), consecutive day streaks, share card generator |
| **Save Isolation** | Dual-slot checksummed JSON persistence (`ExpeditionPersistence.cs` & `GamePersistence.cs`), immune to cross-mode corruption |
| **Accessibility** | English/Spanish localization (`strings.json`), Large Text dynamic scaling, High Contrast durability labels (`🔒 1`, `🔒 2`), Reduced Motion instant animations |
| **Animation Timings** | Snappy 80ms slide ease-out, 70ms merge settle |

---

## 2. Automated Test Matrix

Run all test suites locally:

```sh
# 1. Daily Puzzle & Streak Suite (2,284 assertions)
mcs -out:./daily-tests.exe Assets/Scripts/Board.cs Assets/Scripts/BoardMovement.cs Assets/Scripts/IGameSession.cs Assets/Scripts/Session.cs Assets/Scripts/ExpeditionSession.cs Assets/Scripts/ExpeditionCatalog.cs Assets/Scripts/DailyPuzzle.cs Tests/ExpeditionDailyTests.cs && mono ./daily-tests.exe && rm ./daily-tests.exe

# 2. Expedition 10-Mission Rules Suite (20,148 assertions)
mcs -out:./expedition-tests.exe Assets/Scripts/Board.cs Assets/Scripts/BoardMovement.cs Assets/Scripts/IGameSession.cs Assets/Scripts/Session.cs Assets/Scripts/ExpeditionSession.cs Assets/Scripts/ExpeditionCatalog.cs Tests/ExpeditionRulesTests.cs && mono ./expedition-tests.exe && rm ./expedition-tests.exe

# 3. Classic 4x4, 5x5, 6x6 Rules Suite (8,772,314 assertions)
mcs -out:./classic-tests.exe Assets/Scripts/Board.cs Assets/Scripts/BoardMovement.cs Assets/Scripts/IGameSession.cs Assets/Scripts/Session.cs Assets/Scripts/ExpeditionSession.cs Tests/RulesTests.cs && mono ./classic-tests.exe && rm ./classic-tests.exe
```

---

## 3. Physical Device Testing Checklist

When testing builds on physical Android and iOS devices:

### A. Navigation & Display
- [ ] **Notched & Tall Displays:** Verify top and bottom safe-area insets do not clip header, score boxes, or bottom tool trays.
- [ ] **Tablets / Split Screen:** Ensure board scales proportionally without horizontal overflow.
- [ ] **Home Mode Selector:** Verify Classic resume, Expedition resume, and Daily Puzzle button states.
- [ ] **Expedition Map:** Scroll through Chapters 1, 2, and 3. Ensure unlocked missions show play buttons and locked missions display prerequisites.

### B. Gameplay & Rules
- [ ] **Mission 01–10:** Complete missions and verify star rewards (1, 2, or 3 stars) match action budgets.
- [ ] **Lock Damage:** Confirm only orthogonal adjacent merges crack locks; diagonal merges do not.
- [ ] **Tools:** Test Undo, Swap, and Delete tool counts match mission allowances and do not overflow.
- [ ] **Daily Puzzle:** Start today's puzzle, complete it, and verify streak increments on consecutive days.
- [ ] **Share Result:** Tap "Share Result" on Win dialog; verify clipboard contains formatted text with stars and `#2048Expedition`.

### C. Lifecycle & Stability
- [ ] **App Suspend / Backgrounding:** Background app mid-mission, relaunch, and verify game state is preserved exactly.
- [ ] **Hardware Back Button (Android):** Opens Pause dialog or returns to previous screen gracefully without quitting unexpectedly.
- [ ] **Audio & Haptics:** Verify sounds and vibrations respect Settings toggles.

---

## 4. Release Build Instructions (User Executed)

As per project guidelines, build packages using standard Unity build workflows:
- **Android:** File → Build Settings → Platform: Android → Build App Bundle (AAB) or APK.
- **iOS:** File → Build Settings → Platform: iOS → Build Xcode Project → Archive & Sign via Xcode.
