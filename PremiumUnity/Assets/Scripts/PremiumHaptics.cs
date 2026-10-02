using UnityEngine;

namespace Premium2048 {
public enum HapticImpact { Light, Medium, Heavy, Success, Warning, Error }

public static class PremiumHaptics {
 public static void Trigger(HapticImpact impact) {
  if (GameManager.Instance != null && (!GameManager.Instance.Haptics || GameManager.Instance.ReducedMotion)) return;
#if UNITY_IOS || UNITY_ANDROID
  try {
   if (impact == HapticImpact.Light || impact == HapticImpact.Medium) {
    Handheld.Vibrate();
   } else if (impact == HapticImpact.Heavy || impact == HapticImpact.Success) {
    Handheld.Vibrate();
   }
  } catch (System.Exception) {}
#endif
 }
}
}
