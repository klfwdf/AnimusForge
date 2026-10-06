using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(Concept))]
public class EncyclopediaConceptPageVM : EncyclopediaContentPageVM
{
	private Concept _concept;

	private string _titleText;

	private string _descriptionText;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	public EncyclopediaConceptPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_concept = base.Obj as Concept;
		Concept.SetConceptTextLinks();
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_concept);
		RefreshValues();
		Refresh();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = _concept.Title.ToString();
		DescriptionText = _concept.Description.ToString();
		UpdateBookmarkHintText();
	}

	public override void Refresh()
	{
		base.IsLoadingOver = false;
		base.IsLoadingOver = true;
	}

	public override string GetName()
	{
		return _concept.Title.ToString();
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Concept", GameTexts.FindText("str_encyclopedia_concepts").ToString())), " \\ "), GetName());
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_concept);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_concept);
		}
	}
}
