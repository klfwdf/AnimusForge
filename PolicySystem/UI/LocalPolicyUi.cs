using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal sealed class LocalPolicyFiefData
{
	public string FiefId { get; set; }

	public string NameText { get; set; }

	public string TypeText { get; set; }

	public bool IsSelected { get; set; }
}

internal sealed class LocalPolicyComposeData
{
	public string DateText { get; set; }

	public bool CanPublish { get; set; }

	public string BlockReason { get; set; }

	public string InitialPolicyName { get; set; }

	public string InitialPolicyContent { get; set; }

	public string InitialDurationText { get; set; }

	public string TitleText { get; set; }

	public string PublishText { get; set; }

	public bool RequireExplicitDuration { get; set; }

	public List<LocalPolicyFiefData> Fiefs { get; set; } = new List<LocalPolicyFiefData>();
}

internal sealed class LocalPolicyComposePopup
{
	private static LocalPolicyComposePopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly LocalPolicyComposePopupVM _dataSource;
	private readonly Action<string, string, string, string, List<string>> _onPublish;
	private readonly Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> _onAutoDraft;
	private readonly Action _onCancel;
	private bool _isClosed;
	private bool _publishPending;
	private bool _cancelPending;
	private string _pendingName;
	private string _pendingContent;
	private string _pendingDuration;
	private string _pendingDate;
	private List<string> _pendingFiefIds;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private LocalPolicyComposePopup(ScreenBase screen, LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> onAutoDraft, Action onCancel)
	{
		_screen = screen;
		_onPublish = onPublish;
		_onAutoDraft = onAutoDraft;
		_onCancel = onCancel;
		_dataSource = new LocalPolicyComposePopupVM(data, HandlePublishRequested, HandleAutoDraftRequested, HandleCancelRequested);
		_layer = new GauntletLayer("LocalPolicyComposePopup", 4005, false);
	}

	public static bool Show(LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> onAutoDraft, Action onCancel)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			LocalPolicyComposePopup popup = new LocalPolicyComposePopup(topScreen, data ?? new LocalPolicyComposeData(), onPublish, onAutoDraft, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-compose-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseAction()
	{
		_activePopup?.ProcessPendingCloseAction();
	}

	private void Open()
	{
		_layer.LoadMovie("LocalPolicyComposePopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try { _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory")); } catch { }
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandlePublishRequested(string name, string content, string duration, string date, List<string> fiefIds)
	{
		if (_isClosed || _publishPending || _cancelPending) return;
		_publishPending = true;
		_pendingName = name ?? "";
		_pendingContent = content ?? "";
		_pendingDuration = duration ?? "";
		_pendingDate = date ?? "";
		_pendingFiefIds = fiefIds?.ToList() ?? new List<string>();
	}

	private void HandleCancelRequested()
	{
		if (_isClosed || _publishPending || _cancelPending) return;
		_cancelPending = true;
	}

	private void HandleAutoDraftRequested(PlayerPolicyAutoDraftRequest request)
	{
		if (_isClosed) return;
		if (_onAutoDraft == null)
		{
			_dataSource.ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AiDraftUnavailable", "AI drafting is unavailable.")));
			return;
		}
		try
		{
			_onAutoDraft(request, result =>
			{
				if (!_isClosed) _dataSource.ApplyAutoDraftResult(request, result);
			});
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-compose-auto-draft-dispatch-failed", ex.Message, ex.ToString());
			_dataSource.ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AiDraftUiError", "AI drafting encountered a UI error; see the log.")));
		}
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || (!_publishPending && !_cancelPending)) return;
		bool publish = _publishPending;
		string name = _pendingName ?? "";
		string content = _pendingContent ?? "";
		string duration = _pendingDuration ?? "";
		string date = _pendingDate ?? "";
		List<string> fiefIds = _pendingFiefIds?.ToList() ?? new List<string>();
		_publishPending = false;
		_cancelPending = false;
		Close(silent: true);
		if (publish) _onPublish?.Invoke(name, content, duration, date, fiefIds);
		else _onCancel?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed) return;
		_isClosed = true;
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch { }
		try { _screen.RemoveLayer(_layer); }
		catch (Exception ex) { if (!silent) PolicySystemLog.Failure("UI", "local-compose-close-failed", ex.Message, ex.ToString()); }
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this)) _activePopup = null;
	}
}

