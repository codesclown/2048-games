using UnityEngine;
namespace Premium2048 {
[ExecuteAlways]
public sealed class PremiumSafeArea : MonoBehaviour {
 public Vector2 padding=new Vector2(40,50);
 Rect previous;int width,height;
 void OnEnable(){Apply();}
 void Update(){if(previous!=Screen.safeArea || width!=Screen.width || height!=Screen.height)Apply();}
 void Apply(){width=Screen.width;height=Screen.height;previous=Screen.safeArea;if(width<=0 || height<=0)return;var rt=GetComponent<RectTransform>();var safe=previous.width>0 && previous.height>0?previous:new Rect(0,0,width,height);rt.anchorMin=new Vector2(safe.xMin/width,safe.yMin/height);rt.anchorMax=new Vector2(safe.xMax/width,safe.yMax/height);rt.offsetMin=padding;rt.offsetMax=-padding;}
}
}
