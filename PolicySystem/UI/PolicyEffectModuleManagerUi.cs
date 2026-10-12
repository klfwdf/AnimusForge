using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectModuleManagerUi
{
	internal static void Open()
	{
		if (!PolicyEffectModuleManagerPopup.Show())
		{
			InformationManager.DisplayMessage(new InformationMessage(
				PolicyUiTexts.Get("AFPOL_UI_ModuleManagerOpenFailed", "Unable to open the policy effect module manager."),
				Color.FromUint(4294901760u)));
		}
	}
}

internal sealed class PolicyEffectModuleManagerPopup
{
	private enum PendingAction
	{
		None,
		Cancel,
		Save
	}

	private static PolicyEffectModuleManagerPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly PolicyEffectModuleManagerVM _dataSource;
	private PendingAction _pendingAction;
	private bool _isClosed;

	private PolicyEffectModuleManagerPopup(ScreenBase screen)
	{
		_screen = screen;
		_dataSource = new PolicyEffectModuleManagerVM(HandleSaveRequested, HandleCancelRequested);
		_layer = new GauntletLayer("PolicyEffectModuleManagerPopup", 4210, false);
	}

	internal static bool Show()
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			PolicyEffectModuleManagerPopup popup = new PolicyEffectModuleManagerPopup(topScreen);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "policy-effect-module-manager-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	internal static void ProcessDeferredCloseIfNeeded()
	{
		PolicyEffectModuleManagerPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCancelForEscapeKey())
		{
			popup.HandleCancelRequested();
		}
		popup.ProcessPendingAction();
	}

	private void Open()
	{
		_layer.LoadMovie("PolicyEffectModuleManagerPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private bool ShouldCancelForEscapeKey()
	{
		try
		{
			return _layer?.Input != null
				&& (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape));
		}
		catch
		{
		}
		try
		{
			return Input.IsKeyReleased(InputKey.Escape);
		}
		catch
		{
			return false;
		}
	}

	private void HandleSaveRequested()
	{
		if (!_isClosed && _pendingAction == PendingAction.None)
		{
			_pendingAction = PendingAction.Save;
		}
	}

	private void HandleCancelRequested()
	{
		if (!_isClosed && _pendingAction == PendingAction.None)
		{
			_pendingAction = PendingAction.Cancel;
		}
	}

	private void ProcessPendingAction()
	{
		if (_isClosed || _pendingAction == PendingAction.None)
		{
			return;
		}
		PendingAction action = _pendingAction;
		_pendingAction = PendingAction.None;
		if (action == PendingAction.Cancel)
		{
			Close(silent: true);
			return;
		}
		if (!PolicyEffectModuleRetrievalSettings.TrySave(_dataSource.CreateSaveSnapshot(), out string error))
		{
			_dataSource.SetSaveFailure(PolicyUiTexts.Format("AFPOL_UI_SaveFailed", "Save failed: {ERROR}", "ERROR", error));
			InformationManager.DisplayMessage(new InformationMessage(
				PolicyUiTexts.Format("AFPOL_UI_ModuleManagerSaveFailed", "Failed to save policy effect module settings: {ERROR}", "ERROR", error),
				Color.FromUint(4294901760u)));
			return;
		}
		Close(silent: true);
		InformationManager.DisplayMessage(new InformationMessage(
			PolicyUiTexts.Get("AFPOL_UI_ModuleManagerSaved", "Policy effect module settings saved. They affect newly started policies only."),
			Color.FromUint(4282569842u)));
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "policy-effect-module-manager-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

internal sealed class PolicyEffectModuleManagerVM : ViewModel
{
	private readonly Action _onSave;
	private readonly Action _onCancel;
	private bool _suppressRowRefresh;
	private string _summaryText;
	private string _statusText;

	internal PolicyEffectModuleManagerVM(Action onSave, Action onCancel)
	{
		_onSave = onSave;
		_onCancel = onCancel;
		TitleText = PolicyUiTexts.Get("AFPOL_UI_ModuleManagerTitle", "Policy Effect Module Manager");
		DescriptionText = PolicyUiTexts.Get("AFPOL_UI_ModuleManagerDescription", "Choose which effects each of the four new policy contexts can retrieve. Changes take effect after saving and do not alter existing, pending, or generating policies.");
		ModuleHeaderText = PolicyUiTexts.Get("AFPOL_UI_Module", "Module");
		PlayerHeaderText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicy", "Player Policy");
		LocalHeaderText = PolicyUiTexts.Get("AFPOL_UI_LocalPolicy", "Local Policy");
		RulerHeaderText = PolicyUiTexts.Get("AFPOL_UI_RulerPolicy", "Ruler Policy");
		VassalHeaderText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicy", "Vassal Policy");
		RestoreDefaultsText = PolicyUiTexts.Get("AFPOL_UI_RestoreDefaults", "Restore Defaults");
		CancelText = PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel");
		SaveText = PolicyUiTexts.Get("AFPOL_UI_SaveAndClose", "Save and Close");
		StatusText = PolicyUiTexts.Get("AFPOL_UI_ModuleManagerInitialStatus", "Changes are kept in this window until saved; Cancel or Esc discards them.");
		ModuleItems = new MBBindingList<PolicyEffectModuleManagerRowVM>();
		Dictionary<string, PolicyEffectModuleRetrievalState> states
			= PolicyEffectModuleRetrievalSettings.CreateEditableStateSnapshot();
		foreach (PolicyEffectModuleManagerRowVM row in BuildRows(states, HandleRowChanged))
		{
			ModuleItems.Add(row);
		}
		RefreshSummary();
	}

	[DataSourceProperty] public string TitleText { get; }
	[DataSourceProperty] public string DescriptionText { get; }
	[DataSourceProperty] public string ModuleHeaderText { get; }
	[DataSourceProperty] public string PlayerHeaderText { get; }
	[DataSourceProperty] public string LocalHeaderText { get; }
	[DataSourceProperty] public string RulerHeaderText { get; }
	[DataSourceProperty] public string VassalHeaderText { get; }
	[DataSourceProperty] public string RestoreDefaultsText { get; }
	[DataSourceProperty] public string CancelText { get; }
	[DataSourceProperty] public string SaveText { get; }
	[DataSourceProperty] public MBBindingList<PolicyEffectModuleManagerRowVM> ModuleItems { get; }

	[DataSourceProperty]
	public string SummaryText
	{
		get => _summaryText;
		private set
		{
			if (!string.Equals(value, _summaryText, StringComparison.Ordinal))
			{
				_summaryText = value;
				OnPropertyChangedWithValue(value, nameof(SummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		private set
		{
			if (!string.Equals(value, _statusText, StringComparison.Ordinal))
			{
				_statusText = value;
				OnPropertyChangedWithValue(value, nameof(StatusText));
			}
		}
	}

	public void ExecuteRestoreDefaults()
	{
		_suppressRowRefresh = true;
		try
		{
			foreach (PolicyEffectModuleManagerRowVM row in ModuleItems)
			{
				row.RestoreDefault();
			}
		}
		finally
		{
			_suppressRowRefresh = false;
		}
		RefreshSummary();
		StatusText = PolicyUiTexts.Get("AFPOL_UI_RestoredDefaultsStatus", "The working copy now enables every compatible context; click Save and Close to write the changes.");
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void ExecuteSave()
	{
		_onSave?.Invoke();
	}

	internal Dictionary<string, PolicyEffectModuleRetrievalState> CreateSaveSnapshot()
	{
		return ModuleItems.ToDictionary(
			row => row.ModuleIdText,
			row => row.CreateStateSnapshot(),
			StringComparer.Ordinal);
	}

	internal void SetSaveFailure(string message)
	{
		StatusText = string.IsNullOrWhiteSpace(message) ? PolicyUiTexts.Get("AFPOL_UI_SaveFailedShort", "Save failed.") : message.Trim();
	}

	internal static IReadOnlyList<PolicyEffectModuleManagerRowVM> BuildRowsForContractTests(
		IReadOnlyDictionary<string, PolicyEffectModuleRetrievalState> states)
	{
		return BuildRows(states, null);
	}

	private static IReadOnlyList<PolicyEffectModuleManagerRowVM> BuildRows(
		IReadOnlyDictionary<string, PolicyEffectModuleRetrievalState> states,
		Action onChanged)
	{
		return PolicyEffectModuleCatalog.Modules
			.Select(module => new PolicyEffectModuleManagerRowVM(
				module,
				states != null && states.TryGetValue(module.Id, out PolicyEffectModuleRetrievalState state)
					? state
					: new PolicyEffectModuleRetrievalState(),
				onChanged))
			.ToArray();
	}

	private void HandleRowChanged()
	{
		if (_suppressRowRefresh)
		{
			return;
		}
		RefreshSummary();
		StatusText = PolicyUiTexts.Get("AFPOL_UI_UnsavedStatus", "There are unsaved changes. Only newly started policies use the saved retrieval scope.");
	}

	private void RefreshSummary()
	{
		SummaryText = PolicyUiTexts.Format(
			"AFPOL_UI_ModuleManagerSummary",
			"Enabled  Player {PLAYER_ON}/{PLAYER_TOTAL}   Local {LOCAL_ON}/{LOCAL_TOTAL}   Ruler {RULER_ON}/{RULER_TOTAL}   Vassal {VASSAL_ON}/{VASSAL_TOTAL}",
			"PLAYER_ON", ModuleItems.Count(row => row.CanTogglePlayer && row.PlayerPolicyEnabled).ToString(),
			"PLAYER_TOTAL", ModuleItems.Count(row => row.CanTogglePlayer).ToString(),
			"LOCAL_ON", ModuleItems.Count(row => row.CanToggleLocal && row.LocalPolicyEnabled).ToString(),
			"LOCAL_TOTAL", ModuleItems.Count(row => row.CanToggleLocal).ToString(),
			"RULER_ON", ModuleItems.Count(row => row.CanToggleRuler && row.RulerPolicyEnabled).ToString(),
			"RULER_TOTAL", ModuleItems.Count(row => row.CanToggleRuler).ToString(),
			"VASSAL_ON", ModuleItems.Count(row => row.CanToggleVassal && row.VassalPolicyEnabled).ToString(),
			"VASSAL_TOTAL", ModuleItems.Count(row => row.CanToggleVassal).ToString());
	}
}

internal sealed class PolicyEffectModuleManagerRowVM : ViewModel
{
	private readonly Action _onChanged;
	private bool _playerPolicyEnabled;
	private bool _localPolicyEnabled;
	private bool _rulerPolicyEnabled;
	private bool _vassalPolicyEnabled;

	internal PolicyEffectModuleManagerRowVM(
		IPolicyEffectModule module,
		PolicyEffectModuleRetrievalState state,
		Action onChanged)
	{
		if (module == null)
		{
			throw new ArgumentNullException(nameof(module));
		}
		_onChanged = onChanged;
		ModuleIdText = module.Id;
		ModuleNameText = PolicyEffectUiText.Name(module);
		CapabilitySummaryText = PolicyEffectUiText.Summary(module);
		CanTogglePlayer = PolicyEffectModuleRetrievalSettings.IsContextSupported(module, PolicyEffectRetrievalContext.PlayerKingdom);
		CanToggleLocal = PolicyEffectModuleRetrievalSettings.IsContextSupported(module, PolicyEffectRetrievalContext.PlayerLocal);
		CanToggleRuler = PolicyEffectModuleRetrievalSettings.IsContextSupported(module, PolicyEffectRetrievalContext.NpcRulerKingdom);
		CanToggleVassal = PolicyEffectModuleRetrievalSettings.IsContextSupported(module, PolicyEffectRetrievalContext.PlayerVassal);
		PolicyEffectModuleRetrievalState initial = state ?? new PolicyEffectModuleRetrievalState();
		_playerPolicyEnabled = CanTogglePlayer && initial.PlayerPolicyEnabled;
		_localPolicyEnabled = CanToggleLocal && initial.LocalPolicyEnabled;
		_rulerPolicyEnabled = CanToggleRuler && initial.RulerPolicyEnabled;
		_vassalPolicyEnabled = CanToggleVassal && initial.VassalPolicyEnabled;
	}

	[DataSourceProperty] public string ModuleIdText { get; }
	[DataSourceProperty] public string ModuleNameText { get; }
	[DataSourceProperty] public string CapabilitySummaryText { get; }
	[DataSourceProperty] public bool CanTogglePlayer { get; }
	[DataSourceProperty] public bool CanToggleLocal { get; }
	[DataSourceProperty] public bool CanToggleRuler { get; }
	[DataSourceProperty] public bool CanToggleVassal { get; }

	[DataSourceProperty]
	public bool PlayerPolicyEnabled
	{
		get => _playerPolicyEnabled;
		private set => SetEnabled(ref _playerPolicyEnabled, value, nameof(PlayerPolicyEnabled), nameof(PlayerStatusText), nameof(PlayerPolicyDisabled), nameof(PlayerPolicyUnavailable));
	}

	[DataSourceProperty]
	public bool LocalPolicyEnabled
	{
		get => _localPolicyEnabled;
		private set => SetEnabled(ref _localPolicyEnabled, value, nameof(LocalPolicyEnabled), nameof(LocalStatusText), nameof(LocalPolicyDisabled), nameof(LocalPolicyUnavailable));
	}

	[DataSourceProperty]
	public bool RulerPolicyEnabled
	{
		get => _rulerPolicyEnabled;
		private set => SetEnabled(ref _rulerPolicyEnabled, value, nameof(RulerPolicyEnabled), nameof(RulerStatusText), nameof(RulerPolicyDisabled), nameof(RulerPolicyUnavailable));
	}

	[DataSourceProperty]
	public bool VassalPolicyEnabled
	{
		get => _vassalPolicyEnabled;
		private set => SetEnabled(ref _vassalPolicyEnabled, value, nameof(VassalPolicyEnabled), nameof(VassalStatusText), nameof(VassalPolicyDisabled), nameof(VassalPolicyUnavailable));
	}

	[DataSourceProperty] public string PlayerStatusText => BuildStatusText(CanTogglePlayer, PlayerPolicyEnabled);
	[DataSourceProperty] public string LocalStatusText => BuildStatusText(CanToggleLocal, LocalPolicyEnabled);
	[DataSourceProperty] public string RulerStatusText => BuildStatusText(CanToggleRuler, RulerPolicyEnabled);
	[DataSourceProperty] public string VassalStatusText => BuildStatusText(CanToggleVassal, VassalPolicyEnabled);
	[DataSourceProperty] public bool PlayerPolicyDisabled => CanTogglePlayer && !PlayerPolicyEnabled;
	[DataSourceProperty] public bool LocalPolicyDisabled => CanToggleLocal && !LocalPolicyEnabled;
	[DataSourceProperty] public bool RulerPolicyDisabled => CanToggleRuler && !RulerPolicyEnabled;
	[DataSourceProperty] public bool VassalPolicyDisabled => CanToggleVassal && !VassalPolicyEnabled;
	[DataSourceProperty] public bool PlayerPolicyUnavailable => !CanTogglePlayer;
	[DataSourceProperty] public bool LocalPolicyUnavailable => !CanToggleLocal;
	[DataSourceProperty] public bool RulerPolicyUnavailable => !CanToggleRuler;
	[DataSourceProperty] public bool VassalPolicyUnavailable => !CanToggleVassal;

	public void ExecuteTogglePlayer()
	{
		if (CanTogglePlayer) PlayerPolicyEnabled = !PlayerPolicyEnabled;
	}

	public void ExecuteToggleLocal()
	{
		if (CanToggleLocal) LocalPolicyEnabled = !LocalPolicyEnabled;
	}

	public void ExecuteToggleRuler()
	{
		if (CanToggleRuler) RulerPolicyEnabled = !RulerPolicyEnabled;
	}

	public void ExecuteToggleVassal()
	{
		if (CanToggleVassal) VassalPolicyEnabled = !VassalPolicyEnabled;
	}

	internal void RestoreDefault()
	{
		PlayerPolicyEnabled = CanTogglePlayer;
		LocalPolicyEnabled = CanToggleLocal;
		RulerPolicyEnabled = CanToggleRuler;
		VassalPolicyEnabled = CanToggleVassal;
	}

	internal PolicyEffectModuleRetrievalState CreateStateSnapshot()
	{
		return new PolicyEffectModuleRetrievalState
		{
			PlayerPolicyEnabled = CanTogglePlayer && PlayerPolicyEnabled,
			LocalPolicyEnabled = CanToggleLocal && LocalPolicyEnabled,
			RulerPolicyEnabled = CanToggleRuler && RulerPolicyEnabled,
			VassalPolicyEnabled = CanToggleVassal && VassalPolicyEnabled
		};
	}

	private void SetEnabled(ref bool field, bool value, params string[] propertyNames)
	{
		if (field == value)
		{
			return;
		}
		field = value;
		foreach (string propertyName in propertyNames)
		{
			OnPropertyChanged(propertyName);
		}
		_onChanged?.Invoke();
	}

	private static string BuildStatusText(bool supported, bool enabled)
	{
		return !supported
			? PolicyUiTexts.Get("AFPOL_UI_Unavailable", "Unavailable")
			: enabled
				? PolicyUiTexts.Get("AFPOL_UI_Enabled", "Enabled")
				: PolicyUiTexts.Get("AFPOL_UI_Disabled", "Disabled");
	}
}