internal sealed class LocalPolicyComposePopupVM : ViewModel
{
	private readonly Action<string, string, string, string, List<string>> _onPublish;
	private readonly Action<PlayerPolicyAutoDraftRequest> _onAutoDraft;
	private readonly Action _onCancel;
	private readonly bool _externalCanPublish;
	private readonly string _externalBlockReason;
	private readonly bool _requireExplicitDuration;
	private string _policyName;
	private string _policyContent;
	private string _durationText;
	private string _statusText;
	private bool _canPublish;
	private bool _canAutoDraft;
	private bool _isAutoDrafting;
	private string _autoDraftText;
	private int _selectedCount;

	public LocalPolicyComposePopupVM(LocalPolicyComposeData data, Action<string, string, string, string, List<string>> onPublish, Action<PlayerPolicyAutoDraftRequest> onAutoDraft, Action onCancel)
	{
		data ??= new LocalPolicyComposeData();
		_onPublish = onPublish;
		_onAutoDraft = onAutoDraft;
		_onCancel = onCancel;
		_externalCanPublish = data.CanPublish;
		_externalBlockReason = data.BlockReason ?? "";
		_requireExplicitDuration = data.RequireExplicitDuration;
		TitleText = string.IsNullOrWhiteSpace(data.TitleText) ? PolicyUiTexts.Get("AFPOL_UI_LocalPublishTitle", "Publish Local Policy") : data.TitleText;
		ScopeTitleText = PolicyUiTexts.Get("AFPOL_UI_LocalScopeTitle", "Select fiefs in scope");
		SelectAllText = PolicyUiTexts.Get("AFPOL_UI_SelectAll", "Select All");
		ClearText = PolicyUiTexts.Get("AFPOL_UI_ClearSelection", "Clear");
		NameLabelText = PolicyUiTexts.Get("AFPOL_UI_PolicyNameLabel", "Policy Name");
		ContentLabelText = PolicyUiTexts.Get("AFPOL_UI_LocalPolicyContentLabel", "Policy Content / AI Draft Input");
		DurationLabelText = PolicyUiTexts.Get("AFPOL_UI_DurationLabel", "Effect duration in days (leave blank for permanent)");
		DateText = string.IsNullOrWhiteSpace(data.DateText) ? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date") : data.DateText;
		PublishText = string.IsNullOrWhiteSpace(data.PublishText) ? PolicyUiTexts.Get("AFPOL_UI_LocalPublishTitle", "Publish Local Policy") : data.PublishText;
		AutoDraftText = PolicyUiTexts.Get("AFPOL_UI_AiDraft", "AI Draft");
		CancelText = PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel");
		FiefItems = new MBBindingList<LocalPolicyFiefItemVM>();
		foreach (LocalPolicyFiefData fief in data.Fiefs ?? new List<LocalPolicyFiefData>())
		{
			if (fief != null) FiefItems.Add(new LocalPolicyFiefItemVM(fief, RefreshCanPublish));
		}
		PolicyName = data.InitialPolicyName ?? "";
		PolicyContent = data.InitialPolicyContent ?? "";
		DurationText = data.InitialDurationText ?? "";
		RefreshCanPublish();
	}

	[DataSourceProperty] public string TitleText { get; set; }
	[DataSourceProperty] public string ScopeTitleText { get; set; }
	[DataSourceProperty] public string SelectAllText { get; set; }
	[DataSourceProperty] public string ClearText { get; set; }
	[DataSourceProperty] public string NameLabelText { get; set; }
	[DataSourceProperty] public string ContentLabelText { get; set; }
	[DataSourceProperty] public string DurationLabelText { get; set; }
	[DataSourceProperty] public string DateText { get; set; }
	[DataSourceProperty] public string PublishText { get; set; }
	[DataSourceProperty] public string CancelText { get; set; }
	[DataSourceProperty] public MBBindingList<LocalPolicyFiefItemVM> FiefItems { get; set; }

