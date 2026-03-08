using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bannerlord.ModuleManager;
using BUTR.DependencyInjection;
using ComparerExtensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using MCM.UI.Dropdown;
using MCM.UI.Extensions;
using MCM.UI.Patches;
using MCM.UI.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace MCM.UI.GUI.ViewModels
{
	// Token: 0x0200001E RID: 30
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModOptionsVM : ViewModel
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000B4 RID: 180 RVA: 0x00003EA7 File Offset: 0x000020A7
		// (set) Token: 0x060000B5 RID: 181 RVA: 0x00003EB0 File Offset: 0x000020B0
		[Nullable(2)]
		private SettingsEntryVM SelectedEntry
		{
			[NullableContext(2)]
			get
			{
				return this._selectedEntry;
			}
			[NullableContext(2)]
			set
			{
				SettingsEntryVM selectedEntry = this._selectedEntry;
				MCMSelectorVM<MCMSelectorItemVM<PresetKey>> mcmselectorVM;
				if (selectedEntry == null)
				{
					mcmselectorVM = null;
				}
				else
				{
					SettingsVM settingsVM = selectedEntry.SettingsVM;
					mcmselectorVM = ((settingsVM != null) ? settingsVM.PresetsSelector : null);
				}
				MCMSelectorVM<MCMSelectorItemVM<PresetKey>> oldModPresetsSelector = mcmselectorVM;
				if (oldModPresetsSelector != null)
				{
					oldModPresetsSelector.PropertyChanged -= this.OnModPresetsSelectorChange;
				}
				if (base.SetField<SettingsEntryVM>(ref this._selectedEntry, value, "SelectedMod"))
				{
					base.OnPropertyChanged("IsSettingVisible");
					base.OnPropertyChanged("IsSettingUnavailableVisible");
					base.OnPropertyChanged("SelectedDisplayName");
					base.OnPropertyChanged("SomethingSelected");
					this.DoPresetsSelectorCopyWithoutEvents(delegate
					{
						SettingsEntryVM selectedEntry2 = this.SelectedEntry;
						MCMSelectorVM<MCMSelectorItemVM<PresetKey>> mcmselectorVM2;
						if (selectedEntry2 == null)
						{
							mcmselectorVM2 = null;
						}
						else
						{
							SettingsVM settingsVM2 = selectedEntry2.SettingsVM;
							mcmselectorVM2 = ((settingsVM2 != null) ? settingsVM2.PresetsSelector : null);
						}
						MCMSelectorVM<MCMSelectorItemVM<PresetKey>> modPresetsSelector = mcmselectorVM2;
						if (modPresetsSelector != null)
						{
							modPresetsSelector.PropertyChanged += this.OnModPresetsSelectorChange;
							this.PresetsSelectorCopy.Refresh(from x in this.SelectedEntry.SettingsVM.PresetsSelector.ItemList
								select x.OriginalItem, this.SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex);
							return;
						}
						this.PresetsSelectorCopy.Refresh(Array.Empty<object>(), -1);
					});
					base.OnPropertyChanged("IsPresetsSelectorVisible");
				}
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x00003F4E File Offset: 0x0000214E
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00003F56 File Offset: 0x00002156
		public bool IsDisabled { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00003F5F File Offset: 0x0000215F
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x00003F67 File Offset: 0x00002167
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._titleLabel;
			}
			set
			{
				base.SetField<string>(ref this._titleLabel, value, "Name");
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00003F7C File Offset: 0x0000217C
		// (set) Token: 0x060000BB RID: 187 RVA: 0x00003F84 File Offset: 0x00002184
		[DataSourceProperty]
		public string DoneButtonText
		{
			get
			{
				return this._doneButtonText;
			}
			set
			{
				base.SetField<string>(ref this._doneButtonText, value, "DoneButtonText");
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00003F99 File Offset: 0x00002199
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00003FA1 File Offset: 0x000021A1
		[DataSourceProperty]
		public string CancelButtonText
		{
			get
			{
				return this._cancelButtonText;
			}
			set
			{
				base.SetField<string>(ref this._cancelButtonText, value, "CancelButtonText");
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00003FB6 File Offset: 0x000021B6
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00003FBE File Offset: 0x000021BE
		[DataSourceProperty]
		public string ModsText
		{
			get
			{
				return this._modsText;
			}
			set
			{
				base.SetField<string>(ref this._modsText, value, "ModsText");
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00003FD3 File Offset: 0x000021D3
		[DataSourceProperty]
		public MBBindingList<SettingsEntryVM> ModSettingsList { get; } = new MBBindingList<SettingsEntryVM>();

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000C1 RID: 193 RVA: 0x00003FDB File Offset: 0x000021DB
		[Nullable(2)]
		[DataSourceProperty]
		public SettingsVM SelectedMod
		{
			[NullableContext(2)]
			get
			{
				SettingsEntryVM selectedEntry = this.SelectedEntry;
				if (selectedEntry == null)
				{
					return null;
				}
				return selectedEntry.SettingsVM;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00003FEE File Offset: 0x000021EE
		[DataSourceProperty]
		public string SelectedDisplayName
		{
			get
			{
				if (this.SelectedEntry != null)
				{
					return this.SelectedEntry.DisplayName;
				}
				return this.ModNameNotSpecifiedText;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000400A File Offset: 0x0000220A
		[DataSourceProperty]
		public bool SomethingSelected
		{
			get
			{
				return this.SelectedEntry != null;
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00004018 File Offset: 0x00002218
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00004020 File Offset: 0x00002220
		[DataSourceProperty]
		public string HintText
		{
			get
			{
				return this._hintText;
			}
			set
			{
				if (base.SetField<string>(ref this._hintText, value, "HintText"))
				{
					base.OnPropertyChanged("IsHintVisible");
				}
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x00004041 File Offset: 0x00002241
		[DataSourceProperty]
		public bool IsHintVisible
		{
			get
			{
				return !string.IsNullOrWhiteSpace(this.HintText);
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00004051 File Offset: 0x00002251
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x0000405C File Offset: 0x0000225C
		[DataSourceProperty]
		public string SearchText
		{
			get
			{
				return this._searchText;
			}
			set
			{
				if (base.SetField<string>(ref this._searchText, value, "SearchText"))
				{
					SettingsEntryVM selectedEntry = this.SelectedEntry;
					bool flag;
					if (selectedEntry == null)
					{
						flag = false;
					}
					else
					{
						SettingsVM settingsVM = selectedEntry.SettingsVM;
						int? num = ((settingsVM != null) ? new int?(settingsVM.SettingPropertyGroups.Count) : null);
						int num2 = 0;
						flag = (num.GetValueOrDefault() > num2) & (num != null);
					}
					if (flag)
					{
						foreach (SettingsPropertyGroupVM group in this.SelectedEntry.SettingsVM.SettingPropertyGroups)
						{
							group.NotifySearchChanged();
						}
					}
				}
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000C9 RID: 201 RVA: 0x00004114 File Offset: 0x00002314
		[DataSourceProperty]
		public MCMSelectorVM<MCMSelectorItemVM<PresetKey>> PresetsSelectorCopy { get; } = new MCMSelectorVM<MCMSelectorItemVM<PresetKey>>(Enumerable.Empty<PresetKey>(), -1);

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000CA RID: 202 RVA: 0x0000411C File Offset: 0x0000231C
		[DataSourceProperty]
		public bool IsPresetsSelectorVisible
		{
			get
			{
				return this.SelectedEntry != null;
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000CB RID: 203 RVA: 0x0000412A File Offset: 0x0000232A
		[DataSourceProperty]
		public bool IsSettingVisible
		{
			get
			{
				SettingsEntryVM selectedEntry = this.SelectedEntry;
				return ((selectedEntry != null) ? selectedEntry.SettingsVM : null) != null;
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000CC RID: 204 RVA: 0x00004144 File Offset: 0x00002344
		[DataSourceProperty]
		public bool IsSettingUnavailableVisible
		{
			get
			{
				SettingsEntryVM selectedEntry = this.SelectedEntry;
				return ((selectedEntry != null) ? selectedEntry.SettingsVM : null) == null;
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000CD RID: 205 RVA: 0x0000415B File Offset: 0x0000235B
		// (set) Token: 0x060000CE RID: 206 RVA: 0x00004163 File Offset: 0x00002363
		[DataSourceProperty]
		public string ModNameNotSpecifiedText
		{
			get
			{
				return this._modNameNotSpecifiedText;
			}
			set
			{
				base.SetField<string>(ref this._modNameNotSpecifiedText, value, "ModNameNotSpecifiedText");
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000CF RID: 207 RVA: 0x00004178 File Offset: 0x00002378
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x00004180 File Offset: 0x00002380
		[DataSourceProperty]
		public string UnavailableText
		{
			get
			{
				return this._unavailableText;
			}
			set
			{
				base.SetField<string>(ref this._unavailableText, value, "UnavailableText");
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00004198 File Offset: 0x00002398
		public ModOptionsVM()
		{
			this._logger = GenericServiceProvider.GetService<ILogger<ModOptionsVM>>() ?? NullLogger<ModOptionsVM>.Instance;
			this.SearchText = string.Empty;
			this.InitializeModSettings();
			this.RefreshValues();
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000424A File Offset: 0x0000244A
		private void InitializeModSettings()
		{
			Task.Factory.StartNew(delegate(object syncContext)
			{
				try
				{
					SynchronizationContext uiContext = syncContext as SynchronizationContext;
					if (uiContext != null)
					{
						if (SynchronizationContext.Current == uiContext)
						{
							LoggerExtensions.LogWarning(this._logger, "SynchronizationContext.Current is the UI SynchronizationContext", Array.Empty<object>());
						}
						BaseSettingsProvider instance = BaseSettingsProvider.Instance;
						IEnumerable<SettingsVM> enumerable;
						if (instance == null)
						{
							enumerable = null;
						}
						else
						{
							enumerable = from vm in instance.SettingsDefinitions.Parallel<SettingsDefinition>().Select(delegate(SettingsDefinition s)
								{
									SettingsVM result;
									try
									{
										result = new SettingsVM(s, this);
									}
									catch (Exception e2)
									{
										LoggerExtensions.LogError(this._logger, e2, "Error while creating a ViewModel for settings {Id}", new object[] { s.SettingsId });
										InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=HNduGf7H5a}There was an error while parsing settings from '" + s.SettingsId + "'! Please contact the MCM developers and the mod developer!", null).ToString(), Colors.Red));
										result = null;
									}
									return result;
								})
								where vm != null
								select vm;
						}
						IEnumerable<SettingsVM> settingsVM = enumerable;
						foreach (SettingsVM viewModel in (settingsVM ?? Array.Empty<SettingsVM>()))
						{
							uiContext.Send(delegate(object state)
							{
								SettingsVM vm = state as SettingsVM;
								if (vm != null)
								{
									this.ModSettingsList.Add(new SettingsEntryVM(vm, new Action<SettingsEntryVM>(this.ExecuteSelect)));
									vm.RefreshValues();
								}
							}, viewModel);
						}
						this.ModSettingsList.Sort((from x in KeyComparer<SettingsEntryVM>
							orderby x.Id.StartsWith("MCM") || x.Id.StartsWith("Testing") || x.Id.StartsWith("ModLib") descending
							select x).ThenByDescending((SettingsEntryVM x) => x.DisplayName, new AlphanumComparatorFast()));
					}
				}
				catch (Exception e)
				{
					LoggerExtensions.LogError(this._logger, e, "Error while creating ViewModels for the settings", Array.Empty<object>());
					InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=JLKaTyJcyu}There was a major error while building the settings list! Please contact the MCM developers!", null).ToString(), Colors.Red));
				}
			}, SynchronizationContext.Current);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00004268 File Offset: 0x00002468
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = new TextObject("{=ModOptionsVM_Name}Mod Options", null).ToString();
			this.DoneButtonText = new TextObject("{=WiNRdfsm}Done", null).ToString();
			this.CancelButtonText = new TextObject("{=3CpNUnVl}Cancel", null).ToString();
			this.ModsText = new TextObject("{=ModOptionsPageView_Mods}Mods", null).ToString();
			this.ModNameNotSpecifiedText = new TextObject("{=ModOptionsVM_NotSpecified}Mod Name not Specified.", null).ToString();
			this.UnavailableText = new TextObject("{=ModOptionsVM_Unavailable}Settings are available within a Game Session!", null).ToString();
			this.PresetsSelectorCopy.RefreshValues();
			foreach (SettingsEntryVM viewModel in this.ModSettingsList)
			{
				viewModel.RefreshValues();
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000434C File Offset: 0x0000254C
		private void OnPresetsSelectorChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			MCMSelectorVM<MCMSelectorItemVM<PresetKey>> selector = sender as MCMSelectorVM<MCMSelectorItemVM<PresetKey>>;
			if (selector == null)
			{
				return;
			}
			if (propertyChangedEventArgs.PropertyName != "SelectedIndex")
			{
				return;
			}
			if (this.IsDisabled)
			{
				SettingsEntryVM selectedEntry = this.SelectedEntry;
				bool flag;
				if (selectedEntry == null)
				{
					flag = null != null;
				}
				else
				{
					SettingsVM settingsVM = selectedEntry.SettingsVM;
					flag = ((settingsVM != null) ? settingsVM.PresetsSelector : null) != null;
				}
				if (flag && selector.SelectedIndex == -1)
				{
					this.DoPresetsSelectorCopyWithoutEvents(delegate
					{
						this.PresetsSelectorCopy.SelectedIndex = this.SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex;
					});
				}
				return;
			}
			if (selector.SelectedItem == null || selector.SelectedIndex == -1)
			{
				return;
			}
			if (selector.ItemList.Count < selector.SelectedIndex)
			{
				return;
			}
			PresetKey presetKey = selector.ItemList[selector.SelectedIndex].OriginalItem;
			string titleText = new TextObject("{=ModOptionsVM_ChangeToPreset}Change to preset '{PRESET}'", new Dictionary<string, object> { { "PRESET", presetKey.Name } }).ToString();
			string value = "{=ModOptionsVM_Discard}Are you sure you wish to discard the current settings for {NAME} to '{ITEM}'?";
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			string key = "NAME";
			SettingsEntryVM selectedEntry2 = this.SelectedEntry;
			dictionary.Add(key, (selectedEntry2 != null) ? selectedEntry2.DisplayName : null);
			dictionary.Add("ITEM", presetKey.Name);
			Action <>9__3;
			InformationManager.ShowInquiry(new InquiryData(titleText, new TextObject(value, dictionary).ToString(), true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), delegate()
			{
				if (this.SelectedEntry != null)
				{
					SettingsVM settingsVM2 = this.SelectedEntry.SettingsVM;
					if (settingsVM2 != null)
					{
						settingsVM2.ChangePreset(presetKey.Id);
					}
					SettingsEntryVM selectedMod = this.SelectedEntry;
					this.ExecuteSelect(null);
					this.ExecuteSelect(selectedMod);
				}
			}, delegate()
			{
				ModOptionsVM <>4__this = this;
				Action action;
				if ((action = <>9__3) == null)
				{
					action = (<>9__3 = delegate()
					{
						MCMSelectorVM<MCMSelectorItemVM<PresetKey>> presetsSelectorCopy = this.PresetsSelectorCopy;
						SettingsEntryVM selectedEntry3 = this.SelectedEntry;
						int? num;
						if (selectedEntry3 == null)
						{
							num = null;
						}
						else
						{
							SettingsVM settingsVM2 = selectedEntry3.SettingsVM;
							num = ((settingsVM2 != null) ? new int?(settingsVM2.PresetsSelector.SelectedIndex) : null);
						}
						int? num2 = num;
						presetsSelectorCopy.SelectedIndex = num2.GetValueOrDefault(-1);
					});
				}
				<>4__this.DoPresetsSelectorCopyWithoutEvents(action);
			}, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x000044D0 File Offset: 0x000026D0
		private void OnModPresetsSelectorChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			MCMSelectorVM<MCMSelectorItemVM<PresetKey>> selector = sender as MCMSelectorVM<MCMSelectorItemVM<PresetKey>>;
			if (selector == null)
			{
				return;
			}
			if (propertyChangedEventArgs.PropertyName != "SelectedIndex")
			{
				return;
			}
			this.DoPresetsSelectorCopyWithoutEvents(delegate
			{
				this.PresetsSelectorCopy.SelectedIndex = selector.SelectedIndex;
			});
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00004524 File Offset: 0x00002724
		public void ExecuteClose()
		{
			if (this.IsDisabled)
			{
				return;
			}
			foreach (SettingsVM viewModel in (from x in this.ModSettingsList
				select x.SettingsVM).OfType<SettingsVM>())
			{
				viewModel.URS.UndoAll();
				viewModel.URS.ClearStack();
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x000045B4 File Offset: 0x000027B4
		public bool ExecuteCancel()
		{
			return this.ExecuteCancelInternal(true, null);
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x000045BE File Offset: 0x000027BE
		public void StartTyping()
		{
			OptionsVMPatch.BlockSwitch = true;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x000045C6 File Offset: 0x000027C6
		public void StopTyping()
		{
			OptionsVMPatch.BlockSwitch = false;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x000045D0 File Offset: 0x000027D0
		[NullableContext(2)]
		public bool ExecuteCancelInternal(bool popScreen, Action onClose = null)
		{
			this.OnFinalize();
			if (popScreen)
			{
				ScreenManager.PopScreen();
			}
			else if (onClose != null)
			{
				onClose();
			}
			foreach (SettingsVM viewModel in (from x in this.ModSettingsList
				select x.SettingsVM).OfType<SettingsVM>())
			{
				viewModel.URS.UndoAll();
				viewModel.URS.ClearStack();
			}
			return true;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00004670 File Offset: 0x00002870
		public void ExecuteDone()
		{
			this.ExecuteDoneInternal(true, null);
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000467C File Offset: 0x0000287C
		[NullableContext(2)]
		public void ExecuteDoneInternal(bool popScreen, Action onClose = null)
		{
			List<SettingsVM> settingsVMs = (from x in this.ModSettingsList
				select x.SettingsVM).OfType<SettingsVM>().ToList<SettingsVM>();
			if (!settingsVMs.Any((SettingsVM x) => x.URS.ChangesMade))
			{
				this.OnFinalize();
				if (popScreen)
				{
					ScreenManager.PopScreen();
					return;
				}
				Action onClose2 = onClose;
				if (onClose2 == null)
				{
					return;
				}
				onClose2();
				return;
			}
			else
			{
				List<SettingsVM> changedModSettings = (from x in settingsVMs
					where x.URS.ChangesMade
					select x).ToList<SettingsVM>();
				bool requireRestart = changedModSettings.Any((SettingsVM x) => x.RestartRequired());
				if (requireRestart)
				{
					InformationManager.ShowInquiry(InquiryDataUtils.CreateTranslatable("{=ModOptionsVM_RestartTitle}Game Needs to Restart", "{=ModOptionsVM_RestartDesc}The game needs to be restarted to apply mod settings changes. Do you want to close the game now?", true, true, "{=aeouhelq}Yes", "{=3CpNUnVl}Cancel", delegate
					{
						foreach (SettingsVM changedModSetting2 in changedModSettings)
						{
							changedModSetting2.SaveSettings();
							changedModSetting2.URS.ClearStack();
						}
						this.OnFinalize();
						Action onClose4 = onClose;
						if (onClose4 != null)
						{
							onClose4();
						}
						Utilities.QuitGame();
					}, delegate
					{
					}), false, false);
					return;
				}
				foreach (SettingsVM changedModSetting in changedModSettings)
				{
					changedModSetting.SaveSettings();
					changedModSetting.URS.ClearStack();
				}
				this.OnFinalize();
				if (popScreen)
				{
					ScreenManager.PopScreen();
					return;
				}
				Action onClose3 = onClose;
				if (onClose3 == null)
				{
					return;
				}
				onClose3();
				return;
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00004838 File Offset: 0x00002A38
		[NullableContext(2)]
		public void ExecuteSelect(SettingsEntryVM viewModel)
		{
			if (this.IsDisabled)
			{
				return;
			}
			if (this.SelectedEntry != viewModel)
			{
				if (this.SelectedEntry != null)
				{
					this.SelectedEntry.IsSelected = false;
				}
				this.SelectedEntry = viewModel;
				if (this.SelectedEntry != null)
				{
					this.SelectedEntry.IsSelected = true;
				}
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00004886 File Offset: 0x00002A86
		private void RefreshPresetList()
		{
			SettingsEntryVM selectedEntry = this.SelectedEntry;
			if (((selectedEntry != null) ? selectedEntry.SettingsVM : null) == null)
			{
				return;
			}
			this.SelectedEntry.SettingsVM.ReloadPresetList();
			this.DoPresetsSelectorCopyWithoutEvents(delegate
			{
				this.PresetsSelectorCopy.Refresh(from x in this.SelectedEntry.SettingsVM.PresetsSelector.ItemList
					select x.OriginalItem, this.SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex);
			});
		}

		// Token: 0x060000DF RID: 223 RVA: 0x000048C0 File Offset: 0x00002AC0
		private void OverridePreset(Action onOverride)
		{
			InformationManager.ShowInquiry(InquiryDataUtils.CreateTranslatable("{=ModOptionsVM_OverridePreset}Preset Already Exists", "{=ModOptionsVM_OverridePresetDesc}Preset already exists! Do you want to override it?", true, true, "{=aeouhelq}Yes", "{=3CpNUnVl}Cancel", onOverride, delegate
			{
			}), false, false);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00004910 File Offset: 0x00002B10
		public void ExecuteManagePresets()
		{
			ModOptionsVM.<>c__DisplayClass76_0 CS$<>8__locals1 = new ModOptionsVM.<>c__DisplayClass76_0();
			CS$<>8__locals1.<>4__this = this;
			ModOptionsVM.<>c__DisplayClass76_0 CS$<>8__locals2 = CS$<>8__locals1;
			SettingsEntryVM selectedEntry = this.SelectedEntry;
			BaseSettings settings;
			if (selectedEntry == null)
			{
				settings = null;
			}
			else
			{
				SettingsVM settingsVM = selectedEntry.SettingsVM;
				settings = ((settingsVM != null) ? settingsVM.SettingsInstance : null);
			}
			CS$<>8__locals2.settings = settings;
			if (CS$<>8__locals1.settings == null)
			{
				return;
			}
			CS$<>8__locals1.fileSystem = GenericServiceProvider.GetService<IFileSystemProvider>();
			if (CS$<>8__locals1.fileSystem == null)
			{
				return;
			}
			MCMSelectorItemVM<PresetKey> selectedItem = this.PresetsSelectorCopy.SelectedItem;
			if (((selectedItem != null) ? selectedItem.OriginalItem : null) == null)
			{
				return;
			}
			List<InquiryElement> inquiries = new List<InquiryElement>
			{
				new InquiryElement("import_preset", new TextObject("{=ModOptionsVM_ManagePresetsImport}Import a new Preset", null).ToString(), null)
			};
			if (this.PresetsSelectorCopy.SelectedItem.OriginalItem.Id == "custom")
			{
				inquiries.Add(new InquiryElement("save_preset", new TextObject("{=ModOptionsVM_SaveAsPreset}Save As Preset", null).ToString(), null));
			}
			string id = this.PresetsSelectorCopy.SelectedItem.OriginalItem.Id;
			if (!(id == "custom") && !(id == "default"))
			{
				inquiries.Add(new InquiryElement("export_preset", new TextObject("{=ModOptionsVM_ManagePresetsExport}Export Preset '{PRESETNAME}'", new Dictionary<string, object> { 
				{
					"PRESETNAME",
					this.PresetsSelectorCopy.SelectedItem.OriginalItem.Name
				} }).ToString(), null));
				inquiries.Add(new InquiryElement("delete_preset", new TextObject("{=ModOptionsVM_ManagePresetsDelete}Delete Preset '{PRESETNAME}'", new Dictionary<string, object> { 
				{
					"PRESETNAME",
					this.PresetsSelectorCopy.SelectedItem.OriginalItem.Name
				} }).ToString(), null));
			}
			MBInformationManager.ShowMultiSelectionInquiry(InquiryDataUtils.CreateMultiTranslatable("{=ModOptionsVM_ManagePresets}Manage Presets", "", inquiries, true, 1, 1, "{=5Unqsx3N}Confirm", "{=3CpNUnVl}Cancel", new Action<List<InquiryElement>>(CS$<>8__locals1.<ExecuteManagePresets>g__OnActionSelected|4), delegate(List<InquiryElement> _)
			{
			}), false, false);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00004B00 File Offset: 0x00002D00
		public void ExecuteManageSettingsPack()
		{
			string titleText = "{=ModOptionsVM_ManagePacks}Manage Settings Packs";
			string descriptionText = "";
			List<InquiryElement> list = new List<InquiryElement>(2);
			list.Add(new InquiryElement("import_pack", new TextObject("{=ModOptionsVM_ManagePackImport}Import a Settings Pack", null).ToString(), null));
			list.Add(new InquiryElement("export_pack", new TextObject("{=ModOptionsVM_ManagePackExport}Export Settings Pack", null).ToString(), null));
			MBInformationManager.ShowMultiSelectionInquiry(InquiryDataUtils.CreateMultiTranslatable(titleText, descriptionText, list, true, 1, 1, "{=5Unqsx3N}Confirm", "{=3CpNUnVl}Cancel", new Action<List<InquiryElement>>(ModOptionsVM.<ExecuteManageSettingsPack>g__OnActionSelected|77_2), delegate(List<InquiryElement> _)
			{
			}), false, false);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00004BA4 File Offset: 0x00002DA4
		public override void OnFinalize()
		{
			foreach (SettingsEntryVM modSettings in this.ModSettingsList)
			{
				modSettings.OnFinalize();
			}
			SettingPropertyDefinitionCache.Clear();
			base.OnFinalize();
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00004BFC File Offset: 0x00002DFC
		private void DoPresetsSelectorCopyWithoutEvents(Action action)
		{
			this.PresetsSelectorCopy.PropertyChanged -= this.OnPresetsSelectorChange;
			action();
			this.PresetsSelectorCopy.PropertyChanged += this.OnPresetsSelectorChange;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00004F8A File Offset: 0x0000318A
		[CompilerGenerated]
		internal static void <ExecuteManagePresets>g__ImportNewPreset|76_1(GameDirectory settingsDirectory)
		{
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00004F8C File Offset: 0x0000318C
		[CompilerGenerated]
		internal static void <ExecuteManageSettingsPack>g__ImportPack|77_0()
		{
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00004F8E File Offset: 0x0000318E
		[CompilerGenerated]
		internal static void <ExecuteManageSettingsPack>g__ExportPack|77_1()
		{
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00004F90 File Offset: 0x00003190
		[CompilerGenerated]
		internal static void <ExecuteManageSettingsPack>g__OnActionSelected|77_2(List<InquiryElement> selected)
		{
			object identifier = selected[0].Identifier;
			string text = identifier as string;
			if (text != null)
			{
				if (text == "import_pack")
				{
					ModOptionsVM.<ExecuteManageSettingsPack>g__ImportPack|77_0();
					return;
				}
				if (!(text == "export_pack"))
				{
					return;
				}
				ModOptionsVM.<ExecuteManageSettingsPack>g__ExportPack|77_1();
			}
		}

		// Token: 0x0400002D RID: 45
		private readonly ILogger<ModOptionsVM> _logger;

		// Token: 0x0400002E RID: 46
		private string _titleLabel = string.Empty;

		// Token: 0x0400002F RID: 47
		private string _doneButtonText = string.Empty;

		// Token: 0x04000030 RID: 48
		private string _cancelButtonText = string.Empty;

		// Token: 0x04000031 RID: 49
		private string _modsText = string.Empty;

		// Token: 0x04000032 RID: 50
		private string _hintText = string.Empty;

		// Token: 0x04000033 RID: 51
		private string _modNameNotSpecifiedText = string.Empty;

		// Token: 0x04000034 RID: 52
		private string _unavailableText = string.Empty;

		// Token: 0x04000035 RID: 53
		[Nullable(2)]
		private SettingsEntryVM _selectedEntry;

		// Token: 0x04000036 RID: 54
		private string _searchText = string.Empty;
	}
}
