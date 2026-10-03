using System;
using System.Collections.Generic;
using System.Text;

namespace Premium2048 {
[Serializable]
public sealed class DailyRecord {
 public string dateKey; // "YYYY-MM-DD"
 public int version = 1;
 public int stars;
 public int actions;
 public long completedUtcTimestamp;
}

[Serializable]
public sealed class DailyProgress {
 public int currentStreak;
 public int maxStreak;
 public string lastCompletedDate = "";
 public List<DailyRecord> history = new List<DailyRecord>();

 public bool IsCompleted(string dateKey) {
  if (string.IsNullOrEmpty(dateKey) || history == null) return false;
  return history.Exists(r => r != null && r.dateKey == dateKey);
 }

 public DailyRecord GetRecord(string dateKey) {
  if (string.IsNullOrEmpty(dateKey) || history == null) return null;
  return history.Find(r => r != null && r.dateKey == dateKey);
 }

 public bool Record(string dateKey, int version, int stars, int actions, DateTime? utcNow = null) {
  if (string.IsNullOrEmpty(dateKey)) return false;
  if (history == null) history = new List<DailyRecord>();
  var existing = history.Find(r => r != null && r.dateKey == dateKey);
  DateTime now = utcNow ?? DateTime.UtcNow;
  long timestamp = ((DateTimeOffset)now).ToUnixTimeSeconds();

  if (existing != null) {
   // Already completed this date: update personal best without inflating streak
   if (stars > existing.stars || (stars == existing.stars && actions < existing.bestActions(existing.actions))) {
    existing.stars = Math.Max(existing.stars, stars);
    existing.actions = Math.Min(existing.actions, actions);
    existing.version = version;
   }
   return false; // Not a new day completion
  }

  history.Add(new DailyRecord {
   dateKey = dateKey,
   version = version,
   stars = Math.Max(1, Math.Min(3, stars)),
   actions = actions,
   completedUtcTimestamp = timestamp
  });

  // Calculate streak based on calendar continuity
  UpdateStreak(dateKey);
  return true;
 }

 void UpdateStreak(string newDateKey) {
  if (string.IsNullOrEmpty(lastCompletedDate)) {
   currentStreak = 1;
   maxStreak = Math.Max(maxStreak, currentStreak);
   lastCompletedDate = newDateKey;
   return;
  }

  DateTime lastDate, currDate;
  if (DateTime.TryParse(lastCompletedDate, out lastDate) && DateTime.TryParse(newDateKey, out currDate)) {
   double daysDiff = (currDate.Date - lastDate.Date).TotalDays;
   if (daysDiff == 1) {
    currentStreak++;
   } else if (daysDiff > 1) {
    currentStreak = 1; // Skipped at least a day
   } // If daysDiff <= 0 (e.g. earlier date played), do not alter active streak
  } else {
   currentStreak = 1;
  }

  maxStreak = Math.Max(maxStreak, currentStreak);
  lastCompletedDate = newDateKey;
 }
}

public static class DailyRecordExtensions {
 public static int bestActions(this DailyRecord r, int fallback) {
  return r != null && r.actions > 0 ? r.actions : fallback;
 }
}

public static class DailyPuzzleService {
 public const int ContentVersion = 1;

 // Platform-stable 32-bit FNV-1a hash
 public static uint ComputeHash(string input) {
  if (input == null) return 0;
  uint hash = 2166136261u;
  for (int i = 0; i < input.Length; i++) {
   hash ^= (uint)input[i];
   hash *= 16777619u;
  }
  return hash;
 }

 // Deterministic pseudo-random sequence from seed
 sealed class StablePrng {
  uint state;
  public StablePrng(uint seed) { state = seed == 0 ? 0x853c49e6u : seed; }
  public uint Next() {
   state ^= state << 13;
   state ^= state >> 17;
   state ^= state << 5;
   return state;
  }
  public int Range(int min, int max) {
   if (max <= min) return min;
   return min + (int)(Next() % (uint)(max - min));
  }
 }

 public static string GetTodayKey(DateTime? utcNow = null) {
  DateTime date = utcNow ?? DateTime.UtcNow;
  return date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
 }

