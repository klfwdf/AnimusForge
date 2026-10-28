using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public abstract class KeyOptionVM : ViewModel
{
	private Key _currentKey;

	private Key _key;

	protected readonly string _groupId;

	protected readonly string _id;

	protected readonly Action<KeyOptionVM> _onKeybindRequest;

	private string _optionValueText;

	private string _name;

	private string _description;

	private string _extraInformationText;

	private bool _isChanged;

	private HintViewModel _revertHint;

	public Key CurrentKey
	{
		get
		{
			return _currentKey;
		}
		protected set
		{
			_currentKey = value;
			UpdateIsChanged();
		}
	}

	public Key Key
	{
		get
		{
			return _key;
		}
		protected set
		{
			_key = value;
			UpdateIsChanged();
		}
	}

	[DataSourceProperty]
	public string OptionValueText
	{
		get
		{
			return _optionValueText;
		}
		set
		{
			if (value != _optionValueText)
			{
				_optionValueText = value;
				OnPropertyChangedWithValue(value, "OptionValueText");
			}
		}
	}

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

	[DataSourceProperty]
	public bool IsChanged
	{
		get
		{
			return _isChanged;
		}
		set
		{
			if (value != _isChanged)
			{
				_isChanged = value;
				OnPropertyChangedWithValue(value, "IsChanged");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RevertHint
	{
		get
		{
			return _revertHint;
		}
		set
		{
			if (value != _revertHint)
			{
				_revertHint = value;
				OnPropertyChangedWithValue(value, "RevertHint");
			}
		}
	}

	[DataSourceProperty]
	public string ExtraInformationText
	{
		get
		{
			return _extraInformationText;
		}
		set
		{
			if (value != _extraInformationText)
			{
				_extraInformationText = value;
				OnPropertyChangedWithValue(value, "ExtraInformationText");
			}
		}
	}

	public KeyOptionVM(string groupId, string id, Action<KeyOptionVM> onKeybindRequest)
	{
		_groupId = groupId;
		_id = id;
		_onKeybindRequest = onKeybindRequest;
		RevertHint = new HintViewModel(new TextObject("{=ftM2TjQ5}Revert changes"));
	}

	public abstract void Set(InputKey newKey);

	public abstract void Update();

	public abstract void OnDone();

	public abstract void ExecuteRevert();

	internal abstract void UpdateIsChanged();
}
