using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipUpgradeSlotVM : ShipUpgradeSlotBaseVM
{
	private readonly ShipUpgradePiece _initialSelectedPiece;

	public ShipUpgradeSlotVM(Ship ship, TextObject slotName, string shipSlotTag, string slotTypeId, Action<ShipUpgradeSlotBaseVM> onSelected)
		: base(ship, slotName, shipSlotTag, slotTypeId, onSelected)
	{
		_initialSelectedPiece = Ship.GetPieceAtSlot(ShipSlotTag);
		List<ShipUpgradePiece> list = (from x in MBObjectManager.Instance.GetObjectTypeList<ShipUpgradePiece>()
			where !x.NotMerchandise && x.DoesPieceMatchSlot(Ship.ShipHull.AvailableSlots[ShipSlotTag])
			select x).ToList();
		List<ShipUpgradePiece> list2 = new List<ShipUpgradePiece>();
		if (Settlement.CurrentSettlement?.Town != null)
		{
			list2 = (from x in Settlement.CurrentSettlement.Town.GetAvailableShipUpgradePieces()
				where x.DoesPieceMatchSlot(Ship.ShipHull.AvailableSlots[ShipSlotTag])
				select x).ToList();
		}
		if (_initialSelectedPiece != null && !list.Contains(_initialSelectedPiece))
		{
			list.Add(_initialSelectedPiece);
		}
		if (Ship.UnlockedUpgradePieces != null)
		{
			foreach (ShipUpgradePiece unlockedUpgradePiece in Ship.UnlockedUpgradePieces)
			{
				if (unlockedUpgradePiece.DoesPieceMatchSlot(Ship.ShipHull.AvailableSlots[ShipSlotTag]) && !list.Contains(unlockedUpgradePiece))
				{
					list.Add(unlockedUpgradePiece);
				}
			}
		}
		if (_initialSelectedPiece != null && !list2.Contains(_initialSelectedPiece))
		{
			list2.Add(_initialSelectedPiece);
		}
		if (Ship.UnlockedUpgradePieces != null)
		{
			foreach (ShipUpgradePiece unlockedUpgradePiece2 in Ship.UnlockedUpgradePieces)
			{
				if (unlockedUpgradePiece2.DoesPieceMatchSlot(Ship.ShipHull.AvailableSlots[ShipSlotTag]) && !list2.Contains(unlockedUpgradePiece2))
				{
					list2.Add(unlockedUpgradePiece2);
				}
			}
		}
		_ = Ship.ShipHull.AvailableSlots[ShipSlotTag];
		for (int num = 0; num < list.Count; num++)
		{
			ShipUpgradePiece shipUpgradePiece = list[num];
			ShipUpgradePieceVM shipUpgradePieceVM = new ShipUpgradePieceVM(shipUpgradePiece, Ship, OnPieceSelected)
			{
				IsDisabled = !list2.Contains(shipUpgradePiece)
			};
			base.AvailablePieces.Add(shipUpgradePieceVM);
			if (shipUpgradePiece == _initialSelectedPiece)
			{
				base.SelectedPiece = shipUpgradePieceVM;
			}
		}
		base.AvailablePieces.Sort(new UpgradePieceComparer());
		UpdateAnyBetterPiecesAvailable();
	}

	public override void ResetPieces()
	{
		base.SelectedPiece = base.AvailablePieces.FirstOrDefault((ShipUpgradePieceBaseVM x) => (x as ShipUpgradePieceVM)?.Piece == _initialSelectedPiece);
		base.IsChanged = false;
	}

	protected override bool GetIsChanged()
	{
		return (base.SelectedPiece as ShipUpgradePieceVM)?.Piece != _initialSelectedPiece;
	}
}
