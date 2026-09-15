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
    /// 子模块侧周报结构化快照：弹窗文本只解析一次，之后人物、地点、主题与日期全部由快照承载，
    /// 不再在生图链路上二次猜测。地点只在周报文本真实提及定居点时解析，解析不到就标未知，
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
                sb.AppendLine("=== 【登场人物真实视觉档案】 ===");
                sb.AppendLine(ProtagonistProfile.BuildVisualSummary());
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

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Title)) sb.AppendLine("【报头原文】" + Title);
            if (!string.IsNullOrWhiteSpace(Subtitle)) sb.AppendLine("【核心局势原文】" + Subtitle);
            if (!string.IsNullOrWhiteSpace(HeadlineSummary)) sb.AppendLine("【事件要闻原文】" + HeadlineSummary);
            if (ProtagonistProfile != null) sb.AppendLine(ProtagonistProfile.BuildDirectorOnlyFacts());
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
            if (EnvironmentProfile != null)
            {
                string direction = EnvironmentProfile.BuildArtDirectionSummary();
                if (!string.IsNullOrWhiteSpace(direction)) sb.AppendLine(direction);
            }
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
                EventFacts = NarrativeFactRouter.BuildEventEvidence(cleanTitle, cleanSubtitle, cleanHeadline),
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
        /// 无命中时保持未知；收报人不能冒充事件当事人。
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
                case WeeklyReportEventTheme.Naval:
                    profile.SpecificLocation = settlement != null ? $"{locName}外海/港口水域" : "开阔海面战列";
                    profile.IndoorOutdoorDetails = "可参考的海战元素：战船甲板与接舷跳帮、桅杆帆布、船舷弓弩对射、浪花碎木与远处海岸线。";
                    profile.ConflictStatus = "【事件性质】海上交战/舰队行动";
                    break;
                case WeeklyReportEventTheme.PrisonerExecution:
                    profile.SpecificLocation = settlement != null ? $"{locName}地牢囚室或行刑广场" : "地牢囚室或行刑场";
                    profile.IndoorOutdoorDetails = "可参考的囚禁/行刑元素：石砌地牢铁栅、锁链镣铐、行刑台与围观人群、狱卒卫兵、被押解的战俘。";
                    profile.ConflictStatus = "【事件性质】俘虏囚禁/处决行刑";
                    break;
                case WeeklyReportEventTheme.SettlementChange:
                    profile.SpecificLocation = settlement != null ? $"{locName}城门与市集易主现场" : "城池易主现场";
                    profile.IndoorOutdoorDetails = "可参考的易主元素：城头更换的旗帜、列队入城的占领军、燃烧的余烬烟尘、围观或撤离的平民、被收缴的武备。";
                    profile.ConflictStatus = "【事件性质】定居点陷落/易主";
                    break;
                case WeeklyReportEventTheme.DynastyDeath:
                    profile.SpecificLocation = settlement != null ? $"{locName}厅堂或灵堂" : "厅堂/灵堂/继位现场";
                    profile.IndoorOutdoorDetails = "可参考的丧葬继位元素：素缟帷幔与烛火、肃立默哀的家族成员、传递中的冠冕或权戒、凝重的继位仪式。";
                    profile.ConflictStatus = "【事件性质】重要人物亡故/权力交接";
                    break;
                default:
                    // 未命中预设主题时，先按要闻中的场所名词锚定（酒馆/渡口/密林等），
                    // 再退回定居点泛指——绝不用玩家当前位置冒充事件现场。
                    string nounLoc, nounDetail;
                    if (TryResolveSceneNoun(context.HeadlineSummary, settlement, out nounLoc, out nounDetail))
                    {
                        profile.SpecificLocation = nounLoc;
                        if (!string.IsNullOrWhiteSpace(nounDetail))
                        {
                            profile.IndoorOutdoorDetails = nounDetail;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(profile.SpecificLocation) && settlement != null)
                    {
                        profile.SpecificLocation = $"{locName}城内外";
                    }
                    break;
            }
        }

        /// <summary>
        /// 预设主题未命中时的场景名词兜底：扫描要闻中的明确场所词锚定现场。
        /// 词条按特异性排序，命中第一条即返回。
        /// </summary>
        private static bool TryResolveSceneNoun(string headline, Settlement settlement, out string location, out string detail)
        {
            location = string.Empty;
            detail = string.Empty;
            if (string.IsNullOrWhiteSpace(headline)) return false;
            string lower = headline.ToLowerInvariant();
            string locName = settlement != null && settlement.Name != null ? settlement.Name.ToString() : string.Empty;
            string prefix = string.IsNullOrWhiteSpace(locName) ? string.Empty : locName;

            // (关键词, 场所, 氛围参考) —— 命中第一条。场所词一律用复合词，
            // 不用裸单字（"海/山/河"会误中人名地名如"海因茨""山德"）
            var table = new[]
            {
                new[] { "酒馆", "旅店", "tavern", "inn" },
                new[] { "地牢", "监狱", "牢", "dungeon", "gaol" },
                new[] { "竞技场", "决斗", "arena", "duel" },
                new[] { "港口", "码头", "港湾", "port", "harbor" },
                new[] { "海上", "海面", "海边", "战船", "舰船", "舰队", "sea", "ship" },
                new[] { "市场", "市集", "集市", "商队", "商路", "驿站", "market", "caravan" },
                new[] { "密林", "森林", "树林", "狩猎", "forest", "hunt" },
                new[] { "渡口", "河上", "河边", "河口", "桥上", "river", "ford", "bridge" },
                new[] { "山地", "山区", "山脚", "峡谷", "隘口", "mountain", "pass" },
                new[] { "营地", "军营", "行营", "大帐", "camp", "tent" },
                new[] { "王座", "宫廷", "王庭", "throne", "court" },
                new[] { "教堂", "圣堂", "修道院", "神殿", "church", "temple" },
                new[] { "农田", "丰收", "疫病", "瘟疫", "harvest", "plague" },
                new[] { "长城", "城墙", "wall" },
            };
            var sceneText = new[]
            {
                "酒馆/旅店雅座", "地牢囚室", "竞技场/决斗场", "港口码头", "海船甲板",
                "市集商路", "林间猎场", "河岸渡口", "山地隘口", "行营大帐",
                "王座宫廷", "圣堂神殿", "村庄田野", "城墙防线",
            };
            var sceneDetail = new[]
            {
                "可参考元素：昏暗烛光、橡木长桌与酒盏、穿梭的侍者与低声密谈的酒客",
                "可参考元素：石砌牢墙、铁栅锁链、火把微光与狱卒",
                "可参考元素：沙场围栏、欢呼看台、对峙中的斗士与裁判",
                "可参考元素：停靠的帆船、缆绳跳板、搬运货箱的脚夫与海风",
                "可参考元素：甲板桅杆、鼓风的帆、起伏的浪与远处海岸线",
                "可参考元素：货摊帆布、驮货骡马、讨价还价的商贩与尘土飞扬的商路",
                "可参考元素：密林光影、猎手与猎犬、林间小径与倒伏的猎物",
                "可参考元素：渡口浅滩、木桥、涉水的人马与河面波光",
                "可参考元素：崎岖山道、隘口岩壁、盘旋的鹰与远处雪峰",
                "可参考元素：连绵帐篷、篝火炊烟、巡逻卫兵与堆放的辎重",
                "可参考元素：高台王座、垂坠帷幔、廷臣仪仗与火炬烛台",
                "可参考元素：高耸穹顶、彩绘窗光、烛火祭坛与诵经修士",
                "可参考元素：麦浪田垄、农舍炊烟、劳作的农夫或疫病笼罩的空巷",
                "可参考元素：高耸城垣、垛口哨兵、城门吊桥与城下关厢",
            };

            for (int i = 0; i < table.Length; i++)
            {
                string[] keys = table[i];
                bool hit = false;
                for (int k = 0; k < keys.Length; k++)
                {
                    if (lower.Contains(keys[k])) { hit = true; break; }
                }
                if (!hit) continue;
                location = string.IsNullOrWhiteSpace(prefix) ? sceneText[i] : prefix + "的" + sceneText[i];
                detail = sceneDetail[i];
                return true;
            }
            return false;
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

                case WeeklyReportEventTheme.Naval:
                    return $"【事件主题：海上交战/舰队行动】要闻指向【{locName}】附近水域。" +
                           "可选取景：战船接舷跳帮、甲板弓弩对射、船艏破浪的舰队纵队、或落水者攀附碎木——海面广阔，构图自由。";

                case WeeklyReportEventTheme.PrisonerExecution:
                    return $"【事件主题：俘虏囚禁/处决行刑】要闻指向【{locName}】。" +
                           "可选取景：地牢铁栅后的囚徒、押赴行刑台的队伍、刽子手与围观人群、或镣铐中的不屈目光——" +
                           "强调囚禁的压抑与权力威压，绝非自由会面。";

                case WeeklyReportEventTheme.SettlementChange:
                    return $"【事件主题：定居点陷落/易主】要闻指向【{locName}】。" +
                           "可选取景：城头旗帜更换、占领军列队入城、余烬烟尘中的街市、撤离的平民车队、或降者献城的俯首瞬间——" +
                           "事件余波与权力更迭的纪实感。";

                case WeeklyReportEventTheme.DynastyDeath:
                    return $"【事件主题：重要人物亡故/权力交接】要闻指向【{locName}】。" +
                           "可选取景：素缟灵堂与烛火、肃立默哀的族人、冠冕权戒的传递、或继位者接受朝拜的瞬间——庄重肃穆。";

                default:
                    return "【事件主题：以事件证据为准】该事件未落入预设主题，请依据事件行动与结果证据选择场景类型" +
                           "（行军、狩猎、疫病、贸易、密会、决斗、流亡、庆典等皆可，不必拘泥既定类别），" +
                           "自由取景：可选人物行动瞬间、事件余波、或当事人在环境中的决断姿态——避免千篇一律的看风景站桩。";
            }
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
