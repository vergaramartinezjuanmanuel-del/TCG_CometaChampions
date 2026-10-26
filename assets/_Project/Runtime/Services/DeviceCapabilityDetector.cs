#nullable enable
using System;
using UnityEngine;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Collects hardware and display capabilities using supported Unity APIs once at startup.
    /// Safely handles missing, zero, or unreliable values without executing expensive startup benchmarks.
    /// </summary>
    public static class DeviceCapabilityDetector
    {
        private static DeviceCapabilities? cachedCapabilities;

        /// <summary>
        /// Returns the cached hardware capability snapshot, or collects it from Unity SystemInfo/Screen on first access.
        /// </summary>
        public static DeviceCapabilities Detect(bool forceRefresh = false)
        {
            if (cachedCapabilities != null && !forceRefresh)
            {
                return cachedCapabilities;
            }

            int systemMemoryMB = SafeGet(() => SystemInfo.systemMemorySize, 0);
            int graphicsMemoryMB = SafeGet(() => SystemInfo.graphicsMemorySize, 0);
            int processorCount = SafeGet(() => SystemInfo.processorCount, 1);
            int processorFrequencyMHz = SafeGet(() => SystemInfo.processorFrequency, 0);
            string graphicsDeviceName = SafeGet(() => SystemInfo.graphicsDeviceName, "Unknown GPU") ?? "Unknown GPU";
            string graphicsDeviceVendor = SafeGet(() => SystemInfo.graphicsDeviceVendor, "Unknown Vendor") ?? "Unknown Vendor";
            string graphicsApiType = SafeGet(() => SystemInfo.graphicsDeviceType.ToString(), "Unknown API") ?? "Unknown API";
            int maxTextureSize = SafeGet(() => SystemInfo.maxTextureSize, 4096);
            int screenWidth = SafeGet(() => Screen.width, 1920);
            int screenHeight = SafeGet(() => Screen.height, 1080);
            float screenDpi = SafeGet(() => Screen.dpi, 0f);
            int shaderLevel = SafeGet(() => SystemInfo.graphicsShaderLevel, 35);
            bool supportsCompute = SafeGet(() => SystemInfo.supportsComputeShaders, false);
            bool supportsInstancing = SafeGet(() => SystemInfo.supportsInstancing, true);
            bool supportsAstc = SafeGet(() => SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6), true);
            bool supportsRT = true; // Always true in Unity 6+
            bool supportsDynRes = SafeGet(() => SystemInfo.supportsGpuRecorder, false);

            cachedCapabilities = new DeviceCapabilities(
                systemMemoryMB: systemMemoryMB,
                graphicsMemoryMB: graphicsMemoryMB,
                processorCount: processorCount,
                processorFrequencyMHz: processorFrequencyMHz,
                graphicsDeviceName: graphicsDeviceName,
                graphicsDeviceVendor: graphicsDeviceVendor,
                graphicsApiType: graphicsApiType,
                maxTextureSize: maxTextureSize,
                screenWidth: screenWidth,
                screenHeight: screenHeight,
                screenDpi: screenDpi,
                shaderLevel: shaderLevel,
                supportsComputeShaders: supportsCompute,
                supportsInstancing: supportsInstancing,
                supportsAstcCompression: supportsAstc,
                supportsRenderTextures: supportsRT,
                supportsDynamicResolution: supportsDynRes);

            return cachedCapabilities;
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch
            {
                return fallback;
            }
        }
    }
}
