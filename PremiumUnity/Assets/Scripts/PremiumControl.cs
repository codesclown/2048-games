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
  var background=Tone==ControlTone.Primary?PremiumTheme.Accent:Tone==ControlTone.Destructive?PremiumTheme.Color("713A48"):PremiumTheme.Color("263952");
  if(manager.HighContrast && Tone==ControlTone.Secondary)background=PremiumTheme.Color("101D30");
  if(locked || !button.interactable)background=PremiumTheme.Color("1C2C43");
  if(selected)background=PremiumTheme.Accent;
  surface.color=background;
  var foreground=(Tone==ControlTone.Primary && button.interactable || selected)?PremiumTheme.Ink:PremiumTheme.Text;
  foreach(var label in GetComponentsInChildren<Text>())label.color=foreground;
  foreach(var icon in GetComponentsInChildren<PremiumIcon>())icon.color=foreground;
 }
}
public sealed class PremiumSelectionFrame : MaskableGraphic {
 public override Texture mainTexture => Texture2D.whiteTexture;
 protected override void OnPopulateMesh(VertexHelper mesh) {
  mesh.Clear();var r=rectTransform.rect;float t=3;
  Quad(mesh,new Rect(r.xMin,r.yMin,r.width,t));Quad(mesh,new Rect(r.xMin,r.yMax-t,r.width,t));Quad(mesh,new Rect(r.xMin,r.yMin,t,r.height));Quad(mesh,new Rect(r.xMax-t,r.yMin,t,r.height));
 }
 void Quad(VertexHelper mesh,Rect r){int start=mesh.currentVertCount;mesh.AddVert(new Vector2(r.xMin,r.yMin),color,new Vector2(0.5f,0.5f));mesh.AddVert(new Vector2(r.xMin,r.yMax),color,new Vector2(0.5f,0.5f));mesh.AddVert(new Vector2(r.xMax,r.yMax),color,new Vector2(0.5f,0.5f));mesh.AddVert(new Vector2(r.xMax,r.yMin),color,new Vector2(0.5f,0.5f));mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);}
}
}
