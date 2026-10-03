using System;
using UnityEngine;
namespace Premium2048 {
public enum GameChange { NewGame, Move, Undo, Swap, Delete, Continue, KeepGoing, Settings, Reset }
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour {
 public static GameManager Instance {get;private set;}
 public Session Session {get;private set;}
 public IGameSession ActiveSession {get{return Mode==GameMode.Classic?(IGameSession)Session:Expedition;}}
 public ExpeditionSession Expedition {get;private set;}
 public CampaignProgress Campaign {get;private set;}=new CampaignProgress();
 public DailyProgress Daily {get;private set;}=new DailyProgress();
 public GameMode Mode {get{return Navigation.Mode;}}
 public bool CanResumeClassic {get{return Session!=null && Session.Valid();}}
 public bool CanResumeExpedition {get{return Expedition!=null && Expedition.Valid();}}
 ExpeditionPersistence expeditionPersistence;
 public Stats Statistics {get;private set;}=new Stats();
 public long LastScoreGain {get;private set;}
 public bool LargeText {get;private set;}
 public bool ReducedMotion {get;private set;}
 public bool HighContrast {get;private set;}
 public string Language {get;private set;}="en";
 public bool Sound {get;private set;}=true;
 public bool Haptics {get;private set;}=true;
 public readonly GameNavigation Navigation=new GameNavigation();
 public bool HasUnsavedChanges {get;private set;}
 public GamePersistence Persistence {get;private set;}
 public event Action<GameChange> Changed;
 public event Action<string> ResultShared;
 readonly System.Random random=new System.Random();
 int rewardSequence;Session rewardingSession;
 void Awake(){if(Instance!=null && Instance!=this){Destroy(this);return;}Instance=this;}
 void OnDestroy(){if(Instance==this)Instance=null;}
 public void Initialize(string prefix,IPreferenceStore preferences=null) {
  preferences=preferences??new NativeSaveStore();
  expeditionPersistence=new ExpeditionPersistence(prefix,preferences);var expeditionSave=expeditionPersistence.Load();Expedition=expeditionSave.session;Campaign=expeditionSave.progress;Daily=expeditionSave.daily??new DailyProgress();
  Persistence=new GamePersistence(prefix,preferences);var save=Persistence.Load();HasUnsavedChanges=Persistence.ReadOnly || Persistence.NeedsCommit || expeditionPersistence.ReadOnly;
  Session=save.session;Statistics=save.stats;Sound=save.sound;Haptics=save.haptics;LargeText=save.largeText;ReducedMotion=save.reducedMotion;HighContrast=save.highContrast;Language=save.language=="es"?"es":"en";
  if(Session!=null)foreach(var stock in Session.stock)stock.lastEarn=-1;
 }
 public void StartGame(int dimension) {
  if(Navigation.Suspended || Navigation.State==GameState.Rewarding)return;
  if(dimension<4 || dimension>6)throw new ArgumentOutOfRangeException(nameof(dimension));
  Session=new Session(dimension,random);Statistics.played++;Navigation.Play();Commit(GameChange.NewGame);
 }
 public bool Move(Direction direction,double now) {
  if(Mode==GameMode.Expedition){if(Expedition==null || Navigation.Suspended || Navigation.State!=GameState.Playing || double.IsNaN(now) || double.IsInfinity(now) || !Expedition.Move(direction))return false;LastScoreGain=Expedition.lastTurn.scoreGain;Navigation.Animate();Commit(GameChange.Move);return true;}
  if(Session==null || Navigation.Suspended || Navigation.State!=GameState.Playing || Session.board.Blocked || (int)direction<0 || (int)direction>3 || double.IsNaN(now) || double.IsInfinity(now))return false;
  long previous=Session.board.score;
  if(!Session.Move(direction,random,now))return false;
  LastScoreGain=Session.board.score-previous;Navigation.Animate();Commit(GameChange.Move);return true;
 }
 public bool BeginSelection(Power power){
  if(Mode==GameMode.Expedition) return Expedition!=null && power!=Power.Undo && Expedition.CanUseTool(power) && Navigation.Select(power);
  return Mode==GameMode.Classic && Session!=null && power!=Power.Undo && Session.CanUse(power) && Navigation.Select(power);
 }
 public void CancelSelection(){Navigation.CancelSelection();}
 public bool Undo(){
  if(Mode==GameMode.Expedition){if(!CanActExpedition(GameState.Playing)||!Expedition.Undo())return false;Commit(GameChange.Undo);return true;}
  if(!CanAct(GameState.Playing)||!Session.Undo())return false;Commit(GameChange.Undo);return true;
 }
 public bool Swap(int first,int second){
  if(Mode==GameMode.Expedition){if(!CanSelectExpedition(Power.Swap)||!Expedition.Swap(first,second))return false;Navigation.CancelSelection();Commit(GameChange.Swap);return true;}
  if(!CanSelect(Power.Swap)||!Session.Swap(first,second))return false;Navigation.CancelSelection();Commit(GameChange.Swap);return true;
 }
 public bool Delete(int index){
  if(Mode==GameMode.Expedition){if(!CanSelectExpedition(Power.Delete)||!Expedition.Delete(index))return false;Navigation.CancelSelection();Commit(GameChange.Delete);return true;}
  if(!CanSelect(Power.Delete)||!Session.Delete(index))return false;Navigation.CancelSelection();Commit(GameChange.Delete);return true;
 }
 bool CanAct(GameState state){return Mode==GameMode.Classic && Session!=null && !Navigation.Suspended && Navigation.State==state;}
 bool CanSelect(Power power){return CanAct(GameState.Selecting) && Navigation.SelectedPower==power;}
 bool CanActExpedition(GameState state){return Mode==GameMode.Expedition && Expedition!=null && !Navigation.Suspended && Navigation.State==state;}
 bool CanSelectExpedition(Power power){return CanActExpedition(GameState.Selecting) && Navigation.SelectedPower==power;}
 public bool KeepGoing() {
  if(!CanAct(GameState.Result) || Navigation.Dialog!=GameDialog.Win || !Session.board.won || Session.board.Over)return false;
  Session.board.continued=true;Navigation.Play();Commit(GameChange.KeepGoing);return true;
 }
 public void ResolveAnimation(){Navigation.CompleteAnimation();ShowResult();}
 public void ShowResult(){
  if(Mode==GameMode.Expedition){
   if(Expedition==null || Navigation.Screen!=GameScreen.Expedition || Navigation.Dialog!=GameDialog.None) return;
   if(Expedition.completed){
    Record();
    Navigation.Open(GameDialog.Win);
    Save();
   } else if(Expedition.failed){
    Navigation.Open(GameDialog.Loss);
    Save();
   }
   return;
  }
  if(Session==null || Navigation.Screen!=GameScreen.Game || Navigation.Dialog!=GameDialog.None || !Session.board.Blocked)return;
  Navigation.Open(Session.board.won && !Session.board.continued?GameDialog.Win:GameDialog.Loss);
 }
 public void Home(){if(Navigation.State==GameState.Rewarding)return;Navigation.Home();Save();}
 public void ExpeditionMap(){if(Navigation.State==GameState.Rewarding)return;Navigation.ExpeditionMap();Save();}
 public void Resume(){Resume(GameMode.Classic);}
 public bool Resume(GameMode mode){if(Navigation.Suspended || Navigation.State==GameState.Rewarding || (mode==GameMode.Classic?!CanResumeClassic:mode!=GameMode.Expedition||!CanResumeExpedition))return false;Navigation.Play(mode);ShowResult();return true;}
 public bool StartMission(MissionDefinition mission){if(Navigation.Suspended || Navigation.State==GameState.Rewarding || mission==null)return false;var next=new ExpeditionSession(mission);Expedition=next;Navigation.Play(GameMode.Expedition);Commit(GameChange.NewGame);return true;}
 public bool StartDailyPuzzle(DateTime? utcDate = null){
  string key = DailyPuzzleService.GetTodayKey(utcDate);
  var mission = DailyPuzzleService.Generate(key);
  return StartMission(mission);
 }
 public bool RetryMission(){if(Expedition==null)return false;return StartMission(Expedition.mission.Copy());}
 public bool NextMission(){
  if(Expedition==null)return false;
  int currentIdx = ExpeditionCatalog.Missions.FindIndex(m=>m.id==Expedition.mission.id);
  if(currentIdx>=0 && currentIdx<ExpeditionCatalog.Missions.Count-1){
   return StartMission(ExpeditionCatalog.Get(currentIdx+1));
  }
  ExpeditionMap();
  return true;
 }
 public string GetShareText(){
  if(Expedition==null)return "";
  bool isDaily = Expedition.mission.id.StartsWith("daily-");
  string dateKey = isDaily ? Expedition.mission.id.Substring("daily-".Length) : ("M" + Expedition.mission.id);
  int stars = Expedition.completed ? Expedition.CalculateStars() : 0;
  return DailyPuzzleService.FormatShareText(dateKey, stars, Expedition.actions, Expedition.completed, Expedition.mission.goal != null ? Expedition.mission.goal.Describe() : "");
 }
 public void ShareResult(){
  string text = GetShareText();
  if(!string.IsNullOrEmpty(text)){
   try{GUIUtility.systemCopyBuffer = text;}catch(Exception){}
   ResultShared?.Invoke(text);
  }
 }
 public int BeginReward() {
  if(!CanAct(GameState.Result) || !Session.CanReward)return -1;
  rewardingSession=Session;Navigation.Open(GameDialog.RewardVideo);return ++rewardSequence;
 }
 public bool CompleteReward(int request,bool success) {
  if(request!=rewardSequence || rewardingSession!=Session)return false;
  rewardingSession=null;
  if(success && Session.Reward()){Navigation.EndReward();Navigation.Play();Commit(GameChange.Continue);return true;}
  Navigation.EndReward();ShowResult();return false;
 }
 public bool ContinueOffline() {
  if(!CanAct(GameState.Result) || Navigation.Dialog!=GameDialog.Continue || !Session.Reward())return false;
  Navigation.Play();Commit(GameChange.Continue);return true;
 }
 public void ToggleSound(){if(Navigation.Suspended)return;Sound=!Sound;Commit(GameChange.Settings);}
 public void ToggleHaptics(){if(Navigation.Suspended)return;Haptics=!Haptics;Commit(GameChange.Settings);}
 public void ToggleLargeText(){if(Navigation.Suspended)return;LargeText=!LargeText;Commit(GameChange.Settings);}
 public void ToggleReducedMotion(){if(Navigation.Suspended)return;ReducedMotion=!ReducedMotion;Commit(GameChange.Settings);}
 public void ToggleHighContrast(){if(Navigation.Suspended)return;HighContrast=!HighContrast;Commit(GameChange.Settings);}
 public void ToggleLanguage(){if(Navigation.Suspended)return;Language=Language=="en"?"es":"en";Commit(GameChange.Settings);}
 public void ResetProgress() {
  if(Navigation.Suspended || Navigation.Dialog!=GameDialog.ClearScores)return;
  Session=null;Statistics=new Stats();Persistence.Reset();Navigation.Home();Commit(GameChange.Reset);
 }
 public void Record() {
  if(Session!=null){
   int index=Session.board.size-4;
   Statistics.best[index]=Math.Max(Statistics.best[index],Session.board.score);
   if(Session.board.won && !Session.winRecorded){Session.winRecorded=true;Statistics.won++;}
  }
  if(Expedition!=null && Expedition.completed){
   int stars=Expedition.CalculateStars();
   if(Expedition.mission.id.StartsWith("daily-")){
    string key = Expedition.mission.id.Substring("daily-".Length);
    Daily.Record(key, Expedition.mission.version, stars, Expedition.actions);
   } else if(Campaign!=null){
    Campaign.Record(Expedition.mission.id,Expedition.mission.version,stars,Expedition.actions);
   }
  }
 }
 void Commit(GameChange change){Save();Changed?.Invoke(change);}
 public void Save() {
  bool expeditionFailed=false;
  if(expeditionPersistence!=null){try{expeditionPersistence.Save(new ExpeditionSave{session=Expedition,progress=Campaign,daily=Daily});}catch(Exception e){expeditionFailed=true;Debug.LogWarning("Expedition save failed: "+e.Message);}}
  if(Persistence==null)return;Record();
  try{Persistence.Save(new GameSave{session=Session,stats=Statistics,sound=Sound,haptics=Haptics,largeText=LargeText,reducedMotion=ReducedMotion,highContrast=HighContrast,language=Language});HasUnsavedChanges=expeditionFailed || Persistence.ReadOnly || expeditionPersistence.ReadOnly;}
  catch(Exception e){HasUnsavedChanges=true;Debug.LogWarning("Game save failed; retrying at the next action or lifecycle boundary: "+e.Message);}
 }
 public void SetBackground(bool background){if(background){Navigation.Suspend();Save();}else Navigation.Foreground();}
}
}
