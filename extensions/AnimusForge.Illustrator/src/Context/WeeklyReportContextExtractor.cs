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
        Naval,
        PrisonerExecution,
        SettlementChange,
        DynastyDeath,
        General
    }

    /// <summary>
    /// 子模块侧周报结构化快照：弹窗文本只解析一次，人物、地点候选及其来源句与日期由快照承载。
    /// 导演在同一次请求中选择事件；最早提及的人物与地点不是已选中的主角和现场。
    /// 地点只在周报文本真实提及定居点时解析，解析不到就标未知，
    /// 绝不用"主角/玩家当前所在地"冒充事件现场。
    /// </summary>
    public sealed class WeeklyReportIllustrationSnapshot
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Headline = string.Empty;
        public string EventFacts = string.Empty;
        public WeeklyReportEventTheme Theme = WeeklyReportEventTheme.General;
        public Hero ProtagonistHero;
        public string ProtagonistEvidence = string.Empty;
        public Settlement EventSettlement;
        public string EventSettlementEvidence = string.Empty;
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
        /// 硬事实区：完整事件证据句、人物视觉事实与已确认场景；报头及背景原文仅给导演。
        /// 导演与生图模型不得改写此区内容。
        /// </summary>
        public string BuildHardFacts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【事件行动与结果证据】");
            sb.AppendLine(!string.IsNullOrWhiteSpace(Snapshot?.EventFacts) ? Snapshot.EventFacts
                : NarrativeFactRouter.BuildEventEvidence(Title, Subtitle, HeadlineSummary));
            sb.AppendLine("以上是事件内容而非画面文字。保留各句的当事人、地点、否定、计划与结果，不把人物或结果跨事件拼接；未明确的结果保持未知。");

            if (ProtagonistProfile != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== 【本期提及人物的可选身份资料】 ===");
                if (!string.IsNullOrWhiteSpace(Snapshot?.ProtagonistEvidence))
                    sb.AppendLine("【此人对应原文】" + Snapshot.ProtagonistEvidence);
                sb.AppendLine("此人为本期正文中识别到的参考人物，并非预先指定的画面主角。只有选中的事件确实涉及此人时才使用其身份资料；选择其他事件时不必让他入画。没有参考图的参与方仍按所选事件表达，参考图数量不决定画中人数。");
                sb.AppendLine(ProtagonistProfile.BuildVisualSummary());
            }

            if (EnvironmentProfile != null && (Snapshot == null || Snapshot.EventSettlement != null))
            {
                string facts = EnvironmentProfile.BuildHardFactsSummary();
                if (!string.IsNullOrWhiteSpace(facts))
                {
                    sb.AppendLine();
                    sb.AppendLine("=== 【本期提及地点资料，并非已选事件现场】 ===");
                    if (!string.IsNullOrWhiteSpace(Snapshot?.EventSettlementEvidence))
                        sb.AppendLine("【此地点对应原文】" + Snapshot.EventSettlementEvidence);
                    sb.AppendLine("只在所选事件原文明确关联此地点时使用其资料；提及地点不等于行动发生于此，不能把其他事件的地点、文化或地貌移入本画。未采集该历史现场的实际场景图。");
                    sb.AppendLine(facts);
                }
            }
            if (Snapshot != null && Snapshot.EventSettlement == null)
            {
                sb.AppendLine();
                sb.AppendLine("【地点证据边界】未解析到具体定居点；所选事件的发生地以原文为准，不能用玩家或参考人物当前所在地替代。允许设计与该事件及时代相容的非具名艺术环境，但不得声称是实际地点的精确还原。");
            }

            return sb.ToString().TrimEnd();
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Title)) sb.AppendLine("【报头原文】" + Title);
            if (!string.IsNullOrWhiteSpace(Subtitle)) sb.AppendLine("【核心局势原文】" + Subtitle);
            if (!string.IsNullOrWhiteSpace(HeadlineSummary)) sb.AppendLine("【事件要闻原文】" + HeadlineSummary);
            if (ProtagonistProfile != null)
            {
                sb.AppendLine("【可选参考人物背景】以下仅补充该人物的身份；生平、地位与装备不构成本期事件，也不要求将其作为主角。");
                sb.AppendLine(ProtagonistProfile.BuildDirectorOnlyFacts());
            }
            if (EnvironmentProfile != null) sb.AppendLine(EnvironmentProfile.BuildDirectorOnlyFacts());
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 开放艺术指导区：事件主题倾向、可选场景元素与构图方向。全部是建议而非命令。
        /// </summary>
        public string BuildArtDirection()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(SceneThemeDirective)) sb.AppendLine(SceneThemeDirective);
            // 周报是历史事件的艺术再现，不复用实时会话中“未知陈设一律不补”的现场复原规则。
            return sb.ToString().TrimEnd();
        }

        public string BuildCompositeContext()
        {
            return BuildHardFacts() + "\n\n【导演专属背景】\n" + BuildDirectorOnlyFacts() + "\n\n【开放艺术指导】\n" + BuildArtDirection();
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

            // 提取一位已提及人物的可选身份基准；是否登场以及画面重心由所选事件决定。
            if (snapshot.ProtagonistHero != null)
            {
                var app = CharacterAppearanceSnapshot.FromHero(snapshot.ProtagonistHero, snapshot.ProtagonistHero.BattleEquipment);
                context.ProtagonistProfile = HeroVisualExtractor.Extract(snapshot.ProtagonistHero, useCivilian: false, appearance: app);
                context.ProtagonistProfile.CurrentStateDetail = string.Empty;
                context.ProtagonistProfile.Appearance = app;
            }

            // 地点资料保留对应原文，不能将整期最早出现的地点直接绑定为导演选中事件的现场。
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
            string eventFacts = NarrativeFactRouter.BuildEventEvidence(cleanTitle, cleanSubtitle, cleanHeadline);

            var snapshot = new WeeklyReportIllustrationSnapshot
            {
                Title = cleanTitle,
                Subtitle = cleanSubtitle,
                Headline = cleanHeadline,
                EventFacts = eventFacts,
                Theme = ClassifyEventTheme(eventFacts),
                ProtagonistHero = ResolveProtagonistHero(eventFacts),
                EventSettlement = ResolveEventSettlement(eventFacts)
            };
            snapshot.ProtagonistEvidence = FindMentionEvidence(eventFacts, snapshot.ProtagonistHero?.Name?.ToString());
            snapshot.EventSettlementEvidence = FindMentionEvidence(eventFacts, snapshot.EventSettlement?.Name?.ToString());

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
        /// 在证据正文中选一位可供身份图的已提及英雄；不预选事件或要求此人登场。
        /// 无命中时保持未知；收报人不能冒充事件当事人。只在打开周报时扫描一次。
        /// </summary>
        private static Hero ResolveProtagonistHero(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText)) return null;

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

            return best;
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

        private static string FindMentionEvidence(string eventFacts, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            // BuildEventEvidence 已保留完整句并按行分隔；只附回来源句，不按关键词重组事件。
            foreach (string sentence in (eventFacts ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                if (sentence.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return sentence.Trim();
            return string.Empty;
        }

        /// <summary>
        /// 按事件主题补充可选的场景元素建议（进开放艺术指导区，不是硬事实）。
        /// 只写"氛围参考/建议"字段；定居点、日期等硬事实由探针/快照承载。
        /// </summary>
        private static void ApplyEventSceneAnchoring(WeeklyReportVisualContext context, Settlement settlement)
        {
            // Keyword themes are suggestions, never evidence of outcome or a physical sub-scene.
            if (context?.EnvironmentProfile == null) return;
            context.EnvironmentProfile.ConflictStatus = string.Empty;
            context.EnvironmentProfile.SpecificLocation = string.Empty;
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

            // 处决/行刑（强信号优先——"劫掠并俘虏"不该盖过处决要闻）
            if (lower.Contains("处决") || lower.Contains("斩首") || lower.Contains("绞刑") ||
                lower.Contains("行刑") || lower.Contains("处刑") || lower.Contains("execution") ||
                lower.Contains("beheaded") || lower.Contains("executed"))
            {
                return WeeklyReportEventTheme.PrisonerExecution;
            }

            // 海上交战/舰队行动
            if (lower.Contains("海战") || lower.Contains("舰队") || lower.Contains("战船") ||
                lower.Contains("海上") || lower.Contains("港口") || lower.Contains("水师") ||
                lower.Contains("naval") || lower.Contains("fleet") || lower.Contains("fleet"))
            {
                return WeeklyReportEventTheme.Naval;
            }

            // 定居点陷落/易主（战后状态，区别于进行中的围攻）
            if (lower.Contains("陷落") || lower.Contains("失垒") || lower.Contains("易主") ||
                lower.Contains("失守") || lower.Contains("攻陷") || lower.Contains("占领") ||
                lower.Contains("收复") || lower.Contains("投降") || lower.Contains("献出") ||
                lower.Contains("fallen") || lower.Contains("captured") || lower.Contains("surrendered") || lower.Contains("ceded"))
            {
                return WeeklyReportEventTheme.SettlementChange;
            }

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

            // 重要人物亡故/权力交接
            if (lower.Contains("驾崩") || lower.Contains("逝世") || lower.Contains("薨逝") ||
                lower.Contains("去世") || lower.Contains("继位") || lower.Contains("继承") ||
                lower.Contains("died") || lower.Contains("succession") || lower.Contains("abdicated"))
            {
                return WeeklyReportEventTheme.DynastyDeath;
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

            // 俘虏/囚禁（弱信号，靠后——处决词已在前命中）
            if (lower.Contains("俘虏") || lower.Contains("囚禁") || lower.Contains("赎俘") ||
                lower.Contains("越狱") || lower.Contains("captive") || lower.Contains("prisoner"))
            {
                return WeeklyReportEventTheme.PrisonerExecution;
            }

            return WeeklyReportEventTheme.General;
        }

        /// <summary>
        /// 事件主题对应的开放取景方向：给出可选取景元素与氛围倾向，
        /// 由导演按要闻自由择取一个最有叙事力的瞬间——不锁定单一构图。
        /// </summary>
        private static string GenerateSceneDirective(WeeklyReportEventTheme theme, Settlement settlement, string headline)
        {
            return "【周报事件纪事管线】从本期完整正文中自主选定一个明确事件及一个有叙事力的瞬间，再推导行动、空间关系和机位。" +
                "让画面通过参与方正在做什么、事件如何发生及已知结果说明本期纪事；不能仅让领主站立或骑马展示，再把事件地点缩成远处布景。" +
                "并非每次都要战斗、群像或广角：安静的谈判、重整、交接等瞬间也可以，只要确实属于选中事件，画面的关系与行动能让人读懂发生了什么。" +
                "人物与环境篇幅随事件需要决定，不罗列整套装备；图中的具名人物、参与阵营、地点关联、计划、否定、未遂与胜负结局必须有正文依据，不把不同要闻拼成一次事件。" +
                "允许自由设计与事件、文化和时代相容的非具名建筑细部、地貌、生活痕迹、材质、必要的匿名参与者与氛围，使环境参与叙事；这些是艺术再现，不是假称实测的现场。" +
                "艺术细节不能新增关键行动、特定参与者、伤亡、胜败或历史结果。已有结果只按证据表现，未知结果不替报道作结论。";
        }

        private static string ExtractHeadline(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }
            // 按完整句提取证据前不能截断，否则尾部的失败/撤退/未遂会丢失。
            return NarrativeFactRouter.CleanText(body);
        }

        private static string CleanText(string text)
        {
            return NarrativeFactRouter.CleanText(text);
        }
    }
}
