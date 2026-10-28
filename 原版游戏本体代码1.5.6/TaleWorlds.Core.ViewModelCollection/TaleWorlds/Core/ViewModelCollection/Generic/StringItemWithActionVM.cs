using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class StringItemWithActionVM : ViewModel
{
	public object Identifier;

	protected Action<object> _onExecute;

	private string _actionText;

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

	public StringItemWithActionVM(Action<object> onExecute, string item, object identifier)
	{
		_onExecute = onExecute;
		Identifier = identifier;
		ActionText = item;
	}

	public void ExecuteAction()
	{
		_onExecute(Identifier);
	}
}
