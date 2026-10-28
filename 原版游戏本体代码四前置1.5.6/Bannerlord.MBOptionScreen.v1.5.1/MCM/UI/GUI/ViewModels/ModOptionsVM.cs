using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BUTR.DependencyInjection;
using Bannerlord.ModuleManager;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using MCM.Implementation;
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

namespace MCM.UI.GUI.ViewModels;

internal sealed class ModOptionsVM : ViewModel
{
	private static readonly Bannerlord.ModuleManager.AlphanumComparatorFast NameComparer = new Bannerlord.ModuleManager.AlphanumComparatorFast();

	private static readonly IComparer<SettingsEntryVM> ModSettingsListComparer = Comparer<SettingsEntryVM>.Create(delegate(SettingsEntryVM x, SettingsEntryVM y)
	{
		int num = IsMcmEntry(x).CompareTo(IsMcmEntry(y));
		return (num == 0) ? NameComparer.Compare(x.DisplayName, y.DisplayName) : num;
	});

	private readonly ILogger<ModOptionsVM> _logger;

	private string _titleLabel = string.Empty;

	private string _doneButtonText = string.Empty;

	private string _cancelButtonText = string.Empty;

	private string _modsText = string.Empty;

	private string _hintText = string.Empty;

	private string _modNameNotSpecifiedText = string.Empty;

	private string _unavailableText = string.Empty;

	private SettingsEntryVM? _selectedEntry;

	private string _searchText = string.Empty;

	private SettingsEntryVM? SelectedEntry
	{
		get
		{
			return _selectedEntry;
		}
		set
		{
			MCMSelectorVM<MCMSelectorItemVM<PresetKey>> mCMSelectorVM = _selectedEntry?.SettingsVM?.PresetsSelector;
			if (mCMSelectorVM != null)
			{
				mCMSelectorVM.PropertyChanged -= OnModPresetsSelectorChange;
			}
			if (!SetField(ref _selectedEntry, value, "SelectedMod"))
			{
				return;
			}
			OnPropertyChanged("IsSettingVisible");
			OnPropertyChanged("IsSettingUnavailableVisible");
			OnPropertyChanged("SelectedDisplayName");
			OnPropertyChanged("SomethingSelected");
			DoPresetsSelectorCopyWithoutEvents(delegate
			{
				MCMSelectorVM<MCMSelectorItemVM<PresetKey>> mCMSelectorVM2 = SelectedEntry?.SettingsVM?.PresetsSelector;
				if (mCMSelectorVM2 != null)
				{
					mCMSelectorVM2.PropertyChanged += OnModPresetsSelectorChange;
					PresetsSelectorCopy.Refresh(SelectedEntry.SettingsVM.PresetsSelector.ItemList.Select((MCMSelectorItemVM<PresetKey> x) => x.OriginalItem), SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex);
				}
				else
				{
					PresetsSelectorCopy.Refresh(Array.Empty<object>(), -1);
				}
			});
			OnPropertyChanged("IsPresetsSelectorVisible");
		}
	}

