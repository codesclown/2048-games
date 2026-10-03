using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Premium2048;

namespace Premium2048 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class PremiumSmokeRunner : MonoBehaviour {
 const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
 static int stage, frames;
 static double started;
 static PremiumGame game;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot() {
  if (Array.IndexOf(Environment.GetCommandLineArgs(), "-premiumSmoke") < 0) return;
  started = Time.realtimeSinceStartupAsDouble;
  new GameObject("Premium QA Runner").AddComponent<PremiumSmokeRunner>();
 }

 void Update() { Tick(); }

 static T Get<T>(string field) {
  var info = game.GetType().GetField(field, Flags);
  return (T)(info != null ? info.GetValue(game) : game.GetType().GetProperty(field, Flags).GetValue(game));
 }

 static void Call(string method, params object[] args) {
  game.GetType().GetMethod(method, Flags).Invoke(game, args);
 }

 static void Check(bool condition, string message) {
  if (!condition) throw new Exception(message);
  Debug.Log("QA PASS: " + message);
 }

 static void Click(string label) {
  foreach (var button in game.GetComponentsInChildren<Button>()) {
   if (button.gameObject.name == label) {
    Check(button.interactable, "button enabled: " + label);
    button.onClick.Invoke();
    return;
   }
  }
  throw new Exception("Missing button: " + label);
 }

 static void Select(int index) {
  var grid = Get<RectTransform>("grid");
  var position = (Vector2)game.GetType().GetMethod("CellPosition", Flags).Invoke(game, new object[] { index });
  Call("SelectCell", RectTransformUtility.WorldToScreenPoint(null, grid.TransformPoint(position)));
 }

 static void GestureChecks() {
  Call("NewGame", 4);
  var manager = Get<GameManager>("manager");
  var controller = Get<GameController>("controller");
  var grid = Get<RectTransform>("grid");
  var begin = controller.GetType().GetMethod("Begin", Flags);
  var end = controller.GetType().GetMethod("End", Flags);
  var center = RectTransformUtility.WorldToScreenPoint(null, grid.position);
  Action<Direction> move = direction => Call("Move", direction);
  Action<Vector2> select = point => Call("SelectCell", point);
  var session = manager.Session;
  session.board = new Board(4);
  session.board.cells[0] = 2;
  session.board.cells[1] = 2;
  Call("Refresh", true);
  int writes = manager.Persistence.Commits;

  begin.Invoke(controller, new object[] { grid, center });
  end.Invoke(controller, new object[] { center + Vector2.one * 2, false, move, select });
  Check(session.moves == 0 && manager.Persistence.Commits == writes, "tiny gesture does not move or persist");

  begin.Invoke(controller, new object[] { grid, center + Vector2.right * 1000 });
  end.Invoke(controller, new object[] { center, false, move, select });
  Check(session.moves == 0, "gesture starting outside board is ignored");

  begin.Invoke(controller, new object[] { grid, center });
  controller.Cancel();
  end.Invoke(controller, new object[] { center + Vector2.right * 180, false, move, select });
  Check(session.moves == 0, "cancelled gesture cannot apply later");

  begin.Invoke(controller, new object[] { grid, center });
  end.Invoke(controller, new object[] { center + Vector2.right * 180, false, move, select });
  Check(session.moves == 1 && session.board.score == 4 && Get<bool>("animating"), "real controller swipe reaches manager and animation");

  Call("OnApplicationPause", true);
  Call("OnApplicationFocus", false);
  Call("OnApplicationPause", false);
  Check(manager.Navigation.Suspended && manager.Navigation.State == GameState.Paused && !Get<bool>("animating"), "pause mid-animation resolves visuals and overlapping focus loss stays suspended");

  Call("Move", Direction.Down);
  Check(session.moves == 1, "background gesture cannot alter saved board");

  Call("OnApplicationFocus", true);
  Check(!manager.Navigation.Suspended && manager.Navigation.State == GameState.Paused, "foreground retains pause menu");

  Click("Resume");
  Check(manager.Navigation.State == GameState.Playing && session.board.score == 4, "explicit resume preserves score after interrupted animation");
  Check(manager.Persistence.Load().session.board.score == 4, "lifecycle boundary persisted committed swipe");

  Call("ShowHome");
 }

