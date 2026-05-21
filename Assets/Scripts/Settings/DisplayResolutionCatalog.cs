using System.Collections.Generic;
using UnityEngine;

/// <summary>常见窗口/全屏分辨率预设。</summary>
public static class DisplayResolutionCatalog
{
    public readonly struct ResolutionPreset
    {
        public readonly int Width;
        public readonly int Height;
        public readonly string Label;

        public ResolutionPreset(int width, int height)
        {
            Width = width;
            Height = height;
            Label = $"{width} × {height}";
        }
    }

    static readonly ResolutionPreset[] Presets =
    {
        new(3840, 2160),
        new(2560, 1440),
        new(1920, 1080),
        new(1680, 1050),
        new(1600, 900),
        new(1366, 768),
        new(1280, 720),
        new(1280, 960),
        new(1024, 768),
    };

    public static IReadOnlyList<ResolutionPreset> All => Presets;

    public static int Count => Presets.Length;

    public static ResolutionPreset GetPreset(int index)
    {
        if (Presets.Length == 0)
            return new ResolutionPreset(1920, 1080);
        index = Mathf.Clamp(index, 0, Presets.Length - 1);
        return Presets[index];
    }

    public static int FindClosestPresetIndex(int width, int height)
    {
        int best = 0;
        long bestDist = long.MaxValue;
        for (int i = 0; i < Presets.Length; i++)
        {
            long dw = Presets[i].Width - width;
            long dh = Presets[i].Height - height;
            long dist = dw * dw + dh * dh;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    public static void ApplyPresetToData(GameSettingsData data, int presetIndex)
    {
        if (data == null) return;
        var preset = GetPreset(presetIndex);
        data.resolutionPresetIndex = Mathf.Clamp(presetIndex, 0, Count - 1);
        data.screenWidth = preset.Width;
        data.screenHeight = preset.Height;
    }
}
