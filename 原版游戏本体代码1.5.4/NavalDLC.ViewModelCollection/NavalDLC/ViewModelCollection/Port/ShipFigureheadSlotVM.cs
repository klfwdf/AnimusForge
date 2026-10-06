using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipFigureheadSlotVM : ShipUpgradeSlotBaseVM
{
	public delegate Figurehead GetCurrentFigureheadDelegate(Ship ship);

	public delegate Ship GetShipOfFigureheadDelegate(Figurehead figurehead, bool isRightSide);

	public delegate bool GetIsRightSideDelegate(Ship ship);

	private readonly Figurehead _initialSelectedFigurehead;

	private readonly List<Figurehead> _enabledFigureheads;

	public static event GetCurrentFigureheadDelegate GetCurrentFigurehead;

	public static event GetShipOfFigureheadDelegate GetShipOfFigurehead;

	public static event GetIsRightSideDelegate GetIsRightSide;

	public ShipFigureheadSlotVM(Ship ship, TextObject slotName, string shipSlotTag, string slotTypeId, Action<ShipUpgradeSlotBaseVM> onSelected)
		: base(ship, slotName, shipSlotTag, slotTypeId, onSelected)
	{
		_initialSelectedFigurehead = Ship.Figurehead;
		List<Figurehead> list = MBObjectManager.Instance.GetObjectTypeList<Figurehead>().ToList();
		_enabledFigureheads = Campaign.Current.UnlockedFigureheadsByMainHero?.ToList() ?? new List<Figurehead>();
		if (_initialSelectedFigurehead != null && !list.Contains(_initialSelectedFigurehead))
		{
			list.Add(_initialSelectedFigurehead);
		}
		foreach (Figurehead enabledFigurehead in _enabledFigureheads)
		{
			if (!list.Contains(enabledFigurehead))
			{
				list.Add(enabledFigurehead);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Figurehead figurehead = list[i];
			ShipFigureheadVM shipFigureheadVM = new ShipFigureheadVM(figurehead, OnPieceSelected)
			{
				IsDisabled = !_enabledFigureheads.Contains(figurehead)
			};
			base.AvailablePieces.Add(shipFigureheadVM);
			if (figurehead == _initialSelectedFigurehead)
			{
				base.SelectedPiece = shipFigureheadVM;
			}
		}
		base.AvailablePieces.Sort(new UpgradePieceComparer());
	}

	public override void ResetPieces()
	{
		UpdateAvailableFigureheads();
		base.IsChanged = false;
	}

	protected override bool GetIsChanged()
	{
		return (base.SelectedPiece as ShipFigureheadVM)?.Figurehead != _initialSelectedFigurehead;
	}

	public void UpdateAvailableFigureheads()
	{
		Figurehead currentFigurehead = ShipFigureheadSlotVM.GetCurrentFigurehead?.Invoke(Ship);
		base.SelectedPiece = base.AvailablePieces.FirstOrDefault((ShipUpgradePieceBaseVM x) => (x as ShipFigureheadVM)?.Figurehead == currentFigurehead);
		bool isRightSide = ShipFigureheadSlotVM.GetIsRightSide?.Invoke(Ship) ?? true;
		for (int num = 0; num < base.AvailablePieces.Count; num++)
		{
			ShipUpgradePieceBaseVM shipUpgradePieceBaseVM = base.AvailablePieces[num];
			ShipFigureheadVM shipFigureheadVM = shipUpgradePieceBaseVM as ShipFigureheadVM;
			shipFigureheadVM.EquippedShip = ShipFigureheadSlotVM.GetShipOfFigurehead?.Invoke(shipFigureheadVM.Figurehead, isRightSide);
			shipUpgradePieceBaseVM.IsDisabled = (shipFigureheadVM.EquippedShip != null && shipFigureheadVM.EquippedShip != Ship) || (shipFigureheadVM.EquippedShip == null && !_enabledFigureheads.Contains(shipFigureheadVM.Figurehead));
			shipUpgradePieceBaseVM.IsHiddenFromPlayer = shipFigureheadVM.EquippedShip == null && !_enabledFigureheads.Contains(shipFigureheadVM.Figurehead);
		}
		UpdateAnyBetterPiecesAvailable();
	}
}
