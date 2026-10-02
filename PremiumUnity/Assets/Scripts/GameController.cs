using System;
using UnityEngine;
namespace Premium2048 {
// Single-finger board gestures. Multi-touch and focus loss cancel pending input.
[DisallowMultipleComponent]
public sealed class GameController : MonoBehaviour {
 bool dragging;int finger=-1;Vector2 start;
 public void Cancel(){dragging=false;finger=-1;}
 public void Poll(RectTransform board,bool selecting,Action<Direction> move,Action<Vector2> select,Action restart) {
#if UNITY_EDITOR || UNITY_STANDALONE
  bool modifier=Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand)||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
  if(!modifier) {
   if(Input.GetKeyDown(KeyCode.R)){Cancel();restart();return;}
   if(!selecting) {
    if(Pressed(KeyCode.UpArrow,KeyCode.W,KeyCode.K)){Cancel();move(Direction.Up);return;}
    else if(Pressed(KeyCode.RightArrow,KeyCode.D,KeyCode.L)){Cancel();move(Direction.Right);return;}
    else if(Pressed(KeyCode.DownArrow,KeyCode.S,KeyCode.J)){Cancel();move(Direction.Down);return;}
    else if(Pressed(KeyCode.LeftArrow,KeyCode.A,KeyCode.H)){Cancel();move(Direction.Left);return;}
   }
  }
#endif
  if(Input.touchCount>1){Cancel();return;}
  if(Input.touchCount==1) {
   var touch=Input.GetTouch(0);
   if(touch.phase==TouchPhase.Began){finger=touch.fingerId;Begin(board,touch.position);}
   if(touch.fingerId==finger && touch.phase==TouchPhase.Ended){End(touch.position,selecting,move,select);finger=-1;}
   if(touch.phase==TouchPhase.Canceled)Cancel();
  }
#if UNITY_EDITOR || UNITY_STANDALONE
  else {
   if(Input.GetMouseButtonDown(0))Begin(board,Input.mousePosition);
   if(Input.GetMouseButtonUp(0))End(Input.mousePosition,selecting,move,select);
  }
#endif
 }
 static bool Pressed(KeyCode a,KeyCode b,KeyCode c){return Input.GetKeyDown(a)||Input.GetKeyDown(b)||Input.GetKeyDown(c);}
 void Begin(RectTransform board,Vector2 point){start=point;dragging=RectTransformUtility.RectangleContainsScreenPoint(board,point);}
 void End(Vector2 point,bool selecting,Action<Direction> move,Action<Vector2> select) {
  if(!dragging)return;dragging=false;Vector2 delta=point-start;
  float dpi=Screen.dpi>0?Mathf.Clamp(Screen.dpi,120,640):160;
  float threshold=Mathf.Max(14,dpi*0.08f);
  if(selecting){if(delta.magnitude<threshold*1.4f)select(point);return;}
  if(Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.y))<threshold)return;
  move(Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?(delta.x>0?Direction.Right:Direction.Left):(delta.y>0?Direction.Up:Direction.Down));
 }
 void OnApplicationFocus(bool focus){if(!focus)Cancel();}
 void OnApplicationPause(bool paused){if(paused)Cancel();}
}
}
