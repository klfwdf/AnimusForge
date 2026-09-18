using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Illustrator.Context
{
    public sealed class HeroVisualProfile
    {
        public string HeroName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Culture { get; set; } = string.Empty;
        public string CultureId { get; set; } = string.Empty;
        public string SocialStatus { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string SpeciesDescription { get; set; } = string.Empty;
        public int Age { get; set; }
        public string ClanName { get; set; } = string.Empty;
        public string KingdomName { get; set; } = string.Empty;
        public string PrimaryBannerColorHex { get; set; } = string.Empty;
        public string SecondaryBannerColorHex { get; set; } = string.Empty;
        public string BannerDescription { get; set; } = string.Empty;
        public string CultureLore { get; set; } = string.Empty;
        public string FactionLore { get; set; } = string.Empty;
        public string BackgroundLore { get; set; } = string.Empty;
        public string TraitsSummary { get; set; } = string.Empty;
        public string TopSkillsSummary { get; set; } = string.Empty;
        public string PhysicalFeatures { get; set; } = string.Empty;
        public string CurrentStateDetail { get; set; } = string.Empty;
        public List<string> EquipmentDetails { get; set; } = new List<string>();
        public List<string> WeaponDetails { get; set; } = new List<string>();
        public List<string> BannerEquipmentDetails { get; set; } = new List<string>();
        public string MountDetail { get; set; } = string.Empty;
        public CharacterAppearanceSnapshot Appearance { get; set; }
        public string EquipmentCode { get; set; } = string.Empty;
        public string EquipmentSource { get; set; } = string.Empty;
        public string HeadgearDetail { get; set; } = string.Empty;
        public string LiveryColorsSummary { get; set; } = string.Empty;
        public bool HasHeraldicArmor { get; set; }
        public bool HasHeraldicShield { get; set; }

        public string BuildSummary()
        {
            return BuildVisualSummary() + "\n" + BuildDirectorOnlyFacts();
        }

        public string BuildDirectorOnlyFacts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【人物背景归属】" + HeroName);
            if (!string.IsNullOrWhiteSpace(CultureLore)) sb.AppendLine("【文化背景原文】" + CultureLore);
            if (!string.IsNullOrWhiteSpace(FactionLore)) sb.AppendLine("【势力背景原文】" + FactionLore);
            if (!string.IsNullOrWhiteSpace(BackgroundLore)) sb.AppendLine("【人物生平原文】" + BackgroundLore);
            if (!string.IsNullOrWhiteSpace(TraitsSummary)) sb.AppendLine("【性格参考】" + TraitsSummary);
            if (!string.IsNullOrWhiteSpace(TopSkillsSummary)) sb.AppendLine("【专长参考】" + TopSkillsSummary);
            return sb.ToString().TrimEnd();
        }

        public string BuildVisualSummary(bool includeMount = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"【人物身份】{HeroName}" + (!string.IsNullOrWhiteSpace(Title) ? $" · {Title}" : "") +
                $" ({Culture}文化, {Gender}, 约{Age}岁" + (!string.IsNullOrWhiteSpace(SocialStatus) ? $", 身份: {SocialStatus}" : "") + ")");
            if (!string.IsNullOrWhiteSpace(SpeciesDescription)) sb.AppendLine("【真实种族/物种】" + SpeciesDescription);
            if (!string.IsNullOrWhiteSpace(ClanName) || !string.IsNullOrWhiteSpace(BannerDescription))
            {
                string factionPart = !string.IsNullOrWhiteSpace(KingdomName) ? $", 所属王国: {KingdomName}" : "";
                sb.AppendLine($"【家族与纹章】" + (!string.IsNullOrWhiteSpace(ClanName) ? $"所属家族: {ClanName}{factionPart}" : "") +
                    (!string.IsNullOrWhiteSpace(BannerDescription) ? $" | {BannerDescription}" : $" (识别色: {PrimaryBannerColorHex}/{SecondaryBannerColorHex})"));
            }
            if (!string.IsNullOrWhiteSpace(LiveryColorsSummary))
            {
                sb.AppendLine("【服饰与阵营布料配色】" + LiveryColorsSummary);
            }
            if (!string.IsNullOrWhiteSpace(CurrentStateDetail))
            {
                sb.AppendLine($"【当前处境状态】{CurrentStateDetail}");
            }
            if (!string.IsNullOrWhiteSpace(PhysicalFeatures))
            {
                sb.AppendLine($"【面貌骨相与发型】{PhysicalFeatures}");
            }
            if (!string.IsNullOrWhiteSpace(EquipmentSource)) sb.AppendLine("【装备快照来源】" + EquipmentSource);
            if (!string.IsNullOrWhiteSpace(HeadgearDetail)) sb.AppendLine("【当前头戴装备】" + HeadgearDetail);
            if (EquipmentDetails.Count > 0)
            {
                sb.AppendLine($"【真实穿戴装备与材质】" + string.Join("，", EquipmentDetails));
            }
            if (WeaponDetails.Count > 0)
            {
                sb.AppendLine($"【当前装备中的武器与盾牌（不得替换成旗帜）】" + string.Join("，", WeaponDetails));
            }
            if (BannerEquipmentDetails.Count > 0)
            {
                sb.AppendLine("【旗帜装备栏记录（非普通手持武器、非现场可见性证据）】" + string.Join("，", BannerEquipmentDetails) +
                    "；完整读取该槽不代表本人正在举旗。百科肖像不展示旗帜；现场或事件插画须另有可见/使用证据，不能因该栏有物品就安排举旗或布置背景旗。");
            }
            if (includeMount && !string.IsNullOrWhiteSpace(MountDetail))
            {
                sb.AppendLine($"【可用坐骑装备（仅在场景支持骑乘时入画）】{MountDetail}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class HeroVisualExtractor
    {
        public static HeroVisualProfile Extract(Hero hero, bool useCivilian = false, Equipment equipmentSnapshot = null, string equipmentSource = null, CharacterAppearanceSnapshot appearance = null)
        {
            if (hero == null)
            {
                return new HeroVisualProfile { HeroName = "未知角色" };
            }

            string cultureName = hero.Culture?.Name != null ? hero.Culture.Name.ToString() : "未知文化";
            string cultureId = hero.Culture?.StringId?.ToLowerInvariant() ?? "";

            string socialStatus;
            string title = ExtractTitleAndStatus(hero, out socialStatus);

            var profile = new HeroVisualProfile
            {
                HeroName = hero.Name != null ? hero.Name.ToString() : "英雄",
                Title = title,
                Culture = cultureName,
                CultureId = cultureId,
                SocialStatus = socialStatus,
                Gender = hero.IsFemale ? "女性" : "男性",
                Age = (int)hero.Age,
                SpeciesDescription = ResolveSpeciesDescription(hero),
                ClanName = hero.Clan?.Name != null ? hero.Clan.Name.ToString() : string.Empty,
                KingdomName = hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : string.Empty,
                Appearance = appearance
            };

            // 1. 全量动态读取文化背景与官方百科传记 (彻底兼容所有 MOD，绝不硬编码)
            profile.CultureLore = ExtractCultureLore(hero);

            // 2. 全量动态读取王国与家族官方背景传记
            profile.FactionLore = ExtractFactionLore(hero);

            // 3. 提取人物生平传记、家庭关系与统治规模
            profile.BackgroundLore = ExtractBackgroundLore(hero);

            // 4. 提取五大性格心理特质与神态
            profile.TraitsSummary = ExtractTraitsSummary(hero);

            // 5. 提取人物顶尖专长技能 (动态识别战神、神射手、医师、智囊等)
            profile.TopSkillsSummary = ExtractTopSkillsSummary(hero);

            // 5b. 提取当前处境状态 (俘虏关押/负伤/留驻/随军/已故)
            profile.CurrentStateDetail = ExtractCurrentState(hero);

            // 6. 提取纹章识别色、家族旗帜与阵营服饰布料主色 (优先使用立绘渲染色彩或当前所属王国主色)
            uint liveryColor1 = (appearance != null && appearance.Color1 != 0)
                ? appearance.Color1
                : (hero.Clan?.Kingdom != null && hero.Clan.Kingdom.Color != 0)
                    ? hero.Clan.Kingdom.Color
                    : (hero.MapFaction != null && hero.MapFaction.Color != 0)
                        ? hero.MapFaction.Color
                        : (hero.Clan != null && hero.Clan.Color != 0)
                            ? hero.Clan.Color
                            : (hero.Culture != null ? hero.Culture.Color : 0);

            uint liveryColor2 = (appearance != null && appearance.Color2 != 0)
                ? appearance.Color2
                : (hero.Clan?.Kingdom != null && hero.Clan.Kingdom.Color2 != 0)
                    ? hero.Clan.Kingdom.Color2
                    : (hero.MapFaction != null && hero.MapFaction.Color2 != 0)
                        ? hero.MapFaction.Color2
                        : (hero.Clan != null && hero.Clan.Color2 != 0)
                            ? hero.Clan.Color2
                            : (hero.Culture != null ? hero.Culture.Color2 : 0);

            if (liveryColor1 != 0)
            {
                string c1Name = ResolveColorName(liveryColor1);
                string c1Hex = "#" + (liveryColor1 & 0x00FFFFFF).ToString("X6");
                string c2Part = "";
                if (liveryColor2 != 0 && liveryColor2 != liveryColor1)
                {
                    c2Part = $"，副色为 {ResolveColorName(liveryColor2)} (#{(liveryColor2 & 0x00FFFFFF):X6})";
                }
                profile.LiveryColorsSummary = $"人物所属阵营服饰主色为【{c1Name}】({c1Hex}){c2Part}；其身着的披风、斗篷、罩袍长袍及盔甲布料内衬鲜明展现此阵营色彩，呈现饱满的历史布料光泽与质感。";
            }
            if (hero.Clan != null)
            {
                // 纹章识别色必须与参考图一致：参考图按 Clan.Banner 合成（家族色），家族色缺失时才退回王国色
                uint bannerPri = hero.Clan.Color != 0 ? hero.Clan.Color : (hero.Clan.Kingdom != null ? hero.Clan.Kingdom.Color : 0);
                uint bannerSec = hero.Clan.Color2 != 0 ? hero.Clan.Color2 : (hero.Clan.Kingdom != null ? hero.Clan.Kingdom.Color2 : 0);
                profile.PrimaryBannerColorHex = "#" + (bannerPri & 0x00FFFFFF).ToString("X6");
                profile.SecondaryBannerColorHex = "#" + (bannerSec & 0x00FFFFFF).ToString("X6");
                profile.BannerDescription = ExtractBannerDescription(hero);
            }

            // 7. 提取装备槽位 (真实反射装备，绝无粗麻学者硬编码)
            Equipment equipment = equipmentSnapshot ?? (useCivilian ? hero.CivilianEquipment : hero.BattleEquipment);
            ApplyEquipmentSnapshot(profile, equipment, equipmentSource ?? (useCivilian ? "人物便服装备栏" : "人物战斗装备栏"));

            // 8. 提取生理与面部特征 (根据头部全遮蔽状态智能调整)
            profile.PhysicalFeatures = ExtractPhysicalFeatures(hero, equipment, appearance);

            TaleWorlds.Library.Debug.Print($"[IllustratorFidelity] hero={hero.StringId}, equipmentSource={profile.EquipmentSource}, head={equipment?[EquipmentIndex.Head].Item?.StringId ?? "empty"}, shield={profile.WeaponDetails.Exists(x => x.StartsWith("盾牌: ") && !x.Contains("无盾牌"))}");
            return profile;
        }

        internal static void ApplyEquipmentSnapshot(HeroVisualProfile profile, Equipment equipment, string source)
        {
            profile.EquipmentSource = source ?? string.Empty;
            profile.EquipmentDetails.Clear();
            profile.WeaponDetails.Clear();
            profile.HeadgearDetail = string.Empty;
            profile.MountDetail = string.Empty;
            profile.EquipmentCode = string.Empty;
            if (equipment == null) return;
            // Native serialization retains all 12 slots, including empty slots and modifiers.
            equipment = CharacterAppearanceSnapshot.VisibleEquipment(equipment);
            profile.EquipmentCode = equipment.CalculateEquipmentCode();
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Head, "头部", null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Body, "身甲", null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Cape, "披风", null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Gloves, "手部", null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Leg, "腿部", null);
            ExtractWeapons(profile, equipment, null);
            ExtractMount(profile, equipment);
        }

        private static string ResolveSpeciesDescription(Hero hero)
        {
            if (hero?.CharacterObject == null) return string.Empty;
            string identity = ((hero.CharacterObject.StringId ?? string.Empty) + " " + (hero.Culture?.StringId ?? string.Empty) + " " + (hero.Culture?.Name?.ToString() ?? string.Empty)).ToLowerInvariant();
            var known = new[] { new[] { "兽人", "orc", "orcs", "greenskin", "warhammer_orc" }, new[] { "地精", "goblin", "goblins", "snotling" }, new[] { "精灵", "elf", "elves", "elven", "asrai", "druchii" }, new[] { "矮人", "dwarf", "dwarves", "dawi" }, new[] { "鼠人", "skaven" }, new[] { "野兽人", "beastman", "beastmen", "minotaur" }, new[] { "混沌", "chaos", "daemon", "demon" }, new[] { "巨魔", "troll", "ogre" } };
            foreach (var group in known)
                foreach (string token in group)
                    if (identity.Contains(token)) return group[0];
            if (hero.CharacterObject.Race != 0) return "自定义非人类种族（游戏 Race=" + hero.CharacterObject.Race + "，以真实立绘为准）";
            return "人类（若 MOD 通过立绘加入特殊物种，以参考图和文化设定为准）";
        }

        private static string ExtractCultureLore(Hero hero)
        {
            if (hero.Culture == null) return string.Empty;
            var sb = new StringBuilder();
            string cultureName = hero.Culture.Name != null ? hero.Culture.Name.ToString() : hero.Culture.StringId;
            sb.Append($"{cultureName}文化。");

            // 全量读取 MOD 或游戏底层为该文化编写的官方百科传记
            try
            {
                if (hero.Culture.EncyclopediaText != null)
                {
                    string lore = hero.Culture.EncyclopediaText.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(lore))
                    {
                        lore = System.Text.RegularExpressions.Regex.Replace(lore, "<.*?>", string.Empty).Trim();
                        if (lore.Length > 280) lore = lore.Substring(0, 280) + "...";
                        sb.Append($" 文化风貌与传统：{lore}");
                    }
                }
            }
            catch { }

            return sb.ToString().Trim();
        }

        private static string ExtractFactionLore(Hero hero)
        {
            var sb = new StringBuilder();

            // 王国背景
            if (hero.Clan?.Kingdom != null)
            {
                string kingdomName = hero.Clan.Kingdom.Name != null ? hero.Clan.Kingdom.Name.ToString() : "王国";
                sb.Append($"所属王国: {kingdomName}。");

                try
                {
                    if (hero.Clan.Kingdom.EncyclopediaText != null)
                    {
                        string kLore = hero.Clan.Kingdom.EncyclopediaText.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(kLore))
                        {
                            kLore = System.Text.RegularExpressions.Regex.Replace(kLore, "<.*?>", string.Empty).Trim();
                            if (kLore.Length > 200) kLore = kLore.Substring(0, 200) + "...";
                            sb.Append($" 王国历史与势力格局: {kLore}。");
                        }
                    }
                }
                catch { }
            }

            // 家族背景与声誉
            if (hero.Clan != null)
            {
                string clanName = hero.Clan.Name != null ? hero.Clan.Name.ToString() : "家族";
                sb.Append($" 所属家族: {clanName} (声望第{hero.Clan.Tier}阶)。");

                try
                {
                    if (hero.Clan.EncyclopediaText != null)
                    {
                        string cLore = hero.Clan.EncyclopediaText.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(cLore))
                        {
                            cLore = System.Text.RegularExpressions.Regex.Replace(cLore, "<.*?>", string.Empty).Trim();
                            if (cLore.Length > 180) cLore = cLore.Substring(0, 180) + "...";
                            sb.Append($" 宗族历史: {cLore}。");
                        }
                    }
                }
                catch { }
            }

            return sb.ToString().Trim();
        }

        private static string ExtractTitleAndStatus(Hero hero, out string socialStatus)
        {
            bool isFemale = hero.IsFemale;
            string kingdomName = hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : (hero.Culture?.Name != null ? hero.Culture.Name.ToString() : "阵营");
            string clanName = hero.Clan?.Name != null ? hero.Clan.Name.ToString() : "家族";

            bool isFactionLeader = hero.IsFactionLeader || (hero.MapFaction != null && hero.MapFaction.Leader == hero);
            bool isClanLeader = hero.Clan != null && hero.Clan.Leader == hero;

            if (isFactionLeader)
            {
                socialStatus = "最高统治者/阵营至尊君主 (Faction Sovereign & Supreme Monarch)";
                return isFemale
                    ? $"{kingdomName} 最高女统治者/至尊君主 (Supreme Sovereign Queen/Empress of {kingdomName})"
                    : $"{kingdomName} 最高统治者/至尊君王 (Supreme Sovereign King/Monarch of {kingdomName})";
            }

            if (hero.IsLord)
            {
                if (isClanLeader)
                {
                    socialStatus = "宗族首领/世袭大领主 (Clan Patriarch/Matriarch & High Feudal Lord)";
                    return isFemale
                        ? $"{clanName} 家族领袖/贵胄贵妇 (High Noble Lady & Patriarch of {clanName})"
                        : $"{clanName} 家族宗主/显贵大领主 (High Feudal Lord & Patriarch of {clanName})";
                }
                else
                {
                    socialStatus = "封建贵族领主 (Feudal Noble Lord/Lady)";
                    return isFemale
                        ? $"{clanName} 贵族千金/贵妇 (Noble Lady of {clanName})"
                        : $"{clanName} 贵族领主/武将骑士 (Noble Lord of {clanName})";
                }
            }

            if (hero.IsNotable)
            {
                socialStatus = "城镇/乡村有影响力的要人名流 (Influential Town/Village Notable)";
                switch (hero.Occupation)
                {
                    case Occupation.Merchant:
                        return "商会行首/富商巨贾 (Wealthy Merchant Magnate & Guildmaster)";
                    case Occupation.Artisan:
                        return "工匠行会宗匠/名匠大师 (Master Craftsman & Guild Artisan)";
                    case Occupation.GangLeader:
                        return "地下黑街首领/教父 (Underworld Boss & Shadow Kingpin)";
                    case Occupation.Preacher:
                        return "圣所大祭司/宗教先知 (High Preacher & Holy Cleric)";
                    case Occupation.Headman:
                        return "村落耆老/庄园保正 (Respected Village Elder & Headman)";
                    default:
                        return "地方名门要人 (Prominent Notable)";
                }
            }

            if (hero.IsWanderer)
            {
                socialStatus = "在野游侠同伴 (Wandering Hero & Companion)";
                // 动态依据其实际最高特长识别职业
                int med = hero.GetSkillValue(DefaultSkills.Medicine);
                int eng = hero.GetSkillValue(DefaultSkills.Engineering);
                int rog = hero.GetSkillValue(DefaultSkills.Roguery);
                int combat = Math.Max(hero.GetSkillValue(DefaultSkills.OneHanded), Math.Max(hero.GetSkillValue(DefaultSkills.TwoHanded), hero.GetSkillValue(DefaultSkills.Polearm)));

                if (med >= 80 && med >= combat) return "随军医师/在野学者智囊 (Traveling Physician & Intellectual Scholar)";
                if (eng >= 80 && eng >= combat) return "攻城技工大师/随军工兵 (Siege Engineer & Artificer)";
                if (rog >= 80 && rog >= combat) return "边境独行游侠/荒原浪子 (Trail Ranger & Outlaw Companion)";
                if (combat >= 100) return "百战剑客/独行雇佣勇士 (Hardened Mercenary Warrior)";
                return "卡拉迪亚在野游历同伴 (Wandering Companion)";
            }

            socialStatus = "知名人物 (Notable Character)";
            return "英雄角色 (Notable Hero)";
        }

        /// <summary>
        /// 提取人物当前处境状态：俘虏关押位置与看押方、负伤、留驻、随军、已故。
        /// 俘虏状态附带装束约束（武器收缴/不披战甲），覆盖百科、会话、周报全部链路。
        /// </summary>
        private static string ExtractCurrentState(Hero hero)
        {
            try
            {
                var states = new List<string>();

                if (hero.IsDead)
                {
                    states.Add("已故（画面可作为肃穆的纪念性肖像或生平回顾氛围呈现）");
                    return string.Join("；", states);
                }

                if (hero.IsPrisoner)
                {
                    var sb = new StringBuilder("阶下囚/战俘状态");
                    PartyBase holder = null;
                    try { holder = hero.PartyBelongedToAsPrisoner; } catch { }
                    if (holder != null)
                    {
                        try
                        {
                            if (holder.IsSettlement && holder.Settlement != null)
                            {
                                Settlement s = holder.Settlement;
                                string sName = s.Name != null ? s.Name.ToString() : s.StringId;
                                string sType = s.IsCastle ? "城堡" : (s.IsTown ? "城镇" : "定居点");
                                sb.Append("：被关押在 ").Append(sName).Append("（").Append(sType).Append("）的地牢囚室中");
                            }
                            else if (holder.IsMobile && holder.MobileParty != null)
                            {
                                string pName = holder.MobileParty.Name != null ? holder.MobileParty.Name.ToString() : "一支队伍";
                                sb.Append("：正被 ").Append(pName).Append(" 的队伍看押（具体设施与锁具未确认）");
                            }
                        }
                        catch
                        {
                        }
                    }
                    sb.Append("；仅确认俘虏身份，不据此推导换装、缴械或镣铐；当前穿戴以本次装备与人物参考图为准");
                    states.Add(sb.ToString());
                }
                else
                {
                    if (hero.IsWounded)
                    {
                        states.Add("当前负伤；绷带、伤口与动作须有实际视觉证据");
                    }
                    try
                    {
                        if (hero.StayingInSettlement != null)
                        {
                            Settlement s = hero.StayingInSettlement;
                            states.Add("正留驻/下榻于 " + (s.Name != null ? s.Name.ToString() : s.StringId));
                        }
                        else if (hero.PartyBelongedTo != null)
                        {
                            states.Add("正随 " + (hero.PartyBelongedTo.Name != null ? hero.PartyBelongedTo.Name.ToString() : "其队伍") + " 行军扎营");
                        }
                    }
                    catch
                    {
                    }
                }

                return string.Join("；", states);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractTopSkillsSummary(Hero hero)
        {
            try
            {
                var skillList = new List<KeyValuePair<string, int>>
                {
                    new KeyValuePair<string, int>("单手武器", hero.GetSkillValue(DefaultSkills.OneHanded)),
                    new KeyValuePair<string, int>("双手武器", hero.GetSkillValue(DefaultSkills.TwoHanded)),
                    new KeyValuePair<string, int>("长柄武器", hero.GetSkillValue(DefaultSkills.Polearm)),
                    new KeyValuePair<string, int>("弓箭", hero.GetSkillValue(DefaultSkills.Bow)),
                    new KeyValuePair<string, int>("十字弩", hero.GetSkillValue(DefaultSkills.Crossbow)),
                    new KeyValuePair<string, int>("战术", hero.GetSkillValue(DefaultSkills.Tactics)),
                    new KeyValuePair<string, int>("统御", hero.GetSkillValue(DefaultSkills.Leadership)),
                    new KeyValuePair<string, int>("魅力", hero.GetSkillValue(DefaultSkills.Charm)),
                    new KeyValuePair<string, int>("管理", hero.GetSkillValue(DefaultSkills.Steward)),
                    new KeyValuePair<string, int>("医术", hero.GetSkillValue(DefaultSkills.Medicine)),
                    new KeyValuePair<string, int>("工程", hero.GetSkillValue(DefaultSkills.Engineering)),
                    new KeyValuePair<string, int>("狡诈/流氓习气", hero.GetSkillValue(DefaultSkills.Roguery)),
                    new KeyValuePair<string, int>("交易", hero.GetSkillValue(DefaultSkills.Trade)),
                    new KeyValuePair<string, int>("骑术", hero.GetSkillValue(DefaultSkills.Riding))
                };

                skillList.Sort((a, b) => b.Value.CompareTo(a.Value));

                var top = new List<string>();
                for (int i = 0; i < Math.Min(3, skillList.Count); i++)
                {
                    if (skillList[i].Value >= 50)
                    {
                        top.Add($"{skillList[i].Key} (造诣: {skillList[i].Value})");
                    }
                }

                return top.Count > 0 ? string.Join("，", top) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractTraitsSummary(Hero hero)
        {
            var traits = new List<string>();

            int calculating = hero.GetTraitLevel(DefaultTraits.Calculating);
            int honor = hero.GetTraitLevel(DefaultTraits.Honor);
            int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
            int valor = hero.GetTraitLevel(DefaultTraits.Valor);
            int generosity = hero.GetTraitLevel(DefaultTraits.Generosity);

            if (calculating > 0)
                traits.Add("【深谋远虑/算度深沉】眼神如深潭般沉静锐利，神情喜怒不形于色，带着洞察世事的城府与谋略 (Deeply calculating, analytical gaze, calm unreadable composure)");
            else if (calculating < 0)
                traits.Add("【豪迈直率/烈火心性】目光热烈张扬，神态果决桀骜，透出直来直去的狂放脾气 (Bold, impulsive, fiery and direct expression)");

            if (honor > 0)
                traits.Add("【高洁信守/正直坦荡】眉宇轩昂坦荡，下颌从容昂起，自带令人敬畏的道德威严与光明磊落的气度 (Honorable, dignified, righteous posture with commanding moral presence)");
            else if (honor < 0)
                traits.Add("【权谋现实/从容冷酷】唇角微抿带着一丝难以捉摸的淡漠笑意，眼神透出看透利益规则的现实与狡黠 (Pragmatic, cynical, subtle knowing smirk, ruthless political cunning)");

            if (mercy > 0)
                traits.Add("【仁慈温和/悲悯天下】目光中蕴含着成熟君主的温和与慈爱，神色亲和从容，散发着宽厚仁善的人格光芒 (Merciful, benevolent, gentle warmth in eyes, compassionate grace)");
            else if (mercy < 0)
                traits.Add("【铁血雷霆/杀伐决断】目光森冷凛冽如出鞘利刃，面部轮廓紧绷刚硬，散发着令人不寒而栗的铁血威慑 (Merciless, cold-blooded, stern unforgiving gaze, chilling iron authority)");

            if (valor > 0)
                traits.Add("【英勇无畏/百战雄风】身姿昂然挺拔如险峰，眉宇间流露出睥睨沙场的万丈豪情与战将霸气 (Valiant, heroic, fearless warrior aura, commanding martial vigor)");

            if (generosity > 0)
                traits.Add("【大度豪迈/皇者气魄】举手投足从容舒展，气象恢弘，流露出天下为怀的宽广气度 (Generous, magnanimous, open-handed sovereign nobility)");
            else if (generosity < 0)
                traits.Add("【严苛节制/一丝不苟】神态紧凑内敛，目光细致挑剔，流露出深思熟虑的严密节制 (Frugal, austere, meticulous and tightly guarded)");

            try
            {
                TraitObject persona = hero.CharacterObject?.GetPersona();
                if (persona != null)
                {
                    traits.Add($"个性基调: {persona.Name}");
                }
            }
            catch { }

            return traits.Count > 0 ? string.Join("；", traits) : "神态沉稳内敛，举止符合所属文化社会规范";
        }

        private static void ExtractArmorSlot(HeroVisualProfile profile, Equipment equipment, EquipmentIndex slot, string slotName, Hero hero)
        {
            EquipmentElement element = equipment[slot];

            if (element.Item == null)
            {
                if (slot == EquipmentIndex.Head)
                {
                    profile.HeadgearDetail = "当前所选装备快照的头部槽为空；不得凭身份添加头盔、冠冕或头饰。若现场截图显示不同装备，应报告来源不一致，不把其他装备栏当作当前穿戴。";
                    profile.EquipmentDetails.Add("头部: 当前装备快照未佩戴头部装备");
                }
                else if (slot == EquipmentIndex.Cape)
                {
                    profile.EquipmentDetails.Add("颈部与肩部: 领口自然裸露，未佩戴斗篷或围巾 (Bare neck/shoulders, NO cowl, NO cape)");
                }
                else if (slot == EquipmentIndex.Gloves)
                {
                    profile.EquipmentDetails.Add("手部: 未戴铁手套，双手自然裸露 (Bare hands, no armored gauntlets)");
                }
                return;
            }

            ItemObject item = element.Item;
            string itemName = item.Name != null ? item.Name.ToString() : item.StringId;
            string modifierStr = element.ItemModifier?.Name != null ? element.ItemModifier.Name.ToString() + "的" : "";
            int tier = (int)item.Tier;

            string materialStr = ResolveItemMaterial(item);
            bool isHeraldic = item.IsUsingTableau;
            if (slot == EquipmentIndex.Body && isHeraldic)
            {
                profile.HasHeraldicArmor = true;
            }

            if (slot == EquipmentIndex.Head)
            {
                string hId = (item.StringId ?? "").ToLowerInvariant();
                string hName = (itemName ?? "").ToLowerInvariant();
                if (hId.Contains("daimao") || hName.Contains("呆喵"))
                {
                    profile.HeadgearDetail = $"已佩戴头部装备：呆喵（物品ID: {item.StringId}，全包覆立体蓝白猫咪头饰）。标志性的蓝白双色立体猫咪头饰（前脸为纯白底色的呆萌猫猫面具表情，后脑与猫耳为天蓝色），完全笼罩整个头部与脸庞，完整呈现参考图中的猫咪头套原貌。";
                }
                else
                {
                    profile.HeadgearDetail = BuildHeadgearDescription(modifierStr + itemName, item.StringId, materialStr,
                        item.ArmorComponent?.HairCoverType == ArmorComponent.HairCoverTypes.All,
                        item.ArmorComponent?.BeardCoverType == ArmorComponent.BeardCoverTypes.All);
                }
            }

            if (slot == EquipmentIndex.Cape)
            {
                profile.EquipmentDetails.Add($"披风/肩部装备: {modifierStr}{itemName} ({materialStr}, 等阶Tier {tier})；按人物参考图保留肩颈覆盖范围、轮廓宽窄、扣合位置、固有主色与叠穿关系；名称不代表具体形状，不将披肩改写成内衬或金属肩甲");
                return;
            }

            string heraldicNote = (isHeraldic && slot == EquipmentIndex.Body) ? "，身覆家族纹章图案/罩袍" : "";
            string desc = $"{slotName}: {modifierStr}{itemName} ({materialStr}, 等阶Tier {tier}{heraldicNote})；具体颜色与纹样以人物参考图为准";
            profile.EquipmentDetails.Add(desc);
        }

        internal static string BuildHeadgearDescription(string name, string id, string material, bool hidesHair, bool hidesBeard)
        {
            // HairCover/BeardCover control hidden hair meshes, not face visibility.
            // The native reference renderer still applies those flags through the unchanged equipment.
            return $"已佩戴头部装备：{name}（物品ID: {id}，{material}）。名称仅供识别，不能据名称另造款式；" +
                "头部入镜时按人物参考图转写实际盔壳、面部覆盖范围与可见护具，保留其轮廓和装饰位置。" +
                "遮发/遮须标记不等于面部全遮覆；全包覆头盔按参考图保持完整遮覆，开放式头盔保留实际露出的面部范围。" +
                (hidesHair ? "装备标记隐藏全部头发，不补画被隐藏的头发。" : "") +
                (hidesBeard ? "装备标记隐藏全部胡须，不补画被隐藏的胡须。" : "");
        }

        public static string ExtractCharacterPhysicalFeatures(CharacterObject character, Equipment equipment)
        {
            if (character == null) return string.Empty;
            var sb = new StringBuilder();

            bool isBandit = false;
            string id = (character.StringId ?? "").ToLowerInvariant();
            string name = (character.Name?.ToString() ?? "").ToLowerInvariant();
            if (id.Contains("looter") || id.Contains("bandit") || id.Contains("raider") || id.Contains("outlaw") ||
                name.Contains("劫匪") || name.Contains("强盗") || name.Contains("山贼") || name.Contains("海寇") || name.Contains("响马"))
            {
                isBandit = true;
            }

            if (isBandit)
            {
                sb.Append("体格精悍魁梧、目光凶悍桀骜的荒野壮年战士（约26-32岁），面带风霜之色，肌肉紧绷强健。");
                if (equipment != null)
                {
                    var headItem = equipment[EquipmentIndex.Head].Item;
                    if (headItem != null)
                    {
                        string hName = (headItem.Name?.ToString() ?? "").ToLowerInvariant();
                        string hId = (headItem.StringId ?? "").ToLowerInvariant();
                        if (hName.Contains("面罩") || hName.Contains("兜帽") || hId.Contains("mask") || hId.Contains("hood"))
                        {
                            sb.Append(" 头部装备的材质、形状与口鼻实际覆盖范围以本人参考图为准，不按兜帽或面罩名称推断遮面程度。");
                        }
                    }

                    var bodyItem = equipment[EquipmentIndex.Body].Item;
                    if (bodyItem != null && bodyItem.Tier <= 0 && bodyItem.ArmorComponent?.MaterialType == ArmorComponent.ArmorMaterialTypes.Cloth)
                    {
                        string bName = (bodyItem.Name?.ToString() ?? "").ToLowerInvariant();
                        string bId = (bodyItem.StringId ?? "").ToLowerInvariant();
                        if (bName.Contains("破烂") || bName.Contains("粗布短衫") || bId.Contains("rags") || bId.Contains("tattered"))
                        {
                            sb.Append(" 身着粗麻开襟无袖短衣，袒露出强健结实的胸膛与宽厚胸肌。");
                        }
                    }

                    var gloveItem = equipment[EquipmentIndex.Gloves].Item;
                    if (gloveItem != null && gloveItem.Tier <= 0)
                    {
                        string gName = (gloveItem.Name?.ToString() ?? "").ToLowerInvariant();
                        string gId = (gloveItem.StringId ?? "").ToLowerInvariant();
                        if (gName.Contains("裹手") || gId.Contains("bandage"))
                        {
                            sb.Append(" 双手与前臂紧紧缠绕着层层粗亚麻布条绷带。");
                        }
                    }
                }
            }
            else
            {
                sb.Append("正值壮年、身形挺拔结实的战士（约25-35岁），神态坚毅沉稳、英武健硕。");
            }

            return sb.ToString().Trim();
        }

        private static string ResolveItemMaterial(ItemObject item)
        {
            if (item == null) return "材质未标明";
            string name = (item.Name != null ? item.Name.ToString() : "").ToLowerInvariant();
            string id = (item.StringId ?? "").ToLowerInvariant();

            // Material metadata must not turn a name such as battle_crown into a shape claim.
            if (item.ArmorComponent == null) return "防护装备材质";

            switch (item.ArmorComponent.MaterialType)
            {
                case ArmorComponent.ArmorMaterialTypes.Cloth:
                    if (name.Contains("丝") || id.Contains("silk")) return "丝织物";
                    if (name.Contains("麻") || id.Contains("linen") || id.Contains("burlap")) return "麻布织物";
                    return "布料织物";
                case ArmorComponent.ArmorMaterialTypes.Leather:
                    return "皮革";
                case ArmorComponent.ArmorMaterialTypes.Chainmail:
                    return "金属锁子甲";
                case ArmorComponent.ArmorMaterialTypes.Plate:
                    return "金属甲胄";
                default:
                    return "复合防护材质";
            }
        }

        private static void ExtractWeapons(HeroVisualProfile profile, Equipment equipment, Hero hero)
        {
            bool hasShield = false;
            for (EquipmentIndex i = EquipmentIndex.Weapon0; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                EquipmentElement element = equipment[i];
                if (element.Item == null)
                {
                    continue;
                }

                string itemName = element.Item.Name != null ? element.Item.Name.ToString() : element.Item.StringId;
                string modifierStr = element.ItemModifier?.Name != null ? element.ItemModifier.Name.ToString() + "的" : "";

                if (element.Item.ItemType == ItemObject.ItemTypeEnum.Banner)
                {
                    profile.BannerEquipmentDetails.Add(modifierStr + itemName);
                    continue;
                }
                if (element.Item.ItemType == ItemObject.ItemTypeEnum.Shield)
                {
                    hasShield = true;
                    if (element.Item.IsUsingTableau)
                    {
                        profile.HasHeraldicShield = true;
                    }
                    if (profile.EquipmentSource?.Contains("百科") != true)
                    {
                        profile.WeaponDetails.Add($"盾牌: {modifierStr}{itemName}；仅为装备持有记录，不是展示要求；禁止背负或用作背景摆设；其他场景只有明确的实际持盾动作依据才可少量入镜，图案仅在确已入镜时参照本人纹章，不能为展示纹章增加盾牌或军旗");
                    }
                }
                else if (element.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                {
                    profile.WeaponDetails.Add($"远程武器: {modifierStr}{itemName} (挂在背后 slung across back)");
                }
                else if (element.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon || element.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon)
                {
                    profile.WeaponDetails.Add($"近战武器: {modifierStr}{itemName} (配在腰间剑鞘 sheathed at hip)");
                }
                else
                {
                    profile.WeaponDetails.Add($"武器: {modifierStr}{itemName}");
                }
            }

            if (!hasShield)
            {
                profile.WeaponDetails.Add("盾牌: 无盾牌 (NO shield held in hand or on back)");
            }
        }

        private static void ExtractMount(HeroVisualProfile profile, Equipment equipment)
        {
            EquipmentElement horse = equipment[EquipmentIndex.Horse];
            if (horse.Item != null)
            {
                string horseName = horse.Item.Name != null ? horse.Item.Name.ToString() : horse.Item.StringId;
                EquipmentElement harness = equipment[EquipmentIndex.HorseHarness];
                string harnessName = harness.Item != null ? ("配备" + (harness.Item.Name != null ? harness.Item.Name.ToString() : harness.Item.StringId)) : "无马铠";
                profile.MountDetail = $"{horseName} ({harnessName})";
            }
        }

        private static string ExtractPhysicalFeatures(Hero hero, Equipment equipment, CharacterAppearanceSnapshot appearance = null)
        {
            var headItem = equipment?[EquipmentIndex.Head].Item;
            if (headItem != null)
            {
                string hId = (headItem.StringId ?? "").ToLowerInvariant();
                string hName = (headItem.Name?.ToString() ?? "").ToLowerInvariant();
                if (hId.Contains("daimao") || hName.Contains("呆喵"))
                {
                    return "【头部全包覆猫咪头饰】：人物头部佩戴着标志性的蓝白色呆喵立体猫猫头饰（正脸为白色猫猫面具表情，后脑与猫耳为天蓝色），完全遮覆住整张面孔与头部，完整呈现该蓝白猫咪头饰本身的标志形态与色彩。";
                }
            }

            // 提取人物真实发色、发型、胡须及年龄特征
            string hairColorDesc = "深色发丝";
            string beardDesc = hero.IsFemale ? "面容干净无胡须" : "修剪整齐的短胡须";
            bool hairExtracted = false;

            try
            {
                BodyProperties bp = hero.BodyProperties;
                if (appearance != null && !string.IsNullOrWhiteSpace(appearance.BodyProperties))
                {
                    BodyProperties.FromString(appearance.BodyProperties, out bp);
                }

#if BANNERLORD_1_4_OR_GREATER
                var faceParams = FaceGenerationParams.Create();
                MBBodyProperties.GetParamsFromKey(ref faceParams, bp, false, false);
                float hairOffset = faceParams.CurrentHairColorOffset;
                int beardIndex = faceParams.CurrentBeard;
                hairExtracted = true;

                if (hairOffset < 0.22f)
                    hairColorDesc = "金黄色发丝（亚麻金发/亮金色微卷发丝）";
                else if (hairOffset < 0.40f)
                    hairColorDesc = "浅棕色/暖栗色发丝";
                else if (hairOffset < 0.58f)
                    hairColorDesc = "赤褐色/红棕色发丝";
                else if (hairOffset < 0.78f)
                    hairColorDesc = "深棕色/深褐色发丝";
                else
                    hairColorDesc = "乌黑/深黑色发丝";

                if (hero.IsFemale || beardIndex <= 0)
                {
                    beardDesc = "面部剃刮干净无胡须";
                }
                else
                {
                    string bColor = hairOffset < 0.22f ? "金黄色" : (hairOffset < 0.40f ? "浅棕色" : (hairOffset < 0.58f ? "红棕色" : (hairOffset < 0.78f ? "深褐色" : "黑色")));
                    beardDesc = $"修剪利落的{bColor}胡须（与发色一致的{bColor}短髭与八字胡/下巴胡，呈现纯正{bColor}光泽）";
                }
#endif
            }
            catch
            {
            }

            string ageTone;
            if (hero.Age >= 60)
                ageTone = $"约{(int)hero.Age}岁长者，两鬓与须发微霜斑白，眼神深邃坚毅";
            else if (hero.Age >= 40)
                ageTone = $"约{(int)hero.Age}岁成熟统帅，面庞沉稳威严、骨相深邃、正值鼎盛之年";
            else if (hero.Age >= 28)
                ageTone = $"约{(int)hero.Age}岁壮年将领/勇士，英姿挺拔、面庞棱角分明紧致、极富力量感";
            else
                ageTone = $"约{(int)hero.Age}岁青年骑士/贵族，身姿挺拔修长、肤色健康平滑";

            if (!hairExtracted)
            {
                return $"【面貌骨相与发色胡须】：{ageTone}；发色、胡须样式与五官骨相完全以人物参考图为最高依据，不凭身份或猜测补造发色胡须。";
            }

            if (headItem?.ArmorComponent?.HairCoverType == ArmorComponent.HairCoverTypes.All)
                hairColorDesc = "当前装备隐藏头发";
            if (headItem?.ArmorComponent?.BeardCoverType == ArmorComponent.BeardCoverTypes.All)
                beardDesc = "当前装备隐藏胡须";
            return $"【面貌骨相与发色胡须】：{ageTone}；发色为【{hairColorDesc}】；胡须为【{beardDesc}】；仅表现参考图中实际露出的五官与须发，不为展示面貌移除或打开头盔护具。";
        }

        private static string ExtractBackgroundLore(Hero hero)
        {
            var sb = new StringBuilder();

            // 1. 官方原版/MOD百科传记生平
            try
            {
                if (hero.EncyclopediaText != null)
                {
                    string bio = hero.EncyclopediaText.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(bio))
                    {
                        bio = System.Text.RegularExpressions.Regex.Replace(bio, "<.*?>", string.Empty).Trim();
                        if (bio.Length > 280) bio = bio.Substring(0, 280) + "...";
                        sb.Append($"【百科官方传记】：{bio}。");
                    }
                }
            }
            catch { }

            // 2. 家族谱系与至亲关系
            try
            {
                var relatives = new List<string>();
                if (hero.Spouse != null && hero.Spouse.Name != null)
                {
                    relatives.Add($"配偶: {hero.Spouse.Name}");
                }
                if (hero.Father != null && hero.Father.Name != null)
                {
                    relatives.Add($"父亲: {hero.Father.Name}");
                }
                if (hero.Mother != null && hero.Mother.Name != null)
                {
                    relatives.Add($"母亲: {hero.Mother.Name}");
                }
                if (hero.Children != null && hero.Children.Count > 0)
                {
                    relatives.Add($"育有{hero.Children.Count}名子女");
                }
                if (relatives.Count > 0)
                {
                    sb.Append($" 【至亲谱系】：{string.Join("，", relatives)}。");
                }
            }
            catch { }

            // 3. 统治封邑领地与军力
            try
            {
                if (hero.Clan != null && hero.Clan.Leader == hero)
                {
                    var settlements = hero.Clan.Settlements;
                    if (settlements != null && settlements.Count > 0)
                    {
                        var names = new List<string>();
                        for (int i = 0; i < Math.Min(3, settlements.Count); i++)
                        {
                            if (settlements[i]?.Name != null) names.Add(settlements[i].Name.ToString());
                        }
                        sb.Append($" 【领地封邑】：坐拥{settlements.Count}处领地 (包含: {string.Join("、", names)})。");
                    }
                }
                if (hero.GovernorOf != null && hero.GovernorOf.Name != null)
                {
                    sb.Append($" 【总督职守】：担任{hero.GovernorOf.Name}总督。");
                }
                if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero)
                {
                    sb.Append($" 【随行军力】：统率{hero.PartyBelongedTo.MemberRoster.TotalManCount}名武装军团。");
                }
            }
            catch { }

            return sb.ToString().Trim();
        }

        private static string ExtractBannerDescription(Hero hero)
        {
            if (hero.Clan == null) return string.Empty;

            string clanName = hero.Clan.Name != null ? hero.Clan.Name.ToString() : "家族";
            string composition = string.Empty;
            try
            {
                composition = DescribeBannerComposition(hero.Clan.Banner ?? hero.Clan.Kingdom?.Banner);
            }
            catch { }

            // Clan.Color/Color2 are faction/livery metadata, not the actual BannerData background.
            return $"{clanName} 的家族纹章标准图案规格（仅供参考）：" +
                (!string.IsNullOrEmpty(composition)
                    ? composition + "。颜色HEX来自实际BannerData图层。"
                    : "未取得有效纹章图层配色。") +
                "【纹章载体说明】：纹章仅在画面存在盾牌、罩袍或背景旗帜等合理载体时绘制，普通金属胸甲保持原装金属质感。";
        }

        /// <summary>
        /// 将 Banner.BannerDataList 结构化数据转成文字构图：每枚纹章的颜色/位置/大小/旋转/镜像与类别。
        /// 纹章图形本身是图集切片无法转文字，但构图与配色可被文字完整描述。
        /// </summary>
        private static string DescribeBannerComposition(Banner banner)
        {
            if (banner == null || banner.IsBannerDataListEmpty()) return string.Empty;
            var manager = BannerManager.Instance;
            if (manager == null) return string.Empty;

            float full = Math.Max(Banner.BannerFullSize, 1f);
            int count = banner.GetBannerDataListCount();
            var parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var data = banner.GetBannerDataAtIndex(i);
                if (data == null) continue;

                if (i == Banner.BackgroundDataIndex)
                {
                    var background = new StringBuilder("底层（实际BannerData，底纹布局按标准图）");
                    if (data.ColorId >= 0)
                    {
                        uint color = BannerManager.GetColor(data.ColorId);
                        background.Append($"主色#{(color & 0x00FFFFFF):X6}（{ResolveColorName(color)}）");
                    }
                    if (data.ColorId2 >= 0 && data.ColorId2 != data.ColorId)
                    {
                        uint color = BannerManager.GetColor(data.ColorId2);
                        background.Append($"，第二底色#{(color & 0x00FFFFFF):X6}（{ResolveColorName(color)}）");
                    }
                    parts.Add(background.ToString());
                    continue;
                }

                var sb = new StringBuilder($"图层{i}：纹章画布{DescribeBannerRegion(data.Position, full)}的{DescribeBannerScale(data.Size, full)}");
                if (data.ColorId >= 0)
                {
                    uint c = BannerManager.GetColor(data.ColorId);
                    sb.Append($"{ResolveColorName(c)} (#{(c & 0x00FFFFFF):X6}) ");
                }
                string group = ResolveIconGroupName(data.MeshId);
                if (!string.IsNullOrEmpty(group)) sb.Append(group).Append("类");
                sb.Append("纹章");
                if (data.ColorId2 >= 0 && data.ColorId2 != data.ColorId)
                {
                    uint c2 = BannerManager.GetColor(data.ColorId2);
                    sb.Append($"，辅以{ResolveColorName(c2)} (#{(c2 & 0x00FFFFFF):X6})双色");
                }
                if (data.DrawStroke) sb.Append("，带描边");
                float deg = data.Rotation * 57.29578f;
                if (Math.Abs(deg) > 5f) sb.Append($"，旋转{Math.Round(deg):0}°");
                if (data.Mirror) sb.Append("，水平镜像");
                parts.Add(sb.ToString());
            }
            return string.Join("；", parts.ToArray());
        }

        private static string ResolveIconGroupName(int meshId)
        {
            try
            {
                var groups = BannerManager.Instance?.BannerIconGroups;
                if (groups == null) return null;
                foreach (var g in groups)
                {
                    if (g?.AllIcons == null || g.IsPattern) continue;
                    if (g.AllIcons.ContainsKey(meshId)) return g.Name?.ToString();
                }
            }
            catch { }
            return null;
        }

        private static string DescribeBannerRegion(Vec2 pos, float full)
        {
            float nx = pos.X / full;
            float ny = pos.Y / full;
            string h = nx < 0.33f ? "左" : nx > 0.67f ? "右" : string.Empty;
            string v = ny < 0.33f ? "上" : ny > 0.67f ? "下" : string.Empty;
            string region = h + v;
            return string.IsNullOrEmpty(region) ? "中央" : region;
        }

        private static string DescribeBannerScale(Vec2 size, float full)
        {
            float ratio = Math.Max(size.X, size.Y) / full;
            if (ratio >= 0.45f) return "大型";
            if (ratio >= 0.22f) return "中型";
            return "小型";
        }

        public static string ResolveColorName(uint colorUint)
        {
            int r = (int)((colorUint >> 16) & 0xFF);
            int g = (int)((colorUint >> 8) & 0xFF);
            int b = (int)(colorUint & 0xFF);

            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;

            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            float h = 0f;
            if (delta > 0.0001f)
            {
                if (max == rf)
                {
                    h = 60f * (((gf - bf) / delta) % 6f);
                    if (h < 0) h += 360f;
                }
                else if (max == gf)
                {
                    h = 60f * (((bf - rf) / delta) + 2f);
                }
                else
                {
                    h = 60f * (((rf - gf) / delta) + 4f);
                }
            }

            float s = max > 0.0001f ? (delta / max) : 0f;
            float v = max;

            if (v < 0.16f) return "近黑色";
            if (s < 0.12f)
            {
                if (v > 0.9f) return "近白色";
                if (v > 0.6f) return "浅灰色";
                return "灰色";
            }
            if (s < 0.3f && v > 0.6f && h >= 20f && h < 70f) return "浅米色/灰米色";
            string tone = v < 0.45f ? "深" : s < 0.35f ? "灰调" : string.Empty;
            if (h >= 345f || h < 15f) return tone + "红色";
            if (h < 45f) return v < 0.65f ? "棕褐色" : tone + "橙色";
            if (h < 70f) return tone + "黄色";
            if (h < 165f) return tone + "绿色";
            if (h < 200f) return tone + "青色";
            if (h < 250f) return tone + "蓝色";
            if (h < 290f) return tone + "紫色";
            return tone + "品红色";
        }

    }
}
