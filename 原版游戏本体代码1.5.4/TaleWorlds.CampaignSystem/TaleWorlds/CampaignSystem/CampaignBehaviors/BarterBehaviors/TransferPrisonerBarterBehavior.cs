using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;

public class TransferPrisonerBarterBehavior : CampaignBehaviorBase
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
		foreach (CharacterObject prisonerHero in offererParty.PrisonerHeroes)
		{
			if (prisonerHero.IsHero && FactionManager.IsAtWarAgainstFaction(prisonerHero.HeroObject.MapFaction, otherParty.MapFaction))
			{
				Barterable barterable = new TransferPrisonerBarterable(prisonerHero.HeroObject, args.OffererHero, args.OffererParty, args.OtherHero, otherParty);
				args.AddBarterable<PrisonerBarterGroup>(barterable);
			}
		}
		foreach (CharacterObject prisonerHero2 in otherParty.PrisonerHeroes)
		{
			if (prisonerHero2.IsHero && FactionManager.IsAtWarAgainstFaction(prisonerHero2.HeroObject.MapFaction, offererParty.MapFaction))
			{
				Barterable barterable2 = new TransferPrisonerBarterable(prisonerHero2.HeroObject, args.OtherHero, args.OtherParty, args.OffererHero, offererParty);
				args.AddBarterable<PrisonerBarterGroup>(barterable2);
			}
		}
	}
}
