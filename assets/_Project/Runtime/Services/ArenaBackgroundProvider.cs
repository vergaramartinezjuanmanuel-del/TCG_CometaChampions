using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Utility provider that caches and furnishes 2D Sprites for official arena backgrounds
    /// from Assets/Resources/Backgrounds/.
    /// Supports random selection for the Battle scene and multi-platform fallback loading.
    /// </summary>
    public static class ArenaBackgroundProvider
    {
        public const string TemploMilenario = ArenaBackgroundCatalog.TemploMilenario;
        public const string PistaDraconia = ArenaBackgroundCatalog.PistaDraconia;
        public const string LlanurasTirano = ArenaBackgroundCatalog.LlanurasTirano;
        public const string NebulosaCosmica = ArenaBackgroundCatalog.NebulosaCosmica;
        public const string Pedestal3DActive = ArenaBackgroundCatalog.Pedestal3DActive;
        public const string Pedestal3DIdle = ArenaBackgroundCatalog.Pedestal3DIdle;

        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns all 6 official arena background names.
        /// </summary>
        public static IReadOnlyList<string> GetAllBackgroundNames()
        {
            return ArenaBackgroundCatalog.GetAllBackgroundNames();
        }

        /// <summary>
        /// Gets a random background name from the 6 official arenas.
        /// </summary>
        public static string GetRandomBackgroundName(System.Random rng = null)
        {
            return ArenaBackgroundCatalog.GetRandomBackgroundName(rng);
        }

        /// <summary>
        /// Returns the human-readable display name for the given arena background ID.
        /// </summary>
        public static string GetDisplayName(string backgroundName)
        {
            return ArenaBackgroundCatalog.GetDisplayName(backgroundName);
        }

        /// <summary>
        /// Retrieves or loads the 2D Sprite for an arena background by name.
        /// Uses memory cache, Unity Resources, and standalone/editor disk fallbacks.
        /// </summary>
        public static Sprite GetBackgroundSprite(string backgroundName)
        {
            if (string.IsNullOrEmpty(backgroundName)) return null;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(backgroundName);

            if (SpriteCache.TryGetValue(nameWithoutExt, out var cached) && cached != null)
            {
                return cached;
            }

            string resourcePath = "Backgrounds/" + nameWithoutExt;

            // 1. Primary runtime path: Unity Resources
            var resSprite = Resources.Load<Sprite>(resourcePath);
            if (resSprite != null)
            {
                SpriteCache[nameWithoutExt] = resSprite;
                return resSprite;
            }

            var resTex = Resources.Load<Texture2D>(resourcePath);
            if (resTex != null)
            {
                var s = Sprite.Create(
                    resTex,
                    new Rect(0, 0, resTex.width, resTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                s.name = nameWithoutExt + "_Sprite";
                SpriteCache[nameWithoutExt] = s;
                return s;
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            string[] editorPaths = {
                "Assets/Resources/Backgrounds/" + nameWithoutExt + ".jpg",
                "Assets/Resources/Backgrounds/" + nameWithoutExt + ".png",
                "Assets/_Project/Art/Backgrounds/" + nameWithoutExt + ".jpg"
            };

            foreach (var assetPath in editorPaths)
            {
                var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (editorSprite != null)
                {
                    SpriteCache[nameWithoutExt] = editorSprite;
                    return editorSprite;
                }

                var editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (editorTex != null)
                {
                    var s = Sprite.Create(
                        editorTex,
                        new Rect(0, 0, editorTex.width, editorTex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    s.name = nameWithoutExt + "_Sprite";
                    SpriteCache[nameWithoutExt] = s;
                    return s;
                }
            }
#endif

            // 3. Fallback: Direct file read (for Standalone PC/Mac if files are present on disk)
            string[] candidateFiles = {
                Path.Combine(Application.dataPath, "Resources/Backgrounds", nameWithoutExt + ".jpg"),
                Path.Combine(Application.dataPath, "Resources/Backgrounds", nameWithoutExt + ".png"),
                Path.Combine(Application.dataPath, "_Project/Art/Backgrounds", nameWithoutExt + ".jpg")
            };

            foreach (var fullPath in candidateFiles)
            {
                if (File.Exists(fullPath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(fullPath);
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            tex.name = nameWithoutExt;
                            var s = Sprite.Create(
                                tex,
                                new Rect(0, 0, tex.width, tex.height),
                                new Vector2(0.5f, 0.5f),
                                100f
                            );
                            s.name = nameWithoutExt + "_Sprite";
                            SpriteCache[nameWithoutExt] = s;
                            return s;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ArenaBackgroundProvider] Error loading background from disk ({fullPath}): {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Selects a random background from the 6 official arenas and returns its Sprite.
        /// </summary>
        public static Sprite GetRandomBackgroundSprite(System.Random rng = null)
        {
            string name = GetRandomBackgroundName(rng);
            return GetBackgroundSprite(name);
        }

        /// <summary>
        /// Clears the in-memory sprite cache.
        /// </summary>
        public static void ClearCache()
        {
            SpriteCache.Clear();
        }
    }
}
