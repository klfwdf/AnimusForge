using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

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
        public string AgeSource { get; set; } = string.Empty;
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
            sb.AppendLine(IllustratorText.Get("Hero_286") + HeroName);
            if (!string.IsNullOrWhiteSpace(CultureLore)) sb.AppendLine(IllustratorText.Get("Hero_287") + CultureLore);
            if (!string.IsNullOrWhiteSpace(FactionLore)) sb.AppendLine(IllustratorText.Get("Hero_288") + FactionLore);
            if (!string.IsNullOrWhiteSpace(BackgroundLore)) sb.AppendLine(IllustratorText.Get("Hero_289") + BackgroundLore);
            if (!string.IsNullOrWhiteSpace(TraitsSummary)) sb.AppendLine(IllustratorText.Get("Hero_290") + TraitsSummary);
            if (!string.IsNullOrWhiteSpace(TopSkillsSummary)) sb.AppendLine(IllustratorText.Get("Hero_291") + TopSkillsSummary);
            return sb.ToString().TrimEnd();
        }

        public string BuildConversationDirectorContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine(IllustratorText.Get("Hero_292") + HeroName);
            AppendBoundedNarrative(sb, IllustratorText.Get("Hero_293"), BackgroundLore, 360);
            AppendBoundedNarrative(sb, IllustratorText.Get("Hero_294"), TraitsSummary, 180);
            AppendBoundedNarrative(sb, IllustratorText.Get("Hero_295"), TopSkillsSummary, 180);
            return sb.ToString().TrimEnd();
        }

        private static void AppendBoundedNarrative(StringBuilder sb, string label, string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            value = value.Trim();
            if (value.Length > maxChars)
            {
                int boundary = -1;
                for (int i = maxChars - 1; i >= maxChars * 2 / 3; i--)
                    if (value[i] == '。' || value[i] == '！' || value[i] == '？' || value[i] == '；') { boundary = i + 1; break; }
                value = value.Substring(0, boundary > 0 ? boundary : maxChars).TrimEnd() + "…";
            }
            sb.AppendLine(label + value);
        }

        public string BuildVisualSummary(bool includeMount = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine(IllustratorText.Format("Hero_296", HeroName) + (!string.IsNullOrWhiteSpace(Title) ? $" · {Title}" : "") +
                IllustratorText.Format("Hero_297", Culture, Gender) + (Age > 0 ? IllustratorText.Format("Hero_298", Age) : IllustratorText.Get("Hero_299")) + (!string.IsNullOrWhiteSpace(SocialStatus) ? IllustratorText.Format("Hero_300", SocialStatus) : "") + ")");
            if (!string.IsNullOrWhiteSpace(SpeciesDescription)) sb.AppendLine(IllustratorText.Get("Hero_301") + SpeciesDescription);
            if (!string.IsNullOrWhiteSpace(AgeSource)) sb.AppendLine(IllustratorText.Get("Hero_302") + AgeSource);
            if (!string.IsNullOrWhiteSpace(ClanName) || !string.IsNullOrWhiteSpace(BannerDescription))
            {
                string factionPart = !string.IsNullOrWhiteSpace(KingdomName) ? IllustratorText.Format("Hero_303", KingdomName) : "";
                sb.AppendLine(IllustratorText.Get("Hero_304") + (!string.IsNullOrWhiteSpace(ClanName) ? IllustratorText.Format("Hero_305", ClanName, factionPart) : "") +
                    (!string.IsNullOrWhiteSpace(BannerDescription) ? $" | {BannerDescription}" : IllustratorText.Format("Hero_306", PrimaryBannerColorHex, SecondaryBannerColorHex)));
            }
            if (!string.IsNullOrWhiteSpace(LiveryColorsSummary))
            {
                sb.AppendLine(IllustratorText.Get("Hero_307") + LiveryColorsSummary);
            }
            if (!string.IsNullOrWhiteSpace(CurrentStateDetail))
            {
                sb.AppendLine(IllustratorText.Format("Hero_308", CurrentStateDetail));
            }
            if (!string.IsNullOrWhiteSpace(PhysicalFeatures))
            {
                sb.AppendLine(IllustratorText.Format("Hero_309", PhysicalFeatures));
            }
            if (!string.IsNullOrWhiteSpace(EquipmentSource)) sb.AppendLine(IllustratorText.Get("Hero_310") + EquipmentSource);
            if (!string.IsNullOrWhiteSpace(HeadgearDetail)) sb.AppendLine(IllustratorText.Get("Hero_311") + HeadgearDetail);
            if (EquipmentDetails.Count > 0)
            {
                sb.AppendLine(IllustratorText.Get("Hero_312") + string.Join("，", EquipmentDetails));
            }
            if (WeaponDetails.Count > 0)
            {
                sb.AppendLine(IllustratorText.Get("Hero_313") + string.Join("，", WeaponDetails));
            }
            if (BannerEquipmentDetails.Count > 0)
            {
                sb.AppendLine(IllustratorText.Get("Hero_314") + string.Join("，", BannerEquipmentDetails) +
                    IllustratorText.Get("Hero_315"));
            }
            if (includeMount && !string.IsNullOrWhiteSpace(MountDetail))
            {
                sb.AppendLine(IllustratorText.Format("Hero_316", MountDetail));
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
                return new HeroVisualProfile { HeroName = IllustratorText.Get("Hero_317") };
            }

            string cultureName = hero.Culture?.Name != null ? hero.Culture.Name.ToString() : IllustratorText.Get("Hero_318");
            string cultureId = hero.Culture?.StringId?.ToLowerInvariant() ?? "";

            string socialStatus;
            string title = ExtractTitleAndStatus(hero, out socialStatus);

            var profile = new HeroVisualProfile
            {
                HeroName = hero.Name != null ? hero.Name.ToString() : IllustratorText.Get("Hero_319"),
                Title = title,
                Culture = cultureName,
                CultureId = cultureId,
                SocialStatus = socialStatus,
                Gender = hero.IsFemale ? IllustratorText.Get("Hero_320") : IllustratorText.Get("Hero_321"),
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
                    c2Part = IllustratorText.Format("Hero_322", ResolveColorName(liveryColor2), (liveryColor2 & 0x00FFFFFF));
                }
                profile.LiveryColorsSummary = IllustratorText.Format("Hero_323", c1Name, c1Hex, c2Part);
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
            ApplyEquipmentSnapshot(profile, equipment, equipmentSource ?? (useCivilian ? IllustratorText.Get("Hero_324") : IllustratorText.Get("Hero_325")));

            // 8. 提取生理与面部特征 (根据头部全遮蔽状态智能调整)
            profile.PhysicalFeatures = ExtractPhysicalFeatures(hero, equipment);

            TaleWorlds.Library.Debug.Print($"[IllustratorFidelity] hero={hero.StringId}, equipmentSource={profile.EquipmentSource}, head={equipment?[EquipmentIndex.Head].Item?.StringId ?? "empty"}, shield={profile.WeaponDetails.Exists(x => x.StartsWith(IllustratorText.Get("Hero_ShieldPrefix")) && x != IllustratorText.Get("Hero_449"))}");
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
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Head, IllustratorText.Get("Hero_326"), null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Body, IllustratorText.Get("Hero_327"), null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Cape, IllustratorText.Get("Hero_328"), null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Gloves, IllustratorText.Get("Hero_329"), null);
            ExtractArmorSlot(profile, equipment, EquipmentIndex.Leg, IllustratorText.Get("Hero_330"), null);
            ExtractWeapons(profile, equipment, null);
            ExtractMount(profile, equipment);
        }

        private static string ResolveSpeciesDescription(Hero hero)
        {
            if (hero?.CharacterObject == null) return string.Empty;
            string identity = (hero.CharacterObject.StringId ?? string.Empty) + " " + (hero.Culture?.StringId ?? string.Empty) + " " + (hero.Culture?.Name?.ToString() ?? string.Empty);
            return DescribeSpecies(identity, hero.CharacterObject.Race);
        }

        private static readonly string[][] KnownSpeciesTokens =
        {
            // More specific labels must precede their substrings (野兽人 contains 兽人).
            new[] { IllustratorText.Raw("Hero_331"), "beastman", "beastmen", "minotaur" },
            new[] { IllustratorText.Raw("Hero_332"), "orc", "orcs", "greenskin", "warhammer_orc" },
            new[] { IllustratorText.Raw("Hero_333"), "goblin", "goblins", "snotling" },
            new[] { IllustratorText.Raw("Hero_334"), "elf", "elves", "elven", "asrai", "druchii" },
            new[] { IllustratorText.Raw("Hero_335"), "dwarf", "dwarves", "dawi" },
            new[] { IllustratorText.Raw("Hero_336"), "skaven" },
            new[] { IllustratorText.Raw("Hero_337"), "chaos", "daemon", "demon" },
            new[] { IllustratorText.Raw("Hero_338"), "troll", "ogre" }
        };

        private static readonly string[] KnownSpeciesTextKeys = { "Hero_331", "Hero_332", "Hero_333", "Hero_334", "Hero_335", "Hero_336", "Hero_337", "Hero_338" };

        internal static string DescribeSpecies(string identity, int race)
        {
            identity = (identity ?? string.Empty).ToLowerInvariant();
            for (int speciesIndex = 0; speciesIndex < KnownSpeciesTokens.Length; speciesIndex++)
                foreach (string token in KnownSpeciesTokens[speciesIndex])
                    if (token[0] > 127 ? identity.Contains(token) : ContainsIdentityToken(identity, token))
                        return IllustratorText.Get(KnownSpeciesTextKeys[speciesIndex]);
            return IllustratorText.Get("Hero_339") + race + IllustratorText.Get("Hero_340");
        }

        private static bool ContainsIdentityToken(string identity, string token)
        {
            int start = 0;
            while ((start = identity.IndexOf(token, start, StringComparison.Ordinal)) >= 0)
            {
                int end = start + token.Length;
                if ((start == 0 || !char.IsLetterOrDigit(identity[start - 1])) &&
                    (end == identity.Length || !char.IsLetterOrDigit(identity[end]))) return true;
                start = end;
            }
            return false;
        }

        private static string ExtractCultureLore(Hero hero)
        {
            if (hero.Culture == null) return string.Empty;
            var sb = new StringBuilder();
            string cultureName = hero.Culture.Name != null ? hero.Culture.Name.ToString() : hero.Culture.StringId;
            sb.Append(IllustratorText.Format("Hero_341", cultureName));

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
                        sb.Append(IllustratorText.Format("Hero_342", lore));
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
                string kingdomName = hero.Clan.Kingdom.Name != null ? hero.Clan.Kingdom.Name.ToString() : IllustratorText.Get("Hero_343");
                sb.Append(IllustratorText.Format("Hero_344", kingdomName));

                try
                {
                    if (hero.Clan.Kingdom.EncyclopediaText != null)
                    {
                        string kLore = hero.Clan.Kingdom.EncyclopediaText.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(kLore))
                        {
                            kLore = System.Text.RegularExpressions.Regex.Replace(kLore, "<.*?>", string.Empty).Trim();
                            if (kLore.Length > 200) kLore = kLore.Substring(0, 200) + "...";
                            sb.Append(IllustratorText.Format("Hero_345", kLore));
                        }
                    }
                }
                catch { }
            }

            // 家族背景与声誉
            if (hero.Clan != null)
            {
                string clanName = hero.Clan.Name != null ? hero.Clan.Name.ToString() : IllustratorText.Get("Hero_346");
                sb.Append(IllustratorText.Format("Hero_347", clanName, hero.Clan.Tier));

                try
                {
                    if (hero.Clan.EncyclopediaText != null)
                    {
                        string cLore = hero.Clan.EncyclopediaText.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(cLore))
                        {
                            cLore = System.Text.RegularExpressions.Regex.Replace(cLore, "<.*?>", string.Empty).Trim();
                            if (cLore.Length > 180) cLore = cLore.Substring(0, 180) + "...";
                            sb.Append(IllustratorText.Format("Hero_348", cLore));
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
            string kingdomName = hero.Clan?.Kingdom?.Name != null ? hero.Clan.Kingdom.Name.ToString() : (hero.Culture?.Name != null ? hero.Culture.Name.ToString() : IllustratorText.Get("Hero_349"));
            string clanName = hero.Clan?.Name != null ? hero.Clan.Name.ToString() : IllustratorText.Get("Hero_350");

            bool isFactionLeader = hero.IsFactionLeader || (hero.MapFaction != null && hero.MapFaction.Leader == hero);
            bool isClanLeader = hero.Clan != null && hero.Clan.Leader == hero;

            if (isFactionLeader)
            {
                socialStatus = IllustratorText.Get("Hero_351");
                return isFemale
                    ? IllustratorText.Format("Hero_352", kingdomName, kingdomName)
                    : IllustratorText.Format("Hero_353", kingdomName, kingdomName);
            }

            if (hero.IsLord)
            {
                if (isClanLeader)
                {
                    socialStatus = IllustratorText.Get("Hero_354");
                    return isFemale
                        ? IllustratorText.Format("Hero_355", clanName, clanName)
                        : IllustratorText.Format("Hero_356", clanName, clanName);
                }
                else
                {
                    socialStatus = IllustratorText.Get("Hero_357");
                    return isFemale
                        ? IllustratorText.Format("Hero_358", clanName, clanName)
                        : IllustratorText.Format("Hero_359", clanName, clanName);
                }
            }

            if (hero.IsNotable)
            {
                socialStatus = IllustratorText.Get("Hero_360");
                switch (hero.Occupation)
                {
                    case Occupation.Merchant:
                        return IllustratorText.Get("Hero_361");
                    case Occupation.Artisan:
                        return IllustratorText.Get("Hero_362");
                    case Occupation.GangLeader:
                        return IllustratorText.Get("Hero_363");
                    case Occupation.Preacher:
                        return IllustratorText.Get("Hero_364");
                    case Occupation.Headman:
                        return IllustratorText.Get("Hero_365");
                    default:
                        return IllustratorText.Get("Hero_366");
                }
            }

            if (hero.IsWanderer)
            {
                socialStatus = IllustratorText.Get("Hero_367");
                // 动态依据其实际最高特长识别职业
                int med = hero.GetSkillValue(DefaultSkills.Medicine);
                int eng = hero.GetSkillValue(DefaultSkills.Engineering);
                int rog = hero.GetSkillValue(DefaultSkills.Roguery);
                int combat = Math.Max(hero.GetSkillValue(DefaultSkills.OneHanded), Math.Max(hero.GetSkillValue(DefaultSkills.TwoHanded), hero.GetSkillValue(DefaultSkills.Polearm)));

                if (med >= 80 && med >= combat) return IllustratorText.Get("Hero_368");
                if (eng >= 80 && eng >= combat) return IllustratorText.Get("Hero_369");
                if (rog >= 80 && rog >= combat) return IllustratorText.Get("Hero_370");
                if (combat >= 100) return IllustratorText.Get("Hero_371");
                return IllustratorText.Get("Hero_372");
            }

            socialStatus = IllustratorText.Get("Hero_373");
            return IllustratorText.Get("Hero_374");
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
                    states.Add(IllustratorText.Get("Hero_375"));
                    return string.Join("；", states);
                }

                if (hero.IsPrisoner)
                {
                    var sb = new StringBuilder(IllustratorText.Get("Hero_376"));
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
                                string sType = s.IsCastle ? IllustratorText.Get("Hero_377") : (s.IsTown ? IllustratorText.Get("Hero_378") : IllustratorText.Get("Hero_379"));
                                sb.Append(IllustratorText.Get("Hero_380")).Append(sName).Append("（").Append(sType).Append(IllustratorText.Get("Hero_381"));
                            }
                            else if (holder.IsMobile && holder.MobileParty != null)
                            {
                                string pName = holder.MobileParty.Name != null ? holder.MobileParty.Name.ToString() : IllustratorText.Get("Hero_382");
                                sb.Append(IllustratorText.Get("Hero_383")).Append(pName).Append(IllustratorText.Get("Hero_384"));
                            }
                        }
                        catch
                        {
                        }
                    }
                    sb.Append(IllustratorText.Get("Hero_385"));
                    states.Add(sb.ToString());
                }
                else
                {
                    if (hero.IsWounded)
                    {
                        states.Add(IllustratorText.Get("Hero_386"));
                    }
                    try
                    {
                        if (hero.StayingInSettlement != null)
                        {
                            Settlement s = hero.StayingInSettlement;
                            states.Add(IllustratorText.Get("Hero_387") + (s.Name != null ? s.Name.ToString() : s.StringId));
                        }
                        else if (hero.PartyBelongedTo != null)
                        {
                            states.Add(IllustratorText.Get("Hero_388") + (hero.PartyBelongedTo.Name != null ? hero.PartyBelongedTo.Name.ToString() : IllustratorText.Get("Hero_389")) + IllustratorText.Get("Hero_390"));
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
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_391"), hero.GetSkillValue(DefaultSkills.OneHanded)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_392"), hero.GetSkillValue(DefaultSkills.TwoHanded)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_393"), hero.GetSkillValue(DefaultSkills.Polearm)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_394"), hero.GetSkillValue(DefaultSkills.Bow)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_395"), hero.GetSkillValue(DefaultSkills.Crossbow)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_396"), hero.GetSkillValue(DefaultSkills.Tactics)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_397"), hero.GetSkillValue(DefaultSkills.Leadership)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_398"), hero.GetSkillValue(DefaultSkills.Charm)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_399"), hero.GetSkillValue(DefaultSkills.Steward)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_400"), hero.GetSkillValue(DefaultSkills.Medicine)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_401"), hero.GetSkillValue(DefaultSkills.Engineering)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_402"), hero.GetSkillValue(DefaultSkills.Roguery)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_403"), hero.GetSkillValue(DefaultSkills.Trade)),
                    new KeyValuePair<string, int>(IllustratorText.Get("Hero_404"), hero.GetSkillValue(DefaultSkills.Riding))
                };

                skillList.Sort((a, b) => b.Value.CompareTo(a.Value));

                var top = new List<string>();
                for (int i = 0; i < Math.Min(3, skillList.Count); i++)
                {
                    if (skillList[i].Value >= 50)
                    {
                        top.Add(IllustratorText.Format("Hero_405", skillList[i].Key, skillList[i].Value));
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
                traits.Add(IllustratorText.Get("Hero_406"));
            else if (calculating < 0)
                traits.Add(IllustratorText.Get("Hero_407"));

            if (honor > 0)
                traits.Add(IllustratorText.Get("Hero_408"));
            else if (honor < 0)
                traits.Add(IllustratorText.Get("Hero_409"));

            if (mercy > 0)
                traits.Add(IllustratorText.Get("Hero_410"));
            else if (mercy < 0)
                traits.Add(IllustratorText.Get("Hero_411"));

            if (valor > 0)
                traits.Add(IllustratorText.Get("Hero_412"));

            if (generosity > 0)
                traits.Add(IllustratorText.Get("Hero_413"));
            else if (generosity < 0)
                traits.Add(IllustratorText.Get("Hero_414"));

            try
            {
                TraitObject persona = hero.CharacterObject?.GetPersona();
                if (persona != null)
                {
                    traits.Add(IllustratorText.Format("Hero_415", persona.Name));
                }
            }
            catch { }

            return traits.Count > 0 ? string.Join("；", traits) : IllustratorText.Get("Hero_416");
        }

        private static void ExtractArmorSlot(HeroVisualProfile profile, Equipment equipment, EquipmentIndex slot, string slotName, Hero hero)
        {
            EquipmentElement element = equipment[slot];

            if (element.Item == null)
            {
                if (slot == EquipmentIndex.Head)
                {
                    profile.HeadgearDetail = IllustratorText.Get("Hero_417");
                    profile.EquipmentDetails.Add(IllustratorText.Get("Hero_418"));
                }
                else if (slot == EquipmentIndex.Cape)
                {
                    profile.EquipmentDetails.Add(IllustratorText.Get("Hero_419"));
                }
                else if (slot == EquipmentIndex.Gloves)
                {
                    profile.EquipmentDetails.Add(IllustratorText.Get("Hero_420"));
                }
                return;
            }

            ItemObject item = element.Item;
            string itemName = item.Name != null ? item.Name.ToString() : item.StringId;
            string modifierStr = element.ItemModifier?.Name != null ? element.ItemModifier.Name.ToString() + IllustratorText.Get("Hero_421") : "";
            int tier = (int)item.Tier;

            string materialStr = ResolveItemMaterial(item);
            bool isHeraldic = item.IsUsingTableau;
            if (IsHeraldicArmorSlot(slot, isHeraldic))
            {
                profile.HasHeraldicArmor = true;
            }

            if (slot == EquipmentIndex.Head)
            {
                string hId = (item.StringId ?? "").ToLowerInvariant();
                string hName = (itemName ?? "").ToLowerInvariant();
                if (hId.Contains("daimao") || hName.Contains(IllustratorText.Raw("Hero_422")))
                {
                    profile.HeadgearDetail = IllustratorText.Format("Hero_423", item.StringId);
                }
                else
                {
                    profile.HeadgearDetail = BuildHeadgearDescription(modifierStr + itemName, item.StringId, materialStr,
                        item.ArmorComponent?.HairCoverType == ArmorComponent.HairCoverTypes.All,
                        item.ArmorComponent?.BeardCoverType == ArmorComponent.BeardCoverTypes.All);
                }
            }

            string heraldicNote = IsHeraldicArmorSlot(slot, isHeraldic)
                ? IllustratorText.Get("Hero_424") : "";
            if (slot == EquipmentIndex.Cape)
            {
                profile.EquipmentDetails.Add(IllustratorText.Format("Hero_425", modifierStr, itemName, materialStr, tier, heraldicNote));
                return;
            }

            string desc = IllustratorText.Format("Hero_426", slotName, modifierStr, itemName, materialStr, tier, heraldicNote);
            profile.EquipmentDetails.Add(desc);
        }

        internal static string BuildHeadgearDescription(string name, string id, string material, bool hidesHair, bool hidesBeard)
        {
            // HairCover/BeardCover control hidden hair meshes, not face visibility.
            // The native reference renderer still applies those flags through the unchanged equipment.
            return IllustratorText.Format("Hero_427", name, id, material) +
                IllustratorText.Get("Hero_428") +
                IllustratorText.Get("Hero_429") +
                (hidesHair ? IllustratorText.Get("Hero_430") : "") +
                (hidesBeard ? IllustratorText.Get("Hero_431") : "");
        }

        public static string ExtractCharacterPhysicalFeatures(CharacterObject character, Equipment equipment, int? resolvedAge = null)
        {
            if (character == null) return string.Empty;
            var headItem = equipment?[EquipmentIndex.Head].CosmeticItem ?? equipment?[EquipmentIndex.Head].Item;
            return BuildPhysicalFeaturesDescription(resolvedAge ?? ResolveAppearanceAge(character.Age, null),
                headItem?.ArmorComponent?.HairCoverType == ArmorComponent.HairCoverTypes.All,
                headItem?.ArmorComponent?.BeardCoverType == ArmorComponent.BeardCoverTypes.All);
        }

        internal static int ResolveAppearanceAge(float fallbackAge, string bodyProperties)
        {
            float age = fallbackAge;
            if (!string.IsNullOrWhiteSpace(bodyProperties) && BodyProperties.FromString(bodyProperties, out var body))
                age = body.Age;
            return !float.IsNaN(age) && !float.IsInfinity(age) && age > 0 && age < int.MaxValue ? (int)age : 0;
        }

        internal static bool IsHeraldicArmorSlot(EquipmentIndex slot, bool isUsingTableau)
        {
            return isUsingTableau && (slot == EquipmentIndex.Head || slot == EquipmentIndex.Body ||
                slot == EquipmentIndex.Leg || slot == EquipmentIndex.Gloves || slot == EquipmentIndex.Cape);
        }

        private static string ResolveItemMaterial(ItemObject item)
        {
            if (item == null) return IllustratorText.Get("Hero_432");
            string name = (item.Name != null ? item.Name.ToString() : "").ToLowerInvariant();
            string id = (item.StringId ?? "").ToLowerInvariant();

            // Material metadata must not turn a name such as battle_crown into a shape claim.
            if (item.ArmorComponent == null) return IllustratorText.Get("Hero_433");

            switch (item.ArmorComponent.MaterialType)
            {
                case ArmorComponent.ArmorMaterialTypes.Cloth:
                    if (name.Contains(IllustratorText.Raw("Hero_434")) || id.Contains("silk")) return IllustratorText.Get("Hero_435");
                    if (name.Contains(IllustratorText.Raw("Hero_436")) || id.Contains("linen") || id.Contains("burlap")) return IllustratorText.Get("Hero_437");
                    return IllustratorText.Get("Hero_438");
                case ArmorComponent.ArmorMaterialTypes.Leather:
                    return IllustratorText.Get("Hero_439");
                case ArmorComponent.ArmorMaterialTypes.Chainmail:
                    return IllustratorText.Get("Hero_440");
                case ArmorComponent.ArmorMaterialTypes.Plate:
                    return IllustratorText.Get("Hero_441");
                default:
                    return IllustratorText.Get("Hero_442");
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
                string modifierStr = element.ItemModifier?.Name != null ? element.ItemModifier.Name.ToString() + IllustratorText.Get("Hero_443") : "";

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
                    if (profile.EquipmentSource?.Contains(IllustratorText.Raw("Hero_444")) != true)
                    {
                        profile.WeaponDetails.Add(IllustratorText.Format("Hero_445", modifierStr, itemName));
                    }
                }
                else if (element.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                {
                    profile.WeaponDetails.Add(IllustratorText.Format("Hero_446", modifierStr, itemName));
                }
                else if (element.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon || element.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon)
                {
                    profile.WeaponDetails.Add(IllustratorText.Format("Hero_447", modifierStr, itemName));
                }
                else
                {
                    profile.WeaponDetails.Add(IllustratorText.Format("Hero_448", modifierStr, itemName));
                }
            }

            if (!hasShield)
            {
                profile.WeaponDetails.Add(IllustratorText.Get("Hero_449"));
            }
        }

        private static void ExtractMount(HeroVisualProfile profile, Equipment equipment)
        {
            EquipmentElement horse = equipment[EquipmentIndex.Horse];
            if (horse.Item != null)
            {
                string horseName = horse.Item.Name != null ? horse.Item.Name.ToString() : horse.Item.StringId;
                EquipmentElement harness = equipment[EquipmentIndex.HorseHarness];
                string harnessName = harness.Item != null ? (IllustratorText.Get("Hero_450") + (harness.Item.Name != null ? harness.Item.Name.ToString() : harness.Item.StringId)) : IllustratorText.Get("Hero_451");
                profile.MountDetail = $"{horseName} ({harnessName})";
            }
        }

        private static string ExtractPhysicalFeatures(Hero hero, Equipment equipment)
        {
            var headItem = equipment?[EquipmentIndex.Head].CosmeticItem ?? equipment?[EquipmentIndex.Head].Item;
            if (headItem != null)
            {
                string hId = (headItem.StringId ?? "").ToLowerInvariant();
                string hName = (headItem.Name?.ToString() ?? "").ToLowerInvariant();
                if (hId.Contains("daimao") || hName.Contains(IllustratorText.Raw("Hero_452")))
                {
                    return IllustratorText.Get("Hero_453");
                }
            }

            // BodyProperties stays intact in the appearance snapshot used by the native portrait.
            // A hair-color offset indexes a race/gender/age-specific palette; it is neither
            // an absolute color nor evidence for curl, length, beard style or greying.
            return BuildPhysicalFeaturesDescription((int)hero.Age,
                headItem?.ArmorComponent?.HairCoverType == ArmorComponent.HairCoverTypes.All,
                headItem?.ArmorComponent?.BeardCoverType == ArmorComponent.BeardCoverTypes.All);
        }

        internal static string BuildPhysicalFeaturesDescription(int age, bool hidesHair, bool hidesBeard)
        {
            return IllustratorText.Get("Hero_454") + (age > 0 ? IllustratorText.Format("Hero_455", age) : IllustratorText.Get("Hero_456")) + IllustratorText.Get("Hero_457") +
                IllustratorText.Get("Hero_458") +
                (hidesHair ? IllustratorText.Get("Hero_459") : "") +
                (hidesBeard ? IllustratorText.Get("Hero_460") : "") +
                IllustratorText.Get("Hero_461");
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
                        sb.Append(IllustratorText.Format("Hero_462", bio));
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
                    relatives.Add(IllustratorText.Format("Hero_463", hero.Spouse.Name));
                }
                if (hero.Father != null && hero.Father.Name != null)
                {
                    relatives.Add(IllustratorText.Format("Hero_464", hero.Father.Name));
                }
                if (hero.Mother != null && hero.Mother.Name != null)
                {
                    relatives.Add(IllustratorText.Format("Hero_465", hero.Mother.Name));
                }
                if (hero.Children != null && hero.Children.Count > 0)
                {
                    relatives.Add(IllustratorText.Format("Hero_466", hero.Children.Count));
                }
                if (relatives.Count > 0)
                {
                    sb.Append(IllustratorText.Format("Hero_467", string.Join("，", relatives)));
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
                        sb.Append(IllustratorText.Format("Hero_468", settlements.Count, string.Join("、", names)));
                    }
                }
                if (hero.GovernorOf != null && hero.GovernorOf.Name != null)
                {
                    sb.Append(IllustratorText.Format("Hero_469", hero.GovernorOf.Name));
                }
                if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero)
                {
                    sb.Append(IllustratorText.Format("Hero_470", hero.PartyBelongedTo.MemberRoster.TotalManCount));
                }
            }
            catch { }

            return sb.ToString().Trim();
        }

        private static string ExtractBannerDescription(Hero hero)
        {
            if (hero.Clan == null) return string.Empty;

            // The real banner is sent as a dedicated emblem reference image when a
            // confirmed heraldic carrier is present. Expanding every BannerData layer
            // into prose duplicated that image and could consume tens of thousands of
            // prompt characters for complex banners. Keep only attribution and carrier
            // semantics here; the reference image owns exact colors, shapes and layout.
            return IllustratorText.Get("Hero_471") +
                IllustratorText.Get("Hero_472");
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

            if (v < 0.16f) return IllustratorText.Get("Hero_473");
            if (s < 0.12f)
            {
                if (v > 0.9f) return IllustratorText.Get("Hero_474");
                if (v > 0.6f) return IllustratorText.Get("Hero_475");
                return IllustratorText.Get("Hero_476");
            }
            if (s < 0.3f && v > 0.6f && h >= 20f && h < 70f) return IllustratorText.Get("Hero_477");
            string tone = v < 0.45f ? IllustratorText.Get("Hero_478") : s < 0.35f ? IllustratorText.Get("Hero_479") : string.Empty;
            if (h >= 345f || h < 15f) return tone + IllustratorText.Get("Hero_480");
            if (h < 45f) return v < 0.65f ? IllustratorText.Get("Hero_481") : tone + IllustratorText.Get("Hero_482");
            if (h < 70f) return tone + IllustratorText.Get("Hero_483");
            if (h < 165f) return tone + IllustratorText.Get("Hero_484");
            if (h < 200f) return tone + IllustratorText.Get("Hero_485");
            if (h < 250f) return tone + IllustratorText.Get("Hero_486");
            if (h < 290f) return tone + IllustratorText.Get("Hero_487");
            return tone + IllustratorText.Get("Hero_488");
        }

    }
}
