using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.UI.Actions;
using MCM.UI.Dropdown;
using MCM.UI.Extensions;
using MCM.UI.Utils;
using TaleWorlds.Library;

namespace MCM.UI.GUI.ViewModels
{
	// Token: 0x02000022 RID: 34
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SettingsVM : ViewModel
	{
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000159 RID: 345 RVA: 0x000064DC File Offset: 0x000046DC
		public ModOptionsVM MainView { get; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x0600015A RID: 346 RVA: 0x000064E4 File Offset: 0x000046E4
		public UndoRedoStack URS { get; } = new UndoRedoStack();

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600015B RID: 347 RVA: 0x000064EC File Offset: 0x000046EC
		public SettingsDefinition SettingsDefinition { get; }

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x0600015C RID: 348 RVA: 0x000064F4 File Offset: 0x000046F4
		[Nullable(2)]
		public BaseSettings SettingsInstance
		{
			[NullableContext(2)]
			get
			{
				BaseSettingsProvider instance = BaseSettingsProvider.Instance;
				if (instance == null)
				{
					return null;
				}
				return instance.GetSettings(this.SettingsDefinition.SettingsId);
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600015D RID: 349 RVA: 0x00006511 File Offset: 0x00004711
		public MCMSelectorVM<MCMSelectorItemVM<PresetKey>> PresetsSelector { get; } = new MCMSelectorVM<MCMSelectorItemVM<PresetKey>>(Array.Empty<object>(), -1);

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600015E RID: 350 RVA: 0x00006519 File Offset: 0x00004719
		// (set) Token: 0x0600015F RID: 351 RVA: 0x00006521 File Offset: 0x00004721
		[DataSourceProperty]
		public string DisplayName
		{
			get
			{
				return this._displayName;
			}
			private set
			{
				base.SetField<string>(ref this._displayName, value, "DisplayName");
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000160 RID: 352 RVA: 0x00006536 File Offset: 0x00004736
		// (set) Token: 0x06000161 RID: 353 RVA: 0x0000653E File Offset: 0x0000473E
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				base.SetField<bool>(ref this._isSelected, value, "IsSelected");
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00006553 File Offset: 0x00004753
		[DataSourceProperty]
		public MBBindingList<SettingsPropertyGroupVM> SettingPropertyGroups { get; } = new MBBindingList<SettingsPropertyGroupVM>();

		// Token: 0x06000163 RID: 355 RVA: 0x0000655C File Offset: 0x0000475C
		public SettingsVM(SettingsDefinition definition, ModOptionsVM mainView)
		{
			this.SettingsDefinition = definition;
			this.MainView = mainView;
			this.ReloadPresetList();
			if (this.SettingsInstance != null)
			{
				this.SettingPropertyGroups.AddRange(from x in SettingPropertyDefinitionCache.GetSettingPropertyGroups(this.SettingsInstance)
					select new SettingsPropertyGroupVM(x, this, null));
			}
			this.RefreshValues();
		}

		// Token: 0x06000164 RID: 356 RVA: 0x000065F8 File Offset: 0x000047F8
		public void ReloadPresetList()
		{
			this._cachedPresets.Clear();
			BaseSettings settingsInstance = this.SettingsInstance;
			foreach (ISettingsPreset preset in (((settingsInstance != null) ? settingsInstance.GetBuiltInPresets().Concat(BaseSettingsExtensions.GetExternalPresets(this.SettingsInstance)) : null) ?? Array.Empty<ISettingsPreset>()))
			{
				this._cachedPresets.Add(new PresetKey(preset), preset.LoadPreset());
			}
			IEnumerable<PresetKey> presets = new List<PresetKey>
			{
				new PresetKey("custom", "{=SettingsVM_Custom}Custom")
			}.Concat(this._cachedPresets.Keys);
			this.PresetsSelector.Refresh(presets, -1);
			this.PresetsSelector.ItemList[0].CanBeSelected = false;
			this.RecalculatePresetIndex();
		}

		// Token: 0x06000165 RID: 357 RVA: 0x000066DC File Offset: 0x000048DC
		public void RecalculatePresetIndex()
		{
			if (this.SettingsInstance == null)
			{
				return;
			}
			int index = 1;
			foreach (BaseSettings preset in this._cachedPresets.Values)
			{
				if (SettingPropertyDefinitionCache.Equals(this.SettingsInstance, preset))
				{
					this.PresetsSelector.SelectedIndex = index;
					return;
				}
				index++;
			}
			this.PresetsSelector.SelectedIndex = 0;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00006764 File Offset: 0x00004964
		public override void RefreshValues()
		{
			base.RefreshValues();
			BaseSettings settingsInstance = this.SettingsInstance;
			this.DisplayName = ((settingsInstance != null) ? settingsInstance.DisplayName : null) ?? "ERROR";
			this.SettingPropertyGroups.Sort(UISettingsUtils.SettingsPropertyGroupVMComparer);
			foreach (SettingsPropertyGroupVM group in this.SettingPropertyGroups)
			{
				group.RefreshValues();
			}
		}

		// Token: 0x06000167 RID: 359 RVA: 0x000067E8 File Offset: 0x000049E8
		public bool RestartRequired()
		{
			return this.SettingPropertyGroups.SelectMany((SettingsPropertyGroupVM x) => SettingsUtils.GetAllSettingPropertyDefinitions(x.SettingPropertyGroupDefinition)).Any((ISettingsPropertyDefinition p) => p.RequireRestart && this.URS.RefChanged(p.PropertyReference));
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00006828 File Offset: 0x00004A28
		public void ChangePreset(string presetId)
		{
			BaseSettings preset;
			if (this.SettingsInstance != null && this._cachedPresets.TryGetValue(new PresetKey(presetId, string.Empty), out preset))
			{
				UISettingsUtils.OverrideValues(this.URS, this.SettingsInstance, preset);
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000686C File Offset: 0x00004A6C
		public void ChangePresetValue(string presetId, string valueId)
		{
			BaseSettings preset;
			if (this.SettingsInstance != null && this._cachedPresets.TryGetValue(new PresetKey(presetId, string.Empty), out preset))
			{
				ISettingsPropertyDefinition current = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(this.SettingsInstance).FirstOrDefault((ISettingsPropertyDefinition spd) => spd.Id == valueId);
				ISettingsPropertyDefinition @new = SettingPropertyDefinitionCache.GetAllSettingPropertyDefinitions(preset).FirstOrDefault((ISettingsPropertyDefinition spd) => spd.Id == valueId);
				if (current != null && @new != null)
				{
					UISettingsUtils.OverrideValues(this.URS, current, @new);
				}
			}
			this.RecalculatePresetIndex();
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000068F6 File Offset: 0x00004AF6
		public void ResetSettings()
		{
			this.ChangePreset("default");
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00006903 File Offset: 0x00004B03
		public void SaveSettings()
		{
			if (this.SettingsInstance != null)
			{
				BaseSettingsProvider instance = BaseSettingsProvider.Instance;
				if (instance == null)
				{
					return;
				}
				instance.SaveSettings(this.SettingsInstance);
			}
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00006922 File Offset: 0x00004B22
		public void ResetSettingsValue(string valueId)
		{
			this.ChangePresetValue("default", valueId);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00006930 File Offset: 0x00004B30
		public override void OnFinalize()
		{
			foreach (SettingsPropertyGroupVM settingPropertyGroup in this.SettingPropertyGroups)
			{
				settingPropertyGroup.OnFinalize();
			}
			base.OnFinalize();
		}

		// Token: 0x04000053 RID: 83
		private string _displayName = string.Empty;

		// Token: 0x04000054 RID: 84
		private bool _isSelected;

		// Token: 0x04000055 RID: 85
		private readonly Dictionary<PresetKey, BaseSettings> _cachedPresets = new Dictionary<PresetKey, BaseSettings>();
	}
}
