using System;
using UnityEngine;
namespace Premium2048 {
public enum GameChange { NewGame, Move, Undo, Swap, Delete, Continue, KeepGoing, Settings, Reset }
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour {
 public static GameManager Instance {get;private set;}
 public Session Session {get;private set;}
 public Stats Statistics {get;private set;}=new Stats();
 public long LastScoreGain {get;private set;}
 public bool Sound {get;private set;}=true;
 public readonly GameNavigation Navigation=new GameNavigation();
 public bool HasUnsavedChanges {get;private set;}
 public GamePersistence Persistence {get;private set;}
 public event Action<GameChange> Changed;
 readonly System.Random random=new System.Random();
 int rewardSequence;Session rewardingSession;
 void Awake(){if(Instance!=null && Instance!=this){Destroy(this);return;}Instance=this;}
 void OnDestroy(){if(Instance==this)Instance=null;}
 public void Initialize(string prefix,IPreferenceStore preferences=null) {
  Persistence=new GamePersistence(prefix,preferences);var save=Persistence.Load();HasUnsavedChanges=Persistence.ReadOnly || Persistence.NeedsCommit;
  Session=save.session;Statistics=save.stats;Sound=save.sound;
  if(Session!=null)foreach(var stock in Session.stock)stock.lastEarn=-1;
 }
 public void StartGame(int dimension) {
  if(Navigation.Suspended || Navigation.State==GameState.Rewarding)return;
  if(dimension<4 || dimension>6)throw new ArgumentOutOfRangeException(nameof(dimension));
  Session=new Session(dimension,random);Statistics.played++;Navigation.Play();Commit(GameChange.NewGame);
 }
 public bool Move(Direction direction,double now) {
  if(Session==null || Navigation.Suspended || Navigation.State!=GameState.Playing || Session.board.Blocked || (int)direction<0 || (int)direction>3 || double.IsNaN(now) || double.IsInfinity(now))return false;
  long previous=Session.board.score;
  if(!Session.Move(direction,random,now))return false;
  LastScoreGain=Session.board.score-previous;Navigation.Animate();Commit(GameChange.Move);return true;
 }
 public bool BeginSelection(Power power){return Session!=null && power!=Power.Undo && Session.CanUse(power) && Navigation.Select(power);}
 public void CancelSelection(){Navigation.CancelSelection();}
 public bool Undo(){if(!CanAct(GameState.Playing) || !Session.Undo())return false;Commit(GameChange.Undo);return true;}
 public bool Swap(int first,int second){if(!CanSelect(Power.Swap) || !Session.Swap(first,second))return false;Navigation.CancelSelection();Commit(GameChange.Swap);return true;}
 public bool Delete(int index){if(!CanSelect(Power.Delete) || !Session.Delete(index))return false;Navigation.CancelSelection();Commit(GameChange.Delete);return true;}
 bool CanAct(GameState state){return Session!=null && !Navigation.Suspended && Navigation.State==state;}
 bool CanSelect(Power power){return CanAct(GameState.Selecting) && Navigation.SelectedPower==power;}
 public bool KeepGoing() {
  if(!CanAct(GameState.Result) || Navigation.Dialog!=GameDialog.Win || !Session.board.won || Session.board.Over)return false;
  Session.board.continued=true;Navigation.Play();Commit(GameChange.KeepGoing);return true;
 }
 public void ResolveAnimation(){Navigation.CompleteAnimation();ShowResult();}
 public void ShowResult(){if(Session==null || Navigation.Screen!=GameScreen.Game || Navigation.Dialog!=GameDialog.None || !Session.board.Blocked)return;Navigation.Open(Session.board.Over?GameDialog.Loss:GameDialog.Win);}
 public void Home(){if(Navigation.State==GameState.Rewarding)return;Navigation.Home();Save();}
 public void Resume(){if(Session==null || Navigation.Suspended || Navigation.State==GameState.Rewarding)return;Navigation.Play();ShowResult();}
 public int BeginReward() {
  if(!CanAct(GameState.Result) || !Session.CanReward)return -1;
  rewardingSession=Session;Navigation.Open(GameDialog.RewardVideo);return ++rewardSequence;
 }
 public bool CompleteReward(int request,bool success) {
  if(request!=rewardSequence || rewardingSession!=Session || Navigation.State!=GameState.Rewarding)return false;
  rewardingSession=null;
  if(success && Session.Reward()){Navigation.EndReward();Navigation.Play();Commit(GameChange.Continue);return true;}
  Navigation.EndReward();ShowResult();return false;
 }
 public bool ContinueOffline() {
  if(!CanAct(GameState.Result) || Navigation.Dialog!=GameDialog.Continue || !Session.Reward())return false;
  Navigation.Play();Commit(GameChange.Continue);return true;
 }
 public void ToggleSound(){if(Navigation.Suspended)return;Sound=!Sound;Commit(GameChange.Settings);}
 public void ResetProgress() {
  if(Navigation.Suspended || Navigation.Dialog!=GameDialog.ClearScores)return;
  Session=null;Statistics=new Stats();Persistence.Reset();Navigation.Home();Commit(GameChange.Reset);
 }
 public void Record() {
  if(Session==null)return;int index=Session.board.size-4;
  Statistics.best[index]=Math.Max(Statistics.best[index],Session.board.score);
  if(Session.board.won && !Session.winRecorded){Session.winRecorded=true;Statistics.won++;}
 }
 void Commit(GameChange change){Save();Changed?.Invoke(change);}
 public void Save() {
  if(Persistence==null)return;Record();
  try{Persistence.Save(new GameSave{session=Session,stats=Statistics,sound=Sound});HasUnsavedChanges=Persistence.ReadOnly;}
  catch(Exception e){HasUnsavedChanges=true;Debug.LogWarning("Game save failed; retrying at the next action or lifecycle boundary: "+e.Message);}
 }
 public void SetBackground(bool background){if(background){Navigation.Suspend();Save();}else Navigation.Foreground();}
}
}
