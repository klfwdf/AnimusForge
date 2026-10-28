using System.Collections.Generic;
using System.Linq;
using NavalDLC.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public class PortScreenTradeModeHandler : PortScreenHandler
{
	private readonly PartyBase _leftOwner;

	private readonly PartyBase _rightOwner;

	public PortScreenTradeModeHandler(PartyBase leftOwner, PartyBase rightOwner)
		: base(leftOwner.Ships, rightOwner.Ships)
	{
		_leftOwner = leftOwner;
		_rightOwner = rightOwner;
	}

	public override TextObject GetLeftRosterName()
	{
		if (_leftOwner.IsSettlement)
		{
			return new TextObject("{=UeUkbDVz}Port of {SETTLEMENT}").SetTextVariable("SETTLEMENT", _leftOwner.Name);
		}
		return _leftOwner.Name;
	}

	public override TextObject GetRightRosterName()
	{
		return _rightOwner.Name;
	}

	public override PartyBase GetLeftSideOwnerParty()
	{
		return _leftOwner;
	}

	public override PartyBase GetRightSideOwnerParty()
	{
		return _rightOwner;
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
		if (MobileParty.MainParty.IsCurrentlyAtSea && base.RightShips.Count == 1)
		{
			Debug.FailedAssert("Trade mode should not be accessible from the sea!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortScreenHandlers\\PortScreenTradeModeHandler.cs", "CanSellShip", 67);
			PortActionInfo.CreateValid(isEnabled: false, goldCost, name, GameTexts.FindText("str_cannot_give_all_ships"));
		}
		return PortActionInfo.CreateValid(isEnabled: true, goldCost, name, TextObject.GetEmpty());
	}

	protected override PortActionInfo CanRepairShip(Ship ship)
	{
		if (base.ShipsToRepair.Contains(ship))
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_ship"), new TextObject("{=Ma26nyeo}Already repaired"));
		}
		return PortActionInfo.CreateValid(isEnabled: true, GetRepairCostOfShip(ship, isRightSideRepairing: true), GameTexts.FindText("str_port_repair_ship"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanRepairAll()
	{
		MBList<Ship> mBList = new MBList<Ship>();
		int num = 0;
		foreach (Ship rightShip in base.RightShips)
		{
			if (!base.ShipsToRepair.Contains(rightShip) && rightShip.HitPoints < rightShip.MaxHitPoints)
			{
				mBList.Add(rightShip);
				num += GetRepairCostOfShip(rightShip, isRightSideRepairing: true);
			}
		}
		if (mBList.Count == 0)
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, GameTexts.FindText("str_port_repair_all_ships"), new TextObject("{=Ma26nyeo}Already repaired"));
		}
		return PortActionInfo.CreateValid(isEnabled: true, num, GameTexts.FindText("str_port_repair_all_ships"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanUpgradeShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_port_upgrade_ship"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanRenameShip(Ship ship)
	{
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_port_rename_ship"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanSendToClan(Ship ship)
	{
		int troopsCountToSend = base.ShipsToSend.Count * Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips;
		TextObject hint;
		return PortActionInfo.CreateValid(Campaign.Current.Models.FleetManagementModel.CanSendShipToPlayerClan(ship, base.RightShips.Count, troopsCountToSend, out hint), 0, GameTexts.FindText("str_port_send_ship_to_clan"), hint);
	}

	protected override PortActionInfo CanStashShip(Ship ship)
	{
		if (Settlement.CurrentSettlement == null)
		{
			PortActionInfo.CreateInvalid();
		}
		return PortActionInfo.CreateValid(isEnabled: true, 0, GameTexts.FindText("str_port_stash_ship"), TextObject.GetEmpty());
	}

	protected override PortActionInfo CanViewStash(bool isRightRoster)
	{
		if (Settlement.CurrentSettlement == null)
		{
			PortActionInfo.CreateInvalid();
		}
		if (base.ShipsToStash.Count > 0 || Settlement.All.Sum((Settlement x) => x.ShipStash.Count) > base.ShipsToRetrieveFromStash.Count)
		{
			TextObject name = GameTexts.FindText("str_port_view_stash");
			return PortActionInfo.CreateValid(isEnabled: true, 0, name, TextObject.GetEmpty());
		}
		return PortActionInfo.CreateInvalid();
	}

	public override int GetTradeCostOfShip(Ship ship, bool isRightSideSelling)
	{
		PartyBase seller = (isRightSideSelling ? _rightOwner : _leftOwner);
		PartyBase buyer = (isRightSideSelling ? _leftOwner : _rightOwner);
		return (int)Campaign.Current.Models.ShipCostModel.GetShipTradeValue(ship, seller, buyer);
	}

	public override int GetRepairCostOfShip(Ship ship, bool isRightSideRepairing)
	{
		PartyBase owner = (isRightSideRepairing ? _rightOwner : _leftOwner);
		return (int)Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, owner);
	}

	public override int GetUpgradeCostOfShip(Ship ship, ShipUpgradePiece piece, bool isRightSideUpgrading)
	{
		PartyBase owner = (isRightSideUpgrading ? _rightOwner : _leftOwner);
		return Campaign.Current.Models.ShipCostModel.GetShipUpgradePieceCost(ship, piece, owner);
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
		for (int k = 0; k < base.ShipsToRepair.Count; k++)
		{
			Ship ship = base.ShipsToRepair[k];
			num += GetRepairCostOfShip(ship, isRightSideRepairing: true);
		}
		for (int l = 0; l < base.SelectedShipPieces.Count; l++)
		{
			Ship ship2 = base.SelectedShipPieces[l].Ship;
			ShipUpgradePiece piece = base.SelectedShipPieces[l].Piece;
			if (piece != null)
			{
				num += GetUpgradeCostOfShip(ship2, piece, isRightSideUpgrading: true);
			}
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
			ChangeShipOwnerAction.ApplyByTrade(_rightOwner, ship);
		}
		for (int j = 0; j < base.ShipsToRetrieveFromStash.Count; j++)
		{
			ChangeShipOwnerAction.ApplyByUnstashingShipFromSettlement(MobileParty.MainParty.Party, _leftOwner.Settlement, base.ShipsToRetrieveFromStash[j]);
		}
		for (int k = 0; k < base.ShipsToSell.Count; k++)
		{
			Ship ship2 = base.ShipsToSell[k].Ship;
			ChangeShipOwnerAction.ApplyByTrade(_leftOwner, ship2);
		}
		for (int l = 0; l < base.ShipsToRepair.Count; l++)
		{
			RepairShipAction.Apply(base.ShipsToRepair[l], Settlement.CurrentSettlement);
		}
		for (int m = 0; m < base.ShipsToRename.Count; m++)
		{
			ShipRenameInfo shipRenameInfo = base.ShipsToRename[m];
			shipRenameInfo.Ship.SetName(new TextObject("{=!}" + shipRenameInfo.NewName));
		}
		for (int n = 0; n < base.SelectedShipPieces.Count; n++)
		{
			Ship ship3 = base.SelectedShipPieces[n].Ship;
			string shipSlotTag = base.SelectedShipPieces[n].ShipSlotTag;
			ShipUpgradePiece piece = base.SelectedShipPieces[n].Piece;
			int num = 0;
			if (piece != null)
			{
				num += GetUpgradeCostOfShip(ship3, piece, isRightSideUpgrading: true);
			}
			ship3.EquipUpgradePiece(shipSlotTag, piece);
			if (num > 0)
			{
				GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, _leftOwner.Settlement, num);
			}
			else
			{
				GiveGoldAction.ApplyForSettlementToCharacter(_leftOwner.Settlement, Hero.MainHero, -num);
			}
		}
		for (int num2 = 0; num2 < base.SelectedFigureheads.Count; num2++)
		{
			Ship ship4 = base.SelectedFigureheads[num2].Ship;
			Figurehead figurehead = base.SelectedFigureheads[num2].Figurehead;
			ship4.ChangeFigurehead(figurehead);
		}
		IFleetManagementCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IFleetManagementCampaignBehavior>();
		for (int num3 = 0; num3 < base.ShipsToSend.Count; num3++)
		{
			campaignBehavior.SendShipToClan(base.ShipsToSend[num3], Clan.PlayerClan);
		}
		for (int num4 = 0; num4 < base.ShipsToStash.Count; num4++)
		{
			ChangeShipOwnerAction.ApplyByStashingShipIntoSettlement(_leftOwner.Settlement, base.ShipsToStash[num4]);
		}
		if (MobileParty.MainParty.Ships.Count == 0 && MobileParty.MainParty.Anchor.IsValid)
		{
			MobileParty.MainParty.Anchor.ResetPosition();
		}
		else if (MobileParty.MainParty.Ships.Count > 0 && !MobileParty.MainParty.Anchor.IsValid && _leftOwner.IsSettlement)
		{
			MobileParty.MainParty.Anchor.Settlement = _leftOwner.Settlement;
		}
	}

	public override List<PortChangeInfo> GetChanges()
	{
		List<PortChangeInfo> list = new List<PortChangeInfo>();
		for (int i = 0; i < base.ShipsToBuy.Count; i++)
		{
			list.Add(new PortChangeInfo(base.ShipsToBuy[i].Price, new TextObject("{=9AIOcUuH}Buy {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToBuy[i].Ship.Name).ToString()));
		}
		for (int j = 0; j < base.ShipsToRetrieveFromStash.Count; j++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=OWbPPwRR}Retrieve {SHIP_NAME} from stash").SetTextVariable("SHIP_NAME", base.ShipsToRetrieveFromStash[j].Name).ToString()));
		}
		for (int k = 0; k < base.ShipsToRename.Count; k++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=Fidoxgd1}Rename {SHIP_NAME} to {NEW_SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToRename[k].Ship.Name).SetTextVariable("NEW_SHIP_NAME", base.ShipsToRename[k].NewName).ToString()));
		}
		for (int l = 0; l < base.ShipsToRepair.Count; l++)
		{
			list.Add(new PortChangeInfo(GetRepairCostOfShip(base.ShipsToRepair[l], isRightSideRepairing: true), new TextObject("{=HQK9kUD9}Repair {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.ShipsToRepair[l])).ToString()));
		}
		for (int m = 0; m < base.SelectedShipPieces.Count; m++)
		{
			ShipUpgradePiece piece = base.SelectedShipPieces[m].Piece;
			ShipUpgradePiece pieceAtSlot = base.SelectedShipPieces[m].Ship.GetPieceAtSlot(base.SelectedShipPieces[m].ShipSlotTag);
			if (pieceAtSlot != null)
			{
				list.Add(new PortChangeInfo(0f, new TextObject("{=PniFsE6M}Remove {PIECE_NAME} from {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.SelectedShipPieces[m].Ship)).SetTextVariable("PIECE_NAME", pieceAtSlot.GetName()).ToString()));
			}
			if (piece != null)
			{
				list.Add(new PortChangeInfo(GetUpgradeCostOfShip(base.SelectedShipPieces[m].Ship, piece, isRightSideUpgrading: true), new TextObject("{=jwgUwyKO}Add {PIECE_NAME} to {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.SelectedShipPieces[m].Ship)).SetTextVariable("PIECE_NAME", piece.GetName()).ToString()));
			}
		}
		for (int n = 0; n < base.SelectedFigureheads.Count; n++)
		{
			Figurehead figurehead = base.SelectedFigureheads[n].Figurehead;
			Figurehead figurehead2 = base.SelectedFigureheads[n].Ship.Figurehead;
			if (figurehead2 != null)
			{
				list.Add(new PortChangeInfo(0f, new TextObject("{=PniFsE6M}Remove {PIECE_NAME} from {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.SelectedFigureheads[n].Ship)).SetTextVariable("PIECE_NAME", figurehead2.GetName()).ToString()));
			}
			if (figurehead != null)
			{
				list.Add(new PortChangeInfo(0f, new TextObject("{=jwgUwyKO}Add {PIECE_NAME} to {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.SelectedFigureheads[n].Ship)).SetTextVariable("PIECE_NAME", figurehead.GetName()).ToString()));
			}
		}
		for (int num = 0; num < base.ShipsToSend.Count; num++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=L1x30kUJ}Send {SHIP_NAME} to clan").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.ShipsToSend[num])).ToString()));
		}
		for (int num2 = 0; num2 < base.ShipsToStash.Count; num2++)
		{
			list.Add(new PortChangeInfo(0f, new TextObject("{=b8gqqEHE}Stash {SHIP_NAME}").SetTextVariable("SHIP_NAME", GetShipNameConsideringRenames(base.ShipsToStash[num2])).ToString()));
		}
		for (int num3 = 0; num3 < base.ShipsToSell.Count; num3++)
		{
			list.Add(new PortChangeInfo(-base.ShipsToSell[num3].Price, new TextObject("{=1Yaq0qy1}Sell {SHIP_NAME}").SetTextVariable("SHIP_NAME", base.ShipsToSell[num3].Ship.Name).ToString()));
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
