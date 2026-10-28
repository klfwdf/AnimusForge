using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public abstract class GenericOptionDataVM : ViewModel
{
	private TextObject _nameObj;

	private TextObject _descriptionObj;

	protected OptionsVM _optionsVM;

	protected IOptionData Option;

	private string _description;

	private string _name;

	private int _optionTypeId = -1;

	private string[] _imageIDs;

	private bool _isEnabled = true;

	private HintViewModel _hint;

	public bool IsNative => Option.IsNative();

	public bool IsAction => Option.IsAction();

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
	public string[] ImageIDs
	{
		get
		{
			return _imageIDs;
		}
		set
		{
			if (value != _imageIDs)
			{
				_imageIDs = value;
				OnPropertyChangedWithValue(value, "ImageIDs");
			}
		}
	}

	[DataSourceProperty]
	public int OptionTypeID
	{
		get
		{
			return _optionTypeId;
		}
		set
		{
			if (value != _optionTypeId)
			{
				_optionTypeId = value;
				OnPropertyChangedWithValue(value, "OptionTypeID");
			}
		}
	}

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
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	protected GenericOptionDataVM(OptionsVM optionsVM, IOptionData option, TextObject name, TextObject description, OptionsVM.OptionsDataType typeID)
	{
		_nameObj = name;
		_descriptionObj = description;
		_optionsVM = optionsVM;
		Option = option;
		OptionTypeID = (int)typeID;
		Hint = new HintViewModel();
		RefreshValues();
		UpdateEnableState();
	}

	public virtual void UpdateData(bool initUpdate)
	{
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameObj.ToString();
		Description = _descriptionObj.ToString();
	}

	public object GetOptionType()
	{
		return Option.GetOptionType();
	}

	public IOptionData GetOptionData()
	{
		return Option;
	}

	public void ResetToDefault()
	{
		SetValue(Option.GetDefaultValue());
	}

	public void UpdateEnableState()
	{
		(string, bool) isDisabledAndReasonID = Option.GetIsDisabledAndReasonID();
		if (!string.IsNullOrEmpty(isDisabledAndReasonID.Item1))
		{
			Hint.HintText = Module.CurrentModule.GlobalTextManager.FindText(isDisabledAndReasonID.Item1);
		}
		else
		{
			Hint.HintText = TextObject.GetEmpty();
		}
		IsEnabled = !isDisabledAndReasonID.Item2;
	}

	public abstract void UpdateValue();

	public abstract void Cancel();

	public abstract bool IsChanged();

	public abstract void SetValue(float value);

	public abstract void ResetData();

	public abstract void ApplyValue();
}
