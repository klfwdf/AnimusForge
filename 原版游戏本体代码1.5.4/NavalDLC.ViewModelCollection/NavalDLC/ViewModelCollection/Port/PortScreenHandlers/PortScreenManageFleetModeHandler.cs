using System.Collections.Generic;
using System.Linq;
using NavalDLC.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public class PortScreenManageFleetModeHandler : PortScreenHandler
{
	private readonly TextObject _leftSideName;

	private readonly PartyBase _rightSide;

	public PortScreenManageFleetModeHandler(TextObject leftSideName, PartyBase rightSide, MBReadOnlyList<Ship> initialLeftShips, MBReadOnlyList<Ship> initialRightShips)
		: base(initialLeftShips, initialRightShips)
	{
		_leftSideName = leftSideName;
		_rightSide = rightSide;
	}

	public override bool GetCanConfirm(out TextObject disabledHint)
	{
		disabledHint = null;
		return true;
	}

	public override PartyBase GetLeftSideOwnerParty()
	{
		return null;
	}

	public override PartyBase GetRightSideOwnerParty()
	{
		return _rightSide;
	}

	public override TextObject GetLeftRosterName()
	{
		return _leftSideName;
	}

	public override TextObject GetRightRosterName()
	{
		return _rightSide.Name;
	}

	public override int GetTradeCostOfShip(Ship ship, bool isSelling)
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

	public override int GetTotalGoldCost()
	{
		return 0;
	}

	public override void OnConfirmChanges()
	{
		for (int i = 0; i < base.ShipsToSell.Count; i++)
		{
			DestroyShipAction.ApplyByDiscard(base.ShipsToSell[i].Ship);
		}
		for (int j = 0; j < base.ShipsToBuy.Count; j++)
		{
			ShipTradeInfo shipTradeInfo = base.ShipsToBuy[j];
			ChangeShipOwnerAction.ApplyByTransferring(_rightSide, shipTradeInfo.Ship);
		}
		for (int k = 0; k < base.ShipsToRename.Count; k++)
		{
			ShipRenameInfo shipRenameInfo = base.ShipsToRename[k];
			shipRenameInfo.Ship.SetName(new TextObject("{=!}" + shipRenameInfo.NewName));
		}
		IFleetManagementCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IFleetManagementCampaignBehavior>();
		for (int l = 0; l < base.ShipsToSend.Count; l++)
		{
			campaignBehavior.SendShipToClan(base.ShipsToSend[l], Clan.PlayerClan);
		}
		if (MobileParty.MainParty.Ships.Count == 0 && MobileParty.MainParty.Anchor.IsValid)
		{
			MobileParty.MainParty.Anchor.ResetPosition();
		}
	}

	protected override PortActionInfo CanBuyShip(Ship ship)
	{
		if (base.ShipsToSell.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_take_ship_back"), null);
		}
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_take"), null);
	}

	protected override PortActionInfo CanSellShip(Ship ship)
	{
		if (MobileParty.MainParty.IsCurrentlyAtSea && base.RightShips.Count == 1)
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_discard_ship"), GameTexts.FindText("str_cannot_give_all_ships"));
		}
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_port_discard_ship"), null);
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
		int troopsCountToSend = base.ShipsToSend.Count * Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips;
		TextObject hint;
		return PortActionInfo.CreateValid(Campaign.Current.Models.FleetManagementModel.CanSendShipToPlayerClan(ship, base.RightShips.Count, troopsCountToSend, out hint), 0, GameTexts.FindText("str_port_send_ship_to_clan"), hint);
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
			list.Add(new PortChangeInfo(0f, new TextObject("{=TsQzdjvd}Take {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToBuy[i].Ship.Name).ToString()));
		}
		for (int j = 0; j < base.ShipsToSell.Count; j++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=cItrQpwh}Discard {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToSell[j].Ship.Name).ToString()));
		}
		for (int k = 0; k < base.ShipsToRename.Count; k++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=Fidoxgd1}Rename {SHIP_NAME} to {NEW_SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToRename[k].Ship.Name).SetTextVariable("NEW_SHIP_NAME", base.ShipsToRename[k].NewName).ToString()));
		}
		for (int l = 0; l < base.ShipsToSend.Count; l++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=L1x30kUJ}Send {SHIP_NAME} to clan").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.ShipsToSend[l])).ToString()));
		}
		return list;
	}

	private TextObject GetShipNameConsideringRenames(Ship ship)
	{
		TextObject result = ship.Name;
		if (base.ShipsToRename.Any((ShipRenameInfo x) => x.Ship == ship))
		{
			result = new TextObject("{=!}" + base.ShipsToRename.First((ShipRenameInfo x) => x.Ship == ship).NewName);
		}
		return result;
	}
}
