using System;
using UnityEngine;

namespace Premium2048 {
public enum DeviceProfile { CompactPhone, TallPhone, Tablet }

public static class PremiumLayout {
 public const float MinTouchTarget = 72f;
 public const float ReferenceWidth = 540f;
 public const float ReferenceHeight = 960f;
 public const float BannerReserve = 90f;

 public static DeviceProfile GetProfile(float width, float height, Rect safeArea) {
  float safeW = safeArea.width > 0 ? safeArea.width : (width > 0 ? width : ReferenceWidth);
  float safeH = safeArea.height > 0 ? safeArea.height : (height > 0 ? height : ReferenceHeight);
  float aspect = safeH / Mathf.Max(1f, safeW);

  if (aspect < 1.70f) return DeviceProfile.Tablet;
  if (aspect >= 1.95f) return DeviceProfile.TallPhone;
  return DeviceProfile.CompactPhone;
 }

 public static float CalculateScale(int screenWidth, int screenHeight, Rect safeArea, Vector2 reference) {
  if (screenWidth <= 0 || screenHeight <= 0) return 1f;
  Rect safe = safeArea.width > 0 && safeArea.height > 0 ? safeArea : new Rect(0, 0, screenWidth, screenHeight);
  float scaleX = safe.width / Mathf.Max(1f, reference.x);
  float scaleY = safe.height / Mathf.Max(1f, reference.y);
  return Mathf.Max(0.01f, Mathf.Min(scaleX, scaleY));
 }

 public static Rect CalculateSafeAnchors(int screenWidth, int screenHeight, Rect safeArea) {
  if (screenWidth <= 0 || screenHeight <= 0) return new Rect(0, 0, 1, 1);
  Rect safe = safeArea.width > 0 && safeArea.height > 0 ? safeArea : new Rect(0, 0, screenWidth, screenHeight);
  return new Rect(
   safe.xMin / screenWidth,
   safe.yMin / screenHeight,
   safe.width / screenWidth,
   safe.height / screenHeight
  );
 }

 public static bool IsTouchTargetCompliant(Vector2 size) {
  return size.x >= MinTouchTarget && size.y >= MinTouchTarget;
 }

 public static float CalculateCellSize(int boardSize, float boardDimension, float spacing = 10f) {
  if (boardSize < 4 || boardSize > 6) throw new ArgumentOutOfRangeException(nameof(boardSize));
  return (boardDimension - (boardSize - 1) * spacing) / boardSize;
 }

 public static Vector2 CalculateCellPosition(int index, int boardSize, float cellSize, float spacing = 10f) {
  if (boardSize < 4 || boardSize > 6) throw new ArgumentOutOfRangeException(nameof(boardSize));
  float totalBoard = boardSize * cellSize + (boardSize - 1) * spacing;
  float step = cellSize + spacing;
  float edge = totalBoard * 0.5f - cellSize * 0.5f;
  return new Vector2(
   -edge + (index % boardSize) * step,
   edge - (index / boardSize) * step
  );
 }
}
}
