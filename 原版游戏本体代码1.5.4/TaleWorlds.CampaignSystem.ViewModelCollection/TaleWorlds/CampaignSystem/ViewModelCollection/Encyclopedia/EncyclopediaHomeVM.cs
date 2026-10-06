using System.Linq;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaHomeVM : EncyclopediaPageVM
{
	private string _baseName;

	private MBBindingList<ListTypeVM> _lists;

	private bool _isListActive;

	private string _homeTitleText;

	[DataSourceProperty]
	public bool IsListActive
	{
		get
		{
			return _isListActive;
		}
		set
		{
			if (value != _isListActive)
			{
				_isListActive = value;
				OnPropertyChangedWithValue(value, "IsListActive");
			}
		}
	}

	[DataSourceProperty]
	public string HomeTitleText
	{
		get
		{
			return _homeTitleText;
		}
		set
		{
			if (value != _homeTitleText)
			{
				_homeTitleText = value;
				OnPropertyChangedWithValue(value, "HomeTitleText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ListTypeVM> Lists
	{
		get
		{
			return _lists;
		}
		set
		{
			if (value != _lists)
			{
				_lists = value;
				OnPropertyChangedWithValue(value, "Lists");
			}
		}
	}

	public EncyclopediaHomeVM(EncyclopediaPageArgs args)
		: base(args)
	{
		Lists = new MBBindingList<ListTypeVM>();
		foreach (EncyclopediaPage item in from p in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages()
			orderby p.HomePageOrderIndex
			select p)
		{
			if (item.IsRelevant())
			{
				Lists.Add(new ListTypeVM(item));
			}
		}
		RefreshValues();
	}

	public override void Refresh()
	{
		base.Refresh();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_baseName = GameTexts.FindText("str_encyclopedia_name").ToString();
		HomeTitleText = GameTexts.FindText("str_encyclopedia_name").ToString();
		Lists.ApplyActionOnAllItems(delegate(ListTypeVM x)
		{
			x.RefreshValues();
		});
	}

	public override string GetNavigationBarURL()
	{
		return GameTexts.FindText("str_encyclopedia_home").ToString() + " \\";
	}

	public override string GetName()
	{
		return _baseName;
	}
}
