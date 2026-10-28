using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;

public class SetPrisonerFreeBarterBehavior : CampaignBehaviorBase
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
		PartyBase offererParty = args.OffererParty;
		PartyBase otherParty = args.OtherParty;
		if (offererParty == null || otherParty == null)
		{
			return;
		}
		List<CharacterObject> list = offererParty.PrisonerHeroes.ToList();
		if (offererParty.LeaderHero != null && offererParty.LeaderHero == offererParty.LeaderHero?.Clan?.Leader)
		{
			list.AddRange(offererParty.LeaderHero.Clan.DungeonPrisonersOfClan);
		}
		foreach (CharacterObject item in list)
		{
			if (item.IsHero && !FactionManager.IsAtWarAgainstFaction(item.HeroObject.MapFaction, otherParty.MapFaction))
			{
				Barterable barterable = new SetPrisonerFreeBarterable(item.HeroObject, args.OffererHero, args.OffererParty, args.OtherHero);
				args.AddBarterable<PrisonerBarterGroup>(barterable);
			}
		}
		List<CharacterObject> list2 = otherParty.PrisonerHeroes.ToList();
		if (otherParty.LeaderHero != null && otherParty.LeaderHero == otherParty.LeaderHero?.Clan?.Leader)
		{
			list2.AddRange(otherParty.LeaderHero.Clan.DungeonPrisonersOfClan);
		}
		foreach (CharacterObject item2 in list2)
		{
			if (item2.IsHero && !FactionManager.IsAtWarAgainstFaction(item2.HeroObject.MapFaction, offererParty.MapFaction))
			{
				Barterable barterable2 = new SetPrisonerFreeBarterable(item2.HeroObject, args.OtherHero, args.OtherParty, args.OffererHero);
				args.AddBarterable<PrisonerBarterGroup>(barterable2);
			}
		}
	}
}
