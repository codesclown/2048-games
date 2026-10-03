#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048 {
public static class PhaseTwoVerification {
 static int checks;
 static void Check(bool condition, string message) {
  checks++;
  if (!condition) throw new Exception("Phase 2: " + message);
  Debug.Log("PHASE2 PASS: " + message);
 }

 public static void Run(GameManager manager) {
  checks = 0;
  LocalizationChecks();
  TypographyAndBrandChecks();
  ThemeAndContrastChecks();
  IconAndControlChecks(manager);
  AccessibilityModeChecks(manager);
  PlatformAssetChecks();
  Debug.Log("PHASE2_SERVICE_SUCCESS: " + checks + " checks");
 }

 static void LocalizationChecks() {
  string[] requiredKeys = {
   "Preparing premium interface", "Loading scores and saved game", "Preparing controls and audio", "Ready",
   "THE NUMBER COLLECTION", "A little focus. A better move.", "One swipe can change everything", "New Game", "Resume",
   "How to Play", "High Scores", "Settings", "SWIPE · MERGE · MASTER", "SCORE", "BEST",
   "Undo", "Swap", "Delete", "SELECTED", "LOCKED", "READY", "EMPTY",
   "Hold 128 / 256 / 512 to earn tools", "Tools unlock after {0} more moves",
   "Tap two different adjacent tiles", "Tap the tile to remove · Cancel to exit",
   "Tile 1 selected · Tap an adjacent tile", "Choose a different tile next to tile 1", "Cancel selection",
   "Paused", "Take a breath. Your board is saved.", "Change level", "Home",
   "Choose your pace", "Classic", "Short and focused", "Advanced", "Room to build", "Expert", "Think a few moves ahead", "CURRENT BOARD",
   "Simple moves. Thoughtful choices.", "Your personal bests, saved on this device",
   "Played {0}  ·  Won {1}  ·  Win rate {2}%", "Clear scores", "Clear saved scores?", "Clear", "Cancel",
   "Restart level?", "Restart", "You win!", "A new milestone. Keep building.", "TARGET REACHED", "Keep going", "Play again",
   "Game over", "No moves remain. A fresh challenge awaits.", "FINAL SCORE", "Continue: rewind 5 moves",
   "Continue playing?", "Continue", "No thanks", "Close", "Sound", "Reduced motion", "Large text", "High contrast", "ON", "OFF"
  };

  foreach (var key in requiredKeys) {
   Check(GameLocalization.Has(key), "localization catalog contains key: " + key);
   string es = GameLocalization.Translate("es", key);
   Check(!string.IsNullOrEmpty(es) && es != key, "spanish translation exists and is distinct: " + key);
  }

  string formatted = GameLocalization.Translate("es", "Level {0}  /  {1} × {1}  /  Target {2}", 1, 4, 2048);
  Check(formatted.Contains("Nivel 1") && formatted.Contains("4 × 4") && formatted.Contains("2048"), "localized string formats arguments correctly");

  string fallback = GameLocalization.Translate("es", "NonExistentKey12345");
  Check(fallback == "NonExistentKey12345", "missing localization falls back to english key");
 }

 static void TypographyAndBrandChecks() {
  var regular = Resources.Load<Font>("Fonts/NotoSans-Regular");
  var bold = Resources.Load<Font>("Fonts/NotoSans-Bold");
  Check(regular != null, "NotoSans-Regular font bundled in Resources/Fonts");
  Check(bold != null, "NotoSans-Bold font bundled in Resources/Fonts");

  var mark = Resources.Load<Sprite>("Brand/Mark");
  var markTex = Resources.Load<Texture2D>("Brand/Mark");
  var appIcon = Resources.Load<Texture2D>("Brand/AppIcon");
  var launch = Resources.Load<Texture2D>("Brand/LaunchPortrait");
  Check(mark != null || markTex != null, "brand mark sprite/texture bundled in Resources/Brand");
  Check(appIcon != null && appIcon.width >= 512, "brand app icon bundled in Resources/Brand (>= 512px)");
  Check(launch != null && launch.height >= 512, "launch portrait art bundled in Resources/Brand");
 }

