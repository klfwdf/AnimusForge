using System;

namespace TaleWorlds.Core.ViewModelCollection;

public class CharacterWithActionViewModel : CharacterViewModel
{
	private Action _onAction;

	public CharacterWithActionViewModel(Action onAction)
	{
		_onAction = onAction;
	}

	private void ExecuteAction()
	{
		_onAction?.Invoke();
	}
}
