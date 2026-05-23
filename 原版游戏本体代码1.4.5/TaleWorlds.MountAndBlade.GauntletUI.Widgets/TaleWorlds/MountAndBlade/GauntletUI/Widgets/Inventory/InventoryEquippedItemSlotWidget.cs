using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryEquippedItemSlotWidget : InventoryItemButtonWidget
{
	private ImageIdentifierWidget _imageIdentifier;

	private Widget _background;

	private int _targetEquipmentIndex;

	[Editor(false)]
	public ImageIdentifierWidget ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (_imageIdentifier != value)
			{
				if (_imageIdentifier != null)
				{
					_imageIdentifier.PropertyChanged -= ImageIdentifierOnPropertyChanged;
				}
				_imageIdentifier = value;
				if (_imageIdentifier != null)
				{
					_imageIdentifier.PropertyChanged += ImageIdentifierOnPropertyChanged;
				}
				OnPropertyChanged(value, "ImageIdentifier");
			}
		}
	}

	[Editor(false)]
	public Widget Background
	{
		get
		{
			return _background;
		}
		set
		{
			if (_background != value)
			{
				_background = value;
				_background.AddState("Selected");
				OnPropertyChanged(value, "Background");
			}
		}
	}

	[Editor(false)]
	public int TargetEquipmentIndex
	{
		get
		{
			return _targetEquipmentIndex;
		}
		set
		{
			if (_targetEquipmentIndex != value)
			{
				_targetEquipmentIndex = value;
				OnPropertyChanged(value, "TargetEquipmentIndex");
			}
		}
	}

	public InventoryEquippedItemSlotWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.ScreenWidget != null && Background != null)
		{
			bool num = base.ScreenWidget.TargetEquipmentIndex == TargetEquipmentIndex;
			bool flag = TargetEquipmentIndex == 0 && base.ScreenWidget.TargetEquipmentIndex >= 0 && base.ScreenWidget.TargetEquipmentIndex <= 3;
			if (num || flag)
			{
				Background.SetState("Selected");
			}
			else
			{
				Background.SetState("Default");
			}
		}
	}

	private void ImageIdentifierOnPropertyChanged(PropertyOwnerObject owner, string propertyName, object value)
	{
		if (propertyName == "ImageId")
		{
			base.IsHidden = string.IsNullOrEmpty((string)value);
		}
	}
}
