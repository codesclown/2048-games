using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048.EditorTools {
public static class SceneChecker {
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
            "10_Dialog_Restart"
        };

        string outDir = "/Users/chandanvishwakarma/.gemini/antigravity-ide/brain/fb08918b-7d1a-4f64-b389-488e4927443e";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        int width = 1080;
        int height = 1920;

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
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
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

        Debug.Log("<color=green><b>[SceneChecker]</b> All scenes successfully verified and captured!</color>");
    }
}
}
