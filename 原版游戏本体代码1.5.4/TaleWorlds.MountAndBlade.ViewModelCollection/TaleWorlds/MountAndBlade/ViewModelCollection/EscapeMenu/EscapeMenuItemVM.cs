using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

public class EscapeMenuItemVM : ViewModel
{
	private readonly object _identifier;

	private readonly Action<object> _onExecute;

	private readonly TextObject _itemObj;

	private readonly Func<Tuple<bool, TextObject>> _getIsDisabledAndReason;

	private HintViewModel _disabledHint;

	private string _actionText;

	private bool _isDisabled;

	private bool _isPositiveBehaviored;

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (value != _actionText)
			{
				_actionText = value;
				OnPropertyChangedWithValue(value, "ActionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPositiveBehaviored
	{
		get
		{
			return _isPositiveBehaviored;
		}
		set
		{
			if (value != _isPositiveBehaviored)
			{
				_isPositiveBehaviored = value;
				OnPropertyChangedWithValue(value, "IsPositiveBehaviored");
			}
		}
	}

	public EscapeMenuItemVM(TextObject item, Action<object> onExecute, object identifier, Func<Tuple<bool, TextObject>> getIsDisabledAndReason, bool isPositiveBehaviored = false)
	{
		_onExecute = onExecute;
		_identifier = identifier;
		_itemObj = item;
		ActionText = _itemObj.ToString();
		IsPositiveBehaviored = isPositiveBehaviored;
		_getIsDisabledAndReason = getIsDisabledAndReason;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Tuple<bool, TextObject> tuple = _getIsDisabledAndReason?.Invoke();
		IsDisabled = tuple.Item1;
		DisabledHint = new HintViewModel(tuple.Item2);
		ActionText = _itemObj.ToString();
	}

	public void ExecuteAction()
	{
		_onExecute(_identifier);
	}
}
