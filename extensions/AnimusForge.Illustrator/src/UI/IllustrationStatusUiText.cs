using System;
using System.Text.RegularExpressions;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.UI
{
    // Presentation projections only. Raw director, cache and capture metadata stay unchanged.
    internal static class IllustrationStatusUiText
    {
        private static readonly Regex SceneBudget = new Regex(@"^环境物件超过([0-9]+)个预算，已跳过环境采集，继续绘制$", RegexOptions.CultureInvariant);

        internal static string Direction(IllustrationDirection direction)
        {
            if (direction == null) return string.Empty;
            string raw = direction.StatusText;
            if (direction.DirectionStatus == "failed")
                return Failed(direction.FallbackReason);
            if (direction.DirectionStatus == "truncated") return Truncated;
            if (direction.UsedLocalFallback || direction.DirectionStatus == "local_fallback")
                return KnownLocalFallback(raw);
            if (direction.VisionUnsupported || direction.DirectionStatus == "vision_unsupported") return VisionUnsupported;
            if (direction.DirectionStatus != "complete") return raw;
            return string.IsNullOrWhiteSpace(direction.FinishReason) ? Validated : Complete;
        }

        internal static string Cached(CachedIllustrationItem item, string styleFingerprint = null, bool checkStyle = false)
        {
            if (item == null) return string.Empty;
            string display = string.IsNullOrWhiteSpace(item.Theme)
                ? IllustratorUiText.StatusChronicle
                : IllustratorUiText.StatusTheme(item.Theme);
            if (!string.IsNullOrWhiteSpace(item.DirectorStatus) && item.DirectorStatus != "complete" && !string.IsNullOrWhiteSpace(item.DirectorStatusText))
                display += "\n" + CachedDirector(item.DirectorStatus, item.DirectorStatusText, item.DirectorFallbackReason);
            if (checkStyle && !DiskImageCacheManager.IsStyleCompatible(item, styleFingerprint))
                display += "\n" + IllustratorUiText.StatusStyleChanged;
            return display;
        }

        private static string CachedDirector(string status, string raw, string reason)
        {
            if (status == "failed" && raw == (reason ?? string.Empty) + "，已停止生图") return Failed(reason);
            if (status == "truncated" && raw == "导演输出截断，已停止生图") return Truncated;
            if (status == "vision_unsupported" && raw == "导演不支持识图，已使用文字构思") return VisionUnsupported;
            return status == "local_fallback" ? KnownLocalFallback(raw) : raw;
        }

        private static string KnownLocalFallback(string raw)
        {
            const string visionPrefix = "导演不支持识图；";
            bool vision = raw != null && raw.StartsWith(visionPrefix, StringComparison.Ordinal);
            string body = vision ? raw.Substring(visionPrefix.Length) : raw;
            string reason;
            switch (body)
            {
                case "导演已关闭，已使用本地构图": reason = IllustratorUiText.StatusDirectorDisabled; break;
                case "导演接口未配置，已使用本地构图": reason = IllustratorUiText.StatusDirectorUnconfigured; break;
                case "导演正文违反人物或装备约束，已使用本地构图": reason = IllustratorUiText.StatusDirectorConstraint; break;
                case "导演正文未转成画面描述，已使用本地构图": reason = IllustratorUiText.StatusDirectorNarrative; break;
                case "导演正文结构或构图不合要求，已使用本地构图": reason = IllustratorUiText.StatusDirectorStructure; break;
                case "导演正文缺少画面描述，已使用本地构图": reason = IllustratorUiText.StatusDirectorEmpty; break;
                case "导演正文不可用，已使用本地构图": reason = IllustratorUiText.StatusDirectorUnavailable; break;
                default: return raw;
            }
            return (vision ? IllustratorUiText.StatusVisionPrefix : string.Empty) + IllustratorUiText.StatusLocalFallback(reason);
        }

        internal static string Scene(string raw)
        {
            switch (raw)
            {
                case "野外对话：按地形事实构图（不截图）": return IllustratorUiText.StatusSceneTerrain;
                case "场景全景已关闭：使用当前画面截图": return IllustratorUiText.StatusSceneDisabledScreenshot;
                case "场景全景已关闭：按文字事实构图": return IllustratorUiText.StatusSceneDisabledFacts;
                case "任务场景：附近30米全景参考可用": return IllustratorUiText.StatusScenePanorama;
                case "任务场景：附近30米全景参考可用；当前位置截图未取得": return IllustratorUiText.StatusScenePanoramaNoScreenshot;
                default:
                    var match = SceneBudget.Match(raw ?? string.Empty);
                    return match.Success ? IllustratorUiText.StatusSceneBudget(match.Groups[1].Value) : raw;
            }
        }

        private static string Failed(string reason) => IllustratorUiText.StatusDirectorFailed(reason);
        private static string Truncated => IllustratorUiText.StatusDirectorTruncated;
        private static string VisionUnsupported => IllustratorUiText.StatusDirectorTextOnly;
        private static string Validated => IllustratorUiText.StatusDirectorValidated;
        private static string Complete => IllustratorUiText.StatusDirectorComplete;
    }
}
