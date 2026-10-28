using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPNewsItemVM : ViewModel
{
	private readonly string _link;

	private string _newsImageUrl;

	private string _category;

	private string _title;

	[DataSourceProperty]
	public string NewsImageUrl
	{
		get
		{
			return _newsImageUrl;
		}
		set
		{
			if (value != _newsImageUrl)
			{
				_newsImageUrl = value;
				OnPropertyChangedWithValue(value, "NewsImageUrl");
			}
		}
	}

	[DataSourceProperty]
	public string Category
	{
		get
		{
			return _category;
		}
		set
		{
			if (value != _category)
			{
				_category = value;
				OnPropertyChangedWithValue(value, "Category");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	public MPNewsItemVM(NewsItem item)
	{
		NewsImageUrl = item.ImageSourcePath;
		Category = item.Title;
		Title = item.Description;
		_link = item.NewsLink + "?referrer=lobby";
	}

	private void ExecuteOpenLink()
	{
		if (!string.IsNullOrEmpty(_link) && !PlatformServices.Instance.ShowOverlayForWebPage(_link).Result)
		{
			Process.Start(new ProcessStartInfo(_link)
			{
				UseShellExecute = true
			});
		}
	}
}
