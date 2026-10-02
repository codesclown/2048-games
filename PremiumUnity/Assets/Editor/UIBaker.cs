using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Premium2048;

namespace Premium2048.EditorTools {
public static class UIBaker {
    [MenuItem("2048 Premium/Bake All Screens to Scene and Prefabs")]
    public static void BakeAll() {
        EnsureDirectories();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Premium.unity", OpenSceneMode.Single);
        if (!scene.IsValid()) {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Clean existing root objects
        foreach (var rootObj in scene.GetRootGameObjects()) {
            Object.DestroyImmediate(rootObj);
        }

        // 1. Create EventSystem
        var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        // 2. Create Canvas
        var canvasGo = new GameObject("Canvas_2048Premium", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // 3. Background
        var bg = CreateUIObject("Background", canvasGo.transform);
        Stretch(bg);
        var bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = PremiumTheme.Ink;

        // 4. Safe Area Root
        var safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        Stretch(safeArea);
        safeArea.offsetMin = new Vector2(24, 48);
        safeArea.offsetMax = new Vector2(-24, -48);

        // 5. Game Manager Controller Object
        var gameManagerGo = new GameObject("2048 Premium Controller", typeof(PremiumGame), typeof(GameManager), typeof(GameController), typeof(AudioSource), typeof(AudioListener));

        // 6. Build Home Screen
        var homeScreen = BuildHomeScreen(safeArea);

        // 7. Build Game Screen
        var gameScreen = BuildGameScreen(safeArea);

        // 8. Build Dialog Layer & Prefabs
        var dialogContainer = CreateUIObject("DialogContainer", safeArea);
        Stretch(dialogContainer);
        var dialogBg = dialogContainer.gameObject.AddComponent<Image>();
        dialogBg.color = new Color(0.04f, 0.04f, 0.08f, 0.75f);
        dialogContainer.gameObject.SetActive(false);

        var levelsDialog = BuildLevelsDialog(dialogContainer);
        var scoresDialog = BuildScoresDialog(dialogContainer);
        var rulesDialog = BuildRulesDialog(dialogContainer);
        var settingsDialog = BuildSettingsDialog(dialogContainer);
        var pauseDialog = BuildPauseDialog(dialogContainer);
        var gameOverDialog = BuildGameOverDialog(dialogContainer);
        var victoryDialog = BuildVictoryDialog(dialogContainer);
        var restartDialog = BuildRestartDialog(dialogContainer);

        // Save Prefabs
        SavePrefab(homeScreen.gameObject, "Assets/Prefabs/Screens/HomeScreen.prefab");
        SavePrefab(gameScreen.gameObject, "Assets/Prefabs/Screens/GameScreen.prefab");
        SavePrefab(levelsDialog.gameObject, "Assets/Prefabs/Dialogs/LevelsDialog.prefab");
        SavePrefab(scoresDialog.gameObject, "Assets/Prefabs/Dialogs/ScoresDialog.prefab");
        SavePrefab(rulesDialog.gameObject, "Assets/Prefabs/Dialogs/RulesDialog.prefab");
        SavePrefab(settingsDialog.gameObject, "Assets/Prefabs/Dialogs/SettingsDialog.prefab");
        SavePrefab(pauseDialog.gameObject, "Assets/Prefabs/Dialogs/PauseDialog.prefab");
        SavePrefab(gameOverDialog.gameObject, "Assets/Prefabs/Dialogs/GameOverDialog.prefab");
        SavePrefab(victoryDialog.gameObject, "Assets/Prefabs/Dialogs/VictoryDialog.prefab");
        SavePrefab(restartDialog.gameObject, "Assets/Prefabs/Dialogs/RestartDialog.prefab");
        SavePrefab(canvasGo, "Assets/Prefabs/Canvas_2048Premium.prefab");

        // Keep all dialog templates in scene for visual inspection
        levelsDialog.gameObject.SetActive(false);
        scoresDialog.gameObject.SetActive(false);
        rulesDialog.gameObject.SetActive(false);
        settingsDialog.gameObject.SetActive(false);
        pauseDialog.gameObject.SetActive(false);
        gameOverDialog.gameObject.SetActive(false);
        victoryDialog.gameObject.SetActive(false);
        restartDialog.gameObject.SetActive(false);

        homeScreen.gameObject.SetActive(true);
        gameScreen.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Premium.unity");
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[UIBaker]</b> All 13 UI screens successfully baked to Scene and Assets/Prefabs/!</color>");
    }

    static void EnsureDirectories() {
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Prefabs/Screens")) Directory.CreateDirectory("Assets/Prefabs/Screens");
        if (!Directory.Exists("Assets/Prefabs/Dialogs")) Directory.CreateDirectory("Assets/Prefabs/Dialogs");
    }

    static void SavePrefab(GameObject go, string path) {
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
    }

    static RectTransform CreateUIObject(string name, Transform parent) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform BuildHomeScreen(RectTransform parent) {
        var screen = CreateUIObject("HomeScreen", parent);
        Stretch(screen);

        // Header / Logo
        var header = CreateUIObject("Header", screen);
        header.anchorMin = new Vector2(0, 0.75f);
        header.anchorMax = new Vector2(1, 1f);
        header.offsetMin = header.offsetMax = Vector2.zero;

        var mark = CreateUIObject("BrandMark", header);
        mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.7f);
        mark.sizeDelta = new Vector2(100, 100);
        var markImg = mark.gameObject.AddComponent<Image>();
        markImg.sprite = Resources.Load<Sprite>("Brand/Mark");
        markImg.color = PremiumTheme.Accent;

        var title = CreateUIObject("Title", header);
        title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.35f);
        title.sizeDelta = new Vector2(500, 50);
        var titleTxt = title.gameObject.AddComponent<Text>();
        titleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        titleTxt.fontSize = 32;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = "2048 PREMIUM";
        titleTxt.color = PremiumTheme.Text;

        var subtitle = CreateUIObject("Subtitle", header);
        subtitle.anchorMin = subtitle.anchorMax = new Vector2(0.5f, 0.15f);
        subtitle.sizeDelta = new Vector2(500, 30);
        var subTxt = subtitle.gameObject.AddComponent<Text>();
        subTxt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        subTxt.fontSize = 16;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.text = "THE NUMBER COLLECTION";
        subTxt.color = PremiumTheme.Muted;

        // Content Area / Hero Card
        var hero = CreateUIObject("HeroCard", screen);
        hero.anchorMin = new Vector2(0.05f, 0.35f);
        hero.anchorMax = new Vector2(0.95f, 0.72f);
        hero.offsetMin = hero.offsetMax = Vector2.zero;
        var heroBg = hero.gameObject.AddComponent<Image>();
        heroBg.color = PremiumTheme.Surface;

        var heroTitle = CreateUIObject("HeroTitle", hero);
        heroTitle.anchorMin = heroTitle.anchorMax = new Vector2(0.5f, 0.7f);
        heroTitle.sizeDelta = new Vector2(400, 40);
        var heroTitleTxt = heroTitle.gameObject.AddComponent<Text>();
        heroTitleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        heroTitleTxt.fontSize = 24;
        heroTitleTxt.alignment = TextAnchor.MiddleCenter;
        heroTitleTxt.text = "CLASSIC EXPEDITION";
        heroTitleTxt.color = PremiumTheme.Accent;

        var heroDesc = CreateUIObject("HeroDesc", hero);
        heroDesc.anchorMin = heroDesc.anchorMax = new Vector2(0.5f, 0.45f);
        heroDesc.sizeDelta = new Vector2(400, 60);
        var heroDescTxt = heroDesc.gameObject.AddComponent<Text>();
        heroDescTxt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        heroDescTxt.fontSize = 16;
        heroDescTxt.alignment = TextAnchor.MiddleCenter;
        heroDescTxt.text = "Swipe to merge numbers and reach the legendary 2048 tile.";
        heroDescTxt.color = PremiumTheme.Text;

        // Actions
        var newGameBtn = CreateUIObject("NewGameButton", screen);
        newGameBtn.anchorMin = new Vector2(0.05f, 0.22f);
        newGameBtn.anchorMax = new Vector2(0.95f, 0.30f);
        newGameBtn.offsetMin = newGameBtn.offsetMax = Vector2.zero;
        var ngImg = newGameBtn.gameObject.AddComponent<Image>();
        ngImg.color = PremiumTheme.Accent;
        newGameBtn.gameObject.AddComponent<Button>();

        var ngTxt = CreateUIObject("Label", newGameBtn);
        Stretch(ngTxt);
        var ngLabel = ngTxt.gameObject.AddComponent<Text>();
        ngLabel.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        ngLabel.fontSize = 22;
        ngLabel.alignment = TextAnchor.MiddleCenter;
        ngLabel.text = "NEW GAME";
        ngLabel.color = PremiumTheme.Ink;

        // Bottom Actions Row (How to Play, High Scores, Settings)
        var bottomBar = CreateUIObject("BottomBar", screen);
        bottomBar.anchorMin = new Vector2(0.05f, 0.05f);
        bottomBar.anchorMax = new Vector2(0.95f, 0.18f);
        bottomBar.offsetMin = bottomBar.offsetMax = Vector2.zero;

        var rulesBtn = CreateButton("HowToPlayButton", bottomBar, new Vector2(0, 0), new Vector2(0.31f, 1), "HOW TO PLAY");
        var scoresBtn = CreateButton("HighScoresButton", bottomBar, new Vector2(0.345f, 0), new Vector2(0.655f, 1), "HIGH SCORES");
        var settingsBtn = CreateButton("SettingsButton", bottomBar, new Vector2(0.69f, 0), new Vector2(1, 1), "SETTINGS");

        return screen;
    }

