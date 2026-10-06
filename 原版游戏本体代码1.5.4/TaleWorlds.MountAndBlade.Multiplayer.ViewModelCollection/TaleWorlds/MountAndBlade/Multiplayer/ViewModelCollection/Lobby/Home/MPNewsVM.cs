using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPNewsVM : ViewModel
{
	private NewsManager _newsManager;

	private const int _numOfNewsItemsToShow = 4;

	private MBReadOnlyList<NewsItem> _newsItemsCached;

	private bool _hasValidNews;

	private MPNewsItemVM _mainNews;

	private MBBindingList<MPNewsItemVM> _importantNews;

	[DataSourceProperty]
	public bool HasValidNews
	{
		get
		{
			return _hasValidNews;
		}
		set
		{
			if (value != _hasValidNews)
			{
				_hasValidNews = value;
				OnPropertyChangedWithValue(value, "HasValidNews");
			}
		}
	}

	[DataSourceProperty]
	public MPNewsItemVM MainNews
	{
		get
		{
			return _mainNews;
		}
		set
		{
			if (value != _mainNews)
			{
				_mainNews = value;
				OnPropertyChangedWithValue(value, "MainNews");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPNewsItemVM> ImportantNews
	{
		get
		{
			return _importantNews;
		}
		set
		{
			if (value != _importantNews)
			{
				_importantNews = value;
				OnPropertyChangedWithValue(value, "ImportantNews");
			}
		}
	}

	public MPNewsVM(NewsManager newsManager)
	{
		_newsManager = newsManager;
		ImportantNews = new MBBindingList<MPNewsItemVM>();
		GetNewsItems();
	}

	private async void GetNewsItems()
	{
		if (_newsManager == null)
		{
			Debug.FailedAssert("News manager is null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Home\\MPNewsVM.cs", "GetNewsItems", 27);
			return;
		}
		_newsItemsCached = await _newsManager.GetNewsItems(forceRefresh: false);
		RefreshNews();
	}

	private void RefreshNews()
	{
		MainNews = null;
		ImportantNews.Clear();
		HasValidNews = false;
		if (_newsItemsCached == null)
		{
			Debug.FailedAssert("News items list is null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\Home\\MPNewsVM.cs", "RefreshNews", 43);
			return;
		}
		List<IGrouping<int, NewsItem>> list = (from i in (from i in _newsItemsCached.Where((NewsItem n) => n.Feeds.Any((NewsType t) => t.Type == NewsItem.NewsTypes.MultiplayerLobby) && !string.IsNullOrEmpty(n.Title) && !string.IsNullOrEmpty(n.NewsLink) && !string.IsNullOrEmpty(n.ImageSourcePath)).ToList()
				group i by i.Feeds.First((NewsType t) => t.Type == NewsItem.NewsTypes.MultiplayerLobby).Index).ToList()
			orderby i.Key
			select i).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			if (ImportantNews.Count + 1 >= 4)
			{
				break;
			}
			NewsItem newsItem = list[num].First();
			NewsItem item = (newsItem.Equals(default(NewsItem)) ? default(NewsItem) : newsItem);
			if (num == 0)
			{
				MainNews = new MPNewsItemVM(item);
			}
			else
			{
				ImportantNews.Add(new MPNewsItemVM(item));
			}
		}
		if (MainNews != null)
		{
			HasValidNews = true;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_newsManager = null;
	}
}
