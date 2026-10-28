using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public class PortScreenStoryModeHandler : PortScreenHandler
{
	private readonly PartyBase _leftParty;

	private readonly PartyBase _rightParty;

	public PortScreenStoryModeHandler(PartyBase leftParty, PartyBase rightParty)
		: base(leftParty.Ships, rightParty.Ships)
	{
		_leftParty = leftParty;
		_rightParty = rightParty;
	}

	public override TextObject GetLeftRosterName()
	{
		if (_leftParty.IsSettlement)
		{
			return new TextObject("{=UeUkbDVz}Port of {SETTLEMENT}").SetTextVariable("SETTLEMENT", _leftParty.Name);
		}
		return _leftParty.Name;
	}

	public override TextObject GetRightRosterName()
	{
		return _rightParty.Name;
	}

	public override PartyBase GetLeftSideOwnerParty()
	{
		return _leftParty;
	}

	public override PartyBase GetRightSideOwnerParty()
	{
		return _rightParty;
	}

	public override int GetTradeCostOfShip(Ship ship, bool isRightSideSelling)
	{
		PartyBase seller = (isRightSideSelling ? _rightParty : _leftParty);
		PartyBase buyer = (isRightSideSelling ? _leftParty : _rightParty);
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

	public override int GetTotalGoldCost()
	{
		int num = 0;
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			num += base.ShipsToBuy[i].Price;
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			num -= base.ShipsToSell[j].Price;
		}
		return num;
	}

	public override bool GetCanConfirm(out TextObject disabledHint)
	{
		if (GetTotalGoldCost() > Hero.MainHero.Gold)
		{
			disabledHint = new TextObject("{=RYJdU43V}Not Enough Gold");
			return false;
		}
		disabledHint = null;
		return true;
	}

	public override void OnConfirmChanges()
	{
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			Ship ship = base.ShipsToBuy[i].Ship;
			ChangeShipOwnerAction.ApplyByTrade(_rightParty, ship);
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			Ship ship2 = base.ShipsToSell[j].Ship;
			ChangeShipOwnerAction.ApplyByTrade(_leftParty, ship2);
		}
		if (MobileParty.MainParty.Ships.Count == 0 && MobileParty.MainParty.Anchor.IsValid)
		{
			MobileParty.MainParty.Anchor.ResetPosition();
		}
		else if (MobileParty.MainParty.Ships.Count > 0 && !MobileParty.MainParty.Anchor.IsValid && _leftParty.IsSettlement)
		{
			MobileParty.MainParty.Anchor.Settlement = _leftParty.Settlement;
		}
	}

	protected override PortActionInfo CanBuyShip(Ship ship)
	{
		bool num = base.ShipsToSell.Any((ShipTradeInfo x) => x.Ship == ship);
		int goldCost = (num ? base.ShipsToSell.FirstOrDefault((ShipTradeInfo x) => x.Ship == ship).Price : GetTradeCostOfShip(ship, isRightSideSelling: false));
		TextObject name = (num ? GameTexts.FindText("str_port_buy_ship_back") : GameTexts.FindText("str_port_buy_ship"));
		return PortActionInfo.CreateValid(isEnabled: true, goldCost, name, TextObject.GetEmpty());
	}

	protected override PortActionInfo CanSellShip(Ship ship)
	{
		bool num = base.ShipsToBuy.Any((ShipTradeInfo x) => x.Ship == ship);
		int goldCost = (num ? base.ShipsToBuy.FirstOrDefault((ShipTradeInfo x) => x.Ship == ship).Price : GetTradeCostOfShip(ship, isRightSideSelling: true));
		TextObject name = (num ? GameTexts.FindText("str_port_sell_ship_back") : GameTexts.FindText("str_port_sell_ship"));
		return PortActionInfo.CreateValid(isEnabled: true, goldCost, name, TextObject.GetEmpty());
	}

	protected override PortActionInfo CanRenameShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_rename_ship"), new TextObject("{=i6BBEAXI}You can't rename ships at this stage"));
	}

	protected override PortActionInfo CanRepairShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_ship"), new TextObject("{=HqraYjwT}You can't repair ships at this stage"));
	}

	protected override PortActionInfo CanRepairAll()
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_all_ships"), new TextObject("{=HqraYjwT}You can't repair ships at this stage"));
	}

	protected override PortActionInfo CanUpgradeShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_upgrade_ship"), new TextObject("{=b3eIbvr0}You can't upgrade ships at this stage"));
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

	public override List<PortChangeInfo> GetChanges()
	{
		List<PortChangeInfo> list = new List<PortChangeInfo>();
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			list.Add(new PortChangeInfo(base.ShipsToBuy[i].Price, new TextObject("{=9AIOcUuH}Buy {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToBuy[i].Ship.Name).ToString()));
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			list.Add(new PortChangeInfo(base.ShipsToSell[j].Price, new TextObject("{=1Yaq0qy1}Sell {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToSell[j].Ship.Name).ToString()));
		}
		return list;
	}
}
