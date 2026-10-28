using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Perks;

public class MultiplayerPerkItemToggleWidget : ToggleButtonWidget
{
	private string _iconType;

	private BrushWidget _iconWidget;

	private bool _isSelectable;

	private MultiplayerPerkContainerPanelWidget _containerPanel;

	[DataSourceProperty]
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChanged(value, "IconType");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public BrushWidget IconWidget
	{
		get
		{
			return _iconWidget;
		}
		set
		{
			if (value != _iconWidget)
			{
				_iconWidget = value;
				OnPropertyChanged(value, "IconWidget");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChanged(value, "IsSelectable");
			}
		}
	}

	[DataSourceProperty]
	public MultiplayerPerkContainerPanelWidget ContainerPanel
	{
		get
		{
			return _containerPanel;
		}
		set
		{
			if (value != _containerPanel)
			{
				_containerPanel = value;
				OnPropertyChanged(value, "ContainerPanel");
			}
		}
	}

	public MultiplayerPerkItemToggleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		ContainerPanel?.PerkSelected(_isSelectable ? this : null);
	}

	private void UpdateIcon()
	{
		if (string.IsNullOrEmpty(IconType) || _iconWidget == null)
		{
			return;
		}
		foreach (Style style in IconWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Sprite = base.Context.SpriteData.GetSprite("General\\Perks\\" + IconType);
			}
		}
	}
}
