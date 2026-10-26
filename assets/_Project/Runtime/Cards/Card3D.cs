using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using CometaChampions.Core.Helpers;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Cards
{
    /// <summary>
    /// Double-sided 3D card representation with high-res front texture,
    /// customizable official reverso (Oro, Color, Bi), millimeter edge geometry,
    /// and dynamic Foil metallic/smoothness shader properties compatible with
    /// both Built-in and Universal Render Pipelines.
    /// </summary>
    [ExecuteAlways]
    public class Card3D : MonoBehaviour
    {
        public const float CardWidth = 2.5f;
        public const float CardHeight = 3.5f;
        public const float CardThickness = 0.04f;

        [Header("Renderers")]
        [SerializeField] private MeshRenderer frontRenderer;
        [SerializeField] private MeshRenderer backRenderer;
        [SerializeField] private MeshRenderer edgeRenderer;

        [Header("Current State")]
        [SerializeField] private string currentCardId = "CC-01";
        [SerializeField] private string currentReversoId = "oro";
        [SerializeField] [Range(1, 4)] private int currentFoilLevel = 1;

        private MaterialPropertyBlock frontPropBlock;
        private MaterialPropertyBlock backPropBlock;
        private MaterialPropertyBlock edgePropBlock;
        private RectTransform frontCanvasRect;

        private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

        public static int CachedTextureCount => TextureCache.Count;
        public static int CachedSpriteCount => SpriteCache.Count;

        public ShowcaseCardData CurrentCardData { get; private set; }
        public ShowcaseReversoData CurrentReversoData { get; private set; }
        public int FoilLevel => currentFoilLevel;

        private void Awake()
        {
            EnsureHierarchy();
            InitPropertyBlocks();
        }

        private void Start()
        {
            if (CurrentCardData == null && !string.IsNullOrEmpty(currentCardId))
            {
                var initial = CardShowcaseCatalog.GetCardById(currentCardId) ?? CardShowcaseCatalog.Cards[0];
                SetCard(initial, currentFoilLevel);
            }
        }

        private void LateUpdate()
        {
            if (frontCanvasRect != null && frontRenderer != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 toCam = (cam.transform.position - frontRenderer.transform.position).normalized;
                    // FrontFace quad normal points along -frontRenderer.transform.forward
                    bool facingCamera = Vector3.Dot(-frontRenderer.transform.forward, toCam) > 0f;
                    if (frontCanvasRect.gameObject.activeSelf != facingCamera)
                    {
                        frontCanvasRect.gameObject.SetActive(facingCamera);
                    }
                }
            }
        }

        private void OnValidate()
        {
            EnsureHierarchy();
            InitPropertyBlocks();
        }

        private void InitPropertyBlocks()
        {
            frontPropBlock ??= new MaterialPropertyBlock();
            backPropBlock ??= new MaterialPropertyBlock();
            edgePropBlock ??= new MaterialPropertyBlock();
        }

        public static Shader GetAppropriateShader(bool isColorOnly = false)
        {
            // Only use Universal Render Pipeline shaders if a Scriptable Render Pipeline is actually configured!
            bool isSrpActive = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

            if (isSrpActive)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) return urpLit;
                var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (urpUnlit != null) return urpUnlit;
            }

            // Built-in Render Pipeline
            if (isColorOnly)
            {
                return Shader.Find("Standard")
                       ?? Shader.Find("Unlit/Color")
                       ?? Shader.Find("Mobile/Diffuse")
                       ?? Shader.Find("UI/Default")
                       ?? Shader.Find("Legacy Shaders/Diffuse");
            }

            return Shader.Find("Standard")
                   ?? Shader.Find("Unlit/Texture")
                   ?? Shader.Find("Mobile/Diffuse")
                   ?? Shader.Find("Mobile/Unlit (Supports Lightmap)")
                   ?? Shader.Find("UI/Default")
                   ?? Shader.Find("Legacy Shaders/Diffuse");
        }

        private void EnsureMaterialForRenderer(MeshRenderer mr, string matName, bool isColorOnly = false)
        {
            if (mr == null) return;
            var targetShader = GetAppropriateShader(isColorOnly);

            bool needsNewMat = false;
            var currentMat = mr.sharedMaterial;
            if (currentMat == null)
            {
                needsNewMat = true;
            }
            else
            {
                var curShader = currentMat.shader;
                if (curShader == null || !curShader.isSupported)
                {
                    needsNewMat = true;
                }
                // If a URP shader was assigned but the project is running Built-in pipeline, replace it!
                else if (curShader.name.Contains("Universal Render Pipeline") && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                {
                    needsNewMat = true;
                }
            }

            if (needsNewMat && targetShader != null)
            {
                var mat = new Material(targetShader) { name = matName };
                mr.sharedMaterial = mat;
            }
        }

        public void EnsureHierarchy()
        {
            // 1. Front Quad (facing camera at negative Z)
            var frontTransform = transform.Find("FrontFace");
            if (frontTransform == null)
            {
                var frontGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                frontGo.name = "FrontFace";
                frontGo.transform.SetParent(transform, false);
                frontGo.transform.localPosition = new Vector3(0f, 0f, -CardThickness * 0.51f);
                frontGo.transform.localRotation = Quaternion.identity;
                frontGo.transform.localScale = new Vector3(CardWidth, CardHeight, 1f);

                var col = frontGo.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                frontRenderer = frontGo.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(frontRenderer, "Card_Front_Mat", false);
            }
            else
            {
                frontTransform.localPosition = new Vector3(0f, 0f, -CardThickness * 0.51f);
                frontTransform.localRotation = Quaternion.identity;
                frontTransform.localScale = new Vector3(CardWidth, CardHeight, 1f);
                frontRenderer = frontTransform.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(frontRenderer, "Card_Front_Mat", false);
            }

            // 2. Back Quad (facing away from camera at positive Z, rotated 180 on Y)
            var backTransform = transform.Find("BackFace");
            if (backTransform == null)
            {
                var backGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backGo.name = "BackFace";
                backGo.transform.SetParent(transform, false);
                backGo.transform.localPosition = new Vector3(0f, 0f, CardThickness * 0.51f);
                backGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                backGo.transform.localScale = new Vector3(CardWidth, CardHeight, 1f);

                var col = backGo.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                backRenderer = backGo.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(backRenderer, "Card_Back_Mat", false);
            }
            else
            {
                backTransform.localPosition = new Vector3(0f, 0f, CardThickness * 0.51f);
                backTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                backTransform.localScale = new Vector3(CardWidth, CardHeight, 1f);
                backRenderer = backTransform.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(backRenderer, "Card_Back_Mat", false);
            }

            // 3. Edge Core (thin cube acting as the card's physical cardstock edge)
            var edgeTransform = transform.Find("CardEdge");
            if (edgeTransform == null)
            {
                var edgeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                edgeGo.name = "CardEdge";
                edgeGo.transform.SetParent(transform, false);
                edgeGo.transform.localPosition = Vector3.zero;
                edgeGo.transform.localRotation = Quaternion.identity;
                edgeGo.transform.localScale = new Vector3(CardWidth - 0.01f, CardHeight - 0.01f, CardThickness);

                var col = edgeGo.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                edgeRenderer = edgeGo.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(edgeRenderer, "Card_Edge_Mat", true);
            }
            else
            {
                edgeRenderer = edgeTransform.GetComponent<MeshRenderer>();
                EnsureMaterialForRenderer(edgeRenderer, "Card_Edge_Mat", true);
            }
        }

        public void SetCard(CardDefinition definition, int foilLevel = 1)
        {
            if (definition == null) return;
            var showcaseData = DynamicCardAssembler.ResolveShowcaseCardData(definition);
            if (showcaseData != null)
            {
                SetCard(showcaseData, foilLevel);
            }
        }

        public void SetCard(ShowcaseCardData cardData, int foilLevel = 1)
        {
            if (cardData == null) return;
            CurrentCardData = cardData;
            currentCardId = cardData.Id;
            currentFoilLevel = Mathf.Clamp(foilLevel, 1, 4);

            EnsureHierarchy();
            InitPropertyBlocks();

            string flatArtPath = CardArtMapper.GetFlatArtPath(cardData);
            var frontTex = DynamicCardAssembler.RenderCardToTexture(cardData, isMain3DCard: true)
                           ?? LoadTextureFromCardsFolder(flatArtPath)
                           ?? LoadTextureFromCardsFolder(cardData.Image);

            if (frontTex != null && frontRenderer != null)
            {
                // Ensure front renderer has its own material with the texture assigned
                var mat = Application.isPlaying ? frontRenderer.material : frontRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_MainTex")) mat.mainTexture = frontTex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", frontTex);
                    if (mat.HasProperty("_Color")) mat.color = Color.white;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                }

                frontRenderer.GetPropertyBlock(frontPropBlock);
                frontPropBlock.SetTexture("_BaseMap", frontTex);
                frontPropBlock.SetTexture("_MainTex", frontTex);
                ApplyFoilToPropertyBlock(frontPropBlock, currentFoilLevel);
                frontRenderer.SetPropertyBlock(frontPropBlock);
            }

            if (frontRenderer != null)
            {
                EnsureFrontDynamicCanvas(cardData);
            }

            // Ensure back reverso is updated
            if (CurrentReversoData == null)
            {
                SetReverso(currentReversoId);
            }
            else
            {
                UpdateBackRenderer();
            }

            // Edge sheen matches foil level
            UpdateEdgeFoil();
        }

        private void EnsureFrontDynamicCanvas(ShowcaseCardData cardData)
        {
            if (frontRenderer == null || cardData == null) return;

            var frontTransform = frontRenderer.transform;
            var existingCanvas = frontTransform.Find("FrontDynamicCanvas");
            if (existingCanvas == null)
            {
                var canvasGo = new GameObject("FrontDynamicCanvas", typeof(RectTransform));
                canvasGo.transform.SetParent(frontTransform, false);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 1;

                frontCanvasRect = canvasGo.GetComponent<RectTransform>();
            }
            else
            {
                frontCanvasRect = existingCanvas.GetComponent<RectTransform>();
            }

            if (frontCanvasRect != null)
            {
                frontCanvasRect.localPosition = new Vector3(0f, 0f, -0.002f);
                frontCanvasRect.localRotation = Quaternion.identity;
                frontCanvasRect.sizeDelta = new Vector2(DynamicCardAssembler.ReferenceWidth, DynamicCardAssembler.ReferenceHeight);
                frontCanvasRect.localScale = new Vector3(
                    1f / DynamicCardAssembler.ReferenceWidth,
                    1f / DynamicCardAssembler.ReferenceHeight,
                    1f
                );

                var clanData = CardShowcaseCatalog.GetClanById(cardData.Clan);
                DynamicCardAssembler.AssembleCard(
                    cardData,
                    clanData,
                    frontCanvasRect,
                    DynamicCardAssembler.ReferenceWidth,
                    DynamicCardAssembler.ReferenceHeight,
                    isMain3DCard: true
                );
            }
        }

        public void SetReverso(string reversoId)
        {
            var rev = CardShowcaseCatalog.GetReversoById(reversoId);
            CurrentReversoData = rev;
            currentReversoId = rev.Id;

            UpdateBackRenderer();
        }

        public void SetFoilLevel(int level)
        {
            currentFoilLevel = Mathf.Clamp(level, 1, 4);
            if (frontRenderer != null)
            {
                InitPropertyBlocks();
                frontRenderer.GetPropertyBlock(frontPropBlock);
                ApplyFoilToPropertyBlock(frontPropBlock, currentFoilLevel);
                frontRenderer.SetPropertyBlock(frontPropBlock);
            }
            UpdateEdgeFoil();
        }

        private void UpdateBackRenderer()
        {
            if (CurrentReversoData == null || backRenderer == null) return;
            EnsureHierarchy();
            InitPropertyBlocks();

            var backTex = LoadTextureFromCardsFolder(CurrentReversoData.File);
            if (backTex != null)
            {
                var mat = Application.isPlaying ? backRenderer.material : backRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_MainTex")) mat.mainTexture = backTex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", backTex);
                    if (mat.HasProperty("_Color")) mat.color = Color.white;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                }

                backRenderer.GetPropertyBlock(backPropBlock);
                backPropBlock.SetTexture("_BaseMap", backTex);
                backPropBlock.SetTexture("_MainTex", backTex);
                backPropBlock.SetColor("_Color", Color.white);
                backPropBlock.SetColor("_BaseColor", Color.white);
                backPropBlock.SetFloat("_Metallic", 0.15f);
                backPropBlock.SetFloat("_Glossiness", 0.5f);
                backPropBlock.SetFloat("_Smoothness", 0.5f);
                backRenderer.SetPropertyBlock(backPropBlock);
            }
        }

        private void UpdateEdgeFoil()
        {
            if (edgeRenderer == null) return;
            InitPropertyBlocks();
            edgeRenderer.GetPropertyBlock(edgePropBlock);

            Color edgeColor;
            float metallic = 0.1f;
            float smoothness = 0.3f;

            switch (currentFoilLevel)
            {
                case 2: // Silver Foil
                    edgeColor = new Color(0.75f, 0.78f, 0.82f);
                    metallic = 0.7f;
                    smoothness = 0.75f;
                    break;
                case 3: // Gold Foil
                    edgeColor = new Color(0.92f, 0.76f, 0.25f);
                    metallic = 0.9f;
                    smoothness = 0.85f;
                    break;
                case 4: // Rainbow Cosmic
                    edgeColor = new Color(0.85f, 0.65f, 0.95f);
                    metallic = 0.85f;
                    smoothness = 0.92f;
                    break;
                case 1:
                default: // Matte black core
                    edgeColor = new Color(0.12f, 0.12f, 0.14f);
                    metallic = 0.05f;
                    smoothness = 0.2f;
                    break;
            }

            var mat = Application.isPlaying ? edgeRenderer.material : edgeRenderer.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty("_Color")) mat.color = edgeColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", edgeColor);
            }

            edgePropBlock.SetColor("_BaseColor", edgeColor);
            edgePropBlock.SetColor("_Color", edgeColor);
            edgePropBlock.SetFloat("_Metallic", metallic);
            edgePropBlock.SetFloat("_Glossiness", smoothness);
            edgePropBlock.SetFloat("_Smoothness", smoothness);
            edgeRenderer.SetPropertyBlock(edgePropBlock);
        }

        private void ApplyFoilToPropertyBlock(MaterialPropertyBlock block, int level)
        {
            switch (level)
            {
                case 2: // Silver Foil Sheen
                    block.SetFloat("_Metallic", 0.65f);
                    block.SetFloat("_Glossiness", 0.75f);
                    block.SetFloat("_Smoothness", 0.75f);
                    block.SetColor("_BaseColor", Color.white);
                    block.SetColor("_Color", Color.white);
                    break;
                case 3: // Gold Foil Sheen
                    block.SetFloat("_Metallic", 0.90f);
                    block.SetFloat("_Glossiness", 0.88f);
                    block.SetFloat("_Smoothness", 0.88f);
                    block.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.90f));
                    block.SetColor("_Color", new Color(1.0f, 0.98f, 0.90f));
                    break;
                case 4: // Cosmic Prism Foil
                    block.SetFloat("_Metallic", 0.95f);
                    block.SetFloat("_Glossiness", 0.96f);
                    block.SetFloat("_Smoothness", 0.96f);
                    block.SetColor("_BaseColor", new Color(1.0f, 0.95f, 1.0f));
                    block.SetColor("_Color", new Color(1.0f, 0.95f, 1.0f));
                    break;
                case 1:
                default: // Standard Matte Card
                    block.SetFloat("_Metallic", 0.02f);
                    block.SetFloat("_Glossiness", 0.22f);
                    block.SetFloat("_Smoothness", 0.22f);
                    block.SetColor("_BaseColor", Color.white);
                    block.SetColor("_Color", Color.white);
                    break;
            }
        }

        private static List<string> GetResourceCandidates(string relativePath)
        {
            var candidates = new List<string>();
            if (string.IsNullOrWhiteSpace(relativePath)) return candidates;

            string clean = relativePath.Replace('\\', '/').Trim();
            if (clean.StartsWith("/")) clean = clean.Substring(1);

            int dotIdx = clean.LastIndexOf('.');
            string pathNoExt = dotIdx > 0 ? clean.Substring(0, dotIdx) : clean;

            string tarjetasRel = CardArtMapper.ConvertLegacyPathToTarjetasPath(relativePath);
            string tarjetasNoExt = string.Empty;
            if (!string.IsNullOrWhiteSpace(tarjetasRel))
            {
                string tClean = tarjetasRel.Replace('\\', '/').Trim();
                if (tClean.StartsWith("/")) tClean = tClean.Substring(1);
                int tDot = tClean.LastIndexOf('.');
                tarjetasNoExt = tDot > 0 ? tClean.Substring(0, tDot) : tClean;
            }

            var basePaths = new List<string>();
            if (!string.IsNullOrEmpty(tarjetasNoExt))
            {
                basePaths.Add(tarjetasNoExt);
            }
            basePaths.Add(pathNoExt);

            var rawCandidates = new List<string>();
            foreach (var bp in basePaths)
            {
                if (bp.StartsWith("Tarjetas/", StringComparison.OrdinalIgnoreCase))
                {
                    rawCandidates.Add(bp);
                    rawCandidates.Add(bp.Substring("Tarjetas/".Length));
                    rawCandidates.Add("Cards/" + bp);
                }
                else
                {
                    rawCandidates.Add(bp);
                    rawCandidates.Add("Tarjetas/" + bp);
                    rawCandidates.Add("Cards/" + bp);
                }
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in rawCandidates)
            {
                string[] forms = {
                    raw,
                    raw.Normalize(NormalizationForm.FormC),
                    raw.Normalize(NormalizationForm.FormD),
                    CardArtMapper.RemoveDiacritics(raw)
                };

                foreach (var f in forms)
                {
                    if (seen.Add(f))
                    {
                        candidates.Add(f);
                    }
                }
            }

            return candidates;
        }

        public static Texture2D LoadTextureFromCardsFolder(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (TextureCache.TryGetValue(relativePath, out var cached) && cached != null)
            {
                return cached;
            }

            string tarjetasRel = CardArtMapper.ConvertLegacyPathToTarjetasPath(relativePath);

            // 1. Primary: Unity Resources (Universal across Android, iOS, Standalone, Editor)
            var resourceCandidates = GetResourceCandidates(relativePath);
            foreach (var resPath in resourceCandidates)
            {
                var resTex = Resources.Load<Texture2D>(resPath);
                if (resTex != null)
                {
                    TextureCache[relativePath] = resTex;
                    return resTex;
                }
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            var priorityEditorPaths = new List<string>();
            foreach (var resPath in resourceCandidates)
            {
                priorityEditorPaths.Add("Assets/Resources/" + resPath + ".png");
                priorityEditorPaths.Add("Assets/Resources/" + resPath + ".jpg");
                priorityEditorPaths.Add("Assets/_Project/Art/" + resPath + ".png");
                priorityEditorPaths.Add("Assets/_Project/Art/" + resPath + ".jpg");
            }
            if (!string.IsNullOrEmpty(tarjetasRel))
            {
                priorityEditorPaths.Add("Assets/Resources/" + tarjetasRel);
                priorityEditorPaths.Add("Assets/_Project/Art/" + tarjetasRel);
            }
            priorityEditorPaths.Add("Assets/Resources/" + relativePath);
            priorityEditorPaths.Add("Assets/_Project/Art/" + relativePath);

            foreach (var rawPath in priorityEditorPaths)
            {
                string[] normForms = {
                    rawPath.Normalize(NormalizationForm.FormC),
                    rawPath.Normalize(NormalizationForm.FormD)
                };
                foreach (var assetPath in normForms)
                {
                    var assetTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (assetTex != null)
                    {
                        TextureCache[relativePath] = assetTex;
                        return assetTex;
                    }
                }
            }
#endif

            // 3. Direct file read fallback (ONLY on standalone desktop where files may reside on disk)
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                var priorityDiskFiles = new List<string>();
                if (!string.IsNullOrEmpty(tarjetasRel))
                {
                    priorityDiskFiles.Add(Path.Combine(Application.dataPath, "Resources", tarjetasRel));
                    priorityDiskFiles.Add(Path.Combine(Application.dataPath, "_Project/Art", tarjetasRel));
                }
                priorityDiskFiles.Add(Path.Combine(Application.dataPath, "Resources", relativePath));
                priorityDiskFiles.Add(Path.Combine(Application.dataPath, "_Project/Art", relativePath));

                foreach (var rawFullPath in priorityDiskFiles)
                {
                    string[] normForms = {
                        rawFullPath.Normalize(NormalizationForm.FormC),
                        rawFullPath.Normalize(NormalizationForm.FormD)
                    };
                    foreach (var fullPath in normForms)
                    {
                        if (File.Exists(fullPath))
                        {
                            byte[] bytes = File.ReadAllBytes(fullPath);
                            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                            if (tex.LoadImage(bytes))
                            {
                                tex.name = Path.GetFileNameWithoutExtension(relativePath);
                                tex.filterMode = FilterMode.Trilinear;
                                tex.anisoLevel = 4;
                                TextureCache[relativePath] = tex;
                                return tex;
                            }
                        }
                    }
                }
            }

            Debug.LogWarning($"[Card3D] Could not find texture at: {relativePath}");
            return null;
        }

        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads or creates a cached UI Sprite for a card, Tarjetas UI icon, or reverso asset.
        /// Works universally on Android, iOS, Standalone, and in the Unity Editor.
        /// </summary>
        public static Sprite LoadSpriteFromCardsFolder(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (SpriteCache.TryGetValue(relativePath, out var cached) && cached != null)
            {
                return cached;
            }

            string tarjetasRel = CardArtMapper.ConvertLegacyPathToTarjetasPath(relativePath);

            // 1. Primary: Unity Resources (Universal across Android, iOS, Standalone, Editor)
            var resourceCandidates = GetResourceCandidates(relativePath);
            foreach (var resPath in resourceCandidates)
            {
                // 1a. Try single Sprite directly
                var resSprite = Resources.Load<Sprite>(resPath);
                if (resSprite != null)
                {
                    SpriteCache[relativePath] = resSprite;
                    return resSprite;
                }

                // 1b. Try Multiple Sprite sheet (BG_Milenials, costo, pleca, espada2, etc.)
                var allSprites = Resources.LoadAll<Sprite>(resPath);
                if (allSprites != null && allSprites.Length > 0 && allSprites[0] != null)
                {
                    SpriteCache[relativePath] = allSprites[0];
                    return allSprites[0];
                }

                // 1c. Try Texture2D and create Sprite (Heroes, Bestias, Trompos, Arenas)
                var resTex = Resources.Load<Texture2D>(resPath);
                if (resTex != null)
                {
                    var created = Sprite.Create(
                        resTex,
                        new Rect(0, 0, resTex.width, resTex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    created.name = resTex.name + "_Sprite";
                    SpriteCache[relativePath] = created;
                    return created;
                }
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            var priorityEditorPaths = new List<string>();
            foreach (var resPath in resourceCandidates)
            {
                priorityEditorPaths.Add("Assets/Resources/" + resPath + ".png");
                priorityEditorPaths.Add("Assets/Resources/" + resPath + ".jpg");
                priorityEditorPaths.Add("Assets/_Project/Art/" + resPath + ".png");
                priorityEditorPaths.Add("Assets/_Project/Art/" + resPath + ".jpg");
            }
            if (!string.IsNullOrEmpty(tarjetasRel))
            {
                priorityEditorPaths.Add("Assets/Resources/" + tarjetasRel);
                priorityEditorPaths.Add("Assets/_Project/Art/" + tarjetasRel);
            }
            priorityEditorPaths.Add("Assets/Resources/" + relativePath);
            priorityEditorPaths.Add("Assets/_Project/Art/" + relativePath);

            foreach (var rawPath in priorityEditorPaths)
            {
                string[] normForms = {
                    rawPath.Normalize(NormalizationForm.FormC),
                    rawPath.Normalize(NormalizationForm.FormD)
                };
                foreach (var assetPath in normForms)
                {
                    var assetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (assetSprite == null)
                    {
                        var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        if (allAssets != null)
                        {
                            assetSprite = allAssets.OfType<Sprite>().FirstOrDefault();
                        }
                    }

                    if (assetSprite != null)
                    {
                        SpriteCache[relativePath] = assetSprite;
                        return assetSprite;
                    }

                    var assetTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (assetTex != null)
                    {
                        var created = Sprite.Create(
                            assetTex,
                            new Rect(0, 0, assetTex.width, assetTex.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );
                        created.name = assetTex.name + "_Sprite";
                        SpriteCache[relativePath] = created;
                        return created;
                    }
                }
            }
#endif

            // 3. Fallback: Load Texture2D from disk / legacy and convert to Sprite
            var fallbackTex = LoadTextureFromCardsFolder(relativePath);
            if (fallbackTex != null)
            {
                var sprite = Sprite.Create(
                    fallbackTex,
                    new Rect(0, 0, fallbackTex.width, fallbackTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = fallbackTex.name + "_Sprite";
                SpriteCache[relativePath] = sprite;
                return sprite;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the flat illustration Sprite for any CardDefinition from Assets/_Project/Art/Tarjetas/.
        /// </summary>
        public static Sprite GetFlatArtSprite(CardDefinition definition)
        {
            if (definition == null) return null;
            string flatPath = CardArtMapper.GetFlatArtPath(definition);
            if (!string.IsNullOrEmpty(flatPath))
            {
                var sprite = LoadSpriteFromCardsFolder(flatPath);
                if (sprite != null) return sprite;
            }
            if (!string.IsNullOrEmpty(definition.ImagePath))
            {
                var sprite = LoadSpriteFromCardsFolder(definition.ImagePath);
                if (sprite != null) return sprite;
            }
            return GetReversoThumbnail("oro");
        }

        /// <summary>
        /// Retrieves the cached high-resolution 2D Sprite thumbnail for any CardDefinition.
        /// Dynamically renders the complete card (flat art + UI badges + texts) or falls back to flat art / reverso.
        /// </summary>
        public static Sprite GetCardThumbnail(CardDefinition definition)
        {
            if (definition == null) return null;

            var showcaseData = DynamicCardAssembler.ResolveShowcaseCardData(definition);
            if (showcaseData != null)
            {
                var dynamicSprite = DynamicCardAssembler.RenderCardToSprite(showcaseData);
                if (dynamicSprite != null) return dynamicSprite;

                string flatPath = CardArtMapper.GetFlatArtPath(showcaseData);
                if (!string.IsNullOrEmpty(flatPath))
                {
                    var sprite = LoadSpriteFromCardsFolder(flatPath);
                    if (sprite != null) return sprite;
                }
            }

            if (!string.IsNullOrEmpty(definition.ImagePath))
            {
                var sprite = LoadSpriteFromCardsFolder(definition.ImagePath);
                if (sprite != null) return sprite;
            }

            return GetReversoThumbnail("oro");
        }

        /// <summary>
        /// Retrieves the cached 2D Sprite for an official card back (reverso).
        /// </summary>
        public static Sprite GetReversoThumbnail(string reversoId = "oro")
        {
            var rev = CardShowcaseCatalog.GetReversoById(reversoId);
            if (rev == null) return null;
            return LoadSpriteFromCardsFolder(rev.File);
        }

        /// <summary>
        /// Retrieves the cached official padlock Sprite (from Assets/Resources/Characters/locked_52x52.png).
        /// Works universally on Android, iOS, Standalone, and in the Unity Editor.
        /// </summary>
        public static Sprite GetLockIconSprite()
        {
            if (SpriteCache.TryGetValue("LockIcon", out var cached) && cached != null)
            {
                return cached;
            }

            // 1. Primary: Resources
            string[] resNames = { "Characters/locked_52x52", "Characters/locked" };
            foreach (var rn in resNames)
            {
                var resSprite = Resources.Load<Sprite>(rn);
                if (resSprite != null)
                {
                    SpriteCache["LockIcon"] = resSprite;
                    return resSprite;
                }
                var resTex = Resources.Load<Texture2D>(rn);
                if (resTex != null)
                {
                    var s = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                    s.name = "LockIcon_Sprite";
                    SpriteCache["LockIcon"] = s;
                    return s;
                }
            }

            string[] candidateFiles = {
                "Characters/locked_52x52.png",
                "Characters/locked.png"
            };

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            foreach (var relFile in candidateFiles)
            {
                string[] editorPaths = {
                    "Assets/Resources/" + relFile,
                    "Assets/_Project/Art/" + relFile
                };
                foreach (var assetPath in editorPaths)
                {
                    var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (editorSprite != null)
                    {
                        SpriteCache["LockIcon"] = editorSprite;
                        return editorSprite;
                    }
                    var editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (editorTex != null)
                    {
                        var s = Sprite.Create(editorTex, new Rect(0, 0, editorTex.width, editorTex.height), new Vector2(0.5f, 0.5f), 100f);
                        s.name = "LockIcon_Sprite";
                        SpriteCache["LockIcon"] = s;
                        return s;
                    }
                }
            }
#endif

            // 3. File fallback
            foreach (var relFile in candidateFiles)
            {
                string[] diskPaths = {
                    Path.Combine(Application.dataPath, "Resources", relFile),
                    Path.Combine(Application.dataPath, "_Project/Art", relFile)
                };
                foreach (var fullPath in diskPaths)
                {
                    if (File.Exists(fullPath))
                    {
                        byte[] bytes = File.ReadAllBytes(fullPath);
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                        if (tex.LoadImage(bytes))
                        {
                            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                            s.name = "LockIcon_Sprite";
                            SpriteCache["LockIcon"] = s;
                            return s;
                        }
                    }
                }
            }

            return null;
        }
    }
}
