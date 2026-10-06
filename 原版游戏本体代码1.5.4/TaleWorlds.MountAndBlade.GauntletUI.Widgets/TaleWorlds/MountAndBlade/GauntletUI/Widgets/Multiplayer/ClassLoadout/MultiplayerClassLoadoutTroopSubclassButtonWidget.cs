using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutTroopSubclassButtonWidget : ButtonWidget
{
	private string _troopType;

	private Brush _iconBrush;

	private BrushWidget _iconWidget;

	private NavigationScopeTargeter _perksNavigationScopeTargeter;

	[DataSourceProperty]
	public string TroopType
	{
		get
		{
			return _troopType;
		}
		set
		{
			if (value != _troopType)
			{
				_troopType = value;
				OnPropertyChanged(value, "TroopType");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public Brush IconBrush
	{
		get
		{
			return _iconBrush;
		}
		set
		{
			if (value != _iconBrush)
			{
				_iconBrush = value;
				OnPropertyChanged(value, "IconBrush");
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

	public NavigationScopeTargeter PerksNavigationScopeTargeter
	{
		get
		{
			return _perksNavigationScopeTargeter;
		}
		set
		{
			if (value != _perksNavigationScopeTargeter)
			{
				_perksNavigationScopeTargeter = value;
				OnPropertyChanged(value, "PerksNavigationScopeTargeter");
				if (_perksNavigationScopeTargeter != null)
				{
					_perksNavigationScopeTargeter.IsScopeEnabled = false;
				}
			}
		}
	}

	public MultiplayerClassLoadoutTroopSubclassButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (string.IsNullOrEmpty(TroopType) || _iconWidget == null)
		{
			return;
		}
		Sprite sprite = IconBrush?.GetLayer(TroopType)?.Sprite;
		foreach (Style style in IconWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Sprite = sprite;
			}
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		base.ParentWidget?.SetState(base.CurrentState);
	}

	public override void SetState(string stateName)
	{
		base.SetState(stateName);
		if (PerksNavigationScopeTargeter != null)
		{
			PerksNavigationScopeTargeter.IsScopeEnabled = stateName == "Selected";
		}
	}
}
