using System.Collections.Generic;
using UnityEngine;
namespace Premium2048 {
// Staggered entrance on the home view; idle movement is limited to the brand.
public sealed class PremiumHomeMotion : MonoBehaviour {
 struct Entry { public RectTransform rect; public CanvasGroup group; public Vector2 position; public float delay; }
 readonly List<Entry> entries=new List<Entry>();
 GameManager manager;float elapsed;bool restoring;RectTransform brand;Vector2 brandPosition;
 public bool IsRevealing {get;private set;}
 public void Reveal(GameManager owner) {
  // Layout has just been repositioned by the view; capture the current safe-area positions.
  manager=owner;entries.Clear();brand=null;elapsed=0;restoring=false;IsRevealing=!owner.ReducedMotion;
  int order=0;
  foreach(Transform child in transform){
   var rect=child as RectTransform;if(rect==null || !child.gameObject.activeSelf)continue;
   var group=child.GetComponent<CanvasGroup>();if(group==null)group=child.gameObject.AddComponent<CanvasGroup>();
   bool header=child.name=="Sound: On" || child.name=="Settings";
   float delay=header?0:Mathf.Min(order++*.025f,.22f);
   entries.Add(new Entry{rect=rect,group=group,position=rect.anchoredPosition,delay=delay});
   if(child.name=="Brand"){brand=rect;brandPosition=rect.anchoredPosition;}
   group.alpha=IsRevealing?0:1;
   if(IsRevealing)rect.anchoredPosition+=Vector2.down*12;
  }
 }
 void LateUpdate(){
  if(manager==null || entries.Count==0)return;
  if(manager.ReducedMotion || manager.Navigation.Suspended || manager.Navigation.Dialog!=GameDialog.None){if(!restoring){StopAndRestore();restoring=true;}return;}
  restoring=false;elapsed+=Time.unscaledDeltaTime;
  if(!IsRevealing){if(brand!=null)brand.anchoredPosition=brandPosition+Vector2.up*(2*Mathf.Sin(Mathf.Max(0,elapsed-.6f)*1.3f));return;}
  bool entering=false;
  foreach(var entry in entries){
   if(entry.rect==null)continue;
   float t=Mathf.Clamp01((elapsed-entry.delay)/.32f);float ease=t*t*(3-2*t);
   entry.group.alpha=ease;
   if(t<1){entering=true;entry.rect.anchoredPosition=entry.position+Vector2.down*(12*(1-ease));}
   else if(entry.rect.name=="Brand")entry.rect.anchoredPosition=entry.position+Vector2.up*(2*Mathf.Sin(Mathf.Max(0,elapsed-.6f)*1.3f));
   else entry.rect.anchoredPosition=entry.position;
  }
  IsRevealing=entering;
 }
 public void Rebase(){for(int i=0;i<entries.Count;i++){var entry=entries[i];if(entry.rect==null)continue;entry.position=entry.rect.anchoredPosition;entries[i]=entry;if(entry.rect==brand)brandPosition=entry.position;}}
 public void StopAndRestore(){foreach(var entry in entries){if(entry.rect==null)continue;entry.rect.anchoredPosition=entry.position;entry.group.alpha=1;}IsRevealing=false;elapsed=1;}
 void OnDisable(){StopAndRestore();}
}
}