 static void ThemeAndContrastChecks() {
  int[] tileValues = { 0, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
  foreach (int val in tileValues) {
   PremiumTheme.TileColors(val, out var c1, out var c2);
   Color text = PremiumTheme.TileText(val, false);
   float contrast1 = PremiumTheme.Contrast(text, c1);
   float contrast2 = PremiumTheme.Contrast(text, c2);
   Check(Mathf.Min(contrast1, contrast2) >= 4.5f, "WCAG AA contrast (>= 4.5) met for tile " + val + " (contrast: " + Mathf.Min(contrast1, contrast2).ToString("F2") + ")");

   Color highContrastText = PremiumTheme.TileText(val, true);
   Check(Mathf.Min(PremiumTheme.Contrast(highContrastText,c1),PremiumTheme.Contrast(highContrastText,c2)) >= 4.5f, "high contrast label stays readable on the distinct colour for tile " + val);
  }

  Check(PremiumTheme.Contrast(PremiumTheme.Text, PremiumTheme.Ink) >= 7.0f, "ink to text contrast exceeds WCAG AAA (>= 7.0)");
  Check(PremiumTheme.Contrast(PremiumTheme.Ink, PremiumTheme.Accent) >= 4.5f, "accent to ink contrast meets WCAG AA");
 }

 static void IconAndControlChecks(GameManager manager) {
  var go = new GameObject("IconTest", typeof(RectTransform));
  var icon = go.AddComponent<PremiumIcon>();
  var vh = new VertexHelper();

  foreach (IconKind kind in Enum.GetValues(typeof(IconKind))) {
   icon.Kind = kind;
   vh.Clear();
   var method = typeof(PremiumIcon).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null);
   method.Invoke(icon, new object[] { vh });
   Check(vh.currentVertCount > 0, "vector icon renders mesh vertices: " + kind + " (" + vh.currentVertCount + " verts)");
  }

  var frameGo = new GameObject("FrameTest", typeof(RectTransform));
  var frame = frameGo.AddComponent<PremiumSelectionFrame>();
  vh.Clear();
  var frameMethod = typeof(PremiumSelectionFrame).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null);
  frameMethod.Invoke(frame, new object[] { vh });
  Check(vh.currentVertCount == 16, "selection frame generates 16 outline vertices");

  var btnGo = new GameObject("ButtonTest", typeof(RectTransform), typeof(Image), typeof(Button));
  var ctrl = btnGo.AddComponent<PremiumControl>();
  ctrl.Initialize(manager, ControlTone.Primary, "Test Primary");
  ctrl.Apply();
  Check(btnGo.GetComponent<Image>().color == PremiumTheme.Accent, "primary control applies accent background");

  ctrl.Initialize(manager, ControlTone.Destructive, "Test Destructive");
  ctrl.Apply();
  Check(btnGo.GetComponent<Image>().color == PremiumTheme.Color("713A48"), "destructive control applies destructive tone");

  ctrl.Initialize(manager, ControlTone.Secondary, "Test Secondary");
  ctrl.State(selected: true);
  Check(btnGo.GetComponent<Image>().color == PremiumTheme.Accent, "selected state applies accent background");

  ctrl.State(locked: true);
  Check(btnGo.GetComponent<Image>().color == PremiumTheme.Color("1C2C43"), "locked state applies muted tone");

  GameObject.DestroyImmediate(go);
  GameObject.DestroyImmediate(frameGo);
  GameObject.DestroyImmediate(btnGo);
 }

 static void AccessibilityModeChecks(GameManager manager) {
  bool origLarge = manager.LargeText;
  bool origReduced = manager.ReducedMotion;
  bool origContrast = manager.HighContrast;
  string origLang = manager.Language;

  manager.ToggleLargeText();
  Check(manager.LargeText != origLarge, "toggle large text modifies manager state");

  manager.ToggleReducedMotion();
  Check(manager.ReducedMotion != origReduced, "toggle reduced motion modifies manager state");

  manager.ToggleHighContrast();
  Check(manager.HighContrast != origContrast, "toggle high contrast modifies manager state");

  manager.ToggleLanguage();
  Check(manager.Language == (origLang == "en" ? "es" : "en"), "toggle language switches between en and es");

  // Restore
  if (manager.LargeText != origLarge) manager.ToggleLargeText();
  if (manager.ReducedMotion != origReduced) manager.ToggleReducedMotion();
  if (manager.HighContrast != origContrast) manager.ToggleHighContrast();
  if (manager.Language != origLang) manager.ToggleLanguage();
 }

 static void PlatformAssetChecks() {
  string root = System.IO.Directory.Exists("Assets/Art") ? "." : "/Users/chandanvishwakarma/2048-games/PremiumUnity";
  string androidDir = System.IO.Path.Combine(root, "Assets/Art/Platform/Android");
  Check(System.IO.File.Exists(System.IO.Path.Combine(androidDir, "AdaptiveBackground.png")), "Android adaptive background asset exists");
  Check(System.IO.File.Exists(System.IO.Path.Combine(androidDir, "AdaptiveForeground.png")), "Android adaptive foreground asset exists");
  Check(System.IO.File.Exists(System.IO.Path.Combine(androidDir, "Monochrome.png")), "Android monochrome icon asset exists");

  string iosDir = System.IO.Path.Combine(root, "Assets/Art/Platform/iOS");
  Check(System.IO.File.Exists(System.IO.Path.Combine(iosDir, "AppIcon-1024.png")), "iOS 1024x1024 app store icon asset exists");
  Check(System.IO.File.Exists(System.IO.Path.Combine(iosDir, "AppIcon.appiconset/Contents.json")), "iOS AppIcon.appiconset catalog exists");

  string[] iosSizes = { "icon-20.png", "icon-29.png", "icon-40.png", "icon-58.png", "icon-60.png", "icon-76.png", "icon-80.png", "icon-87.png", "icon-120.png", "icon-152.png", "icon-167.png", "icon-180.png", "icon-1024.png" };
  foreach (var size in iosSizes) {
   Check(System.IO.File.Exists(System.IO.Path.Combine(iosDir, "AppIcon.appiconset/" + size)), "iOS icon asset present: " + size);
  }
 }
}
}
#endif
