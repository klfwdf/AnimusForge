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



	private static readonly Lazy<string[]> PlayerRpForgeInquiryGuardTokens = new Lazy<string[]>(() => new[]
	{
		LlmSourceTexts.Get("Signal_Failed"),
		LlmSourceTexts.Get("Signal_Error"),
		LlmSourceTexts.Get("Signal_Problem"),
		LlmSourceTexts.Get("Signal_Exception"),
		LlmSourceTexts.Get("Signal_ErrorDetail"),
		LlmSourceTexts.Get("Signal_Cannot"),
		LlmSourceTexts.Get("Signal_Unavailable"),
		LlmSourceTexts.Get("Signal_Uninitialized"),
		LlmSourceTexts.Get("Signal_Blocked"),
		LlmSourceTexts.Get("Signal_Timeout"),
		LlmSourceTexts.Get("Signal_Interrupted"),
		LlmSourceTexts.Get("Signal_Refused"),
		LlmSourceTexts.Get("Signal_Invalid"),
		LlmSourceTexts.Get("Signal_Unconfigured"),
		LlmSourceTexts.Get("Signal_NotFound"),
		"error",
		"problem",
		"failed",
		"failure",
		"exception",
		"timeout",
		"timed out",
		"interrupted",
		"refused",
		"rejected",
		"blocked",
		"invalid",
		"not found"
	});



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

		// Idempotent; runs before the campaign map screen builds its navigation bar.

		AFWarStatsTerminal.UI.AfWarStatsMapNavigationEntry.EnsurePatched();

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

		AnimusForgeTerminalPopup.TickActive();

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

		if (!AnimusForgeTerminalSettings.IsHotkeyEnabled)

		{

			_wasTerminalKeyDown = false;

			return;

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

		if (CourierLetterInputPopup.IsOpen || CourierLetterReplyPopup.IsOpen || DiplomacyPresentationBridge.IsOpen)

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

			// Hide the periodic hotkey text; retain the hotkey and map navigation entry.
			if (!AnimusForgeTerminalSettings.IsHotkeyEnabled && AnimusForgeTerminalSettings.IsMapIconEnabled)

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_DailyMapIconHint", "Click the terminal icon in the lower-left campaign map navigation bar to open the AnimusForge terminal.")));

			}

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



	public void OpenRootMenu()

	{

		_terminalUiActive = true;

		List<AnimusForgeTerminalNode> roots = BuildTerminalRootNodes();

		if (!AnimusForgeTerminalPopup.Show(roots, ExecuteTerminalLeaf, CloseTerminal))

		{

			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_OpenFailed", "Failed to open the AnimusForge terminal.")));

			CloseTerminal();

		}

	}



	public void OpenTerminalToWarStats()

	{

		OpenRootMenu();

		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.ShowWarStats();

	}



	private static AnimusForgeTerminalNode Node(string id, string title, string category, string icon, string hint, params AnimusForgeTerminalNode[] children)

	{

		AnimusForgeTerminalNode node = new AnimusForgeTerminalNode

		{

			Id = id ?? "",

			Title = title ?? "",

			Category = string.IsNullOrWhiteSpace(category) ? TerminalCategories.All : category,

			Icon = string.IsNullOrWhiteSpace(icon) ? "❖" : icon,

			Hint = hint ?? ""

		};

		if (children != null)

		{

			node.Children.AddRange(children);

		}

		return node;

	}



	private static AnimusForgeTerminalNode Node(string id, string title, string category, string hint, params AnimusForgeTerminalNode[] children)

	{

		return Node(id, title, category, "❖", hint, children);

	}



	private static List<AnimusForgeTerminalNode> BuildTerminalRootNodes()

	{

		return new List<AnimusForgeTerminalNode>

		{

			Node("custom_policy_management", Ui("AF_TERMINAL_UI_Root_KingdomNotices", "Kingdom notices"), TerminalCategories.Diplomacy, "♚", Ui("AF_TERMINAL_UI_Root_KingdomNotices_Hint", "Open the subpanel for notices and local policies."),

				Node("policy_compose", Ui("AF_TERMINAL_UI_Root_ComposeNotice", "Compose kingdom notice"), TerminalCategories.Diplomacy, "✍", Ui("AF_TERMINAL_UI_Root_ComposeNotice_Hint", "Continue to custom policy or diplomatic declaration."),

					Node("custom_policy", Ui("AF_TERMINAL_UI_Root_CustomPolicy", "Compose custom policy"), TerminalCategories.Diplomacy, "⚖", Ui("AF_TERMINAL_UI_Root_CustomPolicy_Hint", "Open the custom policy editor.")),

					Node("diplomatic_document", Ui("AF_TERMINAL_UI_Root_DiplomaticDocument", "Compose diplomatic declaration"), TerminalCategories.Diplomacy, "⚐", Ui("AF_TERMINAL_UI_Root_DiplomaticDocument_Hint", "Publish a diplomatic declaration."))),

				Node("local_policies", Ui("AF_TERMINAL_UI_Root_LocalPolicies", "Local policies"), TerminalCategories.Diplomacy, "♜", Ui("AF_TERMINAL_UI_Root_LocalPolicies_Hint", "Publish policies that affect only the player's clan fiefs."))),

			Node("vassalage_management", Ui("AF_TERMINAL_UI_Root_Vassalage", "Vassalage management"), TerminalCategories.Diplomacy, "⚜", Ui("AF_TERMINAL_UI_Root_Vassalage_Hint", "View existing vassal states; use LLM dialogue for termination, amendment, or annexation.")),

			Node("weekly_reports", Ui("AF_TERMINAL_UI_Root_KingdomNews", "Kingdom news"), TerminalCategories.Diplomacy, "✉", Ui("AF_TERMINAL_UI_Root_KingdomNews_Hint", "View recent diplomatic declarations, policies, bulletins, and news from each kingdom.")),



			Node("tag_catalog", Ui("AF_TERMINAL_UI_Root_TagCatalog", "Tag catalog"), TerminalCategories.Prompt, "❖", Ui("AF_TERMINAL_UI_Root_TagCatalog_Hint", "View and refresh the body/postprocess tag index or export it to TXT.")),

			Node("reload_database", Ui("AF_TERMINAL_UI_Root_ReloadDatabase", "Reload database"), TerminalCategories.Prompt, "⚡", Ui("AF_TERMINAL_UI_Root_ReloadDatabase_Hint", "Replace non-player knowledge, world/kingdom starting knowledge, kingdom personalities/strategy, and voices.")),



			Node("api_onboarding", Ui("AF_TERMINAL_UI_Root_ApiOnboarding", "Run API setup again"), TerminalCategories.Ai, "⚙", Ui("AF_TERMINAL_UI_Root_ApiOnboarding_Hint", "Select and test API configuration again.")),

			Node("analyze_latest_error", Ui("AF_TERMINAL_UI_Root_AnalyzeError", "Analyze latest error"), TerminalCategories.Ai, "⚠", Ui("AF_TERMINAL_UI_Root_AnalyzeError_Hint", "Use the preprocessing API to analyze the latest AnimusForge error in this campaign.")),



			Node("settlement_entry_troops", Ui("AF_TERMINAL_UI_Root_SettlementEntryTroops", "Settlement entry escort"), TerminalCategories.Duel, "♞", Ui("AF_TERMINAL_UI_Root_SettlementEntryTroops_Hint", "Configure companions and troops automatically brought into towns, castles, and villages by SETS.")),

			Node("noble_prisoner_escort", Ui("AF_TERMINAL_UI_Root_NoblePrisonerEscort", "Noble prisoner escort"), TerminalCategories.Duel, "⚓", Ui("AF_TERMINAL_UI_Root_NoblePrisonerEscort_Hint", "Configure hero prisoners brought into sieges, settlements, lord halls, and wilderness meetings.")),

			Node("troop_inspection", Ui("AF_TERMINAL_UI_Root_TroopInspection", "Inspect troops"), TerminalCategories.Duel, "⚔", Ui("AF_TERMINAL_UI_Root_TroopInspection_Hint", "Open the troop inspection flow.")),

			Node("military_exercise", Ui("AF_TERMINAL_UI_Root_MilitaryExercise", "Military exercise"), TerminalCategories.Duel, "⚑", Ui("AF_TERMINAL_UI_Root_MilitaryExercise_Hint", "Open the military exercise flow.")),



			Node("player_persona", Ui("AF_TERMINAL_UI_Root_PlayerPersona", "Edit player appearance and background"), TerminalCategories.Characters, "♟", Ui("AF_TERMINAL_UI_Root_PlayerPersona_Hint", "Edit the player's RP appearance and background.")),

			Node("player_rp_forge", Ui("AF_TERMINAL_UI_Root_PlayerRpForge", "Craft RP item"), TerminalCategories.Characters, "⚒", Ui("AF_TERMINAL_UI_Root_PlayerRpForge_Hint", "Spend denars to craft a personal RP item or weapon/equipment.")),

			Node("trust_query", Ui("AF_TERMINAL_UI_Root_TrustQuery", "Trust query"), TerminalCategories.Characters, "⚖", Ui("AF_TERMINAL_UI_Root_TrustQuery_Hint", "Query trust sources for heroes, settlements, and merchants."))

		};

	}



	private static string Ui(string id, string fallback)

	{

		return new TextObject("{=" + id + "}" + fallback).ToString();

	}



	private static string UiFormat(string id, string fallback, params (string Key, object Value)[] variables)

	{

		return DeveloperUiText.Format(id, fallback, variables);

	}



	private bool ExecuteTerminalLeaf(string id)

	{

		AnimusForgeTerminalPopupVM popupVm = AnimusForgeTerminalPopup.ActivePopup?.ViewModel;

		switch (id ?? "")

		{

		case "war_stats":

			if (popupVm != null)

			{

				popupVm.ShowWarStats();

				return true;

			}

			return false;

		case "weekly_reports":

			if (popupVm == null) return false;

			List<MyBehavior.WeeklyReportBrowserCountryData> countries = MyBehavior.Instance?.GetTerminalKingdomNewsCountries() ?? new List<MyBehavior.WeeklyReportBrowserCountryData>();

			popupVm.ShowWeeklyReports(countries);

			return true;



		case "vassalage_management":

			if (popupVm == null) return false;

			ShowVassalageManagement(popupVm);

			return true;



		case "tag_catalog":

			if (popupVm == null) return false;

			popupVm.ShowTagCatalog(AnimusForgeTagCatalog.BuildSnapshot(forceRefresh: false));

			return true;



		case "trust_query":

			if (popupVm == null) return false;

			ShowTrustQuery(popupVm);

			return true;



		case "analyze_latest_error":

			if (popupVm == null) return false;

			bool hasError = AiErrorAnalysisInquiry.TryGetLatestFailure(out string errTitle, out string errDetail);

			string latestError = UiFormat("AF_TERMINAL_UI_DiagnosticsLatestError", "Latest error: {TITLE}\n{DETAIL}",

				("TITLE", errTitle), ("DETAIL", LlmRetryPrompt.ProjectKnownErrorForUi(errDetail)));

			popupVm.ShowDiagnostics(

				Ui("AF_TERMINAL_UI_DiagnosticsOnline", "Sentinel status: online"),

				hasError ? latestError : Ui("AF_TERMINAL_UI_DiagnosticsNoError", "No fatal error stack has been recorded in this campaign."),

				hasError

			);

			return true;



		case "custom_policy":

			CloseTerminal();

			CustomPolicyBehavior.OpenFromTerminal();

			return true;



		case "diplomatic_document":

			CloseTerminal();

			if (!DiplomacyPresentationBridge.OpenComposeFromTerminal())

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_DiplomaticComposeFailed", "Failed to open the diplomatic declaration editor.")));

			}

			return true;



		case "local_policies":

			CloseTerminal();

			CustomPolicyBehavior.OpenLocalPolicyManagementFromTerminal(OpenCustomPolicyManagementView);

			return true;



		case "settlement_entry_troops":

			CloseTerminal();

			SettlementEntryTroopSelectionBehavior.OpenConfigFromTerminal();

			return true;



		case "noble_prisoner_escort":

			CloseTerminal();

			NoblePrisonerEscortBehavior.OpenConfigFromTerminal();

			return true;



		case "troop_inspection":

			CloseTerminal();

			TroopInspectionBehavior.OpenInspectionFromTerminal();

			return true;



		case "military_exercise":

			CloseTerminal();

			MilitaryExerciseBehavior.OpenExerciseFromTerminal();

			return true;



		case "player_persona":

			OpenPlayerPersonaEditor();

			return true;



		case "player_rp_forge":

			OpenPlayerRpCrafterSelection();

			return true;



		case "reload_database":

			CloseTerminal();

			if (!MyBehavior.OpenDatabaseReloadFromTerminal(OpenRootMenu))

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_ReloadDatabaseFailed", "Unable to open the database reload flow.")));

			}

			return true;



		case "api_onboarding":

			CloseTerminal();

			if (!ModOnboardingBehavior.OpenApiSetupOnlyFlow())

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_ApiOnboardingFailed", "Unable to open API setup.")));

			}

			return true;



		default:

			return false;

		}

	}





	private void OpenCustomPolicyManagementView()

	{

		if (!ReferenceEquals(Instance, this)) return;

		OpenRootMenu();

		AnimusForgeTerminalPopup.ActivePopup?.ViewModel?.OpenNode(

			BuildTerminalRootNodes().First(node => node.Id == "custom_policy_management"));

	}



	private static void ShowVassalageManagement(AnimusForgeTerminalPopupVM popupVm)

	{

		VassalageBehavior owner = VassalageBehavior.Instance;

		TerminalVassalageManagementData data = owner?.BuildTerminalVassalageManagementData();

		if (data == null || data.Subjects.Count == 0)

		{

			popupVm.ShowDetails(data?.TitleText ?? Ui("AF_TERMINAL_UI_VassalageManagement", "Vassalage management"), data?.DescriptionText ?? Ui("AF_TERMINAL_UI_VassalageNotInitialized", "Vassalage management is not initialized yet."));

			return;

		}

		popupVm.ShowBrowser(data.TitleText ?? Ui("AF_TERMINAL_UI_VassalageManagement", "Vassalage management"), new[]

		{

			new AnimusForgeTerminalNode

			{

				Title = Ui("AF_TERMINAL_UI_VassalageManagementInfo", "Vassalage management details"),

				Hint = data.DescriptionText,

				OnExecute = () => popupVm.ShowDetails(data.TitleText ?? Ui("AF_TERMINAL_UI_VassalageManagement", "Vassalage management"), data.DescriptionText)

			}

		}.Concat(data.Subjects.Select(subject => new AnimusForgeTerminalNode

		{

			Id = subject.AgreementId,

			Title = subject.EntryTitleText,

			Hint = subject.EntryHintText,

			OnExecute = () =>

			{

				if (subject.IsTributePaying)

					popupVm.ShowVassalageTributeHistory(owner.BuildTerminalTributaryPaymentHistoryData(subject.AgreementId));

				else

					popupVm.ShowDetails(subject.EntryTitleText, subject.EntryHintText);

			}

		})));

	}



	private void OpenPlayerPersonaEditor()

	{

		CloseTerminal();

		try

		{

			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();

			if (knowledgeLibraryBehavior == null)

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_PlayerPersonaNotReady", "Unable to open the player appearance and background editor: the Knowledge behavior is not initialized.")));

				return;

			}

			knowledgeLibraryBehavior.OpenPlayerAppearanceAndBackgroundEditor(delegate

			{

				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_PlayerPersonaReturned", "Returned to player appearance and background settings.")));

			});

		}

		catch (Exception ex)

		{

			Logger.Log("Terminal", "[ERROR] open player persona editor failed: " + ex);

			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_PlayerPersonaOpenFailed", "Failed to open the player appearance and background editor.")));

		}

	}



	private void OpenPlayerRpCrafterSelection()

	{

		CloseTerminal();

		_terminalUiActive = true;

		if (!RewardSystemBehavior.TryGetAvailablePlayerRpCraftersForExternal(

			out List<PlayerRpCrafterOption> crafters,

			out string error))

		{

			InformationManager.ShowInquiry(

				new InquiryData(

					Ui("AF_TERMINAL_UI_Forge_SelectCrafter", "Select crafter"),

					string.IsNullOrWhiteSpace(error)

						? Ui("AF_TERMINAL_UI_Forge_NoCrafters", "No clan member or companion is currently available.")

						: error.Trim(),

					isAffirmativeOptionShown: true,

					isNegativeOptionShown: false,

					Ui("AF_TERMINAL_UI_Forge_Back", "Back"),

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

				UiFormat("AF_TERMINAL_UI_Forge_CrafterEntry", "{NAME}\nSmithing skill: {SKILL}\nCrafting stamina: {STAMINA} ({COST} cost)",

					("NAME", crafter.DisplayName), ("SKILL", crafter.SmithingSkill),

					("STAMINA", crafter.HasCraftingStamina ? crafter.CraftingStamina + "/" + crafter.MaxCraftingStamina : Ui("AF_TERMINAL_UI_Forge_Unavailable", "unavailable")),

					("COST", crafter.CraftingStaminaCost)),

				null,

				isEnabled: crafter.HasEnoughCraftingStamina,

				!crafter.HasCraftingStamina

					? Ui("AF_TERMINAL_UI_Forge_StaminaReadFailed", "Unable to read this crafter's crafting stamina.")

					: crafter.HasEnoughCraftingStamina

						? ""

						: UiFormat("AF_TERMINAL_UI_Forge_StaminaInsufficient", "Crafting stamina is insufficient; at least {COST} points are required.", ("COST", crafter.CraftingStaminaCost)))).ToList();

		MultiSelectionInquiryData data = new MultiSelectionInquiryData(

			Ui("AF_TERMINAL_UI_Forge_SelectCrafter", "Select crafter"),

			"",

			entries,

			isExitShown: true,

			1,

			1,

			Ui("AF_TERMINAL_UI_Forge_Select", "Select"),

			Ui("AF_TERMINAL_UI_Forge_Back", "Back"),

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

		InformationManager.DisplayMessage(new InformationMessage(Ui("AF_TERMINAL_UI_Forge_OpenFailed", "Unable to open the player RP item crafting interface.")));

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

						? Ui("AF_TERMINAL_UI_Forge_TemplateCandidatesFailed", "Unable to build a safe template candidate list.")

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

				Ui("AF_TERMINAL_UI_Forge_TemplateSelectionOpenFailed", "Unable to open template selection"),

				Ui("AF_TERMINAL_UI_Forge_NoSelectableTemplates", "No safe templates are available for selection."),

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

					? Ui("AF_TERMINAL_UI_Forge_UnknownType", "Unknown type")

					: candidate.TypeLabel.Trim();

				int price = Math.Max(0, candidate.StandardPrice);

				string title = UiFormat("AF_TERMINAL_UI_Forge_TemplateEntryTitle", "#{RANK} {NAME}\n{TYPE}\n{PRICE} denars", ("RANK", Math.Max(1, candidate.Rank)), ("NAME", displayName), ("TYPE", typeLabel), ("PRICE", price));

				string hint = UiFormat("AF_TERMINAL_UI_Forge_TemplateEntryHint", "Template ID: {ID}\nType: {TYPE}\nStandard price: {PRICE} denars", ("ID", templateId), ("TYPE", typeLabel), ("PRICE", price));

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

					Ui("AF_TERMINAL_UI_Forge_TemplateSelectionOpenFailed", "Unable to open template selection"),

					Ui("AF_TERMINAL_UI_Forge_NoDisplayableTemplates", "No safe templates can be displayed."),

					runtimeGeneration,

					() => OpenPlayerRpForgePopup(request?.CrafterHeroId));

				return;

			}

			int callbackGate = 0;

			MultiSelectionInquiryData data = new MultiSelectionInquiryData(

				Ui("AF_TERMINAL_UI_Forge_SelectTemplateTitle", "Select RP item template"),

				UiFormat("AF_TERMINAL_UI_Forge_SelectTemplateDescription", "Choose one of {COUNT} safe candidates (up to 50). Search is available; each entry shows the native item icon, type, and standard price.", ("COUNT", entries.Count)),

				entries,

				isExitShown: true,

				1,

				1,

				Ui("AF_TERMINAL_UI_Forge_SelectTemplate", "Select template"),

				Ui("AF_TERMINAL_UI_Forge_ReturnToEdit", "Return to edit"),

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

				Ui("AF_TERMINAL_UI_Forge_TemplateSelectionOpenFailed", "Unable to open template selection"),

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

				Ui("AF_TERMINAL_UI_Forge_PreviewFailed", "Unable to preview crafting result"),

				PlayerRpForgeDisplayTexts.ForDisplay(error),

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

			Ui("AF_TERMINAL_UI_Forge_IntroductionTitle", "Enter RP item introduction"),

			UiFormat("AF_TERMINAL_UI_Forge_IntroductionItem", "Item: {NAME}", ("NAME", displayName)),

			Ui("AF_TERMINAL_UI_Forge_IntroductionPrompt", "Enter an introduction for this item; cancelling or leaving it blank will keep the existing introduction."),

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

			|| DiplomacyPresentationBridge.IsOpen)

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

					Ui("AF_TERMINAL_UI_Forge_IntroductionNotSaved", "Item introduction was not saved: ")

						+ (string.IsNullOrWhiteSpace(error) ? Ui("AF_TERMINAL_UI_Forge_UnknownReason", "unknown reason.") : error.Trim())));

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

			message = Ui("AF_TERMINAL_UI_Forge_CompletedFallback", "RP item crafted successfully.");

		}

		InformationManager.ShowInquiry(

			new InquiryData(

				Ui("AF_TERMINAL_UI_Forge_CompletionTitle", "Crafting complete"),

				SanitizePlayerRpForgeInquiryText(message),

				isAffirmativeOptionShown: true,

				isNegativeOptionShown: false,

				Ui("AF_TERMINAL_UI_Forge_ReturnToTerminal", "Return to terminal"),

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

			confirmationText = UiFormat("AF_TERMINAL_UI_Forge_ConfirmationText", "Name: {NAME}\nInvestment: {INVESTMENT} denars\nMode: {MODE}\n\nDenars will be deducted and the item crafted only after confirmation.", ("NAME", (itemName ?? "").Trim()), ("INVESTMENT", investmentDenars), ("MODE", forgeAsWeapon ? Ui("AF_TERMINAL_UI_Forge_ModeEquipment", "Weapon and equipment") : Ui("AF_TERMINAL_UI_Forge_ModeItem", "Regular RP item")));

		}

		string confirmationTitle = Ui("AF_TERMINAL_UI_Forge_ConfirmationTitle", "Confirm crafting");

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

			confirmationTitle = UiFormat("AF_TERMINAL_UI_Forge_ConfirmationWeaponTitle", "Craft \"{NAME}\" · {INVESTMENT} denars", ("NAME", compactItemName), ("INVESTMENT", investmentDenars));

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

				Ui("AF_TERMINAL_UI_Forge_Confirm", "Confirm crafting"),

				Ui("AF_TERMINAL_UI_Forge_ReturnToEdit", "Return to edit"),

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

							Ui("AF_TERMINAL_UI_Forge_CommitFailed", "Crafting did not complete"),

							PlayerRpForgeDisplayTexts.ForDisplay(commitError),

							runtimeGeneration,

							() => OpenPlayerRpForgePopup(preview.CrafterHeroId));

						return;

					}

					PlayerRpForgePopup.ClearDraft();

					string resultMessage = (result?.Message ?? "").Trim();

					if (string.IsNullOrWhiteSpace(resultMessage))

					{

						resultMessage = UiFormat("AF_TERMINAL_UI_Forge_CommitSuccess", "\"{NAME}\" was crafted successfully.", ("NAME", (itemName ?? "").Trim()));

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

			message = Ui("AF_TERMINAL_UI_Forge_ReasonUnavailable", "No reason was provided.");

		}

		InformationManager.ShowInquiry(

			new InquiryData(

				string.IsNullOrWhiteSpace(safeTitle) ? Ui("AF_TERMINAL_UI_Forge_ErrorTitle", "Player RP item crafting") : safeTitle,

				message,

				isAffirmativeOptionShown: true,

				isNegativeOptionShown: false,

				Ui("AF_TERMINAL_UI_Forge_ReturnToEdit", "Return to edit"),

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

		foreach (string token in PlayerRpForgeInquiryGuardTokens.Value)

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



	private static void ShowTrustQuery(AnimusForgeTerminalPopupVM popupVm)

	{

		popupVm.ShowBrowser(Ui("AF_TERMINAL_UI_Trust_QueryTitle", "Trust query"), new[]

		{

			new AnimusForgeTerminalNode { Title = Ui("AF_TERMINAL_UI_Trust_SearchSettlement", "Search settlement trust"), OnExecute = () => ShowSettlementTrustBrowser(popupVm) },

			new AnimusForgeTerminalNode { Title = Ui("AF_TERMINAL_UI_Trust_SearchHero", "Search hero trust"), OnExecute = () => ShowHeroTrustBrowser(popupVm) }

		});

	}



	private static void ShowSettlementTrustBrowser(AnimusForgeTerminalPopupVM popupVm)

	{

		RewardSystemBehavior reward = RewardSystemBehavior.Instance;

		popupVm.ShowBrowser(Ui("AF_TERMINAL_UI_Trust_SettlementBrowserTitle", "Settlement trust query"), Settlement.All.Where(x => x != null)

			.OrderBy(x => x.Name?.ToString() ?? x.StringId).Select(settlement => new AnimusForgeTerminalNode

			{

				Id = settlement.StringId,

				ImageFactory = () => GetSettlementImageIdentifier(settlement),

				Title = settlement.Name?.ToString() ?? settlement.StringId,

				Hint = UiFormat("AF_TERMINAL_UI_Trust_Hint", "Trust: {TRUST}", ("TRUST", FormatTrustDisplay(ClampTrustForDisplay((reward?.GetSettlementLocalPublicTrust(settlement) ?? 0) + (reward?.GetSettlementSharedPublicTrust(settlement) ?? 0))))),

				OnExecute = () => popupVm.ShowDetails(Ui("AF_TERMINAL_UI_Trust_SettlementDetailsTitle", "Settlement trust details"), BuildSettlementTrustReport(settlement, RewardSystemBehavior.Instance))

			}));

	}



	private static void ShowHeroTrustBrowser(AnimusForgeTerminalPopupVM popupVm)

	{

		RewardSystemBehavior reward = RewardSystemBehavior.Instance;

		popupVm.ShowBrowser(Ui("AF_TERMINAL_UI_Trust_HeroBrowserTitle", "Hero trust query"), Hero.AllAliveHeroes.Where(x => x != null)

			.OrderBy(x => x.Name?.ToString() ?? x.StringId).Select(hero => new AnimusForgeTerminalNode

			{

				Id = hero.StringId,

				ImageFactory = () => GetHeroImageIdentifier(hero),

				Title = hero.Name?.ToString() ?? hero.StringId,

				Hint = UiFormat("AF_TERMINAL_UI_Trust_Hint", "Trust: {TRUST}", ("TRUST", FormatTrustDisplay(reward?.GetEffectiveTrust(hero) ?? 0))),

				OnExecute = () => popupVm.ShowDetails(Ui("AF_TERMINAL_UI_Trust_HeroDetailsTitle", "Hero trust details"), BuildHeroTrustReport(hero, RewardSystemBehavior.Instance))

			}));

	}



	private static string BuildSettlementTrustReport(Settlement settlement, RewardSystemBehavior reward)

	{

		if (settlement == null)

		{

			return Ui("AF_TERMINAL_UI_Trust_SettlementNotFound", "Settlement not found.");

		}

		string text = settlement.Name?.ToString() ?? settlement.StringId ?? Ui("AF_TERMINAL_UI_Trust_UnknownSettlement", "Unknown settlement");

		string text2 = settlement.MapFaction?.Name?.ToString() ?? settlement.OwnerClan?.Kingdom?.Name?.ToString() ?? settlement.OwnerClan?.Name?.ToString() ?? Ui("AF_TERMINAL_UI_Trust_UnknownFaction", "Unknown faction");

		int num = reward?.GetSettlementLocalPublicTrust(settlement) ?? 0;

		int num2 = reward?.GetSettlementSharedPublicTrust(settlement) ?? 0;

		int num3 = ClampTrustForDisplay(num + num2);

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_NameLine", "Name: {NAME}", ("NAME", text)));

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_FactionLine", "Faction: {FACTION}", ("FACTION", text2)));

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_LevelLine", "Trust: {TRUST}", ("TRUST", FormatTrustDisplay(num3))));

		stringBuilder.AppendLine();

		stringBuilder.AppendLine(Ui("AF_TERMINAL_UI_Trust_MerchantTrustHeading", "Merchant trust:"));

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



	private static string BuildHeroTrustReport(Hero hero, RewardSystemBehavior reward)

	{

		if (hero == null)

		{

			return Ui("AF_TERMINAL_UI_Trust_HeroNotFound", "Hero not found.");

		}

		string text = hero.Name?.ToString() ?? hero.StringId ?? Ui("AF_TERMINAL_UI_Trust_UnknownHero", "Unknown hero");

		string text2 = hero.MapFaction?.Name?.ToString() ?? hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? hero.Culture?.Name?.ToString() ?? Ui("AF_TERMINAL_UI_Trust_UnknownFaction", "Unknown faction");

		int num3 = reward?.GetEffectiveTrust(hero) ?? 0;

		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_NameLine", "Name: {NAME}", ("NAME", text)));

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_FactionLine", "Faction: {FACTION}", ("FACTION", text2)));

		stringBuilder.AppendLine(UiFormat("AF_TERMINAL_UI_Trust_LevelLine", "Trust: {TRUST}", ("TRUST", FormatTrustDisplay(num3))));

		return stringBuilder.ToString().TrimEnd();

	}



	private static string GetMerchantKindLabel(RewardSystemBehavior.SettlementMerchantKind kind)

	{

		switch (kind)

		{

		case RewardSystemBehavior.SettlementMerchantKind.Weapon:

			return Ui("AF_TERMINAL_UI_Trust_MerchantWeapon", "Weaponsmith");

		case RewardSystemBehavior.SettlementMerchantKind.Armor:

			return Ui("AF_TERMINAL_UI_Trust_MerchantArmor", "Armorsmith");

		case RewardSystemBehavior.SettlementMerchantKind.Horse:

			return Ui("AF_TERMINAL_UI_Trust_MerchantHorse", "Horse trader");

		case RewardSystemBehavior.SettlementMerchantKind.Goods:

			return Ui("AF_TERMINAL_UI_Trust_MerchantGoods", "Goods merchant");

		case RewardSystemBehavior.SettlementMerchantKind.Blacksmith:

			return Ui("AF_TERMINAL_UI_Trust_MerchantBlacksmith", "Blacksmith");

		default:

			return kind.ToString();

		}

	}



	private void CloseTerminal()

	{

		_terminalUiActive = false;

		AnimusForgeTerminalPopup.CloseActive(silent: true);

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

		return UiFormat("AF_UI_Terminal_TrustDisplay", "({TRUST}, {LEVEL}, {INDEX}/10)",

			("TRUST", trust), ("LEVEL", trustLevelText), ("INDEX", trustLevelIndex));

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

