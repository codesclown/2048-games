using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
// A brief, quiet reward: one mesh, no particle GameObjects, no per-frame arrays.
public sealed class PremiumCelebration : MaskableGraphic {
 public override Texture mainTexture => Texture2D.whiteTexture;
 float elapsed;
 protected override void Awake(){base.Awake();raycastTarget=false;}
 void Update(){if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){enabled=false;return;}elapsed+=Time.unscaledDeltaTime;SetVerticesDirty();if(elapsed>1.4f)enabled=false;}
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(elapsed>1.4f)return;var r=rectTransform.rect;
  for(int i=0;i<28;i++){
   float delay=(i%7)*.025f,t=Mathf.Clamp01((elapsed-delay)/1.2f);if(t<=0 || t>=1)continue;
   float x=Mathf.Sin(i*2.399f)*r.width*.47f+Mathf.Sin(t*4+i)*12;
   float y=r.height*.46f-t*r.height*.8f;float size=3+(i%3);float alpha=(1-t)*.7f;
   var c=i%3==0?PremiumTheme.Text:PremiumTheme.Accent;c.a=alpha;int start=mesh.currentVertCount;
   mesh.AddVert(new Vector2(x-size,y),c,Vector2.zero);mesh.AddVert(new Vector2(x,y+size*1.8f),c,Vector2.zero);mesh.AddVert(new Vector2(x+size,y),c,Vector2.zero);mesh.AddVert(new Vector2(x,y-size*1.8f),c,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
  }
 }
}
}
