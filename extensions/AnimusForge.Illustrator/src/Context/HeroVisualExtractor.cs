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
            bool isNobleOrRuler = hero.IsFactionLeader || hero.IsLord;

            if (element.Item == null)
            {
                if (slot == EquipmentIndex.Head)
                {
                    if (hero.IsFactionLeader)
                    {
                        string headwear = ResolveRegalHeadwear(hero);
                        profile.EquipmentDetails.Add($"头部: 未戴战斗铁盔；面容裸露，额前佩戴{headwear}，长发梳理尊贵华丽 (Bareheaded without combat helmet; wearing culturally-accurate regal headwear as described, STRICTLY NO mismatched foreign crown)");
                    }
                    else if (isNobleOrRuler)
                    {
                        profile.EquipmentDetails.Add("头部: 未戴战盔；面容裸露，佩戴贵族典雅发饰或整齐梳理的发束 (Bareheaded without helmet; wearing noble accessories or neatly styled hair)");
                    }
                    else
                    {
                        profile.EquipmentDetails.Add("头部: 未戴头盔，面容与发型自然裸露 (Bareheaded, NO helmet)");
                    }
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
            int value = item.Value;

            string materialStr = ResolveItemMaterial(item, isNobleOrRuler);

            if (slot == EquipmentIndex.Cape)
            {
                string bannerColorDesc = "";
                if (hero.Clan != null)
                {
                    string color1Name = ResolveColorName(hero.Clan.Color);
                    bannerColorDesc = $" (主色调符合家族代表色: {color1Name} {profile.PrimaryBannerColorHex})";
                }
                if (isNobleOrRuler)
                {
                    profile.EquipmentDetails.Add($"披风/斗篷/颈间围巾: {modifierStr}{itemName} ({materialStr}{bannerColorDesc}, 贵族/将领华贵斗篷披风或彰显宗族身份的丝绸围巾，以精美金属搭扣佩戴于肩，Noble/royal mantle/cloak/scarf draped gracefully)");
                }
                else
                {
                    profile.EquipmentDetails.Add($"披风/肩部: {modifierStr}{itemName} ({materialStr}{bannerColorDesc})");
                }
                return;
            }

            string desc = $"{slotName}: {modifierStr}{itemName} ({materialStr}, 等阶Tier {tier})";

            if (slot == EquipmentIndex.Body)
            {
                if (hero.Clan != null)
                {
                    string primaryColorName = ResolveColorName(hero.Clan.Color);
                    string secondaryColorName = ResolveColorName(hero.Clan.Color2);
                    desc += $" 【极其关键服装配色与纹样】：衣物/战甲主体布料底色为【{primaryColorName}】，领口、袖口、胸前与肩部边缘的刺绣装饰与滚边均为耀眼的【{secondaryColorName}】纹样；【极其关键颜色铁律：黄色/金色刺绣绝对严禁画成蓝色！严禁颠倒服饰颜色！】";
                }

                if (isNobleOrRuler)
                {
                    desc += " [贵族/王室华贵服饰，绝非粗麻破布，STRICTLY NO ragged peasant cloth, NO beggar burlap!]";
                }
                else if (item.ArmorComponent != null && item.ArmorComponent.MaterialType == ArmorComponent.ArmorMaterialTypes.Cloth && (tier <= 1 || value < 200))
                {
                    desc += " [朴素平民衣着 (Simple rustic civilian attire)]";
                }
            }

            profile.EquipmentDetails.Add(desc);
        }

        private static string ResolveRegalHeadwear(Hero hero)
        {
            string cultureCode = hero?.Culture?.StringId?.ToLowerInvariant() ?? "";
            string cultureName = hero?.Culture?.Name != null ? hero.Culture.Name.ToString() : "";
            bool isFemale = hero?.IsFemale == true;

            if (cultureCode.Contains("aserai") || cultureName.Contains("阿塞莱"))
            {
                return "阿塞莱苏丹式样的金丝刺绣华贵缠头巾/头巾王冠（正中缀以宝石与金链坠饰），【严格符合沙漠文化形制，严禁西式尖顶金冠】 (A majestic gold-embroidered jeweled turban crown of the Sultanate, STRICTLY NO European-style pointed crown)";
            }
            if (cultureCode.Contains("khuzait") || cultureName.Contains("库赛特"))
            {
                return "库赛特可汗式样的貂皮滚边金饰尖顶汗冠/雄鹰羽冠，【严格符合草原游牧形制，严禁西式王冠】 (A sable-trimmed pointed steppe Khan crown with eagle feathers, STRICTLY NO European-style crown)";
            }
            if (cultureCode.Contains("sturgia") || cultureName.Contains("斯特吉亚"))
            {
                return "斯特吉亚北境王者式样的厚重青铜/暗金环形战冠（饰有渡鸦或狼首浮雕），【严格符合北境诺斯形制，严禁西式王冠】 (A heavy Nordic bronze/dark-gold ringed war crown with raven or wolf motifs, STRICTLY NO European-style crown)";
            }
            if (cultureCode.Contains("battania") || cultureName.Contains("巴旦尼亚"))
            {
                return "巴旦尼亚至高王式样的凯尔特青铜环形王冠（饰有绳结与兽首图腾），【严格符合高地凯尔特形制，严禁西式王冠】 (A Celtic bronze torc-style circlet crown with knotwork and beast motifs, STRICTLY NO European-style crown)";
            }
            if (cultureCode.Contains("empire") || cultureName.Contains("帝国"))
            {
                return isFemale
                    ? "帝国女皇式样的紫坠黄金月桂冠冕（饰有宝石垂坠），【严格符合拜占庭形制】 (An imperial Byzantine golden laurel diadem with amethyst pendants)"
                    : "帝国皇帝式样的紫坠黄金月桂冠冕（古典拜占庭式，饰有宝石垂坠），【严格符合拜占庭形制，严禁哥特式尖顶王冠】 (An imperial Byzantine golden laurel wreath diadem with gem pendants, STRICTLY NO gothic pointed crown)";
            }
            return "象征最高统治者尊贵权柄的华美皇冠/金冠/冠冕，形制严格符合所属文化的高贵头饰 (A magnificent royal crown/diadem strictly matching their own culture's regal tradition)";
        }

        private static string ResolveItemMaterial(ItemObject item, bool isNobleOrRuler)
        {
            if (item == null || item.ArmorComponent == null) return "中世纪织物";

            int tier = (int)item.Tier;
            int value = item.Value;
            string name = item.Name != null ? item.Name.ToString() : "";

            switch (item.ArmorComponent.MaterialType)
            {
                case ArmorComponent.ArmorMaterialTypes.Cloth:
                    if (isNobleOrRuler || tier >= 4 || value >= 2000)
                    {
                        return "奢华丝绸、天鹅绒与金银线精细刺绣面料 (Luxurious tailored noble silk, velvet, or fine brocade with intricate embroidery)";
                    }
                    if (name.Contains("粗") || name.Contains("麻") || item.StringId.IndexOf("ragged", StringComparison.OrdinalIgnoreCase) >= 0 || item.StringId.IndexOf("burlap", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "粗麻布/粗棉土布 (Coarse homespun linen/burlap)";
                    }
                    return "细纺布料/平民羊毛呢长袍 (Fine cloth/wool)";

                case ArmorComponent.ArmorMaterialTypes.Leather:
                    return "熟牛皮/鞣制硬皮护甲 (Hardened boiled leather armor)";

                case ArmorComponent.ArmorMaterialTypes.Chainmail:
                    return "精制细环铆接金属锁子甲 (Fine riveted metal chainmail)";

                case ArmorComponent.ArmorMaterialTypes.Plate:
                    return "精工锻造重型金属甲胄 (Forged heavy metal armor: plate, heavy scale, or lamellar)";

                default:
                    return "复合防护甲胄";
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
                    string shieldDesc = $"盾牌: {modifierStr}{itemName}";
                    if (hero.Clan != null)
                    {
                        string primaryColorName = ResolveColorName(hero.Clan.Color);
                        string secondaryColorName = ResolveColorName(hero.Clan.Color2);
                        shieldDesc += $" (漆绘{hero.Clan.Name}家族专属纹章，盾面底色为【{primaryColorName} ({profile.PrimaryBannerColorHex})】，中央徽记图腾为耀眼的【{secondaryColorName} ({profile.SecondaryBannerColorHex})】；极其关键：严禁颠倒盾牌与图腾颜色！绝对严禁将金色徽记画成蓝色！)";
                    }
                    profile.WeaponDetails.Add(shieldDesc);
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
            string skinToneDesc = "健康自然的温润浅白人类肤色 (Healthy natural fair human skin tone)";
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
                        skinToneDesc = "白皙透红的自然欧洲/高加索人类肤色，面颊带着自然血色 (Fair Caucasian skin with healthy warm rosy cheeks)";
                    }
                    else if (sr > 165 && sg > 125)
                    {
                        skinToneDesc = "健康温暖的地中海浅小麦色/米色人类肤色 (Warm Mediterranean olive / light beige human skin tone)";
                    }
                    else if (sr > 125)
                    {
                        skinToneDesc = "阳光晒染的健康棕褐色/古铜色人类肤色 (Warm sun-tanned bronze human skin tone)";
                    }
                    else
                    {
                        skinToneDesc = "深邃健康的深褐色/棕黑人类肤色 (Deep rich dark human skin tone)";
                    }
                }
            }
            catch
            {
                skinToneDesc = "健康自然的浅白/浅小麦色人类肤色 (Healthy natural warm human skin tone)";
            }

            traits.Add($"【真实人类肤色约束】{skinToneDesc}。【极其关键最高艺术准则：必须是完全正常且富有血色的真实人类皮肤，绝对严禁画成蓝色、青色、灰色、紫色等异类怪异皮肤！严禁阿凡达式蓝皮！】(Real natural human skin tone, healthy organic human flesh with warm blood circulation; ABSOLUTELY FORBIDDEN to depict blue, cyan, green, grey, or purple skin! Real human flesh only!)");

            // 2. 发型与发长真实动态提取
            if (isFemale)
            {
                if (hairIndex == 0)
                {
                    traits.Add($"【女性发型与发色】发型为干练超短发，发色为{hairColorWithAge}");
                }
                else if (hairIndex >= 8 && hairIndex <= 15)
                {
                    traits.Add($"【女性发型与发色】柔顺丰盈的波浪长发自然垂落过肩，发色为{hairColorWithAge} (Luxurious long wavy hair cascading gracefully down past shoulders and back)");
                }
                else
                {
                    traits.Add($"【女性发型与发色】梳理为端庄典雅的中世纪贵妇/女皇发髻，发色为{hairColorWithAge} (Elegant medieval noblewoman/empress hairstyle, hair gracefully gathered or braided under her crown or diadem)");
                }
            }
            else
            {
                if (hairIndex == 0)
                {
                    traits.Add("【发型极其关键】完全光头/剃光头发，额头与头顶光滑裸露 (Completely bald / shaved head, bald exposed scalp, absolutely NO hair on head)");
                }
                else if (hairIndex == 4 || hairIndex == 5)
                {
                    traits.Add($"【发型极其关键】发际线严重后退、额前高耸微秃、顶部稀疏地中海发型，发色为{hairColorWithAge} (Prominent high receding forehead, balding thinning crown with sparse hair at temples)");
                }
                else if (hairIndex == 2 || hairIndex == 10 || hairIndex == 17 || hairIndex == 20 || hairIndex >= 23)
                {
                    // 齐肩浓密波浪卷曲长发（如斯特吉亚朗瓦德、狂战士等长发）
                    traits.Add($"【发型极其关键】齐肩蓬松波浪长发，富有体积感的{hairColorWithAge}长发自然中分并向两侧垂落，发梢垂至双肩与耳畔，充满北境战神与维京部族首领的狂野豪迈长发，【绝非干练贴头皮短发】 (Thick, voluminous, wavy shoulder-length {hairColorWithAge} hair falling naturally past ears down to his shoulders, rugged wild Nordic warrior mane, STRICTLY NOT neat short hair!)");
                }
                else if (hairIndex == 6 || hairIndex == 7 || hairIndex == 14 || hairIndex == 15)
                {
                    traits.Add($"【发型极其关键】顶部高高束起的战将武士发髻或马尾束发，两侧与脑后铲短剃平，发色为{hairColorWithAge} (Warrior high topknot / ponytail tied at the crown, with shaved undercut temples)");
                }
                else if (hairIndex == 11 || hairIndex == 12 || hairIndex == 13)
                {
                    traits.Add($"【发型极其关键】编结有细小战团发辫的部族编发，发色为{hairColorWithAge} (Braided warrior locks and small war plaits flowing down the neck)");
                }
                else if (hairIndex == 3 || hairIndex == 16)
                {
                    traits.Add($"【发型极其关键】中分自然向后梳理的层次中长发，发色为{hairColorWithAge} (Medium-length flowing hair swept back naturally, falling just above shoulders)");
                }
                else
                {
                    traits.Add($"【发型极其关键】干练利落的中世纪短发，发色为{hairColorWithAge} (Neatly cropped short hair)");
                }

                // 男性面部毛发与胡须真实动态提取
                if (beardIndex == 0)
                {
                    traits.Add("【面部毛发约束】面容剃得极干净，绝对无任何胡须与胡茬，皮肤平整光洁 (Clean-shaven, smooth skin, absolutely NO beard, NO mustache, NO stubble)");
                }
                else if (beardIndex == 3)
                {
                    traits.Add("【面部胡须】下颌带有青灰色的刚硬浓重胡茬，无长胡须 (Heavy rugged 5 o'clock stubble across jaw, but NO long beard)");
                }
                else if (beardIndex == 6 || beardIndex == 7 || beardIndex == 8)
                {
                    traits.Add("【面部胡须】唇上蓄有修剪考究的八字胡髭，下巴与脸颊剃净 (Distinguished classic mustache, clean-shaven cheeks and chin)");
                }
                else if (beardIndex == 1 || beardIndex == 5)
                {
                    traits.Add("【面部胡须】修剪精细的山羊短胡与唇髭 (Trimmed goatee and neat mustache)");
                }
                else if (beardIndex == 14 || beardIndex == 13 || beardIndex == 9 || beardIndex == 10 || beardIndex == 15 || beardIndex >= 28)
                {
                    // 极其浓密卷曲的维京大胡子（朗瓦德正是 index 14）
                    traits.Add($"【面部胡须极其关键】极其浓密蓬松的厚重大胡子、粗犷卷曲的金色维京大腮胡与浓密唇髭，与发色一致的{baseColorName}大胡须饱满浓密地包裹整片下巴、下颌骨与两腮脸颊，犹如雄狮鬃毛般粗犷威严，【极其关键：绝非细细修剪的薄胡子，必须是浓密茂盛的大胡子】 (Magnificent, very thick, bushy, curly full Nordic beard and heavy mustache matching his hair color, dense wild golden-blonde beard completely covering his jaw, chin, and lower cheeks like a lion's mane, rugged barbarian monarch beard, STRICTLY NOT a thin or neatly trimmed beard!)");
                }
                else
                {
                    traits.Add($"【面部胡须】成熟浓密的中世纪全腮胡与唇髭，与发色一致为{baseColorName} (Mature full beard and mustache matching his hair color)");
                }
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

            // 3.5 所属文化人种骨相与民族风貌特征
            string cultureCode = hero.Culture?.StringId?.ToLowerInvariant() ?? "";
            string cultureName = hero.Culture?.Name != null ? hero.Culture.Name.ToString() : "";
            if (cultureCode.Contains("sturgia") || cultureName.Contains("斯特吉亚"))
            {
                traits.Add("【北境斯拉夫/诺斯王者相貌】坚毅冷峻的北境王者骨相，鼻梁宽直高挺，眼窝深邃，目光凛冽如寒冬冰霜，皮肤白皙中带着饱经风霜的红润，轮廓刚强粗犷 (Commanding Slavic/Nordic monarch facial structure, strong broad straight nose, deep-set intense frost-blue or steel-grey eyes, weathered fair complexion with ruddy Nordic cheeks, rugged sculpted jawline, fierce stoic Scandinavian majesty)");
            }
            else if (cultureCode.Contains("aserai") || cultureName.Contains("阿塞莱"))
            {
                traits.Add("【沙漠绿洲贵胄相貌】深邃明亮如鹰隼般的深褐色眼眸，高挺微曲的鹰钩鼻，深邃内敛的轮廓，健康温暖的浅棕小麦色肌肤 (Distinguished Arabian/Moorish noble features, deep piercing amber-brown eyes, prominent aquiline nose, warm olive-bronze complexion)");
            }
            else if (cultureCode.Contains("khuzait") || cultureName.Contains("库赛特"))
            {
                traits.Add("【草原游牧王者相貌】高颧骨，深邃细长的坚毅丹凤眼，目光如草原苍鹰般锐利，饱经烈日风沙洗礼的坚韧铜色肌肤 (Distinguished Steppe Nomad / Mongolian warrior features, high prominent cheekbones, sharp intense almond eyes, weather-beaten bronze skin)");
            }
            else if (cultureCode.Contains("battania") || cultureName.Contains("巴旦尼亚"))
            {
                traits.Add("【凯尔特森林高地部族相貌】坚毅粗犷的高地人骨相，刚硬的下巴，目光如林间猎豹般桀骜不驯，带有浓郁的凯尔特原始野性 (Rugged Highland Celtic facial structure, strong stubborn jaw, fierce wild untamed gaze)");
            }
            else if (cultureCode.Contains("empire") || cultureName.Contains("帝国"))
            {
                traits.Add("【古典罗马拜占庭高贵相貌】高挺笔直的古典罗马鼻，深邃明锐的双眸，端庄对称的贵族骨相，流露出沉着精明的地中海君阀威仪 (Classical Greco-Roman aristocratic facial structure, straight noble Roman nose, refined symmetrical jawline, Mediterranean poise and commanding presence)");
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

            var sb = new StringBuilder();
            sb.Append($"{clanName} 家族旗帜与识别色: 主底色为【{color1Name} ({color1Hex})】，图腾徽记色彩为耀眼的【{color2Name} ({color2Hex})】");

            // 动态解析 Banner 图腾与徽记细节
            try
            {
                var banner = hero.Clan.Banner;
                if (banner != null && banner.BannerDataList != null && banner.BannerDataList.Count > 1)
                {
                    var emblems = new List<string>();
                    for (int i = 1; i < banner.BannerDataList.Count; i++)
                    {
                        var data = banner.BannerDataList[i];
                        int meshId = data.MeshId;
                        string emblemName = ResolveBannerIconName(meshId);
                        if (!string.IsNullOrWhiteSpace(emblemName) && !emblems.Contains(emblemName))
                        {
                            emblems.Add(emblemName);
                        }
                    }

                    if (emblems.Count > 0)
                    {
                        sb.Append($"，中央绘有【{string.Join("与", emblems)}】图腾徽记 (Heraldic emblem: {string.Join(", ", emblems)})");
                    }
                }
            }
            catch { }

            sb.Append("。其持握盾牌、随行战旗与精工披风搭扣上均鲜明漆绘此专属纹章。");
            return sb.ToString();
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

        private static string ResolveBannerIconName(int meshId)
        {
            try
            {
                if (BannerManager.Instance != null)
                {
                    foreach (var group in BannerManager.Instance.BannerIconGroups)
                    {
                        if (!group.IsPattern && group.AllIcons.ContainsKey(meshId))
                        {
                            string groupName = group.Name != null ? group.Name.ToString() : "纹章";
                            return groupName;
                        }
                    }
                }
            }
            catch { }

            if (meshId >= 100 && meshId < 200) return "猛兽/飞禽图腾 (Heraldic beast/bird: eagle, falcon, or wolf)";
            if (meshId >= 200 && meshId < 300) return "草木花卉纹样 (Heraldic flora: rose, tree, or fleur-de-lis)";
            if (meshId >= 300 && meshId < 400) return "兵戈器物/王权图腾 (Heraldic weapon/symbol: crown, sword, axe, or fortress)";
            if (meshId >= 400 && meshId < 500) return "日月星辰天象印记 (Heraldic sign: celestial sun, moon, star, or cross)";
            return "经典贵族家族图腾徽记 (Heraldic clan coat of arms)";
        }
    }
}
