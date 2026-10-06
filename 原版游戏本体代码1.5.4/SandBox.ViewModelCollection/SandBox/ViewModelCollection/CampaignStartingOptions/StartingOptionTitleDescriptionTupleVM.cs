using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class StartingOptionTitleDescriptionTupleVM : ViewModel
{
	private string _name;

	private string _description;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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

	public StartingOptionTitleDescriptionTupleVM(string name, string description)
	{
		Name = name;
		Description = description;
	}
}
