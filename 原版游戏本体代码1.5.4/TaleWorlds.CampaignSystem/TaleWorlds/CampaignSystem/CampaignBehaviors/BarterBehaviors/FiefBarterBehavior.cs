using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;

public class FiefBarterBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, CheckForBarters);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void CheckForBarters(BarterData args)
	{
		if (args.OffererHero == null || args.OtherHero == null || !args.OffererHero.GetPerkValue(DefaultPerks.Trade.EverythingHasAPrice) || (args.OtherHero.Clan.IsMinorFaction && args.OtherHero.Clan != Clan.PlayerClan) || args.OtherHero.Clan.IsUnderMercenaryService || args.OffererHero.Clan.IsUnderMercenaryService)
		{
			return;
		}
		foreach (Town allFief in Town.AllFiefs)
		{
			if (allFief.OwnerClan?.Leader == args.OffererHero)
			{
				Barterable barterable = new FiefBarterable(allFief.Settlement, args.OffererHero, args.OtherHero);
				args.AddBarterable<FiefBarterGroup>(barterable);
			}
			else if (allFief.OwnerClan?.Leader == args.OtherHero)
			{
				Barterable barterable2 = new FiefBarterable(allFief.Settlement, args.OtherHero, args.OffererHero);
				args.AddBarterable<FiefBarterGroup>(barterable2);
			}
		}
	}
}
