using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Helpers;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Cards
{
    /// <summary>
    /// Automatically scales the 750x1050 reference card layout to fit any parent RectTransform.
    /// </summary>
    [ExecuteAlways]
    public class DynamicCardAutoScaler : MonoBehaviour
    {
        [SerializeField] private RectTransform referenceRoot;

        public void Initialize(RectTransform refRoot)
        {
            referenceRoot = refRoot;
            UpdateScale();
        }

        private void OnEnable()
        {
            UpdateScale();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateScale();
        }

        private void LateUpdate()
        {
            UpdateScale();
        }

        public void UpdateScale()
        {
            if (referenceRoot == null) return;
            var parentRect = transform as RectTransform;
            if (parentRect == null) return;

            Rect r = parentRect.rect;
            float w = r.width;
            float h = r.height;

            if (w <= 1f || h <= 1f)
            {
                w = parentRect.sizeDelta.x;
                h = parentRect.sizeDelta.y;
            }

            if (w <= 1f || h <= 1f) return;

            float sx = w / DynamicCardAssembler.ReferenceWidth;
            float sy = h / DynamicCardAssembler.ReferenceHeight;
            Vector3 targetScale = new Vector3(sx, sy, 1f);
            if ((referenceRoot.localScale - targetScale).sqrMagnitude > 0.000001f)
            {
                referenceRoot.localScale = targetScale;
            }
        }
    }

    /// <summary>
    /// Dynamically assembles any card at runtime using the flat character/arena illustrations
    /// and UI layers from Assets/_Project/Art/Tarjetas/, matching the official Avispón reference layout.
    /// </summary>
    public static class DynamicCardAssembler
    {
        public const float ReferenceWidth = 750f;
        public const float ReferenceHeight = 1050f;

        private const string FontAssetEditorPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset";
        private const string FontFileEditorPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf";

        private static TMP_FontAsset cachedCardFont;
        private static Sprite cachedCircleSprite;
        private static Sprite cachedShieldSprite;

        private static readonly Dictionary<string, Texture2D> RenderedCardTextures = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> RenderedCardSprites = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads or dynamically creates the TMP_FontAsset for "SemiExpanded ExtraBold Italic".
        /// </summary>
        public static TMP_FontAsset GetCardFont()
        {
            if (cachedCardFont != null) return cachedCardFont;

#if UNITY_EDITOR
            cachedCardFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetEditorPath);
            if (cachedCardFont != null) return cachedCardFont;

            var editorSourceFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(FontFileEditorPath);
            if (editorSourceFont != null)
            {
                cachedCardFont = TMP_FontAsset.CreateFontAsset(
                    editorSourceFont,
                    48,
                    5,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true
                );
                if (cachedCardFont != null)
                {
                    cachedCardFont.name = "SemiExpanded ExtraBold Italic SDF (Runtime)";
                    return cachedCardFont;
                }
            }
#endif

            var resFontAsset = Resources.Load<TMP_FontAsset>("Fonts/SemiExpanded ExtraBold Italic SDF")
                               ?? Resources.Load<TMP_FontAsset>("SemiExpanded ExtraBold Italic SDF");
            if (resFontAsset != null)
            {
                cachedCardFont = resFontAsset;
                return cachedCardFont;
            }

            string diskFontPath = Path.Combine(Application.dataPath, "_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf");
            if (File.Exists(diskFontPath))
            {
                var diskFont = new Font(diskFontPath);
                cachedCardFont = TMP_FontAsset.CreateFontAsset(diskFont);
                if (cachedCardFont != null) return cachedCardFont;
            }

            return TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// Converts a CardDefinition into ShowcaseCardData (preserving live runtime stats if needed).
        /// </summary>
        public static ShowcaseCardData ResolveShowcaseCardData(CardDefinition definition)
        {
            if (definition == null) return null;

            var catalogEntry = CardShowcaseCatalog.GetCardByDefinition(definition);
            if (catalogEntry != null)
            {
                return catalogEntry;
            }

            string categoryKey = definition.Category switch
            {
                CardCategory.Champion => "champions",
                CardCategory.KombatBeast => "kombat",
                CardCategory.PhysicalTrompo => "trompos",
                CardCategory.Arena => "arenas",
                _ => "champions"
            };

            return new ShowcaseCardData
            {
                Id = definition.Id,
                Name = definition.Name,
                Category = categoryKey,
                CategoryName = CardArtMapper.GetCategoryDisplayName(definition.Category),
                Clan = definition.Clan.ToString().ToLowerInvariant(),
                Cost = definition.EnergyCost,
                Attack = definition.BaseAttack,
                Defense = definition.BaseHealth,
                Image = definition.ImagePath,
                Rarity = definition.Rarity.ToString(),
                Description = !string.IsNullOrWhiteSpace(definition.AbilityText) ? definition.AbilityText : definition.LoreText,
                Quote = !string.IsNullOrWhiteSpace(definition.IconicQuote) ? definition.IconicQuote : definition.LoreText,
                Effect = definition.AbilityText,
                Lore = definition.LoreText
            };
        }

        /// <summary>
        /// Assembles the complete dynamic card UI inside the given parent RectTransform from a CardDefinition.
        /// </summary>
        public static GameObject AssembleCard(
            CardDefinition definition,
            RectTransform parent,
            float cardWidth = 0f,
            float cardHeight = 0f)
        {
            if (definition == null || parent == null) return null;
            var cardData = ResolveShowcaseCardData(definition);
            var clanData = CardShowcaseCatalog.GetClanById(cardData.Clan);
            return AssembleCard(cardData, clanData, parent, cardWidth, cardHeight);
        }

        /// <summary>
        /// Main entry point: Dynamically builds all card layers matching the Avispón reference card.
        /// </summary>
        public static GameObject AssembleCard(
            ShowcaseCardData cardData,
            ShowcaseClanData clanData,
            RectTransform parent,
            float cardWidth = 0f,
            float cardHeight = 0f)
        {
            if (cardData == null || parent == null) return null;

            // Remove any prior DynamicCard child in this parent to avoid duplicates on refresh
            var existing = parent.Find("DynamicCard");
            if (existing != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(existing.gameObject);
                else UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            if (clanData == null)
            {
                clanData = CardShowcaseCatalog.GetClanById(cardData.Clan);
            }

            ClanType clanType = CardArtMapper.ParseClanType(cardData.Clan);
            Color clanColor = GetClanColor(clanType);
            TMP_FontAsset cardFont = GetCardFont();

            // Root container stretches to fill parent
            var rootGo = new GameObject("DynamicCard", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;

            // Reference 750x1050 coordinate space container
            var refGo = new GameObject("ReferenceCanvas_750x1050", typeof(RectTransform));
            refGo.transform.SetParent(rootRt, false);
            var refRt = refGo.GetComponent<RectTransform>();
            refRt.anchorMin = new Vector2(0.5f, 0.5f);
            refRt.anchorMax = new Vector2(0.5f, 0.5f);
            refRt.pivot = new Vector2(0.5f, 0.5f);
            refRt.anchoredPosition = Vector2.zero;
            refRt.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);

            float targetW = cardWidth > 1f ? cardWidth : (parent.rect.width > 1f ? parent.rect.width : parent.sizeDelta.x);
            float targetH = cardHeight > 1f ? cardHeight : (parent.rect.height > 1f ? parent.rect.height : parent.sizeDelta.y);
            if (targetW <= 1f) targetW = ReferenceWidth;
            if (targetH <= 1f) targetH = ReferenceHeight;
            refRt.localScale = new Vector3(targetW / ReferenceWidth, targetH / ReferenceHeight, 1f);

            var scaler = rootGo.AddComponent<DynamicCardAutoScaler>();
            scaler.Initialize(refRt);

            // 1. Flat Card Artwork (Full 750x1050 background)
            var artGo = CreateChild("CardArt", refRt);
            SetRectFromTopLeft(artGo.GetComponent<RectTransform>(), 0f, 0f, ReferenceWidth, ReferenceHeight);
            var artImg = artGo.AddComponent<Image>();
            string flatArtPath = CardArtMapper.GetFlatArtPath(cardData);
            var artSprite = Card3D.LoadSpriteFromCardsFolder(flatArtPath)
                            ?? Card3D.LoadSpriteFromCardsFolder(cardData.Image)
                            ?? Card3D.GetReversoThumbnail("oro");
            artImg.sprite = artSprite;
            artImg.color = Color.white;
            artImg.preserveAspect = false;
            artImg.raycastTarget = false;

            // 2. Top-Left Energy Cost Badge (X: 12..142, Y_top: 13..143)
            var costBadgeGo = CreateChild("CostBadge", refRt);
            SetRectFromTopLeft(costBadgeGo.GetComponent<RectTransform>(), 12f, 13f, 142f, 143f);

            var costBackGo = CreateChild("CostBacking", costBadgeGo.transform);
            SetStretch(costBackGo.GetComponent<RectTransform>(), 14f);
            var costBackImg = costBackGo.AddComponent<Image>();
            costBackImg.sprite = GetCircleSprite();
            costBackImg.color = new Color(0.03f, 0.05f, 0.08f, 0.78f);
            costBackImg.raycastTarget = false;

            var costIconGo = CreateChild("CostIcon", costBadgeGo.transform);
            SetStretch(costIconGo.GetComponent<RectTransform>(), 0f);
            var costIconImg = costIconGo.AddComponent<Image>();
            costIconImg.sprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetCostIconPath());
            costIconImg.color = Color.white;
            costIconImg.preserveAspect = true;
            costIconImg.raycastTarget = false;

            var costText = CreateCardTMP("CostText", costBadgeGo.transform, cardData.Cost.ToString(), cardFont, 64f, Color.white, TextAlignmentOptions.Center);
            SetStretch(costText.rectTransform, 0f);

            // 3. Top-Right Clan Banner (X: 563..715, Y_top: 0..147)
            var clanBannerGo = CreateChild("ClanBanner", refRt);
            SetRectFromTopLeft(clanBannerGo.GetComponent<RectTransform>(), 563f, 0f, 715f, 147f);
            var clanBannerImg = clanBannerGo.AddComponent<Image>();
            clanBannerImg.sprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetClanBannerPath(clanType));
            clanBannerImg.color = Color.white;
            clanBannerImg.preserveAspect = true;
            clanBannerImg.raycastTarget = false;

            // 4. Clan Lower Frame & NameBar (Full 750x1050 BG_<Clan>.png overlay)
            var nameBarGo = CreateChild("NameBar", refRt);
            SetRectFromTopLeft(nameBarGo.GetComponent<RectTransform>(), 0f, 0f, ReferenceWidth, ReferenceHeight);
            var nameBarImg = nameBarGo.AddComponent<Image>();
            nameBarImg.sprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetClanBgPath(clanType));
            nameBarImg.color = Color.white;
            nameBarImg.preserveAspect = false;
            nameBarImg.raycastTarget = false;

            // Card Name on Left of Clan Band (X: 33..505, Y_top: 650..734)
            string displayName = CardArtMapper.GetCardDisplayName(cardData.Name);
            var nameText = CreateCardTMP("NameText", nameBarGo.transform, displayName, cardFont, 68f, Color.white, TextAlignmentOptions.MidlineLeft);
            SetRectFromTopLeft(nameText.rectTransform, 33f, 650f, 505f, 734f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 32f;
            nameText.fontSizeMax = 68f;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Attack & Defense Badges on Right of Clan Band
            var statsGroupGo = CreateChild("StatsGroup", nameBarGo.transform);
            SetRectFromTopLeft(statsGroupGo.GetComponent<RectTransform>(), 520f, 632f, 736f, 750f);

            bool showStats = cardData.Attack > 0 || cardData.Defense > 0 ||
                             cardData.Category.Equals("champions", StringComparison.OrdinalIgnoreCase) ||
                             cardData.Category.Equals("kombat", StringComparison.OrdinalIgnoreCase);
            statsGroupGo.SetActive(showStats);

            if (showStats)
            {
                // Attack Badge (Left stat badge: X: 528..630, Y_top: 638..740)
                var atkBadgeGo = CreateChild("AtkBadge", nameBarGo.transform);
                SetRectFromTopLeft(atkBadgeGo.GetComponent<RectTransform>(), 528f, 638f, 630f, 740f);
                var atkOuterImg = atkBadgeGo.AddComponent<Image>();
                atkOuterImg.sprite = GetCircleSprite();
                atkOuterImg.color = Color.white;
                atkOuterImg.raycastTarget = false;

                var atkInnerGo = CreateChild("AtkInner", atkBadgeGo.transform);
                SetStretch(atkInnerGo.GetComponent<RectTransform>(), 8f);
                var atkInnerImg = atkInnerGo.AddComponent<Image>();
                atkInnerImg.sprite = GetCircleSprite();
                atkInnerImg.color = clanColor;
                atkInnerImg.raycastTarget = false;

                var atkText = CreateCardTMP("AtkText", atkBadgeGo.transform, cardData.Attack.ToString(), cardFont, 56f, new Color(0.05f, 0.05f, 0.05f, 1f), TextAlignmentOptions.Center);
                SetStretch(atkText.rectTransform, 4f);

                // Defense Badge (Right stat badge: X: 638..730, Y_top: 654..746)
                var defBadgeGo = CreateChild("DefBadge", nameBarGo.transform);
                SetRectFromTopLeft(defBadgeGo.GetComponent<RectTransform>(), 638f, 654f, 730f, 746f);
                var defOuterImg = defBadgeGo.AddComponent<Image>();
                defOuterImg.sprite = GetShieldSprite();
                defOuterImg.color = clanColor;
                defOuterImg.raycastTarget = false;

                var defInnerGo = CreateChild("DefInner", defBadgeGo.transform);
                SetStretch(defInnerGo.GetComponent<RectTransform>(), 6f);
                var defInnerImg = defInnerGo.AddComponent<Image>();
                defInnerImg.sprite = GetShieldSprite();
                defInnerImg.color = new Color(0.68f, 0.68f, 0.68f, 0.90f);
                defInnerImg.raycastTarget = false;

                var defText = CreateCardTMP("DefText", defBadgeGo.transform, cardData.Defense.ToString(), cardFont, 50f, Color.white, TextAlignmentOptions.Center);
                SetStretch(defText.rectTransform, 4f);
            }

            // 5. Category Label Centered in Dark Strip (X: 50..700, Y_top: 750..792)
            string categoryDisplay = CardArtMapper.GetCategoryDisplayName(cardData.Category);
            var categoryText = CreateCardTMP("CategoryText", refRt, categoryDisplay, cardFont, 30f, Color.white, TextAlignmentOptions.Center);
            SetRectFromTopLeft(categoryText.rectTransform, 50f, 750f, 700f, 792f);

            // 6. Description Row (Y_top: 822..884)
            var descRowGo = CreateChild("DescriptionRow", refRt);
            SetRectFromTopLeft(descRowGo.GetComponent<RectTransform>(), 0f, 822f, ReferenceWidth, 884f);

            var descPlecaGo = CreateChild("Pleca", descRowGo.transform);
            SetRectFromTopLeft(descPlecaGo.transform.parent == refRt ? descPlecaGo.GetComponent<RectTransform>() : descPlecaGo.GetComponent<RectTransform>(), 0f, 829f, 193f, 877f);
            // Adjust relative to DescriptionRow
            var descPlecaRt = descPlecaGo.GetComponent<RectTransform>();
            descPlecaRt.anchorMin = new Vector2(0f, 0.11f);
            descPlecaRt.anchorMax = new Vector2(193f / ReferenceWidth, 0.89f);
            descPlecaRt.offsetMin = Vector2.zero;
            descPlecaRt.offsetMax = Vector2.zero;

            var descPlecaImg = descPlecaGo.AddComponent<Image>();
            var plecaSprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetPlecaPath());
            descPlecaImg.sprite = plecaSprite;
            descPlecaImg.color = Color.white;
            descPlecaImg.raycastTarget = false;

            var descLabel = CreateCardTMP("DescLabel", descPlecaGo.transform, "DESCRIPCIÓN", cardFont, 28f, Color.white, TextAlignmentOptions.Center);
            SetStretch(descLabel.rectTransform, 4f);

            string descriptionContent = GetDescriptionText(cardData);
            var descText = CreateCardTMP("DescText", descRowGo.transform, descriptionContent, cardFont, 28f, new Color(0.06f, 0.06f, 0.06f, 1f), TextAlignmentOptions.MidlineLeft);
            var descTextRt = descText.rectTransform;
            descTextRt.anchorMin = new Vector2(212f / ReferenceWidth, 0f);
            descTextRt.anchorMax = new Vector2(732f / ReferenceWidth, 1f);
            descTextRt.offsetMin = Vector2.zero;
            descTextRt.offsetMax = Vector2.zero;
            descText.enableAutoSizing = true;
            descText.fontSizeMin = 15f;
            descText.fontSizeMax = 28f;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.overflowMode = TextOverflowModes.Ellipsis;

            // 7. Quote Row (Y_top: 895..957)
            var quoteRowGo = CreateChild("QuoteRow", refRt);
            SetRectFromTopLeft(quoteRowGo.GetComponent<RectTransform>(), 0f, 895f, ReferenceWidth, 957f);

            var quotePlecaGo = CreateChild("Pleca", quoteRowGo.transform);
            var quotePlecaRt = quotePlecaGo.GetComponent<RectTransform>();
            quotePlecaRt.anchorMin = new Vector2(0f, 0.11f);
            quotePlecaRt.anchorMax = new Vector2(193f / ReferenceWidth, 0.89f);
            quotePlecaRt.offsetMin = Vector2.zero;
            quotePlecaRt.offsetMax = Vector2.zero;

            var quotePlecaImg = quotePlecaGo.AddComponent<Image>();
            quotePlecaImg.sprite = plecaSprite;
            quotePlecaImg.color = Color.white;
            quotePlecaImg.raycastTarget = false;

            var quoteLabel = CreateCardTMP("QuoteLabel", quotePlecaGo.transform, "FRASE ICÓNICA", cardFont, 28f, Color.white, TextAlignmentOptions.Center);
            SetStretch(quoteLabel.rectTransform, 4f);

            string quoteContent = GetQuoteText(cardData);
            var quoteText = CreateCardTMP("QuoteText", quoteRowGo.transform, quoteContent, cardFont, 28f, new Color(0.14f, 0.14f, 0.14f, 1f), TextAlignmentOptions.MidlineLeft);
            var quoteTextRt = quoteText.rectTransform;
            quoteTextRt.anchorMin = new Vector2(212f / ReferenceWidth, 0f);
            quoteTextRt.anchorMax = new Vector2(732f / ReferenceWidth, 1f);
            quoteTextRt.offsetMin = Vector2.zero;
            quoteTextRt.offsetMax = Vector2.zero;
            quoteText.enableAutoSizing = true;
            quoteText.fontSizeMin = 15f;
            quoteText.fontSizeMax = 28f;
            quoteText.textWrappingMode = TextWrappingModes.Normal;
            quoteText.overflowMode = TextOverflowModes.Ellipsis;

            // 8. Bottom-Right Copyright (X: 380..718, Y_top: 992..1034)
            var copyrightText = CreateCardTMP("CopyrightText", refRt, "© TROMPOS COMETA", cardFont, 27f, Color.white, TextAlignmentOptions.MidlineRight);
            SetRectFromTopLeft(copyrightText.rectTransform, 380f, 992f, 718f, 1034f);

            return rootGo;
        }

        /// <summary>
        /// Renders the dynamically assembled card into a cached 750x1050 Texture2D for 3D FrontFace rendering.
        /// </summary>
        public static Texture2D RenderCardToTexture(ShowcaseCardData cardData)
        {
            if (cardData == null) return null;

            string cacheKey = $"{cardData.Id}_{cardData.Cost}_{cardData.Attack}_{cardData.Defense}";
            if (RenderedCardTextures.TryGetValue(cacheKey, out var cachedTex) && cachedTex != null)
            {
                return cachedTex;
            }

            const int width = 750;
            const int height = 1050;

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;

            var camGo = new GameObject("DynamicCardOffscreenCam");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            camGo.transform.position = new Vector3(50000f, 50000f, -10f);
            camGo.transform.rotation = Quaternion.identity;

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.06f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5.25f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.targetTexture = rt;
            cam.enabled = false;

            var canvasGo = new GameObject("DynamicCardOffscreenCanvas", typeof(RectTransform));
            canvasGo.hideFlags = HideFlags.HideAndDontSave;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, height);
            scaler.matchWidthOrHeight = 1f;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(width, height);

            var clanData = CardShowcaseCatalog.GetClanById(cardData.Clan);
            var assembled = AssembleCard(cardData, clanData, canvasRt, width, height);

            // Ensure all TMP and Image meshes are updated before rendering
            Canvas.ForceUpdateCanvases();
            if (assembled != null)
            {
                foreach (var tmp in assembled.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.ForceMeshUpdate(true, true);
                }
                foreach (var graphic in assembled.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.Rebuild(CanvasUpdate.PreRender);
                }
            }
            Canvas.ForceUpdateCanvases();

            cam.Render();

            RenderTexture.active = rt;
            var resultTex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            resultTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resultTex.Apply(true, false);
            resultTex.name = $"DynamicCard_{cardData.Id}_Tex";
            resultTex.filterMode = FilterMode.Trilinear;
            resultTex.anisoLevel = 4;

            RenderTexture.active = prevActive;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(canvasGo);
                UnityEngine.Object.Destroy(camGo);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(canvasGo);
                UnityEngine.Object.DestroyImmediate(camGo);
            }

            RenderedCardTextures[cacheKey] = resultTex;
            return resultTex;
        }

        /// <summary>
        /// Renders the dynamically assembled card into a cached 750x1050 Sprite.
        /// </summary>
        public static Sprite RenderCardToSprite(ShowcaseCardData cardData)
        {
            if (cardData == null) return null;

            string cacheKey = $"{cardData.Id}_{cardData.Cost}_{cardData.Attack}_{cardData.Defense}";
            if (RenderedCardSprites.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var tex = RenderCardToTexture(cardData);
            if (tex == null) return null;

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sprite.name = $"DynamicCard_{cardData.Id}_Sprite";
            RenderedCardSprites[cacheKey] = sprite;
            return sprite;
        }

        public static string GetDescriptionText(ShowcaseCardData cardData)
        {
            if (cardData == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(cardData.Description)) return cardData.Description;
            if (!string.IsNullOrWhiteSpace(cardData.Effect)) return cardData.Effect;
            if (!string.IsNullOrWhiteSpace(cardData.Lore)) return cardData.Lore;
            return string.Empty;
        }

        public static string GetQuoteText(ShowcaseCardData cardData)
        {
            if (cardData == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(cardData.Quote)) return cardData.Quote;
            if (!string.IsNullOrWhiteSpace(cardData.RealProductFlavor)) return cardData.RealProductFlavor;
            if (!string.IsNullOrWhiteSpace(cardData.Lore)) return cardData.Lore;
            return string.Empty;
        }

        public static Color GetClanColor(ClanType clan)
        {
            string hex = CardArtMapper.GetClanColorHex(clan);
            if (ColorUtility.TryParseHtmlString(hex, out var col))
            {
                return col;
            }
            return new Color(0.94f, 0.36f, 0.03f, 1f);
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRectFromTopLeft(RectTransform rt, float xMin, float yTopMin, float xMax, float yTopMax)
        {
            rt.anchorMin = new Vector2(xMin / ReferenceWidth, (ReferenceHeight - yTopMax) / ReferenceHeight);
            rt.anchorMax = new Vector2(xMax / ReferenceWidth, (ReferenceHeight - yTopMin) / ReferenceHeight);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetStretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static TextMeshProUGUI CreateCardTMP(
            string name,
            Transform parent,
            string text,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var go = CreateChild(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text ?? string.Empty;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = FontStyles.Italic | FontStyles.Bold;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float radius = size * 0.48f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedCircleSprite.name = "ProceduralCircleBadge";
            return cachedCircleSprite;
        }

        private static Sprite GetShieldSprite()
        {
            if (cachedShieldSprite != null) return cachedShieldSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1); // 0 = bottom point, 1 = top
                for (int x = 0; x < size; x++)
                {
                    float u = Mathf.Abs((x - (size - 1) * 0.5f) / (size * 0.44f));
                    byte a = 0;

                    if (v >= 0.06f && v <= 0.92f)
                    {
                        float t = (v - 0.06f) / 0.86f;
                        float halfWidth = Mathf.Pow(Mathf.Clamp01(t), 0.55f);
                        float edgeDist = halfWidth - u;
                        if (edgeDist >= -0.04f)
                        {
                            float alpha = Mathf.Clamp01((edgeDist + 0.04f) * 18f);
                            a = (byte)Mathf.RoundToInt(alpha * 255f);
                        }
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            cachedShieldSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedShieldSprite.name = "ProceduralShieldBadge";
            return cachedShieldSprite;
        }
    }
}
