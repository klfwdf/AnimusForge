using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerItemTabButtonWidget : ButtonWidget
{
	private const string BaseSpritePath = "StdAssets\\ItemIcons\\";

	private string _itemType;

	private BrushWidget _iconWidget;

	[Editor(false)]
	public string ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
				UpdateIcon();
			}
		}
	}

	[Editor(false)]
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

	public MultiplayerItemTabButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (!string.IsNullOrEmpty(ItemType) && _iconWidget != null)
		{
			Sprite sprite = base.Context.SpriteData.GetSprite("StdAssets\\ItemIcons\\" + ItemType);
			IconWidget.Brush.DefaultLayer.Sprite = sprite;
			Sprite sprite2 = base.Context.SpriteData.GetSprite("StdAssets\\ItemIcons\\" + ItemType + "_selected");
			IconWidget.Brush.GetLayer("Selected").Sprite = sprite2;
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (base.IsSelected && base.ParentWidget is Container)
		{
			(base.ParentWidget as Container).OnChildSelected(this);
		}
	}
}
