using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.SaveLoad;

public class SavedGameGroupVM : ViewModel
{
	private bool _isFilteredOut;

	private MBBindingList<SavedGameVM> _savedGamesList;

	private string _identifierID;

	[DataSourceProperty]
	public bool IsFilteredOut
	{
		get
		{
			return _isFilteredOut;
		}
		set
		{
			if (value != _isFilteredOut)
			{
				_isFilteredOut = value;
				OnPropertyChangedWithValue(value, "IsFilteredOut");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SavedGameVM> SavedGamesList
	{
		get
		{
			return _savedGamesList;
		}
		set
		{
			if (value != _savedGamesList)
			{
				_savedGamesList = value;
				OnPropertyChangedWithValue(value, "SavedGamesList");
			}
		}
	}

	[DataSourceProperty]
	public string IdentifierID
	{
		get
		{
			return _identifierID;
		}
		set
		{
			if (value != _identifierID)
			{
				_identifierID = value;
				OnPropertyChangedWithValue(value, "IdentifierID");
			}
		}
	}

	public SavedGameGroupVM()
	{
		SavedGamesList = new MBBindingList<SavedGameVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SavedGamesList.ApplyActionOnAllItems(delegate(SavedGameVM s)
		{
			s.RefreshValues();
		});
	}
}
