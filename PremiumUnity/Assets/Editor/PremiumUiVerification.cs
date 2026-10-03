using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Premium2048.Editor {
public static class PremiumUiVerification {
 const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception("UI layout: "+message);}
 static RectTransform Find(GameObject parent,string name){return parent.transform.Find(name) as RectTransform;}
 static float Bottom(RectTransform rect){return rect.anchoredPosition.y-rect.sizeDelta.y*rect.localScale.y*.5f;}
 static float Top(RectTransform rect){return rect.anchoredPosition.y+rect.sizeDelta.y*rect.localScale.y*.5f;}
 [MenuItem("2048 Premium/Verify Responsive UI")]
 public static void Run(){
  var owner=new GameObject("UI layout verification");
  try{
   var view=owner.AddComponent<PremiumGame>();view.BuildEditorPreview(GameDialog.None,true);
   var home=(GameObject)typeof(PremiumGame).GetField("home",Fields).GetValue(view);
   var game=(GameObject)typeof(PremiumGame).GetField("game",Fields).GetValue(view);
   var panel=(RectTransform)typeof(PremiumGame).GetField("modalPanel",Fields).GetValue(view);
   var safe=(RectTransform)typeof(PremiumGame).GetField("safeRoot",Fields).GetValue(view);
   var backdrop=safe.GetComponentInChildren<PremiumFullScreenBackdrop>(true);
   var viewport=(RectTransform)safe.GetComponentInParent<Canvas>().transform;
   var viewportCorners=new Vector3[4];var backdropCorners=new Vector3[4];
   foreach(var inset in new[]{Vector2.zero,new Vector2(24,70),new Vector2(64,30)}){
    safe.offsetMin=inset;safe.offsetMax=-inset;
    backdrop.Fit();viewport.GetWorldCorners(viewportCorners);
    ((RectTransform)backdrop.transform).GetWorldCorners(backdropCorners);
    for(int i=0;i<4;i++)Check(Vector3.Distance(viewportCorners[i],backdropCorners[i])<.1f,"dialog backdrop fills viewport with safe-area inset "+inset);
   }
   safe.offsetMin=safe.offsetMax=Vector2.zero;
   foreach(float height in new[]{960f,1000f,1170f,1280f})foreach(bool resume in new[]{false,true}){
    PremiumGame.PositionScreens(home,game,panel,height,resume,false,false);
    string[] names=resume?new[]{"Preview Board","New Game","Expedition","Resume","Stats Summary","How to Play","Footer"}:new[]{"Preview Board","New Game","Expedition","Stats Summary","How to Play","Footer"};
    for(int i=1;i<names.Length;i++)Check(Bottom(Find(home,names[i-1]))>=Top(Find(home,names[i]))+4,"home overlap: "+names[i]+" at "+height);
    Check(Mathf.Abs(Find(home,"Expedition").anchoredPosition.y-Find(home,"Daily Puzzle").anchoredPosition.y)<.01f,"mode cards share a row");
    Check(Find(home,"Expedition").anchoredPosition.x+Find(home,"Expedition").sizeDelta.x*.5f<Find(home,"Daily Puzzle").anchoredPosition.x-Find(home,"Daily Puzzle").sizeDelta.x*.5f,"mode cards do not overlap horizontally");
    Check(Bottom(Find(home,"Footer"))>-height*.5f+PremiumLayout.BannerReserve,"footer clears bottom banner");
    Check(Bottom(Find(home,"Footer"))+height*.5f<=PremiumLayout.BannerReserve+200f,"footer leaves banner space without excessive gap");
    Check(height*.5f-Top(Find(home,"Settings"))<=24,"header uses top safe area without excess margin");
    foreach(string name in new[]{"New Game","Expedition","Daily Puzzle","How to Play","High Scores"})Check(Find(home,name).sizeDelta.y>=PremiumLayout.MinTouchTarget,"touch size: "+name);
    foreach(bool selecting in new[]{false,true}){
     PremiumGame.PositionScreens(home,game,panel,height,resume,selecting,false);
     var board=Find(game,"Board");var tools=Find(game,"Action Bar");
     Check(Top(board)<=Bottom(Find(game,"Level Pill"))-12,"board clears level caption");
     Check(Bottom(board)>=Top(tools)+12,"board clears tools during selection");
     Check(board.localScale.x>0 && board.localScale.x<=1,"valid responsive board scale");
    }
   }
   // Opening a dialog must not reconstruct the same classic board.
   var original=typeof(PremiumGame).GetField("tiles",Fields).GetValue(view);
   typeof(PremiumGame).GetMethod("BuildBoard",Fields).Invoke(view,null);
   Check(ReferenceEquals(original,typeof(PremiumGame).GetField("tiles",Fields).GetValue(view)),"reuse classic board cells");
   foreach(var dialog in new[]{GameDialog.Pause,GameDialog.Levels,GameDialog.Instructions,GameDialog.Scores,GameDialog.Settings,GameDialog.About,GameDialog.ClearScores,GameDialog.ReplaceBoard,GameDialog.Restart,GameDialog.Win,GameDialog.Loss,GameDialog.Continue,GameDialog.VideoUnavailable}){
    var dialogOwner=new GameObject("Dialog verification");
    try{
     var dialogView=dialogOwner.AddComponent<PremiumGame>();dialogView.BuildEditorPreview(dialog,true);
     var dialogPanel=(RectTransform)typeof(PremiumGame).GetField("modalPanel",Fields).GetValue(dialogView);
     var dialogManager=dialogOwner.GetComponent<GameManager>();
     dialogManager.ToggleLargeText();dialogManager.ToggleLanguage();
     PremiumGame.FitDialog(dialogPanel);
     Check(dialogPanel.sizeDelta.y<=920,"dialog fits compact viewport: "+dialog);
     float originalHeight=dialogPanel.sizeDelta.y;PremiumGame.FitDialog(dialogPanel);
     Check(Mathf.Abs(originalHeight-dialogPanel.sizeDelta.y)<.1f,"dialog layout remains stable: "+dialog);
     foreach(Transform child in dialogPanel){
      if(!child.gameObject.activeSelf || child.name=="Celebration" || child.name=="Modal Border")continue;
      var rect=child as RectTransform;if(rect==null)continue;
      Check(Top(rect)<=dialogPanel.sizeDelta.y*.5f-20 && Bottom(rect)>=-dialogPanel.sizeDelta.y*.5f+20,"dialog padding: "+dialog+" / "+child.name);
     }
     foreach(var device in new[]{new Vector2(320,568),new Vector2(390,844),new Vector2(430,932),new Vector2(768,1024),new Vector2(1024,768),new Vector2(1920,1080)}){
      var safeArea=new Rect(16,24,device.x-32,device.y-72);
      float fit=PremiumLayout.CalculateScale((int)device.x,(int)device.y,safeArea,new Vector2(540,960));
      Check(dialogPanel.sizeDelta.x*fit<=safeArea.width && dialogPanel.sizeDelta.y*fit<=safeArea.height,"dialog fits device: "+dialog+" / "+device);
     }
    }finally{UnityEngine.Object.DestroyImmediate(dialogOwner);}
   }
   Debug.Log("PREMIUM_UI_LAYOUT_SUCCESS: 4 aspect ratios, resume variants, selection spacing and board reuse");
  }finally{UnityEngine.Object.DestroyImmediate(owner);}
 }
}
}
