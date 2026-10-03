using UnityEditor;
using UnityEditor.SceneManagement;
namespace Premium2048.Editor {
// Generated preview canvases follow the source view after compilation and Play.
[InitializeOnLoad]
public static class PremiumPreviewSync {
 static bool queued;
 static PremiumPreviewSync(){
  EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)Queue();};
  EditorSceneManager.sceneOpened+=(scene,mode)=>Queue();
  Queue();
 }
 static void Queue(){if(queued || UnityEngine.Application.isBatchMode)return;queued=true;EditorApplication.delayCall+=Refresh;}
 static void Refresh(){
  queued=false;if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  if(EditorApplication.isCompiling || EditorApplication.isUpdating){Queue();return;}
  UIBaker.RefreshOpenPreview();
 }
 [MenuItem("2048 Premium/Refresh Current Preview")]
 public static void RefreshCurrent(){if(!EditorApplication.isPlayingOrWillChangePlaymode)UIBaker.RefreshOpenPreview();}
}
}
