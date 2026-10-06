using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PlayerMoveTroopEvent : EventBase
{
	public CharacterObject Troop { get; private set; }

	public int Amount { get; private set; }

	public bool IsPrisoner { get; private set; }

	public PartyScreenLogic.PartyRosterSide FromSide { get; private set; }

	public PartyScreenLogic.PartyRosterSide ToSide { get; private set; }

	public PlayerMoveTroopEvent(CharacterObject troop, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.PartyRosterSide toSide, int amount, bool isPrisoner)
	{
		Troop = troop;
		FromSide = fromSide;
		ToSide = toSide;
		IsPrisoner = isPrisoner;
		Amount = amount;
	}
}
