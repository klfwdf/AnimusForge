using System;
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

        public string BuildCompositeContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 【卡拉迪亚现场二人对话与重大交锋会面】 ===");
            if (!string.IsNullOrWhiteSpace(DialogueSentence))
            {
                sb.AppendLine($"【现场对话要旨】\"{DialogueSentence}\"");
            }

            if (!string.IsNullOrWhiteSpace(SceneDirective))
            {
                sb.AppendLine();
                sb.AppendLine("=== 【画面核心指导原则 (二人面对面交谈/对峙，强烈的戏剧叙事感)】 ===");
                sb.AppendLine(SceneDirective);
            }

            sb.AppendLine();
            sb.AppendLine("=== 【当事人甲 (玩家主角视觉设定)】 ===");
            if (MainHeroProfile != null)
            {
                sb.AppendLine(MainHeroProfile.BuildSummary());
            }

            sb.AppendLine();
            sb.AppendLine("=== 【当事人乙 (对话对方视觉设定)】 ===");
            if (InterlocutorProfile != null)
            {
                sb.AppendLine(InterlocutorProfile.BuildSummary());
            }
            else if (InterlocutorCharacter != null)
            {
                sb.AppendLine($"【人物】{InterlocutorCharacter.Name} ({InterlocutorCharacter.Culture?.Name}文化, {(InterlocutorCharacter.IsFemale ? "女性" : "男性")})");
            }

            sb.AppendLine();
            sb.AppendLine("=== 【所处时空环境与氛围描写】 ===");
            if (EnvironmentProfile != null)
            {
                sb.AppendLine(EnvironmentProfile.BuildSummary());
            }

            return sb.ToString().TrimEnd();
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
                    playerIsDefender = true; // 遭遇外敌交谈默认居城守卫
                }
            }

            // 4. 生成精准现场身姿与空间交互指令
            string poseDirective;
            if (isUnderSiege)
            {
                if (playerIsDefender)
                {
                    string bodyguardText = bodyguardCount > 0
                        ? $"敌将战马身侧肃立着 {bodyguardCount} 名全副武装、身披重锁甲、手持数米高长矛与战盾的精锐近卫步兵战士严阵护卫；"
                        : "敌将身侧有警戒甲士手持长矛肃穆护卫；";

                    poseDirective =
                        $"【极其关键核心构图：城楼俯瞰与城下战马仰视的高低差城墙对峙构图，绝非平地站立！】\n" +
                        $"空间位置：\n" +
                        $"- 守城统帅（{mainName}）全副重铠，屹立于上方高耸险峻的城堡石砌城堞垛口/箭垛箭楼之上，单手按在佩剑剑柄，居高临下严峻俯瞰审视城下；\n" +
                        $"- 敌军统帅（{partnerName}）全副精工战甲战盔，跨骑在一匹雄壮的具装战马鞍座之上，立于下方紧闭的要塞包铁城门与护城河壕沟外数丈开阔泥泞空地，昂首仰望城头守将；\n" +
                        $"- {bodyguardText}\n" +
                        $"构图关系：双方纵深与高度差极其鲜明，守城将领在上俯视，攻城统帅在下昂首对视，目光在城门壕沟半空中激烈碰撞交锋，充满战前谈判的火药味与剑拔弩张的大敌压境之势！";
                }
                else
                {
                    poseDirective =
                        $"【攻城大军兵临城下对峙构图】\n" +
                        $"- 攻城统帅（{mainName}）跨骑战马率军立于城外护城壕前；\n" +
                        $"- 守军将领（{partnerName}）屹立于上方要塞城堞石垛之后向下探身俯瞰喊话；\n" +
                        $"呈现高低对视的围城谈判气场。";
                }
            }
            else if (partnerIsMounted && !playerIsMounted)
            {
                poseDirective = $"【骑兵将领与立姿对话构图】{partnerName}跨骑于高大披甲战马之上，居高临下与面前的主角交谈会晤，神采英武威严。";
            }
            else
            {
                poseDirective = "【现场互动身姿】二人自然面对面立姿交谈 (Natural standing conversation posture)";
                try
                {
                    if (partnerAgent != null)
                    {
                        string actionName = partnerAgent.GetCurrentAction(0).GetName() ?? string.Empty;
                        poseDirective = MapActionToPoseDirective(actionName);
                    }
                    else if (partnerChar != null)
                    {
                        string idleName = Helpers.CharacterHelper.GetStandingBodyIdle(partnerChar, MobileParty.MainParty?.Party);
                        poseDirective = MapIdleToPoseDirective(idleName);
                    }
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[Illustrator] Failed to resolve conversation pose: {ex.Message}");
                }
            }

            string specificSceneGuidance = string.Empty;
            if (!string.IsNullOrWhiteSpace(context.EnvironmentProfile?.SpecificLocation))
            {
                specificSceneGuidance += $"【现场具体环境与空间布局】：双方身处【{context.EnvironmentProfile.SpecificLocation}】。\n{context.EnvironmentProfile.IndoorOutdoorDetails}\n";
            }
            if (!string.IsNullOrWhiteSpace(context.EnvironmentProfile?.SurroundingCharacters))
            {
                specificSceneGuidance += $"【周围在场人物与动向】：{context.EnvironmentProfile.SurroundingCharacters}\n";
            }
            if (!string.IsNullOrWhiteSpace(context.EnvironmentProfile?.SurroundingProps))
            {
                specificSceneGuidance += $"【近景与周围陈设道具】：{context.EnvironmentProfile.SurroundingProps}\n";
            }

            context.SceneDirective =
                $"【核心要求：生动的二人面对面会晤或要塞战前对峙场景】\n" +
                $"画面聚焦于在【{locName}】展开的谈判与交锋。两位主角分别为【{mainName}】与【{partnerName}】。\n" +
                $"{specificSceneGuidance}" +
                $"画面生动刻画交锋时的神情与眼神交锋，当前对话论题为：\"{context.DialogueSentence}\"。\n" +
                $"{poseDirective}\n" +
                $"必须严格还原双方的文化外貌、身着铠甲、真实武器与坐骑，背景精确融入当前所处的真实环境特征。";

            return context;
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
