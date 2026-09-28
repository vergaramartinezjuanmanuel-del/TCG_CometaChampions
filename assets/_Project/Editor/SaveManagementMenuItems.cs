using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Services;
using UnityEngine.Purchasing;

namespace CometaChampions.Editor
{
    /// <summary>
    /// Editor shortcuts to manage and reset player progress and save files.
    /// Accessible via menu: Cometa Champions -> Save System -> ...
    /// </summary>
    public static class SaveManagementMenuItems
    {
        [MenuItem("Cometa Champions/Save System/Reset Progress to Default", priority = 100)]
        public static void ResetProgressToDefault()
        {
            if (EditorApplication.isPlaying && SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetToDefaultProfile();
                Debug.Log("[SaveManagement] Reset player progress to default in Play mode.");
                EditorUtility.DisplayDialog("Progreso Reiniciado", "El progreso del jugador ha sido reiniciado a los valores iniciales por defecto.", "Aceptar");
                return;
            }

            // Edit Mode: Write fresh default profile to save file
            var freshProfile = PlayerProfile.CreateDefaultProfile();
            string json = JsonConvert.SerializeObject(freshProfile, Formatting.Indented);
            string path = SaveManager.SaveFilePath;
            string dir = Path.GetDirectoryName(path)!;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, json);
            Debug.Log($"[SaveManagement] Reset save file at {path} with fresh default profile.");
            EditorUtility.DisplayDialog("Progreso Reiniciado", $"El progreso ha sido reiniciado correctamente.\n\nNuevo Apodo: {freshProfile.PlayerName}\nMonedas: {freshProfile.GoldCoins}\nCartas iniciales restauradas.", "Aceptar");
        }

