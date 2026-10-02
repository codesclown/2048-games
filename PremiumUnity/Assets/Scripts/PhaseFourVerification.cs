#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048 {
public static class PhaseFourVerification {
 const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
 static int checks;
 static void Check(bool condition, string message) {
  checks++;
  if (!condition) throw new Exception("Phase 4: " + message);
  Debug.Log("PHASE4 PASS: " + message);
 }

 sealed class MemStore : IPreferenceStore {
  public readonly Dictionary<string, string> dict = new Dictionary<string, string>();
  public string Read(string key, string fallback = "") { return dict.TryGetValue(key, out var v) ? v : fallback; }
  public int ReadInt(string key, int fallback) { return int.TryParse(Read(key), out var v) ? v : fallback; }
  public void Write(string key, string value) { dict[key] = value; }
  public void Delete(string key) { dict.Remove(key); }
  public void Flush() {}
 }

 static GameSave CreateSave(bool haptics) {
  var session = new Session(4, new System.Random(1));
  return new GameSave {
   session = session,
   stats = new Stats { played = 1, best = new long[] { 0, 0, 0 } },
   haptics = haptics
  };
 }

 public static void Run(PremiumGame game, GameManager manager) {
  checks = 0;
  HapticsAndAudioPreferenceChecks(manager);
  GhostPoolingChecks(game);
  AnimationGateAndInterruptionChecks(game, manager);
  ReducedMotionIntegrityChecks(game, manager);
  Debug.Log("PHASE4_SERVICE_SUCCESS: " + checks + " checks");
 }

 static void HapticsAndAudioPreferenceChecks(GameManager manager) {
  bool origHaptics = manager.Haptics;
  manager.ToggleHaptics();
  Check(manager.Haptics != origHaptics, "toggle haptics alters manager state");

  // Restore manager
  if (manager.Haptics != origHaptics) {
   manager.ToggleHaptics();
  }

  // Verify persistence serialization of haptics in schema v2
  var mem = new MemStore();
  var store = new GamePersistence("haptics-test", mem);
  store.Load();
  var save = CreateSave(false);
  store.Save(save);
  var reloaded = new GamePersistence("haptics-test", mem).Load();
  Check(reloaded.haptics == false, "haptic preference persisted in GameSave schema v2 as false");

  var saveTrue = CreateSave(true);
  store.Save(saveTrue);
  reloaded = new GamePersistence("haptics-test", mem).Load();
  Check(reloaded.haptics == true, "haptic preference persisted in GameSave schema v2 as true");

  // Verify PremiumHaptics triggers without exception
  foreach (HapticImpact impact in Enum.GetValues(typeof(HapticImpact))) {
   bool threw = false;
   try { PremiumHaptics.Trigger(impact); }
   catch (Exception) { threw = true; }
   Check(!threw, "PremiumHaptics.Trigger runs cleanly for " + impact);
  }

  // Check localization for Haptic feedback
  Check(GameLocalization.Has("Haptic feedback"), "strings catalog contains 'Haptic feedback'");
  Check(GameLocalization.Translate("es", "Haptic feedback") == "Vibración háptica", "spanish translation for 'Haptic feedback' matches");
 }

 static void GhostPoolingChecks(PremiumGame game) {
  var pool = (List<RectTransform>)game.GetType().GetField("ghostPool", Flags).GetValue(game);
  var active = (List<RectTransform>)game.GetType().GetField("activeGhosts", Flags).GetValue(game);
  var grid = (RectTransform)game.GetType().GetField("grid", Flags).GetValue(game);
  var acquire = game.GetType().GetMethod("AcquireGhost", Flags);
  var clear = game.GetType().GetMethod("ClearGhosts", Flags);

  clear.Invoke(game, null);
  pool.Clear();

  // Acquire 4 ghosts
  var g1 = (RectTransform)acquire.Invoke(game, new object[] { grid, 0, 2 });
  var g2 = (RectTransform)acquire.Invoke(game, new object[] { grid, 1, 4 });
  var g3 = (RectTransform)acquire.Invoke(game, new object[] { grid, 2, 8 });
  var g4 = (RectTransform)acquire.Invoke(game, new object[] { grid, 3, 16 });

  Check(active.Count == 4, "4 active ghosts registered");
  Check(g1.gameObject.activeSelf && g4.gameObject.activeSelf, "acquired ghosts are active in hierarchy");

  // Recycle ghosts into pool
  clear.Invoke(game, null);
  Check(active.Count == 0, "active ghosts list cleared after recycle");
  Check(pool.Count == 4, "4 ghosts returned to pool");
  Check(!g1.gameObject.activeSelf && !g4.gameObject.activeSelf, "pooled ghosts deactivated in hierarchy");

  // Re-acquire from pool (must reuse instance, not create new)
  var reused = (RectTransform)acquire.Invoke(game, new object[] { grid, 0, 32 });
  Check(reused == g4, "pooled ghost reused LIFO without reallocating");
  Check(reused.gameObject.activeSelf, "reused ghost reactivated");

  clear.Invoke(game, null);
 }

 static void AnimationGateAndInterruptionChecks(PremiumGame game, GameManager manager) {
  var finish = game.GetType().GetMethod("FinishAnimation", Flags);
  var grid = (RectTransform)game.GetType().GetField("grid", Flags).GetValue(game);

  // Simulate interrupted state
  grid.localScale = Vector3.one * 0.9f;
  finish.Invoke(game, null);

  bool animating = (bool)game.GetType().GetField("animating", Flags).GetValue(game);
  Check(!animating, "FinishAnimation clears animating flag");
  Check(grid.localScale == Vector3.one, "FinishAnimation restores grid scale to 1.0");

  var active = (List<RectTransform>)game.GetType().GetField("activeGhosts", Flags).GetValue(game);
  Check(active.Count == 0, "FinishAnimation clears all active ghost tiles");
 }

 static void ReducedMotionIntegrityChecks(PremiumGame game, GameManager manager) {
  bool orig = manager.ReducedMotion;
  if (!manager.ReducedMotion) manager.ToggleReducedMotion();

  manager.StartGame(4);
  manager.Session.board = new Board(4);
  manager.Session.board.cells[0] = 2;
  manager.Session.board.cells[1] = 2;

  game.GetType().GetMethod("Refresh", Flags).Invoke(game, new object[] { true });

  // Move under reduced motion should resolve immediately without running coroutine
  bool moved = manager.Move(Direction.Right, 10.0);
  Check(moved, "move succeeds under reduced motion");

  bool animating = (bool)game.GetType().GetField("animating", Flags).GetValue(game);
  Check(!animating, "reduced motion executes move without lingering animation state");

  // Restore
  if (manager.ReducedMotion != orig) manager.ToggleReducedMotion();
 }
}
}
#endif
