using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

public class EncyclopediaPageVM : ViewModel
{
	private EncyclopediaPageArgs _args;

	private bool _isLoadingOver;

	private bool _isBookmarked;

	private HintViewModel _bookmarkHint;

	public object Obj => _args.Obj;

	[DataSourceProperty]
	public bool IsLoadingOver
	{
		get
		{
			return _isLoadingOver;
		}
		set
		{
			if (value != _isLoadingOver)
			{
				_isLoadingOver = value;
				OnPropertyChangedWithValue(value, "IsLoadingOver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBookmarked
	{
		get
		{
			return _isBookmarked;
		}
		set
		{
			if (value != _isBookmarked)
			{
				_isBookmarked = value;
				OnPropertyChanged("IsBookmarked");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BookmarkHint
	{
		get
		{
			return _bookmarkHint;
		}
		set
		{
			if (value != _bookmarkHint)
			{
				_bookmarkHint = value;
				OnPropertyChanged("BookmarkHint");
			}
		}
	}

	[DataSourceProperty]
	public virtual MBBindingList<EncyclopediaListItemVM> Items
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public virtual MBBindingList<EncyclopediaFilterGroupVM> FilterGroups
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public virtual EncyclopediaListSortControllerVM SortController
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public virtual string GetName()
	{
		return "";
	}

	public virtual string GetNavigationBarURL()
	{
		return "";
	}

	public virtual void Refresh()
	{
	}

	public EncyclopediaPageVM(EncyclopediaPageArgs args)
	{
		_args = args;
		BookmarkHint = new HintViewModel();
	}

	public virtual void OnTick()
	{
	}

	public virtual void ExecuteSwitchBookmarkedState()
	{
		IsBookmarked = !IsBookmarked;
		UpdateBookmarkHintText();
	}

	protected void UpdateBookmarkHintText()
	{
		if (IsBookmarked)
		{
			BookmarkHint.HintText = new TextObject("{=BV5exuPf}Remove From Bookmarks");
		}
		else
		{
			BookmarkHint.HintText = new TextObject("{=d8jrv3nA}Add To Bookmarks");
		}
	}
}
