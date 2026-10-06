using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipUpgradePieceVM : ShipUpgradePieceBaseVM
{
	public readonly ShipUpgradePiece Piece;

	public readonly Ship Ship;

	public static event Func<Ship, ShipUpgradePiece, int> GetUpgradePrice;

	public ShipUpgradePieceVM(ShipUpgradePiece piece, Ship ship, Action<ShipUpgradePieceBaseVM> onSelected)
		: base(onSelected)
	{
		Piece = piece;
		Ship = ship;
		base.UpgradePieceTier = (ShipUpgradePieceTier)TaleWorlds.Library.MathF.Clamp(Piece.RequiredPortLevel, 1f, 4f);
		base.Identifier = piece.StringId;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Piece.GetName().ToString();
		base.Price = ShipUpgradePieceVM.GetUpgradePrice?.Invoke(Ship, Piece) ?? 0;
	}

	protected override PropertyBasedTooltipVM GetProperties()
	{
		object[] invokedArgs = new object[1] { Piece };
		PropertyBasedTooltipVM propertyBasedTooltipVM = new PropertyBasedTooltipVM(typeof(ShipUpgradePiece), invokedArgs);
		if (!TextObject.IsNullOrEmpty(Piece.Description))
		{
			TooltipProperty item = new TooltipProperty(Piece.Description.ToString(), string.Empty, 0);
			propertyBasedTooltipVM.TooltipPropertyList.Insert(0, item);
			item = new TooltipProperty(" ", " ", 0);
			propertyBasedTooltipVM.TooltipPropertyList.Insert(1, item);
		}
		if (!base.IsInspectedFromSlot)
		{
			propertyBasedTooltipVM.AddProperty(" ", " ");
			if (base.IsSelected)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=OSoAVlqc}Equipped").ToString(), string.Empty);
			}
			else if (base.IsDisabled)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=DovqkMg1}Not Available In Settlement").ToString(), string.Empty);
			}
			else if (base.Price > 0)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=ebUrBmHK}Price").ToString(), base.Price.ToString());
			}
			else
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=Ve1E1wXz}Unlocked").ToString(), string.Empty);
			}
		}
		else if (!TextObject.IsNullOrEmpty(_slotHintText))
		{
			propertyBasedTooltipVM.AddProperty(" ", " ");
			propertyBasedTooltipVM.AddProperty(_slotHintText.ToString(), string.Empty);
		}
		return propertyBasedTooltipVM;
	}
}
