#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Premium2048 {
// Integration checks run only in the development player, using isolated memory storage.
public static class PhaseOneVerification {
 sealed class MemoryPreferences : IPreferenceStore {
  public readonly Dictionary<string,string> values=new Dictionary<string,string>();
  public int flushes;public bool failNext;
  public string Read(string key,string fallback=""){return values.TryGetValue(key,out var value)?value:fallback;}
  public int ReadInt(string key,int fallback){return int.TryParse(Read(key),out var value)?value:fallback;}
  public void Write(string key,string value){if(failNext){failNext=false;throw new Exception("Simulated interrupted write");}values[key]=value;}
  public void Delete(string key){values.Remove(key);}
  public void Flush(){flushes++;}
 }
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception("Phase 1: "+message);Debug.Log("PHASE1 PASS: "+message);}
 public static void Run(GameManager manager) {
  checks=0;NativeFileChecks();PersistenceChecks();NavigationChecks();ManagerChecks(manager);
  Debug.Log("PHASE1_SERVICE_SUCCESS: "+checks+" checks");
 }
 static GameSave Save(int size,long score) {var session=new Session(size,new System.Random(7));session.board.score=score;return new GameSave{session=session,stats=new Stats{played=1,best=new[]{size==4?score:0,size==5?score:0,size==6?score:0}}};}
 static void NativeFileChecks() {
  string folder=System.IO.Path.Combine(Application.temporaryCachePath,"PhaseOne."+Guid.NewGuid().ToString("N"));
  try {
   var files=new NativeSaveStore(folder);var store=new GamePersistence("native-file-test",files);store.Load();store.Save(Save(4,40));store.Save(Save(4,80));
   Check(new GamePersistence("native-file-test",files).Load().session.board.score==80,"native atomic replacement reloads newest slot");
   foreach(var path in System.IO.Directory.GetFiles(folder,"*.json")) {
    var slot=JsonUtility.FromJson<SaveSlot>(System.IO.File.ReadAllText(path));
    if(slot.payload.Contains("\"score\":80")){System.IO.File.WriteAllText(path,"broken");System.IO.File.WriteAllText(path+".tmp","interrupted next commit");}
   }
   Check(new GamePersistence("native-file-test",files).Load().session.board.score==40,"separate native slot recovers from corruption and leftover temp file");
  } finally {if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
 }
 static void PersistenceChecks() {
  var memory=new MemoryPreferences();memory.values["test.session.v1"]=JsonUtility.ToJson(Save(5,42).session);memory.values["test.stats.v1"]=JsonUtility.ToJson(new Stats{played=3,best=new long[]{0,42,0}});memory.values["test.sound"]="0";
  memory.failNext=true;var failedMigration=new GamePersistence("test",memory);var legacy=failedMigration.Load();
  Check(legacy.session.board.score==42 && failedMigration.NeedsCommit && memory.values.ContainsKey("test.session.v1"),"failed migration remains playable and preserves legacy data");
  var store=new GamePersistence("test",memory);var loaded=store.Load();
  Check(loaded.session.board.size==5 && loaded.session.board.score==42 && loaded.stats.played==3 && !loaded.sound,"legacy session/stats/integer sound migrate together");
  Check(store.Commits==1 && !store.Save(loaded),"unchanged refresh/save does not flush");
  Check(new GamePersistence("test",memory).Load().session.board.score==42,"migrated save reloads");
  store.Save(Save(5,80));Check(new GamePersistence("test",memory).Load().session.board.score==80,"newest complete transaction reloads");
  memory.values["test.save.v2.1"]="{broken";
  Check(new GamePersistence("test",memory).Load().session.board.score==42,"corrupt newest slot falls back to previous commit");
  memory.values["test.save.v2.1"]=memory.values["test.save.v2.0"].Replace("42","99");
  Check(new GamePersistence("test",memory).Load().session.board.score==42,"checksum rejects changed payload");
  memory.failNext=true;try{store.Save(Save(5,120));}catch(Exception){}
  Check(new GamePersistence("test",memory).Load().session.board.score==42,"interrupted write preserves previous session and stats");
  var invalid=Save(4,0);invalid.session.board.cells=null;bool rejected=false;try{store.Save(invalid);}catch(ArgumentException){rejected=true;}
  Check(rejected,"invalid save cannot replace a committed record");
  var future=Save(4,0);future.version=3;
  var record=JsonUtility.FromJson<SaveSlot>(memory.values["test.save.v2.0"]);
  record.payload=JsonUtility.ToJson(future);
  using(var hash=System.Security.Cryptography.SHA256.Create())record.checksum=Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(record.revision.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+record.payload)));
  memory.values["test.save.v2.0"]=JsonUtility.ToJson(record);memory.values.Remove("test.save.v2.1");
  var oldApp=new GamePersistence("test",memory);oldApp.Load();Check(oldApp.ReadOnly && !oldApp.Save(Save(4,0)),"future schema is preserved by an older app");
  store.Reset();store.Save(new GameSave());loaded=new GamePersistence("test",memory).Load();
  Check(loaded.session==null && loaded.stats.played==0 && !memory.values.ContainsKey("test.session.v1"),"reset cannot resurrect legacy or backup session");
 }
 static void NavigationChecks() {
  var nav=new GameNavigation();Check(!nav.AcceptsBoardInput,"loading blocks input");nav.Home();Check(nav.State==GameState.Menu && !nav.AcceptsBoardInput,"home blocks board input");
  nav.Play();nav.Animate();Check(nav.State==GameState.Animating && !nav.AcceptsBoardInput && !nav.Select(Power.Delete),"animation gates selection and board input");
  nav.CompleteAnimation();nav.Select(Power.Swap);Check(nav.State==GameState.Selecting && nav.AcceptsBoardInput,"selection accepts targeting");
  nav.Suspend();Check(nav.State==GameState.Paused && nav.SelectedPower==null && !nav.AcceptsBoardInput,"background cancels selection into pause");
  nav.Foreground();Check(nav.State==GameState.Paused,"foreground does not auto-resume gameplay");nav.Play();
  nav.Open(GameDialog.Pause);nav.Open(GameDialog.Instructions);nav.Back();Check(nav.Dialog==GameDialog.Pause,"instructions return to pause menu");
  nav.Open(GameDialog.Scores);nav.Open(GameDialog.ClearScores);nav.Back();Check(nav.Dialog==GameDialog.Scores,"reset cancellation returns to scores");nav.Back();Check(nav.Dialog==GameDialog.Pause,"nested scores cancellation preserves its pause parent");
  nav.Play();nav.Open(GameDialog.Loss);nav.Open(GameDialog.Continue);nav.Back();Check(nav.Dialog==GameDialog.Loss,"continuation cancellation returns to loss");
  nav.Open(GameDialog.RewardVideo);nav.Back();Check(nav.State==GameState.Rewarding && !nav.AcceptsBoardInput,"reward cannot be dismissed into active play");
  nav.Suspend();nav.EndReward();nav.Play();nav.Foreground();Check(nav.State==GameState.Paused,"reward completion in background returns to pause");
 }
 static void ManagerChecks(GameManager manager) {
  var memory=new MemoryPreferences();manager.Initialize("manager-test",memory);manager.Navigation.Foreground();
  int changes=0;Action<GameChange> listen=change=>changes++;manager.Changed+=listen;
  for(int size=4;size<=6;size++) {
   manager.StartGame(size);var session=manager.Session;session.board=new Board(size);session.board.cells[0]=2;session.board.cells[1]=2;
   int writes=manager.Persistence.Commits;Check(!manager.Move(Direction.Up,0) && manager.Persistence.Commits==writes,"invalid move spends/writes nothing: "+size);
   Check(manager.Move(Direction.Right,1) && manager.Navigation.State==GameState.Animating,"valid move enters animation: "+size);
   Check(!manager.Move(Direction.Down,2) && !manager.Undo(),"concurrent gameplay actions rejected: "+size);
   manager.SetBackground(true);manager.SetBackground(false);Check(manager.Navigation.State==GameState.Paused && session.moves==1 && session.board.score==4,"pause mid-animation preserves committed move: "+size);
   manager.Resume();Check(manager.Move(Direction.Down,3),"resume accepts next valid move: "+size);manager.ResolveAnimation();Check(manager.Undo(),"manager owns undo: "+size);
   session.board=new Board(size);session.board.cells[0]=2;session.board.cells[1]=4;session.board.cells[2]=8;
   Check(!manager.Delete(2) && manager.BeginSelection(Power.Swap),"tools require the matching selection state: "+size);
   writes=manager.Persistence.Commits;Check(!manager.Swap(0,2) && manager.Persistence.Commits==writes,"invalid swap does not consume/save: "+size);
   Check(manager.Swap(0,1) && session.board.cells[0]==4 && manager.Navigation.State==GameState.Playing,"valid swap commits and exits selection: "+size);
   Check(manager.BeginSelection(Power.Delete) && manager.Delete(2) && session.board.cells[2]==0,"manager owns delete: "+size);
   manager.BeginSelection(Power.Swap);manager.CancelSelection();Check(manager.Navigation.State==GameState.Playing,"cancel selection returns to playing: "+size);
   manager.Home();Check(!manager.Move(Direction.Left,4),"home rejects moves: "+size);manager.Resume();
   session.board=new Board(size);session.board.cells[0]=session.board.cells[1]=session.board.target/4;session.board.score=999999;
   Check(manager.Move(Direction.Left,4.5),"below-target merge commits despite high total score: "+size);manager.ResolveAnimation();
   Check(!session.board.won && manager.Navigation.Dialog==GameDialog.None,"score and below-target tile cannot complete the level: "+size);
   session.board=new Board(size);session.board.cells[0]=session.board.cells[1]=session.board.target/2;
   Check(manager.Move(Direction.Left,5),"target merge commits: "+size);manager.ResolveAnimation();Check(manager.Navigation.Dialog==GameDialog.Win && session.winRecorded,"win is recorded once and opens result: "+size);
   int wins=manager.Statistics.won;Check(manager.KeepGoing() && session.board.continued,"keep going is a manager action: "+size);manager.Save();Check(manager.Statistics.won==wins,"repeated save cannot count win twice: "+size);
   session.board=new Board(size);for(int i=0;i<size*size;i++)session.board.cells[i]=(i%size+i/size)%2==0?2:4;
   session.board.score=999;session.history.Clear();var previous=new Board(size);previous.cells[0]=2;previous.cells[1]=4;session.history.Add(previous);manager.ShowResult();
   Check(manager.Navigation.Dialog==GameDialog.Loss,"loss opens result: "+size);manager.Navigation.Open(GameDialog.Continue);
   Check(manager.ContinueOffline() && session.rewardUsed && session.board.score==999,"offline continuation preserves score and commits: "+size);
   Check(!manager.ContinueOffline(),"continuation cannot be granted twice: "+size);
   session.rewardUsed=false;session.history.Add(previous);session.board=new Board(size);for(int i=0;i<size*size;i++)session.board.cells[i]=(i%size+i/size)%2==0?2:4;manager.ShowResult();
   int request=manager.BeginReward();Check(request>0 && !manager.Move(Direction.Left,6),"pending reward blocks input: "+size);
   Check(!manager.CompleteReward(request,false) && manager.Navigation.Dialog==GameDialog.Loss,"failed reward grants nothing: "+size);
   request=manager.BeginReward();Check(manager.CompleteReward(request,true) && !manager.CompleteReward(request,true),"reward callback is granted exactly once: "+size);
   manager.Save();int commits=manager.Persistence.Commits;manager.Save();Check(manager.Persistence.Commits==commits,"repeated lifecycle save is deduplicated: "+size);
   var reloaded=new GamePersistence("manager-test",memory).Load();Check(reloaded.session.Valid() && reloaded.session.rewardUsed && reloaded.stats.won==manager.Statistics.won,"board/history/inventory/stats resume together: "+size);
  }
  foreach(int size in new[]{4,5,6}){
   manager.StartGame(size);var blocked=manager.Session.board;
   for(int i=0;i<blocked.cells.Length;i++)blocked.cells[i]=(i%size+i/size)%2==0?2:4;
   blocked.cells[0]=blocked.target;blocked.won=true;manager.ShowResult();
   Check(blocked.Over && manager.Navigation.Dialog==GameDialog.Win,"target achievement takes priority over a blocked board: "+size);
  }
  memory.failNext=true;manager.ToggleSound();Check(manager.HasUnsavedChanges,"failed commit retains the live state and signals a pending retry");
  manager.Save();Check(!manager.HasUnsavedChanges,"next lifecycle boundary retries the pending save");
  Check(changes>0,"committed actions publish view events");manager.Changed-=listen;
  manager.Initialize("Premium2048.QA");manager.Home();
 }
}
}
#endif
