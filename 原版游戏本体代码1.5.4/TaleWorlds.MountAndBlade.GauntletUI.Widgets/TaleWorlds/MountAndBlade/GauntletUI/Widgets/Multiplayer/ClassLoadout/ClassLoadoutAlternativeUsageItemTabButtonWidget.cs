using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class ClassLoadoutAlternativeUsageItemTabButtonWidget : ButtonWidget
{
	private string _usageType;

	private BrushWidget _iconWidget;

	public string UsageType
	{
		get
		{
			return _usageType;
		}
		set
		{
			if (value != _usageType)
			{
				_usageType = value;
				OnPropertyChanged(value, "UsageType");
				UpdateIcon();
			}
		}
	}

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

	public ClassLoadoutAlternativeUsageItemTabButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (string.IsNullOrEmpty(UsageType) || _iconWidget == null)
		{
			return;
		}
		Sprite sprite = base.Context.SpriteData.GetSprite("MPClassLoadout\\UsageIcons\\" + UsageType);
		foreach (Style style in IconWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Sprite = sprite;
			}
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
