#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Premium2048.Editor {
public static class ExpeditionToolsEditor {
 [MenuItem("Tools/2048/Validate Expedition Campaign & Daily")]
 public static void ValidateAll() {
  Debug.Log("--- Starting 2048 Expedition Batch Validation ---");
  int campaignCount = ExpeditionCatalog.Missions.Count;
  int passed = 0;
  for (int i = 0; i < campaignCount; i++) {
   var m = ExpeditionCatalog.Get(i);
   if (m != null && m.Valid()) {
    passed++;
   } else {
    Debug.LogError("Mission " + (i + 1) + " validation failed!");
   }
  }
  Debug.Log("Campaign Missions: " + passed + "/" + campaignCount + " Valid");

  // Validate 30 upcoming daily puzzles
  int dailyPassed = 0;
  DateTime start = DateTime.UtcNow;
  for (int d = 0; d < 30; d++) {
   string key = DailyPuzzleService.GetTodayKey(start.AddDays(d));
   var puzzle = DailyPuzzleService.Generate(key);
   if (puzzle != null && puzzle.Valid()) {
    dailyPassed++;
   } else {
    Debug.LogError("Daily puzzle failed for date: " + key);
   }
  }
  Debug.Log("Daily Puzzles (30 days): " + dailyPassed + "/30 Valid");

  // Run native smoke integration suite
  ExpeditionVerification.Run();
  Debug.Log("--- 2048 Expedition Validation Completed Successfully ---");
 }
}
}
#endif
