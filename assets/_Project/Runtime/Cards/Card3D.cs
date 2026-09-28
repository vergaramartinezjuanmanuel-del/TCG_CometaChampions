using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

        private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.OrdinalIgnoreCase);

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
                       ?? Shader.Find("Legacy Shaders/Diffuse");
            }

            return Shader.Find("Standard")
                   ?? Shader.Find("Unlit/Texture")
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
            var showcaseData = CardShowcaseCatalog.GetCardByDefinition(definition);
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

            // Load & assign front texture
            var frontTex = LoadTextureFromCardsFolder(cardData.Image);
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

        private static string GetResourceCardPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return string.Empty;
            string path = relativePath.Replace('\\', '/');
            if (path.StartsWith("/")) path = path.Substring(1);

            int lastDot = path.LastIndexOf('.');
            if (lastDot > 0)
            {
                path = path.Substring(0, lastDot);
            }

            return "Cards/" + path;
        }

        public static Texture2D LoadTextureFromCardsFolder(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (TextureCache.TryGetValue(relativePath, out var cached) && cached != null)
            {
                return cached;
            }

            // 1. Primary runtime path (Android, iOS, Standalone, Editor): Unity Resources
            string resPath = GetResourceCardPath(relativePath);
            var resTex = Resources.Load<Texture2D>(resPath);
            if (resTex != null)
            {
                TextureCache[relativePath] = resTex;
                return resTex;
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            string[] editorPaths = {
                "Assets/Resources/Cards/" + relativePath,
                "Assets/_Project/Art/Cards/" + relativePath,
                "Assets/_Project/Resources/Cards/" + relativePath
            };

            foreach (var assetPath in editorPaths)
            {
                var assetTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (assetTex != null)
                {
                    TextureCache[relativePath] = assetTex;
                    return assetTex;
                }
            }
#endif

            // 3. Fallback: Direct file read (for standalone PC/Mac if files are present on disk)
            string[] candidateFiles = {
                Path.Combine(Application.dataPath, "Resources/Cards", relativePath),
                Path.Combine(Application.dataPath, "_Project/Art/Cards", relativePath),
                Path.Combine(Application.dataPath, "_Project/Resources/Cards", relativePath)
            };

            foreach (var fullPath in candidateFiles)
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

            Debug.LogWarning($"[Card3D] Could not find texture at: {relativePath}");
            return null;
        }

        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads or creates a cached UI Sprite for a card or reverso asset.
        /// Works universally on Android, iOS, Standalone, and in the Unity Editor.
        /// </summary>
        public static Sprite LoadSpriteFromCardsFolder(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (SpriteCache.TryGetValue(relativePath, out var cached) && cached != null)
            {
                return cached;
            }

            // 1. Try directly loading as Sprite from Resources
            string resPath = GetResourceCardPath(relativePath);
            var resSprite = Resources.Load<Sprite>(resPath);
            if (resSprite != null)
            {
                SpriteCache[relativePath] = resSprite;
                return resSprite;
            }

#if UNITY_EDITOR
            // 2. Try loading as Sprite from AssetDatabase (Editor)
            string[] editorPaths = {
                "Assets/Resources/Cards/" + relativePath,
                "Assets/_Project/Art/Cards/" + relativePath
            };

            foreach (var assetPath in editorPaths)
            {
                var assetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (assetSprite != null)
                {
                    SpriteCache[relativePath] = assetSprite;
                    return assetSprite;
                }
            }
#endif

            // 3. Fallback: Load Texture2D (via Resources, AssetDatabase or File) and create Sprite dynamically
            var tex = LoadTextureFromCardsFolder(relativePath);
            if (tex != null)
            {
                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = tex.name + "_Sprite";
                SpriteCache[relativePath] = sprite;
                return sprite;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the cached high-resolution 2D Sprite thumbnail for any CardDefinition.
        /// Guaranteed to return a valid Sprite (falls back to official card back if not found).
        /// </summary>
        public static Sprite GetCardThumbnail(CardDefinition definition)
        {
            if (definition == null) return null;

            // 1. Direct explicit ImagePath from CardDefinition (MVP card pool)
            if (!string.IsNullOrEmpty(definition.ImagePath))
            {
                var sprite = LoadSpriteFromCardsFolder(definition.ImagePath);
                if (sprite != null) return sprite;
            }

            // 2. Showcase catalog lookup
            var showcaseData = CardShowcaseCatalog.GetCardByDefinition(definition);
            if (showcaseData != null && !string.IsNullOrEmpty(showcaseData.Image))
            {
                var sprite = LoadSpriteFromCardsFolder(showcaseData.Image);
                if (sprite != null) return sprite;
            }

            // 3. Safe fallback to golden card back (never returns null to prevent blank white boxes)
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
