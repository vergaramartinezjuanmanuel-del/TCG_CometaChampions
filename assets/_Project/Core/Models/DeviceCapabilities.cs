#nullable enable
using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Immutable snapshot of hardware and display characteristics collected at startup.
    /// Sanitizes missing, zero, or unreliable values reported by Android drivers.
    /// </summary>
    [Serializable]
    public sealed class DeviceCapabilities
    {
        public int SystemMemoryMB { get; }
        public int GraphicsMemoryMB { get; }
        public int ProcessorCount { get; }
        public int ProcessorFrequencyMHz { get; }
        public string GraphicsDeviceName { get; }
        public string GraphicsDeviceVendor { get; }
        public string GraphicsApiType { get; }
        public int MaxTextureSize { get; }
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }
        public float ScreenDpi { get; }
        public int ShaderLevel { get; }
        public bool SupportsComputeShaders { get; }
        public bool SupportsInstancing { get; }
        public bool SupportsAstcCompression { get; }
        public bool SupportsRenderTextures { get; }
        public bool SupportsDynamicResolution { get; }

        public bool HasReliableSystemMemory => SystemMemoryMB > 256;
        public bool HasReliableGpuMemory => GraphicsMemoryMB > 128 && GraphicsMemoryMB <= 32768;
        public bool HasReliableCpuCount => ProcessorCount > 0 && ProcessorCount <= 64;
        public bool HasReliableCpuFrequency => ProcessorFrequencyMHz >= 400 && ProcessorFrequencyMHz <= 8000;
        public bool HasReliableScreenDimensions => ScreenWidth > 0 && ScreenHeight > 0;
        public bool HasReliableDpi => ScreenDpi >= 72f && ScreenDpi <= 1200f;

        public int ShortEdgePixels => HasReliableScreenDimensions ? Math.Min(ScreenWidth, ScreenHeight) : 1080;
        public int LongEdgePixels => HasReliableScreenDimensions ? Math.Max(ScreenWidth, ScreenHeight) : 1920;
        public long TotalScreenPixels => HasReliableScreenDimensions ? (long)ScreenWidth * ScreenHeight : 1920L * 1080L;

        public DeviceCapabilities(
            int systemMemoryMB,
            int graphicsMemoryMB,
            int processorCount,
            int processorFrequencyMHz,
            string? graphicsDeviceName,
            string? graphicsDeviceVendor,
            string? graphicsApiType,
            int maxTextureSize,
            int screenWidth,
            int screenHeight,
            float screenDpi,
            int shaderLevel = 45,
            bool supportsComputeShaders = true,
            bool supportsInstancing = true,
            bool supportsAstcCompression = true,
            bool supportsRenderTextures = true,
            bool supportsDynamicResolution = false)
        {
            SystemMemoryMB = Math.Max(0, systemMemoryMB);
            GraphicsMemoryMB = Math.Max(0, graphicsMemoryMB);
            ProcessorCount = Math.Max(0, processorCount);
            ProcessorFrequencyMHz = Math.Max(0, processorFrequencyMHz);
            GraphicsDeviceName = string.IsNullOrWhiteSpace(graphicsDeviceName) ? "Unknown GPU" : graphicsDeviceName!.Trim();
            GraphicsDeviceVendor = string.IsNullOrWhiteSpace(graphicsDeviceVendor) ? "Unknown Vendor" : graphicsDeviceVendor!.Trim();
            GraphicsApiType = string.IsNullOrWhiteSpace(graphicsApiType) ? "Unknown API" : graphicsApiType!.Trim();
            MaxTextureSize = Math.Max(0, maxTextureSize);
            ScreenWidth = Math.Max(0, screenWidth);
            ScreenHeight = Math.Max(0, screenHeight);
            ScreenDpi = float.IsNaN(screenDpi) || float.IsInfinity(screenDpi) || screenDpi < 0f ? 0f : screenDpi;
            ShaderLevel = Math.Max(0, shaderLevel);
            SupportsComputeShaders = supportsComputeShaders;
            SupportsInstancing = supportsInstancing;
            SupportsAstcCompression = supportsAstcCompression;
            SupportsRenderTextures = supportsRenderTextures;
            SupportsDynamicResolution = supportsDynamicResolution;
        }

        public override string ToString()
        {
            return $"RAM={SystemMemoryMB}MB (Valid={HasReliableSystemMemory}), " +
                   $"VRAM={GraphicsMemoryMB}MB (Reliable={HasReliableGpuMemory}), " +
                   $"CPU={ProcessorCount} cores @ {ProcessorFrequencyMHz}MHz, " +
                   $"GPU='{GraphicsDeviceName}' ({GraphicsDeviceVendor}, {GraphicsApiType}, ShaderLvl={ShaderLevel}, MaxTex={MaxTextureSize}), " +
                   $"Display={ScreenWidth}x{ScreenHeight} @ {ScreenDpi:F0}dpi";
        }
    }
}
