using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.ProfileSelection;

public class ProfileSelectionVM : ViewModel
{
	private bool _isPlayEnabled;

	private string _selectProfileText;

	private string _playText;

	private InputKeyItemVM _playKey;

	private InputKeyItemVM _selectProfileKey;

	[DataSourceProperty]
	public string SelectProfileText
	{
		get
		{
			return _selectProfileText;
		}
		set
		{
			if (value != _selectProfileText)
			{
				_selectProfileText = value;
				OnPropertyChangedWithValue(value, "SelectProfileText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayEnabled
	{
		get
		{
			return _isPlayEnabled;
		}
		set
		{
			if (value != _isPlayEnabled)
			{
				_isPlayEnabled = value;
				OnPropertyChangedWithValue(value, "IsPlayEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string PlayText
	{
		get
		{
			return _playText;
		}
		set
		{
			if (value != _playText)
			{
				_playText = value;
				OnPropertyChangedWithValue(value, "PlayText");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SelectProfileKey
	{
		get
		{
			return _selectProfileKey;
		}
		set
		{
			if (value != _selectProfileKey)
			{
				_selectProfileKey = value;
				OnPropertyChangedWithValue(value, "SelectProfileKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PlayKey
	{
		get
		{
			return _playKey;
		}
		set
		{
			if (value != _playKey)
			{
				_playKey = value;
				OnPropertyChangedWithValue(value, "PlayKey");
			}
		}
	}

	public ProfileSelectionVM(bool isDirectPlayPossible)
	{
		SelectProfileText = new TextObject("{=wubDWOlh}Select Profile").ToString();
		SelectProfileKey = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SelectProfile"), isConsoleOnly: false);
		PlayKey = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Play"), isConsoleOnly: false);
	}

	public void OnActivate(bool isDirectPlayPossible)
	{
		IsPlayEnabled = isDirectPlayPossible;
		if (!string.IsNullOrEmpty(PlatformServices.Instance.UserDisplayName))
		{
			PlayText = new TextObject("{=FTXx0aRp}Play as").ToString() + PlatformServices.Instance.UserDisplayName;
		}
		else
		{
			PlayText = new TextObject("{=playgame}Play").ToString();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		SelectProfileKey.OnFinalize();
		PlayKey.OnFinalize();
	}
}
