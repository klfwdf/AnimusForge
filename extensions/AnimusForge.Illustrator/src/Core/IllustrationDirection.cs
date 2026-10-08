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
                if (DirectionStatus == "failed") return FallbackReason + "，已停止生图";
                if (DirectionStatus == "truncated") return "导演输出截断，已停止生图";
                if (UsedLocalFallback || DirectionStatus == "local_fallback")
                    return (VisionUnsupported ? "导演不支持识图；" : string.Empty) + LocalFallbackStatusReason() + "，已使用本地构图";
                if (VisionUnsupported || DirectionStatus == "vision_unsupported")
                    return "导演不支持识图，已使用文字构思";
                return string.IsNullOrWhiteSpace(FinishReason)
                    ? "导演正文已校验（接口未返回结束标记）" : "导演构思完成";
            }
        }

        private string LocalFallbackStatusReason()
        {
            // Keep UI reasons bounded and selected locally, never expose a provider's raw error body.
            string reason = FallbackReason ?? string.Empty;
            if (reason.IndexOf("导演已关闭", StringComparison.Ordinal) >= 0) return "导演已关闭";
            if (reason.IndexOf("导演接口未配置", StringComparison.Ordinal) >= 0) return "导演接口未配置";
            if (reason.IndexOf("无依据盾牌", StringComparison.Ordinal) >= 0) return "导演正文违反人物或装备约束";
            if (reason.IndexOf("复述叙事原文", StringComparison.Ordinal) >= 0) return "导演正文未转成画面描述";
            if (reason.IndexOf("缺少完整场景", StringComparison.Ordinal) >= 0) return "导演正文结构或构图不合要求";
            if (reason.IndexOf("空正文", StringComparison.Ordinal) >= 0) return "导演正文缺少画面描述";
            return "导演正文不可用";
        }

        internal static IllustrationDirection SplitMetadata(string output)
        {
            var result = new IllustrationDirection();
            result.Prompt = Regex.Replace(output ?? string.Empty,
                @"【(画作标题|画作主题|人物行动|环境取景|环境辅助取景)】([^【]*)", match =>
                {
                    string label = match.Groups[1].Value;
                    if (label == "环境取景" || label == "环境辅助取景")
                    {
                        ReadSceneFraming(result, match.Groups[2].Value, label == "环境辅助取景");
                        return string.Empty;
                    }
                    // Theme is shown in a wrapping/scrolling viewport. Keep its full sentence
                    // in metadata instead of permanently cutting it off before disk save.
                    string value = CleanLabel(match.Groups[2].Value, label == "画作标题" ? 18 : label == "画作主题" ? int.MaxValue : 160);
                    if (label == "画作标题") result.Title = value;
                    else if (label == "画作主题") result.Theme = value;
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
            var match = Regex.Match(prompt ?? string.Empty, @"【(?:人物与镜头|人物镜头|角色与镜头|人物与构图)】([^【]+)");
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
                "【近期人物行动参考】以下是旧画构思，不是本次现场事实：\n" + string.Join("\n", summaries) +
                (weeklyReport
                    ? "\n以上仅是旧纪事画的构思，不是本期事件证据。依据本期正文选择事件与叙事瞬间，保留所选事件参与方、地点关联、行动和结果；艺术环境与陈设可围绕事件重新设计，不因旧图是领主肖像就沿用肖像构图。"
                    : eventAnchored
                    ? "\n当前现场与事件事实优先，旧画行动不作为本次事实；在当前事实允许的叙事瞬间内推导手势、视线和机位，不能为动作去重改变事件或添加道具。"
                    : "\n本次先选择符合人物的新行动意图，再推导姿态、手部动作与视线；减少重复的双手下垂展示姿势，不能仅换背景。换一个幅度清楚、轮廓可读的肢体动作，支撑须可信。") +
                "\n【近期人物行动参考结束】";
        }

        internal static bool IsLocalFallback(CachedIllustrationItem item)
        {
            return string.Equals(item?.DirectorStatus, "local_fallback", StringComparison.Ordinal);
        }

        internal static string RemoveActionHistory(string artDirection)
        {
            string value = artDirection ?? string.Empty;
            value = Regex.Replace(value, @"【近期人物行动参考】[\s\S]*?【近期人物行动参考结束】", string.Empty);
            // Accept callers with the earlier unframed format without consuming later sections.
            return Regex.Replace(value, @"【近期人物行动参考】[^【]*", string.Empty).Trim();
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
