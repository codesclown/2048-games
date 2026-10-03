using System;
using System.Collections.Generic;
namespace Premium2048 {
public static class ExpeditionCatalog {
 public static readonly List<MissionDefinition> Missions = new List<MissionDefinition> {
  CreateMission1(),
  CreateMission2(),
  CreateMission3(),
  CreateMission4(),
  CreateMission5(),
  CreateMission6(),
  CreateMission7(),
  CreateMission8(),
  CreateMission9(),
  CreateMission10()
 };

 public static MissionDefinition Get(int index) {
  if (index < 0 || index >= Missions.Count) return null;
  return Missions[index].Copy();
 }

 public static MissionDefinition Get(string id) {
  foreach (var m in Missions) if (m.id == id) return m.Copy();
  return null;
 }

 static MissionDefinition CreateMission1() {
  var m = new MissionDefinition {
   id = "expedition-01",
   title = "First opening",
   description = "Merge tiles orthogonally beside the lock to crack it open and make 16.",
   chapter = 1,
   version = 1,
   size = 4,
   actionLimit = 12,
   toolAllowance = new int[] { 1, 0, 0 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.All,
    children = new MissionGoalDefinition[] {
     new MissionGoalDefinition { kind = MissionGoalKind.UnlockCells, cells = new int[] { 5 } },
     new MissionGoalDefinition { kind = MissionGoalKind.ReachTile, value = 16 }
    }
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 4, priority = new int[] { 12, 13 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 14, 15 } },
    new MissionSpawnStep { value = 2, priority = new int[] { 2, 3 } },
    new MissionSpawnStep { value = 2, priority = new int[] { 6, 7 } }
   }
  };
  m.initialTiles[0] = 8;
  m.initialTiles[1] = 4;
  m.initialTiles[2] = 4;
  m.terrain[5] = 1; // 1-hit lock at (row 1, col 1)
  return m;
 }

