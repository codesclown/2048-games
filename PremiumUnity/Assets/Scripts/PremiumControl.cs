using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
public enum ControlTone { Secondary, Primary, Destructive }
// Explicit visual states remain legible; button tint never washes out disabled labels.
public sealed class PremiumControl : MonoBehaviour {
 public ControlTone Tone;
 public string AccessibleName;
 GameManager manager;Button button;Image surface;bool selected,locked;
 public void Initialize(GameManager manager,ControlTone tone,string name){this.manager=manager;Tone=tone;AccessibleName=name;button=GetComponent<Button>();surface=GetComponent<Image>();Apply();}
 public void State(bool selected=false,bool locked=false){this.selected=selected;this.locked=locked;Apply();}
 public void Apply() {
  if(manager==null || surface==null)return;
  var background=Tone==ControlTone.Primary?PremiumTheme.Accent:Tone==ControlTone.Destructive?PremiumTheme.Color("713A48"):PremiumTheme.SurfaceElevated;
  if(manager.HighContrast && Tone==ControlTone.Secondary)background=PremiumTheme.Background;
  if(locked)background=PremiumTheme.Color("1C2C43");
  if(selected)background=PremiumTheme.Accent;
  surface.sprite=PremiumTheme.SurfaceSprite(Tone==ControlTone.Primary || selected);
  surface.type=Image.Type.Sliced;surface.color=background;
  var depth=GetComponent<Shadow>();if(depth==null){depth=gameObject.AddComponent<Shadow>();depth.effectDistance=new Vector2(0,-3);depth.useGraphicAlpha=true;}
  depth.effectColor=new Color(0,0,0,button!=null && !button.interactable?.12f:.3f);
  var foreground=(Tone==ControlTone.Primary || selected)?PremiumTheme.Ink:PremiumTheme.Text;
  foreach(var label in GetComponentsInChildren<Text>())label.color=foreground;
  foreach(var icon in GetComponentsInChildren<PremiumIcon>())icon.color=foreground;
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PremiumSelectionFrame : MaskableGraphic {
 public override Texture mainTexture => Texture2D.whiteTexture;
 protected override void OnPopulateMesh(VertexHelper mesh) {
  mesh.Clear();var rect=rectTransform.rect;float radius=Mathf.Min(14,Mathf.Min(rect.width,rect.height)*.5f),thickness=2;
  const int steps=8,points=4*(steps+1);
  for(int corner=0;corner<4;corner++) {
   var center=new Vector2(corner==0 || corner==3?rect.xMax-radius:rect.xMin+radius,corner<2?rect.yMax-radius:rect.yMin+radius);
   for(int step=0;step<=steps;step++) {
    float angle=(corner*90f+step*90f/steps)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    mesh.AddVert(center+direction*radius,color,new Vector2(.5f,.5f));mesh.AddVert(center+direction*Mathf.Max(0,radius-thickness),color,new Vector2(.5f,.5f));
   }
  }
  for(int i=0;i<points;i++){int a=i*2,b=((i+1)%points)*2;mesh.AddTriangle(a,b,b+1);mesh.AddTriangle(a,b+1,a+1);}
 }
}
}
