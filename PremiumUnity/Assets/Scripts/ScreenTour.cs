using System.Collections;
using System.IO;
using UnityEngine;
using Premium2048;

namespace Premium2048 {
public class ScreenTour : MonoBehaviour {
 static ScreenTour instance;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Init() {
  if (instance == null) {
   var go = new GameObject("ScreenTour");
   instance = go.AddComponent<ScreenTour>();
   DontDestroyOnLoad(go);
  }
 }

 void Start() {
  StartCoroutine(TourRoutine());
 }

 IEnumerator Capture(string filename) {
  yield return new WaitForEndOfFrame();
  yield return new WaitForEndOfFrame();
  var tex = ScreenCapture.CaptureScreenshotAsTexture();
  if (tex != null) {
   byte[] bytes = tex.EncodeToPNG();
   string dir = "/Users/chandanvishwakarma/.gemini/antigravity-ide/brain/fb08918b-7d1a-4f64-b389-488e4927443e";
   File.WriteAllBytes(Path.Combine(dir, filename), bytes);
   Destroy(tex);
   Debug.Log("[ScreenTour] Captured: " + filename);
  }
 }

 IEnumerator TourRoutine() {
  var manager = GameManager.Instance;
  if (manager == null) yield break;

  yield return new WaitForSeconds(0.6f);

  // 1. Home
  manager.Home();
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_01_home.png"));

  // 2. Gameplay 4x4
  manager.StartGame(4);
  if (manager.Session != null && manager.Session.board != null) {
   manager.Session.board.cells[0] = 2;
   manager.Session.board.cells[1] = 4;
   manager.Session.board.cells[2] = 8;
   manager.Session.board.cells[3] = 16;
   manager.Session.board.cells[4] = 32;
   manager.Session.board.cells[5] = 64;
   manager.Session.board.cells[6] = 128;
   manager.Session.board.cells[7] = 256;
   manager.Session.board.cells[8] = 512;
   manager.Session.board.cells[9] = 1024;
   manager.Session.board.cells[10] = 2048;
   manager.Session.board.cells[11] = 4096;
   manager.Session.board.score = 28640;
  }
  manager.Navigation.Play();
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_02_gameplay_4x4.png"));

  // 3. Gameplay 5x5
  manager.StartGame(5);
  if (manager.Session != null && manager.Session.board != null) {
   manager.Session.board.cells[0] = 4;
   manager.Session.board.cells[1] = 8;
   manager.Session.board.cells[2] = 16;
   manager.Session.board.cells[3] = 32;
   manager.Session.board.cells[4] = 64;
   manager.Session.board.cells[5] = 128;
   manager.Session.board.cells[6] = 256;
   manager.Session.board.cells[7] = 512;
   manager.Session.board.score = 14200;
  }
  manager.Navigation.Play();
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_03_gameplay_5x5.png"));

  // 4. Gameplay 6x6
  manager.StartGame(6);
  if (manager.Session != null && manager.Session.board != null) {
   manager.Session.board.cells[0] = 8;
   manager.Session.board.cells[1] = 16;
   manager.Session.board.cells[2] = 32;
   manager.Session.board.cells[3] = 64;
   manager.Session.board.cells[4] = 128;
   manager.Session.board.score = 8600;
  }
  manager.Navigation.Play();
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_04_gameplay_6x6.png"));

  // 5. Levels Dialog
  manager.StartGame(4);
  manager.Navigation.Open(GameDialog.Levels);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_05_levels.png"));

  // 6. Scores Dialog
  manager.Navigation.Back();
  manager.Navigation.Open(GameDialog.Scores);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_06_scores.png"));

  // 7. Instructions Dialog
  manager.Navigation.Back();
  manager.Navigation.Open(GameDialog.Instructions);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_07_instructions.png"));

  // 8. Settings Dialog
  manager.Navigation.Back();
  manager.Navigation.Open(GameDialog.Settings);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_08_settings.png"));

  // 9. Pause Dialog
  manager.Navigation.Back();
  manager.Navigation.Open(GameDialog.Pause);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_09_pause.png"));

  // 10. Game Over Dialog
  manager.Navigation.Back();
  if (manager.Session != null && manager.Session.board != null) {
   manager.Session.board.cells[0] = 512;
   manager.Session.board.score = 6480;
  }
  manager.Navigation.Open(GameDialog.Loss);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_10_gameover.png"));

  // 11. Victory Dialog
  manager.Navigation.Back();
  if (manager.Session != null && manager.Session.board != null) {
   manager.Session.board.cells[0] = 2048;
   manager.Session.board.score = 24800;
  }
  manager.Navigation.Open(GameDialog.Win);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_11_victory.png"));

  // 12. Restart Confirmation
  manager.Navigation.Back();
  manager.Navigation.Open(GameDialog.Restart);
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_12_restart.png"));

  // 13. Back to home with session
  manager.Navigation.Back();
  manager.Home();
  yield return new WaitForSeconds(0.3f);
  yield return StartCoroutine(Capture("tour_13_home_resumable.png"));

  Debug.Log("[ScreenTour] COMPLETED CAPTURE OF ALL 13 SCREENS!");
 }
}
}
