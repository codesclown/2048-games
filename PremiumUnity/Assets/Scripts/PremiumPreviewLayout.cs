using UnityEngine;
namespace Premium2048 {
// Editor previews use the runtime positioning logic at the current simulator aspect ratio.
[ExecuteAlways]
public sealed class PremiumPreviewLayout : MonoBehaviour {
 public bool compactDialog;
 Vector2 previous;
 void OnEnable(){Fit();}
 void Update(){if(Application.isPlaying)return;var safe=transform.Find("Safe Area") as RectTransform;if(safe!=null && safe.rect.size!=previous)Fit();}
 public void Fit(){
  if(Application.isPlaying)return;
  var safe=transform.Find("Safe Area") as RectTransform;if(safe==null || safe.rect.width<=0 || safe.rect.height<=0)return;previous=safe.rect.size;
  var content=safe.Find("Content") as RectTransform;var home=content?.Find("Home");var game=content?.Find("Game");var panel=safe.Find("Modal Overlay/Dialog") as RectTransform;
  if(content==null || home==null || game==null)return;
  float scale=Mathf.Min(safe.rect.width/540f,safe.rect.height/(960f+PremiumLayout.BannerReserve));float height=Mathf.Max(960,safe.rect.height/scale-PremiumLayout.BannerReserve);
  content.anchorMin=content.anchorMax=new Vector2(.5f,.5f);content.anchoredPosition=new Vector2(0,PremiumLayout.BannerReserve*scale*.5f);
  content.localScale=Vector3.one*scale;content.sizeDelta=new Vector2(540,height);
  ((RectTransform)home).sizeDelta=((RectTransform)game).sizeDelta=content.sizeDelta;
  if(panel!=null){panel.localScale=Vector3.one*scale;panel.anchoredPosition=content.anchoredPosition;}
  var banner=safe.Find("Banner Ad Slot") as RectTransform;if(banner!=null){banner.localScale=Vector3.one*scale;banner.anchoredPosition=new Vector2(0,-safe.rect.height*.5f+PremiumLayout.BannerReserve*scale*.5f);}
  var resume=home.Find("Resume");PremiumGame.PositionScreens(home.gameObject,game.gameObject,panel,height,resume!=null && resume.gameObject.activeSelf,false,compactDialog);
  var backdrop=safe.GetComponentInChildren<PremiumFullScreenBackdrop>(true);if(backdrop!=null)backdrop.Fit();
 }
}
}
