using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000010 RID: 16
	public static class MobilePartyHelper
	{
		// Token: 0x06000082 RID: 130 RVA: 0x00007C7C File Offset: 0x00005E7C
		public static MobileParty SpawnLordParty(Hero hero, Settlement spawnSettlement)
		{
			return MobilePartyHelper.SpawnLordPartyAux(hero, spawnSettlement.GatePosition, 0f, spawnSettlement);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00007C90 File Offset: 0x00005E90
		public static MobileParty SpawnLordParty(Hero hero, CampaignVec2 position, float spawnRadius)
		{
			return MobilePartyHelper.SpawnLordPartyAux(hero, position, spawnRadius, null);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00007C9B File Offset: 0x00005E9B
		private static MobileParty SpawnLordPartyAux(Hero hero, CampaignVec2 position, float spawnRadius, Settlement spawnSettlement)
		{
			return LordPartyComponent.CreateLordParty(hero.CharacterObject.StringId, hero, position, spawnRadius, spawnSettlement, hero);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00007CB4 File Offset: 0x00005EB4
		public static MobileParty CreateNewClanMobileParty(Hero hero, Clan clan)
		{
			MobileParty result;
			if (hero.CurrentSettlement != null)
			{
				Settlement currentSettlement = hero.CurrentSettlement;
				if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsMainParty)
				{
					PartyBase.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
				}
				result = MobilePartyHelper.SpawnLordParty(hero, currentSettlement);
			}
			else
			{
				MobileParty partyBelongedTo = hero.PartyBelongedTo;
				if (partyBelongedTo != null)
				{
					partyBelongedTo.AddElementToMemberRoster(hero.CharacterObject, -1, false);
				}
				MobileParty.NavigationType navigationType = MobileParty.NavigationType.Default;
				Settlement bestSettlementToSpawnAround = SettlementHelper.GetBestSettlementToSpawnAround(hero);
				CampaignVec2 position = CampaignVec2.Invalid;
				if (partyBelongedTo != null && NavigationHelper.IsPositionValidForNavigationType(partyBelongedTo.Position, navigationType))
				{
					position = partyBelongedTo.Position;
				}
				else if (bestSettlementToSpawnAround != null)
				{
					position = bestSettlementToSpawnAround.GatePosition;
				}
				else
				{
					Debug.FailedAssert("Cant find a position to spawn mobile party.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CreateNewClanMobileParty", 3711);
				}
				result = MobilePartyHelper.SpawnLordParty(hero, position, Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2f);
			}
			return result;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00007DA5 File Offset: 0x00005FA5
		public static bool IsHeroAssignableForScoutInParty(Hero hero, MobileParty party)
		{
			return hero.PartyBelongedTo == party && hero != party.GetRoleHolder(PartyRole.Scout) && hero.GetSkillValue(DefaultSkills.Scouting) >= 0;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00007DCE File Offset: 0x00005FCE
		public static bool IsHeroAssignableForEngineerInParty(Hero hero, MobileParty party)
		{
			return hero.PartyBelongedTo == party && hero != party.GetRoleHolder(PartyRole.Engineer) && hero.GetSkillValue(DefaultSkills.Engineering) >= 0;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00007DF6 File Offset: 0x00005FF6
		public static bool IsHeroAssignableForSurgeonInParty(Hero hero, MobileParty party)
		{
			return hero.PartyBelongedTo == party && hero != party.GetRoleHolder(PartyRole.Surgeon) && hero.GetSkillValue(DefaultSkills.Medicine) >= 0;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00007E1E File Offset: 0x0000601E
		public static bool IsHeroAssignableForQuartermasterInParty(Hero hero, MobileParty party)
		{
			return hero.PartyBelongedTo == party && hero != party.GetRoleHolder(PartyRole.Quartermaster) && hero.GetSkillValue(DefaultSkills.Trade) >= 0;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00007E48 File Offset: 0x00006048
		public static Hero GetHeroWithHighestSkill(MobileParty party, SkillObject skill)
		{
			Hero result = null;
			int num = -1;
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				CharacterObject characterAtIndex = party.MemberRoster.GetCharacterAtIndex(i);
				if (characterAtIndex.HeroObject != null && characterAtIndex.HeroObject.GetSkillValue(skill) > num)
				{
					num = characterAtIndex.HeroObject.GetSkillValue(skill);
					result = characterAtIndex.HeroObject;
				}
			}
			return result;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00007EA8 File Offset: 0x000060A8
		public static TroopRoster GetStrongestAndPriorTroops(MobileParty mobileParty, int maxTroopCount, bool includePlayer)
		{
			FlattenedTroopRoster flattenedTroopRoster = mobileParty.MemberRoster.ToFlattenedRoster();
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
			return MobilePartyHelper.GetStrongestAndPriorTroops(flattenedTroopRoster, maxTroopCount, includePlayer);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00007EE4 File Offset: 0x000060E4
		public static TroopRoster GetStrongestAndPriorTroops(FlattenedTroopRoster roster, int maxTroopCount, bool includePlayer)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			List<CharacterObject> list = (from x in roster
				select x.Troop into x
				orderby x.Level descending
				select x).ToList<CharacterObject>();
			if (list.Any((CharacterObject x) => x.IsPlayerCharacter))
			{
				list.Remove(CharacterObject.PlayerCharacter);
				if (includePlayer)
				{
					troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1, false, 0, 0, true, -1);
					maxTroopCount--;
				}
			}
			List<CharacterObject> list2 = (from x in list
				where x.IsNotTransferableInPartyScreen && x.IsHero
				select x).ToList<CharacterObject>();
			int num = MathF.Min(list2.Count, maxTroopCount);
			for (int i = 0; i < num; i++)
			{
				troopRoster.AddToCounts(list2[i], 1, false, 0, 0, true, -1);
				list.Remove(list2[i]);
			}
			int count = list.Count;
			int num2 = num;
			while (num2 < maxTroopCount && num2 < count)
			{
				troopRoster.AddToCounts(list[num2], 1, false, 0, 0, true, -1);
				num2++;
			}
			return troopRoster;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00008034 File Offset: 0x00006234
		public static int GetMaximumXpAmountPartyCanGet(MobileParty party)
		{
			TroopRoster memberRoster = party.MemberRoster;
			int num = 0;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
				int num2;
				if (MobilePartyHelper.CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out num2))
				{
					num += num2;
				}
			}
			return num;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00008080 File Offset: 0x00006280
		public static void PartyAddSharedXp(MobileParty party, float xpToDistribute)
		{
			TroopRoster memberRoster = party.MemberRoster;
			int num = 0;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
				int num2;
				if (MobilePartyHelper.CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out num2))
				{
					num += num2;
				}
			}
			int num3 = 0;
			while (num3 < memberRoster.Count && xpToDistribute >= 1f && num > 0)
			{
				TroopRosterElement elementCopyAtIndex2 = memberRoster.GetElementCopyAtIndex(num3);
				int num4;
				if (MobilePartyHelper.CanTroopGainXp(party.Party, elementCopyAtIndex2.Character, out num4))
				{
					int num5 = MathF.Floor(MathF.Max(1f, xpToDistribute * (float)num4 / (float)num));
					memberRoster.AddXpToTroopAtIndex(num3, num5);
					xpToDistribute -= (float)num5;
					num -= num4;
				}
				num3++;
			}
		}

		// Token: 0x0600008F RID: 143 RVA: 0x0000813C File Offset: 0x0000633C
		public static void WoundNumberOfNonHeroTroopsRandomlyWithChanceOfDeath(TroopRoster roster, int numberOfMen, float chanceOfDeathPerUnit, out int deathAmount)
		{
			deathAmount = 0;
			for (int i = 0; i < numberOfMen; i++)
			{
				if (MBRandom.RandomFloat < chanceOfDeathPerUnit)
				{
					deathAmount++;
				}
			}
			if (deathAmount > 0)
			{
				roster.RemoveNumberOfNonHeroTroopsRandomly(deathAmount);
			}
			if (numberOfMen > deathAmount)
			{
				roster.WoundNumberOfNonHeroTroopsRandomly(numberOfMen - deathAmount);
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00008184 File Offset: 0x00006384
		public static bool CanTroopGainXp(PartyBase owner, CharacterObject character, out int gainableMaxXp)
		{
			gainableMaxXp = 0;
			if (character.UpgradeTargets == null)
			{
				Debug.FailedAssert("Upgrade target is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CanTroopGainXp", 3884);
				return false;
			}
			bool result = false;
			int index = owner.MemberRoster.FindIndexOfTroop(character);
			int elementNumber = owner.MemberRoster.GetElementNumber(index);
			int elementXp = owner.MemberRoster.GetElementXp(index);
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				int upgradeXpCost = character.GetUpgradeXpCost(owner, i);
				if (elementXp < upgradeXpCost * elementNumber)
				{
					result = true;
					int num = upgradeXpCost * elementNumber - elementXp;
					if (num > gainableMaxXp)
					{
						gainableMaxXp = num;
					}
				}
			}
			return result;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00008224 File Offset: 0x00006424
		public static void TryMatchPartySpeedWithItemWeight(MobileParty party, float targetPartySpeed, ItemObject itemToUse = null)
		{
			targetPartySpeed = MathF.Max(1f, targetPartySpeed);
			ItemObject item = itemToUse ?? DefaultItems.HardWood;
			float speed = party.Speed;
			int num = MathF.Sign(speed - targetPartySpeed);
			int num2 = 0;
			while (num2 < 200 && MathF.Abs(speed - targetPartySpeed) >= 0.1f && MathF.Sign(speed - targetPartySpeed) == num)
			{
				if (speed >= targetPartySpeed)
				{
					party.ItemRoster.AddToCounts(item, 1);
				}
				else
				{
					if (party.ItemRoster.GetItemNumber(item) <= 0)
					{
						break;
					}
					party.ItemRoster.AddToCounts(item, -1);
				}
				speed = party.Speed;
				num2++;
			}
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000082BC File Offset: 0x000064BC
		public static Hero GetMainPartySkillCounsellor(SkillObject skill)
		{
			PartyBase mainParty = PartyBase.MainParty;
			Hero hero = null;
			int num = 0;
			for (int i = 0; i < mainParty.MemberRoster.Count; i++)
			{
				CharacterObject characterAtIndex = mainParty.MemberRoster.GetCharacterAtIndex(i);
				if (characterAtIndex.IsHero && !characterAtIndex.HeroObject.IsWounded)
				{
					int skillValue = characterAtIndex.GetSkillValue(skill);
					if (skillValue >= num)
					{
						num = skillValue;
						hero = characterAtIndex.HeroObject;
					}
				}
			}
			return hero ?? mainParty.LeaderHero;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00008334 File Offset: 0x00006534
		public static Settlement GetCurrentSettlementOfMobilePartyForAICalculation(MobileParty mobileParty)
		{
			Settlement result;
			if ((result = mobileParty.CurrentSettlement) == null)
			{
				if (mobileParty.LastVisitedSettlement == null || mobileParty.LastVisitedSettlement.Position.DistanceSquared(mobileParty.Position) >= 1f)
				{
					return null;
				}
				result = mobileParty.LastVisitedSettlement;
			}
			return result;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x0000837C File Offset: 0x0000657C
		public static TroopRoster GetPlayerPrisonersPlayerCanSell()
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			List<string> list = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks().ToList<string>();
			foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.PrisonRoster.GetTroopRoster())
			{
				if (!list.Contains(troopRosterElement.Character.StringId))
				{
					troopRoster.Add(troopRosterElement);
				}
			}
			return troopRoster;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00008408 File Offset: 0x00006608
		public static void FillPartyManuallyAfterCreation(MobileParty mobileParty, PartyTemplateObject partyTemplate, int desiredMenCount)
		{
			mobileParty.MemberRoster.Clear();
			int num = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MinValue);
			int num2 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
			float num3;
			if (desiredMenCount < num)
			{
				num3 = (float)desiredMenCount / (float)num - 1f;
			}
			else if (num <= desiredMenCount && desiredMenCount <= num2)
			{
				num3 = (float)(desiredMenCount - num) / (float)(num2 - num);
			}
			else
			{
				num3 = (float)desiredMenCount / (float)num2;
			}
			for (int i = 0; i < partyTemplate.Stacks.Count; i++)
			{
				PartyTemplateStack partyTemplateStack = partyTemplate.Stacks[i];
				int minValue = partyTemplateStack.MinValue;
				int maxValue = partyTemplateStack.MaxValue;
				int num4;
				if (-1f <= num3 && num3 < 0f)
				{
					num4 = MBRandom.RoundRandomized((float)minValue + (float)minValue * num3);
				}
				else if (0f <= num3 && num3 <= 1f)
				{
					num4 = MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * num3);
				}
				else
				{
					num4 = MBRandom.RoundRandomized((float)maxValue * num3);
				}
				if (num4 > 0)
				{
					mobileParty.MemberRoster.AddToCounts(partyTemplateStack.Character, num4, false, 0, 0, true, -1);
				}
			}
			float maxVal = partyTemplate.Stacks.Sum((PartyTemplateStack x) => (float)(x.MaxValue + x.MinValue) / 2f);
			while (mobileParty.MemberRoster.TotalManCount > desiredMenCount)
			{
				int index = 0;
				float num5 = MBRandom.RandomFloatRanged(maxVal);
				for (int j = 0; j < partyTemplate.Stacks.Count; j++)
				{
					PartyTemplateStack partyTemplateStack2 = partyTemplate.Stacks[j];
					float num6 = (float)(partyTemplateStack2.MaxValue + partyTemplateStack2.MinValue) / 2f;
					num5 -= num6;
					if (num5 <= 0f)
					{
						index = j;
						break;
					}
				}
				CharacterObject character = partyTemplate.Stacks[index].Character;
				mobileParty.MemberRoster.AddToCounts(character, -1, false, 0, 0, true, -1);
			}
			while (mobileParty.MemberRoster.TotalManCount < desiredMenCount)
			{
				int index2 = 0;
				float num7 = MBRandom.RandomFloatRanged(maxVal);
				for (int k = 0; k < partyTemplate.Stacks.Count; k++)
				{
					PartyTemplateStack partyTemplateStack3 = partyTemplate.Stacks[k];
					float num8 = (float)(partyTemplateStack3.MaxValue + partyTemplateStack3.MinValue) / 2f;
					num7 -= num8;
					if (num7 <= 0f)
					{
						index2 = k;
						break;
					}
				}
				CharacterObject character2 = partyTemplate.Stacks[index2].Character;
				mobileParty.MemberRoster.AddToCounts(character2, 1, false, 0, 0, true, -1);
			}
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000086C6 File Offset: 0x000068C6
		public static bool CanPartyAttackWithCurrentMorale(MobileParty mobileParty)
		{
			return mobileParty.Morale > 0f;
		}

		// Token: 0x020004D5 RID: 1237
		// (Invoke) Token: 0x06004A6E RID: 19054
		public delegate void ResumePartyEscortBehaviorDelegate();
	}
}
