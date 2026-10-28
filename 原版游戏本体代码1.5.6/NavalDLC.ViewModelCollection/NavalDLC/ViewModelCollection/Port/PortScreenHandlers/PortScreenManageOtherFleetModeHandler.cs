using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public class PortScreenManageOtherFleetModeHandler : PortScreenHandler
{
	private readonly PartyBase _other;

	public PortScreenManageOtherFleetModeHandler(PartyBase other)
		: base(other.Ships, MobileParty.MainParty.Ships)
	{
		_other = other;
	}

	public override bool GetCanConfirm(out TextObject disabledHint)
	{
		disabledHint = null;
		return true;
	}

	public override PartyBase GetLeftSideOwnerParty()
	{
		return _other;
	}

	public override PartyBase GetRightSideOwnerParty()
	{
		return MobileParty.MainParty.Party;
	}

	public override TextObject GetLeftRosterName()
	{
		if (_other.IsSettlement)
		{
			return new TextObject("{=UeUkbDVz}Port of {SETTLEMENT}").SetTextVariable("SETTLEMENT", _other.Name);
		}
		return _other.Name;
	}

	public override int GetTradeCostOfShip(Ship ship, bool isRightSideSelling)
	{
		return 0;
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
		return MobileParty.MainParty.Name;
	}

	public override int GetTotalGoldCost()
	{
		return 0;
	}

	public override void OnConfirmChanges()
	{
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			Ship ship = base.ShipsToBuy[i].Ship;
			ChangeShipOwnerAction.ApplyByTransferring(MobileParty.MainParty.Party, ship);
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			Ship ship2 = base.ShipsToSell[j].Ship;
			ChangeShipOwnerAction.ApplyByTransferring(_other, ship2);
		}
		if (MobileParty.MainParty.Ships.Count == 0 && MobileParty.MainParty.Anchor.IsValid)
		{
			MobileParty.MainParty.Anchor.ResetPosition();
		}
		if (_other.Ships.Count == 0 && _other.IsMobile && _other.MobileParty.Anchor.IsValid)
		{
			_other.MobileParty.Anchor.ResetPosition();
		}
		for (int k = 0; k < base.ShipsToRename.Count; k++)
		{
			ShipRenameInfo shipRenameInfo = base.ShipsToRename[k];
			shipRenameInfo.Ship.SetName(new TextObject("{=!}" + shipRenameInfo.NewName));
		}
	}

	protected override PortActionInfo CanBuyShip(Ship ship)
	{
		TextObject name = (base.ShipsToSell.Any((ShipTradeInfo x) => x.Ship == ship) ? GameTexts.FindText("str_take_ship_back") : GameTexts.FindText("str_take"));
		TextObject disabledHint;
		return PortActionInfo.CreateValid(CanBuyShip(ship, out disabledHint), 0, name, disabledHint);
	}

	protected override PortActionInfo CanSellShip(Ship ship)
	{
		TextObject name = (base.ShipsToBuy.Any((ShipTradeInfo x) => x.Ship == ship) ? GameTexts.FindText("str_give_ship_back") : GameTexts.FindText("str_give"));
		TextObject disabledHint;
		return PortActionInfo.CreateValid(CanSellShip(ship, out disabledHint), 0, name, disabledHint);
	}

	protected override PortActionInfo CanUpgradeShip(Ship ship)
	{
		return PortActionInfo.CreateInvalid(new TextObject("{=4d7XLElL}You can't upgrade ships outside a port."));
	}

	protected override PortActionInfo CanRenameShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_port_rename_ship"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanRepairShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_ship"), new TextObject("{=Pm6JbaXa}You can't repair ships outside a port."));
	}

	protected override PortActionInfo CanRepairAll()
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_all_ships"), new TextObject("{=Pm6JbaXa}You can't repair ships outside a port."));
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

	private bool CanSellShip(Ship ship, out TextObject disabledHint)
	{
		disabledHint = TextObject.GetEmpty();
		if (base.ShipsToSell.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea && base.RightShips.Count == 1)
		{
			disabledHint = GameTexts.FindText("str_cannot_give_all_ships");
			return false;
		}
		return true;
	}

	private bool CanBuyShip(Ship ship, out TextObject disabledHint)
	{
		disabledHint = TextObject.GetEmpty();
		if (base.ShipsToBuy.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			return false;
		}
		if (_other.MobileParty.IsCurrentlyAtSea && _other.Ships.Count + base.ShipsToSell.Count - base.ShipsToBuy.Count <= 1)
		{
			disabledHint = GameTexts.FindText("str_cannot_take_all_ships");
			return false;
		}
		return true;
	}

	public override List<PortChangeInfo> GetChanges()
	{
		List<PortChangeInfo> list = new List<PortChangeInfo>();
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=TsQzdjvd}Take {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToBuy[i].Ship.Name).ToString()));
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=LZsY5SyD}Give {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToSell[j].Ship.Name).ToString()));
		}
		for (int k = 0; k < base.ShipsToRename.Count; k++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=Fidoxgd1}Rename {SHIP_NAME} to {NEW_SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToRename[k].Ship.Name).SetTextVariable("NEW_SHIP_NAME", base.ShipsToRename[k].NewName).ToString()));
		}
		return list;
	}
}
