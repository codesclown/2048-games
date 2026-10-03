using System;
using Premium2048;

class ExpeditionRulesTests {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static MissionDefinition Mission(){
  var m=new MissionDefinition{
   id="test-foundation",
   actionLimit=30,
   toolAllowance=new[]{2,1,1},
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=128},
   spawnScript=new[]{
    new MissionSpawnStep{value=4,priority=new[]{0,15}},
    new MissionSpawnStep{value=2,priority=new[]{0,15}},
    new MissionSpawnStep{value=2,priority=new[]{1,2,3}}
   }
  };
  m.initialTiles[0]=2;m.initialTiles[1]=2;
  return m;
 }
 static bool Equal(int[] a,int[] b){for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}

 static void Main(){
  var definition=Mission();var first=new ExpeditionSession(definition);
  definition.initialTiles[0]=512;definition.spawnScript[0].priority[0]=9;
  Check(first.tiles[0]==2&&first.mission.spawnScript[0].priority[0]==0,"mission is privately copied");
  Check(first.Move(Direction.Left)&&first.tiles[0]==4&&first.tiles[15]==4&&first.spawnCursor==1,"authored value and priority spawning");
  Check(first.history[0].spawnCursor==0&&first.history[0].tiles[1]==2,"snapshot captures pre-action state");
  Check(first.Valid(),"mission valid without pretending Classic won");

  var noOp=new ExpeditionSession(Mission());int cursor=noOp.spawnCursor;
  Check(!noOp.Move(Direction.Up)&&noOp.spawnCursor==cursor&&noOp.actions==0&&noOp.history.Count==0,"invalid slide spends nothing");

  // E2: Lock damage from adjacent merge
  var lockMission=new MissionDefinition{id="e2-lock-test",spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}};
  lockMission.initialTiles[0]=2;lockMission.initialTiles[1]=2;
  lockMission.terrain[4]=2; // Lock at cell 4 directly below merge cell 0
  var lockSession=new ExpeditionSession(lockMission);
  Check(lockSession.Move(Direction.Left),"move creates merge at cell 0");
  Check(lockSession.terrain[4]==1,"adjacent merge deals 1 damage to lock (2 -> 1)");
  Check(lockSession.events.Exists(e=>e.kind==MissionTurnEventKind.LockDamaged&&e.cell==4&&e.value==1),"LockDamaged event emitted with remaining durability 1");

  // E2: Lock opening on second damage
  lockSession.tiles[0]=4;lockSession.tiles[1]=4;
  Check(lockSession.Move(Direction.Left),"second merge at cell 0");
  Check(lockSession.terrain[4]==0,"second merge reduces durability to 0 (lock opens)");
  Check(lockSession.events.Exists(e=>e.kind==MissionTurnEventKind.LockOpened&&e.cell==4&&e.value==0),"LockOpened event emitted");

  // E2: Newly opened cell excluded from spawning in the same turn
  var openSpawnMission=new MissionDefinition{id="open-spawn",spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{4,15}}}};
  openSpawnMission.initialTiles[0]=2;openSpawnMission.initialTiles[1]=2;
  openSpawnMission.terrain[4]=1;
  var openSpawnSession=new ExpeditionSession(openSpawnMission);
  Check(openSpawnSession.Move(Direction.Left),"move merges at cell 0 and opens cell 4");
  Check(openSpawnSession.terrain[4]==0,"cell 4 opened");
  Check(openSpawnSession.tiles[4]==0&&openSpawnSession.tiles[15]==2,"newly opened cell 4 was NOT spawned into during same turn (fallback to 15)");

  // E2: Multiple merges touching the same lock in one turn deal at most 1 damage
  var multiMerge=new MissionDefinition{id="multi-merge",spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}};
  multiMerge.terrain[5]=2;
  multiMerge.initialTiles[0]=4;multiMerge.initialTiles[1]=2;multiMerge.initialTiles[2]=2;
  multiMerge.initialTiles[8]=4;multiMerge.initialTiles[9]=2;multiMerge.initialTiles[10]=2;
  var multiSession=new ExpeditionSession(multiMerge);
  Check(multiSession.Move(Direction.Left),"simultaneous merges at cell 1 and cell 9");
  Check(multiSession.terrain[5]==1,"lock 5 received exactly 1 damage despite two simultaneous adjacent merges");

  // E3: Goal Evaluation - ReachTile
  var reachMission=new MissionDefinition{
   id="reach-64",actionLimit=10,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=64},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  reachMission.initialTiles[0]=32;reachMission.initialTiles[1]=32;
  var reachSession=new ExpeditionSession(reachMission);
  Check(!reachSession.completed,"not completed initially");
  Check(reachSession.Move(Direction.Left),"merge 32+32 -> 64");
  Check(reachSession.completed&&!reachSession.failed,"ReachTile goal evaluated and completed");
  Check(reachSession.events.Exists(e=>e.kind==MissionTurnEventKind.Completed),"Completed event emitted");
  Check(reachSession.CalculateStars()==3,"completed in 1 action awards 3 stars");

  // E3: High score without target tile cannot satisfy ReachTile
  var subTargetMission=new MissionDefinition{
   id="reach-128",actionLimit=10,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=128},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  subTargetMission.initialTiles[0]=16;subTargetMission.initialTiles[1]=16;
  var subSession=new ExpeditionSession(subTargetMission);
  subSession.score=50000; // Artificially high score
  subSession.Move(Direction.Left); // Merges 16+16 -> 32 (score increases, but target tile 128 not reached)
  Check(!subSession.completed,"high score without 128 tile does not complete ReachTile");

  // E3: Goal Evaluation - HoldTiles (e.g. Hold 2x 32 tiles)
  var holdMission=new MissionDefinition{
   id="hold-test",actionLimit=10,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.HoldTiles,value=32,count=2},
   spawnScript=new[]{new MissionSpawnStep{value=32,priority=new[]{15}}}
  };
  holdMission.initialTiles[0]=16;holdMission.initialTiles[1]=16;holdMission.initialTiles[4]=32;
  var holdSession=new ExpeditionSession(holdMission);
  Check(!holdSession.completed,"HoldTiles not completed initially");
  Check(holdSession.Move(Direction.Left),"swipe merges 16+16->32, so now two 32s exist");
  Check(holdSession.completed,"HoldTiles completed when 2x 32 tiles are on the board");

  // E3: Goal Evaluation - UnlockCells
  var unlockGoalMission=new MissionDefinition{
   id="unlock-goal",actionLimit=10,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.UnlockCells,cells=new[]{4}},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  unlockGoalMission.initialTiles[0]=2;unlockGoalMission.initialTiles[1]=2;unlockGoalMission.terrain[4]=1;
  var unlockSession=new ExpeditionSession(unlockGoalMission);
  Check(!unlockSession.completed,"UnlockCells not completed initially");
  Check(unlockSession.Move(Direction.Left),"merge damages and unlocks cell 4");
  Check(unlockSession.completed,"UnlockCells goal completed when cell 4 is opened");

  // E3: Last-action win vs action budget exhaustion
  var lastActionMission=new MissionDefinition{
   id="last-act",actionLimit=1,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=8},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  lastActionMission.initialTiles[0]=4;lastActionMission.initialTiles[1]=4;
  var lastActSession=new ExpeditionSession(lastActionMission);
  Check(lastActSession.Move(Direction.Left),"merges on final action (action 1 of 1)");
  Check(lastActSession.completed&&!lastActSession.failed,"last-action win takes precedence over action limit");

  // E3: Action limit failure
  var failMission=new MissionDefinition{
   id="fail-act",actionLimit=1,
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=128},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  failMission.initialTiles[0]=2;failMission.initialTiles[1]=2;
  var failSession=new ExpeditionSession(failMission);
  Check(failSession.Move(Direction.Left),"merges 2+2 -> 4 on action 1 of 1");
  Check(!failSession.completed&&failSession.failed,"action budget exhausted without reaching goal triggers Failed");

  // E3: Tool Actions - Undo, Swap, Delete
  var toolMission=new MissionDefinition{
   id="tool-mission",actionLimit=10,
   toolAllowance=new[]{1,1,1},
   goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=128},
   spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
  };
  toolMission.initialTiles[0]=4;toolMission.initialTiles[1]=8;
  var toolSession=new ExpeditionSession(toolMission);
  // Test Swap: swap cell 0 and cell 1
  Check(toolSession.Swap(0,1),"swap cell 0 and cell 1");
  Check(toolSession.tiles[0]==8&&toolSession.tiles[1]==4&&toolSession.tools[1]==0,"Swap successful and consumes 1 tool");
  Check(!toolSession.Swap(0,1),"cannot swap when tool count is 0");

  // Test Delete: delete tile at cell 1
  Check(toolSession.Delete(1),"delete tile at cell 1");
  Check(toolSession.tiles[1]==0&&toolSession.tools[2]==0,"Delete successful and consumes 1 tool");

  // Test Undo: revert previous action
  Check(toolSession.Undo(),"undo reverts to pre-delete snapshot");
  Check(toolSession.tiles[1]==4&&toolSession.tools[0]==0,"Undo restored tile 4 at cell 1 and consumed 1 Undo tool");

  // E3: Campaign Progress
  var campaign=new CampaignProgress();
  campaign.Record("m1",1,3,5);
  Check(campaign.Get("m1").stars==3&&campaign.Get("m1").bestActions==5,"campaign recorded m1 progress");
  campaign.Record("m1",1,2,4); // Better actions (4), lower stars (2) -> stars should stay 3, bestActions should become 4
  Check(campaign.Get("m1").stars==3&&campaign.Get("m1").bestActions==4,"campaign retains highest stars and best action count");

  // Deterministic rerun across 100 seeds
  var random=new Random(17);
  for(int run=0;run<100;run++){
   var a=new ExpeditionSession(Mission());var b=new ExpeditionSession(Mission());
   for(int move=0;move<100;move++){
    var d=(Direction)random.Next(4);
    Check(a.Move(d)==b.Move(d)&&Equal(a.tiles,b.tiles)&&Equal(a.terrain,b.terrain)&&Equal(a.tools,b.tools)&&a.score==b.score&&a.spawnCursor==b.spawnCursor&&a.actions==b.actions&&a.completed==b.completed&&a.failed==b.failed,"deterministic rerun with E3 engine");
    Check(a.Valid()&&b.Valid(),"state remains valid");
   }
  }

  // E4: Three-mission vertical slice catalog tests & reference solutions
  Check(ExpeditionCatalog.Missions.Count>=10,"catalog has all 10 campaign missions authored");
  
  // Validate all 10 mission definitions
  for(int i=0;i<ExpeditionCatalog.Missions.Count;i++){
   var def = ExpeditionCatalog.Get(i);
   Check(def.Valid(),"Mission "+(i+1)+" definition is valid");
   Check(!string.IsNullOrEmpty(def.id)&&!string.IsNullOrEmpty(def.title)&&!string.IsNullOrEmpty(def.description),"Mission "+(i+1)+" metadata populated");
   Check(def.chapter>=1&&def.chapter<=3,"Mission "+(i+1)+" chapter in range [1..3]");
   Check(def.actionLimit>=10&&def.actionLimit<=50,"Mission "+(i+1)+" action budget reasonable");
  }

  // Mission 1: "First opening" reference solution
  var m1 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-01"));
  Check(m1.terrain[5]==1&&m1.tiles[0]==8&&m1.tiles[1]==4&&m1.tiles[2]==4,"M1 initial board");
  Check(m1.Move(Direction.Left),"M1 move 1 Left");
  Check(m1.terrain[5]==0,"M1 lock 5 opened by adjacent merge at cell 1");
  Check(m1.tiles[0]==8&&m1.tiles[1]==8&&m1.tiles[12]==4,"M1 board after move 1");
  Check(!m1.completed,"M1 not yet completed after move 1 (need reach 16)");
  Check(m1.Move(Direction.Left),"M1 move 2 Left");
  Check(m1.tiles[0]==16,"M1 cell 0 merged 8+8 -> 16");
  Check(m1.completed&&!m1.failed,"M1 completed on action 2");
  Check(m1.CalculateStars()==3,"M1 achieves 3 stars (2/12 actions used)");

  // Mission 2: "Make room" reference solution
  var m2 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-02"));
  Check(m2.terrain[2]==1&&m2.terrain[14]==1,"M2 initial locks at 2 and 14");
  Check(m2.Move(Direction.Left),"M2 move 1 Left");
  Check(m2.tiles[0]==16&&m2.tiles[12]==16&&m2.tiles[4]==8,"M2 two 16 tiles formed");
  Check(m2.Swap(0,4),"M2 use Swap tool on cell 0 and cell 4");
  Check(m2.tiles[0]==8&&m2.tiles[4]==16,"M2 swapped 0 and 4");
  Check(m2.Move(Direction.Down),"M2 move Down to merge 16+16 at cell 12");
  Check(m2.tiles[12]==32,"M2 reach 32 achieved!");
  Check(m2.completed&&!m2.failed,"M2 completed on action 3");
  Check(m2.CalculateStars()==3,"M2 achieves 3 stars (3/16 actions used)");

  // Mission 3: "Twin tiles" reference solution
  var m3 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-03"));
  Check(m3.terrain[6]==1&&m3.terrain[9]==1,"M3 initial locks at 6 and 9");
  Check(m3.Move(Direction.Left),"M3 move 1 Left");
  Check(m3.tiles[0]==32&&m3.tiles[12]==32,"M3 two 32 tiles simultaneously on board");
  Check(m3.completed&&!m3.failed,"M3 completed on action 1 (HoldTiles 2x32)");
  Check(m3.CalculateStars()==3,"M3 achieves 3 stars (1/20 actions used)");

  // Mission 5: "Crossroads" reference solution
  var m5 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-05"));
  Check(m5.Move(Direction.Left),"M5 move 1 Left merges to two 64s");
  Check(m5.Move(Direction.Down),"M5 move 2 Down merges 64+64 -> 128");
  Check(m5.completed&&!m5.failed,"M5 completed on action 2");
  Check(m5.CalculateStars()==3,"M5 achieves 3 stars");

  // Mission 6: "Open the path" reference solution
  var m6 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-06"));
  Check(m6.Move(Direction.Left),"M6 move 1 Left opens locks 5 and 9");
  Check(m6.terrain[5]==0&&m6.terrain[9]==0,"M6 locks 5 and 9 opened");
  Check(m6.Swap(1,2),"M6 swap tool moves 16 to cell 2");
  Check(m6.Swap(13,14),"M6 swap tool moves 16 to cell 14");
  Check(m6.Move(Direction.Right),"M6 move 2 Right merges at cell 2 (above lock 6) and cell 14 (below lock 10)");
  Check(m6.terrain[6]==0&&m6.terrain[10]==0,"M6 locks 6 and 10 opened");
  Check(m6.completed&&!m6.failed,"M6 completed on action 4 (all 4 locks opened)");
  Check(m6.CalculateStars()==3,"M6 achieves 3 stars (4/28 actions used)");

  // Mission 7: "A careful swap" reference solution
  var m7 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-07"));
  Check(m7.Move(Direction.Down),"M7 move 1 Down merges 32+32 -> 64 at cell 12 and 15");
  Check(m7.tiles[12]==64&&m7.tiles[15]==64,"M7 two 64s formed simultaneously");
  Check(m7.completed&&!m7.failed,"M7 completed on action 1");
  Check(m7.CalculateStars()==3,"M7 achieves 3 stars");

  // Mission 8: "Small spaces" reference solution
  var m8 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-08"));
  Check(m8.Move(Direction.Left),"M8 move 1 Left");
  Check(m8.Move(Direction.Down),"M8 move 2 Down merges to reach 256");
  Check(m8.completed&&!m8.failed,"M8 completed on action 2");
  Check(m8.CalculateStars()==3,"M8 achieves 3 stars");

  // Mission 9: "Two goals" reference solution
  var m9 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-09"));
  Check(m9.Move(Direction.Left),"M9 move 1 Left merges two 128s and opens lock 5");
  Check(m9.terrain[5]==0,"M9 lock 5 opened");
  Check(m9.Move(Direction.Down),"M9 move 2 Down merges 128+128 at cell 13 into 256");
  Check(m9.tiles[13]==256,"M9 reached 256");
  Check(m9.Delete(13),"M9 delete tool test");
  Check(m9.tiles[13]==0,"M9 cell 13 cleared");

  // Mission 10: "Expedition finale" reference solution
  var m10 = new ExpeditionSession(ExpeditionCatalog.Get("expedition-10"));
  Check(m10.Move(Direction.Left),"M10 move 1 Left merges to two 256s");
  Check(m10.Move(Direction.Down),"M10 move 2 Down merges 256+256 at cell 13 into 512");
  Check(m10.tiles[13]==512,"M10 reach 512 achieved");
  Check(m10.Undo(),"M10 undo tool restores state before final merge");
  Check(m10.tiles[13]==256&&m10.tiles[1]==256,"M10 undo restored both 256 tiles");
  Check(m10.Move(Direction.Down),"M10 move Down re-merges into 512");
  Check(m10.tiles[13]==512,"M10 reach 512 re-achieved");

  // Full Campaign progression & chapter unlocks test
  var campaignProgress = new CampaignProgress();
  for(int i=0;i<10;i++){
   var def = ExpeditionCatalog.Get(i);
   campaignProgress.Record(def.id, def.version, 3, 3);
  }
  for(int i=0;i<10;i++){
   var p = campaignProgress.Get("expedition-0"+(i+1));
   if(i==9) p = campaignProgress.Get("expedition-10");
   Check(p!=null&&p.stars==3&&p.completed,"Campaign has 3-star record for mission "+(i+1));
  }

  Console.WriteLine("EXPEDITION_RULES_SUCCESS: "+checks+" checks");
 }
}
