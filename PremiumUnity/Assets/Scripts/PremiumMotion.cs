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
 public void Reveal(float startScale=.98f){minimum=startScale;if(group==null){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();}elapsed=0;group.alpha=0;transform.localScale=Vector3.one*minimum;}
 void Update(){if(group==null || elapsed>=.2f)return;if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){group.alpha=1;transform.localScale=Vector3.one;return;}elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/0.2f);group.alpha=t;transform.localScale=Vector3.one*Mathf.Lerp(minimum,1,1-Mathf.Pow(1-t,3));}
}
}
