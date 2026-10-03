using UnityEditor;
using UnityEngine;
public static class ConfigureAdMobTests {
 public static void Run(){
  var assembly=System.Reflection.Assembly.Load("GoogleMobileAds.Editor");
  var type=assembly.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");
  var settings=(ScriptableObject)type.GetMethod("LoadInstance",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
  var serialized=new SerializedObject(settings);
  serialized.FindProperty("adMobAndroidAppId").stringValue="ca-app-pub-3940256099942544~3347511713";
  serialized.FindProperty("adMobIOSAppId").stringValue="ca-app-pub-3940256099942544~1458002511";
  serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
  Premium2048.Editor.PremiumUiVerification.Run();
  Premium2048.EditorTools.SceneChecker.RebuildAndVerify();
  Debug.Log("PREMIUM_ADMOB_SETUP_SUCCESS");
 }
}
