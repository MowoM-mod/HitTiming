
using System;
using System.Collections.Generic;
using System.IO;
using IPA.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace HitTiming
{
    internal sealed class TimingThreshold
    {
        public float? MaxMs { get; set; }
        public string Color { get; set; } = "#FFFFFF";
    }

    internal sealed class TimingConfiguration
    {
        public bool Enabled { get; set; } = true;
        public float OffsetX { get; set; } = 0.0f;
        public float OffsetY { get; set; } = -0.5f;
        public float OffsetZ { get; set; } = -2.0f;
        public float FontSizeMultiplier { get; set; } = 1.8f;

        public float HoldTime { get; set; } = 0.3f;
        public float FadeTime { get; set; } = 0.4f;
        public float FallSpeed { get; set; } = 3.4f;
        public string FallAxis { get; set; } = "Z";

        public bool NormalNotesOnly { get; set; } = true;
        public float CorrectionDistance { get; set; } = 1.0f;

        public List<TimingThreshold> Thresholds { get; set; } = new List<TimingThreshold>
        {
            new TimingThreshold { MaxMs = 2f, Color = "#FFFFFF" },
            new TimingThreshold { MaxMs = 4f, Color = "#2D9DAD" },
            new TimingThreshold { MaxMs = 8f, Color = "#FFFF00" },
            new TimingThreshold { MaxMs = 15f, Color = "#FF8000" },
            new TimingThreshold { MaxMs = 25f, Color = "#FF0000" },
            new TimingThreshold { MaxMs = 35f, Color = "#8B0000" },
            new TimingThreshold { MaxMs = null, Color = "#8B0000" }
        };

        internal static TimingConfiguration Current { get; private set; } =
            new TimingConfiguration();

        internal static void Load(string path, IPA.Logging.Logger logger)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                if (File.Exists(path))
                {
                    var loaded = JsonConvert.DeserializeObject<TimingConfiguration>(
                        File.ReadAllText(path));

                    if (loaded != null)
                        Current = loaded;
                }
                else
                {
                    File.WriteAllText(
                        path,
                        JsonConvert.SerializeObject(Current, Formatting.Indented));
                }

                logger.Info($"HitTiming configuration loaded: {path}");
            }
            catch (Exception exception)
            {
                logger.Error($"Could not load HitTiming configuration: {exception}");
                Current = new TimingConfiguration();
            }
        }

        internal Color GetColor(float absoluteDeviationMs)
        {
            if (Thresholds == null || Thresholds.Count == 0)
                return Color.white;

            foreach (var threshold in Thresholds)
            {
                if (!ColorUtility.TryParseHtmlString(
                    threshold.Color,
                    out var parsed))
                {
                    parsed = Color.white;
                }

                if (!threshold.MaxMs.HasValue ||
                    absoluteDeviationMs <= threshold.MaxMs.Value)
                {
                    return parsed;
                }
            }

            return Color.red;
        }
    }
}
