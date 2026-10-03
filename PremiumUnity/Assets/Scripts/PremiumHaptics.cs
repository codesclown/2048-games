using UnityEngine;

namespace Premium2048 {
public enum HapticImpact { Light, Medium, Heavy, Success, Warning, Error }

public static class PremiumHaptics {
#if UNITY_ANDROID && !UNITY_EDITOR
 static AndroidJavaObject vibrator;
 static int sdkInt = -1;
 static bool initialized;

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
  } catch (System.Exception) {}
 }
#endif

 public static void Trigger(HapticImpact impact) {
  if (GameManager.Instance != null && (!GameManager.Instance.Haptics || GameManager.Instance.ReducedMotion)) return;
#if UNITY_ANDROID && !UNITY_EDITOR
  try {
   InitAndroid();
   if (vibrator != null && vibrator.Call<bool>("hasVibrator")) {
    long ms = 12;
    int amp = 40;
    switch (impact) {
     case HapticImpact.Light: ms = 12; amp = 35; break;
     case HapticImpact.Medium: ms = 22; amp = 75; break;
     case HapticImpact.Heavy: ms = 40; amp = 140; break;
     case HapticImpact.Success: ms = 50; amp = 160; break;
     case HapticImpact.Warning: case HapticImpact.Error: ms = 30; amp = 90; break;
    }
    if (sdkInt >= 26) {
     using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
     using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp)) {
      vibrator.Call("vibrate", effect);
     }
    } else {
     vibrator.Call("vibrate", ms);
    }
    return;
   }
  } catch (System.Exception) {}
#elif UNITY_IOS && !UNITY_EDITOR
  try {
   if (impact == HapticImpact.Heavy || impact == HapticImpact.Success) {
    Handheld.Vibrate();
   }
  } catch (System.Exception) {}
#endif
 }
}
}
