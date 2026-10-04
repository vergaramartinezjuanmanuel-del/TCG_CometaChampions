using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CometaChampions.Runtime.UI;
using CometaChampions.Runtime.Cards;
using CometaChampions.Runtime.Services;
using CometaChampions.Core.Models;

namespace CometaChampions.Editor
{
    /// <summary>
    /// Editor utility to construct the complete "Home" (Album de Cartas) scene
    /// directly matching the UI_test.png mockup layout.
    /// Accessible via menu: Cometa Champions -> Build Home Scene
    /// </summary>
    public static class HomeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Home.unity";
        private const string PrefabDir = "Assets/_Project/Prefabs";
        private const string GridItemPrefabPath = "Assets/_Project/Prefabs/CardGridItem.prefab";

        [MenuItem("Cometa Champions/Build Home Scene")]
        public static void BuildHomeScene()
        {
            EnsureDirectories();

            // 1. Create Empty Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Lighting & Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // Studio 3-Point Lighting for 360 card inspection
            var keyLightObj = new GameObject("Key_Light");
            var keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.25f;
            keyLight.color = new Color(1f, 0.97f, 0.90f);
            keyLightObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var fillLightObj = new GameObject("Fill_Light");
            var fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.7f;
            fillLight.color = new Color(0.65f, 0.8f, 1f);
            fillLightObj.transform.rotation = Quaternion.Euler(15f, 60f, 0f);

            var rimLightObj = new GameObject("Rim_Light");
            var rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.intensity = 1.3f;
            rimLight.color = new Color(1f, 0.85f, 0.55f);
            rimLightObj.transform.rotation = Quaternion.Euler(-25f, 160f, 0f);

            // 3. EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 4. SaveManager Service
            var saveMgrObj = new GameObject("SaveManager");
            saveMgrObj.AddComponent<SaveManager>();

            // 4.5 Background Canvas (ScreenSpaceCamera behind 3D Card)
            var bgCanvasObj = new GameObject("Background_Canvas");
            var bgCanvas = bgCanvasObj.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            bgCanvas.worldCamera = camera;
            bgCanvas.planeDistance = 25f;
            bgCanvas.sortingOrder = -100;
            var bgScaler = bgCanvasObj.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1920, 1080);
            bgScaler.matchWidthOrHeight = 0.5f;

