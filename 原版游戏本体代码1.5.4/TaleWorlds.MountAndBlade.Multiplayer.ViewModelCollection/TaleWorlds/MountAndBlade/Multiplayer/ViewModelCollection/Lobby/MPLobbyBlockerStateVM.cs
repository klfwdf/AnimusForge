using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyBlockerStateVM : ViewModel
{
	private Action<bool> _setNavigationRestriction;

	private TextObject _descriptionObj;

	private bool _isEnabled;

	private string _description;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				_setNavigationRestriction(value);
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

	public MPLobbyBlockerStateVM(Action<bool> setNavigationRestriction)
	{
		_setNavigationRestriction = setNavigationRestriction;
	}

	public void OnLobbyStateIsBlocker(TextObject description)
	{
		_descriptionObj = description;
		IsEnabled = true;
		Description = _descriptionObj.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionObj?.ToString();
	}

	public void OnLobbyStateNotBlocker()
	{
		IsEnabled = false;
	}
}
