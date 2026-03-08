using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x0200001A RID: 26
	public static class PerkHelper
	{
		// Token: 0x060000F0 RID: 240 RVA: 0x0000C674 File Offset: 0x0000A874
		public static IEnumerable<PerkObject> GetCaptainPerksForTroopUsages(TroopUsageFlags troopUsageFlags)
		{
			List<PerkObject> list = new List<PerkObject>();
			foreach (PerkObject perkObject in PerkObject.All)
			{
				bool flag = perkObject.PrimaryTroopUsageMask != TroopUsageFlags.Undefined && troopUsageFlags.HasAllFlags(perkObject.PrimaryTroopUsageMask);
				bool flag2 = perkObject.SecondaryTroopUsageMask != TroopUsageFlags.Undefined && troopUsageFlags.HasAllFlags(perkObject.SecondaryTroopUsageMask);
				if (flag || flag2)
				{
					list.Add(perkObject);
				}
			}
			return list;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000C70C File Offset: 0x0000A90C
		public static bool PlayerHasAnyItemDonationPerk()
		{
			return MobileParty.MainParty.HasPerk(DefaultPerks.Steward.GivingHands, false) || MobileParty.MainParty.HasPerk(DefaultPerks.Steward.PaidInPromise, true);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000C734 File Offset: 0x0000A934
		public static void AddPerkBonusForParty(PerkObject perk, MobileParty party, bool isPrimaryBonus, ref ExplainedNumber stat, bool shouldApplyNavalMultiplier = false)
		{
			if (party != null && party.HasPerk(perk, !isPrimaryBonus))
			{
				float num = (isPrimaryBonus ? perk.PrimaryBonus : perk.SecondaryBonus);
				if (shouldApplyNavalMultiplier)
				{
					num *= 0.5f;
				}
				EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
				PerkHelper.AddToStat(ref stat, effectIncrementType, num, perk.Name);
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000C78F File Offset: 0x0000A98F
		private static void AddToStat(ref ExplainedNumber stat, EffectIncrementType effectIncrementType, float number, TextObject text)
		{
			if (effectIncrementType == EffectIncrementType.Add)
			{
				stat.Add(number, text, null);
				return;
			}
			if (effectIncrementType == EffectIncrementType.AddFactor)
			{
				stat.AddFactor(number, text);
			}
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000C7AC File Offset: 0x0000A9AC
		public static void AddPerkBonusForCharacter(PerkObject perk, CharacterObject character, bool isPrimaryBonus, ref ExplainedNumber bonuses, bool shouldApplyNavalMultiplier = false)
		{
			float num = (shouldApplyNavalMultiplier ? 0.5f : 1f);
			if (isPrimaryBonus && perk.PrimaryRole == PartyRole.Personal)
			{
				if (character.GetPerkValue(perk))
				{
					PerkHelper.AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * num, perk.Name);
				}
			}
			else if (!isPrimaryBonus && perk.SecondaryRole == PartyRole.Personal && character.GetPerkValue(perk))
			{
				PerkHelper.AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * num, perk.Name);
			}
			if (isPrimaryBonus && perk.PrimaryRole == PartyRole.ClanLeader)
			{
				if (character.IsHero)
				{
					Clan clan = character.HeroObject.Clan;
					if (((clan != null) ? clan.Leader : null) != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
					{
						PerkHelper.AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * num, perk.Name);
						return;
					}
				}
			}
			else if (!isPrimaryBonus && perk.SecondaryRole == PartyRole.ClanLeader && character.IsHero && character.HeroObject.Clan.Leader != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
			{
				PerkHelper.AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * num, perk.Name);
			}
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000C8EC File Offset: 0x0000AAEC
		public static void AddEpicPerkBonusForCharacter(PerkObject perk, CharacterObject character, SkillObject skillType, bool applyPrimaryBonus, ref ExplainedNumber bonuses, int skillRequired, bool shouldApplyNavalMultiplier = false)
		{
			if (character.GetPerkValue(perk))
			{
				int skillValue = character.GetSkillValue(skillType);
				if (skillValue > skillRequired)
				{
					float num = (shouldApplyNavalMultiplier ? 0.5f : 1f);
					if (applyPrimaryBonus)
					{
						PerkHelper.AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * (float)(skillValue - skillRequired) * num, perk.Name);
						return;
					}
					PerkHelper.AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * (float)(skillValue - skillRequired) * num, perk.Name);
				}
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000C968 File Offset: 0x0000AB68
		public static void AddPerkBonusFromCaptain(PerkObject perk, CharacterObject captainCharacter, ref ExplainedNumber bonuses)
		{
			if (perk.PrimaryRole == PartyRole.Captain)
			{
				if (captainCharacter != null && captainCharacter.GetPerkValue(perk))
				{
					PerkHelper.AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus, perk.Name);
					return;
				}
			}
			else if (perk.SecondaryRole == PartyRole.Captain && captainCharacter != null && captainCharacter.GetPerkValue(perk))
			{
				PerkHelper.AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus, perk.Name);
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000C9D4 File Offset: 0x0000ABD4
		public static void AddPerkBonusForTown(PerkObject perk, Town town, ref ExplainedNumber bonuses)
		{
			bool flag = perk.PrimaryRole == PartyRole.Governor;
			bool flag2 = perk.SecondaryRole == PartyRole.Governor;
			if (flag || flag2)
			{
				Hero governor = town.Governor;
				if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
				{
					if (flag)
					{
						PerkHelper.AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus, perk.Name);
						return;
					}
					PerkHelper.AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus, perk.Name);
				}
			}
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000CA58 File Offset: 0x0000AC58
		public static bool GetPerkValueForTown(PerkObject perk, Town town)
		{
			if (perk.PrimaryRole == PartyRole.ClanLeader || perk.SecondaryRole == PartyRole.ClanLeader)
			{
				Clan ownerClan = town.Owner.Settlement.OwnerClan;
				Hero hero = ((ownerClan != null) ? ownerClan.Leader : null);
				if (hero != null && hero.GetPerkValue(perk))
				{
					return true;
				}
			}
			if (perk.PrimaryRole == PartyRole.Governor || perk.SecondaryRole == PartyRole.Governor)
			{
				Hero governor = town.Governor;
				if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000CAE0 File Offset: 0x0000ACE0
		public static List<PerkObject> GetGovernorPerksForHero(Hero hero)
		{
			List<PerkObject> list = new List<PerkObject>();
			foreach (PerkObject perkObject in PerkObject.All)
			{
				if ((perkObject.PrimaryRole == PartyRole.Governor || perkObject.SecondaryRole == PartyRole.Governor) && hero.GetPerkValue(perkObject))
				{
					list.Add(perkObject);
				}
			}
			return list;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000CB54 File Offset: 0x0000AD54
		public static ValueTuple<TextObject, TextObject> GetGovernorEngineeringSkillEffectForHero(Hero governor)
		{
			if (governor != null && governor.GetSkillValue(DefaultSkills.Engineering) > 0)
			{
				SkillEffect townProjectBuildingBonus = DefaultSkillEffects.TownProjectBuildingBonus;
				int skillValue = governor.GetSkillValue(townProjectBuildingBonus.EffectedSkill);
				TextObject effectDescriptionForSkillLevel = SkillHelper.GetEffectDescriptionForSkillLevel(townProjectBuildingBonus, skillValue);
				return new ValueTuple<TextObject, TextObject>(DefaultSkills.Engineering.Name, effectDescriptionForSkillLevel);
			}
			return new ValueTuple<TextObject, TextObject>(TextObject.GetEmpty(), new TextObject("{=0rBsbw1T}No effect", null));
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000CBB4 File Offset: 0x0000ADB4
		public static int AvailablePerkCountOfHero(Hero hero)
		{
			MBList<PerkObject> mblist = new MBList<PerkObject>();
			foreach (PerkObject perkObject in PerkObject.All)
			{
				SkillObject skill = perkObject.Skill;
				if ((float)hero.GetSkillValue(skill) >= perkObject.RequiredSkillValue && !hero.GetPerkValue(perkObject) && (perkObject.AlternativePerk == null || !hero.GetPerkValue(perkObject.AlternativePerk)) && !mblist.Contains(perkObject.AlternativePerk))
				{
					mblist.Add(perkObject);
				}
			}
			return mblist.Count;
		}

		// Token: 0x04000004 RID: 4
		public const float NavalMultiplier = 0.5f;
	}
}
