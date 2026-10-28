using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class BoolItemWithActionVM : ViewModel
{
	public object Identifier;

	protected Action<object> _onExecute;

	private bool _isActive;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	public void ExecuteAction()
	{
		_onExecute(Identifier);
	}

	public BoolItemWithActionVM(Action<object> onExecute, bool isActive, object identifier)
	{
		_onExecute = onExecute;
		Identifier = identifier;
		IsActive = isActive;
	}
}
