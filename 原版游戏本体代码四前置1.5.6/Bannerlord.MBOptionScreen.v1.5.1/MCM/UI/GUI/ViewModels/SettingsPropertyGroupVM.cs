using System;
using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions;
using MCM.UI.Extensions;
using MCM.UI.Utils;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.GUI.ViewModels;

internal sealed class SettingsPropertyGroupVM : ViewModel
{
	private bool _isExpanded = true;

	private string _groupName = string.Empty;

	private string _hintText = string.Empty;

	private string _groupNameDisplay = string.Empty;

	private ModOptionsVM MainView => SettingsVM.MainView;

	private SettingsVM SettingsVM { get; }

	public SettingsPropertyGroupDefinition SettingPropertyGroupDefinition { get; }

	public string GroupName
	{
		get
		{
			return _groupName;
		}
		private set
		{
			SetField(ref _groupName, value, "GroupName");
		}
	}

	public SettingsPropertyGroupVM? ParentGroup { get; }

	public SettingsPropertyVM? GroupToggleSettingProperty { get; private set; }

	public string HintText
	{
		get
		{
			return _hintText;
		}
		private set
		{
			SetField(ref _hintText, value, "HintText");
		}
	}

	public bool SatisfiesSearch
	{
		get
		{
			if (string.IsNullOrEmpty(MainView.SearchText))
			{
				return true;
			}
			if (GroupName.IndexOf(MainView.SearchText, StringComparison.InvariantCultureIgnoreCase) < 0)
			{
				return AnyChildSettingSatisfiesSearch;
			}
			return true;
		}
	}

	public bool AnyChildSettingSatisfiesSearch
	{
		get
		{
			if (!SettingProperties.Any((SettingsPropertyVM x) => x.SatisfiesSearch))
			{
				return SettingPropertyGroups.Any((SettingsPropertyGroupVM x) => x.SatisfiesSearch);
			}
			return true;
		}
	}

	[DataSourceProperty]
	public string GroupNameDisplay
	{
		get
		{
			return _groupNameDisplay;
		}
		set
		{
			SetField(ref _groupNameDisplay, value, "GroupNameDisplay");
		}
	}

	[DataSourceProperty]
	public MBBindingList<SettingsPropertyVM> SettingProperties { get; } = new MBBindingList<SettingsPropertyVM>();

	[DataSourceProperty]
	public MBBindingList<SettingsPropertyGroupVM> SettingPropertyGroups { get; } = new MBBindingList<SettingsPropertyGroupVM>();

	[DataSourceProperty]
	public bool GroupToggle
	{
		get
		{
			return GroupToggleSettingProperty?.BoolValue ?? true;
		}
		set
		{
			if (GroupToggleSettingProperty == null || GroupToggleSettingProperty.BoolValue == value)
			{
				return;
			}
			GroupToggleSettingProperty.BoolValue = value;
			OnPropertyChanged("GroupToggle");
			OnPropertyChanged("IsExpanded");
			OnGroupClick();
			OnGroupClick();
			OnPropertyChanged("GroupNameDisplay");
			foreach (SettingsPropertyVM settingProperty in SettingProperties)
			{
				settingProperty.OnPropertyChanged("IsEnabled");
				settingProperty.OnPropertyChanged("IsSettingVisible");
			}
			foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
			{
				settingPropertyGroup.OnPropertyChanged("IsGroupVisible");
				settingPropertyGroup.OnPropertyChanged("IsExpanded");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGroupVisible
	{
		get
		{
			if (!SatisfiesSearch && !AnyChildSettingSatisfiesSearch)
			{
				return false;
			}
			if (ParentGroup != null)
			{
				if (ParentGroup.IsExpanded)
				{
					return ParentGroup.GroupToggle;
				}
				return false;
			}
			return true;
		}
	}

	[DataSourceProperty]
	public bool IsExpanded
	{
		get
		{
			return _isExpanded;
		}
		set
		{
			if (!SetField(ref _isExpanded, value, "IsExpanded"))
			{
				return;
			}
			OnPropertyChanged("IsGroupVisible");
			foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
			{
				settingPropertyGroup.OnPropertyChanged("IsGroupVisible");
				settingPropertyGroup.OnPropertyChanged("IsExpanded");
			}
			foreach (SettingsPropertyVM settingProperty in SettingProperties)
			{
				settingProperty.OnPropertyChanged("IsSettingVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool HasGroupToggle => GroupToggleSettingProperty != null;

	public SettingsPropertyGroupVM(SettingsPropertyGroupDefinition definition, SettingsVM settingsVM, SettingsPropertyGroupVM? parentGroup = null)
	{
		SettingsVM = settingsVM;
		SettingPropertyGroupDefinition = definition;
		ParentGroup = parentGroup;
		AddRange(SettingPropertyGroupDefinition.SettingProperties);
		SettingPropertyGroups.AddRange(SettingPropertyGroupDefinition.SubGroups.Select((SettingsPropertyGroupDefinition x) => new SettingsPropertyGroupVM(x, SettingsVM, this)));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GroupName = SettingPropertyGroupDefinition.GroupName;
		HintText = ((GroupToggleSettingProperty != null && !string.IsNullOrWhiteSpace(GroupToggleSettingProperty.HintText)) ? GroupToggleSettingProperty.HintText : string.Empty);
		GroupNameDisplay = (GroupToggle ? new TextObject(GroupName).ToString() : new TextObject("{=SettingsPropertyGroupVM_Disabled}{GROUPNAME} (Disabled)", new Dictionary<string, object> { 
		{
			"GROUPNAME",
			new TextObject(GroupName).ToString()
		} }).ToString());
		SettingProperties.Sort(UISettingsUtils.SettingsPropertyVMComparer);
		SettingPropertyGroups.Sort(UISettingsUtils.SettingsPropertyGroupVMComparer);
		foreach (SettingsPropertyVM settingProperty in SettingProperties)
		{
			settingProperty.RefreshValues();
		}
		foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
		{
			settingPropertyGroup.RefreshValues();
		}
	}

	private void AddRange(IEnumerable<ISettingsPropertyDefinition> definitions)
	{
		foreach (ISettingsPropertyDefinition definition in definitions)
		{
			SettingsPropertyVM settingsPropertyVM = new SettingsPropertyVM(definition, SettingsVM);
			SettingProperties.Add(settingsPropertyVM);
			settingsPropertyVM.Group = this;
			if (settingsPropertyVM.SettingPropertyDefinition.IsToggle)
			{
				_ = HasGroupToggle;
				GroupToggleSettingProperty = settingsPropertyVM;
			}
		}
	}

	public void NotifySearchChanged()
	{
		foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
		{
			settingPropertyGroup.NotifySearchChanged();
		}
		foreach (SettingsPropertyVM settingProperty in SettingProperties)
		{
			settingProperty.OnPropertyChanged("IsSettingVisible");
		}
		OnPropertyChanged("IsGroupVisible");
	}

	public void OnHover()
	{
		MainView.HintText = HintText;
	}

	public void OnHoverEnd()
	{
		MainView.HintText = string.Empty;
	}

	public void OnGroupClick()
	{
		IsExpanded = !IsExpanded;
	}

	public override string ToString()
	{
		return GroupName;
	}

	public override int GetHashCode()
	{
		return GroupName.GetHashCode();
	}

	public override void OnFinalize()
	{
		foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
		{
			settingPropertyGroup.OnFinalize();
		}
		foreach (SettingsPropertyVM settingProperty in SettingProperties)
		{
			settingProperty.OnFinalize();
		}
		base.OnFinalize();
	}
}
