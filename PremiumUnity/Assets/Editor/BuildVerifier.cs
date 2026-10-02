using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildVerifier {
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
