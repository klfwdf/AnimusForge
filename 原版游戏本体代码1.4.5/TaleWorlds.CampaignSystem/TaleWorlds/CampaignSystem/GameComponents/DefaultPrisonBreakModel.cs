using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPrisonBreakModel : PrisonBreakModel
{
	private const int BasePrisonBreakCost = 1000;

	public override int GetNumberOfGuardsToSpawn(Settlement settlement)
	{
		int num = (int)Math.Ceiling(2f + settlement.Town.Security / 30f);
		int num2 = settlement.Town.GetWallLevel() - 1;
		return num + num2;
	}

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

	public override int GetPrisonBreakStartCost(Hero prisonerHero)
	{
		int num = TaleWorlds.Library.MathF.Ceiling((float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisonerHero.CharacterObject) / 2000f * prisonerHero.CurrentSettlement.Town.Security * 40f - (float)(Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) * 10));
		num = ((num >= 100) ? (num / 100 * 100) : 0);
		return num + 1000;
	}

	public override int GetRelationRewardOnPrisonBreak(Hero prisonerHero)
	{
		return 15;
	}

	public override float GetRogueryRewardOnPrisonBreak(Hero prisonerHero, bool isSuccess)
	{
		return isSuccess ? MBRandom.RandomInt(2000, 4500) : MBRandom.RandomInt(500, 1000);
	}
}
