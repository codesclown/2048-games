using System;
using System.Collections.Generic;
using Premium2048;

public static class ExpeditionDailyTests {
 static int checks = 0;
 static void Check(bool valid, string label) {
  if (!valid) throw new Exception("DAILY_FAIL: " + label);
  checks++;
 }

 public static void Main() {
  checks = 0;
  Console.WriteLine("Running Phase E7 Daily Puzzle & Result Sharing Tests...");

  // 1. Stable Hash verification
  uint h1 = DailyPuzzleService.ComputeHash("2048-Expedition-Daily-v1:2026-10-03");
  uint h2 = DailyPuzzleService.ComputeHash("2048-Expedition-Daily-v1:2026-10-03");
  Check(h1 == h2 && h1 != 0, "FNV-1a hash is deterministic");

  // 2. Deterministic Puzzle Generation
  var d1 = DailyPuzzleService.Generate("2026-10-03");
  var d2 = DailyPuzzleService.Generate("2026-10-03");
  Check(d1 != null && d1.Valid(), "generated daily puzzle is valid");
  Check(d1.id == "daily-2026-10-03", "daily puzzle id matches dateKey");
  Check(d1.actionLimit == d2.actionLimit, "action limits match");
  for (int i = 0; i < 16; i++) {
   Check(d1.terrain[i] == d2.terrain[i], "terrain matches at " + i);
   Check(d1.initialTiles[i] == d2.initialTiles[i], "initial tiles match at " + i);
  }
  Check(d1.spawnScript.Length == d2.spawnScript.Length, "spawn scripts match length");
  for (int i = 0; i < d1.spawnScript.Length; i++) {
   Check(d1.spawnScript[i].value == d2.spawnScript[i].value, "spawn script value matches at " + i);
  }

  // 3. 365 Days Validity Test
  DateTime start = new DateTime(2026, 1, 1);
  for (int day = 0; day < 365; day++) {
   string key = start.AddDays(day).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
   var puzzle = DailyPuzzleService.Generate(key);
   Check(puzzle.Valid(), "365-day check: valid puzzle on " + key);
   Check(puzzle.actionLimit >= 20 && puzzle.actionLimit <= 32, "action limit bounded on " + key);
   Check(puzzle.goal != null, "goal defined on " + key);
   // Initial open tile count
   int occupied = 0;
   for (int i = 0; i < 16; i++) {
    if (puzzle.initialTiles[i] > 0) {
     occupied++;
     Check(puzzle.terrain[i] == 0, "initial tile not on lock at " + i + " on " + key);
    }
   }
   Check(occupied == 2, "initial tiles count is 2 on " + key);
  }

  // 4. DailyProgress & Streaks
  var progress = new DailyProgress();
  Check(!progress.IsCompleted("2026-10-01"), "initial progress has no completion");

  // Day 1
  bool r1 = progress.Record("2026-10-01", 1, 3, 14, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
  Check(r1, "recorded day 1");
  Check(progress.IsCompleted("2026-10-01"), "day 1 marked completed");
  Check(progress.currentStreak == 1 && progress.maxStreak == 1, "streak is 1 after day 1");

  // Replay of Day 1 with better actions
  bool r1Replay = progress.Record("2026-10-01", 1, 3, 12, new DateTime(2026, 10, 1, 13, 0, 0, DateTimeKind.Utc));
  Check(!r1Replay, "replay does not report as new day completion");
  Check(progress.currentStreak == 1, "streak unchanged on replay");
  Check(progress.GetRecord("2026-10-01").actions == 12, "best actions updated on replay");

  // Day 2 (Consecutive)
  bool r2 = progress.Record("2026-10-02", 1, 3, 16, new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc));
  Check(r2 && progress.currentStreak == 2 && progress.maxStreak == 2, "consecutive day increments streak to 2");

  // Day 3 (Consecutive)
  bool r3 = progress.Record("2026-10-03", 1, 2, 18, new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc));
  Check(r3 && progress.currentStreak == 3 && progress.maxStreak == 3, "consecutive day increments streak to 3");

  // Day 5 (Skipped day 4!)
  bool r5 = progress.Record("2026-10-05", 1, 3, 15, new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc));
  Check(r5 && progress.currentStreak == 1 && progress.maxStreak == 3, "skipped day resets streak to 1 while preserving maxStreak 3");

  // 5. Share Card Text Generation
  string shareWon = DailyPuzzleService.FormatShareText("2026-10-03", 3, 15, true, "Reach 128");
  Check(shareWon.Contains("2026-10-03") && shareWon.Contains("⭐⭐⭐") && shareWon.Contains("15 actions") && shareWon.Contains("Reach 128"), "share text includes date, stars, actions, and goal");

  string shareLost = DailyPuzzleService.FormatShareText("2026-10-03", 0, 24, false, "Reach 128");
  Check(shareLost.Contains("Attempted") && shareLost.Contains("24 actions"), "loss share text correctly formatted");

  Console.WriteLine("EXPEDITION_DAILY_SUCCESS: " + checks + " checks");
 }
}