	[DataSourceProperty]
	public string PolicyName
	{
		get => _policyName;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, AnimusForgeTextInputSanitizer.MaxPolicyNameChars);
			if (text == _policyName) return;
			_policyName = text;
			OnPropertyChangedWithValue(text, nameof(PolicyName));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string PolicyContent
	{
		get => _policyContent;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxPolicyContentChars);
			if (text == _policyContent) return;
			_policyContent = text;
			OnPropertyChangedWithValue(text, nameof(PolicyContent));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string DurationText
	{
		get => _durationText;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, 16);
			if (text == _durationText) return;
			_durationText = text;
			OnPropertyChangedWithValue(text, nameof(DurationText));
			RefreshCanPublish();
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		set { if (value != _statusText) { _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); } }
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		set { if (value != _canPublish) { _canPublish = value; OnPropertyChangedWithValue(value, nameof(CanPublish)); } }
	}

	[DataSourceProperty]
	public bool CanAutoDraft
	{
		get => _canAutoDraft;
		set { if (value != _canAutoDraft) { _canAutoDraft = value; OnPropertyChangedWithValue(value, nameof(CanAutoDraft)); } }
	}

	[DataSourceProperty]
	public string AutoDraftText
	{
		get => _autoDraftText;
		set { if (value != _autoDraftText) { _autoDraftText = value; OnPropertyChangedWithValue(value, nameof(AutoDraftText)); } }
	}

	[DataSourceProperty]
	public int SelectedCount
	{
		get => _selectedCount;
		set { if (value != _selectedCount) { _selectedCount = value; OnPropertyChangedWithValue(value, nameof(SelectedCount)); } }
	}

	public void ExecuteSelectAll() { foreach (LocalPolicyFiefItemVM item in FiefItems) item.SetSelected(true); RefreshCanPublish(); }
	public void ExecuteClear() { foreach (LocalPolicyFiefItemVM item in FiefItems) item.SetSelected(false); RefreshCanPublish(); }
	public void ExecuteCancel() => _onCancel?.Invoke();
	public void StartTyping() { }
	public void StopTyping() { }

	public void ExecuteAutoDraft()
	{
		RefreshCanPublish();
		if (!CanAutoDraft)
		{
			if (!PlayerPolicyAutoDraftInputContract.HasInput(PolicyName, PolicyContent)) StatusText = PolicyUiTexts.Get("AFPOL_UI_FillTitleOrContent", "Enter a policy title or content first.");
			return;
		}
		PlayerPolicyAutoDraftRequest request = new PlayerPolicyAutoDraftRequest
		{
			PlayerDescription = PolicyContent ?? "",
			ExistingPolicyName = PolicyName ?? "",
			DurationText = DurationText ?? "",
			ScopeKind = "local",
			DateText = DateText ?? "",
			SelectedFiefIds = FiefItems.Where(item => item.IsSelected).Select(item => item.FiefId).ToList()
		};
		_isAutoDrafting = true;
		AutoDraftText = PolicyUiTexts.Get("AFPOL_UI_AiDrafting", "AI drafting…");
		StatusText = PolicyUiTexts.Get("AFPOL_UI_AiDraftStatus", "AI is expanding the current input using the player-editable prompt; it does not read the general policy prompt, world context, or policy history, and it will not publish automatically.");
		RefreshCanPublish();
		try { _onAutoDraft?.Invoke(request); }
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-compose-auto-draft-start-failed", ex.Message, ex.ToString());
			ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AiDraftUiError", "AI drafting encountered a UI error; see the log.")));
		}
	}

	internal void ApplyAutoDraftResult(PlayerPolicyAutoDraftRequest request, PlayerPolicyAutoDraftResult result)
	{
		_isAutoDrafting = false;
		AutoDraftText = PolicyUiTexts.Get("AFPOL_UI_AiDraft", "AI Draft");
		List<string> currentFiefs = FiefItems.Where(item => item.IsSelected).Select(item => item.FiefId).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
		List<string> requestedFiefs = (request?.SelectedFiefIds ?? new List<string>()).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
		bool formUnchanged = request != null
			&& string.Equals(PolicyName ?? "", request.ExistingPolicyName ?? "", StringComparison.Ordinal)
			&& string.Equals(PolicyContent ?? "", request.PlayerDescription ?? "", StringComparison.Ordinal)
			&& string.Equals(DurationText ?? "", request.DurationText ?? "", StringComparison.Ordinal)
			&& currentFiefs.SequenceEqual(requestedFiefs, StringComparer.OrdinalIgnoreCase);
		if (!formUnchanged)
		{
			RefreshCanPublish();
			StatusText = PolicyUiTexts.Get("AFPOL_UI_AiDraftFormChanged", "The form changed while AI drafting was running; the stale result was discarded.");
			return;
		}
		if (result?.Success != true)
		{
			RefreshCanPublish();
			StatusText = string.IsNullOrWhiteSpace(result?.Error)
				? PolicyUiTexts.Get("AFPOL_UI_AiDraftFailed", "AI drafting failed; the original input was kept.")
				: MapPlayerPolicyAutoDraftErrorToDisplay(result.Error);
			return;
		}
		PolicyName = result.PolicyName ?? "";
		PolicyContent = result.PolicyContent ?? "";
		RefreshCanPublish();
		StatusText = PolicyUiTexts.Get("AFPOL_UI_AiDraftCompleted", "AI draft filled the form; review and edit it before publishing.");
	}

	private static string MapPlayerPolicyAutoDraftErrorToDisplay(string error)
	{
		switch ((error ?? string.Empty).Trim())
		{
			case "缺少政策描述。":
			case "请先填写政策标题或政策内容。":
				return PolicyUiTexts.Get("AFPOL_UI_FillTitleOrContent", "Enter a policy title or content first.");
			case "上一份政策仍在评议，暂时不能使用AI编写。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftBlockedDuringEvaluation", "AI drafting is unavailable while a policy evaluation is running.");
			case "已有一份内容正在AI编写，请稍候。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftAlreadyRunning", "Another policy draft is already in progress; please wait.");
			case "AI服务配置不可用，请检查模型、密钥与连接设置。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftServiceUnavailable", "AI drafting is unavailable; check the model, API key, and connection settings.");
			case "AI编写没有返回有效的结构化结果。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftEmptyResult", "AI drafting returned no valid structured result.");
			case "AI编写返回的结构化结果无法解析。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftUnparseableResult", "The structured result from AI drafting could not be parsed.");
			case "AI编写只能返回政策名和政策正文两个字段。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftFieldsOnly", "AI drafting must return only the policy name and policy content fields.");
			case "AI编写返回的政策名和政策正文必须是文本。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftFieldsMustBeText", "The policy name and policy content returned by AI drafting must be text.");
			case "AI编写返回了空的政策名或政策正文。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftEmptyFields", "AI drafting returned an empty policy name or policy content.");
			case "AI编写修改了玩家已指定的政策名。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftChangedPolicyName", "AI drafting changed the policy name specified by the player.");
			case "AI编写请求失败，请检查连接设置并查看日志。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftRequestFailed", "The AI drafting request failed; check the connection settings and see the log.");
			case "政策草案请求已因读档失效。":
				return PolicyUiTexts.Get("AFPOL_UI_AiDraftDiscardedAfterLoad", "The policy draft was discarded after loading a save.");
			default:
				return error ?? string.Empty;
		}
	}

	public void ExecutePublish()
	{
		RefreshCanPublish();
		if (!CanPublish) return;
		_onPublish?.Invoke(PolicyName ?? "", PolicyContent ?? "", DurationText ?? "", DateText ?? "", FiefItems.Where(x => x.IsSelected).Select(x => x.FiefId).ToList());
	}

	private void RefreshCanPublish()
	{
		SelectedCount = FiefItems?.Count(x => x.IsSelected) ?? 0;
		bool durationValid = (!_requireExplicitDuration || !string.IsNullOrWhiteSpace(DurationText))
			&& (string.IsNullOrWhiteSpace(DurationText) || (int.TryParse(DurationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) && days > 0));
		CanPublish = !_isAutoDrafting && _externalCanPublish && SelectedCount > 0 && !string.IsNullOrWhiteSpace(PolicyName) && !string.IsNullOrWhiteSpace(PolicyContent) && durationValid;
		bool hasName = !string.IsNullOrWhiteSpace(PolicyName);
		bool hasContent = !string.IsNullOrWhiteSpace(PolicyContent);
		CanAutoDraft = !_isAutoDrafting && _externalCanPublish && SelectedCount > 0
			&& PlayerPolicyAutoDraftInputContract.HasInput(PolicyName, PolicyContent) && durationValid;
		if (!_externalCanPublish) StatusText = string.IsNullOrWhiteSpace(_externalBlockReason) ? PolicyUiTexts.Get("AFPOL_UI_CannotPublishLocalPolicy", "The local policy cannot be published right now.") : _externalBlockReason;
		else if (SelectedCount <= 0) StatusText = PolicyUiTexts.Get("AFPOL_UI_SelectOwnedFief", "Select at least one town or castle owned by the player clan.");
		else if (!hasName && !hasContent) StatusText = PolicyUiTexts.Get("AFPOL_UI_FillForAiDraft", "Enter a policy title or content before using AI Draft.");
		else if (!hasName) StatusText = PolicyUiTexts.Get("AFPOL_UI_AiCompleteTitle", "Click AI Draft to complete the policy title and content.");
		else if (!hasContent) StatusText = PolicyUiTexts.Get("AFPOL_UI_AiCompleteContent", "Click AI Draft to complete the content from the policy title.");
		else if (!durationValid) StatusText = _requireExplicitDuration && string.IsNullOrWhiteSpace(DurationText)
			? PolicyUiTexts.Get("AFPOL_UI_DurationRequired", "The old record has no confirmed duration; enter a positive integer number of days.")
			: PolicyUiTexts.Get("AFPOL_UI_DurationPositive", "Duration must be blank or a positive integer number of days.");
		else StatusText = PolicyUiTexts.Format("AFPOL_UI_SelectedFiefs", "{COUNT} fiefs selected; scope is determined automatically by the selected fiefs.", "COUNT", SelectedCount.ToString(CultureInfo.InvariantCulture));
	}
}

