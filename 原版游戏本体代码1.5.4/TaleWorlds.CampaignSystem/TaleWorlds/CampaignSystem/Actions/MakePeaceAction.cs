using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class MakePeaceAction
{
	public enum MakePeaceDetail
	{
		Default,
		ByKingdomDecision
	}

	private const float DefaultValueForBeingLimitedAfterPeace = 100000f;

	private static void ApplyInternal(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration, MakePeaceDetail detail = MakePeaceDetail.Default)
	{
		StanceLink stanceWith = faction1.GetStanceWith(faction2);
		FactionManager.SetNeutral(faction1, faction2);
		stanceWith.SetDailyTributePaid(faction1, dailyTributeFrom1To2, dailyTributeDuration);
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
		CampaignEventDispatcher.Instance.OnMakePeace(faction1, faction2, detail);
	}

	public static void Apply(IFaction faction1, IFaction faction2)
	{
		ApplyInternal(faction1, faction2, 0, 0);
	}

	public static void ApplyByKingdomDecision(IFaction faction1, IFaction faction2, int dailyTributeFrom1To2, int dailyTributeDuration)
	{
		ApplyInternal(faction1, faction2, dailyTributeFrom1To2, dailyTributeDuration, MakePeaceDetail.ByKingdomDecision);
	}
}
