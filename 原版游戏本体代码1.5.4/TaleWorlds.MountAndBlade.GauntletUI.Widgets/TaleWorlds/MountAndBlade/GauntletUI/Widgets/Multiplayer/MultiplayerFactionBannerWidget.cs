using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerFactionBannerWidget : Widget
{
	private bool _firstFrame = true;

	private Color _cultureColor1;

	private Color _cultureColor2;

	private string _factionCode;

	private Widget _bannerWidget;

	private Widget _iconWidget;

	[DataSourceProperty]
	public Color CultureColor1
	{
		get
		{
			return _cultureColor1;
		}
		set
		{
			if (value != _cultureColor1)
			{
				_cultureColor1 = value;
				OnPropertyChanged(value, "CultureColor1");
				UpdateBanner();
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor2
	{
		get
		{
			return _cultureColor2;
		}
		set
		{
			if (value != _cultureColor2)
			{
				_cultureColor2 = value;
				OnPropertyChanged(value, "CultureColor2");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public string FactionCode
	{
		get
		{
			return _factionCode;
		}
		set
		{
			if (value != _factionCode)
			{
				_factionCode = value;
				OnPropertyChanged(value, "FactionCode");
				UpdateIcon();
			}
		}
	}

	[DataSourceProperty]
	public Widget BannerWidget
	{
		get
		{
			return _bannerWidget;
		}
		set
		{
			if (value != _bannerWidget)
			{
				_bannerWidget = value;
				OnPropertyChanged(value, "BannerWidget");
				UpdateBanner();
			}
		}
	}

	[DataSourceProperty]
	public Widget IconWidget
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

	public MultiplayerFactionBannerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_firstFrame)
		{
			UpdateBanner();
			UpdateIcon();
			_firstFrame = false;
		}
	}

	private void UpdateBanner()
	{
		if (_bannerWidget == null)
		{
			return;
		}
		if (BannerWidget is BrushWidget brushWidget)
		{
			{
				foreach (Style style in brushWidget.Brush.Styles)
				{
					StyleLayer[] layers = style.GetLayers();
					for (int i = 0; i < layers.Length; i++)
					{
						layers[i].Color = CultureColor1;
					}
				}
				return;
			}
		}
		BannerWidget.Color = CultureColor1;
	}

	private void UpdateIcon()
	{
		if (!string.IsNullOrEmpty(FactionCode) && _iconWidget != null)
		{
			IconWidget.Sprite = base.Context.SpriteData.GetSprite("StdAssets\\FactionIcons\\LargeIcons\\" + FactionCode);
			IconWidget.Color = CultureColor2;
		}
	}
}
