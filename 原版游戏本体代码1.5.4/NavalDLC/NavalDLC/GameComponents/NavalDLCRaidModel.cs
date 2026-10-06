using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCRaidModel : RaidModel
{
	public override int GoldRewardForEachLostHearth => base.BaseModel.GoldRewardForEachLostHearth;

	public override ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
	{
		ExplainedNumber result = base.BaseModel.CalculateHitDamage(attackerSide, settlementHitPoints);
		int num = 0;
		foreach (MapEventParty party2 in attackerSide.Parties)
		{
			num += party2.Party.MemberRoster.TotalManCount;
		}
		if (num > 0)
		{
			foreach (MapEventParty party3 in attackerSide.Parties)
			{
				PartyBase party = party3.Party;
				int totalManCount = party.MemberRoster.TotalManCount;
				if (totalManCount <= 0)
				{
					continue;
				}
				float num2 = (float)totalManCount / (float)num;
				if (PartyBaseHelper.HasFeat(party, NavalCulturalFeats.NordHostileActionSpeedFeat))
				{
					result.AddFactor(NavalCulturalFeats.NordHostileActionSpeedFeat.EffectBonus * num2);
				}
				if (party.MobileParty != null && party.MobileParty.IsCurrentlyAtSea)
				{
					ExplainedNumber stat = new ExplainedNumber(0f, includeDescriptions: false, null);
					PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.Forceful, party.MobileParty, isPrimaryBonus: false, ref stat);
					if (stat.ResultNumber != 0f)
					{
						result.AddFactor(stat.ResultNumber * num2);
					}
				}
			}
		}
		return result;
	}

	public override float GetRaidLootMultiplier(PartyBase receivingParty)
	{
		float num = base.BaseModel.GetRaidLootMultiplier(receivingParty);
		if (receivingParty != null && receivingParty.IsMobile && receivingParty.MobileParty.IsCurrentlyAtSea)
		{
			num += NavalPerks.Mariner.BruteForce.PrimaryBonus;
		}
		return num;
	}

	public override MBReadOnlyList<(ItemObject, float)> GetCommonLootItemScores()
	{
		return base.BaseModel.GetCommonLootItemScores();
	}
}
