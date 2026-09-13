using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
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
        public string LightingAndAtmosphere { get; set; } = string.Empty;
        public string ConflictStatus { get; set; } = string.Empty;
        public string SurroundingCharacters { get; set; } = string.Empty;
        public string SurroundingProps { get; set; } = string.Empty;

        public string BuildSummary()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(SettlementName))
            {
                sb.AppendLine($"【定居点】{SettlementName} ({SettlementType})");
            }
            if (!string.IsNullOrWhiteSpace(SpecificLocation))
            {
                sb.AppendLine($"【所处具体子场景】{SpecificLocation}");
            }
            if (!string.IsNullOrWhiteSpace(IndoorOutdoorDetails))
            {
                sb.AppendLine($"【场景空间与布局】{IndoorOutdoorDetails}");
            }
            if (!string.IsNullOrWhiteSpace(SurroundingCharacters))
            {
                sb.AppendLine($"【周围在场人物与动向】{SurroundingCharacters}");
            }
            if (!string.IsNullOrWhiteSpace(SurroundingProps))
            {
                sb.AppendLine($"【近景与周围陈设道具】{SurroundingProps}");
            }
            if (!string.IsNullOrWhiteSpace(ArchitectureStyle))
            {
                sb.AppendLine($"【建筑风格与风貌】{ArchitectureStyle}");
            }
            if (!string.IsNullOrWhiteSpace(TerrainAndLandscape))
            {
                sb.AppendLine($"【地形与地貌】{TerrainAndLandscape}");
            }
            sb.AppendLine($"【时令与天候】{Season}，{Weather}");
            sb.AppendLine($"【时辰与光影】{TimeOfDay}，{LightingAndAtmosphere}");
            if (!string.IsNullOrWhiteSpace(ConflictStatus))
            {
                sb.AppendLine($"【战场与战局状态】{ConflictStatus}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class EnvironmentVisualExtractor
    {
        /// <param name="eventAnchored">为 true 时跳过对当前 Mission/菜单位置的实时探测（用于周报等回顾性场景——环境由事件主题决定，而非玩家当前所在位置）。</param>
        public static EnvironmentVisualProfile Extract(Settlement settlement = null, bool eventAnchored = false)
        {
            var profile = new EnvironmentVisualProfile();

            // 1. 定居点与建筑文化提取
            if (settlement != null)
            {
                profile.SettlementName = settlement.Name != null ? settlement.Name.ToString() : settlement.StringId;
                profile.SettlementType = settlement.IsTown ? "雄伟巨城" : (settlement.IsCastle ? "险要要塞城堡" : "乡野村落");

                string cultureCode = settlement.Culture?.StringId?.ToLowerInvariant() ?? "";
                profile.ArchitectureStyle = ResolveArchitectureStyle(cultureCode, settlement.IsTown, settlement.IsCastle);
                profile.TerrainAndLandscape = ResolveTerrain(settlement);
                profile.ConflictStatus = ResolveConflictStatus(settlement);
            }
            else
            {
                profile.SettlementType = "开阔荒野/野战古战场";
                profile.TerrainAndLandscape = "广袤的卡拉迪亚丘陵起伏地带与地平线远山";
            }

            // 2. 季节时令
            int seasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
            profile.Season = ResolveSeason(seasonIndex);

            // 3. 天气天候
            profile.Weather = ResolveWeather(seasonIndex);

            // 4. 时辰与光影
            int hour = (int)CampaignTime.Now.GetHourOfDay;
            profile.TimeOfDay = ResolveTimeOfDay(hour);
            profile.LightingAndAtmosphere = ResolveLighting(hour, seasonIndex);

            if (!eventAnchored)
            {
                // 5. 深度室内/子场景识别 (如酒馆 Tavern、领主大厅 Lordshall、竞技场 Arena、地牢 Prison、围城城墙城门等)
                ResolveSpecificLocation(profile, settlement);

                // 6. 场景周边人物群像与标志性陈设道具动态提取
                ResolveSurroundings(profile, settlement);
            }

            return profile;
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

            if (isUnderSiege)
            {
                profile.SpecificLocation = "被围攻的要塞城门、护城河壕沟与险峻城堞 (Besieged Fortress Walls, Castle Gate & Ramparts)";
                profile.IndoorOutdoorDetails = "战云密布的中世纪城堡要塞防御前沿：高耸险峻的石砌城堡城堞与箭垛垛口、紧闭包铁的巨型要塞城门与吊桥，城门外是泥泞深邃的护城河壕沟与拒马鹿砦。空气中弥漫着刺鼻的硝烟与大军围城的肃杀死寂。城头守军据险扼守，城下围城大军严阵以待。";
                profile.LightingAndAtmosphere = "暗沉肃杀的天光，城堞垛口上烈烈燃烧的火把投下跳跃的橘红光斑，城外远景处漫山遍野隐现着围城大军的篝火宿营与攻城器械巨影 (Dramatic War Torches, Siege Campfires & Chiaroscuro)";
                profile.ConflictStatus = "【大军围城 · 剑拔弩张】城池正被敌国大军水泄不通地严密围困，城头守将与城下敌将隔着城堞与护城壕进行紧张压迫的战前谈判与意志对决！";
                return;
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

            if (locId.Contains("tavern"))
            {
                profile.SpecificLocation = "城镇酒馆旅店内部 (Tavern / Inn)";
                profile.IndoorOutdoorDetails = "充满中世纪市井烟火气的城镇酒馆内部：粗粝厚重的原木长桌、冒着白色麦沫的陶制大麦酒杯、粗大铁链吊起的铁艺烛台吊灯、跃动着温暖橘红柴火的巨型石砌壁炉，四周隐约可见歇脚的持械雇佣兵、下棋赌骰的酒客与抱着鲁特琴的游吟诗人，浓郁的麦芽酒与熏烤柴火氛围。";
                profile.LightingAndAtmosphere = "室内昏黄温暖的壁炉柴火与烛台微光，在斑驳木梁与粗石墙面上交织出浓郁的明暗对照光影 (Warm Tavern Firelight & Chiaroscuro)";
            }
            else if (locId.Contains("lordshall") || locId.Contains("keep"))
            {
                profile.SpecificLocation = "领主城堡正厅主殿 (Lord's Keep / Throne Hall)";
                profile.IndoorOutdoorDetails = "庄严高耸的领主城堡主殿：大理石立柱与高耸哥特拱券穹顶，两侧悬挂着带有家族图腾的华贵刺绣丝绒挂毯，地面铺着厚重兽皮，铁铸长烛台与壁炉火光在冷硬石墙上投下肃穆阴影。";
            }
            else if (locId.Contains("arena"))
            {
                profile.SpecificLocation = "城镇竞技角斗场 (Town Arena)";
                profile.IndoorOutdoorDetails = "沙石与黄土飞扬的环形角斗场：四周是层叠木石看台，兵刃与木盾插在沙地边缘，充满竞技角逐的狂热与尘土气息。";
            }
            else if (locId.Contains("prison"))
            {
                profile.SpecificLocation = "城堡地下石牢 (Castle Dungeon / Prison)";
                profile.IndoorOutdoorDetails = "潮湿阴冷的地下石牢：沉重锈蚀的精铁栅栏，石壁上渗着水渍与青苔，仅有一支插在铁箍里的摇曳火把投射出昏暗跳动的火光。";
            }
            else if (locId.Contains("center") || (!isIndoor && settlement != null && settlement.IsTown))
            {
                profile.SpecificLocation = "城镇市集街道 (Marketplace / Town Streets)";
                profile.IndoorOutdoorDetails = "熙熙攘攘的中世纪城镇市集街道：两旁是石木结构的民居店铺与遮阳帆布货摊，平民与巡逻卫兵穿行其间。";
            }
            else if (settlement == null)
            {
                // 野外遭遇会话（大地图/野战遭遇）：双方在两军阵前的旷野会面，绝非城镇街道
                profile.SpecificLocation = "两军阵前的开阔旷野会面地 (Field Parley Ground)";
                profile.IndoorOutdoorDetails = "开阔苍茫的旷野临阵会面之地：起伏的草地丘陵与远处隐现的群山地平线，双方军队的旌旗仪仗在身后列阵隐约可见，空气中弥漫着战前谈判的紧绷肃杀气息。";
            }
        }

        private static void ResolveSurroundings(EnvironmentVisualProfile profile, Settlement settlement)
        {
            // 1. 若处于 Mission 场景中，动态统计附近 NPC 角色
            try
            {
                var mission = TaleWorlds.MountAndBlade.Mission.Current;
                if (mission != null)
                {
                    var mainAgent = mission.MainAgent;
                    Vec3 centerPos = mainAgent != null ? mainAgent.Position : Vec3.Zero;
                    var agents = mission.Agents;

                    int guardCount = 0;
                    int nobleCount = 0;
                    int tavernKeeperCount = 0;
                    int tavernWenchCount = 0;
                    int musicianCount = 0;
                    int gamblerCount = 0;
                    int mercenaryCount = 0;
                    int merchantCount = 0;
                    int villagerCount = 0;
                    int townsfolkCount = 0;

                    if (agents != null)
                    {
                        foreach (var agent in agents)
                        {
                            if (agent == null || !agent.IsActive() || agent == mainAgent) continue;

                            if (centerPos != Vec3.Zero && agent.Position.Distance(centerPos) > 25f) continue;

                            var character = agent.Character as CharacterObject;
                            if (character == null) continue;

                            if (character.Occupation == Occupation.Guard || character.Occupation == Occupation.PrisonGuard || character.Occupation == Occupation.Soldier)
                            {
                                guardCount++;
                            }
                            else if (character.IsHero || character.Occupation == Occupation.Lord)
                            {
                                nobleCount++;
                            }
                            else if (character.Occupation == Occupation.Tavernkeeper)
                            {
                                tavernKeeperCount++;
                            }
                            else if (character.Occupation == Occupation.TavernWench)
                            {
                                tavernWenchCount++;
                            }
                            else if (character.Occupation == Occupation.Musician)
                            {
                                musicianCount++;
                            }
                            else if (character.Occupation == Occupation.TavernGameHost)
                            {
                                gamblerCount++;
                            }
                            else if (character.Occupation == Occupation.Mercenary)
                            {
                                mercenaryCount++;
                            }
                            else if (character.Occupation == Occupation.Merchant || character.Occupation == Occupation.GoodsTrader || character.Occupation == Occupation.Artisan || character.Occupation == Occupation.Blacksmith || character.Occupation == Occupation.Armorer || character.Occupation == Occupation.Weaponsmith)
                            {
                                merchantCount++;
                            }
                            else if (character.Occupation == Occupation.Villager)
                            {
                                villagerCount++;
                            }
                            else if (character.Occupation == Occupation.Townsfolk)
                            {
                                townsfolkCount++;
                            }
                        }
                    }

                    var charSb = new StringBuilder();
                    if (guardCount > 0)
                    {
                        charSb.Append($"近处有 {guardCount} 名全副武装的戒备守卫手持长戟/盾矛肃穆巡哨警戒；");
                    }
                    if (nobleCount > 0)
                    {
                        charSb.Append($"席间/近旁有 {nobleCount} 位身着锦缎华服的领地贵族领主与贵妇低声交谈；");
                    }
                    if (tavernKeeperCount > 0 || tavernWenchCount > 0)
                    {
                        charSb.Append("吧台后酒馆老板正在擦拭陶土酒杯，侍女端着木托盘在席间穿梭添送麦芽酒；");
                    }
                    if (musicianCount > 0)
                    {
                        charSb.Append("角落处游吟乐师正在低头拨弄鲁特琴弦奏出中世纪民谣；");
                    }
                    if (mercenaryCount > 0 || gamblerCount > 0)
                    {
                        charSb.Append("旁侧长条木桌旁围坐着数名身佩刀剑的粗犷雇佣兵与掷骰对弈的酒客；");
                    }
                    if (merchantCount > 0)
                    {
                        charSb.Append("两侧摊位处有市集货郎与工匠在整理货架上的布匹器皿；");
                    }
                    if (townsfolkCount > 0 || villagerCount > 0)
                    {
                        charSb.Append("周围空地上穿行着提篮挑担的寻常平民与好奇驻足的当地百姓；");
                    }

                    if (charSb.Length > 0)
                    {
                        profile.SurroundingCharacters = charSb.ToString().TrimEnd('；');
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Surrounding agents extraction: {ex.Message}");
            }

            // 2. 根据所处具体子场景，解析周围标志性环境道具陈设 (SurroundingProps)
            string loc = (profile.SpecificLocation ?? "").ToLowerInvariant();

            if (loc.Contains("围攻") || loc.Contains("fortress") || loc.Contains("rampart") || loc.Contains("gate"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "城头垛口处有手持长弓强弩的城防守军据险警戒，城下护城河外有敌军骑兵与长矛近卫严阵护卫";
                }
                profile.SurroundingProps = "高耸险峻的石砌城堡箭垛、紧闭包铁的千斤沉重城门与绞盘吊桥、城下泥泞深壕、削尖倒插的拒马鹿砦、城头熊熊燃烧的铁皮火把桶、远处黑夜中连绵闪烁的攻城营地篝火";
            }
            else if (loc.Contains("tavern") || loc.Contains("酒馆"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "吧台后酒馆老板正在擦拭陶土酒杯，侍女端着木托盘在席间穿梭，围坐的长桌旁有刀口舔血的雇佣兵在掷骰豪饮，角落游吟乐师弹拨着鲁特琴";
                }
                profile.SurroundingProps = "粗糙厚重的原木长桌、溢出白色酒沫的陶制大麦酒杯、墙上悬挂的鹿角兽首与盾牌装饰、粗铁链吊起的黑色锻铁烛台吊灯、巨型石砌壁炉中熊熊燃烧的噼啪柴火与烤肉铁架";
            }
            else if (loc.Contains("lord") || loc.Contains("keep") || loc.Contains("正厅") || loc.Contains("主殿"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "高耸石柱旁立着手持长戟与盾牌的重甲精锐禁卫，大殿阴影里肃立着捧着卷轴的宫廷侍从与低声耳语的封建贵族";
                }
                profile.SurroundingProps = "雕刻有家族徽记的高背领主宝座、铺展在长条宴桌上的亚麻桌布与银质烛台高脚杯、垂挂在大理石立柱上的华丽丝绒刺绣挂毯、地面整张灰狼皮与熊皮地毯、熊熊燃烧的巨型暖殿壁炉";
            }
            else if (loc.Contains("prison") || loc.Contains("牢") || loc.Contains("dungeon"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "铁栏旁站着腰挎沉重钥匙串的披甲狱卒，邻近阴暗囚室里蜷缩着戴有铁枷的囚犯";
                }
                profile.SurroundingProps = "锈迹斑斑的粗大精铁栅栏、湿滑滴水的青苔黑石墙壁、散落发霉的枯黄稻草垫、墙壁铁箍里跳跃着橘红火苗与黑烟的松明火把、沉重的铁镣与锁链";
            }
            else if (loc.Contains("arena") || loc.Contains("角斗") || loc.Contains("竞技"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "沙地边缘站着手持练习兵刃与木盾的比武战士，四周层叠看台上聚集着喧闹呐喊的市民观众";
                }
                profile.SurroundingProps = "飞扬的黄土沙砾角斗场、四周环形层叠的木石看台、插在沙地边缘的木质训练剑与盾牌、随风舞动的比武彩旗与观众席栏杆";
            }
            else if (loc.Contains("center") || loc.Contains("市集") || loc.Contains("街道") || loc.Contains("街"))
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "沿街货摊遮阳棚下有叫卖布匹香料的货郎，石板路上有手按佩剑巡视的城防守卫，以及提篮穿行的市井平民";
                }
                profile.SurroundingProps = "石木结构的民居店铺、各色粗亚麻遮阳帆布棚顶、堆放着陶罐麻袋与木箱的货摊、钉满铁蹄的木车轮与系在拴马桩上的挽马、铁匠铺炉膛里冒出的青烟";
            }
            else
            {
                if (string.IsNullOrEmpty(profile.SurroundingCharacters))
                {
                    profile.SurroundingCharacters = "身侧肃立着披甲随从与战备护卫，周围是开阔原野与扎营连绵的队伍";
                }
                profile.SurroundingProps = "驻扎的行军牛皮帐篷、插在草地上的锋利矛戈与彩绘战盾、余烬微红的野外行军篝火、系在树桩旁的战马与运粮大车";
            }
        }

        private static string ResolveArchitectureStyle(string culture, bool isTown, bool isCastle)
        {
            // 若为乡村聚落，真实反映田园乡野民居，而非宏伟城墙穹顶
            if (!isTown && !isCastle)
            {
                if (culture.Contains("empire"))
                {
                    return "帝国的地中海风情乡野村落：白石泥墙农舍、红陶瓦坡屋顶、石砌水井与粮仓，四周环绕着金黄麦田、葡萄架与木栅栏";
                }
                if (culture.Contains("vlandia"))
                {
                    return "西欧诺曼式庄园村落：茅草土坯木屋、谷仓磨坊、蜿蜒泥泞的乡间小道与绿意盎然的放牧草场";
                }
                if (culture.Contains("battania"))
                {
                    return "高地原始林间聚落：粗糙原木茅舍、苔藓覆盖的石围墙、袅袅炊烟与茂密的古橡树林";
                }
                if (culture.Contains("aserai"))
                {
                    return "荒漠绿洲泥砖村庄：平顶晒台泥舍、水渠井台、高耸的椰枣树与漫卷黄沙边缘的骆驼围栏";
                }
                if (culture.Contains("khuzait"))
                {
                    return "草原游牧集居地：星罗棋布的圆形毛毡蒙古包毡帐、木质马厩、牛羊围栏与广袤无垠的草甸";
                }
                if (culture.Contains("sturgia"))
                {
                    return "北境渔猎雪村：厚重原木长屋、烟熏木制干燥棚、避风港湾里停泊的小木舟与覆盖着厚雪的林缘小径";
                }
                return "宁静淳朴的中世纪乡村聚落：茅草屋、木栅栏与起伏农田";
            }

            if (culture.Contains("empire"))
            {
                return isCastle
                    ? "拜占庭与罗马式险要依山要塞城堡，层叠重石要塞、高耸箭塔与巡道城垛"
                    : "拜占庭与罗马式巨石古典都会，恢弘的大理石圆顶大教堂、斑驳的罗马石砌拱券长廊与巍峨重石城墙";
            }
            if (culture.Contains("vlandia"))
            {
                return isCastle
                    ? "西欧诺曼哥特式高耸方石古堡，带有角楼巡道、射击狭缝与悬台要塞，冷峻威严"
                    : "繁华的诺曼式封建中世纪城市，砖石商行、双层拱顶行会大厅与高耸钟楼";
            }
            if (culture.Contains("battania"))
            {
                return "凯尔特高地风格的粗犷原木与巨石垒砌要塞，青苔覆石，幽深冷杉密林环绕";
            }
            if (culture.Contains("aserai"))
            {
                return isCastle
                    ? "沙漠边陲巨石要塞，高耸圆柱形瞭望哨塔与干燥坚硬的泥砖雉堞"
                    : "阿拉伯与马穆鲁克风泥坯平顶都会、精美马蹄形镂空拱券、集市挂满色彩斑斓的羊毛地毯与香料帐幔";
            }
            if (culture.Contains("khuzait"))
            {
                return "中亚草原游牧穹庐毡帐营地与泥木简易要塞，猎鹰旗帜在呼啸烈风中猎猎作响";
            }
            if (culture.Contains("sturgia"))
            {
                return "斯拉夫与维京式重木大厅（Mead Hall），圆木墙体包裹厚重兽皮，积雪倾覆屋顶";
            }
            return isCastle ? "坚不可摧的欧式中世纪石堡" : "熙熙攘攘的典型中世纪城市";
        }

        private static string ResolveTerrain(Settlement settlement)
        {
            string culture = settlement.Culture?.StringId?.ToLowerInvariant() ?? "";
            if (culture.Contains("aserai"))
            {
                return "漫无边际的起伏红砂荒漠与灼热沙丘，点缀着微风拂动的棕榈绿洲";
            }
            if (culture.Contains("sturgia"))
            {
                return "被积雪终年冰封的北境冻土原野、苍茫深邃的黑松林与冷冽河流";
            }
            if (culture.Contains("battania"))
            {
                return "多雾潮湿的青翠高地群山、嶙峋岩壁与古老橡树密林";
            }
            return "广袤丰饶的欧式平原农田、起伏丘陵与远处起伏的山脊地平线";
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
                return "【盛世安澜】城内繁盛太平，街道商贩车水马龙，锦旗随风舒卷，呈现出中世纪都会的蓬勃生机";
            }
            return "平和肃穆的日常城防守备状态";
        }

        private static string ResolveSeason(int seasonIndex)
        {
            switch (seasonIndex)
            {
                case 0:
                    return "春意盎然 (早春残雪消融，嫩草初生)";
                case 1:
                    return "盛夏烈日 (日光炽烈，晴空深远)";
                case 2:
                    return "深秋萧瑟 (金黄枫叶与枯草，落叶在秋风中翻滚)";
                case 3:
                    return "凛冬漫天 (极寒严冬，天地银装素裹，积雪深厚)";
                default:
                    return "平季";
            }
        }

        private static string ResolveWeather(int seasonIndex)
        {
            if (seasonIndex == 3)
            {
                return "狂风卷着雪花在空中呼啸飞扬，寒气逼人";
            }
            return "空气澄澈明净，视野开阔，偶尔掠过几缕流云";
        }

        private static string ResolveTimeOfDay(int hour)
        {
            if (hour >= 5 && hour < 8) return "破晓清晨 (Dawn)";
            if (hour >= 8 && hour < 16) return "明媚白昼 (Midday)";
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