    static RectTransform BuildGameScreen(RectTransform parent) {
        var screen = CreateUIObject("GameScreen", parent);
        Stretch(screen);

        // Top Stats Bar
        var topBar = CreateUIObject("TopBar", screen);
        topBar.anchorMin = new Vector2(0.05f, 0.85f);
        topBar.anchorMax = new Vector2(0.95f, 0.98f);
        topBar.offsetMin = topBar.offsetMax = Vector2.zero;

        var scoreCard = CreateCard("ScoreCard", topBar, new Vector2(0, 0), new Vector2(0.48f, 1), "SCORE", "0");
        var bestCard = CreateCard("BestCard", topBar, new Vector2(0.52f, 0), new Vector2(1, 1), "BEST", "0");

        // Board Area Container
        var boardContainer = CreateUIObject("BoardContainer", screen);
        boardContainer.anchorMin = new Vector2(0.05f, 0.25f);
        boardContainer.anchorMax = new Vector2(0.95f, 0.80f);
        boardContainer.offsetMin = boardContainer.offsetMax = Vector2.zero;
        var boardBg = boardContainer.gameObject.AddComponent<Image>();
        boardBg.color = PremiumTheme.Surface;

        var grid = CreateUIObject("Grid", boardContainer);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.sizeDelta = new Vector2(400, 400);

        // Power Ups Bar
        var powerBar = CreateUIObject("PowerUpsBar", screen);
        powerBar.anchorMin = new Vector2(0.05f, 0.08f);
        powerBar.anchorMax = new Vector2(0.95f, 0.20f);
        powerBar.offsetMin = powerBar.offsetMax = Vector2.zero;

        var undoBtn = CreatePowerButton("UndoButton", powerBar, new Vector2(0, 0), new Vector2(0.31f, 1), "UNDO", "50 🪙");
        var swapBtn = CreatePowerButton("SwapButton", powerBar, new Vector2(0.345f, 0), new Vector2(0.655f, 1), "SWAP", "100 🪙");
        var deleteBtn = CreatePowerButton("DeleteButton", powerBar, new Vector2(0.69f, 0), new Vector2(1, 1), "DELETE", "150 🪙");

        return screen;
    }

