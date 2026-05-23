using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000140 RID: 320
	public class DefaultPrisonBreakModel : PrisonBreakModel
	{
		// Token: 0x0600196D RID: 6509 RVA: 0x0007EE58 File Offset: 0x0007D058
		public override int GetNumberOfGuardsToSpawn(Settlement settlement)
		{
			int num = (int)Math.Ceiling((double)(2f + settlement.Town.Security / 30f));
			int num2 = settlement.Town.GetWallLevel() - 1;
			return num + num2;
		}

		// Token: 0x0600196E RID: 6510 RVA: 0x0007EE94 File Offset: 0x0007D094
		public override bool CanPlayerStagePrisonBreak(Settlement settlement)
		{
			bool result = false;
			if (settlement.IsFortification)
			{
				MobileParty garrisonParty = settlement.Town.GarrisonParty;
				bool flag = (garrisonParty != null && garrisonParty.PrisonRoster.TotalHeroes > 0) || settlement.Party.PrisonRoster.TotalHeroes > 0;
				result = settlement.MapFaction != Clan.PlayerClan.MapFaction && !DiplomacyHelper.IsSameFactionAndNotEliminated(settlement.MapFaction, Clan.PlayerClan.MapFaction) && flag;
			}
			return result;
		}

		// Token: 0x0600196F RID: 6511 RVA: 0x0007EF14 File Offset: 0x0007D114
		public override int GetPrisonBreakStartCost(Hero prisonerHero)
		{
			int num = MathF.Ceiling((float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisonerHero.CharacterObject, null) / 2000f * prisonerHero.CurrentSettlement.Town.Security * 40f - (float)(Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) * 10));
			num = ((num < 100) ? 0 : (num / 100 * 100));
			return num + 1000;
		}

		// Token: 0x06001970 RID: 6512 RVA: 0x0007EF8B File Offset: 0x0007D18B
		public override int GetRelationRewardOnPrisonBreak(Hero prisonerHero)
		{
			return 15;
		}

		// Token: 0x06001971 RID: 6513 RVA: 0x0007EF8F File Offset: 0x0007D18F
		public override float GetRogueryRewardOnPrisonBreak(Hero prisonerHero, bool isSuccess)
		{
			return (float)(isSuccess ? MBRandom.RandomInt(2000, 4500) : MBRandom.RandomInt(500, 1000));
		}

		// Token: 0x0400086A RID: 2154
		private const int BasePrisonBreakCost = 1000;
	}
}
