using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace CometaChampions.Editor
{
    /// <summary>
    /// Editor utility that generates the TMP_FontAsset (SDF) from
    /// "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf".
    /// Accessible via menu: Cometa/Generate TMP Font
    /// </summary>
    public static class TmpFontGenerator
    {
        public const string SourceFontPath = "Assets/Resources/Tarjetas/SemiExpanded ExtraBold Italic.otf";
        public const string OutputSdfAssetPath = "Assets/Resources/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset";
        public const string LegacySourceFontPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic.otf";
        public const string LegacyOutputSdfAssetPath = "Assets/_Project/Art/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset";

        private const string PrewarmCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "¡!¿?©.,:-+'()/\"áéíóúüñÁÉÍÓÚÜÑ ";

        [InitializeOnLoadMethod]
        private static void EnsureFontAssetOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(OutputSdfAssetPath) && !File.Exists(LegacyOutputSdfAssetPath))
                {
                    GenerateTmpFontAsset();
                }
            };
        }

        [MenuItem("Cometa/Generate TMP Font")]
        [MenuItem("Cometa Champions/Generate Card TMP Font")]
        public static TMP_FontAsset GenerateTmpFontAsset()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath)
                             ?? AssetDatabase.LoadAssetAtPath<Font>(LegacySourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[TmpFontGenerator] Source font not found at: {SourceFontPath}");
                return null;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputSdfAssetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(OutputSdfAssetPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                48,
                5,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[TmpFontGenerator] Failed to create TMP_FontAsset.");
                return null;
            }

            fontAsset.name = "SemiExpanded ExtraBold Italic SDF";
            AssetDatabase.CreateAsset(fontAsset, OutputSdfAssetPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            fontAsset.TryAddCharacters(PrewarmCharacters, out _);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TmpFontGenerator] Generated TMP SDF Font Asset at: {OutputSdfAssetPath}");
            return fontAsset;
        }
    }
}
