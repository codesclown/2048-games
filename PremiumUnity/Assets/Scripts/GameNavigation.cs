using System;
using System.Collections.Generic;
namespace Premium2048 {
public enum GameScreen { Loading, Home, Game }
public enum GameState { Loading, Menu, Playing, Animating, Selecting, Rewarding, Paused, Result }
public enum GameDialog { None, Pause, Levels, Instructions, Scores, Settings, Restart, ClearScores, Win, Loss, Continue, VideoUnavailable, RewardVideo }

// Owns navigation and input gates; the view only renders these decisions.
public sealed class GameNavigation {
 public GameScreen Screen {get;private set;}=GameScreen.Loading;
 public GameState State {get;private set;}=GameState.Loading;
 public GameDialog Dialog {get;private set;}
 public Power? SelectedPower {get;private set;}
 public bool Suspended {get;private set;}
 public event Action Changed;
 readonly Stack<GameDialog> history=new Stack<GameDialog>();
 public bool AcceptsBoardInput {get{return !Suspended && (State==GameState.Playing || State==GameState.Selecting);}}
 void Notify(){Changed?.Invoke();}
 public void Home(){Screen=GameScreen.Home;State=GameState.Menu;Dialog=GameDialog.None;history.Clear();SelectedPower=null;Notify();}
 public void Play(){Screen=GameScreen.Game;State=Suspended?GameState.Paused:GameState.Playing;Dialog=Suspended?GameDialog.Pause:GameDialog.None;history.Clear();SelectedPower=null;Notify();}
 public void Loading(){Screen=GameScreen.Loading;State=GameState.Loading;Dialog=GameDialog.None;history.Clear();SelectedPower=null;Notify();}
 public bool Animate(){if(Suspended || State!=GameState.Playing)return false;State=GameState.Animating;Notify();return true;}
 public void CompleteAnimation(){if(State!=GameState.Animating)return;State=GameState.Playing;Notify();}
 public bool Select(Power power){if(Suspended || State!=GameState.Playing)return false;SelectedPower=power;State=GameState.Selecting;Notify();return true;}
 public void CancelSelection(){if(State!=GameState.Selecting)return;SelectedPower=null;State=GameState.Playing;Notify();}
 public void Open(GameDialog dialog) {
  if(dialog==GameDialog.None || State==GameState.Rewarding)return;
  if(Dialog==dialog)return;
  bool child=(dialog==GameDialog.Instructions || dialog==GameDialog.Scores || dialog==GameDialog.Levels || dialog==GameDialog.Settings) && Dialog==GameDialog.Pause
   || dialog==GameDialog.ClearScores && Dialog==GameDialog.Scores
   || (dialog==GameDialog.Continue || dialog==GameDialog.VideoUnavailable) && Dialog==GameDialog.Loss;
  if(child)history.Push(Dialog);else history.Clear();
  Activate(dialog);
 }
 void Activate(GameDialog dialog) {
  Dialog=dialog;SelectedPower=null;
  State=dialog==GameDialog.RewardVideo?GameState.Rewarding:
   dialog==GameDialog.Win || dialog==GameDialog.Loss || dialog==GameDialog.Continue || dialog==GameDialog.VideoUnavailable?GameState.Result:
   Screen==GameScreen.Game?GameState.Paused:GameState.Menu;
  Notify();
 }
 public void Back() {
  if(State==GameState.Rewarding)return;
  if(history.Count>0){Activate(history.Pop());return;}
  Dialog=GameDialog.None;history.Clear();SelectedPower=null;State=Screen==GameScreen.Game?GameState.Playing:Screen==GameScreen.Loading?GameState.Loading:GameState.Menu;Notify();
 }
 public void EndReward(){if(State!=GameState.Rewarding)return;Dialog=GameDialog.None;history.Clear();State=GameState.Playing;Notify();}
 public void Suspend() {
  if(Suspended)return;Suspended=true;
  if(Screen==GameScreen.Game && State!=GameState.Rewarding && Dialog==GameDialog.None){SelectedPower=null;Dialog=GameDialog.Pause;State=GameState.Paused;}
  Notify();
 }
 public void Foreground(){if(!Suspended)return;Suspended=false;Notify();}
}
}
