#nullable enable
using UnityEngine;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Central service to retrieve the game version string formatted strictly according to project standards:
    /// "v" + Version + "." + Bundle Version Code (e.g., "v1.0.0.2").
    /// </summary>
    public static class GameVersionService
    {
        private static string? cachedFormattedVersion;

        /// <summary>
        /// Returns the formatted version string matching: "v" + Version + "." + Bundle Version Code
        /// </summary>
        public static string GetFormattedVersion()
        {
            if (!string.IsNullOrEmpty(cachedFormattedVersion))
            {
                return cachedFormattedVersion!;
            }

            string version = Application.version;
            string bundleVersionCode = GetBundleVersionCode();
            cachedFormattedVersion = $"v{version}.{bundleVersionCode}";
            return cachedFormattedVersion!;
        }

        /// <summary>
        /// Retrieves the platform bundle version code (e.g., Android bundleVersionCode / versionCode).
        /// In Editor: reads PlayerSettings.Android.bundleVersionCode.
        /// On Android Device: reads Android PackageManager packageInfo versionCode dynamically.
        /// Fallback: returns "2" (matching project default).
        /// </summary>
        public static string GetBundleVersionCode()
        {
#if UNITY_EDITOR
            return UnityEditor.PlayerSettings.Android.bundleVersionCode.ToString();
#elif UNITY_ANDROID
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                string packageName = currentActivity.Call<string>("getPackageName");
                using var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);

                long versionCode = -1;
                try
                {
                    versionCode = packageInfo.Call<long>("getLongVersionCode");
                }
                catch
                {
                    versionCode = packageInfo.Get<int>("versionCode");
                }

                if (versionCode > 0)
                {
                    return versionCode.ToString();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameVersionService] Could not dynamically retrieve Android versionCode: {ex.Message}");
            }
            return "4";
#else
            return "4";
#endif
        }
    }
}
