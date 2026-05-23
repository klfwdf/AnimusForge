using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000428 RID: 1064
	public class PartyUpgraderCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004327 RID: 17191 RVA: 0x00144D80 File Offset: 0x00142F80
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.MapEventEnded));
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DailyTickParty));
		}

		// Token: 0x06004328 RID: 17192 RVA: 0x00144DB0 File Offset: 0x00142FB0
		private void MapEventEnded(MapEvent mapEvent)
		{
			foreach (PartyBase party in mapEvent.InvolvedParties)
			{
				this.UpgradeReadyTroops(party);
			}
		}

		// Token: 0x06004329 RID: 17193 RVA: 0x00144E00 File Offset: 0x00143000
		public void DailyTickParty(MobileParty party)
		{
			if (party.MapEvent == null)
			{
				this.UpgradeReadyTroops(party.Party);
			}
		}

		// Token: 0x0600432A RID: 17194 RVA: 0x00144E16 File Offset: 0x00143016
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600432B RID: 17195 RVA: 0x00144E18 File Offset: 0x00143018
		private PartyUpgraderCampaignBehavior.TroopUpgradeArgs SelectPossibleUpgrade(List<PartyUpgraderCampaignBehavior.TroopUpgradeArgs> possibleUpgrades)
		{
			PartyUpgraderCampaignBehavior.TroopUpgradeArgs result = possibleUpgrades[0];
			if (possibleUpgrades.Count > 1)
			{
				float num = 0f;
				foreach (PartyUpgraderCampaignBehavior.TroopUpgradeArgs troopUpgradeArgs in possibleUpgrades)
				{
					num += troopUpgradeArgs.UpgradeChance;
				}
				float num2 = num * MBRandom.RandomFloat;
				foreach (PartyUpgraderCampaignBehavior.TroopUpgradeArgs troopUpgradeArgs2 in possibleUpgrades)
				{
					num2 -= troopUpgradeArgs2.UpgradeChance;
					if (num2 <= 0f)
					{
						result = troopUpgradeArgs2;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x0600432C RID: 17196 RVA: 0x00144EDC File Offset: 0x001430DC
		private List<PartyUpgraderCampaignBehavior.TroopUpgradeArgs> GetPossibleUpgradeTargets(PartyBase party, TroopRosterElement element)
		{
			PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
			List<PartyUpgraderCampaignBehavior.TroopUpgradeArgs> list = new List<PartyUpgraderCampaignBehavior.TroopUpgradeArgs>();
			CharacterObject character = element.Character;
			int num = element.Number - element.WoundedNumber;
			if (num > 0)
			{
				PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
				int i = 0;
				while (i < character.UpgradeTargets.Length)
				{
					num = element.Number - element.WoundedNumber;
					CharacterObject characterObject = character.UpgradeTargets[i];
					int upgradeXpCost = character.GetUpgradeXpCost(party, i);
					if (upgradeXpCost <= 0)
					{
						goto IL_8F;
					}
					num = MathF.Min(num, element.Xp / upgradeXpCost);
					if (num != 0)
					{
						goto IL_8F;
					}
					IL_1AC:
					i++;
					continue;
					IL_8F:
					if (characterObject.Tier > character.Tier && party.MobileParty.HasLimitedWage() && party.MobileParty.TotalWage + num * (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character)) > party.MobileParty.PaymentLimit)
					{
						num = MathF.Max(0, MathF.Min(num, (party.MobileParty.PaymentLimit - party.MobileParty.TotalWage) / (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character))));
						if (num == 0)
						{
							goto IL_1AC;
						}
					}
					int upgradeGoldCost = character.GetUpgradeGoldCost(party, i);
					if (party.LeaderHero != null && upgradeGoldCost != 0 && num * upgradeGoldCost > party.MobileParty.PartyTradeGold)
					{
						num = party.MobileParty.PartyTradeGold / upgradeGoldCost;
						if (num == 0)
						{
							goto IL_1AC;
						}
					}
					if ((!party.Culture.IsBandit || characterObject.Culture.IsBandit) && (character.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, character, characterObject)))
					{
						float upgradeChanceForTroopUpgrade = Campaign.Current.Models.PartyTroopUpgradeModel.GetUpgradeChanceForTroopUpgrade(party, character, i);
						list.Add(new PartyUpgraderCampaignBehavior.TroopUpgradeArgs(character, characterObject, num, upgradeGoldCost, upgradeXpCost, upgradeChanceForTroopUpgrade));
						goto IL_1AC;
					}
					goto IL_1AC;
				}
			}
			return list;
		}

		// Token: 0x0600432D RID: 17197 RVA: 0x001450AC File Offset: 0x001432AC
		private void ApplyEffects(PartyBase party, PartyUpgraderCampaignBehavior.TroopUpgradeArgs upgradeArgs)
		{
			if (party.Owner != null && party.Owner.IsAlive)
			{
				SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
				GiveGoldAction.ApplyBetweenCharacters(party.Owner, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
				return;
			}
			if (party.LeaderHero != null && party.LeaderHero.IsAlive)
			{
				SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
				GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
			}
		}

		// Token: 0x0600432E RID: 17198 RVA: 0x00145148 File Offset: 0x00143348
		private void UpgradeTroop(PartyBase party, int rosterIndex, PartyUpgraderCampaignBehavior.TroopUpgradeArgs upgradeArgs)
		{
			TroopRoster memberRoster = party.MemberRoster;
			CharacterObject upgradeTarget = upgradeArgs.UpgradeTarget;
			int possibleUpgradeCount = upgradeArgs.PossibleUpgradeCount;
			int num = upgradeArgs.UpgradeXpCost * possibleUpgradeCount;
			memberRoster.SetElementXp(rosterIndex, memberRoster.GetElementXp(rosterIndex) - num);
			memberRoster.AddToCounts(upgradeArgs.Target, -possibleUpgradeCount, false, 0, 0, true, -1);
			memberRoster.AddToCounts(upgradeTarget, possibleUpgradeCount, false, 0, 0, true, -1);
			if (possibleUpgradeCount > 0)
			{
				this.ApplyEffects(party, upgradeArgs);
			}
		}

		// Token: 0x0600432F RID: 17199 RVA: 0x001451B4 File Offset: 0x001433B4
		public void UpgradeReadyTroops(PartyBase party)
		{
			if (party != PartyBase.MainParty && party.IsActive)
			{
				TroopRoster memberRoster = party.MemberRoster;
				PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
				for (int i = 0; i < memberRoster.Count; i++)
				{
					TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
					if (partyTroopUpgradeModel.IsTroopUpgradeable(party, elementCopyAtIndex.Character))
					{
						List<PartyUpgraderCampaignBehavior.TroopUpgradeArgs> possibleUpgradeTargets = this.GetPossibleUpgradeTargets(party, elementCopyAtIndex);
						if (possibleUpgradeTargets.Count > 0)
						{
							PartyUpgraderCampaignBehavior.TroopUpgradeArgs upgradeArgs = this.SelectPossibleUpgrade(possibleUpgradeTargets);
							this.UpgradeTroop(party, i, upgradeArgs);
						}
					}
				}
			}
		}

		// Token: 0x02000828 RID: 2088
		private readonly struct TroopUpgradeArgs
		{
			// Token: 0x06006656 RID: 26198 RVA: 0x001C2637 File Offset: 0x001C0837
			public TroopUpgradeArgs(CharacterObject target, CharacterObject upgradeTarget, int possibleUpgradeCount, int upgradeGoldCost, int upgradeXpCost, float upgradeChance)
			{
				this.Target = target;
				this.UpgradeTarget = upgradeTarget;
				this.PossibleUpgradeCount = possibleUpgradeCount;
				this.UpgradeGoldCost = upgradeGoldCost;
				this.UpgradeXpCost = upgradeXpCost;
				this.UpgradeChance = upgradeChance;
			}

			// Token: 0x040022A6 RID: 8870
			public readonly CharacterObject Target;

			// Token: 0x040022A7 RID: 8871
			public readonly CharacterObject UpgradeTarget;

			// Token: 0x040022A8 RID: 8872
			public readonly int PossibleUpgradeCount;

			// Token: 0x040022A9 RID: 8873
			public readonly int UpgradeGoldCost;

			// Token: 0x040022AA RID: 8874
			public readonly int UpgradeXpCost;

			// Token: 0x040022AB RID: 8875
			public readonly float UpgradeChance;
		}
	}
}
