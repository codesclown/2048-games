using UnityEngine;

namespace Premium2048 {
// Animate only the gear artwork, leaving its touch target fixed.
public sealed class PremiumIconRotation : MonoBehaviour {
 float angle;
 void Update(){
  if(GameManager.Instance!=null && GameManager.Instance.ReducedMotion){ResetRotation();return;}
  angle=Mathf.Repeat(angle-30f*Time.unscaledDeltaTime,360f);
  transform.localRotation=Quaternion.Euler(0,0,angle);
 }
 void OnDisable(){ResetRotation();}
 void ResetRotation(){angle=0;transform.localRotation=Quaternion.identity;}
}
}
