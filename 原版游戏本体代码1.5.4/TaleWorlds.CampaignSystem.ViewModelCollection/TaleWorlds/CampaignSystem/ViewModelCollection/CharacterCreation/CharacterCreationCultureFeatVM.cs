using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationCultureFeatVM : ViewModel
{
	private bool _isPositive;

	private string _description;

	[DataSourceProperty]
	public bool IsPositive
	{
		get
		{
			return _isPositive;
		}
		set
		{
			if (value != _isPositive)
			{
				_isPositive = value;
				OnPropertyChangedWithValue(value, "IsPositive");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	public CharacterCreationCultureFeatVM(bool isPositive, string description)
	{
		IsPositive = isPositive;
		Description = description;
	}
}
