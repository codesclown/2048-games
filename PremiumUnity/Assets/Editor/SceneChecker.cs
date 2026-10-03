using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048.EditorTools {
public static class SceneChecker {
    public static void RebuildAndVerify() { Premium2048.Editor.UIBaker.BakeAll(); VerifyAll(); }

    [MenuItem("2048 Premium/Verify All Scenes")]
    public static void VerifyAll() {
        string[] scenes = {
            "01_HomeScreen",
            "02_GameScreen_4x4",
            "03_Dialog_Levels",
            "04_Dialog_HighScores",
            "05_Dialog_HowToPlay",
            "06_Dialog_Settings",
            "07_Dialog_Pause",
            "08_Dialog_GameOver",
            "09_Dialog_Victory",
            "10_Dialog_Restart",
            "11_Dialog_ClearProgress",
            "12_Dialog_Continue",
            "13_Dialog_NewBoard",
            "14_Dialog_About"
        };

        string outDir = Path.GetFullPath("Docs/Previews/AllScreens");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        int width = 1080;
        int height = 2340;

        foreach (var sceneName in scenes) {
            string path = "Assets/Scenes/" + sceneName + ".unity";
            if (!File.Exists(path)) {
                Debug.LogWarning("Scene not found: " + path);
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            
            // Setup a render camera for the canvas
            var camGo = new GameObject("RenderCamera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.14f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = height * .5f;
            cam.aspect = (float)width / height;
            cam.transform.position = new Vector3(0, 0, -10);

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.GetComponent<CanvasScaler>().enabled=false;
                var root=(RectTransform)canvas.transform;
                root.sizeDelta=new Vector2(width,height);
                root.position=Vector3.zero;root.localScale=Vector3.one;
                var safe=canvas.GetComponentInChildren<Premium2048.PremiumSafeArea>();
                if(safe!=null){safe.enabled=false;var area=(RectTransform)safe.transform;area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=new Vector2(20,50);area.offsetMax=new Vector2(-20,-50);}
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            canvas.GetComponent<Premium2048.PremiumPreviewLayout>()?.Fit();Canvas.ForceUpdateCanvases();
            foreach(var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)){if(text.font==null)throw new System.Exception("Missing font in "+sceneName+": "+text.name);}
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string outPath = Path.Combine(outDir, sceneName + "_preview.png");
            File.WriteAllBytes(outPath, bytes);

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);

            Debug.Log("[SceneChecker] Verified and captured: " + outPath);
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Premium.unity",OpenSceneMode.Single);
        Debug.Log("<color=green><b>[SceneChecker]</b> All scenes successfully verified and captured!</color>");
    }
}
}
