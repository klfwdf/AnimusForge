using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerTroopTypeIconWidget : Widget
{
	private Widget _backgroundWidget;

	private Widget _foregroundWidget;

	private string _iconSpriteType;

	public float ScaleFactor { get; set; } = 1f;

	[DataSourceProperty]
	public Widget BackgroundWidget
	{
		get
		{
			return _backgroundWidget;
		}
		set
		{
			if (_backgroundWidget != value)
			{
				_backgroundWidget = value;
				OnPropertyChanged(value, "BackgroundWidget");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public Widget ForegroundWidget
	{
		get
		{
			return _foregroundWidget;
		}
		set
		{
			if (_foregroundWidget != value)
			{
				_foregroundWidget = value;
				OnPropertyChanged(value, "ForegroundWidget");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public string IconSpriteType
	{
		get
		{
			return _iconSpriteType;
		}
		set
		{
			if (_iconSpriteType != value)
			{
				_iconSpriteType = value;
				OnPropertyChanged(value, "IconSpriteType");
				UpdateIcon();
			}
		}
	}

	public MultiplayerTroopTypeIconWidget(UIContext context)
		: base(context)
	{
		BackgroundWidget = this;
	}

	private void UpdateIcon()
	{
		if (BackgroundWidget != null && ForegroundWidget != null && !string.IsNullOrEmpty(IconSpriteType))
		{
			string text = "MPHud\\TroopIcons\\" + IconSpriteType;
			string name = text + "_Outline";
			ForegroundWidget.Sprite = base.Context.SpriteData.GetSprite(text);
			BackgroundWidget.Sprite = base.Context.SpriteData.GetSprite(name);
			if (BackgroundWidget.Sprite != null)
			{
				float num = BackgroundWidget.Sprite.Width;
				BackgroundWidget.SuggestedWidth = num * ScaleFactor;
				ForegroundWidget.SuggestedWidth = num * ScaleFactor;
				float num2 = BackgroundWidget.Sprite.Height;
				BackgroundWidget.SuggestedHeight = num2 * ScaleFactor;
				ForegroundWidget.SuggestedHeight = num2 * ScaleFactor;
			}
		}
	}
}
