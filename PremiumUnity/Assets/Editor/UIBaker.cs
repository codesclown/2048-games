using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Premium2048;

namespace Premium2048.EditorTools {
public static class UIBaker {
    static Sprite roundedSprite;

    static Sprite GetRoundedSprite() {
        if (roundedSprite != null) return roundedSprite;
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(0, Mathf.Abs(x - r + 0.5f) - (r - 28));
                float dy = Mathf.Max(0, Mathf.Abs(y - r + 0.5f) - (r - 28));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (dist - 28f));
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(28, 28, 28, 28));
        return roundedSprite;
    }

    static Color DialogBg => new Color(0.10f, 0.10f, 0.22f, 1f);
    static Color CardBg => new Color(0.16f, 0.16f, 0.34f, 1f);
    static Color CardBgLight => new Color(0.20f, 0.20f, 0.40f, 1f);

    [MenuItem("2048 Premium/Bake All Screens to Dedicated Scenes and Prefabs")]
    public static void BakeAll() {
        EnsureDirectories();
        GetRoundedSprite();

        // 1. Bake Master Unified Scene
        BakeMasterScene();

        // 2. Bake Individual Dedicated Preview Scenes for Every Screen
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
        bgImg.color = PremiumTheme.Ink;

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

        var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

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
        bgImage.color = PremiumTheme.Ink;

        var safeArea = CreateUIObject("SafeArea", canvasGo.transform);
        Stretch(safeArea);
        safeArea.offsetMin = new Vector2(36, 64);
        safeArea.offsetMax = new Vector2(-36, -64);

        var gameManagerGo = new GameObject("2048 Premium Controller", typeof(PremiumGame), typeof(GameManager), typeof(GameController), typeof(AudioSource), typeof(AudioListener));

        var homeScreen = BuildHomeScreen(safeArea);
        var gameScreen = BuildGameScreen(safeArea);

        var dialogContainer = CreateUIObject("DialogContainer", safeArea);
        Stretch(dialogContainer);
        var dialogBg = dialogContainer.gameObject.AddComponent<Image>();
        dialogBg.color = new Color(0.04f, 0.04f, 0.08f, 0.85f);
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

    public static RectTransform BuildHomeScreen(RectTransform parent) {
        var screen = CreateUIObject("HomeScreen", parent);
        Stretch(screen);

        var header = CreateUIObject("Header", screen);
        header.anchorMin = new Vector2(0, 0.72f);
        header.anchorMax = new Vector2(1, 1f);
        header.offsetMin = header.offsetMax = Vector2.zero;

        var mark = CreateUIObject("BrandMarkBadge", header);
        mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.72f);
        mark.sizeDelta = new Vector2(100, 100);
        var markBg = mark.gameObject.AddComponent<Image>();
        markBg.sprite = GetRoundedSprite();
        markBg.type = Image.Type.Sliced;
        markBg.color = PremiumTheme.Accent;

        var markTxt = CreateUIObject("Label", mark);
        Stretch(markTxt);
        var mLabel = markTxt.gameObject.AddComponent<Text>();
        mLabel.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        mLabel.fontSize = 24;
        mLabel.alignment = TextAnchor.MiddleCenter;
        mLabel.text = "2 0\n4 8";
        mLabel.lineSpacing = 0.85f;
        mLabel.color = PremiumTheme.Ink;

        var title = CreateUIObject("Title", header);
        title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.32f);
        title.sizeDelta = new Vector2(500, 48);
        var titleTxt = title.gameObject.AddComponent<Text>();
        titleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        titleTxt.fontSize = 34;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = "2048";
        titleTxt.color = PremiumTheme.Accent;

        var subtitle = CreateUIObject("Subtitle", header);
        subtitle.anchorMin = subtitle.anchorMax = new Vector2(0.5f, 0.12f);
        subtitle.sizeDelta = new Vector2(500, 26);
        var subTxt = subtitle.gameObject.AddComponent<Text>();
        subTxt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        subTxt.fontSize = 14;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.text = "THE NUMBER COLLECTION";
        subTxt.color = PremiumTheme.Muted;

        var hero = CreateUIObject("HeroCard", screen);
        hero.anchorMin = new Vector2(0.04f, 0.34f);
        hero.anchorMax = new Vector2(0.96f, 0.70f);
        hero.offsetMin = hero.offsetMax = Vector2.zero;
        var heroBg = hero.gameObject.AddComponent<Image>();
        heroBg.sprite = GetRoundedSprite();
        heroBg.type = Image.Type.Sliced;
        heroBg.color = CardBg;

        var miniGrid = CreateUIObject("MiniGrid", hero);
        miniGrid.anchorMin = new Vector2(0.08f, 0.18f);
        miniGrid.anchorMax = new Vector2(0.44f, 0.82f);
        miniGrid.offsetMin = miniGrid.offsetMax = Vector2.zero;
        var mgBg = miniGrid.gameObject.AddComponent<Image>();
        mgBg.sprite = GetRoundedSprite();
        mgBg.type = Image.Type.Sliced;
        mgBg.color = new Color(0.11f, 0.11f, 0.24f, 1f);

        for (int row = 0; row < 2; row++) {
            for (int col = 0; col < 2; col++) {
                var miniTile = CreateUIObject($"Tile_{row}_{col}", miniGrid);
                miniTile.anchorMin = new Vector2(0.1f + col * 0.45f, 0.1f + (1 - row) * 0.45f);
                miniTile.anchorMax = new Vector2(0.45f + col * 0.45f, 0.45f + (1 - row) * 0.45f);
                miniTile.offsetMin = miniTile.offsetMax = Vector2.zero;
                var mtImg = miniTile.gameObject.AddComponent<Image>();
                mtImg.sprite = GetRoundedSprite();
                mtImg.type = Image.Type.Sliced;
                int val = (row == 0 && col == 1) ? 4 : ((row == 1 && col == 0) ? 2 : 0);
                PremiumTheme.TileColors(val, out var c1, out _);
                mtImg.color = c1;
                if (val > 0) {
                    var tLabel = CreateUIObject("T", miniTile);
                    Stretch(tLabel);
                    var tl = tLabel.gameObject.AddComponent<Text>();
                    tl.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
                    tl.fontSize = 18; tl.alignment = TextAnchor.MiddleCenter;
                    tl.text = val.ToString();
                    tl.color = PremiumTheme.TileText(val);
                }
            }
        }

        var heroTitle = CreateUIObject("HeroTitle", hero);
        heroTitle.anchorMin = new Vector2(0.48f, 0.55f);
        heroTitle.anchorMax = new Vector2(0.96f, 0.82f);
        heroTitle.offsetMin = heroTitle.offsetMax = Vector2.zero;
        var heroTitleTxt = heroTitle.gameObject.AddComponent<Text>();
        heroTitleTxt.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        heroTitleTxt.fontSize = 20;
        heroTitleTxt.alignment = TextAnchor.MiddleLeft;
        heroTitleTxt.text = "EXPEDITION IN PROGRESS";
        heroTitleTxt.color = PremiumTheme.Accent;

        var heroDesc = CreateUIObject("HeroDesc", hero);
        heroDesc.anchorMin = new Vector2(0.48f, 0.20f);
        heroDesc.anchorMax = new Vector2(0.96f, 0.52f);
        heroDesc.offsetMin = heroDesc.offsetMax = Vector2.zero;
        var heroDescTxt = heroDesc.gameObject.AddComponent<Text>();
        heroDescTxt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        heroDescTxt.fontSize = 15;
        heroDescTxt.alignment = TextAnchor.MiddleLeft;
        heroDescTxt.text = "4×4 • Target 2048\nScore: 2,480";
        heroDescTxt.color = PremiumTheme.Muted;

        var newGameBtn = CreateUIObject("NewGameButton", screen);
        newGameBtn.anchorMin = new Vector2(0.04f, 0.20f);
        newGameBtn.anchorMax = new Vector2(0.96f, 0.29f);
        newGameBtn.offsetMin = newGameBtn.offsetMax = Vector2.zero;
        var ngImg = newGameBtn.gameObject.AddComponent<Image>();
        ngImg.sprite = GetRoundedSprite();
        ngImg.type = Image.Type.Sliced;
        ngImg.color = PremiumTheme.Accent;
        newGameBtn.gameObject.AddComponent<Button>();

        var ngTxt = CreateUIObject("Label", newGameBtn);
        Stretch(ngTxt);
        var ngLabel = ngTxt.gameObject.AddComponent<Text>();
        ngLabel.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        ngLabel.fontSize = 24;
        ngLabel.alignment = TextAnchor.MiddleCenter;
        ngLabel.text = "NEW GAME";
        ngLabel.color = PremiumTheme.Ink;

        var bottomBar = CreateUIObject("BottomBar", screen);
        bottomBar.anchorMin = new Vector2(0.04f, 0.05f);
        bottomBar.anchorMax = new Vector2(0.96f, 0.16f);
        bottomBar.offsetMin = bottomBar.offsetMax = Vector2.zero;

        CreateButton("HowToPlayButton", bottomBar, new Vector2(0, 0), new Vector2(0.31f, 1), "HOW TO PLAY", CardBg);
        CreateButton("HighScoresButton", bottomBar, new Vector2(0.345f, 0), new Vector2(0.655f, 1), "HIGH SCORES", CardBg);
        CreateButton("SettingsButton", bottomBar, new Vector2(0.69f, 0), new Vector2(1, 1), "SETTINGS", CardBg);

        return screen;
    }

    public static RectTransform BuildGameScreen(RectTransform parent) {
        var screen = CreateUIObject("GameScreen", parent);
        Stretch(screen);

        var topBar = CreateUIObject("TopBar", screen);
        topBar.anchorMin = new Vector2(0.04f, 0.86f);
        topBar.anchorMax = new Vector2(0.96f, 0.98f);
        topBar.offsetMin = topBar.offsetMax = Vector2.zero;

        CreateCard("ScoreCard", topBar, new Vector2(0, 0), new Vector2(0.48f, 1), "SCORE", "4,096", CardBg);
        CreateCard("BestCard", topBar, new Vector2(0.52f, 0), new Vector2(1, 1), "BEST", "24,860", CardBg);

        var boardContainer = CreateUIObject("BoardContainer", screen);
        boardContainer.anchorMin = new Vector2(0.04f, 0.24f);
        boardContainer.anchorMax = new Vector2(0.96f, 0.82f);
        boardContainer.offsetMin = boardContainer.offsetMax = Vector2.zero;
        var boardBg = boardContainer.gameObject.AddComponent<Image>();
        boardBg.sprite = GetRoundedSprite();
        boardBg.type = Image.Type.Sliced;
        boardBg.color = CardBg;

        var grid = CreateUIObject("Grid", boardContainer);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.sizeDelta = new Vector2(420, 420);

        int[] sampleValues = {
            2, 4, 8, 16,
            32, 64, 128, 256,
            512, 1024, 2048, 4096,
            0, 2, 4, 8
        };

        float cellSize = 94;
        float spacing = 10;
        for (int r = 0; r < 4; r++) {
            for (int c = 0; c < 4; c++) {
                int idx = r * 4 + c;
                var tile = CreateUIObject($"Tile_{r}_{c}", grid);
                tile.sizeDelta = new Vector2(cellSize, cellSize);
                tile.anchoredPosition = new Vector2((c - 1.5f) * (cellSize + spacing), (1.5f - r) * (cellSize + spacing));
                var img = tile.gameObject.AddComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                
                int val = sampleValues[idx];
                PremiumTheme.TileColors(val, out var c1, out _);
                img.color = c1;

                if (val > 0) {
                    var lGo = CreateUIObject("Label", tile);
                    Stretch(lGo);
                    var l = lGo.gameObject.AddComponent<Text>();
                    l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
                    l.fontSize = val >= 1000 ? 22 : (val >= 100 ? 26 : 30);
                    l.alignment = TextAnchor.MiddleCenter;
                    l.text = val.ToString();
                    l.color = PremiumTheme.TileText(val);
                }
            }
        }

        var powerBar = CreateUIObject("PowerUpsBar", screen);
        powerBar.anchorMin = new Vector2(0.04f, 0.08f);
        powerBar.anchorMax = new Vector2(0.96f, 0.20f);
        powerBar.offsetMin = powerBar.offsetMax = Vector2.zero;

        CreatePowerButton("UndoButton", powerBar, new Vector2(0, 0), new Vector2(0.31f, 1), "UNDO", "50 🪙", IconKind.Undo);
        CreatePowerButton("SwapButton", powerBar, new Vector2(0.345f, 0), new Vector2(0.655f, 1), "SWAP", "100 🪙", IconKind.Swap);
        CreatePowerButton("DeleteButton", powerBar, new Vector2(0.69f, 0), new Vector2(1, 1), "DELETE", "150 🪙", IconKind.Delete);

        return screen;
    }

    static RectTransform CreateCard(string name, Transform parent, Vector2 min, Vector2 max, string title, string val, Color bgCol) {
        var card = CreateUIObject(name, parent);
        card.anchorMin = min; card.anchorMax = max;
        card.offsetMin = card.offsetMax = Vector2.zero;
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = bgCol;

        var tGo = CreateUIObject("Title", card);
        tGo.anchorMin = new Vector2(0, 0.55f); tGo.anchorMax = new Vector2(1, 0.95f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Muted;

        var vGo = CreateUIObject("Value", card);
        vGo.anchorMin = new Vector2(0, 0.05f); vGo.anchorMax = new Vector2(1, 0.55f);
        vGo.offsetMin = vGo.offsetMax = Vector2.zero;
        var v = vGo.gameObject.AddComponent<Text>();
        v.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        v.fontSize = 28; v.alignment = TextAnchor.MiddleCenter; v.text = val; v.color = PremiumTheme.Text;

        return card;
    }

    static RectTransform CreateButton(string name, Transform parent, Vector2 min, Vector2 max, string label, Color bgCol, Color? textCol = null) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = bgCol;
        btn.gameObject.AddComponent<Button>();

        var lGo = CreateUIObject("Label", btn);
        Stretch(lGo);
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 16; l.alignment = TextAnchor.MiddleCenter; l.text = label;
        l.color = textCol.HasValue ? textCol.Value : PremiumTheme.Text;

        return btn;
    }

    static RectTransform CreatePowerButton(string name, Transform parent, Vector2 min, Vector2 max, string label, string cost, IconKind iconKind) {
        var btn = CreateUIObject(name, parent);
        btn.anchorMin = min; btn.anchorMax = max;
        btn.offsetMin = btn.offsetMax = Vector2.zero;
        var img = btn.gameObject.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = CardBg;
        btn.gameObject.AddComponent<Button>();

        var iconGo = CreateUIObject("Icon", btn);
        iconGo.anchorMin = iconGo.anchorMax = new Vector2(0.5f, 0.72f);
        iconGo.sizeDelta = new Vector2(36, 36);
        var icon = iconGo.gameObject.AddComponent<PremiumIcon>();
        icon.Kind = iconKind;
        icon.color = PremiumTheme.Accent;

        var lGo = CreateUIObject("Label", btn);
        lGo.anchorMin = new Vector2(0, 0.32f); lGo.anchorMax = new Vector2(1, 0.58f);
        lGo.offsetMin = lGo.offsetMax = Vector2.zero;
        var l = lGo.gameObject.AddComponent<Text>();
        l.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        l.fontSize = 14; l.alignment = TextAnchor.MiddleCenter; l.text = label; l.color = PremiumTheme.Accent;

        var cGo = CreateUIObject("Cost", btn);
        cGo.anchorMin = new Vector2(0, 0.05f); cGo.anchorMax = new Vector2(1, 0.32f);
        cGo.offsetMin = cGo.offsetMax = Vector2.zero;
        var c = cGo.gameObject.AddComponent<Text>();
        c.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        c.fontSize = 12; c.alignment = TextAnchor.MiddleCenter; c.text = cost; c.color = PremiumTheme.Muted;

        return btn;
    }

    static RectTransform CreateDialogBase(string name, Transform parent, string title) {
        var dialog = CreateUIObject(name, parent);
        dialog.anchorMin = new Vector2(0.06f, 0.12f);
        dialog.anchorMax = new Vector2(0.94f, 0.88f);
        dialog.offsetMin = dialog.offsetMax = Vector2.zero;
        var img = dialog.gameObject.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = DialogBg;

        var header = CreateUIObject("Header", dialog);
        header.anchorMin = new Vector2(0, 0.88f);
        header.anchorMax = new Vector2(1, 1f);
        header.offsetMin = header.offsetMax = Vector2.zero;
        var t = header.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        t.fontSize = 26; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Accent;

        return dialog;
    }

    public static RectTransform BuildLevelsDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Levels", parent, "CHOOSE YOUR PACE");
        CreateButton("Level4x4", d, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.80f), "CLASSIC 4x4\n<size=13><color=#B8B8FF>Target 2048 • Casual Pace</color></size>", CardBg);
        CreateButton("Level5x5", d, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.58f), "ADVANCED 5x5\n<size=13><color=#B8B8FF>Target 4096 • Strategic Board</color></size>", CardBg);
        CreateButton("Level6x6", d, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.36f), "EXPERT 6x6\n<size=13><color=#B8B8FF>Target 8192 • Grandmaster Expedition</color></size>", CardBg);
        CreateButton("BackButton", d, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.13f), "BACK", CardBgLight);
        return d;
    }

    public static RectTransform BuildScoresDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_HighScores", parent, "HIGH SCORES");
        CreateCard("Score4x4", d, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f), "CLASSIC 4x4", "24,860", CardBg);
        CreateCard("Score5x5", d, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.64f), "ADVANCED 5x5", "68,420", CardBg);
        CreateCard("Score6x6", d, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.46f), "EXPERT 6x6", "142,890", CardBg);

        var summaryCard = CreateUIObject("SummaryCard", d);
        summaryCard.anchorMin = new Vector2(0.08f, 0.16f);
        summaryCard.anchorMax = new Vector2(0.92f, 0.27f);
        summaryCard.offsetMin = summaryCard.offsetMax = Vector2.zero;
        var sImg = summaryCard.gameObject.AddComponent<Image>();
        sImg.sprite = GetRoundedSprite();
        sImg.type = Image.Type.Sliced;
        sImg.color = CardBg;

        CreateStatCol(summaryCard, "PLAYED", "28", 0);
        CreateStatCol(summaryCard, "VICTORIES", "9", 1);
        CreateStatCol(summaryCard, "WIN RATE", "32%", 2);

        CreateButton("CloseButton", d, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.13f), "CLOSE", CardBgLight);
        return d;
    }

    static void CreateStatCol(RectTransform parent, string title, string val, int col) {
        var go = CreateUIObject("Col_" + col, parent);
        go.anchorMin = new Vector2(col * 0.333f, 0);
        go.anchorMax = new Vector2((col + 1) * 0.333f, 1);
        go.offsetMin = go.offsetMax = Vector2.zero;

        var tGo = CreateUIObject("Title", go);
        tGo.anchorMin = new Vector2(0, 0.5f); tGo.anchorMax = new Vector2(1, 0.95f);
        tGo.offsetMin = tGo.offsetMax = Vector2.zero;
        var t = tGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 12; t.alignment = TextAnchor.MiddleCenter; t.text = title; t.color = PremiumTheme.Muted;

        var vGo = CreateUIObject("Val", go);
        vGo.anchorMin = new Vector2(0, 0.05f); vGo.anchorMax = new Vector2(1, 0.5f);
        vGo.offsetMin = vGo.offsetMax = Vector2.zero;
        var v = vGo.gameObject.AddComponent<Text>();
        v.font = Resources.Load<Font>("Fonts/NotoSans-Bold");
        v.fontSize = 20; v.alignment = TextAnchor.MiddleCenter; v.text = val; v.color = PremiumTheme.Accent;
    }

    public static RectTransform BuildRulesDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_HowToPlay", parent, "HOW TO PLAY");
        var content = CreateUIObject("Content", d);
        content.anchorMin = new Vector2(0.08f, 0.18f); content.anchorMax = new Vector2(0.92f, 0.84f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var cImg = content.gameObject.AddComponent<Image>();
        cImg.sprite = GetRoundedSprite();
        cImg.type = Image.Type.Sliced;
        cImg.color = CardBg;

        var txtGo = CreateUIObject("Text", content);
        txtGo.anchorMin = new Vector2(0.06f, 0.06f); txtGo.anchorMax = new Vector2(0.94f, 0.94f);
        txtGo.offsetMin = txtGo.offsetMax = Vector2.zero;
        var txt = txtGo.gameObject.AddComponent<Text>();
        txt.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        txt.fontSize = 18; txt.lineSpacing = 1.3f; txt.alignment = TextAnchor.UpperLeft;
        txt.text = "• <color=#FFD700>MOVE & MERGE:</color> Swipe up, down, left, or right to slide tiles.\n\n• <color=#FFD700>COMBINE:</color> Matching numbers merge into double values.\n\n• <color=#FFD700>GOAL:</color> Create the legendary 2048 tile to achieve victory!\n\n• <color=#FFD700>POWER-UPS:</color> Use Undo, Swap, and Delete to master tough spots.";
        txt.color = PremiumTheme.Text;

        CreateButton("CloseButton", d, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.14f), "CLOSE", CardBgLight);
        return d;
    }

    public static RectTransform BuildSettingsDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Settings", parent, "SETTINGS");
        CreateButton("ToggleSound", d, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.80f), "🔊 SOUND: ON", CardBg);
        CreateButton("ToggleHaptics", d, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.64f), "📳 HAPTIC FEEDBACK: ON", CardBg);
        CreateButton("ToggleLanguage", d, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.48f), "🌐 LANGUAGE: ENGLISH", CardBg);
        CreateButton("ToggleContrast", d, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.32f), "🎨 HIGH CONTRAST: OFF", CardBg);
        CreateButton("CloseButton", d, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.15f), "CLOSE", CardBgLight);
        return d;
    }

    public static RectTransform BuildPauseDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Pause", parent, "GAME PAUSED");
        CreateButton("ResumeButton", d, new Vector2(0.08f, 0.65f), new Vector2(0.92f, 0.80f), "▶ RESUME", PremiumTheme.Accent, PremiumTheme.Ink);
        CreateButton("RestartButton", d, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.60f), "🔄 RESTART LEVEL", CardBg);
        CreateButton("HomeButton", d, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.40f), "🏠 HOME MENU", CardBgLight);
        return d;
    }

    public static RectTransform BuildGameOverDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_GameOver", parent, "GAME OVER");
        CreateCard("FinalScoreCard", d, new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.78f), "FINAL SCORE", "18,460\n<size=14><color=#FFD700>Highest Tile: 1024</color></size>", CardBg);
        CreateButton("PlayAgainButton", d, new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.41f), "PLAY AGAIN", PremiumTheme.Accent, PremiumTheme.Ink);
        CreateButton("HomeButton", d, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.22f), "HOME MENU", CardBgLight);
        return d;
    }

    public static RectTransform BuildVictoryDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Victory", parent, "VICTORY! 👑");
        CreateCard("VictoryScoreCard", d, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.80f), "YOU REACHED 2048!", "Score: 20,480\n<size=14><color=#FFD700>Crown Achieved!</color></size>", CardBg);
        CreateButton("KeepGoingButton", d, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.50f), "KEEP GOING (ENDLESS)", PremiumTheme.Accent, PremiumTheme.Ink);
        CreateButton("PlayAgainButton", d, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.32f), "PLAY AGAIN", CardBg);
        CreateButton("HomeButton", d, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.16f), "HOME MENU", CardBgLight);
        return d;
    }

    public static RectTransform BuildRestartDialog(RectTransform parent) {
        var d = CreateDialogBase("Dialog_Restart", parent, "RESTART LEVEL?");
        var txtCard = CreateUIObject("DescCard", d);
        txtCard.anchorMin = new Vector2(0.08f, 0.45f); txtCard.anchorMax = new Vector2(0.92f, 0.78f);
        txtCard.offsetMin = txtCard.offsetMax = Vector2.zero;
        var tcImg = txtCard.gameObject.AddComponent<Image>();
        tcImg.sprite = GetRoundedSprite();
        tcImg.type = Image.Type.Sliced;
        tcImg.color = CardBg;

        var txtGo = CreateUIObject("Desc", txtCard);
        Stretch(txtGo);
        var t = txtGo.gameObject.AddComponent<Text>();
        t.font = Resources.Load<Font>("Fonts/NotoSans-Regular");
        t.fontSize = 20; t.alignment = TextAnchor.MiddleCenter;
        t.text = "Are you sure you want to restart?\n\n<color=#B8B8FF>Current board progress will be lost.</color>";
        t.color = PremiumTheme.Text;

        CreateButton("ConfirmRestartButton", d, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.40f), "RESTART", new Color(0.85f, 0.25f, 0.35f, 1f), Color.white);
        CreateButton("CancelButton", d, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.22f), "CANCEL", CardBgLight);
        return d;
    }
}
}
