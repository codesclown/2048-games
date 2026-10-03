using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Premium2048.Editor {
public static class PremiumPreviewVerification {
 static void Check(bool value,string message){if(!value)throw new Exception("Preview parity: "+message);}
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Premium.unity");
  var controller=scene.GetRootGameObjects().First(go=>go.GetComponent<PremiumGame>()!=null);
  var canvas=scene.GetRootGameObjects().First(go=>go.GetComponent<PremiumPreviewCanvas>()!=null);
  var content=(RectTransform)canvas.transform.Find("Safe Area/Content");
  content.localScale=Vector3.one*.25f;
  var staleTitle=canvas.transform.Find("Safe Area/Content/Home/Expedition/Label").GetComponent<Text>();staleTitle.text="OLD UI";
  Check(UIBaker.RefreshOpenPreview(),"recognized generated scene");
  Check(controller!=null && controller.GetComponent<PremiumGame>()!=null,"controller survives refresh");
  Check(scene.GetRootGameObjects().Count(go=>go.GetComponent<PremiumPreviewCanvas>()!=null)==1,"one preview canvas");
  canvas=scene.GetRootGameObjects().First(go=>go.GetComponent<PremiumPreviewCanvas>()!=null);
  Canvas.ForceUpdateCanvases();canvas.GetComponent<PremiumPreviewLayout>().Fit();
  var safe=(RectTransform)canvas.transform.Find("Safe Area");content=(RectTransform)safe.Find("Content");
  var home=safe.Find("Content/Home");
  Check(((RectTransform)home.Find("Expedition")).sizeDelta.x==244,"current paired card geometry replaces stale hierarchy");
  Check(home.Find("Expedition/Label").GetComponent<Text>().text!="OLD UI","current source text replaces old labels");
  Check(canvas.GetComponentInChildren<PremiumSafeArea>().padding==Vector2.zero,"same safe-area padding as runtime");
  float expected=Mathf.Min(safe.rect.width/540,safe.rect.height/(960+PremiumLayout.BannerReserve));
  Check(Mathf.Abs(content.localScale.x-expected)<.001f,"stale scale replaced by current viewport fit");
  foreach(var group in canvas.GetComponentsInChildren<CanvasGroup>(true))Check(group.alpha==1,"preview finishes fully visible without Play");
  Check(home.Find("Preview Board/Hero Preview").GetComponent<RectTransform>().sizeDelta.x==156,"same enlarged live board preview");
  Debug.Log("PREMIUM_PREVIEW_PARITY_SUCCESS: stale hierarchy/scale repaired, source labels, safe area, visibility, preserved controller");
 }
}
}