 static MissionDefinition CreateMission2() {
  var m = new MissionDefinition {
   id = "expedition-02",
   title = "Make room",
   description = "Navigate around perimeter locks and merge up to a 32 tile.",
   chapter = 1,
   version = 1,
   size = 4,
   actionLimit = 16,
   toolAllowance = new int[] { 1, 1, 0 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.ReachTile,
    value = 32
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 8, priority = new int[] { 4, 5 } },
    new MissionSpawnStep { value = 8, priority = new int[] { 6, 7 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 8, 9 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 10, 11 } }
   }
  };
  m.initialTiles[0] = 8;
  m.initialTiles[1] = 8;
  m.initialTiles[12] = 8;
  m.initialTiles[13] = 8;
  m.terrain[2] = 1;  // Lock at (row 0, col 2)
  m.terrain[14] = 1; // Lock at (row 3, col 2)
  return m;
 }

 static MissionDefinition CreateMission3() {
  var m = new MissionDefinition {
   id = "expedition-03",
   title = "Twin tiles",
   description = "Create and hold two 32 tiles simultaneously on the board.",
   chapter = 1,
   version = 1,
   size = 4,
   actionLimit = 20,
   toolAllowance = new int[] { 1, 1, 1 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.HoldTiles,
    value = 32,
    count = 2
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 8, priority = new int[] { 2, 3 } },
    new MissionSpawnStep { value = 8, priority = new int[] { 4, 5 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 10, 11 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 14, 15 } }
   }
  };
  m.initialTiles[0] = 16;
  m.initialTiles[1] = 16;
  m.initialTiles[12] = 16;
  m.initialTiles[13] = 16;
  m.terrain[6] = 1; // Lock at (row 1, col 2)
  m.terrain[9] = 1; // Lock at (row 2, col 1)
  return m;
 }

 static MissionDefinition CreateMission4() {
  var m = new MissionDefinition {
   id = "expedition-04",
   title = "Corner key",
   description = "Break open the reinforced corner gates and merge your way to 64.",
   chapter = 1,
   version = 1,
   size = 4,
   actionLimit = 24,
   toolAllowance = new int[] { 1, 1, 0 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.All,
    children = new MissionGoalDefinition[] {
     new MissionGoalDefinition { kind = MissionGoalKind.UnlockCells, cells = new int[] { 0, 15 } },
     new MissionGoalDefinition { kind = MissionGoalKind.ReachTile, value = 64 }
    }
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 16, priority = new int[] { 5, 6 } },
    new MissionSpawnStep { value = 16, priority = new int[] { 9, 10 } },
    new MissionSpawnStep { value = 8, priority = new int[] { 7, 11 } },
    new MissionSpawnStep { value = 8, priority = new int[] { 3, 12 } }
   }
  };
  m.initialTiles[1] = 16;
  m.initialTiles[2] = 16;
  m.initialTiles[4] = 16;
  m.initialTiles[8] = 16;
  m.initialTiles[13] = 16;
  m.initialTiles[14] = 16;
  m.terrain[0] = 2;  // 2-hit corner lock at (row 0, col 0)
  m.terrain[15] = 2; // 2-hit corner lock at (row 3, col 3)
  return m;
 }

 static MissionDefinition CreateMission5() {
  var m = new MissionDefinition {
   id = "expedition-05",
   title = "Crossroads",
   description = "Divide and conquer through segmented lanes to forge a 128 tile.",
   chapter = 2,
   version = 1,
   size = 4,
   actionLimit = 28,
   toolAllowance = new int[] { 2, 1, 0 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.ReachTile,
    value = 128
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 32, priority = new int[] { 2, 3 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 12, 13 } },
    new MissionSpawnStep { value = 16, priority = new int[] { 4, 7 } },
    new MissionSpawnStep { value = 16, priority = new int[] { 8, 11 } }
   }
  };
  m.initialTiles[0] = 32;
  m.initialTiles[1] = 32;
  m.initialTiles[12] = 32;
  m.initialTiles[13] = 32;
  m.terrain[5] = 1;
  m.terrain[6] = 1;
  m.terrain[9] = 1;
  return m;
 }

 static MissionDefinition CreateMission6() {
  var m = new MissionDefinition {
   id = "expedition-06",
   title = "Open the path",
   description = "Trigger multi-directional merges to unlock all four center gates.",
   chapter = 2,
   version = 1,
   size = 4,
   actionLimit = 28,
   toolAllowance = new int[] { 2, 2, 1 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.UnlockCells,
    cells = new int[] { 5, 6, 9, 10 }
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 8, priority = new int[] { 4, 7 } },
    new MissionSpawnStep { value = 8, priority = new int[] { 8, 11 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 0, 3 } },
    new MissionSpawnStep { value = 4, priority = new int[] { 12, 15 } }
   }
  };
  m.initialTiles[0] = 4;
  m.initialTiles[1] = 8;
  m.initialTiles[2] = 8;
  m.initialTiles[3] = 4;
  m.initialTiles[12] = 4;
  m.initialTiles[13] = 8;
  m.initialTiles[14] = 8;
  m.initialTiles[15] = 4;
  m.terrain[5] = 1;
  m.terrain[6] = 1;
  m.terrain[9] = 1;
  m.terrain[10] = 1;
  return m;
 }

 static MissionDefinition CreateMission7() {
  var m = new MissionDefinition {
   id = "expedition-07",
   title = "A careful swap",
   description = "Position tiles with surgical swaps across narrow lanes to hold two 64s.",
   chapter = 2,
   version = 1,
   size = 4,
   actionLimit = 30,
   toolAllowance = new int[] { 2, 2, 1 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.HoldTiles,
    value = 64,
    count = 2
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 32, priority = new int[] { 4, 7 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 8, 11 } },
    new MissionSpawnStep { value = 16, priority = new int[] { 5, 6 } },
    new MissionSpawnStep { value = 16, priority = new int[] { 9, 10 } }
   }
  };
  m.initialTiles[0] = 32;
  m.initialTiles[3] = 32;
  m.initialTiles[12] = 32;
  m.initialTiles[15] = 32;
  m.terrain[1] = 1;
  m.terrain[2] = 1;
  m.terrain[13] = 1;
  m.terrain[14] = 1;
  return m;
 }

 static MissionDefinition CreateMission8() {
  var m = new MissionDefinition {
   id = "expedition-08",
   title = "Small spaces",
   description = "Maneuver through tight fortified corners to synthesize a 256 tile.",
   chapter = 3,
   version = 1,
   size = 4,
   actionLimit = 36,
   toolAllowance = new int[] { 2, 2, 1 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.ReachTile,
    value = 256
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 64, priority = new int[] { 3, 4 } },
    new MissionSpawnStep { value = 64, priority = new int[] { 11, 12 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 5, 6 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 9, 10 } }
   }
  };
  m.initialTiles[0] = 64;
  m.initialTiles[1] = 64;
  m.initialTiles[12] = 64;
  m.initialTiles[13] = 64;
  m.terrain[2] = 2;
  m.terrain[7] = 1;
  m.terrain[8] = 1;
  m.terrain[14] = 2;
  return m;
 }

 static MissionDefinition CreateMission9() {
  var m = new MissionDefinition {
   id = "expedition-09",
   title = "Two goals",
   description = "Demolish three reinforced barriers while simultaneously forging a 256 tile.",
   chapter = 3,
   version = 1,
   size = 4,
   actionLimit = 40,
   toolAllowance = new int[] { 3, 2, 1 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.All,
    children = new MissionGoalDefinition[] {
     new MissionGoalDefinition { kind = MissionGoalKind.UnlockCells, cells = new int[] { 5, 6, 10 } },
     new MissionGoalDefinition { kind = MissionGoalKind.ReachTile, value = 256 }
    }
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 64, priority = new int[] { 8, 9 } },
    new MissionSpawnStep { value = 64, priority = new int[] { 11, 12 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 13, 14 } },
    new MissionSpawnStep { value = 32, priority = new int[] { 15, 0 } }
   }
  };
  m.initialTiles[0] = 64;
  m.initialTiles[1] = 64;
  m.initialTiles[2] = 64;
  m.initialTiles[3] = 64;
  m.initialTiles[12] = 64;
  m.initialTiles[13] = 64;
  m.initialTiles[14] = 64;
  m.initialTiles[15] = 64;
  m.terrain[5] = 1;
  m.terrain[6] = 1;
  m.terrain[10] = 1;
  return m;
 }

 static MissionDefinition CreateMission10() {
  var m = new MissionDefinition {
   id = "expedition-10",
   title = "Expedition finale",
   description = "The ultimate challenge: breach the quadri-core citadel and build a 512 tile.",
   chapter = 3,
   version = 1,
   size = 4,
   actionLimit = 50,
   toolAllowance = new int[] { 3, 2, 2 },
   goal = new MissionGoalDefinition {
    kind = MissionGoalKind.All,
    children = new MissionGoalDefinition[] {
     new MissionGoalDefinition { kind = MissionGoalKind.UnlockCells, cells = new int[] { 5, 6, 9, 10 } },
     new MissionGoalDefinition { kind = MissionGoalKind.ReachTile, value = 512 }
    }
   },
   spawnScript = new MissionSpawnStep[] {
    new MissionSpawnStep { value = 128, priority = new int[] { 4, 7 } },
    new MissionSpawnStep { value = 128, priority = new int[] { 8, 11 } },
    new MissionSpawnStep { value = 64, priority = new int[] { 14, 15 } },
    new MissionSpawnStep { value = 64, priority = new int[] { 0, 1 } }
   }
  };
  m.initialTiles[0] = 128;
  m.initialTiles[1] = 128;
  m.initialTiles[2] = 128;
  m.initialTiles[3] = 128;
  m.initialTiles[12] = 128;
  m.initialTiles[13] = 128;
  m.initialTiles[14] = 128;
  m.initialTiles[15] = 128;
  m.terrain[5] = 1;
  m.terrain[6] = 1;
  m.terrain[9] = 1;
  m.terrain[10] = 1;
  return m;
 }
}
}