 static void Tick() {
  try {
   if (Time.realtimeSinceStartupAsDouble - started > 60) throw new Exception("Smoke verification timed out");

   if (game == null) game = UnityEngine.Object.FindFirstObjectByType<PremiumGame>();
   if (game == null || !Get<bool>("ready")) return;
   if (Get<bool>("animating")) return;
   if (++frames < 3) return;
   frames = 0;
   Canvas.ForceUpdateCanvases();
   var session = Get<Session>("session");
   var manager = Get<GameManager>("manager");

   switch (stage++) {
    case 0:
     ExpeditionVerification.Run();
     PhaseOneVerification.Run(manager);
     PhaseTwoVerification.Run(manager);
     PhaseThreeVerification.Run(game);
     PhaseFourVerification.Run(game, manager);
     GestureChecks();
     Check(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1 && UnityEngine.Object.FindObjectsByType<GameController>(FindObjectsSortMode.None).Length == 1, "exactly one game manager and controller");
     Check(Get<GameObject>("home").activeSelf, "home initializes");
     ScreenCapture.CaptureScreenshot("/tmp/premium-home.png");
     // Test Settings dialog
     Click("Settings");
     Check(Get<GameObject>("modal").activeSelf, "settings modal opened");
     ScreenCapture.CaptureScreenshot("/tmp/premium-settings.png");
     Click("About");Check(manager.Navigation.Dialog==GameDialog.About,"about screen opens from settings");
     Click("Back");Check(manager.Navigation.Dialog==GameDialog.Settings,"about returns to the settings screen");
     Click("Haptic feedback");
     Check(!manager.Haptics, "haptic feedback toggled off");
     Click("Haptic feedback");
     Check(manager.Haptics, "haptic feedback toggled on");
     Click("Large text");
     Check(manager.LargeText, "large text toggled on");
     Click("Large text");
     Check(!manager.LargeText, "large text toggled off");
     Click("High contrast");
     Check(manager.HighContrast, "high contrast toggled on");
     Click("High contrast");
     Check(!manager.HighContrast, "high contrast toggled off");
     Click("Language");
     Check(manager.Language == "es", "language toggled to Spanish");
     Click("Language");
     Check(manager.Language == "en", "language toggled back to English");
     Click("Close");
     // Test Levels dialog
     Click("New Game");
     Check(Get<GameObject>("modal").activeSelf, "levels modal opened");
     ScreenCapture.CaptureScreenshot("/tmp/premium-levels.png");
     var existing=manager.Session;
     Click("Level 1");
     Check(manager.Navigation.Dialog==GameDialog.ReplaceBoard && manager.Session==existing,"changing boards requires confirmation and preserves the saved board");
     Click("Keep current board");
     Check(manager.Navigation.Dialog==GameDialog.Levels && manager.Session==existing,"cancel new board returns to levels without losing progress");
     Click("Level 1");Click("Start new board");
     stage = 1;
     break;

    case 1:
     Check(session.board.cells.Length == 16 && Get<Image[]>("tiles").Length == 16, "4x4 UI and board");
     ScreenCapture.CaptureScreenshot("/tmp/premium-game.png");
     stage = 15;
     break;

    case 15:
     stage = 2;
     session.board = new Board(4);
     session.board.cells[0] = 2;
     session.board.cells[1] = 2;
     Call("Refresh", true);
     Call("Move", Direction.Right);
     break;

    case 2:
     Check(session.board.score == 4 && session.moves == 1, "input to animated move and score");
     Call("Move", Direction.Down);
     break;

    case 3:
     Check(session.Unlocked, "powers unlocked by actual moves");
     Click("Undo");
     break;

    case 4:
     Check(session.stock[0].used == 1, "Undo UI consumes stock");
     session.board = new Board(4);
     session.board.cells[0] = 2;
     session.board.cells[1] = 4;
     session.board.cells[2] = 8;
     Call("Refresh", true);
     Click("Swap");
     Select(0);
     Select(1);
     Check(Get<bool>("animating") && !Get<bool>("Playing"),"swap animates with board input gated");
     break;

    case 5:
     Check(session.board.cells[0] == 4 && session.board.cells[1] == 2, "Swap selection UI");
     Click("Delete");
     Select(2);
     Check(Get<bool>("animating") && !Get<bool>("Playing"),"delete animates with board input gated");
     break;

    case 6:
     Check(session.board.cells[2] == 0 && session.stock[2].used == 1, "Delete selection UI");
     Click("Menu");
     Check(Get<GameObject>("modal").activeSelf, "pause menu");
     Click("Resume");
     Call("NewGame", 5);
     break;

    case 7:
     Check(session.board.cells.Length == 25 && Get<Image[]>("tiles").Length == 25, "5x5 UI and board");
     Call("NewGame", 6);
     break;

    case 8:
     Check(session.board.cells.Length == 36 && Get<Image[]>("tiles").Length == 36, "6x6 UI and board");
     session.board = new Board(6);
     session.board.cells[0] = session.board.cells[1] = 4096;
     Call("Refresh", true);
     Call("Move", Direction.Left);
     break;

    case 9:
     Check(session.board.won && Get<GameObject>("modal").activeSelf, "8192 win dialog");
     ScreenCapture.CaptureScreenshot("/tmp/premium-win.png");
     Click("Keep going");
     Check(!session.board.Blocked, "keep going UI");
     session.board = new Board(6);
     for (int i = 0; i < 36; i++) session.board.cells[i] = (i % 6 + i / 6) % 2 == 0 ? 2 : 4;
     session.board.score = 999;
     session.history.Clear();
     var old = new Board(6);
     old.cells[0] = 2;
     old.cells[1] = 4;
     session.history.Add(old);
     Call("Refresh", true);
     break;

    case 10:
     Check(Get<GameObject>("modal").activeSelf && session.board.Over, "loss dialog");
     ScreenCapture.CaptureScreenshot("/tmp/premium-loss.png");
     Click("Continue: rewind 5 moves");
     Click("Continue");
     Check(session.rewardUsed && session.board.score == 999 && !session.board.Over, "offline continuation UI");
     Click("Menu");
     Click("How to Play");
     break;

    case 11:
     foreach (var text in Get<RectTransform>("modalPanel").GetComponentsInChildren<Text>()) {
      if (text.name == "Rules") Check(text.preferredHeight <= text.rectTransform.rect.height, "instructions fit dialog");
     }
     ScreenCapture.CaptureScreenshot("/tmp/premium-instructions.png");
     Click("Close");
     Call("ShowScores");
     break;

    case 12:
     Check(Get<GameObject>("modal").activeSelf, "high score dialog");
     ScreenCapture.CaptureScreenshot("/tmp/premium-scores.png");
     Click("Close");
     Call("ShowHome");stage=17;
     break;

    case 17:
     Call("NewGame",4);session=manager.Session;session.board=new Board(4);
     session.board.cells[0]=session.board.cells[1]=1024;Call("Refresh",true);Call("Move",Direction.Left);
     break;
    case 18:
     Check(manager.Navigation.Dialog==GameDialog.Win && session.board.cells[0]==2048,"level one completes on its 2048 tile");
     Click("Next level");session=manager.Session;
     Check(session.board.size==5 && session.board.target==4096,"next level starts the 4096-target board");
     session.board=new Board(5);session.board.cells[0]=session.board.cells[1]=2048;Call("Refresh",true);Call("Move",Direction.Left);
     break;
    case 19:
     Check(manager.Navigation.Dialog==GameDialog.Win && session.board.cells[0]==4096,"level two completes on its 4096 tile");
     Click("Next level");session=manager.Session;
     Check(session.board.size==6 && session.board.target==8192,"next level starts the 8192-target board");
     Call("ShowHome");
     Check(Get<GameObject>("home").activeSelf, "home navigation");
     Click("Resume");
     Check(Get<GameObject>("game").activeSelf, "saved game resume");
     Call("NewGame",4);session=manager.Session;
     session.board=new Board(4);session.board.cells[0]=session.board.cells[1]=2;Call("Refresh",true);
     Call("Move",Direction.Right);Call("QueueMove",Direction.Down);
     Check(session.moves==1 && Get<Direction?>("queuedMove")==Direction.Down,"rapid second swipe is buffered without mutating the animated board");
     break;
    case 20:
     Check(session.moves==2,"buffered swipe executes once after the first animation resolves");
     session.board=new Board(4);session.board.cells[0]=session.board.cells[1]=2;Call("Refresh",true);
     Call("Move",Direction.Right);Call("QueueMove",Direction.Down);Call("OnApplicationPause",true);
     Check(!Get<Direction?>("queuedMove").HasValue && !Get<bool>("animating"),"backgrounding discards buffered input and resolves visual tiles");
     Call("OnApplicationPause",false);Click("Resume");Call("ShowHome");
     Debug.Log("PREMIUM_SMOKE_SUCCESS");Finish(0);
     break;
   }
  } catch (Exception e) {
   Debug.LogError("PREMIUM_SMOKE_FAILED: " + e);
   Finish(1);
  }
 }

 static void Finish(int code) { Application.Quit(code); }
}
#endif
}
