using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Premium2048.Editor {
// There is one UI kit: the native runtime view. Preview scenes and prefabs are derived from it.
public static class UIBaker {
 static readonly string[] Names={"01_HomeScreen","02_GameScreen_4x4","03_Dialog_Levels","04_Dialog_HighScores","05_Dialog_HowToPlay","06_Dialog_Settings","07_Dialog_Pause","08_Dialog_GameOver","09_Dialog_Victory","10_Dialog_Restart","11_Dialog_ClearProgress","12_Dialog_Continue","13_Dialog_NewBoard","14_Dialog_About"};
 static readonly GameDialog[] Dialogs={GameDialog.None,GameDialog.None,GameDialog.Levels,GameDialog.Scores,GameDialog.Instructions,GameDialog.Settings,GameDialog.Pause,GameDialog.Loss,GameDialog.Win,GameDialog.Restart,GameDialog.ClearScores,GameDialog.Continue,GameDialog.ReplaceBoard,GameDialog.About};
 [MenuItem("2048 Premium/Bake All Screens & Prefabs")]
 public static void BakeAll() {
  BrandAssetBaker.Bake();
  Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Prefabs/Screens");Directory.CreateDirectory("Assets/Prefabs/Dialogs");
  for(int i=0;i<Names.Length;i++)Bake(Names[i],Dialogs[i],i==1,false);
  Bake("Premium",GameDialog.None,false,true);
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Premium.unity");
  Debug.Log("[UIBaker] All native UI previews and prefabs rebuilt from the runtime view.");
 }
 static void Bake(string name,GameDialog dialog,bool gameplay,bool master) {
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var view=new GameObject("Preview source").AddComponent<PremiumGame>();view.BuildEditorPreview(dialog,gameplay);
  var canvas=view.GetComponentInChildren<Canvas>();var canvasGo=canvas.gameObject;canvasGo.transform.SetParent(null,false);canvasGo.name="Canvas_"+name;canvasGo.tag="EditorOnly";
  PersistSprites(canvasGo);
  var scaler=canvas.GetComponent<CanvasScaler>();scaler.enabled=true;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=0f;
  canvasGo.AddComponent<PremiumPreviewCanvas>();
  var safe=canvasGo.transform.Find("Safe Area");safe.gameObject.AddComponent<PremiumSafeArea>().padding=new Vector2(10,12);
  var layout=canvasGo.AddComponent<PremiumPreviewLayout>();layout.compactDialog=dialog==GameDialog.Restart || dialog==GameDialog.ClearScores || dialog==GameDialog.ReplaceBoard || dialog==GameDialog.Continue || dialog==GameDialog.VideoUnavailable;
  Canvas.ForceUpdateCanvases();layout.Fit();
  // Prefabs contain the visible native view, including its real controls and vector icons.
  Transform screen=dialog==GameDialog.None?safe.Find("Content/"+(gameplay?"Game":"Home")):safe.Find("Modal Overlay/Dialog");
  if(screen!=null){string prefab=dialog==GameDialog.None?(gameplay?"GameScreen":"HomeScreen"):dialog==GameDialog.Scores?"HighScoresDialog":dialog==GameDialog.Instructions?"HowToPlayDialog":dialog==GameDialog.Loss?"GameOverDialog":dialog==GameDialog.Win?"VictoryDialog":dialog+"Dialog";PrefabUtility.SaveAsPrefabAsset(screen.gameObject,"Assets/Prefabs/"+(dialog==GameDialog.None?"Screens/":"Dialogs/")+prefab+".prefab");}
  UnityEngine.Object.DestroyImmediate(view.gameObject);
  if(master)new GameObject("2048 Premium Controller").AddComponent<PremiumGame>();
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+name+".unity");
 }
 static void PersistSprites(GameObject canvas) {
  const string folder="Assets/Art/BakedUI";Directory.CreateDirectory(folder);
  foreach(var image in canvas.GetComponentsInChildren<Image>(true)) {
   var sprite=image.sprite;if(sprite==null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite)))continue;
   var bytes=sprite.texture.EncodeToPNG();string hash;
   using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20);
   string path=folder+"/UI-"+hash+".png";
   if(!File.Exists(path)){File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single || importer.spriteBorder!=sprite.border){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=sprite.border;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
   var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(saved==null)throw new InvalidOperationException("Missing baked sprite: "+path);image.sprite=saved;
  }
 }
}
}