	public bool IsDisabled { get; set; }

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _titleLabel;
		}
		set
		{
			SetField(ref _titleLabel, value, "Name");
		}
	}

	[DataSourceProperty]
	public string DoneButtonText
	{
		get
		{
			return _doneButtonText;
		}
		set
		{
			SetField(ref _doneButtonText, value, "DoneButtonText");
		}
	}

	[DataSourceProperty]
	public string CancelButtonText
	{
		get
		{
			return _cancelButtonText;
		}
		set
		{
			SetField(ref _cancelButtonText, value, "CancelButtonText");
		}
	}

	[DataSourceProperty]
	public string ModsText
	{
		get
		{
			return _modsText;
		}
		set
		{
			SetField(ref _modsText, value, "ModsText");
		}
	}

	[DataSourceProperty]
	public MBBindingList<SettingsEntryVM> ModSettingsList { get; } = new MBBindingList<SettingsEntryVM>();

	[DataSourceProperty]
	public SettingsVM? SelectedMod => SelectedEntry?.SettingsVM;

	[DataSourceProperty]
	public string SelectedDisplayName
	{
		get
		{
			if (SelectedEntry != null)
			{
				return SelectedEntry.DisplayName;
			}
			return ModNameNotSpecifiedText;
		}
	}

	[DataSourceProperty]
	public bool SomethingSelected => SelectedEntry != null;

	[DataSourceProperty]
	public string HintText
	{
		get
		{
			return _hintText;
		}
		set
		{
			if (SetField(ref _hintText, value, "HintText"))
			{
				OnPropertyChanged("IsHintVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHintVisible => !string.IsNullOrWhiteSpace(HintText);

	[DataSourceProperty]
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (!SetField(ref _searchText, value, "SearchText"))
			{
				return;
			}
			SettingsEntryVM? selectedEntry = SelectedEntry;
			if (selectedEntry == null || !(selectedEntry.SettingsVM?.SettingPropertyGroups.Count > 0))
			{
				return;
			}
			foreach (SettingsPropertyGroupVM settingPropertyGroup in SelectedEntry.SettingsVM.SettingPropertyGroups)
			{
				settingPropertyGroup.NotifySearchChanged();
			}
		}
	}

	[DataSourceProperty]
	public MCMSelectorVM<MCMSelectorItemVM<PresetKey>> PresetsSelectorCopy { get; } = new MCMSelectorVM<MCMSelectorItemVM<PresetKey>>(Enumerable.Empty<PresetKey>(), -1);

	[DataSourceProperty]
	public bool IsPresetsSelectorVisible => SelectedEntry != null;

	[DataSourceProperty]
	public bool IsSettingVisible => SelectedEntry?.SettingsVM != null;

	[DataSourceProperty]
	public bool IsSettingUnavailableVisible => SelectedEntry?.SettingsVM == null;

	[DataSourceProperty]
	public string ModNameNotSpecifiedText
	{
		get
		{
			return _modNameNotSpecifiedText;
		}
		set
		{
			SetField(ref _modNameNotSpecifiedText, value, "ModNameNotSpecifiedText");
		}
	}

	[DataSourceProperty]
	public string UnavailableText
	{
		get
		{
			return _unavailableText;
		}
		set
		{
			SetField(ref _unavailableText, value, "UnavailableText");
		}
	}

	private static bool IsMcmEntry(SettingsEntryVM entry)
	{
		if (!entry.Id.StartsWith("MCM") && !entry.Id.StartsWith("Testing"))
		{
			return entry.Id.StartsWith("ModLib");
		}
		return true;
	}

	public ModOptionsVM()
	{
		_logger = GenericServiceProvider.GetService<ILogger<ModOptionsVM>>() ?? NullLogger<ModOptionsVM>.Instance;
		SearchText = string.Empty;
		InitializeModSettings();
		RefreshValues();
	}

	private void InitializeModSettings()
	{
		Task.Factory.StartNew(delegate(object syncContext)
		{
			try
			{
				if (syncContext is SynchronizationContext synchronizationContext)
				{
					if (SynchronizationContext.Current == synchronizationContext)
					{
						_logger.LogWarning("SynchronizationContext.Current is the UI SynchronizationContext");
					}
					IEnumerable<SettingsVM> enumerable = BaseSettingsProvider.Instance?.SettingsDefinitions.Parallel().Select(delegate(SettingsDefinition s)
					{
						try
						{
							return new SettingsVM(s, this);
						}
						catch (Exception exception2)
						{
							_logger.LogError(exception2, "Error while creating a ViewModel for settings {Id}", s.SettingsId);
							InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=HNduGf7H5a}There was an error while parsing settings from '" + s.SettingsId + "'! Please contact the MCM developers and the mod developer!").ToString(), Colors.Red));
							return (SettingsVM)null;
						}
					}).Where((SettingsVM vm) => vm != null);
					foreach (SettingsVM item in enumerable ?? Array.Empty<SettingsVM>())
					{
						synchronizationContext.Send(delegate(object state)
						{
							if (state is SettingsVM settingsVM)
							{
								ModSettingsList.Add(new SettingsEntryVM(settingsVM, ExecuteSelect));
								settingsVM.RefreshValues();
							}
						}, item);
					}
					ModSettingsList.Sort(ModSettingsListComparer);
				}
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error while creating ViewModels for the settings");
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=JLKaTyJcyu}There was a major error while building the settings list! Please contact the MCM developers!").ToString(), Colors.Red));
			}
		}, SynchronizationContext.Current);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = new TextObject("{=ModOptionsVM_Name}Mod Options").ToString();
		DoneButtonText = new TextObject("{=WiNRdfsm}Done").ToString();
		CancelButtonText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		ModsText = new TextObject("{=ModOptionsPageView_Mods}Mods").ToString();
		ModNameNotSpecifiedText = new TextObject("{=ModOptionsVM_NotSpecified}Mod Name not Specified.").ToString();
		UnavailableText = new TextObject("{=ModOptionsVM_Unavailable}Settings are available within a Game Session!").ToString();
		PresetsSelectorCopy.RefreshValues();
		foreach (SettingsEntryVM modSettings in ModSettingsList)
		{
			modSettings.RefreshValues();
		}
	}

	private void OnPresetsSelectorChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
	{
		if (!(sender is MCMSelectorVM<MCMSelectorItemVM<PresetKey>> mCMSelectorVM) || propertyChangedEventArgs.PropertyName != "SelectedIndex")
		{
			return;
		}
		if (IsDisabled)
		{
			if (SelectedEntry?.SettingsVM?.PresetsSelector != null && mCMSelectorVM.SelectedIndex == -1)
			{
				DoPresetsSelectorCopyWithoutEvents(delegate
				{
					PresetsSelectorCopy.SelectedIndex = SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex;
				});
			}
		}
		else
		{
			if (mCMSelectorVM.SelectedItem == null || mCMSelectorVM.SelectedIndex == -1 || mCMSelectorVM.ItemList.Count < mCMSelectorVM.SelectedIndex)
			{
				return;
			}
			PresetKey presetKey = mCMSelectorVM.ItemList[mCMSelectorVM.SelectedIndex].OriginalItem;
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=ModOptionsVM_ChangeToPreset}Change to preset '{PRESET}'", new Dictionary<string, object> { { "PRESET", presetKey.Name } }).ToString(), new TextObject("{=ModOptionsVM_Discard}Are you sure you wish to discard the current settings for {NAME} to '{ITEM}'?", new Dictionary<string, object>
			{
				{
					"NAME",
					SelectedEntry?.DisplayName
				},
				{ "ITEM", presetKey.Name }
			}).ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), delegate
			{
				if (SelectedEntry != null)
				{
					SelectedEntry.SettingsVM?.ChangePreset(presetKey.Id);
					SettingsEntryVM selectedEntry = SelectedEntry;
					ExecuteSelect(null);
					ExecuteSelect(selectedEntry);
				}
			}, delegate
			{
				DoPresetsSelectorCopyWithoutEvents(delegate
				{
					PresetsSelectorCopy.SelectedIndex = SelectedEntry?.SettingsVM?.PresetsSelector.SelectedIndex ?? (-1);
				});
			}));
		}
	}

	private void OnModPresetsSelectorChange(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
	{
		MCMSelectorVM<MCMSelectorItemVM<PresetKey>> selector = sender as MCMSelectorVM<MCMSelectorItemVM<PresetKey>>;
		if (selector != null && !(propertyChangedEventArgs.PropertyName != "SelectedIndex"))
		{
			DoPresetsSelectorCopyWithoutEvents(delegate
			{
				PresetsSelectorCopy.SelectedIndex = selector.SelectedIndex;
			});
		}
	}

	public void ExecuteClose()
	{
		if (IsDisabled)
		{
			return;
		}
		foreach (SettingsVM item in ModSettingsList.Select((SettingsEntryVM x) => x.SettingsVM).OfType<SettingsVM>())
		{
			item.URS.UndoAll();
			item.URS.ClearStack();
		}
	}

	public bool ExecuteCancel()
	{
		return ExecuteCancelInternal(popScreen: true);
	}

	public void StartTyping()
	{
		OptionsVMPatch.BlockSwitch = true;
	}

	public void StopTyping()
	{
		OptionsVMPatch.BlockSwitch = false;
	}

	public bool ExecuteCancelInternal(bool popScreen, Action? onClose = null)
	{
		OnFinalize();
		if (popScreen)
		{
			ScreenManager.PopScreen();
		}
		else
		{
			onClose?.Invoke();
		}
		foreach (SettingsVM item in ModSettingsList.Select((SettingsEntryVM x) => x.SettingsVM).OfType<SettingsVM>())
		{
			item.URS.UndoAll();
			item.URS.ClearStack();
		}
		return true;
	}

	public void ExecuteDone()
	{
		ExecuteDoneInternal(popScreen: true);
	}

	public void ExecuteDoneInternal(bool popScreen, Action? onClose = null)
	{
		List<SettingsVM> source = ModSettingsList.Select((SettingsEntryVM x) => x.SettingsVM).OfType<SettingsVM>().ToList();
		if (!source.Any((SettingsVM x) => x.URS.ChangesMade))
		{
			OnFinalize();
			if (popScreen)
			{
				ScreenManager.PopScreen();
			}
			else
			{
				onClose?.Invoke();
			}
			return;
		}
		List<SettingsVM> changedModSettings = source.Where((SettingsVM x) => x.URS.ChangesMade).ToList();
		if (changedModSettings.Any((SettingsVM x) => x.RestartRequired()))
		{
			InformationManager.ShowInquiry(InquiryDataUtils.CreateTranslatable("{=ModOptionsVM_RestartTitle}Game Needs to Restart", "{=ModOptionsVM_RestartDesc}The game needs to be restarted to apply mod settings changes. Do you want to close the game now?", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "{=aeouhelq}Yes", "{=3CpNUnVl}Cancel", delegate
			{
				foreach (SettingsVM item in changedModSettings)
				{
					item.SaveSettings();
					item.URS.ClearStack();
				}
				OnFinalize();
				onClose?.Invoke();
				Utilities.QuitGame();
			}, delegate
			{
			}));
			return;
		}
		foreach (SettingsVM item2 in changedModSettings)
		{
			item2.SaveSettings();
			item2.URS.ClearStack();
		}
		OnFinalize();
		if (popScreen)
		{
			ScreenManager.PopScreen();
		}
		else
		{
			onClose?.Invoke();
		}
	}

	public void ExecuteSelect(SettingsEntryVM? viewModel)
	{
		if (!IsDisabled && SelectedEntry != viewModel)
		{
			if (SelectedEntry != null)
			{
				SelectedEntry.IsSelected = false;
			}
			SelectedEntry = viewModel;
			if (SelectedEntry != null)
			{
				SelectedEntry.IsSelected = true;
			}
		}
	}

	private void RefreshPresetList()
	{
		if (SelectedEntry?.SettingsVM == null)
		{
			return;
		}
		SelectedEntry.SettingsVM.ReloadPresetList();
		DoPresetsSelectorCopyWithoutEvents(delegate
		{
			PresetsSelectorCopy.Refresh(SelectedEntry.SettingsVM.PresetsSelector.ItemList.Select((MCMSelectorItemVM<PresetKey> x) => x.OriginalItem), SelectedEntry.SettingsVM.PresetsSelector.SelectedIndex);
		});
	}

	private void OverridePreset(Action onOverride)
	{
		InformationManager.ShowInquiry(InquiryDataUtils.CreateTranslatable("{=ModOptionsVM_OverridePreset}Preset Already Exists", "{=ModOptionsVM_OverridePresetDesc}Preset already exists! Do you want to override it?", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "{=aeouhelq}Yes", "{=3CpNUnVl}Cancel", onOverride, delegate
		{
		}));
	}

	public void ExecuteManagePresets()
	{
		BaseSettings settings = SelectedEntry?.SettingsVM?.SettingsInstance;
		if (settings == null)
		{
			return;
		}
		IFileSystemProvider fileSystem = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (fileSystem != null && (object)PresetsSelectorCopy.SelectedItem?.OriginalItem != null)
		{
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("import_preset", new TextObject("{=ModOptionsVM_ManagePresetsImport}Import a new Preset").ToString(), null)
			};
			if (PresetsSelectorCopy.SelectedItem.OriginalItem.Id == "custom")
			{
				list.Add(new InquiryElement("save_preset", new TextObject("{=ModOptionsVM_SaveAsPreset}Save As Preset").ToString(), null));
			}
			string id = PresetsSelectorCopy.SelectedItem.OriginalItem.Id;
			if (!(id == "custom") && !(id == "default"))
			{
				list.Add(new InquiryElement("export_preset", new TextObject("{=ModOptionsVM_ManagePresetsExport}Export Preset '{PRESETNAME}'", new Dictionary<string, object> { 
				{
					"PRESETNAME",
					PresetsSelectorCopy.SelectedItem.OriginalItem.Name
				} }).ToString(), null));
				list.Add(new InquiryElement("delete_preset", new TextObject("{=ModOptionsVM_ManagePresetsDelete}Delete Preset '{PRESETNAME}'", new Dictionary<string, object> { 
				{
					"PRESETNAME",
					PresetsSelectorCopy.SelectedItem.OriginalItem.Name
				} }).ToString(), null));
			}
			MBInformationManager.ShowMultiSelectionInquiry(InquiryDataUtils.CreateMultiTranslatable("{=ModOptionsVM_ManagePresets}Manage Presets", "", list, isExitShown: true, 1, 1, "{=5Unqsx3N}Confirm", "{=3CpNUnVl}Cancel", OnActionSelected, delegate
			{
			}));
		}
		void DeletePreset(GameFile presetFile)
		{
			fileSystem.WriteData(presetFile, null);
			RefreshPresetList();
		}
		void ExportPreset(GameFile presetFile)
		{
			string systemPath = fileSystem.GetSystemPath(presetFile);
		}
		static void ImportNewPreset(GameDirectory settingsDirectory)
		{
		}
		void OnActionSelected(List<InquiryElement> selected)
		{
			PresetKey presetKey = PresetsSelectorCopy.SelectedItem?.OriginalItem;
			if ((object)presetKey != null)
			{
				GameDirectory orCreateDirectory = fileSystem.GetOrCreateDirectory(fileSystem.GetModSettingsDirectory(), "Presets");
				GameDirectory orCreateDirectory2 = fileSystem.GetOrCreateDirectory(orCreateDirectory, settings.Id);
				string fileName = presetKey.Id + ".json";
				object identifier = selected[0].Identifier;
				switch (identifier as string)
				{
				case "save_preset":
					SaveAsPreset(orCreateDirectory2);
					break;
				case "import_preset":
					ImportNewPreset(orCreateDirectory2);
					break;
				case "export_preset":
				{
					GameFile file2 = fileSystem.GetFile(orCreateDirectory2, fileName);
					if ((object)file2 != null)
					{
						ExportPreset(file2);
					}
					break;
				}
				case "delete_preset":
				{
					GameFile file = fileSystem.GetFile(orCreateDirectory2, fileName);
					if ((object)file != null)
					{
						DeletePreset(file);
					}
					break;
				}
				}
			}
		}
		void SaveAsPreset(GameDirectory settingsDirectory)
		{
			BaseSettings settingsSnapshot = settings.CopyAsNew();
			InformationManager.ShowTextInquiry(InquiryDataUtils.CreateTextTranslatable("{=ModOptionsVM_SaveAsPreset}Save As Preset", "{=ModOptionsVM_SaveAsPresetDesc}Choose the name of the preset", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "{=5Unqsx3N}Confirm", "{=3CpNUnVl}Cancel", delegate(string input)
			{
				HashSet<char> hashSet = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars());
				StringBuilder stringBuilder = new StringBuilder();
				string text = input;
				foreach (char c in text)
				{
					stringBuilder.Append(hashSet.Contains(c) ? '_' : c);
				}
				string id2 = stringBuilder.ToString();
				string filename = id2 + ".json";
				if ((object)fileSystem.GetFile(settingsDirectory, filename) != null)
				{
					OverridePreset(SavePreset);
				}
				else
				{
					SavePreset();
				}
				void SavePreset()
				{
					GameFile orCreateFile = fileSystem.GetOrCreateFile(settingsDirectory, filename);
					JsonSettingsPreset jsonSettingsPreset = new JsonSettingsPreset(settingsSnapshot.Id, id2, input, orCreateFile, () => (BaseSettings)null);
					if (!jsonSettingsPreset.SavePreset(settingsSnapshot))
					{
						InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=ModOptionsVM_SaveAsPresetError}Failed to save preset '{PRESETNAME}'!", new Dictionary<string, object> { { "PRESETNAME", input } }).ToString(), Color.White));
					}
					RefreshPresetList();
				}
			}, delegate
			{
			}));
		}
	}

	public void ExecuteManageSettingsPack()
	{
		MBInformationManager.ShowMultiSelectionInquiry(InquiryDataUtils.CreateMultiTranslatable("{=ModOptionsVM_ManagePacks}Manage Settings Packs", "", new List<InquiryElement>(2)
		{
			new InquiryElement("import_pack", new TextObject("{=ModOptionsVM_ManagePackImport}Import a Settings Pack").ToString(), null),
			new InquiryElement("export_pack", new TextObject("{=ModOptionsVM_ManagePackExport}Export Settings Pack").ToString(), null)
		}, isExitShown: true, 1, 1, "{=5Unqsx3N}Confirm", "{=3CpNUnVl}Cancel", OnActionSelected, delegate
		{
		}));
		static void ExportPack()
		{
		}
		static void ImportPack()
		{
		}
		static void OnActionSelected(List<InquiryElement> selected)
		{
			object identifier = selected[0].Identifier;
			switch (identifier as string)
			{
			case "import_pack":
				ImportPack();
				break;
			case "export_pack":
				ExportPack();
				break;
			}
		}
	}

	public override void OnFinalize()
	{
		foreach (SettingsEntryVM modSettings in ModSettingsList)
		{
			modSettings.OnFinalize();
		}
		SettingPropertyDefinitionCache.Clear();
		base.OnFinalize();
	}

	private void DoPresetsSelectorCopyWithoutEvents(Action action)
	{
		PresetsSelectorCopy.PropertyChanged -= OnPresetsSelectorChange;
		action();
		PresetsSelectorCopy.PropertyChanged += OnPresetsSelectorChange;
	}
}
