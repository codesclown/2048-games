using UnityEngine;

namespace Premium2048 {
public enum HapticImpact { Light, Medium, Heavy, Success, Warning, Error }

public static class PremiumHaptics {
#if UNITY_ANDROID && !UNITY_EDITOR
 static AndroidJavaObject vibrator;
 static int sdkInt = -1;
 static bool initialized;
 static bool hasAmplitude;

 static void InitAndroid() {
  if (initialized) return;
  initialized = true;
  try {
   using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
   using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {
    vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
   }
   using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
    sdkInt = version.GetStatic<int>("SDK_INT");
   }
   if (vibrator != null && sdkInt >= 26) {
    try { hasAmplitude = vibrator.Call<bool>("hasAmplitudeControl"); } catch {}
   }
  } catch (System.Exception) {}
 }
#endif

 static float lastVibrateTime;

 public static void Trigger(HapticImpact impact) {
  if (GameManager.Instance != null && (!GameManager.Instance.Haptics || GameManager.Instance.ReducedMotion)) return;

  // Small throttle between consecutive vibrations so it feels like crisp distinct taps
  if (Time.unscaledTime - lastVibrateTime < 0.05f && impact == HapticImpact.Light) return;
  lastVibrateTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
  try {
   InitAndroid();
   if (vibrator != null && vibrator.Call<bool>("hasVibrator")) {
    long ms = 30;
    int amp = 160;
    switch (impact) {
     case HapticImpact.Light: ms = 28; amp = 140; break;
     case HapticImpact.Medium: ms = 45; amp = 190; break;
     case HapticImpact.Heavy: ms = 80; amp = 240; break;
     case HapticImpact.Success: ms = 95; amp = 255; break;
     case HapticImpact.Warning: case HapticImpact.Error: ms = 50; amp = 180; break;
    }
    if (sdkInt >= 26 && hasAmplitude) {
     using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
     using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp)) {
      vibrator.Call("vibrate", effect);
      return;
     }
    } else if (sdkInt >= 26) {
     using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
     using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, -1)) {
      vibrator.Call("vibrate", effect);
      return;
     }
    } else {
     vibrator.Call("vibrate", ms);
     return;
    }
   }
  } catch (System.Exception) {}
  Handheld.Vibrate();
#elif UNITY_IOS || UNITY_ANDROID
  try {
   Handheld.Vibrate();
  } catch (System.Exception) {}
#endif
 }
}
}
