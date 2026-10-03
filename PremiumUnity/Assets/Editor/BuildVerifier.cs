using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildVerifier {
 public static void BuildNativeTargets(){BuildMac();BuildAndroidDevelopment();}
 public static void BuildAndroidDevelopment(){BuildAndroid(true);}
 public static void BuildAndroidPreview(){BuildAndroid(false);}
 static void BuildAndroid(bool development) {
  ProjectSetup.Configure();var target=UnityEditor.Build.NamedBuildTarget.Android;
  var backend=PlayerSettings.GetScriptingBackend(target);var architecture=PlayerSettings.Android.targetArchitectures;var sdk=PlayerSettings.Android.targetSdkVersion;bool bundle=EditorUserBuildSettings.buildAppBundle,keystore=PlayerSettings.Android.useCustomKeystore;
  try {
   PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;EditorUserBuildSettings.buildAppBundle=false;
   System.IO.Directory.CreateDirectory("Builds/Android");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Premium.unity"},locationPathName=development?"Builds/Android/2048-Premium-dev.apk":"Builds/Android/2048-Premium-preview.apk",target=BuildTarget.Android,options=development?BuildOptions.Development:BuildOptions.CompressWithLz4HC});
   if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Android build failed: "+report.summary.result);
   Debug.Log("PREMIUM_ANDROID_BUILD_SUCCESS: "+report.summary.totalSize+" bytes");
  } finally {PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.Android.targetArchitectures=architecture;PlayerSettings.Android.useCustomKeystore=keystore;PlayerSettings.Android.targetSdkVersion=sdk;EditorUserBuildSettings.buildAppBundle=bundle;}
 }
 public static void BuildReleaseMac() {Build(false);}
 public static void BuildMac() {Build(true);}
 static void Build(bool development) {
  ProjectSetup.Configure();
  BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
   scenes=new[]{"Assets/Scenes/Premium.unity"},locationPathName=development?"/tmp/2048Premium.app":"/tmp/2048Premium-release.app",target=BuildTarget.StandaloneOSX,options=development?BuildOptions.Development:BuildOptions.None
  });
  if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Mac build failed: "+report.summary.result);
  Debug.Log("PREMIUM_BUILD_SUCCESS: "+report.summary.totalSize+" bytes");
 }
}
