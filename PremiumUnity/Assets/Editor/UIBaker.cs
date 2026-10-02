using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Premium2048;

namespace Premium2048.EditorTools {
public static class UIBaker {

    static Sprite CardSprite => PremiumTheme.CreateRoundedCard(128, 28, PremiumTheme.Surface, PremiumTheme.Border, 1.8f);
    static Sprite CardElevatedSprite => PremiumTheme.CreateRoundedCard(128, 28, PremiumTheme.SurfaceElevated, PremiumTheme.BorderLight, 1.8f);
    static Sprite DialogSprite => PremiumTheme.CreateRoundedCard(128, 32, PremiumTheme.Surface, PremiumTheme.Border, 2f);
    static Sprite GoldBtnSprite => PremiumTheme.CreateGoldButton(128, 28);
    static Sprite PillSprite => PremiumTheme.CreateRoundedCard(64, 32, PremiumTheme.SurfaceElevated, PremiumTheme.BorderLight, 1.2f);
    static Sprite DangerBtnSprite => PremiumTheme.CreateRoundedCard(128, 28, PremiumTheme.Coral, new UnityEngine.Color(1f, 0.45f, 0.55f, 1f), 1.8f);

    [MenuItem("2048 Premium/Bake All Screens to Dedicated Scenes and Prefabs")]
    public static void BakeAll() {
        EnsureDirectories();

        BakeMasterScene();

        BakeIndividualScene("01_HomeScreen", BuildHomeScreen);
        BakeIndividualScene("02_GameScreen_4x4", BuildGameScreen);
        BakeIndividualScene("03_Dialog_Levels", BuildLevelsDialog);
        BakeIndividualScene("04_Dialog_HighScores", BuildScoresDialog);
        BakeIndividualScene("05_Dialog_HowToPlay", BuildRulesDialog);
        BakeIndividualScene("06_Dialog_Settings", BuildSettingsDialog);
        BakeIndividualScene("07_Dialog_Pause", BuildPauseDialog);
        BakeIndividualScene("08_Dialog_GameOver", BuildGameOverDialog);
        BakeIndividualScene("09_Dialog_Victory", BuildVictoryDialog);
        BakeIndividualScene("10_Dialog_Restart", BuildRestartDialog);

        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[UIBaker]</b> All dedicated scenes and prefabs successfully baked into Assets/Scenes/ and Assets/Prefabs/!</color>");
    }

    static void EnsureDirectories() {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Prefabs/Screens")) Directory.CreateDirectory("Assets/Prefabs/Screens");
        if (!Directory.Exists("Assets/Prefabs/Dialogs")) Directory.CreateDirectory("Assets/Prefabs/Dialogs");
    }

