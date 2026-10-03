using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
// One reusable mesh for all merged tiles; no per-move particles or GameObjects.
public sealed class PremiumMergeFeedback : MaskableGraphic {
 readonly Vector2[] positions=new Vector2[36];int count;float elapsed,size;
 public override Texture mainTexture=>Texture2D.whiteTexture;
 public bool IsPlaying=>enabled && count>0;
 protected override void Awake(){base.Awake();raycastTarget=false;enabled=false;}
 public void Begin(float cellSize){count=0;elapsed=0;size=cellSize;transform.SetAsLastSibling();enabled=true;}
 public void Add(Vector2 position){if(count<positions.Length)positions[count++]=position;SetVerticesDirty();}
 public void Stop(){count=0;SetVerticesDirty();enabled=false;}
 void Update(){
  var manager=GameManager.Instance;
  if(manager!=null && (manager.ReducedMotion || manager.Navigation.Suspended || manager.Navigation.Dialog!=GameDialog.None)){Stop();return;}
  elapsed+=Time.unscaledDeltaTime;if(elapsed>=.18f){Stop();return;}SetVerticesDirty();
 }
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(count==0)return;float t=Mathf.Clamp01(elapsed/.18f),half=size*.5f+1+4*t,radius=10;
  var tone=PremiumTheme.Accent;tone.a=.65f*(1-t)*(1-t);
  for(int n=0;n<count;n++){
   int start=mesh.currentVertCount;
   for(int i=0;i<32;i++){
    int corner=i/8;float angle=(corner+(i%8)/7f)*Mathf.PI*.5f;
    var normal=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    var center=new Vector2(corner==0 || corner==3?half-radius:-half+radius,corner<2?half-radius:-half+radius);
    mesh.AddVert(positions[n]+center+normal*(radius+1.2f),tone,Vector2.zero);
    mesh.AddVert(positions[n]+center+normal*(radius-1.2f),tone,Vector2.zero);
   }
   for(int i=0;i<32;i++){int a=start+i*2,b=start+((i+1)%32)*2;mesh.AddTriangle(a,b,a+1);mesh.AddTriangle(a+1,b,b+1);}
  }
 }
}
}
