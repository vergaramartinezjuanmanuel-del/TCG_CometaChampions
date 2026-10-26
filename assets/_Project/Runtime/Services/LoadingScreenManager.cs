#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Centralized persistent loading transition manager.
    /// Manages smooth scene transitions between Home and Battle featuring
    /// an interactive 3D circular card carousel (matching Center_Card3D_Viewport dimensions)
    /// and a minimum 1.5-second cinematic display duration.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        private static LoadingScreenManager? instance;
        public static LoadingScreenManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindAnyObjectByType<LoadingScreenManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("LoadingScreenManager");
                        instance = go.AddComponent<LoadingScreenManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public const float MinimumDisplayDuration = 1.5f;
        public const float FadeDuration = 0.25f;

        [Header("State")]
        public bool IsLoading { get; private set; }

        private GameObject? loadingRoot;
        private Camera? loadingCamera;
        private CardCarousel3D? cardCarousel;
        private Canvas? loadingCanvas;
        private CanvasGroup? fadeCanvasGroup;
        private TextMeshProUGUI? headerTitleText;
        private TextMeshProUGUI? subtitleHintText;
        private TextMeshProUGUI? statusDotsText;
        private RectTransform? progressBarFill;
        private GameObject? progressBarRoot;
        private Coroutine? activeLoadingRoutine;

        private static readonly string[] LoreHints = new[]
        {
            "Consejo: ¡Los Milenarios tienen ventaja contra Fugitivos (+1 Daño)!",
            "Consejo: ¡Los Fugitivos superan a los Depredadores con astucia y velocidad!",
            "Consejo: ¡Los Depredadores cazan a los Míticos con ataques contundentes!",
            "Consejo: ¡Los Míticos trascienden a los Milenarios con magia cósmica!",
            "Consejo: ¡Las cartas de Arena otorgan bonificaciones de clan en cada carril!",
            "Consejo: ¡Puedes consultar y afinar tu mazo en cualquier momento desde 'Mis Cartas'!"
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureLoadingHierarchy();
        }

        /// <summary>
        /// Public static helper to initiate a loading transition safely from any script.
        /// </summary>
        public static void Load(string sceneName, string loadingMessage = "CARGANDO...", string hint = "")
        {
            Instance.LoadScene(sceneName, loadingMessage, hint);
        }

        /// <summary>
        /// Initiates asynchronous scene loading with the 3D circular card carousel.
        /// </summary>
        public void LoadScene(string sceneName, string loadingMessage = "CARGANDO...", string hint = "")
        {
            if (IsLoading) return;

            if (string.IsNullOrEmpty(hint))
            {
                hint = LoreHints[UnityEngine.Random.Range(0, LoreHints.Length)];
            }

            EnsureLoadingHierarchy();
            if (activeLoadingRoutine != null) StopCoroutine(activeLoadingRoutine);
            activeLoadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName, loadingMessage, hint));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string message, string hint)
        {
            IsLoading = true;

            // 1. Play Scene Transition SFX
            AudioManager.Instance.PlaySfx(SfxType.SceneTransition);

            // 2. Prepare UI Text & Progress
            if (headerTitleText != null) headerTitleText.text = message;
            if (subtitleHintText != null) subtitleHintText.text = hint;
            if (statusDotsText != null) statusDotsText.text = "CARGANDO";
            if (progressBarFill != null) progressBarFill.sizeDelta = new Vector2(0f, progressBarFill.sizeDelta.y);

            // 3. Immediately hide all scene canvases, cameras, and objects so ONLY Loading is shown
            HideAllSceneContent();

            // 4. Activate Loading Hierarchy & 3D Carousel under a black veil
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;
            if (loadingRoot != null) loadingRoot.SetActive(true);
            if (loadingCamera != null) loadingCamera.enabled = true;
            if (cardCarousel != null) cardCarousel.gameObject.SetActive(true);
            if (headerTitleText != null) headerTitleText.gameObject.SetActive(true);
            if (subtitleHintText != null) subtitleHintText.gameObject.SetActive(true);
            if (statusDotsText != null) statusDotsText.gameObject.SetActive(true);
            if (progressBarRoot != null) progressBarRoot.SetActive(true);

            // 5. Begin Asynchronous Scene Loading
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            // 6. Smoothly reveal the 3D rotating carousel and Loading UI (0.2s)
            yield return StartCoroutine(FadeRoutine(1f, 0f, 0.2f));

            // 7. Wait for both: minimum display duration (1.5s) AND scene loading progress
            float timer = 0f;
            int dotIndex = 0;
            float dotTimer = 0f;

            while (timer < MinimumDisplayDuration || asyncOp.progress < 0.9f)
            {
                timer += Time.unscaledDeltaTime;
                dotTimer += Time.unscaledDeltaTime;

                // Animate loading dots
                if (dotTimer >= 0.35f)
                {
                    dotTimer = 0f;
                    dotIndex = (dotIndex + 1) % 4;
                    string dots = new string('.', dotIndex);
                    if (statusDotsText != null) statusDotsText.text = $"CARGANDO{dots}";
                }

                // Update visual progress bar (normalized from 0 to 1 based on 0.9 progress)
                float simulatedProgress = Mathf.Clamp01(Mathf.Max(timer / MinimumDisplayDuration, asyncOp.progress / 0.9f));
                if (progressBarFill != null)
                {
                    progressBarFill.sizeDelta = new Vector2(simulatedProgress * 480f, progressBarFill.sizeDelta.y);
                }

                yield return null;
            }

            // Completed loading
            if (progressBarFill != null)
            {
                progressBarFill.sizeDelta = new Vector2(480f, progressBarFill.sizeDelta.y);
            }
            if (statusDotsText != null) statusDotsText.text = "¡LISTO!";

            // 8. Fade to black before activating scene
            yield return StartCoroutine(FadeRoutine(0f, 1f, FadeDuration));

            // 9. Allow scene activation and wait for completion
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            // Wait for new scene Awake & Start to run, and wait for initial frame rendering
            yield return null;
            yield return new WaitForEndOfFrame();

            // 10. Completely hide all loading visual elements (3D carousel cards, camera, UI texts, progress bar)
            // BEFORE fading from black, while STILL completely concealed by the black veil.
            // This guarantees that the user NEVER sees the loading cards or loading UI when returning to Home!
            if (cardCarousel != null) cardCarousel.gameObject.SetActive(false);
            if (loadingCamera != null) loadingCamera.enabled = false;
            if (headerTitleText != null) headerTitleText.gameObject.SetActive(false);
            if (subtitleHintText != null) subtitleHintText.gameObject.SetActive(false);
            if (statusDotsText != null) statusDotsText.gameObject.SetActive(false);
            if (progressBarRoot != null) progressBarRoot.SetActive(false);

            // 11. Fade from black revealing ONLY the newly loaded scene
            yield return StartCoroutine(FadeRoutine(1f, 0f, FadeDuration));

            // 12. Deactivate Loading Hierarchy completely
            if (loadingRoot != null) loadingRoot.SetActive(false);

            IsLoading = false;
            activeLoadingRoutine = null;
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
        {
            if (fadeCanvasGroup == null) yield break;

            float elapsed = 0f;
            fadeCanvasGroup.alpha = fromAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }

            fadeCanvasGroup.alpha = toAlpha;
        }

        /// <summary>
        /// Ensures that ALL scene cameras, canvases, viewports, and visual elements from the current scene
        /// are disabled/hidden so that ONLY the loading screen (3D carousel, loading UI, dark background) is visible.
        /// </summary>
        public void HideAllSceneContent()
        {
            // 1. Disable all other cameras in the scene
            var allCameras = Camera.allCameras;
            foreach (var cam in allCameras)
            {
                if (cam != null && cam != loadingCamera)
                {
                    cam.enabled = false;
                }
            }

            // 2. Disable all other canvases outside loadingRoot
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allCanvases)
            {
                if (c != null && c != loadingCanvas && (loadingRoot == null || !c.transform.IsChildOf(loadingRoot.transform)))
                {
                    c.enabled = false;
                }
            }

            // 3. Deactivate known 3D viewports, backgrounds, and anchors
            var centerViewport = GameObject.Find("Center_Card3D_Viewport");
            if (centerViewport != null) centerViewport.SetActive(false);

            var pedestal = GameObject.Find("Pedestal_3D");
            if (pedestal != null) pedestal.SetActive(false);

            var bgCanvas = GameObject.Find("Background_Canvas");
            if (bgCanvas != null) bgCanvas.SetActive(false);

            var battleCanvas = GameObject.Find("BattleCanvas");
            if (battleCanvas != null) battleCanvas.SetActive(false);
        }

        private void EnsureLoadingHierarchy()
        {
            if (loadingRoot != null) return;

            // 1. Root Container
            loadingRoot = new GameObject("LoadingScreen_Root");
            loadingRoot.transform.SetParent(transform, false);

            // 2. Dedicated Camera
            var camGo = new GameObject("LoadingCamera");
            camGo.transform.SetParent(loadingRoot.transform, false);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.transform.rotation = Quaternion.identity;

            loadingCamera = camGo.AddComponent<Camera>();
            loadingCamera.clearFlags = CameraClearFlags.SolidColor;
            loadingCamera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            loadingCamera.fieldOfView = 60f;
            loadingCamera.nearClipPlane = 0.3f;
            loadingCamera.farClipPlane = 100f;
            loadingCamera.depth = 100f; // Render on top of scene cameras
            loadingCamera.enabled = false;

            // 3. Directional Light for 3D card materials
            var lightGo = new GameObject("LoadingLight");
            lightGo.transform.SetParent(loadingRoot.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(20f, -20f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.15f;

            // 4. 3D Card Carousel
            var carouselGo = new GameObject("CardCarousel3D");
            carouselGo.transform.SetParent(loadingRoot.transform, false);
            carouselGo.transform.position = new Vector3(0f, 0.15f, 0f);
            cardCarousel = carouselGo.AddComponent<CardCarousel3D>();

            // 5. Screen Space Overlay Canvas
            var canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(loadingRoot.transform, false);
            loadingCanvas = canvasGo.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 6. Full-screen raycast blocker (transparent, consumes clicks during loading)
            var blockerGo = new GameObject("InputBlocker", typeof(RectTransform));
            blockerGo.transform.SetParent(canvasGo.transform, false);
            var blockerRt = blockerGo.GetComponent<RectTransform>();
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.sizeDelta = Vector2.zero;
            var blockerImg = blockerGo.AddComponent<Image>();
            blockerImg.color = Color.clear;
            blockerImg.raycastTarget = true;

            // 7. Fade Overlay (Black Image covering entire screen with CanvasGroup)
            var fadeGo = new GameObject("FadeOverlay", typeof(RectTransform));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            var fadeRt = fadeGo.GetComponent<RectTransform>();
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.sizeDelta = Vector2.zero;

            var fadeImg = fadeGo.AddComponent<Image>();
            fadeImg.color = Color.black;
            fadeImg.raycastTarget = false;

            fadeCanvasGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 1f;

            // 8. Header Title Text (Top)
            var titleFont = DynamicCardAssembler.GetCardFont();
            headerTitleText = CreateTMP(canvasGo.transform, "HeaderTitle", "INICIANDO COMBATE...", titleFont, 36f, new Color(1f, 0.85f, 0.25f, 1f), new Vector2(0f, -90f), new Vector2(900f, 60f), FontStyles.Bold);

            // 9. Subtitle Hint Text (Below Title)
            subtitleHintText = CreateTMP(canvasGo.transform, "SubtitleHint", LoreHints[0], titleFont, 19f, new Color(0.80f, 0.84f, 0.92f, 0.90f), new Vector2(0f, -145f), new Vector2(1000f, 50f), FontStyles.Normal);

            // 10. Status & Progress Bar (Bottom)
            statusDotsText = CreateTMP(canvasGo.transform, "StatusText", "CARGANDO...", titleFont, 22f, new Color(0.35f, 0.88f, 1f, 1f), new Vector2(0f, 120f), new Vector2(400f, 40f), FontStyles.Bold);

            // Progress Bar Background
            var pbBgGo = new GameObject("ProgressBar_Bg", typeof(RectTransform));
            pbBgGo.transform.SetParent(canvasGo.transform, false);
            progressBarRoot = pbBgGo;
            var pbBgRt = pbBgGo.GetComponent<RectTransform>();
            pbBgRt.anchorMin = new Vector2(0.5f, 0f);
            pbBgRt.anchorMax = new Vector2(0.5f, 0f);
            pbBgRt.pivot = new Vector2(0.5f, 0.5f);
            pbBgRt.anchoredPosition = new Vector2(0f, 80f);
            pbBgRt.sizeDelta = new Vector2(480f, 6f);

            var pbBgImg = pbBgGo.AddComponent<Image>();
            pbBgImg.color = new Color(0.12f, 0.15f, 0.22f, 0.85f);
            pbBgImg.raycastTarget = false;

            // Progress Bar Fill
            var pbFillGo = new GameObject("ProgressBar_Fill", typeof(RectTransform));
            pbFillGo.transform.SetParent(pbBgGo.transform, false);
            progressBarFill = pbFillGo.GetComponent<RectTransform>();
            progressBarFill.anchorMin = new Vector2(0f, 0f);
            progressBarFill.anchorMax = new Vector2(0f, 1f);
            progressBarFill.pivot = new Vector2(0f, 0.5f);
            progressBarFill.anchoredPosition = Vector2.zero;
            progressBarFill.sizeDelta = new Vector2(0f, 0f);

            var pbFillImg = pbFillGo.AddComponent<Image>();
            pbFillImg.color = new Color(0.20f, 0.85f, 1f, 1f);
            pbFillImg.raycastTarget = false;

            // Ensure FadeOverlay stays in front of all loading canvas elements
            fadeGo.transform.SetAsLastSibling();

            loadingRoot.SetActive(false);
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, Vector2 anchoredPos, Vector2 sizeDelta, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchoredPos.y < 0 ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rt.anchorMax = anchoredPos.y < 0 ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;

            return tmp;
        }
    }
}
