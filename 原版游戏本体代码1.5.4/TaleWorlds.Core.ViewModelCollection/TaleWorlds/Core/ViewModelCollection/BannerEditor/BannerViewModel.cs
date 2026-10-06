using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor;

public class BannerViewModel : ViewModel
{
	public Banner Banner { get; }

	[DataSourceProperty]
	public string BannerCode
	{
		get
		{
			return Banner.BannerCode;
		}
		set
		{
			if (value != Banner.BannerCode)
			{
				Banner.Deserialize(value);
				OnPropertyChangedWithValue(value, "BannerCode");
			}
		}
	}

	public BannerViewModel(Banner banner)
	{
		Banner = banner;
	}
}
