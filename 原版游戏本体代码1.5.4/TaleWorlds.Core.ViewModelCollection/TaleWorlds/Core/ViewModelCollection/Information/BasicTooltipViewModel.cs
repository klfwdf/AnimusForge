using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class BasicTooltipViewModel : ViewModel
{
	private Func<string> _hintProperty;

	private Func<List<TooltipProperty>> _tooltipProperties;

	private Action _preBuiltTooltipCallback;

	public BasicTooltipViewModel(Func<string> hintTextDelegate)
	{
		_hintProperty = hintTextDelegate;
	}

	public BasicTooltipViewModel(Func<List<TooltipProperty>> tooltipPropertiesDelegate)
	{
		_tooltipProperties = tooltipPropertiesDelegate;
	}

	public BasicTooltipViewModel(Action preBuiltTooltipCallback)
	{
		_preBuiltTooltipCallback = preBuiltTooltipCallback;
	}

	public BasicTooltipViewModel()
	{
		_hintProperty = null;
		_tooltipProperties = null;
		_preBuiltTooltipCallback = null;
	}

	public void SetToolipCallback(Func<List<TooltipProperty>> tooltipPropertiesDelegate)
	{
		_tooltipProperties = tooltipPropertiesDelegate;
	}

	public void SetGenericTooltipCallback(Action preBuiltTooltipCallback)
	{
		_preBuiltTooltipCallback = preBuiltTooltipCallback;
	}

	public void SetHintCallback(Func<string> hintProperty)
	{
		_hintProperty = hintProperty;
	}

	public void ExecuteBeginHint()
	{
		if (_hintProperty == null && _tooltipProperties == null && _preBuiltTooltipCallback == null)
		{
			return;
		}
		if (_hintProperty != null)
		{
			_ = _tooltipProperties;
		}
		if (_tooltipProperties != null)
		{
			InformationManager.ShowTooltip(typeof(List<TooltipProperty>), _tooltipProperties());
		}
		else if (_hintProperty != null)
		{
			string text = _hintProperty();
			if (!string.IsNullOrEmpty(text))
			{
				MBInformationManager.ShowHint(text);
			}
		}
		else if (_preBuiltTooltipCallback != null)
		{
			_preBuiltTooltipCallback();
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
