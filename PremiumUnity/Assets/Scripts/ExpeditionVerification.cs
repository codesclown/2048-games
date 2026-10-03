#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Premium2048 {
public static class ExpeditionVerification {
 sealed class Memory : IPreferenceStore {
  public Dictionary<string,string> values=new Dictionary<string,string>();
  public string Read(string key,string fallback=""){string value;return values.TryGetValue(key,out value)?value:fallback;}
  public int ReadInt(string key,int fallback){return fallback;}
  public void Write(string key,string value){values[key]=value;}
  public void Delete(string key){values.Remove(key);}public void Flush(){}
 }
 static int checks;
 static void Check(bool valid,string label){if(!valid)throw new Exception("E1-E5: "+label);checks++;Debug.Log("EXPEDITION PASS: "+label);}
 public static void Run(){
  checks=0;var host=new GameObject("Expedition isolated verification");var manager=host.AddComponent<GameManager>();var memory=new Memory();
  try{
   manager.Initialize("expedition-verification",memory);manager.Navigation.Home();manager.StartGame(4);var classic=manager.Session;classic.moves=3;classic.stock[0].available=1;manager.Save();string classicBefore=JsonUtility.ToJson(classic);
   var mission=new MissionDefinition{id="foundation-test"};mission.initialTiles[0]=2;mission.initialTiles[1]=2;
   Check(manager.StartMission(mission)&&manager.Mode==GameMode.Expedition&&manager.Navigation.Screen==GameScreen.Expedition,"mission selects its own route");
   Check(manager.ActiveSession==manager.Expedition&&manager.Session==classic,"active session interface preserves Classic reference");
   Check(!manager.Undo()&&!manager.BeginSelection(Power.Swap)&&manager.BeginReward()==-1&&!manager.KeepGoing(),"mission cannot spend Classic tools or continuation");
   Check(manager.Move(Direction.Left,0),"manager routes movement into mission");manager.ResolveAnimation();manager.Home();
   Check(manager.CanResumeClassic&&manager.CanResumeExpedition,"independent resume entries available");
   Check(JsonUtility.ToJson(classic)==classicBefore,"mission movement preserves entire Classic run");
   var loadedClassic=new GamePersistence("expedition-verification",memory).Load();var loadedMission=new ExpeditionPersistence("expedition-verification",memory).Load();
   Check(JsonUtility.ToJson(loadedClassic.session)==classicBefore&&loadedMission.session.actions==1&&loadedMission.session.spawnCursor==1,"both modes reload independently");
   manager.Initialize("expedition-verification",memory);Check(manager.Resume(GameMode.Expedition)&&manager.ActiveSession==manager.Expedition,"mission resume survives manager reinitialization");manager.Home();Check(manager.Resume(GameMode.Classic)&&manager.ActiveSession==manager.Session,"Classic resume selects Classic route");
   manager.Home();manager.StartMission(mission);manager.SetBackground(true);Check(!manager.Move(Direction.Right,1)&&!manager.Resume(GameMode.Classic),"background gates both modes");manager.SetBackground(false);manager.Home();manager.Resume(GameMode.Classic);
   var store=new ExpeditionPersistence("slot-test",memory);store.Save(new ExpeditionSave{session=new ExpeditionSession(mission)});var advanced=new ExpeditionSession(mission);advanced.Move(Direction.Left);store.Save(new ExpeditionSave{session=advanced});memory.values["slot-test.expedition.save.v1.1"]="broken";
   Check(new ExpeditionPersistence("slot-test",memory).Load().session.actions==0,"damaged mission save falls back to preceding slot");
   string folder=System.IO.Path.Combine(Application.temporaryCachePath,"Expedition."+Guid.NewGuid().ToString("N"));try{var native=new NativeSaveStore(folder);new ExpeditionPersistence("native-e1",native).Save(new ExpeditionSave{session=advanced});Check(System.IO.Directory.GetFiles(folder,"*.json").Length==1&&new ExpeditionPersistence("native-e1",native).Load().session.actions==1,"mission save uses native file backend");}finally{if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}

   // E2: Locked-cell damage and opening verification
   var e2Mission=new MissionDefinition{id="e2-native-verify",spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{4,15}}}};
   e2Mission.initialTiles[0]=2;e2Mission.initialTiles[1]=2;
   e2Mission.terrain[4]=1; // 1 HP lock directly below merge destination 0
   Check(manager.StartMission(e2Mission),"start E2 lock mission");
   Check(manager.Move(Direction.Left,2),"swipe left triggers merge at cell 0 and damages lock 4");
   Check(manager.Expedition.terrain[4]==0,"adjacent lock 4 opened upon damage");
   Check(manager.Expedition.events.Exists(e=>e.kind==MissionTurnEventKind.LockOpened&&e.cell==4),"LockOpened event emitted in live session");
   Check(manager.Expedition.tiles[4]==0&&manager.Expedition.tiles[15]==2,"newly opened lock 4 was excluded from spawn this turn");
   manager.ResolveAnimation();manager.Save();
   var loadedE2=new ExpeditionPersistence("expedition-verification",memory).Load();
   Check(loadedE2.session.terrain[4]==0&&loadedE2.session.tiles[4]==0&&loadedE2.session.tiles[15]==2,"E2 opened terrain state persists accurately");

