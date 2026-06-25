using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public sealed class CourierDeliveryBehavior : CampaignBehaviorBase
{
	private const string LogSource = "CourierDelivery";
	private const string SessionStorageKey = "_af_courier_sessions_v1";
	private const string NpcDiplomacyLetterStorageKey = "_af_courier_npc_diplomacy_letters_v1";
	private const float MobilePartyArrivalDistance = 3.5f;
	private const float SenderArrivalDistanceSquared = 9f;
	private const float SettlementArrivalDistanceSquared = 1.44f;
	private const int MainReplyMaxTokens = 5000;
	private const double RouteRefreshSeconds = 2.0;
	private const double CampaignTickThrottleSeconds = 0.75;
	private const string CourierPartyPrefix = "af_courier_";
	private const string TemporaryCourierShipName = "AnimusForge Courier Boat";
	private const double NavalStuckRefreshHours = 12.0;
	private const float NavalStuckDistanceSquared = 0.0625f;
	private const int NavalStuckSafeRouteThreshold = 3;
	private const string CourierDirectionOutbound = "Outbound";
	private const string CourierDirectionInboundToPlayer = "InboundToPlayer";
	private const float NpcDiplomacyLetterScanIntervalHours = 12f;
	private const float NpcDiplomacyLetterGlobalCooldownDays = 5f;
	private const float NpcDiplomacyLetterSenderCooldownDays = 21f;
	private const float NpcDiplomacyLetterSendChance = 0.25f;
	private static readonly string[] CourierExcludedRuleIds = new[] { "duel", "lords_hall_access", "scene_mechanism_actions", "encounter_release_player" };

	private enum CourierStage
	{
		Outbound,
		WaitingRecipient,
		GeneratingReply,
		Returning,
		WaitingSender,
		Completed,
		Destroyed
	}

	private enum CourierPayloadMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners,
		GiveSettlements
	}

	private sealed class CourierSession
	{
		public string Id;
		public string Direction = CourierDirectionOutbound;
		public string SenderHeroId;
		public string SenderName;
		public string RecipientHeroId;
		public string RecipientName;
		public string CourierPartyId;
		public string Stage = CourierStage.Outbound.ToString();
		public string PayloadMode = CourierPayloadMode.Normal.ToString();
		public string LetterText;
		public string DeliveryFactText;
		public string ReplyText;
		public string ReplyPostprocessedText;
		public string LastRouteKey;
		public string TemporaryShipHullId;
		public bool TemporaryShipCreated;
		public string LastProgressRouteKey;
		public float LastProgressX;
		public float LastProgressY;
		public double LastProgressCampaignHours;
		public int NavalStuckRefreshCount;
		public string SafeSettlementId;
		public string RecipientWaitReason;
		public bool DeliveryApplied;
		public bool ReplyGenerated;
		public bool ReplyGenerationStarted;
		public bool ReplyPopupShown;
		public bool PostprocessConsumed;
		public bool ReplyWaitPopupShown;
		public int EscrowGold;
		public List<CourierCargoEntry> Entries = new List<CourierCargoEntry>();
		public List<CourierCargoEntry> CrewEntries = new List<CourierCargoEntry>();
	}

	private sealed class CourierCargoEntry
	{
		public string Kind;
		public string Id;
		public string Name;
		public int Amount;
		public int GuidePriceDenars;
		public bool IsHero;
		public bool Delivered;
	}

	private sealed class NpcDiplomacyLetterStorage
	{
		public Dictionary<string, float> SenderCooldownUntilDays { get; set; }
		public float GlobalCooldownUntilDays { get; set; }
		public float NextScanHour { get; set; }
	}

	private sealed class CourierRoutePlan
	{
		public MobileParty.NavigationType NavigationType;
		public bool RequiresNaval;
		public bool UsePort;
		public string Reason;

		public string KeySuffix
		{
			get
			{
				return ":nav=" + NavigationType + ":port=" + (UsePort ? "1" : "0") + ":sea=" + (RequiresNaval ? "1" : "0") + ":reason=" + (Reason ?? "");
			}
		}
	}

	private sealed class PendingCourierFlow
	{
		public Hero Recipient;
		public TroopRoster CrewRoster;
		public CourierPayloadMode Mode;
		public List<CourierCargoEntry> CrewEntries = new List<CourierCargoEntry>();
		public List<CourierTradeOption> TradeOptions = new List<CourierTradeOption>();
		public List<CourierCargoEntry> SelectedEntries = new List<CourierCargoEntry>();
		public int PendingAmountIndex;
	}

	private sealed class CourierTradeOption
	{
		public string Kind;
		public string Id;
		public string Name;
		public int AvailableAmount;
		public int GuidePriceDenars;
		public ItemObject Item;
		public MyBehavior.PartyTransferPromptEntry PartyEntry;
		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}

	private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
	private static bool _letterInputOpen;
	private static Harmony _courierHarmony;
	private static bool _partyNameplatePatchApplied;
	private static bool _partyNameplatePatchFailed;
	private static bool _mapTrackerProviderPatchApplied;
	private static bool _mapTrackerProviderPatchFailed;
	private static MapNotificationView _courierReplyRegisteredMapNotificationView;
	private static readonly Dictionary<string, long> LastTrackerEventPulseTicks = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, long> LastCourierLogicPulseTicks = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, CourierSession> _sessions = new Dictionary<string, CourierSession>(StringComparer.OrdinalIgnoreCase);
	private readonly object _sessionLock = new object();
	private volatile HashSet<string> _activeCourierPartyIdsSnapshot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, MobileParty> _courierPartyCache = new Dictionary<string, MobileParty>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _npcDiplomacyLetterSenderCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private PendingCourierFlow _pendingFlow;
	private long _lastCampaignTickUtcTicks;
	private float _npcDiplomacyLetterGlobalCooldownUntilDays;
	private float _nextNpcDiplomacyLetterScanHour;
	private bool _courierReplyWaitTimeLocked;
	private CampaignTimeControlMode _courierReplyWaitPreviousMode = CampaignTimeControlMode.Stop;
	private bool _courierReplyWaitPreviousLock;

	public static CourierDeliveryBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dataStore.IsSaving)
		{
			lock (_sessionLock)
			{
				foreach (KeyValuePair<string, CourierSession> pair in _sessions)
				{
					if (pair.Value == null)
					{
						continue;
					}
					storage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
				}
			}
			storage = CampaignSaveChunkHelper.FlattenStringDictionary(storage, SessionStorageKey, "CourierDelivery");
		}
		dataStore.SyncData(SessionStorageKey, ref storage);
		if (dataStore.IsLoading)
		{
			storage = CampaignSaveChunkHelper.RestoreStringDictionary(storage, "CourierDelivery");
			lock (_sessionLock)
			{
				_sessions.Clear();
				foreach (KeyValuePair<string, string> pair in storage ?? new Dictionary<string, string>())
				{
					try
					{
						CourierSession session = JsonConvert.DeserializeObject<CourierSession>(pair.Value ?? "");
						if (session == null || string.IsNullOrWhiteSpace(session.Id))
						{
							continue;
						}
						NormalizeSession(session);
						ResetReplyGenerationAfterLoad(session, "sync_load");
						_sessions[session.Id] = session;
					}
					catch (Exception ex)
					{
						Log("load session failed key=" + pair.Key + " error=" + ex.Message);
					}
				}
			}
			RebuildCourierRuntimeIndexes();
		}

		string npcLetterStorageJson = null;
		if (dataStore.IsSaving)
		{
			npcLetterStorageJson = JsonConvert.SerializeObject(new NpcDiplomacyLetterStorage
			{
				SenderCooldownUntilDays = _npcDiplomacyLetterSenderCooldownUntilDays,
				GlobalCooldownUntilDays = _npcDiplomacyLetterGlobalCooldownUntilDays,
				NextScanHour = _nextNpcDiplomacyLetterScanHour
			});
		}
		dataStore.SyncData(NpcDiplomacyLetterStorageKey, ref npcLetterStorageJson);
		if (dataStore.IsLoading)
		{
			try
			{
				NpcDiplomacyLetterStorage npcStorage = string.IsNullOrWhiteSpace(npcLetterStorageJson) ? null : JsonConvert.DeserializeObject<NpcDiplomacyLetterStorage>(npcLetterStorageJson);
				_npcDiplomacyLetterSenderCooldownUntilDays = NormalizeFloatDictionary(npcStorage?.SenderCooldownUntilDays);
				_npcDiplomacyLetterGlobalCooldownUntilDays = npcStorage?.GlobalCooldownUntilDays ?? 0f;
				_nextNpcDiplomacyLetterScanHour = npcStorage?.NextScanHour ?? 0f;
			}
			catch (Exception ex)
			{
				Log("load npc diplomacy letter storage failed: " + ex.Message);
				_npcDiplomacyLetterSenderCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
				_npcDiplomacyLetterGlobalCooldownUntilDays = 0f;
				_nextNpcDiplomacyLetterScanHour = 0f;
			}
		}
	}

	public void OnEngineTick()
	{
		try
		{
			CourierLetterInputPopup.ProcessDeferredCloseIfNeeded();
			CourierLetterReplyPopup.ProcessDeferredCloseIfNeeded();
		}
		catch (Exception ex)
		{
			Log("courier letter popup tick failed: " + ex.Message);
		}
		while (MainThreadActions.TryDequeue(out var action))
		{
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Log("main action failed: " + ex);
			}
		}
	}

	public static bool IsCourierInputOpen => _letterInputOpen;

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		try
		{
			Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.courier.delivery");
			_courierHarmony = activeHarmony;
			MethodInfoAccess.PatchDefaultEncounterModel(activeHarmony);
			MethodInfoAccess.PatchCustomPartyComponentBanner(activeHarmony);
			AnimusForgeCourierUiSprites.EnsurePatched(activeHarmony);
			Log("harmony patches registered");
		}
		catch (Exception ex)
		{
			Log("harmony register failed: " + ex);
		}
	}

	public static bool IsCourierParty(MobileParty party)
	{
		try
		{
			if (party == null || Instance == null)
			{
				return false;
			}
			string partyId = (party.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(partyId))
			{
				return false;
			}
			HashSet<string> snapshot = Instance._activeCourierPartyIdsSnapshot;
			if (snapshot != null && snapshot.Contains(partyId))
			{
				return true;
			}
			if (snapshot != null && snapshot.Count > 0)
			{
				return false;
			}
			lock (Instance._sessionLock)
			{
				return Instance._sessions.Values.Any(x => x != null && string.Equals((x.CourierPartyId ?? "").Trim(), partyId, StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
			}
		}
		catch
		{
			return false;
		}
	}

	public static bool IsBanditOrOutlawParty(MobileParty party)
	{
		try
		{
			return party != null && (party.IsBandit || party.MapFaction?.IsBanditFaction == true || party.ActualClan?.IsBanditFaction == true);
		}
		catch
		{
			return false;
		}
	}

	public static bool ShouldShowCourierButtonForExternal(Hero hero, bool informationHidden)
	{
		try
		{
			if (hero == null || hero == Hero.MainHero || hero.CharacterObject?.IsHero != true || informationHidden)
			{
				return false;
			}
			if (hero.IsDead || !hero.IsKnownToPlayer)
			{
				return false;
			}
			if (IsHeroInPlayerPartyForCourier(hero))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroInPlayerPartyForCourier(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return false;
			}
			if (hero == Hero.MainHero || hero.IsHumanPlayerCharacter)
			{
				return true;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}
			if (hero.PartyBelongedTo == mainParty || hero.PartyBelongedToAsPrisoner == mainParty.Party)
			{
				return true;
			}
			CharacterObject character = hero.CharacterObject;
			return CountRosterCharacter(mainParty.MemberRoster, character) > 0 || CountRosterCharacter(mainParty.PrisonRoster, character) > 0;
		}
		catch
		{
			return false;
		}
	}

	public static bool HasActiveCourierForHeroForExternal(Hero hero)
	{
		try
		{
			return Instance != null && Instance.HasActiveCourierForHero(hero);
		}
		catch
		{
			return false;
		}
	}

	public static void OpenCourierFlowForExternal(Hero recipient)
	{
		try
		{
			Instance?.OpenCourierFlow(recipient);
		}
		catch (Exception ex)
		{
			Log("open courier flow failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("信使与邮递打开失败：" + ex.Message, Colors.Red));
		}
	}

	public static bool TrySendNpcDiplomacyLetterToPlayerForExternal(Hero sender, string letterText, out string status)
	{
		status = "";
		try
		{
			if (Instance == null)
			{
				status = "courier_behavior_missing";
				return false;
			}
			return Instance.TryCreateNpcDiplomacyLetterSession(sender, letterText, "external", out status);
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("external npc diplomacy letter failed sender=" + SafeHeroId(sender) + " error=" + ex);
			return false;
		}
	}

	public static bool TrySendNpcLetterToPlayerForExternal(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			if (Instance == null)
			{
				status = "behavior_not_initialized";
				return false;
			}
			return Instance.TryCreateNpcLetterToPlayerSession(sender, letterText, reason, out status);
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("external npc letter failed sender=" + SafeHeroId(sender) + " reason=" + (reason ?? "") + " error=" + ex);
			return false;
		}
	}

	private void OnGameLoadFinished()
	{
		try
		{
			lock (_sessionLock)
			{
				foreach (CourierSession session in _sessions.Values)
				{
					NormalizeSession(session);
					ResetReplyGenerationAfterLoad(session, "game_load_finished");
					MobileParty courier = ResolveCourierParty(session);
					MarkExistingCourierTemporaryShips(session, courier);
					ApplyCourierAiOverrides(courier, "load_restore");
				}
			}
			RebuildCourierRuntimeIndexes();
			Log("game_load_finished active=" + GetActiveSessionCount());
		}
		catch (Exception ex)
		{
			Log("game_load_finished failed: " + ex);
		}
	}

	private void RebuildCourierRuntimeIndexes()
	{
		try
		{
			HashSet<string> partyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			lock (_sessionLock)
			{
				foreach (CourierSession session in _sessions.Values)
				{
					string partyId = (session?.CourierPartyId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(partyId) && !IsTerminalStage(session))
					{
						partyIds.Add(partyId);
					}
				}
				foreach (string cachedId in _courierPartyCache.Keys.ToList())
				{
					if (!partyIds.Contains(cachedId))
					{
						_courierPartyCache.Remove(cachedId);
					}
				}
			}
			_activeCourierPartyIdsSnapshot = partyIds;
		}
		catch (Exception ex)
		{
			Log("runtime index rebuild failed: " + ex.Message);
		}
	}

	private void AddCourierRuntimeIndex(CourierSession session, MobileParty courier = null)
	{
		try
		{
			string partyId = (session?.CourierPartyId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(partyId) || IsTerminalStage(session))
			{
				return;
			}
			HashSet<string> partyIds = new HashSet<string>(_activeCourierPartyIdsSnapshot ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
			{
				partyId
			};
			_activeCourierPartyIdsSnapshot = partyIds;
			if (courier != null)
			{
				lock (_sessionLock)
				{
					_courierPartyCache[partyId] = courier;
				}
			}
		}
		catch
		{
		}
	}

	private void RemoveCourierRuntimeIndex(CourierSession session)
	{
		try
		{
			string partyId = (session?.CourierPartyId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(partyId))
			{
				return;
			}
			HashSet<string> partyIds = new HashSet<string>(_activeCourierPartyIdsSnapshot ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
			partyIds.Remove(partyId);
			_activeCourierPartyIdsSnapshot = partyIds;
			lock (_sessionLock)
			{
				_courierPartyCache.Remove(partyId);
			}
		}
		catch
		{
		}
	}

	private void OpenCourierFlow(Hero recipient)
	{
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			return;
		}
		if (IsHeroInPlayerPartyForCourier(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage("该角色正在你的队伍中，不能通过信使写信。", Colors.Yellow));
			return;
		}
		if (!ShouldShowCourierButtonForExternal(recipient, informationHidden: false))
		{
			InformationManager.DisplayMessage(new InformationMessage("你尚未掌握此人的信息，不能寄信。", Colors.Yellow));
			return;
		}
		if (HasActiveCourierForHero(recipient))
		{
			InformationManager.DisplayMessage(new InformationMessage("已经有一支信使队正在处理发往此人的信件。", Colors.Yellow));
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty?.MemberRoster == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前找不到玩家部队。", Colors.Red));
			return;
		}
		TroopRoster available = BuildSelectableCrewRoster(mainParty.MemberRoster);
		if (available.TotalManCount <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你当前没有可派出的信使成员。", Colors.Yellow));
			return;
		}
		_pendingFlow = new PendingCourierFlow
		{
			Recipient = recipient,
			CrewRoster = null,
			Mode = CourierPayloadMode.Normal
		};
		Log("open crew selection recipient=" + SafeHeroId(recipient) + " available=" + available.TotalManCount);
		PartyScreenHelper.OpenScreenWithDummyRoster(
			available,
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			TroopRoster.CreateDummyTroopRoster(),
			new TextObject("可选信使成员"),
			new TextObject("信使部队"),
			Math.Max(available.TotalManCount, 0),
			Math.Max(1, available.TotalManCount),
			new PartyPresentationDoneButtonConditionDelegate(CrewSelectionDoneCondition),
			new PartyScreenClosedDelegate(OnCrewSelectionClosed),
			new IsTroopTransferableDelegate(CourierCrewTransferableDelegate));
	}

	private bool HasActiveCourierForHero(Hero hero)
	{
		string heroId = SafeHeroId(hero);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null && string.Equals(x.RecipientHeroId ?? "", heroId, StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
		}
	}

	private static Tuple<bool, TextObject> CrewSelectionDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		if (rightMemberRoster == null || rightMemberRoster.TotalManCount <= 0)
		{
			return new Tuple<bool, TextObject>(false, new TextObject("信使部队必须至少 1 人。"));
		}
		return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
	}

	private static bool CourierCrewTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character != null && !character.IsPlayerCharacter && type == PartyScreenLogic.TroopType.Member;
	}

	private void OnCrewSelectionClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		try
		{
			if (fromCancel || _pendingFlow == null)
			{
				ResetPendingFlow("crew_cancel");
				return;
			}
			TroopRoster selected = BuildSelectionRosterFromUi(rightMemberRoster);
			if (selected.TotalManCount <= 0)
			{
				ResetPendingFlow("crew_empty");
				InformationManager.DisplayMessage(new InformationMessage("信使部队必须至少 1 人。", Colors.Yellow));
				return;
			}
			_pendingFlow.CrewRoster = selected;
			_pendingFlow.CrewEntries = BuildCargoEntriesFromRoster(selected, "crew");
			Log("crew selected recipient=" + SafeHeroId(_pendingFlow.Recipient) + " roster=" + RosterSummary(selected));
			ShowCourierModeInquiry();
		}
		catch (Exception ex)
		{
			Log("crew close failed: " + ex);
			ResetPendingFlow("crew_exception");
			InformationManager.DisplayMessage(new InformationMessage("信使部队选择失败：" + ex.Message, Colors.Red));
		}
	}

	private void ShowCourierModeInquiry()
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("mode_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		List<InquiryElement> items = new List<InquiryElement>
		{
			new InquiryElement("normal", "单纯写信", null, true, ""),
			new InquiryElement("give", "发送物品并写信", null, true, ""),
			new InquiryElement("show", "展示物品并写信", null, true, ""),
			new InquiryElement("give_troops", "转移部队并写信", null, true, ""),
			new InquiryElement("give_prisoners", "转移俘虏并写信", null, true, ""),
			new InquiryElement("give_settlements", "转移固定资产并写信", null, true, "")
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"信使与邮递 - " + targetName,
			"当前收件人：" + targetName + "\n请选择寄送方式：",
			items,
			true,
			1,
			1,
			"确定",
			"取消",
			selected =>
			{
				if (selected == null || selected.Count == 0)
				{
					ResetPendingFlow("mode_empty");
					return;
				}
				string id = (selected[0]?.Identifier ?? "").ToString();
				if (id == "give")
				{
					BeginPayloadSelection(CourierPayloadMode.Give);
				}
				else if (id == "show")
				{
					BeginPayloadSelection(CourierPayloadMode.Show);
				}
				else if (id == "give_troops")
				{
					BeginPayloadSelection(CourierPayloadMode.GiveTroops);
				}
				else if (id == "give_prisoners")
				{
					BeginPayloadSelection(CourierPayloadMode.GivePrisoners);
				}
				else if (id == "give_settlements")
				{
					BeginPayloadSelection(CourierPayloadMode.GiveSettlements);
				}
				else
				{
					flow.Mode = CourierPayloadMode.Normal;
					flow.SelectedEntries.Clear();
					ShowLetterInput();
				}
			},
			_ => ResetPendingFlow("mode_cancel"),
			"",
			true), true);
	}

	private void BeginPayloadSelection(CourierPayloadMode mode)
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("payload_no_flow");
			return;
		}
		flow.Mode = mode;
		if (mode == CourierPayloadMode.GiveTroops && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("只有领主才能谈部队转移。", Colors.Yellow));
			ResetPendingFlow("payload_troop_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GivePrisoners && !MyBehavior.IsPartyTransferLordEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("只有领主才能谈俘虏转移。", Colors.Yellow));
			ResetPendingFlow("payload_prisoner_ineligible");
			return;
		}
		if (mode == CourierPayloadMode.GiveSettlements && !MyBehavior.IsSettlementTransferLeaderEligibleForExternal(flow.Recipient, flow.Recipient.CharacterObject))
		{
			InformationManager.DisplayMessage(new InformationMessage("当前收件人没有可接收或可谈的固定资产。", Colors.Yellow));
			ResetPendingFlow("payload_settlement_ineligible");
			return;
		}
		flow.TradeOptions = BuildCourierTradeOptions(flow, mode);
		flow.SelectedEntries.Clear();
		flow.PendingAmountIndex = 0;
		if (flow.TradeOptions.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(BuildEmptyPayloadMessage(mode), Colors.Yellow));
			ResetPendingFlow("payload_empty");
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < flow.TradeOptions.Count; i++)
		{
			CourierTradeOption option = flow.TradeOptions[i];
			string hint = "可用数量: " + Math.Max(0, option.AvailableAmount);
			if (option.PartyEntry != null)
			{
				hint = option.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops
					? $"可用数量: {option.AvailableAmount} | 日薪: {option.PartyEntry.WageDenarsPerDay}第纳尔/天 | 雇佣价: {option.PartyEntry.HirePriceDenarsPerUnit}第纳尔/人"
					: $"可用数量: {option.AvailableAmount} | 购买价: {option.PartyEntry.BuyPriceDenarsPerUnit}第纳尔/人";
			}
			else if (option.SettlementEntry != null)
			{
				hint = $"类型: {(string.IsNullOrWhiteSpace(option.SettlementEntry.TypeLabel) ? "固定资产" : option.SettlementEntry.TypeLabel)} | 每日收益: {Math.Max(0, option.SettlementEntry.DailyIncomeDenars)} 第纳尔 | 一次结清指导价: {Math.Max(0, option.SettlementEntry.GuidePriceDenars)} 第纳尔";
			}
			list.Add(new InquiryElement(i, option.Name + " (×" + Math.Max(1, option.AvailableAmount) + ")", null, true, hint));
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			BuildPayloadTitle(mode, targetName),
			BuildPayloadDescription(mode, targetName),
			list,
			true,
			1,
			list.Count,
			"确定",
			"取消",
			OnPayloadResourcesSelected,
			_ => ResetPendingFlow("payload_cancel"),
			"",
			true), true);
	}

	private void OnPayloadResourcesSelected(List<InquiryElement> selected)
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow == null || selected == null || selected.Count == 0)
		{
			ResetPendingFlow("payload_selected_empty");
			return;
		}
		flow.SelectedEntries.Clear();
		foreach (InquiryElement element in selected)
		{
			int index = -1;
			try
			{
				index = (int)element.Identifier;
			}
			catch
			{
				index = -1;
			}
			if (index < 0 || index >= flow.TradeOptions.Count)
			{
				continue;
			}
			CourierTradeOption option = flow.TradeOptions[index];
			flow.SelectedEntries.Add(new CourierCargoEntry
			{
				Kind = option.Kind,
				Id = option.Id,
				Name = option.Name,
				Amount = option.SettlementEntry != null ? 1 : 0,
				GuidePriceDenars = Math.Max(0, option.GuidePriceDenars),
				IsHero = option.PartyEntry?.IsHero ?? false
			});
		}
		if (flow.SelectedEntries.Count == 0)
		{
			ResetPendingFlow("payload_selected_no_entries");
			return;
		}
		if (flow.Mode == CourierPayloadMode.GiveSettlements)
		{
			ShowLetterInput();
			return;
		}
		ShowPayloadAmountInquiry();
	}

	private void ShowPayloadAmountInquiry()
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow == null)
		{
			ResetPendingFlow("amount_no_flow");
			return;
		}
		if (flow.PendingAmountIndex >= flow.SelectedEntries.Count)
		{
			ShowLetterInput();
			return;
		}
		CourierCargoEntry entry = flow.SelectedEntries[flow.PendingAmountIndex];
		CourierTradeOption option = flow.TradeOptions.FirstOrDefault(x => string.Equals(x.Kind, entry.Kind, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Id ?? "", entry.Id ?? "", StringComparison.OrdinalIgnoreCase));
		int max = Math.Max(0, option?.AvailableAmount ?? 0);
		if (max <= 0)
		{
			flow.PendingAmountIndex++;
			ShowPayloadAmountInquiry();
			return;
		}
		string title = flow.Mode == CourierPayloadMode.Show ? "展示数量" : (flow.Mode == CourierPayloadMode.Give ? "发送数量" : "转移数量");
		string text = $"[{flow.PendingAmountIndex + 1}/{flow.SelectedEntries.Count}] {entry.Name} 最多可填 {max}。\n请输入 1 到 {max} 的整数：";
		InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "确定", "返回", input =>
		{
			if (!int.TryParse(input, out var amount) || amount <= 0 || amount > max)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的数量。", Colors.Yellow));
				ShowPayloadAmountInquiry();
				return;
			}
			entry.Amount = amount;
			flow.PendingAmountIndex++;
			ShowPayloadAmountInquiry();
		}, () => BeginPayloadSelection(flow.Mode)), true);
	}

	private void ShowLetterInput()
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow?.Recipient == null)
		{
			ResetPendingFlow("letter_no_flow");
			return;
		}
		string targetName = flow.Recipient.Name?.ToString() ?? "收件人";
		_letterInputOpen = true;
		bool opened = CourierLetterInputPopup.Show("写给 " + targetName + " 的信", "", "", "", input =>
		{
			_letterInputOpen = false;
			OnLetterConfirmed(input);
		}, () =>
		{
			_letterInputOpen = false;
			ResetPendingFlow("letter_cancel");
		});
		if (!opened)
		{
			InformationManager.ShowTextInquiry(new TextInquiryData("写给 " + targetName + " 的信", "", true, true, "发送", "取消", input =>
			{
				_letterInputOpen = false;
				OnLetterConfirmed(input);
			}, () =>
			{
				_letterInputOpen = false;
				ResetPendingFlow("letter_cancel_fallback");
			}), true);
		}
	}

	private void OnLetterConfirmed(string input)
	{
		PendingCourierFlow flow = _pendingFlow;
		if (flow == null || flow.Recipient == null)
		{
			ResetPendingFlow("confirm_no_flow");
			return;
		}
		if (string.IsNullOrWhiteSpace(input))
		{
			ResetPendingFlow("confirm_empty");
			return;
		}
		try
		{
			CourierSession session = CreateCourierSession(flow, input.Trim());
			if (session == null)
			{
				ResetPendingFlow("confirm_create_null");
				return;
			}
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session);
			Log("session created id=" + session.Id + " recipient=" + session.RecipientHeroId + " party=" + session.CourierPartyId + " mode=" + session.PayloadMode + " entries=" + session.Entries.Count);
			StartCourierReplyGeneration(session, "created_preflight");
			InformationManager.DisplayMessage(new InformationMessage("信使队已出发，正在前往 " + session.RecipientName + "。", Colors.Green));
			ResetPendingFlow("confirm_done");
			ProcessSession(session);
		}
		catch (Exception ex)
		{
			Log("confirm failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("信使出发失败：" + ex.Message, Colors.Red));
			ResetPendingFlow("confirm_exception");
		}
	}

	private CourierSession CreateCourierSession(PendingCourierFlow flow, string letter)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || flow?.Recipient == null || flow.CrewRoster == null || flow.CrewRoster.TotalManCount <= 0)
		{
			throw new InvalidOperationException("信使队数据不完整。");
		}
		string id = NewSessionId();
		TroopRoster emptyMembers = TroopRoster.CreateDummyTroopRoster();
		TroopRoster emptyPrisoners = TroopRoster.CreateDummyTroopRoster();
		float speed = Math.Max(4f, mainParty.Speed) * 4f;
		TextObject name = new TextObject("AnimusForge 信使队");
		MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(mainParty.Position, 0.05f, mainParty.CurrentSettlement, name, Clan.PlayerClan, emptyMembers, emptyPrisoners, Hero.MainHero, "", "", speed, true);
		if (courier == null)
		{
			throw new InvalidOperationException("创建信使队失败。");
		}
		courier.IsVisible = true;
		ApplyCourierMapBannerVisual(courier, "create");
		courier.Party.SetCustomName(new TextObject("信使队 - " + (flow.Recipient.Name?.ToString() ?? "收件人")));
		courier.SetMoveModeHold();
		ApplyCourierAiOverrides(courier, "create");

		CourierSession session = new CourierSession
		{
			Id = id,
			RecipientHeroId = SafeHeroId(flow.Recipient),
			RecipientName = flow.Recipient.Name?.ToString() ?? "",
			CourierPartyId = courier.StringId,
			Stage = CourierStage.Outbound.ToString(),
			PayloadMode = flow.Mode.ToString(),
			LetterText = letter,
			Entries = CloneEntries(flow.SelectedEntries),
			CrewEntries = BuildCargoEntriesFromRoster(flow.CrewRoster, "crew")
		};
		MoveRosterFromMainParty(flow.CrewRoster, courier, "crew");
		AssignCourierLeader(courier);
		PrepareOutgoingPayload(session, courier);
		session.DeliveryFactText = BuildDeliveryFactText(session, delivered: false, flow.Recipient);
		PlayerNotorietyBehavior.NoteCourierSentForExternal(flow.Recipient);
		return session;
	}

	private void PrepareOutgoingPayload(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		if (ParsePayloadMode(session.PayloadMode) == CourierPayloadMode.Show)
		{
			Log("payload show mode staged session=" + session.Id + " entries=" + session.Entries.Count);
			return;
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			if (string.Equals(entry.Kind, "gold", StringComparison.OrdinalIgnoreCase))
			{
				int amount = Math.Min(Math.Max(0, Hero.MainHero?.Gold ?? 0), entry.Amount);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount, true);
				}
				entry.Amount = amount;
				session.EscrowGold += amount;
				Log("escrow gold session=" + session.Id + " amount=" + amount);
			}
			else if (string.Equals(entry.Kind, "item", StringComparison.OrdinalIgnoreCase))
			{
				ItemObject item;
				int moved = MyBehavior.TransferItemsFromRosterByStringId(MobileParty.MainParty?.ItemRoster, courier.ItemRoster, entry.Id, entry.Amount, out item);
				entry.Amount = moved;
				if (item != null)
				{
					entry.Name = item.Name?.ToString() ?? entry.Name;
				}
				Log("escrow item session=" + session.Id + " item=" + entry.Id + " moved=" + moved);
			}
			else if (string.Equals(entry.Kind, "troop", StringComparison.OrdinalIgnoreCase))
			{
				MoveCharacterFromMainMembersToParty(entry.Id, entry.Amount, courier, entry.IsHero);
				Log("escrow troop session=" + session.Id + " troop=" + entry.Id + " amount=" + entry.Amount);
			}
			else if (string.Equals(entry.Kind, "prisoner", StringComparison.OrdinalIgnoreCase))
			{
				MoveCharacterFromMainPrisonersToParty(entry.Id, entry.Amount, courier, entry.IsHero);
				Log("escrow prisoner session=" + session.Id + " troop=" + entry.Id + " amount=" + entry.Amount);
			}
		}
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("CourierDelivery.OnCampaignTick"))
		{
		try
		{
			long now = DateTime.UtcNow.Ticks;
			if (now - _lastCampaignTickUtcTicks < TimeSpan.FromSeconds(CampaignTickThrottleSeconds).Ticks)
			{
				return;
			}
			_lastCampaignTickUtcTicks = now;
			if (!_partyNameplatePatchApplied && !_partyNameplatePatchFailed)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.PatchPartyNameplate"))
				{
					TryPatchPartyNameplateForCourierBanner();
				}
			}
			if (!_mapTrackerProviderPatchApplied && !_mapTrackerProviderPatchFailed)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.PatchMapTrackerProvider"))
				{
					TryPatchMapTrackerProviderForCourierDiagnostics();
				}
			}
			List<CourierSession> snapshot;
			using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.BuildSnapshot"))
			{
				lock (_sessionLock)
				{
					snapshot = _sessions.Values.Where(x => x != null && !IsTerminalStage(x)).ToList();
				}
			}
			foreach (CourierSession session in snapshot)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.ProcessSession"))
				{
					ProcessSession(session);
				}
			}
		}
		catch (Exception ex)
		{
			Log("campaign tick failed: " + ex);
		}
		}
	}

	private void OnHourlyTick()
	{
		try
		{
			TryStartNpcDiplomacyLetterScan();
		}
		catch (Exception ex)
		{
			Log("npc diplomacy letter hourly tick failed: " + ex);
		}
	}

	private void TryStartNpcDiplomacyLetterScan()
	{
		float nowHours = NowHours();
		if (nowHours < _nextNpcDiplomacyLetterScanHour)
		{
			return;
		}
		_nextNpcDiplomacyLetterScanHour = nowHours + NpcDiplomacyLetterScanIntervalHours;
		float nowDays = NowDays();
		if (_npcDiplomacyLetterGlobalCooldownUntilDays > nowDays)
		{
			return;
		}
		if (MBRandom.RandomFloat > NpcDiplomacyLetterSendChance)
		{
			return;
		}
		Hero sender = SelectNpcDiplomacyLetterSender(out string diplomacyKind);
		if (sender == null)
		{
			return;
		}
		string letterText = BuildNpcDiplomacyLetterText(sender, diplomacyKind);
		if (!TryCreateNpcDiplomacyLetterSession(sender, letterText, "hourly_scan:" + diplomacyKind, out string status))
		{
			Log("npc diplomacy letter skipped sender=" + SafeHeroId(sender) + " kind=" + (diplomacyKind ?? "") + " status=" + (status ?? ""));
		}
	}

	private Hero SelectNpcDiplomacyLetterSender(out string diplomacyKind)
	{
		diplomacyKind = "";
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated || Hero.MainHero != playerKingdom.RulingClan?.Leader)
			{
				return null;
			}
			float nowDays = NowDays();
			List<Tuple<Hero, string, float>> candidates = new List<Tuple<Hero, string, float>>();
			foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom == null || kingdom.IsEliminated || kingdom == playerKingdom)
				{
					continue;
				}
				Hero leader = kingdom.RulingClan?.Leader;
				if (!CanNpcKingSendDiplomacyLetter(leader, playerKingdom, out string reason))
				{
					continue;
				}
				string senderId = SafeHeroId(leader);
				if (!string.IsNullOrWhiteSpace(senderId)
					&& _npcDiplomacyLetterSenderCooldownUntilDays.TryGetValue(senderId, out float untilDays)
					&& untilDays > nowDays)
				{
					continue;
				}
				if (HasActiveInboundCourierFromSender(leader))
				{
					continue;
				}
				if (!TryResolveNpcDiplomacyLetterKind(kingdom, playerKingdom, out string kind, out float urgency))
				{
					continue;
				}
				candidates.Add(new Tuple<Hero, string, float>(leader, kind, urgency + MBRandom.RandomFloat * 5f));
			}
			Tuple<Hero, string, float> selected = candidates.OrderByDescending(x => x.Item3).FirstOrDefault();
			if (selected == null)
			{
				return null;
			}
			diplomacyKind = selected.Item2;
			return selected.Item1;
		}
		catch (Exception ex)
		{
			Log("select npc diplomacy letter sender failed: " + ex.Message);
			return null;
		}
	}

	private bool CanNpcKingSendDiplomacyLetter(Hero sender, Kingdom playerKingdom, out string reason)
	{
		reason = "";
		try
		{
			if (sender == null || sender == Hero.MainHero || sender.IsDead)
			{
				reason = "sender_invalid";
				return false;
			}
			Kingdom senderKingdom = sender.Clan?.Kingdom;
			if (senderKingdom == null || senderKingdom.IsEliminated)
			{
				reason = "sender_no_kingdom";
				return false;
			}
			if (sender != senderKingdom.RulingClan?.Leader)
			{
				reason = "sender_not_king";
				return false;
			}
			if (playerKingdom == null || playerKingdom.IsEliminated || Hero.MainHero != playerKingdom.RulingClan?.Leader)
			{
				reason = "player_not_king";
				return false;
			}
			if (senderKingdom == playerKingdom)
			{
				reason = "same_kingdom";
				return false;
			}
			if (sender.IsPrisoner || sender.IsFugitive || sender.PartyBelongedToAsPrisoner != null)
			{
				reason = "sender_unavailable";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out _, out _))
			{
				reason = "sender_location_missing";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return false;
		}
	}

	private static bool TryResolveNpcDiplomacyLetterKind(Kingdom senderKingdom, Kingdom playerKingdom, out string kind, out float urgency)
	{
		kind = "";
		urgency = 0f;
		try
		{
			if (senderKingdom == null || playerKingdom == null || senderKingdom == playerKingdom)
			{
				return false;
			}
			if (FactionManager.IsAtWarAgainstFaction(senderKingdom, playerKingdom))
			{
				kind = "MAKE_PEACE";
				urgency = 80f;
				return true;
			}
			if (HasCommonEnemyForDiplomacyLetter(senderKingdom, playerKingdom))
			{
				kind = "FORM_ALLIANCE";
				urgency = 62f;
				return true;
			}
			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (!BannerlordApiCompat.HasTradeAgreement(tradeBeh, senderKingdom, playerKingdom))
			{
				kind = "MAKE_TRADE";
				urgency = 48f;
				return true;
			}
			return false;
		}
		catch
		{
			kind = "";
			urgency = 0f;
			return false;
		}
	}

	private static bool HasCommonEnemyForDiplomacyLetter(Kingdom first, Kingdom second)
	{
		try
		{
			if (first == null || second == null)
			{
				return false;
			}
			foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom == null || kingdom.IsEliminated || kingdom == first || kingdom == second)
				{
					continue;
				}
				if (FactionManager.IsAtWarAgainstFaction(first, kingdom) && FactionManager.IsAtWarAgainstFaction(second, kingdom))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildNpcDiplomacyLetterText(Hero sender, string diplomacyKind)
	{
		Kingdom senderKingdom = sender?.Clan?.Kingdom;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		string senderName = sender?.Name?.ToString() ?? "NPC";
		string senderKingdomName = senderKingdom?.Name?.ToString() ?? senderKingdom?.StringId ?? "unknown kingdom";
		string playerKingdomName = playerKingdom?.Name?.ToString() ?? playerKingdom?.StringId ?? "your kingdom";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "player";
		}
		string kind = (diplomacyKind ?? "").Trim().ToUpperInvariant();
		if (kind == "MAKE_PEACE")
		{
			return senderName + "致" + playerName + "：\n\n我们两国，" + senderKingdomName + "与" + playerKingdomName + "，继续流血只会削弱各自的王冠。我愿意讨论议和，包括白和平、贡金方向和期限。若你愿意给出条件，请回信或当面谈判。";
		}
		if (kind == "FORM_ALLIANCE")
		{
			return senderName + "致" + playerName + "：\n\n" + senderKingdomName + "与" + playerKingdomName + "面对共同威胁。若我们结盟，双方都能从中获益。我愿意听取你的条件，也可以讨论期限和互相支持的边界。";
		}
		if (kind == "MAKE_TRADE")
		{
			return senderName + "致" + playerName + "：\n\n" + senderKingdomName + "愿与" + playerKingdomName + "建立贸易协议，让商队与市场都获得更稳定的道路。我愿意讨论协议期限和附带条件。";
		}
		return senderName + "致" + playerName + "：\n\n我希望以国王身份与你讨论两国外交。若你愿意，请回信说明你的条件。";
	}

	private bool TryCreateNpcDiplomacyLetterSession(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (!CanNpcKingSendDiplomacyLetter(sender, playerKingdom, out status))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(letterText))
			{
				status = "empty_letter";
				return false;
			}
			if (HasActiveInboundCourierFromSender(sender))
			{
				status = "active_inbound_exists";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out CampaignVec2 startPosition, out Settlement startSettlement))
			{
				status = "sender_location_missing";
				return false;
			}
			string id = NewSessionId();
			TroopRoster members = TroopRoster.CreateDummyTroopRoster();
			TroopRoster prisoners = TroopRoster.CreateDummyTroopRoster();
			CharacterObject messenger = ResolveNpcCourierMessengerTroop(sender);
			if (messenger != null)
			{
				members.AddToCounts(messenger, 1, false, 0, 0, true, -1);
			}
			float baseSpeed = Math.Max(4f, sender.PartyBelongedTo?.Speed ?? MobileParty.MainParty?.Speed ?? 4f);
			Clan ownerClan = sender.Clan ?? sender.Clan?.Kingdom?.RulingClan ?? Clan.PlayerClan;
			TextObject name = new TextObject("AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound");
			courier.Party.SetCustomName(new TextObject("信使队 - " + (sender.Name?.ToString() ?? "NPC")));
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "create_inbound");
			CourierSession session = new CourierSession
			{
				Id = id,
				Direction = CourierDirectionInboundToPlayer,
				SenderHeroId = SafeHeroId(sender),
				SenderName = sender.Name?.ToString() ?? "",
				RecipientHeroId = SafeHeroId(Hero.MainHero),
				RecipientName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "",
				CourierPartyId = courier.StringId,
				Stage = CourierStage.Outbound.ToString(),
				PayloadMode = CourierPayloadMode.Normal.ToString(),
				LetterText = letterText.Trim()
			};
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: false, sender);
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session, courier);
			StartInboundLetterGeneration(session, "created_preflight");
			float nowDays = NowDays();
			string senderId = SafeHeroId(sender);
			if (!string.IsNullOrWhiteSpace(senderId))
			{
				_npcDiplomacyLetterSenderCooldownUntilDays[senderId] = nowDays + NpcDiplomacyLetterSenderCooldownDays;
			}
			_npcDiplomacyLetterGlobalCooldownUntilDays = nowDays + NpcDiplomacyLetterGlobalCooldownDays;
			InformationManager.DisplayMessage(new InformationMessage((sender.Name?.ToString() ?? "NPC") + "正在写外交信，并已派出信使送来。", Colors.Green));
			Log("npc diplomacy letter session created id=" + session.Id + " sender=" + session.SenderHeroId + " party=" + session.CourierPartyId + " reason=" + (reason ?? ""));
			ProcessSession(session);
			status = "created";
			return true;
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("create npc diplomacy letter failed sender=" + SafeHeroId(sender) + " error=" + ex);
			return false;
		}
	}

	private bool TryCreateNpcLetterToPlayerSession(Hero sender, string letterText, string reason, out string status)
	{
		status = "";
		try
		{
			if (sender == null || sender == Hero.MainHero || sender.IsDead || sender.IsPrisoner || sender.IsFugitive || sender.PartyBelongedToAsPrisoner != null)
			{
				status = "sender_unavailable";
				return false;
			}
			if (string.IsNullOrWhiteSpace(letterText))
			{
				status = "empty_letter";
				return false;
			}
			if (HasActiveInboundCourierFromSender(sender))
			{
				status = "active_inbound_exists";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out CampaignVec2 startPosition, out Settlement startSettlement))
			{
				status = "sender_location_missing";
				return false;
			}
			string id = NewSessionId();
			TroopRoster members = TroopRoster.CreateDummyTroopRoster();
			TroopRoster prisoners = TroopRoster.CreateDummyTroopRoster();
			CharacterObject messenger = ResolveNpcCourierMessengerTroop(sender);
			if (messenger != null)
			{
				members.AddToCounts(messenger, 1, false, 0, 0, true, -1);
			}
			float baseSpeed = Math.Max(4f, sender.PartyBelongedTo?.Speed ?? MobileParty.MainParty?.Speed ?? 4f);
			Clan ownerClan = sender.Clan ?? sender.Clan?.Kingdom?.RulingClan ?? Clan.PlayerClan;
			TextObject name = new TextObject("AnimusForge NPC Courier");
			MobileParty courier = CustomPartyComponent.CreateCustomPartyWithTroopRoster(startPosition, 0.05f, startSettlement, name, ownerClan, members, prisoners, sender, "", "", baseSpeed * 4f, true);
			if (courier == null)
			{
				status = "create_party_failed";
				return false;
			}
			courier.IsVisible = true;
			ApplyCourierMapBannerVisual(courier, "create_inbound_generic");
			courier.Party.SetCustomName(new TextObject("信使队 - " + (sender.Name?.ToString() ?? "NPC")));
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "create_inbound_generic");
			CourierSession session = new CourierSession
			{
				Id = id,
				Direction = CourierDirectionInboundToPlayer,
				SenderHeroId = SafeHeroId(sender),
				SenderName = sender.Name?.ToString() ?? "",
				RecipientHeroId = SafeHeroId(Hero.MainHero),
				RecipientName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "",
				CourierPartyId = courier.StringId,
				Stage = CourierStage.Outbound.ToString(),
				PayloadMode = CourierPayloadMode.Normal.ToString(),
				LetterText = letterText.Trim(),
				ReplyGenerated = true
			};
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: false, sender);
			lock (_sessionLock)
			{
				_sessions[session.Id] = session;
			}
			AddCourierRuntimeIndex(session, courier);
			InformationManager.DisplayMessage(new InformationMessage((sender.Name?.ToString() ?? "NPC") + "已派出信使送来信件。", Colors.Green));
			Log("npc generic letter session created id=" + session.Id + " sender=" + session.SenderHeroId + " party=" + session.CourierPartyId + " reason=" + (reason ?? ""));
			ProcessSession(session);
			status = "created";
			return true;
		}
		catch (Exception ex)
		{
			status = "exception:" + ex.Message;
			Log("create npc generic letter failed sender=" + SafeHeroId(sender) + " reason=" + (reason ?? "") + " error=" + ex);
			return false;
		}
	}

	private bool HasActiveInboundCourierFromSender(Hero sender)
	{
		string senderId = SafeHeroId(sender);
		if (string.IsNullOrWhiteSpace(senderId))
		{
			return false;
		}
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null
				&& IsInboundToPlayer(x)
				&& !IsTerminalStage(x)
				&& string.Equals((x.SenderHeroId ?? "").Trim(), senderId, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static bool TryGetNpcCourierStart(Hero sender, out CampaignVec2 position, out Settlement settlement)
	{
		position = CampaignVec2.Invalid;
		settlement = null;
		try
		{
			MobileParty party = sender?.PartyBelongedTo;
			if (party != null && party.IsActive)
			{
				position = party.Position;
				settlement = party.CurrentSettlement;
				return IsValidCampaignPosition(position);
			}
			settlement = sender?.CurrentSettlement ?? sender?.StayingInSettlement;
			if (settlement != null)
			{
				position = settlement.GatePosition;
				return IsValidCampaignPosition(position);
			}
		}
		catch
		{
		}
		return false;
	}

	private static CharacterObject ResolveNpcCourierMessengerTroop(Hero sender)
	{
		try
		{
			CharacterObject troop = sender?.Culture?.BasicTroop;
			if (troop != null && !troop.IsHero)
			{
				return troop;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject troop = sender?.Clan?.Culture?.BasicTroop;
			if (troop != null && !troop.IsHero)
			{
				return troop;
			}
		}
		catch
		{
		}
		try
		{
			return Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>()?.FirstOrDefault(x => x != null && x.IsBasicTroop && x.IsSoldier && !x.IsHero);
		}
		catch
		{
			return null;
		}
	}

	private void ProcessSession(CourierSession session)
	{
		if (session == null || IsTerminalStage(session))
		{
			return;
		}
		NormalizeSession(session);
		CourierStage stage = ParseStage(session.Stage);
		MobileParty courier = ResolveCourierParty(session);
		if (courier == null || !courier.IsActive)
		{
			HandleCourierMissing(session);
			return;
		}
		ApplyCourierAiOverrides(courier, "tick");
		Hero recipient = ResolveRecipient(session);
		LogCourierStatusVerbose("tick:" + session.Id, BuildCourierStatusSnapshot(session, courier, recipient, "tick"), 6.0);
		if (IsInboundToPlayer(session))
		{
			ProcessInboundToPlayerSession(session, courier);
			return;
		}
		if (!session.DeliveryApplied && recipient != null && !recipient.IsDead && !session.ReplyGenerated && !session.ReplyGenerationStarted)
		{
			StartCourierReplyGeneration(session, "outbound_preflight");
		}
		if (stage == CourierStage.GeneratingReply)
		{
			if (session.DeliveryApplied && (recipient == null || recipient.IsDead))
			{
				EndCourierReplyWaitPause(session, "recipient_invalid_after_delivery");
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				session.Stage = CourierStage.Returning.ToString();
				RouteToSender(session, courier);
				return;
			}
			if (session.ReplyGenerated)
			{
				CommitGeneratedReplyAtRecipient(session, recipient);
				EndCourierReplyWaitPause(session, "reply_generated");
				session.Stage = CourierStage.Returning.ToString();
				RouteToSender(session, courier);
				return;
			}
			if (!session.ReplyGenerationStarted)
			{
				StartCourierReplyGeneration(session, "resume_or_tick");
			}
			ShowCourierReplyWaitPopupAndPause(session, recipient);
			MaintainReplyWaitAtRecipient(session, courier, recipient);
			return;
		}
		if ((stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient) && recipient == null)
		{
			LogCourierStatusVerbose("target_unresolved:" + session.Id, "target_unresolved session=" + session.Id + " reason=recipient_null " + BuildCourierStatusSnapshot(session, courier, recipient, "target_unresolved"), 5.0);
			RouteToSafeSettlement(session, courier, "recipient_unresolved");
			return;
		}
		if ((stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient) && recipient != null && recipient.IsDead)
		{
			Log("recipient dead before delivery, returning and refunding session=" + session.Id + " recipient=" + SafeHeroId(recipient));
			session.Stage = CourierStage.Returning.ToString();
			session.DeliveryApplied = false;
			session.RecipientWaitReason = "";
			EndCourierReplyWaitPause(session, "recipient_dead_before_delivery");
			RouteToSender(session, courier);
			return;
		}
		if (stage == CourierStage.Outbound || stage == CourierStage.WaitingRecipient)
		{
			if (HandleRecipientUnavailableStatus(session, courier, recipient))
			{
				return;
			}
			if (TryGetRecipientTarget(recipient, out var targetParty, out var targetSettlement))
			{
				session.Stage = CourierStage.Outbound.ToString();
				ClearRecipientWaitReasonIfNeeded(session, "target_resolved");
				LogCourierStatusVerbose("target_resolved:" + session.Id, "target_resolved session=" + session.Id + " " + DescribeRecipientTarget(recipient, targetParty, targetSettlement), 3.0);
				if (IsAtRecipient(courier, targetParty, targetSettlement))
				{
					DeliverToRecipient(session, courier, recipient);
					return;
				}
				RouteToRecipient(session, courier, targetParty, targetSettlement);
				return;
			}
			session.Stage = CourierStage.WaitingRecipient.ToString();
			if (!IsBlockingRecipientWaitReason(session.RecipientWaitReason))
			{
				SetRecipientWaitReason(session, "unresolved", "target_unresolved");
			}
			LogCourierStatusVerbose("target_unresolved:" + session.Id, "target_unresolved session=" + session.Id + " reason=no_party_or_settlement " + DescribeHero(recipient) + " courier=" + DescribeMobileParty(courier), 5.0);
			RouteToSafeSettlement(session, courier, "recipient_wait_respawn");
			return;
		}
		if (stage == CourierStage.Returning || stage == CourierStage.WaitingSender)
		{
			MobileParty senderParty = MobileParty.MainParty;
			if (senderParty == null || !senderParty.IsActive)
			{
				session.Stage = CourierStage.WaitingSender.ToString();
				RouteToSafeSettlement(session, courier, "sender_wait_respawn");
				return;
			}
			session.Stage = CourierStage.Returning.ToString();
			if (IsAtSender(courier, senderParty))
			{
				CompleteReturn(session, courier, recipient);
				return;
			}
			RouteToSender(session, courier);
		}
	}

	private void ProcessInboundToPlayerSession(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null || !mainParty.IsActive)
		{
			session.Stage = CourierStage.WaitingSender.ToString();
			RouteToSafeSettlement(session, courier, "player_wait_respawn");
			return;
		}
		if (!session.ReplyGenerated && !session.ReplyGenerationStarted)
		{
			StartInboundLetterGeneration(session, "inbound_tick");
		}
		if (IsAtSender(courier, mainParty))
		{
			DeliverInboundLetterToPlayer(session, courier);
			return;
		}
		session.Stage = CourierStage.Outbound.ToString();
		RouteToSender(session, courier);
	}

	private void DeliverInboundLetterToPlayer(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		Hero sender = ResolveSender(session);
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		if (!session.ReplyGenerated)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
			ShowCourierReplyWaitPopupAndPause(session, sender);
			StartInboundLetterGeneration(session, "delivered");
			HoldInboundCourierAtPlayer(session, courier);
			return;
		}
		if (session.ReplyWaitPopupShown || _courierReplyWaitTimeLocked)
		{
			EndCourierReplyWaitPause(session, "inbound_letter_generated");
		}
		string letter = (session.LetterText ?? "").Trim();
		if (!session.DeliveryApplied)
		{
			session.DeliveryApplied = true;
			session.DeliveryFactText = BuildInboundDeliveryFactText(session, delivered: true, sender);
			string historyLine = "【来信】" + senderName + "通过信使写道：" + letter;
			MyBehavior.AppendExternalDialogueHistory(sender, null, historyLine, session.DeliveryFactText);
			ShoutBehavior.RecordNativeConversationNpcLineForExternal(sender, sender?.CharacterObject, senderName, historyLine);
			Log("inbound delivered session=" + session.Id + " sender=" + SafeHeroId(sender) + " factLen=" + (session.DeliveryFactText ?? "").Length);
		}
		string visibleLetter = StripCourierActionTags(letter);
		MainThreadActions.Enqueue(() =>
		{
			InformationManager.ShowInquiry(new InquiryData("信使送来外交信", senderName + "写道：\n\n" + visibleLetter, true, false, "知道了", "", null, null), true);
		});
		CompleteAndDestroyCourier(session, courier);
	}

	private void DeliverToRecipient(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null || recipient == null)
		{
			return;
		}
		if (!session.DeliveryApplied)
		{
			ApplyDeliveryPayload(session, courier, recipient);
			session.DeliveryApplied = true;
			session.DeliveryFactText = BuildDeliveryFactText(session, delivered: true, recipient);
			string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, "【来信】" + playerName + "通过信使写道：" + session.LetterText, null, session.DeliveryFactText);
			Log("delivered session=" + session.Id + " recipient=" + SafeHeroId(recipient) + " factLen=" + (session.DeliveryFactText ?? "").Length);
		}
		if (!session.ReplyGenerated)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
			ShowCourierReplyWaitPopupAndPause(session, recipient);
			StartCourierReplyGeneration(session, "delivered");
			MaintainReplyWaitAtRecipient(session, courier, recipient);
			return;
		}
		CommitGeneratedReplyAtRecipient(session, recipient);
		EndCourierReplyWaitPause(session, "delivered_reply_ready");
		session.Stage = CourierStage.Returning.ToString();
		RouteToSender(session, courier);
	}

	private void StartCourierReplyGeneration(CourierSession session, string reason)
	{
		if (session == null || session.ReplyGenerated || session.ReplyGenerationStarted)
		{
			return;
		}
		if (session.DeliveryApplied)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
		}
		session.ReplyGenerationStarted = true;
		Log("reply generation queued session=" + session.Id + " reason=" + (reason ?? ""));
		_ = Task.Run(() => GenerateNpcReplyAsync(session.Id));
	}

	private void StartInboundLetterGeneration(CourierSession session, string reason)
	{
		if (session == null || !IsInboundToPlayer(session) || session.ReplyGenerated || session.ReplyGenerationStarted)
		{
			return;
		}
		session.ReplyGenerationStarted = true;
		Log("inbound letter generation queued session=" + session.Id + " reason=" + (reason ?? ""));
		_ = Task.Run(() => GenerateInboundNpcLetterAsync(session.Id));
	}

	private void HoldInboundCourierAtPlayer(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		string key = "inbound_letter_wait:" + session.Id;
		if (ShouldRefreshRoute(session, key, courier, AiBehavior.Hold))
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "inbound_letter_wait_hold");
			LogVerbose("inbound_letter_wait_hold:" + session.Id, "inbound letter wait hold session=" + session.Id, 5.0);
		}
	}

	private void MaintainReplyWaitAtRecipient(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null)
		{
			return;
		}
		if (recipient != null && TryGetRecipientTarget(recipient, out var targetParty, out var targetSettlement) && !IsAtRecipient(courier, targetParty, targetSettlement))
		{
			RouteToRecipient(session, courier, targetParty, targetSettlement);
			return;
		}
		string key = BuildReplyWaitRouteKey(courier, recipient);
		if (ShouldRefreshRoute(session, key, courier, AiBehavior.Hold))
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "reply_wait_hold");
			LogVerbose("reply_wait_hold:" + session.Id, "reply wait hold session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private static string BuildReplyWaitRouteKey(MobileParty courier, Hero recipient)
	{
		string recipientId = SafeHeroId(recipient);
		CampaignVec2 position = courier?.Position ?? MobileParty.MainParty?.Position ?? default;
		int x = (int)MathF.Round(position.X * 2f);
		int y = (int)MathF.Round(position.Y * 2f);
		return "reply_wait:" + recipientId + ":" + x + ":" + y;
	}

	private async Task GenerateNpcReplyAsync(string sessionId)
	{
		CourierSession session = null;
		try
		{
			lock (_sessionLock)
			{
				_sessions.TryGetValue(sessionId ?? "", out session);
			}
			if (session == null)
			{
				return;
			}
			if (IsTerminalStage(session))
			{
				return;
			}
			Hero recipient = ResolveRecipient(session);
			if (recipient == null || recipient.IsDead)
			{
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "reply_generated_recipient_invalid"));
				return;
			}
			Log("llm main start session=" + session.Id + " recipient=" + SafeHeroId(recipient));
			string extraFact = BuildDeliveryFactText(session, delivered: true, recipient);
			List<string> preprocessRuleHits = MyBehavior.RunCourierRulePreprocessForExternal(recipient, session.LetterText, extraFact, recipient.CharacterObject, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds);
			MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(recipient, session.LetterText, extraFact, recipient.Culture?.StringId ?? "neutral", hasAnyHero: true, targetCharacter: recipient.CharacterObject, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds, forcedPreprocessRuleIds: preprocessRuleHits);
			List<string> selectedRuleHits = MergeCourierSelectedRuleIds(preprocessRuleHits, ctx?.PreprocessRuleIds);
			selectedRuleHits = ExcludeCourierSelectedRuleIds(selectedRuleHits, CourierExcludedRuleIds);
			string extras = FilterCourierInjectedRuleBlocks(ctx?.Extras ?? "", selectedRuleHits, CourierExcludedRuleIds);
			extras = AppendCourierPlayerRecentActionsIfSelected(extras, recipient, selectedRuleHits);
			string historyText = MyBehavior.BuildHistoryContextForExternal(recipient, 24, session.LetterText, extraFact);
			List<ConversationMessage> persistentMemoryRoleMessages = MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(recipient, -1, includeCurrentActiveSceneSession: false);
			List<object> messages = BuildCourierReplyMessages(recipient, session, extras, extraFact, historyText, persistentMemoryRoleMessages);
			ShoutNetwork.RecordPrimaryRequestBodyForTokenStats(messages, MainReplyMaxTokens, "courier_reply_preflight");
			string output = await ShoutNetwork.CallApiWithMessages(messages, MainReplyMaxTokens);
			if (IsTerminalStage(session))
			{
				return;
			}
			string reply = CleanNpcReply(output);
			if (LooksLikeApiError(reply))
			{
				Log("llm main failed session=" + session.Id + " output=" + reply);
				reply = "";
			}
			if (string.IsNullOrWhiteSpace(reply))
			{
				session.ReplyText = "";
				session.ReplyPostprocessedText = "";
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				Log("npc no reply session=" + session.Id);
				MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "reply_generated_empty"));
				return;
			}
			bool duelInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "duel");
			bool rewardInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "reward");
			bool loanInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "loan");
			bool lordsHallInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "lords_hall_access");
			bool meetingReleaseInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "encounter_release_player");
			bool vanillaIssueInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "vanilla_issue");
			bool heroJoinPartyInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "hero_join_party");
			bool sceneMechanismInjected = false;
			bool partyTransferInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "party_transfer");
			bool settlementTransferInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "settlement_transfer");
			bool voteDealInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "vote_deal");
			bool diplomacyInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "diplomacy");
			bool worldMapPartyCommandInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "worldmap_party_command");
			bool kingdomServiceInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_service");
			bool kingdomVassalageRuleBlockInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_vassalage");
			bool kingdomAnnexationRuleBlockInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_annexation");
			bool kingdomVassalageSelected = HasPreprocessRuleHit(selectedRuleHits, "kingdom_vassalage");
			bool kingdomAnnexationSelected = HasPreprocessRuleHit(selectedRuleHits, "kingdom_annexation");
			bool kingdomVassalageInjected = kingdomVassalageRuleBlockInjected || kingdomVassalageSelected;
			bool kingdomAnnexationInjected = kingdomAnnexationRuleBlockInjected || kingdomAnnexationSelected;
			Log("postprocess setup chain=courier session=" + session.Id
				+ " selectedRuleHits=" + ((selectedRuleHits == null || selectedRuleHits.Count == 0) ? "(none)" : string.Join(",", selectedRuleHits))
				+ " kingdom_vassalage_selected=" + kingdomVassalageSelected
				+ " kingdom_annexation_selected=" + kingdomAnnexationSelected
				+ " kingdom_vassalage_block=" + kingdomVassalageRuleBlockInjected
				+ " kingdom_annexation_block=" + kingdomAnnexationRuleBlockInjected
				+ " kingdomVassalageInjected=" + kingdomVassalageInjected
				+ " kingdomAnnexationInjected=" + kingdomAnnexationInjected);
			string postprocessed = ShoutBehavior.RunCourierActionPostprocessForExternal(recipient, recipient.CharacterObject, recipient.Name?.ToString() ?? "NPC", session.LetterText, historyText, reply, duelInjected, rewardInjected, loanInjected, kingdomServiceInjected, lordsHallInjected, meetingReleaseInjected, vanillaIssueInjected, heroJoinPartyInjected, sceneMechanismInjected, partyTransferInjected, settlementTransferInjected, voteDealInjected, diplomacyInjected, worldMapPartyCommandInjected, preprocessRuleHits: selectedRuleHits, entityPostprocessContext: ctx?.EntityPostprocessContext, forceLooseWeeklyMemoryMaterialSession: true, kingdomVassalageRuleInjected: kingdomVassalageInjected, kingdomAnnexationRuleInjected: kingdomAnnexationInjected, chainName: "courier");
			string replyPostprocessed = string.IsNullOrWhiteSpace(postprocessed) ? reply : postprocessed;
			session.ReplyText = reply;
			session.ReplyPostprocessedText = replyPostprocessed;
			session.ReplyGenerated = true;
			session.ReplyGenerationStarted = false;
			Log("llm main done session=" + session.Id + " replyLen=" + reply.Length + " postLen=" + (session.ReplyPostprocessedText ?? "").Length + " preprocessHits=" + ((selectedRuleHits == null || selectedRuleHits.Count == 0) ? "(none)" : string.Join(",", selectedRuleHits)) + " duel=" + duelInjected + " reward=" + rewardInjected + " loan=" + loanInjected + " kingdom=" + kingdomServiceInjected + " kingdomVassalage=" + kingdomVassalageInjected + " kingdomAnnexation=" + kingdomAnnexationInjected + " lordsHall=" + lordsHallInjected + " meetingRelease=" + meetingReleaseInjected + " vanillaIssue=" + vanillaIssueInjected + " heroJoin=" + heroJoinPartyInjected + " sceneMechanism=" + sceneMechanismInjected + " partyTransfer=" + partyTransferInjected + " settlementTransfer=" + settlementTransferInjected + " voteDeal=" + voteDealInjected + " diplomacy=" + diplomacyInjected + " worldMap=" + worldMapPartyCommandInjected);
			MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "reply_generated"));
		}
		catch (Exception ex)
		{
			Log("generate reply failed session=" + sessionId + " error=" + ex);
			if (session != null && !IsTerminalStage(session))
			{
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "reply_generation_failed"));
			}
		}
	}

	private async Task GenerateInboundNpcLetterAsync(string sessionId)
	{
		CourierSession session = null;
		try
		{
			lock (_sessionLock)
			{
				_sessions.TryGetValue(sessionId ?? "", out session);
			}
			if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session))
			{
				return;
			}
			Hero sender = ResolveSender(session);
			string fallbackLetter = NormalizeInboundLetterText(session.LetterText, session, sender);
			if (sender == null || sender.IsDead)
			{
				session.LetterText = fallbackLetter;
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "inbound_letter_generated_sender_invalid"));
				return;
			}
			string seed = string.IsNullOrWhiteSpace(session.LetterText) ? fallbackLetter : session.LetterText.Trim();
			Log("inbound letter llm start session=" + session.Id + " sender=" + SafeHeroId(sender));
			string extraFact = BuildInboundDeliveryFactText(session, delivered: false, sender);
			List<string> preprocessRuleHits = MyBehavior.RunCourierRulePreprocessForExternal(sender, seed, extraFact, sender.CharacterObject, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds);
			MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(sender, seed, extraFact, sender.Culture?.StringId ?? "neutral", hasAnyHero: true, targetCharacter: sender.CharacterObject, targetAgentIndex: -1, excludedRuleIds: CourierExcludedRuleIds, forcedPreprocessRuleIds: preprocessRuleHits);
			List<string> selectedRuleHits = MergeCourierSelectedRuleIds(preprocessRuleHits, ctx?.PreprocessRuleIds);
			selectedRuleHits = ExcludeCourierSelectedRuleIds(selectedRuleHits, CourierExcludedRuleIds);
			string extras = FilterCourierInjectedRuleBlocks(ctx?.Extras ?? "", selectedRuleHits, CourierExcludedRuleIds);
			extras = AppendCourierPlayerRecentActionsIfSelected(extras, sender, selectedRuleHits);
			string historyText = MyBehavior.BuildHistoryContextForExternal(sender, 24, seed, extraFact);
			List<ConversationMessage> persistentMemoryRoleMessages = MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(sender, -1, includeCurrentActiveSceneSession: false);
			List<object> messages = BuildInboundNpcLetterMessages(sender, session, seed, extras, extraFact, historyText, persistentMemoryRoleMessages);
			ShoutNetwork.RecordPrimaryRequestBodyForTokenStats(messages, MainReplyMaxTokens, "courier_inbound_letter_preflight");
			string output = await ShoutNetwork.CallApiWithMessages(messages, MainReplyMaxTokens);
			if (IsTerminalStage(session))
			{
				return;
			}
			string letter = NormalizeInboundLetterText(output, session, sender);
			if (LooksLikeApiError(letter))
			{
				Log("inbound letter llm failed session=" + session.Id + " output=" + letter);
				letter = fallbackLetter;
			}
			if (string.IsNullOrWhiteSpace(letter))
			{
				letter = fallbackLetter;
			}
			session.LetterText = letter;
			session.ReplyGenerated = true;
			session.ReplyGenerationStarted = false;
			Log("inbound letter llm done session=" + session.Id + " letterLen=" + letter.Length + " preprocessHits=" + ((selectedRuleHits == null || selectedRuleHits.Count == 0) ? "(none)" : string.Join(",", selectedRuleHits)));
			MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "inbound_letter_generated"));
		}
		catch (Exception ex)
		{
			Log("generate inbound letter failed session=" + sessionId + " error=" + ex);
			if (session != null && !IsTerminalStage(session))
			{
				session.LetterText = NormalizeInboundLetterText(session.LetterText, session, ResolveSender(session));
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				MainThreadActions.Enqueue(() => ProcessSessionById(sessionId, "inbound_letter_generation_failed"));
			}
		}
	}

	private void ProcessSessionById(string sessionId, string reason)
	{
		try
		{
			CourierSession session = null;
			lock (_sessionLock)
			{
				_sessions.TryGetValue(sessionId ?? "", out session);
			}
			if (session == null || IsTerminalStage(session))
			{
				return;
			}
			Log("process session by id session=" + session.Id + " reason=" + (reason ?? ""));
			ProcessSession(session);
		}
		catch (Exception ex)
		{
			Log("process session by id failed session=" + (sessionId ?? "") + " error=" + ex);
		}
	}

	private void CommitGeneratedReplyAtRecipient(CourierSession session, Hero recipient)
	{
		if (session == null || session.PostprocessConsumed)
		{
			return;
		}
		if (!session.DeliveryApplied)
		{
			return;
		}
		string text = session.ReplyPostprocessedText ?? session.ReplyText ?? "";
		if (recipient == null || recipient.IsDead)
		{
			session.PostprocessConsumed = true;
			session.ReplyPostprocessedText = StripCourierActionTags(text);
			Log("postprocess skipped recipient invalid session=" + session.Id);
			return;
		}
		try
		{
			VoteDealBehavior.ProcessVoteDealTagsDispatch(recipient, ref text);
			DiplomacyBehavior.ProcessDiplomacyTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vote deal tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply world map tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (NobleGatheringBehavior.TryApplyNobleGatheringTagsForExternal(recipient, ref text, out var nobleFacts, out var nobleNotifications))
			{
				foreach (string fact in nobleFacts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in nobleNotifications ?? new List<string>())
				{
					if (!string.IsNullOrWhiteSpace(note))
					{
						InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply noble gathering tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (MyBehavior.TryApplyPartyTransferTagsForExternal(recipient, recipient.CharacterObject, -1, ref text, out var facts, out var notifications))
			{
				foreach (string fact in facts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in notifications ?? new List<string>())
				{
					InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply party transfer tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			bool rewardBeforeHasVassalage = ContainsVassalageActionTag(text);
			bool rewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTag(text);
			Log("ApplyRewardTags start chain=courier session=" + session.Id + " containsVASSALAGE=" + rewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex);
			RewardSystemBehavior.Instance?.ApplyRewardTags(recipient, Hero.MainHero, ref text);
			Log("ApplyRewardTags done chain=courier session=" + session.Id + " beforeVASSALAGE=" + rewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTag(text) + " beforeKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTag(text));
		}
		catch (Exception ex)
		{
			Log("apply reward tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			VanillaIssueOfferBridge.ApplyIssueOfferTags(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vanilla issue tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			RomanceSystemBehavior.Instance?.ApplyMarriageTags(recipient, Hero.MainHero, ref text);
		}
		catch (Exception ex)
		{
			Log("apply marriage tags failed session=" + session.Id + " error=" + ex.Message);
		}
		session.ReplyPostprocessedText = text;
		session.PostprocessConsumed = true;
		PersistCourierReplyToHistories(session, recipient, text);
		Log("postprocess committed at recipient session=" + session.Id + " remainingLen=" + (text ?? "").Length);
	}

	private void PersistCourierReplyToHistories(CourierSession session, Hero recipient, string processedReplyText)
	{
		try
		{
			if (session == null || recipient == null)
			{
				return;
			}
			string reply = StripCourierActionTags(processedReplyText);
			if (string.IsNullOrWhiteSpace(reply))
			{
				reply = StripCourierActionTags(session.ReplyText);
			}
			reply = (reply ?? "").Trim();
			if (string.IsNullOrWhiteSpace(reply))
			{
				return;
			}
			string historyLine = "【回信】" + reply;
			string npcName = (recipient.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = "NPC";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, historyLine, "[AFEF NPC行为补充] " + npcName + "已通过信使写下回信，信使正在把回信带给玩家。");
			ShoutBehavior.RecordNativeConversationNpcLineForExternal(recipient, recipient.CharacterObject, npcName, historyLine);
			PlayerNotorietyBehavior.NoteCourierReplyForExternal(recipient);
			Log("reply history persisted session=" + session.Id + " recipient=" + SafeHeroId(recipient));
		}
		catch (Exception ex)
		{
			Log("persist reply history failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}

	private void ShowCourierReplyWaitPopupAndPause(CourierSession session, Hero recipient)
	{
		if (session == null || session.ReplyGenerated)
		{
			return;
		}
		BeginCourierReplyWaitPause(session, recipient);
		if (session.ReplyWaitPopupShown)
		{
			return;
		}
		session.ReplyWaitPopupShown = true;
		try
		{
			if (IsInboundToPlayer(session))
			{
				string senderName = recipient?.Name?.ToString() ?? session.SenderName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使来信生成", "信使已经抵达你的队伍，正在等待" + senderName + "写完信件正文。\n\n游戏时间已暂停，信件生成完成后会自动送达。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
			else
			{
				string name = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使回信生成", "信使已经抵达 " + name + " 的位置，正在等待对方读信并写下回信。\n\n游戏时间已暂停，回信生成完成后会自动继续并执行后处理标签。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
		}
		catch (Exception ex)
		{
			Log("show reply wait inquiry failed session=" + session.Id + " error=" + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage(IsInboundToPlayer(session) ? "信使已抵达，正在等待来信生成。游戏时间已暂停。" : "信使已抵达，正在等待回信生成。游戏时间已暂停。", Colors.Yellow));
		}
	}

	private void BeginCourierReplyWaitPause(CourierSession session, Hero recipient)
	{
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return;
			}
			if (!_courierReplyWaitTimeLocked)
			{
				_courierReplyWaitPreviousMode = campaign.TimeControlMode;
				_courierReplyWaitPreviousLock = campaign.TimeControlModeLock;
				campaign.TimeControlMode = CampaignTimeControlMode.Stop;
				campaign.SetTimeControlModeLock(true);
				_courierReplyWaitTimeLocked = true;
				Log("reply wait time locked session=" + (session?.Id ?? "") + " recipient=" + SafeHeroId(recipient));
			}
			else
			{
				campaign.SetTimeSpeed(0);
			}
		}
		catch (Exception ex)
		{
			Log("reply wait pause failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}

	private void EndCourierReplyWaitPause(CourierSession completedSession, string reason)
	{
		if (completedSession != null)
		{
			completedSession.ReplyWaitPopupShown = false;
		}
		if (HasActiveCourierReplyWait())
		{
			return;
		}
		try
		{
			InformationManager.HideInquiry();
		}
		catch
		{
		}
		if (!_courierReplyWaitTimeLocked)
		{
			return;
		}
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign != null)
			{
				campaign.SetTimeControlModeLock(_courierReplyWaitPreviousLock);
				if (!_courierReplyWaitPreviousLock)
				{
					campaign.TimeControlMode = _courierReplyWaitPreviousMode;
				}
			}
			Log("reply wait time released reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Log("reply wait release failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
		_courierReplyWaitTimeLocked = false;
	}

	private bool HasActiveCourierReplyWait()
	{
		try
		{
			lock (_sessionLock)
			{
				return _sessions.Values.Any(x => x != null && !IsTerminalStage(x) && !x.ReplyGenerated && x.ReplyWaitPopupShown);
			}
		}
		catch
		{
			return false;
		}
	}

	private void CompleteReturn(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null)
		{
			return;
		}
		Log("return arrived session=" + session.Id + " deliveryApplied=" + session.DeliveryApplied + " replyGenerated=" + session.ReplyGenerated + " postConsumed=" + session.PostprocessConsumed);
		if (session.DeliveryApplied && !session.PostprocessConsumed && !string.IsNullOrWhiteSpace(session.ReplyPostprocessedText) && recipient != null)
		{
			string text = session.ReplyPostprocessedText;
			try
			{
				VoteDealBehavior.ProcessVoteDealTagsDispatch(recipient, ref text);
			DiplomacyBehavior.ProcessDiplomacyTagsDispatch(recipient, ref text);
			}
			catch (Exception ex)
			{
				Log("apply vote deal tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(recipient, ref text);
			}
			catch (Exception ex)
			{
				Log("apply world map tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				if (NobleGatheringBehavior.TryApplyNobleGatheringTagsForExternal(recipient, ref text, out var nobleFacts, out var nobleNotifications))
				{
						foreach (string fact in nobleFacts ?? new List<string>())
						{
							MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
						}
						foreach (string note in nobleNotifications ?? new List<string>())
						{
							if (!string.IsNullOrWhiteSpace(note))
							{
									InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
							}
						}
				}
			}
			catch (Exception ex)
			{
				Log("apply noble gathering tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				if (MyBehavior.TryApplyPartyTransferTagsForExternal(recipient, recipient.CharacterObject, -1, ref text, out var facts, out var notifications))
				{
					foreach (string fact in facts ?? new List<string>())
					{
						MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
					}
					foreach (string note in notifications ?? new List<string>())
					{
						InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
					}
				}
			}
			catch (Exception ex)
			{
				Log("apply party transfer tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				bool rewardBeforeHasVassalage = ContainsVassalageActionTag(text);
				bool rewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTag(text);
				Log("ApplyRewardTags start chain=courier session=" + session.Id + " containsVASSALAGE=" + rewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex);
				RewardSystemBehavior.Instance?.ApplyRewardTags(recipient, Hero.MainHero, ref text);
				Log("ApplyRewardTags done chain=courier session=" + session.Id + " beforeVASSALAGE=" + rewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTag(text) + " beforeKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTag(text));
			}
			catch (Exception ex)
			{
				Log("apply reward tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				VanillaIssueOfferBridge.ApplyIssueOfferTags(recipient, ref text);
			}
			catch (Exception ex)
			{
				Log("apply vanilla issue tags failed session=" + session.Id + " error=" + ex.Message);
			}
			try
			{
				RomanceSystemBehavior.Instance?.ApplyMarriageTags(recipient, Hero.MainHero, ref text);
			}
			catch (Exception ex)
			{
				Log("apply marriage tags failed session=" + session.Id + " error=" + ex.Message);
			}
			session.ReplyPostprocessedText = text;
			session.PostprocessConsumed = true;
			PersistCourierReplyToHistories(session, recipient, text);
			Log("postprocess consumed session=" + session.Id + " remainingLen=" + (text ?? "").Length);
		}
		ReturnCourierContentsToPlayer(session, courier);
		if (session.DeliveryApplied && !session.ReplyPopupShown && !string.IsNullOrWhiteSpace(session.ReplyText))
		{
			session.ReplyPopupShown = true;
			string reply = StripCourierActionTags(session.ReplyPostprocessedText ?? session.ReplyText);
			string senderName = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
			MainThreadActions.Enqueue(() =>
			{
				ShowCourierReplyNotice(senderName, reply);
			});
		}
		else if (!session.DeliveryApplied)
		{
			InformationManager.DisplayMessage(new InformationMessage("信使队已返回，未交付的信件与物资已退还。", Colors.Yellow));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("信使队已返回并解散。", Colors.Green));
		}
		CompleteAndDestroyCourier(session, courier);
	}

	private static void ShowCourierReplyNotice(string senderName, string replyText)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		string body = string.IsNullOrWhiteSpace(replyText) ? "（无回信正文）" : replyText.Trim();
		if (TryPublishCourierReplyMapNotification(name, body))
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage("信使带回了" + name + "的回信。", Colors.Green));
	}

	private static bool TryPublishCourierReplyMapNotification(string senderName, string replyText)
	{
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is not MapState)
			{
				return false;
			}
			MapNotificationView mapNotificationView = MapScreen.Instance?.MapNotificationView;
			if (mapNotificationView == null)
			{
				return false;
			}
			if (!ReferenceEquals(_courierReplyRegisteredMapNotificationView, mapNotificationView))
			{
				mapNotificationView.RegisterMapNotificationType(typeof(AnimusForgeCourierReplyMapNotification), typeof(AnimusForgeCourierReplyMapNotificationItemVM));
				_courierReplyRegisteredMapNotificationView = mapNotificationView;
			}
			MBInformationManager.AddNotice(new AnimusForgeCourierReplyMapNotification(senderName, replyText));
			return true;
		}
		catch (Exception ex)
		{
			Log("publish courier reply map notification failed error=" + ex.Message);
			return false;
		}
	}

	private void ApplyDeliveryPayload(CourierSession session, MobileParty courier, Hero recipient)
	{
		CourierPayloadMode mode = ParsePayloadMode(session.PayloadMode);
		if (mode == CourierPayloadMode.Show)
		{
			int shownGold = 0;
			Dictionary<string, int> shownItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
			{
				if (entry == null || entry.Amount <= 0)
				{
					continue;
				}
				if (string.Equals(entry.Kind, "show_gold", StringComparison.OrdinalIgnoreCase))
				{
					shownGold += entry.Amount;
				}
				else if (string.Equals(entry.Kind, "show_item", StringComparison.OrdinalIgnoreCase))
				{
					shownItems[entry.Id ?? ""] = (shownItems.TryGetValue(entry.Id ?? "", out var old) ? old : 0) + entry.Amount;
				}
				entry.Delivered = true;
			}
			MyBehavior.RecordShownResourcesForExternal(recipient, BuildCourierShownTargetKey(recipient), shownGold, shownItems);
			return;
		}
		PartyBase targetParty = ResolveRecipientPartyBase(recipient);
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Delivered || entry.Amount <= 0)
			{
				continue;
			}
			if (string.Equals(entry.Kind, "gold", StringComparison.OrdinalIgnoreCase))
			{
				int amount = Math.Min(Math.Max(0, session.EscrowGold), entry.Amount);
				if (amount > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, recipient, amount, true);
					session.EscrowGold -= amount;
					entry.Amount = amount;
					entry.Delivered = true;
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(recipient, amount, null, 0);
					}
					catch
					{
					}
				}
			}
			else if (string.Equals(entry.Kind, "item", StringComparison.OrdinalIgnoreCase))
			{
				ItemObject item;
				ItemRoster targetRoster = ResolveRecipientItemRoster(recipient);
				int moved = MyBehavior.TransferItemsFromRosterByStringId(courier.ItemRoster, targetRoster, entry.Id, entry.Amount, out item);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
				if (item != null)
				{
					entry.Name = item.Name?.ToString() ?? entry.Name;
					if (entry.GuidePriceDenars <= 0)
					{
						entry.GuidePriceDenars = EstimateCourierItemUnitValue(item);
					}
				}
				try
				{
					if (moved > 0)
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(recipient, 0, entry.Id, moved);
					}
				}
				catch
				{
				}
			}
			else if (string.Equals(entry.Kind, "troop", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierPartyTransferUnitValue(recipient, entry, isPrisoner: false);
				}
				int moved = MoveCharacterBetweenMemberRosters(courier.Party, targetParty, entry.Id, entry.Amount, entry.IsHero);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
			}
			else if (string.Equals(entry.Kind, "prisoner", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierPartyTransferUnitValue(recipient, entry, isPrisoner: true);
				}
				int moved = MoveCharacterBetweenPrisonRosters(courier.Party, targetParty, entry.Id, entry.Amount, entry.IsHero);
				entry.Amount = moved;
				entry.Delivered = moved > 0;
			}
			else if (string.Equals(entry.Kind, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				if (entry.GuidePriceDenars <= 0)
				{
					entry.GuidePriceDenars = EstimateCourierSettlementTransferValue(recipient, entry);
				}
				string status = null;
				bool ok = RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryApplyPlayerSettlementTransferForExternal(recipient, entry.Id, out status);
				entry.Delivered = ok;
				entry.Amount = ok ? 1 : 0;
				if (!ok && !string.IsNullOrWhiteSpace(status))
				{
					Log("fixed asset transfer failed session=" + session.Id + " asset=" + entry.Id + " status=" + status);
				}
			}
		}
	}

	private void ReturnCourierContentsToPlayer(CourierSession session, MobileParty courier)
	{
		PartyBase playerParty = MobileParty.MainParty?.Party ?? PartyBase.MainParty;
		if (playerParty == null || courier == null)
		{
			return;
		}
		MoveWholeMemberRoster(courier.Party, playerParty);
		MoveWholePrisonRoster(courier.Party, playerParty);
		MoveWholeItemRoster(courier.ItemRoster, MobileParty.MainParty?.ItemRoster);
		if (!session.DeliveryApplied && session.EscrowGold > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, session.EscrowGold, true);
			Log("refunded escrow gold session=" + session.Id + " amount=" + session.EscrowGold);
			session.EscrowGold = 0;
		}
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		try
		{
			if (destroyedParty == null)
			{
				return;
			}
			CourierSession session = null;
			lock (_sessionLock)
			{
				session = _sessions.Values.FirstOrDefault(x => x != null && string.Equals((x.CourierPartyId ?? "").Trim(), destroyedParty.StringId ?? "", StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
			}
			if (session == null)
			{
				return;
			}
			Hero recipient = ResolveRecipient(session);
			string destroyerName = destroyerParty?.Name?.ToString() ?? "未知势力";
			Log("destroyed session=" + session.Id + " party=" + destroyedParty.StringId + " destroyer=" + destroyerName + " deliveryApplied=" + session.DeliveryApplied);
			if (IsInboundToPlayer(session))
			{
				HandleInboundCourierDestroyed(session, destroyedParty, destroyerName);
				return;
			}
			DisplayCourierDestroyedStatus(session, "歼灭者：" + destroyerName);
			TryMoveHeroLossesToDestroyer(session, destroyerParty);
			string fact = "[AFEF玩家行为补充] " + (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家") + "通过信使寄出的信使队在途中被" + destroyerName + "歼灭。";
			if (!session.DeliveryApplied)
			{
				fact += "这封信和随信寄出的物品、金钱、部队或俘虏未能送达；固定资产转移没有发生。";
			}
			else
			{
				fact += "该信使队已完成交付，但未能把回信或剩余人员安全带回玩家处。";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
			UntrackCourierMapVisual(destroyedParty, "destroyed");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed");
			lock (_sessionLock)
			{
				_sessions.Remove(session.Id);
			}
			RemoveCourierRuntimeIndex(session);
		}
		catch (Exception ex)
		{
			Log("destroy handler failed: " + ex);
		}
	}

	private void TryMoveHeroLossesToDestroyer(CourierSession session, PartyBase destroyerParty)
	{
		if (session == null || destroyerParty == null)
		{
			return;
		}
		foreach (CourierCargoEntry entry in (session.CrewEntries ?? new List<CourierCargoEntry>()).Concat(session.Entries ?? new List<CourierCargoEntry>()))
		{
			if (entry == null || !entry.IsHero)
			{
				continue;
			}
			if (!string.Equals(entry.Kind, "crew", StringComparison.OrdinalIgnoreCase) && entry.Delivered)
			{
				continue;
			}
			CharacterObject character = ResolveCharacter(entry.Id);
			Hero hero = character?.HeroObject;
			if (hero == null || hero.IsDead || hero.IsHumanPlayerCharacter)
			{
				continue;
			}
			try
			{
				if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null)
				{
					TakePrisonerAction.Apply(destroyerParty, hero);
					Log("hero loss captured session=" + session.Id + " hero=" + hero.StringId + " destroyer=" + destroyerParty.Name);
				}
			}
			catch (Exception ex)
			{
				Log("hero loss capture failed session=" + session.Id + " hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
			}
		}
	}

	private void CompleteAndDestroyCourier(CourierSession session, MobileParty courier)
	{
		if (session == null)
		{
			return;
		}
		session.Stage = CourierStage.Completed.ToString();
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
		try
		{
			if (courier != null && courier.IsActive)
			{
				UntrackCourierMapVisual(courier, "completed");
				DestroyCourierTemporaryShips(session, courier, "completed");
				if (courier.IsCurrentlyUsedByAQuest)
				{
					courier.SetPartyUsedByQuest(false);
				}
				DestroyPartyAction.Apply(null, courier);
			}
		}
		catch (Exception ex)
		{
			Log("destroy completed courier failed session=" + session.Id + " error=" + ex.Message);
		}
		Log("session completed id=" + session.Id);
	}

	private void HandleCourierMissing(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		Log("courier missing session=" + session.Id + " party=" + session.CourierPartyId);
		if (IsInboundToPlayer(session))
		{
			HandleInboundCourierMissing(session);
			return;
		}
		DisplayCourierDestroyedStatus(session, "信使队伍已从大地图消失。");
		Hero recipient = ResolveRecipient(session);
		MyBehavior.AppendExternalDialogueHistory(recipient, null, null, "[AFEF玩家行为补充] 玩家派出的信使队失去踪迹，信件与随信物资未能确认送达。");
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing");
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
	}

	private void HandleInboundCourierDestroyed(CourierSession session, MobileParty destroyedParty, string destroyerName)
	{
		try
		{
			DisplayCourierDestroyedStatus(session, "歼灭者：" + (destroyerName ?? "未知势力"));
			Hero sender = ResolveSender(session);
			string senderName = string.IsNullOrWhiteSpace(session?.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
			MyBehavior.AppendExternalDialogueHistory(sender, null, null, "[AFEF NPC行为补充] " + senderName + "派往玩家处的信使队在途中被" + (destroyerName ?? "未知势力") + "歼灭，这封外交信未能确认送达。");
			UntrackCourierMapVisual(destroyedParty, "destroyed_inbound");
			DestroyCourierTemporaryShips(session, destroyedParty, "destroyed_inbound");
			session.Stage = CourierStage.Destroyed.ToString();
			EndCourierReplyWaitPause(session, "destroyed_inbound");
			lock (_sessionLock)
			{
				_sessions.Remove(session.Id);
			}
			RemoveCourierRuntimeIndex(session);
		}
		catch (Exception ex)
		{
			Log("inbound destroy handler failed: " + ex);
		}
	}

	private void HandleInboundCourierMissing(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		DisplayCourierDestroyedStatus(session, "信使队伍已从大地图消失。");
		Hero sender = ResolveSender(session);
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		MyBehavior.AppendExternalDialogueHistory(sender, null, null, "[AFEF NPC行为补充] " + senderName + "派往玩家处的信使队失去踪迹，这封外交信未能确认送达。");
		session.Stage = CourierStage.Destroyed.ToString();
		session.TemporaryShipCreated = false;
		session.TemporaryShipHullId = "";
		EndCourierReplyWaitPause(session, "missing_inbound");
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
	}

	private static void DisplayCourierDestroyedStatus(CourierSession session, string detail)
	{
		try
		{
			string recipientName = string.IsNullOrWhiteSpace(session?.RecipientName) ? "" : " 收件人：" + session.RecipientName + "。";
			string detailText = string.IsNullOrWhiteSpace(detail) ? "" : " " + detail.Trim();
			InformationManager.DisplayMessage(new InformationMessage("信使部队已被歼灭。" + recipientName + detailText, Colors.Red));
			Log("status courier_destroyed session=" + (session?.Id ?? "") + " recipient=" + (session?.RecipientHeroId ?? "") + " detail=" + (detail ?? ""));
		}
		catch
		{
		}
	}

	private static CourierRoutePlan BuildCourierRoutePlan(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort)
	{
		bool requiresNaval = ShouldUseNavalRoute(courier, targetPosition, targetSettlement, targetParty, preferPort);
		bool usePort = targetSettlement != null && targetSettlement.HasPort && (requiresNaval || preferPort || courier?.IsCurrentlyAtSea == true || targetParty?.IsCurrentlyAtSea == true);
		return new CourierRoutePlan
		{
			RequiresNaval = requiresNaval,
			UsePort = usePort,
			NavigationType = GetEffectiveCourierNavigationType(courier, requiresNaval),
			Reason = BuildNavalRouteReason(courier, targetPosition, targetSettlement, targetParty, preferPort, requiresNaval)
		};
	}

	private static bool ShouldUseNavalRoute(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort)
	{
		try
		{
			if (courier == null || !IsNavalRuntimeAvailable())
			{
				return false;
			}
			if (courier.IsCurrentlyAtSea || !courier.Position.IsOnLand || targetParty?.IsCurrentlyAtSea == true || !targetPosition.IsOnLand)
			{
				return true;
			}
			if (preferPort && targetSettlement?.HasPort == true)
			{
				return true;
			}
			CampaignVec2 landTarget = targetSettlement?.GatePosition ?? targetPosition;
			return IsValidCampaignPosition(courier.Position) && IsValidCampaignPosition(landTarget) && !DefaultLandPathExists(courier.Position, landTarget);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildNavalRouteReason(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort, bool requiresNaval)
	{
		if (!requiresNaval)
		{
			return "land";
		}
		if (courier?.IsCurrentlyAtSea == true || courier?.Position.IsOnLand == false)
		{
			return "courier_sea";
		}
		if (targetParty?.IsCurrentlyAtSea == true || !targetPosition.IsOnLand)
		{
			return "target_sea";
		}
		if (preferPort && targetSettlement?.HasPort == true)
		{
			return "prefer_port";
		}
		return "no_land_path";
	}

	private static MobileParty.NavigationType GetEffectiveCourierNavigationType(MobileParty courier, bool requiresNaval)
	{
		if (requiresNaval)
		{
			return MobileParty.NavigationType.All;
		}
		MobileParty.NavigationType navigationType = courier?.NavigationCapability ?? MobileParty.NavigationType.Default;
		return navigationType == MobileParty.NavigationType.None ? MobileParty.NavigationType.Default : navigationType;
	}

	private static bool EnsureCourierNavalReadiness(CourierSession session, MobileParty courier, CourierRoutePlan plan, string reason)
	{
		if (session == null || courier == null || plan == null || !plan.RequiresNaval)
		{
			return true;
		}
		try
		{
			if (courier.Ships != null && courier.Ships.Count > 0)
			{
				MarkExistingCourierTemporaryShips(session, courier);
				if (!courier.HasNavalNavigationCapability)
				{
					LogCourierStatusVerbose("naval_existing_no_cap:" + session.Id, "naval_existing_no_cap session=" + session.Id + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier) + " tempShipCreated=" + session.TemporaryShipCreated + " hull=" + (session.TemporaryShipHullId ?? ""), 10.0);
				}
				return courier.HasNavalNavigationCapability;
			}
			if (!IsNavalRuntimeAvailable() || courier.Party == null)
			{
				LogVerbose("naval_unavailable:" + session.Id, "courier naval runtime unavailable session=" + session.Id + " reason=" + (reason ?? ""), 10.0);
				LogCourierStatusVerbose("naval_unavailable:" + session.Id, "naval_unavailable session=" + session.Id + " reason=" + (reason ?? "") + " runtime=" + IsNavalRuntimeAvailable() + " hasParty=" + (courier.Party != null) + " courier=" + DescribeMobileParty(courier), 10.0);
				return false;
			}
			ShipHull hull = SelectCourierTemporaryShipHull(courier);
			if (hull == null)
			{
				Log("courier temporary ship hull missing session=" + session.Id + " reason=" + (reason ?? ""));
				LogCourierStatus("temporary_ship_hull_missing session=" + session.Id + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier));
				return false;
			}
			Ship ship = new Ship(hull);
			ship.SetName(new TextObject(TemporaryCourierShipName));
			ship.IsUsedByQuest = true;
			ship.IsInvulnerable = true;
			ChangeShipOwnerAction.ApplyByMobilePartyCreation(courier.Party, ship);
			session.TemporaryShipCreated = true;
			session.TemporaryShipHullId = hull.StringId ?? "";
			session.LastRouteKey = "";
			Log("courier temporary ship created session=" + session.Id + " party=" + (courier.StringId ?? "") + " hull=" + (hull.StringId ?? "") + " reason=" + (reason ?? ""));
			LogCourierStatus("temporary_ship_created session=" + session.Id + " reason=" + (reason ?? "") + " hull=" + (hull.StringId ?? "") + " capacity=" + hull.TotalCrewCapacity + " speed=" + hull.BaseSpeed + " courier=" + DescribeMobileParty(courier));
			return courier.HasNavalNavigationCapability || (courier.Ships != null && courier.Ships.Count > 0);
		}
		catch (Exception ex)
		{
			Log("courier temporary ship create failed session=" + session.Id + " reason=" + (reason ?? "") + " error=" + ex.Message);
			LogCourierStatus("temporary_ship_create_failed session=" + session.Id + " reason=" + (reason ?? "") + " error=" + ex.Message + " courier=" + DescribeMobileParty(courier));
			return false;
		}
	}

	private static void MarkExistingCourierTemporaryShips(CourierSession session, MobileParty courier)
	{
		if (session == null || courier?.Ships == null || !session.TemporaryShipCreated)
		{
			return;
		}
		foreach (Ship ship in courier.Ships)
		{
			if (IsCourierTemporaryShip(session, ship))
			{
				ship.IsUsedByQuest = true;
				ship.IsInvulnerable = true;
				ship.SetName(new TextObject(TemporaryCourierShipName));
			}
		}
	}

	private static ShipHull SelectCourierTemporaryShipHull(MobileParty courier)
	{
		try
		{
			Ship playerShip = MobileParty.MainParty?.Ships?.FirstOrDefault(x => x?.ShipHull != null);
			if (playerShip?.ShipHull != null)
			{
				return playerShip.ShipHull;
			}
		}
		catch
		{
		}
		List<ShipHull> hulls = GetLoadedShipHulls();
		if (hulls.Count == 0)
		{
			return null;
		}
		int crewCount = Math.Max(1, courier?.MemberRoster?.TotalManCount ?? 1);
		ShipHull fitting = hulls
			.Where(x => x != null && x.TotalCrewCapacity >= crewCount)
			.OrderBy(x => x.TotalCrewCapacity)
			.ThenByDescending(x => x.BaseSpeed)
			.FirstOrDefault();
		return fitting ?? hulls
			.Where(x => x != null)
			.OrderByDescending(x => x.TotalCrewCapacity)
			.ThenByDescending(x => x.BaseSpeed)
			.FirstOrDefault();
	}

	private static List<ShipHull> GetLoadedShipHulls()
	{
		Dictionary<string, ShipHull> hulls = new Dictionary<string, ShipHull>(StringComparer.OrdinalIgnoreCase);
		AddLoadedShipHulls(hulls, MBObjectManager.Instance);
		AddLoadedShipHulls(hulls, Game.Current?.ObjectManager);
		return hulls.Values.Where(x => x != null && x.TotalCrewCapacity > 0).ToList();
	}

	private static void AddLoadedShipHulls(Dictionary<string, ShipHull> hulls, MBObjectManager manager)
	{
		if (hulls == null || manager == null)
		{
			return;
		}
		try
		{
			MBReadOnlyList<ShipHull> list = manager.GetObjectTypeList<ShipHull>();
			if (list == null)
			{
				return;
			}
			foreach (ShipHull hull in list)
			{
				if (hull == null)
				{
					continue;
				}
				string key = string.IsNullOrWhiteSpace(hull.StringId) ? hull.GetHashCode().ToString() : hull.StringId;
				hulls[key] = hull;
			}
		}
		catch
		{
		}
	}

	private static bool IsNavalRuntimeAvailable()
	{
		try
		{
			if (GetLoadedShipHulls().Count == 0 || Settlement.All == null || !Settlement.All.Any(x => x != null && x.HasPort))
			{
				return false;
			}
			string modelName = Campaign.Current?.Models?.PartyNavigationModel?.GetType()?.FullName ?? "";
			if (modelName.IndexOf("Naval", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return MobileParty.All?.Any(x => x != null && x.IsActive && x.HasNavalNavigationCapability) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool DefaultLandPathExists(CampaignVec2 fromPoint, CampaignVec2 toPoint)
	{
		try
		{
			if (!IsValidCampaignPosition(fromPoint) || !IsValidCampaignPosition(toPoint))
			{
				return true;
			}
			return Campaign.Current?.Models?.MapDistanceModel?.PathExistBetweenPoints(fromPoint, toPoint, MobileParty.NavigationType.Default) != false;
		}
		catch
		{
			return fromPoint.IsOnLand && toPoint.IsOnLand;
		}
	}

	private static bool IsValidCampaignPosition(CampaignVec2 position)
	{
		try
		{
			return position.IsValid();
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldPreferSafeSettlementPort(MobileParty courier, string reason)
	{
		return courier?.IsCurrentlyAtSea == true || courier?.Position.IsOnLand == false || (reason ?? "").IndexOf("naval", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static float GetSafeSettlementDistanceSquared(Settlement settlement, CampaignVec2 position, bool preferPort)
	{
		if (settlement == null)
		{
			return float.MaxValue;
		}
		CampaignVec2 approach = preferPort && settlement.HasPort ? settlement.PortPosition : settlement.GatePosition;
		return approach.DistanceSquared(position);
	}

	private static bool ShouldDivertToSafeSettlementAfterNavalStuck(CourierSession session, CourierRoutePlan plan)
	{
		return session != null && plan?.RequiresNaval == true && session.NavalStuckRefreshCount >= NavalStuckSafeRouteThreshold;
	}

	private static void DestroyCourierTemporaryShips(CourierSession session, MobileParty courier, string reason)
	{
		try
		{
			if (session == null || courier?.Ships == null)
			{
				return;
			}
			List<Ship> ships = courier.Ships.Where(x => IsCourierTemporaryShip(session, x)).ToList();
			foreach (Ship ship in ships)
			{
				DestroyShipAction.Apply(ship);
			}
			if (ships.Count > 0)
			{
				Log("courier temporary ships destroyed session=" + session.Id + " count=" + ships.Count + " reason=" + (reason ?? ""));
			}
			session.TemporaryShipCreated = false;
			session.TemporaryShipHullId = "";
		}
		catch (Exception ex)
		{
			Log("courier temporary ship cleanup failed session=" + (session?.Id ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsCourierTemporaryShip(CourierSession session, Ship ship)
	{
		if (ship == null)
		{
			return false;
		}
		string shipName = ship.Name?.ToString() ?? "";
		bool nameMatches = string.Equals(shipName, TemporaryCourierShipName, StringComparison.OrdinalIgnoreCase);
		string hullId = (session?.TemporaryShipHullId ?? "").Trim();
		bool hullMatches = !string.IsNullOrWhiteSpace(hullId) && string.Equals(ship.ShipHull?.StringId ?? "", hullId, StringComparison.OrdinalIgnoreCase);
		if (session?.TemporaryShipCreated == true)
		{
			return hullMatches || nameMatches || ship.IsUsedByQuest;
		}
		return nameMatches && ship.IsUsedByQuest;
	}

	private void RouteToRecipient(CourierSession session, MobileParty courier, MobileParty targetParty, Settlement targetSettlement)
	{
		if (session == null || courier == null)
		{
			return;
		}
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, targetParty?.Position ?? targetSettlement?.GatePosition ?? courier.Position, targetSettlement, targetParty, false);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "recipient"))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = BuildRecipientRouteKey(targetParty, targetSettlement, plan);
		AiBehavior expectedBehavior = targetParty != null && targetParty.IsActive ? AiBehavior.GoToPoint : AiBehavior.GoToSettlement;
		LogCourierStatusVerbose("route_eval_recipient:" + session.Id, "route_eval_recipient session=" + session.Id + " key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, expectedBehavior, plan, targetParty, targetSettlement, targetParty?.Position ?? CampaignVec2.Invalid))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":recipient", "route_target_mismatch session=" + session.Id + " route=recipient key=" + key + " expectedBehavior=" + expectedBehavior + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, expectedBehavior))
		{
			if (ShouldDivertToSafeSettlementAfterNavalStuck(session, plan))
			{
				LogCourierStatus("divert_to_safe_after_stuck session=" + session.Id + " route=recipient key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount);
				RouteToSafeSettlement(session, courier, "naval_stuck");
				return;
			}
			if (targetParty != null && targetParty.IsActive)
			{
				courier.SetMoveGoToPoint(targetParty.Position, plan.NavigationType);
				LogCourierStatus("route_recipient_set session=" + session.Id + " command=go_to_party key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " courier=" + DescribeMobileParty(courier));
			}
			else if (targetSettlement != null)
			{
				courier.SetMoveGoToSettlement(targetSettlement, plan.NavigationType, plan.UsePort);
				LogCourierStatus("route_recipient_set session=" + session.Id + " command=go_to_settlement key=" + key + " " + DescribeRoutePlan(plan) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier));
			}
			ApplyCourierAiOverrides(courier, "route_recipient");
			LogVerbose("route_recipient:" + session.Id, "route recipient session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private static string BuildRecipientRouteKey(MobileParty targetParty, Settlement targetSettlement, CourierRoutePlan plan)
	{
		string suffix = plan?.KeySuffix ?? "";
		if (targetParty != null && targetParty.IsActive)
		{
			CampaignVec2 position = targetParty.Position;
			int x = (int)MathF.Round(position.X * 2f);
			int y = (int)MathF.Round(position.Y * 2f);
			return "recipient_party:" + (targetParty.StringId ?? "") + ":" + x + ":" + y + ":targetSea=" + (targetParty.IsCurrentlyAtSea ? "1" : "0") + suffix;
		}
		return "recipient_settlement:" + (targetSettlement?.StringId ?? "") + suffix;
	}

	private void RouteToSender(CourierSession session, MobileParty courier)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (session == null || courier == null || mainParty == null)
		{
			return;
		}
		Settlement targetSettlement = mainParty.CurrentSettlement;
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, targetSettlement?.GatePosition ?? mainParty.Position, targetSettlement, mainParty, targetSettlement != null && courier.IsCurrentlyAtSea);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "sender"))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = BuildSenderRouteKey(mainParty, plan);
		AiBehavior expectedBehavior = mainParty.CurrentSettlement != null ? AiBehavior.GoToSettlement : AiBehavior.GoToPoint;
		LogCourierStatusVerbose("route_eval_sender:" + session.Id, "route_eval_sender session=" + session.Id + " key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, expectedBehavior, plan, mainParty, mainParty.CurrentSettlement, mainParty.Position))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":sender", "route_target_mismatch session=" + session.Id + " route=sender key=" + key + " expectedBehavior=" + expectedBehavior + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, expectedBehavior))
		{
			if (ShouldDivertToSafeSettlementAfterNavalStuck(session, plan))
			{
				LogCourierStatus("divert_to_safe_after_stuck session=" + session.Id + " route=sender key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount);
				RouteToSafeSettlement(session, courier, "naval_stuck_return");
				return;
			}
			if (mainParty.CurrentSettlement != null)
			{
				courier.SetMoveGoToSettlement(mainParty.CurrentSettlement, plan.NavigationType, plan.UsePort);
				LogCourierStatus("route_sender_set session=" + session.Id + " command=go_to_settlement key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier));
			}
			else
			{
				courier.SetMoveGoToPoint(mainParty.Position, plan.NavigationType);
				LogCourierStatus("route_sender_set session=" + session.Id + " command=go_to_party key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier));
			}
			ApplyCourierAiOverrides(courier, "route_sender");
			LogVerbose("route_sender:" + session.Id, "route sender session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private static string BuildSenderRouteKey(MobileParty mainParty, CourierRoutePlan plan)
	{
		string suffix = plan?.KeySuffix ?? "";
		if (mainParty == null)
		{
			return "sender_point:null" + suffix;
		}
		if (mainParty.CurrentSettlement != null)
		{
			return "sender_settlement:" + mainParty.CurrentSettlement.StringId + suffix;
		}
		CampaignVec2 position = mainParty.Position;
		int x = (int)MathF.Round(position.X * 2f);
		int y = (int)MathF.Round(position.Y * 2f);
		return "sender_point:" + x + ":" + y + ":targetSea=" + (mainParty.IsCurrentlyAtSea ? "1" : "0") + suffix;
	}

	private void RouteToSafeSettlement(CourierSession session, MobileParty courier, string reason)
	{
		if (session == null || courier == null)
		{
			return;
		}
		bool preferPort = ShouldPreferSafeSettlementPort(courier, reason);
		Settlement settlement = ResolveSafeSettlement(session, courier, preferPort);
		if (settlement == null)
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "route_safe_hold");
			LogCourierStatusVerbose("route_safe_hold:" + session.Id + ":" + (reason ?? ""), "route_safe_hold session=" + session.Id + " reason=" + (reason ?? "") + " preferPort=" + preferPort + " courier=" + DescribeMobileParty(courier), 5.0);
			return;
		}
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, settlement.GatePosition, settlement, null, preferPort);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "safe_" + (reason ?? "")))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = "safe:" + settlement.StringId + ":" + reason + plan.KeySuffix;
		LogCourierStatusVerbose("route_eval_safe:" + session.Id + ":" + (reason ?? ""), "route_eval_safe session=" + session.Id + " reason=" + (reason ?? "") + " key=" + key + " preferPort=" + preferPort + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, AiBehavior.GoToSettlement, plan, null, settlement, CampaignVec2.Invalid))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":safe", "route_target_mismatch session=" + session.Id + " route=safe reason=" + (reason ?? "") + " key=" + key + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, AiBehavior.GoToSettlement))
		{
			courier.SetMoveGoToSettlement(settlement, plan.NavigationType, plan.UsePort);
			ApplyCourierAiOverrides(courier, "route_safe");
			LogCourierStatus("route_safe_set session=" + session.Id + " reason=" + (reason ?? "") + " key=" + key + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier));
			LogVerbose("route_safe:" + session.Id, "route safe session=" + session.Id + " settlement=" + settlement.StringId + " reason=" + reason, 5.0);
		}
	}

	private Settlement ResolveSafeSettlement(CourierSession session, MobileParty courier, bool preferPort)
	{
		try
		{
			CampaignVec2 position = courier?.Position ?? MobileParty.MainParty?.Position ?? CampaignVec2.Invalid;
			List<Settlement> candidates = Settlement.All?
				.Where(x => IsCourierSafeSettlementCandidate(x, courier))
				.ToList() ?? new List<Settlement>();
			if (preferPort && candidates.Any(x => x.HasPort))
			{
				candidates = candidates.Where(x => x.HasPort).ToList();
			}
			List<Settlement> friendlyCandidates = candidates
				.Where(x => IsCourierFriendlySafeSettlementCandidate(x, courier))
				.ToList();
			if (!string.IsNullOrWhiteSpace(session.SafeSettlementId))
			{
				Settlement existing = Settlement.Find(session.SafeSettlementId);
				if (existing != null && candidates.Contains(existing) && (friendlyCandidates.Count == 0 || IsCourierFriendlySafeSettlementCandidate(existing, courier)))
				{
					return existing;
				}
				session.SafeSettlementId = "";
			}
			IEnumerable<Settlement> primaryCandidates = friendlyCandidates.Count > 0 ? friendlyCandidates : candidates;
			Settlement settlement = primaryCandidates
				.OrderBy(x => GetSafeSettlementDistanceSquared(x, position, preferPort))
				.FirstOrDefault();
			session.SafeSettlementId = settlement?.StringId ?? "";
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsCourierFriendlySafeSettlementCandidate(Settlement settlement, MobileParty courier)
	{
		try
		{
			if (!IsCourierSafeSettlementCandidate(settlement, courier))
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			Clan courierClan = courier?.ActualClan ?? playerClan;
			IFaction courierFaction = courier?.MapFaction ?? courierClan ?? playerClan ?? Hero.MainHero?.MapFaction;
			if (playerClan != null && settlement.OwnerClan == playerClan)
			{
				return true;
			}
			if (courierClan != null && settlement.OwnerClan == courierClan)
			{
				return true;
			}
			if (courierFaction != null && settlement.MapFaction == courierFaction)
			{
				return true;
			}
			Kingdom courierKingdom = courierFaction as Kingdom ?? courierClan?.Kingdom ?? playerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
			return courierKingdom != null && (settlement.MapFaction == courierKingdom || settlement.OwnerClan?.Kingdom == courierKingdom);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCourierSafeSettlementCandidate(Settlement settlement, MobileParty courier)
	{
		try
		{
			if (settlement == null || !settlement.IsFortification || settlement.IsHideout || settlement.IsUnderSiege)
			{
				return false;
			}
			IFaction courierFaction = courier?.MapFaction ?? courier?.ActualClan ?? Clan.PlayerClan ?? Hero.MainHero?.MapFaction;
			IFaction settlementFaction = settlement.MapFaction ?? settlement.OwnerClan;
			if (courierFaction == null || settlementFaction == null || courierFaction == settlementFaction)
			{
				return true;
			}
			return !FactionManager.IsAtWarAgainstFaction(courierFaction, settlementFaction);
		}
		catch
		{
			return false;
		}
	}

	private bool HandleRecipientUnavailableStatus(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null || recipient == null)
		{
			return false;
		}
		string reason = GetRecipientUnavailableReason(recipient);
		if (!string.IsNullOrWhiteSpace(reason))
		{
			LogCourierStatusVerbose("recipient_unavailable:" + session.Id, "recipient_unavailable session=" + session.Id + " reason=" + reason + " " + DescribeHero(recipient) + " courier=" + DescribeMobileParty(courier), 5.0);
			SetRecipientWaitReason(session, reason, "target_" + reason);
			session.Stage = CourierStage.WaitingRecipient.ToString();
			RouteToSafeSettlement(session, courier, "recipient_" + reason + "_wait");
			return true;
		}
		return false;
	}

	private static string GetRecipientUnavailableReason(Hero recipient)
	{
		if (recipient == null || recipient.IsDead)
		{
			return "";
		}
		try
		{
			if (recipient.IsFugitive)
			{
				return "fugitive";
			}
			if (recipient.IsPrisoner || recipient.PartyBelongedToAsPrisoner != null)
			{
				return "prisoner";
			}
		}
		catch
		{
		}
		return "";
	}

	private void SetRecipientWaitReason(CourierSession session, string reason, string source)
	{
		if (session == null)
		{
			return;
		}
		string normalized = (reason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return;
		}
		if (string.Equals(session.RecipientWaitReason ?? "", normalized, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		session.RecipientWaitReason = normalized;
		Log("recipient unavailable session=" + session.Id + " recipient=" + session.RecipientHeroId + " status=" + normalized + " source=" + (source ?? "") + " action=safe_wait");
		LogCourierStatus("recipient_wait_set session=" + session.Id + " recipient=" + session.RecipientHeroId + " status=" + normalized + " source=" + (source ?? "") + " action=safe_wait safeSettlement=" + (session.SafeSettlementId ?? ""));
		if (string.Equals(normalized, "fugitive", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("信使目标正在逃亡，信使正前往最近定居点等待。", Colors.Yellow));
		}
		else if (string.Equals(normalized, "prisoner", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("信使目标处于俘虏状态，信使正前往最近定居点等待。", Colors.Yellow));
		}
	}

	private void ClearRecipientWaitReasonIfNeeded(CourierSession session, string source)
	{
		if (session == null || string.IsNullOrWhiteSpace(session.RecipientWaitReason))
		{
			return;
		}
		string previous = session.RecipientWaitReason;
		session.RecipientWaitReason = "";
		session.SafeSettlementId = "";
		session.LastRouteKey = "";
		if (IsBlockingRecipientWaitReason(previous))
		{
			LogCourierStatus("recipient_wait_cleared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? "") + " status=resume_route");
			Log("recipient reappeared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? "") + " status=resume_route");
			InformationManager.DisplayMessage(new InformationMessage("信使目标重新出现，信使正在路上。", Colors.Green));
			return;
		}
		Log("recipient wait cleared session=" + session.Id + " previousStatus=" + previous + " source=" + (source ?? ""));
		LogCourierStatus("recipient_wait_cleared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? ""));
	}

	private static bool IsBlockingRecipientWaitReason(string reason)
	{
		string value = (reason ?? "").Trim();
		return string.Equals(value, "fugitive", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryGetRecipientTarget(Hero recipient, out MobileParty party, out Settlement settlement)
	{
		party = null;
		settlement = null;
		if (recipient == null || recipient.IsDead)
		{
			return false;
		}
		party = recipient.PartyBelongedTo;
		if (party != null && party.IsActive)
		{
			settlement = party.CurrentSettlement;
			return true;
		}
		try
		{
			if (recipient.PartyBelongedToAsPrisoner != null)
			{
				PartyBase prisonerParty = recipient.PartyBelongedToAsPrisoner;
				party = prisonerParty.MobileParty;
				settlement = prisonerParty.Settlement;
				if ((party != null && party.IsActive) || settlement != null)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		settlement = recipient.CurrentSettlement ?? recipient.StayingInSettlement;
		return settlement != null;
	}

	private static bool IsAtRecipient(MobileParty courier, MobileParty targetParty, Settlement settlement)
	{
		if (courier == null)
		{
			return false;
		}
		if (targetParty != null && targetParty.IsActive)
		{
			if (courier.CurrentSettlement != null && courier.CurrentSettlement == targetParty.CurrentSettlement)
			{
				return true;
			}
			float arrivalDistance = GetMobilePartyArrivalDistance();
			try
			{
				MobileParty.NavigationType navigationType = targetParty.IsCurrentlyAtSea || courier.IsCurrentlyAtSea ? MobileParty.NavigationType.All : courier.NavigationCapability;
				float distance = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(courier, targetParty, navigationType);
				if (distance <= arrivalDistance)
				{
					return true;
				}
			}
			catch
			{
			}
			return courier.Position.DistanceSquared(targetParty.Position) <= arrivalDistance * arrivalDistance;
		}
		if (settlement != null)
		{
			return IsAtSettlementApproach(courier, settlement) || courier.CurrentSettlement == settlement;
		}
		return false;
	}

	private static bool IsAtSettlementApproach(MobileParty courier, Settlement settlement)
	{
		if (courier == null || settlement == null)
		{
			return false;
		}
		if (courier.Position.DistanceSquared(settlement.GatePosition) <= SettlementArrivalDistanceSquared)
		{
			return true;
		}
		return settlement.HasPort && courier.Position.DistanceSquared(settlement.PortPosition) <= SettlementArrivalDistanceSquared;
	}

	private static float GetMobilePartyArrivalDistance()
	{
		try
		{
			float encounterRadius = Campaign.Current?.Models?.EncounterModel?.GetEncounterJoiningRadius ?? 0f;
			if (encounterRadius > 0f)
			{
				return MathF.Max(MobilePartyArrivalDistance, encounterRadius * 2.5f);
			}
		}
		catch
		{
		}
		return MobilePartyArrivalDistance;
	}

	private static bool IsAtSender(MobileParty courier, MobileParty sender)
	{
		if (courier == null || sender == null)
		{
			return false;
		}
		if (sender.CurrentSettlement != null)
		{
			return IsAtSettlementApproach(courier, sender.CurrentSettlement) || courier.CurrentSettlement == sender.CurrentSettlement;
		}
		return courier.Position.DistanceSquared(sender.Position) <= SenderArrivalDistanceSquared;
	}

	private static bool ShouldRefreshRoute(CourierSession session, string routeKey, MobileParty courier = null, params AiBehavior[] expectedDefaultBehaviors)
	{
		return ShouldRefreshRouteCore(session, routeKey, courier, false, expectedDefaultBehaviors);
	}

	private static bool ShouldRefreshRouteWithProgress(CourierSession session, string routeKey, MobileParty courier, bool monitorProgress, params AiBehavior[] expectedDefaultBehaviors)
	{
		return ShouldRefreshRouteCore(session, routeKey, courier, monitorProgress, expectedDefaultBehaviors);
	}

	private static bool IsCourierRouteTargetMismatched(MobileParty courier, AiBehavior expectedBehavior, CourierRoutePlan plan, MobileParty expectedParty, Settlement expectedSettlement, CampaignVec2 expectedPoint)
	{
		if (courier == null || plan == null)
		{
			return false;
		}
		try
		{
			if (courier.DefaultBehavior != expectedBehavior)
			{
				return true;
			}
			if (courier.DesiredAiNavigationType != plan.NavigationType)
			{
				return true;
			}
			if (expectedBehavior == AiBehavior.GoToSettlement)
			{
				return courier.TargetSettlement != expectedSettlement || courier.IsTargetingPort != plan.UsePort;
			}
			if (expectedBehavior == AiBehavior.GoToPoint)
			{
				if (courier.TargetSettlement != null || courier.TargetParty != null)
				{
					return true;
				}
				if (expectedParty != null && expectedParty.IsActive)
				{
					expectedPoint = expectedParty.Position;
				}
				if (IsValidCampaignPosition(expectedPoint) && IsValidCampaignPosition(courier.TargetPosition) && courier.TargetPosition.DistanceSquared(expectedPoint) > 1f)
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool ShouldRefreshRouteCore(CourierSession session, string routeKey, MobileParty courier, bool monitorProgress, params AiBehavior[] expectedDefaultBehaviors)
	{
		if (session == null)
		{
			return false;
		}
		string key = routeKey ?? "";
		if (!string.Equals(session.LastRouteKey ?? "", key, StringComparison.OrdinalIgnoreCase))
		{
			session.LastRouteKey = key;
			ResetCourierRouteProgress(session, key, courier);
			return true;
		}
		if (monitorProgress && IsCourierRouteStuck(session, key, courier))
		{
			return true;
		}
		if (courier != null && expectedDefaultBehaviors != null && expectedDefaultBehaviors.Length > 0 && !expectedDefaultBehaviors.Contains(courier.DefaultBehavior))
		{
			LogVerbose("route_refresh_forced:" + session.Id, "route refresh forced session=" + session.Id + " key=" + key + " defaultBehavior=" + courier.DefaultBehavior + " shortTerm=" + courier.ShortTermBehavior, 5.0);
			LogCourierStatusVerbose("route_refresh_forced:" + session.Id, "route_refresh_forced session=" + session.Id + " key=" + key + " expected=" + string.Join(",", expectedDefaultBehaviors.Select(x => x.ToString())) + " courier=" + DescribeMobileParty(courier), 5.0);
			return true;
		}
		return false;
	}

	private static void ResetCourierRouteProgress(CourierSession session, string routeKey, MobileParty courier)
	{
		if (session == null)
		{
			return;
		}
		session.LastProgressRouteKey = routeKey ?? "";
		if (courier != null)
		{
			session.LastProgressX = courier.Position.X;
			session.LastProgressY = courier.Position.Y;
		}
		session.LastProgressCampaignHours = GetCampaignHours();
		session.NavalStuckRefreshCount = 0;
	}

	private static bool IsCourierRouteStuck(CourierSession session, string routeKey, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return false;
		}
		double nowHours = GetCampaignHours();
		if (nowHours <= 0)
		{
			return false;
		}
		if (!string.Equals(session.LastProgressRouteKey ?? "", routeKey ?? "", StringComparison.OrdinalIgnoreCase) || session.LastProgressCampaignHours <= 0)
		{
			ResetCourierRouteProgress(session, routeKey, courier);
			return false;
		}
		float dx = courier.Position.X - session.LastProgressX;
		float dy = courier.Position.Y - session.LastProgressY;
		float distanceSquared = dx * dx + dy * dy;
		if (distanceSquared > NavalStuckDistanceSquared)
		{
			ResetCourierRouteProgress(session, routeKey, courier);
			return false;
		}
		double elapsedHours = nowHours - session.LastProgressCampaignHours;
		if (elapsedHours < NavalStuckRefreshHours)
		{
			return false;
		}
		session.LastProgressCampaignHours = nowHours;
		session.NavalStuckRefreshCount++;
		session.LastRouteKey = "";
		LogVerbose("naval_route_stuck:" + session.Id, "naval route stuck session=" + session.Id + " key=" + (routeKey ?? "") + " count=" + session.NavalStuckRefreshCount + " distanceSquared=" + distanceSquared, 1.0);
		LogCourierStatus("naval_route_stuck session=" + session.Id + " key=" + (routeKey ?? "") + " count=" + session.NavalStuckRefreshCount + " distanceSquared=" + distanceSquared + " elapsedHours=" + elapsedHours + " courier=" + DescribeMobileParty(courier));
		return true;
	}

	private static double GetCampaignHours()
	{
		try
		{
			return CampaignTime.Now.ToHours;
		}
		catch
		{
			return 0;
		}
	}

	private static List<object> BuildCourierReplyMessages(Hero recipient, CourierSession session, string extras, string deliveryFactForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null)
	{
		string npcName = recipient?.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string deliveryFact = string.IsNullOrWhiteSpace(deliveryFactForPrompt) ? (session?.DeliveryFactText ?? "") : deliveryFactForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(recipient, 24, session.LetterText, deliveryFact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(recipient, 6);
		string senderIdentity = MyBehavior.BuildPlayerCourierSenderIdentityForExternal(recipient);
		string senderRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(recipient);
		string currentLocationLine = BuildCourierCurrentLocationLine(recipient);
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你刚刚通过信使收到" + playerName + "写给你的一封信。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含来信、玩家发言、事实、旁听内容与规则。\n"
			+ "你必须根据来信者的公开身份选择合适称呼；如果来信者是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在回信中的正文，不要写旁白、动作描写、系统说明或标签解释。\n"
			+ "如果你认为没有必要回信，可以完全空回复。\n"
			+ "如果你在回信中明确同意给玩家物品、部队、俘虏或固定资产，仍然按已注入的后处理规则在正文语义中表达，标签由后处理阶段生成。";
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(senderIdentity))
		{
			context.AppendLine(senderIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(senderRelationship))
		{
			AppendCourierUserSection(context, "【来信者与你的关系】", senderRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【信件内容】");
		current.AppendLine(session.LetterText ?? "");
		if (!string.IsNullOrWhiteSpace(deliveryFact))
		{
			current.AppendLine();
			current.AppendLine("【随信送达事实】");
			current.AppendLine("【当下行为】" + deliveryFact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份决定是否回信；如果回信，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	private static List<object> BuildInboundNpcLetterMessages(Hero sender, CourierSession session, string seed, string extras, string factForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null)
	{
		string npcName = sender?.Name?.ToString() ?? session?.SenderName ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string fact = string.IsNullOrWhiteSpace(factForPrompt) ? (session?.DeliveryFactText ?? "") : factForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(sender, 24, seed, fact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(sender, 6);
		string playerIdentity = MyBehavior.BuildPlayerCourierSenderIdentityForExternal(sender);
		string playerRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(sender);
		string currentLocationLine = BuildCourierCurrentLocationLine(sender);
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你决定主动通过信使给" + playerName + "写一封外交信。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含历史、事实、旁听内容、规则与本次写信意图。\n"
			+ "你必须根据收信人的公开身份选择合适称呼；如果收信人是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在信中的正文，不要写旁白、动作描写、系统说明、标签解释或方括号动作标签。\n"
			+ "你可以提出条件、邀请谈判、表明立场或请求回信，但不能宣布" + playerName + "已经同意，也不能把尚未发生的外交结果写成事实。";
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(playerIdentity))
		{
			context.AppendLine(playerIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(playerRelationship))
		{
			AppendCourierUserSection(context, "【收信人与你的关系】", playerRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【本次主动写信意图或草稿】");
		current.AppendLine(seed ?? "");
		if (!string.IsNullOrWhiteSpace(fact))
		{
			current.AppendLine();
			current.AppendLine("【送信事实】");
			current.AppendLine("【当下行为】" + fact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份写给" + playerName + "一封会由信使送达的信，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	private static string BuildCourierCurrentLocationLine(Hero recipient)
	{
		try
		{
			MobileParty party = ResolveCourierPromptParty(recipient);
			Settlement currentSettlement = recipient?.CurrentSettlement ?? party?.CurrentSettlement;
			if (currentSettlement != null)
			{
				return "你当前位于" + FormatCourierSettlementNameWithType(currentSettlement) + "。";
			}
			if (party == null)
			{
				return "";
			}
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				Settlement nearest = FindNearestSettlementForCourierPrompt(party);
				string nearestName = FormatCourierSettlementNameWithType(nearest);
				string locationLine = string.IsNullOrWhiteSpace(nearestName)
					? "你正位于海上。"
					: "你正位于" + nearestName + "附近的海上。";
				string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(party);
				if (!string.IsNullOrWhiteSpace(shipText))
				{
					locationLine += "舰船：" + shipText + "。";
				}
				return locationLine;
			}
			string terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
			if (string.IsNullOrWhiteSpace(terrainLabel))
			{
				terrainLabel = "野外";
			}
			Settlement landNearest = FindNearestSettlementForCourierPrompt(party);
			string landNearestName = FormatCourierSettlementNameWithType(landNearest);
			return string.IsNullOrWhiteSpace(landNearestName)
				? "你当前位于" + terrainLabel + "。"
				: "你当前位于" + landNearestName + "附近的" + terrainLabel + "。";
		}
		catch
		{
			return "";
		}
	}

	private static MobileParty ResolveCourierPromptParty(Hero hero)
	{
		try
		{
			if (hero?.PartyBelongedTo != null && hero.PartyBelongedTo.IsActive)
			{
				return hero.PartyBelongedTo;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase prisonerParty = hero?.PartyBelongedToAsPrisoner;
			if (prisonerParty?.MobileParty != null && prisonerParty.MobileParty.IsActive)
			{
				return prisonerParty.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement FindNearestSettlementForCourierPrompt(MobileParty party)
	{
		return MapSeaContextGuard.FindNearestSettlementForPrompt(party);
	}

	private static string FormatCourierSettlementNameWithType(Settlement settlement)
	{
		return MapSeaContextGuard.FormatSettlementNameWithTypeForPrompt(settlement);
	}

	private static object CreateCourierChatMessage(string role, string content)
	{
		return new
		{
			role = role ?? "",
			content = content ?? ""
		};
	}

	private static void AppendCourierRawUserSection(StringBuilder builder, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		builder.AppendLine(content.Trim());
	}

	private static void AppendCourierUserSection(StringBuilder builder, string title, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		if (!string.IsNullOrWhiteSpace(title))
		{
			builder.AppendLine(title.Trim());
		}
		builder.AppendLine(content.Trim());
	}

	private static void AppendCourierPersistentMemoryRoleMessages(List<object> messages, IEnumerable<ConversationMessage> historyMessages, string npcName, string playerName)
	{
		if (messages == null || historyMessages == null)
		{
			return;
		}
		foreach (ConversationMessage message in historyMessages.Where((ConversationMessage x) => x != null && !string.IsNullOrWhiteSpace(x.Content)).Take(24))
		{
			if (TryConvertCourierMemoryMessageToChatMessage(message, npcName, playerName, out var chatMessage))
			{
				messages.Add(chatMessage);
			}
		}
	}

	private static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker);
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && IsCourierMemorySpeakerRecipient(speaker, npcName))
		{
			chatMessage = CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName));
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往行为】" + StripCourierPromptScopeLabel(content));
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? "某NPC" : speaker;
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往听闻】" + otherSpeaker + "说：" + StripCourierSpeakerPrefix(content, otherSpeaker));
			return true;
		}
		string player = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
		chatMessage = CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, player));
		return true;
	}

	private static bool IsCourierMemorySpeakerRecipient(string speaker, string npcName)
	{
		string left = (speaker ?? "").Trim();
		string right = (npcName ?? "").Trim();
		return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
	{
		string date = string.IsNullOrWhiteSpace(message?.GameDate) ? ("第" + Math.Max(0, message?.GameDayIndex ?? 0) + "日") : message.GameDate.Trim();
		int hour = Math.Max(0, Math.Min(23, message?.GameHour ?? 0));
		string scene = (message?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string speaker = string.IsNullOrWhiteSpace(fallbackSpeaker) ? "记录" : fallbackSpeaker.Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			return "[" + date + " " + hour + "时｜" + speaker + "] ";
		}
		return "[" + date + " " + hour + "时｜" + scene + "｜" + speaker + "] ";
	}

	private static string StripCourierPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		bool changed;
		do
		{
			changed = false;
			string[] prefixes = new string[4] { "【当下行为】", "【过往行为】", "[当下行为]", "[过往行为]" };
			foreach (string prefix in prefixes)
			{
				if (value.StartsWith(prefix, StringComparison.Ordinal))
				{
					value = value.Substring(prefix.Length).Trim();
					changed = true;
				}
			}
		}
		while (changed);
		return value;
	}

	private static string StripCourierSpeakerPrefix(string content, string speaker)
	{
		string text = (content ?? "").Trim();
		string name = (speaker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(name))
		{
			return text;
		}
		string[] prefixes = new string[2] { name + ": ", name + "：" };
		foreach (string prefix in prefixes)
		{
			if (text.StartsWith(prefix, StringComparison.Ordinal))
			{
				return text.Substring(prefix.Length).Trim();
			}
		}
		return text;
	}

	private static string BuildDeliveryFactText(CourierSession session, bool delivered, Hero recipient = null)
	{
		if (session == null)
		{
			return "";
		}
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		StringBuilder sb = new StringBuilder();
		if (delivered)
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("通过信使向你寄来一封信。");
		}
		else
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("已安排信使携带一封信出发。");
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			string verb = delivered ? "通过信使" : "准备通过信使";
			if (entry.Kind == "gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 第纳尔").Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 第纳尔，但没有转移所有权。");
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name).Append("，但没有转移所有权。");
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append(entry.IsHero ? "转移了俘虏 " : "转移了俘虏 ").Append(entry.IsHero ? entry.Name : (entry.Amount + " 名 " + entry.Name)).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了固定资产 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
		}
		return sb.ToString().Trim();
	}

	private static string BuildInboundDeliveryFactText(CourierSession session, bool delivered, Hero sender = null)
	{
		if (session == null)
		{
			return "";
		}
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		return delivered
			? "[AFEF NPC行为补充] " + senderName + "通过信使给" + playerName + "送来一封外交信。"
			: "[AFEF NPC行为补充] " + senderName + "已经派出信使，准备把一封外交信送给" + playerName + "。";
	}

	private static string BuildCourierCargoValueSuffix(CourierCargoEntry entry)
	{
		long value = EstimateCourierCargoEntryTotalValue(entry);
		return value > 0L ? ("（估值约 " + value + " 第纳尔）") : "";
	}

	private static long EstimateCourierCargoEntryTotalValue(CourierCargoEntry entry)
	{
		if (entry == null || entry.Amount <= 0)
		{
			return 0L;
		}
		string kind = (entry.Kind ?? "").Trim();
		if (string.Equals(kind, "gold", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Max(0, entry.Amount);
		}
		int unitValue = Math.Max(0, entry.GuidePriceDenars);
		if (unitValue <= 0 && string.Equals(kind, "item", StringComparison.OrdinalIgnoreCase))
		{
			unitValue = EstimateCourierItemUnitValue(ResolveItem(entry.Id));
		}
		if (unitValue <= 0)
		{
			return 0L;
		}
		int amount = string.Equals(kind, "settlement", StringComparison.OrdinalIgnoreCase) || entry.IsHero ? 1 : Math.Max(1, entry.Amount);
		return (long)amount * unitValue;
	}

	private static string BuildPendingPayloadSummary(PendingCourierFlow flow)
	{
		if (flow == null)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("信使成员：");
		sb.AppendLine("  " + RosterSummary(flow.CrewRoster));
		if (flow.Mode == CourierPayloadMode.Normal || flow.SelectedEntries.Count == 0)
		{
			sb.AppendLine("随信内容：仅信件。");
			return sb.ToString().Trim();
		}
		sb.AppendLine("随信内容：");
		foreach (CourierCargoEntry entry in flow.SelectedEntries)
		{
			if (entry == null)
			{
				continue;
			}
			sb.Append("  · ");
			if (entry.Kind == "gold")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("转移 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name);
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append(entry.IsHero ? ("转移俘虏 " + entry.Name) : ("转移 " + entry.Amount + " 名 " + entry.Name + " 俘虏"));
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("转移固定资产 ").Append(entry.Name);
			}
			sb.AppendLine();
		}
		return sb.ToString().Trim();
	}

	private List<CourierTradeOption> BuildCourierTradeOptions(PendingCourierFlow flow, CourierPayloadMode mode)
	{
		List<CourierTradeOption> list = new List<CourierTradeOption>();
		if (flow?.Recipient == null)
		{
			return list;
		}
		if (mode == CourierPayloadMode.GiveTroops || mode == CourierPayloadMode.GivePrisoners)
		{
			List<MyBehavior.PartyTransferPromptEntry> entries = MyBehavior.BuildPartyTransferPromptEntriesForExternal(flow.Recipient, flow.Recipient.CharacterObject, -1);
			MyBehavior.PartyTransferEntrySection section = mode == CourierPayloadMode.GiveTroops ? MyBehavior.PartyTransferEntrySection.PlayerTroops : MyBehavior.PartyTransferEntrySection.PlayerPrisoners;
			foreach (MyBehavior.PartyTransferPromptEntry entry in entries.Where(x => x != null && x.Section == section))
			{
				int available = Math.Max(0, entry.Count);
				if (mode == CourierPayloadMode.GiveTroops)
				{
					available -= CountRosterCharacter(flow.CrewRoster, entry.Character);
				}
				if (available <= 0)
				{
					continue;
				}
				list.Add(new CourierTradeOption
				{
					Kind = mode == CourierPayloadMode.GiveTroops ? "troop" : "prisoner",
					Id = entry.Character?.StringId ?? "",
					Name = entry.DisplayName,
					AvailableAmount = available,
					GuidePriceDenars = mode == CourierPayloadMode.GiveTroops ? Math.Max(1, entry.HirePriceDenarsPerUnit) : Math.Max(1, entry.BuyPriceDenarsPerUnit),
					PartyEntry = entry
				});
			}
			return list;
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			foreach (MyBehavior.SettlementTransferPromptEntry entry in MyBehavior.BuildSettlementTransferPromptEntriesForExternal(flow.Recipient, flow.Recipient.CharacterObject).Where(x => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)))
			{
				list.Add(new CourierTradeOption
				{
					Kind = "settlement",
					Id = MyBehavior.GetSettlementTransferAssetIdForExternal(entry),
					Name = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(entry),
					AvailableAmount = 1,
					GuidePriceDenars = Math.Max(0, entry.GuidePriceDenars),
					SettlementEntry = entry
				});
			}
			return list;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			return list;
		}
		string shownKey = BuildCourierShownTargetKey(flow.Recipient);
		int gold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		if (mode == CourierPayloadMode.Show)
		{
			gold = MyBehavior.GetRemainingShowableGoldForExternal(flow.Recipient, shownKey, gold);
		}
		if (gold > 0)
		{
			list.Add(new CourierTradeOption
			{
				Kind = mode == CourierPayloadMode.Show ? "show_gold" : "gold",
				Id = "gold",
				Name = "第纳尔",
				AvailableAmount = gold,
				GuidePriceDenars = 1
			});
		}
		ItemRoster itemRoster = mainParty.ItemRoster;
		if (itemRoster == null)
		{
			return list;
		}
		Dictionary<string, CourierTradeOption> byItem = new Dictionary<string, CourierTradeOption>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement element = itemRoster.GetElementCopyAtIndex(i);
			ItemObject item = element.EquipmentElement.Item;
			string id = (item?.StringId ?? "").Trim();
			if (item == null || string.IsNullOrWhiteSpace(id) || element.Amount <= 0)
			{
				continue;
			}
			if (!byItem.TryGetValue(id, out var option))
			{
				option = new CourierTradeOption
				{
					Kind = mode == CourierPayloadMode.Show ? "show_item" : "item",
					Id = id,
					Name = item.Name?.ToString() ?? id,
					AvailableAmount = 0,
					GuidePriceDenars = EstimateCourierItemUnitValue(item),
					Item = item
				};
				byItem[id] = option;
			}
			option.AvailableAmount += element.Amount;
		}
		foreach (CourierTradeOption option in byItem.Values)
		{
			int available = option.AvailableAmount;
			if (mode == CourierPayloadMode.Show)
			{
				available = MyBehavior.GetRemainingShowableItemCountForExternal(flow.Recipient, shownKey, option.Id, available);
			}
			if (available > 0)
			{
				option.AvailableAmount = available;
				list.Add(option);
			}
		}
		return list;
	}

	private static int EstimateCourierItemUnitValue(ItemObject item)
	{
		if (item == null)
		{
			return 0;
		}
		try
		{
			long value = RewardSystemBehavior.Instance?.EstimateItemValueForExternal(Hero.MainHero, item, 1) ?? 0L;
			if (value > 0L)
			{
				return (int)Math.Min(int.MaxValue, value);
			}
		}
		catch
		{
		}
		return Math.Max(1, item.Value);
	}

	private static int EstimateCourierPartyTransferUnitValue(Hero recipient, CourierCargoEntry entry, bool isPrisoner)
	{
		try
		{
			MyBehavior.PartyTransferEntrySection section = isPrisoner ? MyBehavior.PartyTransferEntrySection.PlayerPrisoners : MyBehavior.PartyTransferEntrySection.PlayerTroops;
			string id = (entry?.Id ?? "").Trim();
			string name = (entry?.Name ?? "").Trim();
			MyBehavior.PartyTransferPromptEntry match = MyBehavior.BuildPartyTransferPromptEntriesForExternal(recipient, recipient?.CharacterObject, -1)
				.FirstOrDefault(x => x != null && x.Section == section && (string.Equals((x.Character?.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase)));
			if (match == null)
			{
				return 0;
			}
			return isPrisoner ? Math.Max(1, match.BuyPriceDenarsPerUnit) : Math.Max(1, match.HirePriceDenarsPerUnit);
		}
		catch
		{
			return 0;
		}
	}

	private static int EstimateCourierSettlementTransferValue(Hero recipient, CourierCargoEntry entry)
	{
		try
		{
			string id = (entry?.Id ?? "").Trim();
			string name = (entry?.Name ?? "").Trim();
			MyBehavior.SettlementTransferPromptEntry match = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(recipient, recipient?.CharacterObject)
				.FirstOrDefault(x => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x) && (string.Equals(MyBehavior.GetSettlementTransferAssetIdForExternal(x), id, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase) || string.Equals(MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(x), name, StringComparison.OrdinalIgnoreCase)));
			return Math.Max(0, match?.GuidePriceDenars ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string BuildEmptyPayloadMessage(CourierPayloadMode mode)
	{
		if (mode == CourierPayloadMode.GiveTroops)
		{
			return "你当前没有可转移给对方的部队。";
		}
		if (mode == CourierPayloadMode.GivePrisoners)
		{
			return "你当前没有可转移给对方的俘虏。";
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			return "你当前没有可转移给对方的固定资产。";
		}
		return "你没有可用的物品或第纳尔。";
	}

	private static string BuildPayloadTitle(CourierPayloadMode mode, string targetName)
	{
		string prefix = mode == CourierPayloadMode.Give ? "发送物品并写信" : mode == CourierPayloadMode.Show ? "展示物品并写信" : mode == CourierPayloadMode.GiveTroops ? "转移部队并写信" : mode == CourierPayloadMode.GivePrisoners ? "转移俘虏并写信" : "转移固定资产并写信";
		return prefix + " - " + targetName;
	}

	private static string BuildPayloadDescription(CourierPayloadMode mode, string targetName)
	{
		if (mode == CourierPayloadMode.GiveTroops)
		{
			return "当前收件人：" + targetName + "\n选择要随信转入对方麾下的部队（可多选）：";
		}
		if (mode == CourierPayloadMode.GivePrisoners)
		{
			return "当前收件人：" + targetName + "\n选择要随信交给对方的俘虏（可多选）：";
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			return "当前收件人：" + targetName + "\n选择要随信转给对方的固定资产（可多选）：";
		}
		return "当前收件人：" + targetName + "\n选择要" + (mode == CourierPayloadMode.Show ? "展示" : "发送") + "的物品或第纳尔（可多选）：";
	}

	private static TroopRoster BuildSelectableCrewRoster(TroopRoster source)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in SnapshotRoster(source))
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			roster.AddToCounts(character, item.Number, false, item.WoundedNumber, item.Xp, true, -1);
		}
		return roster;
	}

	private static TroopRoster BuildSelectionRosterFromUi(TroopRoster source)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in SnapshotRoster(source))
		{
			if (item.Character == null || item.Number <= 0 || item.Character.IsPlayerCharacter)
			{
				continue;
			}
			roster.AddToCounts(item.Character, item.Number, false, item.WoundedNumber, item.Xp, true, -1);
		}
		return roster;
	}

	private static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		if (roster == null)
		{
			return list;
		}
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			list.Add(item);
		}
		return list;
	}

	private static List<CourierCargoEntry> BuildCargoEntriesFromRoster(TroopRoster roster, string kind)
	{
		List<CourierCargoEntry> list = new List<CourierCargoEntry>();
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			list.Add(new CourierCargoEntry
			{
				Kind = kind,
				Id = character.StringId ?? "",
				Name = character.Name?.ToString() ?? character.StringId ?? "",
				Amount = item.Number,
				IsHero = character.IsHero
			});
		}
		return list;
	}

	private static void MoveRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				AddHeroToPartyAction.Apply(character.HeroObject, targetParty, false);
				continue;
			}
			MoveRegularMember(MobileParty.MainParty?.Party, targetParty?.Party, character, item.Number, item.WoundedNumber, item.Xp);
		}
	}

	private static void MoveCharacterFromMainMembersToParty(string characterId, int amount, MobileParty targetParty, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || targetParty == null || amount <= 0)
		{
			return;
		}
		if (isHero || character.IsHero)
		{
			AddHeroToPartyAction.Apply(character.HeroObject, targetParty, false);
			return;
		}
		MoveRegularMember(MobileParty.MainParty?.Party, targetParty.Party, character, amount, -1, -1);
	}

	private static void MoveCharacterFromMainPrisonersToParty(string characterId, int amount, MobileParty targetParty, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || targetParty == null || amount <= 0)
		{
			return;
		}
		if (isHero || character.IsHero)
		{
			try
			{
				TransferPrisonerAction.Apply(character, MobileParty.MainParty?.Party, targetParty.Party);
			}
			catch
			{
			}
			return;
		}
		MoveRegularPrisoner(MobileParty.MainParty?.Party, targetParty.Party, character, amount);
	}

	private static int MoveCharacterBetweenMemberRosters(PartyBase source, PartyBase target, string characterId, int amount, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || source == null || target == null || amount <= 0)
		{
			return 0;
		}
		if (isHero || character.IsHero)
		{
			try
			{
				AddHeroToPartyAction.Apply(character.HeroObject, target.MobileParty, false);
				return 1;
			}
			catch
			{
				return 0;
			}
		}
		return MoveRegularMember(source, target, character, amount, -1, -1);
	}

	private static int MoveCharacterBetweenPrisonRosters(PartyBase source, PartyBase target, string characterId, int amount, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || source == null || target == null || amount <= 0)
		{
			return 0;
		}
		if (isHero || character.IsHero)
		{
			try
			{
				TransferPrisonerAction.Apply(character, source, target);
				return 1;
			}
			catch
			{
				return 0;
			}
		}
		return MoveRegularPrisoner(source, target, character, amount);
	}

	private static int MoveRegularMember(PartyBase source, PartyBase target, CharacterObject character, int amount, int woundedOverride, int xpOverride)
	{
		TroopRoster sourceRoster = source?.MemberRoster;
		TroopRoster targetRoster = target?.MemberRoster;
		if (sourceRoster == null || targetRoster == null || character == null || amount <= 0)
		{
			return 0;
		}
		int index = sourceRoster.FindIndexOfTroop(character);
		if (index < 0)
		{
			return 0;
		}
		TroopRosterElement sourceElement = sourceRoster.GetElementCopyAtIndex(index);
		int moved = Math.Min(amount, Math.Max(0, sourceElement.Number));
		if (moved <= 0)
		{
			return 0;
		}
		int wounded = woundedOverride >= 0 ? Math.Min(woundedOverride, moved) : CalculateProportional(sourceElement.WoundedNumber, sourceElement.Number, moved);
		int xp = xpOverride >= 0 ? Math.Min(xpOverride, sourceElement.Xp) : CalculateProportional(sourceElement.Xp, sourceElement.Number, moved);
		sourceRoster.AddToCounts(character, -moved, false, -wounded, -xp, true, -1);
		targetRoster.AddToCounts(character, moved, false, wounded, xp, true, -1);
		return moved;
	}

	private static int MoveRegularPrisoner(PartyBase source, PartyBase target, CharacterObject character, int amount)
	{
		TroopRoster sourceRoster = source?.PrisonRoster;
		if (sourceRoster == null || target == null || character == null || amount <= 0)
		{
			return 0;
		}
		int index = sourceRoster.FindIndexOfTroop(character);
		if (index < 0)
		{
			return 0;
		}
		TroopRosterElement sourceElement = sourceRoster.GetElementCopyAtIndex(index);
		int moved = Math.Min(amount, Math.Max(0, sourceElement.Number));
		if (moved <= 0)
		{
			return 0;
		}
		int xp = CalculateProportional(sourceElement.Xp, sourceElement.Number, moved);
		sourceRoster.AddToCounts(character, -moved, false, 0, -xp, true, -1);
		target.AddPrisoner(character, moved);
		if (xp > 0)
		{
			target.PrisonRoster?.AddXpToTroop(character, xp);
		}
		return moved;
	}

	private static void MoveWholeMemberRoster(PartyBase source, PartyBase target)
	{
		foreach (TroopRosterElement item in SnapshotRoster(source?.MemberRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					AddHeroToPartyAction.Apply(character.HeroObject, target.MobileParty, false);
				}
				catch
				{
				}
			}
			else
			{
				MoveRegularMember(source, target, character, item.Number, item.WoundedNumber, item.Xp);
			}
		}
	}

	private static void MoveWholePrisonRoster(PartyBase source, PartyBase target)
	{
		foreach (TroopRosterElement item in SnapshotRoster(source?.PrisonRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					TransferPrisonerAction.Apply(character, source, target);
				}
				catch
				{
				}
			}
			else
			{
				MoveRegularPrisoner(source, target, character, item.Number);
			}
		}
	}

	private static void MoveWholeItemRoster(ItemRoster source, ItemRoster target)
	{
		if (source == null || target == null)
		{
			return;
		}
		for (int i = source.Count - 1; i >= 0; i--)
		{
			ItemRosterElement element = source.GetElementCopyAtIndex(i);
			if (element.Amount <= 0 || element.EquipmentElement.Item == null)
			{
				continue;
			}
			source.AddToCounts(element.EquipmentElement, -element.Amount);
			target.AddToCounts(element.EquipmentElement, element.Amount);
		}
	}

	private static int CalculateProportional(int totalValue, int totalCount, int movedCount)
	{
		if (totalValue <= 0 || totalCount <= 0 || movedCount <= 0)
		{
			return 0;
		}
		if (movedCount >= totalCount)
		{
			return totalValue;
		}
		return Math.Max(0, Math.Min(totalValue, (int)Math.Round((double)totalValue * movedCount / totalCount, MidpointRounding.AwayFromZero)));
	}

	private static CharacterObject ResolveCharacter(string characterId)
	{
		string id = (characterId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return MBObjectManager.Instance?.GetObject<CharacterObject>(id) ?? Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>()?.FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static ItemObject ResolveItem(string itemId)
	{
		string id = (itemId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(id);
		}
		catch
		{
			return null;
		}
	}

	private static PartyBase ResolveRecipientPartyBase(Hero recipient)
	{
		if (recipient?.PartyBelongedTo?.Party != null)
		{
			return recipient.PartyBelongedTo.Party;
		}
		if (recipient?.PartyBelongedToAsPrisoner != null)
		{
			return recipient.PartyBelongedToAsPrisoner;
		}
		if (recipient?.Clan?.Leader?.PartyBelongedTo?.Party != null)
		{
			return recipient.Clan.Leader.PartyBelongedTo.Party;
		}
		if (recipient?.CurrentSettlement?.Town?.GarrisonParty?.Party != null)
		{
			return recipient.CurrentSettlement.Town.GarrisonParty.Party;
		}
		return recipient?.CurrentSettlement?.Party;
	}

	private static ItemRoster ResolveRecipientItemRoster(Hero recipient)
	{
		if (recipient?.PartyBelongedTo?.ItemRoster != null)
		{
			return recipient.PartyBelongedTo.ItemRoster;
		}
		if (recipient?.CurrentSettlement?.ItemRoster != null)
		{
			return recipient.CurrentSettlement.ItemRoster;
		}
		return recipient?.Clan?.Leader?.PartyBelongedTo?.ItemRoster;
	}

	private static int CountRosterCharacter(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return 0;
		}
		int index = roster.FindIndexOfTroop(character);
		return index < 0 ? 0 : Math.Max(0, roster.GetElementCopyAtIndex(index).Number);
	}

	private static void ApplyCourierAiOverrides(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			EnsureCourierNonSettlementCombatState(courier, reason);
			if (courier.Ai != null)
			{
				if (!courier.Ai.DoNotMakeNewDecisions)
				{
					courier.Ai.SetDoNotMakeNewDecisions(true);
					LogVerbose("ai_lock:" + (courier.StringId ?? ""), "ai decisions locked party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""), 10.0);
					LogCourierStatusVerbose("ai_lock:" + (courier.StringId ?? ""), "ai_lock sessionParty=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier), 10.0);
				}
			}
			ApplyCourierFoodOverrides(courier, reason);
			ApplyCourierMapBannerVisual(courier, reason);
		}
		catch (Exception ex)
		{
			Log("ai override failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void EnsureCourierNonSettlementCombatState(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			bool forbiddenBehavior = IsCourierForbiddenSettlementCombatBehavior(courier.DefaultBehavior)
				|| IsCourierForbiddenSettlementCombatBehavior(courier.ShortTermBehavior);
			bool inSiege = courier.BesiegerCamp != null || courier.SiegeEvent != null || courier.BesiegedSettlement != null;
			if (!forbiddenBehavior && !inSiege)
			{
				return;
			}
			string before = DescribeMobileParty(courier);
			if (courier.BesiegerCamp != null)
			{
				try
				{
					courier.BesiegerCamp = null;
				}
				catch (Exception ex)
				{
					Log("courier siege detach failed party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
				}
			}
			if (forbiddenBehavior || courier.BesiegerCamp != null || courier.SiegeEvent != null || courier.BesiegedSettlement != null)
			{
				courier.SetMoveModeHold();
			}
			LogCourierStatusVerbose("settlement_combat_suppressed:" + (courier.StringId ?? ""), "settlement_combat_suppressed party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " before=" + before + " after=" + DescribeMobileParty(courier), 5.0);
		}
		catch (Exception ex)
		{
			Log("courier settlement combat guard failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsCourierForbiddenSettlementCombatBehavior(AiBehavior behavior)
	{
		return behavior == AiBehavior.BesiegeSettlement
			|| behavior == AiBehavior.AssaultSettlement
			|| behavior == AiBehavior.RaidSettlement
			|| behavior == AiBehavior.DefendSettlement;
	}

	private static void ApplyCourierMapBannerVisual(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			if (Clan.PlayerClan != null)
			{
				courier.ActualClan = Clan.PlayerClan;
			}
			courier.IsVisible = true;
			courier.IsInspected = true;
			EnsureCourierVisualTracked(courier, reason);
			if (!courier.IsCurrentlyUsedByAQuest)
			{
				courier.SetPartyUsedByQuest(true);
				LogVerbose("tracker_quest_flag:" + (courier.StringId ?? ""), "map tracker quest flag applied party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""), 10.0);
			}
			else
			{
				PulseCourierTrackerQuestEvent(courier, reason);
			}
			courier.Party?.SetVisualAsDirty();
			if (string.Equals(reason ?? "", "create", StringComparison.OrdinalIgnoreCase) || string.Equals(reason ?? "", "load_restore", StringComparison.OrdinalIgnoreCase))
			{
				Log("banner visual applied party=" + (courier.StringId ?? "") + " clan=" + (courier.ActualClan?.StringId ?? "null") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("banner visual failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void EnsureCourierVisualTracked(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || !IsCourierParty(courier) || Campaign.Current?.VisualTrackerManager == null)
			{
				return;
			}
			bool beforeTracked = Campaign.Current.VisualTrackerManager.CheckTracked(courier);
			LogCourierTrackerSnapshot(courier, reason, "before_register", beforeTracked);
			if (!beforeTracked)
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(courier);
				Log("map tracker registered party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""));
			}
			LogCourierTrackerSnapshot(courier, reason, "after_register", Campaign.Current.VisualTrackerManager.CheckTracked(courier));
		}
		catch (Exception ex)
		{
			Log("map tracker register failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void PulseCourierTrackerQuestEvent(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || !IsCourierParty(courier) || Campaign.Current?.VisualTrackerManager == null || !Campaign.Current.VisualTrackerManager.CheckTracked(courier))
			{
				return;
			}
			string id = courier.StringId ?? "";
			long now = DateTime.UtcNow.Ticks;
			if (LastTrackerEventPulseTicks.TryGetValue(id, out long last) && now - last < TimeSpan.FromSeconds(8).Ticks)
			{
				return;
			}
			LastTrackerEventPulseTicks[id] = now;
			courier.SetPartyUsedByQuest(false);
			courier.SetPartyUsedByQuest(true);
			LogVerbose("tracker_pulse:" + id, "map tracker quest event pulsed party=" + id + " reason=" + (reason ?? ""), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker quest event pulse failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void LogCourierTrackerSnapshot(MobileParty courier, string reason, string phase, bool tracked)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			LogVerbose("tracker_snapshot:" + (courier.StringId ?? "") + ":" + (phase ?? ""), "map tracker snapshot phase=" + (phase ?? "") +
				" party=" + (courier.StringId ?? "") +
				" reason=" + (reason ?? "") +
				" tracked=" + tracked +
				" questUsed=" + courier.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (courier.LeaderHero == null) +
				" active=" + courier.IsActive +
				" visible=" + courier.IsVisible +
				" inspected=" + courier.IsInspected +
				" usedByQuest=" + courier.IsCurrentlyUsedByAQuest +
				" actualClan=" + (courier.ActualClan?.StringId ?? "null") +
				" mapFaction=" + (courier.MapFaction?.StringId ?? "null") +
				" component=" + (courier.PartyComponent?.GetType().FullName ?? "null"), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker snapshot failed party=" + (courier?.StringId ?? "") + " phase=" + (phase ?? "") + " error=" + ex.Message);
		}
	}

	private static void UntrackCourierMapVisual(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || Campaign.Current?.VisualTrackerManager == null)
			{
				return;
			}
			if (Campaign.Current.VisualTrackerManager.CheckTracked(courier))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(courier, true);
				Log("map tracker unregistered party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("map tracker unregister failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void TryPatchPartyNameplateForCourierBanner()
	{
		if (_partyNameplatePatchApplied || _partyNameplatePatchFailed)
		{
			return;
		}
		try
		{
			Harmony harmony = _courierHarmony ?? new Harmony("AnimusForge.courier.delivery");
			Type nameplateType = AccessTools.TypeByName("SandBox.ViewModelCollection.Nameplate.PartyNameplateVM");
			MethodInfo method = nameplateType == null ? null : AccessTools.Method(nameplateType, "RefreshBinding");
			MethodInfo postfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(PartyNameplateRefreshBindingCourierPostfix));
			if (method == null || postfix == null)
			{
				_partyNameplatePatchFailed = true;
				Log("party nameplate delayed patch skipped method_missing type=" + (nameplateType == null ? "null" : nameplateType.FullName));
				return;
			}
			harmony.Patch(method, postfix: new HarmonyMethod(postfix));
			_partyNameplatePatchApplied = true;
			Log("party nameplate delayed patch applied");
		}
		catch (Exception ex)
		{
			_partyNameplatePatchFailed = true;
			Log("party nameplate delayed patch failed: " + ex);
		}
	}

	public static void PartyNameplateRefreshBindingCourierPostfix(object __instance)
	{
		try
		{
			if (__instance == null)
			{
				return;
			}
			Type type = __instance.GetType();
			PropertyInfo partyProperty = type.GetProperty("Party", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MobileParty party = partyProperty?.GetValue(__instance, null) as MobileParty;
			if (!IsCourierParty(party))
			{
				return;
			}
			type.GetProperty("IsArmy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.GetProperty("ShouldShowFullName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.BaseType?.GetProperty("IsVisibleOnMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.GetProperty("PartyBanner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, CreateCourierBannerImageIdentifier(party), null);
		}
		catch
		{
		}
	}

	private static object CreateCourierBannerImageIdentifier(MobileParty party)
	{
		try
		{
			Banner banner = party?.Banner ?? Clan.PlayerClan?.Banner ?? Hero.MainHero?.Clan?.Banner;
			if (banner == null)
			{
				return null;
			}
			Type bannerVmType = AccessTools.TypeByName("TaleWorlds.Core.ViewModelCollection.ImageIdentifiers.BannerImageIdentifierVM");
			ConstructorInfo ctor = bannerVmType?.GetConstructor(new[] { typeof(Banner), typeof(bool) });
			return ctor?.Invoke(new object[] { banner, true });
		}
		catch
		{
			return null;
		}
	}

	private static void TryPatchMapTrackerProviderForCourierDiagnostics()
	{
		if (_mapTrackerProviderPatchApplied || _mapTrackerProviderPatchFailed)
		{
			return;
		}
		try
		{
			Harmony harmony = _courierHarmony ?? new Harmony("AnimusForge.courier.delivery");
			Type providerType = AccessTools.TypeByName("SandBox.ViewModelCollection.Map.Tracker.MapTrackerProvider");
			MethodInfo canAdd = providerType == null ? null : AccessTools.Method(providerType, "CanAddMobileParty", new[] { typeof(MobileParty) });
			MethodInfo addIfEligible = providerType == null ? null : AccessTools.Method(providerType, "AddIfEligible", new[] { typeof(MobileParty) });
			MethodInfo canAddPostfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(MapTrackerProviderCanAddMobilePartyCourierPostfix));
			MethodInfo addPostfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(MapTrackerProviderAddIfEligibleCourierPostfix));
			if (providerType == null || canAdd == null || addIfEligible == null || canAddPostfix == null || addPostfix == null)
			{
				_mapTrackerProviderPatchFailed = true;
				Log("map tracker provider diagnostics patch skipped type=" + (providerType?.FullName ?? "null") + " canAdd=" + (canAdd != null) + " addIfEligible=" + (addIfEligible != null));
				return;
			}
			harmony.Patch(canAdd, postfix: new HarmonyMethod(canAddPostfix));
			harmony.Patch(addIfEligible, postfix: new HarmonyMethod(addPostfix));
			_mapTrackerProviderPatchApplied = true;
			Log("map tracker provider diagnostics patch applied");
		}
		catch (Exception ex)
		{
			_mapTrackerProviderPatchFailed = true;
			Log("map tracker provider diagnostics patch failed: " + ex);
		}
	}

	public static void MapTrackerProviderCanAddMobilePartyCourierPostfix(MobileParty party, ref bool __result)
	{
		try
		{
			if (!IsCourierParty(party))
			{
				return;
			}
			bool tracked = Campaign.Current?.VisualTrackerManager != null && Campaign.Current.VisualTrackerManager.CheckTracked(party);
			if (tracked && party.IsActive)
			{
				__result = true;
			}
			LogVerbose("tracker_provider_can_add:" + (party.StringId ?? ""), "map tracker provider CanAddMobileParty courier party=" + (party.StringId ?? "") +
				" result=" + __result +
				" tracked=" + tracked +
				" questUsed=" + party.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (party.LeaderHero == null) +
				" active=" + party.IsActive +
				" forced=" + (tracked && party.IsActive) +
				" isQuestCondition=" + (party.LeaderHero == null && party.IsCurrentlyUsedByAQuest && tracked), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker provider CanAddMobileParty log failed: " + ex.Message);
		}
	}

	public static void MapTrackerProviderAddIfEligibleCourierPostfix(object __instance, MobileParty party)
	{
		try
		{
			if (!IsCourierParty(party))
			{
				return;
			}
			bool hasTracker = false;
			object container = __instance?.GetType().GetField("_trackerContainer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
			MethodInfo hasTrackerFor = container?.GetType().GetMethod("HasTrackerFor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (container != null && hasTrackerFor != null)
			{
				hasTracker = (bool)hasTrackerFor.Invoke(container, new object[] { party });
			}
			bool tracked = Campaign.Current?.VisualTrackerManager != null && Campaign.Current.VisualTrackerManager.CheckTracked(party);
			LogVerbose("tracker_provider_add:" + (party.StringId ?? ""), "map tracker provider AddIfEligible courier party=" + (party.StringId ?? "") +
				" hasTracker=" + hasTracker +
				" tracked=" + tracked +
				" questUsed=" + party.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (party.LeaderHero == null), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker provider AddIfEligible log failed: " + ex.Message);
		}
	}

	private static void ApplyCourierFoodOverrides(MobileParty courier, string reason)
	{
		try
		{
			PartyBase party = courier?.Party;
			if (party == null || party.RemainingFoodPercentage >= 0)
			{
				return;
			}
			int oldValue = party.RemainingFoodPercentage;
			party.RemainingFoodPercentage = 100;
			party.OnConsumedFood();
			LogVerbose("food_override:" + (courier.StringId ?? ""), "food override applied party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " oldRemaining=" + oldValue + " newRemaining=100", 30.0);
		}
		catch (Exception ex)
		{
			Log("food override failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void AssignCourierLeader(MobileParty courier)
	{
		try
		{
			Hero leader = SnapshotRoster(courier?.MemberRoster).Select(x => x.Character?.HeroObject).FirstOrDefault(x => x != null && !x.IsHumanPlayerCharacter && !x.IsDead);
			if (leader != null)
			{
				courier.PartyComponent?.ChangePartyLeader(leader);
				courier.Party.SetCustomOwner(leader);
			}
		}
		catch (Exception ex)
		{
			Log("assign leader failed: " + ex.Message);
		}
	}

	private static string BuildCourierShownTargetKey(Hero hero)
	{
		return "courier:" + (hero?.StringId ?? "").Trim().ToLowerInvariant();
	}

	private MobileParty ResolveCourierParty(CourierSession session)
	{
		string id = (session?.CourierPartyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			lock (_sessionLock)
			{
				if (_courierPartyCache.TryGetValue(id, out var cached) && cached != null && cached.IsActive && string.Equals((cached.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase))
				{
					return cached;
				}
			}
			MobileParty resolved = MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
			lock (_sessionLock)
			{
				if (resolved != null)
				{
					_courierPartyCache[id] = resolved;
				}
				else
				{
					_courierPartyCache.Remove(id);
				}
			}
			return resolved;
		}
		catch
		{
			return null;
		}
	}

	private Hero ResolveRecipient(CourierSession session)
	{
		string id = (session?.RecipientHeroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Hero.Find(id) ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private Hero ResolveSender(CourierSession session)
	{
		string id = (session?.SenderHeroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Hero.Find(id) ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static CourierStage ParseStage(string value)
	{
		if (Enum.TryParse(value ?? "", true, out CourierStage stage))
		{
			return stage;
		}
		return CourierStage.Outbound;
	}

	private static CourierPayloadMode ParsePayloadMode(string value)
	{
		if (Enum.TryParse(value ?? "", true, out CourierPayloadMode mode))
		{
			return mode;
		}
		return CourierPayloadMode.Normal;
	}

	private static bool IsTerminalStage(CourierSession session)
	{
		CourierStage stage = ParseStage(session?.Stage);
		return stage == CourierStage.Completed || stage == CourierStage.Destroyed;
	}

	private static void NormalizeSession(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		session.Id = (session.Id ?? "").Trim();
		session.Direction = NormalizeCourierDirection(session.Direction);
		session.SenderHeroId = (session.SenderHeroId ?? "").Trim();
		session.SenderName = session.SenderName ?? "";
		session.RecipientHeroId = (session.RecipientHeroId ?? "").Trim();
		session.RecipientName = session.RecipientName ?? "";
		session.CourierPartyId = (session.CourierPartyId ?? "").Trim();
		session.Stage = ParseStage(session.Stage).ToString();
		session.PayloadMode = ParsePayloadMode(session.PayloadMode).ToString();
		session.LastRouteKey = session.LastRouteKey ?? "";
		session.TemporaryShipHullId = (session.TemporaryShipHullId ?? "").Trim();
		session.LastProgressRouteKey = session.LastProgressRouteKey ?? "";
		session.Entries = session.Entries ?? new List<CourierCargoEntry>();
		session.CrewEntries = session.CrewEntries ?? new List<CourierCargoEntry>();
	}

	private static string NormalizeCourierDirection(string direction)
	{
		string value = (direction ?? "").Trim();
		if (string.Equals(value, CourierDirectionInboundToPlayer, StringComparison.OrdinalIgnoreCase))
		{
			return CourierDirectionInboundToPlayer;
		}
		return CourierDirectionOutbound;
	}

	private static bool IsInboundToPlayer(CourierSession session)
	{
		return string.Equals(NormalizeCourierDirection(session?.Direction), CourierDirectionInboundToPlayer, StringComparison.OrdinalIgnoreCase);
	}

	private static Dictionary<string, float> NormalizeFloatDictionary(Dictionary<string, float> source)
	{
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		if (source == null)
		{
			return result;
		}
		foreach (KeyValuePair<string, float> pair in source)
		{
			string key = (pair.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key))
			{
				result[key] = pair.Value;
			}
		}
		return result;
	}

	private static float NowHours()
	{
		try
		{
			return (float)CampaignTime.Now.ToHours;
		}
		catch
		{
			return 0f;
		}
	}

	private static float NowDays()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private static void ResetReplyGenerationAfterLoad(CourierSession session, string reason)
	{
		if (session == null)
		{
			return;
		}
		session.ReplyWaitPopupShown = false;
		if (session.ReplyGenerated || IsTerminalStage(session))
		{
			session.ReplyGenerationStarted = false;
			return;
		}
		CourierStage stage = ParseStage(session.Stage);
		if (!session.ReplyGenerationStarted && stage != CourierStage.GeneratingReply)
		{
			return;
		}
		if (session.ReplyGenerationStarted)
		{
			Log("reply generation restart armed session=" + session.Id + " reason=" + (reason ?? ""));
		}
		session.ReplyGenerationStarted = false;
	}

	private static List<CourierCargoEntry> CloneEntries(List<CourierCargoEntry> entries)
	{
		return (entries ?? new List<CourierCargoEntry>()).Where(x => x != null).Select(x => new CourierCargoEntry
		{
			Kind = x.Kind,
			Id = x.Id,
			Name = x.Name,
			Amount = x.Amount,
			GuidePriceDenars = x.GuidePriceDenars,
			IsHero = x.IsHero,
			Delivered = x.Delivered
		}).ToList();
	}

	private void ResetPendingFlow(string reason)
	{
		Log("reset pending reason=" + reason);
		_pendingFlow = null;
		_letterInputOpen = false;
	}

	private int GetActiveSessionCount()
	{
		lock (_sessionLock)
		{
			return _sessions.Values.Count(x => x != null && !IsTerminalStage(x));
		}
	}

	private static string NewSessionId()
	{
		return CourierPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
	}

	private static string SafeHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string RosterSummary(TroopRoster roster)
	{
		if (roster == null || roster.TotalManCount <= 0)
		{
			return "无";
		}
		List<string> parts = new List<string>();
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			if (item.Character == null || item.Number <= 0)
			{
				continue;
			}
			parts.Add((item.Character.Name?.ToString() ?? item.Character.StringId ?? "未知") + "×" + item.Number);
		}
		return parts.Count == 0 ? "无" : string.Join("，", parts);
	}

	private static string CleanNpcReply(string text)
	{
		string value = (text ?? "").Trim();
		value = Regex.Replace(value, "<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
		value = Regex.Replace(value, "^(NPC|回复|回信)[:：]\\s*", "", RegexOptions.IgnoreCase).Trim();
		if (value == "（没说话）" || value == "无" || value == "无回信")
		{
			return "";
		}
		return value;
	}

	private static string NormalizeInboundLetterText(string text, CourierSession session, Hero sender)
	{
		string value = (text ?? "").Trim();
		value = Regex.Replace(value, "<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
		value = Regex.Replace(value, "^(NPC|来信|信件|外交信)[:：]\\s*", "", RegexOptions.IgnoreCase).Trim();
		value = StripCourierActionTags(value).Trim();
		if (!string.IsNullOrWhiteSpace(value) && value != "（没说话）" && value != "无" && value != "无信件")
		{
			return value;
		}
		string fallback = (session?.LetterText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(fallback))
		{
			return StripCourierActionTags(fallback).Trim();
		}
		string senderName = string.IsNullOrWhiteSpace(session?.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		return senderName + "致" + playerName + "：\n\n我希望以正式信件与你讨论两国外交。若你愿意，请回信说明你的条件。";
	}

	private static bool LooksLikeApiError(string text)
	{
		string value = (text ?? "").Trim();
		return value.StartsWith("（错误", StringComparison.Ordinal) || value.StartsWith("（程序错误", StringComparison.Ordinal) || value.StartsWith("（API请求失败", StringComparison.Ordinal);
	}

	private static bool HasPreprocessRuleHit(List<string> hits, string ruleId)
	{
		string value = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value) || hits == null || hits.Count == 0)
		{
			return false;
		}
		return hits.Any(x => string.Equals((x ?? "").Trim(), value, StringComparison.OrdinalIgnoreCase));
	}

	private static string AppendCourierPlayerRecentActionsIfSelected(string extras, Hero recipient, List<string> selectedRuleHits)
	{
		if (!HasPreprocessRuleHit(selectedRuleHits, "npc_recent_actions"))
		{
			return extras ?? "";
		}
		string playerRecent = PlayerNotorietyBehavior.BuildPlayerRecentRuntimeInstructionForExternal(recipient, courier: true);
		if (string.IsNullOrWhiteSpace(playerRecent))
		{
			return extras ?? "";
		}
		string block = "【附加规则:npc_recent_actions】" + Environment.NewLine + playerRecent.Trim();
		return string.IsNullOrWhiteSpace(extras) ? block : (extras.TrimEnd() + Environment.NewLine + block);
	}

	private static bool ContainsKingdomAnnexActionTag(string text)
	{
		return (text ?? "").IndexOf("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool ContainsVassalageActionTag(string text)
	{
		return (text ?? "").IndexOf("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static List<string> MergeCourierSelectedRuleIds(params IEnumerable<string>[] sources)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (IEnumerable<string> source in sources ?? new IEnumerable<string>[0])
			{
				foreach (string raw in source ?? Enumerable.Empty<string>())
				{
					string value = (raw ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
					{
						result.Add(value);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<string> ExcludeCourierSelectedRuleIds(List<string> source, IEnumerable<string> excludedRuleIds)
	{
		if (source == null || source.Count == 0)
		{
			return new List<string>();
		}
		HashSet<string> excluded = new HashSet<string>((excludedRuleIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
		if (excluded.Count == 0)
		{
			return source;
		}
		return source.Where(x => !string.IsNullOrWhiteSpace(x) && !excluded.Contains(x.Trim())).ToList();
	}

	private static string FilterCourierInjectedRuleBlocks(string text, List<string> allowedRuleIds, IEnumerable<string> excludedRuleIds)
	{
		string value = text ?? "";
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		HashSet<string> allowed = new HashSet<string>((allowedRuleIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
		HashSet<string> excluded = new HashSet<string>((excludedRuleIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
		StringBuilder builder = new StringBuilder();
		bool keepCurrentRuleBlock = true;
		bool keptAnyRuleBlock = false;
		foreach (string rawLine in value.Replace("\r", "").Split('\n'))
		{
			string line = rawLine ?? "";
			string ruleId = TryExtractInjectedRuleBlockId(line);
			if (!string.IsNullOrWhiteSpace(ruleId))
			{
				keepCurrentRuleBlock = allowed.Contains(ruleId) && !excluded.Contains(ruleId);
				if (keepCurrentRuleBlock)
				{
					keptAnyRuleBlock = true;
					builder.AppendLine(line);
				}
				continue;
			}
			if (keepCurrentRuleBlock)
			{
				builder.AppendLine(line);
			}
		}
		string result = builder.ToString().Trim();
		if (!keptAnyRuleBlock)
		{
			result = Regex.Replace(result, "(?m)^【说明】你不必提到附加规则内的内容，除非有人问起。\\s*$", "").Trim();
		}
		return result;
	}

	private static string TryExtractInjectedRuleBlockId(string line)
	{
		string value = (line ?? "").Trim();
		const string prefix = "【附加规则:";
		if (!value.StartsWith(prefix, StringComparison.Ordinal))
		{
			return "";
		}
		int end = value.IndexOf('】', prefix.Length);
		if (end <= prefix.Length)
		{
			return "";
		}
		return value.Substring(prefix.Length, end - prefix.Length).Trim();
	}

	private static string RemoveInjectedRuleBlock(string text, string ruleId)
	{
		string value = text ?? "";
		string id = Regex.Escape((ruleId ?? "").Trim());
		if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		string pattern = "(?ms)^【附加规则:" + id + "】.*?(?=^【附加规则:|\\z)";
		return Regex.Replace(value, pattern, "").Trim();
	}

	private static string StripCourierActionTags(string text)
	{
		string value = text ?? "";
		value = Regex.Replace(value, "\\[ACTION:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[AD;[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ADP[:;][^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ATT[:;][^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ATP[:;][^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:H_J_P_P\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[(?:FOL|STP|END)\\]", "", RegexOptions.IgnoreCase);
		return value.Trim();
	}

	private static string BuildCourierStatusSnapshot(CourierSession session, MobileParty courier, Hero recipient, string phase)
	{
		try
		{
			return "phase=" + (phase ?? "") +
				" session=" + (session?.Id ?? "") +
				" stage=" + (session?.Stage ?? "") +
				" deliveryApplied=" + (session?.DeliveryApplied == true) +
				" replyStarted=" + (session?.ReplyGenerationStarted == true) +
				" replyGenerated=" + (session?.ReplyGenerated == true) +
				" waitReason=" + (session?.RecipientWaitReason ?? "") +
				" safeSettlement=" + (session?.SafeSettlementId ?? "") +
				" lastRoute=" + (session?.LastRouteKey ?? "") +
				" progressRoute=" + (session?.LastProgressRouteKey ?? "") +
				" stuckCount=" + (session?.NavalStuckRefreshCount ?? 0) +
				" tempShipCreated=" + (session?.TemporaryShipCreated == true) +
				" tempShipHull=" + (session?.TemporaryShipHullId ?? "") +
				" " + DescribeHero(recipient) +
				" courier=" + DescribeMobileParty(courier);
		}
		catch (Exception ex)
		{
			return "phase=" + (phase ?? "") + " session=" + (session?.Id ?? "") + " snapshot_error=" + ex.Message;
		}
	}

	private static string DescribeRecipientTarget(Hero recipient, MobileParty targetParty, Settlement targetSettlement)
	{
		return "source=" + GetRecipientTargetSource(recipient, targetParty, targetSettlement) +
			" " + DescribeHero(recipient) +
			" targetParty=" + DescribeMobileParty(targetParty) +
			" targetSettlement=" + DescribeSettlement(targetSettlement);
	}

	private static string GetRecipientTargetSource(Hero recipient, MobileParty targetParty, Settlement targetSettlement)
	{
		try
		{
			if (recipient == null)
			{
				return "none";
			}
			if (targetParty != null && recipient.PartyBelongedTo == targetParty)
			{
				return "party_belonged_to";
			}
			PartyBase prisonerParty = recipient.PartyBelongedToAsPrisoner;
			if (prisonerParty != null)
			{
				if (targetParty != null && prisonerParty.MobileParty == targetParty)
				{
					return "prisoner_mobile_party";
				}
				if (targetSettlement != null && prisonerParty.Settlement == targetSettlement)
				{
					return "prisoner_settlement";
				}
			}
			if (targetSettlement != null && recipient.CurrentSettlement == targetSettlement)
			{
				return "current_settlement";
			}
			if (targetSettlement != null && recipient.StayingInSettlement == targetSettlement)
			{
				return "staying_settlement";
			}
		}
		catch
		{
		}
		return "resolved_fallback";
	}

	private static string DescribeRoutePlan(CourierRoutePlan plan)
	{
		if (plan == null)
		{
			return "plan=null";
		}
		return "planNav=" + plan.NavigationType +
			" requiresNaval=" + plan.RequiresNaval +
			" usePort=" + plan.UsePort +
			" reason=" + (plan.Reason ?? "");
	}

	private static string DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return "recipient=null";
		}
		try
		{
			return "recipient=" + SafeHeroId(hero) +
				" name=" + SafeLogText(hero.Name?.ToString()) +
				" dead=" + hero.IsDead +
				" fugitive=" + hero.IsFugitive +
				" prisoner=" + hero.IsPrisoner +
				" party=" + PartyIdOnly(hero.PartyBelongedTo) +
				" prisonerParty=" + DescribePartyBase(hero.PartyBelongedToAsPrisoner) +
				" currentSettlement=" + SettlementIdOnly(hero.CurrentSettlement) +
				" stayingSettlement=" + SettlementIdOnly(hero.StayingInSettlement);
		}
		catch (Exception ex)
		{
			return "recipient=" + SafeHeroId(hero) + " describe_error=" + ex.Message;
		}
	}

	private static string DescribeMobileParty(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return (party.StringId ?? "") +
				"(name=" + SafeLogText(party.Name?.ToString()) +
				",active=" + party.IsActive +
				",pos=" + FormatCampaignVec2(party.Position) +
				",posLand=" + SafeIsOnLand(party.Position) +
				",sea=" + party.IsCurrentlyAtSea +
				",current=" + SettlementIdOnly(party.CurrentSettlement) +
				",default=" + party.DefaultBehavior +
				",short=" + party.ShortTermBehavior +
				",nav=" + party.NavigationCapability +
				",desiredNav=" + party.DesiredAiNavigationType +
				",isTargetingPort=" + party.IsTargetingPort +
				",targetSettlement=" + SettlementIdOnly(party.TargetSettlement) +
				",targetParty=" + PartyIdOnly(party.TargetParty) +
				",shortTargetSettlement=" + SettlementIdOnly(party.ShortTermTargetSettlement) +
				",shortTargetParty=" + PartyIdOnly(party.ShortTermTargetParty) +
				",targetPos=" + FormatCampaignVec2(party.TargetPosition) +
				",moveTarget=" + FormatCampaignVec2(party.MoveTargetPoint) +
				",ships=" + SafeShipCount(party) +
				",landCap=" + SafeHasLandNavigation(party) +
				",navalCap=" + SafeHasNavalNavigation(party) +
				",transition=" + party.IsTransitionInProgress +
				",quest=" + party.IsCurrentlyUsedByAQuest +
				")";
		}
		catch (Exception ex)
		{
			return (party.StringId ?? "") + "(describe_error=" + ex.Message + ")";
		}
	}

	private static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		try
		{
			return (settlement.StringId ?? "") +
				"(name=" + SafeLogText(settlement.Name?.ToString()) +
				",town=" + settlement.IsTown +
				",castle=" + settlement.IsCastle +
				",village=" + settlement.IsVillage +
				",fort=" + settlement.IsFortification +
				",hasPort=" + settlement.HasPort +
				",underSiege=" + settlement.IsUnderSiege +
				",gate=" + FormatCampaignVec2(settlement.GatePosition) +
				",port=" + (settlement.HasPort ? FormatCampaignVec2(settlement.PortPosition) : "none") +
				")";
		}
		catch (Exception ex)
		{
			return (settlement.StringId ?? "") + "(describe_error=" + ex.Message + ")";
		}
	}

	private static string DescribePartyBase(PartyBase party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return "(name=" + SafeLogText(party.Name?.ToString()) +
				",mobile=" + PartyIdOnly(party.MobileParty) +
				",settlement=" + SettlementIdOnly(party.Settlement) +
				",isMobile=" + party.IsMobile +
				",isSettlement=" + party.IsSettlement +
				")";
		}
		catch (Exception ex)
		{
			return "(describe_error=" + ex.Message + ")";
		}
	}

	private static string PartyIdOnly(MobileParty party)
	{
		return party == null ? "null" : (party.StringId ?? "");
	}

	private static string SettlementIdOnly(Settlement settlement)
	{
		return settlement == null ? "null" : (settlement.StringId ?? "");
	}

	private static string FormatCampaignVec2(CampaignVec2 position)
	{
		try
		{
			if (!position.IsValid())
			{
				return "invalid";
			}
		}
		catch
		{
			return "invalid";
		}
		return position.X.ToString("0.##") + "," + position.Y.ToString("0.##");
	}

	private static string SafeIsOnLand(CampaignVec2 position)
	{
		try
		{
			return position.IsOnLand.ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static int SafeShipCount(MobileParty party)
	{
		try
		{
			return party?.Ships?.Count ?? 0;
		}
		catch
		{
			return -1;
		}
	}

	private static string SafeHasLandNavigation(MobileParty party)
	{
		try
		{
			return (party?.HasLandNavigationCapability == true).ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeHasNavalNavigation(MobileParty party)
	{
		try
		{
			return (party?.HasNavalNavigationCapability == true).ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeLogText(string text)
	{
		string value = (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
		if (value.Length > 80)
		{
			value = value.Substring(0, 80);
		}
		return value;
	}

	private static void LogCourierStatus(string message)
	{
		try
		{
			Logger.Log("Logic", "[CourierStatus] " + (message ?? ""));
		}
		catch
		{
		}
	}

	private static void LogCourierStatusVerbose(string key, string message, double minIntervalSeconds = 0.0)
	{
		try
		{
			if (!Logger.IsModLogicEnabled)
			{
				return;
			}
			long now = DateTime.UtcNow.Ticks;
			string throttleKey = (key ?? "").Trim();
			long minTicks = TimeSpan.FromSeconds(Math.Max(0.0, minIntervalSeconds)).Ticks;
			lock (LastCourierLogicPulseTicks)
			{
				if (!string.IsNullOrWhiteSpace(throttleKey) && minTicks > 0L && LastCourierLogicPulseTicks.TryGetValue(throttleKey, out long last) && now - last < minTicks)
				{
					return;
				}
				if (LastCourierLogicPulseTicks.Count > 512)
				{
					LastCourierLogicPulseTicks.Clear();
				}
				if (!string.IsNullOrWhiteSpace(throttleKey))
				{
					LastCourierLogicPulseTicks[throttleKey] = now;
				}
			}
			LogCourierStatus(message);
		}
		catch
		{
		}
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log(LogSource, message ?? "");
		}
		catch
		{
		}
	}

	private static void LogVerbose(string key, string message, double minIntervalSeconds = 0.0)
	{
		try
		{
			Logger.LogVerbose(LogSource, key, () => message ?? "", minIntervalSeconds);
		}
		catch
		{
		}
	}

	private static class MethodInfoAccess
	{
		public static void PatchDefaultEncounterModel(Harmony harmony)
		{
			var method = AccessTools.Method(typeof(DefaultEncounterModel), nameof(DefaultEncounterModel.IsEncounterExemptFromHostileActions));
			if (method != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(DefaultEncounterModelIsEncounterExemptPrefix)));
			}
		}

		public static void PatchCustomPartyComponentBanner(Harmony harmony)
		{
			var method = AccessTools.Method(typeof(CustomPartyComponent), nameof(CustomPartyComponent.GetDefaultComponentBanner));
			if (method != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(CustomPartyComponentGetDefaultComponentBannerPrefix)));
			}
		}
	}

	public static bool CustomPartyComponentGetDefaultComponentBannerPrefix(CustomPartyComponent __instance, ref Banner __result)
	{
		try
		{
			MobileParty party = __instance?.MobileParty;
			if (!IsCourierParty(party))
			{
				return true;
			}
			__result = Clan.PlayerClan?.Banner ?? party?.ActualClan?.Banner ?? Hero.MainHero?.Clan?.Banner;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool DefaultEncounterModelIsEncounterExemptPrefix(PartyBase side1, PartyBase side2, ref bool __result)
	{
		try
		{
			MobileParty courier = side1?.MobileParty != null && IsCourierParty(side1.MobileParty) ? side1.MobileParty : (side2?.MobileParty != null && IsCourierParty(side2.MobileParty) ? side2.MobileParty : null);
			if (courier == null)
			{
				return true;
			}
			PartyBase other = courier == side1?.MobileParty ? side2 : side1;
			IFaction otherFaction = other?.MapFaction;
			bool otherIsBandit = IsBanditOrOutlawParty(other?.MobileParty) || otherFaction?.IsBanditFaction == true;
			if (otherIsBandit)
			{
				__result = false;
				return false;
			}
			__result = true;
			return false;
		}
		catch
		{
			return true;
		}
	}
}
