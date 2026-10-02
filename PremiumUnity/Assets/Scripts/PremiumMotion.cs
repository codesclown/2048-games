using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Premium2048 {
public sealed class PremiumButtonMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler {
 float target=1;Button button;
 void Awake(){button=GetComponent<Button>();}
 public void OnPointerDown(PointerEventData data){if(button.interactable)target=0.94f;}
 public void OnPointerUp(PointerEventData data){target=1;}
 public void OnPointerExit(PointerEventData data){target=1;}
 void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-24*Time.unscaledDeltaTime));}
 void OnDisable(){target=1;transform.localScale=Vector3.one;}
}
public sealed class PremiumPanelMotion : MonoBehaviour {
 CanvasGroup group;float elapsed;
 public void Reveal(){if(group==null)group=gameObject.AddComponent<CanvasGroup>();elapsed=0;group.alpha=0;transform.localScale=Vector3.one*0.94f;}
 void Update(){if(group==null)return;elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.2f);group.alpha=t;transform.localScale=Vector3.one*Mathf.Lerp(0.94f,1,1-Mathf.Pow(1-t,3));}
}
}