    static RectTransform CreateCard(string name, Transform parent, Vector2 min, Vector2 max, string title, string val) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.color = PremiumTheme.Surface;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0, 0.55f); tGo.anchorMax = new Vector2(1, 1f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Muted;

        var vGo = CreateUIObject("Value", card);
        vGo.anchorMin = new Vector2(0, 0); vGo.anchorMax = new Vector2(1, 0.55f);
        vGo.offsetMin = vGo.offsetMax = Vector2.zero;
        var v = vGo.gameObject.AddComponent<Text>();
        v.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        v.fontSize = 26; v.alignment = TextAnchor.MiddleCenter; v.text = val; v.color = PremiumTheme.Text;

        return card;
    }

    static RectTransform CreateButton(string name, Transform parent, Vector2 min, Vector2 max, string label) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.color = PremiumTheme.Surface;
        btn.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", btn);
        Stretch(lGo);
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 14; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Text;

        return btn;
    }

    static RectTransform CreatePowerButton(string name, Transform parent, Vector2 min, Vector2 max, string label, string cost) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.color = PremiumTheme.Surface;
        btn.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", btn);
        lGo.anchorMin = new Vector2(0, 0.45f); lGo.anchorMax = new Vector2(1, 1f);
        lGo.offsetMin = lGo.offsetMax = Vector2.zero;
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 16; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Accent;

        var cGo = CreateUIObject("Cost", btn);
        cGo.anchorMin = new Vector2(0, 0); cGo.anchorMax = new Vector2(1, 0.45f);
        cGo.offsetMin = cGo.offsetMax = Vector2.zero;
        var c = cGo.gameObject.AddComponent<Text>();
        c.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        c.fontSize = 13; c.alignment = TextAnchor.MiddleCenter; c.text = cost; c.color = PremiumTheme.Muted;

        return btn;
    }

    static RectTransform CreateDialogBase(string name, Transform parent, string title) {
        var dialog = CreateUIObject(name, parent);
        dialog.anchorMin = new Vector2(0.08f, 0.15f);
        dialog.anchorMax = new Vector2(0.92f, 0.85f);
        dialog.offsetMin = dialog.offsetMax = Vector2.zero;
        var img = dialog.gameObject.AddComponent<Image>();
        img.color = PremiumTheme.Surface;

        var header = CreateUIObject("Header", dialog);
        header.anchorMin = new Vector2(0, 0.85f);
        header.anchorMax = new Vector2(1, 1f);
        header.offsetMin = header.offsetMax = Vector2.zero;
        var t = header.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Accent;

        return dialog;
    }

    static RectTransform BuildLevelsDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_Levels", parent, "CHOOSE YOUR PACE");
        CreateButton("Level4x4", d, new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.78f), "CLASSIC 4x4");
        CreateButton("Level5x5", d, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.56f), "ADVANCED 5x5");
        CreateButton("Level6x6", d, new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.34f), "EXPERT 6x6");
        CreateButton("BackButton", d, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.12f), "BACK");
        return d;
    }

    static RectTransform BuildScoresDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_HighScores", parent, "HIGH SCORES");
        CreateCard("Score4x4", d, new Vector2(0.1f, 0.64f), new Vector2(0.9f, 0.80f), "CLASSIC 4x4", "24,860");
        CreateCard("Score5x5", d, new Vector2(0.1f, 0.44f), new Vector2(0.9f, 0.60f), "ADVANCED 5x5", "68,420");
        CreateCard("Score6x6", d, new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.40f), "EXPERT 6x6", "142,890");
        CreateButton("CloseButton", d, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.16f), "CLOSE");
        return d;
    }

    static RectTransform BuildRulesDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_HowToPlay", parent, "HOW TO PLAY");
        var content = CreateUIObject("Content", d);
        content.anchorMin = new Vector2(0.1f, 0.20f); content.anchorMax = new Vector2(0.9f, 0.82f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var txt = content.gameObject.AddComponent<Text>();
        txt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        txt.fontSize = 16; txt.alignment = TextAnchor.UpperLeft;
        txt.text = "• Swipe up, down, left, or right to move tiles.\n\n• Tiles with the same number merge into one.\n\n• Reach 2048 to win!\n\n• Use Undo, Swap, and Delete to get out of tight spots.";
        txt.color = PremiumTheme.Text;
        CreateButton("CloseButton", d, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.16f), "CLOSE");
        return d;
    }

    static RectTransform BuildSettingsDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_Settings", parent, "SETTINGS");
        CreateButton("ToggleSound", d, new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.78f), "SOUND: ON");
        CreateButton("ToggleHaptics", d, new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.62f), "HAPTICS: ON");
        CreateButton("ToggleLanguage", d, new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.46f), "LANGUAGE: ENGLISH");
        CreateButton("ToggleContrast", d, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.30f), "HIGH CONTRAST: OFF");
        CreateButton("CloseButton", d, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.14f), "CLOSE");
        return d;
    }

    static RectTransform BuildPauseDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_Pause", parent, "GAME PAUSED");
        CreateButton("ResumeButton", d, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), "RESUME");
        CreateButton("RestartButton", d, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.58f), "RESTART");
        CreateButton("HomeButton", d, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.38f), "HOME MENU");
        return d;
    }

    static RectTransform BuildGameOverDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_GameOver", parent, "GAME OVER");
        CreateCard("FinalScoreCard", d, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.75f), "FINAL SCORE", "12,480");
        CreateButton("PlayAgainButton", d, new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.40f), "PLAY AGAIN");
        CreateButton("HomeButton", d, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f), "HOME MENU");
        return d;
    }

    static RectTransform BuildVictoryDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_Victory", parent, "VICTORY! 👑");
        CreateCard("VictoryScoreCard", d, new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.78f), "YOU REACHED 2048!", "Score: 20,480");
        CreateButton("KeepGoingButton", d, new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.48f), "KEEP GOING");
        CreateButton("PlayAgainButton", d, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.30f), "PLAY AGAIN");
        CreateButton("HomeButton", d, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.14f), "HOME MENU");
        return d;
    }

    static RectTransform BuildRestartDialog(Transform parent) {
        var d = CreateDialogBase("Dialog_Restart", parent, "RESTART GAME?");
        var txt = CreateUIObject("Desc", d);
        txt.anchorMin = new Vector2(0.1f, 0.45f); txt.anchorMax = new Vector2(0.9f, 0.75f);
        txt.offsetMin = txt.offsetMax = Vector2.zero;
        var t = txt.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 18; t.alignment = TextAnchor.MiddleCenter;
        t.text = "Are you sure you want to restart?\nCurrent game progress will be lost.";
        t.color = PremiumTheme.Text;

        CreateButton("ConfirmRestartButton", d, new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.38f), "RESTART");
        CreateButton("CancelButton", d, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.20f), "CANCEL");
        return d;
    }
}
}
