using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameState;

public class PortState : TaleWorlds.Core.GameState
{
	public readonly PortScreenModes PortScreenMode;

	public readonly PartyBase LeftOwner;

	public readonly PartyBase RightOwner;

	public readonly MBReadOnlyList<Ship> LeftShips;

	public readonly MBReadOnlyList<Ship> RightShips;

	public readonly Action OnEndAction;

	public override bool IsMenuState => true;

	public PortState()
	{
		Debug.FailedAssert("do not use parameterless constructor.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameState\\PortState.cs", ".ctor", 39);
	}

	public PortState(PartyBase leftOwner, PartyBase rightOwner, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = leftOwner;
		RightOwner = rightOwner;
		LeftShips = leftOwner?.Ships;
		RightShips = rightOwner?.Ships;
	}

	public PortState(PartyBase leftOwner, PartyBase rightOwner, Action onEndAction, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = leftOwner;
		RightOwner = rightOwner;
		LeftShips = leftOwner?.Ships;
		RightShips = rightOwner?.Ships;
		OnEndAction = onEndAction;
	}

	public PortState(MBReadOnlyList<Ship> leftShips, MBReadOnlyList<Ship> rightShips, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = null;
		RightOwner = null;
		LeftShips = leftShips;
		RightShips = rightShips;
	}

	public PortState(PartyBase leftOwner, PartyBase rightOwner, MBReadOnlyList<Ship> leftShips, MBReadOnlyList<Ship> rightShips, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = leftOwner;
		RightOwner = rightOwner;
		LeftShips = leftShips;
		RightShips = rightShips;
	}

	public PortState(PartyBase leftOwner, PartyBase rightOwner, MBReadOnlyList<Ship> leftShips, MBReadOnlyList<Ship> rightShips, Action onEndAction, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = leftOwner;
		RightOwner = rightOwner;
		LeftShips = leftShips;
		RightShips = rightShips;
		OnEndAction = onEndAction;
	}

	public PortState(Settlement settlement, PartyBase rightOwner, PortScreenModes portScreenMode)
	{
		PortScreenMode = portScreenMode;
		LeftOwner = settlement.Party;
		RightOwner = rightOwner;
		LeftShips = settlement.Party.Ships;
		RightShips = rightOwner.Ships;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		OnEndAction?.Invoke();
	}
}
