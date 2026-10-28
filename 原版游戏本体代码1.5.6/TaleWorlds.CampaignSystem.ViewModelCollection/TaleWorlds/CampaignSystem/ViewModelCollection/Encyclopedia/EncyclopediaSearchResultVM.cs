using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaSearchResultVM : ViewModel
{
	private string _searchedText;

	public readonly int MatchStartIndex;

	public string LinkId = "";

	public string PageType;

	public string _nameText;

	public string OrgNameText { get; private set; }

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (_nameText != value)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	public EncyclopediaSearchResultVM(EncyclopediaListItem source, string searchedText, int matchStartIndex)
	{
		MatchStartIndex = matchStartIndex;
		LinkId = source.Id;
		PageType = source.TypeName;
		OrgNameText = source.Name;
		_nameText = source.Name;
		UpdateSearchedText(searchedText);
	}

	public void UpdateSearchedText(string searchedText)
	{
		_searchedText = searchedText;
		if (string.IsNullOrEmpty(OrgNameText))
		{
			return;
		}
		int num = OrgNameText.IndexOf(_searchedText, StringComparison.InvariantCultureIgnoreCase);
		if (num < 0)
		{
			return;
		}
		int num2 = MBMath.ClampInt(_searchedText.Length, 0, OrgNameText.Length - num);
		if (num2 != 0)
		{
			string text = OrgNameText.Substring(num, num2);
			if (!string.IsNullOrEmpty(text))
			{
				NameText = OrgNameText.Replace(text, "<a>" + text + "</a>");
			}
		}
	}

	public void Execute()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(PageType, LinkId);
	}
}
