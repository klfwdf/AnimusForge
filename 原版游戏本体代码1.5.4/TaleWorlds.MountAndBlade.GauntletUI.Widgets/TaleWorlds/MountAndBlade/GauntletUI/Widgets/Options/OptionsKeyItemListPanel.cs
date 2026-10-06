using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options;

public class OptionsKeyItemListPanel : ListPanel
{
	private OptionsScreenWidget _screenWidget;

	private bool _eventsRegistered;

	private bool _initialized;

	private string _optionDescription;

	private string _optionTitle;

	private string _optionExtraInformation;

	public string OptionTitle
	{
		get
		{
			return _optionTitle;
		}
		set
		{
			if (_optionTitle != value)
			{
				_optionTitle = value;
			}
		}
	}

	public string OptionDescription
	{
		get
		{
			return _optionDescription;
		}
		set
		{
			if (_optionDescription != value)
			{
				_optionDescription = value;
			}
		}
	}

	public string OptionExtraInformation
	{
		get
		{
			return _optionExtraInformation;
		}
		set
		{
			if (_optionExtraInformation != value)
			{
				_optionExtraInformation = value;
			}
		}
	}

	public OptionsKeyItemListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			_screenWidget = FindScreenWidget(base.ParentWidget);
			_initialized = true;
		}
		if (!_eventsRegistered)
		{
			RegisterHoverEvents();
			_eventsRegistered = true;
		}
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		SetCurrentOption(fromHoverOverDropdown: false, fromBooleanSelection: false);
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		ResetCurrentOption();
	}

	private OptionsScreenWidget FindScreenWidget(Widget parent)
	{
		if (parent is OptionsScreenWidget result)
		{
			return result;
		}
		if (parent == null)
		{
			return null;
		}
		return FindScreenWidget(parent.ParentWidget);
	}

	private void SetCurrentOption(bool fromHoverOverDropdown, bool fromBooleanSelection, int hoverDropdownItemIndex = -1)
	{
		_screenWidget?.SetCurrentOption(this, null);
	}

	private void ResetCurrentOption()
	{
		_screenWidget?.SetCurrentOption(null, null);
	}

	private void RegisterHoverEvents()
	{
		List<Widget> allChildrenRecursive = GetAllChildrenRecursive();
		for (int i = 0; i < allChildrenRecursive.Count; i++)
		{
			allChildrenRecursive[i].boolPropertyChanged += Child_PropertyChanged;
		}
	}

	private void Child_PropertyChanged(PropertyOwnerObject childWidget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			if (propertyValue)
			{
				SetCurrentOption(fromHoverOverDropdown: false, fromBooleanSelection: false);
			}
			else
			{
				ResetCurrentOption();
			}
		}
	}
}
