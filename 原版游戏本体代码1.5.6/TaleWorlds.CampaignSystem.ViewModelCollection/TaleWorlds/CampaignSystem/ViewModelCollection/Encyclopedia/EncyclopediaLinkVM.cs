using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaLinkVM : ViewModel
{
	private string _activeLink;

	[DataSourceProperty]
	public string ActiveLink
	{
		get
		{
			return _activeLink;
		}
		set
		{
			if (_activeLink != value)
			{
				_activeLink = value;
				OnPropertyChangedWithValue(value, "ActiveLink");
			}
		}
	}

	public void ExecuteActiveLink()
	{
		if (!string.IsNullOrEmpty(ActiveLink))
		{
			Campaign.Current.EncyclopediaManager.GoToLink(ActiveLink);
		}
	}
}