internal sealed class LocalPolicyFiefItemVM : ViewModel
{
	private readonly Action _onChanged;
	private bool _isSelected;
	public LocalPolicyFiefItemVM(LocalPolicyFiefData data, Action onChanged)
	{
		_onChanged = onChanged;
		FiefId = data?.FiefId ?? "";
		NameText = data?.NameText ?? PolicyUiTexts.Get("AFPOL_UI_UnknownFief", "Unknown Fief");
		TypeText = data?.TypeText ?? PolicyUiTexts.Get("AFPOL_UI_Fief", "Fief");
		_isSelected = data?.IsSelected == true;
	}
	[DataSourceProperty] public string FiefId { get; set; }
	[DataSourceProperty] public string NameText { get; set; }
	[DataSourceProperty] public string TypeText { get; set; }
	[DataSourceProperty]
	public bool IsSelected
	{
		get => _isSelected;
		set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); _onChanged?.Invoke(); } }
	}
	public void ExecuteToggle() => IsSelected = !IsSelected;
	public void SetSelected(bool value) => IsSelected = value;
}

internal sealed class LocalPolicyHistoryData
{
	public List<LocalPolicyHistoryRecordData> Records { get; set; } = new List<LocalPolicyHistoryRecordData>();
}

internal sealed class LocalPolicyHistoryRecordData
{
	public string ScopeKind { get; set; }
	public string RecordId { get; set; }
	public string HistoryKey { get; set; }
	public bool CanDelete { get; set; }
	public string DateText { get; set; }
	public string PolicyNameText { get; set; }
	public string StatusText { get; set; }
	public string TargetText { get; set; }
	public string RemainingText { get; set; }
	public string ContentText { get; set; }
	public string FeedbackText { get; set; }
	public string EffectText { get; set; }
	public string CostText { get; set; }
	public string CycleText { get; set; }
	public string RenewalText { get; set; }
	public bool CanRenew { get; set; }
	public bool CanAbolish { get; set; }
	public bool CanReReview { get; set; }
}

