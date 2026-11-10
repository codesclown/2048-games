using System;
using UnityEngine;
namespace Premium2048 {
// "Rate app" and "Share app" actions from Settings. Android uses native intents;
// other platforms fall back to the web listing / clipboard.
public static class PremiumStoreActions {
 public static string StoreWebUrl {get{return "https://play.google.com/store/apps/details?id="+Application.identifier;}}

 public static void RateApp() {
#if UNITY_ANDROID && !UNITY_EDITOR
  try {
   using(var uriClass=new AndroidJavaClass("android.net.Uri"))
   using(var uri=uriClass.CallStatic<AndroidJavaObject>("parse","market://details?id="+Application.identifier))
   using(var intent=new AndroidJavaObject("android.content.Intent","android.intent.action.VIEW",uri))
   using(var activity=CurrentActivity()) {
    // NO_HISTORY | NEW_DOCUMENT | MULTIPLE_TASK: back from the Play Store returns to the game.
    intent.Call<AndroidJavaObject>("addFlags",0x40000000|0x00080000|0x08000000)?.Dispose();
    activity.Call("startActivity",intent);
    return;
   }
  } catch(Exception e){Debug.LogWarning("Play Store app unavailable, opening web listing: "+e.Message);}
#endif
  Application.OpenURL(StoreWebUrl);
 }

 // Opens the system share sheet. The receiving app (WhatsApp, Gmail, ...) lets the player edit the text before sending.
 public static void ShareApp(string subject,string message,string chooserTitle) {
  string text=message+"\n"+StoreWebUrl;
#if UNITY_ANDROID && !UNITY_EDITOR
  try {
   using(var intentClass=new AndroidJavaClass("android.content.Intent"))
   using(var intent=new AndroidJavaObject("android.content.Intent"))
   using(var activity=CurrentActivity()) {
    intent.Call<AndroidJavaObject>("setAction",intentClass.GetStatic<string>("ACTION_SEND"))?.Dispose();
    intent.Call<AndroidJavaObject>("setType","text/plain")?.Dispose();
    intent.Call<AndroidJavaObject>("putExtra",intentClass.GetStatic<string>("EXTRA_SUBJECT"),subject)?.Dispose();
    intent.Call<AndroidJavaObject>("putExtra",intentClass.GetStatic<string>("EXTRA_TEXT"),text)?.Dispose();
    using(var chooser=intentClass.CallStatic<AndroidJavaObject>("createChooser",intent,chooserTitle))
     activity.Call("startActivity",chooser);
    return;
   }
  } catch(Exception e){Debug.LogWarning("Share sheet unavailable, copying link instead: "+e.Message);}
#endif
  try{GUIUtility.systemCopyBuffer=text;}catch(Exception){}
  Debug.Log("Share text copied to clipboard:\n"+text);
 }

#if UNITY_ANDROID && !UNITY_EDITOR
 static AndroidJavaObject CurrentActivity() {
  using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
   return player.GetStatic<AndroidJavaObject>("currentActivity");
 }
#endif
}
}
