using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Inquiries;

public abstract class PopUpBaseVM : ViewModel
{
	protected Action _affirmativeAction;

	protected Action _negativeAction;

	private Action _closeQuery;

	private string _titleText;

	private string _popUpLabel;

	private string _buttonOkLabel;

	private string _buttonCancelLabel;

	private bool _isButtonOkShown;

	private bool _isButtonCancelShown;

	private bool _isButtonOkEnabled;

	private bool _isButtonCancelEnabled;

	private HintViewModel _buttonOkHint;

	private HintViewModel _buttonCancelHint;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

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
	public string PopUpLabel
	{
		get
		{
			return _popUpLabel;
		}
		set
		{
			if (value != _popUpLabel)
			{
				_popUpLabel = value;
				OnPropertyChangedWithValue(value, "PopUpLabel");
			}
		}
	}

	[DataSourceProperty]
	public string ButtonOkLabel
	{
		get
		{
			return _buttonOkLabel;
		}
		set
		{
			if (value != _buttonOkLabel)
			{
				_buttonOkLabel = value;
				OnPropertyChangedWithValue(value, "ButtonOkLabel");
			}
		}
	}

	[DataSourceProperty]
	public string ButtonCancelLabel
	{
		get
		{
			return _buttonCancelLabel;
		}
		set
		{
			if (value != _buttonCancelLabel)
			{
				_buttonCancelLabel = value;
				OnPropertyChangedWithValue(value, "ButtonCancelLabel");
			}
		}
	}

	[DataSourceProperty]
	public bool IsButtonOkShown
	{
		get
		{
			return _isButtonOkShown;
		}
		set
		{
			if (value != _isButtonOkShown)
			{
				_isButtonOkShown = value;
				OnPropertyChangedWithValue(value, "IsButtonOkShown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsButtonCancelShown
	{
		get
		{
			return _isButtonCancelShown;
		}
		set
		{
			if (value != _isButtonCancelShown)
			{
				_isButtonCancelShown = value;
				OnPropertyChangedWithValue(value, "IsButtonCancelShown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsButtonOkEnabled
	{
		get
		{
			return _isButtonOkEnabled;
		}
		set
		{
			if (value != _isButtonOkEnabled)
			{
				_isButtonOkEnabled = value;
				OnPropertyChangedWithValue(value, "IsButtonOkEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsButtonCancelEnabled
	{
		get
		{
			return _isButtonCancelEnabled;
		}
		set
		{
			if (value != _isButtonCancelEnabled)
			{
				_isButtonCancelEnabled = value;
				OnPropertyChangedWithValue(value, "IsButtonCancelEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ButtonOkHint
	{
		get
		{
			return _buttonOkHint;
		}
		set
		{
			if (value != _buttonOkHint)
			{
				_buttonOkHint = value;
				OnPropertyChangedWithValue(value, "ButtonOkHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ButtonCancelHint
	{
		get
		{
			return _buttonCancelHint;
		}
		set
		{
			if (value != _buttonCancelHint)
			{
				_buttonCancelHint = value;
				OnPropertyChangedWithValue(value, "ButtonCancelHint");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public PopUpBaseVM(Action closeQuery)
	{
		_closeQuery = closeQuery;
	}

	public abstract void ExecuteAffirmativeAction();

	public abstract void ExecuteNegativeAction();

	public virtual void OnTick(float dt)
	{
	}

	public virtual void OnClearData()
	{
		TitleText = null;
		PopUpLabel = null;
		ButtonOkLabel = null;
		ButtonCancelLabel = null;
		IsButtonOkShown = false;
		IsButtonCancelShown = false;
		IsButtonOkEnabled = false;
	}

	public void ForceRefreshKeyVisuals()
	{
		CancelInputKey?.RefreshValues();
		DoneInputKey?.RefreshValues();
	}

	public void CloseQuery()
	{
		_closeQuery?.Invoke();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
