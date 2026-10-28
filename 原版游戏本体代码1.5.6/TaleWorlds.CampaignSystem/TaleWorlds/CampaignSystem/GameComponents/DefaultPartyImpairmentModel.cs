using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyImpairmentModel : PartyImpairmentModel
{
	private const float BaseDisorganizedStateDuration = 6f;

	private static readonly TextObject _settlementInvolvedMapEvent = new TextObject("{=KVlPhPSD}Settlement involved map event");

	public override float GetSiegeExpectedVulnerabilityTime()
	{
		float num = ((float)CampaignTime.SunRise + MBRandom.RandomFloatNormal + (float)CampaignTime.HoursInDay - CampaignTime.Now.CurrentHourInDay) % (float)CampaignTime.HoursInDay;
		float num2 = MathF.Pow(MBRandom.RandomFloat, 6f);
		return (((MBRandom.RandomFloatNormal > 0f) ? num2 : (1f - num2)) * (float)CampaignTime.HoursInDay + num) % (float)CampaignTime.HoursInDay;
	}

	public override ExplainedNumber GetDisorganizedStateDuration(MobileParty party)
	{
		ExplainedNumber stat = new ExplainedNumber(6f);
		if (party.MapEvent != null && (party.MapEvent.IsRaid || party.MapEvent.IsSiegeAssault))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.SwiftRegroup, party, isPrimaryBonus: true, ref stat);
		}
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Foragers, party, isPrimaryBonus: false, ref stat);
		Hero hero = party.Army?.LeaderParty?.LeaderHero ?? party.LeaderHero;
		if (hero != null)
		{
			TraitEffectHelper.ApplyTraitEffect(hero, DefaultPersonalityTraitEffects.CalculatingLongerDisorganizeEffect, ref stat);
		}
		return stat;
	}

	public override bool CanGetDisorganized(PartyBase party)
	{
		if (party.IsActive && party.IsMobile && party.MobileParty.MemberRoster.TotalManCount >= 10)
		{
			if (party.MobileParty.Army != null && party.MobileParty != party.MobileParty.Army.LeaderParty)
			{
				return party.MobileParty.AttachedTo != null;
			}
			return true;
		}
		return false;
	}

	public override float GetVulnerabilityStateDuration(PartyBase party)
	{
		return MBRandom.RandomFloatNormal + 4f;
	}
}
