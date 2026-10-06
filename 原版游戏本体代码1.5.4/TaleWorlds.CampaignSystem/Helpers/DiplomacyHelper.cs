using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public static class DiplomacyHelper
{
	public static bool IsWarCausedByPlayer(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		switch (declareWarDetail)
		{
		case DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility:
			return true;
		case DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision:
			if (faction1 == Hero.MainHero.MapFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero)
			{
				return true;
			}
			return false;
		case DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange:
			if (faction2 == Hero.MainHero.MapFaction && faction1.MainHeroCrimeRating > Campaign.Current.Models.CrimeModel.DeclareWarCrimeRatingThreshold)
			{
				return true;
			}
			return false;
		case DeclareWarAction.DeclareWarDetail.CausedByKingdomCreation:
			if (faction1 == Hero.MainHero.MapFaction)
			{
				return true;
			}
			return false;
		default:
			return false;
		}
	}

	public static bool IsSameFactionAndNotEliminated(IFaction faction1, IFaction faction2)
	{
		if (faction1 != null && faction2 != null && faction1 == faction2 && !faction1.IsEliminated)
		{
			return !faction2.IsEliminated;
		}
		return false;
	}

	private static bool IsLogInTimeRange(LogEntry entry, CampaignTime time)
	{
		return entry.GameTime.NumTicks >= time.NumTicks;
	}

	public static List<(LogEntry, IFaction, IFaction)> GetLogsForWar(StanceLink stance)
	{
		CampaignTime warStartDate = stance.WarStartDate;
		List<(LogEntry, IFaction, IFaction)> list = new List<(LogEntry, IFaction, IFaction)>();
		for (int num = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; num >= 0; num--)
		{
			LogEntry logEntry = Campaign.Current.LogEntryHistory.GameActionLogs[num];
			if (IsLogInTimeRange(logEntry, warStartDate) && logEntry is IWarLog warLog && warLog.IsRelatedToWar(stance, out var effector, out var effected))
			{
				list.Add((logEntry, effector, effected));
			}
		}
		return list;
	}

	public static List<Hero> GetPrisonersOfWarTakenByFaction(IFaction capturerFaction, IFaction prisonerFaction)
	{
		List<Hero> list = new List<Hero>();
		foreach (Hero aliveLord in prisonerFaction.AliveLords)
		{
			if (aliveLord.IsPrisoner && aliveLord.PartyBelongedToAsPrisoner?.MapFaction == capturerFaction)
			{
				list.Add(aliveLord);
			}
		}
		return list;
	}

	public static bool DidMainHeroSwornNotToAttackFaction(IFaction faction, out TextObject explanation)
	{
		if (faction.NotAttackableByPlayerUntilTime.IsFuture)
		{
			explanation = GameTexts.FindText("str_enemy_not_attackable_tooltip");
			return true;
		}
		explanation = null;
		return false;
	}

	public static bool HasAllianceWithFaction(IFaction faction1, IFaction faction2)
	{
		if (faction1 == null || faction2 == null || faction1 == faction2 || faction1.IsEliminated || faction2.IsEliminated || !faction1.IsKingdomFaction || !faction2.IsKingdomFaction)
		{
			return false;
		}
		Kingdom obj = (Kingdom)faction1;
		Kingdom other = (Kingdom)faction2;
		return obj.IsAllyWith(other);
	}
}
