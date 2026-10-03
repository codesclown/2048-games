using UnityEngine;

namespace Premium2048 {
// Extend dialog shading beyond the safe area while keeping controls inside it.
[ExecuteAlways]
public sealed class PremiumFullScreenBackdrop : MonoBehaviour {
 readonly Vector3[] corners = new Vector3[4];
 void OnEnable(){Fit();}
 void LateUpdate(){Fit();}
 public void Fit(){
  var canvas=GetComponentInParent<Canvas>();
  var rect=transform as RectTransform;
  var parent=transform.parent as RectTransform;
  if(canvas==null || rect==null || parent==null)return;
  var viewport=canvas.rootCanvas.transform as RectTransform;
  if(viewport==null)return;
  viewport.GetWorldCorners(corners);
  var lower=parent.InverseTransformPoint(corners[0]);
  var upper=parent.InverseTransformPoint(corners[2]);
  rect.anchorMin=rect.anchorMax=Vector2.zero;
  rect.pivot=Vector2.zero;
  var position=(Vector2)lower-parent.rect.min;
  var size=(Vector2)(upper-lower);
  if(rect.anchoredPosition!=position)rect.anchoredPosition=position;
  if(rect.sizeDelta!=size)rect.sizeDelta=size;
 }
}
}
