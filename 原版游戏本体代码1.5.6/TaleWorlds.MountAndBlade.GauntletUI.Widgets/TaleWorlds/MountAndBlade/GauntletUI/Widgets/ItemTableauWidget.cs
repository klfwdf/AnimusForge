using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ItemTableauWidget : TextureWidget
{
	private string _itemModifierId;

	private string _stringId;

	private float _initialTiltRotation;

	private float _initialPanRotation;

	private string _bannerCode;

	[Editor(false)]
	public string ItemModifierId
	{
		get
		{
			return _itemModifierId;
		}
		set
		{
			if (value != _itemModifierId)
			{
				_itemModifierId = value;
				OnPropertyChanged(value, "ItemModifierId");
				SetTextureProviderProperty("ItemModifierId", value);
			}
		}
	}

	[Editor(false)]
	public string StringId
	{
		get
		{
			return _stringId;
		}
		set
		{
			if (value != _stringId)
			{
				_stringId = value;
				OnPropertyChanged(value, "StringId");
				if (value != null)
				{
					SetTextureProviderProperty("StringId", value);
				}
			}
		}
	}

	[Editor(false)]
	public float InitialTiltRotation
	{
		get
		{
			return _initialTiltRotation;
		}
		set
		{
			if (value != _initialTiltRotation)
			{
				_initialTiltRotation = value;
				OnPropertyChanged(value, "InitialTiltRotation");
				SetTextureProviderProperty("InitialTiltRotation", value);
			}
		}
	}

	[Editor(false)]
	public float InitialPanRotation
	{
		get
		{
			return _initialPanRotation;
		}
		set
		{
			if (value != _initialPanRotation)
			{
				_initialPanRotation = value;
				OnPropertyChanged(value, "InitialPanRotation");
				SetTextureProviderProperty("InitialPanRotation", value);
			}
		}
	}

	[Editor(false)]
	public string BannerCode
	{
		get
		{
			return _bannerCode;
		}
		set
		{
			if (value != _bannerCode)
			{
				_bannerCode = value;
				OnPropertyChanged(value, "BannerCode");
				SetTextureProviderProperty("BannerCode", value);
			}
		}
	}

	public ItemTableauWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "ItemTableauTextureProvider";
	}

	protected override bool OnPreviewMouseScroll()
	{
		return true;
	}

	protected override void OnMouseScroll()
	{
		base.OnMouseScroll();
		SetTextureProviderProperty("CurrentZoom", Input.DeltaMouseScroll * 0.1f);
	}

	protected override void OnMousePressed()
	{
		SetTextureProviderProperty("CurrentlyRotating", true);
	}

	protected override void OnRightStickMovement()
	{
		base.OnRightStickMovement();
		SetTextureProviderProperty("RotateItemVertical", base.EventManager.RightStickVerticalScrollAmount);
		SetTextureProviderProperty("RotateItemHorizontal", base.EventManager.RightStickHorizontalScrollAmount);
	}

	protected override void OnMouseReleased(bool isFromInput)
	{
		SetTextureProviderProperty("CurrentlyRotating", false);
	}

	protected override bool OnPreviewRightStickMovement()
	{
		return true;
	}
}
