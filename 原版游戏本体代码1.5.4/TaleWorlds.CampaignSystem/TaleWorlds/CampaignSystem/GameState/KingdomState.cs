using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class KingdomState : TaleWorlds.Core.GameState
{
	private IKingdomStateHandler _handler;

	public override bool IsMenuState => true;

	public Army InitialSelectedArmy { get; private set; }

	public Settlement InitialSelectedSettlement { get; private set; }

	public Clan InitialSelectedClan { get; private set; }

	public PolicyObject InitialSelectedPolicy { get; private set; }

	public Kingdom InitialSelectedKingdom { get; private set; }

	public KingdomDecision InitialSelectedDecision { get; private set; }

	public IKingdomStateHandler Handler
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

	public KingdomState()
	{
	}

	public KingdomState(KingdomDecision initialSelectedDecision)
	{
		InitialSelectedDecision = initialSelectedDecision;
	}

	public KingdomState(Army initialSelectedArmy)
	{
		InitialSelectedArmy = initialSelectedArmy;
	}

	public KingdomState(Settlement initialSelectedSettlement)
	{
		InitialSelectedSettlement = initialSelectedSettlement;
	}

	public KingdomState(IFaction initialSelectedFaction)
	{
		if (initialSelectedFaction is Clan initialSelectedClan)
		{
			InitialSelectedClan = initialSelectedClan;
		}
		else if (initialSelectedFaction is Kingdom initialSelectedKingdom)
		{
			InitialSelectedKingdom = initialSelectedKingdom;
		}
	}

	public KingdomState(PolicyObject initialSelectedPolicy)
	{
		InitialSelectedPolicy = initialSelectedPolicy;
	}
}
