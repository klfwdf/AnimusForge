using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Helpers;

public static class MobilePartyHelper
{
	public delegate void ResumePartyEscortBehaviorDelegate();

	public static MobileParty SpawnLordParty(Hero hero, Settlement spawnSettlement)
	{
		return SpawnLordPartyAux(hero, spawnSettlement.GatePosition, 0f, spawnSettlement);
	}

	public static MobileParty SpawnLordParty(Hero hero, CampaignVec2 position, float spawnRadius)
	{
		return SpawnLordPartyAux(hero, position, spawnRadius, null);
	}

	private static MobileParty SpawnLordPartyAux(Hero hero, CampaignVec2 position, float spawnRadius, Settlement spawnSettlement)
	{
		return LordPartyComponent.CreateLordParty(hero.CharacterObject.StringId, hero, position, spawnRadius, spawnSettlement, hero);
	}

	public static MobileParty CreateNewClanMobileParty(Hero hero, Clan clan)
	{
		if (hero.CurrentSettlement != null)
		{
			Settlement currentSettlement = hero.CurrentSettlement;
			if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsMainParty)
			{
				PartyBase.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject);
			}
			return SpawnLordParty(hero, currentSettlement);
		}
		MobileParty partyBelongedTo = hero.PartyBelongedTo;
		partyBelongedTo?.AddElementToMemberRoster(hero.CharacterObject, -1);
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
			Debug.FailedAssert("Cant find a position to spawn mobile party.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CreateNewClanMobileParty", 3698);
		}
		return SpawnLordParty(hero, position, Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2f);
	}

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

	public static TroopRoster GetStrongestAndPriorTroops(MobileParty mobileParty, int maxTroopCount, bool includePlayer)
	{
		FlattenedTroopRoster flattenedTroopRoster = mobileParty.MemberRoster.ToFlattenedRoster();
		flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
		return GetStrongestAndPriorTroops(flattenedTroopRoster, maxTroopCount, includePlayer);
	}

	public static TroopRoster GetStrongestAndPriorTroops(FlattenedTroopRoster roster, int maxTroopCount, bool includePlayer)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		List<CharacterObject> list = (from x in roster
			select x.Troop into x
			orderby x.Level descending
			select x).ToList();
		if (list.Any((CharacterObject x) => x.IsPlayerCharacter))
		{
			list.Remove(CharacterObject.PlayerCharacter);
			if (includePlayer)
			{
				troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1);
				maxTroopCount--;
			}
		}
		List<CharacterObject> list2 = list.Where((CharacterObject x) => x.IsNotTransferableInPartyScreen && x.IsHero).ToList();
		int num = MathF.Min(list2.Count, maxTroopCount);
		for (int num2 = 0; num2 < num; num2++)
		{
			troopRoster.AddToCounts(list2[num2], 1);
			list.Remove(list2[num2]);
		}
		int count = list.Count;
		for (int num3 = num; num3 < maxTroopCount && num3 < count; num3++)
		{
			troopRoster.AddToCounts(list[num3], 1);
		}
		return troopRoster;
	}

	public static int GetMaximumXpAmountPartyCanGet(MobileParty party)
	{
		TroopRoster memberRoster = party.MemberRoster;
		int num = 0;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
			if (CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out var gainableMaxXp))
			{
				num += gainableMaxXp;
			}
		}
		return num;
	}

	public static void PartyAddSharedXp(MobileParty party, float xpToDistribute)
	{
		TroopRoster memberRoster = party.MemberRoster;
		int num = 0;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
			if (CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out var gainableMaxXp))
			{
				num += gainableMaxXp;
			}
		}
		for (int j = 0; j < memberRoster.Count; j++)
		{
			if (!(xpToDistribute >= 1f))
			{
				break;
			}
			if (num <= 0)
			{
				break;
			}
			TroopRosterElement elementCopyAtIndex2 = memberRoster.GetElementCopyAtIndex(j);
			if (CanTroopGainXp(party.Party, elementCopyAtIndex2.Character, out var gainableMaxXp2))
			{
				int num2 = MathF.Floor(MathF.Max(1f, xpToDistribute * (float)gainableMaxXp2 / (float)num));
				memberRoster.AddXpToTroopAtIndex(j, num2);
				xpToDistribute -= (float)num2;
				num -= gainableMaxXp2;
			}
		}
	}

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

	public static bool CanTroopGainXp(PartyBase owner, CharacterObject character, out int gainableMaxXp)
	{
		gainableMaxXp = 0;
		if (character.UpgradeTargets == null)
		{
			Debug.FailedAssert("Upgrade target is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CanTroopGainXp", 3843);
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

	public static void TryMatchPartySpeedWithItemWeight(MobileParty party, float targetPartySpeed, ItemObject itemToUse = null)
	{
		targetPartySpeed = MathF.Max(1f, targetPartySpeed);
		ItemObject item = itemToUse ?? DefaultItems.HardWood;
		float speed = party.Speed;
		int num = MathF.Sign(speed - targetPartySpeed);
		for (int i = 0; i < 200; i++)
		{
			if (MathF.Abs(speed - targetPartySpeed) < 0.1f)
			{
				break;
			}
			if (MathF.Sign(speed - targetPartySpeed) != num)
			{
				break;
			}
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
		}
	}

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

	public static Settlement GetCurrentSettlementOfMobilePartyForAICalculation(MobileParty mobileParty)
	{
		Settlement settlement = mobileParty.CurrentSettlement;
		if (settlement == null)
		{
			if (mobileParty.LastVisitedSettlement == null || !(mobileParty.LastVisitedSettlement.Position.DistanceSquared(mobileParty.Position) < 1f))
			{
				return null;
			}
			settlement = mobileParty.LastVisitedSettlement;
		}
		return settlement;
	}

	public static TroopRoster GetPlayerPrisonersPlayerCanSell()
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		List<string> list = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks().ToList();
		foreach (TroopRosterElement item in MobileParty.MainParty.PrisonRoster.GetTroopRoster())
		{
			if (!list.Contains(item.Character.StringId))
			{
				troopRoster.Add(item);
			}
		}
		return troopRoster;
	}

	public static void FillPartyManuallyAfterCreation(MobileParty mobileParty, PartyTemplateObject partyTemplate, int desiredMenCount)
	{
		mobileParty.MemberRoster.Clear();
		float num = 0f;
		int num2 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MinValue);
		int num3 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
		num = ((desiredMenCount < num2) ? ((float)desiredMenCount / (float)num2 - 1f) : ((num2 > desiredMenCount || desiredMenCount > num3) ? ((float)desiredMenCount / (float)num3) : ((float)(desiredMenCount - num2) / (float)(num3 - num2))));
		for (int num4 = 0; num4 < partyTemplate.Stacks.Count; num4++)
		{
			PartyTemplateStack partyTemplateStack = partyTemplate.Stacks[num4];
			int minValue = partyTemplateStack.MinValue;
			int maxValue = partyTemplateStack.MaxValue;
			int num5 = ((-1f <= num && num < 0f) ? MBRandom.RoundRandomized((float)minValue + (float)minValue * num) : ((!(0f <= num) || !(num <= 1f)) ? MBRandom.RoundRandomized((float)maxValue * num) : MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * num)));
			if (num5 > 0)
			{
				mobileParty.MemberRoster.AddToCounts(partyTemplateStack.Character, num5);
			}
		}
		float maxVal = partyTemplate.Stacks.Sum((PartyTemplateStack x) => (float)(x.MaxValue + x.MinValue) / 2f);
		while (mobileParty.MemberRoster.TotalManCount > desiredMenCount)
		{
			int index = 0;
			float num6 = MBRandom.RandomFloatRanged(maxVal);
			for (int num7 = 0; num7 < partyTemplate.Stacks.Count; num7++)
			{
				PartyTemplateStack partyTemplateStack2 = partyTemplate.Stacks[num7];
				float num8 = (float)(partyTemplateStack2.MaxValue + partyTemplateStack2.MinValue) / 2f;
				num6 -= num8;
				if (num6 <= 0f)
				{
					index = num7;
					break;
				}
			}
			CharacterObject character = partyTemplate.Stacks[index].Character;
			mobileParty.MemberRoster.AddToCounts(character, -1);
		}
		while (mobileParty.MemberRoster.TotalManCount < desiredMenCount)
		{
			int index2 = 0;
			float num9 = MBRandom.RandomFloatRanged(maxVal);
			for (int num10 = 0; num10 < partyTemplate.Stacks.Count; num10++)
			{
				PartyTemplateStack partyTemplateStack3 = partyTemplate.Stacks[num10];
				float num11 = (float)(partyTemplateStack3.MaxValue + partyTemplateStack3.MinValue) / 2f;
				num9 -= num11;
				if (num9 <= 0f)
				{
					index2 = num10;
					break;
				}
			}
			CharacterObject character2 = partyTemplate.Stacks[index2].Character;
			mobileParty.MemberRoster.AddToCounts(character2, 1);
		}
	}

	public static bool CanPartyAttackWithCurrentMorale(MobileParty mobileParty)
	{
		return mobileParty.Morale > 0f;
	}
}
