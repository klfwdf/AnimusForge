using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DeclareWarAction
{
	public enum DeclareWarDetail
	{
		Default,
		CausedByPlayerHostility,
		CausedByKingdomDecision,
		CausedByRebellion,
		CausedByCrimeRatingChange,
		CausedByKingdomCreation,
		CausedByClaimOnThrone,
		CausedByCallToWarAgreement
	}

	private static void ApplyInternal(IFaction faction1, IFaction faction2, DeclareWarDetail declareWarDetail)
	{
		FactionManager.DeclareWar(faction1, faction2);
		if (faction1.IsKingdomFaction && (float)faction2.Fiefs.Count > 1f + (float)faction1.Fiefs.Count * 0.2f)
		{
			Kingdom kingdom = (Kingdom)faction1;
			kingdom.PoliticalStagnation = (int)((float)kingdom.PoliticalStagnation * 0.85f - 3f);
			if (kingdom.PoliticalStagnation < 0)
			{
				kingdom.PoliticalStagnation = 0;
			}
		}
		if (faction2.IsKingdomFaction && (float)faction1.Fiefs.Count > 1f + (float)faction2.Fiefs.Count * 0.2f)
		{
			Kingdom kingdom2 = (Kingdom)faction2;
			kingdom2.PoliticalStagnation = (int)((float)kingdom2.PoliticalStagnation * 0.85f - 3f);
			if (kingdom2.PoliticalStagnation < 0)
			{
				kingdom2.PoliticalStagnation = 0;
			}
		}
		if (faction1 == Hero.MainHero.MapFaction || faction2 == Hero.MainHero.MapFaction)
		{
			IFaction dirtySide = ((faction1 == Hero.MainHero.MapFaction) ? faction2 : faction1);
			foreach (Settlement item in Settlement.All.Where((Settlement party) => party.IsVisible && party.MapFaction == dirtySide))
			{
				item.Party.SetVisualAsDirty();
			}
			foreach (MobileParty item2 in MobileParty.All.Where((MobileParty party) => party.IsVisible && party.MapFaction == dirtySide))
			{
				item2.Party.SetVisualAsDirty();
			}
		}
		CampaignEventDispatcher.Instance.OnWarDeclared(faction1, faction2, declareWarDetail);
	}

	public static void ApplyByKingdomDecision(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByKingdomDecision);
	}

	public static void ApplyByDefault(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.Default);
	}

	public static void ApplyByPlayerHostility(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByPlayerHostility);
	}

	public static void ApplyByRebellion(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByRebellion);
	}

	public static void ApplyByCrimeRatingChange(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByCrimeRatingChange);
	}

	public static void ApplyByKingdomCreation(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByKingdomCreation);
	}

	public static void ApplyByClaimOnThrone(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByClaimOnThrone);
	}

	public static void ApplyByCallToWarAgreement(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, DeclareWarDetail.CausedByCallToWarAgreement);
	}
}
