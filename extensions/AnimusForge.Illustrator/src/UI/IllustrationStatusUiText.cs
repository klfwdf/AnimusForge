using System;
using System.Text.RegularExpressions;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.UI
{
    // Presentation projections only. Raw director, cache and capture metadata stay unchanged.
    internal static class IllustrationStatusUiText
    {
        private static readonly Regex SceneBudget = new Regex(IllustratorText.Raw("Residual_IllustrationStatusUiText_cbe2dc408a8b"), RegexOptions.CultureInvariant);

        internal static string Bulletin(BulletinIllustrationPreloader.Job job)
        {
            if (job == null) return string.Empty;
            switch (job.DisplayCode)
            {
                case "preparing": return IllustratorUiText.BulletinPreparing;
                case "discarded": return IllustratorUiText.BulletinDiscarded;
                case "cancelled": return IllustratorUiText.BulletinCancelled;
                case "missing": return IllustratorUiText.BulletinMissing;
                case "redraw_cancelled": return IllustratorUiText.BulletinRedrawCancelled;
                case "redraw_complete": return IllustratorUiText.BulletinRedrawComplete;
                case "deleted": return IllustratorUiText.WeeklyDeleted;
                case "prepare_failed": return IllustratorUiText.BulletinPrepareFailed(job.DisplayDetail);
                case "redraw_failed": return IllustratorUiText.WeeklyRedrawFailed(job.DisplayDetail ?? IllustratorUiText.BulletinSaveImageFailed);
                case "image_failed": return IllustratorUiText.BulletinImageFailed(job.DisplayDetail ?? IllustratorUiText.BulletinSaveUnavailable);
                case "retry": return IllustratorUiText.BulletinRetry(job.DisplayDetail);
                case "cached": return job.DisplayItem != null ? Cached(job.DisplayItem) : job.Status;
                default: return job.Status;
            }
        }

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

        internal static string DisplayTheme(string raw)
        {
            return string.Equals(raw, IllustratorText.Raw("Residual_IllustrationStatusUiText_ade70c013f11"), StringComparison.Ordinal)
                ? IllustratorUiText.Resolve("AF_ILL_UI_DEFAULT_THEME", "Characters and circumstances") : raw;
        }

        internal static string Cached(CachedIllustrationItem item, string styleFingerprint = null, bool checkStyle = false)
        {
            if (item == null) return string.Empty;
            string display = string.IsNullOrWhiteSpace(item.Theme)
                ? IllustratorUiText.StatusChronicle
                : IllustratorUiText.StatusTheme(DisplayTheme(item.Theme));
            if (!string.IsNullOrWhiteSpace(item.DirectorStatus) && item.DirectorStatus != "complete" && !string.IsNullOrWhiteSpace(item.DirectorStatusText))
                display += "\n" + CachedDirector(item.DirectorStatus, item.DirectorStatusText, item.DirectorFallbackReason);
            if (checkStyle && !DiskImageCacheManager.IsStyleCompatible(item, styleFingerprint))
                display += "\n" + IllustratorUiText.StatusStyleChanged;
            return display;
        }

        private static string CachedDirector(string status, string raw, string reason)
        {
            if (status == "failed" && (raw == (reason ?? string.Empty) + IllustratorText.Raw("Residual_IllustrationStatusUiText_10fb0edab25d") || raw == (reason ?? string.Empty) + ". Image generation stopped.")) return Failed(reason);
            if (status == "truncated" && (raw == IllustratorText.Raw("Residual_IllustrationStatusUiText_49d596298256") || raw == "The director output was truncated. Image generation stopped.")) return Truncated;
            if (status == "vision_unsupported" && (raw == IllustratorText.Raw("Residual_IllustrationStatusUiText_761f454d6192") || raw == "The director does not support vision. Text-only conception was used.")) return VisionUnsupported;
            return status == "local_fallback" ? KnownLocalFallback(raw) : raw;
        }

        private static string KnownLocalFallback(string raw)
        {
            string visionPrefix = IllustratorText.Raw("Residual_IllustrationStatusUiText_4d674f005f6e");
            const string englishVisionPrefix = "The director does not support vision; ";
            bool chineseVision = raw != null && raw.StartsWith(visionPrefix, StringComparison.Ordinal);
            bool englishVision = raw != null && raw.StartsWith(englishVisionPrefix, StringComparison.Ordinal);
            bool vision = chineseVision || englishVision;
            string body = chineseVision ? raw.Substring(visionPrefix.Length) : englishVision ? raw.Substring(englishVisionPrefix.Length) : raw;
            string reason;
            switch (IllustratorText.NormalizeLegacyCase(body))
            {
                case "AFICASE0f18bd4735d56b6e":
                case "The director is disabled. A local composition was used": reason = IllustratorUiText.StatusDirectorDisabled; break;
                case "AFICASEa01eeb634c60faff":
                case "The director interface is not configured. A local composition was used": reason = IllustratorUiText.StatusDirectorUnconfigured; break;
                case "AFICASEfd4f3116db18cbda":
                case "The director body violates character or equipment constraints. A local composition was used": reason = IllustratorUiText.StatusDirectorConstraint; break;
                case "AFICASE77f3d1765c9838f7":
                case "The director body was not translated into a visual description. A local composition was used": reason = IllustratorUiText.StatusDirectorNarrative; break;
                case "AFICASE3c08919c06430fe7":
                case "The director body has an unsuitable structure or composition. A local composition was used": reason = IllustratorUiText.StatusDirectorStructure; break;
                case "AFICASEac7bd50a6013b00d":
                case "The director body lacks a visual description. A local composition was used": reason = IllustratorUiText.StatusDirectorEmpty; break;
                case "AFICASE9807866671a11e30":
                case "The director body is unusable. A local composition was used": reason = IllustratorUiText.StatusDirectorUnavailable; break;
                default: return raw;
            }
            return (vision ? IllustratorUiText.StatusVisionPrefix : string.Empty) + IllustratorUiText.StatusLocalFallback(reason);
        }

        internal static string Scene(string raw)
        {
            switch (IllustratorText.NormalizeLegacyCase(raw))
            {
                case "AFICASE6bab834a93172e19": return IllustratorUiText.StatusSceneTerrain;
                case "AFICASE9b51875a1934a3b6": return IllustratorUiText.StatusSceneDisabledScreenshot;
                case "AFICASE1f88a73b3f5e8a7d": return IllustratorUiText.StatusSceneDisabledFacts;
                case "AFICASE226ef240347eeb15": return IllustratorUiText.StatusScenePanorama;
                case "AFICASE8989e163cce94d4b": return IllustratorUiText.StatusScenePanoramaNoScreenshot;
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