internal sealed class LocalPolicyHistoryPopup
{
	private static LocalPolicyHistoryPopup _activePopup;
	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly LocalPolicyHistoryPopupVM _dataSource;
	private readonly Action _onClose;
	private bool _isClosed;
	private LocalPolicyHistoryPopup(ScreenBase screen, LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action<string> onReReview, Action onClose, Action<string> onDelete)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new LocalPolicyHistoryPopupVM(data, id => { Close(true); onRenew?.Invoke(id); }, id => { Close(true); onAbolish?.Invoke(id); }, id => { Close(true); onReReview?.Invoke(id); }, HandleClose, key => { Close(true); onDelete?.Invoke(key); });
		_layer = new GauntletLayer("LocalPolicyHistoryPopup", 4110, false);
	}
	public static bool Show(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action<string> onReReview, Action onClose)
		=> Show(data, onRenew, onAbolish, onReReview, onClose, null);
	public static bool Show(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action<string> onReReview, Action onClose, Action<string> onDelete)
	{
		ScreenBase screen = ScreenManager.TopScreen;
		if (screen == null) return false;
		try
		{
			_activePopup?.Close(true);
			LocalPolicyHistoryPopup popup = new LocalPolicyHistoryPopup(screen, data ?? new LocalPolicyHistoryData(), onRenew, onAbolish, onReReview, onClose, onDelete);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "local-history-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(true);
			_activePopup = null;
			return false;
		}
	}
	private void Open()
	{
		_layer.LoadMovie("LocalPolicyHistoryPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}
	private void HandleClose() { Close(true); _onClose?.Invoke(); }
	private void Close(bool silent)
	{
		if (_isClosed) return;
		_isClosed = true;
		try { _layer.InputRestrictions.ResetInputRestrictions(); _layer.IsFocusLayer = false; ScreenManager.TryLoseFocus(_layer); } catch { }
		try { _screen.RemoveLayer(_layer); } catch (Exception ex) { if (!silent) PolicySystemLog.Failure("UI", "local-history-close-failed", ex.Message, ex.ToString()); }
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this)) _activePopup = null;
	}
}

