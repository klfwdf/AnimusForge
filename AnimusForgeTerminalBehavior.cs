using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public class AnimusForgeTerminalBehavior : CampaignBehaviorBase
{
	private sealed class PendingPlayerRpItemIntroduction
	{
		public string GeneratedStringId;

		public string DisplayName;

		public string CompletionMessage;

		public long RuntimeGeneration;

		public long EarliestEngineTick;

		public long NextOpenAttemptEngineTick;
	}

	private const int HintIntervalDays = 2;

	private const float OpenCooldownSeconds = 0.35f;

	private const float HotkeyBlockLogCooldownSeconds = 5f;

	private const float TerminalKeyRefreshIntervalSeconds = 1f;

	private static readonly Stopwatch TerminalClock = Stopwatch.StartNew();

	private static readonly string[] PlayerRpForgeInquiryGuardTokens =
	{
		"失败",
		"错误",
		"问题",
		"error",
		"problem"
	};

	private int _lastTerminalHintDay = -999999;

	private bool _terminalUiActive;

	private float _lastOpenRealTime = -999f;

	private float _lastHotkeyBlockLogRealTime = -999f;

	private bool _wasTerminalKeyDown;

	private InputKey _cachedTerminalKey = InputKey.U;

	private string _cachedTerminalKeyRaw = "";

	private float _nextTerminalKeyRefreshRealTime = -999f;

	private PendingPlayerRpItemIntroduction _pendingPlayerRpItemIntroduction;

	private long _engineTickSequence;

	public static AnimusForgeTerminalBehavior Instance { get; private set; }

	public AnimusForgeTerminalBehavior()
	{
		Instance?.RetirePlayerRpForgeUi();
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_af_terminal_last_hint_day_v1", ref _lastTerminalHintDay);
	}

	public void OnEngineTick()
	{
		_engineTickSequence++;
		ProcessPendingPlayerRpItemIntroduction();
		if (MilitaryExerciseBehavior.NeedsEngineTick())
		{
			using (PerfProbe.Scope("SubModule.AnimusForgeTerminalBehavior.MilitaryExerciseTick"))
			{
				MilitaryExerciseBehavior.OnEngineTick();
			}
		}
		if (TroopInspectionBehavior.NeedsEngineTick())
		{
			using (PerfProbe.Scope("SubModule.AnimusForgeTerminalBehavior.TroopInspectionTick"))
			{
				TroopInspectionBehavior.OnEngineTick();
			}
		}
		InputKey configuredTerminalKey = GetCachedConfiguredTerminalKey();
		bool flag = false;
		using (PerfProbe.Scope("SubModule.AnimusForgeTerminalBehavior.TerminalHotkeyPoll"))
		{
			try
			{
				flag = Input.IsKeyDown(configuredTerminalKey);
			}
			catch
			{
				flag = false;
			}
		}
		if (!flag)
		{
			_wasTerminalKeyDown = false;
			return;
		}
		if (CourierLetterInputPopup.IsOpen || CourierLetterReplyPopup.IsOpen || WorldDiplomacyComposePopup.IsOpen)
		{
			LogHotkeyBlocked("modal_text_input_ui", configuredTerminalKey);
			_wasTerminalKeyDown = true;
			return;
		}
		if (HotkeyInputGuard.IsTextInputFocused())
		{
			LogHotkeyBlocked("text_input_or_inquiry", configuredTerminalKey);
			_wasTerminalKeyDown = true;
			return;
		}
		if (_wasTerminalKeyDown || _terminalUiActive)
		{
			return;
		}
		if (!IsCampaignMapLikeStateActive())
		{
			LogHotkeyBlocked("not_campaign_map", configuredTerminalKey);
			return;
		}
		_wasTerminalKeyDown = true;
		float num = GetRealTimeSeconds();
		if (num - _lastOpenRealTime < OpenCooldownSeconds)
		{
			return;
		}
		_lastOpenRealTime = num;
		OpenRootMenu();
	}

	private float GetRealTimeSeconds()
	{
		return (float)TerminalClock.Elapsed.TotalSeconds;
	}

	private InputKey GetCachedConfiguredTerminalKey()
	{
		float realTimeSeconds = GetRealTimeSeconds();
		if (realTimeSeconds < _nextTerminalKeyRefreshRealTime)
		{
			return _cachedTerminalKey;
		}
		_nextTerminalKeyRefreshRealTime = realTimeSeconds + TerminalKeyRefreshIntervalSeconds;
		try
		{
			string raw = DuelSettings.GetSettings()?.TerminalKey ?? "";
			if (!string.Equals(raw ?? "", _cachedTerminalKeyRaw ?? "", StringComparison.Ordinal))
			{
				_cachedTerminalKeyRaw = raw ?? "";
				_cachedTerminalKey = ParseTerminalKey(_cachedTerminalKeyRaw);
			}
		}
		catch
		{
			_cachedTerminalKeyRaw = "";
			_cachedTerminalKey = InputKey.U;
		}
		return _cachedTerminalKey;
	}

	private void LogHotkeyBlocked(string reason, InputKey configuredTerminalKey)
	{
		try
		{
			float realTimeSeconds = GetRealTimeSeconds();
			if (realTimeSeconds - _lastHotkeyBlockLogRealTime < HotkeyBlockLogCooldownSeconds)
			{
				return;
			}
			_lastHotkeyBlockLogRealTime = realTimeSeconds;
			ScreenBase topScreen = ScreenManager.TopScreen;
			string topScreenName = topScreen?.GetType().FullName ?? "";
			string activeStateName = Game.Current?.GameStateManager?.ActiveState?.GetType().FullName ?? "";
			bool conversationInProgress = false;
			try
			{
				conversationInProgress = Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
			}
			catch
			{
			}
			bool inquiryActive = false;
			try
			{
				inquiryActive = InformationManager.IsAnyInquiryActive();
			}
			catch
			{
			}
			bool focusedOnInput = false;
			try
			{
				focusedOnInput = ScreenManager.FocusedLayer?.IsFocusedOnInput() == true;
			}
			catch
			{
			}
			string rawKey = "";
			try
			{
				rawKey = DuelSettings.GetSettings()?.TerminalKey ?? "";
			}
			catch
			{
			}
			Logger.Log("Terminal", "[INFO] hotkey blocked reason=" + reason + " configuredKey=" + configuredTerminalKey + " rawKey=\"" + rawKey + "\" topScreen=\"" + topScreenName + "\" activeState=\"" + activeStateName + "\" mission=" + (Mission.Current != null) + " conversation=" + conversationInProgress + " inquiry=" + inquiryActive + " focusedOnInput=" + focusedOnInput);
		}
		catch
		{
		}
	}

	private void OnDailyTick()
	{
		try
		{
			int campaignDayIndex = GetCampaignDayIndex();
			if (campaignDayIndex - _lastTerminalHintDay < HintIntervalDays)
			{
				return;
			}
			_lastTerminalHintDay = campaignDayIndex;
			InformationManager.DisplayMessage(new InformationMessage($"按{GetConfiguredTerminalKeyLabel()}键打开AnimusForge终端。"));
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[WARN] terminal hint failed: " + ex.Message);
		}
	}

	private static int GetCampaignDayIndex()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsCampaignMapLikeStateActive()
	{
		try
		{
			if (Campaign.Current == null || Mission.Current != null)
			{
				return false;
			}
			if (Campaign.Current.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				return false;
			}
			ScreenBase topScreen = ScreenManager.TopScreen;
			string text = topScreen?.GetType().Name ?? "";
			if (text.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text2 = Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "";
			if (text2.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return topScreen == null && Mission.Current == null;
		}
		catch
		{
			return false;
		}
	}

	private void OpenRootMenu()
	{
		_terminalUiActive = true;
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("trust_query", "信任度查询", null, isEnabled: true, ""),
			new InquiryElement("weekly_reports", "查看周报", null, isEnabled: true, ""),
			new InquiryElement("custom_policy_management", "王国公告", null, isEnabled: true, "撰写自定义政策或外交宣言，并统一查看各国王国公告。"),
			new InquiryElement("vassalage_management", "臣属国管理", null, isEnabled: true, "只查看已有臣属国；解约、改约、吞并请通过 LLM 对话推进。"),
			new InquiryElement("player_persona", "修改玩家外貌与背景", null, isEnabled: true, ""),
			new InquiryElement("player_rp_forge", "制造RP物品", null, isEnabled: true, "投入第纳尔，制造玩家自己的普通RP物品或武器装备。"),
			new InquiryElement("settlement_entry_troops", "进城随行配置", null, isEnabled: true, "配置 SETS 进城/城堡/村庄自动带入的同伴和士兵。"),
			new InquiryElement("noble_prisoner_escort", "贵族俘虏随行配置", null, isEnabled: true, "分别配置攻城处置、普通定居点、领主大厅和野外会面中带入的英雄俘虏。"),
			new InquiryElement("troop_inspection", "检阅士兵", null, isEnabled: true, ""),
			new InquiryElement("military_exercise", "军事演习", null, isEnabled: true, ""),
			new InquiryElement("api_onboarding", "重新进行API首次引导", null, isEnabled: true, "只重新选择和测试 API 配置，不进入数据库导入或首次使用流程。"),
			// 错误不再自动弹窗；此入口保留玩家按需调用 AI 分析最近错误的原有能力。
			new InquiryElement("analyze_latest_error", "分析最近错误", null, isEnabled: true, "使用前处理 API 分析本局最近一次 AnimusForge 错误。"),
			new InquiryElement("tag_catalog", "标签列表", null, isEnabled: true, "查看从当前 AnimusForge 模块文件和程序集里提取到的正文/后处理标签。")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("你现在想做什么？", "请选择终端功能：", list, isExitShown: true, 1, 1, "确定", "关闭", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				CloseTerminal();
				return;
			}
			string text = selected[0].Identifier as string;
			if (string.Equals(text, "trust_query", StringComparison.Ordinal))
			{
				OpenTrustQueryMenu(CloseTerminal);
			}
			else if (string.Equals(text, "weekly_reports", StringComparison.Ordinal))
			{
				OpenWeeklyReportBrowser();
			}
			else if (string.Equals(text, "custom_policy_management", StringComparison.Ordinal))
			{
				OpenCustomPolicyManagementView();
			}
			else if (string.Equals(text, "vassalage_management", StringComparison.Ordinal))
			{
				OpenVassalageManagementView();
			}
			else if (string.Equals(text, "player_persona", StringComparison.Ordinal))
			{
				OpenPlayerPersonaEditor();
			}
			else if (string.Equals(text, "player_rp_forge", StringComparison.Ordinal))
			{
				OpenPlayerRpCrafterSelection();
			}
			else if (string.Equals(text, "settlement_entry_troops", StringComparison.Ordinal))
			{
				CloseTerminal();
				SettlementEntryTroopSelectionBehavior.OpenConfigFromTerminal();
			}
			else if (string.Equals(text, "noble_prisoner_escort", StringComparison.Ordinal))
			{
				CloseTerminal();
				NoblePrisonerEscortBehavior.OpenConfigFromTerminal();
			}
			else if (string.Equals(text, "troop_inspection", StringComparison.Ordinal))
			{
				CloseTerminal();
				TroopInspectionBehavior.OpenInspectionFromTerminal();
			}
			else if (string.Equals(text, "military_exercise", StringComparison.Ordinal))
			{
				CloseTerminal();
				MilitaryExerciseBehavior.OpenExerciseFromTerminal();
			}
			else if (string.Equals(text, "api_onboarding", StringComparison.Ordinal))
			{
				CloseTerminal();
				if (!ModOnboardingBehavior.OpenApiSetupOnlyFlow())
				{
					InformationManager.DisplayMessage(new InformationMessage("无法打开 API 首次引导。"));
				}
			}
			else if (string.Equals(text, "analyze_latest_error", StringComparison.Ordinal))
			{
				// 先关闭选择窗口，避免玩家主动查看的分析结果与终端菜单叠加。
				CloseTerminal();
				AiErrorAnalysisInquiry.AnalyzeLatestFailure();
			}
			else if (string.Equals(text, "tag_catalog", StringComparison.Ordinal))
			{
				OpenTagCatalogBrowser(null, forceRefresh: true);
			}
			else
			{
				CloseTerminal();
			}
		}, delegate
		{
			CloseTerminal();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenCustomPolicyManagementView()
	{
		_terminalUiActive = true;
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("compose", "撰写王国公告", null, isEnabled: true, "选择撰写自定义政策或外交宣言。两套功能只共享入口，内部处理互不影响。"),
			new InquiryElement("local_policies", "地方政策", null, isEnabled: true, "发布只影响玩家家族封地范围的地方政策，或查看地方政策记录。"),
			new InquiryElement("world_policies", "查看王国公告", null, isEnabled: true, "统一查看自定义政策、政策衍生事件与各国公开外交宣言。")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("王国公告", "请选择公告功能：", list, isExitShown: true, 1, 1, "确定", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRootMenu();
				return;
			}
			string text = selected[0].Identifier as string;
			if (string.Equals(text, "compose", StringComparison.Ordinal))
			{
				OpenRoyalAnnouncementComposeChoice();
			}
			else if (string.Equals(text, "local_policies", StringComparison.Ordinal))
			{
				CloseTerminal();
				CustomPolicyBehavior.OpenLocalPolicyManagementFromTerminal(OpenCustomPolicyManagementView);
			}
			else if (string.Equals(text, "world_policies", StringComparison.Ordinal))
			{
				CloseTerminal();
				if (!WorldDiplomacyBehavior.ShowRoyalAnnouncementArchive())
				{
					InformationManager.DisplayMessage(new InformationMessage("打开王国公告界面失败。"));
				}
			}
			else
			{
				OpenRootMenu();
			}
		}, delegate
		{
			OpenRootMenu();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenRoyalAnnouncementComposeChoice()
	{
		_terminalUiActive = true;
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("custom_policy", "撰写自定义政策", null, isEnabled: true, "进入原有自定义政策功能。政策评议、费用、议程、效果和存档链路保持不变。"),
			new InquiryElement("diplomatic_document", "撰写外交宣言", null, isEnabled: true, "发布王国外交宣言；对象国与外交意图由后台判断，不会暂停游戏等待生成。")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("撰写王国公告", "请选择公告类别：", list, isExitShown: true, 1, 1, "确定", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenCustomPolicyManagementView();
				return;
			}
			string id = selected[0].Identifier as string;
			if (string.Equals(id, "custom_policy", StringComparison.Ordinal))
			{
				CloseTerminal();
				CustomPolicyBehavior.OpenFromTerminal();
			}
			else if (string.Equals(id, "diplomatic_document", StringComparison.Ordinal))
			{
				CloseTerminal();
				if (!WorldDiplomacyBehavior.OpenComposeFromTerminal())
				{
					InformationManager.DisplayMessage(new InformationMessage("打开外交宣言撰写界面失败。"));
				}
			}
			else
			{
				OpenCustomPolicyManagementView();
			}
		}, delegate
		{
			OpenCustomPolicyManagementView();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenVassalageManagementView()
	{
		_terminalUiActive = true;
		VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
		if (vassalageBehavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData("臣属国管理", "臣属国管理页不可用：VassalageBehavior 尚未初始化。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "关闭", "", delegate
			{
				CloseTerminal();
			}, null), pauseGameActiveState: true, prioritize: false);
			return;
		}
		TerminalVassalageManagementData data = vassalageBehavior.BuildTerminalVassalageManagementData();
		if (data.Subjects.Count <= 0)
		{
			InformationManager.ShowInquiry(new InquiryData(data.TitleText ?? "臣属国管理", data.DescriptionText ?? "", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "关闭", "", delegate
			{
				CloseTerminal();
			}, null), pauseGameActiveState: true, prioritize: false);
			return;
		}
		List<InquiryElement> list = data.Subjects.Select((TerminalVassalageSubjectData subject) => new InquiryElement(subject.AgreementId, subject.EntryTitleText, null, subject.IsTributePaying, subject.EntryHintText)).ToList();
		MultiSelectionInquiryData inquiryData = new MultiSelectionInquiryData(data.TitleText ?? "臣属国管理", data.DescriptionText ?? "请选择臣属国：", list, isExitShown: true, 1, 1, "查看贡赋记录", "关闭", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				CloseTerminal();
				return;
			}
			string agreementId = selected[0].Identifier as string;
			TerminalVassalageSubjectData subject = data.Subjects.FirstOrDefault((TerminalVassalageSubjectData x) => string.Equals(x.AgreementId, agreementId, StringComparison.OrdinalIgnoreCase));
			if (subject == null || !subject.IsTributePaying)
			{
				OpenVassalageManagementView();
				return;
			}
			OpenVassalageTributeHistoryView(subject.AgreementId);
		}, delegate
		{
			CloseTerminal();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(inquiryData, pauseGameActiveState: true);
	}

	private void OpenVassalageTributeHistoryView(string agreementId)
	{
		TerminalTributaryPaymentHistoryData historyData = VassalageBehavior.Instance?.BuildTerminalTributaryPaymentHistoryData(agreementId) ?? new TerminalTributaryPaymentHistoryData
		{
			SubtitleText = "VassalageBehavior 尚未初始化。",
			EmptyStateText = "尚无贡赋入库记录。"
		};
		if (!TerminalVassalageTributeHistoryPopup.Show(historyData, OpenVassalageManagementView))
		{
			InformationManager.ShowInquiry(new InquiryData(historyData.TitleText ?? "贡赋记录", BuildVassalageTributeHistoryFallbackText(historyData), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
			{
				OpenVassalageManagementView();
			}, null), pauseGameActiveState: true, prioritize: false);
		}
	}

	private static string BuildVassalageTributeHistoryFallbackText(TerminalTributaryPaymentHistoryData data)
	{
		if (data == null)
		{
			return "尚无贡赋入库记录。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(data.SubtitleText))
		{
			stringBuilder.AppendLine(data.SubtitleText);
			stringBuilder.AppendLine();
		}
		if (data.Records == null || data.Records.Count <= 0)
		{
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(data.EmptyStateText) ? "尚无贡赋入库记录。" : data.EmptyStateText);
			return stringBuilder.ToString().TrimEnd();
		}
		for (int i = 0; i < data.Records.Count; i++)
		{
			TerminalTributaryPaymentRecordData record = data.Records[i];
			stringBuilder.AppendLine((i + 1).ToString() + ". " + record.DateText + "  " + record.TributeValueText);
			if (!string.IsNullOrWhiteSpace(record.PlayerGainSummaryText))
			{
				stringBuilder.AppendLine(record.PlayerGainSummaryText);
			}
			if (!string.IsNullOrWhiteSpace(record.PlayerSettlementGainText))
			{
				stringBuilder.AppendLine("【宗主国各领地所得】");
				stringBuilder.AppendLine(record.PlayerSettlementGainText);
			}
			if (!string.IsNullOrWhiteSpace(record.TributaryCostText))
			{
				stringBuilder.AppendLine("【臣属国消耗】");
				stringBuilder.AppendLine(record.TributaryCostText);
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenWeeklyReportBrowser()
	{
		List<MyBehavior.WeeklyReportBrowserCountryData> terminalWeeklyReportBrowserCountries = MyBehavior.Instance?.GetTerminalWeeklyReportBrowserCountries() ?? new List<MyBehavior.WeeklyReportBrowserCountryData>();
		CloseTerminal();
		if (!TerminalWeeklyReportBrowserPopup.Show(terminalWeeklyReportBrowserCountries))
		{
			InformationManager.DisplayMessage(new InformationMessage("打开周报浏览器失败。"));
		}
	}

	private void OpenPlayerPersonaEditor()
	{
		CloseTerminal();
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法打开玩家外貌与背景编辑器：Knowledge 行为尚未初始化。"));
				return;
			}
			knowledgeLibraryBehavior.OpenPlayerAppearanceAndBackgroundEditor(delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已返回玩家外貌与背景设置。"));
			});
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] open player persona editor failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("打开玩家外貌与背景编辑器失败。"));
		}
	}

	private void OpenPlayerRpCrafterSelection()
	{
		_terminalUiActive = true;
		if (!RewardSystemBehavior.TryGetAvailablePlayerRpCraftersForExternal(
			out List<PlayerRpCrafterOption> crafters,
			out string error))
		{
			InformationManager.ShowInquiry(
				new InquiryData(
					"选择制造者",
					string.IsNullOrWhiteSpace(error)
						? "当前没有可用的家族成员或同伴。"
						: error.Trim(),
					isAffirmativeOptionShown: true,
					isNegativeOptionShown: false,
					"返回",
					"",
					OpenRootMenu,
					null),
				pauseGameActiveState: true,
				prioritize: false);
			return;
		}
		List<InquiryElement> entries = crafters.Select(crafter =>
			new InquiryElement(
				crafter.HeroId,
				crafter.DisplayName
					+ "　锻造等级：" + crafter.SmithingSkill
					+ "　锻造体力："
					+ (crafter.HasCraftingStamina
						? crafter.CraftingStamina + "/" + crafter.MaxCraftingStamina
							+ "（消耗 " + crafter.CraftingStaminaCost + "）"
						: "不可用"),
				null,
				isEnabled: crafter.HasEnoughCraftingStamina,
				!crafter.HasCraftingStamina
					? "无法读取该制造者的锻造体力。"
					: crafter.HasEnoughCraftingStamina
						? ""
						: "锻造体力不足，至少需要 "
							+ crafter.CraftingStaminaCost
							+ " 点。")).ToList();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			"选择制造者",
			"",
			entries,
			isExitShown: true,
			1,
			1,
			"选择",
			"返回",
			delegate(List<InquiryElement> selected)
			{
				string crafterHeroId = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(crafterHeroId))
				{
					OpenRootMenu();
					return;
				}
				OpenPlayerRpForgePopup(crafterHeroId);
			},
			delegate(List<InquiryElement> _)
			{
				OpenRootMenu();
			},
			"",
			isSeachAvailable: false);
		MBInformationManager.ShowMultiSelectionInquiry(
			data,
			pauseGameActiveState: true);
	}

	private void OpenPlayerRpForgePopup(string crafterHeroId)
	{
		if (!ReferenceEquals(Instance, this))
		{
			return;
		}
		CloseTerminal();
		int availableDenars = 0;
		try
		{
			availableDenars = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		}
		catch
		{
		}
		long popupSaveGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (PlayerRpForgePopup.Show(
			availableDenars,
			(sessionId, itemName, investmentDenars, forgeAsWeapon) =>
				HandlePlayerRpForgePreviewRequested(
					sessionId,
					itemName,
					investmentDenars,
					forgeAsWeapon,
					crafterHeroId,
					popupSaveGeneration),
			null,
			sessionId => HandlePlayerRpForgeCancelled(
				sessionId,
				popupSaveGeneration)))
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage("无法打开玩家RP物品制造界面。"));
		OpenPlayerRpCrafterSelection();
	}

	private void HandlePlayerRpForgePreviewRequested(
		long sessionId,
		string itemName,
		int investmentDenars,
		bool forgeAsWeapon,
		string crafterHeroId,
		long popupSaveGeneration)
	{
		if (!IsPlayerRpForgeUiCallbackCurrent(popupSaveGeneration))
		{
			PlayerRpForgePopup.TryCloseForPreview(sessionId);
			return;
		}
		if (!RewardSystemBehavior.TryBuildPlayerRpCraftTemplateSelectionForExternal(
			itemName,
			investmentDenars,
			forgeAsWeapon,
			crafterHeroId,
			out PlayerRpCraftTemplateSelectionRequest request,
			out string error))
		{
			InformationManager.DisplayMessage(
				new InformationMessage(
					string.IsNullOrWhiteSpace(error)
						? "无法建立安全模板候选榜单。"
						: error.Trim()));
			PlayerRpForgePopup.TryRestoreEditing(sessionId);
			return;
		}
		if (!PlayerRpForgePopup.TryCloseForPreview(sessionId))
		{
			return;
		}
		ShowPlayerRpCraftTemplateSelection(request, popupSaveGeneration);
	}

	private void ShowPlayerRpCraftTemplateSelection(
		PlayerRpCraftTemplateSelectionRequest request,
		long runtimeGeneration)
	{
		if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			return;
		}
		List<PlayerRpCraftTemplateCandidate> candidates = request?.Candidates;
		if (candidates == null || candidates.Count == 0)
		{
			ShowPlayerRpForgeError(
				"无法打开模板选择",
				"当前没有可选择的安全模板。",
				runtimeGeneration,
				() => OpenPlayerRpForgePopup(request?.CrafterHeroId));
			return;
		}
		try
		{
			List<InquiryElement> entries = new List<InquiryElement>(candidates.Count);
			foreach (PlayerRpCraftTemplateCandidate candidate in candidates)
			{
				string templateId = (candidate?.TemplateStringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(templateId))
				{
					continue;
				}
				string displayName = (candidate.DisplayName ?? templateId).Trim();
				string typeLabel = string.IsNullOrWhiteSpace(candidate.TypeLabel)
					? "未知类型"
					: candidate.TypeLabel.Trim();
				int price = Math.Max(0, candidate.StandardPrice);
				string title = "#" + Math.Max(1, candidate.Rank).ToString()
					+ " " + displayName
					+ "\n" + typeLabel
					+ "　" + price.ToString() + " 第纳尔";
				string hint = "模板 ID：" + templateId
					+ "\n类型：" + typeLabel
					+ "\n标准价格：" + price.ToString() + " 第纳尔";
				entries.Add(new InquiryElement(
					templateId,
					title,
					GetPlayerRpCraftTemplateImageIdentifier(templateId),
					isEnabled: true,
					hint));
			}
			if (entries.Count == 0)
			{
				ShowPlayerRpForgeError(
					"无法打开模板选择",
					"当前没有可显示的安全模板。",
					runtimeGeneration,
					() => OpenPlayerRpForgePopup(request?.CrafterHeroId));
				return;
			}
			int callbackGate = 0;
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(
				"选择 RP 物品模板",
				"请从 " + entries.Count.ToString()
					+ " 个安全候选（最多 50）中手动选择。可搜索；每项显示原版物品图标、类型和标准价格。",
				entries,
				isExitShown: true,
				1,
				1,
				"选择模板",
				"返回修改",
				delegate(List<InquiryElement> selected)
				{
					if (Interlocked.Exchange(ref callbackGate, 1) != 0
						|| !IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
					{
						return;
					}
					string selectedTemplateId =
						selected?.FirstOrDefault()?.Identifier as string;
					if (string.IsNullOrWhiteSpace(selectedTemplateId))
					{
						OpenPlayerRpForgePopup(request?.CrafterHeroId);
						return;
					}
					HandlePlayerRpCraftTemplateSelected(
						request,
						selectedTemplateId,
						runtimeGeneration);
				},
				delegate(List<InquiryElement> _)
				{
					if (Interlocked.Exchange(ref callbackGate, 1) == 0
						&& IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
					{
						OpenPlayerRpForgePopup(request?.CrafterHeroId);
					}
				},
				"",
				isSeachAvailable: true);
			MBInformationManager.ShowMultiSelectionInquiry(
				data,
				pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerRpCraft", "[WARN] template_selection_ui_failed " + ex.Message);
			ShowPlayerRpForgeError(
				"无法打开模板选择",
				ex.Message,
				runtimeGeneration,
				() => OpenPlayerRpForgePopup(request?.CrafterHeroId));
		}
	}

	private void HandlePlayerRpCraftTemplateSelected(
		PlayerRpCraftTemplateSelectionRequest request,
		string selectedTemplateId,
		long runtimeGeneration)
	{
		if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			return;
		}
		if (!RewardSystemBehavior.TryPreviewPlayerRpCraftWithPlayerSelectedTemplateForExternal(
			request,
			selectedTemplateId,
			out PlayerRpCraftPreview preview,
			out string error))
		{
			ShowPlayerRpForgeError(
				"无法预览制造结果",
				error,
				runtimeGeneration,
				() => OpenPlayerRpForgePopup(request?.CrafterHeroId));
			return;
		}
		preview.RuntimeGeneration = runtimeGeneration;
		ShowPlayerRpForgeConfirmation(preview);
	}

	private static ImageIdentifier GetPlayerRpCraftTemplateImageIdentifier(
		string templateStringId)
	{
		try
		{
			string templateId = (templateStringId ?? "").Trim();
			ItemObject template = string.IsNullOrWhiteSpace(templateId)
				? null
				: Game.Current?.ObjectManager?.GetObject<ItemObject>(templateId);
			return template == null ? null : new ItemImageIdentifier(template);
		}
		catch
		{
			return null;
		}
	}

	private void RetirePlayerRpForgeUi()
	{
		PlayerRpForgePopup.ClearDraft();
		_pendingPlayerRpItemIntroduction = null;
	}

	private void QueuePlayerRpItemIntroduction(
		string generatedStringId,
		string displayName,
		string completionMessage,
		long runtimeGeneration)
	{
		string itemId = (generatedStringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(itemId)
			|| !IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			ShowPlayerRpForgeCompletion(completionMessage, runtimeGeneration);
			return;
		}
		_pendingPlayerRpItemIntroduction = new PendingPlayerRpItemIntroduction
		{
			GeneratedStringId = itemId,
			DisplayName = displayName ?? "",
			CompletionMessage = completionMessage ?? "",
			RuntimeGeneration = runtimeGeneration,
			EarliestEngineTick = _engineTickSequence + 1L,
			NextOpenAttemptEngineTick = _engineTickSequence + 1L
		};
	}

	private void ProcessPendingPlayerRpItemIntroduction()
	{
		PendingPlayerRpItemIntroduction pending = _pendingPlayerRpItemIntroduction;
		if (pending == null)
		{
			return;
		}
		if (!IsPlayerRpForgeUiCallbackCurrent(pending.RuntimeGeneration))
		{
			_pendingPlayerRpItemIntroduction = null;
			return;
		}
		if (_engineTickSequence < pending.EarliestEngineTick
			|| _engineTickSequence < pending.NextOpenAttemptEngineTick
			|| !IsSafeToOpenPlayerRpItemIntroductionInput())
		{
			return;
		}
		string itemId = pending.GeneratedStringId;
		string displayName = pending.DisplayName;
		string completionMessage = pending.CompletionMessage;
		long runtimeGeneration = pending.RuntimeGeneration;
		bool opened = CourierLetterInputPopup.Show(
			"填写 RP 物品介绍",
			"物品：" + displayName,
			"请填写该物品的介绍；取消或留空不会覆盖已有介绍。",
			"",
			introduction => CompletePlayerRpItemIntroductionInput(
				itemId,
				introduction,
				completionMessage,
				runtimeGeneration),
			() => CompletePlayerRpItemIntroductionInput(
				itemId,
				null,
				completionMessage,
				runtimeGeneration));
		if (opened)
		{
			_pendingPlayerRpItemIntroduction = null;
			return;
		}
		// Avoid reopening attempts every frame if another UI mod temporarily owns the
		// current screen. The pending item remains intact and is retried on a later
		// safe UI tick.
		pending.NextOpenAttemptEngineTick = _engineTickSequence + 30L;
	}

	private static bool IsSafeToOpenPlayerRpItemIntroductionInput()
	{
		if (PlayerRpForgePopup.IsOpen
			|| CourierLetterInputPopup.IsOpen
			|| CourierLetterReplyPopup.IsOpen
			|| WorldDiplomacyComposePopup.IsOpen)
		{
			return false;
		}
		try
		{
			if (InformationManager.IsAnyInquiryActive())
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			return ScreenManager.TopScreen != null
				&& !HotkeyInputGuard.IsTextInputFocused();
		}
		catch
		{
			return false;
		}
	}

	private void CompletePlayerRpItemIntroductionInput(
		string generatedStringId,
		string introduction,
		string completionMessage,
		long runtimeGeneration)
	{
		if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			return;
		}
		string error = "";
		if (!string.IsNullOrWhiteSpace(introduction)
			&& !RewardSystemBehavior.TrySetGeneratedRpItemIntroductionForExternal(
				generatedStringId,
				introduction,
				out error))
		{
			InformationManager.DisplayMessage(
				new InformationMessage(
					"物品介绍未保存："
						+ (string.IsNullOrWhiteSpace(error) ? "未知原因。" : error.Trim())));
		}
		ShowPlayerRpForgeCompletion(completionMessage, runtimeGeneration);
	}

	private void ShowPlayerRpForgeCompletion(
		string completionMessage,
		long runtimeGeneration)
	{
		if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			return;
		}
		string message = (completionMessage ?? "").Trim();
		if (string.IsNullOrWhiteSpace(message))
		{
			message = "已成功制造 RP 物品。";
		}
		InformationManager.ShowInquiry(
			new InquiryData(
				"制造完成",
				SanitizePlayerRpForgeInquiryText(message),
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: false,
				"返回终端",
				"",
				delegate
				{
					if (IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
					{
						OpenRootMenu();
					}
				},
				null),
			pauseGameActiveState: true,
			prioritize: false);
	}

	private void HandlePlayerRpForgeCancelled(
		long sessionId,
		long popupSaveGeneration)
	{
		if (IsPlayerRpForgeUiCallbackCurrent(popupSaveGeneration))
		{
			OpenRootMenu();
		}
	}

	private void ShowPlayerRpForgeConfirmation(PlayerRpCraftPreview preview)
	{
		long runtimeGeneration = preview?.RuntimeGeneration ?? 0L;
		if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
		{
			return;
		}
		string itemName = preview?.RequestedName ?? "";
		int investmentDenars = preview?.InvestedDenars ?? 0;
		bool forgeAsWeapon = preview?.IsEquipment ?? false;
		string confirmationText = (preview?.ConfirmationText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(confirmationText))
		{
			confirmationText = "名称：" + (itemName ?? "").Trim()
				+ "\n投入：" + investmentDenars + " 第纳尔"
				+ "\n模式：" + (forgeAsWeapon ? "武器与装备" : "普通RP物品")
				+ "\n\n确认后才会扣除第纳尔并制造物品。";
		}
		string confirmationTitle = "确认制造";
		if (forgeAsWeapon)
		{
			string compactItemName = (itemName ?? "").Trim();
			const int maxTitleItemNameChars = 28;
			if (compactItemName.Length > maxTitleItemNameChars)
			{
				int takeLength = maxTitleItemNameChars;
				if (char.IsHighSurrogate(compactItemName[takeLength - 1])
					&& char.IsLowSurrogate(compactItemName[takeLength]))
				{
					takeLength--;
				}
				compactItemName = compactItemName.Substring(0, takeLength) + "…";
			}
			confirmationTitle = "制造“" + compactItemName + "” · "
				+ investmentDenars + " 第纳尔";
		}
		string safeConfirmationTitle =
			SanitizePlayerRpForgeInquiryText(confirmationTitle);
		string safeConfirmationText =
			SanitizePlayerRpForgeInquiryText(confirmationText);
		int decisionGate = 0;
		InformationManager.ShowInquiry(
			new InquiryData(
				safeConfirmationTitle,
				safeConfirmationText,
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				"确认制造",
				"返回修改",
				delegate
				{
					if (!IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration)
						|| Interlocked.Exchange(ref decisionGate, 1) != 0)
					{
						return;
					}
					if (!RewardSystemBehavior.TryCommitPlayerRpCraftForExternal(
						preview,
						out var result,
						out string commitError))
					{
						ShowPlayerRpForgeError(
							"制造未完成",
							commitError,
							runtimeGeneration,
							() => OpenPlayerRpForgePopup(preview.CrafterHeroId));
						return;
					}
					PlayerRpForgePopup.ClearDraft();
					string resultMessage = (result?.Message ?? "").Trim();
					if (string.IsNullOrWhiteSpace(resultMessage))
					{
						resultMessage = "已成功制造“" + (itemName ?? "").Trim() + "”。";
					}
					resultMessage = SanitizePlayerRpForgeInquiryText(resultMessage);
					QueuePlayerRpItemIntroduction(
						result?.GeneratedStringId,
						result?.DisplayName ?? itemName,
						resultMessage,
						runtimeGeneration);
				},
				delegate
				{
					if (IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration)
						&& Interlocked.Exchange(ref decisionGate, 1) == 0)
					{
						OpenPlayerRpForgePopup(preview.CrafterHeroId);
					}
				}),
			pauseGameActiveState: true,
			prioritize: false);
	}

	private bool IsPlayerRpForgeUiCallbackCurrent(long runtimeGeneration)
	{
		return ReferenceEquals(Instance, this)
			&& SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration);
	}

	private void ShowPlayerRpForgeError(
		string title,
		string error,
		long runtimeGeneration,
		Action onReturn)
	{
		string safeTitle = SanitizePlayerRpForgeInquiryText(title);
		string message = SanitizePlayerRpForgeInquiryText(error);
		if (string.IsNullOrWhiteSpace(message))
		{
			message = "原因未提供。";
		}
		InformationManager.ShowInquiry(
			new InquiryData(
				string.IsNullOrWhiteSpace(safeTitle) ? "玩家RP物品制造" : safeTitle,
				message,
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: false,
				"返回修改",
				"",
				delegate
				{
					if (IsPlayerRpForgeUiCallbackCurrent(runtimeGeneration))
					{
						onReturn?.Invoke();
					}
				},
				null),
			pauseGameActiveState: true,
			prioritize: false);
	}

	private static string SanitizePlayerRpForgeInquiryText(string value)
	{
		string text = (value ?? "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		foreach (string token in PlayerRpForgeInquiryGuardTokens)
		{
			text = BreakPlayerRpForgeInquiryToken(text, token);
		}
		return text;
	}

	private static string BreakPlayerRpForgeInquiryToken(
		string source,
		string token)
	{
		int matchIndex = source.IndexOf(
			token,
			StringComparison.OrdinalIgnoreCase);
		if (matchIndex < 0)
		{
			return source;
		}
		StringBuilder builder = new StringBuilder(source.Length + 4);
		int copyIndex = 0;
		while (matchIndex >= 0)
		{
			builder.Append(
				source,
				copyIndex,
				matchIndex - copyIndex);
			builder.Append(source, matchIndex, 1);
			builder.Append('\u200B');
			builder.Append(
				source,
				matchIndex + 1,
				token.Length - 1);
			copyIndex = matchIndex + token.Length;
			matchIndex = source.IndexOf(
				token,
				copyIndex,
				StringComparison.OrdinalIgnoreCase);
		}
		builder.Append(source, copyIndex, source.Length - copyIndex);
		return builder.ToString();
	}

	private void OpenTrustQueryMenu(Action onReturn)
	{
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("settlement", "搜索定居点信任", null, isEnabled: true, ""),
			new InquiryElement("hero", "搜索NPC信任", null, isEnabled: true, "")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("信任度查询", "请选择查询方式：", list, isExitShown: true, 1, 1, "确定", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text = selected[0].Identifier as string;
			if (string.Equals(text, "settlement", StringComparison.Ordinal))
			{
				OpenSettlementBrowser(delegate
				{
					OpenTrustQueryMenu(onReturn);
				});
			}
			else if (string.Equals(text, "hero", StringComparison.Ordinal))
			{
				OpenHeroBrowser(delegate
				{
					OpenTrustQueryMenu(onReturn);
				});
			}
			else
			{
				OpenTrustQueryMenu(onReturn);
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenSettlementBrowser(Action onReturn)
	{
		List<Settlement> list = Settlement.All.Where((Settlement x) => x != null).OrderBy((Settlement x) => x.Name?.ToString() ?? x.StringId ?? "").ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的定居点。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__back__", "返回上级", null, isEnabled: true, ""));
		foreach (Settlement item in list)
		{
			int num = instance?.GetSettlementLocalPublicTrust(item) ?? 0;
			int num2 = instance?.GetSettlementSharedPublicTrust(item) ?? 0;
			int num3 = ClampTrustForDisplay(num + num2);
			string text6 = $"{item.Name} 信任度：{FormatTrustDisplay(num3)}";
			list2.Add(new InquiryElement("settlement:" + item.StringId, text6, GetSettlementImageIdentifier(item), isEnabled: true, ""));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("定居点信任查询", "可直接在上方搜索框中筛选定居点。", list2, isExitShown: true, 1, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text7 = selected[0].Identifier as string;
			if (text7 == "__back__")
			{
				onReturn();
				return;
			}
			if (text7 != null && text7.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
			{
				string value = text7.Substring("settlement:".Length);
				Settlement settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals(x.StringId, value, StringComparison.OrdinalIgnoreCase));
				if (settlement == null)
				{
					OpenSettlementBrowser(onReturn);
				}
				else
				{
					OpenSettlementDetails(settlement, delegate
					{
						OpenSettlementBrowser(onReturn);
					});
				}
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenHeroBrowser(Action onReturn)
	{
		List<Hero> list = Hero.AllAliveHeroes.Where((Hero x) => x != null).OrderBy((Hero x) => x.Name?.ToString() ?? x.StringId ?? "").ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的 NPC。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__back__", "返回上级", null, isEnabled: true, ""));
		foreach (Hero item in list)
		{
			int num = instance?.GetEffectiveTrust(item) ?? 0;
			string text4 = item.Name?.ToString() ?? item.StringId ?? "未知NPC";
			list2.Add(new InquiryElement("hero:" + item.StringId, $"{text4} 信任度：{FormatTrustDisplay(num)}", GetHeroImageIdentifier(item), isEnabled: true, ""));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("NPC信任查询", "可直接在上方搜索框中筛选 NPC。", list2, isExitShown: true, 1, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text5 = selected[0].Identifier as string;
			if (text5 == "__back__")
			{
				onReturn();
				return;
			}
			if (text5 != null && text5.StartsWith("hero:", StringComparison.OrdinalIgnoreCase))
			{
				string value = text5.Substring("hero:".Length);
				Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != null && string.Equals(x.StringId, value, StringComparison.OrdinalIgnoreCase));
				if (hero == null)
				{
					OpenHeroBrowser(onReturn);
				}
				else
				{
					OpenHeroDetails(hero, delegate
					{
						OpenHeroBrowser(onReturn);
					});
				}
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenSettlementDetails(Settlement settlement, Action onReturn)
	{
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildSettlementTrustReport(settlement, instance);
		InformationManager.ShowInquiry(new InquiryData("定居点信任详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
		{
			onReturn();
		}, null), pauseGameActiveState: true, prioritize: false);
	}

	private static string BuildSettlementTrustReport(Settlement settlement, RewardSystemBehavior reward)
	{
		if (settlement == null)
		{
			return "未找到定居点。";
		}
		string text = settlement.Name?.ToString() ?? settlement.StringId ?? "未知定居点";
		string text2 = settlement.MapFaction?.Name?.ToString() ?? settlement.OwnerClan?.Kingdom?.Name?.ToString() ?? settlement.OwnerClan?.Name?.ToString() ?? "未知势力";
		int num = reward?.GetSettlementLocalPublicTrust(settlement) ?? 0;
		int num2 = reward?.GetSettlementSharedPublicTrust(settlement) ?? 0;
		int num3 = ClampTrustForDisplay(num + num2);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"名称：{text}");
		stringBuilder.AppendLine($"所属势力：{text2}");
		stringBuilder.AppendLine($"信任度：{FormatTrustDisplay(num3)}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("商贩信任度：");
		foreach (RewardSystemBehavior.SettlementMerchantKind value in Enum.GetValues(typeof(RewardSystemBehavior.SettlementMerchantKind)))
		{
			if (value == RewardSystemBehavior.SettlementMerchantKind.None)
			{
				continue;
			}
			int settlementMerchantEffectiveTrust = reward?.GetSettlementMerchantEffectiveTrust(settlement, value) ?? 0;
			stringBuilder.AppendLine($"{GetMerchantKindLabel(value)}：{FormatTrustDisplay(settlementMerchantEffectiveTrust)}");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenHeroDetails(Hero hero, Action onReturn)
	{
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildHeroTrustReport(hero, instance);
		InformationManager.ShowInquiry(new InquiryData("NPC信任详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
		{
			onReturn();
		}, null), pauseGameActiveState: true, prioritize: false);
	}

	private static string BuildHeroTrustReport(Hero hero, RewardSystemBehavior reward)
	{
		if (hero == null)
		{
			return "未找到 NPC。";
		}
		string text = hero.Name?.ToString() ?? hero.StringId ?? "未知NPC";
		string text2 = hero.MapFaction?.Name?.ToString() ?? hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? hero.Culture?.Name?.ToString() ?? "未知势力";
		int num = reward?.GetNpcTrust(hero) ?? 0;
		int num2 = reward?.GetPublicTrust(hero) ?? 0;
		int num3 = reward?.GetEffectiveTrust(hero) ?? 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"名称：{text}");
		stringBuilder.AppendLine($"所属势力：{text2}");
		stringBuilder.AppendLine($"信任度：{FormatTrustDisplay(num3)}");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string GetMerchantKindLabel(RewardSystemBehavior.SettlementMerchantKind kind)
	{
		switch (kind)
		{
		case RewardSystemBehavior.SettlementMerchantKind.Weapon:
			return "武器商";
		case RewardSystemBehavior.SettlementMerchantKind.Armor:
			return "防具商";
		case RewardSystemBehavior.SettlementMerchantKind.Horse:
			return "马商";
		case RewardSystemBehavior.SettlementMerchantKind.Goods:
			return "杂货商";
		case RewardSystemBehavior.SettlementMerchantKind.Blacksmith:
			return "铁匠";
		default:
			return kind.ToString();
		}
	}

	private void OpenTagCatalogBrowser(AnimusForgeTagCatalogSnapshot snapshot = null, bool forceRefresh = false)
	{
		_terminalUiActive = true;
		try
		{
			snapshot ??= AnimusForgeTagCatalog.BuildSnapshot(forceRefresh);
			if (snapshot == null || snapshot.Entries.Count <= 0)
			{
				InformationManager.ShowInquiry(new InquiryData("AnimusForge 标签列表", "没有扫描到可显示的标签。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", OpenRootMenu, null), pauseGameActiveState: true, prioritize: false);
				return;
			}
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("__refresh__", "刷新标签索引", null, isEnabled: true, BuildTagCatalogRefreshHint(snapshot)),
				new InquiryElement("__export__", "导出为TXT", null, isEnabled: true, "把当前标签列表一键导出到 AnimusForge 模块目录，文件名带时间戳，不覆盖旧文件。")
			};
			foreach (AnimusForgeTagCatalogEntry entry in snapshot.Entries)
			{
				list.Add(new InquiryElement(entry.Id, BuildTagCatalogEntryTitle(entry), null, isEnabled: true, BuildTagCatalogEntryHint(entry)));
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("AnimusForge 标签列表", BuildTagCatalogSummary(snapshot), list, isExitShown: true, 1, 1, "查看", "返回", delegate(List<InquiryElement> selected)
			{
				if (selected == null || selected.Count == 0)
				{
					OpenRootMenu();
					return;
				}
				string text = selected[0].Identifier as string;
				if (string.Equals(text, "__refresh__", StringComparison.Ordinal))
				{
					OpenTagCatalogBrowser(null, forceRefresh: true);
					return;
				}
				if (string.Equals(text, "__export__", StringComparison.Ordinal))
				{
					ExportTagCatalogToModuleTxt(snapshot);
					return;
				}
				AnimusForgeTagCatalogEntry tagEntry = snapshot.Entries.FirstOrDefault((AnimusForgeTagCatalogEntry x) => string.Equals(x.Id, text, StringComparison.Ordinal));
				if (tagEntry == null)
				{
					OpenTagCatalogBrowser(snapshot);
					return;
				}
				OpenTagCatalogEntryDetail(snapshot, tagEntry);
			}, delegate
			{
				OpenRootMenu();
			}, "", isSeachAvailable: true);
			MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] open tag catalog failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("打开标签列表失败：" + ex.Message));
			OpenRootMenu();
		}
	}

	private void ExportTagCatalogToModuleTxt(AnimusForgeTagCatalogSnapshot snapshot)
	{
		_terminalUiActive = true;
		try
		{
			if (AnimusForgeTagCatalog.TryExportSnapshotToModuleTxt(snapshot, out var filePath, out var error))
			{
				InformationManager.ShowInquiry(new InquiryData("标签列表已导出", "已导出到：\n" + filePath, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
				{
					OpenTagCatalogBrowser(snapshot);
				}, null), pauseGameActiveState: true, prioritize: false);
				return;
			}
			InformationManager.ShowInquiry(new InquiryData("标签列表导出失败", string.IsNullOrWhiteSpace(error) ? "未知错误。" : error, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
			{
				OpenTagCatalogBrowser(snapshot);
			}, null), pauseGameActiveState: true, prioritize: false);
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] export tag catalog failed: " + ex);
			InformationManager.ShowInquiry(new InquiryData("标签列表导出失败", ex.Message, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
			{
				OpenTagCatalogBrowser(snapshot);
			}, null), pauseGameActiveState: true, prioritize: false);
		}
	}

	private void OpenTagCatalogEntryDetail(AnimusForgeTagCatalogSnapshot snapshot, AnimusForgeTagCatalogEntry entry)
	{
		_terminalUiActive = true;
		if (entry == null)
		{
			OpenTagCatalogBrowser(snapshot);
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("标签详情", BuildTagCatalogEntryDetailText(entry), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
		{
			OpenTagCatalogBrowser(snapshot);
		}, null), pauseGameActiveState: true, prioritize: false);
	}

	private static string BuildTagCatalogSummary(AnimusForgeTagCatalogSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return "";
		}
		int bodyCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("正文", StringComparison.Ordinal));
		int postprocessCount = snapshot.Entries.Count((AnimusForgeTagCatalogEntry x) => (x.Category ?? "").StartsWith("后处理", StringComparison.Ordinal));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("从当前 AnimusForge 模块文件、当前程序集和内置运行时规则提取。");
		stringBuilder.AppendLine("共 " + snapshot.Entries.Count + " 项；正文/历史 " + bodyCount + " 项，后处理 " + postprocessCount + " 项。");
		stringBuilder.AppendLine("已扫描文件：" + snapshot.ScannedFileCount + " 个。可用上方搜索框按标签、功能名或参数名筛选。");
		if (snapshot.SourceRoots.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("来源根目录：");
			foreach (string root in snapshot.SourceRoots.Take(3))
			{
				stringBuilder.AppendLine(root);
			}
			if (snapshot.SourceRoots.Count > 3)
			{
				stringBuilder.AppendLine("+" + (snapshot.SourceRoots.Count - 3) + " 个目录");
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildTagCatalogRefreshHint(AnimusForgeTagCatalogSnapshot snapshot)
	{
		int count = snapshot?.Entries?.Count ?? 0;
		return "重新扫描当前模块文件和程序集。当前索引：" + count + " 项。";
	}

	private static string BuildTagCatalogEntryTitle(AnimusForgeTagCatalogEntry entry)
	{
		if (entry == null)
		{
			return "标签";
		}
		return "[" + (entry.Category ?? "标签") + "] " + (entry.Tag ?? "");
	}

	private static string BuildTagCatalogEntryHint(AnimusForgeTagCatalogEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string description = CompactOneLine(entry.Description);
		string source = (entry.Sources != null && entry.Sources.Count > 0) ? entry.Sources[0] : "";
		string text = "";
		if (!string.IsNullOrWhiteSpace(description))
		{
			text = description;
		}
		if (!string.IsNullOrWhiteSpace(source))
		{
			text = string.IsNullOrWhiteSpace(text) ? ("来源：" + source) : (text + " 来源：" + source);
		}
		return TruncateForInquiry(text, 220);
	}

	private static string BuildTagCatalogEntryDetailText(AnimusForgeTagCatalogEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("标签：" + (entry.Tag ?? ""));
		stringBuilder.AppendLine("分类：" + (entry.Category ?? "标签"));
		if (!string.IsNullOrWhiteSpace(entry.Description))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("说明：");
			stringBuilder.AppendLine(entry.Description.Trim());
		}
		if (entry.Sources != null && entry.Sources.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("来源：");
			foreach (string source in entry.Sources.Take(12))
			{
				stringBuilder.AppendLine(source);
			}
			if (entry.Sources.Count > 12)
			{
				stringBuilder.AppendLine("+" + (entry.Sources.Count - 12) + " 个来源");
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string CompactOneLine(string text)
	{
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		return text;
	}

	private static string TruncateForInquiry(string text, int maxLength)
	{
		text = text ?? "";
		if (maxLength <= 0 || text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, Math.Max(0, maxLength - 1)).TrimEnd() + "…";
	}

	private void CloseTerminal()
	{
		_terminalUiActive = false;
	}

	private static ImageIdentifier GetHeroImageIdentifier(Hero hero)
	{
		try
		{
			CharacterObject characterObject = hero?.CharacterObject;
			if (characterObject == null)
			{
				return null;
			}
			CharacterCode characterCode = CharacterCode.CreateFrom(characterObject);
			return new CharacterImageIdentifier(characterCode);
		}
		catch
		{
			return null;
		}
	}

	private static ImageIdentifier GetSettlementImageIdentifier(Settlement settlement)
	{
		try
		{
			Banner banner = settlement?.OwnerClan?.Banner ?? settlement?.MapFaction?.Banner ?? settlement?.OwnerClan?.Kingdom?.Banner;
			if (banner == null)
			{
				return null;
			}
			return new BannerImageIdentifier(banner);
		}
		catch
		{
			return null;
		}
	}

	private static string FormatTrustDisplay(int trust)
	{
		int trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
		string trustLevelText = RewardSystemBehavior.GetTrustLevelText(trust);
		return $"({trust}，{trustLevelText}，{trustLevelIndex}/10)";
	}

	private static int ClampTrustForDisplay(int trust)
	{
		if (trust < -100)
		{
			return -100;
		}
		if (trust > 100)
		{
			return 100;
		}
		return trust;
	}

	private static InputKey GetConfiguredTerminalKey()
	{
		try
		{
			return ParseTerminalKey(DuelSettings.GetSettings()?.TerminalKey);
		}
		catch
		{
			return InputKey.U;
		}
	}

	private static InputKey ParseTerminalKey(string terminalKey)
	{
		if (!string.IsNullOrWhiteSpace(terminalKey) && Enum.TryParse<InputKey>(terminalKey.Trim().ToUpperInvariant(), out var result))
		{
			return result;
		}
		return InputKey.U;
	}

	private static string GetConfiguredTerminalKeyLabel()
	{
		try
		{
			return GetConfiguredTerminalKey().ToString().ToUpperInvariant();
		}
		catch
		{
			return "U";
		}
	}
}
