using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
public enum IconKind {Menu,Sound,Mute,Restart,Undo,Swap,Delete}
// Vector geometry avoids emoji/font fallback differences between Android and iOS.
public sealed class PremiumIcon : MaskableGraphic {
 IconKind kind;
 public IconKind Kind {get{return kind;}set{kind=value;SetVerticesDirty();}}
 protected override void OnPopulateMesh(VertexHelper mesh) {
  mesh.Clear();
  switch(kind) {
   case IconKind.Menu:Line(mesh,-.8f,.6f,.8f,.6f);Line(mesh,-.8f,0,.8f,0);Line(mesh,-.8f,-.6f,.8f,-.6f);break;
   case IconKind.Sound:case IconKind.Mute:
    Line(mesh,-.8f,-.25f,-.8f,.25f);Line(mesh,-.8f,.25f,-.45f,.25f);Line(mesh,-.45f,.25f,0,.65f);Line(mesh,0,.65f,0,-.65f);Line(mesh,0,-.65f,-.45f,-.25f);Line(mesh,-.45f,-.25f,-.8f,-.25f);
    if(kind==IconKind.Mute){Line(mesh,.3f,.3f,.8f,-.3f);Line(mesh,.3f,-.3f,.8f,.3f);}else {Arc(mesh,.13f,0,.4f,-60,60);Arc(mesh,.13f,0,.72f,-60,60);}break;
   case IconKind.Restart:case IconKind.Undo:
    Arc(mesh,0,0,.68f,-55,225);Line(mesh,-.48f,.48f,-.48f,.85f);Line(mesh,-.48f,.48f,-.08f,.48f);break;
   case IconKind.Swap:
    Line(mesh,-.8f,.35f,.8f,.35f);Line(mesh,.8f,.35f,.45f,.7f);Line(mesh,.8f,.35f,.45f,0);Line(mesh,.8f,-.35f,-.8f,-.35f);Line(mesh,-.8f,-.35f,-.45f,0);Line(mesh,-.8f,-.35f,-.45f,-.7f);break;
   case IconKind.Delete:
    Line(mesh,-.75f,.5f,.75f,.5f);Line(mesh,-.3f,.8f,.3f,.8f);Line(mesh,-.55f,.5f,-.45f,-.75f);Line(mesh,-.45f,-.75f,.45f,-.75f);Line(mesh,.45f,-.75f,.55f,.5f);Line(mesh,-.18f,.2f,-.18f,-.45f);Line(mesh,.18f,.2f,.18f,-.45f);break;
  }
 }
 void Arc(VertexHelper mesh,float x,float y,float radius,float start,float end){for(int i=0;i<24;i++){float a=Mathf.Lerp(start,end,i/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/24f)*Mathf.Deg2Rad;Line(mesh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
 void Line(VertexHelper mesh,float x1,float y1,float x2,float y2) {
  var rect=rectTransform.rect;float scale=Mathf.Min(rect.width,rect.height)*.5f;
  Vector2 a=rect.center+new Vector2(x1,y1)*scale,b=rect.center+new Vector2(x2,y2)*scale,n=new Vector2(-(b-a).y,(b-a).x).normalized*scale*.075f;
  int index=mesh.currentVertCount;mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
 }
}
}