   // E3: Mission goals, budget and tools in GameManager
   var e3Mission=new MissionDefinition{
    id="e3-native-verify",actionLimit=2,
    toolAllowance=new[]{1,1,1},
    goal=new MissionGoalDefinition{kind=MissionGoalKind.ReachTile,value=4},
    spawnScript=new[]{new MissionSpawnStep{value=2,priority=new[]{15}}}
   };
   e3Mission.initialTiles[0]=2;e3Mission.initialTiles[1]=2;
   Check(manager.StartMission(e3Mission),"start E3 goal mission");
   Check(manager.Expedition.CanUseTool(Power.Swap)&&manager.BeginSelection(Power.Swap),"Expedition Swap tool selectable");
   manager.CancelSelection();
   Check(manager.Move(Direction.Left,3),"merge 2+2 -> 4 completes ReachTile goal on action 1");
   Check(manager.Expedition.completed&&!manager.Expedition.failed,"Expedition completed in GameManager");
   manager.ResolveAnimation();manager.Save();
   Check(manager.Campaign.Get("e3-native-verify")!=null&&manager.Campaign.Get("e3-native-verify").stars==3,"campaign progress and 3-star rating committed");

   // E4-E5: Ten handcrafted missions catalog integration & verification
   Check(ExpeditionCatalog.Missions.Count==10,"catalog contains all 10 campaign missions");
   for(int i=0;i<10;i++){
    var def=ExpeditionCatalog.Get(i);
    Check(def!=null&&def.Valid(),"catalog mission "+(i+1)+" valid");
    Check(manager.StartMission(def),"manager can start mission "+(i+1));
   }

   // Verify campaign unlocks across chapters
   Check(manager.StartMission(ExpeditionCatalog.Get("expedition-01")),"start catalog mission 1");
   Check(manager.Move(Direction.Left,0),"M1 move 1 Left opens lock");
   manager.ResolveAnimation();
   Check(manager.Move(Direction.Left,1),"M1 move 2 Left reaches 16");
   manager.ResolveAnimation();
   Check(manager.Expedition.completed&&manager.Expedition.CalculateStars()==3,"M1 won with 3 stars");
   manager.Save();

   Check(manager.StartMission(ExpeditionCatalog.Get("expedition-05")),"start Chapter 2 mission 5");
   Check(manager.Move(Direction.Left,0),"M5 move 1 Left");
   manager.ResolveAnimation();
   Check(manager.Move(Direction.Down,1),"M5 move 2 Down reaches 128");
   manager.ResolveAnimation();
   Check(manager.Expedition.completed&&manager.Expedition.CalculateStars()==3,"M5 won with 3 stars");
   manager.Save();

   var reloadedCampaign = new ExpeditionPersistence("expedition-verification",memory).Load().progress;
   Check(reloadedCampaign.Get("expedition-01").stars==3&&reloadedCampaign.Get("expedition-05").stars==3,"campaign progress records persisted accurately");

   // E6: Screen navigation, map, next/retry mission, and dialog verification
   manager.ExpeditionMap();
   Check(manager.Navigation.Screen==GameScreen.ExpeditionMap&&manager.Navigation.State==GameState.Menu,"ExpeditionMap navigation selects ExpeditionMap screen");
   Check(manager.StartMission(ExpeditionCatalog.Get("expedition-01")),"start M1 for E6 dialog flow");
   Check(manager.Navigation.Screen==GameScreen.Expedition&&manager.Navigation.State==GameState.Playing,"playing screen active");
   Check(manager.RetryMission()&&manager.Expedition.mission.id=="expedition-01"&&manager.Expedition.actions==0,"RetryMission resets mission state cleanly");
   Check(manager.Move(Direction.Left,0)&&manager.Move(Direction.Left,1),"play winning moves for M1");
   manager.ResolveAnimation();
   Check(manager.Navigation.Dialog==GameDialog.Win&&manager.Navigation.State==GameState.Result,"winning mission opens GameDialog.Win");
   // E7: Daily Puzzle generation, completion, streaks, and share verification
   Check(manager.StartDailyPuzzle(new DateTime(2026, 10, 3)),"start daily puzzle for 2026-10-03");
   Check(manager.Expedition.mission.id=="daily-2026-10-03","daily puzzle id matches UTC date");
   string shareText = manager.GetShareText();
   Check(shareText.Contains("2026-10-03")&&shareText.Contains("#2048Expedition"),"daily share text formatted correctly");
   manager.Daily.Record("2026-10-03", 1, 3, 14);
   Check(manager.Daily.IsCompleted("2026-10-03")&&manager.Daily.currentStreak==1,"daily completion and streak recorded");
   manager.Save();
   var reloadedDaily = new ExpeditionPersistence("expedition-verification",memory).Load().daily;
   Check(reloadedDaily.IsCompleted("2026-10-03")&&reloadedDaily.currentStreak==1,"daily progress persisted safely");
   manager.Home();
   Check(manager.Navigation.Screen==GameScreen.Home,"return to Home after daily puzzle");
  }finally{UnityEngine.Object.DestroyImmediate(host);}
  Debug.Log("EXPEDITION_INTEGRATION_SUCCESS: "+checks+" checks");
 }
}
}
#endif
