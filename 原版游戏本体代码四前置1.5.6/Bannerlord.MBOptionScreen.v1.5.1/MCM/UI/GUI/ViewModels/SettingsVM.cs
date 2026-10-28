using System;
using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.UI.Actions;
using MCM.UI.Dropdown;
using MCM.UI.Extensions;
using MCM.UI.Utils;
using TaleWorlds.Library;

namespace MCM.UI.GUI.ViewModels;

internal sealed class SettingsVM : ViewModel
{
	private string _displayName = string.Empty;

	private bool _isSelected;

	private readonly Dictionary<PresetKey, BaseSettings> _cachedPresets = new Dictionary<PresetKey, BaseSettings>();

	public ModOptionsVM MainView { get; }

	public UndoRedoStack URS { get; } = new UndoRedoStack();

	public SettingsDefinition SettingsDefinition { get; }

	public BaseSettings? SettingsInstance => BaseSettingsProvider.Instance?.GetSettings(SettingsDefinition.SettingsId);

	public MCMSelectorVM<MCMSelectorItemVM<PresetKey>> PresetsSelector { get; } = new MCMSelectorVM<MCMSelectorItemVM<PresetKey>>(Array.Empty<object>(), -1);

	[DataSourceProperty]
	public string DisplayName
	{
		get
		{
			return _displayName;
		}
		private set
		{
			SetField(ref _displayName, value, "DisplayName");
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			SetField(ref _isSelected, value, "IsSelected");
		}
	}

	[DataSourceProperty]
	public MBBindingList<SettingsPropertyGroupVM> SettingPropertyGroups { get; } = new MBBindingList<SettingsPropertyGroupVM>();

	public SettingsVM(SettingsDefinition definition, ModOptionsVM mainView)
	{
		SettingsDefinition = definition;
		MainView = mainView;
		ReloadPresetList();
		if (SettingsInstance != null)
		{
			SettingPropertyGroups.AddRange(from x in SettingPropertyDefinitionCache.GetSettingPropertyGroups(SettingsInstance)
				select new SettingsPropertyGroupVM(x, this));
		}
		RefreshValues();
	}

	public void ReloadPresetList()
	{
		_cachedPresets.Clear();
		foreach (ISettingsPreset item in SettingsInstance?.GetBuiltInPresets().Concat(SettingsInstance.GetExternalPresets()) ?? Array.Empty<ISettingsPreset>())
		{
			_cachedPresets.Add(new PresetKey(item), item.LoadPreset());
		}
		IEnumerable<PresetKey> list = new List<PresetKey>
		{
			new PresetKey("custom", "{=SettingsVM_Custom}Custom")
		}.Concat(_cachedPresets.Keys);
		PresetsSelector.Refresh(list, -1);
		PresetsSelector.ItemList[0].CanBeSelected = false;
		RecalculatePresetIndex();
	}

	public void RecalculatePresetIndex()
	{
		if (SettingsInstance == null)
		{
			return;
		}
		int num = 1;
		foreach (BaseSettings value in _cachedPresets.Values)
		{
			if (SettingPropertyDefinitionCache.Equals(SettingsInstance, value))
			{
				PresetsSelector.SelectedIndex = num;
				return;
			}
			num++;
		}
		PresetsSelector.SelectedIndex = 0;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DisplayName = SettingsInstance?.DisplayName ?? "ERROR";
		SettingPropertyGroups.Sort(UISettingsUtils.SettingsPropertyGroupVMComparer);
		foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
		{
			settingPropertyGroup.RefreshValues();
		}
	}

	public bool RestartRequired()
	{
		return SettingPropertyGroups.SelectMany((SettingsPropertyGroupVM x) => SettingsUtils.GetAllSettingPropertyDefinitions(x.SettingPropertyGroupDefinition)).Any((ISettingsPropertyDefinition p) => p.RequireRestart && URS.RefChanged(p.PropertyReference));
	}

	public void ChangePreset(string presetId)
	{
		if (SettingsInstance != null && _cachedPresets.TryGetValue(new PresetKey(presetId, string.Empty), out BaseSettings value))
		{
			UISettingsUtils.OverrideValues(URS, SettingsInstance, value);
		}
	}

	public void ChangePresetValue(string presetId, string valueId)
	{
		if (SettingsInstance != null && _cachedPresets.TryGetValue(new PresetKey(presetId, string.Empty), out BaseSettings value))
		{
			ISettingsPropertyDefinition settingsPropertyDefinition = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(SettingsInstance).FirstOrDefault((ISettingsPropertyDefinition spd) => spd.Id == valueId);
			ISettingsPropertyDefinition settingsPropertyDefinition2 = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(value).FirstOrDefault((ISettingsPropertyDefinition spd) => spd.Id == valueId);
			if (settingsPropertyDefinition != null && settingsPropertyDefinition2 != null)
			{
				UISettingsUtils.OverrideValues(URS, settingsPropertyDefinition, settingsPropertyDefinition2);
			}
		}
		RecalculatePresetIndex();
	}

	public void ResetSettings()
	{
		ChangePreset("default");
	}

	public void SaveSettings()
	{
		if (SettingsInstance != null)
		{
			BaseSettingsProvider.Instance?.SaveSettings(SettingsInstance);
		}
	}

	public void ResetSettingsValue(string valueId)
	{
		ChangePresetValue("default", valueId);
	}

	public override void OnFinalize()
	{
		foreach (SettingsPropertyGroupVM settingPropertyGroup in SettingPropertyGroups)
		{
			settingPropertyGroup.OnFinalize();
		}
		base.OnFinalize();
	}
}
