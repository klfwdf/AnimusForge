using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
    /// 快报先选定事实并冻结参与者；普通周报保留正文候选。提及地点不自动等于事件现场。
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

    public sealed class WeeklyReportCharacterReference
    {
        public Hero Hero;
        public HeroVisualProfile Profile;
        public string HeroId, Name, Role, Evidence, BannerCode;
    }

    public sealed class WeeklyReportVisualContext
    {
        public WeeklyReportIllustrationSnapshot Snapshot { get; set; }
        public bool IsFrozenEvent { get; set; }
        public List<WeeklyReportCharacterReference> Characters { get; } = new List<WeeklyReportCharacterReference>();
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
            sb.AppendLine(IllustratorText.Get("Weekly_579"));
            sb.AppendLine(!string.IsNullOrWhiteSpace(Snapshot?.EventFacts) ? Snapshot.EventFacts
                : NarrativeFactRouter.BuildEventEvidence(Title, Subtitle, HeadlineSummary));
            sb.AppendLine(IllustratorText.Get("Weekly_580"));

            if (IsFrozenEvent)
                sb.AppendLine(IllustratorText.Get("Weekly_581"));
            foreach (var person in Characters)
            {
                sb.AppendLine();
                sb.AppendLine(IllustratorText.Get("Weekly_582") + person.Name + IllustratorText.Get("Weekly_583") + person.Role + "】 ===");
                sb.AppendLine(IllustratorText.Get("Weekly_584") + person.Evidence);
                sb.AppendLine(person.Profile?.BuildVisualSummary() ?? IllustratorText.Get("Weekly_585"));
            }
            sb.AppendLine(IllustratorText.Get("Weekly_586"));

            if (EnvironmentProfile != null && (Snapshot == null || Snapshot.EventSettlement != null))
            {
                string facts = EnvironmentProfile.BuildHardFactsSummary();
                if (!string.IsNullOrWhiteSpace(facts))
                {
                    sb.AppendLine();
                    sb.AppendLine(IllustratorText.Get("Weekly_587"));
                    if (!string.IsNullOrWhiteSpace(Snapshot?.EventSettlementEvidence))
                        sb.AppendLine(IllustratorText.Get("Weekly_588") + Snapshot.EventSettlementEvidence);
                    sb.AppendLine(IllustratorText.Get("Weekly_589"));
                    sb.AppendLine(facts);
                }
            }
            if (Snapshot != null && Snapshot.EventSettlement == null)
            {
                sb.AppendLine();
                sb.AppendLine(IllustratorText.Get("Weekly_590"));
            }

            return sb.ToString().TrimEnd();
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Title)) sb.AppendLine(IllustratorText.Get("Weekly_591") + Title);
            if (!string.IsNullOrWhiteSpace(Subtitle)) sb.AppendLine(IllustratorText.Get("Weekly_592") + Subtitle);
            if (!string.IsNullOrWhiteSpace(HeadlineSummary)) sb.AppendLine(IllustratorText.Get("Weekly_593") + HeadlineSummary);
            foreach (var person in Characters)
            {
                sb.AppendLine(IllustratorText.Get("Weekly_594") + person.Name + IllustratorText.Get("Weekly_595") + person.Role);
                sb.AppendLine(person.Profile?.BuildDirectorOnlyFacts());
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
            sb.AppendLine(IllustratorText.Get("Weekly_596"));
            // 周报是历史事件的艺术再现，不复用实时会话中“未知陈设一律不补”的现场复原规则。
            return sb.ToString().TrimEnd();
        }

        public string BuildCompositeContext()
        {
            return BuildHardFacts() + IllustratorText.Get("Weekly_597") + BuildDirectorOnlyFacts() + IllustratorText.Get("Weekly_598") + BuildArtDirection();
        }
    }

    public static class WeeklyReportContextExtractor
    {
        public static WeeklyReportVisualContext ExtractFromWeeklyReport(string title, string subtitle, string body)
        {
            return Extract(BuildSnapshot(title, subtitle, body), null);
        }

        internal static WeeklyReportVisualContext ExtractFromPlan(global::AnimusForge.WorldBulletinIllustrationPlan plan)
        {
            if (plan == null) return null;
            var snapshot = BuildSnapshot(plan.Title, "", plan.Facts);
            snapshot.ReportDateLabel = plan.DateText ?? "";
            return Extract(snapshot, plan);
        }

        // Called once on the game thread at selection/open. One hero enumeration, bounded two portraits.
        private static WeeklyReportVisualContext Extract(WeeklyReportIllustrationSnapshot snapshot, global::AnimusForge.WorldBulletinIllustrationPlan plan)
        {
            var context = new WeeklyReportVisualContext
            {
                Snapshot = snapshot, IsFrozenEvent = plan != null,
                Title = snapshot.Title, Subtitle = snapshot.Subtitle,
                HeadlineSummary = snapshot.Headline, EventTheme = snapshot.Theme,
                SceneThemeDirective = GenerateSceneDirective(snapshot.Theme, snapshot.EventSettlement, snapshot.Headline)
            };
            var heroes = EnumerateHeroes().Where(h => h != null).GroupBy(h => h.StringId).Select(g => g.First()).ToList();
            var requested = plan?.Participants?.Where(p => p != null && !string.IsNullOrWhiteSpace(p.HeroId)).Take(WorldBulletinPolicy.IllustrationParticipantLimit).ToList();
            if (requested != null && requested.Count > 0)
            {
                var byId = heroes.ToDictionary(h => h.StringId, StringComparer.Ordinal);
                foreach (var p in requested)
                {
                    byId.TryGetValue(p.HeroId, out var hero);
                    AddCharacter(context, hero, p.HeroId, p.Name, p.Role);
                }
            }
            else
            {
                // Old saves have no IDs. Match only the major text, never add people from minor news.
                foreach (var hero in heroes.Where(h => !string.IsNullOrWhiteSpace(h.Name?.ToString()) && h.Name.ToString().Length >= 2)
                    .Select(h => new { Hero = h, Index = snapshot.EventFacts.IndexOf(h.Name.ToString(), StringComparison.Ordinal) })
                    .Where(h => h.Index >= 0).OrderBy(h => h.Index).ThenByDescending(h => h.Hero.IsFactionLeader).Take(WorldBulletinPolicy.IllustrationParticipantLimit))
                    AddCharacter(context, hero.Hero, hero.Hero.StringId, hero.Hero.Name.ToString(), IllustratorText.Get("Weekly_599"));
            }
            var first = context.Characters.FirstOrDefault(c => c.Hero != null);
            context.ProtagonistHero = snapshot.ProtagonistHero = first?.Hero;
            context.ProtagonistProfile = first?.Profile;
            snapshot.ProtagonistEvidence = first?.Evidence ?? "";

            // 地点资料保留对应原文，不能将整期最早出现的地点直接绑定为导演选中事件的现场。
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(snapshot.EventSettlement, eventAnchored: true, eventDateLabel: snapshot.ReportDateLabel);
            ApplyEventSceneAnchoring(context, snapshot.EventSettlement);

            return context;
        }

        private static void AddCharacter(WeeklyReportVisualContext context, Hero hero, string id, string name, string role)
        {
            if (context.Characters.Any(p => p.HeroId == id)) return;
            var person = new WeeklyReportCharacterReference { Hero = hero, HeroId = id,
                Name = string.IsNullOrWhiteSpace(name) ? hero?.Name?.ToString() ?? id : name, Role = role ?? "" };
            person.Evidence = FindMentionEvidence(context.Snapshot.EventFacts, person.Name);
            if (hero != null)
            {
                try
                {
                    var appearance = CharacterAppearanceSnapshot.FromHero(hero, hero.BattleEquipment);
                    person.Profile = HeroVisualExtractor.Extract(hero, useCivilian: false, appearance: appearance);
                    person.Profile.CurrentStateDetail = string.Empty;
                    person.Profile.Appearance = appearance;
                    person.BannerCode = (hero.Clan?.Banner ?? hero.Clan?.Kingdom?.Banner)?.BannerCode;
                }
                catch (Exception ex) { TaleWorlds.Library.Debug.Print("[Illustrator] Participant snapshot failed: " + id + " " + ex.Message); }
            }
            context.Characters.Add(person);
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
                EventSettlement = ResolveEventSettlement(eventFacts)
            };
            snapshot.ProtagonistEvidence = FindMentionEvidence(eventFacts, snapshot.ProtagonistHero?.Name?.ToString());
            snapshot.EventSettlementEvidence = FindMentionEvidence(eventFacts, snapshot.EventSettlement?.Name?.ToString());

            try
            {
                int seasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
                string seasonName = seasonIndex == 0 ? IllustratorText.Get("Weekly_600") : seasonIndex == 1 ? IllustratorText.Get("Weekly_601") : seasonIndex == 2 ? IllustratorText.Get("Weekly_602") : IllustratorText.Get("Weekly_603");
                snapshot.ReportDateLabel = IllustratorText.Format("Weekly_604", CampaignTime.Now.GetYear, seasonName, CampaignTime.Now.GetDayOfSeason + 1);
            }
            catch
            {
            }

            return snapshot;
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
            if (lower.Contains(IllustratorText.Raw("Weekly_605")) || lower.Contains(IllustratorText.Raw("Weekly_606")) || lower.Contains(IllustratorText.Raw("Weekly_607")) ||
                lower.Contains(IllustratorText.Raw("Weekly_608")) || lower.Contains(IllustratorText.Raw("Weekly_609")) || lower.Contains("execution") ||
                lower.Contains("beheaded") || lower.Contains("executed"))
            {
                return WeeklyReportEventTheme.PrisonerExecution;
            }

            // 海上交战/舰队行动
            if (lower.Contains(IllustratorText.Raw("Weekly_610")) || lower.Contains(IllustratorText.Raw("Weekly_611")) || lower.Contains(IllustratorText.Raw("Weekly_612")) ||
                lower.Contains(IllustratorText.Raw("Weekly_613")) || lower.Contains(IllustratorText.Raw("Weekly_614")) || lower.Contains(IllustratorText.Raw("Weekly_615")) ||
                lower.Contains("naval") || lower.Contains("fleet") || lower.Contains("fleet"))
            {
                return WeeklyReportEventTheme.Naval;
            }

            // 定居点陷落/易主（战后状态，区别于进行中的围攻）
            if (lower.Contains(IllustratorText.Raw("Weekly_616")) || lower.Contains(IllustratorText.Raw("Weekly_617")) || lower.Contains(IllustratorText.Raw("Weekly_618")) ||
                lower.Contains(IllustratorText.Raw("Weekly_619")) || lower.Contains(IllustratorText.Raw("Weekly_620")) || lower.Contains(IllustratorText.Raw("Weekly_621")) ||
                lower.Contains(IllustratorText.Raw("Weekly_622")) || lower.Contains(IllustratorText.Raw("Weekly_623")) || lower.Contains(IllustratorText.Raw("Weekly_624")) ||
                lower.Contains("fallen") || lower.Contains("captured") || lower.Contains("surrendered") || lower.Contains("ceded"))
            {
                return WeeklyReportEventTheme.SettlementChange;
            }

            // 村庄遭扰、袭击、劫掠
            if (lower.Contains(IllustratorText.Raw("Weekly_625")) || lower.Contains(IllustratorText.Raw("Weekly_626")) || lower.Contains(IllustratorText.Raw("Weekly_627")) ||
                lower.Contains(IllustratorText.Raw("Weekly_628")) || lower.Contains(IllustratorText.Raw("Weekly_629")) || lower.Contains(IllustratorText.Raw("Weekly_630")) ||
                lower.Contains("raid") || lower.Contains("pillage") || lower.Contains("loot"))
            {
                return WeeklyReportEventTheme.VillageRaid;
            }

            // 围攻防守战
            if (lower.Contains(IllustratorText.Raw("Weekly_631")) || lower.Contains(IllustratorText.Raw("Weekly_632")) || lower.Contains(IllustratorText.Raw("Weekly_633")) ||
                lower.Contains(IllustratorText.Raw("Weekly_634")) || lower.Contains(IllustratorText.Raw("Weekly_635")) || lower.Contains(IllustratorText.Raw("Weekly_636")) ||
                lower.Contains("siege") || lower.Contains("assault"))
            {
                return WeeklyReportEventTheme.Siege;
            }

            // 野外战役交锋
            if (lower.Contains(IllustratorText.Raw("Weekly_637")) || lower.Contains(IllustratorText.Raw("Weekly_638")) || lower.Contains(IllustratorText.Raw("Weekly_639")) ||
                lower.Contains(IllustratorText.Raw("Weekly_640")) || lower.Contains(IllustratorText.Raw("Weekly_641")) || lower.Contains(IllustratorText.Raw("Weekly_642")) ||
                lower.Contains(IllustratorText.Raw("Weekly_643")) || lower.Contains(IllustratorText.Raw("Weekly_644")) || lower.Contains("battle") || lower.Contains("clash"))
            {
                return WeeklyReportEventTheme.FieldBattle;
            }

            // 重要人物亡故/权力交接
            if (lower.Contains(IllustratorText.Raw("Weekly_645")) || lower.Contains(IllustratorText.Raw("Weekly_646")) || lower.Contains(IllustratorText.Raw("Weekly_647")) ||
                lower.Contains(IllustratorText.Raw("Weekly_648")) || lower.Contains(IllustratorText.Raw("Weekly_649")) || lower.Contains(IllustratorText.Raw("Weekly_650")) ||
                lower.Contains("died") || lower.Contains("succession") || lower.Contains("abdicated"))
            {
                return WeeklyReportEventTheme.DynastyDeath;
            }

            // 宴会、比武竞技
            if (lower.Contains(IllustratorText.Raw("Weekly_651")) || lower.Contains(IllustratorText.Raw("Weekly_652")) || lower.Contains(IllustratorText.Raw("Weekly_653")) ||
                lower.Contains(IllustratorText.Raw("Weekly_654")) || lower.Contains(IllustratorText.Raw("Weekly_655")) || lower.Contains("tournament") || lower.Contains("feast"))
            {
                return WeeklyReportEventTheme.FeastTournament;
            }

            // 外交和谈、宣战
            if (lower.Contains(IllustratorText.Raw("Weekly_656")) || lower.Contains(IllustratorText.Raw("Weekly_657")) || lower.Contains(IllustratorText.Raw("Weekly_658")) ||
                lower.Contains(IllustratorText.Raw("Weekly_659")) || lower.Contains(IllustratorText.Raw("Weekly_660")) || lower.Contains("peace") || lower.Contains("war") || lower.Contains("treaty"))
            {
                return WeeklyReportEventTheme.Diplomacy;
            }

            // 俘虏/囚禁（弱信号，靠后——处决词已在前命中）
            if (lower.Contains(IllustratorText.Raw("Weekly_661")) || lower.Contains(IllustratorText.Raw("Weekly_662")) || lower.Contains(IllustratorText.Raw("Weekly_663")) ||
                lower.Contains(IllustratorText.Raw("Weekly_664")) || lower.Contains("captive") || lower.Contains("prisoner"))
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
            return IllustratorText.Get("Weekly_665") +
                IllustratorText.Get("Weekly_666") +
                IllustratorText.Get("Weekly_667") +
                IllustratorText.Get("Weekly_668") +
                IllustratorText.Get("Weekly_669") +
                IllustratorText.Get("Weekly_670");
        }

        private static string ExtractHeadline(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }
            // 按完整句提取证据前不能截断，否则尾部的失败/撤退/未遂会丢失。
            int minorStart = body.IndexOf(IllustratorText.Raw("Weekly_671"), StringComparison.Ordinal);
            if (minorStart >= 0) body = body.Substring(0, minorStart);
            return NarrativeFactRouter.CleanText(body);
        }

        private static string CleanText(string text)
        {
            return NarrativeFactRouter.CleanText(text);
        }
    }
}
