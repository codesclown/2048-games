using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Premium2048.Editor {
// Draw launcher assets from the game's own type and tile geometry.
public static class BrandAssetBaker {
 public static void Bake(){
  Render("Assets/Resources/Brand/AppIcon.png",512,false,false);
  Render("Assets/Resources/Brand/Mark.png",512,true,false);
  Render("Assets/Art/Platform/Android/AdaptiveForeground.png",432,true,false);
  Render("Assets/Art/Platform/Android/AdaptiveBackground.png",432,false,true);
  Render("Assets/Art/Platform/Android/Monochrome.png",432,true,false,true);
  Render("Assets/Art/Platform/iOS/AppIcon-1024.png",1024,false,false);
  foreach(int size in new[]{20,29,40,58,60,76,80,87,120,152,167,180,1024})Render("Assets/Art/Platform/iOS/AppIcon.appiconset/icon-"+size+".png",size,false,false);
  Render("Assets/Resources/Brand/LaunchPortrait.png",1080,false,false,false,true);
  AssetDatabase.Refresh();
 }
 static void Render(string path,int size,bool transparent,bool backgroundOnly,bool monochrome=false,bool portrait=false){
  var root=new GameObject("Brand render",typeof(RectTransform),typeof(Canvas));root.layer=31;
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
  var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1024,1024);
  var cameraObject=new GameObject("Brand camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=portrait?910.222f:512;camera.aspect=portrait?9f/16f:1;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=transparent?Color.clear:PremiumTheme.Background;
  Sprite shape=PremiumTheme.CreateRoundedCard(128,20,Color.white,Color.clear,0);
  if(!backgroundOnly){
   float extent=transparent?270:300,cell=transparent?244:276;
   for(int i=0;i<4;i++){
    var go=new GameObject("Tile",typeof(RectTransform),typeof(Image));go.layer=31;go.transform.SetParent(root.transform,false);var rt=(RectTransform)go.transform;rt.sizeDelta=Vector2.one*cell;rt.anchoredPosition=new Vector2(i%2==0?-extent*.5f:extent*.5f,i<2?extent*.5f:-extent*.5f);var image=go.GetComponent<Image>();image.sprite=shape;image.type=Image.Type.Sliced;image.color=monochrome?Color.white:i==3?PremiumTheme.Accent:PremiumTheme.SurfaceElevated;
    if(!monochrome){var labelGo=new GameObject("Digit",typeof(RectTransform),typeof(Text));labelGo.layer=31;labelGo.transform.SetParent(go.transform,false);var lr=(RectTransform)labelGo.transform;lr.sizeDelta=Vector2.one*cell;var text=labelGo.GetComponent<Text>();text.font=Resources.Load<Font>("Fonts/NotoSans-Bold");text.fontSize=164;text.alignment=TextAnchor.MiddleCenter;text.color=i==3?PremiumTheme.Ink:PremiumTheme.Text;text.text=new[]{"2","0","4","8"}[i];}
   }
  }
  if(portrait){var titleGo=new GameObject("Wordmark",typeof(RectTransform),typeof(Text));titleGo.layer=31;titleGo.transform.SetParent(root.transform,false);var tr=(RectTransform)titleGo.transform;tr.sizeDelta=new Vector2(800,200);tr.anchoredPosition=new Vector2(0,-430);var title=titleGo.GetComponent<Text>();title.font=Resources.Load<Font>("Fonts/NotoSans-Bold");title.fontSize=144;title.text="2048";title.color=PremiumTheme.Accent;title.alignment=TextAnchor.MiddleCenter;}
  int height=portrait?Mathf.RoundToInt(size*16f/9f):size;
  var target=new RenderTexture(size,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var output=new Texture2D(size,height,transparent?TextureFormat.RGBA32:TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,size,height),0,0);output.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,output.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;
  Object.DestroyImmediate(target);Object.DestroyImmediate(output);Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
 }
}
}
