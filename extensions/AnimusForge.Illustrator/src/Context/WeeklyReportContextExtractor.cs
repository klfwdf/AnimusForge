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

    /// <summary>
    /// 子模块侧周报结构化快照：弹窗文本只解析一次，之后人物、地点、主题与日期全部由快照承载，
    /// 不再在生图链路上二次猜测。地点只在周报文本真实提及定居点时解析，解析不到就标未知，
    /// 绝不用"主角/玩家当前所在地"冒充事件现场。
    /// </summary>
    public sealed class WeeklyReportIllustrationSnapshot
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Headline = string.Empty;
        public WeeklyReportEventTheme Theme = WeeklyReportEventTheme.General;
        public Hero ProtagonistHero;
        public Settlement EventSettlement;
        public string ReportDateLabel = string.Empty;
    }

    public sealed class WeeklyReportVisualContext
    {
        public WeeklyReportIllustrationSnapshot Snapshot { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string HeadlineSummary { get; set; } = string.Empty;
        public WeeklyReportEventTheme EventTheme { get; set; } = WeeklyReportEventTheme.General;
        public string SceneThemeDirective { get; set; } = string.Empty;
        public Hero ProtagonistHero { get; set; }
        public HeroVisualProfile ProtagonistProfile { get; set; }
        public EnvironmentVisualProfile EnvironmentProfile { get; set; }

        /// <summary>
        /// 硬事实区：报文原文、当事人真实档案、已确认的事件定居点与周报发布日期。
        /// 导演与生图模型不得改写此区内容。
        /// </summary>
        public string BuildHardFacts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 【卡拉迪亚历史纪事周报】 ===");
            if (!string.IsNullOrWhiteSpace(Title)) sb.AppendLine($"【报头】{Title}");
            if (!string.IsNullOrWhiteSpace(Subtitle)) sb.AppendLine($"【核心局势】{Subtitle}");
            if (!string.IsNullOrWhiteSpace(HeadlineSummary)) sb.AppendLine($"【事件要闻】{HeadlineSummary}");

            if (ProtagonistProfile != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== 【登场人物真实视觉档案】 ===");
                sb.AppendLine(ProtagonistProfile.BuildSummary());
            }

            if (EnvironmentProfile != null)
            {
                string facts = EnvironmentProfile.BuildHardFactsSummary();
                if (!string.IsNullOrWhiteSpace(facts))
                {
                    sb.AppendLine();
                    sb.AppendLine("=== 【事件现场已确认事实】 ===");
                    sb.AppendLine(facts);
                }
            }
            else if (Snapshot != null && Snapshot.EventSettlement == null)
            {
                sb.AppendLine();
                sb.AppendLine("【事件现场】周报未指明具体定居点，地点由导演按事件要闻合理设定。");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 开放艺术指导区：事件主题倾向、可选场景元素与构图方向。全部是建议而非命令。
        /// </summary>
        public string BuildArtDirection()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(SceneThemeDirective)) sb.AppendLine(SceneThemeDirective);
            if (EnvironmentProfile != null)
            {
                string direction = EnvironmentProfile.BuildArtDirectionSummary();
                if (!string.IsNullOrWhiteSpace(direction)) sb.AppendLine(direction);
            }
            return sb.ToString().TrimEnd();
        }

        public string BuildCompositeContext()
        {
            return BuildHardFacts() + "\n\n【开放艺术指导】\n" + BuildArtDirection();
        }
    }

    public static class WeeklyReportContextExtractor
    {
        public static WeeklyReportVisualContext ExtractFromWeeklyReport(string title, string subtitle, string body)
        {
            var snapshot = BuildSnapshot(title, subtitle, body);
            var context = new WeeklyReportVisualContext
            {
                Snapshot = snapshot,
                Title = snapshot.Title,
                Subtitle = snapshot.Subtitle,
                HeadlineSummary = snapshot.Headline,
                EventTheme = snapshot.Theme,
                ProtagonistHero = snapshot.ProtagonistHero,
                SceneThemeDirective = GenerateSceneDirective(snapshot.Theme, snapshot.EventSettlement, snapshot.Headline)
            };

            // 提取关键人物视觉（严格反映真实穿着与无胡须特征）
            if (snapshot.ProtagonistHero != null)
            {
                context.ProtagonistProfile = HeroVisualExtractor.Extract(snapshot.ProtagonistHero, useCivilian: false);
            }

            // 环境只承载已确认事实：定居点（若文本真实提及）与周报发布纪元日期。
            // 未提及地点时 settlement 为 null——宁可标未知，也不用玩家当前位置冒充事件现场。
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(snapshot.EventSettlement, eventAnchored: true, eventDateLabel: snapshot.ReportDateLabel);
            ApplyEventSceneAnchoring(context, snapshot.EventSettlement);

            return context;
        }

        /// <summary>
        /// 把周报弹窗文本一次性解析为结构化快照：主题分类、当事人、事件定居点、发布日期。
        /// </summary>
        private static WeeklyReportIllustrationSnapshot BuildSnapshot(string title, string subtitle, string body)
        {
            string cleanTitle = CleanText(title);
            string cleanSubtitle = CleanText(subtitle);
            string cleanHeadline = CleanText(ExtractHeadline(body));
            string fullText = $"{cleanTitle} {cleanSubtitle} {cleanHeadline}";

            var snapshot = new WeeklyReportIllustrationSnapshot
            {
                Title = cleanTitle,
                Subtitle = cleanSubtitle,
                Headline = cleanHeadline,
                Theme = ClassifyEventTheme(fullText),
                ProtagonistHero = ResolveProtagonistHero(fullText),
                EventSettlement = ResolveEventSettlement(fullText)
            };

            try
            {
                int seasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
                string seasonName = seasonIndex == 0 ? "春" : seasonIndex == 1 ? "夏" : seasonIndex == 2 ? "秋" : "冬";
                snapshot.ReportDateLabel = $"卡拉迪亚历 {CampaignTime.Now.GetYear} 年 · {seasonName}季 · 第 {CampaignTime.Now.GetDayOfSeason + 1} 日（周报发布日，事件发生在本周期内）";
            }
            catch
            {
            }

            return snapshot;
        }

        /// <summary>
        /// 在周报文本中查找被提及的英雄：优先取文本中最先出现者，其次阵营领袖/宗族首领。
        /// 无命中时退回玩家主角（周报本就是呈给玩家的纪事）。
        /// </summary>
        private static Hero ResolveProtagonistHero(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText)) return Hero.MainHero;

            Hero best = null;
            int bestIndex = int.MaxValue;
            int bestRank = int.MinValue;

            try
            {
                foreach (var hero in EnumerateHeroes())
                {
                    if (hero == null) continue;
                    string name = hero.Name != null ? hero.Name.ToString() : null;
                    if (string.IsNullOrWhiteSpace(name) || name.Length < 2) continue;

                    int idx = fullText.IndexOf(name, StringComparison.Ordinal);
                    if (idx < 0) continue;

                    int rank = (hero.IsFactionLeader ? 4 : 0) + (hero.Clan != null && hero.Clan.Leader == hero ? 2 : 0) + (hero.IsAlive ? 1 : 0);
                    if (idx < bestIndex || (idx == bestIndex && rank > bestRank))
                    {
                        best = hero;
                        bestIndex = idx;
                        bestRank = rank;
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Weekly report protagonist resolution failed: {ex.Message}");
            }

            return best ?? Hero.MainHero;
        }

        private static IEnumerable<Hero> EnumerateHeroes()
        {
            if (Hero.AllAliveHeroes != null)
            {
                foreach (var hero in Hero.AllAliveHeroes) yield return hero;
            }
            if (Hero.DeadOrDisabledHeroes != null)
            {
                foreach (var hero in Hero.DeadOrDisabledHeroes) yield return hero;
            }
        }

        /// <summary>
        /// 按事件主题补充可选的场景元素建议（进开放艺术指导区，不是硬事实）。
        /// 只写"氛围参考/建议"字段；定居点、日期等硬事实由探针/快照承载。
        /// </summary>
        private static void ApplyEventSceneAnchoring(WeeklyReportVisualContext context, Settlement settlement)
        {
            var profile = context?.EnvironmentProfile;
            if (profile == null) return;
            string locName = settlement != null && settlement.Name != null ? settlement.Name.ToString() : "卡拉迪亚";

            switch (context.EventTheme)
            {
                case WeeklyReportEventTheme.VillageRaid:
                    profile.SpecificLocation = settlement != null ? $"{locName}周边乡野村落" : "遭袭的乡野村落";
                    profile.IndoorOutdoorDetails = "可参考的劫掠现场元素：起火的农舍茅顶、升腾黑烟、奔逃的村民、纵马穿行的掠夺者、受惊的牲畜与被撞倒的栅栏。";
                    profile.ConflictStatus = "【事件性质】定居点遭劫掠袭击";
                    break;
                case WeeklyReportEventTheme.FieldBattle:
                    profile.SpecificLocation = settlement != null ? $"{locName}外围旷野" : "开阔旷野战场";
                    profile.IndoorOutdoorDetails = "可参考的野战元素：尘烟弥漫的原野、盾墙与骑兵冲锋、残破飘扬的战旗、散落的兵器与箭矢。";
                    profile.ConflictStatus = "【事件性质】野外军团交战";
                    break;
                case WeeklyReportEventTheme.Siege:
                    profile.SpecificLocation = settlement != null ? $"{locName}城墙与围攻阵地" : "要塞围攻阵地";
                    profile.IndoorOutdoorDetails = "可参考的围城元素：架上城墙的云梯、攻城塔与破城槌、投石机、城堞后的守军与城下拒马壕沟。";
                    profile.ConflictStatus = "【事件性质】要塞围攻战";
                    break;
                case WeeklyReportEventTheme.FeastTournament:
                    profile.SpecificLocation = settlement != null ? $"{locName}厅堂或竞技场" : "庆典厅堂或竞技场";
                    profile.IndoorOutdoorDetails = "可参考的庆典元素：觥筹交错的宴会、穿梭的侍从、竞技场长矛比武与欢呼看台。";
                    profile.ConflictStatus = "【事件性质】宴会比武盛事";
                    break;
                case WeeklyReportEventTheme.Diplomacy:
                    profile.SpecificLocation = "行军大帐或议事厅";
                    profile.IndoorOutdoorDetails = "可参考的外交场景元素：铺有羊皮纸地图的长桌、封蜡条约、肃立护卫、凝重的使节与谋士。";
                    profile.ConflictStatus = "【事件性质】重大外交角力";
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(profile.SpecificLocation) && settlement != null)
                    {
                        profile.SpecificLocation = $"{locName}城内外";
                    }
                    break;
            }
        }

        /// <summary>
        /// 只在周报文本真实提及定居点名时解析事件地点；无命中返回 null（事件现场标未知）。
        /// 绝不退回"主角或玩家当前所在地"——回顾性事件的发生地与玩家当下位置无关。
        /// </summary>
        private static Settlement ResolveEventSettlement(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText)) return null;

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

            return bestMatch;
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

        /// <summary>
        /// 事件主题对应的开放取景方向：给出可选取景元素与氛围倾向，
        /// 由导演按要闻自由择取一个最有叙事力的瞬间——不锁定单一构图。
        /// </summary>
        private static string GenerateSceneDirective(WeeklyReportEventTheme theme, Settlement settlement, string headline)
        {
            string locName = settlement != null ? (settlement.Name != null ? settlement.Name.ToString() : settlement.StringId) : "卡拉迪亚";

            switch (theme)
            {
                case WeeklyReportEventTheme.VillageRaid:
                    return $"【事件主题：村庄遭袭/劫掠】要闻指向【{locName}】一带的村落劫掠。" +
                           "可选取景：起火的农舍与黑烟、奔逃村民与掠夺者、散落的谷物农具、赶到现场的领主或骑兵——" +
                           "任选一个最有冲突张力的瞬间，远近景、动静视角皆可。";

                case WeeklyReportEventTheme.FieldBattle:
                    return $"【事件主题：野战交锋】要闻指向【{locName}】周边的军队对阵。" +
                           "可选取景：骑兵冲锋与盾墙撞击、漫天箭雨、倒伏的战旗、或将领在军阵前后的决断瞬间——构图与焦点自由。";

                case WeeklyReportEventTheme.Siege:
                    return $"【事件主题：要塞围攻】要闻指向【{locName}】的攻城或守城。" +
                           "可选取景：云梯登城、投石机轰击、破城槌撞门、城头攻守拉锯、或围城营地的肃杀对峙——任取一个瞬间。";

                case WeeklyReportEventTheme.FeastTournament:
                    return $"【事件主题：宴会比武盛事】要闻指向【{locName}】内的庆典。" +
                           "可选取景：厅堂觥筹交错、竞技场长矛对冲、看台欢呼、或胜者受瞩目的瞬间——欢腾或紧张氛围皆可。";

                case WeeklyReportEventTheme.Diplomacy:
                    return "【事件主题：重大外交角力】可选取景：大帐或议事厅内围绕地图的商谈、条约封缄、双方使节对峙、" +
                           "或会谈间隙的眼神交锋——强调紧绷或凝重的关系张力。";

                default:
                    return $"【事件主题：以要闻为准】围绕要闻【{headline}】的实际行为自由取景：" +
                           "可选人物行动瞬间、事件余波、或当事人在环境中的决断姿态——避免千篇一律的看风景站桩。";
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