 public static MissionDefinition Generate(string dateKey, int version = ContentVersion) {
  if (string.IsNullOrEmpty(dateKey)) dateKey = GetTodayKey();
  uint seed = ComputeHash("2048-Expedition-Daily-v" + version + ":" + dateKey);
  var rng = new StablePrng(seed);

  var mission = new MissionDefinition {
   id = "daily-" + dateKey,
   version = version,
   actionLimit = rng.Range(20, 32)
  };

  int puzzleType = rng.Range(0, 4);
  int targetTile = puzzleType == 0 ? 64 : puzzleType == 1 ? 128 : puzzleType == 2 ? 64 : 128;

  // Lock placement archetype
  int lockPattern = rng.Range(0, 5);
  int[] lockCells;
  switch (lockPattern) {
   case 0: lockCells = new[] { 5, 10 }; break; // Center cross
   case 1: lockCells = new[] { 0, 15 }; break; // Opposing corners
   case 2: lockCells = new[] { 6, 9 }; break;  // Center diagonal
   case 3: lockCells = new[] { 3, 12 }; break; // Inverse corners
   default: lockCells = new[] { 4, 7 }; break; // Edge dividers
  }

  foreach (int c in lockCells) {
   mission.terrain[c] = rng.Range(1, 3); // 1 or 2 durability
  }

  mission.title = "Daily " + dateKey;
  mission.description = "Complete today's puzzle within " + mission.actionLimit + " actions.";

  if (puzzleType == 3) {
   // Unlock all locks + reach tile
   mission.goal = new MissionGoalDefinition {
    kind = MissionGoalKind.All,
    children = new[] {
     new MissionGoalDefinition { kind = MissionGoalKind.UnlockCells, cells = lockCells },
     new MissionGoalDefinition { kind = MissionGoalKind.ReachTile, value = 64 }
    }
   };
  } else if (puzzleType == 2) {
   // Hold twin tiles
   mission.goal = new MissionGoalDefinition {
    kind = MissionGoalKind.HoldTiles,
    count = 2,
    value = 32
   };
  } else {
   // Reach tile
   mission.goal = new MissionGoalDefinition {
    kind = MissionGoalKind.ReachTile,
    value = targetTile
   };
  }

  // Initial tiles on open cells
  var openCells = new List<int>();
  for (int i = 0; i < 16; i++) {
   if (mission.terrain[i] == 0) openCells.Add(i);
  }

  int start1 = openCells[rng.Range(0, openCells.Count)];
  openCells.Remove(start1);
  int start2 = openCells[rng.Range(0, openCells.Count)];
  openCells.Remove(start2);

  mission.initialTiles[start1] = 2;
  mission.initialTiles[start2] = 2;

  // Tools: 1 swap, 1 undo
  mission.toolAllowance = new[] { 1, 1, 0 };

  // Generate deterministic spawn script for 40 turns
  var script = new List<MissionSpawnStep>();
  for (int turn = 0; turn < 45; turn++) {
   int val = rng.Range(0, 10) < 8 ? 2 : 4;
   var priorities = new int[4];
   for (int p = 0; p < 4; p++) {
    priorities[p] = rng.Range(0, 16);
   }
   script.Add(new MissionSpawnStep { value = val, priority = priorities });
  }
  mission.spawnScript = script.ToArray();

  return mission;
 }

 public static string FormatShareText(string dateKey, int stars, int actions, bool won, string goalText = "") {
  var sb = new StringBuilder();
  sb.Append("2048 Expedition Daily — ").Append(dateKey).Append("\n");
  if (won) {
   string starIcons = stars == 3 ? "⭐⭐⭐" : stars == 2 ? "⭐⭐" : "⭐";
   sb.Append(starIcons).Append(" Solved in ").Append(actions).Append(" actions!\n");
  } else {
   sb.Append("🧩 Attempted (").Append(actions).Append(" actions)\n");
  }
  if (!string.IsNullOrEmpty(goalText)) {
   sb.Append("🎯 ").Append(goalText).Append("\n");
  }
  sb.Append("#2048Expedition");
  return sb.ToString();
 }
}
}
