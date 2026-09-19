using System;
using System.Collections.Generic;
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
        // Generation metadata is stored/displayed separately and never appended to Prompt.
        public string DirectionStatus { get; internal set; } = "complete";
        public string FallbackReason { get; internal set; } = string.Empty;
        public string FinishReason { get; internal set; } = string.Empty;
        public bool UsedLocalFallback { get; internal set; }
        public bool VisionUnsupported { get; internal set; }
        public int? PromptTokens { get; internal set; }
        public int? CompletionTokens { get; internal set; }
        public int? TotalTokens { get; internal set; }
        public string StatusText
        {
            get
            {
                if (DirectionStatus == "truncated") return "导演输出截断，已使用本地构图";
                if (UsedLocalFallback || DirectionStatus == "local_fallback")
                    return VisionUnsupported ? "导演不支持识图且文字构思不可用，已使用本地构图" : "已使用本地构图";
                if (VisionUnsupported || DirectionStatus == "vision_unsupported")
                    return "导演不支持识图，已使用文字构思";
                return string.IsNullOrWhiteSpace(FinishReason)
                    ? "导演正文已校验（接口未返回结束标记）" : "导演构思完成";
            }
        }

        internal static IllustrationDirection SplitMetadata(string output)
        {
            var result = new IllustrationDirection();
            result.Prompt = Regex.Replace(output ?? string.Empty,
                @"【(画作标题|画作主题|人物行动)】([^【]*)", match =>
                {
                    string label = match.Groups[1].Value;
                    string value = CleanLabel(match.Groups[2].Value, label == "画作标题" ? 18 : label == "画作主题" ? 36 : 160);
                    if (label == "画作标题") result.Title = value;
                    else if (label == "画作主题") result.Theme = value;
                    else result.ActionSummary = value;
                    return string.Empty;
                }).Trim();
            return result;
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
                .Where(i => i != null && !i.Deleted).OrderByDescending(i => i.CreatedTime).Take(3)
                .Select(i => string.IsNullOrWhiteSpace(i.ActionSummary) ? ExtractActionSummary(i.Prompt) : i.ActionSummary)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => CleanLabel(s, 360)).Distinct().ToArray();
            return summaries.Length == 0 ? string.Empty :
                "【近期人物行动参考】以下是旧画构思，不是本次现场事实：\n" + string.Join("\n", summaries) +
                (weeklyReport
                    ? "\n以上仅是旧纪事画的构思，不是本期事件证据。依据本期正文选择事件与叙事瞬间，保留所选事件参与方、地点关联、行动和结果；艺术环境与陈设可围绕事件重新设计，不因旧图是领主肖像就沿用肖像构图。"
                    : eventAnchored
                    ? "\n当前现场与事件事实优先，旧画行动不作为本次事实；在当前事实允许的叙事瞬间内推导手势、视线和机位，不能为动作去重改变事件或添加道具。"
                    : "\n本次先选择符合人物的新行动意图，再推导姿态、手部动作与视线；减少重复的双手下垂展示姿势，不能仅换背景。保留合理自然站姿，不强迫复杂动作。");
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
