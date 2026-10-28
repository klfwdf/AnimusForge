using System;

namespace TaleWorlds.Library.Information;

public class TooltipTriggerVM : ViewModel
{
	private Type _linkedTooltipType;

	private object[] _args;

	public TooltipTriggerVM(Type linkedTooltipType, params object[] args)
	{
		_linkedTooltipType = linkedTooltipType;
		_args = args;
	}

	public void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(_linkedTooltipType, _args);
	}

	public void ExecuteEndHint()
	{
		InformationManager.HideTooltip();
	}
}
