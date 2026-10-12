using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Context
{
    public sealed class EnvironmentVisualProfile
    {
        public string SettlementName { get; set; } = string.Empty;
        public string SettlementType { get; set; } = string.Empty;
        public string SpecificLocation { get; set; } = string.Empty;
        public string IndoorOutdoorDetails { get; set; } = string.Empty;
        public string ArchitectureStyle { get; set; } = string.Empty;
        public string TerrainAndLandscape { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string Weather { get; set; } = string.Empty;
        public string TimeOfDay { get; set; } = string.Empty;
        public bool HasSceneTime { get; set; }
        public string TimeEvidence { get; set; } = string.Empty;
        public string DateLabel { get; set; } = string.Empty;
        public string LightingAndAtmosphere { get; set; } = string.Empty;
        public string ConflictStatus { get; set; } = string.Empty;
        public string SurroundingCharacters { get; set; } = string.Empty;
        internal bool SurroundingCharactersFromLiveScan { get; set; }
        // Legacy descriptive field: no location template is emitted as an observed prop.
        public string SurroundingProps { get; set; } = string.Empty;
        public string RealSceneName { get; set; } = string.Empty;
        public string NamedCharacters { get; set; } = string.Empty;
        public string RealProps { get; set; } = string.Empty;
        public bool HasLiveScene { get; set; }
        public string HostSceneDescription { get; set; } = string.Empty;
        public bool IsIndoor { get; set; }
        public string CultureTag { get; set; } = string.Empty;
        public string TerrainTag { get; set; } = string.Empty;

        public string BuildHardFactsSummary()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(SettlementName)) sb.AppendLine(IllustratorText.Format("Environment_98", SettlementName, SettlementType));
            if (!string.IsNullOrWhiteSpace(SpecificLocation)) sb.AppendLine(IllustratorText.Format("Environment_99", SpecificLocation));
            if (!string.IsNullOrWhiteSpace(NamedCharacters)) sb.AppendLine(IllustratorText.Format("Environment_100", NamedCharacters));
            if (SurroundingCharactersFromLiveScan && !string.IsNullOrWhiteSpace(SurroundingCharacters))
                sb.AppendLine(IllustratorText.Get("Environment_101") + SurroundingCharacters);
            if (!string.IsNullOrWhiteSpace(RealProps)) sb.AppendLine(IllustratorText.Format("Environment_102", RealProps));
            // 精确日期只用于导演理解；发布日期中的季节仍作为回顾事件的季节参考。
            if (!string.IsNullOrWhiteSpace(Season)) sb.AppendLine(IllustratorText.Get("Environment_103") + Season);
            else
                foreach (string seasonKey in new[] { "Environment_104", "Environment_105", "Environment_106", "Environment_107" })
                    if ((DateLabel ?? string.Empty).Contains(IllustratorText.Raw(seasonKey)) ||
                        (DateLabel ?? string.Empty).Contains(IllustratorText.Get(seasonKey)))
                    {
                        sb.AppendLine(IllustratorText.Get("Environment_108") + IllustratorText.Get(seasonKey));
                        break;
                    }
            if (!string.IsNullOrWhiteSpace(TimeOfDay)) sb.AppendLine(IllustratorText.Format("Environment_109", TimeOfDay));
            if (!string.IsNullOrWhiteSpace(TimeEvidence)) sb.AppendLine(IllustratorText.Format("Environment_110", TimeEvidence));
            if (!string.IsNullOrWhiteSpace(Weather)) sb.AppendLine(IllustratorText.Format("Environment_111", Weather));
            if (!string.IsNullOrWhiteSpace(CultureTag)) sb.AppendLine(IllustratorText.Format("Environment_112", CultureTag));
            if (!string.IsNullOrWhiteSpace(TerrainTag)) sb.AppendLine(IllustratorText.Format("Environment_113", TerrainTag));
            if (IsIndoor) sb.AppendLine(IllustratorText.Get("Environment_114"));
            if (!string.IsNullOrWhiteSpace(ConflictStatus)) sb.AppendLine(IllustratorText.Format("Environment_115", ConflictStatus));
            string extraSceneFacts = NarrativeFactRouter.SceneEvidence(HostSceneDescription, sb.ToString());
            if (!string.IsNullOrWhiteSpace(extraSceneFacts)) sb.AppendLine(IllustratorText.Get("Environment_116") + extraSceneFacts);
            return sb.ToString().TrimEnd();
        }

        internal void UseConversationTimeEvidence()
        {
            if (HasSceneTime)
            {
                TimeEvidence = IllustratorText.Get("Environment_117");
                return;
            }
            // No Mission: a map conversation. Its native tableau picks night/noon/sunset from the
            // campaign clock and shows rain/snow from the map weather at the player's position
            // (GauntletMapConversationView.CreateConversationTableau, identical in 1.3 and 1.4),
            // so the same inputs are valid day/night evidence here.
            if (!HasLiveScene && ApplyMapConversationClock()) return;
            // A Mission whose scene clock could not be read: the campaign clock is not its lighting.
            TimeOfDay = string.Empty;
            LightingAndAtmosphere = string.Empty;
            TimeEvidence = IllustratorText.Get("Environment_118");
        }

        // Runs once per conversation-context extraction on the game thread; two property reads.
        private bool ApplyMapConversationClock()
        {
            float hour;
            int weather = -1;
            try
            {
                if (Campaign.Current == null) return false;
                // Same expression the native tableau passes as MapConversationTableauData.TimeOfDay.
                hour = CampaignTime.Now.CurrentHourInDay * (float)(24 / CampaignTime.HoursInDay);
                var party = MobileParty.MainParty;
                if (party != null && Campaign.Current.Models?.MapWeatherModel != null)
                    weather = (int)Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(party.Position.ToVec2());
            }
            catch
            {
                return false;
            }
            if (float.IsNaN(hour) || float.IsInfinity(hour) || hour < 0f || hour > 24f) return false;
            TimeOfDay = ResolveMapClockTimeOfDay(hour);
            // Interior tableaus (tavern, lord's hall) use a fixed indoor atmosphere; keep that lighting.
            if (!IsIndoor) LightingAndAtmosphere = ResolveMapClockLighting(hour);
            TimeEvidence = IsIndoor
                ? IllustratorText.Get("Environment_119")
                : IllustratorText.Get("Environment_120");
            string weatherText = ResolveMapWeather(weather);
            if (!string.IsNullOrWhiteSpace(weatherText)) Weather = weatherText;
            return true;
        }

        /// <summary>Native map-conversation buckets: ≤3 or ≥21 night, 8–16 noon, otherwise sunset light.</summary>
        internal static string ResolveMapClockTimeOfDay(float hour)
        {
            if (hour <= 3f || hour >= 21f) return IllustratorText.Get("Environment_121");
            if (hour > 8f && hour < 16f) return IllustratorText.Get("Environment_122");
            return hour < 12f ? IllustratorText.Get("Environment_123") : IllustratorText.Get("Environment_124");
        }

        // Neutral outdoor light for each bucket; no invented torches, fires or moon.
        private static string ResolveMapClockLighting(float hour)
        {
            if (hour <= 3f || hour >= 21f) return IllustratorText.Get("Environment_125");
            if (hour > 8f && hour < 16f) return IllustratorText.Get("Environment_126");
            return IllustratorText.Get("Environment_127");
        }

        /// <summary>MapWeatherModel.WeatherEvent order: Clear, LightRain, HeavyRain, Snowy, Blizzard (1.3 and 1.4).</summary>
        internal static string ResolveMapWeather(int weatherEvent)
        {
            switch (weatherEvent)
            {
                case 0: return IllustratorText.Get("Environment_128");
                case 1: return IllustratorText.Get("Environment_129");
                case 2: return IllustratorText.Get("Environment_130");
                case 3: return IllustratorText.Get("Environment_131");
                case 4: return IllustratorText.Get("Environment_132");
                default: return string.Empty;
            }
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(DateLabel)) sb.AppendLine(IllustratorText.Get("Environment_133") + DateLabel);
            if (!string.IsNullOrWhiteSpace(RealSceneName)) sb.AppendLine(IllustratorText.Get("Environment_134") + RealSceneName);
            if (!string.IsNullOrWhiteSpace(HostSceneDescription)) sb.AppendLine(IllustratorText.Get("Environment_135") + HostSceneDescription);
            return sb.ToString().TrimEnd();
        }

        public string BuildArtDirectionSummary()
        {
            // 场景/事件入口只开放表现方式，不将未知现场陈设升级为事实。
            return IllustratorText.Get("Environment_136") +
                IllustratorText.Get("Environment_137");
        }

        public string BuildSummary()
        {
            string facts = BuildHardFactsSummary();
            string direction = BuildArtDirectionSummary();
            if (string.IsNullOrWhiteSpace(direction)) return facts;
            if (string.IsNullOrWhiteSpace(facts)) return direction;
            return facts + "\n" + direction;
        }
    }

    public static class EnvironmentVisualExtractor
    {
        // Field default. It must not assume a pre-battle parley: the same field can be a party-screen talk,
        // a post-battle prisoner scene or a friendly meeting. The conversation entry fact says which.
        internal static readonly string NeutralWildDetails = IllustratorText.Get("Environment_138");

        /// <param name="eventAnchored">为 true 时跳过对当前 Mission/菜单位置的实时探测（用于周报等回顾性场景——环境由事件主题决定，而非玩家当前所在位置）。</param>
        public static EnvironmentVisualProfile Extract(Settlement settlement = null, bool eventAnchored = false, string eventDateLabel = null)
        {
            var profile = new EnvironmentVisualProfile();

            // 1. 定居点与建筑文化提取
            if (settlement != null)
            {
                profile.SettlementName = settlement.Name != null ? settlement.Name.ToString() : settlement.StringId;
                profile.SettlementType = settlement.IsTown ? IllustratorText.Get("Environment_139") : (settlement.IsCastle ? IllustratorText.Get("Environment_140") : IllustratorText.Get("Environment_141"));

                profile.ArchitectureStyle = ResolveArchitectureStyle(settlement);
                profile.TerrainAndLandscape = ResolveTerrain(settlement);
                profile.CultureTag = settlement.Culture?.Name?.ToString() ?? string.Empty;
                var settlementTerrain = TryGetSettlementTerrain(settlement);
                if (settlementTerrain.HasValue) profile.TerrainTag = TerrainTagOf(settlementTerrain.Value);
                profile.ConflictStatus = eventAnchored ? string.Empty : ResolveConflictStatus(settlement);
            }
            else if (!eventAnchored)
            {
                profile.SettlementType = IllustratorText.Get("Environment_142");
                profile.TerrainAndLandscape = ResolveOverlandTerrain();
                var overlandTerrain = TryGetOverlandTerrain();
                if (overlandTerrain.HasValue) profile.TerrainTag = TerrainTagOf(overlandTerrain.Value);
            }

            // 2. 纪元日期 + 季节时令（骑砍历法：年 - 季节 - 该季第几日）
            int seasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
            if (eventAnchored)
            {
                profile.DateLabel = (eventDateLabel ?? string.Empty).Trim();
            }
            else
            {
                profile.Season = ResolveSeason(seasonIndex);
                try
                {
                    string seasonName = seasonIndex == 0 ? IllustratorText.Get("Environment_143") : seasonIndex == 1 ? IllustratorText.Get("Environment_144") : seasonIndex == 2 ? IllustratorText.Get("Environment_145") : IllustratorText.Get("Environment_146");
                    profile.DateLabel = IllustratorText.Format("Environment_147", CampaignTime.Now.GetYear, seasonName, CampaignTime.Now.GetDayOfSeason + 1);
                }
                catch
                {
                }
                int hour = (int)CampaignTime.Now.GetHourOfDay;
                profile.TimeOfDay = ResolveTimeOfDay(hour);
                profile.LightingAndAtmosphere = ResolveLighting(hour, seasonIndex);
            }

            if (!eventAnchored)
            {
                // 5. 深度室内/子场景识别 (如酒馆 Tavern、领主大厅 Lordshall、竞技场 Arena、地牢 Prison、围城城墙城门等)
                ResolveSpecificLocation(profile, settlement);

                // 5b. 海上与军团处境：航行状态覆盖一切陆地子场景；身处军团标注联营
                ResolveSeaAndArmyContext(profile);

                // 6. 场景周边人物群像与标志性陈设道具动态提取
                ResolveSurroundings(profile);

                // 7. 引擎实读：真实场景资源名 + 在场具名人物 + 附近真实预制件道具
                ProbeLiveScene(profile);
            }

            return profile;
        }

        /// <summary>
        /// 海上与军团处境：1.4 航海版用原生 IsCurrentlyAtSea；1.3 与兜底走宿主快照"海上"字样。
        /// 身处军团时在冲突状态上补充联营事实。海上判定覆盖一切陆地子场景。
        /// </summary>
        private static void ResolveSeaAndArmyContext(EnvironmentVisualProfile profile)
        {
            try
            {
                bool atSea = false;
#if BANNERLORD_1_4_OR_GREATER
                try
                {
                    atSea = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.IsCurrentlyAtSea == true;
                }
                catch
                {
                }
#endif
                if (atSea)
                {
                    profile.SpecificLocation = IllustratorText.Get("Environment_148");
                    profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_149");
                    if (string.IsNullOrWhiteSpace(profile.SettlementType) || profile.SettlementType.Contains(IllustratorText.Raw("Environment_150")) || profile.SettlementType.Contains("wilderness"))
                    {
                        profile.SettlementType = IllustratorText.Get("Environment_151");
                        profile.TerrainAndLandscape = IllustratorText.Get("Environment_152");
                    }
                }

                Army army = null;
                try
                {
                    army = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.Army;
                }
                catch
                {
                }
                if (army != null)
                {
                    string armyName = army.Name != null ? army.Name.ToString() : IllustratorText.Get("Environment_153");
                    profile.ConflictStatus = (string.IsNullOrWhiteSpace(profile.ConflictStatus) ? string.Empty : profile.ConflictStatus + "；")
                        + IllustratorText.Format("Environment_154", armyName);
                }
            }
            catch
            {
            }
        }

        // Pure routing over the captured location; no additional game queries.
        internal static void ResolveBesiegedLocation(EnvironmentVisualProfile profile, bool outdoorMission, string locId, bool isIndoor)
        {
            locId = (locId ?? string.Empty).ToLowerInvariant();
            if (outdoorMission)
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_155");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_156");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_157");
                profile.ConflictStatus = IllustratorText.Get("Environment_158");
                return;
            }

            if (locId.Contains("lordshall") || locId.Contains("keep"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_159");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_160");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_161");
                profile.ConflictStatus = IllustratorText.Get("Environment_162");
                return;
            }

            if (locId.Contains("prison"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_163");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_164");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_165");
                profile.ConflictStatus = IllustratorText.Get("Environment_166");
                return;
            }

            if (locId.Contains("tavern"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_167");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_168");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_169");
                profile.ConflictStatus = IllustratorText.Get("Environment_170");
                return;
            }

            if (isIndoor)
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_171");
                profile.ConflictStatus = IllustratorText.Get("Environment_172");
                return;
            }

            // Siege state establishes a blockade, not a gate/parapet location or a parley.
            // Explicit host scene evidence is retained separately in HostSceneDescription.
            profile.SpecificLocation = locId.Contains("center")
                ? IllustratorText.Get("Environment_173")
                : IllustratorText.Get("Environment_174");
            profile.IndoorOutdoorDetails = string.Empty;
            profile.LightingAndAtmosphere = string.Empty;
            profile.ConflictStatus = IllustratorText.Get("Environment_175");
            return;
        }

        private static void ResolveSpecificLocation(EnvironmentVisualProfile profile, Settlement settlement)
        {
            // 优先检测围城/攻城对峙前沿 (杜绝围城谈判定性为市集街道)
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
                else if (TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.BesiegedSettlement != null)
                {
                    isUnderSiege = true;
                }
            }
            catch
            {
            }

            // Mission 存在且 Location 为 null 仅说明缺少子场景元数据，
            // 不能据此判成平地、阵前谈判或城镇街道。
            bool outdoorMission = false;
            try
            {
                outdoorMission = TaleWorlds.MountAndBlade.Mission.Current != null
                    && CampaignMission.Current?.Location == null;
            }
            catch
            {
            }

            string locId = string.Empty;
            bool isIndoor = false;

            try
            {
                if (CampaignMission.Current?.Location != null)
                {
                    locId = CampaignMission.Current.Location.StringId?.ToLowerInvariant() ?? "";
                    isIndoor = CampaignMission.Current.Location.IsIndoor;
                }
            }
            catch
            {
            }

            // 若不是在 Mission 中但在菜单中（如在“前往酒馆区”的菜单界面中点击对话）
            if (string.IsNullOrEmpty(locId))
            {
                try
                {
                    if (Campaign.Current?.GameMenuManager?.MenuLocations?.Count > 0)
                    {
                        locId = Campaign.Current.GameMenuManager.MenuLocations[0]?.StringId?.ToLowerInvariant() ?? "";
                    }

                    if (string.IsNullOrEmpty(locId))
                    {
                        string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId?.ToLowerInvariant() ?? "";
                        if (menuId.Contains("tavern")) locId = "tavern";
                        else if (menuId.Contains("lordshall") || menuId.Contains("keep")) locId = "lordshall";
                        else if (menuId.Contains("arena")) locId = "arena";
                        else if (menuId.Contains("prison")) locId = "prison";
                    }
                }
                catch
                {
                }
            }

            profile.IsIndoor = !outdoorMission && (isIndoor || locId.Contains("tavern") || locId.Contains("lordshall") || locId.Contains("keep") || locId.Contains("prison"));

            // 1. 围城情形判定：区分旷野阵前谈判、室内据点议事与城防前沿
            if (isUnderSiege)
            {
                ResolveBesiegedLocation(profile, outdoorMission, locId, isIndoor);
                return;
            }

            // 2. 常规场景判定（含室内专属光照保护）
            if (locId.Contains("tavern"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_176");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_177");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_178");
            }
            else if (locId.Contains("lordshall") || locId.Contains("keep"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_179");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_180");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_181");
            }
            else if (locId.Contains("arena"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_182");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_183");
            }
            else if (locId.Contains("prison"))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_184");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_185");
                profile.LightingAndAtmosphere = IllustratorText.Get("Environment_186");
            }
            else if (settlement == null || outdoorMission)
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_187");
                profile.IndoorOutdoorDetails = NeutralWildDetails;
            }
            else if (locId.Contains("center") || (!isIndoor && settlement.IsTown))
            {
                profile.SpecificLocation = IllustratorText.Get("Environment_188");
                profile.IndoorOutdoorDetails = IllustratorText.Get("Environment_189");
            }
        }

        private static void ResolveSurroundings(EnvironmentVisualProfile profile)
        {
            // Reuse the existing request-time Agent pass. An occupation confirms presence,
            // not a held prop, activity, clothing or location within the room.
            try
            {
                var mission = TaleWorlds.MountAndBlade.Mission.Current;
                var mainAgent = mission?.MainAgent;
                if (mainAgent == null || mission.Agents == null) return;
                Vec3 center = mainAgent.Position;
                if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsNaN(center.z) ||
                    float.IsInfinity(center.x) || float.IsInfinity(center.y) || float.IsInfinity(center.z)) return;
                var conversationAgents = Campaign.Current?.ConversationManager?.ConversationAgents;
                var partner = conversationAgents != null && conversationAgents.Count > 0
                    ? conversationAgents[0] as TaleWorlds.MountAndBlade.Agent : null;
                var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                int total = 0, near = 0, middle = 0, far = 0, differentHeight = 0;
                foreach (var agent in mission.Agents)
                {
                    if (agent == null || !agent.IsActive() || !agent.IsHuman || agent == mainAgent || agent == partner) continue;
                    Vec3 position = agent.Position;
                    float distanceSquared = position.DistanceSquared(center);
                    if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || distanceSquared > 625f) continue;
                    var character = agent.Character as CharacterObject;
                    if (character == null) continue;
                    string role = DescribeNearbyRole(character);
                    counts.TryGetValue(role, out int count);
                    counts[role] = count + 1;
                    total++;
                    // Same request-time pass, no visibility raycasts or extra native scan.
                    if (distanceSquared <= 9f) near++;
                    else if (distanceSquared <= 64f) middle++;
                    else far++;
                    if (Math.Abs(position.z - center.z) > 2f) differentHeight++;
                }
                // Publish only after the whole pass succeeds; missing/failed observation is not zero people.
                profile.SurroundingCharacters = total == 0
                    ? IllustratorText.Get("Environment_190")
                    : IllustratorText.Get("Environment_191") + total + IllustratorText.Get("Environment_192") +
                      string.Join("、", counts.Select(pair => pair.Key + " " + pair.Value + IllustratorText.Get("Environment_193"))) + "。";
                profile.SurroundingCharacters += partner != null
                    ? IllustratorText.Get("Environment_194")
                    : IllustratorText.Get("Environment_195");
                if (total > 0)
                    profile.SurroundingCharacters += IllustratorText.Format("Environment_196", near, middle, far, differentHeight);
                profile.SurroundingCharacters += IllustratorText.Get("Environment_197");
                profile.SurroundingCharactersFromLiveScan = true;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[Illustrator] Surrounding agents extraction: " + ex.GetType().Name);
            }
            // No location-name fallback: an empty tavern does not imply patrons or furniture.
            // Actual props are supplied separately by the bounded panorama traversal and images.
        }

        private static string DescribeNearbyRole(CharacterObject character)
        {
            if (character.IsHero) return IllustratorText.Get("Environment_198");
            switch (character.Occupation)
            {
                case Occupation.Lord: return IllustratorText.Get("Environment_199");
                case Occupation.Guard:
                case Occupation.PrisonGuard:
                case Occupation.Soldier: return IllustratorText.Get("Environment_200");
                case Occupation.Tavernkeeper: return IllustratorText.Get("Environment_201");
                case Occupation.TavernWench: return IllustratorText.Get("Environment_202");
                case Occupation.Musician: return IllustratorText.Get("Environment_203");
                case Occupation.TavernGameHost: return IllustratorText.Get("Environment_204");
                case Occupation.Mercenary: return IllustratorText.Get("Environment_205");
                case Occupation.RansomBroker: return IllustratorText.Get("Environment_206");
                case Occupation.Merchant:
                case Occupation.GoodsTrader:
                case Occupation.Artisan:
                case Occupation.Blacksmith:
                case Occupation.Armorer:
                case Occupation.Weaponsmith: return IllustratorText.Get("Environment_207");
                case Occupation.Villager: return IllustratorText.Get("Environment_208");
                case Occupation.Townsfolk: return IllustratorText.Get("Environment_209");
                default: return IllustratorText.Get("Environment_210");
            }
        }

        /// <summary>定居点位置的地图地貌枚举（事实用短标签）。</summary>
        private static TerrainType? TryGetSettlementTerrain(Settlement settlement)
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && settlement != null &&
                    (settlement.GatePosition.X != 0f || settlement.GatePosition.Y != 0f))
                {
                    PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(settlement.GatePosition);
                    return Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
                }
            }
            catch { }
            return null;
        }

        /// <summary>玩家当前脚下的大地地形貌枚举（野外遭遇事实用）。</summary>
        private static TerrainType? TryGetOverlandTerrain()
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && MobileParty.MainParty != null)
                {
                    return Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
                }
            }
            catch { }
            return null;
        }

        private static string TerrainTagOf(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Snow: return IllustratorText.Get("Environment_211");
                case TerrainType.Desert: return IllustratorText.Get("Environment_212");
                case TerrainType.Steppe: return IllustratorText.Get("Environment_213");
                case TerrainType.Mountain: return IllustratorText.Get("Environment_214");
                case TerrainType.Forest: return IllustratorText.Get("Environment_215");
                case TerrainType.Swamp: return IllustratorText.Get("Environment_216");
                case TerrainType.Water: return IllustratorText.Get("Environment_217");
                case TerrainType.Canyon: return IllustratorText.Get("Environment_218");
                case TerrainType.Bridge: return IllustratorText.Get("Environment_219");
                case TerrainType.Plain:
                default: return IllustratorText.Get("Environment_220");
            }
        }

        private static string ResolveOverlandTerrain()
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && MobileParty.MainParty != null)
                {
                    TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
                    switch (terrain)
                    {
                        case TerrainType.Forest:
                            return IllustratorText.Get("Environment_221");
                        case TerrainType.Mountain:
                            return IllustratorText.Get("Environment_222");
                        case TerrainType.Snow:
                            return IllustratorText.Get("Environment_223");
                        case TerrainType.Desert:
                            return IllustratorText.Get("Environment_224");
                        case TerrainType.Steppe:
                            return IllustratorText.Get("Environment_225");
                        case TerrainType.Swamp:
                            return IllustratorText.Get("Environment_226");
                        case TerrainType.Water:
                            return IllustratorText.Get("Environment_227");
                        case TerrainType.Canyon:
                            return IllustratorText.Get("Environment_228");
                        case TerrainType.Bridge:
                            return IllustratorText.Get("Environment_229");
                        case TerrainType.Plain:
                        default:
                            return IllustratorText.Get("Environment_230");
                    }
                }
            }
            catch { }
            return IllustratorText.Get("Environment_231");
        }

        /// <summary>
        /// 引擎实读当前 Mission 场景：真实场景资源名、天气与玩家附近具名人物。
        /// 仅主线程读取；物体名称延迟到正式采集的分帧遍历，打开缓存不枚举全场实体。
        /// </summary>
        private static void ProbeLiveScene(EnvironmentVisualProfile profile)
        {
            try
            {
                var mission = TaleWorlds.MountAndBlade.Mission.Current;
                if (mission == null) return;
                profile.HasLiveScene = true;

                try
                {
                    string sceneName = mission.SceneName;
                    if (!string.IsNullOrWhiteSpace(sceneName)) profile.RealSceneName = sceneName;
                    var scene = mission.Scene;
                    if (scene != null)
                    {
                        float sceneTime = scene.TimeOfDay;
                        if (!float.IsNaN(sceneTime) && sceneTime >= 0f && sceneTime <= 24f)
                        {
                            int sceneHour = (int)sceneTime;
                            profile.TimeOfDay = ResolveTimeOfDay(sceneHour);
                            profile.LightingAndAtmosphere = ResolveLighting(sceneHour, (int)CampaignTime.Now.GetSeasonOfYear);
                            profile.HasSceneTime = true;
                        }
                        float rain = scene.GetRainDensity();
                        float snow = scene.GetSnowDensity();
                        profile.Weather = snow > 0.05f ? IllustratorText.Format("Environment_232", snow) : rain > 0.05f ? IllustratorText.Format("Environment_233", rain) : IllustratorText.Get("Environment_234");
                    }
                }
                catch { }

                var mainAgent = mission.MainAgent;
                // 没有玩家定位点不能把整个场景误称为“附近”。
                if (mainAgent == null) return;
                Vec3 center = mainAgent.Position;
                bool hasCenter = true;

                // 在场具名人物（真实 Agent，22m 内，最多 10 名）
                var named = new List<string>();
                var namedSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var agents = mission.Agents;
                    if (agents != null)
                    {
                        foreach (var agent in agents)
                        {
                            if (agent == null || !agent.IsActive() || agent == mainAgent) continue;
                            if (hasCenter && agent.Position.Distance(center) > 22f) continue;
                            var co = agent.Character as CharacterObject;
                            if (co?.Name == null) continue;
                            string name = co.Name.ToString();
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            string role = co.IsHero ? IllustratorText.Get("Environment_235") : co.Occupation.ToString();
                            string label = name + "(" + role + ")";
                            if (namedSeen.Add(label)) named.Add(label);
                            if (named.Count >= 10) break;
                        }
                    }
                }
                catch { }
                if (named.Count > 0)
                {
                    profile.NamedCharacters = string.Join("、", named);
                }

                // Nearby prop names are collected by the generation-only panorama batches.
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Live scene probe failed: {ex.Message}");
            }
        }

        private static string ResolveArchitectureStyle(Settlement settlement)
        {
            if (settlement == null) return IllustratorText.Get("Environment_236");

            bool isTown = settlement.IsTown;
            bool isCastle = settlement.IsCastle;
            var culture = settlement.Culture;
            string cultureName = culture?.Name?.ToString() ?? IllustratorText.Get("Environment_237");

            // 1. 【第一优先级】：动态读取 MOD 或游戏底层为该文化编写的官方百科传记 (彻底兼容所有第三方全面替换 MOD)
            try
            {
                if (culture?.EncyclopediaText != null)
                {
                    string lore = culture.EncyclopediaText.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(lore))
                    {
                        lore = System.Text.RegularExpressions.Regex.Replace(lore, "<.*?>", string.Empty).Trim();
                        if (lore.Length > 150) lore = lore.Substring(0, 150) + "...";
                        string role = isTown ? IllustratorText.Get("Environment_238") : (isCastle ? IllustratorText.Get("Environment_239") : IllustratorText.Get("Environment_240"));
                        return IllustratorText.Format("Environment_241", cultureName, role, lore);
                    }
                }
            }
            catch { }

            // 2. 【第二优先级】：基于地理与生态特征（大地图地形 TerrainType + 气候）动态自适应
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && (settlement.GatePosition.X != 0f || settlement.GatePosition.Y != 0f))
                {
                    PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(settlement.GatePosition);
                    TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
                    switch (terrain)
                    {
                        case TerrainType.Snow:
                            return isCastle
                                ? IllustratorText.Format("Environment_242", cultureName)
                                : (isTown ? IllustratorText.Format("Environment_243", cultureName) : IllustratorText.Format("Environment_244", cultureName));
                        case TerrainType.Desert:
                            return isCastle
                                ? IllustratorText.Format("Environment_245", cultureName)
                                : (isTown ? IllustratorText.Format("Environment_246", cultureName) : IllustratorText.Format("Environment_247", cultureName));
                        case TerrainType.Steppe:
                            return isCastle
                                ? IllustratorText.Format("Environment_248", cultureName)
                                : (isTown ? IllustratorText.Format("Environment_249", cultureName) : IllustratorText.Format("Environment_250", cultureName));
                        case TerrainType.Forest:
                            return isCastle
                                ? IllustratorText.Format("Environment_251", cultureName)
                                : (isTown ? IllustratorText.Format("Environment_252", cultureName) : IllustratorText.Format("Environment_253", cultureName));
                        case TerrainType.Mountain:
                            return isCastle
                                ? IllustratorText.Format("Environment_254", cultureName)
                                : (isTown ? IllustratorText.Format("Environment_255", cultureName) : IllustratorText.Format("Environment_256", cultureName));
                    }
                }
            }
            catch { }

            // 3. 【第三优先级】：通用历史/奇幻风貌保底 (绝不写死任何原版势力名称)
            if (!isTown && !isCastle)
                return IllustratorText.Format("Environment_257", cultureName);
            return isCastle
                ? IllustratorText.Format("Environment_258", cultureName)
                : IllustratorText.Format("Environment_259", cultureName);
        }

        private static string ResolveTerrain(Settlement settlement)
        {
            if (settlement == null) return IllustratorText.Get("Environment_260");
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && (settlement.GatePosition.X != 0f || settlement.GatePosition.Y != 0f))
                {
                    PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(settlement.GatePosition);
                    TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
                    switch (terrain)
                    {
                        case TerrainType.Snow:
                            return IllustratorText.Get("Environment_261");
                        case TerrainType.Desert:
                            return IllustratorText.Get("Environment_262");
                        case TerrainType.Steppe:
                            return IllustratorText.Get("Environment_263");
                        case TerrainType.Mountain:
                            return IllustratorText.Get("Environment_264");
                        case TerrainType.Forest:
                            return IllustratorText.Get("Environment_265");
                        case TerrainType.Swamp:
                            return IllustratorText.Get("Environment_266");
                        case TerrainType.Water:
                            return IllustratorText.Get("Environment_267");
                    }
                }
            }
            catch { }

            return IllustratorText.Get("Environment_268");
        }

        private static string ResolveConflictStatus(Settlement settlement)
        {
            if (settlement.IsUnderSiege)
            {
                return IllustratorText.Get("Environment_269");
            }
            if (settlement.IsRaided)
            {
                return IllustratorText.Get("Environment_270");
            }
            if (settlement.Town != null && settlement.Town.Prosperity > 5000)
            {
                return IllustratorText.Get("Environment_271");
            }
            return IllustratorText.Get("Environment_272");
        }

        private static string ResolveSeason(int seasonIndex)
        {
            switch (seasonIndex)
            {
                case 0:
                    return IllustratorText.Get("Environment_273");
                case 1:
                    return IllustratorText.Get("Environment_274");
                case 2:
                    return IllustratorText.Get("Environment_275");
                case 3:
                    return IllustratorText.Get("Environment_276");
                default:
                    return IllustratorText.Get("Environment_277");
            }
        }

        private static string ResolveTimeOfDay(int hour)
        {
            if (hour >= 5 && hour < 8) return IllustratorText.Get("Environment_278");
            // Scene clock confirms time, not exposure to sunlight or clear weather.
            if (hour >= 8 && hour < 16) return IllustratorText.Get("Environment_279");
            if (hour >= 16 && hour < 19) return IllustratorText.Get("Environment_280");
            return IllustratorText.Get("Environment_281");
        }

        private static string ResolveLighting(int hour, int seasonIndex)
        {
            if (hour >= 5 && hour < 8)
            {
                return IllustratorText.Get("Environment_282");
            }
            if (hour >= 8 && hour < 16)
            {
                return IllustratorText.Get("Environment_283");
            }
            if (hour >= 16 && hour < 19)
            {
                return IllustratorText.Get("Environment_284");
            }
            return IllustratorText.Get("Environment_285");
        }
    }
}
