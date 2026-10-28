using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class ClanState : TaleWorlds.Core.GameState
{
	private IClanStateHandler _handler;

	public override bool IsMenuState => true;

	public Hero InitialSelectedHero { get; private set; }

	public PartyBase InitialSelectedParty { get; private set; }

	public Settlement InitialSelectedSettlement { get; private set; }

	public Workshop InitialSelectedWorkshop { get; private set; }

	public Alley InitialSelectedAlley { get; private set; }

	public IClanStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public ClanState()
	{
	}

	public ClanState(Hero initialSelectedHero)
	{
		InitialSelectedHero = initialSelectedHero;
	}

	public ClanState(PartyBase initialSelectedParty)
	{
		InitialSelectedParty = initialSelectedParty;
	}

	public ClanState(Settlement initialSelectedSettlement)
	{
		InitialSelectedSettlement = initialSelectedSettlement;
	}

	public ClanState(Workshop initialSelectedWorkshop)
	{
		InitialSelectedWorkshop = initialSelectedWorkshop;
	}

	public ClanState(Alley initialSelectedAlley)
	{
		InitialSelectedAlley = initialSelectedAlley;
	}
}
