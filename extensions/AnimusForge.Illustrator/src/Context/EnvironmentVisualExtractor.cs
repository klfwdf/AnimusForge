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
            if (!string.IsNullOrWhiteSpace(SettlementName)) sb.AppendLine($"【定居点】{SettlementName} ({SettlementType})");
            if (!string.IsNullOrWhiteSpace(SpecificLocation)) sb.AppendLine($"【当前子场景】{SpecificLocation}");
            if (!string.IsNullOrWhiteSpace(NamedCharacters)) sb.AppendLine($"【附近实际角色】{NamedCharacters}");
            if (SurroundingCharactersFromLiveScan && !string.IsNullOrWhiteSpace(SurroundingCharacters))
                sb.AppendLine("【附近人群活动依据】" + SurroundingCharacters);
            if (!string.IsNullOrWhiteSpace(RealProps)) sb.AppendLine($"【附近实际预制件】{RealProps}");
            // 精确日期只用于导演理解；发布日期中的季节仍作为回顾事件的季节参考。
            if (!string.IsNullOrWhiteSpace(Season)) sb.AppendLine("【当前季节】" + Season);
            else
                foreach (string season in new[] { "春季", "夏季", "秋季", "冬季" })
                    if ((DateLabel ?? string.Empty).Contains(season)) { sb.AppendLine("【季节参考】" + season); break; }
            if (!string.IsNullOrWhiteSpace(TimeOfDay)) sb.AppendLine($"【现场时段】{TimeOfDay}");
            if (!string.IsNullOrWhiteSpace(TimeEvidence)) sb.AppendLine($"【时段来源】{TimeEvidence}");
            if (!string.IsNullOrWhiteSpace(Weather)) sb.AppendLine($"【现场天气】{Weather}");
            if (!string.IsNullOrWhiteSpace(CultureTag)) sb.AppendLine($"【文化归属】{CultureTag}");
            if (!string.IsNullOrWhiteSpace(TerrainTag)) sb.AppendLine($"【地貌类型】{TerrainTag}");
            if (IsIndoor) sb.AppendLine("【现场空间】室内");
            if (!string.IsNullOrWhiteSpace(ConflictStatus)) sb.AppendLine($"【冲突状态】{ConflictStatus}");
            string extraSceneFacts = NarrativeFactRouter.SceneEvidence(HostSceneDescription, sb.ToString());
            if (!string.IsNullOrWhiteSpace(extraSceneFacts)) sb.AppendLine("【场景补充事实】" + extraSceneFacts);
            return sb.ToString().TrimEnd();
        }

        internal void UseConversationTimeEvidence()
        {
            if (HasSceneTime)
            {
                TimeEvidence = "当前 Mission 场景时间";
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
            TimeEvidence = "未读取到会话渲染场景时间；战役时钟不作为现场昼夜证据。时段仅采用已确认的文字事实，否则时段未确认。";
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
                ? "战役时钟（大地图对话当前时刻）；室内采光按已确认的室内光源，不据时段添加窗外日光"
                : "战役时钟（大地图对话当前时刻）；原生野外对话布景按此时刻选择夜晚、正午或晨昏光照";
            string weatherText = ResolveMapWeather(weather);
            if (!string.IsNullOrWhiteSpace(weatherText)) Weather = weatherText;
            return true;
        }

        /// <summary>Native map-conversation buckets: ≤3 or ≥21 night, 8–16 noon, otherwise sunset light.</summary>
        internal static string ResolveMapClockTimeOfDay(float hour)
        {
            if (hour <= 3f || hour >= 21f) return "夜晚 (Night)";
            if (hour > 8f && hour < 16f) return "白昼 (Daytime)";
            return hour < 12f ? "清晨，低角度晨光 (Dawn)" : "黄昏，低角度斜阳 (Dusk)";
        }

        // Neutral outdoor light for each bucket; no invented torches, fires or moon.
        private static string ResolveMapClockLighting(float hour)
        {
            if (hour <= 3f || hour >= 21f) return "夜间低照度的自然环境光，暗部保留地形与人物轮廓";
            if (hour > 8f && hour < 16f) return "白昼自然天光，人物与地面有清晰投影";
            return "低角度的晨昏斜光，暖色受光面与拉长的投影";
        }

        /// <summary>MapWeatherModel.WeatherEvent order: Clear, LightRain, HeavyRain, Snowy, Blizzard (1.3 and 1.4).</summary>
        internal static string ResolveMapWeather(int weatherEvent)
        {
            switch (weatherEvent)
            {
                case 0: return "大地图天气：无降水（云量未知）";
                case 1: return "大地图天气：小雨";
                case 2: return "大地图天气：大雨";
                case 3: return "大地图天气：降雪，地面积雪";
                case 4: return "大地图天气：暴风雪，地面积雪";
                default: return string.Empty;
            }
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(DateLabel)) sb.AppendLine("【纪元时间】" + DateLabel);
            if (!string.IsNullOrWhiteSpace(RealSceneName)) sb.AppendLine("【引擎场景资源名】" + RealSceneName);
            if (!string.IsNullOrWhiteSpace(HostSceneDescription)) sb.AppendLine("【宿主场景原文】" + HostSceneDescription);
            return sb.ToString().TrimEnd();
        }

        public string BuildArtDirectionSummary()
        {
            // 场景/事件入口只开放表现方式，不将未知现场陈设升级为事实。
            return "【现场艺术指导】：依据事实与标明用途的真实参考图，自主决定叙事瞬间、景别、机位和画法；" +
                "保留已确认的建筑布局、陈设、人物位置及现场采光，未知环境保持克制，不按身份或地点名称补造具体建筑、道具、光源或事件。";
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
        /// <param name="eventAnchored">为 true 时跳过对当前 Mission/菜单位置的实时探测（用于周报等回顾性场景——环境由事件主题决定，而非玩家当前所在位置）。</param>
        public static EnvironmentVisualProfile Extract(Settlement settlement = null, bool eventAnchored = false, string eventDateLabel = null)
        {
            var profile = new EnvironmentVisualProfile();

            // 1. 定居点与建筑文化提取
            if (settlement != null)
            {
                profile.SettlementName = settlement.Name != null ? settlement.Name.ToString() : settlement.StringId;
                profile.SettlementType = settlement.IsTown ? "雄伟巨城" : (settlement.IsCastle ? "险要要塞城堡" : "乡野村落");

                profile.ArchitectureStyle = ResolveArchitectureStyle(settlement);
                profile.TerrainAndLandscape = ResolveTerrain(settlement);
                profile.CultureTag = settlement.Culture?.Name?.ToString() ?? string.Empty;
                var settlementTerrain = TryGetSettlementTerrain(settlement);
                if (settlementTerrain.HasValue) profile.TerrainTag = TerrainTagOf(settlementTerrain.Value);
                profile.ConflictStatus = eventAnchored ? string.Empty : ResolveConflictStatus(settlement);
            }
            else if (!eventAnchored)
            {
                profile.SettlementType = "开阔自然荒野/野外遭遇现场";
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
                    string seasonName = seasonIndex == 0 ? "春" : seasonIndex == 1 ? "夏" : seasonIndex == 2 ? "秋" : "冬";
                    profile.DateLabel = $"卡拉迪亚历 {CampaignTime.Now.GetYear} 年 · {seasonName}季 · 第 {CampaignTime.Now.GetDayOfSeason + 1} 日";
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
                    profile.SpecificLocation = "海船甲板 (Naval Deck)";
                    profile.IndoorOutdoorDetails = "波涛起伏的辽阔海面：木质战船甲板、桅杆索具与鼓满风的帆布，远处隐现海岸线与海鸟；双方立于甲板或两船接舷处会面，脚下随浪轻微起伏。";
                    if (string.IsNullOrWhiteSpace(profile.SettlementType) || profile.SettlementType.Contains("荒野"))
                    {
                        profile.SettlementType = "开阔海面/航线上";
                        profile.TerrainAndLandscape = "无垠海面、波涛与远处朦胧海岸线";
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
                    string armyName = army.Name != null ? army.Name.ToString() : "联合军团";
                    profile.ConflictStatus = (string.IsNullOrWhiteSpace(profile.ConflictStatus) ? string.Empty : profile.ConflictStatus + "；")
                        + $"玩家正身处 {armyName} 军团联营之中（诸部汇集；是否可见营地道具须依据现场）";
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
                profile.SpecificLocation = "围城相关场景（未读取到子场景位置，具体地形未确认；双方高低以【双方实测高差】为准）";
                profile.IndoorOutdoorDetails = "被围城池之外的开阔旷野谈判场：远景是被围城堡的巍峨剪影与森严城堞轮廓，中景按已确认人物呈现交涉，不凭会面类型补造仪仗或随从，更远处围城军营连绵的牛皮帐篷、拒马鹿角与星星点点的营火铺展到地平线。";
                profile.LightingAndAtmosphere = "暗沉肃杀的天光/夜色，双方仪仗火把与远处围城营地的连绵篝火在黑暗中明灭闪烁 (Parley Torches & Distant Siege Campfires)";
                profile.ConflictStatus = "相关定居点正被围困；当前人物活动、骑乘及空间关系以现场记录为准。";
                return;
            }

            if (locId.Contains("lordshall") || locId.Contains("keep"))
            {
                profile.SpecificLocation = "围城封锁下的要塞内堡议事正厅 (Besieged Keep Council Hall)";
                profile.IndoorOutdoorDetails = "大军严密围困下的要塞内堡正厅：厚重石墙严密封闭，暖殿石砌壁炉内柴火沉稳燃烧，长案上摊开防御部署图卷，室外隐隐透入城防喧嚣与战鼓声，气氛凝重紧绷。";
                profile.LightingAndAtmosphere = "室内封闭的暖色壁炉柴火与青铜烛台微光，高处狭小石窗透入一线冷冽天光，浓郁的伦勃朗式明暗光影 (Tense Chiaroscuro & Indoor Firelight)";
                profile.ConflictStatus = "【大军围城 · 内堡正厅】城外处于围城状态，当前位于内堡正厅；具体活动未确认。";
                return;
            }

            if (locId.Contains("prison"))
            {
                profile.SpecificLocation = "围城中的牢房 (Besieged Prison)";
                profile.IndoorOutdoorDetails = "大军围城下阴冷潮湿的地下石牢：沉重精铁栅栏，渗水石壁与单支插在铁箍里的摇曳火把，幽闭压抑。";
                profile.LightingAndAtmosphere = "地下昏暗阴冷的单支火把跳跃照明，深邃厚重的阴影包裹 (Dim Dungeon Torchlight & Deep Shadows)";
                profile.ConflictStatus = "所在定居点正被围困；当前位于牢房，人物是否被关押及其活动以实际记录为准。";
                return;
            }

            if (locId.Contains("tavern"))
            {
                profile.SpecificLocation = "围城中的城镇酒馆 (Besieged Town Tavern)";
                profile.IndoorOutdoorDetails = "大军围城下门窗加固紧闭的城镇酒馆：粗木长桌、跳动的壁炉火光，平民与守兵聚集于此暂避战火。";
                profile.LightingAndAtmosphere = "室内紧闭门窗后的昏黄壁炉火光与微弱烛光 (Dim Refuge Firelight)";
                profile.ConflictStatus = "所在定居点正被围困；当前位于酒馆，人物活动及该场所用途以现场记录为准。";
                return;
            }

            if (isIndoor)
            {
                profile.SpecificLocation = "围城中的室内空间（具体用途未确认）";
                profile.ConflictStatus = "所在定居点正被围困；当前室内活动未确认";
                return;
            }

            // Siege state establishes a blockade, not a gate/parapet location or a parley.
            // Explicit host scene evidence is retained separately in HostSceneDescription.
            profile.SpecificLocation = locId.Contains("center")
                ? "围城中的城镇街道 (Besieged Town Streets)"
                : "围城中的室外空间（具体地点未确认）";
            profile.IndoorOutdoorDetails = string.Empty;
            profile.LightingAndAtmosphere = string.Empty;
            profile.ConflictStatus = "所在定居点正被围困；当前人物活动依据现场记录和实景参考，双方高低以【双方实测高差】为准，围城状态本身不能证明正在谈判。";
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
                profile.SpecificLocation = "城镇酒馆旅店内部 (Tavern / Inn)";
                profile.IndoorOutdoorDetails = "充满中世纪市井烟火气的城镇酒馆内部：粗粝厚重的原木长桌、冒着白色麦沫的陶制大麦酒杯、粗大铁链吊起的铁艺烛台吊灯、跃动着温暖橘红柴火的巨型石砌壁炉，四周隐约可见歇脚的持械雇佣兵、下棋赌骰的酒客与抱着鲁特琴的游吟诗人，浓郁的麦芽酒与熏烤柴火氛围。";
                profile.LightingAndAtmosphere = "室内昏黄温暖的壁炉柴火与烛台微光，在斑驳木梁与粗石墙面上交织出浓郁的明暗对照光影 (Warm Tavern Firelight & Chiaroscuro)";
            }
            else if (locId.Contains("lordshall") || locId.Contains("keep"))
            {
                profile.SpecificLocation = "领主城堡正厅主殿 (Lord's Keep / Throne Hall)";
                profile.IndoorOutdoorDetails = "庄严深邃的领主城堡主殿：雕花原木挑梁穹顶，厚重古朴的古老石砌墙面，墙上垂挂着厚重丝绒刺绣挂毯与古朴青铜烛台，地面铺设整洁平整的磨光石砖与整张巨兽皮地毯，巨型石雕暖殿壁炉内跃动着沉稳火光；大殿主厅空间开阔庄重。";
                profile.LightingAndAtmosphere = "庄严深邃的室内光影：高侧窗倾泻的自然柔和天光与室内石雕壁炉柴火、青铜烛台微光冷暖交融，暗部通透富有层次 (Atmospheric Chiaroscuro, Warm Firelight & Soft Window Light)";
            }
            else if (locId.Contains("arena"))
            {
                profile.SpecificLocation = "城镇竞技角斗场 (Town Arena)";
                profile.IndoorOutdoorDetails = "沙石与黄土飞扬的环形角斗场：四周是层叠木石看台，充满竞技角逐的狂热与尘土气息。";
            }
            else if (locId.Contains("prison"))
            {
                profile.SpecificLocation = "城堡地下石牢 (Castle Dungeon / Prison)";
                profile.IndoorOutdoorDetails = "潮湿阴冷的地下石牢：沉重锈蚀的精铁栅栏，石壁上渗着水渍与青苔，仅有一支插在铁箍里的摇曳火把投射出昏暗跳动的火光。";
                profile.LightingAndAtmosphere = "昏暗幽闭的地下光影：单一铁箍火把投射出昏暗跳动的橘红火焰，大面积沉入深邃阴影之中，极富质感与压迫力 (Dim Dungeon Torchlight & Deep Chiaroscuro Shadows)";
            }
            else if (settlement == null || outdoorMission)
            {
                profile.SpecificLocation = "会话场景（具体子场景位置未确认，地形与人物空间关系以现场记录为准）";
                profile.IndoorOutdoorDetails = "开阔苍茫的旷野临阵会面之地：起伏的草地丘陵与远处隐现的群山地平线，人物身后只保留现场确认的景物，不默认添加军旗与仪仗，空气中弥漫着战前谈判的紧绷肃杀气息。";
            }
            else if (locId.Contains("center") || (!isIndoor && settlement.IsTown))
            {
                profile.SpecificLocation = "城镇市集街道 (Marketplace / Town Streets)";
                profile.IndoorOutdoorDetails = "熙熙攘攘的中世纪城镇市集街道：两旁是石木结构的民居店铺与遮阳帆布货摊，平民与巡逻卫兵穿行其间。";
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
                    ? "玩家25米内未检测到其他活动角色。"
                    : "玩家25米内检测到其他活动角色共" + total + "名：" +
                      string.Join("、", counts.Select(pair => pair.Key + " " + pair.Value + "名")) + "。";
                profile.SurroundingCharacters += partner != null
                    ? "已排除玩家与当前对话对象；与附近实际角色名单可能重合，不叠加人数。"
                    : "已排除玩家；未取得对话对象Agent，统计可能包含对话对象，入画前按身份去重。";
                if (total > 0)
                    profile.SurroundingCharacters += $"距玩家的三维距离分布：3米内{near}名、超过3米至8米{middle}名、超过8米至25米{far}名；其中与玩家脚底高度差超过2米的有{differentHeight}名（是上述人数的子集，不能据此确定楼层）。距离不证明可见、同桌或动作；用当前画面核对遮挡，不把已可见近邻一律改成远处人影。";
                profile.SurroundingCharacters += "这是邻近人数与身份证据，不是全部可见人物清单；墙体、楼层和遮挡以现场参考为准，职业不证明具体动作、服装或手持物。";
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
            if (character.IsHero) return "英雄";
            switch (character.Occupation)
            {
                case Occupation.Lord: return "领主";
                case Occupation.Guard:
                case Occupation.PrisonGuard:
                case Occupation.Soldier: return "守卫/士兵";
                case Occupation.Tavernkeeper: return "酒馆店主";
                case Occupation.TavernWench: return "酒馆侍女";
                case Occupation.Musician: return "乐师";
                case Occupation.TavernGameHost: return "赌徒/游戏主持人";
                case Occupation.Mercenary: return "雇佣兵";
                case Occupation.RansomBroker: return "赎金经纪人";
                case Occupation.Merchant:
                case Occupation.GoodsTrader:
                case Occupation.Artisan:
                case Occupation.Blacksmith:
                case Occupation.Armorer:
                case Occupation.Weaponsmith: return "商贩/工匠";
                case Occupation.Villager: return "村民";
                case Occupation.Townsfolk: return "镇民";
                default: return "其他在场人物";
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
                case TerrainType.Snow: return "雪原冻土";
                case TerrainType.Desert: return "荒漠";
                case TerrainType.Steppe: return "草原";
                case TerrainType.Mountain: return "山地";
                case TerrainType.Forest: return "森林";
                case TerrainType.Swamp: return "沼泽湿地";
                case TerrainType.Water: return "水岸浅滩";
                case TerrainType.Canyon: return "峡谷";
                case TerrainType.Bridge: return "渡口桥梁";
                case TerrainType.Plain:
                default: return "平原旷野";
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
                            return "生机繁茂的中世纪古老自然森林：脚下是覆满湿润落叶与青苔的林间泥径，身侧是苍劲挺拔的古橡树、山毛榉与密集灌木丛，风化青石散布其间，晨曦或树隙微光穿透繁茂枝叶投下斑驳光束，林木层叠延伸至深远背景 (Ancient Dense Forest, Mossy Mud Path, Gnarled Oak Trees & Sunbeams)";
                        case TerrainType.Mountain:
                            return "险峻雄奇的卡拉迪亚崇山绝壁：脚下是风化剥落的碎石高山斜坡与苍凉岩隙，身侧是犬牙差互的巨大陡峭岩壁与冷杉，极目远眺连绵巍峨的险峰雪线隐入苍茫云海，冷冽天光倾泻而下 (Dramatic Mountain Ridge, Weathered Boulders, Scree Slopes & Distant Peaks)";
                        case TerrainType.Snow:
                            return "覆雪严寒的北国莽莽雪原：脚下是踩踏出坚实冰晶与泥雪车辙的雪径，身侧是挂满白霜树挂的厚重冷杉松林，开阔雪野在寒光下绵延起伏，远方天际线泛着幽蓝与冰白的光辉 (Vast Snowy Wilderness, Frosty Evergreens, Snow Tracks & Frozen Vistas)";
                        case TerrainType.Desert:
                            return "炽热浩瀚的金色沙海荒漠：脚下是风纹细腻的起伏金红沙丘与干燥砾石，身侧偶见耐旱棘刺灌木与风蚀岩丘，热浪微微扭曲着遥远的地平线，漫天霞光将沙脊勾勒出锐利金边 (Vast Golden Desert Dunes, Wind Ripples, Arid Rocks & Mirage Horizons)";
                        case TerrainType.Steppe:
                            return "苍茫无垠的辽阔干旱草原：脚下是野草漫漫的起伏土丘与深邃车马辙印，身侧是微风吹拂如波浪起伏的枯黄草海，辽阔无遮的天穹下延伸至极目尽头的地平线 (Boundless Steppe Grasslands, Windblown Plains, Earth Tracks & Endless Sky)";
                        case TerrainType.Swamp:
                            return "多雾深邃的潮湿泥泞沼泽：脚下是泥泞水洼交错的湿地草甸与漂浮水草，身侧是丛生的高大芦苇荡与虬曲枯木，薄雾弥漫在水泽之间，水面反射着冷寂微光 (Misty Marshes, Muddy Shallows, Tall Reeds & Subdued Reflection)";
                        case TerrainType.Water:
                            return "碧波轻漾的湖岸河畔浅滩：脚下是湿润细软的沙石河滩与圆润鹅卵石，身侧是清澈流淌的湍急水流与低矮灌木，极目远眺开阔水面波光粼粼，对岸浅丘与水天相接 (River Shallows, Pebble Shoreline, Sparkling Waters & Distant Banks)";
                        case TerrainType.Canyon:
                            return "险要深邃的红岩峡谷裂谷：脚下是巨石与碎石堆积的蜿蜒谷底窄径，两侧是刀削斧劈般高耸直插天际的赭红岩石断崖，峡口漏下戏剧性侧向天光 (Dramatic Rocky Canyon, Sheer Red Sandstone Cliffs & Narrow Trail)";
                        case TerrainType.Bridge:
                            return "奔腾河流渡口与古朴石木渡桥：脚下是带有马蹄印与车辙的风化石砌引桥地面，身侧是粗粝石砌桥拱与奔腾白浪，两岸延伸向葱郁原野与远方烽燧哨所 (Rushing River, Stone Arch Bridge, Riverbanks & Countryside)";
                        case TerrainType.Plain:
                        default:
                            return "广袤丰饶的卡拉迪亚开阔原野：脚下是绿意盎然、野花点缀的起伏草甸与泥土步道，身侧是天然散布的风化巨石与葱郁树丛，极目远眺连绵丘陵山脊在晨昏光影下层层展开 (Lush Rolling Grasslands, Weathered Rocks, Scenic Wildflowers & Distant Hills)";
                    }
                }
            }
            catch { }
            return "开阔自然的卡拉迪亚原野草甸与远处起伏丘陵 (Open Wilderness & Rolling Hills)";
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
                        profile.Weather = snow > 0.05f ? $"现场降雪，密度约 {snow:0.00}" : rain > 0.05f ? $"现场降雨，密度约 {rain:0.00}" : "现场未检测到明显降雨或降雪";
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
                            string role = co.IsHero ? "英雄" : co.Occupation.ToString();
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
            if (settlement == null) return "中世纪要塞或历史建筑风貌";

            bool isTown = settlement.IsTown;
            bool isCastle = settlement.IsCastle;
            var culture = settlement.Culture;
            string cultureName = culture?.Name?.ToString() ?? "当地";

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
                        string role = isTown ? "都会名城" : (isCastle ? "险要要塞城堡" : "乡村聚落");
                        return $"{cultureName}文化的{role}：融合该文化官方设定风貌（{lore}），建筑造型与风土人情严格遵循其官方设定";
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
                                ? $"{cultureName}北境严寒要塞：厚重黑石垒砌的防风雪高墙，覆满积雪与冰棱的角楼巡道，依山傍雪险阻天成"
                                : (isTown ? $"{cultureName}北境都会：厚重圆木长屋与石木建筑错落，烟气袅袅，白雪皑皑的街道与坚固防雪石墙" : $"{cultureName}北境雪村：厚重原木长屋、烟熏木制干燥棚与积雪林间小道");
                        case TerrainType.Desert:
                            return isCastle
                                ? $"{cultureName}沙漠边陲石塞：高耸的干燥泥石哨塔与箭楼，抵御风沙侵蚀的厚重夯土与石砌城堞"
                                : (isTown ? $"{cultureName}沙漠绿洲都会：平顶晒台泥砖民居、马蹄形拱券门廊、遮阳帐幔与香料集市" : $"{cultureName}绿洲泥砖村落：平顶晒台、椰枣树与黄沙边缘的蓄水井台");
                        case TerrainType.Steppe:
                            return isCastle
                                ? $"{cultureName}草原要塞：依附山岭缓坡的坚固石木营垒与高耸望楼"
                                : (isTown ? $"{cultureName}草原商贸名城：穹庐毡帐与土木民居交错，马厩与开阔集市广场并存" : $"{cultureName}游牧集居地：星罗棋布的圆形毛毡毡帐、马厩与旷野牧场");
                        case TerrainType.Forest:
                            return isCastle
                                ? $"{cultureName}林间古堡要塞：青苔覆盖的粗粝巨石堡垒，高耸木石箭楼隐于郁郁葱葱的参天古木之间"
                                : (isTown ? $"{cultureName}林海都会：石木混合建筑、木梁斜顶工坊与林间商贸石板大道" : $"{cultureName}林间聚落：粗糙原木茅舍、苔藓石围墙与古木环抱的自然小道");
                        case TerrainType.Mountain:
                            return isCastle
                                ? $"{cultureName}崇山险隘要塞：依附垂直绝壁而建的重石碉楼与高耸箭塔，地势险峻拔俗"
                                : (isTown ? $"{cultureName}山城都会：依山势层叠递升的厚重石街石阶、石砌府邸与俯瞰深谷的城墙" : $"{cultureName}山野聚落：石块垒砌的坚实山舍、碎石梯阶与山涧木桥");
                    }
                }
            }
            catch { }

            // 3. 【第三优先级】：通用历史/奇幻风貌保底 (绝不写死任何原版势力名称)
            if (!isTown && !isCastle)
                return $"{cultureName}风格的乡野村落：古朴民居、木栅农庄与自然田园沃野";
            return isCastle
                ? $"{cultureName}风格的险要要塞城堡：巍峨坚固的方石城墙、高耸箭楼与城堞巡道"
                : $"{cultureName}风格的中世纪都会名城：错落有致的斜顶府邸商行、开阔石板广场与繁华集市街巷";
        }

        private static string ResolveTerrain(Settlement settlement)
        {
            if (settlement == null) return "开阔自然的平原农田与起伏山峦";
            try
            {
                if (Campaign.Current?.MapSceneWrapper != null && (settlement.GatePosition.X != 0f || settlement.GatePosition.Y != 0f))
                {
                    PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(settlement.GatePosition);
                    TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
                    switch (terrain)
                    {
                        case TerrainType.Snow:
                            return "被积雪终年覆盖的北境冻土雪原、苍茫深邃的针叶黑松林与冷冽结冰河流 (Frozen Snowy Tundra & Frosty Pines)";
                        case TerrainType.Desert:
                            return "漫无边际的起伏金色荒漠与炽热沙丘，点缀着耐旱棘刺灌木与干燥碎石 (Vast Arid Desert & Golden Dunes)";
                        case TerrainType.Steppe:
                            return "苍茫辽阔的干燥草甸草原、起伏平缓的黄土原野与遥远开阔的地平线 (Endless Steppe Grasslands)";
                        case TerrainType.Mountain:
                            return "崇山峻岭环抱的险峻山谷、嶙峋裸露的巨大岩壁与冷冽高山溪流 (Dramatic Mountain Valley & Rugged Cliffs)";
                        case TerrainType.Forest:
                            return "多雾潮湿的青翠林海、苍劲挺拔的参天古木与繁茂野生灌木林地 (Lush Misty Woodlands & Ancient Forest)";
                        case TerrainType.Swamp:
                            return "水网交错的潮湿湿地泽国、丛生的高大芦苇荡与浮萍水草 (Misty Wetland & Marshes)";
                        case TerrainType.Water:
                            return "碧波荡漾的水泽湖泊或开阔海湾滩涂、湿润沙石河滩 (Scenic Lake Shore & Coastal Shallows)";
                    }
                }
            }
            catch { }

            return "广袤丰饶的起伏平原草野、开阔田园与远处隐现的起伏山峦地平线 (Scenic Rolling Countryside & Distant Hills)";
        }

        private static string ResolveConflictStatus(Settlement settlement)
        {
            if (settlement.IsUnderSiege)
            {
                return "【极度危急】城池正被大军围攻，城头火把通明，投石机呼啸巨石砸出碎石断壁，城外密布拒马与攻城冲车";
            }
            if (settlement.IsRaided)
            {
                return "【战火余烬】定居点刚遭洗劫焚掠，木料仍在冒烟燃烧，残垣断壁，漫天飘洒黑色草木灰烬";
            }
            if (settlement.Town != null && settlement.Town.Prosperity > 5000)
            {
                return "【盛世安澜】城内繁盛太平，街道商贩车水马龙，呈现出中世纪都会的蓬勃生机";
            }
            return "平和肃穆的日常城防守备状态";
        }

        private static string ResolveSeason(int seasonIndex)
        {
            switch (seasonIndex)
            {
                case 0:
                    return "春季";
                case 1:
                    return "夏季";
                case 2:
                    return "秋季";
                case 3:
                    return "冬季";
                default:
                    return "平季";
            }
        }

        private static string ResolveTimeOfDay(int hour)
        {
            if (hour >= 5 && hour < 8) return "破晓清晨 (Dawn)";
            // Scene clock confirms time, not exposure to sunlight or clear weather.
            if (hour >= 8 && hour < 16) return "白昼 (Daytime)";
            if (hour >= 16 && hour < 19) return "暮色黄昏 (Sunset / Golden Hour)";
            return "沉寂夜幕 (Midnight)";
        }

        private static string ResolveLighting(int hour, int seasonIndex)
        {
            if (hour >= 5 && hour < 8)
            {
                return "冷色调的黎明晨曦透过轻柔薄雾，为建筑物边缘勾勒出微弱的粉紫与金光";
            }
            if (hour >= 8 && hour < 16)
            {
                return "清澈明亮的自然顶光，投射出锐利深邃的真实阴影，铠甲金属光泽反射极为生动";
            }
            if (hour >= 16 && hour < 19)
            {
                return "瑰丽浓烈的血红残阳沉入地平线，逆光在战士披风与城墙轮廓上镀上灿烂的暖金与暗橙色高光 (Dramatic Rim Lighting)";
            }
            return "深沉幽蓝的夜色笼罩，城垣上火把跳动的橘红火焰 (Torchlight flare) 与营地篝火带来强烈的明暗对比 (Chiaroscuro)";
        }
    }
}