internal sealed class LocalPolicyHistoryPopupVM : ViewModel
{
	private readonly Action<string> _onDelete;
	private readonly Action<string> _onRenew;
	private readonly Action<string> _onAbolish;
	private readonly Action<string> _onReReview;
	private readonly Action _onClose;
	private readonly List<LocalPolicyHistoryRecordData> _allRecords;
	private LocalPolicyHistoryRecordItemVM _selected;
	private bool _hasRecords;
	private bool _showEmptyState;
	private bool _isLocalTabSelected;
	private bool _isVassalTabSelected;
	private bool _canRenew;
	private bool _canAbolish;
	private bool _canReReview;
	private string _emptyStateText = "";
	private string _policyNameText = "";
	private string _statusText = "";
	private string _targetText = "";
	private string _remainingText = "";
	private string _contentText = "";
	private string _feedbackText = "";
	private string _effectText = "";
	private string _costText = "";
	private string _cycleText = "";
	private string _renewalText = "";
	public LocalPolicyHistoryPopupVM(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action<string> onReReview, Action onClose)
		: this(data, onRenew, onAbolish, onReReview, onClose, null) { }
	public LocalPolicyHistoryPopupVM(LocalPolicyHistoryData data, Action<string> onRenew, Action<string> onAbolish, Action<string> onReReview, Action onClose, Action<string> onDelete)
	{
		_onDelete = onDelete; _onRenew = onRenew; _onAbolish = onAbolish; _onReReview = onReReview; _onClose = onClose;
		_allRecords = (data?.Records ?? new List<LocalPolicyHistoryRecordData>()).Where(x => x != null).ToList();
		RecordItems = new MBBindingList<LocalPolicyHistoryRecordItemVM>();
		TitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyRecordsTitle", "Policy Records");
		SubtitleText = PolicyUiTexts.Get("AFPOL_UI_LocalHistorySubtitle", "Local and vassal policies are managed separately; active records are retained and only the latest 100 ended records are kept.");
		LocalTabText = PolicyUiTexts.Get("AFPOL_UI_LocalPolicy", "Local Policy");
		VassalTabText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicy", "Vassal Policy");
		RenewText = PolicyUiTexts.Get("AFPOL_UI_RenewEffect", "Extend Effect");
		AbolishText = PolicyUiTexts.Get("AFPOL_UI_AbolishPolicy", "Stop Policy");
		ReReviewText = PolicyUiTexts.Get("AFPOL_UI_ReReview", "Re-review");
		CloseText = PolicyUiTexts.Get("AFPOL_UI_BackToPolicyMenu", "Back to Policy Menu");
		ContentSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyContentSection", "[Policy Content]");
		FeedbackSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackSection", "[Public Feedback]");
		ImpactSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyDailyImpactSection", "[Daily Impact]");
		bool showVassalFirst = !_allRecords.Any(x => !string.Equals(x.ScopeKind ?? "", "vassal", StringComparison.OrdinalIgnoreCase))
			&& _allRecords.Any(x => string.Equals(x.ScopeKind ?? "", "vassal", StringComparison.OrdinalIgnoreCase));
		ShowScope(showVassalFirst);
	}
	[DataSourceProperty] public string TitleText { get; set; }
	[DataSourceProperty] public string SubtitleText { get; set; }
	[DataSourceProperty] public string EmptyStateText { get => _emptyStateText; set { _emptyStateText = value; OnPropertyChangedWithValue(value, nameof(EmptyStateText)); } }
	[DataSourceProperty] public string LocalTabText { get; set; }
	[DataSourceProperty] public string VassalTabText { get; set; }
	[DataSourceProperty] public string DeleteText => PolicyUiTexts.Get("AFPOL_UI_DeleteRecord", "Delete Record");
	[DataSourceProperty] public bool CanDelete { get; private set; }
	[DataSourceProperty] public string RenewText { get; set; }
	[DataSourceProperty] public string AbolishText { get; set; }
	[DataSourceProperty] public string ReReviewText { get; set; }
	[DataSourceProperty] public string CloseText { get; set; }
	[DataSourceProperty] public string ContentSectionTitleText { get; set; }
	[DataSourceProperty] public string FeedbackSectionTitleText { get; set; }
	[DataSourceProperty] public string ImpactSectionTitleText { get; set; }
	[DataSourceProperty] public MBBindingList<LocalPolicyHistoryRecordItemVM> RecordItems { get; set; }
	[DataSourceProperty] public bool HasRecords { get => _hasRecords; set { if (value != _hasRecords) { _hasRecords = value; OnPropertyChangedWithValue(value, nameof(HasRecords)); } } }
	[DataSourceProperty] public bool ShowEmptyState { get => _showEmptyState; set { if (value != _showEmptyState) { _showEmptyState = value; OnPropertyChangedWithValue(value, nameof(ShowEmptyState)); } } }
	[DataSourceProperty] public bool IsLocalTabSelected { get => _isLocalTabSelected; set { if (value != _isLocalTabSelected) { _isLocalTabSelected = value; OnPropertyChangedWithValue(value, nameof(IsLocalTabSelected)); } } }
	[DataSourceProperty] public bool IsVassalTabSelected { get => _isVassalTabSelected; set { if (value != _isVassalTabSelected) { _isVassalTabSelected = value; OnPropertyChangedWithValue(value, nameof(IsVassalTabSelected)); } } }
	[DataSourceProperty] public bool CanRenew { get => _canRenew; set { if (value != _canRenew) { _canRenew = value; OnPropertyChangedWithValue(value, nameof(CanRenew)); } } }
	[DataSourceProperty] public bool CanAbolish { get => _canAbolish; set { if (value != _canAbolish) { _canAbolish = value; OnPropertyChangedWithValue(value, nameof(CanAbolish)); } } }
	[DataSourceProperty] public bool CanReReview { get => _canReReview; set { if (value != _canReReview) { _canReReview = value; OnPropertyChangedWithValue(value, nameof(CanReReview)); } } }
	[DataSourceProperty] public string PolicyNameText { get => _policyNameText; set { _policyNameText = value; OnPropertyChangedWithValue(value, nameof(PolicyNameText)); } }
	[DataSourceProperty] public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); } }
	[DataSourceProperty] public string TargetText { get => _targetText; set { _targetText = value; OnPropertyChangedWithValue(value, nameof(TargetText)); } }
	[DataSourceProperty] public string RemainingText { get => _remainingText; set { _remainingText = value; OnPropertyChangedWithValue(value, nameof(RemainingText)); } }
	[DataSourceProperty] public string ContentText { get => _contentText; set { _contentText = value; OnPropertyChangedWithValue(value, nameof(ContentText)); } }
	[DataSourceProperty] public string FeedbackText { get => _feedbackText; set { _feedbackText = value; OnPropertyChangedWithValue(value, nameof(FeedbackText)); } }
	[DataSourceProperty] public string EffectText { get => _effectText; set { _effectText = value; OnPropertyChangedWithValue(value, nameof(EffectText)); } }
	[DataSourceProperty] public string CostText { get => _costText; set { _costText = value; OnPropertyChangedWithValue(value, nameof(CostText)); } }
	[DataSourceProperty] public string CycleText { get => _cycleText; set { _cycleText = value; OnPropertyChangedWithValue(value, nameof(CycleText)); } }
	[DataSourceProperty] public string RenewalText { get => _renewalText; set { _renewalText = value; OnPropertyChangedWithValue(value, nameof(RenewalText)); } }
	private void Select(LocalPolicyHistoryRecordItemVM item)
	{
		if (_selected != null) _selected.IsSelected = false;
		_selected = item;
		if (item == null) return;
		item.IsSelected = true;
		PolicyNameText = item.PolicyNameText; StatusText = item.StatusText; TargetText = item.TargetText; RemainingText = item.RemainingText;
		ContentText = item.ContentText; FeedbackText = item.FeedbackText; EffectText = item.EffectText; CostText = item.CostText; CycleText = item.CycleText; RenewalText = item.RenewalText;
		CanDelete = item.CanDelete; OnPropertyChangedWithValue(CanDelete, nameof(CanDelete));
		CanRenew = item.CanRenew; CanAbolish = item.CanAbolish; CanReReview = item.CanReReview;
	}
	private void ShowScope(bool showVassal)
	{
		IsVassalTabSelected = showVassal;
		IsLocalTabSelected = !showVassal;
		if (_selected != null) _selected.IsSelected = false;
		_selected = null;
		RecordItems.Clear();
		foreach (LocalPolicyHistoryRecordData record in _allRecords.Where(x => string.Equals(x.ScopeKind ?? "", "vassal", StringComparison.OrdinalIgnoreCase) == showVassal))
		{
			RecordItems.Add(new LocalPolicyHistoryRecordItemVM(record, Select));
		}
		HasRecords = RecordItems.Count > 0;
		ShowEmptyState = !HasRecords;
		EmptyStateText = showVassal ? PolicyUiTexts.Get("AFPOL_UI_EmptyVassalRecords", "No vassal policy records.") : PolicyUiTexts.Get("AFPOL_UI_EmptyLocalRecords", "No local policy records.");
		if (HasRecords)
		{
			Select(RecordItems[0]);
		}
		else
		{
			PolicyNameText = ""; StatusText = ""; TargetText = ""; RemainingText = "";
			ContentText = ""; FeedbackText = ""; EffectText = ""; CostText = ""; CycleText = ""; RenewalText = "";
			CanDelete = false; OnPropertyChangedWithValue(CanDelete, nameof(CanDelete));
			CanRenew = false; CanAbolish = false; CanReReview = false;
		}
	}
	public void ExecuteShowLocalPolicies() => ShowScope(false);
	public void ExecuteShowVassalPolicies() => ShowScope(true);
	public void ExecuteDelete() { if (CanDelete && _selected != null) _onDelete?.Invoke(_selected.HistoryKey); }
	public void ExecuteRenew() { if (CanRenew && _selected != null) _onRenew?.Invoke(_selected.RecordId); }
	public void ExecuteAbolish() { if (CanAbolish && _selected != null) _onAbolish?.Invoke(_selected.RecordId); }
	public void ExecuteReReview() { if (CanReReview && _selected != null) _onReReview?.Invoke(_selected.RecordId); }
	public void ExecuteClose() => _onClose?.Invoke();
}

