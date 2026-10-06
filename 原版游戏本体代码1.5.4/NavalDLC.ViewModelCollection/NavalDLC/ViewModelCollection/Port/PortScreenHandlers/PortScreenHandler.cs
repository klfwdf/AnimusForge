using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public abstract class PortScreenHandler
{
	public readonly struct ShipUpgradePieceInfo
	{
		public readonly Ship Ship;

		public readonly string ShipSlotTag;

		public readonly ShipUpgradePiece Piece;

		public ShipUpgradePieceInfo(Ship ship, string shipSlotTag, ShipUpgradePiece piece)
		{
			Ship = ship;
			ShipSlotTag = shipSlotTag;
			Piece = piece;
		}
	}

	public readonly struct ShipFigureheadInfo
	{
		public readonly Ship Ship;

		public readonly Figurehead Figurehead;

		public ShipFigureheadInfo(Ship ship, Figurehead figurehead)
		{
			Ship = ship;
			Figurehead = figurehead;
		}
	}

	public readonly struct ShipRenameInfo
	{
		public readonly Ship Ship;

		public readonly string NewName;

		public ShipRenameInfo(Ship ship, string newName)
		{
			Ship = ship;
			NewName = newName;
		}
	}

	public readonly struct ShipTradeInfo
	{
		public readonly Ship Ship;

		public readonly int Price;

		public ShipTradeInfo(Ship ship, int price)
		{
			Ship = ship;
			Price = price;
		}
	}

	protected MBReadOnlyList<Ship> _initialLeftShips;

	protected MBReadOnlyList<Ship> _initialRightShips;

	private MBList<Ship> _leftShips;

	private MBList<Ship> _rightShips;

	private MBList<ShipTradeInfo> _shipsToBuy;

	private MBList<ShipTradeInfo> _shipsToSell;

	private MBList<Ship> _shipsToRepair;

	private MBList<Ship> _shipsToSend;

	private MBList<Ship> _shipsToStash;

	private MBList<Ship> _shipsToRetrieveFromStash;

	private MBList<ShipRenameInfo> _shipsToRename;

	private MBList<ShipUpgradePieceInfo> _selectedShipPieces;

	private MBList<ShipFigureheadInfo> _selectedFigureheads;

	public MBReadOnlyList<Ship> LeftShips => _leftShips;

	public MBReadOnlyList<Ship> RightShips => _rightShips;

	public MBReadOnlyList<ShipTradeInfo> ShipsToBuy => _shipsToBuy;

	public MBReadOnlyList<ShipTradeInfo> ShipsToSell => _shipsToSell;

	public MBReadOnlyList<Ship> ShipsToRepair => _shipsToRepair;

	public MBReadOnlyList<Ship> ShipsToSend => _shipsToSend;

	public MBReadOnlyList<Ship> ShipsToStash => _shipsToStash;

	public MBReadOnlyList<Ship> ShipsToRetrieveFromStash => _shipsToRetrieveFromStash;

	public MBReadOnlyList<ShipRenameInfo> ShipsToRename => _shipsToRename;

	public MBReadOnlyList<ShipUpgradePieceInfo> SelectedShipPieces => _selectedShipPieces;

	public MBReadOnlyList<ShipFigureheadInfo> SelectedFigureheads => _selectedFigureheads;

	public PortScreenHandler(MBReadOnlyList<Ship> initialLeftShips, MBReadOnlyList<Ship> initialRightShips)
	{
		_initialLeftShips = initialLeftShips;
		_initialRightShips = initialRightShips;
		_leftShips = new MBList<Ship>(_initialLeftShips);
		_rightShips = new MBList<Ship>(_initialRightShips);
		_shipsToBuy = new MBList<ShipTradeInfo>();
		_shipsToSell = new MBList<ShipTradeInfo>();
		_shipsToRepair = new MBList<Ship>();
		_shipsToRename = new MBList<ShipRenameInfo>();
		_shipsToSend = new MBList<Ship>();
		_shipsToStash = new MBList<Ship>();
		_shipsToRetrieveFromStash = new MBList<Ship>();
		_selectedShipPieces = new MBList<ShipUpgradePieceInfo>();
		_selectedFigureheads = new MBList<ShipFigureheadInfo>();
	}

	public abstract TextObject GetLeftRosterName();

	public abstract TextObject GetRightRosterName();

	public abstract PartyBase GetLeftSideOwnerParty();

	public abstract PartyBase GetRightSideOwnerParty();

	public PortActionInfo GetCanBuyShip(Ship ship)
	{
		if (!LeftShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid();
		}
		PortActionInfo result = CanBuyShip(ship);
		if (result.IsRelevant && (!ship.IsTradeable || ship.IsUsedByQuest))
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, result.ActionName, new TextObject("{=pWd0AQm8}You cannot buy this ship"));
		}
		return result;
	}

	public PortActionInfo GetCanSellShip(Ship ship)
	{
		if (!RightShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid();
		}
		PortActionInfo result = CanSellShip(ship);
		if (result.IsRelevant && (!ship.IsTradeable || ship.IsUsedByQuest))
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, result.ActionName, GameTexts.FindText("str_port_cant_take_action_quest_ship"));
		}
		return result;
	}

	public PortActionInfo GetCanRepairShip(Ship ship)
	{
		if (!RightShips.Contains(ship) || ship.HitPoints >= ship.MaxHitPoints)
		{
			return PortActionInfo.CreateInvalid();
		}
		return CanRepairShip(ship);
	}

	public PortActionInfo GetCanRepairAll(Ship selectedShip)
	{
		if (!RightShips.Contains(selectedShip) || RightShips.TrueForAll((Ship ship) => ship.HitPoints >= ship.MaxHitPoints))
		{
			return PortActionInfo.CreateInvalid();
		}
		return CanRepairAll();
	}

	public PortActionInfo GetCanUpgradeShip(Ship ship)
	{
		if (!RightShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid(new TextObject("{=hlBSanaL}You can't upgrade ships that don't belong to you"));
		}
		if (ship.HitPoints < ship.MaxHitPoints && !ShipsToRepair.Contains(ship))
		{
			return PortActionInfo.CreateInvalid(new TextObject("{=8KEmXkaT}You can't upgrade ships that need repairs"));
		}
		return CanUpgradeShip(ship);
	}

	public PortActionInfo GetCanRenameShip(Ship ship)
	{
		if (!RightShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid(new TextObject("{=NmWkD50x}You can't rename ships that don't belong to you"));
		}
		return CanRenameShip(ship);
	}

	public PortActionInfo GetCanSendToClan(Ship ship)
	{
		if (!RightShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid();
		}
		PortActionInfo result = CanSendToClan(ship);
		if (result.IsRelevant && RightShips.Count == 1)
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, result.ActionName, new TextObject("{=DSoB9VCu}You can't send your only ship to your clan"));
		}
		return result;
	}

	public PortActionInfo GetCanStashShip(Ship ship)
	{
		if (!RightShips.Contains(ship))
		{
			return PortActionInfo.CreateInvalid();
		}
		PortActionInfo result = CanStashShip(ship);
		if (result.IsRelevant && RightShips.Count == 1)
		{
			return PortActionInfo.CreateValid(isEnabled: false, 0, result.ActionName, new TextObject("{=nW1cRMoP}You can't moor your only ship"));
		}
		return result;
	}

	public PortActionInfo GetCanViewStash(bool isSelectedRosterRightSide)
	{
		if (!isSelectedRosterRightSide && RightShips.Count != 0)
		{
			return PortActionInfo.CreateInvalid();
		}
		return CanViewStash(isSelectedRosterRightSide);
	}

	public abstract int GetTradeCostOfShip(Ship ship, bool isRightSideSelling);

	public abstract int GetRepairCostOfShip(Ship ship, bool isRightSideRepairing);

	public abstract int GetUpgradeCostOfShip(Ship ship, ShipUpgradePiece piece, bool isRightSideUpgrading);

	public abstract int GetTotalGoldCost();

	public abstract bool GetCanConfirm(out TextObject disabledHint);

	public abstract void OnConfirmChanges();

	public abstract List<PortChangeInfo> GetChanges();

	protected abstract PortActionInfo CanBuyShip(Ship ship);

	protected abstract PortActionInfo CanSellShip(Ship ship);

	protected abstract PortActionInfo CanRepairShip(Ship ship);

	protected abstract PortActionInfo CanRepairAll();

	protected abstract PortActionInfo CanUpgradeShip(Ship ship);

	protected abstract PortActionInfo CanRenameShip(Ship ship);

	protected abstract PortActionInfo CanSendToClan(Ship ship);

	protected abstract PortActionInfo CanStashShip(Ship ship);

	protected abstract PortActionInfo CanViewStash(bool isRightRoster);

	public virtual bool AreThereAnyChanges()
	{
		if (ShipsToBuy.Count <= 0 && ShipsToSell.Count <= 0 && ShipsToSend.Count <= 0 && ShipsToStash.Count <= 0 && ShipsToRetrieveFromStash.Count <= 0 && ShipsToRename.Count <= 0 && ShipsToRepair.Count <= 0 && SelectedShipPieces.Count <= 0)
		{
			return SelectedFigureheads.Count > 0;
		}
		return true;
	}

	public void OnBuyShip(Ship ship)
	{
		bool flag = false;
		if (_shipsToSell.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			flag = true;
			_shipsToSell.RemoveAll((ShipTradeInfo x) => x.Ship == ship);
		}
		else if (!_shipsToBuy.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			_shipsToBuy.Add(new ShipTradeInfo(ship, GetTradeCostOfShip(ship, isRightSideSelling: false)));
		}
		if (_leftShips.Contains(ship))
		{
			_leftShips.Remove(ship);
		}
		if (!_rightShips.Contains(ship))
		{
			_rightShips.Insert(0, ship);
		}
		ClearCurrentFigurehead(ship);
		if (flag)
		{
			ReequipPreviousFigurehead(ship);
		}
	}

	public void OnSellShip(Ship ship)
	{
		OnResetShipName(ship);
		OnResetShipUpgrade(ship);
		bool flag = false;
		if (_shipsToRepair.Contains(ship))
		{
			_shipsToRepair.Remove(ship);
		}
		if (_shipsToBuy.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			flag = true;
			_shipsToBuy.RemoveAll((ShipTradeInfo x) => x.Ship == ship);
		}
		else if (!_shipsToSell.Any((ShipTradeInfo x) => x.Ship == ship))
		{
			_shipsToSell.Add(new ShipTradeInfo(ship, GetTradeCostOfShip(ship, isRightSideSelling: true)));
		}
		if (_rightShips.Contains(ship))
		{
			_rightShips.Remove(ship);
		}
		if (!_leftShips.Contains(ship))
		{
			_leftShips.Insert(0, ship);
		}
		if (!flag)
		{
			ClearCurrentFigurehead(ship);
		}
	}

	public void OnRepairShip(Ship ship)
	{
		if (!_shipsToRepair.Contains(ship))
		{
			_shipsToRepair.Add(ship);
		}
	}

	public void OnSendToClan(Ship ship)
	{
		if (!_shipsToSend.Contains(ship))
		{
			_shipsToSend.Add(ship);
			_rightShips.Remove(ship);
		}
		ClearCurrentFigurehead(ship);
	}

	public void OnSendToStash(Ship ship)
	{
		bool flag = false;
		if (_shipsToRetrieveFromStash.Any((Ship x) => x == ship))
		{
			flag = true;
			_shipsToRetrieveFromStash.RemoveAll((Ship x) => x == ship);
		}
		else if (!_shipsToStash.Any((Ship x) => x == ship))
		{
			_shipsToStash.Add(ship);
		}
		if (_rightShips.Contains(ship))
		{
			_rightShips.Remove(ship);
		}
		if (!flag)
		{
			ClearCurrentFigurehead(ship);
		}
	}

	public void OnRetrieveFromStash(Ship ship)
	{
		bool flag = false;
		if (_shipsToStash.Any((Ship x) => x == ship))
		{
			flag = true;
			_shipsToStash.RemoveAll((Ship x) => x == ship);
		}
		else if (!_shipsToRetrieveFromStash.Any((Ship x) => x == ship))
		{
			_shipsToRetrieveFromStash.Add(ship);
		}
		if (!_rightShips.Contains(ship))
		{
			_rightShips.Insert(0, ship);
		}
		if (flag)
		{
			ReequipPreviousFigurehead(ship);
		}
	}

	public void OnRenameShip(Ship ship, string newName)
	{
		bool flag = false;
		for (int i = 0; i < _shipsToRename.Count; i++)
		{
			if (_shipsToRename[i].Ship == ship)
			{
				flag = true;
				_shipsToRename[i] = new ShipRenameInfo(ship, newName);
				break;
			}
		}
		if (!flag)
		{
			_shipsToRename.Add(new ShipRenameInfo(ship, newName));
		}
	}

	public void OnResetShipName(Ship ship)
	{
		for (int num = _shipsToRename.Count - 1; num >= 0; num--)
		{
			if (_shipsToRename[num].Ship == ship)
			{
				_shipsToRename.RemoveAt(num);
			}
		}
	}

	public void OnResetShipUpgrade(Ship ship)
	{
		for (int num = _selectedShipPieces.Count - 1; num >= 0; num--)
		{
			if (_selectedShipPieces[num].Ship == ship)
			{
				_selectedShipPieces.RemoveAt(num);
			}
		}
		for (int num2 = _selectedFigureheads.Count - 1; num2 >= 0; num2--)
		{
			if (_selectedFigureheads[num2].Ship == ship)
			{
				_selectedFigureheads.RemoveAt(num2);
			}
		}
	}

	public void OnUpgradePieceSelected(Ship ship, string shipSlotTag, ShipUpgradePiece piece)
	{
		bool flag = false;
		bool flag2 = ship.GetPieceAtSlot(shipSlotTag) == piece;
		for (int i = 0; i < _selectedShipPieces.Count; i++)
		{
			ShipUpgradePieceInfo shipUpgradePieceInfo = _selectedShipPieces[i];
			if (shipUpgradePieceInfo.Ship == ship && shipUpgradePieceInfo.ShipSlotTag == shipSlotTag)
			{
				flag = true;
				if (flag2)
				{
					_selectedShipPieces.RemoveAt(i);
				}
				else
				{
					_selectedShipPieces[i] = new ShipUpgradePieceInfo(ship, shipSlotTag, piece);
				}
				break;
			}
		}
		if (!flag && !flag2)
		{
			_selectedShipPieces.Add(new ShipUpgradePieceInfo(ship, shipSlotTag, piece));
		}
	}

	public void OnFigureheadSelected(Ship ship, Figurehead figurehead)
	{
		bool flag = false;
		bool flag2 = figurehead == ship.Figurehead;
		for (int i = 0; i < _selectedFigureheads.Count; i++)
		{
			if (_selectedFigureheads[i].Ship == ship)
			{
				flag = true;
				if (flag2)
				{
					_selectedFigureheads.RemoveAt(i);
				}
				else
				{
					_selectedFigureheads[i] = new ShipFigureheadInfo(ship, figurehead);
				}
				break;
			}
		}
		if (!flag && !flag2)
		{
			_selectedFigureheads.Add(new ShipFigureheadInfo(ship, figurehead));
		}
	}

	public void ResetChanges()
	{
		_shipsToBuy.Clear();
		_shipsToSell.Clear();
		_shipsToRename.Clear();
		_shipsToRepair.Clear();
		_selectedShipPieces.Clear();
		_selectedFigureheads.Clear();
		_shipsToSend.Clear();
		_shipsToStash.Clear();
		_shipsToRetrieveFromStash.Clear();
		_leftShips.Clear();
		_rightShips.Clear();
		_leftShips.AddRange(_initialLeftShips);
		_rightShips.AddRange(_initialRightShips);
	}

	private void ClearCurrentFigurehead(Ship ship)
	{
		Figurehead figurehead = ship.Figurehead;
		for (int i = 0; i < _selectedFigureheads.Count; i++)
		{
			ShipFigureheadInfo shipFigureheadInfo = _selectedFigureheads[i];
			if (shipFigureheadInfo.Ship == ship)
			{
				figurehead = shipFigureheadInfo.Figurehead;
				break;
			}
		}
		if (figurehead != null)
		{
			OnFigureheadSelected(ship, null);
		}
	}

	private void ReequipPreviousFigurehead(Ship ship)
	{
		Figurehead figurehead = ship.Figurehead;
		bool flag = false;
		for (int i = 0; i < _selectedFigureheads.Count; i++)
		{
			ShipFigureheadInfo shipFigureheadInfo = _selectedFigureheads[i];
			if (shipFigureheadInfo.Figurehead == figurehead && shipFigureheadInfo.Ship != null)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			OnFigureheadSelected(ship, figurehead);
		}
	}
}
