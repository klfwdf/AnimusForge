using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

public class MapNavigationItemVM : ViewModel
{
	public readonly INavigationElement NavigationElement;

	private bool _isEnabled;

	private bool _isActive;

	private bool _hasAlert;

	private string _itemId;

	private string _alertText;

	private BasicTooltipViewModel _tooltip;

	private BasicTooltipViewModel _alertTooltip;

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

	[DataSourceProperty]
	public bool HasAlert
	{
		get
		{
			return _hasAlert;
		}
		set
		{
			if (value != _hasAlert)
			{
				_hasAlert = value;
				OnPropertyChangedWithValue(value, "HasAlert");
			}
		}
	}

	[DataSourceProperty]
	public string ItemId
	{
		get
		{
			return _itemId;
		}
		set
		{
			if (value != _itemId)
			{
				_itemId = value;
				OnPropertyChangedWithValue(value, "ItemId");
			}
		}
	}

	[DataSourceProperty]
	public string AlertText
	{
		get
		{
			return _alertText;
		}
		set
		{
			if (value != _alertText)
			{
				_alertText = value;
				OnPropertyChangedWithValue(value, "AlertText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AlertTooltip
	{
		get
		{
			return _alertTooltip;
		}
		set
		{
			if (value != _alertTooltip)
			{
				_alertTooltip = value;
				OnPropertyChangedWithValue(value, "AlertTooltip");
			}
		}
	}

	public MapNavigationItemVM(INavigationElement navigationElement)
	{
		NavigationElement = navigationElement;
		Tooltip = new BasicTooltipViewModel(() => GetTooltip());
		AlertTooltip = new BasicTooltipViewModel(() => GetAlertTooltip());
		ItemId = NavigationElement.StringId;
		RefreshStates(forceRefresh: true);
		RefreshValues();
	}

	private string GetTooltip()
	{
		NavigationPermissionItem permission = NavigationElement.Permission;
		if (permission.IsAuthorized || NavigationElement.IsActive)
		{
			return NavigationElement.Tooltip?.ToString();
		}
		return permission.ReasonString?.ToString();
	}

	private string GetAlertTooltip()
	{
		return NavigationElement.AlertTooltip?.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AlertText = GameTexts.FindText("str_map_bar_alert").ToString();
	}

	public void RefreshStates(bool forceRefresh = false)
	{
		IsActive = NavigationElement.IsActive;
		HasAlert = NavigationElement.HasAlert;
		IsEnabled = NavigationElement.Permission.IsAuthorized;
	}

	public void ExecuteOpen()
	{
		NavigationElement.OpenView();
	}

	public void ExecuteGoToLink()
	{
		NavigationElement.GoToLink();
	}
}
