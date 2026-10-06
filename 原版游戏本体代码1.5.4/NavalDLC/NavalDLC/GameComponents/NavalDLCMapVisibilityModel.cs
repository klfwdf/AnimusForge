using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCMapVisibilityModel : MapVisibilityModel
{
	private const float SeaSpottingRangeBonus = 0.3f;

	private const float StormSpottingRangePenalty = -0.4f;

	public override float MaximumSeeingRange()
	{
		return base.BaseModel.MaximumSeeingRange();
	}

	public override float GetPartySeeingRangeBase(MobileParty party)
	{
		float num = base.BaseModel.GetPartySeeingRangeBase(party);
		if (party.IsCurrentlyAtSea)
		{
			if (party.IsInNavalAutoTravel)
			{
				num *= 0.5f;
			}
			Hero perkOwnerHero = null;
			if (Campaign.Current.IsNight && party.HasPerk(NavalPerks.Shipmaster.NightRaider, out perkOwnerHero))
			{
				num += 3f;
			}
		}
		return num;
	}

	public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
	{
		ExplainedNumber stat = base.BaseModel.GetPartySpottingRange(party, includeDescriptions);
		if (party.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.RavenEye, party, isPrimaryBonus: true, ref stat);
			stat.AddFactor(0.3f, new TextObject("{=B0aCb3Je}At Sea"));
			foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
			{
				if (spawnedStorm.IsActive && spawnedStorm.CurrentPosition.DistanceSquared(party.Position.ToVec2()) < spawnedStorm.EffectRadius * spawnedStorm.EffectRadius)
				{
					stat.AddFactor(-0.4f, new TextObject("{=M6V6eCTg}Storm"));
					break;
				}
			}
		}
		return stat;
	}

	public override float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party)
	{
		return base.BaseModel.GetPartySpottingRatioForMainPartySeeingRange(party);
	}

	public override float GetHideoutSpottingDistance()
	{
		return base.BaseModel.GetHideoutSpottingDistance();
	}

	public override void GetMobilePartyVisibilityAndInspectedState(MobileParty mobileParty, Vec2[] points, float seeingRange, out bool isVisible, out bool isInspected)
	{
		base.BaseModel.GetMobilePartyVisibilityAndInspectedState(mobileParty, points, seeingRange, out isVisible, out isInspected);
	}

	public override void GetSettlementInspectedState(Settlement settlement, Vec2[] points, float seeingRange, out bool isInspected, out bool isDistanceDependent)
	{
		base.BaseModel.GetSettlementInspectedState(settlement, points, seeingRange, out isInspected, out isDistanceDependent);
	}

	public override bool IsVisibilityPersistent(MobileParty mobileParty)
	{
		return base.BaseModel.IsVisibilityPersistent(mobileParty);
	}
}
