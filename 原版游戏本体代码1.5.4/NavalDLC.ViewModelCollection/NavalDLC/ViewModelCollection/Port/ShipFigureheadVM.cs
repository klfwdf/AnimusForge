using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipFigureheadVM : ShipUpgradePieceBaseVM
{
	public Ship EquippedShip;

	public readonly Figurehead Figurehead;

	private readonly IViewDataTracker _viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();

	public ShipFigureheadVM(Figurehead figurehead, Action<ShipUpgradePieceBaseVM> onSelected)
		: base(onSelected)
	{
		Figurehead = figurehead;
		base.Price = 0;
		base.UpgradePieceTier = ShipUpgradePieceTier.Diamond;
		base.Identifier = figurehead.StringId;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Figurehead.Name.ToString();
	}

	protected override PropertyBasedTooltipVM GetProperties()
	{
		object[] invokedArgs = new object[1] { Figurehead };
		PropertyBasedTooltipVM propertyBasedTooltipVM = new PropertyBasedTooltipVM(typeof(Figurehead), invokedArgs);
		if (base.IsHiddenFromPlayer)
		{
			propertyBasedTooltipVM.TooltipPropertyList.Clear();
			propertyBasedTooltipVM.AddProperty(new TextObject("{=4RUs8Cfu}Not Unlocked").ToString(), string.Empty);
			return propertyBasedTooltipVM;
		}
		if (!base.IsInspectedFromSlot)
		{
			propertyBasedTooltipVM.AddProperty(" ", " ");
			if (base.IsSelected)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=OSoAVlqc}Equipped").ToString(), string.Empty);
			}
			else if (EquippedShip != null)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=bQzObjHj}Attached Ship").ToString(), EquippedShip.Name.ToString());
			}
			else if (base.IsDisabled)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=4RUs8Cfu}Not Unlocked").ToString(), string.Empty);
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

	public override void InspectPiece(bool isInspectedFromSlot = false, TextObject slotHintText = null)
	{
		base.InspectPiece(isInspectedFromSlot, slotHintText);
		if (base.IsUnexamined)
		{
			_viewDataTracker.OnFigureheadExamined(Figurehead);
		}
	}

	public override void Update()
	{
		base.Update();
		base.IsUnexamined = !base.IsDisabled && _viewDataTracker.UnexaminedFigureheads.Contains(Figurehead);
	}
}