            var bgImageObj = CreateUIElement("Background_Image", bgCanvasObj.transform);
            SetStretch(bgImageObj.GetComponent<RectTransform>());
            var bgImage = bgImageObj.AddComponent<Image>();
            var bgSprite = ArenaBackgroundProvider.GetBackgroundSprite(ArenaBackgroundCatalog.Pedestal3DActive);
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
            }
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;

            // 5. Canvas Hierarchy
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Background anchor (empty RectTransform)
            var bgObj = CreateUIElement("Background", canvasObj.transform);
            SetStretch(bgObj.GetComponent<RectTransform>());

            // Home Controller Component
            var homeController = canvasObj.AddComponent<Home>();
            var so = new SerializedObject(homeController);
            so.FindProperty("homeBackgroundImage").objectReferenceValue = bgImage;

            // Header Bar
            var headerObj = CreateUIElement("HeaderBar", canvasObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 90);
            var headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.07f, 0.08f, 0.12f, 0.95f);

            var backBtn = CreateButton("BackButton", headerObj.transform, "<", new Vector2(50, -45), new Vector2(60, 60));
            so.FindProperty("backButton").objectReferenceValue = backBtn;

            var titleTMP = CreateTMPText("TitleText", headerObj.transform, "ÁLBUM DE CARTAS", new Vector2(250, -45), new Vector2(400, 60), 38, TextAlignmentOptions.Left);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = Color.white;
            so.FindProperty("titleText").objectReferenceValue = titleTMP;

            // Deck Management Button ("MI MAZO")
            var deckBtn = CreateButton("DeckBtn", headerObj.transform, "MAZO (12/12)", new Vector2(405, -45), new Vector2(145, 48));
            var dImg = deckBtn.GetComponent<Image>();
            dImg.color = new Color(0.15f, 0.45f, 0.65f); // Deep oceanic blue
            var dTMP = deckBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (dTMP != null)
            {
                dTMP.fontSize = 15;
                dTMP.fontStyle = FontStyles.Bold;
                dTMP.color = Color.white;
            }
            so.FindProperty("deckButton").objectReferenceValue = deckBtn;
            so.FindProperty("deckButtonText").objectReferenceValue = dTMP;

            // Battle Combat Navigation Button ("COMBATE")
            var battleBtn = CreateButton("BattleBtn", headerObj.transform, "COMBATE", new Vector2(555, -45), new Vector2(145, 48));
            var batImg = battleBtn.GetComponent<Image>();
            batImg.color = new Color(0.85f, 0.25f, 0.20f); // Fiery combat crimson
            var batTMP = battleBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (batTMP != null)
            {
                batTMP.fontSize = 15;
                batTMP.fontStyle = FontStyles.Bold;
                batTMP.color = Color.white;
            }
            so.FindProperty("battleButton").objectReferenceValue = battleBtn;

            // Showcase 3D Navigation Button
            var showcaseBtn = CreateButton("Showcase3DBtn", headerObj.transform, "SHOWCASE", new Vector2(705, -45), new Vector2(145, 48));
            var scImg = showcaseBtn.GetComponent<Image>();
            scImg.color = new Color(0.12f, 0.42f, 0.62f);
            var scTMP = showcaseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (scTMP != null)
            {
                scTMP.fontSize = 15;
                scTMP.fontStyle = FontStyles.Bold;
                scTMP.color = Color.white;
            }
            so.FindProperty("showcase3DButton").objectReferenceValue = showcaseBtn;

            // Daily Reward Pack Button ("PACK DIARIO")
            var dailyBtn = CreateButton("DailyRewardBtn", headerObj.transform, "PACK DIARIO", new Vector2(855, -45), new Vector2(145, 48));
            var dailyImg = dailyBtn.GetComponent<Image>();
            dailyImg.color = new Color(0.45f, 0.20f, 0.65f); // Royal amethyst purple
            var dailyTMP = dailyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (dailyTMP != null)
            {
                dailyTMP.fontSize = 14;
                dailyTMP.fontStyle = FontStyles.Bold;
                dailyTMP.color = Color.white;
            }

            // Notification Badge (Red dot / exclamation)
            var dailyBadgeObj = CreateUIElement("DailyBadge", dailyBtn.transform);
            var dbRect = dailyBadgeObj.GetComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(1, 1);
            dbRect.anchorMax = new Vector2(1, 1);
            dbRect.pivot = new Vector2(0.5f, 0.5f);
            dbRect.anchoredPosition = new Vector2(-4, -4);
            dbRect.sizeDelta = new Vector2(22, 22);

            var dbImg = dailyBadgeObj.AddComponent<Image>();
            dbImg.color = new Color(0.95f, 0.20f, 0.20f, 1f);

            var dbText = CreateTMPText("Exclamation", dailyBadgeObj.transform, "!", Vector2.zero, new Vector2(22, 22), 14, TextAlignmentOptions.Center);
            dbText.fontStyle = FontStyles.Bold;
            dbText.color = Color.white;

            so.FindProperty("dailyRewardButton").objectReferenceValue = dailyBtn;
            so.FindProperty("dailyRewardBadge").objectReferenceValue = dailyBadgeObj;

            // Card Pack Shop Navigation Button ("SOBRES")
            var packsBtn = CreateButton("PacksBtn", headerObj.transform, "SOBRES", new Vector2(1005, -45), new Vector2(145, 48));
            var pImg = packsBtn.GetComponent<Image>();
            pImg.color = new Color(0.85f, 0.55f, 0.12f); // Warm gold/amber
            var pTMP = packsBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (pTMP != null)
            {
                pTMP.fontSize = 15;
                pTMP.fontStyle = FontStyles.Bold;
                pTMP.color = Color.white;
            }
            so.FindProperty("packsButton").objectReferenceValue = packsBtn;

            var coinTMP = CreateTMPText("GoldCoinsText", headerObj.transform, "1,256", new Vector2(-115, -45), new Vector2(100, 48), 28, TextAlignmentOptions.Right);
            coinTMP.fontStyle = FontStyles.Bold;
            coinTMP.color = new Color(1f, 0.85f, 0.2f);
            coinTMP.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            coinTMP.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            coinTMP.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            so.FindProperty("goldCoinsText").objectReferenceValue = coinTMP;

            var dustTMP = CreateTMPText("EsenciaDustText", headerObj.transform, "150", new Vector2(-225, -45), new Vector2(105, 48), 26, TextAlignmentOptions.Right);
            dustTMP.fontStyle = FontStyles.Bold;
            dustTMP.color = new Color(0.78f, 0.48f, 1f); // Glowing amethyst/sparkle
            dustTMP.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            dustTMP.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            dustTMP.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            so.FindProperty("esenciaDustText").objectReferenceValue = dustTMP;

            // Open Shop "+" Button next to Gold Coins
            var openShopBtn = CreateButton("OpenShopBtn", headerObj.transform, "+", new Vector2(-55, -45), new Vector2(46, 46));
            var osRect = openShopBtn.GetComponent<RectTransform>();
            osRect.anchorMin = new Vector2(1, 1);
            osRect.anchorMax = new Vector2(1, 1);
            osRect.pivot = new Vector2(1, 0.5f);
            var osImg = openShopBtn.GetComponent<Image>();
            osImg.color = new Color(1f, 0.82f, 0.15f);
            var osTMP = openShopBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (osTMP != null)
            {
                osTMP.color = new Color(0.1f, 0.1f, 0.1f);
                osTMP.fontSize = 28;
                osTMP.fontStyle = FontStyles.Bold;
            }
            so.FindProperty("openShopButton").objectReferenceValue = openShopBtn;

            // Player Nickname InputField (Editable)
            var nameInput = CreateTMPInputField("PlayerNameInputField", headerObj.transform, new Vector2(-340, -45), new Vector2(230, 48), "Tu apodo...");
            so.FindProperty("playerNameInput").objectReferenceValue = nameInput;

            // Account Level & XP Progress Widget
            var lvlWidgetObj = CreateUIElement("AccountLevelWidget", headerObj.transform);
            var lvlWidgetRect = lvlWidgetObj.GetComponent<RectTransform>();
            lvlWidgetRect.anchorMin = new Vector2(1, 1);
            lvlWidgetRect.anchorMax = new Vector2(1, 1);
            lvlWidgetRect.pivot = new Vector2(1, 0.5f);
            lvlWidgetRect.anchoredPosition = new Vector2(-590, -45);
            lvlWidgetRect.sizeDelta = new Vector2(240, 48);

            var lvlBg = lvlWidgetObj.AddComponent<Image>();
            lvlBg.color = new Color(0.12f, 0.13f, 0.2f, 0.95f);

            // Level Text Badge
            var lvlTextObj = CreateUIElement("AccountLevelText", lvlWidgetObj.transform);
            var lvlTextRect = lvlTextObj.GetComponent<RectTransform>();
            lvlTextRect.anchorMin = new Vector2(0, 1);
            lvlTextRect.anchorMax = new Vector2(0, 1);
            lvlTextRect.pivot = new Vector2(0, 1);
            lvlTextRect.anchoredPosition = new Vector2(10, -6);
            lvlTextRect.sizeDelta = new Vector2(100, 20);
            var lvlTMP = lvlTextObj.AddComponent<TextMeshProUGUI>();
            lvlTMP.text = "LVL 01";
            lvlTMP.fontSize = 16;
            lvlTMP.fontStyle = FontStyles.Bold;
            lvlTMP.color = new Color(0.2f, 0.85f, 1f);
            lvlTMP.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("accountLevelText").objectReferenceValue = lvlTMP;

            // Level XP Label (e.g. 0 / 100 XP)
            var xpTextObj = CreateUIElement("AccountXpText", lvlWidgetObj.transform);
            var xpTextRect = xpTextObj.GetComponent<RectTransform>();
            xpTextRect.anchorMin = new Vector2(1, 1);
            xpTextRect.anchorMax = new Vector2(1, 1);
            xpTextRect.pivot = new Vector2(1, 1);
            xpTextRect.anchoredPosition = new Vector2(-10, -6);
            xpTextRect.sizeDelta = new Vector2(110, 20);
            var xpTMP = xpTextObj.AddComponent<TextMeshProUGUI>();
            xpTMP.text = "0 / 100 XP";
            xpTMP.fontSize = 12;
            xpTMP.color = new Color(0.75f, 0.8f, 0.9f);
            xpTMP.alignment = TextAlignmentOptions.MidlineRight;
            so.FindProperty("accountLevelXpText").objectReferenceValue = xpTMP;

            // XP Slider Progress Bar
            var xpSliderObj = CreateUIElement("AccountXpSlider", lvlWidgetObj.transform);
            var xpSliderRect = xpSliderObj.GetComponent<RectTransform>();
            xpSliderRect.anchorMin = new Vector2(0.5f, 0);
            xpSliderRect.anchorMax = new Vector2(0.5f, 0);
            xpSliderRect.pivot = new Vector2(0.5f, 0);
            xpSliderRect.anchoredPosition = new Vector2(0, 8);
            xpSliderRect.sizeDelta = new Vector2(220, 8);

            var xpSlider = xpSliderObj.AddComponent<Slider>();
            xpSlider.minValue = 0;
            xpSlider.maxValue = 100;
            xpSlider.value = 0;

            var xpSliderBg = xpSliderObj.AddComponent<Image>();
            xpSliderBg.color = new Color(0.18f, 0.2f, 0.28f);

            var xpFillArea = CreateUIElement("FillArea", xpSliderObj.transform);
            SetStretch(xpFillArea.GetComponent<RectTransform>());
            var xpFill = CreateUIElement("Fill", xpFillArea.transform);
            var xpFillImg = xpFill.AddComponent<Image>();
            xpFillImg.color = new Color(0f, 0.85f, 0.95f);
            xpSlider.fillRect = xpFill.GetComponent<RectTransform>();
            so.FindProperty("accountLevelProgressBar").objectReferenceValue = xpSlider;

            // Player Avatar Button & Frame (adjacent to Account Level and Nickname)
            var avatarBtnObj = CreateUIElement("PlayerAvatarButton", headerObj.transform);
            var avatarBtnRect = avatarBtnObj.GetComponent<RectTransform>();
            avatarBtnRect.anchorMin = new Vector2(1, 1);
            avatarBtnRect.anchorMax = new Vector2(1, 1);
            avatarBtnRect.pivot = new Vector2(1, 0.5f);
            avatarBtnRect.anchoredPosition = new Vector2(-840, -45);
            avatarBtnRect.sizeDelta = new Vector2(56, 56);

            var avatarBtnBg = avatarBtnObj.AddComponent<Image>();
            avatarBtnBg.color = new Color(0.15f, 0.16f, 0.24f, 0.95f);
            var avatarBtn = avatarBtnObj.AddComponent<Button>();

            // Frame Outer Border
            var avatarBorderObj = CreateUIElement("FrameBorder", avatarBtnObj.transform);
            SetStretch(avatarBorderObj.GetComponent<RectTransform>());
            var avatarBorderImg = avatarBorderObj.AddComponent<Image>();
            avatarBorderImg.color = new Color(1f, 0.85f, 0.25f, 0.75f);
            avatarBorderImg.raycastTarget = false;

            // Avatar Inner Image
            var avatarImgObj = CreateUIElement("AvatarImage", avatarBtnObj.transform);
            var avatarImgRect = avatarImgObj.GetComponent<RectTransform>();
            avatarImgRect.anchorMin = Vector2.zero;
            avatarImgRect.anchorMax = Vector2.one;
            avatarImgRect.offsetMin = new Vector2(4, 4);
            avatarImgRect.offsetMax = new Vector2(-4, -4);

            var avatarImg = avatarImgObj.AddComponent<Image>();
            var defaultSprite = AvatarSpriteProvider.GetAvatarSprite(AvatarCatalog.DefaultAvatarId);
            if (defaultSprite != null)
            {
                avatarImg.sprite = defaultSprite;
                avatarImg.preserveAspect = true;
            }
            avatarImg.raycastTarget = false;

            so.FindProperty("playerAvatarButton").objectReferenceValue = avatarBtn;
            so.FindProperty("playerAvatarImage").objectReferenceValue = avatarImg;

            // Navigation Tabs Bar
            var tabsObj = CreateUIElement("CategoryTabs", canvasObj.transform);
            var tabsRect = tabsObj.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0, 1);
            tabsRect.anchorMax = new Vector2(1, 1);
            tabsRect.pivot = new Vector2(0.5f, 1);
            tabsRect.anchoredPosition = new Vector2(0, -90);
            tabsRect.sizeDelta = new Vector2(0, 60);
            var tabsBg = tabsObj.AddComponent<Image>();
            tabsBg.color = new Color(0.08f, 0.09f, 0.13f, 0.90f);

            var tabLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = true;
            tabLayout.spacing = 15;
            tabLayout.padding = new RectOffset(60, 0, 5, 5);

            var heroesBtn = CreateButton("HeroesTab", tabsObj.transform, "Héroes", Vector2.zero, new Vector2(140, 50));
            var bestiasBtn = CreateButton("BestiasTab", tabsObj.transform, "Bestias", Vector2.zero, new Vector2(140, 50));
            var tromposBtn = CreateButton("TromposTab", tabsObj.transform, "Trompos", Vector2.zero, new Vector2(140, 50));
            var arenasBtn = CreateButton("ArenasTab", tabsObj.transform, "Arenas", Vector2.zero, new Vector2(140, 50));

            so.FindProperty("heroesTabButton").objectReferenceValue = heroesBtn;
            so.FindProperty("bestiasTabButton").objectReferenceValue = bestiasBtn;
            so.FindProperty("tromposTabButton").objectReferenceValue = tromposBtn;
            so.FindProperty("arenasTabButton").objectReferenceValue = arenasBtn;

            // Clan Filter Dropdown
            var dropdown = CreateTMPDropdown("ClanDropdown", tabsObj.transform, new Vector2(240, 50));
            so.FindProperty("clanFilterDropdown").objectReferenceValue = dropdown;

            // Left Panel: "MIS CARTAS" Grid
            var leftPanel = CreateUIElement("LeftPanel_Cards", canvasObj.transform);
            var lpRect = leftPanel.GetComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0, 0);
            lpRect.anchorMax = new Vector2(0, 1);
            lpRect.pivot = new Vector2(0, 0.5f);
            lpRect.anchoredPosition = new Vector2(40, -80);
            lpRect.sizeDelta = new Vector2(520, -180);
            var lpBg = leftPanel.AddComponent<Image>();
            lpBg.color = new Color(0.10f, 0.11f, 0.16f, 0.95f);

            var gridTitle = CreateTMPText("GridTitle", leftPanel.transform, "MIS CARTAS", new Vector2(10, -20), new Vector2(300, 40), 26, TextAlignmentOptions.Left);
            gridTitle.fontStyle = FontStyles.Bold;
            gridTitle.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            gridTitle.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            so.FindProperty("gridHeaderTitle").objectReferenceValue = gridTitle;

            // Scroll View
            var scrollObj = CreateUIElement("ScrollView", leftPanel.transform);
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.anchoredPosition = new Vector2(0, -30);
            scrollRectTransform.sizeDelta = new Vector2(0, -60);
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportObj = CreateUIElement("Viewport", scrollObj.transform);
            SetStretch(viewportObj.GetComponent<RectTransform>());
            viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();

            var contentObj = CreateUIElement("Content", viewportObj.transform);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 600);
            scrollRect.content = contentRect;

            var gridLayout = contentObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(236, 330);
            gridLayout.spacing = new Vector2(16, 16);
            gridLayout.padding = new RectOffset(16, 16, 16, 16);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            var contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            so.FindProperty("gridContentParent").objectReferenceValue = contentObj.transform;

            // Build or load CardGridItem Prefab
            var itemPrefab = BuildCardGridItemPrefab();
            so.FindProperty("cardGridItemPrefab").objectReferenceValue = itemPrefab;

            // Center Viewport: 3D Card Anchor & 360 Viewer
            var centerAnchor = new GameObject("Center_Card3D_Viewport");
            centerAnchor.transform.position = new Vector3(0.18f, 0.15f, -4.5f);

            // Sleek Showcase Pedestal Platform under the card (stationary sibling, not child of rotating anchor)
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Showcase_Pedestal";
            pedestal.transform.position = new Vector3(0.18f, -1.95f, -4.5f);
            pedestal.transform.localScale = new Vector3(3.8f, 0.07f, 3.8f);
            var pedCol = pedestal.GetComponent<Collider>();
            if (pedCol != null) Object.DestroyImmediate(pedCol);

            var pedShader = Card3D.GetAppropriateShader(false);
            if (pedShader != null)
            {
                var pedMat = new Material(pedShader) { name = "Pedestal_Mat" };
                if (pedMat.HasProperty("_Color")) pedMat.SetColor("_Color", new Color(0.10f, 0.11f, 0.15f));
                if (pedMat.HasProperty("_BaseColor")) pedMat.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.15f));
                if (pedMat.HasProperty("_Metallic")) pedMat.SetFloat("_Metallic", 0.6f);
                if (pedMat.HasProperty("_Glossiness")) pedMat.SetFloat("_Glossiness", 0.7f);
                if (pedMat.HasProperty("_Smoothness")) pedMat.SetFloat("_Smoothness", 0.7f);
                var pedMr = pedestal.GetComponent<MeshRenderer>();
                pedMr.sharedMaterial = pedMat;
                pedMr.enabled = false; // Disabled to reveal the high-res pedestal artwork from the background
            }

            // Card3D component on the anchor
            var card3D = centerAnchor.AddComponent<Card3D>();
            card3D.EnsureHierarchy();
            so.FindProperty("card3D").objectReferenceValue = card3D;

            // Card3DViewer component for 360 free orbit, parallax tilt, flip & auto-spin
            var cardViewer = centerAnchor.AddComponent<Card3DViewer>();
            var viewerSo = new SerializedObject(cardViewer);
            viewerSo.FindProperty("targetCardTransform").objectReferenceValue = centerAnchor.transform;
            viewerSo.FindProperty("showcaseCamera").objectReferenceValue = camera;
            viewerSo.ApplyModifiedProperties();
            so.FindProperty("card3DViewer").objectReferenceValue = cardViewer;

            // Center Controls Panel (replacing simple SwipePrompt in the middle space)
            var centerControlsPanel = CreateUIElement("Center_3D_ControlsPanel", canvasObj.transform);
            var ccpRect = centerControlsPanel.GetComponent<RectTransform>();
            ccpRect.anchorMin = new Vector2(0.5f, 0.5f);
            ccpRect.anchorMax = new Vector2(0.5f, 0.5f);
            ccpRect.pivot = new Vector2(0.5f, 0.5f);
            ccpRect.anchoredPosition = new Vector2(30, -410);
            ccpRect.sizeDelta = new Vector2(520, 130);

            // Drag hint text
            var hintTMP = CreateTMPText("SwipePrompt", centerControlsPanel.transform, "Arrastra libremente para rotar en 360°", new Vector2(0, 48), new Vector2(500, 24), 16, TextAlignmentOptions.Center);
            hintTMP.color = new Color(0.75f, 0.82f, 0.92f, 0.9f);
            so.FindProperty("swipeToRotateIndicator").objectReferenceValue = hintTMP.gameObject;

            // Middle Action Buttons Row: Flip 180°, Auto-Spin, Reset Front
            var actionsRow = CreateUIElement("ActionsRow", centerControlsPanel.transform);
            var arRect = actionsRow.GetComponent<RectTransform>();
            arRect.anchoredPosition = new Vector2(0, 14);
            arRect.sizeDelta = new Vector2(500, 42);

            var arLayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
            arLayout.spacing = 10;
            arLayout.childAlignment = TextAnchor.MiddleCenter;
            arLayout.childControlWidth = false;
            arLayout.childControlHeight = true;

            var flipBtn = CreateButton("FlipButton", actionsRow.transform, "VOLTEAR", Vector2.zero, new Vector2(156, 40));
            var spinBtn = CreateButton("AutoSpinButton", actionsRow.transform, "AUTO-GIRO", Vector2.zero, new Vector2(156, 40));
            var frontBtn = CreateButton("ResetFrontButton", actionsRow.transform, "FRENTE", Vector2.zero, new Vector2(156, 40));

            flipBtn.GetComponent<Image>().color = new Color(0.18f, 0.42f, 0.65f);
            spinBtn.GetComponent<Image>().color = new Color(0.42f, 0.22f, 0.62f);
            frontBtn.GetComponent<Image>().color = new Color(0.20f, 0.48f, 0.30f);

            so.FindProperty("flipCardButton").objectReferenceValue = flipBtn;
            so.FindProperty("autoSpinButton").objectReferenceValue = spinBtn;
            so.FindProperty("autoSpinButtonText").objectReferenceValue = spinBtn.GetComponentInChildren<TextMeshProUGUI>();
            so.FindProperty("resetFrontButton").objectReferenceValue = frontBtn;

            // Reverso Selector Row: Oro, Color, Bicolor
            var reversoRow = CreateUIElement("ReversoRow", centerControlsPanel.transform);
            var rrRect = reversoRow.GetComponent<RectTransform>();
            rrRect.anchoredPosition = new Vector2(0, -32);
            rrRect.sizeDelta = new Vector2(500, 36);

            var rrLayout = reversoRow.AddComponent<HorizontalLayoutGroup>();
            rrLayout.spacing = 10;
            rrLayout.childAlignment = TextAnchor.MiddleCenter;
            rrLayout.childControlWidth = false;
            rrLayout.childControlHeight = true;

            var revOro = CreateButton("Rev_Oro", reversoRow.transform, "ORO", Vector2.zero, new Vector2(156, 36));
            var revColor = CreateButton("Rev_Color", reversoRow.transform, "COLOR", Vector2.zero, new Vector2(156, 36));
            var revBi = CreateButton("Rev_Bi", reversoRow.transform, "BICOLOR", Vector2.zero, new Vector2(156, 36));

            so.FindProperty("reversoOroButton").objectReferenceValue = revOro;
            so.FindProperty("reversoColorButton").objectReferenceValue = revColor;
            so.FindProperty("reversoBiButton").objectReferenceValue = revBi;

            // Right Panel: Card Inspector
            var rightPanel = CreateUIElement("RightPanel_Inspector", canvasObj.transform);
            var rpRect = rightPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(1, 0);
            rpRect.anchorMax = new Vector2(1, 1);
            rpRect.pivot = new Vector2(1, 0.5f);
            rpRect.anchoredPosition = new Vector2(-40, -70);
            rpRect.sizeDelta = new Vector2(460, -160);

            var rpBg = rightPanel.AddComponent<Image>();
            rpBg.color = new Color(0.12f, 0.13f, 0.19f, 0.95f);

            var cardNameTMP = CreateTMPText("CardName", rightPanel.transform, "ESPECTRO", new Vector2(25, -30), new Vector2(400, 45), 36, TextAlignmentOptions.Left);
            cardNameTMP.fontStyle = FontStyles.Bold;
            so.FindProperty("cardNameText").objectReferenceValue = cardNameTMP;

            var levelTMP = CreateTMPText("CardLevel", rightPanel.transform, "LVL 06", new Vector2(25, -70), new Vector2(150, 30), 22, TextAlignmentOptions.Left);
            levelTMP.color = new Color(1f, 0.5f, 0.8f);
            so.FindProperty("cardLevelText").objectReferenceValue = levelTMP;

            var clanTMP = CreateTMPText("CardClan", rightPanel.transform, "CLAN: Depredadores", new Vector2(180, -70), new Vector2(250, 30), 22, TextAlignmentOptions.Left);
            so.FindProperty("cardClanText").objectReferenceValue = clanTMP;

            var advantageTMP = CreateTMPText("ClanAdvantage", rightPanel.transform, "• Ventaja contra: Míticos (+1 Daño)", new Vector2(25, -100), new Vector2(410, 30), 18, TextAlignmentOptions.Left);
            advantageTMP.color = new Color(0.2f, 1f, 0.5f);
            so.FindProperty("clanAdvantageText").objectReferenceValue = advantageTMP;

            // Stats Row
            var statsRow = CreateUIElement("StatsContainer", rightPanel.transform);
            var srRect = statsRow.GetComponent<RectTransform>();
            srRect.anchoredPosition = new Vector2(25, -145);
            srRect.sizeDelta = new Vector2(410, 50);

            var costVal = CreateTMPText("CostVal", statsRow.transform, "COSTO: 4", new Vector2(0, 0), new Vector2(130, 40), 20, TextAlignmentOptions.Left);
            var atkVal = CreateTMPText("AtkVal", statsRow.transform, "FUERZA: 6", new Vector2(140, 0), new Vector2(130, 40), 20, TextAlignmentOptions.Left);
            var hpVal = CreateTMPText("HpVal", statsRow.transform, "VIDA: 8", new Vector2(280, 0), new Vector2(130, 40), 20, TextAlignmentOptions.Left);

            so.FindProperty("statsContainer").objectReferenceValue = statsRow;
            so.FindProperty("costText").objectReferenceValue = costVal;
            so.FindProperty("attackText").objectReferenceValue = atkVal;
            so.FindProperty("healthText").objectReferenceValue = hpVal;

            // Rules Box
            var rulesBox = CreateUIElement("RulesBox", rightPanel.transform);
            var rbRect = rulesBox.GetComponent<RectTransform>();
            rbRect.anchoredPosition = new Vector2(25, -230);
            rbRect.sizeDelta = new Vector2(410, 110);
            var rbImg = rulesBox.AddComponent<Image>();
            rbImg.color = new Color(0.08f, 0.09f, 0.14f, 0.9f);

            var rulesTMP = CreateTMPText("RulesText", rulesBox.transform, "<b>REGLAS TCG:</b> Cuando es invocado, añade un Trompo a tu mano.", new Vector2(10, -10), new Vector2(390, 90), 16, TextAlignmentOptions.TopLeft);
            rulesTMP.textWrappingMode = TextWrappingModes.Normal;
            so.FindProperty("rulesText").objectReferenceValue = rulesTMP;

            // Ability & Lore
            var abilityTMP = CreateTMPText("AbilityQuote", rightPanel.transform, "<i>\"¡El misterio es mi fuerza!\"</i>", new Vector2(25, -360), new Vector2(410, 35), 18, TextAlignmentOptions.Left);
            abilityTMP.color = new Color(1f, 0.85f, 0.3f);
            so.FindProperty("activeAbilityText").objectReferenceValue = abilityTMP;

            var loreTMP = CreateTMPText("LoreText", rightPanel.transform, "Trompo metálico de los ancestros del panal, capaz de atravesar cualquier defensa.", new Vector2(25, -410), new Vector2(410, 70), 16, TextAlignmentOptions.TopLeft);
            loreTMP.textWrappingMode = TextWrappingModes.Normal;
            loreTMP.color = new Color(0.7f, 0.75f, 0.85f);
            so.FindProperty("loreText").objectReferenceValue = loreTMP;

            // Foil Progression Bar
            var foilTitle = CreateTMPText("FoilHeader", rightPanel.transform, "PROGRESO FOIL", new Vector2(25, -500), new Vector2(410, 25), 18, TextAlignmentOptions.Left);
            foilTitle.fontStyle = FontStyles.Bold;

            var sliderObj = CreateUIElement("FoilSlider", rightPanel.transform);
            var slRect = sliderObj.GetComponent<RectTransform>();
            slRect.anchoredPosition = new Vector2(25, -535);
            slRect.sizeDelta = new Vector2(410, 20);
            var slider = sliderObj.AddComponent<Slider>();
            slider.maxValue = 5;
            slider.value = 2;

            var sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.2f, 0.25f);

            var fillArea = CreateUIElement("FillArea", sliderObj.transform);
            SetStretch(fillArea.GetComponent<RectTransform>());
            var fill = CreateUIElement("Fill", fillArea.transform);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.15f, 0.85f, 0.35f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            var foilProgressTMP = CreateTMPText("FoilProgressText", rightPanel.transform, "Cartas obtenidas: 2/5 para versión Foil", new Vector2(25, -565), new Vector2(410, 22), 15, TextAlignmentOptions.Left);
            foilProgressTMP.color = new Color(0.8f, 0.8f, 0.85f);

            so.FindProperty("foilProgressBar").objectReferenceValue = slider;
            so.FindProperty("foilProgressLabel").objectReferenceValue = foilProgressTMP;

            // Upgrade Button
            var upgradeBtn = CreateButton("UpgradeFoilBtn", rightPanel.transform, "UPGRADE FOIL  (120 GOLD)", new Vector2(205, -630), new Vector2(380, 65));
            var ubImg = upgradeBtn.GetComponent<Image>();
            ubImg.color = new Color(1f, 0.82f, 0.1f);
            var ubTMP = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (ubTMP != null)
            {
                ubTMP.color = new Color(0.1f, 0.1f, 0.1f);
                ubTMP.fontSize = 22;
                ubTMP.fontStyle = FontStyles.Bold;
            }
            so.FindProperty("upgradeFoilButton").objectReferenceValue = upgradeBtn;

            var costLabel = CreateTMPText("UpgradeCostText", upgradeBtn.transform, "120", Vector2.zero, new Vector2(80, 30), 18, TextAlignmentOptions.Center);
            costLabel.gameObject.SetActive(false); // Hidden helper
            so.FindProperty("upgradeCostText").objectReferenceValue = costLabel;

            // Toggle In Deck Button (Card Details)
            var deckToggleBtn = CreateButton("ToggleDeckBtn", rightPanel.transform, "+ AÑADIR AL MAZO (0/2)", new Vector2(205, -705), new Vector2(380, 52));
            var dtImg = deckToggleBtn.GetComponent<Image>();
            dtImg.color = new Color(0.18f, 0.45f, 0.75f);
            var dtTMP = deckToggleBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (dtTMP != null)
            {
                dtTMP.fontSize = 17;
                dtTMP.fontStyle = FontStyles.Bold;
                dtTMP.color = Color.white;
            }
            so.FindProperty("toggleDeckCardButton").objectReferenceValue = deckToggleBtn;
            so.FindProperty("toggleDeckCardButtonText").objectReferenceValue = dtTMP;

            // Craft Card Button (Option A - Esencia Cometa)
            var craftBtn = CreateButton("CraftCardBtn", rightPanel.transform, "+ CRAFTEAR CARTA (100 ESENCIA)", new Vector2(205, -770), new Vector2(380, 50));
            var cbImg = craftBtn.GetComponent<Image>();
            cbImg.color = new Color(0.35f, 0.15f, 0.55f); // Deep arcane amethyst
            var cbTMP = craftBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbTMP != null)
            {
                cbTMP.fontSize = 17;
                cbTMP.fontStyle = FontStyles.Bold;
                cbTMP.color = new Color(0.95f, 0.85f, 1f); // Radiant lavender/cyan
            }
            so.FindProperty("craftCardButton").objectReferenceValue = craftBtn;
            so.FindProperty("craftCardButtonText").objectReferenceValue = cbTMP;

            // 4. Shop Modal (IAP Coin Packs & Unity Ads Rewarded Video)
            BuildShopModal(canvasObj.transform, so);

            // 5. Avatar Selection Modal (20 Champions by Progression Level & Gold)
            BuildAvatarModal(canvasObj.transform, so);

            // 6. Daily Reward Modal (7-Day Cyclical Pack with Cards, Gold, Dust, Booster, Avatar)
            BuildDailyRewardModal(canvasObj.transform, so);

            // 7. Deck Management Modal (12-Card Combat Deck strictly from owned inventory)
            BuildDeckModal(canvasObj.transform, so);

            // 8. Card Pack Opening Modal (Rule 11) & Pack Shop Modal (Rule 1)
            var openingModal = BuildPackOpeningModal(canvasObj.transform, so);
            BuildPackShopModal(canvasObj.transform, so, openingModal);

            so.ApplyModifiedProperties();

            // 5. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 6. Set in Build Settings
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Battle.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/CardShowcase.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HomeSceneBuilder] Successfully built Home scene at {ScenePath}!");
        }

        private static GameObject BuildCardGridItemPrefab()
        {
            if (File.Exists(GridItemPrefabPath))
            {
                AssetDatabase.DeleteAsset(GridItemPrefabPath);
            }

            var itemObj = new GameObject("CardGridItem", typeof(RectTransform));
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(236, 330);

            var bgImg = itemObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.13f, 0.18f);

            var btn = itemObj.AddComponent<Button>();

            // Full card-size thumbnail filling 100% of the box
            var thumbObj = CreateUIElement("Thumbnail", itemObj.transform);
            var thumbRect = thumbObj.GetComponent<RectTransform>();
            SetStretch(thumbRect);
            var thumbImg = thumbObj.AddComponent<Image>();
            thumbImg.color = Color.white;
            thumbImg.preserveAspect = false;

            // Name Banner at bottom (compact, stylish, doesn't hide artwork)
            var nameBanner = CreateUIElement("NameBanner", itemObj.transform);
            var nbRect = nameBanner.GetComponent<RectTransform>();
            nbRect.anchorMin = new Vector2(0, 0);
            nbRect.anchorMax = new Vector2(1, 0);
            nbRect.pivot = new Vector2(0.5f, 0);
            nbRect.anchoredPosition = Vector2.zero;
            nbRect.sizeDelta = new Vector2(0, 20);
            var nbImg = nameBanner.AddComponent<Image>();
            nbImg.color = new Color(0.05f, 0.06f, 0.10f, 0.82f);
            nbImg.raycastTarget = false;

            var nameTMP = CreateTMPText("NameLabel", nameBanner.transform, "Name", Vector2.zero, Vector2.zero, 11, TextAlignmentOptions.Center);
            SetStretch(nameTMP.GetComponent<RectTransform>());
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            nameTMP.raycastTarget = false;

            // Lock Overlay with official LockIcon Image
            var lockObj = CreateUIElement("LockOverlay", itemObj.transform);
            SetStretch(lockObj.GetComponent<RectTransform>());
            var lockImg = lockObj.AddComponent<Image>();
            lockImg.color = new Color(0.04f, 0.05f, 0.08f, 0.45f);
            lockImg.raycastTarget = false;

            var lockIconObj = CreateUIElement("LockIcon", lockObj.transform);
            var lockIconRect = lockIconObj.GetComponent<RectTransform>();
            lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockIconRect.pivot = new Vector2(0.5f, 0.5f);
            lockIconRect.anchoredPosition = Vector2.zero;
            lockIconRect.sizeDelta = new Vector2(56, 56);
            var lockIconImg = lockIconObj.AddComponent<Image>();
            lockIconImg.sprite = Card3D.GetLockIconSprite();
            lockIconImg.color = new Color(1f, 0.85f, 0.25f, 1f); // Vibrant gold
            lockIconImg.preserveAspect = true;
            lockIconImg.raycastTarget = false;

            lockObj.SetActive(false);

            // Selection Border (glowing golden frame)
            var borderObj = CreateUIElement("SelectedBorder", itemObj.transform);
            SetStretch(borderObj.GetComponent<RectTransform>());
            var borderOutline = borderObj.AddComponent<Outline>();
            borderOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.95f);
            borderOutline.effectDistance = new Vector2(3, -3);
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.85f, 0.2f, 0.15f);
            borderImg.raycastTarget = false;
            borderObj.SetActive(false);

            var comp = itemObj.AddComponent<CardGridItem>();
            var so = new SerializedObject(comp);
            so.FindProperty("cardThumbnail").objectReferenceValue = thumbImg;
            so.FindProperty("frameImage").objectReferenceValue = bgImg;
            so.FindProperty("nameLabel").objectReferenceValue = nameTMP;
            so.FindProperty("lockOverlay").objectReferenceValue = lockObj;
            so.FindProperty("lockIconImage").objectReferenceValue = lockIconImg;
            so.FindProperty("selectedBorder").objectReferenceValue = borderObj;
            so.FindProperty("clickButton").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(itemObj, GridItemPrefabPath);
            Object.DestroyImmediate(itemObj);
            return prefab;
        }

        private static CardView3D BuildCardView3D(GameObject parent)
        {
            var cardRoot = new GameObject("Card_3D_Model");
            cardRoot.transform.SetParent(parent.transform, false);
            cardRoot.transform.localPosition = Vector3.zero;

            // Front Quad
            var frontQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontQuad.name = "FrontFace";
            frontQuad.transform.SetParent(cardRoot.transform, false);
            frontQuad.transform.localScale = new Vector3(2.5f, 3.5f, 1f);

            var cardView = cardRoot.AddComponent<CardView3D>();
            var so = new SerializedObject(cardView);
            so.FindProperty("cardTransform").objectReferenceValue = cardRoot.transform;
            so.FindProperty("frontMeshRenderer").objectReferenceValue = frontQuad.GetComponent<MeshRenderer>();

            // TMP text overlays on the 3D card
            var nameTMP = Create3DTMP("NameText", frontQuad.transform, "NAME", new Vector3(0, -1.3f, -0.01f), 2.8f);
            var costTMP = Create3DTMP("CostText", frontQuad.transform, "4", new Vector3(-0.95f, 1.4f, -0.01f), 3.2f);
            var atkTMP = Create3DTMP("AttackText", frontQuad.transform, "6", new Vector3(-0.9f, -1.35f, -0.01f), 3f);
            var hpTMP = Create3DTMP("HealthText", frontQuad.transform, "8", new Vector3(0.9f, -1.35f, -0.01f), 3f);
            var abilityTMP = Create3DTMP("AbilityText", frontQuad.transform, "", new Vector3(0, -0.6f, -0.01f), 1.8f);

            so.FindProperty("nameText").objectReferenceValue = nameTMP;
            so.FindProperty("costText").objectReferenceValue = costTMP;
            so.FindProperty("attackText").objectReferenceValue = atkTMP;
            so.FindProperty("healthText").objectReferenceValue = hpTMP;
            so.FindProperty("abilityText").objectReferenceValue = abilityTMP;

            // Back Quad
            var backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backQuad.name = "BackFace";
            backQuad.transform.SetParent(cardRoot.transform, false);
            backQuad.transform.localRotation = Quaternion.Euler(0, 180, 0);
            backQuad.transform.localScale = new Vector3(2.5f, 3.5f, 1f);

            so.ApplyModifiedProperties();
            return cardView;
        }

        private static TextMeshPro Create3DTMP(string name, Transform parent, string text, Vector3 pos, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.8f);
            return tmp;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var go = CreateUIElement(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.25f, 0.35f);

            var btn = go.AddComponent<Button>();

            var label = CreateTMPText("Text", go.transform, text, Vector2.zero, size, 18, TextAlignmentOptions.Center);
            label.color = Color.white;
            label.raycastTarget = false;

            return btn;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = CreateUIElement(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static TMP_Dropdown CreateTMPDropdown(string name, Transform parent, Vector2 size)
        {
            var root = CreateUIElement(name, parent);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = size;

            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.18f, 0.2f, 0.28f);

            var dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rootImage;

            // Caption Label
            var label = CreateTMPText("Label", root.transform, "Todos los Clanes", new Vector2(-15, 0), new Vector2(size.x - 40, size.y), 18, TextAlignmentOptions.MidlineLeft);
            dropdown.captionText = label;

            // Arrow indicator
            var arrow = CreateTMPText("Arrow", root.transform, "v", new Vector2(size.x / 2 - 20, 0), new Vector2(30, size.y), 14, TextAlignmentOptions.Center);
            arrow.color = new Color(0.8f, 0.8f, 0.9f);

            // Template (Dropdown popup window)
            var template = CreateUIElement("Template", root.transform);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, -2);
            templateRect.sizeDelta = new Vector2(0, 220);

            var templateImg = template.AddComponent<Image>();
            templateImg.color = new Color(0.12f, 0.13f, 0.18f);

            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = CreateUIElement("Viewport", template.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetStretch(viewportRect);
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            // Content
            var content = CreateUIElement("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 40);
            scrollRect.content = contentRect;

            // Item (Toggle)
            var item = CreateUIElement("Item", content.transform);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 40);

            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.isOn = true;

            // Item Background
            var itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.18f, 0.2f, 0.28f);
            itemToggle.targetGraphic = itemBg;

            // Item Checkmark
            var checkmark = CreateUIElement("Item Checkmark", item.transform);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(20, 0);
            checkmarkRect.sizeDelta = new Vector2(20, 20);
            var checkmarkImg = checkmark.AddComponent<Image>();
            checkmarkImg.color = new Color(0.2f, 0.8f, 0.4f);
            itemToggle.graphic = checkmarkImg;

            // Item Label
            var itemLabel = CreateTMPText("Item Label", item.transform, "Option", new Vector2(25, 0), new Vector2(size.x - 60, 40), 18, TextAlignmentOptions.MidlineLeft);
            itemLabel.color = Color.white;

            // Link references to Dropdown
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            // Template MUST be inactive initially
            template.SetActive(false);

            return dropdown;
        }

        private static TMP_InputField CreateTMPInputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholderText)
        {
            var rootObj = CreateUIElement(name, parent);
            var rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1, 1);
            rootRect.anchorMax = new Vector2(1, 1);
            rootRect.pivot = new Vector2(1, 0.5f);
            rootRect.anchoredPosition = pos;
            rootRect.sizeDelta = size;

            var bgImg = rootObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.16f, 0.24f, 0.95f);

            var inputField = rootObj.AddComponent<TMP_InputField>();
            inputField.targetGraphic = bgImg;
            inputField.characterLimit = 16;

            // Text Area Viewport
            var textAreaObj = CreateUIElement("Text Area", rootObj.transform);
            var textAreaRect = textAreaObj.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.pivot = new Vector2(0.5f, 0.5f);
            textAreaRect.offsetMin = new Vector2(12, 5);
            textAreaRect.offsetMax = new Vector2(-12, -5);
            textAreaObj.AddComponent<RectMask2D>();
            inputField.textViewport = textAreaRect;

            // Placeholder Text
            var placeholderObj = CreateUIElement("Placeholder", textAreaObj.transform);
            SetStretch(placeholderObj.GetComponent<RectTransform>());
            var placeholderTMP = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = placeholderText;
            placeholderTMP.fontSize = 18;
            placeholderTMP.fontStyle = FontStyles.Italic;
            placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderTMP.color = new Color(0.6f, 0.6f, 0.7f, 0.6f);
            inputField.placeholder = placeholderTMP;

            // Editable Text Component
            var textObj = CreateUIElement("Text", textAreaObj.transform);
            SetStretch(textObj.GetComponent<RectTransform>());
            var textTMP = textObj.AddComponent<TextMeshProUGUI>();
            textTMP.fontSize = 20;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;
            textTMP.color = new Color(0.9f, 0.95f, 1f);
            inputField.textComponent = textTMP;

            return inputField;
        }

        private static void BuildShopModal(Transform canvasTransform, SerializedObject so)
        {
            // Root Overlay (dimmed backdrop)
            var modalRoot = CreateUIElement("ShopModalRoot", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());
            var overlayImg = modalRoot.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.78f);
            so.FindProperty("shopModalRoot").objectReferenceValue = modalRoot;

            // Modal Dialog Window
            var dialogObj = CreateUIElement("ShopDialogWindow", modalRoot.transform);
            var dialogRect = dialogObj.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(920, 640);

            var dialogImg = dialogObj.AddComponent<Image>();
            dialogImg.color = new Color(0.10f, 0.11f, 0.17f, 0.98f);

            // Dialog Header Bar
            var headerObj = CreateUIElement("DialogHeader", dialogObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 85);
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = new Color(0.14f, 0.15f, 0.23f, 1f);

            // Title Text
            var titleTMP = CreateTMPText("ShopTitleText", headerObj.transform, "TIENDA DE MONEDAS", new Vector2(0, -22), new Vector2(500, 36), 28, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.85f, 0.2f);

            // Subtitle / Modal Balance
            var balanceTMP = CreateTMPText("ModalBalanceText", headerObj.transform, "Saldo actual: 1,256 Monedas", new Vector2(0, -56), new Vector2(500, 24), 16, TextAlignmentOptions.Center);
            balanceTMP.color = new Color(0.85f, 0.85f, 0.9f);
            so.FindProperty("modalGoldBalanceText").objectReferenceValue = balanceTMP;

            // Close Button "X"
            var closeBtn = CreateButton("CloseShopBtn", headerObj.transform, "X", new Vector2(-20, -20), new Vector2(44, 44));
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1, 1);
            cbRect.anchorMax = new Vector2(1, 1);
            cbRect.pivot = new Vector2(1, 1);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.color = new Color(0.35f, 0.15f, 0.15f);
            var cbTMP = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbTMP != null)
            {
                cbTMP.fontSize = 22;
                cbTMP.fontStyle = FontStyles.Bold;
                cbTMP.color = Color.white;
            }
            so.FindProperty("closeShopButton").objectReferenceValue = closeBtn;

            // 3 Pack Cards Container
            var cardsObj = CreateUIElement("CoinPacksContainer", dialogObj.transform);
            var cardsRect = cardsObj.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.5f, 1);
            cardsRect.anchorMax = new Vector2(0.5f, 1);
            cardsRect.pivot = new Vector2(0.5f, 1);
            cardsRect.anchoredPosition = new Vector2(0, -100);
            cardsRect.sizeDelta = new Vector2(860, 300);

            // Card 1: 1,000 Coins
            var pack1Btn = CreateCoinPackCard(cardsObj.transform, new Vector2(-285, 0), "1,000", "MONEDAS", "Básico", "$0.99 USD", "");
            so.FindProperty("buyPack1000Button").objectReferenceValue = pack1Btn;

            // Card 2: 5,000 Coins
            var pack2Btn = CreateCoinPackCard(cardsObj.transform, new Vector2(0, 0), "5,000", "MONEDAS", "Aventurero", "$4.99 USD", "POPULAR");
            so.FindProperty("buyPack5000Button").objectReferenceValue = pack2Btn;

            // Card 3: 10,000 Coins
            var pack3Btn = CreateCoinPackCard(cardsObj.transform, new Vector2(285, 0), "10,000", "MONEDAS", "Bóveda", "$8.99 USD", "MEJOR VALOR");
            so.FindProperty("buyPack10000Button").objectReferenceValue = pack3Btn;

            // Rewarded Ad Section
            var adSection = CreateUIElement("RewardedAdSection", dialogObj.transform);
            var adRect = adSection.GetComponent<RectTransform>();
            adRect.anchorMin = new Vector2(0.5f, 0);
            adRect.anchorMax = new Vector2(0.5f, 0);
            adRect.pivot = new Vector2(0.5f, 0);
            adRect.anchoredPosition = new Vector2(0, 75);
            adRect.sizeDelta = new Vector2(860, 110);
            var adImg = adSection.AddComponent<Image>();
            adImg.color = new Color(0.14f, 0.18f, 0.25f, 1f);

            var adTitleTMP = CreateTMPText("AdTitle", adSection.transform, "¡MONEDAS GRATIS CON UNITY ADS!", new Vector2(25, -28), new Vector2(520, 30), 19, TextAlignmentOptions.Left);
            adTitleTMP.fontStyle = FontStyles.Bold;
            adTitleTMP.color = new Color(0.2f, 0.85f, 1f);

            var adDescTMP = CreateTMPText("AdDesc", adSection.transform, "Mira un breve anuncio patrocinado y recibe +100 Monedas de Oro gratis al instante.", new Vector2(25, -65), new Vector2(520, 40), 14, TextAlignmentOptions.Left);
            adDescTMP.color = new Color(0.75f, 0.8f, 0.9f);

            var watchBtn = CreateButton("WatchAdBtn", adSection.transform, "VER ANUNCIO\n(+100 ORO)", new Vector2(-30, 0), new Vector2(230, 68));
            var wbRect = watchBtn.GetComponent<RectTransform>();
            wbRect.anchorMin = new Vector2(1, 0.5f);
            wbRect.anchorMax = new Vector2(1, 0.5f);
            wbRect.pivot = new Vector2(1, 0.5f);
            var wbImg = watchBtn.GetComponent<Image>();
            wbImg.color = new Color(0f, 0.8f, 0.7f);
            var wbTMP = watchBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (wbTMP != null)
            {
                wbTMP.fontSize = 18;
                wbTMP.fontStyle = FontStyles.Bold;
                wbTMP.color = new Color(0.05f, 0.1f, 0.1f);
            }
            so.FindProperty("watchAdButton").objectReferenceValue = watchBtn;

            // Status notification label
            var statusTMP = CreateTMPText("ShopStatusText", dialogObj.transform, "", new Vector2(0, 25), new Vector2(840, 30), 16, TextAlignmentOptions.Center);
            var stRect = statusTMP.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.5f, 0);
            stRect.anchorMax = new Vector2(0.5f, 0);
            stRect.pivot = new Vector2(0.5f, 0);
            statusTMP.fontStyle = FontStyles.Bold;
            so.FindProperty("shopStatusText").objectReferenceValue = statusTMP;

            // Start inactive
            modalRoot.SetActive(false);
        }

        private static Button CreateCoinPackCard(Transform parent, Vector2 pos, string amountText, string subtitleText, string tierLabel, string priceText, string badgeText)
        {
            var cardObj = CreateUIElement($"PackCard_{amountText}", parent);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 1);
            cardRect.anchorMax = new Vector2(0.5f, 1);
            cardRect.pivot = new Vector2(0.5f, 1);
            cardRect.anchoredPosition = pos;
            cardRect.sizeDelta = new Vector2(270, 290);

            var bgImg = cardObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.17f, 0.25f, 1f);

            if (!string.IsNullOrEmpty(badgeText))
            {
                var badgeObj = CreateUIElement("Badge", cardObj.transform);
                var badgeRect = badgeObj.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.5f, 1);
                badgeRect.anchorMax = new Vector2(0.5f, 1);
                badgeRect.pivot = new Vector2(0.5f, 1);
                badgeRect.anchoredPosition = new Vector2(0, -8);
                badgeRect.sizeDelta = new Vector2(160, 24);
                var badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.color = new Color(0.9f, 0.45f, 0.1f, 1f);

                var badgeTMP = CreateTMPText("BadgeText", badgeObj.transform, badgeText, Vector2.zero, new Vector2(160, 24), 12, TextAlignmentOptions.Center);
                badgeTMP.fontStyle = FontStyles.Bold;
                badgeTMP.color = Color.white;
            }

            var amountTMP = CreateTMPText("AmountText", cardObj.transform, amountText, new Vector2(0, -50), new Vector2(240, 42), 32, TextAlignmentOptions.Center);
            amountTMP.fontStyle = FontStyles.Bold;
            amountTMP.color = new Color(1f, 0.85f, 0.2f);

            var subTMP = CreateTMPText("SubtitleText", cardObj.transform, subtitleText, new Vector2(0, -90), new Vector2(240, 22), 15, TextAlignmentOptions.Center);
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.color = new Color(0.85f, 0.85f, 0.9f);

            var tierTMP = CreateTMPText("TierText", cardObj.transform, tierLabel, new Vector2(0, -125), new Vector2(240, 26), 16, TextAlignmentOptions.Center);
            tierTMP.color = new Color(0.3f, 0.85f, 1f);

            var buyBtn = CreateButton("BuyButton", cardObj.transform, $"COMPRAR  {priceText}", new Vector2(0, 25), new Vector2(230, 50));
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            var btnImg = buyBtn.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.75f, 0.35f);
            var btnTMP = buyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTMP != null)
            {
                btnTMP.fontSize = 17;
                btnTMP.fontStyle = FontStyles.Bold;
                btnTMP.color = Color.white;
            }

            return buyBtn;
        }

        private static void BuildAvatarModal(Transform canvasTransform, SerializedObject homeSo)
        {
            // Root Overlay (dimmed backdrop)
            var modalRoot = CreateUIElement("AvatarModalRoot", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());
            var overlayImg = modalRoot.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.82f);

            var modalComponent = modalRoot.AddComponent<AvatarSelectionModal>();
            var modalSo = new SerializedObject(modalComponent);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalRoot;

            // Modal Dialog Window
            var dialogObj = CreateUIElement("AvatarDialogWindow", modalRoot.transform);
            var dialogRect = dialogObj.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(960, 680);

            var dialogImg = dialogObj.AddComponent<Image>();
            dialogImg.color = new Color(0.10f, 0.11f, 0.17f, 0.98f);

            // Dialog Header Bar
            var headerObj = CreateUIElement("DialogHeader", dialogObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 85);
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = new Color(0.14f, 0.15f, 0.23f, 1f);

            // Title Text
            var titleTMP = CreateTMPText("AvatarTitleText", headerObj.transform, "SELECCIONAR AVATAR", new Vector2(0, -22), new Vector2(500, 36), 28, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.85f, 0.2f);
            modalSo.FindProperty("modalTitleText").objectReferenceValue = titleTMP;

            // Subtitle / Player Info
            var infoTMP = CreateTMPText("ModalGoldText", headerObj.transform, "Tu Oro: 1,256 ORO  |  Nivel: 1", new Vector2(0, -56), new Vector2(600, 24), 16, TextAlignmentOptions.Center);
            infoTMP.color = new Color(0.85f, 0.85f, 0.9f);
            modalSo.FindProperty("modalGoldText").objectReferenceValue = infoTMP;

            // Status Feedback Text
            var statusTMP = CreateTMPText("StatusFeedbackText", dialogObj.transform, "", new Vector2(0, -96), new Vector2(800, 22), 14, TextAlignmentOptions.Center);
            statusTMP.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
            statusTMP.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
            statusTMP.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            modalSo.FindProperty("statusFeedbackText").objectReferenceValue = statusTMP;

            // Close Button "X"
            var closeBtn = CreateButton("CloseAvatarBtn", headerObj.transform, "X", new Vector2(-20, -20), new Vector2(44, 44));
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1, 1);
            cbRect.anchorMax = new Vector2(1, 1);
            cbRect.pivot = new Vector2(1, 1);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.color = new Color(0.35f, 0.15f, 0.15f);
            var cbTMP = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbTMP != null)
            {
                cbTMP.fontSize = 22;
                cbTMP.fontStyle = FontStyles.Bold;
                cbTMP.color = Color.white;
            }
            modalSo.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // Scroll View for Avatar Grid
            var scrollObj = CreateUIElement("AvatarScrollView", dialogObj.transform);
            var scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0, -45);
            scrollRect.sizeDelta = new Vector2(920, 520);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 25f;

            // Viewport
            var viewportObj = CreateUIElement("Viewport", scrollObj.transform);
            SetStretch(viewportObj.GetComponent<RectTransform>());
            viewportObj.AddComponent<RectMask2D>();
            scroll.viewport = viewportObj.GetComponent<RectTransform>();

            // Content
            var contentObj = CreateUIElement("Content", viewportObj.transform);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 1000);

            var gridLayout = contentObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(165, 225);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.padding = new RectOffset(15, 15, 15, 15);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            modalSo.FindProperty("gridContainer").objectReferenceValue = contentObj.transform;

            modalSo.ApplyModifiedProperties();

            // Link to Home controller
            homeSo.FindProperty("avatarModal").objectReferenceValue = modalComponent;

            // Initially hidden
            modalRoot.SetActive(false);
        }

        private static void BuildDailyRewardModal(Transform canvasTransform, SerializedObject homeSo)
        {
            // Root Overlay (dimmed frosted backdrop)
            var modalRoot = CreateUIElement("DailyRewardModalRoot", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());
            var overlayImg = modalRoot.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.82f);

            var modalComponent = modalRoot.AddComponent<DailyRewardModal>();
            var modalSo = new SerializedObject(modalComponent);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalRoot;

            // Modal Dialog Window
            var dialogObj = CreateUIElement("DailyRewardDialogWindow", modalRoot.transform);
            var dialogRect = dialogObj.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(980, 560);

            var dialogImg = dialogObj.AddComponent<Image>();
            dialogImg.color = new Color(0.10f, 0.11f, 0.17f, 0.98f);

            // Dialog Header Bar
            var headerObj = CreateUIElement("DialogHeader", dialogObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 85);
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = new Color(0.14f, 0.15f, 0.23f, 1f);

            // Title Text
            var titleTMP = CreateTMPText("DailyRewardTitleText", headerObj.transform, "PACK DE RECOMPENSAS DIARIAS", new Vector2(0, -22), new Vector2(600, 36), 26, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.85f, 0.2f);

            // Streak Subtitle / Tracker
            var streakTMP = CreateTMPText("StreakTitleText", headerObj.transform, "Racha Actual: Día 1 de 7 • ¡Vuelve diario para recompensas épicas!", new Vector2(0, -56), new Vector2(700, 24), 15, TextAlignmentOptions.Center);
            streakTMP.color = new Color(0.85f, 0.85f, 0.9f);
            modalSo.FindProperty("streakTitleText").objectReferenceValue = streakTMP;

            // Close Button "X"
            var closeBtn = CreateButton("CloseDailyRewardBtn", headerObj.transform, "X", new Vector2(-20, -20), new Vector2(44, 44));
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1, 1);
            cbRect.anchorMax = new Vector2(1, 1);
            cbRect.pivot = new Vector2(1, 1);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.color = new Color(0.35f, 0.15f, 0.15f);
            var cbTMP = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbTMP != null)
            {
                cbTMP.fontSize = 22;
                cbTMP.fontStyle = FontStyles.Bold;
                cbTMP.color = Color.white;
            }
            modalSo.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // 7-Day Calendar Horizontal Container
            var calendarObj = CreateUIElement("CalendarContainer", dialogObj.transform);
            var calRect = calendarObj.GetComponent<RectTransform>();
            calRect.anchorMin = new Vector2(0.5f, 0.5f);
            calRect.anchorMax = new Vector2(0.5f, 0.5f);
            calRect.pivot = new Vector2(0.5f, 0.5f);
            calRect.anchoredPosition = new Vector2(0, 15);
            calRect.sizeDelta = new Vector2(920, 240);

            var calLayout = calendarObj.AddComponent<HorizontalLayoutGroup>();
            calLayout.childControlWidth = false;
            calLayout.childControlHeight = false;
            calLayout.spacing = 15;
            calLayout.childAlignment = TextAnchor.MiddleCenter;

            modalSo.FindProperty("calendarContainer").objectReferenceValue = calendarObj.transform;

            // Bottom Section: Countdown & Claim Button
            var countdownTMP = CreateTMPText("CountdownText", dialogObj.transform, "¡Tu pack diario está listo para reclamar!", new Vector2(0, -425), new Vector2(600, 24), 14, TextAlignmentOptions.Center);
            countdownTMP.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
            countdownTMP.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
            countdownTMP.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            modalSo.FindProperty("countdownText").objectReferenceValue = countdownTMP;

            var claimBtn = CreateButton("ClaimDailyPackBtn", dialogObj.transform, "¡RECLAMAR PACK DÍA 1!", new Vector2(0, 25), new Vector2(320, 52));
            var claimRect = claimBtn.GetComponent<RectTransform>();
            claimRect.anchorMin = new Vector2(0.5f, 0);
            claimRect.anchorMax = new Vector2(0.5f, 0);
            claimRect.pivot = new Vector2(0.5f, 0);
            var claimImg = claimBtn.GetComponent<Image>();
            claimImg.color = new Color(0.95f, 0.70f, 0.15f);
            var claimBtnTMP = claimBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (claimBtnTMP != null)
            {
                claimBtnTMP.fontSize = 18;
                claimBtnTMP.fontStyle = FontStyles.Bold;
                claimBtnTMP.color = new Color(0.1f, 0.1f, 0.1f);
            }
            modalSo.FindProperty("claimButton").objectReferenceValue = claimBtn;
            modalSo.FindProperty("claimButtonText").objectReferenceValue = claimBtnTMP;

            // Reward Reveal Dialog (Celebratory popup upon opening pack)
            var revealObj = CreateUIElement("RewardRevealRoot", modalRoot.transform);
            SetStretch(revealObj.GetComponent<RectTransform>());
            var revBg = revealObj.AddComponent<Image>();
            revBg.color = new Color(0f, 0f, 0f, 0.90f);
            modalSo.FindProperty("revealDialogRoot").objectReferenceValue = revealObj;

            var revWindow = CreateUIElement("RevealWindow", revealObj.transform);
            var rwRect = revWindow.GetComponent<RectTransform>();
            rwRect.anchorMin = new Vector2(0.5f, 0.5f);
            rwRect.anchorMax = new Vector2(0.5f, 0.5f);
            rwRect.pivot = new Vector2(0.5f, 0.5f);
            rwRect.anchoredPosition = Vector2.zero;
            rwRect.sizeDelta = new Vector2(880, 460);

            var rwImg = revWindow.AddComponent<Image>();
            rwImg.color = new Color(0.12f, 0.13f, 0.20f, 0.98f);

            var revTitle = CreateTMPText("RevealTitle", revWindow.transform, "¡RECOMPENSA RECLAMADA!", new Vector2(0, -30), new Vector2(600, 36), 28, TextAlignmentOptions.Center);
            revTitle.fontStyle = FontStyles.Bold;
            revTitle.color = new Color(1f, 0.85f, 0.2f);
            modalSo.FindProperty("revealTitleText").objectReferenceValue = revTitle;

            var revSub = CreateTMPText("RevealSubtitle", revWindow.transform, "¡Has recibido las siguientes recompensas para tu colección!", new Vector2(0, -70), new Vector2(700, 24), 15, TextAlignmentOptions.Center);
            revSub.color = new Color(0.85f, 0.90f, 1f);
            modalSo.FindProperty("revealSubtitleText").objectReferenceValue = revSub;

            var revItemsObj = CreateUIElement("RevealItemsContainer", revWindow.transform);
            var riRect = revItemsObj.GetComponent<RectTransform>();
            riRect.anchorMin = new Vector2(0.5f, 0.5f);
            riRect.anchorMax = new Vector2(0.5f, 0.5f);
            riRect.pivot = new Vector2(0.5f, 0.5f);
            riRect.anchoredPosition = new Vector2(0, -10);
            riRect.sizeDelta = new Vector2(820, 200);

            var riLayout = revItemsObj.AddComponent<HorizontalLayoutGroup>();
            riLayout.childControlWidth = false;
            riLayout.childControlHeight = false;
            riLayout.spacing = 20;
            riLayout.childAlignment = TextAnchor.MiddleCenter;
            modalSo.FindProperty("revealItemsContainer").objectReferenceValue = revItemsObj.transform;

            var revCloseBtn = CreateButton("RevealConfirmBtn", revWindow.transform, "¡GENIAL!", new Vector2(0, 25), new Vector2(220, 50));
            var rcbRect = revCloseBtn.GetComponent<RectTransform>();
            rcbRect.anchorMin = new Vector2(0.5f, 0);
            rcbRect.anchorMax = new Vector2(0.5f, 0);
            rcbRect.pivot = new Vector2(0.5f, 0);
            var rcbImg = revCloseBtn.GetComponent<Image>();
            rcbImg.color = new Color(0.20f, 0.75f, 0.35f);
            var rcbTMP = revCloseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (rcbTMP != null)
            {
                rcbTMP.fontSize = 18;
                rcbTMP.fontStyle = FontStyles.Bold;
                rcbTMP.color = Color.white;
            }
            modalSo.FindProperty("revealCloseButton").objectReferenceValue = revCloseBtn;

            revealObj.SetActive(false); // Initially hidden

            modalSo.ApplyModifiedProperties();

            // Link to Home controller
            homeSo.FindProperty("dailyRewardModal").objectReferenceValue = modalComponent;

            // Initially hidden
            modalRoot.SetActive(false);
        }

        private static void BuildDeckModal(Transform canvasTransform, SerializedObject homeSo)
        {
            var modalRoot = CreateUIElement("DeckManagerModal", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);

            var dialogObj = CreateUIElement("DialogWindow", modalRoot.transform);
            var dwRect = dialogObj.GetComponent<RectTransform>();
            dwRect.anchorMin = new Vector2(0.5f, 0.5f);
            dwRect.anchorMax = new Vector2(0.5f, 0.5f);
            dwRect.pivot = new Vector2(0.5f, 0.5f);
            dwRect.anchoredPosition = Vector2.zero;
            dwRect.sizeDelta = new Vector2(920, 680);

            var dwImg = dialogObj.AddComponent<Image>();
            dwImg.color = new Color(0.10f, 0.11f, 0.18f, 0.98f);

            var modalComp = dialogObj.AddComponent<DeckManagerModal>();
            var modalSo = new SerializedObject(modalComp);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalRoot;

            // Title
            var title = CreateTMPText("Title", dialogObj.transform, "MI MAZO DE COMBATE", new Vector2(0, -32), new Vector2(600, 40), 28, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.85f, 0.2f);

            // Subtitle
            var subtitle = CreateTMPText("Subtitle", dialogObj.transform, DeckManagerModal.DeckRulesSubtitleText, new Vector2(0, -68), new Vector2(860, 40), 12.5f, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.75f, 0.80f, 0.90f);

            // Deck Count Text
            var countLabel = CreateTMPText("DeckCountText", dialogObj.transform, "CARTAS: 0 / 12", new Vector2(0, -108), new Vector2(400, 30), 18, TextAlignmentOptions.Center);
            countLabel.fontStyle = FontStyles.Bold;
            countLabel.color = new Color(1f, 0.85f, 0.2f);
            modalSo.FindProperty("deckCountText").objectReferenceValue = countLabel;

            // Cards Grid Container
            var gridObj = CreateUIElement("CardsGrid", dialogObj.transform);
            var gRect = gridObj.GetComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.5f, 0.5f);
            gRect.anchorMax = new Vector2(0.5f, 0.5f);
            gRect.pivot = new Vector2(0.5f, 0.5f);
            gRect.anchoredPosition = new Vector2(0, -15);
            gRect.sizeDelta = new Vector2(840, 380);

            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 175);
            gridLayout.spacing = new Vector2(16, 16);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 6;
            modalSo.FindProperty("cardsGridParent").objectReferenceValue = gridObj.transform;

            // Action Buttons: AutoComplete and Close
            var autoBtn = CreateButton("AutoCompleteBtn", dialogObj.transform, "AUTO-COMPLETAR", new Vector2(-150, 45), new Vector2(240, 50));
            var abImg = autoBtn.GetComponent<Image>();
            abImg.color = new Color(0.18f, 0.55f, 0.85f);
            var abTMP = autoBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (abTMP != null)
            {
                abTMP.fontSize = 16;
                abTMP.fontStyle = FontStyles.Bold;
                abTMP.color = Color.white;
            }
            modalSo.FindProperty("autoCompleteButton").objectReferenceValue = autoBtn;

            var closeBtn = CreateButton("CloseBtn", dialogObj.transform, "GUARDAR Y CERRAR", new Vector2(150, 45), new Vector2(240, 50));
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.color = new Color(0.22f, 0.70f, 0.35f);
            var cbTMP = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbTMP != null)
            {
                cbTMP.fontSize = 16;
                cbTMP.fontStyle = FontStyles.Bold;
                cbTMP.color = Color.white;
            }
            modalSo.FindProperty("closeButton").objectReferenceValue = closeBtn;

            modalSo.ApplyModifiedProperties();

            homeSo.FindProperty("deckModal").objectReferenceValue = modalComp;
            modalRoot.SetActive(false);
        }

        private static PackOpeningModal BuildPackOpeningModal(Transform canvasTransform, SerializedObject homeSo)
        {
            var modalRoot = CreateUIElement("PackOpeningModalRoot", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            var modalComp = modalRoot.AddComponent<PackOpeningModal>();
            var modalSo = new SerializedObject(modalComp);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalRoot;

            var dialogObj = CreateUIElement("DialogPanel", modalRoot.transform);
            var dRect = dialogObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(1060, 620);

            var dImg = dialogObj.AddComponent<Image>();
            dImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            // Close 'X' Button
            var closeXBtn = CreateButton("CloseXBtn", dialogObj.transform, "X", new Vector2(495, 275), new Vector2(44, 44));
            var cxImg = closeXBtn.GetComponent<Image>();
            cxImg.color = new Color(0.35f, 0.15f, 0.15f);
            modalSo.FindProperty("closeButton").objectReferenceValue = closeXBtn;

            // Header Title
            var title = CreateTMPText("HeaderTitle", dialogObj.transform, "ABRIENDO SOBRE TOMO 1", new Vector2(0, 255), new Vector2(700, 45), 26, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.85f, 0.2f);
            modalSo.FindProperty("headerTitleText").objectReferenceValue = title;

            // Subtitle
            var sub = CreateTMPText("Subtitle", dialogObj.transform, "Toca el sobre para abrirlo y descubrir tus cartas", new Vector2(0, 220), new Vector2(800, 30), 14, TextAlignmentOptions.Center);
            sub.color = new Color(0.75f, 0.80f, 0.90f);
            modalSo.FindProperty("subtitleText").objectReferenceValue = sub;

            // Sealed Pack Container (Single Pack View)
            var sealedPackObj = CreateUIElement("SealedPackContainer", dialogObj.transform);
            var spRect = sealedPackObj.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.pivot = new Vector2(0.5f, 0.5f);
            spRect.anchoredPosition = new Vector2(0, 20);
            spRect.sizeDelta = new Vector2(360, 450);
            modalSo.FindProperty("sealedPackContainer").objectReferenceValue = sealedPackObj;

            // Sealed Pack Wrapper (Foil Booster Pack)
            var wrapperObj = CreateUIElement("FoilWrapper", sealedPackObj.transform);
            var wRect = wrapperObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.5f);
            wRect.anchorMax = new Vector2(0.5f, 0.5f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.anchoredPosition = new Vector2(0, 45);
            wRect.sizeDelta = new Vector2(230, 320);
            var wImg = wrapperObj.AddComponent<Image>();
            wImg.color = new Color(0.12f, 0.16f, 0.28f, 1f);

            var wBtn = wrapperObj.AddComponent<Button>();
            modalSo.FindProperty("sealedPackWrapper").objectReferenceValue = wRect;
            modalSo.FindProperty("sealedPackButton").objectReferenceValue = wBtn;

            var foilTrim = CreateUIElement("FoilTrim", wrapperObj.transform);
            var ftRect = foilTrim.GetComponent<RectTransform>();
            SetStretch(ftRect);
            ftRect.offsetMin = new Vector2(5, 5);
            ftRect.offsetMax = new Vector2(-5, -5);
            var ftImg = foilTrim.AddComponent<Image>();
            ftImg.color = new Color(0.85f, 0.65f, 0.15f, 0.35f);

            var packContent = CreateTMPText("PackArtwork", wrapperObj.transform,
                "<size=20><b>COMETA\nCHAMPIONS</b></size>\n<size=11><color=#FFD700>SET BASE</color></size>\n\n<size=26><b>TOMO 1</b></size>\n\n<size=11>• 1 CARTA NUEVA\nGARANTIZADA •</size>",
                Vector2.zero, new Vector2(210, 290), 16, TextAlignmentOptions.Center);
            packContent.color = Color.white;
            modalSo.FindProperty("sealedPackTitleText").objectReferenceValue = packContent;

            // Remaining Packs Badge
            var remBadgeObj = CreateUIElement("RemainingBadge", sealedPackObj.transform);
            var rbRect = remBadgeObj.GetComponent<RectTransform>();
            rbRect.anchorMin = new Vector2(0.5f, 0.5f);
            rbRect.anchorMax = new Vector2(0.5f, 0.5f);
            rbRect.pivot = new Vector2(0.5f, 0.5f);
            rbRect.anchoredPosition = new Vector2(0, -135);
            rbRect.sizeDelta = new Vector2(240, 28);
            var rbImg = remBadgeObj.AddComponent<Image>();
            rbImg.color = new Color(0.20f, 0.40f, 0.65f, 0.90f);
            var rbTMP = CreateTMPText("Text", remBadgeObj.transform, "+0 sobres pendientes", Vector2.zero, new Vector2(230, 24), 13, TextAlignmentOptions.Center);
            rbTMP.fontStyle = FontStyles.Bold;
            rbTMP.color = Color.white;
            modalSo.FindProperty("remainingPacksBadgeText").objectReferenceValue = rbTMP;

            // Open Pack Button
            var openBtn = CreateButton("OpenPackBtn", sealedPackObj.transform, "ABRIR SOBRE", new Vector2(0, -185), new Vector2(260, 52));
            var opImg = openBtn.GetComponent<Image>();
            opImg.color = new Color(0.90f, 0.65f, 0.10f);
            var opTMP = openBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (opTMP != null)
            {
                opTMP.fontSize = 17;
                opTMP.fontStyle = FontStyles.Bold;
                opTMP.color = Color.white;
            }
            modalSo.FindProperty("openPackButton").objectReferenceValue = openBtn;
            modalSo.FindProperty("openPackButtonText").objectReferenceValue = opTMP;

            // Cards Container (initially hidden)
            var cardsObj = CreateUIElement("CardsContainer", dialogObj.transform);
            var cRect = cardsObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = new Vector2(0, 20);
            cRect.sizeDelta = new Vector2(980, 290);

            var hLayout = cardsObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            modalSo.FindProperty("cardsContainer").objectReferenceValue = cardsObj.transform;
            cardsObj.SetActive(false);

            // Action Buttons Row
            var revNextBtn = CreateButton("RevealNextBtn", dialogObj.transform, "DESCUBRIR CARTA", new Vector2(-160, -170), new Vector2(230, 48));
            var rnImg = revNextBtn.GetComponent<Image>();
            rnImg.color = new Color(0.85f, 0.60f, 0.10f);
            var rnTMP = revNextBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (rnTMP != null) { rnTMP.fontSize = 15; rnTMP.fontStyle = FontStyles.Bold; rnTMP.color = Color.white; }
            modalSo.FindProperty("revealNextButton").objectReferenceValue = revNextBtn;
            modalSo.FindProperty("revealNextButtonText").objectReferenceValue = rnTMP;

            var revAllBtn = CreateButton("RevealAllBtn", dialogObj.transform, "REVELAR TODAS", new Vector2(160, -170), new Vector2(210, 48));
            var raImg = revAllBtn.GetComponent<Image>();
            raImg.color = new Color(0.20f, 0.45f, 0.75f);
            var raTMP = revAllBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (raTMP != null) { raTMP.fontSize = 15; raTMP.fontStyle = FontStyles.Bold; raTMP.color = Color.white; }
            modalSo.FindProperty("revealAllButton").objectReferenceValue = revAllBtn;

            var doneBtn = CreateButton("NextPackOrDoneBtn", dialogObj.transform, "LISTO / VOLVER AL ÁLBUM", new Vector2(0, -170), new Vector2(320, 50));
            var dBtnImg = doneBtn.GetComponent<Image>();
            dBtnImg.color = new Color(0.22f, 0.70f, 0.35f);
            var doneTMP = doneBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (doneTMP != null) { doneTMP.fontSize = 16; doneTMP.fontStyle = FontStyles.Bold; doneTMP.color = Color.white; }
            modalSo.FindProperty("nextPackOrDoneButton").objectReferenceValue = doneBtn;
            modalSo.FindProperty("nextPackOrDoneButtonText").objectReferenceValue = doneTMP;
            doneBtn.gameObject.SetActive(false);

            // Summary Panel Root
            var summaryObj = CreateUIElement("SummaryPanel", dialogObj.transform);
            var sRect = summaryObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(0, 20);
            sRect.sizeDelta = new Vector2(600, 250);
            var sImg = summaryObj.AddComponent<Image>();
            sImg.color = new Color(0.15f, 0.18f, 0.26f, 0.95f);

            var sText = CreateTMPText("SummaryText", summaryObj.transform, "Resumen de cartas", Vector2.zero, new Vector2(560, 210), 16, TextAlignmentOptions.Center);
            sText.color = Color.white;
            modalSo.FindProperty("summaryPanelRoot").objectReferenceValue = summaryObj;
            modalSo.FindProperty("summaryText").objectReferenceValue = sText;
            summaryObj.SetActive(false);

            modalSo.ApplyModifiedProperties();
            homeSo.FindProperty("packOpeningModal").objectReferenceValue = modalComp;
            modalRoot.SetActive(false);

            return modalComp;
        }

        private static void BuildPackShopModal(Transform canvasTransform, SerializedObject homeSo, PackOpeningModal openingModal)
        {
            var modalRoot = CreateUIElement("PackShopModalRoot", canvasTransform);
            SetStretch(modalRoot.GetComponent<RectTransform>());

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);

            var modalComp = modalRoot.AddComponent<PackShopModal>();
            var modalSo = new SerializedObject(modalComp);
            modalSo.FindProperty("modalRoot").objectReferenceValue = modalRoot;
            modalSo.FindProperty("packOpeningModal").objectReferenceValue = openingModal;

            var dialogObj = CreateUIElement("DialogPanel", modalRoot.transform);
            var dRect = dialogObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(880, 600);

            var dImg = dialogObj.AddComponent<Image>();
            dImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            // Close 'X' Button
            var closeXBtn = CreateButton("CloseXBtn", dialogObj.transform, "X", new Vector2(410, 270), new Vector2(44, 44));
            var cxImg = closeXBtn.GetComponent<Image>();
            cxImg.color = new Color(0.35f, 0.15f, 0.15f);
            modalSo.FindProperty("closeButton").objectReferenceValue = closeXBtn;

            // Title
            var title = CreateTMPText("ShopTitle", dialogObj.transform, "TIENDA DE SOBRES", new Vector2(0, 255), new Vector2(500, 45), 28, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            // Subtitle
            var sub = CreateTMPText("Subtitle", dialogObj.transform, "Set Base — Tomo 1 • 80 Cartas Coleccionables", new Vector2(0, 220), new Vector2(600, 30), 14, TextAlignmentOptions.Center);
            sub.color = new Color(1f, 0.85f, 0.2f);

            // Balances Row
            var goldBal = CreateTMPText("GoldBalance", dialogObj.transform, "1,256 ORO", new Vector2(-150, 180), new Vector2(250, 35), 20, TextAlignmentOptions.Center);
            goldBal.fontStyle = FontStyles.Bold;
            goldBal.color = new Color(1f, 0.85f, 0.2f);
            modalSo.FindProperty("goldBalanceText").objectReferenceValue = goldBal;

            var packBal = CreateTMPText("PackBalance", dialogObj.transform, "3 Sobres Guardados", new Vector2(150, 180), new Vector2(250, 35), 18, TextAlignmentOptions.Center);
            packBal.fontStyle = FontStyles.Bold;
            packBal.color = new Color(0.4f, 0.85f, 1f);
            modalSo.FindProperty("boosterPacksBalanceText").objectReferenceValue = packBal;

            // Guarantee Banner
            var bannerObj = CreateUIElement("GuaranteeBanner", dialogObj.transform);
            var bRect = bannerObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.anchoredPosition = new Vector2(0, 95);
            bRect.sizeDelta = new Vector2(760, 90);
            var bImg = bannerObj.AddComponent<Image>();
            bImg.color = new Color(0.18f, 0.16f, 0.28f, 0.95f);

            var bText = CreateTMPText("BannerText", bannerObj.transform,
                "<b>• ¡GARANTÍA DE CARTA NUEVA EN CADA SOBRE!</b>\n<size=13>Cada sobre contiene 5 cartas y garantiza al menos 1 carta no poseída de tu colección.</size>\n<size=14><color=#FFD700>Precio: 500 Monedas de Oro por sobre</color></size>",
                Vector2.zero, new Vector2(740, 80), 15, TextAlignmentOptions.Center);

            // Buy 1 Pack & Buy 5 Packs Buttons Row
            var buy1Btn = CreateButton("Buy1PackBtn", dialogObj.transform, "COMPRAR 1 SOBRE\n<size=13>500 ORO</size>", new Vector2(-160, -5), new Vector2(260, 60));
            var b1Img = buy1Btn.GetComponent<Image>();
            b1Img.color = new Color(0.85f, 0.55f, 0.12f);
            var b1TMP = buy1Btn.GetComponentInChildren<TextMeshProUGUI>();
            if (b1TMP != null) { b1TMP.fontSize = 15; b1TMP.fontStyle = FontStyles.Bold; b1TMP.color = Color.white; }
            modalSo.FindProperty("buy1PackButton").objectReferenceValue = buy1Btn;
            modalSo.FindProperty("buy1PackButtonText").objectReferenceValue = b1TMP;

            var buy5Btn = CreateButton("Buy5PacksBtn", dialogObj.transform, "COMPRAR 5 SOBRES\n<size=13>2,500 ORO</size>", new Vector2(160, -5), new Vector2(260, 60));
            var b5Img = buy5Btn.GetComponent<Image>();
            b5Img.color = new Color(0.75f, 0.45f, 0.10f);
            var b5TMP = buy5Btn.GetComponentInChildren<TextMeshProUGUI>();
            if (b5TMP != null) { b5TMP.fontSize = 15; b5TMP.fontStyle = FontStyles.Bold; b5TMP.color = Color.white; }
            modalSo.FindProperty("buy5PacksButton").objectReferenceValue = buy5Btn;
            modalSo.FindProperty("buy5PacksButtonText").objectReferenceValue = b5TMP;

            // Custom Quantity Stepper Row
            var decBtn = CreateButton("DecBtn", dialogObj.transform, "-", new Vector2(-250, -85), new Vector2(50, 44));
            var dBtnImg = decBtn.GetComponent<Image>();
            dBtnImg.color = new Color(0.25f, 0.28f, 0.35f);
            modalSo.FindProperty("decreaseQtyButton").objectReferenceValue = decBtn;

            var qtyTMP = CreateTMPText("QtyText", dialogObj.transform, "1", new Vector2(-170, -85), new Vector2(80, 44), 22, TextAlignmentOptions.Center);
            qtyTMP.fontStyle = FontStyles.Bold;
            qtyTMP.color = Color.white;
            modalSo.FindProperty("quantityText").objectReferenceValue = qtyTMP;

            var incBtn = CreateButton("IncBtn", dialogObj.transform, "+", new Vector2(-90, -85), new Vector2(50, 44));
            var iBtnImg = incBtn.GetComponent<Image>();
            iBtnImg.color = new Color(0.25f, 0.28f, 0.35f);
            modalSo.FindProperty("increaseQtyButton").objectReferenceValue = incBtn;

            var maxBtn = CreateButton("MaxBtn", dialogObj.transform, "MÁX", new Vector2(-20, -85), new Vector2(65, 44));
            var mBtnImg = maxBtn.GetComponent<Image>();
            mBtnImg.color = new Color(0.20f, 0.45f, 0.65f);
            var mTMP = maxBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (mTMP != null) { mTMP.fontSize = 14; mTMP.fontStyle = FontStyles.Bold; mTMP.color = Color.white; }
            modalSo.FindProperty("maxQtyButton").objectReferenceValue = maxBtn;

            var buyCustBtn = CreateButton("BuyCustomBtn", dialogObj.transform, "COMPRAR 1 SOBRES (500 ORO)", new Vector2(165, -85), new Vector2(250, 44));
            var bcImg = buyCustBtn.GetComponent<Image>();
            bcImg.color = new Color(0.18f, 0.60f, 0.30f);
            var bcTMP = buyCustBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (bcTMP != null) { bcTMP.fontSize = 14; bcTMP.fontStyle = FontStyles.Bold; bcTMP.color = Color.white; }
            modalSo.FindProperty("buyCustomButton").objectReferenceValue = buyCustBtn;
            modalSo.FindProperty("buyCustomButtonText").objectReferenceValue = bcTMP;

            // Saved Booster Section
            var boosterSec = CreateUIElement("SavedBoosterSection", dialogObj.transform);
            var bsRect = boosterSec.GetComponent<RectTransform>();
            bsRect.anchorMin = new Vector2(0.5f, 0.5f);
            bsRect.anchorMax = new Vector2(0.5f, 0.5f);
            bsRect.pivot = new Vector2(0.5f, 0.5f);
            bsRect.anchoredPosition = new Vector2(0, -165);
            bsRect.sizeDelta = new Vector2(620, 60);

            var openBBtn = CreateButton("OpenBoosterBtn", boosterSec.transform, "ABRIR SOBRE COMPRADO", new Vector2(-155, 0), new Vector2(295, 52));
            var obImg = openBBtn.GetComponent<Image>();
            obImg.color = new Color(0.45f, 0.20f, 0.70f); // Royal purple
            var obTMP = openBBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (obTMP != null) { obTMP.fontSize = 14; obTMP.fontStyle = FontStyles.Bold; obTMP.color = Color.white; }
            modalSo.FindProperty("savedBoosterSection").objectReferenceValue = boosterSec;
            modalSo.FindProperty("openBoosterButton").objectReferenceValue = openBBtn;
            modalSo.FindProperty("openBoosterButtonText").objectReferenceValue = obTMP;

            var openRBtn = CreateButton("OpenRewardPackBtn", boosterSec.transform, "ABRIR RECOMPENSA", new Vector2(155, 0), new Vector2(295, 52));
            var orImg = openRBtn.GetComponent<Image>();
            orImg.color = new Color(0.18f, 0.45f, 0.65f); // Deep Cyan
            var orTMP = openRBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (orTMP != null) { orTMP.fontSize = 14; orTMP.fontStyle = FontStyles.Bold; orTMP.color = Color.white; }
            modalSo.FindProperty("openRewardPackButton").objectReferenceValue = openRBtn;
            modalSo.FindProperty("openRewardPackButtonText").objectReferenceValue = orTMP;

            // Status message
            var statusTMP = CreateTMPText("StatusMsg", dialogObj.transform, "", new Vector2(0, -225), new Vector2(700, 30), 14, TextAlignmentOptions.Center);
            modalSo.FindProperty("statusMessageText").objectReferenceValue = statusTMP;

            modalSo.ApplyModifiedProperties();
            homeSo.FindProperty("packShopModal").objectReferenceValue = modalComp;
            modalRoot.SetActive(false);
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/_Project/Scenes"))
            {
                Directory.CreateDirectory("Assets/_Project/Scenes");
            }
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
            }
        }
    }
}