    static void BakeIndividualScene(string sceneName, System.Func<RectTransform, RectTransform> screenBuilder) {
        var scenePath = "Assets/Scenes/" + sceneName + ".unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        var canvasGo = new GameObject("Canvas_" + sceneName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = CreateUIObject("Background", canvasGo.transform);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = PremiumTheme.Background;

        var safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        Stretch(safeArea);
        safeArea.offsetMin = new Vector2(36, 64);
        safeArea.offsetMax = new Vector2(-36, -64);

        var screenRt = screenBuilder(safeArea);
        screenRt.gameObject.SetActive(true);

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    static void BakeMasterScene() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        var canvasGo = new GameObject("Canvas_2048Premium", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = CreateUIObject("Background", canvasGo.transform);
        Stretch(bg);
        var bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = PremiumTheme.Background;

        var safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        Stretch(safeArea);
        safeArea.offsetMin = new Vector2(36, 64);
        safeArea.offsetMax = new Vector2(-36, -64);

        new GameObject("2048 Premium Controller", typeof(PremiumGame), typeof(GameManager), typeof(GameController), typeof(AudioSource), typeof(AudioListener));

        var homeScreen = BuildHomeScreen(safeArea);
        var gameScreen = BuildGameScreen(safeArea);

        var dialogContainer = CreateUIObject("DialogContainer", safeArea);
        Stretch(dialogContainer);
        var dialogBg = dialogContainer.gameObject.AddComponent<Image>();
        dialogBg.color = new UnityEngine.Color(0.04f, 0.05f, 0.09f, 0.88f);
        dialogContainer.gameObject.SetActive(false);

        var levelsDialog = BuildLevelsDialog(dialogContainer);
        var scoresDialog = BuildScoresDialog(dialogContainer);
        var rulesDialog = BuildRulesDialog(dialogContainer);
        var settingsDialog = BuildSettingsDialog(dialogContainer);
        var pauseDialog = BuildPauseDialog(dialogContainer);
        var gameOverDialog = BuildGameOverDialog(dialogContainer);
        var victoryDialog = BuildVictoryDialog(dialogContainer);
        var restartDialog = BuildRestartDialog(dialogContainer);

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

    // ==========================================
    // 1. HOME SCREEN
    // ==========================================
    public static RectTransform BuildHomeScreen(RectTransform parent) {
        var screen = CreateUIObject("HomeScreen", parent);
        Stretch(screen);

        // Header Section (Top 68% - 98%)
        var header = CreateUIObject("Header", screen);
        header.anchorMin = new Vector2(0, 0.68f);
        header.anchorMax = new Vector2(1, 0.98f);
        header.offsetMin = header.offsetMax = Vector2.zero;

        // Monogram Logo Badge
        var mark = CreateUIObject("BrandMarkBadge", header);
        mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.72f);
        mark.sizeDelta = new Vector2(104, 104);
        var markBg = mark.gameObject.AddComponent<Image>();
        markBg.sprite = GoldBtnSprite;
        markBg.type = Image.Type.Sliced;

        var markTxt = CreateUIObject("Label", mark);
        Stretch(markTxt);
        var mLabel = markTxt.gameObject.AddComponent<Text>();
        mLabel.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        mLabel.fontSize = 26;
        mLabel.alignment = TextAnchor.MiddleCenter;
        mLabel.text = "2 0\n4 8";
        mLabel.lineSpacing = 0.85f;
        mLabel.color = PremiumTheme.Ink;

        var title = CreateUIObject("Title", header);
        title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.32f);
        title.sizeDelta = new Vector2(600, 52);
        var titleTxt = title.gameObject.AddComponent<Text>();
        titleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        titleTxt.fontSize = 38;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = "2048 PREMIUM";
        titleTxt.color = PremiumTheme.Text;

        var subtitle = CreateUIObject("Subtitle", header);
        subtitle.anchorMin = subtitle.anchorMax = new Vector2(0.5f, 0.08f);
        subtitle.sizeDelta = new Vector2(600, 26);
        var subTxt = subtitle.gameObject.AddComponent<Text>();
        subTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        subTxt.fontSize = 13;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.text = "✦  THE NUMBER COLLECTION  ✦";
        subTxt.color = PremiumTheme.Gold;

        // Live Expedition Hero Card (Center 32% - 66%)
        var hero = CreateUIObject("HeroCard", screen);
        hero.anchorMin = new Vector2(0.04f, 0.32f);
        hero.anchorMax = new Vector2(0.96f, 0.66f);
        hero.offsetMin = hero.offsetMax = Vector2.zero;
        var heroBg = hero.gameObject.AddComponent<Image>();
        heroBg.sprite = CardSprite;
        heroBg.type = Image.Type.Sliced;

        // Mini preview grid inside hero card
        var miniGrid = CreateUIObject("MiniGrid", hero);
        miniGrid.anchorMin = new Vector2(0.06f, 0.12f);
        miniGrid.anchorMax = new Vector2(0.44f, 0.88f);
        miniGrid.offsetMin = miniGrid.offsetMax = Vector2.zero;
        var mgBg = miniGrid.gameObject.AddComponent<Image>();
        mgBg.sprite = PremiumTheme.CreateRoundedCard(64, 16, PremiumTheme.Background, PremiumTheme.Border, 1.2f);
        mgBg.type = Image.Type.Sliced;

        for (int row = 0; row < 2; row++) {
            for (int col = 0; col < 2; col++) {
                var miniTile = CreateUIObject($"Tile_{row}_{col}", miniGrid);
                miniTile.anchorMin = new Vector2(0.08f + col * 0.46f, 0.08f + (1 - row) * 0.46f);
                miniTile.anchorMax = new Vector2(0.46f + col * 0.46f, 0.46f + (1 - row) * 0.46f);
                miniTile.offsetMin = miniTile.offsetMax = Vector2.zero;
                var mtImg = miniTile.gameObject.AddComponent<Image>();
                mtImg.sprite = PremiumTheme.CreateRoundedCard(64, 14, UnityEngine.Color.white, UnityEngine.Color.clear, 0);
                mtImg.type = Image.Type.Sliced;
                int val = (row == 0 && col == 1) ? 4 : ((row == 1 && col == 0) ? 2 : 0);
                PremiumTheme.TileColors(val, out var c1, out _);
                mtImg.color = c1;
                if (val > 0) {
                    var tLabel = CreateUIObject("T", miniTile);
                    Stretch(tLabel);
                    var tl = tLabel.gameObject.AddComponent<Text>();
                    tl.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
                    tl.fontSize = 22; tl.alignment = TextAnchor.MiddleCenter;
                    tl.text = val.ToString();
                    tl.color = PremiumTheme.TileText(val);
                }
            }
        }

        // Hero Text Right Side
        var heroTitle = CreateUIObject("HeroTitle", hero);
        heroTitle.anchorMin = new Vector2(0.48f, 0.66f);
        heroTitle.anchorMax = new Vector2(0.96f, 0.90f);
        heroTitle.offsetMin = heroTitle.offsetMax = Vector2.zero;
        var heroTitleTxt = heroTitle.gameObject.AddComponent<Text>();
        heroTitleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        heroTitleTxt.fontSize = 18;
        heroTitleTxt.alignment = TextAnchor.MiddleLeft;
        heroTitleTxt.text = "ACTIVE EXPEDITION";
        heroTitleTxt.color = PremiumTheme.Gold;

        var heroDesc = CreateUIObject("HeroDesc", hero);
        heroDesc.anchorMin = new Vector2(0.48f, 0.36f);
        heroDesc.anchorMax = new Vector2(0.96f, 0.64f);
        heroDesc.offsetMin = heroDesc.offsetMax = Vector2.zero;
        var heroDescTxt = heroDesc.gameObject.AddComponent<Text>();
        heroDescTxt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        heroDescTxt.fontSize = 15;
        heroDescTxt.lineSpacing = 1.3f;
        heroDescTxt.alignment = TextAnchor.MiddleLeft;
        heroDescTxt.text = "Board: <color=#FFFFFF>Classic 4×4</color>\nScore: <color=#FFC837>2,480</color>";
        heroDescTxt.color = PremiumTheme.Muted;

        var resumeBtn = CreateUIObject("ResumeCardBtn", hero);
        resumeBtn.anchorMin = new Vector2(0.48f, 0.10f);
        resumeBtn.anchorMax = new Vector2(0.96f, 0.32f);
        resumeBtn.offsetMin = resumeBtn.offsetMax = Vector2.zero;
        var rImg = resumeBtn.gameObject.AddComponent<Image>();
        rImg.sprite = PillSprite;
        rImg.type = Image.Type.Sliced;
        rImg.color = PremiumTheme.SurfaceElevated;
        resumeBtn.gameObject.AddComponent<Button>();

        var rLabel = CreateUIObject("Label", resumeBtn);
        Stretch(rLabel);
        var rl = rLabel.gameObject.AddComponent<Text>();
        rl.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        rl.fontSize = 13;
        rl.alignment = TextAnchor.MiddleCenter;
        rl.text = "RESUME GAME  ➔";
        rl.color = PremiumTheme.Text;

        // Large Gold NEW GAME Button
        var newGameBtn = CreateUIObject("NewGameButton", screen);
        newGameBtn.anchorMin = new Vector2(0.04f, 0.17f);
        newGameBtn.anchorMax = new Vector2(0.96f, 0.28f);
        newGameBtn.offsetMin = newGameBtn.offsetMax = Vector2.zero;
        var ngImg = newGameBtn.gameObject.AddComponent<Image>();
        ngImg.sprite = GoldBtnSprite;
        ngImg.type = Image.Type.Sliced;
        newGameBtn.gameObject.AddComponent<Button>();

        var ngTxt = CreateUIObject("Label", newGameBtn);
        Stretch(ngTxt);
        var ngLabel = ngTxt.gameObject.AddComponent<Text>();
        ngLabel.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        ngLabel.fontSize = 24;
        ngLabel.alignment = TextAnchor.MiddleCenter;
        ngLabel.text = "⚡  NEW GAME";
        ngLabel.color = PremiumTheme.Ink;

        // Bottom 3 Action Glass Chips (How to Play, High Scores, Settings)
        var bottomBar = CreateUIObject("BottomBar", screen);
        bottomBar.anchorMin = new Vector2(0.04f, 0.04f);
        bottomBar.anchorMax = new Vector2(0.96f, 0.14f);
        bottomBar.offsetMin = bottomBar.offsetMax = Vector2.zero;

        CreateActionChip("HowToPlayButton", bottomBar, new Vector2(0, 0), new Vector2(0.31f, 1), "RULES", IconKind.Menu);
        CreateActionChip("HighScoresButton", bottomBar, new Vector2(0.345f, 0), new Vector2(0.655f, 1), "SCORES", IconKind.Trophy);
        CreateActionChip("SettingsButton", bottomBar, new Vector2(0.69f, 0), new Vector2(1, 1), "SETTINGS", IconKind.Settings);

        return screen;
    }

