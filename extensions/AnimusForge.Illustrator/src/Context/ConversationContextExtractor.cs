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
        public string SceneFacts { get; set; } = string.Empty;
        public string RecentDialogueHistory { get; set; } = string.Empty;
        public bool InterlocutorCivilian { get; set; }
        public bool MainHeroCivilian { get; set; }
        /// <summary>非英雄对话方（劫匪等）在场 Agent 的真实体型/面容序列化——避免用兵种模板重新随机一张脸。</summary>
        public string InterlocutorBodyProperties { get; set; } = string.Empty;
        internal ConversationNpcAgeSnapshot InterlocutorAgeSnapshot { get; set; }

        /// <summary>台词与当前会话最近两轮对话——只进导演，不进生图模型（避免被渲染成画面文字）。</summary>
        public string BuildDialogueBlock()
        {
            var sb = new StringBuilder();
            sb.AppendLine(IllustratorText.Get("Conversation_0"));
            sb.AppendLine(Core.VisualFidelityRules.ConversationActionPriority);
            if (!string.IsNullOrWhiteSpace(RecentDialogueHistory)) sb.AppendLine(RecentDialogueHistory);
            else if (!string.IsNullOrWhiteSpace(DialogueSentence))
                sb.AppendLine(IllustratorText.Format("Conversation_1", DialogueSentence));
            return sb.ToString().TrimEnd();
        }

        public string BuildHardFacts()
        {
            var sb = new StringBuilder();
            sb.AppendLine(IllustratorText.Get("Conversation_2"));
            // Conversation mount presence comes from the live Agent evidence in SceneFacts.
            // The equipment snapshot may contain an available horse even when the current
            // tableau/map conversation has no mounted Agent; exposing that slot makes the
            // image model invent a horse in an otherwise on-foot scene.
            if (MainHeroProfile != null) sb.AppendLine(MainHeroProfile.BuildVisualSummary(includeMount: false));
            sb.AppendLine(IllustratorText.Get("Conversation_3"));
            if (InterlocutorProfile != null)
            {
                sb.AppendLine(InterlocutorProfile.BuildVisualSummary(includeMount: false));
            }
            else if (InterlocutorCharacter != null)
            {
                sb.AppendLine(IllustratorText.Format("Conversation_4", InterlocutorCharacter.Name, InterlocutorCharacter.Culture?.Name, (InterlocutorCharacter.IsFemale ? IllustratorText.Get("Hero_320") : IllustratorText.Get("Hero_321"))));
            }
            if (EnvironmentProfile != null)
            {
                sb.AppendLine(IllustratorText.Get("Conversation_5"));
                sb.AppendLine(EnvironmentProfile.BuildHardFactsSummary());
            }
            // 空间关系是构图硬约束，不能只放在开放艺术指导中，否则导演可能退化为平地对话。
            if (!string.IsNullOrWhiteSpace(SceneFacts))
                sb.AppendLine(IllustratorText.Get("Conversation_6") + SceneFacts);
            return sb.ToString().TrimEnd();
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            sb.AppendLine(BuildDialogueBlock());
            if (MainHeroProfile != null) sb.AppendLine(MainHeroProfile.BuildConversationDirectorContext());
            if (InterlocutorProfile != null) sb.AppendLine(InterlocutorProfile.BuildConversationDirectorContext());
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
            return BuildHardFacts() + IllustratorText.Get("Conversation_7") + BuildDirectorOnlyFacts() + IllustratorText.Get("Conversation_8") + BuildArtDirection();
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

            // Actual mission dress code wins even in an enemy or besieged town.
            // A matched live Agent still overrides this fallback selection below.
            bool isCivilian = Mission.Current != null
                ? Mission.Current.DoesMissionRequireCivilianEquipment
                : settlement != null && !isUnderSiege && !isEnemyEncounter;

            context.MainHeroCivilian = isCivilian;
            if (mainHero != null)
            {
                string source;
                Equipment snapshot = ConversationEquipmentSnapshot.Capture(mainHero, isCivilian, out source, out var appearance);
                context.MainHeroProfile = HeroVisualExtractor.Extract(mainHero, isCivilian, snapshot, source, appearance: appearance);
                context.MainHeroProfile.Appearance = appearance;
            }

            if (partnerHero != null)
            {
                bool partnerCivilian = isCivilian;
                context.InterlocutorCivilian = partnerCivilian;
                string source;
                Equipment snapshot = ConversationEquipmentSnapshot.Capture(partnerHero, partnerCivilian, out source, out var appearance);
                context.InterlocutorProfile = HeroVisualExtractor.Extract(partnerHero, partnerCivilian, snapshot, source, appearance: appearance);
                context.InterlocutorProfile.Appearance = appearance;
            }
            else if (partnerChar != null)
            {
                string source, body;
                Equipment snapshot = ConversationEquipmentSnapshot.CaptureCharacter(partnerChar, out source, out body, out var appearance, out var ageSnapshot);
                context.InterlocutorBodyProperties = body ?? string.Empty;
                context.InterlocutorAgeSnapshot = ageSnapshot;
                int age = ageSnapshot.Age;

                var profile = new HeroVisualProfile
                {
                    HeroName = partnerChar.Name?.ToString() ?? partnerChar.StringId,
                    Culture = partnerChar.Culture?.Name?.ToString() ?? IllustratorText.Get("Conversation_9"),
                    Gender = partnerChar.IsFemale ? IllustratorText.Get("Conversation_10") : IllustratorText.Get("Conversation_11"),
                    SpeciesDescription = HeroVisualExtractor.DescribeSpecies(
                        (partnerChar.StringId ?? string.Empty) + " " + (partnerChar.Culture?.StringId ?? string.Empty) + " " + (partnerChar.Culture?.Name?.ToString() ?? string.Empty),
                        appearance?.Race ?? partnerChar.Race),
                    Age = age,
                    AgeSource = age > 0 ? IllustratorText.Get("Conversation_12")
                        : IllustratorText.Get("Conversation_13"),
                    PhysicalFeatures = HeroVisualExtractor.ExtractCharacterPhysicalFeatures(partnerChar, snapshot, age)
                };

                uint color1 = (appearance != null && appearance.Color1 != 0)
                    ? appearance.Color1
                    : (partnerChar.HeroObject?.Clan?.Kingdom != null && partnerChar.HeroObject.Clan.Kingdom.Color != 0)
                        ? partnerChar.HeroObject.Clan.Kingdom.Color
                        : (partnerChar.HeroObject?.MapFaction != null && partnerChar.HeroObject.MapFaction.Color != 0)
                            ? partnerChar.HeroObject.MapFaction.Color
                            : (partnerChar.Culture != null ? partnerChar.Culture.Color : 0);

                uint color2 = (appearance != null && appearance.Color2 != 0)
                    ? appearance.Color2
                    : (partnerChar.HeroObject?.Clan?.Kingdom != null && partnerChar.HeroObject.Clan.Kingdom.Color2 != 0)
                        ? partnerChar.HeroObject.Clan.Kingdom.Color2
                        : (partnerChar.HeroObject?.MapFaction != null && partnerChar.HeroObject.MapFaction.Color2 != 0)
                            ? partnerChar.HeroObject.MapFaction.Color2
                            : (partnerChar.Culture != null ? partnerChar.Culture.Color2 : 0);
                if (color1 != 0)
                {
                    string c1Name = HeroVisualExtractor.ResolveColorName(color1);
                    string c1Hex = "#" + (color1 & 0x00FFFFFF).ToString("X6");
                    string c2Part = (color2 != 0 && color2 != color1) ? IllustratorText.Format("Conversation_14", HeroVisualExtractor.ResolveColorName(color2), (color2 & 0x00FFFFFF)) : "";
                    profile.LiveryColorsSummary = IllustratorText.Format("Conversation_15", c1Name, c1Hex, c2Part);
                }

                HeroVisualExtractor.ApplyEquipmentSnapshot(profile, snapshot, source);
                profile.Appearance = appearance;
                context.InterlocutorProfile = profile;
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
            context.RecentDialogueHistory = BuildRecentDialogueHistory(ReadNativeConversationHistory(260), RecentDialogueLimit);
            context.EnvironmentProfile = EnvironmentVisualExtractor.Extract(settlement);
            context.EnvironmentProfile.UseConversationTimeEvidence();
            try
            {
                string hostScene = TryGetHostSceneDescription();
                if (!string.IsNullOrWhiteSpace(hostScene) && context.EnvironmentProfile != null)
                {
                    context.EnvironmentProfile.HostSceneDescription = hostScene;
                    string loc = context.EnvironmentProfile.SpecificLocation ?? string.Empty;
                    bool hostIsSea = IsExplicitSeaScene(hostScene);
                    bool weSaidSea = loc.Contains(IllustratorText.Raw("Conversation_16")) || loc.Contains("Naval") || loc.Contains(IllustratorText.Raw("Conversation_17"));
                    bool hostIsWild = hostScene.Contains(IllustratorText.Raw("Conversation_18")) || hostScene.Contains(IllustratorText.Raw("Conversation_19")) || hostScene.Contains(IllustratorText.Raw("Conversation_20"))
                        || hostScene.Contains(IllustratorText.Raw("Conversation_21")) || hostScene.Contains(IllustratorText.Raw("Conversation_22")) || hostScene.Contains(IllustratorText.Raw("Conversation_23"));
                    bool weSaidTown = loc.Contains(IllustratorText.Raw("Conversation_24")) || loc.Contains(IllustratorText.Raw("Conversation_25")) || loc.Contains(IllustratorText.Raw("Conversation_26")) || loc.Contains("Marketplace");
                    if (hostIsSea && !weSaidSea)
                    {
                        context.EnvironmentProfile.SpecificLocation = IllustratorText.Get("Conversation_27") + hostScene + "）";
                        context.EnvironmentProfile.IndoorOutdoorDetails = IllustratorText.Get("Conversation_28");
                    }
                    else if (hostIsWild && weSaidTown)
                    {
                        context.EnvironmentProfile.SpecificLocation = IllustratorText.Get("Conversation_29") + hostScene + "）";
                        context.EnvironmentProfile.IndoorOutdoorDetails = EnvironmentVisualExtractor.NeutralWildDetails;
                    }
                }
            }
            catch
            {
            }

            // 会话入口：战前阵前接触、途中会面、战后处置俘虏、部队界面发起的对话各不相同，野外默认不再一律当作战前谈判。
            // 每次提取一次：少量属性读取，最多两次名册查找，不扫描。
            ConversationEntry entry = ConversationEntryClassifier.Classify(partnerHero, partnerChar, isEnemyEncounter,
                settlement != null, Mission.Current == null);

            // 俘虏处境修正：对话任一方为战俘时，物理现场以关押处为准（地牢/营地囚笼），并注入囚禁动态
            string captiveDirective = string.Empty;
            try
            {
                Hero captive = (partnerHero != null && partnerHero.IsPrisoner) ? partnerHero
                    : ((mainHero != null && mainHero.IsPrisoner) ? mainHero : null);
                if (captive != null && context.EnvironmentProfile != null)
                {
                    string captiveName = captive == mainHero ? IllustratorText.Get("Conversation_30")
                        : (captive.Name != null ? captive.Name.ToString() : IllustratorText.Get("Conversation_31"));
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
                                holderDesc = (holder.Settlement.Name != null ? holder.Settlement.Name.ToString() : holder.Settlement.StringId) + IllustratorText.Get("Conversation_32");
                            }
                            else if (heldByParty)
                            {
                                holderDesc = (holder.MobileParty.Name != null ? holder.MobileParty.Name.ToString() : IllustratorText.Get("Conversation_33")) + IllustratorText.Get("Conversation_34");
                            }
                        }
                        catch
                        {
                        }
                    }

                    captiveDirective = IllustratorText.Format("Conversation_35", captiveName) + (string.IsNullOrWhiteSpace(holderDesc) ? "" : IllustratorText.Format("Conversation_36", holderDesc)) +
                        IllustratorText.Get("Conversation_37");
                }
            }
            catch
            {
            }

            string mainName = mainHero != null && mainHero.Name != null ? mainHero.Name.ToString() : IllustratorText.Get("Conversation_38");
            string partnerName = partnerHero != null && partnerHero.Name != null ? partnerHero.Name.ToString() : (partnerChar != null && partnerChar.Name != null ? partnerChar.Name.ToString() : IllustratorText.Get("Conversation_39"));
            string subLoc = !string.IsNullOrWhiteSpace(context.EnvironmentProfile?.SpecificLocation)
                ? $" · {context.EnvironmentProfile.SpecificLocation}"
                : "";
            string locName = (settlement != null && settlement.Name != null ? settlement.Name.ToString() : IllustratorText.Get("Conversation_40")) + subLoc;

            // 2. 检测会话现场 Agent 状态：坐骑、护卫与站位
            TaleWorlds.MountAndBlade.Agent partnerAgent = null;
            var convAgents = Campaign.Current?.ConversationManager?.ConversationAgents;
            if (convAgents != null && convAgents.Count > 0)
            {
                partnerAgent = convAgents[0] as TaleWorlds.MountAndBlade.Agent;
            }

            // Non-hero face and equipment are frozen together by CaptureCharacter above.

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
            string mountPosture = DescribeMountState(mainName, playerAgent != null, playerIsMounted) + "；" +
                DescribeMountState(partnerName, partnerAgent != null, partnerIsMounted);

            // Empty when no distinctive engine action is recognised: a generic "standing and
            // talking" default only anchors the director to a static composition.
            string basePose = string.Empty;
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

            string siegeFacts = isUnderSiege
                ? IllustratorText.Format("Conversation_41", (playerIsDefender ? IllustratorText.Get("Conversation_58") : IllustratorText.Get("Conversation_56")))
                : string.Empty;
            string siegeForces = isUnderSiege ? DescribeSiegeForces(settlement, playerIsDefender) : string.Empty;
            string elevationFacts = DescribeConversationElevation(playerAgent, partnerAgent, mainName, partnerName);
            string guardFacts = bodyguardCount > 0 ? IllustratorText.Format("Conversation_43", bodyguardCount) : IllustratorText.Get("Conversation_44");

            context.SceneFacts =
                (string.IsNullOrWhiteSpace(entry.Fact) ? string.Empty : entry.Fact + "。\n") +
                (string.IsNullOrWhiteSpace(captiveDirective) ? string.Empty : captiveDirective + "。\n") +
                IllustratorText.Format("Conversation_45", mainName, partnerName) +
                IllustratorText.Format("Conversation_46", mountPosture, guardFacts) +
                (string.IsNullOrWhiteSpace(siegeFacts) ? string.Empty : siegeFacts + "。\n") +
                (string.IsNullOrWhiteSpace(elevationFacts) ? string.Empty : elevationFacts + "\n") +
                (string.IsNullOrWhiteSpace(siegeForces) ? string.Empty : siegeForces + "\n") +
                IllustratorText.Format("Conversation_47", locName);
            context.SceneDirective =
                IllustratorText.Get("Conversation_48") +
                IllustratorText.Get("Conversation_49") +
                (string.IsNullOrWhiteSpace(siegeForces) ? string.Empty :
                    IllustratorText.Get("Conversation_50")) +
                (string.IsNullOrWhiteSpace(basePose) ? string.Empty :
                    IllustratorText.Format("Conversation_51", basePose));

            return context;
        }

        // Once per generation request: two Agent position reads, no scan.
        // Positions are Agent ground points (a rider's point is its mount's footing).
        internal static string DescribeConversationElevation(TaleWorlds.MountAndBlade.Agent player, TaleWorlds.MountAndBlade.Agent partner, string playerName, string partnerName)
        {
            try
            {
                if (player == null || partner == null) return string.Empty;
                var a = player.Position;
                var b = partner.Position;
                float dz = b.z - a.z;
                float dx = b.x - a.x, dy = b.y - a.y;
                double horizontal = Math.Sqrt(dx * dx + dy * dy);
                if (float.IsNaN(dz) || float.IsInfinity(dz) || double.IsNaN(horizontal) || double.IsInfinity(horizontal)) return string.Empty;
                return FormatElevationFact(dz, horizontal, playerName, partnerName);
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string FormatElevationFact(float partnerMinusPlayer, double horizontal, string playerName, string partnerName)
        {
            float drop = Math.Abs(partnerMinusPlayer);
            string distance = IllustratorText.Format("Conversation_52", horizontal);
            if (drop < 1.5f)
                return IllustratorText.Format("Conversation_53", playerName, partnerName, drop, distance);
            string upper = partnerMinusPlayer > 0 ? partnerName : playerName;
            string lower = partnerMinusPlayer > 0 ? playerName : partnerName;
            // 1.8 m reference body height; stated so the image model gets a readable scale.
            double bodies = drop / 1.8;
            return IllustratorText.Format("Conversation_54", upper, lower, drop, bodies, distance) +
                IllustratorText.Format("Conversation_55", lower, upper);
        }

        // Once per generation request while under siege: one pass over each side's involved parties.
        internal static string DescribeSiegeForces(Settlement settlement, bool playerIsDefender)
        {
            try
            {
                var siege = settlement?.SiegeEvent ?? TaleWorlds.CampaignSystem.Siege.PlayerSiege.PlayerSiegeEvent;
                if (siege == null) return string.Empty;
                int attackerParties = 0, attackers = 0, defenderParties = 0, defenders = 0;
                var camp = siege.BesiegerCamp;
                if (camp != null)
                    foreach (var party in camp.GetInvolvedPartiesForEventType())
                    {
                        if (party == null) continue;
                        attackerParties++;
                        attackers += party.NumberOfHealthyMembers;
                    }
                var besieged = siege.BesiegedSettlement ?? settlement;
                if (besieged != null)
                    foreach (var party in besieged.GetInvolvedPartiesForEventType())
                    {
                        if (party == null) continue;
                        defenderParties++;
                        defenders += party.NumberOfHealthyMembers;
                    }
                if (attackers <= 0 && defenders <= 0) return string.Empty;
                string armyName = camp?.LeaderParty?.Army?.Name?.ToString();
                string attackerFaction = camp?.MapFaction?.Name?.ToString();
                string defenderFaction = besieged?.MapFaction?.Name?.ToString();
                return FormatSiegeForces(attackers, attackerParties, attackerFaction, armyName, defenders, defenderParties, defenderFaction, playerIsDefender);
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string FormatSiegeForces(int attackers, int attackerParties, string attackerFaction, string armyName,
            int defenders, int defenderParties, string defenderFaction, bool playerIsDefender)
        {
            string attackerLabel = (string.IsNullOrWhiteSpace(attackerFaction) ? IllustratorText.Get("Conversation_56") : IllustratorText.Get("Conversation_57") + attackerFaction) +
                (string.IsNullOrWhiteSpace(armyName) ? string.Empty : "（" + armyName + "）");
            string defenderLabel = string.IsNullOrWhiteSpace(defenderFaction) ? IllustratorText.Get("Conversation_58") : IllustratorText.Get("Conversation_59") + defenderFaction;
            return IllustratorText.Format("Conversation_60", attackerLabel, attackers, attackerParties) +
                IllustratorText.Format("Conversation_61", defenderLabel, defenders, defenderParties, (playerIsDefender ? IllustratorText.Get("Conversation_58") : IllustratorText.Get("Conversation_56"))) +
                IllustratorText.Get("Conversation_62") +
                IllustratorText.Get("Conversation_63");
        }

        /// <summary>
        /// 反射读取主模组 AnimusForge.ShoutUtils.GetCurrentSceneDescription() 的场景快照，
        /// 与主模组聊天提示词的场景注入同源；主模组未加载或失败时返回空串。
        /// </summary>
        internal static bool IsExplicitSeaScene(string scene)
            => !string.IsNullOrWhiteSpace(scene) && (scene.Contains(IllustratorText.Raw("Conversation_64")) || scene.Contains(IllustratorText.Raw("Conversation_65")) || scene.Contains(IllustratorText.Raw("Conversation_66")));

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
        /// 调用频率：每次生成插画一次，最多 260 行托管快照，非热路径。
        /// </summary>
        internal static List<NativeDialogueLine> ReadNativeConversationHistory(int maxLines)
        {
            var lines = new List<NativeDialogueLine>();
            try
            {
                var type = HarmonyLib.AccessTools.TypeByName("AnimusForge.ShoutBehavior");
                var method = HarmonyLib.AccessTools.Method(type, "GetCurrentNativeConversationHistoryEntriesForExternal", new[] { typeof(int) });
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

        internal const int RecentDialogueLimit = 2;

        internal static string BuildRecentDialogueHistory(IEnumerable<NativeDialogueLine> entries, int maxRounds)
        {
            var lines = (entries ?? Enumerable.Empty<NativeDialogueLine>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Text) &&
                    (string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Kind, "npc", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (lines.Count == 0 || maxRounds <= 0) return string.Empty;
            // A round starts with the player and includes all subsequent NPC reply segments.
            // Keep an unfinished last round; short sessions may contain only the NPC opening.
            int start = 0;
            int rounds = 0;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (string.Equals(lines[i].Kind, "player", StringComparison.OrdinalIgnoreCase) && ++rounds == maxRounds)
                {
                    start = i;
                    break;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(IllustratorText.Format("Conversation_67", maxRounds, lines.Count - start));
            for (int i = start; i < lines.Count; i++)
            {
                var entry = lines[i];
                string speaker = string.Equals(entry.Kind, "player", StringComparison.OrdinalIgnoreCase)
                    ? IllustratorText.Get("Conversation_68")
                    : (!string.IsNullOrWhiteSpace(entry.Speaker) ? entry.Speaker.Trim() : IllustratorText.Get("Conversation_69"));
                // Preserve the whole selected utterance: a late refusal/action can change the scene.
                sb.AppendLine(speaker + "：" + CleanText(entry.Text));
            }
            return sb.ToString().TrimEnd();
        }

        internal static string DescribeMountState(string name, bool hasAgent, bool mounted)
        {
            return $"{name}：" + (!hasAgent ? IllustratorText.Get("Conversation_70") : mounted ? IllustratorText.Get("Conversation_71") : IllustratorText.Get("Conversation_72"));
        }

        private static string MapActionToPoseDirective(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return string.Empty;
            if (actionName.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0 || actionName.IndexOf("cross", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_73");
            }
            if (actionName.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_74");
            }
            if (actionName.IndexOf("aggressive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_75");
            }
            if (actionName.IndexOf("warrior", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_76");
            }
            if (actionName.IndexOf("demure", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_77");
            }
            if (actionName.IndexOf("weary", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_78");
            }
            return string.Empty;
        }

        private static string MapIdleToPoseDirective(string idleName)
        {
            if (string.IsNullOrWhiteSpace(idleName)) return string.Empty;
            if (idleName.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0 || idleName.IndexOf("cross", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_79");
            }
            if (idleName.IndexOf("hip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_80");
            }
            if (idleName.IndexOf("aggressive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_81");
            }
            if (idleName.IndexOf("warrior", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_82");
            }
            if (idleName.IndexOf("demure", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_83");
            }
            if (idleName.IndexOf("weary", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IllustratorText.Get("Conversation_84");
            }
            return string.Empty;
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return NarrativeFactRouter.CleanText(text).Replace("\r\n", " ").Replace("\n", " ").Trim();
        }
    }
}
