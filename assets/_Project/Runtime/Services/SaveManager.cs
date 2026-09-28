using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Persistent singleton service that handles saving and loading the PlayerProfile
    /// to local disk (Application.persistentDataPath/cometa_player_save.json).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "cometa_player_save.json";

        private static SaveManager? instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindAnyObjectByType<SaveManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("SaveManager");
                        instance = go.AddComponent<SaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                if (instance.CurrentProfile == null)
                {
                    instance.LoadProfile();
                }

                return instance;
            }
        }

        public PlayerProfile CurrentProfile { get; private set; } = null!;
        public static PlayerProfile Profile => Instance.CurrentProfile;
        public static void Save(PlayerProfile profile = null) => Instance.SaveProfile();
        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public event Action? OnProfileLoaded;
        public event Action? OnProfileSaved;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            if (CurrentProfile == null)
            {
                LoadProfile();
            }
        }

        public void LoadProfile()
        {
            PlayerProfile? loaded = null;
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    loaded = JsonConvert.DeserializeObject<PlayerProfile>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to read profile from disk: {ex.Message}. Recreating default profile.");
            }

            if (loaded != null && loaded.Inventory != null)
            {
                CurrentProfile = loaded;

                // Migrate legacy default name if present
                if (string.IsNullOrWhiteSpace(CurrentProfile.PlayerName) || CurrentProfile.PlayerName == "Cometa Champion")
                {
                    CurrentProfile.PlayerName = PlayerProfile.GenerateDefaultPlayerName();
                    SaveProfile();
                }

                Debug.Log($"[SaveManager] Successfully loaded player profile from {SaveFilePath}. (Player: {CurrentProfile.PlayerName}, Gold: {CurrentProfile.GoldCoins}, Dust: {CurrentProfile.EsenciaCometa})");
            }
            else
            {
                // Create and save default profile only if file didn't exist or JSON was corrupt
                CurrentProfile = PlayerProfile.CreateDefaultProfile();
                SaveProfile();
            }

            // Fire event outside the disk loading try-catch block so subscriber errors cannot overwrite save
            try
            {
                OnProfileLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error in OnProfileLoaded subscriber: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SaveProfile()
        {
            try
            {
                if (CurrentProfile == null) return;

                string json = JsonConvert.SerializeObject(CurrentProfile, Formatting.Indented);
                string directory = Path.GetDirectoryName(SaveFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Player profile saved successfully to {SaveFilePath}.");
                OnProfileSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error saving player profile: {ex.Message}");
            }
        }

        public void ResetToDefaultProfile()
        {
            CurrentProfile = PlayerProfile.CreateDefaultProfile();
            SaveProfile();
            Debug.Log("[SaveManager] Profile reset to default values.");
            OnProfileLoaded?.Invoke();
        }

        public static void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log($"[SaveManager] Save file deleted successfully from: {SaveFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to delete save file: {ex.Message}");
            }
        }

        public void AddCoins(int amount)
        {
            if (CurrentProfile == null) return;
            CurrentProfile.GoldCoins = Math.Max(0, CurrentProfile.GoldCoins + amount);
            SaveProfile();
        }

        public void AddDust(int amount)
        {
            if (CurrentProfile == null) return;
            CurrentProfile.EsenciaCometa = Math.Max(0, CurrentProfile.EsenciaCometa + amount);
            SaveProfile();
        }

        public bool UpdatePlayerName(string newName, out string failureReason)
        {
            failureReason = string.Empty;
            if (CurrentProfile == null)
            {
                failureReason = "No hay perfil activo cargado.";
                return false;
            }

            if (CurrentProfile.TrySetPlayerName(newName, out failureReason))
            {
                SaveProfile();
                return true;
            }

            return false;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveProfile();
        }

        private void OnApplicationQuit()
        {
            SaveProfile();
        }
    }
}