    // ==========================================
    // 2. GAMEPLAY SCREEN (4x4)
    // ==========================================
    public static RectTransform BuildGameScreen(RectTransform parent) {
        var screen = CreateUIObject("GameScreen", parent);
        Stretch(screen);

        // Top Navigation & Stats Bar
        var topBar = CreateUIObject("TopBar", screen);
        topBar.anchorMin = new Vector2(0.04f, 0.85f);
        topBar.anchorMax = new Vector2(0.96f, 0.98f);
        topBar.offsetMin = topBar.offsetMax = Vector2.zero;

        var pauseBtn = CreateUIObject("PauseButton", topBar);
        pauseBtn.anchorMin = new Vector2(0, 0.12f);
        pauseBtn.anchorMax = new Vector2(0.18f, 0.88f);
        pauseBtn.offsetMin = pauseBtn.offsetMax = Vector2.zero;
        var pbImg = pauseBtn.gameObject.AddComponent<Image>();
        pbImg.sprite = PillSprite;
        pbImg.type = Image.Type.Sliced;
        pbImg.color = PremiumTheme.SurfaceElevated;
        pauseBtn.gameObject.AddComponent<Button>();

        var pIconGo = CreateUIObject("Icon", pauseBtn);
        pIconGo.anchorMin = pIconGo.anchorMax = new Vector2(0.5f, 0.5f);
        pIconGo.sizeDelta = new Vector2(32, 32);
        var pIcon = pIconGo.gameObject.AddComponent<PremiumIcon>();
        pIcon.Kind = IconKind.Menu;
        pIcon.color = PremiumTheme.Text;

        CreateStatsCard("ScoreCard", topBar, new Vector2(0.22f, 0), new Vector2(0.58f, 1), "SCORE", "4,096");
        CreateStatsCard("BestCard", topBar, new Vector2(0.62f, 0), new Vector2(1f, 1), "BEST 🏆", "24,860");

        // Level Pill Header
        var levelPill = CreateUIObject("LevelPill", screen);
        levelPill.anchorMin = new Vector2(0.15f, 0.79f);
        levelPill.anchorMax = new Vector2(0.85f, 0.835f);
        levelPill.offsetMin = levelPill.offsetMax = Vector2.zero;
        var lpImg = levelPill.gameObject.AddComponent<Image>();
        lpImg.sprite = PillSprite;
        lpImg.type = Image.Type.Sliced;
        lpImg.color = PremiumTheme.SurfaceElevated;

        var lpTxt = CreateUIObject("Label", levelPill);
        Stretch(lpTxt);
        var lpl = lpTxt.gameObject.AddComponent<Text>();
        lpl.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        lpl.fontSize = 13;
        lpl.alignment = TextAnchor.MiddleCenter;
        lpl.text = "LEVEL 1  •  4×4 BOARD  •  TARGET 2048";
        lpl.color = PremiumTheme.Gold;

        // Board Container Tray (Big & Prominent!)
        var boardContainer = CreateUIObject("BoardContainer", screen);
        boardContainer.anchorMin = new Vector2(0.04f, 0.20f);
        boardContainer.anchorMax = new Vector2(0.96f, 0.77f);
        boardContainer.offsetMin = boardContainer.offsetMax = Vector2.zero;
        var boardBg = boardContainer.gameObject.AddComponent<Image>();
        boardBg.sprite = PremiumTheme.CreateRoundedCard(128, 32, PremiumTheme.Surface, PremiumTheme.Border, 2f);
        boardBg.type = Image.Type.Sliced;

        var grid = CreateUIObject("Grid", boardContainer);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.sizeDelta = new Vector2(740, 740);

        int[] sampleValues = {
            2, 4, 8, 16,
            32, 64, 128, 256,
            512, 1024, 2048, 4096,
            0, 2, 4, 8
        };

        float cellSize = 164;
        float spacing = 18;
        for (int r = 0; r < 4; r++) {
            for (int c = 0; c < 4; c++) {
                int idx = r * 4 + c;
                var tile = CreateUIObject($"Tile_{r}_{c}", grid);
                tile.sizeDelta = new Vector2(cellSize, cellSize);
                tile.anchoredPosition = new Vector2((c - 1.5f) * (cellSize + spacing), (1.5f - r) * (cellSize + spacing));
                var img = tile.gameObject.AddComponent<Image>();
                img.sprite = PremiumTheme.CreateRoundedCard(64, 18, UnityEngine.Color.white, UnityEngine.Color.clear, 0);
                img.type = Image.Type.Sliced;

                int val = sampleValues[idx];
                PremiumTheme.TileColors(val, out var c1, out _);
                img.color = c1;

                if (val > 0) {
                    var lGo = CreateUIObject("Label", tile);
                    Stretch(lGo);
                    var l = lGo.gameObject.AddComponent<Text>();
                    l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
                    l.fontSize = val >= 1000 ? 38 : (val >= 100 ? 46 : 54);
                    l.alignment = TextAnchor.MiddleCenter;
                    l.text = val.ToString();
                    l.color = PremiumTheme.TileText(val);
                }
            }
        }

        // Floating Power-ups Dock
        var powerBar = CreateUIObject("PowerUpsBar", screen);
        powerBar.anchorMin = new Vector2(0.04f, 0.05f);
        powerBar.anchorMax = new Vector2(0.96f, 0.17f);
        powerBar.offsetMin = powerBar.offsetMax = Vector2.zero;
        var pBarBg = powerBar.gameObject.AddComponent<Image>();
        pBarBg.sprite = CardSprite;
        pBarBg.type = Image.Type.Sliced;

        CreatePowerUpChip("UndoButton", powerBar, new Vector2(0.03f, 0.10f), new Vector2(0.32f, 0.90f), "UNDO", "50 🪙", IconKind.Undo, PremiumTheme.Cyan);
        CreatePowerUpChip("SwapButton", powerBar, new Vector2(0.36f, 0.10f), new Vector2(0.64f, 0.90f), "SWAP", "100 🪙", IconKind.Swap, PremiumTheme.Purple);
        CreatePowerUpChip("DeleteButton", powerBar, new Vector2(0.68f, 0.10f), new Vector2(0.97f, 0.90f), "DELETE", "150 🪙", IconKind.Delete, PremiumTheme.Coral);

        return screen;
    }

