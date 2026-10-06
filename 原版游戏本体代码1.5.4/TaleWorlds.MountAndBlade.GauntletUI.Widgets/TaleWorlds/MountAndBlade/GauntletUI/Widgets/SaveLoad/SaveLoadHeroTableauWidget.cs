using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.SaveLoad;

public class SaveLoadHeroTableauWidget : TextureWidget
{
	private string _heroVisualCode;

	private string _bannerCode;

	public bool IsVersionCompatible => GetTextureProviderProperty<bool>("IsVersionCompatible") == true;

	[Editor(false)]
	public string HeroVisualCode
	{
		get
		{
			return _heroVisualCode;
		}
		set
		{
			if (value != _heroVisualCode)
			{
				_heroVisualCode = value;
				OnPropertyChanged(value, "HeroVisualCode");
				SetTextureProviderProperty("HeroVisualCode", value);
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

	public SaveLoadHeroTableauWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "SaveLoadHeroTableauTextureProvider";
		_isRenderRequestedPreviousFrame = true;
	}

	protected override void OnMousePressed()
	{
		SetTextureProviderProperty("CurrentlyRotating", true);
	}

	protected override void OnMouseReleased(bool isFromInput)
	{
		SetTextureProviderProperty("CurrentlyRotating", false);
	}
}
