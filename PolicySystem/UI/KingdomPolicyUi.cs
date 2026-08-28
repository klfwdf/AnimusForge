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

public sealed class PolicyComposeTargetData
{
	public string TargetId = "";
	public string ScopeKind = "kingdom";
	public string DisplayText = "";
	public string HintText = "";
	public bool IsSelected;
}

public sealed class PolicyComposePrefillData
{
	public string PolicyName { get; set; } = string.Empty;
	public string PolicyContent { get; set; } = string.Empty;
	public string DurationText { get; set; } = string.Empty;
	public string PublishText { get; set; } = string.Empty;
	public bool RequireExplicitDuration { get; set; }
	public bool RequireExplicitTargetSelection { get; set; }
}

public sealed class CustomPolicyComposePopup
{
	private static CustomPolicyComposePopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyComposePopupVM _dataSource;

	private readonly Action<string, string, string, string, string> _onPublish;

	private readonly Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> _onAutoDraft;

	private readonly Action _onCancel;

	private bool _isClosed;

	private PendingCloseAction _pendingCloseAction = PendingCloseAction.None;

	private string _pendingPolicyName;

	private string _pendingPolicyContent;

	private string _pendingDurationText;

	private string _pendingDateText;

	private string _pendingTargetId;

	private enum PendingCloseAction
	{
		None,
		Publish,
		Cancel
	}

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	public static void ProcessDeferredCloseAction()
	{
		try
		{
			_activePopup?.ProcessPendingCloseAction();
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-popup-deferred-close-failed", ex.Message, ex.ToString());
		}
	}

	private CustomPolicyComposePopup(ScreenBase screen, string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, List<PolicyComposeTargetData> targets, Action<string, string, string, string, string> onPublish, Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> onAutoDraft, Action onCancel, PolicyComposePrefillData prefill)
	{
		_screen = screen;
		_onPublish = onPublish;
		_onAutoDraft = onAutoDraft;
		_onCancel = onCancel;
		_dataSource = new CustomPolicyComposePopupVM(titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, targets, HandlePublishRequested, HandleAutoDraftRequested, HandleCancelRequested, prefill);
		_layer = new GauntletLayer("CustomPolicyComposePopup", 4000, false);
	}