    static void CreateStatsCard(string name, Transform parent, Vector2 min, Vector2 max, string title, string val) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardSprite;
        img.type = Image.Type.Sliced;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0, 0.58f); tGo.anchorMax = new Vector2(1, 0.95f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 12; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Muted;

        var vGo = CreateUIObject("Value", card);
        vGo.anchorMin = new Vector2(0, 0.05f); vGo.anchorMax = new Vector2(1, 0.60f);
        vGo.offsetMin = vGo.offsetMax = Vector2.zero;
        var v = vGo.gameObject.AddComponent<Text>();
        v.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        v.fontSize = 28; v.alignment = TextAnchor.MiddleCenter; v.text = val; v.color = PremiumTheme.Text;
    }

    static void CreateActionChip(string name, Transform parent, Vector2 min, Vector2 max, string label, IconKind iconKind) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = CardSprite;
        img.type = Image.Type.Sliced;
        btn.gameObject.AddComponent<Button>();

        var iconGo = CreateUIObject("Icon", btn);
        iconGo.anchorMin = iconGo.anchorMax = new Vector2(0.5f, 0.65f);
        iconGo.sizeDelta = new Vector2(28, 28);
        var icon = iconGo.gameObject.AddComponent<PremiumIcon>();
        icon.Kind = iconKind;
        icon.color = PremiumTheme.Gold;

