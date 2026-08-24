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
				"无法打开政策效果模块管理界面。",
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
			_dataSource.SetSaveFailure("保存失败：" + error);
			InformationManager.DisplayMessage(new InformationMessage(
				"保存政策效果模块设置失败：" + error,
				Color.FromUint(4294901760u)));
			return;
		}
		Close(silent: true);
		InformationManager.DisplayMessage(new InformationMessage(
			"政策效果模块设置已保存，仅影响之后新发起的政策。",
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
		TitleText = "政策效果模块管理";
		DescriptionText = "分别控制四类新政策能检索到哪些效果。修改只在保存后生效，不会改变已有、待审或正在生成的政策。";
		ModuleHeaderText = "模块";
		PlayerHeaderText = "玩家政策";
		LocalHeaderText = "地方政策";
		RulerHeaderText = "统治者政策";
		VassalHeaderText = "附庸国政策";
		RestoreDefaultsText = "恢复默认";
		CancelText = "取消";
		SaveText = "保存并关闭";
		StatusText = "所有改动都暂存在当前界面；取消或 Esc 会丢弃本次修改。";
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
		StatusText = "已把工作副本恢复为所有兼容渠道开启；点击“保存并关闭”后才会写入。";
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
		StatusText = string.IsNullOrWhiteSpace(message) ? "保存失败。" : message.Trim();
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
		StatusText = "有未保存的修改。只有新发起的政策会使用保存后的检索范围。";
	}

	private void RefreshSummary()
	{
		SummaryText = string.Format(
			"已启用  玩家 {0}/{1}   地方 {2}/{3}   统治者 {4}/{5}   附庸国 {6}/{7}",
			ModuleItems.Count(row => row.CanTogglePlayer && row.PlayerPolicyEnabled),
			ModuleItems.Count(row => row.CanTogglePlayer),
			ModuleItems.Count(row => row.CanToggleLocal && row.LocalPolicyEnabled),
			ModuleItems.Count(row => row.CanToggleLocal),
			ModuleItems.Count(row => row.CanToggleRuler && row.RulerPolicyEnabled),
			ModuleItems.Count(row => row.CanToggleRuler),
			ModuleItems.Count(row => row.CanToggleVassal && row.VassalPolicyEnabled),
			ModuleItems.Count(row => row.CanToggleVassal));
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
		ModuleNameText = module.Descriptor?.PlayerDisplayName ?? module.Id;
		CapabilitySummaryText = (module.Descriptor?.CatalogSummary ?? module.RetrievalText ?? string.Empty).Trim();
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
		return !supported ? "不可用" : enabled ? "已开启" : "已关闭";
	}
}
