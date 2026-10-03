using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Premium2048 {
public sealed class PremiumButtonMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler {
 float target=1;Button button;
 void Awake(){button=GetComponent<Button>();}
 public void OnPointerDown(PointerEventData data){if(button.interactable && (GameManager.Instance==null || !GameManager.Instance.ReducedMotion))target=0.94f;}
 public void OnPointerUp(PointerEventData data){target=1;}
 public void OnPointerExit(PointerEventData data){target=1;}
 void Update(){if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){target=1;transform.localScale=Vector3.one;return;}if(Mathf.Abs(transform.localScale.x-target)<.001f)return;transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-24*Time.unscaledDeltaTime));}
 void OnDisable(){target=1;transform.localScale=Vector3.one;}
}
public sealed class PremiumPanelMotion : MonoBehaviour {
 CanvasGroup group;float elapsed,minimum=.98f;
 const float Duration=.24f;
 public void Reveal(float startScale=.98f){
  minimum=startScale;
  if(group==null){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();}
  if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){Restore();return;}
  elapsed=0;group.alpha=0;transform.localScale=Vector3.one*minimum;
 }
 void Update(){
  if(group==null || elapsed>=Duration)return;
  if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){Restore();return;}
  elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/Duration);float ease=1-Mathf.Pow(1-t,3);
  group.alpha=ease;transform.localScale=Vector3.one*Mathf.Lerp(minimum,1,ease);
  if(t>=1)Restore();
 }
 void Restore(){elapsed=Duration;if(group!=null)group.alpha=1;transform.localScale=Vector3.one;}
 void OnDisable(){Restore();}
}
}
