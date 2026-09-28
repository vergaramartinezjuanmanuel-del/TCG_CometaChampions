using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CometaChampions.Runtime.Battle;

namespace CometaChampions.Editor
{
    /// <summary>
    /// Automated builder for the 3-Arena Battle Scene (Assets/_Project/Scenes/Battle.unity).
    /// Constructs a responsive 1920x1080 landscape canvas with:
    /// - Top HUD Bar (Turn, Energy, Score indicator, End Turn, Help, Restart, Home).
    /// - 3 Arena Columns with AI slots, central arena banner & clash meter, and player slots.
    /// - Bottom Player Hand Tray with playable cards.
    /// - Game Over and Help Modals.
    /// - BattleManager orchestrator connecting all serialized fields.
    /// </summary>
    public static class BattleSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Battle.unity";
        [MenuItem("Cometa Champions/Open Battle Scene", priority = 1)]
        public static void OpenBattleScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        [MenuItem("Cometa Champions/Build Battle Scene", priority = 2)]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[BattleSceneBuilder] Stopping Play Mode first. Please run Build Battle Scene in Edit Mode.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.055f, 0.078f, 1f); // Deep dark space navy #0b0e14
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 0, -10);

            // 2. Event System
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // 3. Canvas & UI Root
            var canvasObj = new GameObject("BattleCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 4. Background
            var bgObj = CreateUIElement("Background", canvasObj.transform);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.09f, 1f);

            // 5. HUD Header
            var headerView = BuildHeader(canvasObj.transform);

            // 6. 3 Arena Columns
            var laneViews = BuildLanes(canvasObj.transform);

            // 7. Bottom Hand Tray
            var handView = BuildHandTray(canvasObj.transform);

            // 8. Modals
            var gameOverModal = BuildGameOverModal(canvasObj.transform);
            var helpModal = BuildHelpModal(canvasObj.transform);
            var mulliganModal = BuildMulliganModal(canvasObj.transform);

            // 9. BattleManager GameObject
            var managerObj = new GameObject("BattleManager");
            var manager = managerObj.AddComponent<BattleManager>();

            var so = new SerializedObject(manager);
            so.FindProperty("headerView").objectReferenceValue = headerView;

            var laneArrayProp = so.FindProperty("laneViews");
            laneArrayProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                laneArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = laneViews[i];
            }

            so.FindProperty("handView").objectReferenceValue = handView;
            so.FindProperty("gameOverModal").objectReferenceValue = gameOverModal;
            so.FindProperty("helpModal").objectReferenceValue = helpModal;
            so.FindProperty("mulliganModal").objectReferenceValue = mulliganModal;
            so.FindProperty("autoStartOnAwake").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save Scene
            var dir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"<color=#30d158><b>[BattleSceneBuilder]</b> Battle scene successfully created and saved to {ScenePath}!</color>");
        }

        private static BattleHeaderView BuildHeader(Transform parent)
        {
            var headerObj = CreateUIElement("BattleHeader", parent);
            var rect = headerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(0, 80);

            var bgImg = headerObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

            var headerView = headerObj.AddComponent<BattleHeaderView>();
            var so = new SerializedObject(headerView);

            // Left: Home & Help buttons
            var homeBtn = CreateButton("HomeBtn", headerObj.transform, "🏠 MENÚ", new Vector2(100, -40), new Vector2(130, 48));
            homeBtn.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f);

            var helpBtn = CreateButton("HelpBtn", headerObj.transform, "❓ REGLAS", new Vector2(250, -40), new Vector2(140, 48));
            helpBtn.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f);

            // Center: Turn, Score, Energy
            var turnTMP = CreateTMPText("TurnText", headerObj.transform, "Turno <b>1</b> de 6", new Vector2(560, -40), new Vector2(220, 50), 22, TextAlignmentOptions.Center);
            turnTMP.color = Color.white;

            var scoreTMP = CreateTMPText("ScoreText", headerObj.transform, "0 vs 0", new Vector2(800, -40), new Vector2(200, 50), 26, TextAlignmentOptions.Center);
            scoreTMP.color = Color.white;

            var energyTMP = CreateTMPText("EnergyText", headerObj.transform, "🟢 <b>1</b> / 1", new Vector2(1040, -40), new Vector2(200, 50), 24, TextAlignmentOptions.Center);
            energyTMP.color = Color.white;

            // Right: Restart & End Turn Button
            var restartBtn = CreateButton("RestartBtn", headerObj.transform, "🔄", new Vector2(1340, -40), new Vector2(60, 48));
            restartBtn.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.38f);

            var endTurnBtn = CreateButton("EndTurnBtn", headerObj.transform, "FINALIZAR TURNO ⚔️", new Vector2(1640, -40), new Vector2(280, 56));
            var endTurnImg = endTurnBtn.GetComponent<Image>();
            endTurnImg.color = new Color(0.15f, 0.55f, 0.25f); // Deep emerald green
            var endTurnTMP = endTurnBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (endTurnTMP != null)
            {
                endTurnTMP.fontSize = 20;
                endTurnTMP.fontStyle = FontStyles.Bold;
                endTurnTMP.color = Color.white;
            }

            // Status Notification Banner (Underneath main header bar)
            var bannerObj = CreateUIElement("StatusBanner", headerObj.transform);
            var bRect = bannerObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.anchoredPosition = new Vector2(0, -2);
            bRect.sizeDelta = new Vector2(-40, 32);

            var bImg = bannerObj.AddComponent<Image>();
            bImg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);

            var statusTMP = CreateTMPText("StatusNotification", bannerObj.transform, "Bienvenido a Cometa Champions. Selecciona una carta para desplegar.", Vector2.zero, new Vector2(1200, 32), 15, TextAlignmentOptions.Center);
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.color = new Color(0.9f, 0.93f, 0.98f);

            // Assign SerializedProperties
            so.FindProperty("homeButton").objectReferenceValue = homeBtn;
            so.FindProperty("helpButton").objectReferenceValue = helpBtn;
            so.FindProperty("turnText").objectReferenceValue = turnTMP;
            so.FindProperty("scoreText").objectReferenceValue = scoreTMP;
            so.FindProperty("energyText").objectReferenceValue = energyTMP;
            so.FindProperty("statusNotificationText").objectReferenceValue = statusTMP;
            so.FindProperty("restartButton").objectReferenceValue = restartBtn;
            so.FindProperty("endTurnButton").objectReferenceValue = endTurnBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            return headerView;
        }

        private static BattleLaneView[] BuildLanes(Transform parent)
        {
            var lanesContainer = CreateUIElement("LanesContainer", parent);
            var rect = lanesContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(40, 220); // Above hand tray
            rect.offsetMax = new Vector2(-40, -118); // Below header & notification banner

            var results = new BattleLaneView[3];
            float[] xPositions = new float[] { -600f, 0f, 600f };

            for (int l = 0; l < 3; l++)
            {
                var laneObj = CreateUIElement($"Lane_{l}", lanesContainer.transform);
                var lRect = laneObj.GetComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0.5f, 0.5f);
                lRect.anchorMax = new Vector2(0.5f, 0.5f);
                lRect.pivot = new Vector2(0.5f, 0.5f);
                lRect.anchoredPosition = new Vector2(xPositions[l], 0);
                lRect.sizeDelta = new Vector2(560, 750);

                var laneBgImg = laneObj.AddComponent<Image>();
                laneBgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);

                var laneView = laneObj.AddComponent<BattleLaneView>();
                var so = new SerializedObject(laneView);
                so.FindProperty("laneIndex").intValue = l;

                // 1. AI Cards Zone (Top)
                var aiZone = CreateUIElement("AiZone", laneObj.transform);
                var aiRect = aiZone.GetComponent<RectTransform>();
                aiRect.anchorMin = new Vector2(0, 1);
                aiRect.anchorMax = new Vector2(1, 1);
                aiRect.pivot = new Vector2(0.5f, 1);
                aiRect.anchoredPosition = new Vector2(0, -10);
                aiRect.sizeDelta = new Vector2(0, 200);

                var aiSlotsProp = so.FindProperty("aiSlots");
                aiSlotsProp.arraySize = 4;
                for (int s = 0; s < 4; s++)
                {
                    float slotX = -195f + s * 130f;
                    var slotView = BuildSlotView($"AiSlot_{s}", aiZone.transform, new Vector2(slotX, -95), new Vector2(120, 170));
                    aiSlotsProp.GetArrayElementAtIndex(s).objectReferenceValue = slotView;
                }

                // 2. Arena Center Banner & Clash Meter
                var arenaCenter = CreateUIElement("ArenaCenter", laneObj.transform);
                var acRect = arenaCenter.GetComponent<RectTransform>();
                acRect.anchorMin = new Vector2(0, 0.5f);
                acRect.anchorMax = new Vector2(1, 0.5f);
                acRect.pivot = new Vector2(0.5f, 0.5f);
                acRect.anchoredPosition = new Vector2(0, 0);
                acRect.sizeDelta = new Vector2(0, 280);

                var acBg = arenaCenter.AddComponent<Image>();
                acBg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
                var acBtn = arenaCenter.AddComponent<Button>();
                acBtn.targetGraphic = acBg;
                so.FindProperty("arenaBackground").objectReferenceValue = acBg;
                so.FindProperty("columnClickButton").objectReferenceValue = acBtn;

                var arenaNameTMP = CreateTMPText("ArenaName", arenaCenter.transform, "Arena de Batalla", new Vector2(0, 105), new Vector2(500, 36), 22, TextAlignmentOptions.Center);
                arenaNameTMP.fontStyle = FontStyles.Bold;
                arenaNameTMP.color = new Color(0.96f, 0.82f, 0.35f);
                so.FindProperty("arenaNameText").objectReferenceValue = arenaNameTMP;

                var clanBadgeTMP = CreateTMPText("ClanBadge", arenaCenter.transform, "🌳 Milenarios", new Vector2(0, 75), new Vector2(250, 28), 16, TextAlignmentOptions.Center);
                clanBadgeTMP.color = Color.white;
                so.FindProperty("arenaClanText").objectReferenceValue = clanBadgeTMP;

                var effectTMP = CreateTMPText("ArenaEffect", arenaCenter.transform, "+1 de Poder a personajes del clan afín.", new Vector2(0, 40), new Vector2(500, 44), 14, TextAlignmentOptions.Center);
                effectTMP.color = new Color(0.85f, 0.88f, 0.92f);
                so.FindProperty("arenaEffectText").objectReferenceValue = effectTMP;

                // Power Score Clash Meter
                var meterObj = CreateUIElement("ClashMeter", arenaCenter.transform);
                var meterRect = meterObj.GetComponent<RectTransform>();
                meterRect.anchoredPosition = new Vector2(0, -20);
                meterRect.sizeDelta = new Vector2(400, 60);

                var aiPowerTMP = CreateTMPText("AiPower", meterObj.transform, "0", new Vector2(-90, 0), new Vector2(70, 50), 32, TextAlignmentOptions.Center);
                aiPowerTMP.fontStyle = FontStyles.Bold;
                aiPowerTMP.color = new Color(1f, 0.35f, 0.3f);
                so.FindProperty("aiPowerText").objectReferenceValue = aiPowerTMP;

                var vsTMP = CreateTMPText("VsIcon", meterObj.transform, "VS", new Vector2(0, 0), new Vector2(50, 50), 20, TextAlignmentOptions.Center);
                vsTMP.fontStyle = FontStyles.Bold;
                vsTMP.color = new Color(0.95f, 0.8f, 0.2f);

                var pPowerTMP = CreateTMPText("PlayerPower", meterObj.transform, "0", new Vector2(90, 0), new Vector2(70, 50), 32, TextAlignmentOptions.Center);
                pPowerTMP.fontStyle = FontStyles.Bold;
                pPowerTMP.color = new Color(0.2f, 0.85f, 0.35f);
                so.FindProperty("playerPowerText").objectReferenceValue = pPowerTMP;

                var statusTMP = CreateTMPText("StatusText", arenaCenter.transform, "⚔️ Empate", new Vector2(0, -65), new Vector2(400, 30), 16, TextAlignmentOptions.Center);
                statusTMP.fontStyle = FontStyles.Bold;
                statusTMP.color = new Color(0.9f, 0.85f, 0.3f);
                so.FindProperty("laneStatusText").objectReferenceValue = statusTMP;

                // Deploy Button
                var deployBtn = CreateButton("DeployBtn", arenaCenter.transform, "➕ Desplegar Aquí", new Vector2(0, -105), new Vector2(240, 44));
                var deployImg = deployBtn.GetComponent<Image>();
                deployImg.color = new Color(0.12f, 0.60f, 0.85f); // Vivid cyan blue
                var depTMP = deployBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (depTMP != null)
                {
                    depTMP.fontSize = 17;
                    depTMP.fontStyle = FontStyles.Bold;
                    depTMP.raycastTarget = false;
                }
                deployBtn.gameObject.SetActive(false);
                so.FindProperty("deployButton").objectReferenceValue = deployBtn;
                so.FindProperty("deployButtonText").objectReferenceValue = depTMP;
                so.FindProperty("deployButtonImage").objectReferenceValue = deployImg;

                // 3. Player Cards Zone (Bottom)
                var playerZone = CreateUIElement("PlayerZone", laneObj.transform);
                var pRect = playerZone.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0, 0);
                pRect.anchorMax = new Vector2(1, 0);
                pRect.pivot = new Vector2(0.5f, 0);
                pRect.anchoredPosition = new Vector2(0, 10);
                pRect.sizeDelta = new Vector2(0, 200);

                var playerSlotsProp = so.FindProperty("playerSlots");
                playerSlotsProp.arraySize = 4;
                for (int s = 0; s < 4; s++)
                {
                    float slotX = -195f + s * 130f;
                    var slotView = BuildSlotView($"PlayerSlot_{s}", playerZone.transform, new Vector2(slotX, 95), new Vector2(120, 170));
                    playerSlotsProp.GetArrayElementAtIndex(s).objectReferenceValue = slotView;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                results[l] = laneView;
            }

            return results;
        }

        private static BattleCardSlotView BuildSlotView(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var slotObj = CreateUIElement(name, parent);
            var rect = slotObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var frameImg = slotObj.AddComponent<Image>();
            frameImg.color = new Color(0.15f, 0.18f, 0.25f, 0.5f);

            var button = slotObj.AddComponent<Button>();

            // Card Artwork
            var artObj = CreateUIElement("CardArt", slotObj.transform);
            var artRect = artObj.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            var artImg = artObj.AddComponent<Image>();
            artImg.preserveAspect = false;
            artImg.enabled = false;
            artImg.raycastTarget = false;

            // Power Badge (Bottom Right)
            var pwrObj = CreateUIElement("PowerBadge", slotObj.transform);
            var pwrRect = pwrObj.GetComponent<RectTransform>();
            pwrRect.anchorMin = new Vector2(1, 0);
            pwrRect.anchorMax = new Vector2(1, 0);
            pwrRect.pivot = new Vector2(1, 0);
            pwrRect.anchoredPosition = new Vector2(-4, 4);
            pwrRect.sizeDelta = new Vector2(36, 36);

            var pwrBg = pwrObj.AddComponent<Image>();
            pwrBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            pwrBg.raycastTarget = false;

            var pwrText = CreateTMPText("Value", pwrObj.transform, "3", Vector2.zero, new Vector2(36, 36), 18, TextAlignmentOptions.Center);
            pwrText.fontStyle = FontStyles.Bold;
            pwrText.color = new Color(0.96f, 0.82f, 0.35f);
            pwrText.raycastTarget = false;
            pwrObj.SetActive(false);

            // Undo Badge (Top Right)
            var undoObj = CreateUIElement("UndoBadge", slotObj.transform);
            var undoRect = undoObj.GetComponent<RectTransform>();
            undoRect.anchorMin = new Vector2(1, 1);
            undoRect.anchorMax = new Vector2(1, 1);
            undoRect.pivot = new Vector2(1, 1);
            undoRect.anchoredPosition = new Vector2(-4, -4);
            undoRect.sizeDelta = new Vector2(32, 32);

            var undoBg = undoObj.AddComponent<Image>();
            undoBg.color = new Color(0.9f, 0.2f, 0.2f, 0.95f);
            undoBg.raycastTarget = false;

            var undoText = CreateTMPText("Icon", undoObj.transform, "✕", Vector2.zero, new Vector2(32, 32), 16, TextAlignmentOptions.Center);
            undoText.fontStyle = FontStyles.Bold;
            undoText.color = Color.white;
            undoText.raycastTarget = false;
            undoObj.SetActive(false);

            var slotView = slotObj.AddComponent<BattleCardSlotView>();
            var so = new SerializedObject(slotView);
            so.FindProperty("cardThumbnail").objectReferenceValue = artImg;
            so.FindProperty("slotFrame").objectReferenceValue = frameImg;
            so.FindProperty("powerBadgeRoot").objectReferenceValue = pwrObj;
            so.FindProperty("powerBadgeText").objectReferenceValue = pwrText;
            so.FindProperty("undoBadgeRoot").objectReferenceValue = undoObj;
            so.FindProperty("slotButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return slotView;
        }

        private static BattleHandView BuildHandTray(Transform parent)
        {
            var trayObj = CreateUIElement("BattleHandTray", parent);
            var rect = trayObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(0, 215);

            var bgImg = trayObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            var cardsContainer = CreateUIElement("CardsContainer", trayObj.transform);
            var cRect = cardsContainer.GetComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.offsetMin = new Vector2(20, 10);
            cRect.offsetMax = new Vector2(-20, -10);

            var layout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Empty hand label
            var emptyLabelTMP = CreateTMPText("EmptyLabel", trayObj.transform, "Mano vacía. Espera al siguiente turno...", Vector2.zero, new Vector2(600, 40), 20, TextAlignmentOptions.Center);
            emptyLabelTMP.color = new Color(0.6f, 0.6f, 0.65f);
            emptyLabelTMP.gameObject.SetActive(false);

            // Create 7 Card views for pool
            var handView = trayObj.AddComponent<BattleHandView>();
            var so = new SerializedObject(handView);
            so.FindProperty("cardsContainer").objectReferenceValue = cardsContainer.transform;
            so.FindProperty("emptyHandLabel").objectReferenceValue = emptyLabelTMP.gameObject;

            BattleHandCardView firstCard = null;
            for (int i = 0; i < 7; i++)
            {
                var cardView = BuildHandCardView($"HandCard_{i}", cardsContainer.transform);
                cardView.gameObject.SetActive(false);
                handView.RegisterSpawnedView(cardView);
                if (firstCard == null) firstCard = cardView;
            }
            so.FindProperty("cardPrefab").objectReferenceValue = firstCard;

            so.ApplyModifiedPropertiesWithoutUndo();
            return handView;
        }

        private static BattleHandCardView BuildHandCardView(string name, Transform parent)
        {
            var cardObj = CreateUIElement(name, parent);
            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(135, 195);

            var bgImg = cardObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.15f, 0.22f);

            var btn = cardObj.AddComponent<Button>();

            // Artwork
            var artObj = CreateUIElement("Art", cardObj.transform);
            var artRect = artObj.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            var artImg = artObj.AddComponent<Image>();
            artImg.preserveAspect = false;
            artImg.raycastTarget = false;

            // Cost Badge (Top Left)
            var costObj = CreateUIElement("CostBadge", cardObj.transform);
            var costRect = costObj.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 1);
            costRect.anchorMax = new Vector2(0, 1);
            costRect.pivot = new Vector2(0, 1);
            costRect.anchoredPosition = new Vector2(4, -4);
            costRect.sizeDelta = new Vector2(50, 26);
            var costBg = costObj.AddComponent<Image>();
            costBg.color = new Color(0.1f, 0.4f, 0.15f, 0.95f);
            costBg.raycastTarget = false;
            var costTMP = CreateTMPText("Val", costObj.transform, "1", Vector2.zero, new Vector2(50, 26), 14, TextAlignmentOptions.Center);
            costTMP.fontStyle = FontStyles.Bold;
            costTMP.raycastTarget = false;

            // Attack Badge (Top Right)
            var atkObj = CreateUIElement("AttackBadge", cardObj.transform);
            var atkRect = atkObj.GetComponent<RectTransform>();
            atkRect.anchorMin = new Vector2(1, 1);
            atkRect.anchorMax = new Vector2(1, 1);
            atkRect.pivot = new Vector2(1, 1);
            atkRect.anchoredPosition = new Vector2(-4, -4);
            atkRect.sizeDelta = new Vector2(50, 26);
            var atkBg = atkObj.AddComponent<Image>();
            atkBg.color = new Color(0.4f, 0.1f, 0.1f, 0.95f);
            atkBg.raycastTarget = false;
            var atkTMP = CreateTMPText("Val", atkObj.transform, "2", Vector2.zero, new Vector2(50, 26), 14, TextAlignmentOptions.Center);
            atkTMP.fontStyle = FontStyles.Bold;
            atkTMP.raycastTarget = false;

            // Name (Bottom)
            var nameTMP = CreateTMPText("Name", cardObj.transform, "Nombre", new Vector2(0, -78), new Vector2(130, 24), 12, TextAlignmentOptions.Center);
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            nameTMP.raycastTarget = false;

            // Selected Border
            var selObj = CreateUIElement("SelectedBorder", cardObj.transform);
            var selRect = selObj.GetComponent<RectTransform>();
            selRect.anchorMin = Vector2.zero;
            selRect.anchorMax = Vector2.one;
            selRect.offsetMin = new Vector2(-3, -3);
            selRect.offsetMax = new Vector2(3, 3);
            var selImg = selObj.AddComponent<Image>();
            selImg.color = new Color(0.96f, 0.82f, 0.25f, 0.85f); // Vibrant Gold
            selImg.raycastTarget = false;
            selObj.SetActive(false);

            // Unaffordable Dimmer
            var dimObj = CreateUIElement("Dimmer", cardObj.transform);
            var dimRect = dimObj.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0.04f, 0.05f, 0.08f, 0.35f);
            dimImg.raycastTarget = false;
            dimObj.SetActive(false);

            var view = cardObj.AddComponent<BattleHandCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("cardThumbnail").objectReferenceValue = artImg;
            so.FindProperty("costText").objectReferenceValue = costTMP;
            so.FindProperty("attackText").objectReferenceValue = atkTMP;
            so.FindProperty("nameText").objectReferenceValue = nameTMP;
            so.FindProperty("selectedBorder").objectReferenceValue = selObj;
            so.FindProperty("unaffordableDimmer").objectReferenceValue = dimObj;
            so.FindProperty("cardButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static BattleGameOverModal BuildGameOverModal(Transform parent)
        {
            var modalRoot = CreateUIElement("GameOverModalRoot", parent);
            var rect = modalRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            var windowObj = CreateUIElement("ModalWindow", modalRoot.transform);
            var wRect = windowObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.5f);
            wRect.anchorMax = new Vector2(0.5f, 0.5f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.sizeDelta = new Vector2(680, 520);

            var wImg = windowObj.AddComponent<Image>();
            wImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            var titleTMP = CreateTMPText("Title", windowObj.transform, "🏆 ¡VICTORIA ABSOLUTA!", new Vector2(0, 190), new Vector2(600, 50), 32, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.2f, 0.85f, 0.35f);

            var descTMP = CreateTMPText("Desc", windowObj.transform, "¡Has dominado el tablero y conquistado las arenas legendarias de Cometa Champions!", new Vector2(0, 130), new Vector2(580, 50), 16, TextAlignmentOptions.Center);
            descTMP.color = new Color(0.75f, 0.78f, 0.85f);

            // Stats comparison
            var statsBox = CreateUIElement("StatsBox", windowObj.transform);
            var sbRect = statsBox.GetComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(0, 20);
            sbRect.sizeDelta = new Vector2(560, 110);
            var sbImg = statsBox.AddComponent<Image>();
            sbImg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

            var pStatsTMP = CreateTMPText("PlayerStats", statsBox.transform, "2\nTus Arenas (18 ⚔️)", new Vector2(-150, 0), new Vector2(220, 80), 20, TextAlignmentOptions.Center);
            var vsStatsTMP = CreateTMPText("Vs", statsBox.transform, "VS", Vector2.zero, new Vector2(60, 60), 24, TextAlignmentOptions.Center);
            vsStatsTMP.color = new Color(0.6f, 0.6f, 0.65f);
            var aiStatsTMP = CreateTMPText("AiStats", statsBox.transform, "1\nArenas Rival (12 ⚔️)", new Vector2(150, 0), new Vector2(220, 80), 20, TextAlignmentOptions.Center);

            // Rewards
            var rewardsTMP = CreateTMPText("Rewards", windowObj.transform, "Recompensas: +150 🪙 Oro  •  +50 🌟 XP", new Vector2(0, -60), new Vector2(560, 36), 18, TextAlignmentOptions.Center);
            rewardsTMP.fontStyle = FontStyles.Bold;

            // Buttons
            var playAgainBtn = CreateButton("PlayAgainBtn", windowObj.transform, "🔄 Jugar Otra Partida", new Vector2(-140, -170), new Vector2(240, 52));
            var paImg = playAgainBtn.GetComponent<Image>();
            paImg.color = new Color(0.15f, 0.55f, 0.25f);

            var homeBtn = CreateButton("HomeBtn", windowObj.transform, "🏠 Volver al Menú", new Vector2(140, -170), new Vector2(220, 52));
            var hImg = homeBtn.GetComponent<Image>();
            hImg.color = new Color(0.20f, 0.25f, 0.35f);

            var modal = modalRoot.AddComponent<BattleGameOverModal>();
            var so = new SerializedObject(modal);
            so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
            so.FindProperty("titleText").objectReferenceValue = titleTMP;
            so.FindProperty("descriptionText").objectReferenceValue = descTMP;
            so.FindProperty("playerStatsText").objectReferenceValue = pStatsTMP;
            so.FindProperty("aiStatsText").objectReferenceValue = aiStatsTMP;
            so.FindProperty("rewardsText").objectReferenceValue = rewardsTMP;
            so.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;
            so.FindProperty("homeButton").objectReferenceValue = homeBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            modalRoot.SetActive(false);
            return modal;
        }

        private static BattleHelpModal BuildHelpModal(Transform parent)
        {
            var modalRoot = CreateUIElement("HelpModalRoot", parent);
            var rect = modalRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            var windowObj = CreateUIElement("HelpWindow", modalRoot.transform);
            var wRect = windowObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.5f);
            wRect.anchorMax = new Vector2(0.5f, 0.5f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.sizeDelta = new Vector2(960, 680);

            var wImg = windowObj.AddComponent<Image>();
            wImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

            // Title
            var titleTMP = CreateTMPText("Title", windowObj.transform, "MANUAL DE COMBATE TCG", new Vector2(0, 280), new Vector2(800, 40), 28, TextAlignmentOptions.Center);
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.96f, 0.82f, 0.35f);

            var subtitleTMP = CreateTMPText("Sub", windowObj.transform, "Estrategia de 3 Arenas inspirada en MARVEL SNAP con ventajas de Pokémon TCG Pocket.", new Vector2(0, 240), new Vector2(860, 32), 16, TextAlignmentOptions.Center);
            subtitleTMP.color = new Color(0.7f, 0.75f, 0.85f);

            // Close button X
            var closeBtn = CreateButton("CloseXBtn", windowObj.transform, "✕", new Vector2(430, 280), new Vector2(44, 44));
            closeBtn.GetComponent<Image>().color = new Color(0.3f, 0.35f, 0.45f);

            // 4 Rule cards in 2x2 grid
            float[] rx = new float[] { -230, 230, -230, 230 };
            float[] ry = new float[] { 100, 100, -80, -80 };

            string[] ruleTitles = new string[]
            {
                "🏆 1. Mazo y Objetivo (2 de 3)",
                "🃏 2. Mano Inicial y Mulligan",
                "⚡ 3. Acciones Simultáneas",
                "🌀 4. Habilidades y Círculo Cometa"
            };

            string[] ruleDescs = new string[]
            {
                "Mazo de exactamente 12 cartas (máx 2 copias). Se combate en 3 Arenas a la vez. Al fin del Turno 6, gana quien lidere al menos 2 de las 3 arenas o desempate por Poder Total.",
                "Inicias con 4 cartas. Antes del Turno 1 puedes realizar 1 Mulligan para cambiar cualquier número de cartas. En cada turno se roba 1 carta (viendo 10 de 12 cartas).",
                "Ambos jugadores eligen jugadas al mismo tiempo; las cartas rivales permanecen boca abajo hasta que presionas 'Finalizar Turno', activando la resolución en 8 pasos.",
                "Héroes, Bestias y Trompos con habilidades OnPlay, Pasivas, Fin de Turno y Combos. Ventaja de clan otorga +1 Poder: 🌳 Milenarios > ⛓️ Fugitivos > 🪓 Depredadores > 🐉 Míticos."
            };

            for (int r = 0; r < 4; r++)
            {
                var cardBox = CreateUIElement($"Rule_{r}", windowObj.transform);
                var cRect = cardBox.GetComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(rx[r], ry[r]);
                cRect.sizeDelta = new Vector2(430, 150);

                var cImg = cardBox.AddComponent<Image>();
                cImg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

                var rTitle = CreateTMPText("Title", cardBox.transform, ruleTitles[r], new Vector2(0, 48), new Vector2(400, 32), 16, TextAlignmentOptions.Left);
                rTitle.fontStyle = FontStyles.Bold;
                rTitle.color = new Color(0.96f, 0.82f, 0.35f);

                var rDesc = CreateTMPText("Desc", cardBox.transform, ruleDescs[r], new Vector2(0, -15), new Vector2(400, 95), 13, TextAlignmentOptions.TopLeft);
                rDesc.color = new Color(0.80f, 0.83f, 0.89f);
            }

            // Got it button
            var gotItBtn = CreateButton("GotItBtn", windowObj.transform, "¡Entendido, a Combatir! ⚔️", new Vector2(0, -260), new Vector2(300, 52));
            var gImg = gotItBtn.GetComponent<Image>();
            gImg.color = new Color(0.12f, 0.55f, 0.85f);

            var modal = modalRoot.AddComponent<BattleHelpModal>();
            var so = new SerializedObject(modal);
            so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("gotItButton").objectReferenceValue = gotItBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            modalRoot.SetActive(false);
            return modal;
        }

        private static BattleMulliganModal BuildMulliganModal(Transform parent)
        {
            var modalRoot = CreateUIElement("MulliganModal", parent);
            var mRect = modalRoot.GetComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            var bgImg = modalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);

            var windowObj = CreateUIElement("Window", modalRoot.transform);
            var wRect = windowObj.GetComponent<RectTransform>();
            wRect.anchoredPosition = Vector2.zero;
            wRect.sizeDelta = new Vector2(740, 520);

            var wImg = windowObj.AddComponent<Image>();
            wImg.color = new Color(0.12f, 0.14f, 0.20f, 0.98f);

            var title = CreateTMPText("Title", windowObj.transform, "FASE DE MULLIGAN (Regla 2)", new Vector2(0, 220), new Vector2(680, 40), 22, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.96f, 0.82f, 0.35f);

            var sub = CreateTMPText("Subtitle", windowObj.transform, "Selecciona las cartas que deseas cambiar de tu mano inicial (0 a 4) y presiona confirmar, o conserva tu mano.", new Vector2(0, 180), new Vector2(660, 36), 14, TextAlignmentOptions.Center);
            sub.color = new Color(0.80f, 0.85f, 0.92f);

            // 4 Card Slots
            float[] cardX = { -240f, -80f, 80f, 240f };
            var cardButtons = new Button[4];
            var cardArtImages = new Image[4];
            var cardNameTexts = new TextMeshProUGUI[4];
            var cardCostTexts = new TextMeshProUGUI[4];
            var cardSelectedOverlays = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                var cardObj = CreateUIElement($"CardSlot_{i}", windowObj.transform);
                var cRect = cardObj.GetComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(cardX[i], 20);
                cRect.sizeDelta = new Vector2(145, 230);

                var cBg = cardObj.AddComponent<Image>();
                cBg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);
                var btn = cardObj.AddComponent<Button>();
                btn.targetGraphic = cBg;
                cardButtons[i] = btn;

                // Card Art
                var artObj = CreateUIElement("Art", cardObj.transform);
                var artRect = artObj.GetComponent<RectTransform>();
                artRect.anchoredPosition = new Vector2(0, 25);
                artRect.sizeDelta = new Vector2(130, 140);
                var artImg = artObj.AddComponent<Image>();
                artImg.preserveAspect = true;
                artImg.raycastTarget = false;
                artImg.color = Color.clear;
                cardArtImages[i] = artImg;

                // Cost / Power text
                var costTxt = CreateTMPText("CostPower", cardObj.transform, "COST 1 | PWR 2", new Vector2(0, 85), new Vector2(130, 24), 12, TextAlignmentOptions.Center);
                costTxt.fontStyle = FontStyles.Bold;
                cardCostTexts[i] = costTxt;

                // Name text
                var nameTxt = CreateTMPText("Name", cardObj.transform, "Carta", new Vector2(0, -65), new Vector2(135, 30), 12, TextAlignmentOptions.Center);
                nameTxt.fontStyle = FontStyles.Bold;
                cardNameTexts[i] = nameTxt;

                // Selection Overlay
                var overlayObj = CreateUIElement("SelectedOverlay", cardObj.transform);
                var oRect = overlayObj.GetComponent<RectTransform>();
                oRect.anchorMin = Vector2.zero;
                oRect.anchorMax = Vector2.one;
                oRect.offsetMin = Vector2.zero;
                oRect.offsetMax = Vector2.zero;
                var oImg = overlayObj.AddComponent<Image>();
                oImg.color = new Color(0.85f, 0.25f, 0.20f, 0.45f);
                oImg.raycastTarget = false;

                var badge = CreateTMPText("Badge", overlayObj.transform, "CAMBIAR", new Vector2(0, 0), new Vector2(130, 30), 14, TextAlignmentOptions.Center);
                badge.fontStyle = FontStyles.Bold;
                badge.color = Color.white;

                overlayObj.SetActive(false);
                cardSelectedOverlays[i] = overlayObj;
            }

            // Buttons
            var keepBtn = CreateButton("KeepAllBtn", windowObj.transform, "CONSERVAR MANO", new Vector2(-160, -185), new Vector2(270, 52));
            var kImg = keepBtn.GetComponent<Image>();
            kImg.color = new Color(0.12f, 0.58f, 0.32f);

            var confirmBtn = CreateButton("ConfirmBtn", windowObj.transform, "CAMBIAR NINGUNA", new Vector2(160, -185), new Vector2(270, 52));
            var cfImg = confirmBtn.GetComponent<Image>();
            cfImg.color = new Color(0.18f, 0.42f, 0.75f);
            var cfTxt = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();

            var modal = modalRoot.AddComponent<BattleMulliganModal>();
            var so = new SerializedObject(modal);
            so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("subtitleText").objectReferenceValue = sub;

            var btnProp = so.FindProperty("cardButtons");
            btnProp.arraySize = 4;
            var artProp = so.FindProperty("cardArtImages");
            artProp.arraySize = 4;
            var nameProp = so.FindProperty("cardNameTexts");
            nameProp.arraySize = 4;
            var costProp = so.FindProperty("cardCostTexts");
            costProp.arraySize = 4;
            var overProp = so.FindProperty("cardSelectedOverlays");
            overProp.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                btnProp.GetArrayElementAtIndex(i).objectReferenceValue = cardButtons[i];
                artProp.GetArrayElementAtIndex(i).objectReferenceValue = cardArtImages[i];
                nameProp.GetArrayElementAtIndex(i).objectReferenceValue = cardNameTexts[i];
                costProp.GetArrayElementAtIndex(i).objectReferenceValue = cardCostTexts[i];
                overProp.GetArrayElementAtIndex(i).objectReferenceValue = cardSelectedOverlays[i];
            }

            so.FindProperty("keepAllButton").objectReferenceValue = keepBtn;
            so.FindProperty("confirmMulliganButton").objectReferenceValue = confirmBtn;
            so.FindProperty("confirmButtonText").objectReferenceValue = cfTxt;
            so.ApplyModifiedPropertiesWithoutUndo();

            modalRoot.SetActive(false);
            return modal;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f);

            var btn = go.AddComponent<Button>();

            if (!string.IsNullOrEmpty(label))
            {
                var txt = CreateTMPText("Text", go.transform, label, Vector2.zero, size, 16, TextAlignmentOptions.Center);
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
            }

            return btn;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
