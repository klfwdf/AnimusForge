using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000132 RID: 306
	public class DefaultPartyImpairmentModel : PartyImpairmentModel
	{
		// Token: 0x060018EE RID: 6382 RVA: 0x0007ABD4 File Offset: 0x00078DD4
		public override float GetSiegeExpectedVulnerabilityTime()
		{
			float num = ((float)CampaignTime.SunRise + MBRandom.RandomFloatNormal + (float)CampaignTime.HoursInDay - CampaignTime.Now.CurrentHourInDay) % (float)CampaignTime.HoursInDay;
			float num2 = MathF.Pow(MBRandom.RandomFloat, 6f);
			return (((MBRandom.RandomFloatNormal > 0f) ? num2 : (1f - num2)) * (float)CampaignTime.HoursInDay + num) % (float)CampaignTime.HoursInDay;
		}

		// Token: 0x060018EF RID: 6383 RVA: 0x0007AC40 File Offset: 0x00078E40
		public override ExplainedNumber GetDisorganizedStateDuration(MobileParty party)
		{
			ExplainedNumber result = new ExplainedNumber(6f, false, null);
			bool flag = party.MapEvent != null && (party.MapEvent.IsRaid || party.MapEvent.IsSiegeAssault);
			if (!party.IsCurrentlyAtSea && flag && party.HasPerk(DefaultPerks.Tactics.SwiftRegroup, false))
			{
				result.AddFactor(DefaultPerks.Tactics.SwiftRegroup.PrimaryBonus, DefaultPerks.Tactics.SwiftRegroup.Description);
			}
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Foragers, party, false, ref result, party.IsCurrentlyAtSea);
			return result;
		}

		// Token: 0x060018F0 RID: 6384 RVA: 0x0007ACD0 File Offset: 0x00078ED0
		public override bool CanGetDisorganized(PartyBase party)
		{
			return party.IsActive && party.IsMobile && party.MobileParty.MemberRoster.TotalManCount >= 10 && (party.MobileParty.Army == null || party.MobileParty == party.MobileParty.Army.LeaderParty || party.MobileParty.AttachedTo != null);
		}

		// Token: 0x060018F1 RID: 6385 RVA: 0x0007AD38 File Offset: 0x00078F38
		public override float GetVulnerabilityStateDuration(PartyBase party)
		{
			return MBRandom.RandomFloatNormal + 4f;
		}

		// Token: 0x04000817 RID: 2071
		private const float BaseDisorganizedStateDuration = 6f;

		// Token: 0x04000818 RID: 2072
		private static readonly TextObject _settlementInvolvedMapEvent = new TextObject("{=KVlPhPSD}Settlement involved map event", null);
	}
}
