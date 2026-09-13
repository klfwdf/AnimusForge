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
        public Hero ProtagonistHero { get; set; }
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

            // 1. 从周报正文解析真正的当事人英雄（而非默认主角）——周报要闻可能讲述的是其他领主/君主
            Hero protagonist = ResolveProtagonistHero(fullText);
            Settlement eventSettlement = ResolveEventSettlement(fullText, protagonist);
            WeeklyReportEventTheme theme = ClassifyEventTheme(fullText);

            var context = new WeeklyReportVisualContext
            {
                Title = cleanTitle,
                Subtitle = cleanSubtitle,
                HeadlineSummary = cleanHeadline,
                EventTheme = theme,
                ProtagonistHero = protagonist,
                SceneThemeDirective = GenerateSceneDirective(theme, eventSettlement, cleanHeadline)
            };

            // 提取关键人物视觉（严格反映真实穿着与无胡须特征）
            if (protagonist != null)
            {
                context.ProtagonistProfile = HeroVisualExtractor.Extract(protagonist, useCivilian: false);
            }

            // 提取事件发生地的真实环境视觉——按事件主题锚定，严禁按玩家当前所在菜单/位置推断
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(eventSettlement, eventAnchored: true);
            ApplyEventSceneAnchoring(context, eventSettlement);

            return context;
        }

        /// <summary>
        /// 在周报文本中查找被提及的英雄：优先取文本中最先出现者，其次阵营领袖/宗族首领。
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
        /// 按周报事件主题锁定画面场景，覆盖实时环境探测结果——保证同一份周报多次生成的场景一致。
        /// </summary>
        private static void ApplyEventSceneAnchoring(WeeklyReportVisualContext context, Settlement settlement)
        {
            var profile = context?.EnvironmentProfile;
            if (profile == null) return;
            string locName = settlement != null && settlement.Name != null ? settlement.Name.ToString() : "卡拉迪亚";

            switch (context.EventTheme)
            {
                case WeeklyReportEventTheme.VillageRaid:
                    profile.SpecificLocation = $"{locName}乡野村落的农田与茅舍之间";
                    profile.IndoorOutdoorDetails = "遭袭村庄的实况现场：土石茅草的农舍屋顶正燃起熊熊烈焰，滚滚黑烟遮天蔽日，四散奔逃的农夫村民与挥舞火把的掠夺者交错其间。";
                    profile.SurroundingCharacters = "挥舞火把与弯刀长矛的掠夺骑兵纵马穿行，哭喊奔逃的农夫村民、被拖拽的牲畜散落各处";
                    profile.SurroundingProps = "燃起烈焰的茅草屋顶、被撞倒的木栅栏、散落一地的谷物麻袋与农具、受惊乱窜的鸡犬牲畜、被践踏的泥泞田垄";
                    profile.ConflictStatus = "【村庄遇袭·火光冲天】定居点正遭劫掠焚毁，硝烟与火光交织";
                    break;
                case WeeklyReportEventTheme.FieldBattle:
                    profile.SpecificLocation = $"{locName}外围的开阔旷野战场";
                    profile.IndoorOutdoorDetails = "大军交锋的旷野战场：尘烟弥漫的起伏原野上，步兵盾墙与骑兵冲锋正面撞击，残破的家族战旗在硝烟中飘扬。";
                    profile.SurroundingCharacters = "持枪冲锋的重甲骑兵、结成盾墙的重步兵、后排攒射箭雨的弓弩手与奔走的传令兵";
                    profile.SurroundingProps = "插满箭矢的焦土、倾覆的战旗旗杆、散落的大盾长矛、倒地挣扎的战马与弥漫整个战场的黄褐色尘烟";
                    profile.ConflictStatus = "【野战交锋·大军对阵】双方主力军团正在旷野上殊死搏杀";
                    break;
                case WeeklyReportEventTheme.Siege:
                    profile.SpecificLocation = $"{locName}要塞城墙与城门外围的围攻阵地";
                    profile.IndoorOutdoorDetails = "要塞围攻战的惨烈前沿：高耸石砌城墙上架满云梯，城外壕沟间布满攻城塔楼与破城槌，守军在城堞后拼死抵抗。";
                    profile.SurroundingCharacters = "攀梯而上的攻城士卒、城头倾倒滚油放箭的守军、操作投石机的工兵与督战的双方将领";
                    profile.SurroundingProps = "斜靠城墙的云梯与攻城塔、抛掷巨石的投石机、冲击城门的破城槌、城下密集的拒马鹿砦与堆积的攻城器械残骸";
                    profile.ConflictStatus = "【大军围城·攻守血战】要塞正被重兵围攻，城头城下杀声震天";
                    break;
                case WeeklyReportEventTheme.FeastTournament:
                    profile.SpecificLocation = $"{locName}领主大厅的盛宴厅堂与竞技场地";
                    profile.IndoorOutdoorDetails = "庆典喧腾的贵族厅堂：长条宴桌上觥筹交错，烛台火光摇曳，或竞技场黄沙飞扬、看台人声鼎沸。";
                    profile.SurroundingCharacters = "举杯同饮的贵族领主与贵妇、穿梭斟酒的侍从、竞技场上持枪对冲的比武骑士与欢呼的市民观众";
                    profile.SurroundingProps = "摆满烤肉与美酒的亚麻长桌、成排的银质烛台、悬挂家族纹章的丝绒挂毯、竞技场边缘的木栅栏与彩旗";
                    profile.ConflictStatus = "【盛事庆典】觥筹交错与比武竞技的欢庆时刻";
                    break;
                case WeeklyReportEventTheme.Diplomacy:
                    profile.SpecificLocation = "军事行军大帐与石砌议事厅内的议和谈判现场";
                    profile.IndoorOutdoorDetails = "庄重凝重的谈判现场：行军大帐或大理石议事厅内，双方使节围绕铺满羊皮纸地图的沙盘长桌对峙商榷，烛火映照着紧绷的神情。";
                    profile.SurroundingCharacters = "肃立两侧的披甲护卫、记录条款的宫廷书记官、神情凝重的使节与谋士";
                    profile.SurroundingProps = "摊开的羊皮纸疆域地图与鹅毛笔、封蜡条约卷轴、黄铜烛台、帐外隐约可见的双方仪仗军旗";
                    profile.ConflictStatus = "【外交博弈·剑拔弩张】议和宣战的重大政治角力时刻";
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(profile.SpecificLocation))
                    {
                        profile.SpecificLocation = settlement != null ? $"{locName}城内外" : "卡拉迪亚大地";
                    }
                    break;
            }
        }

        private static Settlement ResolveEventSettlement(string fullText, Hero protagonist)
        {
            if (string.IsNullOrWhiteSpace(fullText))
            {
                return Settlement.CurrentSettlement ?? (protagonist != null ? protagonist.CurrentSettlement : null);
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

            // 无地名命中时优先取事件主角当前所在的定居点，其次其家乡，最后才是玩家当前位置
            return bestMatch ?? protagonist?.CurrentSettlement ?? protagonist?.HomeSettlement ?? Settlement.CurrentSettlement;
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
