using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public class PortScreenRestrictedModeHandler : PortScreenHandler
{
	private readonly PartyBase _leftOwner;

	private readonly PartyBase _rightOwner;

	public PortScreenRestrictedModeHandler(PartyBase leftOwner, PartyBase rightOwner)
		: base(leftOwner.Ships, new MBReadOnlyList<Ship>())
	{
		_leftOwner = leftOwner;
		_rightOwner = rightOwner;
	}

	protected override PortActionInfo CanBuyShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, GetTradeCostOfShip(ship, isRightSideSelling: false), GameTexts.FindText("str_port_buy_ship"), new TextObject("{=a2oyqIOU}You cannot buy ships when your fleet is away"));
	}

	protected override PortActionInfo CanSellShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, GetTradeCostOfShip(ship, isRightSideSelling: true), GameTexts.FindText("str_port_sell_ship"), new TextObject("{=YCwajsdL}You cannot sell ships when your fleet is away"));
	}

	protected override PortActionInfo CanRenameShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_rename_ship"), new TextObject("{=xmmYDcyd}You cannot rename ships when your fleet is away"));
	}

	protected override PortActionInfo CanRepairShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_ship"), new TextObject("{=7ccDIA8H}You cannot repair ships when your fleet is away"));
	}

	protected override PortActionInfo CanRepairAll()
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_all_ships"), new TextObject("{=7ccDIA8H}You cannot repair ships when your fleet is away"));
	}

	protected override PortActionInfo CanUpgradeShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_upgrade_ship"), new TextObject("{=5CXQsbqV}You cannot upgrade ships when your fleet is away"));
	}

	protected override PortActionInfo CanSendToClan(Ship ship)
	{
		return PortActionInfo.CreateInvalid();
	}

	protected override PortActionInfo CanStashShip(Ship ship)
	{
		return PortActionInfo.CreateInvalid();
	}

	protected override PortActionInfo CanViewStash(bool isRightRoster)
	{
		return PortActionInfo.CreateInvalid();
	}

	public override bool GetCanConfirm(out TextObject disabledHint)
	{
		disabledHint = TextObject.GetEmpty();
		return true;
	}

	public override TextObject GetLeftRosterName()
	{
		PartyBase leftOwner = _leftOwner;
		if (leftOwner != null && leftOwner.IsSettlement)
		{
			return new TextObject("{=UeUkbDVz}Port of {SETTLEMENT}").SetTextVariable("SETTLEMENT", _leftOwner.Name);
		}
		return _leftOwner?.Name;
	}

	public override int GetTradeCostOfShip(Ship ship, bool isRightSideSelling)
	{
		PartyBase seller = (isRightSideSelling ? _rightOwner : _leftOwner);
		PartyBase buyer = (isRightSideSelling ? _leftOwner : _rightOwner);
		return (int)Campaign.Current.Models.ShipCostModel.GetShipTradeValue(ship, seller, buyer);
	}

	public override int GetRepairCostOfShip(Ship ship, bool isRightSideRepairing)
	{
		return 0;
	}

	public override int GetUpgradeCostOfShip(Ship ship, ShipUpgradePiece piece, bool isRightSideUpgrading)
	{
		return 0;
	}

	public override TextObject GetRightRosterName()
	{
		return _rightOwner?.Name;
	}

	public override PartyBase GetLeftSideOwnerParty()
	{
		return _leftOwner;
	}

	public override PartyBase GetRightSideOwnerParty()
	{
		return _rightOwner;
	}

	public override int GetTotalGoldCost()
	{
		return 0;
	}

	public override void OnConfirmChanges()
	{
	}

	public override List<PortChangeInfo> GetChanges()
	{
		return new List<PortChangeInfo>();
	}
}
