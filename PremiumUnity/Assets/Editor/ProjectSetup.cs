using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Premium2048;
[InitializeOnLoad] public static class ProjectSetup {
 static ProjectSetup() {EditorApplication.delayCall+=EnsureScene;}
 static void EnsureScene() {if(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool("Premium2048.configured",false)) Configure();}
 [MenuItem("2048 Premium/Configure project")]
 public static void Configure() {
  SessionState.SetBool("Premium2048.configured",true);
  if(!System.IO.File.Exists("Assets/Scenes/Premium.unity")) CreateScene();
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Premium.unity",true)};
  PlayerSettings.productName="2048 Premium";PlayerSettings.companyName="Premium2048";
  PlayerSettings.defaultScreenWidth=600;PlayerSettings.defaultScreenHeight=960;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
  PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
  PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.premium2048.game");
  PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.premium2048.game");
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}AssetDatabase.SaveAssets();
 }
 [MenuItem("2048 Premium/Create scene")]
 public static void CreateScene() {
  System.IO.Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("2048 Premium").AddComponent<PremiumGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Premium.unity");
 }
}
