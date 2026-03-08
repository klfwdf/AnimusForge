using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200012F RID: 303
	public class DefaultPartyDesertionModel : PartyDesertionModel
	{
		// Token: 0x060018D6 RID: 6358 RVA: 0x00079C3B File Offset: 0x00077E3B
		public override int GetMoraleThresholdForTroopDesertion()
		{
			return 10;
		}

		// Token: 0x060018D7 RID: 6359 RVA: 0x00079C3F File Offset: 0x00077E3F
		public override float GetDesertionChanceForTroop(MobileParty mobileParty, in TroopRosterElement troopRosterElement)
		{
			return this.CalculateDesertionChanceFromTroopLevel(mobileParty.Morale, troopRosterElement.Character.Level);
		}

		// Token: 0x060018D8 RID: 6360 RVA: 0x00079C58 File Offset: 0x00077E58
		private float CalculateDesertionChanceFromTroopLevel(float partyMorale, int level)
		{
			int moraleThresholdForTroopDesertion = Campaign.Current.Models.PartyDesertionModel.GetMoraleThresholdForTroopDesertion();
			float num = ((partyMorale > (float)moraleThresholdForTroopDesertion) ? ((float)moraleThresholdForTroopDesertion) : partyMorale);
			return 1f - MathF.Pow((float)level * 0.01f, 0.1f * (((float)moraleThresholdForTroopDesertion - num) / (float)moraleThresholdForTroopDesertion));
		}

		// Token: 0x060018D9 RID: 6361 RVA: 0x00079CA8 File Offset: 0x00077EA8
		public override TroopRoster GetTroopsToDesert(MobileParty mobileParty)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			this.GetTroopsToDesertDueToMorale(mobileParty, troopRoster);
			this.GetTroopsToDesertDueToWageAndPartySize(mobileParty, troopRoster);
			return troopRoster;
		}

		// Token: 0x060018DA RID: 6362 RVA: 0x00079CCC File Offset: 0x00077ECC
		private void GetTroopsToDesertDueToMorale(MobileParty mobileParty, TroopRoster troopsToDesert)
		{
			int num = (int)((float)mobileParty.Party.NumberOfRegularMembers * this.CalculateDesertionChanceFromTroopLevel(mobileParty.Morale, 20));
			if (num <= 0)
			{
				return;
			}
			this.SelectTroopsForDesertion(mobileParty, troopsToDesert, num, true);
		}

		// Token: 0x060018DB RID: 6363 RVA: 0x00079D08 File Offset: 0x00077F08
		private void GetTroopsToDesertDueToWageAndPartySize(MobileParty mobileParty, TroopRoster troopsToDesert)
		{
			int a = 0;
			int b = 0;
			int num = mobileParty.Party.NumberOfAllMembers - troopsToDesert.TotalManCount - mobileParty.Party.PartySizeLimit;
			float resultNumber = Campaign.Current.Models.PartyWageModel.GetTotalWage(mobileParty, troopsToDesert, false).ResultNumber;
			float num2 = (float)mobileParty.TotalWage - resultNumber;
			if (mobileParty.HasLimitedWage() && (float)mobileParty.PaymentLimit < num2)
			{
				int num3 = mobileParty.TotalWage - mobileParty.PaymentLimit;
				a = MathF.Min(20, MathF.Max(1, (int)((float)num3 / Campaign.Current.AverageWage * 0.25f)));
			}
			if (num > 0)
			{
				b = MathF.Max(1, (int)((float)num * 0.25f));
			}
			int num4 = MathF.Max(a, b);
			if (mobileParty.IsGarrison && mobileParty.HasUnpaidWages > 0f)
			{
				num4 += MathF.Min(mobileParty.Party.NumberOfHealthyMembers, 5);
			}
			num4 = MathF.Min(num4, mobileParty.MemberRoster.TotalRegulars);
			if (num4 <= 0)
			{
				return;
			}
			this.SelectTroopsForDesertion(mobileParty, troopsToDesert, num4, false);
		}

		// Token: 0x060018DC RID: 6364 RVA: 0x00079E1C File Offset: 0x0007801C
		private void SelectTroopsForDesertion(MobileParty mobileParty, TroopRoster troopsToDesert, int maxDesertionCount, bool useProbability)
		{
			int num = 0;
			int num2 = mobileParty.MemberRoster.Count - 1;
			while (num2 >= 0 && num < maxDesertionCount)
			{
				TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(num2);
				if (elementCopyAtIndex.Character.HeroObject == null)
				{
					int num3 = 0;
					int num4 = 0;
					float num5 = (useProbability ? this.GetDesertionChanceForTroop(mobileParty, elementCopyAtIndex) : 1f);
					int troopCount = troopsToDesert.GetTroopCount(elementCopyAtIndex.Character);
					int num6 = 0;
					while (num6 < elementCopyAtIndex.WoundedNumber - troopCount && num + num4 < maxDesertionCount)
					{
						if (!useProbability || num5 > mobileParty.RandomFloatWithSeed((uint)(CampaignTime.Now.ToHours + (double)(num2 * 100 + num6))))
						{
							num4++;
						}
						num6++;
					}
					int num7 = 0;
					while (num7 < elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber - troopCount && num + num4 + num3 < maxDesertionCount)
					{
						if (!useProbability || num5 > mobileParty.RandomFloatWithSeed((uint)(CampaignTime.Now.ToHours + (double)(num2 * 100 + num7))))
						{
							num3++;
						}
						num7++;
					}
					if (num3 != 0 || num4 != 0)
					{
						int num8 = num3 + num4;
						troopsToDesert.AddToCounts(elementCopyAtIndex.Character, num8, false, num4, 0, true, -1);
						num += num8;
					}
				}
				num2--;
			}
		}

		// Token: 0x04000805 RID: 2053
		private const int MaxAcceptableDesertionCountForNormal = 20;

		// Token: 0x04000806 RID: 2054
		private const int MoraleThresholdForParty = 10;

		// Token: 0x04000807 RID: 2055
		private const int AverageTroopLevel = 20;
	}
}
