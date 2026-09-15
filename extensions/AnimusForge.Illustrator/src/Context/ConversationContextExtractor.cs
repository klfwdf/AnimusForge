using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Illustrator.Context
{
    public sealed class ConversationVisualContext
    {
        public string DialogueSentence { get; set; } = string.Empty;
        public Hero MainHero { get; set; }
        public Hero InterlocutorHero { get; set; }
        public CharacterObject InterlocutorCharacter { get; set; }
        public HeroVisualProfile MainHeroProfile { get; set; }
        public HeroVisualProfile InterlocutorProfile { get; set; }
        public EnvironmentVisualProfile EnvironmentProfile { get; set; }
        public string SceneDirective { get; set; } = string.Empty;
        public string RecentDialogueHistory { get; set; } = string.Empty;
        public bool InterlocutorCivilian { get; set; }

        public string BuildHardFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(DialogueSentence)) sb.AppendLine($"【当前台词】\"{DialogueSentence}\"");
            if (!string.IsNullOrWhiteSpace(RecentDialogueHistory)) sb.AppendLine(RecentDialogueHistory);
            sb.AppendLine("【玩家主角】");
            if (MainHeroProfile != null) sb.AppendLine(MainHeroProfile.BuildSummary());
            sb.AppendLine("【对话对象】");
            if (InterlocutorProfile != null)
            {
                sb.AppendLine(InterlocutorProfile.BuildSummary());
            }
            else if (InterlocutorCharacter != null)
            {
                sb.AppendLine($"【人物】{InterlocutorCharacter.Name} ({InterlocutorCharacter.Culture?.Name}文化, {(InterlocutorCharacter.IsFemale ? "女性" : "男性")})");
            }
            if (EnvironmentProfile != null)
            {
                sb.AppendLine("【当前现场】");
                sb.AppendLine(EnvironmentProfile.BuildHardFactsSummary());
            }
            return sb.ToString().TrimEnd();
        }

        public string BuildArtDirection(string sceneVariation = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(SceneDirective)) sb.AppendLine(SceneDirective);
            if (EnvironmentProfile != null)
            {
                string environmentDirection = EnvironmentProfile.BuildArtDirectionSummary();
                if (!string.IsNullOrWhiteSpace(environmentDirection)) sb.AppendLine(environmentDirection);
            }
            if (!string.IsNullOrWhiteSpace(sceneVariation)) sb.AppendLine(sceneVariation);
            return sb.ToString().TrimEnd();
        }

        public string BuildCompositeContext()
        {
            return BuildHardFacts() + "\n\n【开放艺术指导】\n" + BuildArtDirection();
        }
    }

    public static class ConversationContextExtractor
    {
        public static ConversationVisualContext ExtractFromCurrentConversation()
        {
            var context = new ConversationVisualContext();

            Hero mainHero = Hero.MainHero;
            context.MainHero = mainHero;

            CharacterObject partnerChar = null;
            try
            {
                partnerChar = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter;
            }
            catch
            {
            }

            Hero partnerHero = null;
            try
            {
                partnerHero = Hero.OneToOneConversationHero ?? partnerChar?.HeroObject;
            }
            catch
            {
            }

            context.InterlocutorCharacter = partnerChar;
            context.InterlocutorHero = partnerHero;

            Settlement settlement = Settlement.CurrentSettlement ?? (mainHero != null ? mainHero.CurrentSettlement : null);

            // 1. 精确判定围城战局、敌军对峙与军事实战环境 (杜绝围城将领穿平民托加长袍)
            bool isUnderSiege = false;
            try
            {
                if (settlement != null && (settlement.IsUnderSiege || settlement.SiegeEvent != null))
                {
                    isUnderSiege = true;
                }
                else if (TaleWorlds.CampaignSystem.Siege.PlayerSiege.PlayerSiegeEvent != null)
                {
                    isUnderSiege = true;
                }
                else if (PlayerEncounter.Current?.EncounterSettlementAux?.IsUnderSiege == true)
                {
                    isUnderSiege = true;
                }
                else if (MobileParty.MainParty?.BesiegedSettlement != null)
                {
                    isUnderSiege = true;
                }
            }
            catch
            {
            }

            bool isEnemyEncounter = false;
            if (partnerHero != null && Hero.MainHero != null)
            {
                try
                {
                    if (FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, partnerHero.MapFaction) ||
                        (partnerHero.Clan != null && FactionManager.IsAtWarAgainstFaction(Hero.MainHero.Clan, partnerHero.Clan)))
                    {
                        isEnemyEncounter = true;
                    }
                }
                catch
                {
                }
            }

            // 只有在明确和平时期且非围城、非敌对军阵对峙时，才穿平民便服
            bool isCivilian = false;
            if (!isUnderSiege && !isEnemyEncounter)
            {
                if (TaleWorlds.MountAndBlade.Mission.Current != null)
                {
                    isCivilian = TaleWorlds.MountAndBlade.Mission.Current.DoesMissionRequireCivilianEquipment;
                }
                else if (settlement != null)
                {
                    isCivilian = true;
                }
            }

            if (mainHero != null)
            {
                context.MainHeroProfile = HeroVisualExtractor.Extract(mainHero, useCivilian: isCivilian);
            }

            if (partnerHero != null)
            {
                bool partnerCivilian = isCivilian || partnerHero.IsNoncombatant || (partnerHero.IsWanderer && partnerHero.PartyBelongedTo == null) || partnerHero.IsNotable;
                context.InterlocutorCivilian = partnerCivilian;
                context.InterlocutorProfile = HeroVisualExtractor.Extract(partnerHero, useCivilian: partnerCivilian);
            }

            string sentence = string.Empty;
            try
            {
                sentence = Campaign.Current?.ConversationManager?.CurrentSentenceText ?? string.Empty;
            }
            catch
            {
            }
            context.DialogueSentence = CleanText(sentence);
            context.RecentDialogueHistory = BuildRecentDialogueHistory(ReadNativeConversationHistory(24), maxRounds: 3);
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(settlement);
            try
            {
                string hostScene = TryGetHostSceneDescription();
                if (!string.IsNullOrWhiteSpace(hostScene) && context.EnvironmentProfile != null)
                {
                    context.EnvironmentProfile.HostSceneDescription = hostScene;
                    // 宿主快照与聊天提示词同源：若它明确指向野外/海上而我们判成了城镇街道，以宿主为准纠正
                    bool hostIsWild = hostScene.Contains("野外") || hostScene.Contains("平原") || hostScene.Contains("森林")
                        || hostScene.Contains("山地") || hostScene.Contains("荒原") || hostScene.Contains("海上") || hostScene.Contains("海岸");
                    string loc = context.EnvironmentProfile.SpecificLocation ?? string.Empty;
                    bool weSaidTown = loc.Contains("城镇") || loc.Contains("市集") || loc.Contains("街道") || loc.Contains("Marketplace");
                    if (hostIsWild && weSaidTown)
                    {
                        context.EnvironmentProfile.SpecificLocation = "开阔旷野会面地（宿主确认：" + hostScene + "）";
                        context.EnvironmentProfile.IndoorOutdoorDetails = "开阔苍茫的旷野临阵会面之地：起伏的草地丘陵与远处隐现的群山地平线，双方军队的旌旗仪仗在身后列阵隐约可见，空气中弥漫着战前谈判的紧绷肃杀气息。";
                    }
                }
            }
            catch
            {
            }

            // 俘虏处境修正：对话任一方为战俘时，物理现场以关押处为准（地牢/营地囚笼），并注入囚禁动态
            string captiveDirective = string.Empty;
            try
            {
                Hero captive = (partnerHero != null && partnerHero.IsPrisoner) ? partnerHero
                    : ((mainHero != null && mainHero.IsPrisoner) ? mainHero : null);
                if (captive != null && context.EnvironmentProfile != null)
                {
                    string captiveName = captive == mainHero ? "玩家"
                        : (captive.Name != null ? captive.Name.ToString() : "对方");
                    PartyBase holder = null;
                    try { holder = captive.PartyBelongedToAsPrisoner; } catch { }
                    string holderDesc = string.Empty;
                    bool heldInSettlement = false;
                    bool heldByParty = false;
                    if (holder != null)
                    {
                        try
                        {
                            heldInSettlement = holder.IsSettlement && holder.Settlement != null;
                            heldByParty = holder.IsMobile && holder.MobileParty != null;
                            if (heldInSettlement)
                            {
                                holderDesc = (holder.Settlement.Name != null ? holder.Settlement.Name.ToString() : holder.Settlement.StringId) + "的地牢";
                            }
                            else if (heldByParty)
                            {
                                holderDesc = (holder.MobileParty.Name != null ? holder.MobileParty.Name.ToString() : "一支队伍") + "的队伍";
                            }
                        }
                        catch
                        {
                        }
                    }

                    if (heldInSettlement)
                    {
                        context.EnvironmentProfile.SpecificLocation = "阴暗地牢囚室 (Settlement Dungeon Cell)";
                        context.EnvironmentProfile.IndoorOutdoorDetails = "幽暗压抑的石砌地牢：粗粝石墙渗着水汽，铁栅栏门与摇曳火把投下微弱光影，俘虏锁链加身坐于草堆，门外隐约有狱卒卫兵值守。";
                    }
                    else if (heldByParty)
                    {
                        context.EnvironmentProfile.SpecificLocation = "行军营地旁的囚笼/囚车 (Field Prisoner Cage)";
                        context.EnvironmentProfile.IndoorOutdoorDetails = "旷野行军营地中的简陋木栅囚笼或囚车：俘虏被锁链看押席地而坐，四周是篝火帐篷、堆放辎重与巡视的武装卫兵。";
                    }
                    captiveDirective = $"【俘虏处境】{captiveName}现为阶下囚" + (string.IsNullOrWhiteSpace(holderDesc) ? "" : $"（被关押于{holderDesc}）") +
                        "：锁链/镣铐加身、武器已被收缴、衣着为被俘后的简朴凌乱装束而非战甲；画面应体现囚禁、看押、审讯或赎买谈判的压抑权力关系，绝非自由平等的会面";
                }
            }
            catch
            {
            }

            string mainName = mainHero != null && mainHero.Name != null ? mainHero.Name.ToString() : "主角";
            string partnerName = partnerHero != null && partnerHero.Name != null ? partnerHero.Name.ToString() : (partnerChar != null && partnerChar.Name != null ? partnerChar.Name.ToString() : "对方");
            string subLoc = !string.IsNullOrWhiteSpace(context.EnvironmentProfile?.SpecificLocation)
                ? $" · {context.EnvironmentProfile.SpecificLocation}"
                : "";
            string locName = (settlement != null && settlement.Name != null ? settlement.Name.ToString() : "卡拉迪亚会面现场") + subLoc;

            // 2. 检测会话现场 Agent 状态：坐骑、护卫与站位
            TaleWorlds.MountAndBlade.Agent partnerAgent = null;
            var convAgents = Campaign.Current?.ConversationManager?.ConversationAgents;
            if (convAgents != null && convAgents.Count > 0)
            {
                partnerAgent = convAgents[0] as TaleWorlds.MountAndBlade.Agent;
            }

            bool partnerIsMounted = partnerAgent != null && (partnerAgent.HasMount || partnerAgent.MountAgent != null);
            TaleWorlds.MountAndBlade.Agent playerAgent = TaleWorlds.MountAndBlade.Mission.Current?.MainAgent;
            bool playerIsMounted = playerAgent != null && (playerAgent.HasMount || playerAgent.MountAgent != null);
            int bodyguardCount = (convAgents != null && convAgents.Count > 1) ? convAgents.Count - 1 : 0;

            // 3. 判定围城攻防双方阵营与城楼高低差空间关系
            bool playerIsDefender = false;
            if (isUnderSiege)
            {
                try
                {
                    if (TaleWorlds.CampaignSystem.Siege.PlayerSiege.PlayerSiegeEvent != null)
                    {
                        playerIsDefender = TaleWorlds.CampaignSystem.Siege.PlayerSiege.PlayerSide == BattleSideEnum.Defender;
                    }
                    else if (TaleWorlds.MountAndBlade.Mission.Current?.PlayerTeam != null)
                    {
                        playerIsDefender = TaleWorlds.MountAndBlade.Mission.Current.PlayerTeam.IsDefender;
                    }
                    else if (PlayerEncounter.InsideSettlement || (settlement != null && settlement.MapFaction == Hero.MainHero?.MapFaction))
                    {
                        playerIsDefender = true;
                    }
                }
                catch
                {
                }
            }

            // 4. 生成精准现场身姿与空间交互指令
            string mountPosture;
            if (partnerIsMounted && !playerIsMounted)
            {
                mountPosture = $"对方（{partnerName}）骑乘，玩家（{mainName}）步行立于地面";
            }
            else if (!partnerIsMounted && playerIsMounted)
            {
                mountPosture = $"玩家（{mainName}）骑乘，对方（{partnerName}）步行立于地面";
            }
            else if (partnerIsMounted && playerIsMounted)
            {
                mountPosture = "双方均处于骑乘状态";
            }
            else
            {
                mountPosture = "双方均步行立于地面";
            }

            string basePose = "现场动作未能精确识别，可采用符合对话情绪的自然姿态";
            try
            {
                if (partnerAgent != null)
                {
                    string actionName = partnerAgent.GetCurrentAction(0).GetName() ?? string.Empty;
                    basePose = MapActionToPoseDirective(actionName);
                }
                else if (partnerChar != null)
                {
                    string idleName = Helpers.CharacterHelper.GetStandingBodyIdle(partnerChar, MobileParty.MainParty?.Party);
                    basePose = MapIdleToPoseDirective(idleName);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Failed to resolve conversation pose: {ex.Message}");
            }

            bool siegeFieldParley = false;
            if (isUnderSiege)
            {
                try
                {
                    siegeFieldParley = TaleWorlds.MountAndBlade.Mission.Current != null
                        && CampaignMission.Current?.Location == null;
                }
                catch
                {
                }
            }

            string siegeDirection = isUnderSiege
                ? (siegeFieldParley
                    ? (playerIsDefender
                        ? "当前是围城中的阵前旷野谈判，玩家一方为守方出城会面；画面应置于城墙之外的旷野，远处可见被围城池剪影与围城军营篝火，双方驻马交涉"
                        : "当前是围城中的阵前旷野谈判，玩家一方为攻方；画面应置于围城军营与城墙之间的旷野，远处可见被围城池剪影，双方驻马交涉")
                    : (playerIsDefender
                        ? "当前是围城会面，玩家一方处于守方；可选择城垛上下关系、城门前交涉或贴近人物的紧张过肩镜头"
                        : "当前是围城会面，玩家一方处于攻方；可选择阵前交涉、城门远景或双方神情近景"))
                : string.Empty;
            string guardDirection = bodyguardCount > 0 ? $"现场确认对方随行队列中另有 {bodyguardCount} 名角色，可按构图需要收入背景" : "未确认额外随行角色，不必强行添加护卫";

            context.SceneDirective =
                (string.IsNullOrWhiteSpace(captiveDirective) ? string.Empty : captiveDirective + "。\n") +
                $"围绕【{mainName}】与【{partnerName}】最近三轮对话选择最能表现关系变化、情绪转折或利益冲突的一个瞬间。\n" +
                $"已确认空间关系：{mountPosture}；{guardDirection}。\n" +
                $"现场动作参考：{basePose}。\n" +
                (string.IsNullOrWhiteSpace(siegeDirection) ? string.Empty : siegeDirection + "。\n") +
                $"地点为【{locName}】。允许环境占据较大画面，也允许聚焦手势、目光、沉默或转身等细节；不要求每次都正面对称站立。";

            return context;
        }

        /// <summary>
        /// 反射读取主模组 AnimusForge.ShoutUtils.GetCurrentSceneDescription() 的场景快照，
        /// 与主模组聊天提示词的场景注入同源；主模组未加载或失败时返回空串。
        /// </summary>
        private static string TryGetHostSceneDescription()
        {
            try
            {
                var type = HarmonyLib.AccessTools.TypeByName("AnimusForge.ShoutUtils");
                var method = type?.GetMethod("GetCurrentSceneDescription",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                return (method?.Invoke(null, null) as string ?? string.Empty).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 主模组原生会话历史的本地镜像行（仅读取 Illustrator 需要的字段）。
        /// </summary>
        internal sealed class NativeDialogueLine
        {
            public string Kind = "";
            public string Speaker = "";
            public string Text = "";
        }

        /// <summary>
        /// 通过 *ForExternal 公共契约反射读取主模组会话历史。主模组由他人独立重构，
        /// 一律走反射：宿主版本偏旧、缺 API 或字段改名时只降级为无历史，绝不抛 MissingMethod。
        /// 调用频率：每次生成插画一次，最多 24 行，非热路径。
        /// </summary>
        private static List<NativeDialogueLine> ReadNativeConversationHistory(int maxLines)
        {
            var lines = new List<NativeDialogueLine>();
            try
            {
                var type = HarmonyLib.AccessTools.TypeByName("AnimusForge.ShoutBehavior");
                var method = HarmonyLib.AccessTools.Method(type, "GetNativeConversationSessionHistoryEntriesForExternal", new[] { typeof(int) });
                var raw = method?.Invoke(null, new object[] { maxLines }) as System.Collections.IEnumerable;
                if (raw == null) return lines;
                foreach (var entry in raw)
                {
                    if (entry == null) continue;
                    var entryType = entry.GetType();
                    lines.Add(new NativeDialogueLine
                    {
                        Kind = (HarmonyLib.AccessTools.Property(entryType, "Kind")?.GetValue(entry) as string) ?? "",
                        Speaker = (HarmonyLib.AccessTools.Property(entryType, "Speaker")?.GetValue(entry) as string) ?? "",
                        Text = (HarmonyLib.AccessTools.Property(entryType, "Text")?.GetValue(entry) as string) ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Native conversation history read failed: {ex.Message}");
            }
            return lines;
        }

        internal static string BuildRecentDialogueHistory(IEnumerable<NativeDialogueLine> entries, int maxRounds)
        {
            var lines = (entries ?? Enumerable.Empty<NativeDialogueLine>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Text) &&
                    (string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Kind, "npc", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (lines.Count == 0 || maxRounds <= 0) return string.Empty;

            var rounds = new List<List<NativeDialogueLine>>();
            List<NativeDialogueLine> current = null;
            foreach (var entry in lines)
            {
                if (string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase) || current == null)
                {
                    current = new List<NativeDialogueLine>();
                    rounds.Add(current);
                }
                current.Add(entry);
            }
            rounds = rounds.Skip(Math.Max(0, rounds.Count - maxRounds)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"【最近{rounds.Count}轮对话记录】");
            for (int i = 0; i < rounds.Count; i++)
            {
                sb.AppendLine($"第{i + 1}轮：");
                foreach (var entry in rounds[i])
                {
                    string speaker = string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase)
                        ? "玩家"
                        : (!string.IsNullOrWhiteSpace(entry.Speaker) ? entry.Speaker.Trim() : "对方");
                    string text = CleanText(entry.Text);
                    if (text.Length > 240) text = text.Substring(0, 240) + "…";
                    sb.AppendLine(speaker + "：" + text);
                }
            }
            return sb.ToString().TrimEnd();
        }

        private static string MapActionToPoseDirective(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return "【现场互动身姿】二人自然面对面立姿交谈";
            if (actionName.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0 || actionName.IndexOf("cross", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿(关键)】对话对方双臂交叠紧紧环抱于胸前，神态严谨审视 (Both arms crossed tightly across chest, standing in a reserved, skeptical, or analytical scholarly posture)";
            }
            if (actionName.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】对话对方单手叉腰，身形放松自信 (One hand casually resting on hip, confident stance)";
            }
            if (actionName.IndexOf("aggressive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】身躯前倾，姿态迫人威严 (Leaning forward in an intense, commanding or aggressive stance)";
            }
            if (actionName.IndexOf("warrior", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】机警戒备的战士立姿，单手微搭在佩剑腰带侧 (Alert warrior stance, hand resting near belt or sword hilt)";
            }
            if (actionName.IndexOf("demure", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】双手交叠于身前，谦逊守礼 (Hands folded politely in front, humble and composed posture)";
            }
            if (actionName.IndexOf("weary", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】身形略显疲惫松弛 (Weary, slightly slumped posture)";
            }
            return "【现场互动身姿】二人自然面对面立姿交谈 (Natural standing conversation posture)";
        }

        private static string MapIdleToPoseDirective(string idleName)
        {
            if (string.IsNullOrWhiteSpace(idleName)) return "【现场互动身姿】二人自然面对面立姿交谈";
            if (idleName.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0 || idleName.IndexOf("cross", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿(关键)】对话对方双臂交叠紧紧环抱于胸前，神态严谨审视 (Both arms crossed tightly across chest, standing in a reserved, skeptical, or analytical scholarly posture)";
            }
            if (idleName.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】对话对方单手叉腰，身形放松自信 (One hand casually resting on hip, confident stance)";
            }
            if (idleName.IndexOf("aggressive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】身躯前倾，姿态迫人威严 (Leaning forward in an intense, commanding or aggressive stance)";
            }
            if (idleName.IndexOf("warrior", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】机警戒备的战士立姿，单手微搭在佩剑腰带侧 (Alert warrior stance, hand resting near belt or sword hilt)";
            }
            if (idleName.IndexOf("demure", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】双手交叠于身前，谦逊守礼 (Hands folded politely in front, humble and composed posture)";
            }
            if (idleName.IndexOf("weary", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "【现场互动动作与身姿】身形略显疲惫松弛 (Weary, slightly slumped posture)";
            }
            return "【现场互动身姿】二人自然面对面立姿交谈 (Natural standing conversation posture)";
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Replace("<a href=\"", "").Replace("</a>", "").Replace("\r\n", " ").Replace("\n", " ").Trim();
        }
    }
}
