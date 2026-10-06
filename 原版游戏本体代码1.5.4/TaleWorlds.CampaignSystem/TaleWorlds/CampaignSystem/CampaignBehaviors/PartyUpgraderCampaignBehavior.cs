using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyUpgraderCampaignBehavior : CampaignBehaviorBase
{
	private readonly struct TroopUpgradeArgs
	{
		public readonly CharacterObject Target;

		public readonly CharacterObject UpgradeTarget;

		public readonly int PossibleUpgradeCount;

		public readonly int UpgradeGoldCost;

		public readonly int UpgradeXpCost;

		public readonly float UpgradeChance;

		public TroopUpgradeArgs(CharacterObject target, CharacterObject upgradeTarget, int possibleUpgradeCount, int upgradeGoldCost, int upgradeXpCost, float upgradeChance)
		{
			Target = target;
			UpgradeTarget = upgradeTarget;
			PossibleUpgradeCount = possibleUpgradeCount;
			UpgradeGoldCost = upgradeGoldCost;
			UpgradeXpCost = upgradeXpCost;
			UpgradeChance = upgradeChance;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		foreach (PartyBase involvedParty in mapEvent.InvolvedParties)
		{
			UpgradeReadyTroops(involvedParty);
		}
	}

	public void DailyTickParty(MobileParty party)
	{
		if (party.MapEvent == null)
		{
			UpgradeReadyTroops(party.Party);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private TroopUpgradeArgs SelectPossibleUpgrade(List<TroopUpgradeArgs> possibleUpgrades)
	{
		TroopUpgradeArgs result = possibleUpgrades[0];
		if (possibleUpgrades.Count > 1)
		{
			float num = 0f;
			foreach (TroopUpgradeArgs possibleUpgrade in possibleUpgrades)
			{
				num += possibleUpgrade.UpgradeChance;
			}
			float num2 = num * MBRandom.RandomFloat;
			foreach (TroopUpgradeArgs possibleUpgrade2 in possibleUpgrades)
			{
				num2 -= possibleUpgrade2.UpgradeChance;
				if (num2 <= 0f)
				{
					result = possibleUpgrade2;
					break;
				}
			}
		}
		return result;
	}

	private List<TroopUpgradeArgs> GetPossibleUpgradeTargets(PartyBase party, TroopRosterElement element)
	{
		PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
		List<TroopUpgradeArgs> list = new List<TroopUpgradeArgs>();
		CharacterObject character = element.Character;
		int num = element.Number - element.WoundedNumber;
		if (num > 0)
		{
			PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				num = element.Number - element.WoundedNumber;
				CharacterObject characterObject = character.UpgradeTargets[i];
				int upgradeXpCost = character.GetUpgradeXpCost(party, i);
				if (upgradeXpCost > 0)
				{
					num = MathF.Min(num, element.Xp / upgradeXpCost);
					if (num == 0)
					{
						continue;
					}
				}
				if (characterObject.Tier > character.Tier && party.MobileParty.HasLimitedWage() && party.MobileParty.TotalWage + num * (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character)) > party.MobileParty.PaymentLimit)
				{
					num = MathF.Max(0, MathF.Min(num, (party.MobileParty.PaymentLimit - party.MobileParty.TotalWage) / (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character))));
					if (num == 0)
					{
						continue;
					}
				}
				int upgradeGoldCost = character.GetUpgradeGoldCost(party, i);
				if (party.LeaderHero != null && upgradeGoldCost != 0 && num * upgradeGoldCost > party.MobileParty.PartyTradeGold)
				{
					num = party.MobileParty.PartyTradeGold / upgradeGoldCost;
					if (num == 0)
					{
						continue;
					}
				}
				if ((!party.Culture.IsBandit || characterObject.Culture.IsBandit) && (character.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, character, characterObject)))
				{
					float upgradeChanceForTroopUpgrade = Campaign.Current.Models.PartyTroopUpgradeModel.GetUpgradeChanceForTroopUpgrade(party, character, i);
					list.Add(new TroopUpgradeArgs(character, characterObject, num, upgradeGoldCost, upgradeXpCost, upgradeChanceForTroopUpgrade));
				}
			}
		}
		return list;
	}

	private void ApplyEffects(PartyBase party, TroopUpgradeArgs upgradeArgs)
	{
		if (party.Owner != null && party.Owner.IsAlive)
		{
			SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
			GiveGoldAction.ApplyBetweenCharacters(party.Owner, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, disableNotification: true);
		}
		else if (party.LeaderHero != null && party.LeaderHero.IsAlive)
		{
			SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
			GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, disableNotification: true);
		}
	}

	private void UpgradeTroop(PartyBase party, int rosterIndex, TroopUpgradeArgs upgradeArgs)
	{
		TroopRoster memberRoster = party.MemberRoster;
		CharacterObject upgradeTarget = upgradeArgs.UpgradeTarget;
		int possibleUpgradeCount = upgradeArgs.PossibleUpgradeCount;
		int num = upgradeArgs.UpgradeXpCost * possibleUpgradeCount;
		memberRoster.SetElementXp(rosterIndex, memberRoster.GetElementXp(rosterIndex) - num);
		memberRoster.AddToCounts(upgradeArgs.Target, -possibleUpgradeCount);
		memberRoster.AddToCounts(upgradeTarget, possibleUpgradeCount);
		if (possibleUpgradeCount > 0)
		{
			ApplyEffects(party, upgradeArgs);
		}
	}

	public void UpgradeReadyTroops(PartyBase party)
	{
		if (party == PartyBase.MainParty || !party.IsActive)
		{
			return;
		}
		TroopRoster memberRoster = party.MemberRoster;
		PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
			if (partyTroopUpgradeModel.IsTroopUpgradeable(party, elementCopyAtIndex.Character))
			{
				List<TroopUpgradeArgs> possibleUpgradeTargets = GetPossibleUpgradeTargets(party, elementCopyAtIndex);
				if (possibleUpgradeTargets.Count > 0)
				{
					TroopUpgradeArgs upgradeArgs = SelectPossibleUpgrade(possibleUpgradeTargets);
					UpgradeTroop(party, i, upgradeArgs);
				}
			}
		}
	}
}