	public static bool Show(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, List<PolicyComposeTargetData> targets, Action<string, string, string, string, string> onPublish, Action<PlayerPolicyAutoDraftRequest, Action<PlayerPolicyAutoDraftResult>> onAutoDraft, Action onCancel, PolicyComposePrefillData prefill = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CustomPolicyComposePopup popup = new CustomPolicyComposePopup(topScreen, titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, targets, onPublish, onAutoDraft, onCancel, prefill);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyComposePopup", _dataSource);
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

	private void HandlePublishRequested(string policyName, string policyContent, string durationText, string dateText, string targetId)
	{
		RequestDeferredClose(PendingCloseAction.Publish, policyName ?? "", policyContent ?? "", durationText ?? "", dateText ?? "", targetId ?? "");
	}

	private void HandleCancelRequested()
	{
		RequestDeferredClose(PendingCloseAction.Cancel, null, null, null, null, null);
	}

	public static bool Show(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, List<PolicyComposeTargetData> targets, Action<string, string, string, string, string> onPublish, Action onCancel)
	{
		return Show(titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, targets, onPublish, null, onCancel);
	}

	private void HandleAutoDraftRequested(PlayerPolicyAutoDraftRequest request)
	{
		if (_isClosed)
		{
			return;
		}
		if (_onAutoDraft == null)
		{
			_dataSource.ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed("AI编写入口不可用。"));
			return;
		}
		try
		{
			_onAutoDraft(request, result =>
			{
				if (!_isClosed)
				{
					_dataSource.ApplyAutoDraftResult(request, result);
				}
			});
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-auto-draft-dispatch-failed", ex.Message, ex.ToString());
			_dataSource.ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed("AI编写遇到界面错误，请查看日志。"));
		}
	}

	private void RequestDeferredClose(PendingCloseAction action, string policyName, string policyContent, string durationText, string dateText, string targetId)
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = action;
		_pendingPolicyName = policyName;
		_pendingPolicyContent = policyContent;
		_pendingDurationText = durationText;
		_pendingDateText = dateText;
		_pendingTargetId = targetId;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingCloseAction;
		string policyName = _pendingPolicyName ?? "";
		string policyContent = _pendingPolicyContent ?? "";
		string durationText = _pendingDurationText ?? "";
		string dateText = _pendingDateText ?? "";
		string targetId = _pendingTargetId ?? "";
		_pendingCloseAction = PendingCloseAction.None;
		_pendingPolicyName = null;
		_pendingPolicyContent = null;
		_pendingDurationText = null;
		_pendingDateText = null;
		_pendingTargetId = null;
		Close(silent: true);
		if (action == PendingCloseAction.Publish)
		{
			_onPublish?.Invoke(policyName, policyContent, durationText, dateText, targetId);
		}
		else if (action == PendingCloseAction.Cancel)
		{
			_onCancel?.Invoke();
		}
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
				PolicySystemLog.Failure("UI", "compose-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class CustomPolicyComposePopupVM : ViewModel
{
	private readonly Action<string, string, string, string, string> _onPublish;

	private readonly Action<PlayerPolicyAutoDraftRequest> _onAutoDraft;

	private readonly Action _onCancel;

	private bool _externalCanPublish;

	private readonly bool _requireExplicitDuration;

	private readonly bool _requireExplicitTargetSelection;

	private string _titleText;

	private string _nameLabelText;

	private string _contentLabelText;

	private string _dateText;

	private string _policyName;

	private string _policyContent;

	private string _durationLabelText;

	private string _durationText;

	private string _publishText;

	private string _cancelText;

	private string _statusText;

	private string _readyStatusText;

	private bool _canPublish;

	private bool _canAutoDraft;

	private bool _isAutoDrafting;

	private string _autoDraftText;

	private string _selectedTargetId;

	private string _targetHintText;

	private MBBindingList<PolicyComposeTargetItemVM> _targetItems;

	public CustomPolicyComposePopupVM(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, List<PolicyComposeTargetData> targets, Action<string, string, string, string, string> onPublish, Action<PlayerPolicyAutoDraftRequest> onAutoDraft, Action onCancel, PolicyComposePrefillData prefill = null)
	{
		_onPublish = onPublish;
		_onAutoDraft = onAutoDraft;
		_onCancel = onCancel;
		_externalCanPublish = canPublish;
		_requireExplicitDuration = prefill?.RequireExplicitDuration == true;
		_requireExplicitTargetSelection = prefill?.RequireExplicitTargetSelection == true;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "发布王国政策" : titleText;
		NameLabelText = string.IsNullOrWhiteSpace(nameLabelText) ? "政策名" : nameLabelText;
		ContentLabelText = string.IsNullOrWhiteSpace(contentLabelText) ? "政策内容" : contentLabelText;
		DurationLabelText = "效果持续天数（留空为永久）";
		DateText = string.IsNullOrWhiteSpace(dateText) ? "未知日期" : dateText;
		PolicyName = prefill?.PolicyName ?? "";
		PolicyContent = prefill?.PolicyContent ?? "";
		DurationText = prefill?.DurationText ?? "";
		PublishText = string.IsNullOrWhiteSpace(prefill?.PublishText) ? "发布王国政策" : prefill.PublishText;
		AutoDraftText = "AI编写";
		CancelText = "取消";
		TargetItems = new MBBindingList<PolicyComposeTargetItemVM>();
		List<PolicyComposeTargetData> availableTargets = (targets ?? new List<PolicyComposeTargetData>()).Where(x => x != null).ToList();
		if (availableTargets.Count == 0 && !_requireExplicitTargetSelection)
		{
			availableTargets.Add(new PolicyComposeTargetData { TargetId = "", ScopeKind = "kingdom", DisplayText = "玩家王国", IsSelected = true });
		}
		for (int i = 0; i < availableTargets.Count; i++)
		{
			PolicyComposeTargetData target = availableTargets[i];
			TargetItems.Add(new PolicyComposeTargetItemVM(target, SelectTarget));
		}
		PolicyComposeTargetItemVM selected = TargetItems.FirstOrDefault(x => x.IsSelected);
		if (selected != null || !_requireExplicitTargetSelection)
		{
			SelectTarget(selected ?? TargetItems.FirstOrDefault());
		}
		_readyStatusText = string.IsNullOrWhiteSpace(blockReason) ? "填写政策名和政策内容后即可发布。" : blockReason;
		StatusText = canPublish ? _readyStatusText : (string.IsNullOrWhiteSpace(blockReason) ? "当前不能发布政策。" : blockReason);
		RefreshCanPublish();
	}

	public CustomPolicyComposePopupVM(string titleText, string nameLabelText, string contentLabelText, string dateText, bool canPublish, string blockReason, List<PolicyComposeTargetData> targets, Action<string, string, string, string, string> onPublish, Action onCancel)
		: this(titleText, nameLabelText, contentLabelText, dateText, canPublish, blockReason, targets, onPublish, null, onCancel)
	{
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string NameLabelText
	{
		get => _nameLabelText;
		set
		{
			if (value != _nameLabelText)
			{
				_nameLabelText = value;
				OnPropertyChangedWithValue(value, nameof(NameLabelText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentLabelText
	{
		get => _contentLabelText;
		set
		{
			if (value != _contentLabelText)
			{
				_contentLabelText = value;
				OnPropertyChangedWithValue(value, nameof(ContentLabelText));
			}
		}
	}

	[DataSourceProperty]
	public string DateText
	{
		get => _dateText;
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, nameof(DateText));
			}
		}
	}

	[DataSourceProperty]
	public string PolicyName
	{
		get => _policyName;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, AnimusForgeTextInputSanitizer.MaxPolicyNameChars);
			if (text != _policyName)
			{
				_policyName = text;
				OnPropertyChangedWithValue(_policyName, nameof(PolicyName));
				RefreshCanPublish();
			}
		}
	}

	[DataSourceProperty]
	public string PolicyContent
	{
		get => _policyContent;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxPolicyContentChars);
			if (text != _policyContent)
			{
				_policyContent = text;
				OnPropertyChangedWithValue(_policyContent, nameof(PolicyContent));
				RefreshCanPublish();
			}
		}
	}

	[DataSourceProperty]
	public string DurationText
	{
		get => _durationText;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, 16);
			if (text != _durationText)
			{
				_durationText = text;
				OnPropertyChangedWithValue(_durationText, nameof(DurationText));
				RefreshCanPublish();
			}
		}
	}

	[DataSourceProperty]
	public string DurationLabelText
	{
		get => _durationLabelText;
		set
		{
			if (value != _durationLabelText)
			{
				_durationLabelText = value;
				OnPropertyChangedWithValue(value, nameof(DurationLabelText));
			}
		}
	}

	[DataSourceProperty]
	public string PublishText
	{
		get => _publishText;
		set
		{
			if (value != _publishText)
			{
				_publishText = value;
				OnPropertyChangedWithValue(value, nameof(PublishText));
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get => _cancelText;
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, nameof(CancelText));
			}
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		set
		{
			if (value != _statusText)
			{
				_statusText = value;
				OnPropertyChangedWithValue(value, nameof(StatusText));
			}
		}
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		set
		{
			if (value != _canPublish)
			{
				_canPublish = value;
				OnPropertyChangedWithValue(value, nameof(CanPublish));
			}
		}
	}

	[DataSourceProperty]
	public bool CanAutoDraft
	{
		get => _canAutoDraft;
		set
		{
			if (value != _canAutoDraft)
			{
				_canAutoDraft = value;
				OnPropertyChangedWithValue(value, nameof(CanAutoDraft));
			}
		}
	}

	[DataSourceProperty]
	public string AutoDraftText
	{
		get => _autoDraftText;
		set
		{
			if (value != _autoDraftText)
			{
				_autoDraftText = value;
				OnPropertyChangedWithValue(value, nameof(AutoDraftText));
			}
		}
	}

	[DataSourceProperty]
	public string SelectedTargetId
	{
		get => _selectedTargetId;
		set
		{
			if (value != _selectedTargetId)
			{
				_selectedTargetId = value;
				OnPropertyChangedWithValue(value, nameof(SelectedTargetId));
			}
		}
	}

	[DataSourceProperty]
	public string TargetHintText
	{
		get => _targetHintText;
		set
		{
			if (value != _targetHintText)
			{
				_targetHintText = value;
				OnPropertyChangedWithValue(value, nameof(TargetHintText));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PolicyComposeTargetItemVM> TargetItems
	{
		get => _targetItems;
		set
		{
			if (value != _targetItems)
			{
				_targetItems = value;
				OnPropertyChangedWithValue(value, nameof(TargetItems));
			}
		}
	}

	public void ExecutePublish()
	{
		RefreshCanPublish();
		if (!CanPublish)
		{
			if (string.IsNullOrWhiteSpace(StatusText))
			{
				StatusText = "当前不能发布政策。";
			}
			return;
		}
		_onPublish?.Invoke(PolicyName ?? "", PolicyContent ?? "", DurationText ?? "", DateText ?? "", SelectedTargetId ?? "");
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void ExecuteAutoDraft()
	{
		RefreshCanPublish();
		if (!CanAutoDraft)
		{
			if (!PlayerPolicyAutoDraftInputContract.HasInput(PolicyName, PolicyContent))
			{
				StatusText = "请先填写政策标题或政策内容。";
			}
			return;
		}
		PlayerPolicyAutoDraftRequest request = new PlayerPolicyAutoDraftRequest
		{
			PlayerDescription = PolicyContent ?? "",
			ExistingPolicyName = PolicyName ?? "",
			DurationText = DurationText ?? "",
			ScopeKind = TargetItems?.FirstOrDefault(item => item.IsSelected)?.ScopeKind ?? "kingdom",
			TargetKingdomId = SelectedTargetId ?? "",
			TargetKingdomName = TargetItems?.FirstOrDefault(item => item.IsSelected)?.DisplayText ?? "",
			DateText = DateText ?? ""
		};
		_isAutoDrafting = true;
		AutoDraftText = "AI编写中…";
		StatusText = "AI正在按照玩家可编辑提示词扩写当前内容；不会读取政策通用提示词、世界上下文或历史政策，也不会自动发布。";
		RefreshCanPublish();
		try
		{
			_onAutoDraft?.Invoke(request);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "compose-auto-draft-start-failed", ex.Message, ex.ToString());
			ApplyAutoDraftResult(request, PlayerPolicyAutoDraftResult.Failed("AI编写遇到界面错误，请查看日志。"));
		}
	}

	internal void ApplyAutoDraftResult(PlayerPolicyAutoDraftRequest request, PlayerPolicyAutoDraftResult result)
	{
		_isAutoDrafting = false;
		AutoDraftText = "AI编写";
		bool formUnchanged = request != null
			&& string.Equals(PolicyName ?? "", request.ExistingPolicyName ?? "", StringComparison.Ordinal)
			&& string.Equals(PolicyContent ?? "", request.PlayerDescription ?? "", StringComparison.Ordinal)
			&& string.Equals(DurationText ?? "", request.DurationText ?? "", StringComparison.Ordinal)
			&& string.Equals(SelectedTargetId ?? "", request.TargetKingdomId ?? "", StringComparison.OrdinalIgnoreCase);
		if (!formUnchanged)
		{
			RefreshCanPublish();
			StatusText = "AI编写期间表单已改变，已丢弃过期结果。";
			return;
		}
		if (result?.Success != true)
		{
			RefreshCanPublish();
			StatusText = string.IsNullOrWhiteSpace(result?.Error) ? "AI编写失败，原输入已保留。" : result.Error;
			return;
		}
		PolicyName = result.PolicyName ?? "";
		PolicyContent = result.PolicyContent ?? "";
		RefreshCanPublish();
		StatusText = "AI编写内容已回填；请检查并修改后再发布。";
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}

	private void SelectTarget(PolicyComposeTargetItemVM selected)
	{
		if (selected == null)
		{
			return;
		}
		foreach (PolicyComposeTargetItemVM item in TargetItems ?? new MBBindingList<PolicyComposeTargetItemVM>())
		{
			item.IsSelected = ReferenceEquals(item, selected);
		}
		SelectedTargetId = selected.TargetId;
		TargetHintText = selected.HintText;
		bool isVassalPolicy = string.Equals(selected.ScopeKind, "vassal", StringComparison.OrdinalIgnoreCase);
		PublishText = isVassalPolicy ? "发布附庸国政策" : "发布王国政策";
		DurationLabelText = isVassalPolicy
			? "效果持续天数（留空为永久）"
			: "效果持续天数（留空为永久）";
		RefreshCanPublish();
	}

	private void RefreshCanPublish()
	{
		bool hasName = !string.IsNullOrWhiteSpace(PolicyName);
		bool hasContent = !string.IsNullOrWhiteSpace(PolicyContent);
		bool hasTarget = !_requireExplicitTargetSelection || !string.IsNullOrWhiteSpace(SelectedTargetId);
		bool durationValid = (!_requireExplicitDuration || !string.IsNullOrWhiteSpace(DurationText))
			&& (string.IsNullOrWhiteSpace(DurationText)
				|| (int.TryParse(DurationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int durationDays) && durationDays > 0));
		CanPublish = !_isAutoDrafting && _externalCanPublish && hasName && hasContent && hasTarget && durationValid;
		CanAutoDraft = !_isAutoDrafting && _externalCanPublish
			&& PlayerPolicyAutoDraftInputContract.HasInput(PolicyName, PolicyContent)
			&& hasTarget && durationValid;
		if (_externalCanPublish)
		{
			if (!hasName && !hasContent)
			{
				StatusText = "请填写政策标题或政策内容后使用AI编写。";
			}
			else if (!hasName)
			{
				StatusText = "可点击AI编写补全政策标题和正文。";
			}
			else if (!hasContent)
			{
				StatusText = "可点击AI编写，根据政策标题补全正文。";
			}
			else if (!hasTarget)
			{
				StatusText = "原目标当前无效，请明确重新选择一个当前可用目标。";
			}
			else if (!durationValid)
			{
				StatusText = _requireExplicitDuration && string.IsNullOrWhiteSpace(DurationText)
					? "旧记录无法确认原持续时间，请明确填写正整数天数。"
					: "持续天数必须留空或填写正整数。";
			}
			else
			{
				StatusText = string.IsNullOrWhiteSpace(_readyStatusText) ? "点击发布后将等待智能评议；成功落地时扣除已配置成本。" : _readyStatusText;
			}
		}
	}
}

public sealed class PolicyComposeTargetItemVM : ViewModel
{
	private readonly Action<PolicyComposeTargetItemVM> _select;
	private bool _isSelected;
	private string _selectionText;

	public PolicyComposeTargetItemVM(PolicyComposeTargetData data, Action<PolicyComposeTargetItemVM> select)
	{
		PolicyComposeTargetData source = data ?? new PolicyComposeTargetData();
		_select = select;
		TargetId = source.TargetId ?? "";
		ScopeKind = source.ScopeKind ?? "kingdom";
		DisplayText = string.IsNullOrWhiteSpace(source.DisplayText) ? "未知国家" : source.DisplayText.Trim();
		HintText = source.HintText ?? "";
		SelectionText = DisplayText;
		IsSelected = source.IsSelected;
	}

	public string TargetId { get; }
	public string ScopeKind { get; }
	[DataSourceProperty] public string DisplayText { get; }
	[DataSourceProperty] public string HintText { get; }
	[DataSourceProperty] public string SelectionText { get => _selectionText; set { if (value != _selectionText) { _selectionText = value; OnPropertyChangedWithValue(value, nameof(SelectionText)); } } }
	[DataSourceProperty] public bool IsSelected { get => _isSelected; set { if (value != _isSelected) { _isSelected = value; SelectionText = value ? "● " + DisplayText : DisplayText; OnPropertyChangedWithValue(value, nameof(IsSelected)); } } }
	public void ExecuteSelect() => _select?.Invoke(this);
}

public sealed class PolicyHistoryData
{
	public string TitleText { get; set; } = "政策记录";

	public string SubtitleText { get; set; } = "";

	public string EmptyStateText { get; set; } = "尚无成功落地的政策记录。";

	public string CloseText { get; set; } = "返回政策管理";

	public List<PolicyHistoryRecordData> Records { get; set; } = new List<PolicyHistoryRecordData>();
}

public sealed class PolicyHistoryRecordData
{
	public string RecordId { get; set; }

	public bool CanReReview { get; set; }

	public string ReReviewText { get; set; } = "重新评议";

	public string DateText { get; set; }

	public string PolicyNameText { get; set; }

	public string CostText { get; set; }

	public string ContentSectionTitleText { get; set; }

	public string ContentSummaryText { get; set; }

	public string FeedbackSectionTitleText { get; set; }

	public string FeedbackSummaryText { get; set; }

	public string ImpactSectionTitleText { get; set; }

	public string ImpactSummaryText { get; set; }
}

public sealed class CustomPolicyResultPopup
{
	private static CustomPolicyResultPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyResultPopupVM _dataSource;

	private readonly Action _onClose;

	private readonly Action _onRetry;

	private bool _isClosed;

	private CustomPolicyResultPopup(
		ScreenBase screen,
		string titleText,
		string bodyText,
		string closeText,
		string retryText,
		bool showRetryButton,
		bool showDismissButton,
		Action onClose,
		Action onRetry)
	{
		_screen = screen;
		_onClose = onClose;
		_onRetry = onRetry;
		_dataSource = new CustomPolicyResultPopupVM(
			titleText,
			bodyText,
			closeText,
			retryText,
			showRetryButton,
			showDismissButton,
			HandleCloseRequested,
			HandleRetryRequested,
			HandleDismissRequested);
		_layer = new GauntletLayer("CustomPolicyResultPopup", 4150, false);
	}

	public static bool Show(string titleText, string bodyText, string closeText, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.CloseForReplacement();
			CustomPolicyResultPopup popup = new CustomPolicyResultPopup(
				topScreen,
				titleText,
				bodyText,
				closeText,
				string.Empty,
				showRetryButton: false,
				showDismissButton: false,
				onClose: onClose,
				onRetry: null);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	internal static bool ShowRetry(string titleText, string bodyText, string retryText, Action onRetry)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.CloseForReplacement();
			CustomPolicyResultPopup popup = new CustomPolicyResultPopup(
				topScreen,
				titleText,
				bodyText,
				string.Empty,
				retryText,
				showRetryButton: true,
				showDismissButton: true,
				onClose: null,
				onRetry: onRetry);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-retry-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyResultPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-hotkey-register-failed", ex.Message, ex.ToString());
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandleCloseRequested()
	{
		Close(silent: true, invokeCloseAction: true);
	}

	private void HandleRetryRequested()
	{
		if (_isClosed)
		{
			return;
		}
		Action retry = _onRetry;
		Close(silent: true);
		try
		{
			retry?.Invoke();
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-retry-failed", ex.Message, ex.ToString());
		}
	}

	private void HandleDismissRequested()
	{
		Close(silent: true);
	}

	private void CloseForReplacement()
	{
		Close(silent: true, invokeCloseAction: _onRetry == null);
	}

	private void Close(bool silent, bool invokeCloseAction = false)
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
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-focus-reset-failed", ex.Message, ex.ToString());
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				PolicySystemLog.Failure("UI", "result-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
		if (!invokeCloseAction)
		{
			return;
		}
		try
		{
			_onClose?.Invoke();
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "result-popup-after-close-failed", ex.Message, ex.ToString());
		}
	}
}

public sealed class CustomPolicyResultPopupVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action _onRetry;

	private readonly Action _onDismiss;

	private string _titleText;

	private string _bodyText;

	private string _closeText;

	private string _retryText;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get => _bodyText;
		set
		{
			if (value != _bodyText)
			{
				_bodyText = value;
				OnPropertyChangedWithValue(value, nameof(BodyText));
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get => _closeText;
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, nameof(CloseText));
			}
		}
	}

	[DataSourceProperty]
	public string RetryText
	{
		get => _retryText;
		set
		{
			if (value != _retryText)
			{
				_retryText = value;
				OnPropertyChangedWithValue(value, nameof(RetryText));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowCloseButton { get; }

	[DataSourceProperty]
	public bool ShowRetryButton { get; }

	[DataSourceProperty]
	public bool ShowDismissButton { get; }

	public CustomPolicyResultPopupVM(string titleText, string bodyText, string closeText, Action onClose)
		: this(
			titleText,
			bodyText,
			closeText,
			string.Empty,
			showRetryButton: false,
			showDismissButton: false,
			onClose: onClose,
			onRetry: null,
			onDismiss: null)
	{
	}

	internal CustomPolicyResultPopupVM(
		string titleText,
		string bodyText,
		string closeText,
		string retryText,
		bool showRetryButton,
		bool showDismissButton,
		Action onClose,
		Action onRetry,
		Action onDismiss)
	{
		_onClose = onClose;
		_onRetry = onRetry;
		_onDismiss = onDismiss;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "政策已经发布" : titleText.Trim();
		BodyText = (bodyText ?? "").Trim();
		CloseText = string.IsNullOrWhiteSpace(closeText) ? "知道了" : closeText.Trim();
		RetryText = string.IsNullOrWhiteSpace(retryText) ? "手动重试" : retryText.Trim();
		ShowRetryButton = showRetryButton;
		ShowCloseButton = !showRetryButton;
		ShowDismissButton = showDismissButton;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public void ExecuteRetry()
	{
		_onRetry?.Invoke();
	}

	public void ExecuteDismiss()
	{
		_onDismiss?.Invoke();
	}
}

public sealed class CustomPolicyHistoryPopup
{
	private static CustomPolicyHistoryPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly CustomPolicyHistoryPopupVM _dataSource;

	private readonly Action _onClose;

	private readonly Action<string> _onReReview;

	private bool _isClosed;

	private CustomPolicyHistoryPopup(ScreenBase screen, PolicyHistoryData data, Action<string> onReReview, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_onReReview = onReReview;
		_dataSource = new CustomPolicyHistoryPopupVM(data, HandleReReviewRequested, HandleCloseRequested);
		_layer = new GauntletLayer("CustomPolicyHistoryPopup", 4100, false);
	}

	public static bool Show(PolicyHistoryData data, Action<string> onReReview, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CustomPolicyHistoryPopup popup = new CustomPolicyHistoryPopup(topScreen, data ?? new PolicyHistoryData(), onReReview, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "history-popup-open-failed", ex.Message, ex.ToString());
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static bool Show(PolicyHistoryData data, Action onClose = null)
	{
		return Show(data, null, onClose);
	}

	private void Open()
	{
		_layer.LoadMovie("CustomPolicyHistoryPopup", _dataSource);
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

	private void HandleCloseRequested()
	{
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void HandleReReviewRequested(string recordId)
	{
		Close(silent: true);
		_onReReview?.Invoke(recordId ?? string.Empty);
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
				PolicySystemLog.Failure("UI", "history-popup-close-failed", ex.Message, ex.ToString());
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class CustomPolicyHistoryPopupVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action<string> _onReReview;

	private string _titleText;

	private string _subtitleText;

	private string _emptyStateText;

	private string _closeText;

	private bool _hasRecords;

	private bool _showEmptyState;

	private MBBindingList<CustomPolicyHistoryRecordItemVM> _recordItems;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string EmptyStateText
	{
		get => _emptyStateText;
		set
		{
			if (value != _emptyStateText)
			{
				_emptyStateText = value;
				OnPropertyChangedWithValue(value, nameof(EmptyStateText));
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get => _closeText;
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, nameof(CloseText));
			}
		}
	}

	[DataSourceProperty]
	public bool HasRecords
	{
		get => _hasRecords;
		set
		{
			if (value != _hasRecords)
			{
				_hasRecords = value;
				OnPropertyChangedWithValue(value, nameof(HasRecords));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowEmptyState
	{
		get => _showEmptyState;
		set
		{
			if (value != _showEmptyState)
			{
				_showEmptyState = value;
				OnPropertyChangedWithValue(value, nameof(ShowEmptyState));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CustomPolicyHistoryRecordItemVM> RecordItems
	{
		get => _recordItems;
		set
		{
			if (value != _recordItems)
			{
				_recordItems = value;
				OnPropertyChangedWithValue(value, nameof(RecordItems));
			}
		}
	}

	public CustomPolicyHistoryPopupVM(PolicyHistoryData data, Action<string> onReReview, Action onClose)
	{
		_onReReview = onReReview;
		_onClose = onClose;
		PolicyHistoryData source = data ?? new PolicyHistoryData();
		TitleText = string.IsNullOrWhiteSpace(source.TitleText) ? "政策记录" : source.TitleText.Trim();
		SubtitleText = (source.SubtitleText ?? "").Trim();
		EmptyStateText = string.IsNullOrWhiteSpace(source.EmptyStateText) ? "尚无成功落地的政策记录。" : source.EmptyStateText.Trim();
		CloseText = string.IsNullOrWhiteSpace(source.CloseText) ? "返回政策管理" : source.CloseText.Trim();
		RecordItems = new MBBindingList<CustomPolicyHistoryRecordItemVM>();
		if (source.Records != null)
		{
			foreach (PolicyHistoryRecordData record in source.Records)
			{
				if (record != null)
				{
					RecordItems.Add(new CustomPolicyHistoryRecordItemVM(record, _onReReview));
				}
			}
		}
		HasRecords = RecordItems.Count > 0;
		ShowEmptyState = !HasRecords;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}

public sealed class CustomPolicyHistoryRecordItemVM : ViewModel
{
	private readonly Action<string> _onReReview;

	[DataSourceProperty] public string RecordId { get; set; }

	[DataSourceProperty] public bool CanReReview { get; set; }

	[DataSourceProperty] public string ReReviewText { get; set; }

	private string _dateText;

	private string _policyNameText;

	private string _costText;

	private string _contentSectionTitleText;

	private string _contentSummaryText;

	private string _feedbackSectionTitleText;

	private string _feedbackSummaryText;

	private string _impactSectionTitleText;

	private string _impactSummaryText;

	[DataSourceProperty]
	public string DateText
	{
		get => _dateText;
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, nameof(DateText));
			}
		}
	}

	[DataSourceProperty]
	public string PolicyNameText
	{
		get => _policyNameText;
		set
		{
			if (value != _policyNameText)
			{
				_policyNameText = value;
				OnPropertyChangedWithValue(value, nameof(PolicyNameText));
			}
		}
	}

	[DataSourceProperty]
	public string CostText
	{
		get => _costText;
		set
		{
			if (value != _costText)
			{
				_costText = value;
				OnPropertyChangedWithValue(value, nameof(CostText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentSectionTitleText
	{
		get => _contentSectionTitleText;
		set
		{
			if (value != _contentSectionTitleText)
			{
				_contentSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(ContentSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string ContentSummaryText
	{
		get => _contentSummaryText;
		set
		{
			if (value != _contentSummaryText)
			{
				_contentSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(ContentSummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string FeedbackSectionTitleText
	{
		get => _feedbackSectionTitleText;
		set
		{
			if (value != _feedbackSectionTitleText)
			{
				_feedbackSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(FeedbackSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string FeedbackSummaryText
	{
		get => _feedbackSummaryText;
		set
		{
			if (value != _feedbackSummaryText)
			{
				_feedbackSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(FeedbackSummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string ImpactSectionTitleText
	{
		get => _impactSectionTitleText;
		set
		{
			if (value != _impactSectionTitleText)
			{
				_impactSectionTitleText = value;
				OnPropertyChangedWithValue(value, nameof(ImpactSectionTitleText));
			}
		}
	}

	[DataSourceProperty]
	public string ImpactSummaryText
	{
		get => _impactSummaryText;
		set
		{
			if (value != _impactSummaryText)
			{
				_impactSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(ImpactSummaryText));
			}
		}
	}

	public CustomPolicyHistoryRecordItemVM(PolicyHistoryRecordData record, Action<string> onReReview)
	{
		_onReReview = onReReview;
		RecordId = record?.RecordId ?? string.Empty;
		CanReReview = record?.CanReReview == true;
		ReReviewText = string.IsNullOrWhiteSpace(record?.ReReviewText) ? "重新评议" : record.ReReviewText.Trim();
		DateText = (record?.DateText ?? "未知日期").Trim();
		PolicyNameText = (record?.PolicyNameText ?? "未命名政策").Trim();
		CostText = (record?.CostText ?? "").Trim();
		ContentSectionTitleText = string.IsNullOrWhiteSpace(record?.ContentSectionTitleText) ? "【政策内容】" : record.ContentSectionTitleText.Trim();
		ContentSummaryText = (record?.ContentSummaryText ?? "").Trim();
		FeedbackSectionTitleText = string.IsNullOrWhiteSpace(record?.FeedbackSectionTitleText) ? "【民众反馈】" : record.FeedbackSectionTitleText.Trim();
		FeedbackSummaryText = (record?.FeedbackSummaryText ?? "").Trim();
		ImpactSectionTitleText = string.IsNullOrWhiteSpace(record?.ImpactSectionTitleText) ? "【每日影响】" : record.ImpactSectionTitleText.Trim();
		ImpactSummaryText = (record?.ImpactSummaryText ?? "").Trim();
	}

	public void ExecuteReReview()
	{
		if (CanReReview)
		{
			_onReReview?.Invoke(RecordId ?? string.Empty);
		}
	}
}
