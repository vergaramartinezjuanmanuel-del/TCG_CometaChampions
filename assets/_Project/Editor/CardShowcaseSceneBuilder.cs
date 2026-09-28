using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CometaChampions.Runtime.UI;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Editor
{
    /// <summary>
    /// Editor utility to construct the dedicated "CardShowcase" 3D scene.
    /// Provides 360-degree free orbit inspection, 80-card catalog browser,
    /// official reverso swapping, dynamic foil metallic sheen, and rich card details.
    /// Accessible via menu: Cometa Champions -> Build Card Showcase Scene
    /// </summary>
    public static class CardShowcaseSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/CardShowcase.unity";
        private const string HomeScenePath = "Assets/_Project/Scenes/Home.unity";

        [MenuItem("Cometa Champions/Build Card Showcase Scene")]
        public static void BuildCardShowcaseScene()
        {
            EnsureDirectories();

            // 1. Create Empty Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Lighting & Camera
            var cameraObj = new GameObject("Showcase_Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.05f, 0.075f, 1f);
            camera.fieldOfView = 40f;
            cameraObj.transform.position = new Vector3(-0.6f, 0f, -5.8f);
            cameraObj.tag = "MainCamera";

            // Studio 3-Point Lighting
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

            // 3. Showcase Pedestal Platform
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Showcase_Pedestal";
            pedestal.transform.position = new Vector3(-0.6f, -2.15f, 0f);
            pedestal.transform.localScale = new Vector3(4.2f, 0.08f, 4.2f);
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
                pedestal.GetComponent<MeshRenderer>().sharedMaterial = pedMat;
            }

            // 4. 3D Card Object
            var cardRootObj = new GameObject("Card_Showcase_3D");
            cardRootObj.transform.position = new Vector3(-0.6f, 0.05f, 0f);
            var card3D = cardRootObj.AddComponent<Card3D>();
            card3D.EnsureHierarchy();

            var cardViewer = cardRootObj.AddComponent<Card3DViewer>();
            var viewerSo = new SerializedObject(cardViewer);
            viewerSo.FindProperty("targetCardTransform").objectReferenceValue = cardRootObj.transform;
            viewerSo.FindProperty("showcaseCamera").objectReferenceValue = camera;
            viewerSo.ApplyModifiedProperties();

            // 5. EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 6. UI Canvas Hierarchy
            var canvasObj = new GameObject("Showcase_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var uiManager = canvasObj.AddComponent<CardShowcaseUI>();
            var uiSo = new SerializedObject(uiManager);
            uiSo.FindProperty("card3D").objectReferenceValue = card3D;
            uiSo.FindProperty("cardViewer").objectReferenceValue = cardViewer;

            // Header Bar
            var headerObj = CreateUIElement("HeaderBar", canvasObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 80);

            var backBtn = CreateButton("BackButton", headerObj.transform, "◀ VOLVER", new Vector2(90, -40), new Vector2(140, 50));
            backBtn.GetComponent<Image>().color = new Color(0.2f, 0.23f, 0.32f);
            uiSo.FindProperty("backButton").objectReferenceValue = backBtn;

            var titleTMP = CreateTMPText("TitleText", headerObj.transform, "SHOWCASE 3D  •  COMETA CHAMPIONS", new Vector2(500, -40), new Vector2(600, 50), 32, TextAlignmentOptions.Left);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.85f, 0.25f);
            uiSo.FindProperty("headerTitleText").objectReferenceValue = titleTMP;

            var catIndTMP = CreateTMPText("CategoryIndicator", headerObj.transform, "HÉROES CHAMPIONS  •  CC-01", new Vector2(-280, -40), new Vector2(480, 40), 20, TextAlignmentOptions.Right);
            catIndTMP.color = new Color(0.4f, 0.85f, 1f);
            catIndTMP.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            catIndTMP.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            catIndTMP.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            uiSo.FindProperty("categoryIndicatorText").objectReferenceValue = catIndTMP;

            // Category Filter Bar (top horizontal)
            var catBarObj = CreateUIElement("CategoryFilterBar", canvasObj.transform);
            var catBarRect = catBarObj.GetComponent<RectTransform>();
            catBarRect.anchorMin = new Vector2(0, 1);
            catBarRect.anchorMax = new Vector2(0, 1);
            catBarRect.pivot = new Vector2(0, 1);
            catBarRect.anchoredPosition = new Vector2(60, -90);
            catBarRect.sizeDelta = new Vector2(760, 45);

            var catLayout = catBarObj.AddComponent<HorizontalLayoutGroup>();
            catLayout.spacing = 10;
            catLayout.childControlWidth = false;
            catLayout.childControlHeight = true;

            var catAll = CreateButton("Cat_All", catBarObj.transform, "Todas (80)", Vector2.zero, new Vector2(140, 42));
            var catChamp = CreateButton("Cat_Champions", catBarObj.transform, "Héroes (20)", Vector2.zero, new Vector2(140, 42));
            var catTromp = CreateButton("Cat_Trompos", catBarObj.transform, "Trompos (20)", Vector2.zero, new Vector2(140, 42));
            var catKombat = CreateButton("Cat_Kombat", catBarObj.transform, "Kombat (20)", Vector2.zero, new Vector2(140, 42));
            var catArenas = CreateButton("Cat_Arenas", catBarObj.transform, "Arenas (20)", Vector2.zero, new Vector2(140, 42));

            uiSo.FindProperty("catAllButton").objectReferenceValue = catAll;
            uiSo.FindProperty("catChampionsButton").objectReferenceValue = catChamp;
            uiSo.FindProperty("catTromposButton").objectReferenceValue = catTromp;
            uiSo.FindProperty("catKombatButton").objectReferenceValue = catKombat;
            uiSo.FindProperty("catArenasButton").objectReferenceValue = catArenas;

            // Hint Text for 3D Orbit
            var hintTMP = CreateTMPText("HintText", canvasObj.transform, "🖱️ Arrastra con el ratón / touch para rotar en 360° libremente", new Vector2(-280, 140), new Vector2(650, 30), 18, TextAlignmentOptions.Center);
            hintTMP.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);
            var hintRect = hintTMP.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0);
            hintRect.anchorMax = new Vector2(0.5f, 0);
            hintRect.pivot = new Vector2(0.5f, 0);

            // Right Inspector Panel
            var rightPanel = CreateUIElement("RightPanel_Details", canvasObj.transform);
            var rpRect = rightPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(1, 0);
            rpRect.anchorMax = new Vector2(1, 1);
            rpRect.pivot = new Vector2(1, 0.5f);
            rpRect.anchoredPosition = new Vector2(-40, -40);
            rpRect.sizeDelta = new Vector2(480, -140);

            var rpBg = rightPanel.AddComponent<Image>();
            rpBg.color = new Color(0.09f, 0.10f, 0.15f, 0.94f);
            uiSo.FindProperty("detailPanel").objectReferenceValue = rightPanel;

            // Card Name & ID
            var cardNameTMP = CreateTMPText("Detail_CardName", rightPanel.transform, "Avispón", new Vector2(25, -25), new Vector2(430, 45), 34, TextAlignmentOptions.Left);
            cardNameTMP.fontStyle = FontStyles.Bold;
            cardNameTMP.color = Color.white;
            uiSo.FindProperty("cardNameText").objectReferenceValue = cardNameTMP;

            // Category & Clan Badges
            var catBadge = CreateTMPText("Detail_CategoryBadge", rightPanel.transform, "Héroes Champions", new Vector2(25, -68), new Vector2(200, 28), 18, TextAlignmentOptions.Left);
            catBadge.color = new Color(0.4f, 0.85f, 1f);
            uiSo.FindProperty("cardCategoryBadgeText").objectReferenceValue = catBadge;

            var clanBadge = CreateTMPText("Detail_ClanBadge", rightPanel.transform, "🪓 Depredadores", new Vector2(230, -68), new Vector2(220, 28), 18, TextAlignmentOptions.Left);
            clanBadge.fontStyle = FontStyles.Bold;
            uiSo.FindProperty("cardClanBadgeText").objectReferenceValue = clanBadge;

            // Stats Chip
            var statsChip = CreateTMPText("Detail_StatsChip", rightPanel.transform, "🪙 Costo: 2   ⚔️ ATK: 3   🛡️ DEF: 1", new Vector2(25, -108), new Vector2(430, 36), 19, TextAlignmentOptions.Left);
            statsChip.color = new Color(1f, 0.85f, 0.3f);
            statsChip.fontStyle = FontStyles.Bold;
            uiSo.FindProperty("cardStatsText").objectReferenceValue = statsChip;

            var rarityTMP = CreateTMPText("Detail_Rarity", rightPanel.transform, "Rareza: Común", new Vector2(25, -140), new Vector2(430, 24), 16, TextAlignmentOptions.Left);
            rarityTMP.color = new Color(0.75f, 0.75f, 0.85f);
            uiSo.FindProperty("cardRarityText").objectReferenceValue = rarityTMP;

            // Effect / Description Box
            var effectHeader = CreateTMPText("Detail_EffectHeader", rightPanel.transform, "HABILIDAD / EFECTO", new Vector2(25, -178), new Vector2(430, 22), 15, TextAlignmentOptions.Left);
            effectHeader.fontStyle = FontStyles.Bold;
            effectHeader.color = new Color(0.85f, 0.85f, 0.9f);

            var effectBox = CreateUIElement("EffectBox", rightPanel.transform);
            var efRect = effectBox.GetComponent<RectTransform>();
            efRect.anchoredPosition = new Vector2(25, -205);
            efRect.sizeDelta = new Vector2(430, 110);
            var efImg = effectBox.AddComponent<Image>();
            efImg.color = new Color(0.06f, 0.07f, 0.11f, 0.95f);

            var effectTMP = CreateTMPText("EffectText", effectBox.transform, "Maestro de la velocidad aérea. Sus ataques veloces son difíciles de predecir.", new Vector2(12, -10), new Vector2(406, 90), 16, TextAlignmentOptions.TopLeft);
            effectTMP.textWrappingMode = TextWrappingModes.Normal;
            effectTMP.color = Color.white;
            uiSo.FindProperty("cardEffectText").objectReferenceValue = effectTMP;

            // Flavor / Lore Box
            var flavorHeader = CreateTMPText("Detail_FlavorHeader", rightPanel.transform, "LORE & CITA DEL PERSONAJE", new Vector2(25, -330), new Vector2(430, 22), 15, TextAlignmentOptions.Left);
            flavorHeader.fontStyle = FontStyles.Bold;
            flavorHeader.color = new Color(0.85f, 0.85f, 0.9f);

            var flavorBox = CreateUIElement("FlavorBox", rightPanel.transform);
            var flRect = flavorBox.GetComponent<RectTransform>();
            flRect.anchoredPosition = new Vector2(25, -355);
            flRect.sizeDelta = new Vector2(430, 130);
            var flImg = flavorBox.AddComponent<Image>();
            flImg.color = new Color(0.06f, 0.07f, 0.11f, 0.95f);

            var flavorTMP = CreateTMPText("FlavorText", flavorBox.transform, "\"¡Soy más rápido que el viento!\"\n\nUno de los primeros héroes descubiertos en el cráter de Milpa Alta.", new Vector2(12, -10), new Vector2(406, 110), 15, TextAlignmentOptions.TopLeft);
            flavorTMP.textWrappingMode = TextWrappingModes.Normal;
            flavorTMP.color = new Color(0.8f, 0.85f, 0.9f);
            uiSo.FindProperty("cardFlavorText").objectReferenceValue = flavorTMP;

            // Reverso Selector Row
            var revLabel = CreateTMPText("ReversoLabel", rightPanel.transform, "REVERSO OFICIAL DE CARTA", new Vector2(25, -505), new Vector2(430, 22), 16, TextAlignmentOptions.Left);
            revLabel.fontStyle = FontStyles.Bold;
            revLabel.color = new Color(1f, 0.85f, 0.35f);

            var revRow = CreateUIElement("ReversoRow", rightPanel.transform);
            var rrRect = revRow.GetComponent<RectTransform>();
            rrRect.anchoredPosition = new Vector2(25, -535);
            rrRect.sizeDelta = new Vector2(430, 42);

            var rrLayout = revRow.AddComponent<HorizontalLayoutGroup>();
            rrLayout.spacing = 10;
            rrLayout.childControlWidth = false;
            rrLayout.childControlHeight = true;

            var revOro = CreateButton("Rev_Oro", revRow.transform, "👑 Oro", Vector2.zero, new Vector2(135, 40));
            var revColor = CreateButton("Rev_Color", revRow.transform, "🎨 Color", Vector2.zero, new Vector2(135, 40));
            var revBi = CreateButton("Rev_Bi", revRow.transform, "☯️ Bicolor", Vector2.zero, new Vector2(135, 40));

            uiSo.FindProperty("reversoOroButton").objectReferenceValue = revOro;
            uiSo.FindProperty("reversoColorButton").objectReferenceValue = revColor;
            uiSo.FindProperty("reversoBiButton").objectReferenceValue = revBi;

            // Foil Sheen Selector Row
            var foilLabel = CreateTMPText("FoilLabel", rightPanel.transform, "ACABADO METÁLICO (FOIL SHEEN)", new Vector2(25, -595), new Vector2(430, 22), 16, TextAlignmentOptions.Left);
            foilLabel.fontStyle = FontStyles.Bold;
            foilLabel.color = new Color(0.9f, 0.7f, 1f);

            var foilRow = CreateUIElement("FoilRow", rightPanel.transform);
            var frRect = foilRow.GetComponent<RectTransform>();
            frRect.anchoredPosition = new Vector2(25, -625);
            frRect.sizeDelta = new Vector2(430, 42);

            var frLayout = foilRow.AddComponent<HorizontalLayoutGroup>();
            frLayout.spacing = 8;
            frLayout.childControlWidth = false;
            frLayout.childControlHeight = true;

            var foilBase = CreateButton("Foil_Base", foilRow.transform, "Mate", Vector2.zero, new Vector2(100, 40));
            var foilSilv = CreateButton("Foil_Plata", foilRow.transform, "Plata", Vector2.zero, new Vector2(100, 40));
            var foilGold = CreateButton("Foil_Oro", foilRow.transform, "Oro", Vector2.zero, new Vector2(100, 40));
            var foilCosm = CreateButton("Foil_Cosm", foilRow.transform, "Cósmico", Vector2.zero, new Vector2(100, 40));

            uiSo.FindProperty("foilBaseButton").objectReferenceValue = foilBase;
            uiSo.FindProperty("foilSilverButton").objectReferenceValue = foilSilv;
            uiSo.FindProperty("foilGoldButton").objectReferenceValue = foilGold;
            uiSo.FindProperty("foilCosmicButton").objectReferenceValue = foilCosm;

            // Bottom Navigation Bar
            var bottomBar = CreateUIElement("BottomNavControls", canvasObj.transform);
            var bbRect = bottomBar.GetComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0.5f, 0);
            bbRect.anchorMax = new Vector2(0.5f, 0);
            bbRect.pivot = new Vector2(0.5f, 0);
            bbRect.anchoredPosition = new Vector2(-240, 30);
            bbRect.sizeDelta = new Vector2(980, 65);

            var bbLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
            bbLayout.spacing = 14;
            bbLayout.childAlignment = TextAnchor.MiddleCenter;
            bbLayout.childControlWidth = false;
            bbLayout.childControlHeight = true;

            var prevBtn = CreateButton("PrevButton", bottomBar.transform, "◀ ANTERIOR", Vector2.zero, new Vector2(140, 52));
            var counterTMP = CreateTMPText("CounterText", bottomBar.transform, "1 / 80", Vector2.zero, new Vector2(90, 52), 20, TextAlignmentOptions.Center);
            counterTMP.fontStyle = FontStyles.Bold;
            var nextBtn = CreateButton("NextButton", bottomBar.transform, "SIGUIENTE ▶", Vector2.zero, new Vector2(140, 52));
            var randBtn = CreateButton("RandomButton", bottomBar.transform, "🎲 AZAR", Vector2.zero, new Vector2(110, 52));
            var flipBtn = CreateButton("FlipButton", bottomBar.transform, "🔄 VOLTEAR", Vector2.zero, new Vector2(130, 52));
            var spinBtn = CreateButton("SpinButton", bottomBar.transform, "💫 AUTO-GIRO", Vector2.zero, new Vector2(145, 52));
            var frontBtn = CreateButton("FrontButton", bottomBar.transform, "🎯 FRENTE", Vector2.zero, new Vector2(110, 52));

            flipBtn.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.7f);
            spinBtn.GetComponent<Image>().color = new Color(0.5f, 0.25f, 0.7f);
            frontBtn.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f);

            uiSo.FindProperty("prevCardButton").objectReferenceValue = prevBtn;
            uiSo.FindProperty("cardCounterText").objectReferenceValue = counterTMP;
            uiSo.FindProperty("nextCardButton").objectReferenceValue = nextBtn;
            uiSo.FindProperty("randomCardButton").objectReferenceValue = randBtn;
            uiSo.FindProperty("flipCardButton").objectReferenceValue = flipBtn;
            uiSo.FindProperty("autoSpinButton").objectReferenceValue = spinBtn;
            uiSo.FindProperty("autoSpinButtonText").objectReferenceValue = spinBtn.GetComponentInChildren<TMP_Text>();
            uiSo.FindProperty("resetFrontButton").objectReferenceValue = frontBtn;

            uiSo.ApplyModifiedProperties();

            // 7. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 8. Register scenes in Build Settings: Home (0), CardShowcase (1)
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardShowcaseSceneBuilder] Successfully built 3D Card Showcase scene at {ScenePath}!");
        }

        private static void RegisterBuildScenes()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(HomeScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
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
            img.color = new Color(0.20f, 0.22f, 0.30f, 0.9f);

            var btn = go.AddComponent<Button>();

            var label = CreateTMPText("Text", go.transform, text, Vector2.zero, size, 17, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;

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
            return tmp;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/_Project/Scenes"))
            {
                Directory.CreateDirectory("Assets/_Project/Scenes");
            }
        }
    }
}