        [MenuItem("Cometa Champions/Save System/Delete Save File", priority = 101)]
        public static void DeleteSaveFile()
        {
            string path = SaveManager.SaveFilePath;
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManagement] Deleted save file at: {path}");
                EditorUtility.DisplayDialog("Archivo Eliminado", "El archivo cometa_player_save.json ha sido eliminado. Se generará un nuevo perfil en la próxima ejecución.", "Aceptar");
            }
            else
            {
                EditorUtility.DisplayDialog("No encontrado", "No se encontró ningún archivo de guardado en el disco.", "Aceptar");
            }
        }

        [MenuItem("Cometa Champions/Save System/Open Save File Location in Finder", priority = 102)]
        public static void RevealSaveFileInFinder()
        {
            string path = SaveManager.SaveFilePath;
            if (File.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                string dir = Path.GetDirectoryName(path)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                EditorUtility.RevealInFinder(dir);
            }
        }

        [MenuItem("Cometa Champions/Save System/Add +150 Test XP", priority = 120)]
        public static void AddTestXp()
        {
            PlayerProfile profile;
            if (EditorApplication.isPlaying && SaveManager.Instance != null)
            {
                profile = SaveManager.Instance.CurrentProfile;
                profile.AddAccountExperience(150, out int levelsGained, out int goldReward, out int packReward);
                SaveManager.Instance.SaveProfile();
                Debug.Log($"[SaveManagement] Added 150 XP! Levels gained: {levelsGained}, Level: {profile.AccountLevel}, XP: {profile.CurrentLevelXp}, Gold: +{goldReward}, Packs: +{packReward}");
                EditorUtility.DisplayDialog("XP Añadida", $"+150 XP añadida.\nNivel actual: {profile.AccountLevel}\nProgreso: {profile.CurrentLevelXp} XP\nSubidas de nivel: {levelsGained}", "Aceptar");
                return;
            }

            string path = SaveManager.SaveFilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                profile = JsonConvert.DeserializeObject<PlayerProfile>(json) ?? PlayerProfile.CreateDefaultProfile();
            }
            else
            {
                profile = PlayerProfile.CreateDefaultProfile();
            }

            profile.AddAccountExperience(150, out int levels, out int gold, out int packs);
            string updatedJson = JsonConvert.SerializeObject(profile, Formatting.Indented);
            string dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, updatedJson);

            Debug.Log($"[SaveManagement] Added 150 XP to save file. Level: {profile.AccountLevel}, XP: {profile.CurrentLevelXp}");
            EditorUtility.DisplayDialog("XP Añadida", $"+150 XP guardada en disco.\nNivel actual: {profile.AccountLevel}\nProgreso: {profile.CurrentLevelXp} XP", "Aceptar");
        }

        [MenuItem("Cometa Champions/Monetization/Buy Pack 1,000 Coins", priority = 200)]
        public static void BuyPack1000()
        {
            if (EditorApplication.isPlaying)
            {
                MonetizationService.Instance.BuyCoinPack(CoinPackType.Pack1000);
            }
            else
            {
                SimulateCoinCredit(1000, "Pack 1,000 Monedas");
            }
        }

        [MenuItem("Cometa Champions/Monetization/Buy Pack 5,000 Coins", priority = 201)]
        public static void BuyPack5000()
        {
            if (EditorApplication.isPlaying)
            {
                MonetizationService.Instance.BuyCoinPack(CoinPackType.Pack5000);
            }
            else
            {
                SimulateCoinCredit(5000, "Pack 5,000 Monedas");
            }
        }

        [MenuItem("Cometa Champions/Monetization/Buy Pack 10,000 Coins", priority = 202)]
        public static void BuyPack10000()
        {
            if (EditorApplication.isPlaying)
            {
                MonetizationService.Instance.BuyCoinPack(CoinPackType.Pack10000);
            }
            else
            {
                SimulateCoinCredit(10000, "Pack 10,000 Monedas");
            }
        }

        [MenuItem("Cometa Champions/Monetization/Watch Video Ad (+100 Coins)", priority = 203)]
        public static void WatchVideoAd()
        {
            if (EditorApplication.isPlaying)
            {
                MonetizationService.Instance.ShowRewardedVideo();
            }
            else
            {
                SimulateCoinCredit(100, "Recompensa Unity Ads");
            }
        }

        [MenuItem("Cometa Champions/Monetization/Validate and Export IAP Catalog", priority = 210)]
        public static void ValidateAndExportIapCatalog()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog == null || catalog.allValidProducts.Count == 0)
            {
                catalog = new ProductCatalog();
                catalog.enableCodelessAutoInitialization = false;
                catalog.enableUnityGamingServicesAutoInitialization = false;

                var pack1000 = new ProductCatalogItem
                {
                    id = PlayerProfile.IapPack1000Id,
                    type = ProductType.Consumable,
                    applePriceTier = 1,
                    googlePrice = new Price { value = 0.99m },
                    defaultDescription = new LocalizedProductDescription
                    {
                        googleLocale = TranslationLocale.es_ES,
                        Title = "1,000 Monedas de Oro",
                        Description = "Paquete básico de 1,000 monedas para Cometa Champions."
                    }
                };

                var pack5000 = new ProductCatalogItem
                {
                    id = PlayerProfile.IapPack5000Id,
                    type = ProductType.Consumable,
                    applePriceTier = 5,
                    googlePrice = new Price { value = 4.99m },
                    defaultDescription = new LocalizedProductDescription
                    {
                        googleLocale = TranslationLocale.es_ES,
                        Title = "5,000 Monedas de Oro",
                        Description = "Paquete aventurero de 5,000 monedas para Cometa Champions."
                    }
                };

                var pack10000 = new ProductCatalogItem
                {
                    id = PlayerProfile.IapPack10000Id,
                    type = ProductType.Consumable,
                    applePriceTier = 9,
                    googlePrice = new Price { value = 8.99m },
                    defaultDescription = new LocalizedProductDescription
                    {
                        googleLocale = TranslationLocale.es_ES,
                        Title = "10,000 Monedas de Oro",
                        Description = "Paquete de campeón de 10,000 monedas para Cometa Champions."
                    }
                };

                catalog.allProducts.Add(pack1000);
                catalog.allProducts.Add(pack5000);
                catalog.allProducts.Add(pack10000);
            }

            string json = ProductCatalog.Serialize(catalog);
            string catalogPath = ProductCatalog.kCatalogPath;
            string dir = Path.GetDirectoryName(catalogPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(catalogPath, json);
            AssetDatabase.ImportAsset(catalogPath);

            int count = catalog.allValidProducts.Count;
            Debug.Log($"[SaveManagement] IAP Catalog validated and exported successfully ({count} products) at {catalogPath}");
            EditorUtility.DisplayDialog("Catálogo IAP Validado", $"El catálogo de productos IAP contiene {count} productos válidos y está sincronizado en:\n\n{catalogPath}", "Aceptar");
        }

        [MenuItem("Cometa Champions/Monetization/Validate and Export Unity Ads Settings", priority = 211)]
        public static void ValidateAndExportUnityAdsSettings()
        {
            string settingsPath = "Assets/Resources/UnityAdsSettings.json";
            UnityAdsConfig config;
            if (File.Exists(settingsPath))
            {
                string existingJson = File.ReadAllText(settingsPath);
                config = JsonConvert.DeserializeObject<UnityAdsConfig>(existingJson) ?? new UnityAdsConfig();
            }
            else
            {
                config = new UnityAdsConfig();
            }

            string dir = Path.GetDirectoryName(settingsPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(settingsPath, json);
            AssetDatabase.ImportAsset(settingsPath);

            Debug.Log($"[SaveManagement] Unity Ads settings validated and exported at {settingsPath}. Active GameId: {config.ActiveGameId}, AdUnit: {config.ActiveAdUnitId}");
            EditorUtility.DisplayDialog("Configuración Unity Ads", $"La configuración de Unity Ads se validó y sincronizó correctamente en:\n\n{settingsPath}\n\nAndroid Game ID: {config.androidGameId}\niOS Game ID: {config.iosGameId}\nAd Unit: {config.ActiveAdUnitId}\nTest Mode: {config.testMode}", "Aceptar");
        }

        private static void SimulateCoinCredit(int amount, string sourceName)
        {
            string path = SaveManager.SaveFilePath;
            PlayerProfile profile;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                profile = JsonConvert.DeserializeObject<PlayerProfile>(json) ?? PlayerProfile.CreateDefaultProfile();
            }
            else
            {
                profile = PlayerProfile.CreateDefaultProfile();
            }

            profile.AddGoldCoins(amount);
            string updatedJson = JsonConvert.SerializeObject(profile, Formatting.Indented);
            string dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, updatedJson);

            Debug.Log($"[Monetization] Credited {amount} coins ({sourceName}). New balance: {profile.GoldCoins:N0}");
            EditorUtility.DisplayDialog("Monedas Acreditadas", $"+{amount:N0} Monedas de Oro acreditadas ({sourceName}).\nNuevo saldo: {profile.GoldCoins:N0}", "Aceptar");
        }
    }
}
