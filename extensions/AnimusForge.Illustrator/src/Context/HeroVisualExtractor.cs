using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
        public List<string> EquipmentDetails { get; set; } = new List<string>();
        public List<string> WeaponDetails { get; set; } = new List<string>();
        public string MountDetail { get; set; } = string.Empty;

        public string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"【人物与至高地位】{HeroName}" + (!string.IsNullOrWhiteSpace(Title) ? $" · {Title}" : "") +
                $" ({Culture}文化, {Gender}, 约{Age}岁" + (!string.IsNullOrWhiteSpace(SocialStatus) ? $", 身份: {SocialStatus}" : "") + ")");

            if (!string.IsNullOrWhiteSpace(CultureLore))
            {
                sb.AppendLine($"【文化全貌与官方背景】{CultureLore}");
            }
            if (!string.IsNullOrWhiteSpace(FactionLore))
            {
                sb.AppendLine($"【势力与王国背景】{FactionLore}");
            }
            if (!string.IsNullOrWhiteSpace(BackgroundLore))
            {
                sb.AppendLine($"【人物生平纪事】{BackgroundLore}");
            }
            if (!string.IsNullOrWhiteSpace(TraitsSummary))
            {
                sb.AppendLine($"【性格特质与神态气场】{TraitsSummary}");
            }
            if (!string.IsNullOrWhiteSpace(TopSkillsSummary))
            {
                sb.AppendLine($"【人物顶尖专长】{TopSkillsSummary}");
            }
            if (!string.IsNullOrWhiteSpace(ClanName) || !string.IsNullOrWhiteSpace(BannerDescription))
            {
                string factionPart = !string.IsNullOrWhiteSpace(KingdomName) ? $", 所属王国: {KingdomName}" : "";
                sb.AppendLine($"【家族与纹章】" + (!string.IsNullOrWhiteSpace(ClanName) ? $"所属家族: {ClanName}{factionPart}" : "") +
                    (!string.IsNullOrWhiteSpace(BannerDescription) ? $" | {BannerDescription}" : $" (识别色: {PrimaryBannerColorHex}/{SecondaryBannerColorHex})"));
            }
            if (!string.IsNullOrWhiteSpace(PhysicalFeatures))
            {
                sb.AppendLine($"【面貌骨相与发型】{PhysicalFeatures}");
            }
            if (EquipmentDetails.Count > 0)
            {
                sb.AppendLine($"【真实穿戴装备与材质】" + string.Join("，", EquipmentDetails));
            }
            if (WeaponDetails.Count > 0)
            {
                sb.AppendLine($"【配戴武器与盾牌】" + string.Join("，", WeaponDetails));
            }
            if (!string.IsNullOrWhiteSpace(MountDetail))
            {
                sb.AppendLine($"【坐骑与马铠】{MountDetail}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class HeroVisualExtractor
    {
        public static HeroVisualProfile Extract(Hero hero, bool useCivilian = false)
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
                ClanName = hero.Clan?.Name != null ? hero.Clan.Name.ToString() : string.Empty,
                KingdomName = hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : string.Empty
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

            // 6. 提取纹章识别色与家族旗帜
            if (hero.Clan != null)
            {
                profile.PrimaryBannerColorHex = "#" + (hero.Clan.Color & 0x00FFFFFF).ToString("X6");
                profile.SecondaryBannerColorHex = "#" + (hero.Clan.Color2 & 0x00FFFFFF).ToString("X6");
                profile.BannerDescription = ExtractBannerDescription(hero);
            }

            // 7. 提取生理与面部特征
            profile.PhysicalFeatures = ExtractPhysicalFeatures(hero);

            // 8. 提取装备槽位 (真实反射装备，绝无粗麻学者硬编码)
            Equipment equipment = useCivilian ? hero.CivilianEquipment : hero.BattleEquipment;
            if (equipment != null)
            {
                ExtractArmorSlot(profile, equipment, EquipmentIndex.Head, "头部", hero);
                ExtractArmorSlot(profile, equipment, EquipmentIndex.Body, "身甲", hero);
                ExtractArmorSlot(profile, equipment, EquipmentIndex.Cape, "披风", hero);
                ExtractArmorSlot(profile, equipment, EquipmentIndex.Gloves, "手部", hero);
                ExtractArmorSlot(profile, equipment, EquipmentIndex.Leg, "腿部", hero);

                ExtractWeapons(profile, equipment, hero);
                ExtractMount(profile, equipment);
            }

            return profile;
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
                    profile.EquipmentDetails.Add("头部: 该装备栏为空，未佩戴任何头盔、冠冕或头饰；保持游戏人物原本发型自然裸露");
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

            if (slot == EquipmentIndex.Cape)
            {
                profile.EquipmentDetails.Add($"披风/肩部装备: {modifierStr}{itemName} ({materialStr}, 等阶Tier {tier})；颜色、纹样与佩戴方式以人物参考图为准");
                return;
            }

            string desc = $"{slotName}: {modifierStr}{itemName} ({materialStr}, 等阶Tier {tier})；具体颜色与纹样以人物参考图为准";
            profile.EquipmentDetails.Add(desc);
        }

        private static string ResolveItemMaterial(ItemObject item)
        {
            if (item == null || item.ArmorComponent == null) return "材质未在装备数据中标明";

            string name = item.Name != null ? item.Name.ToString() : "";
            switch (item.ArmorComponent.MaterialType)
            {
                case ArmorComponent.ArmorMaterialTypes.Cloth:
                    if (name.Contains("丝") || item.StringId.IndexOf("silk", StringComparison.OrdinalIgnoreCase) >= 0) return "丝织物";
                    if (name.Contains("麻") || item.StringId.IndexOf("linen", StringComparison.OrdinalIgnoreCase) >= 0 || item.StringId.IndexOf("burlap", StringComparison.OrdinalIgnoreCase) >= 0) return "麻布织物";
                    return "布料织物；具体纤维与纹样未标明";
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
            for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
            {
                EquipmentElement element = equipment[i];
                if (element.Item == null)
                {
                    continue;
                }

                string itemName = element.Item.Name != null ? element.Item.Name.ToString() : element.Item.StringId;
                string modifierStr = element.ItemModifier?.Name != null ? element.ItemModifier.Name.ToString() + "的" : "";

                if (element.Item.ItemType == ItemObject.ItemTypeEnum.Shield)
                {
                    hasShield = true;
                    profile.WeaponDetails.Add($"盾牌: {modifierStr}{itemName}；盾面颜色与图案以人物或纹章参考图为准，数据未显示时不要自行添加家族徽记");
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

        private static string ExtractPhysicalFeatures(Hero hero)
        {
            var traits = new List<string>();

            int hairIndex = 1;
            int beardIndex = 0;
            float hairColorOffset = 0.1f;
            float skinColorOffset = 0.5f;
            float ageSlider = hero.Age;
            bool isFemale = hero.IsFemale;

            try
            {
                FaceGenerationParams faceParams = FaceGenerationParams.Create();
                MBBodyProperties.GetParamsFromKey(ref faceParams, hero.BodyProperties, false, false);
                hairIndex = faceParams.CurrentHair;
                beardIndex = faceParams.CurrentBeard;
                hairColorOffset = faceParams.CurrentHairColorOffset;
                skinColorOffset = faceParams.CurrentSkinColorOffset;
                if (faceParams.CurrentAge > 1f)
                {
                    ageSlider = faceParams.CurrentAge;
                }
            }
            catch
            {
            }

            int age = (int)Math.Max(hero.Age, ageSlider);

            // 1. 真实精准提取发色与自然岁月痕迹
            string baseColorName = "深栗褐发";
            try
            {
                List<uint> gradient = MBBodyProperties.GetHairColorGradientPoints(hero.CharacterObject?.Race ?? 0, isFemale ? 1 : 0, age);
                if (gradient != null && gradient.Count > 0)
                {
                    int idx = (int)MathF.Round(MathF.Clamp(hairColorOffset, 0f, 1f) * (gradient.Count - 1));
                    uint c = gradient[MBMath.ClampInt(idx, 0, gradient.Count - 1)];
                    int r = (int)((c >> 16) & 0xFF);
                    int g = (int)((c >> 8) & 0xFF);
                    int b = (int)(c & 0xFF);

                    // 在Bannerlord原生渐变色库中：
                    // Index 0~3 为浅金/亚麻金/金发 (R>150, G>120)
                    // Index 4~7 为砂金/浅金棕/赤铜红发
                    // Index 8~12 为中深棕色
                    // Index 13~23 为深褐与乌黑
                    if ((r >= 150 && g >= 120) || hairColorOffset <= 0.20f)
                    {
                        baseColorName = "金发/明亮亚麻浅金发 (Lustrous golden-blonde / flaxen-blonde hair)";
                    }
                    else if ((r >= 130 && g >= 90) || hairColorOffset <= 0.35f)
                    {
                        baseColorName = "暖金棕发/砂金发 (Warm honey-brown / sandy-blonde hair)";
                    }
                    else if ((r >= 120 && r > g * 1.35f) || (hairColorOffset > 0.20f && hairColorOffset <= 0.40f && r > 140))
                    {
                        baseColorName = "赤铜火红发/红发 (Fiery copper-red / auburn hair)";
                    }
                    else if ((r >= 55 || g >= 45) || hairColorOffset <= 0.65f)
                    {
                        baseColorName = "深栗褐发/深棕发 (Deep chestnut / dark brunette hair)";
                    }
                    else
                    {
                        baseColorName = "浓密乌黑发 (Rich deep jet-black hair)";
                    }
                }
            }
            catch
            {
                if (hairColorOffset <= 0.20f) baseColorName = "金发/明亮亚麻浅金发 (Lustrous golden-blonde / flaxen-blonde hair)";
                else if (hairColorOffset <= 0.35f) baseColorName = "暖金棕发/砂金发 (Warm honey-brown / sandy-blonde hair)";
                else if (hairColorOffset <= 0.65f) baseColorName = "深栗褐发/深棕发 (Deep chestnut / dark brunette hair)";
                else baseColorName = "浓密乌黑发 (Rich deep jet-black hair)";
            }

            string hairColorWithAge;
            if (age >= 70)
            {
                hairColorWithAge = "花白银发 (Silver-white hair with wrinkles of great age)";
            }
            else if (age >= 55)
            {
                hairColorWithAge = $"{baseColorName}中夹杂斑白发丝 (Salt-and-pepper hair with visible silver streaks, but NOT entirely white)";
            }
            else if (age >= 35)
            {
                hairColorWithAge = $"{baseColorName}，仅两鬓微染极其轻微的成熟银丝，主体依然保持浓密饱满的原生发色，【极其关键：绝非满头全白或灰白】 (Predominantly rich {baseColorName}, with only faint graceful silver highlights at the temples showing maturity; STRICTLY NOT fully white or grey hair!)";
            }
            else
            {
                hairColorWithAge = baseColorName;
            }

            // 1.5 真实精准提取人类肤色（杜绝任何红蓝反色或怪异变色）
            string skinToneDesc = "肤色数据未能精确分类，以人物参考图为准";
            try
            {
                List<uint> skinGradient = MBBodyProperties.GetSkinColorGradientPoints(hero.CharacterObject?.Race ?? 0, isFemale ? 1 : 0, age);
                if (skinGradient != null && skinGradient.Count > 0)
                {
                    int sIdx = (int)MathF.Round(MathF.Clamp(skinColorOffset, 0f, 1f) * (skinGradient.Count - 1));
                    uint sc = skinGradient[MBMath.ClampInt(sIdx, 0, skinGradient.Count - 1)];
                    int sr = (int)((sc >> 16) & 0xFF);
                    int sg = (int)((sc >> 8) & 0xFF);
                    int sb = (int)(sc & 0xFF);
                    if (sr > 200 && sg > 160)
                    {
                        skinToneDesc = "偏明亮、带暖色血色的自然肤色";
                    }
                    else if (sr > 165 && sg > 125)
                    {
                        skinToneDesc = "温暖的浅小麦色或米色肤色";
                    }
                    else if (sr > 125)
                    {
                        skinToneDesc = "较深的暖棕褐色或古铜色肤色";
                    }
                    else
                    {
                        skinToneDesc = "深褐色肤色";
                    }
                }
            }
            catch
            {
                skinToneDesc = "肤色数据读取失败，以人物参考图为准";
            }

            traits.Add($"【肤色观测】{skinToneDesc}；以人物参考图为最高依据，不要把渲染通道色偏当成角色真实肤色");

            // 2. 发型与发长真实动态提取
            if (hairIndex == 0)
            {
                traits.Add("【头发】游戏面部数据未显示可见头发；不要自行添加长发或发髻");
            }
            else
            {
                traits.Add($"【头发】发色为{hairColorWithAge}；具体长度、发际线、编发与发髻只按人物参考图还原，不根据数字索引猜测");
            }
            if (!isFemale)
            {
                traits.Add(beardIndex == 0
                    ? "【面部毛发】游戏面部数据为无胡须；不要添加胡须或胡茬"
                    : $"【面部毛发】游戏面部数据确认存在胡须，颜色接近{baseColorName}；具体形制与浓密程度只按人物参考图还原");
            }

            // 3. 面貌骨相与身份气场（严格基于性别、年龄与社会地位）
            if (isFemale)
            {
                if (hero.IsFactionLeader)
                {
                    traits.Add($"【女皇/统治者雍容骨相】约{age}岁尊贵的帝国至高女皇/统治者，高贵优雅的成熟女性面容，端庄优美的下颌轮廓（绝非男性胡茬或粗糙棱角），眼神威严中带着深沉柔和的政治洞察力，眉宇间流露尊贵摄政者的神圣威仪与统治魄力，成熟雍容，气度非凡 (A magnificent imperial Empress of {age} years, noble and refined feminine bone structure, graceful elegant jawline, commanding yet deeply poised and intelligent eyes, radiant royal authority and majestic sovereign grace, a mature noblewoman monarch, NOT a frail elder and NOT a masculine face)");
                }
                else if (hero.IsLord)
                {
                    traits.Add($"【贵族贵妇典雅面容】约{age}岁端庄优雅的中世纪贵妇，面容高贵柔美，眼眸明亮沉静，气度优雅端庄 (An elegant noble lady of {age} years, dignified, poised and refined feminine features)");
                }
                else
                {
                    traits.Add($"【女性面貌】约{age}岁成熟坚毅的中世纪女性面容 (A mature, dignified woman of approximately {age} years)");
                }
            }
            else
            {
                bool isScholar = hero.IsWanderer && (hero.Name?.ToString().Contains("学者") == true || hero.EncyclopediaText?.ToString().Contains("学") == true);
                if (isScholar)
                {
                    traits.Add($"【骨相与面貌】约{age}岁清瘦思辨的成熟学者，额头有明显思索抬头纹，深陷的眼窝，目光锐利深邃，带有坚毅沧桑的知识分子气质 (A mature intellectual scholar of roughly {age} years, deep furrowed brow wrinkles, thoughtful deeply sunken eyes, lean ascetic scholarly face)");
                }
                else if (hero.IsFactionLeader || hero.IsLord)
                {
                    traits.Add($"【骨相与面貌】约{age}岁威严中世纪封建领主/君王，轮廓如雕刻般沉稳刚毅，眼神沉着具有统御气场 (A commanding feudal lord/monarch of roughly {age} years, strong sculpted jawline, authoritative gaze and sovereign presence)");
                }
                else
                {
                    traits.Add($"【骨相与面貌】约{age}岁坚毅英武的中世纪武士面容 (A seasoned warrior of approximately {age} years)");
                }
            }

            // 4. 性格神态
            try
            {
                TraitObject persona = hero.CharacterObject?.GetPersona();
                if (persona == DefaultTraits.PersonaCurt)
                {
                    traits.Add("神态：冷峻严谨、深思审视神色 (Stern, analytical and reserved expression)");
                }
                else if (persona == DefaultTraits.PersonaIronic)
                {
                    traits.Add("神态：从容微哂、目光透彻犀利 (Subtle wry, penetrating, confident expression)");
                }
                else if (persona == DefaultTraits.PersonaSoftspoken)
                {
                    traits.Add("神态：温和内敛、安静观察的从容神态 (Quiet, contemplative and gentle expression)");
                }
                else if (persona == DefaultTraits.PersonaEarnest)
                {
                    traits.Add("神态：庄重专注、正直坚毅的目光 (Dignified, earnest and focused expression)");
                }
            }
            catch
            {
            }

            // 5. 体格特征
            if (hero.CharacterObject != null)
            {
                float weight = hero.Weight;
                float build = hero.Build;

                if (build > 0.65f)
                {
                    traits.Add("体格魁梧强壮");
                }
                else if (build < 0.35f)
                {
                    traits.Add(isFemale ? "身形窈窕高挑" : "身形清瘦修长");
                }

                if (weight > 0.7f)
                {
                    traits.Add("身材宽厚");
                }
            }

            return string.Join("，", traits);
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
            string color1Hex = "#" + (hero.Clan.Color & 0x00FFFFFF).ToString("X6");
            string color2Hex = "#" + (hero.Clan.Color2 & 0x00FFFFFF).ToString("X6");
            string color1Name = ResolveColorName(hero.Clan.Color);
            string color2Name = ResolveColorName(hero.Clan.Color2);

            string composition = string.Empty;
            try
            {
                composition = DescribeBannerComposition(hero.Clan.Banner ?? hero.Clan.Kingdom?.Banner);
            }
            catch { }

            return $"{clanName} 家族旗帜识别色：主色【{color1Name} ({color1Hex})】，副色【{color2Name} ({color2Hex})】" +
                (!string.IsNullOrEmpty(composition)
                    ? $"；徽记构图（取自旗帜数据）：{composition}。徽记的具体形状以人物参考图中盾面/罩袍上的纹样为准，看不清时按此构图概括绘制，严禁换成其他图腾"
                    : "。具体徽记形状、层级与朝向只能依据人物参考图中的纹章；未取得参考图时不要猜测动物、兵器、王冠或其他图腾");
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
                if (i == Banner.BackgroundDataIndex) continue;
                var data = banner.GetBannerDataAtIndex(i);
                if (data == null) continue;

                var sb = new StringBuilder($"第{parts.Count + 1}枚：旗面{DescribeBannerRegion(data.Position, full)}的{DescribeBannerScale(data.Size, full)}");
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

            if (s < 0.12f)
            {
                if (v > 0.85f) return "纯白/象牙白 (Ivory white)";
                if (v > 0.55f) return "银灰/浅灰 (Silver grey)";
                if (v > 0.25f) return "暗石灰/深灰 (Dark stone grey)";
                return "漆黑 (Jet black)";
            }

            if (h >= 345 || h < 15) return "绯红/烈焰深红 (Crimson / scarlet red)";
            if (h >= 15 && h < 45)
            {
                if (s > 0.5f && v > 0.6f) return "辉煌灿金/琥珀金橙 (Radiant amber-gold / orange-gold)";
                return "暖赭色/深赤金 (Warm russet / copper-gold)";
            }
            if (h >= 45 && h < 70) return "辉煌灿金色/明黄 (Lustrous imperial gold / yellow)";
            if (h >= 70 && h < 165) return "翡翠森林深绿 (Forest emerald green)";
            if (h >= 165 && h < 200) return "天青/松石青绿 (Turquoise / azure green-blue)";
            if (h >= 200 && h < 250)
            {
                if (v < 0.4f) return "深邃深海藏青/海军蓝 (Deep midnight navy blue)";
                return "皇家蔚蓝/群青 (Royal cobalt / sapphire blue)";
            }
            if (h >= 250 && h < 290)
            {
                if (v > 0.6f && s < 0.55f) return "高贵淡雅的丁香紫/淡紫 (Elegant lilac / lavender purple)";
                return "高贵皇家紫/靛紫 (Royal imperial purple / indigo)";
            }
            return "紫红/品红 (Royal magenta / purple-red)";
        }

    }
}
