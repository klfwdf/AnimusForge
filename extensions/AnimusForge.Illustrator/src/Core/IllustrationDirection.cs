using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.Core
{
    public sealed class IllustrationDirection
    {
        public string Prompt { get; internal set; } = string.Empty;
        public string Title { get; internal set; } = string.Empty;
        public string Theme { get; internal set; } = string.Empty;
        public string ActionSummary { get; internal set; } = string.Empty;
        public double? SceneYawDegrees { get; internal set; }
        public double? ScenePitchDegrees { get; internal set; }
        public double? SceneHorizontalFovDegrees { get; internal set; }
        public double? AuxiliarySceneYawDegrees { get; internal set; }
        public double? AuxiliaryScenePitchDegrees { get; internal set; }
        public double? AuxiliarySceneHorizontalFovDegrees { get; internal set; }
        // Generation metadata is stored/displayed separately and never appended to Prompt.
        public string DirectionStatus { get; internal set; } = "complete";
        public string FallbackReason { get; internal set; } = string.Empty;
        public string FinishReason { get; internal set; } = string.Empty;
        public bool UsedLocalFallback { get; internal set; }
        public bool VisionUnsupported { get; internal set; }
        // True for a successful director call without images, including an intentional
        // vision-off setting. This is availability metadata, not a provider failure.
        public bool UsedTextOnlyDirector { get; internal set; }
        public int? PromptTokens { get; internal set; }
        public int? CompletionTokens { get; internal set; }
        public int? TotalTokens { get; internal set; }
        public string StatusText
        {
            get
            {
                if (DirectionStatus == "failed") return FallbackReason + ". Image generation stopped.";
                if (DirectionStatus == "truncated") return "The director output was truncated. Image generation stopped.";
                if (UsedLocalFallback || DirectionStatus == "local_fallback")
                    return (VisionUnsupported ? "The director does not support vision; " : string.Empty) + LocalFallbackStatusReason() + ". A local composition was used";
                if (VisionUnsupported || DirectionStatus == "vision_unsupported")
                    return "The director does not support vision. Text-only conception was used.";
                return string.IsNullOrWhiteSpace(FinishReason)
                    ? "The director body was validated (the interface did not return a finish marker)" : "Director conception complete";
            }
        }

        private string LocalFallbackStatusReason()
        {
            // Keep UI reasons bounded and selected locally, never expose a provider's raw error body.
            string reason = FallbackReason ?? string.Empty;
            if (reason.IndexOf(IllustratorText.Raw("Direction_709"), StringComparison.Ordinal) >= 0 || reason.IndexOf("The director is disabled", StringComparison.Ordinal) >= 0) return "The director is disabled";
            if (reason.IndexOf(IllustratorText.Raw("Direction_711"), StringComparison.Ordinal) >= 0 || reason.IndexOf("The director interface is not configured", StringComparison.Ordinal) >= 0) return "The director interface is not configured";
            if (reason.IndexOf(IllustratorText.Raw("Direction_713"), StringComparison.Ordinal) >= 0 || reason.IndexOf("unsupported shields", StringComparison.Ordinal) >= 0) return "The director body violates character or equipment constraints";
            if (reason.IndexOf(IllustratorText.Raw("Direction_715"), StringComparison.Ordinal) >= 0 || reason.IndexOf("repeated the narrative", StringComparison.Ordinal) >= 0) return "The director body was not translated into a visual description";
            if (reason.IndexOf(IllustratorText.Raw("Direction_717"), StringComparison.Ordinal) >= 0 || reason.IndexOf("lacks complete scene", StringComparison.Ordinal) >= 0) return "The director body has an unsuitable structure or composition";
            if (reason.IndexOf(IllustratorText.Raw("Direction_719"), StringComparison.Ordinal) >= 0 || reason.IndexOf("empty body", StringComparison.Ordinal) >= 0) return "The director body lacks a visual description";
            return "The director body is unusable";
        }

        internal static IllustrationDirection SplitMetadata(string output)
        {
            var result = new IllustrationDirection();
            int titleLimit = int.TryParse(IllustratorText.Get("Direction_TitleMaxCharacters"), out int configuredTitleLimit)
                ? Math.Max(18, Math.Min(96, configuredTitleLimit)) : 18;
            result.Prompt = Regex.Replace(output ?? string.Empty,
                IllustratorText.Raw("Direction_722"), match =>
                {
                    string label = match.Groups[1].Value;
                    if (label == IllustratorText.Raw("Direction_723") || label == IllustratorText.Raw("Direction_724"))
                    {
                        ReadSceneFraming(result, match.Groups[2].Value, label == IllustratorText.Raw("Direction_725"));
                        return string.Empty;
                    }
                    // Theme is shown in a wrapping/scrolling viewport. Keep its full sentence
                    // in metadata instead of permanently cutting it off before disk save.
                    string value = CleanLabel(match.Groups[2].Value, label == IllustratorText.Raw("Direction_726") ? titleLimit : label == IllustratorText.Raw("Direction_727") ? int.MaxValue : 160);
                    if (label == IllustratorText.Raw("Direction_728")) result.Title = value;
                    else if (label == IllustratorText.Raw("Direction_729")) result.Theme = value;
                    else result.ActionSummary = value;
                    return string.Empty;
                }).Trim();
            return result;
        }

        private static void ReadSceneFraming(IllustrationDirection direction, string value, bool auxiliary)
        {
            // This selects a rectilinear environment reference, not character placement.
            var match = Regex.Match(value ?? string.Empty,
                @"^\s*[:：]?\s*yaw\s*=\s*(?<yaw>[+-]?(?:\d+(?:\.\d*)?|\.\d+))\s*;\s*pitch\s*=\s*(?<pitch>[+-]?(?:\d+(?:\.\d*)?|\.\d+))\s*;\s*hfov\s*=\s*(?<fov>[+-]?(?:\d+(?:\.\d*)?|\.\d+))\s*;?\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            double yaw, pitch, fov;
            if (!match.Success ||
                !double.TryParse(match.Groups["yaw"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out yaw) ||
                !double.TryParse(match.Groups["pitch"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out pitch) ||
                !double.TryParse(match.Groups["fov"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out fov) ||
                double.IsNaN(yaw) || double.IsInfinity(yaw) || yaw < -180 || yaw > 180 ||
                double.IsNaN(pitch) || double.IsInfinity(pitch) || pitch < -60 || pitch > 60 ||
                double.IsNaN(fov) || double.IsInfinity(fov) || fov < 45 || fov > 100)
                return;
            if (auxiliary)
            {
                direction.AuxiliarySceneYawDegrees = yaw;
                direction.AuxiliaryScenePitchDegrees = pitch;
                direction.AuxiliarySceneHorizontalFovDegrees = fov;
            }
            else
            {
                direction.SceneYawDegrees = yaw;
                direction.ScenePitchDegrees = pitch;
                direction.SceneHorizontalFovDegrees = fov;
            }
        }

        private static string CleanLabel(string value, int limit)
        {
            // Plain UI labels, never interpreted as rich-text tags or picture text.
            value = Regex.Replace(value ?? string.Empty, @"<[^>]*>|\{[^}]*\}", string.Empty);
            value = Regex.Replace(value, @"\s+", " ").Trim().Trim('"', '“', '”', '`');
            return value.Length > limit ? value.Substring(0, limit) : value;
        }

        internal static string ExtractActionSummary(string prompt)
        {
            var match = Regex.Match(prompt ?? string.Empty, IllustratorText.Raw("Direction_730"));
            string section = Regex.Replace(match.Groups[1].Value, @"\s+", " ").Trim();
            // Old prompts often put hand placement after the equipment description.
            return section.Length <= 360 ? section : section.Substring(0, 180) + "…" + section.Substring(section.Length - 179);
        }

        internal static string BuildActionHistory(IEnumerable<CachedIllustrationItem> items, bool eventAnchored = false, bool weeklyReport = false)
        {
            var summaries = (items ?? Enumerable.Empty<CachedIllustrationItem>())
                // Local-fallback pictures carry the fixed template, not a designed action.
                .Where(i => i != null && !i.Deleted && !IsLocalFallback(i)).OrderByDescending(i => i.CreatedTime).Take(3)
                .Select(i => string.IsNullOrWhiteSpace(i.ActionSummary) ? ExtractActionSummary(i.Prompt) : i.ActionSummary)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => CleanLabel(s, 360)).Distinct().ToArray();
            return summaries.Length == 0 ? string.Empty :
                IllustratorText.Get("Direction_731") + string.Join("\n", summaries) +
                (weeklyReport
                    ? IllustratorText.Get("Direction_732")
                    : eventAnchored
                    ? IllustratorText.Get("Direction_733")
                    : IllustratorText.Get("Direction_734")) +
                IllustratorText.Raw("Direction_735");
        }

        internal static bool IsLocalFallback(CachedIllustrationItem item)
        {
            return string.Equals(item?.DirectorStatus, "local_fallback", StringComparison.Ordinal);
        }

        internal static string RemoveActionHistory(string artDirection)
        {
            string value = artDirection ?? string.Empty;
            value = Regex.Replace(value, IllustratorText.Raw("Direction_736"), string.Empty);
            // Accept callers with the earlier unframed format without consuming later sections.
            return Regex.Replace(value, IllustratorText.Raw("Direction_737"), string.Empty).Trim();
        }

        internal static string ReadEventActionHistory(string campaignKey, string subjectKey, string category)
        {
            return BuildActionHistory(DiskImageCacheManager.GetAllCachedIllustrations(campaignKey)
                .Where(i => string.Equals(i.SubjectKey, subjectKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase)), true,
                string.Equals(category, "weekly_report", StringComparison.OrdinalIgnoreCase));
        }
    }
}
