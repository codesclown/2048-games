#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048 {
public static class PhaseThreeVerification {
 static int checks;
 static void Check(bool condition, string message) {
  checks++;
  if (!condition) throw new Exception("Phase 3: " + message);
  Debug.Log("PHASE3 PASS: " + message);
 }

 public static void Run(PremiumGame game) {
  checks = 0;
  DeviceProfileAndAspectRatioChecks();
  SafeAreaAndAnchorChecks();
  TouchTargetComplianceChecks(game);
  BoardGeometryAndSquarenessChecks();
  ResponsiveScaleMatrixChecks();
  Debug.Log("PHASE3_SERVICE_SUCCESS: " + checks + " checks");
 }

 static void DeviceProfileAndAspectRatioChecks() {
  // Tablet ratios (< 1.70)
  Check(PremiumLayout.GetProfile(1536, 2048, new Rect(0, 0, 1536, 2048)) == DeviceProfile.Tablet, "iPad 4:3 classified as Tablet");
  Check(PremiumLayout.GetProfile(1200, 1920, new Rect(0, 0, 1200, 1920)) == DeviceProfile.Tablet, "Tablet 16:10 classified as Tablet");
  Check(PremiumLayout.GetProfile(1440, 2160, new Rect(0, 0, 1440, 2160)) == DeviceProfile.Tablet, "Tablet 3:2 classified as Tablet");

  // Compact Phone ratios (1.70 <= aspect < 1.95)
  Check(PremiumLayout.GetProfile(1080, 1920, new Rect(0, 0, 1080, 1920)) == DeviceProfile.CompactPhone, "Compact 16:9 (1080x1920) classified as CompactPhone");
  Check(PremiumLayout.GetProfile(720, 1280, new Rect(0, 0, 720, 1280)) == DeviceProfile.CompactPhone, "Compact 16:9 (720x1280) classified as CompactPhone");
  Check(PremiumLayout.GetProfile(1080, 1980, new Rect(0, 0, 1080, 1980)) == DeviceProfile.CompactPhone, "18:10 classified as CompactPhone");

  // Tall Phone ratios (>= 1.95)
  Check(PremiumLayout.GetProfile(1179, 2556, new Rect(0, 0, 1179, 2556)) == DeviceProfile.TallPhone, "iPhone 15 Pro 19.5:9 classified as TallPhone");
  Check(PremiumLayout.GetProfile(1080, 2400, new Rect(0, 0, 1080, 2400)) == DeviceProfile.TallPhone, "Android 20:9 (1080x2400) classified as TallPhone");
  Check(PremiumLayout.GetProfile(1080, 2520, new Rect(0, 0, 1080, 2520)) == DeviceProfile.TallPhone, "Sony Xperia 21:9 classified as TallPhone");
 }

 static void SafeAreaAndAnchorChecks() {
  // Screen with simulated notch at top (132px) and home bar at bottom (102px)
  int w = 1179, h = 2556;
  Rect safeWithNotch = new Rect(0, 102, 1179, 2322);

  Rect anchors = PremiumLayout.CalculateSafeAnchors(w, h, safeWithNotch);
  Check(anchors.xMin == 0f, "safe anchor xMin is 0");
  Check(anchors.xMax == 1f, "safe anchor xMax is 1");
  Check(anchors.yMin > 0f && anchors.yMin < 0.1f, "safe anchor yMin isolates bottom home bar: " + anchors.yMin);
  Check(anchors.yMax < 1f && anchors.yMax > 0.9f, "safe anchor yMax isolates top notch: " + anchors.yMax);

  float scale = PremiumLayout.CalculateScale(w, h, safeWithNotch, new Vector2(600, 960));
  Check(scale > 0f && scale <= (safeWithNotch.width / 600f) + 0.001f, "scale factor respects safe area width bound");
  Check(scale <= (safeWithNotch.height / 960f) + 0.001f, "scale factor respects safe area height bound");

  // Zero/invalid fallback
  Rect fallbackAnchors = PremiumLayout.CalculateSafeAnchors(0, 0, Rect.zero);
  Check(fallbackAnchors == new Rect(0, 0, 1, 1), "zero dimension fallback returns full rect");
 }

 static void TouchTargetComplianceChecks(PremiumGame game) {
  // Validate all interactive Buttons currently loaded in scene
  var buttons = game.GetComponentsInChildren<Button>(true);
  Check(buttons.Length > 0, "interactive buttons exist in scene (" + buttons.Length + " buttons)");

  foreach (var btn in buttons) {
   var rt = btn.GetComponent<RectTransform>();
   if (rt != null) {
    Vector2 size = rt.rect.size;
    if (size == Vector2.zero) size = rt.sizeDelta;
    if (size.x > 0 && size.y > 0) {
     Check(size.x >= PremiumLayout.MinTouchTarget && size.y >= PremiumLayout.MinTouchTarget,
      "touch target >= " + PremiumLayout.MinTouchTarget + "x" + PremiumLayout.MinTouchTarget + " for button '" + btn.gameObject.name + "' (" + size.x + "x" + size.y + ")");
    }
   }
  }
 }

 static void BoardGeometryAndSquarenessChecks() {
  for (int size = 4; size <= 6; size++) {
   float cellSize = PremiumLayout.CalculateCellSize(size, 496f, 10f);
   Check(cellSize > 0, "calculated valid positive cell size for " + size + "x" + size + ": " + cellSize);

   float expectedTotal = size * cellSize + (size - 1) * 10f;
   Check(Mathf.Approximately(expectedTotal, 496f), "board cells + spacing sum exactly to 496 for " + size + "x" + size);

   for (int i = 0; i < size * size; i++) {
    Vector2 pos = PremiumLayout.CalculateCellPosition(i, size, cellSize, 10f);
    Check(Mathf.Abs(pos.x) <= 248f && Mathf.Abs(pos.y) <= 248f, "cell " + i + " strictly contained within board bounds for " + size + "x" + size);
   }
  }
 }

 static void ResponsiveScaleMatrixChecks() {
  (int w, int h, Rect safe, string name)[] testCases = {
   (1080, 1920, new Rect(0, 0, 1080, 1920), "Compact Phone 16:9"),
   (1179, 2556, new Rect(0, 102, 1179, 2322), "Tall Phone iPhone 15 Pro"),
   (1080, 2400, new Rect(0, 80, 1080, 2240), "Tall Phone Android 20:9"),
   (1536, 2048, new Rect(0, 40, 1536, 1968), "Tablet iPad 4:3"),
   (1200, 1920, new Rect(0, 60, 1200, 1800), "Tablet Android 16:10")
  };

  foreach (var tc in testCases) {
   float scale = PremiumLayout.CalculateScale(tc.w, tc.h, tc.safe, new Vector2(600, 960));
   float renderedW = 600f * scale;
   float renderedH = 960f * scale;
   Check(renderedW <= tc.safe.width + 0.01f, tc.name + ": rendered width " + renderedW + " <= safe width " + tc.safe.width);
   Check(renderedH <= tc.safe.height + 0.01f, tc.name + ": rendered height " + renderedH + " <= safe height " + tc.safe.height);
  }
 }
}
}
#endif
