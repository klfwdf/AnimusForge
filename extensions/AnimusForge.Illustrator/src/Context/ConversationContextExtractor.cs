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

            string siegeDirection = isUnderSiege
                ? (playerIsDefender
                    ? "当前是围城会面，玩家一方处于守方；可选择城垛上下关系、城门前交涉或贴近人物的紧张过肩镜头"
                    : "当前是围城会面，玩家一方处于攻方；可选择阵前交涉、城门远景或双方神情近景")
                : string.Empty;
            string guardDirection = bodyguardCount > 0 ? $"现场确认对方随行队列中另有 {bodyguardCount} 名角色，可按构图需要收入背景" : "未确认额外随行角色，不必强行添加护卫";

            context.SceneDirective =
                $"围绕【{mainName}】与【{partnerName}】最近三轮对话选择最能表现关系变化、情绪转折或利益冲突的一个瞬间。\n" +
                $"已确认空间关系：{mountPosture}；{guardDirection}。\n" +
                $"现场动作参考：{basePose}。\n" +
                (string.IsNullOrWhiteSpace(siegeDirection) ? string.Empty : siegeDirection + "。\n") +
                $"地点为【{locName}】。允许环境占据较大画面，也允许聚焦手势、目光、沉默或转身等细节；不要求每次都正面对称站立。";

            return context;
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
