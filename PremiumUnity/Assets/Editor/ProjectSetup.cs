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
  PlayerSettings.productName="2048 Premium";PlayerSettings.companyName="GSV Infotech";
  PlayerSettings.defaultScreenWidth=600;PlayerSettings.defaultScreenHeight=960;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
  PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
  PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.gsvinfotech.twozerofoureight");
  PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.gsvinfotech.twozerofoureight");
  var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Brand/AppIcon.png");
  if(icon!=null){
   foreach(var group in new[]{BuildTargetGroup.Unknown,BuildTargetGroup.Android,BuildTargetGroup.iOS,BuildTargetGroup.Standalone}) {
    var named=UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
    int count=PlayerSettings.GetIconSizes(named,UnityEditor.IconKind.Any).Length;
    if(count==0)continue;
    var icons=new Texture2D[count];for(int i=0;i<count;i++)icons[i]=icon;
    PlayerSettings.SetIcons(named,icons,UnityEditor.IconKind.Any);
   }
  }
  foreach(var platform in new[]{UnityEditor.Build.NamedBuildTarget.Android,UnityEditor.Build.NamedBuildTarget.iOS}) {
   foreach(var kind in PlayerSettings.GetSupportedIconKinds(platform)) {
    var slots=PlayerSettings.GetPlatformIcons(platform,kind);
    string label=kind.ToString().ToLowerInvariant();
    foreach(var slot in slots) {
     if(platform==UnityEditor.Build.NamedBuildTarget.Android && label.Contains("adaptive")) {
      slot.SetTextures(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Platform/Android/AdaptiveBackground.png"),AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Platform/Android/AdaptiveForeground.png"));
     } else {slot.SetTexture(platform==UnityEditor.Build.NamedBuildTarget.Android && label.Contains("monochrome")?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Platform/Android/Monochrome.png"):icon);}
    }
    PlayerSettings.SetPlatformIcons(platform,kind,slots);
   }
  }
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}AssetDatabase.SaveAssets();
 }
 [MenuItem("2048 Premium/Create scene")]
 public static void CreateScene() {
  System.IO.Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("2048 Premium").AddComponent<PremiumGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Premium.unity");
 }
}
