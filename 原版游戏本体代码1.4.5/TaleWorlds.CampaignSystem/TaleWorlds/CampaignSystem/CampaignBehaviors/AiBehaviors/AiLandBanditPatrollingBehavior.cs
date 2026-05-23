using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiLandBanditPatrollingBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (!mobileParty.IsBandit || mobileParty.IsBanditBossParty || (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsHideout && mobileParty.CurrentSettlement.Parties.CountQ((MobileParty x) => x.IsBandit && !x.IsBanditBossParty) <= Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt + 1))
		{
			return;
		}
		MobileParty.NavigationType navigationType = MobileParty.NavigationType.Default;
		if (!mobileParty.HasLandNavigationCapability)
		{
			return;
		}
		AIBehaviorData item = new AIBehaviorData(mobileParty.HomeSettlement, AiBehavior.PatrolAroundPoint, navigationType, willGatherArmy: false, isFromPort: false, isTargetingPort: false);
		float num = 1f;
		if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsHideout && (mobileParty.CurrentSettlement.MapFaction == mobileParty.MapFaction || mobileParty.CurrentSettlement.Hideout.IsInfested))
		{
			int num2 = mobileParty.CurrentSettlement.Parties.CountQ((MobileParty x) => x.IsBandit && !x.IsBanditBossParty);
			int numberOfMinimumBanditPartiesInAHideoutToInfestIt = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;
			int numberOfMaximumBanditPartiesInEachHideout = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;
			num = (float)(num2 - numberOfMinimumBanditPartiesInAHideoutToInfestIt) / (float)(numberOfMaximumBanditPartiesInEachHideout - numberOfMinimumBanditPartiesInAHideoutToInfestIt);
		}
		float num3 = ((mobileParty.CurrentSettlement != null) ? (MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat) : 0.5f);
		float item2 = 0.5f * num * num3;
		if (num > 0f)
		{
			p.AddBehaviorScore((item, item2));
		}
	}
}
