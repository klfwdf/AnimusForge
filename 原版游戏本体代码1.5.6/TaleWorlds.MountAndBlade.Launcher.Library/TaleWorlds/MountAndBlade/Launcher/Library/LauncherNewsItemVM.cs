using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherNewsItemVM : ViewModel
{
	private string _link;

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

	public LauncherNewsItemVM(NewsItem item, bool isMultiplayer)
	{
		Category = item.Title;
		Title = item.Description;
		NewsImageUrl = item.ImageSourcePath;
		_link = item.NewsLink + (isMultiplayer ? "?referrer=launchermp" : "?referrer=launchersp");
	}

	private void ExecuteOpenLink()
	{
		if (!string.IsNullOrEmpty(_link))
		{
			Process.Start(new ProcessStartInfo(_link)
			{
				UseShellExecute = true
			});
		}
	}
}