        var lGo = CreateUIObject("Label", btn);
        lGo.anchorMin = new Vector2(0, 0.12f); lGo.anchorMax = new Vector2(1, 0.42f);
        lGo.offsetMin = lGo.offsetMax = Vector2.zero;
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 12; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Text;
    }

    static void CreatePowerUpChip(string name, Transform parent, Vector2 min, Vector2 max, string label, string cost, IconKind iconKind, UnityEngine.Color accent) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;
        btn.gameObject.AddComponent<Button>();

        var iconGo = CreateUIObject("Icon", btn);
        iconGo.anchorMin = iconGo.anchorMax = new Vector2(0.5f, 0.70f);
        iconGo.sizeDelta = new Vector2(34, 34);
        var icon = iconGo.gameObject.AddComponent<PremiumIcon>();
        icon.Kind = iconKind;
        icon.color = accent;

        var lGo = CreateUIObject("Label", btn);
        lGo.anchorMin = new Vector2(0, 0.32f); lGo.anchorMax = new Vector2(1, 0.55f);
        lGo.offsetMin = lGo.offsetMax = Vector2.zero;
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 13; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = accent;

        var cGo = CreateUIObject("Cost", btn);
        cGo.anchorMin = new Vector2(0, 0.08f); cGo.anchorMax = new Vector2(1, 0.30f);
        cGo.offsetMin = cGo.offsetMax = Vector2.zero;
        var c = cGo.gameObject.AddComponent<Text>();
        c.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        c.fontSize = 11; c.alignment = TextAnchor.MiddleCenter; c.text = cost; c.color = PremiumTheme.Muted;
    }

    static RectTransform CreateDialogBase(string name, Transform parent, string title) {
        var dialog = CreateUIObject(name, parent);
        dialog.anchorMin = new Vector2(0.05f, 0.10f);
        dialog.anchorMax = new Vector2(0.95f, 0.90f);
        dialog.offsetMin = dialog.offsetMax = Vector2.zero;
        var img = dialog.gameObject.AddComponent<Image>();
        img.sprite = DialogSprite;
        img.type = Image.Type.Sliced;

        var header = CreateUIObject("Header", dialog);
        header.anchorMin = new Vector2(0.05f, 0.86f);
        header.anchorMax = new Vector2(0.95f, 0.98f);
        header.offsetMin = header.offsetMax = Vector2.zero;

        var t = header.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Gold;

        return dialog;
    }

    static RectTransform CreatePrimaryGoldButton(string name, Transform parent, Vector2 min, Vector2 max, string label) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = GoldBtnSprite;
        img.type = Image.Type.Sliced;
        btn.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", btn);
        Stretch(lGo);
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 18; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Ink;
        return btn;
    }

    static RectTransform CreateSecondaryButton(string name, Transform parent, Vector2 min, Vector2 max, string label) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;
        btn.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", btn);
        Stretch(lGo);
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 16; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Text;
        return btn;
    }

    // ==========================================
    // 3. DIALOG: LEVELS
    // ==========================================
    public static RectTransform BuildLevelsDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Levels", parent, "CHOOSE YOUR PACE");

        CreateLevelCard(d, "Classic4x4", new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.82f), "CLASSIC 4×4", "CASUAL", "Target 2048 • Best: 24,860", PremiumTheme.Cyan);
        CreateLevelCard(d, "Advanced5x5", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.58f), "ADVANCED 5×5", "STRATEGY", "Target 4096 • Best: 68,420", PremiumTheme.Purple);
        CreateLevelCard(d, "Expert6x6", new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.34f), "EXPERT 6×6", "MASTER", "Target 8192 • Best: 142,890", PremiumTheme.Gold);

        CreateSecondaryButton("BackButton", d, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.11f), "BACK");
        return d;
    }

    static void CreateLevelCard(Transform parent, string name, Vector2 min, Vector2 max, string title, string badge, string desc, UnityEngine.Color badgeCol) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;
        card.gameObject.AddComponent<Button>();

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0.06f, 0.52f); tGo.anchorMax = new Vector2(0.65f, 0.90f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 20; t.alignment = TextAnchor.MiddleLeft; t.text = title; t.color = PremiumTheme.Text;

        var pill = CreateUIObject("Badge", card);
        pill.anchorMin = new Vector2(0.66f, 0.52f); pill.anchorMax = new Vector2(0.94f, 0.88f);
        pill.offsetMin = pill.offsetMax = Vector2.zero;
        var pImg = pill.gameObject.AddComponent<Image>();
        pImg.sprite = PremiumTheme.CreateRoundedCard(64, 32, badgeCol, UnityEngine.Color.clear, 0);
        pImg.type = Image.Type.Sliced;
        var pl = CreateUIObject("L", pill); Stretch(pl);
        var plt = pl.gameObject.AddComponent<Text>();
        plt.font = Resources.Load<Font>("Fonts/NotoSans-Bold"); plt.fontSize = 11; plt.alignment = TextAnchor.MiddleCenter; plt.text = badge; plt.color = PremiumTheme.Ink;

        var dGo = CreateUIObject("Desc", card);
        dGo.anchorMin = new Vector2(0.06f, 0.12f); dGo.anchorMax = new Vector2(0.94f, 0.48f);
        dGo.offsetMin = dGo.offsetMax = Vector2.zero;
        var dt = dGo.gameObject.AddComponent<Text>();
        dt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        dt.fontSize = 14; dt.alignment = TextAnchor.MiddleLeft; dt.text = desc; dt.color = PremiumTheme.Muted;
    }

    // ==========================================
    // 4. DIALOG: HIGH SCORES
    // ==========================================
    public static RectTransform BuildScoresDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_HighScores", parent, "HALL OF FAME 🏆");

        CreatePodiumScoreCard(d, "Classic4x4", new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.83f), "🥇 CLASSIC 4×4", "24,860", "Best Tile: 2048");
        CreatePodiumScoreCard(d, "Advanced5x5", new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.64f), "🥈 ADVANCED 5×5", "68,420", "Best Tile: 4096");
        CreatePodiumScoreCard(d, "Expert6x6", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.45f), "🥉 EXPERT 6×6", "142,890", "Best Tile: 8192");

        var summaryCard = CreateUIObject("SummaryCard", d);
        summaryCard.anchorMin = new Vector2(0.06f, 0.13f);
        summaryCard.anchorMax = new Vector2(0.94f, 0.25f);
        summaryCard.offsetMin = summaryCard.offsetMax = Vector2.zero;
        var sImg = summaryCard.gameObject.AddComponent<Image>();
        sImg.sprite = CardElevatedSprite;
        sImg.type = Image.Type.Sliced;

        CreateStatCol(summaryCard, "PLAYED", "28", 0);
        CreateStatCol(summaryCard, "VICTORIES", "9", 1);
        CreateStatCol(summaryCard, "WIN RATE", "32%", 2);

        CreateSecondaryButton("CloseButton", d, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.10f), "CLOSE");
        return d;
    }

    static void CreatePodiumScoreCard(Transform parent, string name, Vector2 min, Vector2 max, string title, string score, string note) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0.06f, 0.48f); tGo.anchorMax = new Vector2(0.55f, 0.90f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 17; t.alignment = TextAnchor.MiddleLeft; t.text = title; t.color = PremiumTheme.Text;

        var nGo = CreateUIObject("Note", card);
        nGo.anchorMin = new Vector2(0.06f, 0.10f); nGo.anchorMax = new Vector2(0.55f, 0.48f);
        nGo.offsetMin = nGo.offsetMax = Vector2.zero;
        var n = nGo.gameObject.AddComponent<Text>();
        n.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        n.fontSize = 13; n.alignment = TextAnchor.MiddleLeft; n.text = note; n.color = PremiumTheme.Muted;

        var sGo = CreateUIObject("Score", card);
        sGo.anchorMin = new Vector2(0.55f, 0.15f); sGo.anchorMax = new Vector2(0.94f, 0.85f);
        sGo.offsetMin = sGo.offsetMax = Vector2.zero;
        var s = sGo.gameObject.AddComponent<Text>();
        s.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        s.fontSize = 26; s.alignment = TextAnchor.MiddleRight; s.text = score; s.color = PremiumTheme.Gold;
    }

    static void CreateStatCol(RectTransform parent, string title, string val, int col) {
        var go = CreateUIObject("Col_" + col, parent);
        go.anchorMin = new Vector2(col * 0.333f, 0);
        go.anchorMax = new Vector2((col + 1) * 0.333f, 1);
        go.offsetMin = go.offsetMax = Vector2.zero;

        var tGo = CreateUIObject("Title", go);
        tGo.anchorMin = new Vector2(0, 0.52f); tGo.anchorMax = new Vector2(1, 0.95f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 11; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Muted;

        var vGo = CreateUIObject("Val", go);
        vGo.anchorMin = new Vector2(0, 0.05f); vGo.anchorMax = new Vector2(1, 0.52f);
        vGo.offsetMin = vGo.offsetMax = Vector2.zero;
        var v = vGo.gameObject.AddComponent<Text>();
        v.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        v.fontSize = 20; v.alignment = TextAnchor.MiddleCenter; v.text = val; v.color = PremiumTheme.Gold;
    }

    // ==========================================
    // 5. DIALOG: HOW TO PLAY
    // ==========================================
    public static RectTransform BuildRulesDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_HowToPlay", parent, "HOW TO PLAY 📖");

        CreateRuleCard(d, "Rule1", new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.83f), "👆 SWIPE & MERGE", "Slide tiles in 4 directions. Matching tiles merge into double value.");
        CreateRuleCard(d, "Rule2", new Vector2(0.06f, 0.53f), new Vector2(0.94f, 0.67f), "👑 2048 GOAL", "Combine tiles to reach the legendary 2048 tile and win!");
        CreateRuleCard(d, "Rule3", new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.51f), "🛠️ POWER-UPS", "Use Undo, Swap, and Delete to escape tight situations.");
        CreateRuleCard(d, "Rule4", new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.35f), "🚀 ENDLESS MODE", "Keep going after 2048 to unlock 4096 and 8192 records!");

        CreatePrimaryGoldButton("GotItButton", d, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.14f), "GOT IT!");
        return d;
    }

    static void CreateRuleCard(Transform parent, string name, Vector2 min, Vector2 max, string title, string desc) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0.06f, 0.50f); tGo.anchorMax = new Vector2(0.94f, 0.90f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 15; t.alignment = TextAnchor.MiddleLeft; t.text = title; t.color = PremiumTheme.Gold;

        var dGo = CreateUIObject("Desc", card);
        dGo.anchorMin = new Vector2(0.06f, 0.08f); dGo.anchorMax = new Vector2(0.94f, 0.50f);
        dGo.offsetMin = dGo.offsetMax = Vector2.zero;
        var dt = dGo.gameObject.AddComponent<Text>();
        dt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        dt.fontSize = 13; dt.alignment = TextAnchor.MiddleLeft; dt.text = desc; dt.color = PremiumTheme.Text;
    }

    // ==========================================
    // 6. DIALOG: SETTINGS
    // ==========================================
    public static RectTransform BuildSettingsDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Settings", parent, "SETTINGS ⚙️");

        CreateSettingToggle(d, "ToggleSound", new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.82f), "🔊  Sound Effects", "ON", PremiumTheme.Emerald);
        CreateSettingToggle(d, "ToggleHaptics", new Vector2(0.06f, 0.53f), new Vector2(0.94f, 0.66f), "📳  Haptic Feedback", "ON", PremiumTheme.Emerald);
        CreateSettingToggle(d, "ToggleLanguage", new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.50f), "🌐  Language", "ENGLISH", PremiumTheme.Cyan);
        CreateSettingToggle(d, "ToggleContrast", new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.34f), "👁️  High Contrast", "OFF", PremiumTheme.Muted);

        CreateSecondaryButton("CloseButton", d, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.14f), "CLOSE");
        return d;
    }

    static void CreateSettingToggle(Transform parent, string name, Vector2 min, Vector2 max, string label, string state, UnityEngine.Color stateCol) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;
        card.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", card);
        lGo.anchorMin = new Vector2(0.06f, 0.15f); lGo.anchorMax = new Vector2(0.65f, 0.85f);
        lGo.offsetMin = lGo.offsetMax = Vector2.zero;
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 16; l.alignment = TextAnchor.MiddleLeft; l.text = label; l.color = PremiumTheme.Text;

        var pill = CreateUIObject("Pill", card);
        pill.anchorMin = new Vector2(0.68f, 0.20f); pill.anchorMax = new Vector2(0.94f, 0.80f);
        pill.offsetMin = pill.offsetMax = Vector2.zero;
        var pImg = pill.gameObject.AddComponent<Image>();
        pImg.sprite = PillSprite; pImg.type = Image.Type.Sliced; pImg.color = PremiumTheme.Surface;
        var pl = CreateUIObject("L", pill); Stretch(pl);
        var plt = pl.gameObject.AddComponent<Text>();
        plt.font = Resources.Load<Font>("Fonts/NotoSans-Bold"); plt.fontSize = 12; plt.alignment = TextAnchor.MiddleCenter; plt.text = state; plt.color = stateCol;
    }

    // ==========================================
    // 7. DIALOG: PAUSE
    // ==========================================
    public static RectTransform BuildPauseDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Pause", parent, "GAME PAUSED ⏸️");

        CreatePrimaryGoldButton("ResumeButton", d, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.77f), "▶  RESUME GAME");
        CreateSecondaryButton("RestartButton", d, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.57f), "🔄  RESTART LEVEL");
        CreateSecondaryButton("HomeButton", d, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.37f), "🏠  HOME MENU");

        return d;
    }

    // ==========================================
    // 8. DIALOG: GAME OVER
    // ==========================================
    public static RectTransform BuildGameOverDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_GameOver", parent, "OUT OF MOVES 💀");

        var card = CreateUIObject("FinalScoreCard", d);
        card.anchorMin = new Vector2(0.08f, 0.44f);
        card.anchorMax = new Vector2(0.92f, 0.80f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0, 0.65f); tGo.anchorMax = new Vector2(1, 0.92f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter; t.text = "FINAL SCORE"; t.color = PremiumTheme.Muted;

        var sGo = CreateUIObject("Score", card);
        sGo.anchorMin = new Vector2(0, 0.28f); sGo.anchorMax = new Vector2(1, 0.68f);
        sGo.offsetMin = sGo.offsetMax = Vector2.zero;
        var s = sGo.gameObject.AddComponent<Text>();
        s.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        s.fontSize = 36; s.alignment = TextAnchor.MiddleCenter; s.text = "18,460"; s.color = PremiumTheme.Gold;

        var bGo = CreateUIObject("Badge", card);
        bGo.anchorMin = new Vector2(0, 0.08f); bGo.anchorMax = new Vector2(1, 0.28f);
        bGo.offsetMin = bGo.offsetMax = Vector2.zero;
        var b = bGo.gameObject.AddComponent<Text>();
        b.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        b.fontSize = 14; b.alignment = TextAnchor.MiddleCenter; b.text = "Highest Tile Achieved: <color=#FFC837>1024</color>"; b.color = PremiumTheme.Text;

        CreatePrimaryGoldButton("PlayAgainButton", d, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.38f), "⚡  PLAY AGAIN");
        CreateSecondaryButton("HomeButton", d, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.19f), "🏠  HOME MENU");

        return d;
    }

    // ==========================================
    // 9. DIALOG: VICTORY
    // ==========================================
    public static RectTransform BuildVictoryDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Victory", parent, "VICTORY! 👑");

        var card = CreateUIObject("VictoryCard", d);
        card.anchorMin = new Vector2(0.08f, 0.50f);
        card.anchorMax = new Vector2(0.92f, 0.82f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = CardElevatedSprite;
        img.type = Image.Type.Sliced;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0, 0.65f); tGo.anchorMax = new Vector2(1, 0.92f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 20; t.alignment = TextAnchor.MiddleCenter; t.text = "YOU CREATED 2048!"; t.color = PremiumTheme.Gold;

        var sGo = CreateUIObject("Score", card);
        sGo.anchorMin = new Vector2(0, 0.28f); sGo.anchorMax = new Vector2(1, 0.65f);
        sGo.offsetMin = sGo.offsetMax = Vector2.zero;
        var s = sGo.gameObject.AddComponent<Text>();
        s.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        s.fontSize = 34; s.alignment = TextAnchor.MiddleCenter; s.text = "20,480"; s.color = PremiumTheme.Text;

        var bGo = CreateUIObject("Badge", card);
        bGo.anchorMin = new Vector2(0, 0.08f); bGo.anchorMax = new Vector2(1, 0.28f);
        bGo.offsetMin = bGo.offsetMax = Vector2.zero;
        var b = bGo.gameObject.AddComponent<Text>();
        b.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        b.fontSize = 14; b.alignment = TextAnchor.MiddleCenter; b.text = "🏆 Royal Crown Unlocked!"; b.color = PremiumTheme.Gold;

        CreatePrimaryGoldButton("KeepGoingButton", d, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.45f), "🚀  KEEP GOING (ENDLESS)");
        CreateSecondaryButton("PlayAgainButton", d, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.28f), "NEW GAME");
        CreateSecondaryButton("HomeButton", d, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f), "HOME MENU");

        return d;
    }

    // ==========================================
    // 10. DIALOG: RESTART
    // ==========================================
    public static RectTransform BuildRestartDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Restart", parent, "RESTART LEVEL? ⚠️");

        var card = CreateUIObject("DescCard", d);
        card.anchorMin = new Vector2(0.08f, 0.45f);
        card.anchorMax = new Vector2(0.92f, 0.78f);
        card.offsetMin = card.offsetMax = Vector2.zero;
        var tcImg = card.gameObject.AddComponent<Image>();
        tcImg.sprite = CardElevatedSprite;
        tcImg.type = Image.Type.Sliced;

        var txtGo = CreateUIObject("Desc", card);
        Stretch(txtGo);
        var t = txtGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 18; t.lineSpacing = 1.3f; t.alignment = TextAnchor.MiddleCenter;
        t.text = "Are you sure you want to restart?\n\n<color=#FF8008>Current board progress will be lost.</color>";
        t.color = PremiumTheme.Text;

        var rBtn = CreateUIObject("ConfirmRestartButton", d);
        rBtn.anchorMin = new Vector2(0.08f, 0.25f); rBtn.anchorMax = new Vector2(0.92f, 0.39f);
        rBtn.offsetMin = rBtn.offsetMax = Vector2.zero;
        var rImg = rBtn.gameObject.AddComponent<Image>();
        rImg.sprite = DangerBtnSprite; rImg.type = Image.Type.Sliced;
        rBtn.gameObject.AddComponent<Button>();
        var rl = CreateUIObject("L", rBtn); Stretch(rl);
        var rlt = rl.gameObject.AddComponent<Text>();
        rlt.font = Resources.Load<Font>("Fonts/NotoSans-Bold"); rlt.fontSize = 18; rlt.alignment = TextAnchor.MiddleCenter; rlt.text = "RESTART NOW"; rlt.color = UnityEngine.Color.white;

        CreateSecondaryButton("CancelButton", d, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.21f), "CANCEL");
        return d;
    }
}
}
