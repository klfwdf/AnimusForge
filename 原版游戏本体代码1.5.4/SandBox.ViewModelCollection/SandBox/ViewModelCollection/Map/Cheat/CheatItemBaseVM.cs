using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Cheat;

public abstract class CheatItemBaseVM : ViewModel
{
	private string _name;

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

	public CheatItemBaseVM()
	{
	}

	public abstract void ExecuteAction();
}
