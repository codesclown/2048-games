using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildVerifier {
 [MenuItem("2048 Premium/Build Android App Bundle (.aab)")]
 public static void BuildAndroidAab(){BuildAndroid(false,true);}
 [MenuItem("2048 Premium/Build Android Release APK")]
 public static void BuildAndroidPreview(){BuildAndroid(false,false);}
 [MenuItem("2048 Premium/Build Android Dev APK")]
 public static void BuildAndroidDevelopment(){BuildAndroid(true,false);}
 public static void BuildNativeTargets(){BuildMac();BuildAndroidDevelopment();}
 static void BuildAndroid(bool development, bool aab=false) {
  ProjectSetup.Configure();var target=UnityEditor.Build.NamedBuildTarget.Android;
  var backend=PlayerSettings.GetScriptingBackend(target);var architecture=PlayerSettings.Android.targetArchitectures;var sdk=PlayerSettings.Android.targetSdkVersion;bool bundle=EditorUserBuildSettings.buildAppBundle,keystore=PlayerSettings.Android.useCustomKeystore;
  try {
   PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;EditorUserBuildSettings.buildAppBundle=aab;
   System.IO.Directory.CreateDirectory("Builds/Android");
   string outPath=aab?"Builds/Android/2048-Premium-release.aab":(development?"Builds/Android/2048-Premium-dev.apk":"Builds/Android/2048-Premium-release.apk");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Premium.unity"},locationPathName=outPath,target=BuildTarget.Android,options=development?BuildOptions.Development:BuildOptions.CompressWithLz4HC});
   if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Android build failed: "+report.summary.result);
   Debug.Log("PREMIUM_ANDROID_BUILD_SUCCESS: "+report.summary.totalSize+" bytes");
  } finally {PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.Android.targetArchitectures=architecture;PlayerSettings.Android.useCustomKeystore=keystore;PlayerSettings.Android.targetSdkVersion=sdk;EditorUserBuildSettings.buildAppBundle=bundle;}
 }
 public static void BuildReleaseMac() {Build(false);}
 public static void BuildMac() {Build(true);}
 public static void BuildExpeditionQa() {Build(true,"/tmp/2048ExpeditionQA.app",true);}
 static void Build(bool development,string destination=null,bool clean=false) {
  ProjectSetup.Configure();
  BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
   scenes=new[]{"Assets/Scenes/Premium.unity"},locationPathName=destination??(development?"/tmp/2048Premium.app":"/tmp/2048Premium-release.app"),target=BuildTarget.StandaloneOSX,options=(development?BuildOptions.Development:BuildOptions.None)|(clean?BuildOptions.CleanBuildCache:BuildOptions.None)
  });
  if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Mac build failed: "+report.summary.result);
  Debug.Log("PREMIUM_BUILD_SUCCESS: "+report.summary.totalSize+" bytes");
 }
}