internal sealed class LocalPolicyHistoryRecordItemVM : ViewModel
{
	private readonly Action<LocalPolicyHistoryRecordItemVM> _onSelect;
	private bool _isSelected;
	public LocalPolicyHistoryRecordItemVM(LocalPolicyHistoryRecordData data, Action<LocalPolicyHistoryRecordItemVM> onSelect)
	{
		HistoryKey = data?.HistoryKey ?? ""; CanDelete = data?.CanDelete == true;
		_onSelect = onSelect; ScopeKind = data?.ScopeKind ?? "local"; RecordId = data?.RecordId ?? ""; DateText = data?.DateText ?? ""; PolicyNameText = data?.PolicyNameText ?? ""; StatusText = data?.StatusText ?? "";
		TargetText = data?.TargetText ?? ""; RemainingText = data?.RemainingText ?? ""; ContentText = data?.ContentText ?? ""; FeedbackText = data?.FeedbackText ?? "";
		EffectText = data?.EffectText ?? ""; CostText = data?.CostText ?? ""; CycleText = data?.CycleText ?? ""; RenewalText = data?.RenewalText ?? ""; CanRenew = data?.CanRenew == true; CanAbolish = data?.CanAbolish == true; CanReReview = data?.CanReReview == true;
	}
	public string HistoryKey { get; }
	public bool CanDelete { get; }
	[DataSourceProperty] public string ScopeKind { get; set; }
	[DataSourceProperty] public string RecordId { get; set; }
	[DataSourceProperty] public string DateText { get; set; }
	[DataSourceProperty] public string PolicyNameText { get; set; }
	[DataSourceProperty] public string StatusText { get; set; }
	[DataSourceProperty] public string TargetText { get; set; }
	[DataSourceProperty] public string RemainingText { get; set; }
	[DataSourceProperty] public string ContentText { get; set; }
	[DataSourceProperty] public string FeedbackText { get; set; }
	[DataSourceProperty] public string EffectText { get; set; }
	[DataSourceProperty] public string CostText { get; set; }
	[DataSourceProperty] public string CycleText { get; set; }
	[DataSourceProperty] public string RenewalText { get; set; }
	[DataSourceProperty] public bool CanRenew { get; set; }
	[DataSourceProperty] public bool CanAbolish { get; set; }
	[DataSourceProperty] public bool CanReReview { get; set; }
	[DataSourceProperty] public bool IsSelected { get => _isSelected; set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); } } }
	public void ExecuteSelect() => _onSelect?.Invoke(this);
}
