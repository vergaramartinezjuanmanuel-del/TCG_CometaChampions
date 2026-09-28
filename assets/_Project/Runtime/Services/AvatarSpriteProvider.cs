using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Utility provider that caches and furnishes 2D Sprites for player avatars
    /// from the official character portraits directory (Assets/Resources/Characters/).
    /// Works universally on Android, iOS, Standalone, and in the Unity Editor.
    /// </summary>
    public static class AvatarSpriteProvider
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves the cached 2D Sprite for an avatar definition.
        /// </summary>
        public static Sprite GetAvatarSprite(PlayerAvatarDefinition definition)
        {
            if (definition == null) return null;
            return GetAvatarSpriteByFileName(definition.FileName);
        }

        /// <summary>
        /// Retrieves the cached 2D Sprite for an avatar by its ID.
        /// </summary>
        public static Sprite GetAvatarSprite(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId)) return null;
            var def = AvatarCatalog.GetAvatarById(avatarId);
            return def != null ? GetAvatarSprite(def) : null;
        }

        /// <summary>
        /// Loads and caches a portrait Sprite by file name (e.g. "cometin_Mi.png").
        /// Primary source: Unity Resources ("Characters/{name}").
        /// Fallbacks: AssetDatabase (Editor) and direct file IO (Standalone).
        /// </summary>
        public static Sprite GetAvatarSpriteByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            if (SpriteCache.TryGetValue(fileName, out var cached) && cached != null)
            {
                return cached;
            }

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string resourcePath = "Characters/" + nameWithoutExt;

            // 1. Primary runtime path (Android, iOS, Standalone, Editor): Unity Resources
            var resSprite = Resources.Load<Sprite>(resourcePath);
            if (resSprite != null)
            {
                SpriteCache[fileName] = resSprite;
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
                SpriteCache[fileName] = s;
                return s;
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase Fallbacks
            string[] editorPaths = {
                "Assets/Resources/Characters/" + fileName,
                "Assets/_Project/Art/Characters/" + fileName
            };

            foreach (var assetPath in editorPaths)
            {
                var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (editorSprite != null)
                {
                    SpriteCache[fileName] = editorSprite;
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
                    SpriteCache[fileName] = s;
                    return s;
                }
            }
#endif

            // 3. Fallback: Direct file read (for standalone PC/Mac if files are present on disk)
            string[] candidateFiles = {
                Path.Combine(Application.dataPath, "Resources/Characters", fileName),
                Path.Combine(Application.dataPath, "_Project/Art/Characters", fileName)
            };

            foreach (var fullPath in candidateFiles)
            {
                if (File.Exists(fullPath))
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
                        SpriteCache[fileName] = s;
                        return s;
                    }
                }
            }

            Debug.LogWarning($"[AvatarSpriteProvider] Could not load avatar sprite for: {fileName}");
            return null;
        }
    }
}
