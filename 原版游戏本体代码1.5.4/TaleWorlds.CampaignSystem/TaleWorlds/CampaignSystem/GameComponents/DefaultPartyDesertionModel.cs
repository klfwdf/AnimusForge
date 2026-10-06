using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyDesertionModel : PartyDesertionModel
{
	private const int MaxAcceptableDesertionCountForNormal = 20;

	private const int MoraleThresholdForParty = 10;

	private const int AverageTroopLevel = 20;

	public override int GetMoraleThresholdForTroopDesertion()
	{
		return 10;
	}

	public override float GetDesertionChanceForTroop(MobileParty mobileParty, in TroopRosterElement troopRosterElement)
	{
		return CalculateDesertionChanceFromTroopLevel(mobileParty.Morale, troopRosterElement.Character.Level);
	}

	private float CalculateDesertionChanceFromTroopLevel(float partyMorale, int level)
	{
		int moraleThresholdForTroopDesertion = Campaign.Current.Models.PartyDesertionModel.GetMoraleThresholdForTroopDesertion();
		float num = ((partyMorale > (float)moraleThresholdForTroopDesertion) ? ((float)moraleThresholdForTroopDesertion) : partyMorale);
		return 1f - MathF.Pow((float)level * 0.01f, 0.1f * (((float)moraleThresholdForTroopDesertion - num) / (float)moraleThresholdForTroopDesertion));
	}

	public override TroopRoster GetTroopsToDesert(MobileParty mobileParty)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		GetTroopsToDesertDueToMorale(mobileParty, troopRoster);
		GetTroopsToDesertDueToWageAndPartySize(mobileParty, troopRoster);
		return troopRoster;
	}

	private void GetTroopsToDesertDueToMorale(MobileParty mobileParty, TroopRoster troopsToDesert)
	{
		int num = (int)((float)mobileParty.Party.NumberOfRegularMembers * CalculateDesertionChanceFromTroopLevel(mobileParty.Morale, 20));
		if (num > 0)
		{
			SelectTroopsForDesertion(mobileParty, troopsToDesert, num, useProbability: true);
		}
	}

	private void GetTroopsToDesertDueToWageAndPartySize(MobileParty mobileParty, TroopRoster troopsToDesert)
	{
		int a = 0;
		int b = 0;
		int num = mobileParty.Party.NumberOfAllMembers - troopsToDesert.TotalManCount - mobileParty.Party.PartySizeLimit;
		float resultNumber = Campaign.Current.Models.PartyWageModel.GetTotalWage(mobileParty, troopsToDesert).ResultNumber;
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
		if (num4 > 0)
		{
			SelectTroopsForDesertion(mobileParty, troopsToDesert, num4, useProbability: false);
		}
	}

	private void SelectTroopsForDesertion(MobileParty mobileParty, TroopRoster troopsToDesert, int maxDesertionCount, bool useProbability)
	{
		int num = 0;
		int num2 = mobileParty.MemberRoster.Count - 1;
		while (num2 >= 0 && num < maxDesertionCount)
		{
			TroopRosterElement troopRosterElement = mobileParty.MemberRoster.GetElementCopyAtIndex(num2);
			if (troopRosterElement.Character.HeroObject == null)
			{
				int num3 = 0;
				int num4 = 0;
				float num5 = (useProbability ? GetDesertionChanceForTroop(mobileParty, in troopRosterElement) : 1f);
				int troopCount = troopsToDesert.GetTroopCount(troopRosterElement.Character);
				for (int i = 0; i < troopRosterElement.WoundedNumber - troopCount; i++)
				{
					if (num + num4 >= maxDesertionCount)
					{
						break;
					}
					if (!useProbability || num5 > mobileParty.RandomFloatWithSeed((uint)(CampaignTime.Now.ToHours + (double)(num2 * 100 + i))))
					{
						num4++;
					}
				}
				for (int j = 0; j < troopRosterElement.Number - troopRosterElement.WoundedNumber - troopCount; j++)
				{
					if (num + num4 + num3 >= maxDesertionCount)
					{
						break;
					}
					if (!useProbability || num5 > mobileParty.RandomFloatWithSeed((uint)(CampaignTime.Now.ToHours + (double)(num2 * 100 + j))))
					{
						num3++;
					}
				}
				if (num3 != 0 || num4 != 0)
				{
					int num6 = num3 + num4;
					troopsToDesert.AddToCounts(troopRosterElement.Character, num6, insertAtFront: false, num4);
					num += num6;
				}
			}
			num2--;
		}
	}
}
