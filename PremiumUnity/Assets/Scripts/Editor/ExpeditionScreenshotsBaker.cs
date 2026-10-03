#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Premium2048.Editor {
public static class ExpeditionScreenshotsBaker {
 [MenuItem("Tools/2048/Capture Expedition Preview Screens")]
 public static void CapturePreviews() {
  string folder = "Docs/Previews/Expedition";
  Directory.CreateDirectory(folder);

  var game = Object.FindFirstObjectByType<PremiumGame>();
  if (game == null) {
   Debug.LogWarning("Please enter Play Mode in Unity to capture live screens, or open 01_HomeScreen scene.");
   return;
  }

  var manager = GameManager.Instance;
  if (manager == null) return;

  // 1. Capture Home Screen
  manager.Home();
  ScreenCapture.CaptureScreenshot(Path.Combine(folder, "01_HomeScreen_Updated.png"));

  // 2. Capture Choose Board Dialog
  manager.Navigation.Open(GameDialog.Levels);
  ScreenCapture.CaptureScreenshot(Path.Combine(folder, "02_ChooseYourBoard_Modal.png"));

  // 3. Capture Expedition Map
  manager.ExpeditionMap();
  ScreenCapture.CaptureScreenshot(Path.Combine(folder, "03_Expedition_JourneyMap.png"));

  // 4. Capture Mission 01 Gameplay
  manager.StartMission(ExpeditionCatalog.Get("expedition-01"));
  ScreenCapture.CaptureScreenshot(Path.Combine(folder, "04_Expedition_Mission01_Board.png"));

  // 5. Capture Daily Puzzle
  manager.StartDailyPuzzle(System.DateTime.UtcNow);
  ScreenCapture.CaptureScreenshot(Path.Combine(folder, "05_Daily_Puzzle_Board.png"));

  manager.Home();
  AssetDatabase.Refresh();
  Debug.Log("Captured all Expedition screenshots to: " + folder);
 }
}
}
#endif
