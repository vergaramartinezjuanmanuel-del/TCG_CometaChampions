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

        private const string FontAssetEditorPath = "Assets/Resources/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset";
        private const string FontAssetEditorLegacyPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset";
        private const string FontFileEditorPath = "Assets/Resources/Tarjetas/SemiExpanded ExtraBold Italic.otf";
        private const string FontFileEditorLegacyPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf";

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
            cachedCardFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetEditorPath)
                             ?? UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetEditorLegacyPath);
            if (cachedCardFont != null) return cachedCardFont;

            var editorSourceFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(FontFileEditorPath)
                                   ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(FontFileEditorLegacyPath);
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

            var resFontAsset = Resources.Load<TMP_FontAsset>("Tarjetas/SemiExpanded ExtraBold Italic SDF")
                               ?? Resources.Load<TMP_FontAsset>("Fonts/SemiExpanded ExtraBold Italic SDF")
                               ?? Resources.Load<TMP_FontAsset>("SemiExpanded ExtraBold Italic SDF");
            if (resFontAsset != null)
            {
                cachedCardFont = resFontAsset;
                return cachedCardFont;
            }

            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                string[] diskPaths = {
                    Path.Combine(Application.dataPath, "Resources/Tarjetas/SemiExpanded ExtraBold Italic.otf"),
                    Path.Combine(Application.dataPath, "_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf")
                };
                foreach (var diskFontPath in diskPaths)
                {
                    if (File.Exists(diskFontPath))
                    {
                        var diskFont = new Font(diskFontPath);
                        cachedCardFont = TMP_FontAsset.CreateFontAsset(diskFont);
                        if (cachedCardFont != null) return cachedCardFont;
                    }
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        private const string OswaldFontAssetPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset";
        private static TMP_FontAsset cachedOswaldFont;

        /// <summary>
        /// Loads or dynamically references the TMP_FontAsset for "Oswald Bold SDF".
        /// </summary>
        public static TMP_FontAsset GetOswaldBoldFont()
        {
            if (cachedOswaldFont != null) return cachedOswaldFont;

#if UNITY_EDITOR
            cachedOswaldFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OswaldFontAssetPath);
            if (cachedOswaldFont != null) return cachedOswaldFont;
#endif

            cachedOswaldFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF")
                               ?? Resources.Load<TMP_FontAsset>("Oswald Bold SDF");
            if (cachedOswaldFont != null) return cachedOswaldFont;

            return GetCardFont();
        }

        /// <summary>
        /// Modular body font selector: returns Oswald Bold SDF for cards across Home and Battle.
        /// </summary>
        public static TMP_FontAsset GetBodyFont(bool isMain3DCard = true)
        {
            return GetOswaldBoldFont();
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
                return new ShowcaseCardData
                {
                    Id = catalogEntry.Id,
                    Name = catalogEntry.Name,
                    Category = catalogEntry.Category,
                    CategoryName = catalogEntry.CategoryName,
                    Clan = catalogEntry.Clan,
                    Cost = definition.EnergyCost > 0 ? definition.EnergyCost : catalogEntry.Cost,
                    Attack = definition.BaseAttack > 0 ? definition.BaseAttack : catalogEntry.Attack,
                    Defense = definition.BaseHealth > 0 ? definition.BaseHealth : catalogEntry.Defense,
                    Image = catalogEntry.Image,
                    Rarity = catalogEntry.Rarity,
                    Quote = catalogEntry.Quote,
                    Description = catalogEntry.Description,
                    CardType = catalogEntry.CardType,
                    Effect = !string.IsNullOrWhiteSpace(definition.AbilityText) ? definition.AbilityText : catalogEntry.Effect,
                    RealProductFlavor = catalogEntry.RealProductFlavor,
                    Lore = !string.IsNullOrWhiteSpace(definition.LoreText) ? definition.LoreText : catalogEntry.Lore
                };
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
            float cardHeight = 0f,
            bool isMain3DCard = true)
        {
            if (definition == null || parent == null) return null;
            var cardData = ResolveShowcaseCardData(definition);
            var clanData = CardShowcaseCatalog.GetClanById(cardData.Clan);
            return AssembleCard(cardData, clanData, parent, cardWidth, cardHeight, isMain3DCard);
        }

        /// <summary>
        /// Main entry point: Dynamically builds all card layers matching the Avispón reference card.
        /// </summary>
        public static GameObject AssembleCard(
            ShowcaseCardData cardData,
            ShowcaseClanData clanData,
            RectTransform parent,
            float cardWidth = 0f,
            float cardHeight = 0f,
            bool isMain3DCard = true)
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
            TMP_FontAsset bodyFont = GetBodyFont(isMain3DCard);

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
            artImg.color = artSprite != null ? Color.white : new Color(0.08f, 0.10f, 0.14f, 1f);
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
            costIconImg.color = costIconImg.sprite != null ? Color.white : Color.clear;
            costIconImg.preserveAspect = true;
            costIconImg.raycastTarget = false;

            var costText = CreateCardTMP("CostText", costBadgeGo.transform, cardData.Cost.ToString(), cardFont, 64f, Color.white, TextAlignmentOptions.Center);
            SetStretch(costText.rectTransform, 0f);

            // 3. Top-Right Clan Banner (X: 563..715, Y_top: 0..147)
            var clanBannerGo = CreateChild("ClanBanner", refRt);
            SetRectFromTopLeft(clanBannerGo.GetComponent<RectTransform>(), 563f, 0f, 715f, 147f);
            var clanBannerImg = clanBannerGo.AddComponent<Image>();
            clanBannerImg.sprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetClanBannerPath(clanType));
            clanBannerImg.color = clanBannerImg.sprite != null ? Color.white : Color.clear;
            clanBannerImg.preserveAspect = true;
            clanBannerImg.raycastTarget = false;

            // 4. Clan Lower Frame & NameBar (Full 750x1050 BG_<Clan>.png overlay)
            var nameBarGo = CreateChild("NameBar", refRt);
            SetRectFromTopLeft(nameBarGo.GetComponent<RectTransform>(), 0f, 0f, ReferenceWidth, ReferenceHeight);
            var nameBarImg = nameBarGo.AddComponent<Image>();
            nameBarImg.sprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetClanBgPath(clanType));
            nameBarImg.color = nameBarImg.sprite != null ? Color.white : Color.clear;
            nameBarImg.preserveAspect = false;
            nameBarImg.raycastTarget = false;

            // Card Name on Left of Clan Band (X: 33..608, Y_top: 650..734)
            string displayName = CardArtMapper.GetCardDisplayName(cardData.Name);
            var nameText = CreateCardTMP("NameText", nameBarGo.transform, displayName, cardFont, 68f, Color.white, TextAlignmentOptions.MidlineLeft);
            SetRectFromTopLeft(nameText.rectTransform, 33f, 650f, 608f, 734f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 32f;
            nameText.fontSizeMax = 68f;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Attack & Defense Badges on Right of Clan Band (shown for all card types: Heroes, Beasts, Trompos, Arenas)
            var statsGroupGo = CreateChild("StatsGroup", nameBarGo.transform);
            SetRectFromTopLeft(statsGroupGo.GetComponent<RectTransform>(), 520f, 632f, 736f, 750f);

            bool showStats = true;
            statsGroupGo.SetActive(showStats);

            if (showStats)
            {
                var swordSprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetSwordIconPath())
                                  ?? Card3D.LoadSpriteFromCardsFolder("Home/espada2.png")
                                  ?? GetCircleSprite();

                // Attack Badge (Right stat badge with espada2.png icon: X: 616..736, Y_top: 625..757, enlarged +20%)
                var atkBadgeGo = CreateChild("AtkBadge", nameBarGo.transform);
                SetRectFromTopLeft(atkBadgeGo.GetComponent<RectTransform>(), 616f, 625f, 736f, 757f);
                var atkOuterImg = atkBadgeGo.AddComponent<Image>();
                atkOuterImg.sprite = swordSprite;
                atkOuterImg.color = Color.white;
                atkOuterImg.preserveAspect = true;
                atkOuterImg.raycastTarget = false;

                var atkInnerGo = CreateChild("AtkInner", atkBadgeGo.transform);
                SetStretch(atkInnerGo.GetComponent<RectTransform>(), 4f);
                var atkInnerImg = atkInnerGo.AddComponent<Image>();
                atkInnerImg.sprite = swordSprite;
                atkInnerImg.color = clanColor;
                atkInnerImg.preserveAspect = true;
                atkInnerImg.raycastTarget = false;

                var atkText = CreateCardTMP("AtkText", atkBadgeGo.transform, cardData.Attack.ToString(), cardFont, 65f, Color.white, TextAlignmentOptions.Center);
                SetStretch(atkText.rectTransform, 2f);

                // Defense Badge (Right stat badge - hidden per user instruction, preserving structure)
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
                defBadgeGo.SetActive(false);
            }

            // 5. Category Label Centered in Dark Strip (X: 50..700, Y_top: 750..792)
            string categoryDisplay = CardArtMapper.GetCategoryDisplayName(cardData.Category);
            float catFontSize = 32f;
            FontStyles catFontStyle = FontStyles.Bold;
            var categoryText = CreateCardTMP("CategoryText", refRt, categoryDisplay, bodyFont, catFontSize, Color.white, TextAlignmentOptions.Center, catFontStyle);
            SetRectFromTopLeft(categoryText.rectTransform, 50f, 750f, 700f, 792f);

            // 6. Description Row
            var descRowGo = CreateChild("DescriptionRow", refRt);
            SetRectFromTopLeft(descRowGo.GetComponent<RectTransform>(), 0f, 804f, ReferenceWidth, 886f);

            var descPlecaGo = CreateChild("Pleca", descRowGo.transform);
            var descPlecaRt = descPlecaGo.GetComponent<RectTransform>();
            descPlecaRt.anchorMin = new Vector2(14f / ReferenceWidth, 0.22f);
            descPlecaRt.anchorMax = new Vector2(196f / ReferenceWidth, 0.78f);
            descPlecaRt.offsetMin = Vector2.zero;
            descPlecaRt.offsetMax = Vector2.zero;

            var descPlecaImg = descPlecaGo.AddComponent<Image>();
            var plecaSprite = Card3D.LoadSpriteFromCardsFolder(CardArtMapper.GetPlecaPath());
            descPlecaImg.sprite = plecaSprite;
            descPlecaImg.color = Color.white;
            descPlecaImg.raycastTarget = false;

            float descLabelSize = 25f;
            var descLabel = CreateCardTMP("DescLabel", descPlecaGo.transform, "DESCRIPCIÓN", bodyFont, descLabelSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            SetStretch(descLabel.rectTransform, 4f);

            string descriptionContent = GetDescriptionText(cardData);
            Color descTextColor = new Color(0.07f, 0.07f, 0.09f, 1f);
            FontStyles descFontStyle = FontStyles.Bold;
            var descText = CreateCardTMP("DescText", descRowGo.transform, descriptionContent, bodyFont, 28f, descTextColor, TextAlignmentOptions.MidlineLeft, descFontStyle);
            var descTextRt = descText.rectTransform;
            descTextRt.anchorMin = new Vector2(208f / ReferenceWidth, 0f);
            descTextRt.anchorMax = new Vector2(736f / ReferenceWidth, 1f);
            descText.lineSpacing = 4f;
            descText.fontSizeMin = 18f;
            descTextRt.offsetMin = Vector2.zero;
            descTextRt.offsetMax = Vector2.zero;
            descText.enableAutoSizing = true;
            descText.fontSizeMax = 28f;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.overflowMode = TextOverflowModes.Ellipsis;

            // 7. Quote Row
            var quoteRowGo = CreateChild("QuoteRow", refRt);
            SetRectFromTopLeft(quoteRowGo.GetComponent<RectTransform>(), 0f, 892f, ReferenceWidth, 974f);

            var quotePlecaGo = CreateChild("Pleca", quoteRowGo.transform);
            var quotePlecaRt = quotePlecaGo.GetComponent<RectTransform>();
            quotePlecaRt.anchorMin = new Vector2(14f / ReferenceWidth, 0.22f);
            quotePlecaRt.anchorMax = new Vector2(196f / ReferenceWidth, 0.78f);
            quotePlecaRt.offsetMin = Vector2.zero;
            quotePlecaRt.offsetMax = Vector2.zero;

            var quotePlecaImg = quotePlecaGo.AddComponent<Image>();
            quotePlecaImg.sprite = plecaSprite;
            quotePlecaImg.color = Color.white;
            quotePlecaImg.raycastTarget = false;

            float quoteLabelSize = 23f;
            var quoteLabel = CreateCardTMP("QuoteLabel", quotePlecaGo.transform, "FRASE ICÓNICA", bodyFont, quoteLabelSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            SetStretch(quoteLabel.rectTransform, 4f);

            string quoteContent = GetQuoteText(cardData);
            Color quoteTextColor = new Color(0.10f, 0.10f, 0.13f, 1f);
            var quoteText = CreateCardTMP("QuoteText", quoteRowGo.transform, quoteContent, bodyFont, 28f, quoteTextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold | FontStyles.Italic);
            var quoteTextRt = quoteText.rectTransform;
            quoteTextRt.anchorMin = new Vector2(208f / ReferenceWidth, 0f);
            quoteTextRt.anchorMax = new Vector2(736f / ReferenceWidth, 1f);
            quoteText.lineSpacing = 4f;
            quoteText.fontSizeMin = 18f;
            quoteTextRt.offsetMin = Vector2.zero;
            quoteTextRt.offsetMax = Vector2.zero;
            quoteText.enableAutoSizing = true;
            quoteText.fontSizeMax = 28f;
            quoteText.textWrappingMode = TextWrappingModes.Normal;
            quoteText.overflowMode = TextOverflowModes.Ellipsis;

            // 8. Bottom-Right Copyright
            float copyFontSize = 24f;
            FontStyles copyFontStyle = FontStyles.Bold;
            Color copyColor = new Color(0.90f, 0.90f, 0.92f, 1f);
            var copyrightText = CreateCardTMP("CopyrightText", refRt, "© TROMPOS COMETA", bodyFont, copyFontSize, copyColor, TextAlignmentOptions.MidlineRight, copyFontStyle);
            SetRectFromTopLeft(copyrightText.rectTransform, 350f, 994f, 725f, 1036f);

            return rootGo;
        }

        /// <summary>
        /// Renders the dynamically assembled card into a cached 750x1050 Texture2D for 3D FrontFace rendering.
        /// </summary>
        public static Texture2D RenderCardToTexture(ShowcaseCardData cardData, bool isMain3DCard = true)
        {
            if (cardData == null) return null;

            string cacheKey = $"{cardData.Id}_{cardData.Cost}_{cardData.Attack}_{cardData.Defense}_{(isMain3DCard ? "Main" : "Norm")}";
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
            cam.cullingMask = ~0; // Ensure all UI and geometry layers are rendered
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
            var assembled = AssembleCard(cardData, clanData, canvasRt, width, height, isMain3DCard);

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
            GL.Flush(); // Ensure GPU offscreen commands finish on mobile GPUs before ReadPixels

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
        public static Sprite RenderCardToSprite(ShowcaseCardData cardData, bool isMain3DCard = true)
        {
            if (cardData == null) return null;

            string cacheKey = $"{cardData.Id}_{cardData.Cost}_{cardData.Attack}_{cardData.Defense}_{(isMain3DCard ? "Main" : "Norm")}";
            if (RenderedCardSprites.TryGetValue(cacheKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var tex = RenderCardToTexture(cardData, isMain3DCard);
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
            TextAlignmentOptions alignment,
            FontStyles fontStyle = FontStyles.Italic | FontStyles.Bold)
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
            tmp.fontStyle = fontStyle;
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
