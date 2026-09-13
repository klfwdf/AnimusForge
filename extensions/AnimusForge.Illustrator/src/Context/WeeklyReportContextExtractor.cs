using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge.Illustrator.Context
{
    public enum WeeklyReportEventTheme
    {
        VillageRaid,
        FieldBattle,
        Siege,
        FeastTournament,
        Diplomacy,
        General
    }

    public sealed class WeeklyReportVisualContext
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string HeadlineSummary { get; set; } = string.Empty;
        public WeeklyReportEventTheme EventTheme { get; set; } = WeeklyReportEventTheme.General;
        public string SceneThemeDirective { get; set; } = string.Empty;
        public HeroVisualProfile ProtagonistProfile { get; set; }
        public EnvironmentVisualProfile EnvironmentProfile { get; set; }

        public string BuildCompositeContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 【卡拉迪亚历史纪事核心周报】 ===");
            if (!string.IsNullOrWhiteSpace(Title))
            {
                sb.AppendLine($"【报头】{Title}");
            }
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                sb.AppendLine($"【核心局势】{Subtitle}");
            }
            if (!string.IsNullOrWhiteSpace(HeadlineSummary))
            {
                sb.AppendLine($"【事件要闻】{HeadlineSummary}");
            }

            if (!string.IsNullOrWhiteSpace(SceneThemeDirective))
            {
                sb.AppendLine();
                sb.AppendLine("=== 【画面核心指导原则 (绝对禁止千篇一律的城墙发呆构图)】 ===");
                sb.AppendLine(SceneThemeDirective);
            }

            sb.AppendLine();
            sb.AppendLine("=== 【登场人物视觉设定 (穿着/装备/外貌)】 ===");
            if (ProtagonistProfile != null)
            {
                sb.AppendLine(ProtagonistProfile.BuildSummary());
            }

            sb.AppendLine();
            sb.AppendLine("=== 【所处时空环境与氛围描写 (建筑/时辰/光影)】 ===");
            if (EnvironmentProfile != null)
            {
                sb.AppendLine(EnvironmentProfile.BuildSummary());
            }

            return sb.ToString().TrimEnd();
        }
    }

    public static class WeeklyReportContextExtractor
    {
        public static WeeklyReportVisualContext ExtractFromWeeklyReport(string title, string subtitle, string body)
        {
            string cleanTitle = CleanText(title);
            string cleanSubtitle = CleanText(subtitle);
            string cleanHeadline = CleanText(ExtractHeadline(body));
            string fullText = $"{cleanTitle} {cleanSubtitle} {cleanHeadline}";

            Hero mainHero = Hero.MainHero;
            Settlement eventSettlement = ResolveEventSettlement(fullText, mainHero);
            WeeklyReportEventTheme theme = ClassifyEventTheme(fullText);

            var context = new WeeklyReportVisualContext
            {
                Title = cleanTitle,
                Subtitle = cleanSubtitle,
                HeadlineSummary = cleanHeadline,
                EventTheme = theme,
                SceneThemeDirective = GenerateSceneDirective(theme, eventSettlement, cleanHeadline)
            };

            // 提取关键人物视觉（严格反映真实穿着与无胡须特征）
            if (mainHero != null)
            {
                context.ProtagonistProfile = HeroVisualExtractor.Extract(mainHero, useCivilian: false);
            }

            // 提取事件发生地的真实环境视觉（若为村庄遭袭，则精准采用村庄乡野建筑，而非主城大教堂城墙）
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(eventSettlement);

            return context;
        }

        private static Settlement ResolveEventSettlement(string fullText, Hero mainHero)
        {
            if (string.IsNullOrWhiteSpace(fullText))
            {
                return Settlement.CurrentSettlement ?? (mainHero != null ? mainHero.CurrentSettlement : null);
            }

            Settlement bestMatch = null;
            int earliestIndex = int.MaxValue;

            try
            {
                if (Settlement.All != null)
                {
                    foreach (Settlement s in Settlement.All)
                    {
                        if (s == null) continue;
                        string sName = s.Name != null ? s.Name.ToString() : null;
                        if (!string.IsNullOrWhiteSpace(sName) && sName.Length >= 2)
                        {
                            int idx = fullText.IndexOf(sName, StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0 && idx < earliestIndex)
                            {
                                earliestIndex = idx;
                                bestMatch = s;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return bestMatch ?? Settlement.CurrentSettlement ?? (mainHero != null ? mainHero.CurrentSettlement : null);
        }

        private static WeeklyReportEventTheme ClassifyEventTheme(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return WeeklyReportEventTheme.General;
            }

            string lower = text.ToLowerInvariant();

            // 村庄遭扰、袭击、劫掠
            if (lower.Contains("袭击") || lower.Contains("劫掠") || lower.Contains("烧毁") ||
                lower.Contains("掠夺") || lower.Contains("遭扰") || lower.Contains("袭扰") ||
                lower.Contains("raid") || lower.Contains("pillage") || lower.Contains("loot"))
            {
                return WeeklyReportEventTheme.VillageRaid;
            }

            // 围攻防守战
            if (lower.Contains("围攻") || lower.Contains("攻城") || lower.Contains("围城") ||
                lower.Contains("云梯") || lower.Contains("攻城塔") || lower.Contains("破城") ||
                lower.Contains("siege") || lower.Contains("assault"))
            {
                return WeeklyReportEventTheme.Siege;
            }

            // 野外战役交锋
            if (lower.Contains("交战") || lower.Contains("大捷") || lower.Contains("击溃") ||
                lower.Contains("激战") || lower.Contains("溃败") || lower.Contains("野战") ||
                lower.Contains("阵亡") || lower.Contains("合围") || lower.Contains("battle") || lower.Contains("clash"))
            {
                return WeeklyReportEventTheme.FieldBattle;
            }

            // 宴会、比武竞技
            if (lower.Contains("宴会") || lower.Contains("竞技") || lower.Contains("比武") ||
                lower.Contains("加冕") || lower.Contains("婚礼") || lower.Contains("tournament") || lower.Contains("feast"))
            {
                return WeeklyReportEventTheme.FeastTournament;
            }

            // 外交和谈、宣战
            if (lower.Contains("和平") || lower.Contains("宣战") || lower.Contains("条约") ||
                lower.Contains("停火") || lower.Contains("进贡") || lower.Contains("peace") || lower.Contains("war") || lower.Contains("treaty"))
            {
                return WeeklyReportEventTheme.Diplomacy;
            }

            return WeeklyReportEventTheme.General;
        }

        private static string GenerateSceneDirective(WeeklyReportEventTheme theme, Settlement settlement, string headline)
        {
            string locName = settlement != null ? (settlement.Name != null ? settlement.Name.ToString() : settlement.StringId) : "卡拉迪亚边境";

            switch (theme)
            {
                case WeeklyReportEventTheme.VillageRaid:
                    return $"【核心事件：村庄遭袭/劫掠 - 严禁画成站在城墙上看风景！】\n" +
                           $"画面必须聚焦于在【{locName}】发生的突发村落劫掠与袭击实况！" +
                           $"主要视觉元素应包括：被火光照亮的乡野土石与茅草农舍、滚滚升腾的黑烟与火星、四散奔逃的农夫村民、" +
                           $"挥舞火把与弯刀长矛的掠夺骑兵、受惊的牲畜、被践踏的农田与泥泞泥道。" +
                           $"如果画面中出现领主或主角，必须是策马疾驰赶往现场侦察戒备、拔剑迎战或指挥骑兵警戒的战斗/行动态势，绝对不要悠闲平淡地倚墙站立！";

                case WeeklyReportEventTheme.FieldBattle:
                    return $"【核心事件：野战交锋 - 严禁画成站在城墙上看风景！】\n" +
                           $"画面必须描绘在【{locName}】周边旷野上展开的真实军队野战冲击瞬间！" +
                           $"骑兵持枪冲锋、步兵阵线盾墙撞击、飞射的箭矢、残破飘扬的家族旗帜与战场尘土，展现宏大史诗的残酷交战场面。";

                case WeeklyReportEventTheme.Siege:
                    return $"【核心事件：城市/城堡围攻血战 - 严禁画成和平站桩！】\n" +
                           $"画面必须描绘【{locName}】要塞城墙下的惨烈围攻战：高耸城墙上云梯架起、投石机抛掷巨石炸裂碎屑、破城槌冲击城门、" +
                           $"城头射箭倾倒滚油、浴血拼杀的攻城部队与守军。";

                case WeeklyReportEventTheme.FeastTournament:
                    return $"【核心事件：盛大宴会或比武竞技】\n" +
                           $"画面聚焦于【{locName}】内部的华丽喧闹场景：贵族领主觥筹交错的宴会大厅，或沙石飞扬、看台欢呼鼎沸的骑士竞技长矛比武。";

                case WeeklyReportEventTheme.Diplomacy:
                    return $"【核心事件：重大外交会晤与宣战议和】\n" +
                           $"画面聚焦于庄重威严的军事行军大营或大理石议事厅内：领主、使节与谋士围绕羊皮纸地图沙盘对峙商榷，气氛紧绷凝重。";

                default:
                    return $"【核心要求：紧扣新闻动态动作，拒绝呆板站桩】\n" +
                           $"画面必须直接反映要闻【{headline}】所描述的实际行为，动态展现当事人物的具体动作与环境互动，避免单调重复的看风景站桩构图。";
            }
        }

        private static string ExtractHeadline(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }
            // 截取前 400 字符作为核心新闻事件概述，去除 Gauntlet 超链接标签
            string plain = body.Replace("<a href=\"", "").Replace("</a>", "").Replace("\r\n", " ").Replace("\n", " ");
            if (plain.Length > 400)
            {
                return plain.Substring(0, 400) + "...";
            }
            return plain;
        }

        private static string CleanText(string text)
        {
            return (text ?? string.Empty).Trim();
        }
    }
}
