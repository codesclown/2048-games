#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace Premium2048 {
// Samples the real view animation, not a parallel easing implementation.
public sealed class PremiumMotionVerification : MonoBehaviour {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 PremiumGame game;GameManager manager;int stage,frames,scenario,destination;bool moved,popped,spawned,highlighted;double started;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-premiumMotionSmoke")>=0)new GameObject("Motion QA").AddComponent<PremiumMotionVerification>();}
 void Awake(){started=Time.realtimeSinceStartupAsDouble;}
 T Get<T>(string field){return (T)typeof(PremiumGame).GetField(field,Flags).GetValue(game);}
 void Call(string method,params object[] args){typeof(PremiumGame).GetMethod(method,Flags).Invoke(game,args);}
 Vector2 Position(int cell){return (Vector2)typeof(PremiumGame).GetMethod("CellPosition",Flags).Invoke(game,new object[]{cell});}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);Debug.Log("MOTION QA PASS: "+message);}
 void Update(){try{
  if(Time.realtimeSinceStartupAsDouble-started>40)throw new Exception("Motion verification timed out");
  if(game==null)game=FindFirstObjectByType<PremiumGame>();if(game==null || !Get<bool>("ready"))return;
  manager=Get<GameManager>("manager");
  if(stage==0){stage++;return;}
  if(stage==1){
   if(manager.ReducedMotion)manager.ToggleReducedMotion();int dimension=4+scenario/4;var direction=(Direction)(scenario%4);Call("NewGame",dimension);
   manager.Session.board=new Board(dimension);
   int a,b;
   if(direction==Direction.Right){a=0;b=1;destination=dimension-1;}
   else if(direction==Direction.Left){a=dimension-1;b=dimension-2;destination=0;}
   else if(direction==Direction.Up){a=(dimension-1)*dimension;b=(dimension-2)*dimension;destination=0;}
   else{a=0;b=dimension;destination=(dimension-1)*dimension;}
   manager.Session.board.cells[a]=manager.Session.board.cells[b]=2;Call("Refresh",true);frames=0;moved=popped=spawned=highlighted=false;
   Debug.Log("MOTION QA CASE: "+dimension+"x"+dimension+" "+direction);Call("Move",direction);var ghosts=Get<List<RectTransform>>("activeGhosts");
   Check(ghosts.Count==2,"both pre-merge tiles are visible during the slide");
   Check(ghosts.TrueForAll(g=>g.GetSiblingIndex()>Get<Image[]>("tiles")[0].transform.GetSiblingIndex()),"moving tiles render above board cells after rebuilding a board");
   for(int i=0;i<ghosts.Count;i++)Check(Vector2.Distance(ghosts[i].anchoredPosition,Position(manager.Session.board.motions[i].from))<.01f,"slide begins at its source without a first-frame jump");
   stage++;return;
  }
  if(stage==2){
   var ghosts=Get<List<RectTransform>>("activeGhosts");
   if(ghosts.Count>0){frames++;for(int i=0;i<ghosts.Count;i++){var motion=manager.Session.board.motions[i];if(Vector2.Distance(ghosts[i].anchoredPosition,Position(motion.to))>1 && Vector2.Distance(ghosts[i].anchoredPosition,Position(motion.from))>1)moved=true;}return;}
   highlighted|=Get<PremiumMergeFeedback>("mergeFeedback")!=null && Get<PremiumMergeFeedback>("mergeFeedback").IsPlaying;
   var tiles=Get<Image[]>("tiles");popped|=tiles[destination].transform.localScale.x>1.08f;
   int spawn=manager.Session.board.spawned;spawned|=spawn>=0 && tiles[spawn].transform.localScale.x<.9f;
   if(Get<bool>("animating"))return;
   Check(frames>=5,"slide retains at least five visible interpolation frames at 60fps");Check(moved,"tiles travel through intermediate board positions");
   Check(popped,"merged tile has a visible settle pulse");Check(highlighted,"merged tiles receive a visible pooled highlight");Check(spawned,"new tile grows in after contact");
   Check(manager.Session.board.cells[destination]==4,"merge commits the correct value once");
   foreach(var t in tiles)Check(Mathf.Abs(t.transform.localScale.x-1)<.001f,"tile scale settles exactly to one");
   if(++scenario<12){stage=1;return;}
   Call("ShowHome");stage++;return;
  }
  if(stage==3){var home=Get<GameObject>("home");var motion=home.GetComponent<PremiumHomeMotion>();Check(motion!=null && motion.IsRevealing,"home plays a staggered entrance when returning from gameplay");stage++;return;}
  if(stage==4){var home=Get<GameObject>("home");if(home.GetComponent<PremiumHomeMotion>().IsRevealing)return;
   foreach(var group in home.GetComponentsInChildren<CanvasGroup>())Check(Mathf.Abs(group.alpha-1)<.001f,"home elements finish fully visible");
   manager.ToggleReducedMotion();stage++;return;}
  if(stage==5){Check(!Get<GameObject>("home").GetComponent<PremiumHomeMotion>().IsRevealing,"reduced motion disables home entrance");
   Call("NewGame",4);manager.Session.board=new Board(4);manager.Session.board.cells[0]=manager.Session.board.cells[1]=2;Call("Refresh",true);Call("Move",Direction.Right);
   Check(!Get<bool>("animating") && manager.Session.board.cells[3]==4,"reduced motion still merges correctly without waiting for animation");
   manager.ToggleReducedMotion();Call("ShowExpeditionMap");stage++;return;}
  if(stage==6){
   var map=Get<GameObject>("expeditionMap");var group=map.GetComponent<CanvasGroup>();
   Check(map.activeSelf && map.GetComponent<PremiumPanelMotion>()!=null && group!=null,"Expedition map plays a page entrance");
   if(group.alpha<.999f)return;
   Call("ShowInstructions");stage++;return;
  }
  if(stage==7){
   var panel=Get<RectTransform>("modalPanel");var group=panel.GetComponent<CanvasGroup>();
   Check(Get<GameObject>("modal").activeSelf && panel.GetComponent<PremiumPanelMotion>()!=null,"instructions plays a dialog entrance");
   if(group.alpha<.999f)return;
   manager.ToggleReducedMotion();Call("ShowSettings");
   Check(panel.GetComponent<CanvasGroup>().alpha==1 && panel.localScale==Vector3.one,"reduced motion opens dialogs fully visible in the first frame");
   Debug.Log("PREMIUM_MOTION_SUCCESS");Finish(0);stage++;}
 }catch(Exception e){Debug.LogError("PREMIUM_MOTION_FAILED: "+e);Finish(1);enabled=false;}}
 static void Finish(int code){
#if UNITY_EDITOR
 UnityEditor.EditorApplication.Exit(code);
#else
 Application.Quit(code);
#endif
 }
}
}
#endif
