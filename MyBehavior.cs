using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class MyBehavior : CampaignBehaviorBase
{
	private enum ChatMode
	{
		Normal,
		Give,
		Show
	}

	private class PendingTradeContext
	{
		public bool IsGive;
	}

	private class PendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;
	}

	private class HeroShownRecord
	{
		public int ShownGold;

		public Dictionary<string, int> ShownItems = new Dictionary<string, int>();
	}

	private class TradeResourceOption
	{
		public bool IsGold;

		public string ItemId;

		public string Name;

		public int AvailableAmount;

		public ItemObject Item;
	}

	private class DialogueDay
	{
		public int GameDayIndex;

		public string GameDate;

		public List<string> Lines = new List<string>();
	}

	private class NpcPersonaProfile
	{
		public string Personality;

		public string Background;

		public string VoiceId;
	}

	public class ShoutPromptContext
	{
		public string Extras;

		public bool UseDuelContext;

		public bool UseRewardContext;

		public bool IsLoanContext;

		public bool IsQualified;
	}

	private sealed class HistoryLineEntry
	{
		public int Day;

		public string Date;

		public string Line;

		public int Index;
	}

	private sealed class ArchiveHit
	{
		public HistoryLineEntry Entry;

		public double Score;
	}

	private class PatienceState
	{
		public float Value;

		public float LastDay;

		public int NoInterestRounds;

		public int ExhaustedRefusalCount;
	}

	private class PatienceStateSaveModel
	{
		public float Value { get; set; }

		public float LastDay { get; set; }

		public int NoInterestRounds { get; set; }

		public int ExhaustedRefusalCount { get; set; }
	}

	private struct PatienceSnapshot
	{
		public string Key;

		public string DisplayName;

		public int Relation;

		public int Trust;

		public int PublicTrust;

		public int PrivateLove;

		public int Max;

		public float Current;

		public string PatienceLevel;

		public string RelationLevel;

		public string TrustLevel;

		public string PublicTrustLevel;

		public string PrivateLoveLevel;
	}

	private enum PatienceMood
	{
		Neutral,
		Delighted,
		Joy,
		Annoyed,
		Bored
	}

	private enum ExportImportScope
	{
		All,
		HeroNpcAll,
		PersonalityBackground,
		UnnamedPersona,
		DialogueHistory,
		Debt,
		Knowledge,
		VoiceMapping
	}

	private class UnnamedPersonaSingleJson
	{
		public string Key;

		public string Personality;

		public string Background;
	}

	private const int MOUSEEVENTF_LEFTDOWN = 2;

	private const int MOUSEEVENTF_LEFTUP = 4;

	private string _aiResponseText = "";

	private PendingTradeContext _pendingTrade;

	private List<PendingTradeItem> _pendingTradeItems = new List<PendingTradeItem>();

	private int _pendingTradeItemIndex;

	private Dictionary<string, HeroShownRecord> _shownRecords = new Dictionary<string, HeroShownRecord>();

	private Dictionary<string, string> _shownRecordStorage = new Dictionary<string, string>();

	private List<TradeResourceOption> _currentTradeOptions = new List<TradeResourceOption>();

	private bool _isRewardSessionActive;

	private string _rewardSessionHeroId;

	private int _rewardSessionNoKeywordRounds;

	private bool _rewardSessionFromDuel;

	private bool _rewardSessionPlayerWonLastDuel;

	private bool _isKingdomServiceRuleSessionActive;

	private string _kingdomServiceRuleHeroId;

	private int _kingdomServiceRuleNoHitRounds;

	private string _kingdomServiceRuleCachedBlock;

	private bool _isMarriageRuleSessionActive;

	private string _marriageRuleHeroId;

	private int _marriageRuleNoHitRounds;

	private string _marriageRuleCachedBlock;

	private bool _isDuelSessionActive;

	private string _duelSessionHeroId;

	private int _duelSessionNoKeywordRounds;

	private bool _nextDuelRiskWarningByLiteral = true;

	private bool _isLoanSessionActive;

	private string _loanSessionHeroId;

	private int _loanSessionNoKeywordRounds;

	private string _confirmCarryHeroId;

	private int _confirmCarryRoundsLeft;

	private bool _confirmCarryDuel;

	private bool _confirmCarryReward;

	private bool _confirmCarryLoan;

	private bool _confirmCarrySurroundings;

	private bool _confirmCarryKingdomService;

	private long _suppressAutoClickUntilUtcTicks;

	private bool _overlayQuickTalkDisableHooked;

	private const int KingdomServiceRuleStickyRounds = 2;

	private const int MaxDialogueHistoryLines = 260;

	private const int HistoryRecentTurnsDefault = 20;

	private const int HistoryRecentTurnsMin = 1;

	private const int HistoryRecentTurnsMax = 80;

	private const int HistoryArchiveSectionMaxChars = 900;

	private const int HistoryArchiveTopK = 10;

	private const int HistoryArchiveCandidateLimit = 260;

	private const int HistoryOnnxRerankLimit = 120;

	private const int HistoryArchiveRecallMaxItems = 12;

	private Dictionary<string, List<DialogueDay>> _dialogueHistory = new Dictionary<string, List<DialogueDay>>();

	private Dictionary<string, string> _dialogueHistoryStorage = new Dictionary<string, string>();

	private Dictionary<string, NpcPersonaProfile> _npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();

	private Dictionary<string, string> _npcPersonaProfileStorage = new Dictionary<string, string>();

	private readonly object _npcPersonaAutoGenLock = new object();

	private HashSet<string> _npcPersonaAutoGenInFlight = new HashSet<string>();

	private HashSet<string> _recentlyDefeatedByPlayer = new HashSet<string>();

	private HashSet<string> _recentlyReleasedPrisoners = new HashSet<string>();

	private static int _cachedPlayerClanTier;

	private static long _cachedPlayerClanTierUtcTicks;

	private List<Hero> _devEditableHeroes = new List<Hero>();

	private Hero _devEditingHero;

	private const int PatienceMaxCap = 80;

	private const int PatienceMinCap = 10;

	private const int PatienceDefaultMaxForUnnamed = 30;

	private const float PatienceRecoveryPerDay = 4f;

	private const int PatienceNoInterestPenaltyThreshold = 3;

	private const int RelationGainOnJoy = 1;

	private const int RelationPenaltyOnBored = -1;

	private const int RelationPenaltyOnAnnoyed = -2;

	private static readonly string[] PatienceLevelTexts = new string[10] { "枯竭", "烦躁", "不耐", "冷淡", "一般", "尚可", "愿听", "投入", "热络", "兴致高" };

	private static readonly string[] RelationLevelTexts = new string[10] { "死敌", "敌对", "厌恶", "疏离", "冷漠", "中立", "熟络", "友好", "亲近", "至交" };

	private static readonly string[] RelationAiBehaviorTexts = new string[10] { "把玩家视为重大威胁，语气强硬且排斥，不愿合作。", "明显敌意与戒备，倾向拒绝请求，回应尖锐。", "主观反感较强，容易挑刺，合作意愿很低。", "保持距离与怀疑，只做最基本交流。", "态度偏冷，交流克制，基本不主动示好。", "无明显好恶，按利益与场面决定态度。", "对玩家有一定熟悉感，语气较缓，可有限合作。", "整体友好，愿意倾听并给出建设性回应。", "明显信任玩家，交流积极，合作意愿高。", "高度信任与支持，语气亲近，优先站在玩家一边。" };

	private static readonly Regex MoodTagRegex = new Regex("\\[ACTION:MOOD:([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Dictionary<string, PatienceState> _patienceStates = new Dictionary<string, PatienceState>();

	private Dictionary<string, string> _patienceStorage = new Dictionary<string, string>();

	private readonly object _patienceLock = new object();

	private string _devOpsHeroId;

	private string _devOpsHeroName;

	private bool _devPendingKnowledgeSingleImportPicked;

	private string _devPendingKnowledgeSingleImportFolderName;

	private bool _devPendingUnnamedSingleImportPicked;

	private string _devPendingUnnamedSingleImportFolderName;

	private string _devHistorySearchQuery = string.Empty;

	[DllImport("user32.dll")]
	private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

	private static int ClampRecentDialogueTurns(int turns)
	{
		if (turns < 1)
		{
			return 1;
		}
		if (turns > 80)
		{
			return 80;
		}
		return turns;
	}

	private static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
	}

	private static int GetRecentDialogueTurnsFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return ClampRecentDialogueTurns(settings.RecentDialogueTurns);
			}
		}
		catch
		{
		}
		return 20;
	}

	private static int GetHistoryArchiveTopNFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				int num = settings.HistoryRecallTopN;
				if (num < 1)
				{
					num = 1;
				}
				if (num > 20)
				{
					num = 20;
				}
				return num;
			}
		}
		catch
		{
		}
		return 10;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				return;
			}
			MapEventSide mapEventSide = ((mapEvent.PlayerSide == BattleSideEnum.Attacker) ? mapEvent.DefenderSide : mapEvent.AttackerSide);
			foreach (MapEventParty party in mapEventSide.Parties)
			{
				Hero hero = party.Party?.LeaderHero;
				if (hero != null && hero != Hero.MainHero && hero.IsLord)
				{
					_recentlyDefeatedByPlayer.Add(hero.StringId);
					Logger.Log("BattleStatus", $"原版战斗结束：玩家击败了 {hero.Name}");
					AppendExternalDialogueHistory(hero, null, null, $"你在一场战斗中被 {Hero.MainHero.Name} 击败了。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnMapEventEnded: " + ex.Message);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		try
		{
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturer?.LeaderHero == Hero.MainHero || capturer?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} 被玩家俘虏");
				AppendExternalDialogueHistory(prisoner, null, null, $"你被 {Hero.MainHero.Name} 俘虏了，你现在是他的囚犯。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerTaken: " + ex.Message);
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		try
		{
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturerFaction == Hero.MainHero?.MapFaction || party?.LeaderHero == Hero.MainHero || party?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				_recentlyReleasedPrisoners.Add(prisoner.StringId);
				string text = detail switch
				{
					EndCaptivityDetail.Ransom => "通过支付赎金", 
					EndCaptivityDetail.ReleasedByChoice => "被主动释放", 
					EndCaptivityDetail.ReleasedAfterPeace => "因和平协议", 
					EndCaptivityDetail.ReleasedAfterEscape => "成功逃脱", 
					_ => "", 
				};
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} {text}获得自由 (detail={detail})");
				AppendExternalDialogueHistory(prisoner, null, null, $"你{text}从 {Hero.MainHero.Name} 的囚禁中获得了自由。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerReleased: " + ex.Message);
		}
	}

	private void OnCampaignTick(float dt)
	{
		try
		{
			int num = 0;
			try
			{
				num = Clan.PlayerClan?.Tier ?? 0;
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					num = (Hero.MainHero?.Clan?.Tier).GetValueOrDefault();
				}
				catch
				{
				}
			}
			_cachedPlayerClanTier = num;
			_cachedPlayerClanTierUtcTicks = DateTime.UtcNow.Ticks;
		}
		catch
		{
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_shownRecords == null)
		{
			_shownRecords = new Dictionary<string, HeroShownRecord>();
		}
		if (_shownRecordStorage == null)
		{
			_shownRecordStorage = new Dictionary<string, string>();
		}
		if (_dialogueHistory == null)
		{
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
		}
		if (_dialogueHistoryStorage == null)
		{
			_dialogueHistoryStorage = new Dictionary<string, string>();
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		if (_npcPersonaProfileStorage == null)
		{
			_npcPersonaProfileStorage = new Dictionary<string, string>();
		}
		try
		{
			if (dataStore != null && dataStore.IsSaving)
			{
				_shownRecordStorage.Clear();
				foreach (KeyValuePair<string, HeroShownRecord> shownRecord in _shownRecords)
				{
					if (string.IsNullOrWhiteSpace(shownRecord.Key) || shownRecord.Value == null)
					{
						continue;
					}
					bool flag = Math.Max(0, shownRecord.Value.ShownGold) > 0;
					if (!flag && shownRecord.Value.ShownItems != null)
					{
						foreach (KeyValuePair<string, int> shownItem in shownRecord.Value.ShownItems)
						{
							if (!string.IsNullOrWhiteSpace(shownItem.Key) && shownItem.Value > 0)
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						continue;
					}
					try
					{
						_shownRecordStorage[shownRecord.Key] = JsonConvert.SerializeObject(shownRecord.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("TradeShown", "[ERROR] Serialize shown record for " + shownRecord.Key + ": " + ex.Message);
					}
				}
				dataStore.SyncData("_shownRecords_v1", ref _shownRecordStorage);
				_dialogueHistoryStorage.Clear();
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						try
						{
							string value = JsonConvert.SerializeObject(item.Value);
							_dialogueHistoryStorage[item.Key] = value;
						}
						catch (Exception ex)
						{
							Logger.Log("DialogueHistory", "[ERROR] Serialize history for " + item.Key + ": " + ex.Message);
						}
					}
				}
				dataStore.SyncData("_dialogueHistory_v2", ref _dialogueHistoryStorage);
				_npcPersonaProfileStorage.Clear();
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile2 in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile2.Key) && npcPersonaProfile2.Value != null)
					{
						try
						{
							string value2 = JsonConvert.SerializeObject(npcPersonaProfile2.Value);
							_npcPersonaProfileStorage[npcPersonaProfile2.Key] = value2;
						}
						catch (Exception ex2)
						{
							Logger.Log("NpcPersona", "[ERROR] Serialize profile for " + npcPersonaProfile2.Key + ": " + ex2.Message);
						}
					}
				}
				dataStore.SyncData("_npcPersonaProfiles_v1", ref _npcPersonaProfileStorage);
				SyncPatienceData(dataStore);
				return;
			}
			_shownRecords.Clear();
			_shownRecordStorage.Clear();
			dataStore.SyncData("_shownRecords_v1", ref _shownRecordStorage);
			if (_shownRecordStorage != null)
			{
				foreach (KeyValuePair<string, string> shownRecord2 in _shownRecordStorage)
				{
					if (string.IsNullOrWhiteSpace(shownRecord2.Key) || string.IsNullOrWhiteSpace(shownRecord2.Value))
					{
						continue;
					}
					try
					{
						HeroShownRecord heroShownRecord = JsonConvert.DeserializeObject<HeroShownRecord>(shownRecord2.Value);
						if (heroShownRecord != null)
						{
							if (heroShownRecord.ShownGold < 0)
							{
								heroShownRecord.ShownGold = 0;
							}
							if (heroShownRecord.ShownItems == null)
							{
								heroShownRecord.ShownItems = new Dictionary<string, int>();
							}
							else
							{
								heroShownRecord.ShownItems = heroShownRecord.ShownItems.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).ToDictionary((KeyValuePair<string, int> x) => x.Key, (KeyValuePair<string, int> x) => x.Value);
							}
							_shownRecords[NormalizeShownRecordKey(shownRecord2.Key)] = heroShownRecord;
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("TradeShown", "[ERROR] Deserialize shown record for " + shownRecord2.Key + ": " + ex2.Message);
					}
				}
			}
			_dialogueHistory.Clear();
			_dialogueHistoryStorage.Clear();
			dataStore.SyncData("_dialogueHistory_v2", ref _dialogueHistoryStorage);
			if (_dialogueHistoryStorage != null)
			{
				foreach (KeyValuePair<string, string> item2 in _dialogueHistoryStorage)
				{
					if (string.IsNullOrEmpty(item2.Key) || string.IsNullOrEmpty(item2.Value))
					{
						continue;
					}
					try
					{
						List<DialogueDay> list = JsonConvert.DeserializeObject<List<DialogueDay>>(item2.Value);
						if (list != null)
						{
							_dialogueHistory[item2.Key] = list;
						}
					}
					catch (Exception ex3)
					{
						Logger.Log("DialogueHistory", "[ERROR] Deserialize history for " + item2.Key + ": " + ex3.Message);
					}
				}
			}
			_npcPersonaProfiles.Clear();
			_npcPersonaProfileStorage.Clear();
			dataStore.SyncData("_npcPersonaProfiles_v1", ref _npcPersonaProfileStorage);
			if (_npcPersonaProfileStorage != null)
			{
				foreach (KeyValuePair<string, string> item3 in _npcPersonaProfileStorage)
				{
					if (string.IsNullOrEmpty(item3.Key) || string.IsNullOrEmpty(item3.Value))
					{
						continue;
					}
					try
					{
						NpcPersonaProfile npcPersonaProfile = JsonConvert.DeserializeObject<NpcPersonaProfile>(item3.Value);
						if (npcPersonaProfile != null)
						{
							_npcPersonaProfiles[item3.Key] = npcPersonaProfile;
						}
					}
					catch (Exception ex4)
					{
						Logger.Log("NpcPersona", "[ERROR] Deserialize profile for " + item3.Key + ": " + ex4.Message);
					}
				}
			}
			SyncPatienceData(dataStore);
		}
		catch (Exception ex5)
		{
			Logger.Log("DialogueHistory", "[ERROR] SyncData v2 failed: " + ex5.ToString());
			_shownRecords = new Dictionary<string, HeroShownRecord>();
			_shownRecordStorage = new Dictionary<string, string>();
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			_dialogueHistoryStorage = new Dictionary<string, string>();
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
			_npcPersonaProfileStorage = new Dictionary<string, string>();
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AIConfigHandler.ReloadConfig();
		TryHookOverlayQuickTalkDisable();
		starter.AddGameMenu("AnimusForge_dev_root", "{=!}开发者工具", DevRootMenuInit, GameMenu.MenuOverlayType.SettlementWithBoth);
		starter.AddGameMenuOption("town", "AnimusForge_dev_root_entry", "【开发】数据管理", DevRootEntryCondition, DevRootEntryConsequence, isLeave: false, 99);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_hero", "HeroNPC编辑（领主/流浪者/同伴）", DevRootSubOptionCondition, DevRootHeroOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_nonhero", "非heroNPC编辑（士兵/平民/无名/无姓NPC）", DevRootSubOptionCondition, DevRootNonHeroOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_knowledge", "知识编辑", DevRootSubOptionCondition, DevRootKnowledgeOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_all", "全部导出/导入", DevRootSubOptionCondition, DevRootAllOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_voice", "声音映射管理（VoiceMapping）", DevRootSubOptionCondition, DevRootVoiceMappingOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_back", "返回", DevRootBackCondition, DevRootBackConsequence, isLeave: true);
	}

	private void TryHookOverlayQuickTalkDisable()
	{
		if (_overlayQuickTalkDisableHooked)
		{
			return;
		}
		try
		{
			EventManager eventManager = Game.Current?.EventManager;
			if (eventManager != null)
			{
				eventManager.RegisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
				_overlayQuickTalkDisableHooked = true;
			}
		}
		catch
		{
		}
	}

	private void OnSettlementOverlayQuickTalkPermission(SettlementOverylayQuickTalkPermissionEvent e)
	{
		try
		{
			if (e != null && e.IsTalkAvailable != null)
			{
				Hero heroToTalkTo = e.HeroToTalkTo;
				if (heroToTalkTo != null && !heroToTalkTo.IsPlayerCompanion && (heroToTalkTo.IsLord || heroToTalkTo.IsNotable))
				{
					e.IsTalkAvailable(arg1: false, new TextObject("该交谈选项已被模组禁用，请使用造访进入场景后再互动。"));
				}
			}
		}
		catch
		{
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		if (mission != null && Mission.Current != null)
		{
			try
			{
				string arg = Mission.Current.SceneName ?? "Unknown";
				string arg2 = MobileParty.MainParty?.CurrentSettlement?.Name?.ToString() ?? "";
				MissionMode mode = Mission.Current.Mode;
				Logger.Log("SceneInfo", $"[MyBehavior.OnMissionStarted] SceneName={arg}, Mode={mode}, Settlement={arg2}");
			}
			catch
			{
			}
		}
	}

	private NpcPersonaProfile GetNpcPersonaProfile(Hero npc, bool createIfMissing)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		if (_npcPersonaProfiles.TryGetValue(stringId, out var value))
		{
			return value;
		}
		if (!createIfMissing)
		{
			return null;
		}
		value = new NpcPersonaProfile();
		_npcPersonaProfiles[stringId] = value;
		return value;
	}

	private void SaveNpcPersonaProfile(Hero npc, NpcPersonaProfile profile)
	{
		if (npc == null)
		{
			return;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		string text = (profile?.Personality ?? "").Trim();
		string text2 = (profile?.Background ?? "").Trim();
		string text3 = (profile?.VoiceId ?? "").Trim();
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3))
		{
			_npcPersonaProfiles.Remove(stringId);
			return;
		}
		if (profile == null)
		{
			profile = new NpcPersonaProfile();
		}
		profile.Personality = text;
		profile.Background = text2;
		profile.VoiceId = text3;
		_npcPersonaProfiles[stringId] = profile;
	}

	private void GetNpcPersonaStrings(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (hero != null)
		{
			string stringId = hero.StringId;
			if (!string.IsNullOrEmpty(stringId) && _npcPersonaProfiles != null && _npcPersonaProfiles.TryGetValue(stringId, out var value) && value != null)
			{
				personality = value.Personality ?? "";
				background = value.Background ?? "";
			}
		}
	}

	private string GetNpcVoiceId(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return "";
		}
		if (_npcPersonaProfiles == null)
		{
			return "";
		}
		if (_npcPersonaProfiles.TryGetValue(stringId, out var value) && value != null)
		{
			return (value.VoiceId ?? "").Trim();
		}
		return "";
	}

	public static string GetNpcVoiceIdForExternal(Hero hero)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				return "";
			}
			return myBehavior.GetNpcVoiceId(hero);
		}
		catch
		{
			return "";
		}
	}

	private string BuildNpcPersonaContext(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		string text = (personality ?? "").Trim();
		string text2 = (background ?? "").Trim();
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(text))
		{
			stringBuilder.AppendLine("【角色个性】" + text);
		}
		if (!string.IsNullOrEmpty(text2))
		{
			stringBuilder.AppendLine("【角色背景】" + text2);
		}
		stringBuilder.AppendLine("【约束】你必须在言行与态度上保持与上述个性、背景一致，不要编造与其冲突的经历。");
		return stringBuilder.ToString();
	}

	private static string TrimToMaxChars(string s, int maxChars)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return "";
		}
		s = s.Trim();
		if (s.Length <= maxChars)
		{
			return s;
		}
		return s.Substring(0, maxChars).Trim();
	}

	private string BuildHeroFactsForPersonaGeneration(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"姓名: {hero.Name}");
		stringBuilder.AppendLine($"年龄: {hero.Age:F0}");
		try
		{
			stringBuilder.AppendLine("性别: " + (hero.IsFemale ? "女" : "男"));
		}
		catch
		{
		}
		try
		{
			if (hero.Culture != null)
			{
				stringBuilder.AppendLine("文化: " + hero.Culture.StringId);
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				stringBuilder.AppendLine($"家族: {hero.Clan.Name} (StringId={hero.Clan.StringId})");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null && hero.Clan.Kingdom != null)
			{
				stringBuilder.AppendLine($"势力: {hero.Clan.Kingdom.Name} (StringId={hero.Clan.Kingdom.StringId})");
			}
		}
		catch
		{
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null && kingdom.Leader != null)
			{
				stringBuilder.AppendLine($"势力领袖: {kingdom.Leader.Name} (HeroId={kingdom.Leader.StringId})");
				if (hero.IsFactionLeader)
				{
					stringBuilder.AppendLine("效忠: 你本人即该势力领袖");
				}
				else
				{
					stringBuilder.AppendLine($"效忠: {kingdom.Leader.Name}");
				}
			}
		}
		catch
		{
		}
		string text = "英雄";
		try
		{
			if (hero.IsFactionLeader)
			{
				text = "统治者/派系领袖";
			}
			else if (hero.IsLord)
			{
				text = "领主";
			}
			else if (hero.IsWanderer)
			{
				text = "流浪者";
			}
			else if (hero.IsNotable)
			{
				text = "要人";
			}
		}
		catch
		{
		}
		stringBuilder.AppendLine("身份: " + text);
		try
		{
			stringBuilder.AppendLine($"特质: Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
		}
		catch
		{
		}
		try
		{
			var list = (from x in (from sk in Skills.All
					select new
					{
						Skill = sk,
						Value = hero.GetSkillValue(sk)
					} into x
					orderby x.Value descending, x.Skill.StringId
					select x).Take(8)
				where x.Value > 0
				select x).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("技能(最高8项): " + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
			}
		}
		catch
		{
		}
		try
		{
			List<string> list2 = new List<string>();
			if (hero.Father != null)
			{
				list2.Add("父亲:" + hero.Father.Name);
			}
			if (hero.Mother != null)
			{
				list2.Add("母亲:" + hero.Mother.Name);
			}
			if (hero.Spouse != null)
			{
				list2.Add("配偶:" + hero.Spouse.Name);
			}
			try
			{
				List<string> list3 = (from c in hero.Children?.Take(6)
					select c?.Name?.ToString() into n
					where !string.IsNullOrWhiteSpace(n)
					select n).ToList();
				if (list3 != null && list3.Count > 0)
				{
					list2.Add("子女:" + string.Join("、", list3));
				}
			}
			catch
			{
			}
			if (list2.Count > 0)
			{
				stringBuilder.AppendLine("家族成员: " + string.Join(" | ", list2));
			}
		}
		catch
		{
		}
		return stringBuilder.ToString().Trim();
	}

	private static string GetClanTierReputationLabel(int tier)
	{
		int num = Math.Max(0, tier);
		if (num <= 0)
		{
			return "身份低微";
		}
		return num switch
		{
			1 => "小有名气", 
			2 => "崭露新贵", 
			3 => "声名清贵", 
			4 => "门第高华", 
			5 => "威权显赫", 
			_ => "贵不可言", 
		};
	}

	private static string BuildAgeBracketLabel(float age)
	{
		if (age >= 60f)
		{
			return "老年";
		}
		if (age >= 46f)
		{
			return "中年";
		}
		if (age >= 30f)
		{
			return "壮年";
		}
		if (age >= 18f)
		{
			return "青年";
		}
		if (age > 0f)
		{
			return "少年";
		}
		return "未知";
	}

	private static string BuildDialogueFormatGuardrailPrompt()
	{
		return "【常驻发言格式】你的可见回复必须使用“台词（动作）”结构：只保留一段台词，并在末尾仅使用一组全角括号（）描述动作；不要输出额外旁白、心理活动、评注或第二段叙述。若需输出 ACTION 标签，只能追加在整段文本末尾，且不得写入括号内。";
	}

	private static bool ContainsIgnoreCase(string text, string token)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
		{
			return false;
		}
		return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsShortAckForRuleFollowup(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.Length > 20)
		{
			return false;
		}
		string[] array = new string[17]
		{
			"好", "好的", "行", "可以", "同意", "确认", "就这样", "继续", "嗯", "是",
			"对", "我选雇佣兵", "雇佣兵", "我选封臣", "封臣", "那就按这个", "那就这么办"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (ContainsIgnoreCase(text, array[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsNpcAskingForConfirmation(string npcText)
	{
		string text = StripActionTags(npcText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!text.Contains("？") && !text.Contains("?"))
		{
			return false;
		}
		string[] array = new string[13]
		{
			"是否", "要不要", "你要", "你是要", "确认", "同意", "愿意", "还是", "雇佣兵", "封臣",
			"成交吗", "继续吗", "要这样吗"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (ContainsIgnoreCase(text, array[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void ClearRuleConfirmCarry()
	{
		_confirmCarryHeroId = null;
		_confirmCarryRoundsLeft = 0;
		_confirmCarryDuel = false;
		_confirmCarryReward = false;
		_confirmCarryLoan = false;
		_confirmCarrySurroundings = false;
		_confirmCarryKingdomService = false;
	}

	private bool TryConsumeRuleConfirmCarry(Hero targetHero, string playerInput, out bool duel, out bool reward, out bool loan, out bool surroundings, out bool kingdomService)
	{
		duel = false;
		reward = false;
		loan = false;
		surroundings = false;
		kingdomService = false;
		string text = targetHero?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text) || _confirmCarryRoundsLeft <= 0)
		{
			ClearRuleConfirmCarry();
			return false;
		}
		if (!string.Equals(_confirmCarryHeroId ?? "", text, StringComparison.Ordinal))
		{
			ClearRuleConfirmCarry();
			return false;
		}
		if (!IsShortAckForRuleFollowup(playerInput))
		{
			ClearRuleConfirmCarry();
			return false;
		}
		duel = _confirmCarryDuel;
		reward = _confirmCarryReward;
		loan = _confirmCarryLoan;
		surroundings = _confirmCarrySurroundings;
		kingdomService = _confirmCarryKingdomService;
		ClearRuleConfirmCarry();
		return duel | reward | loan | surroundings | kingdomService;
	}

	private void UpdateRuleConfirmCarryFromResponse(Hero targetHero, bool duel, bool reward, bool loan, bool surroundings, bool kingdomService, string npcResponse)
	{
		string text = targetHero?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			ClearRuleConfirmCarry();
			return;
		}
		if (!(duel || reward || loan || surroundings || kingdomService))
		{
			ClearRuleConfirmCarry();
			return;
		}
		if (!IsNpcAskingForConfirmation(npcResponse))
		{
			ClearRuleConfirmCarry();
			return;
		}
		_confirmCarryHeroId = text;
		_confirmCarryRoundsLeft = 1;
		_confirmCarryDuel = duel;
		_confirmCarryReward = reward;
		_confirmCarryLoan = loan;
		_confirmCarrySurroundings = surroundings;
		_confirmCarryKingdomService = kingdomService;
	}

	private static string BuildHeroIdentityTitleForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "未知身份";
		}
		try
		{
			if (hero.Occupation == Occupation.Wanderer)
			{
				return "流浪者";
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan != null && clan.IsUnderMercenaryService && kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text + "的雇佣兵";
				}
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (hero.MapFaction?.Name?.ToString() ?? "").Trim();
			if (hero.IsFactionLeader)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2 + "的统治者";
				}
				return "统治者";
			}
			if (hero.IsLord)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2 + "的封臣";
				}
				return "领主";
			}
			if (hero.IsWanderer)
			{
				return "流浪者";
			}
			if (hero.IsNotable)
			{
				return "地方要人";
			}
			switch (hero.Occupation)
			{
			case Occupation.Merchant:
				return "商人";
			case Occupation.Artisan:
				return "工匠";
			case Occupation.GangLeader:
				return "帮派首领";
			case Occupation.Headman:
				return "村长";
			case Occupation.Preacher:
				return "教士";
			case Occupation.RuralNotable:
				return "乡绅";
			}
		}
		catch
		{
		}
		return "普通角色";
	}

	private static void GetHeroFactionAndLiegeForPrompt(Hero hero, out string factionName, out string liegeName)
	{
		factionName = "无（独立）";
		liegeName = "无";
		if (hero == null)
		{
			return;
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					factionName = text;
				}
				string text2 = (kingdom.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					liegeName = text2;
				}
				if (kingdom.Leader == hero)
				{
					string text3 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						liegeName = text3 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = hero.MapFaction;
			if (mapFaction != null)
			{
				string text4 = (mapFaction.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4))
				{
					factionName = text4;
				}
				string text5 = (mapFaction.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					liegeName = text5;
				}
				if (mapFaction.Leader == hero)
				{
					string text6 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6))
					{
						liegeName = text6 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			if (clan != null)
			{
				string text7 = (clan.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text7))
				{
					factionName = text7;
				}
				string text8 = (clan.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text8))
				{
					liegeName = text8;
				}
			}
		}
		catch
		{
		}
	}

	private static string GetHeroCultureNameForPrompt(Hero hero)
	{
		try
		{
			string text = (hero?.Culture?.Name?.ToString() ?? "").Trim();
			return string.IsNullOrWhiteSpace(text) ? "未知文化" : text;
		}
		catch
		{
			return "未知文化";
		}
	}

	private static string BuildNpcClanRoleHintForPrompt(Hero npcHero)
	{
		try
		{
			if (npcHero == null || npcHero.Clan == null)
			{
				return "";
			}
			Hero leader = npcHero.Clan.Leader;
			if (leader == null)
			{
				return "";
			}
			if (leader == npcHero)
			{
				return "你是家族的族长";
			}
			if (npcHero.Spouse == leader || leader.Spouse == npcHero)
			{
				return "你是家族族长的配偶";
			}
			if (npcHero.Father == leader || npcHero.Mother == leader)
			{
				return npcHero.IsFemale ? "你是家族族长的女儿" : "你是家族族长的儿子";
			}
			if (leader.Father == npcHero || leader.Mother == npcHero)
			{
				return npcHero.IsFemale ? "你是家族族长的母亲" : "你是家族族长的父亲";
			}
		}
		catch
		{
		}
		return "";
	}

	private static IEnumerable<Hero> GetClanMembersForPrompt(Clan clan)
	{
		if (clan == null)
		{
			yield break;
		}
		IEnumerable enumerable = null;
		try
		{
			PropertyInfo property = clan.GetType().GetProperty("Lords", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				enumerable = property.GetValue(clan, null) as IEnumerable;
			}
			if (enumerable == null)
			{
				PropertyInfo property2 = clan.GetType().GetProperty("Heroes", BindingFlags.Instance | BindingFlags.Public);
				if (property2 != null)
				{
					enumerable = property2.GetValue(clan, null) as IEnumerable;
				}
			}
		}
		catch
		{
			enumerable = null;
		}
		if (enumerable == null)
		{
			yield break;
		}
		HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (object item in enumerable)
		{
			if (item is Hero hero && hero != null)
			{
				string text = (hero.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && yielded.Add(text))
				{
					yield return hero;
				}
			}
		}
	}

	private const int MarriageCandidateMinAgeForPrompt = 18;

	private const int MarriageCandidateMaxAgeForPrompt = 55;

	private const int MarriageCandidateMaxAgeGapForPrompt = 25;

	private static int GetMarriageCandidateMaxAgeSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAge).GetValueOrDefault(MarriageCandidateMaxAgeForPrompt);
			if (valueOrDefault < MarriageCandidateMinAgeForPrompt)
			{
				valueOrDefault = MarriageCandidateMinAgeForPrompt;
			}
			if (valueOrDefault > 80)
			{
				valueOrDefault = 80;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeForPrompt;
		}
	}

	private static int GetMarriageCandidateMaxAgeGapSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap).GetValueOrDefault(MarriageCandidateMaxAgeGapForPrompt);
			if (valueOrDefault < 0)
			{
				valueOrDefault = 0;
			}
			if (valueOrDefault > 60)
			{
				valueOrDefault = 60;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeGapForPrompt;
		}
	}

	private static bool GetMarriageRequireOppositeGenderSettingForPrompt()
	{
		try
		{
			return (DuelSettings.GetSettings()?.MarriageRequireOppositeGender).GetValueOrDefault(true);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsMarriageGenderCompatibleForPrompt(Hero candidate, Hero player)
	{
		try
		{
			if (candidate == null || player == null)
			{
				return true;
			}
			if (!GetMarriageRequireOppositeGenderSettingForPrompt())
			{
				return true;
			}
			return candidate.IsFemale != player.IsFemale;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsMarriageAgeCompatibleForPrompt(Hero candidate, Hero player)
	{
		try
		{
			if (candidate == null)
			{
				return false;
			}
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (candidate.Age < (float)MarriageCandidateMinAgeForPrompt || candidate.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
			{
				return false;
			}
			if (player != null)
			{
				return Math.Abs(candidate.Age - player.Age) <= (float)GetMarriageCandidateMaxAgeGapSettingForPrompt();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMarriageCandidateForPrompt(Hero hero, Hero player)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (!IsMarriageAgeCompatibleForPrompt(hero, player))
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
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				return false;
			}
		}
		catch
		{
		}
		if (player != null && hero == player)
		{
			return false;
		}
		if (!IsMarriageGenderCompatibleForPrompt(hero, player))
		{
			return false;
		}
		return true;
	}

	private static bool IsMarriagePoolCandidateForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (hero.Age < (float)MarriageCandidateMinAgeForPrompt || hero.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
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
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static string GetMarriageCandidateGenderLabelForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			return hero.IsFemale ? "女" : "男";
		}
		catch
		{
			return "未知";
		}
	}

	private static string BuildClanUnmarriedCandidatesForPrompt(Hero npcHero, Hero player, int maxEntries = 12)
	{
		try
		{
			Clan clan = npcHero?.Clan;
			if (clan == null)
			{
				return "无（目标无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（该家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == npcHero)
				{
					text2 = "（你自己）";
				}
				else if (hero == leader)
				{
					text2 = "（族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num} | StringId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成未婚名单时发生异常）";
		}
	}

	private static string BuildHeroEquipmentSummaryForPrompt(Hero hero, int maxEntries = 8)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9]
			{
				EquipmentIndex.NumAllWeaponSlots,
				EquipmentIndex.Body,
				EquipmentIndex.Leg,
				EquipmentIndex.Gloves,
				EquipmentIndex.Cape,
				EquipmentIndex.WeaponItemBeginSlot,
				EquipmentIndex.Weapon1,
				EquipmentIndex.Weapon2,
				EquipmentIndex.Weapon3
			};
			EquipmentIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EquipmentIndex index = array2[i];
				ItemObject item = hero.BattleEquipment[index].Item;
				if (item != null)
				{
					string text = (item.StringId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text))
					{
						text = index.ToString();
					}
					string value = (item.Name?.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(value))
					{
						value = text;
					}
					if (!dictionary.ContainsKey(text))
					{
						dictionary[text] = 0;
						names[text] = value;
					}
					dictionary[text]++;
				}
			}
			if (dictionary.Count == 0)
			{
				return "无可识别装备";
			}
			List<string> values = (from x in (from x in dictionary.Select(delegate(KeyValuePair<string, int> kv)
					{
						string value2;
						string name = (names.TryGetValue(kv.Key, out value2) ? value2 : kv.Key);
						return new
						{
							Name = name,
							Count = kv.Value
						};
					})
					orderby x.Count descending
					select x).ThenBy(x => x.Name, StringComparer.Ordinal).Take(Math.Max(1, maxEntries))
				select x.Name + "x" + x.Count).ToList();
			return string.Join("、", values);
		}
		catch
		{
			return "无可识别装备";
		}
	}

	private string BuildPlayerIdentityInfoForPrompt(Hero playerHero, bool includeRuleGatedFields, bool includeTradePricing, bool includeMarriageCandidates = false, Hero targetHero = null)
	{
		if (playerHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = playerHero.Clan?.Tier ?? 0;
			string text2 = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(playerHero, out var factionName, out var liegeName);
		bool flag = false;
		try
		{
			flag = playerHero.Clan?.Kingdom != null;
		}
		catch
		{
			flag = false;
		}
		string text3 = BuildHeroIdentityTitleForPrompt(playerHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(playerHero);
		string text4 = BuildAgeBracketLabel(playerHero.Age);
		string text5 = BuildHeroEquipmentSummaryForPrompt(playerHero);
		string value = "";
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					value = RewardSystemBehavior.Instance.BuildVisibleEquipmentValueSummaryForAI(playerHero);
				}
			}
			catch
			{
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[player身份信息]");
		stringBuilder.AppendLine("玩家文化：" + heroCultureNameForPrompt);
		if (includeRuleGatedFields)
		{
			stringBuilder.AppendLine("玩家声望：" + GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
			stringBuilder.AppendLine("玩家势力：" + factionName + "（效忠：" + liegeName + "）");
			stringBuilder.AppendLine("玩家身份：" + text3);
			stringBuilder.AppendLine("玩家家族：" + text + $"（{Math.Max(0, num)} level，你是家族的族长）");
		}
		else if (flag)
		{
			stringBuilder.AppendLine("玩家势力：" + factionName + "（效忠：" + liegeName + "）");
		}
		else
		{
			stringBuilder.AppendLine("提示：你感觉player（玩家）只是个普通人。");
		}
		stringBuilder.AppendLine("玩家年纪：" + text4);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
			stringBuilder.AppendLine(value);
		}
		else
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
		}
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			stringBuilder.AppendLine("【玩家家族可婚配未婚成员（事实清单）】");
			stringBuilder.AppendLine($"筛选口径：仅列出玩家家族内部基础适婚池（年龄 {MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与对方成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildPlayerClanUnmarriedCandidatesForPrompt(playerHero, targetHero, 12));
		}
		return stringBuilder.ToString().Trim();
	}

	private string BuildNpcIdentityInfoForPrompt(Hero npcHero, bool includeTradePricing, bool includeMarriageCandidates = false)
	{
		if (npcHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = npcHero.Clan?.Tier ?? 0;
			string text2 = (npcHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string text3 = BuildHeroIdentityTitleForPrompt(npcHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(npcHero);
		string text4 = BuildNpcClanRoleHintForPrompt(npcHero);
		string text5 = BuildAgeBracketLabel(npcHero.Age);
		string text6 = BuildHeroEquipmentSummaryForPrompt(npcHero);
		int num2 = 0;
		try
		{
			num2 = Math.Max(0, npcHero.Gold);
		}
		catch
		{
			num2 = 0;
		}
		Hero hero = null;
		try
		{
			hero = npcHero.Clan?.Leader;
		}
		catch
		{
			hero = null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[NPC身份信息]");
		stringBuilder.AppendLine("NPC文化：" + heroCultureNameForPrompt);
		stringBuilder.AppendLine("NPC声望：" + GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
		stringBuilder.AppendLine("NPC势力：" + factionName + "（效忠：" + liegeName + "）");
		stringBuilder.AppendLine("NPC身份：" + text3);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level，{text4}）");
		}
		else
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level）");
		}
		if (hero != null)
		{
			if (hero == npcHero)
			{
				stringBuilder.AppendLine("该家族族长：你本人（StringId=" + (npcHero.StringId ?? "") + "）");
			}
			else
			{
				stringBuilder.AppendLine("该家族族长：" + (hero.Name?.ToString() ?? "未知") + "（StringId=" + (hero.StringId ?? "") + "）");
			}
		}
		stringBuilder.AppendLine("NPC年纪：" + text5);
		stringBuilder.AppendLine("NPC存款：" + num2 + " 第纳尔");
		stringBuilder.AppendLine("NPC装备：" + text6);
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			stringBuilder.AppendLine("【该家族可婚配未婚成员（事实清单）】");
			stringBuilder.AppendLine($"筛选口径：仅列出该家族内部基础适婚池（年龄 {MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与玩家家族成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildClanUnmarriedCandidatesForPrompt(npcHero, Hero.MainHero, 12));
		}
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				string text7 = RewardSystemBehavior.Instance.BuildInventorySummaryForAI(npcHero);
				if (!string.IsNullOrWhiteSpace(text7))
				{
					stringBuilder.AppendLine("【NPC当前可用财富与物品】");
					stringBuilder.AppendLine(text7);
					Logger.Log("Logic", "[Context] InventorySummary=\n" + text7);
				}
			}
			catch
			{
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private static int GetDaysInSeasonSafeForPrompt()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafeForPrompt()
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return GetDaysInSeasonSafeForPrompt() * 4;
	}

	private static string GetSeasonTextZhForPrompt(int seasonIndexZeroBased)
	{
		int num = seasonIndexZeroBased % 4;
		if (num < 0)
		{
			num += 4;
		}
		return num switch
		{
			0 => "春", 
			1 => "夏", 
			2 => "秋", 
			_ => "冬", 
		};
	}

	private string BuildCurrentDateFactForPrompt()
	{
		try
		{
			int num = Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
			int daysInSeasonSafeForPrompt = GetDaysInSeasonSafeForPrompt();
			int daysInYearSafeForPrompt = GetDaysInYearSafeForPrompt();
			int num2 = num / Math.Max(1, daysInYearSafeForPrompt);
			int num3 = num % Math.Max(1, daysInYearSafeForPrompt);
			int seasonIndexZeroBased = num3 / Math.Max(1, daysInSeasonSafeForPrompt);
			int num4 = num3 % Math.Max(1, daysInSeasonSafeForPrompt) + 1;
			string seasonTextZhForPrompt = GetSeasonTextZhForPrompt(seasonIndexZeroBased);
			string text = "";
			try
			{
				text = CampaignTime.Now.ToString();
			}
			catch
			{
				text = "";
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = $"{num2}年{seasonTextZhForPrompt}第{num4}天";
			}
			return $"当前游戏日期：{text}（绝对天数第 {num} 天；{num2}年{seasonTextZhForPrompt}第{num4}天）";
		}
		catch
		{
			return "";
		}
	}

	private static bool TryParsePersonaJson(string text, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.Trim();
		int num = text2.IndexOf('{');
		int num2 = text2.LastIndexOf('}');
		if (num >= 0 && num2 > num)
		{
			text2 = text2.Substring(num, num2 - num + 1);
		}
		try
		{
			JObject jObject = JObject.Parse(text2);
			personality = (jObject["personality"] ?? jObject["Personality"])?.ToString() ?? "";
			background = (jObject["background"] ?? jObject["Background"])?.ToString() ?? "";
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task EnsureNpcPersonaGeneratedAsync(Hero hero)
	{
		if (hero == null)
		{
			return;
		}
		string id = hero.StringId;
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		bool needP = string.IsNullOrWhiteSpace(personality);
		bool needB = string.IsNullOrWhiteSpace(background);
		if (!needP && !needB)
		{
			return;
		}
		lock (_npcPersonaAutoGenLock)
		{
			if (_npcPersonaAutoGenInFlight.Contains(id))
			{
				return;
			}
			_npcPersonaAutoGenInFlight.Add(id);
		}
		try
		{
			string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。personality 大约 300 个中文字符；background 大约 300 个中文字符。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系或身份；若事实中提供了势力/效忠信息，必须保持一致，禁止声称效忠于其他统治者或属于其他势力。";
			string facts = BuildHeroFactsForPersonaGeneration(hero);
			string user = "请基于以下信息生成该 NPC 的【个性】与【历史背景】。\n" + facts;
			string resp = await CallUniversalApi(sys, user);
			if (!string.IsNullOrWhiteSpace(resp) && !resp.StartsWith("错误") && TryParsePersonaJson(resp, out var genP, out var genB))
			{
				genP = TrimToMaxChars(genP, 380);
				genB = TrimToMaxChars(genB, 380);
				GetNpcPersonaStrings(hero, out var curP, out var curB);
				NpcPersonaProfile prof = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
				prof.Personality = (string.IsNullOrWhiteSpace(curP) ? genP : curP.Trim());
				prof.Background = (string.IsNullOrWhiteSpace(curB) ? genB : curB.Trim());
				SaveNpcPersonaProfile(hero, prof);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("NpcPersona", "[ERROR] AutoGen failed: " + ex2.Message);
		}
		finally
		{
			lock (_npcPersonaAutoGenLock)
			{
				_npcPersonaAutoGenInFlight.Remove(id);
			}
		}
	}

	private void OnChatConfirmed(string input)
	{
		if (!string.IsNullOrWhiteSpace(input))
		{
			StartAiConversation(input, null);
		}
	}

	private void BeginGiveFlow(string npcName)
	{
		_pendingTrade = new PendingTradeContext
		{
			IsGive = true
		};
		ShowResourceSelectionInquiry(npcName);
	}

	private void BeginShowFlow(string npcName)
	{
		_pendingTrade = new PendingTradeContext
		{
			IsGive = false
		};
		ShowResourceSelectionInquiry(npcName);
	}

	private void ShowResourceSelectionInquiry(string npcName)
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		List<TradeResourceOption> list = (_currentTradeOptions = BuildResourceOptions(oneToOneConversationHero));
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你没有可以使用的物品或第纳尔。"));
			_pendingTrade = null;
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		for (int i = 0; i < list.Count; i++)
		{
			TradeResourceOption tradeResourceOption = list[i];
			string hint = $"可用数量: {tradeResourceOption.AvailableAmount}";
			list2.Add(new InquiryElement(i, $"{tradeResourceOption.Name} (×{tradeResourceOption.AvailableAmount})", null, isEnabled: true, hint));
		}
		string titleText = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予其物品" : "展示物品");
		string descriptionText = "选择要使用的物品或第纳尔（可多选）：";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list2, isExitShown: true, 1, list2.Count, "确定", "取消", OnMultiResourceSelected, OnMultiResourceCancelled, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OnMultiResourceSelected(List<InquiryElement> selectedElements)
	{
		if (selectedElements == null || selectedElements.Count == 0 || _currentTradeOptions == null)
		{
			_pendingTrade = null;
			return;
		}
		_pendingTradeItems = new List<PendingTradeItem>();
		foreach (InquiryElement selectedElement in selectedElements)
		{
			int num = (int)selectedElement.Identifier;
			if (num >= 0 && num < _currentTradeOptions.Count)
			{
				TradeResourceOption tradeResourceOption = _currentTradeOptions[num];
				_pendingTradeItems.Add(new PendingTradeItem
				{
					IsGold = tradeResourceOption.IsGold,
					ItemId = tradeResourceOption.ItemId,
					ItemName = tradeResourceOption.Name,
					Item = tradeResourceOption.Item,
					Amount = 0
				});
			}
		}
		if (_pendingTradeItems.Count == 0)
		{
			_pendingTrade = null;
		}
		else
		{
			_pendingTradeItemIndex = 0;
			ShowAmountInquiryForCurrentItem();
		}
	}

	private void OnMultiResourceCancelled(List<InquiryElement> _)
	{
		_pendingTrade = null;
		_pendingTradeItems.Clear();
	}

	private void ShowAmountInquiryForCurrentItem()
	{
		if (_pendingTradeItemIndex >= _pendingTradeItems.Count)
		{
			ShowTradeChatInput();
			return;
		}
		PendingTradeItem pendingTradeItem = _pendingTradeItems[_pendingTradeItemIndex];
		int num = 0;
		foreach (TradeResourceOption currentTradeOption in _currentTradeOptions)
		{
			if (currentTradeOption.IsGold == pendingTradeItem.IsGold && currentTradeOption.ItemId == pendingTradeItem.ItemId)
			{
				num = currentTradeOption.AvailableAmount;
				break;
			}
		}
		if (num <= 0)
		{
			_pendingTradeItemIndex++;
			ShowAmountInquiryForCurrentItem();
			return;
		}
		string titleText = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予数量" : "展示数量");
		string text = $"[{_pendingTradeItemIndex + 1}/{_pendingTradeItems.Count}] 你可以使用的 {pendingTradeItem.ItemName} 数量最多为 {num}。\n请输入 1 到 {num} 的整数：";
		int maxAmount = num;
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
			if (!int.TryParse(input, out var result) || result <= 0 || result > maxAmount)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的数量。"));
				ShowAmountInquiryForCurrentItem();
			}
			else
			{
				_pendingTradeItems[_pendingTradeItemIndex].Amount = result;
				_pendingTradeItemIndex++;
				ShowAmountInquiryForCurrentItem();
			}
		}, delegate
		{
			string npcName = ((Hero.OneToOneConversationHero != null) ? Hero.OneToOneConversationHero.Name.ToString() : "陌生人");
			ShowResourceSelectionInquiry(npcName);
		}));
	}

	private void ShowTradeChatInput()
	{
		string text = ((Hero.OneToOneConversationHero != null) ? Hero.OneToOneConversationHero.Name.ToString() : "陌生人");
		bool flag = _pendingTrade != null && _pendingTrade.IsGive;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					stringBuilder.AppendLine(flag ? $"  · 给予 {pendingTradeItem.Amount} 第纳尔" : $"  · 展示 {pendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					stringBuilder.AppendLine(flag ? $"  · 给予 {pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}" : $"  · 展示 {pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}");
				}
			}
		}
		string text2 = (flag ? "你准备给予对方以下物品：\n" : "你准备向对方展示以下物品：\n") + stringBuilder.ToString();
		string titleText = "与 " + text + " 交流";
		string text3 = text2 + "\n请输入你想说的话：";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text3, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "说", "取消", OnTradeChatConfirmed, null));
	}

	private void OnTradeChatConfirmed(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			_pendingTrade = null;
			_pendingTradeItems.Clear();
			return;
		}
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		string extraFact = "";
		if (_pendingTrade != null && _pendingTradeItems != null && _pendingTradeItems.Count > 0)
		{
			if (_pendingTrade.IsGive)
			{
				ApplyGiveTransfer(oneToOneConversationHero);
				extraFact = BuildGiveFactText();
			}
			else
			{
				ApplyShowRecord(oneToOneConversationHero);
				extraFact = BuildShowFactText();
			}
		}
		_pendingTradeItems.Clear();
		StartAiConversation(input, extraFact);
	}

	private void ApplyGiveTransfer(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0 || targetHero == null)
		{
			return;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				int num = Math.Min(pendingTradeItem.Amount, Hero.MainHero.Gold);
				if (num > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, targetHero, num);
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(targetHero, num, null, 0);
					}
					catch
					{
					}
				}
			}
			else
			{
				if (mobileParty == null)
				{
					continue;
				}
				ItemRoster itemRoster = mobileParty.ItemRoster;
				if (itemRoster == null || pendingTradeItem.Item == null)
				{
					continue;
				}
				int itemNumber = itemRoster.GetItemNumber(pendingTradeItem.Item);
				int num2 = Math.Min(pendingTradeItem.Amount, itemNumber);
				if (num2 > 0)
				{
					itemRoster.AddToCounts(pendingTradeItem.Item, -num2);
					if (targetHero.PartyBelongedTo != null)
					{
						targetHero.PartyBelongedTo.ItemRoster.AddToCounts(pendingTradeItem.Item, num2);
					}
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(targetHero, 0, pendingTradeItem.ItemId, num2);
					}
					catch
					{
					}
				}
			}
		}
	}

	private void ApplyShowRecord(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0 || targetHero == null)
		{
			return;
		}
		HeroShownRecord shownRecord = GetShownRecord(targetHero);
		if (shownRecord == null)
		{
			return;
		}
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				shownRecord.ShownGold += pendingTradeItem.Amount;
				continue;
			}
			if (!shownRecord.ShownItems.ContainsKey(pendingTradeItem.ItemId))
			{
				shownRecord.ShownItems[pendingTradeItem.ItemId] = 0;
			}
			shownRecord.ShownItems[pendingTradeItem.ItemId] += pendingTradeItem.Amount;
		}
	}

	private static string BuildPlayerClanUnmarriedCandidatesForPrompt(Hero playerHero, Hero targetHero, int maxEntries = 12)
	{
		try
		{
			Clan clan = playerHero?.Clan;
			if (clan == null)
			{
				return "无（玩家无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（玩家家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == playerHero)
				{
					text2 = "（玩家本人）";
				}
				else if (hero == leader)
				{
					text2 = "（玩家家族族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num} | StringId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成玩家家族未婚名单时发生异常）";
		}
	}

	public static void RecordShownResourcesForExternal(Hero hero, string targetKey, int shownGold, Dictionary<string, int> shownItems)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordShownResources(hero, targetKey, shownGold, shownItems);
		}
		catch
		{
		}
	}

	public static int GetRemainingShowableGoldForExternal(Hero hero, string targetKey, int currentGold)
	{
		try
		{
			MyBehavior campaignBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (campaignBehavior == null)
			{
				return Math.Max(0, currentGold);
			}
			return campaignBehavior.GetRemainingShowableGold(hero, targetKey, currentGold);
		}
		catch
		{
			return Math.Max(0, currentGold);
		}
	}

	public static int GetRemainingShowableItemCountForExternal(Hero hero, string targetKey, string itemId, int currentAmount)
	{
		try
		{
			MyBehavior campaignBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (campaignBehavior == null)
			{
				return Math.Max(0, currentAmount);
			}
			return campaignBehavior.GetRemainingShowableItemCount(hero, targetKey, itemId, currentAmount);
		}
		catch
		{
			return Math.Max(0, currentAmount);
		}
	}

	private string BuildGiveFactText()
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		string text = Hero.MainHero?.Name?.ToString() ?? "玩家";
		List<string> list = new List<string>();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					list.Add($"{pendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return text + "已经将 " + string.Join("、", list) + " 交给你。";
	}

	private string BuildShowFactText()
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		string text = Hero.MainHero?.Name?.ToString() ?? "玩家";
		List<string> list = new List<string>();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					list.Add($"{pendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		string text2 = string.Join("、", list);
		return text + "给你看了看 " + text2 + "，但是没有将这些东西交给你。";
	}

	private List<TradeResourceOption> BuildResourceOptions(Hero targetHero)
	{
		List<TradeResourceOption> list = new List<TradeResourceOption>();
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return list;
		}
		ItemRoster itemRoster = mobileParty.ItemRoster;
		HeroShownRecord heroShownRecord = ((_pendingTrade != null && !_pendingTrade.IsGive) ? GetShownRecord(targetHero) : null);
		int gold = Hero.MainHero.Gold;
		if (_pendingTrade != null && _pendingTrade.IsGive)
		{
			if (gold > 0)
			{
				list.Add(new TradeResourceOption
				{
					IsGold = true,
					ItemId = null,
					Name = "第纳尔",
					AvailableAmount = gold,
					Item = null
				});
			}
		}
		else
		{
			int num = gold;
			if (heroShownRecord != null)
			{
				num = Math.Max(0, gold - heroShownRecord.ShownGold);
			}
			if (num > 0)
			{
				list.Add(new TradeResourceOption
				{
					IsGold = true,
					ItemId = null,
					Name = "第纳尔",
					AvailableAmount = num,
					Item = null
				});
			}
		}
		if (itemRoster != null)
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemObject itemAtIndex = itemRoster.GetItemAtIndex(i);
				if (itemAtIndex == null)
				{
					continue;
				}
				int itemNumber = itemRoster.GetItemNumber(itemAtIndex);
				if (itemNumber > 0)
				{
					int num2 = itemNumber;
					if (_pendingTrade != null && !_pendingTrade.IsGive && heroShownRecord != null && heroShownRecord.ShownItems.TryGetValue(itemAtIndex.StringId, out var value))
					{
						num2 = Math.Max(0, itemNumber - value);
					}
					if (num2 > 0)
					{
						list.Add(new TradeResourceOption
						{
							IsGold = false,
							ItemId = itemAtIndex.StringId,
							Name = itemAtIndex.Name.ToString(),
							AvailableAmount = num2,
							Item = itemAtIndex
						});
					}
				}
			}
		}
		return list;
	}

	private HeroShownRecord GetShownRecord(Hero hero)
	{
		return GetShownRecord(hero, null, createIfMissing: true);
	}

	private HeroShownRecord GetShownRecord(Hero hero, string targetKey, bool createIfMissing)
	{
		string text = ResolveShownRecordKey(hero, targetKey);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (!_shownRecords.TryGetValue(text, out var value))
		{
			if (!createIfMissing)
			{
				return null;
			}
			value = new HeroShownRecord();
			_shownRecords[text] = value;
		}
		if (value.ShownItems == null)
		{
			value.ShownItems = new Dictionary<string, int>();
		}
		return value;
	}

	private void RecordShownResources(Hero hero, string targetKey, int shownGold, Dictionary<string, int> shownItems)
	{
		if (shownGold <= 0 && (shownItems == null || shownItems.Count == 0))
		{
			return;
		}
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: true);
		if (shownRecord == null)
		{
			return;
		}
		if (shownGold > 0)
		{
			shownRecord.ShownGold += shownGold;
		}
		if (shownItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> shownItem in shownItems)
		{
			string text = (shownItem.Key ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || shownItem.Value <= 0)
			{
				continue;
			}
			if (!shownRecord.ShownItems.ContainsKey(text))
			{
				shownRecord.ShownItems[text] = 0;
			}
			shownRecord.ShownItems[text] += shownItem.Value;
		}
	}

	private int GetRemainingShowableGold(Hero hero, string targetKey, int currentGold)
	{
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: false);
		if (shownRecord == null)
		{
			return Math.Max(0, currentGold);
		}
		return Math.Max(0, currentGold - Math.Max(0, shownRecord.ShownGold));
	}

	private int GetRemainingShowableItemCount(Hero hero, string targetKey, string itemId, int currentAmount)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return Math.Max(0, currentAmount);
		}
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: false);
		if (shownRecord == null || shownRecord.ShownItems == null || !shownRecord.ShownItems.TryGetValue(itemId, out var value))
		{
			return Math.Max(0, currentAmount);
		}
		return Math.Max(0, currentAmount - Math.Max(0, value));
	}

	private static string ResolveShownRecordKey(Hero hero, string targetKey)
	{
		string text = hero?.StringId;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = targetKey;
		}
		return NormalizeShownRecordKey(text);
	}

	private static string NormalizeShownRecordKey(string targetKey)
	{
		return (targetKey ?? "").Trim().ToLowerInvariant();
	}

	private void StartAiConversation(string input, string extraFact)
	{
		Hero targetHero = Hero.OneToOneConversationHero;
		CharacterObject targetCharacter = null;
		try
		{
			targetCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
		}
		catch
		{
		}
		string npcName = targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC";
		string cultureId = targetHero?.Culture?.StringId ?? targetCharacter?.Culture?.StringId ?? "neutral";
		int playerTier = Hero.MainHero.Clan.Tier;
		string characterDescription = GetHeroDescriptionSafe(targetHero);
		string locationInfo = GetCurrentLocationInfoSafe();
		bool hasUnpaidDebt = RewardSystemBehavior.Instance != null && targetHero != null && RewardSystemBehavior.Instance.HasUnpaidDebt(targetHero);
		if (hasUnpaidDebt)
		{
			Logger.Log("Logic", "[Trade] 检测到玩家尚欠 " + npcName + " 的货款或物品，疑似上一笔交易逃单。");
		}
		InformationManager.DisplayMessage(new InformationMessage(npcName + " 正在思考...", new Color(0.7f, 0.7f, 0.7f)));
		Task.Run(async delegate
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			using (Logger.BeginTrace("direct", targetHero?.StringId, npcName))
			{
				Logger.Obs("DirectChat", "start", new Dictionary<string, object>
				{
					["npc"] = npcName,
					["heroId"] = targetHero?.StringId ?? "",
					["inputLen"] = (input ?? "").Length,
					["hasUnpaidDebt"] = hasUnpaidDebt
				});
				await Task.Delay(300);
				SimulateLeftClick();
				try
				{
					if (targetHero != null)
					{
						_ = EnsureNpcPersonaGeneratedAsync(targetHero);
					}
					AIConfigHandler.SetGuardrailSemanticContext(BuildGuardrailSemanticContext(targetHero, extraFact));
					bool patienceExhausted = false;
					string patienceExhaustedReply = "";
					string patiencePrompt = BuildDirectPatiencePromptForExternal(targetHero, out patienceExhausted, out patienceExhaustedReply);
					try
					{
						string badge = BuildDirectNamePatienceBadgeForExternal(targetHero);
						ConversationHelper.SetNameSuffix(badge);
					}
					catch
					{
					}
					List<string> triggerKeywords = AIConfigHandler.DuelTriggerKeywords;
					string matchedKeyword;
					bool duelLiteralHit = ContainsLiteralKeywordHit(input, triggerKeywords, out matchedKeyword);
					_nextDuelRiskWarningByLiteral = duelLiteralHit;
					bool isTriggerWordDetected = false;
					string duelHitKeyword = "";
					float duelHitScore = 0f;
					if (!patienceExhausted)
					{
						isTriggerWordDetected = AIConfigHandler.IsGuardrailSemanticHit(input, "duel", AIConfigHandler.DuelInstruction, triggerKeywords, out duelHitKeyword, out duelHitScore);
					}
					bool useDuelContext = isTriggerWordDetected;
					if (targetHero != null)
					{
						string heroIdForDuel = targetHero.StringId;
						if (isTriggerWordDetected)
						{
							_isDuelSessionActive = true;
							_duelSessionHeroId = heroIdForDuel;
							_duelSessionNoKeywordRounds = 0;
						}
						else if (_isDuelSessionActive && _duelSessionHeroId == heroIdForDuel)
						{
							_duelSessionNoKeywordRounds++;
							if (_duelSessionNoKeywordRounds >= 2)
							{
								_isDuelSessionActive = false;
								_duelSessionHeroId = null;
								_duelSessionNoKeywordRounds = 0;
								useDuelContext = false;
								Logger.Log("Logic", "[Duel] 连续 1 轮未命中，结束决斗规则延续。");
							}
							else
							{
								useDuelContext = true;
							}
						}
						else if (_isDuelSessionActive && _duelSessionHeroId != heroIdForDuel)
						{
							_isDuelSessionActive = false;
							_duelSessionHeroId = null;
							_duelSessionNoKeywordRounds = 0;
						}
					}
					List<string> rewardKeywords = AIConfigHandler.RewardTriggerKeywords;
					bool isRewardContext = false;
					string rewardHitKeyword = "";
					float rewardHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.RewardEnabled)
					{
						isRewardContext = AIConfigHandler.IsGuardrailSemanticHit(input, "reward", AIConfigHandler.RewardInstruction, rewardKeywords, out rewardHitKeyword, out rewardHitScore);
					}
					List<string> loanKeywords = AIConfigHandler.LoanTriggerKeywords;
					bool isLoanContext = false;
					string loanHitKeyword = "";
					float loanHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.LoanEnabled)
					{
						isLoanContext = AIConfigHandler.IsGuardrailSemanticHit(input, "loan", AIConfigHandler.LoanInstruction, loanKeywords, out loanHitKeyword, out loanHitScore);
					}
					List<string> surroundingsKeywords = AIConfigHandler.SurroundingsTriggerKeywords;
					bool isSurroundingsContext = false;
					string surroundingsHitKeyword = "";
					float surroundingsHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.SurroundingsEnabled)
					{
						isSurroundingsContext = AIConfigHandler.IsGuardrailSemanticHit(input, "surroundings", AIConfigHandler.SurroundingsInstruction, surroundingsKeywords, out surroundingsHitKeyword, out surroundingsHitScore);
					}
					bool isKingdomServiceHit = false;
					string kingdomServiceHitKeyword = "";
					float kingdomServiceHitScore = 0f;
					if (!patienceExhausted)
					{
						isKingdomServiceHit = AIConfigHandler.IsGuardrailSemanticHit(ruleInstruction: AIConfigHandler.GetGuardrailRuleInstruction("kingdom_service"), triggerKeywords: AIConfigHandler.GetGuardrailRuleKeywords("kingdom_service"), input: input, ruleTag: "kingdom_service", matchedKeyword: out kingdomServiceHitKeyword, score: out kingdomServiceHitScore);
					}
					bool isMarriageHit = false;
					string marriageHitKeyword = "";
					float marriageHitScore = 0f;
					if (!patienceExhausted)
					{
						isMarriageHit = AIConfigHandler.IsGuardrailSemanticHit(ruleInstruction: AIConfigHandler.GetGuardrailRuleInstruction("marriage"), triggerKeywords: AIConfigHandler.GetGuardrailRuleKeywords("marriage"), input: input, ruleTag: "marriage", matchedKeyword: out marriageHitKeyword, score: out marriageHitScore);
					}
					bool useRewardContext = isRewardContext;
					if (AIConfigHandler.RewardEnabled && targetHero != null)
					{
						string heroId = targetHero.StringId;
						if (isRewardContext)
						{
							_isRewardSessionActive = true;
							_rewardSessionHeroId = heroId;
							_rewardSessionNoKeywordRounds = 0;
							_rewardSessionFromDuel = false;
							_rewardSessionPlayerWonLastDuel = false;
						}
						else if (_isRewardSessionActive && _rewardSessionHeroId == heroId)
						{
							_rewardSessionNoKeywordRounds++;
							if (_rewardSessionNoKeywordRounds >= 2)
							{
								_isRewardSessionActive = false;
								_rewardSessionHeroId = null;
								_rewardSessionNoKeywordRounds = 0;
								_rewardSessionFromDuel = false;
								_rewardSessionPlayerWonLastDuel = false;
								Logger.Log("Logic", "[Trade] 连续 1 轮未命中，结束交易规则延续。");
							}
							else
							{
								useRewardContext = true;
							}
						}
						else if (_isRewardSessionActive && _rewardSessionHeroId != heroId)
						{
							_isRewardSessionActive = false;
							_rewardSessionHeroId = null;
							_rewardSessionNoKeywordRounds = 0;
							_rewardSessionFromDuel = false;
							_rewardSessionPlayerWonLastDuel = false;
						}
					}
					if (AIConfigHandler.LoanEnabled && targetHero != null)
					{
						string heroId2 = targetHero.StringId;
						if (isLoanContext)
						{
							_isLoanSessionActive = true;
							_loanSessionHeroId = heroId2;
							_loanSessionNoKeywordRounds = 0;
						}
						else if (_isLoanSessionActive && _loanSessionHeroId == heroId2)
						{
							_loanSessionNoKeywordRounds++;
							if (_loanSessionNoKeywordRounds >= 2)
							{
								_isLoanSessionActive = false;
								_loanSessionHeroId = null;
								_loanSessionNoKeywordRounds = 0;
							}
							else
							{
								isLoanContext = true;
							}
						}
						else if (_isLoanSessionActive && _loanSessionHeroId != heroId2)
						{
							_isLoanSessionActive = false;
							_loanSessionHeroId = null;
							_loanSessionNoKeywordRounds = 0;
						}
					}
					if (patienceExhausted)
					{
						useDuelContext = false;
						useRewardContext = false;
						isLoanContext = false;
						isSurroundingsContext = false;
					}
					string guardrailClarifyHint = "";
					if (!patienceExhausted && !useDuelContext && !useRewardContext && !isLoanContext && !isSurroundingsContext)
					{
						guardrailClarifyHint = AIConfigHandler.BuildGuardrailClarificationHint(input, isTriggerWordDetected, duelHitScore, isRewardContext, rewardHitScore, isLoanContext, loanHitScore, isSurroundingsContext, surroundingsHitScore);
					}
					string duelHits = (string.IsNullOrWhiteSpace(duelHitKeyword) ? "" : $"{duelHitKeyword}@{duelHitScore:0.00}");
					string rewardHits = (string.IsNullOrWhiteSpace(rewardHitKeyword) ? "" : $"{rewardHitKeyword}@{rewardHitScore:0.00}");
					string loanHits = (string.IsNullOrWhiteSpace(loanHitKeyword) ? "" : $"{loanHitKeyword}@{loanHitScore:0.00}");
					string surroundingsHits = (string.IsNullOrWhiteSpace(surroundingsHitKeyword) ? "" : $"{surroundingsHitKeyword}@{surroundingsHitScore:0.00}");
					string kingdomServiceHits = (string.IsNullOrWhiteSpace(kingdomServiceHitKeyword) ? "" : $"{kingdomServiceHitKeyword}@{kingdomServiceHitScore:0.00}");
					string marriageHits = (string.IsNullOrWhiteSpace(marriageHitKeyword) ? "" : $"{marriageHitKeyword}@{marriageHitScore:0.00}");
					Logger.Log("Logic", $"[SemanticTrigger] DuelHit={isTriggerWordDetected} [{duelHits}] StickyDuel={_isDuelSessionActive} RewardHit={isRewardContext} [{rewardHits}] LoanHit={isLoanContext} [{loanHits}] StickyReward={_isRewardSessionActive} SurroundingsHit={isSurroundingsContext} [{surroundingsHits}] KingdomServiceHit={isKingdomServiceHit} [{kingdomServiceHits}] StickyKingdomService={_isKingdomServiceRuleSessionActive} MarriageHit={isMarriageHit} [{marriageHits}] StickyMarriage={_isMarriageRuleSessionActive} Input='{input}' NPC='{npcName}'");
					Logger.Obs("DirectChat", "keywords", new Dictionary<string, object>
					{
						["duelHit"] = isTriggerWordDetected,
						["duelHitKeyword"] = duelHitKeyword ?? "",
						["duelHitScore"] = Math.Round(duelHitScore, 3),
						["rewardHit"] = isRewardContext,
						["rewardHitKeyword"] = rewardHitKeyword ?? "",
						["rewardHitScore"] = Math.Round(rewardHitScore, 3),
						["loanHit"] = isLoanContext,
						["loanHitKeyword"] = loanHitKeyword ?? "",
						["loanHitScore"] = Math.Round(loanHitScore, 3),
						["surroundingsHit"] = isSurroundingsContext,
						["surroundingsHitKeyword"] = surroundingsHitKeyword ?? "",
						["surroundingsHitScore"] = Math.Round(surroundingsHitScore, 3),
						["kingdomServiceHit"] = isKingdomServiceHit,
						["kingdomServiceHitKeyword"] = kingdomServiceHitKeyword ?? "",
						["kingdomServiceHitScore"] = Math.Round(kingdomServiceHitScore, 3),
						["marriageHit"] = isMarriageHit,
						["marriageHitKeyword"] = marriageHitKeyword ?? "",
						["marriageHitScore"] = Math.Round(marriageHitScore, 3),
						["clarifyHint"] = (string.IsNullOrWhiteSpace(guardrailClarifyHint) ? "" : "on"),
						["stickyDuel"] = _isDuelSessionActive,
						["stickyReward"] = _isRewardSessionActive,
						["stickyKingdomService"] = _isKingdomServiceRuleSessionActive,
						["stickyMarriage"] = _isMarriageRuleSessionActive
					});
					StringBuilder sb = new StringBuilder();
					Stopwatch swPromptBuild = Stopwatch.StartNew();
					string basePrompt = AIConfigHandler.GlobalPrompt;
					if (string.IsNullOrEmpty(basePrompt))
					{
						basePrompt = "你是一个中世纪领主。";
					}
					string guardrail = AIConfigHandler.GlobalGuardrail;
					string personaContext = BuildNpcPersonaContext(targetHero);
					bool personaInFlight = false;
					bool personaReadyForFixedCache = IsPersonaReadyForFixedLayer(targetHero, out personaInFlight);
					string fixedLayerCacheKey = BuildDirectFixedLayerCacheKey(targetHero, cultureId, basePrompt, guardrail, characterDescription, personaContext, personaReadyForFixedCache);
					bool fixedLayerCacheHit = false;
					string fixedLayerText = PromptComposer.GetOrBuildFixedLayer(fixedLayerCacheKey, personaReadyForFixedCache, delegate
					{
						StringBuilder stringBuilder = new StringBuilder();
						stringBuilder.AppendLine(basePrompt);
						if (!string.IsNullOrEmpty(guardrail))
						{
							stringBuilder.AppendLine(guardrail);
						}
						stringBuilder.AppendLine(BuildDialogueFormatGuardrailPrompt());
						if (!string.IsNullOrWhiteSpace(characterDescription))
						{
							stringBuilder.AppendLine(characterDescription);
						}
						if (!string.IsNullOrEmpty(personaContext))
						{
							stringBuilder.AppendLine(personaContext);
						}
						return stringBuilder.ToString();
					}, out fixedLayerCacheHit);
					DuelSettings settings = DuelSettings.GetSettings();
					int minTier = settings.MinimumClanTier;
					bool isQualified = playerTier >= minTier;
					if (!string.IsNullOrWhiteSpace(locationInfo))
					{
						sb.AppendLine(locationInfo);
					}
					string currentDateFact = BuildCurrentDateFactForPrompt();
					if (!string.IsNullOrWhiteSpace(currentDateFact))
					{
						sb.AppendLine(currentDateFact);
					}
					if (isSurroundingsContext)
					{
						bool hasPos;
						CampaignVec2 pos;
						try
						{
							if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
							{
								hasPos = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
							}
							else
							{
								pos = MobileParty.MainParty.Position;
								hasPos = pos.IsValid();
							}
						}
						catch
						{
							hasPos = false;
							pos = default(CampaignVec2);
						}
						if (hasPos)
						{
							string nearby = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos);
							if (!string.IsNullOrWhiteSpace(nearby))
							{
								sb.AppendLine(nearby);
							}
						}
					}
					try
					{
						if (!LordEncounterBehavior.IsEncounterMeetingMissionActive)
						{
							string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt() ?? "").Replace("\r", "").Trim();
							if (!string.IsNullOrWhiteSpace(nativeInfo))
							{
								if (nativeInfo.Length > 700)
								{
									nativeInfo = nativeInfo.Substring(0, 700) + "…";
								}
								sb.AppendLine(" ");
								sb.AppendLine("【当前定居点（原版到达介绍）】：");
								sb.AppendLine(nativeInfo);
							}
						}
					}
					catch
					{
					}
					string loreContext = (targetHero != null) ? AIConfigHandler.GetLoreContext(input, targetHero) : AIConfigHandler.GetLoreContext(input, targetCharacter);
					if (!string.IsNullOrEmpty(loreContext))
					{
						sb.AppendLine(loreContext);
					}
					PersistLoreToDialogueHistory(targetHero, loreContext);
					string historyContext = BuildHistoryContext(targetHero, 0, input);
					if (!string.IsNullOrEmpty(historyContext))
					{
						sb.AppendLine(historyContext);
					}
					if (RewardSystemBehavior.Instance != null && targetHero != null)
					{
						if (isLoanContext)
						{
							string dueDateHint = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
							if (!string.IsNullOrEmpty(dueDateHint))
							{
								sb.AppendLine(dueDateHint);
							}
							string debtHint = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
							if (!string.IsNullOrEmpty(debtHint))
							{
								sb.AppendLine(debtHint);
							}
						}
						string trustHint = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
						if (!string.IsNullOrEmpty(trustHint))
						{
							sb.AppendLine(trustHint);
						}
					}
					if (!string.IsNullOrEmpty(patiencePrompt))
					{
						sb.AppendLine(patiencePrompt);
					}
					if (targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out var playerWonLastDuel))
					{
						if (playerWonLastDuel)
						{
							sb.AppendLine("【战斗结果】你刚刚在一场正式的决斗中输给了玩家。赌注应已在决斗结束瞬间自动结算；如果【玩家行为补充】中已经记录你已支付/仍欠款，请不要重复支付，只需确认或解释。");
							if (AIConfigHandler.RewardEnabled && !AIConfigHandler.DuelStakeEnabled)
							{
								string heroId3 = targetHero.StringId;
								_isRewardSessionActive = true;
								_rewardSessionHeroId = heroId3;
								_rewardSessionNoKeywordRounds = 0;
								_rewardSessionFromDuel = true;
								_rewardSessionPlayerWonLastDuel = true;
								useRewardContext = true;
							}
						}
						else
						{
							sb.AppendLine("【战斗结果】你刚刚在一场正式的决斗中打败了玩家。赌注应已在决斗结束瞬间自动结算；如果【玩家行为补充】中已经记录玩家欠你多少，请不要重复记账，只需确认并催促履行。");
							if (AIConfigHandler.RewardEnabled && !AIConfigHandler.DuelStakeEnabled)
							{
								string heroId4 = targetHero.StringId;
								_isRewardSessionActive = true;
								_rewardSessionHeroId = heroId4;
								_rewardSessionNoKeywordRounds = 0;
								_rewardSessionFromDuel = true;
								_rewardSessionPlayerWonLastDuel = false;
								useRewardContext = true;
							}
						}
					}
					if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
					{
						if (_recentlyDefeatedByPlayer.Remove(targetHero.StringId))
						{
							sb.AppendLine("【原版战斗结果】你刚刚在一场战斗中被玩家击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
						}
						if (_recentlyReleasedPrisoners.Remove(targetHero.StringId))
						{
							sb.AppendLine("【释放通知】你之前被玩家俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
						}
					}
					AppendPlayerExtraFactLine(sb, extraFact);
					if (!string.IsNullOrWhiteSpace(guardrailClarifyHint))
					{
						sb.AppendLine(guardrailClarifyHint);
					}
					string triggeredRuleInstructions = BuildTriggeredRuleInstructions(input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, hasAnyHero: true, targetCharacter: targetCharacter);
					if (!string.IsNullOrWhiteSpace(triggeredRuleInstructions))
					{
						sb.AppendLine(triggeredRuleInstructions);
					}
					bool includeTradePricing = useRewardContext || isLoanContext || useDuelContext;
					bool includeMarriageCandidates = targetHero != null && (isMarriageHit || (_isMarriageRuleSessionActive && _marriageRuleHeroId == targetHero.StringId));
					bool playerIdentityAlwaysOn = playerTier >= 2;
					bool includePlayerRuleGatedFields = playerIdentityAlwaysOn;
					string playerIdentityInfo = BuildPlayerIdentityInfoForPrompt(Hero.MainHero, includePlayerRuleGatedFields, includeTradePricing, includeMarriageCandidates, targetHero);
					if (!string.IsNullOrWhiteSpace(playerIdentityInfo))
					{
						sb.AppendLine(playerIdentityInfo);
					}
					string npcIdentityInfo = BuildNpcIdentityInfoForPrompt(targetHero, includeTradePricing, includeMarriageCandidates);
					if (!string.IsNullOrWhiteSpace(npcIdentityInfo))
					{
						sb.AppendLine(npcIdentityInfo);
					}
					Logger.Log("Logic", "[Context] SystemPrompt_Base=" + basePrompt.Replace("\n", "\\n"));
					int replyMaxChars = settings.MaxTokens;
					int replyMinChars = ((replyMaxChars > 200) ? (replyMaxChars - 200) : ((int)Math.Ceiling((double)replyMaxChars * 0.8)));
					if (replyMinChars < 1)
					{
						replyMinChars = 1;
					}
					if (replyMinChars > replyMaxChars)
					{
						replyMinChars = replyMaxChars;
					}
					sb.AppendLine($"(回复长度要求：请将本轮回复控制在 {replyMinChars}-{replyMaxChars} 字之间；除非玩家明确要求简短，否则尽量贴近上限，不要少于 {replyMinChars} 字。)");
					string deltaLayerText = sb.ToString();
					string finalSystemPrompt = PromptComposer.Compose(fixedLayerText, deltaLayerText, "直接对话");
					swPromptBuild.Stop();
					Logger.Log("Logic", $"[PromptLayer] mode=direct fixedChars={PromptComposer.CountChars(fixedLayerText)} deltaChars={PromptComposer.CountChars(deltaLayerText)} totalChars={PromptComposer.CountChars(finalSystemPrompt)} fixedCacheHit={fixedLayerCacheHit} personaReady={personaReadyForFixedCache} personaInFlight={personaInFlight} fixedCacheSize={PromptComposer.GetFixedLayerCacheSize()}");
					Logger.Obs("Prompt", "compose", new Dictionary<string, object>
					{
						["mode"] = "direct",
						["fixedChars"] = PromptComposer.CountChars(fixedLayerText),
						["deltaChars"] = PromptComposer.CountChars(deltaLayerText),
						["totalChars"] = PromptComposer.CountChars(finalSystemPrompt),
						["fixedCacheHit"] = fixedLayerCacheHit,
						["personaReady"] = personaReadyForFixedCache,
						["personaInFlight"] = personaInFlight,
						["buildMs"] = Math.Round(swPromptBuild.Elapsed.TotalMilliseconds, 2)
					});
					Logger.Metric("prompt.compose.direct", ok: true, swPromptBuild.Elapsed.TotalMilliseconds);
					Logger.Log("Logic", "[AI Request] NPC=" + npcName + " HeroId=" + (targetHero?.StringId ?? "null") + "\n[SYSTEM]=\n" + finalSystemPrompt + "\n[USER]=\n" + input + "\n");
					List<object> apiMessages = new List<object>
					{
						new
						{
							role = "system",
							content = finalSystemPrompt
						},
						new
						{
							role = "user",
							content = input
						}
					};
					string streamResult = null;
					string streamError = null;
					StringBuilder streamBuf = new StringBuilder();
					int lastDispLen = 0;
					string capturedNpcName = npcName;
					int chunkCount = 0;
					double firstChunkMs = -1.0;
					Stopwatch swApi = Stopwatch.StartNew();
					Logger.Log("Logic", "[HTTP-Stream] 直接对话流式请求 NPC=" + npcName);
					ConversationHelper.BeginStreaming();
					await ShoutNetwork.CallApiWithMessagesStream(apiMessages, 4096, delegate(string delta)
					{
						chunkCount++;
						streamBuf.Append(delta);
						int length = streamBuf.Length;
						if (chunkCount == 1)
						{
							firstChunkMs = swApi.Elapsed.TotalMilliseconds;
							Logger.Log("Logic", $"[HTTP-Stream] 首个 chunk 到达, HasActiveVM={ConversationHelper.HasActiveVM}, delta.len={delta.Length}");
							Logger.Obs("API", "first_chunk", new Dictionary<string, object>
							{
								["mode"] = "direct_stream",
								["firstChunkMs"] = Math.Round(firstChunkMs, 2),
								["deltaLen"] = delta.Length,
								["hasActiveVm"] = ConversationHelper.HasActiveVM
							});
						}
						if (length - lastDispLen >= 2)
						{
							string text = streamBuf.ToString();
							try
							{
								if (ConversationHelper.HasActiveVM)
								{
									ConversationHelper.UpdateDialogText(StripActionTags(text));
								}
								else
								{
									if (chunkCount <= 3)
									{
										Logger.Log("Logic", $"[HTTP-Stream] ⚠\ufe0f VM 未捕获！chunk #{chunkCount}, 使用 DisplayMessage 回退");
									}
									string text2 = ((text.Length > 150) ? ("..." + text.Substring(text.Length - 150)) : text);
									InformationManager.DisplayMessage(new InformationMessage("[" + capturedNpcName + "] " + text2, new Color(0.85f, 0.85f, 0.6f)));
								}
							}
							catch
							{
							}
							lastDispLen = length;
						}
					}, delegate(string fullText)
					{
						streamResult = fullText;
					}, delegate(string err)
					{
						streamError = err;
					});
					ConversationHelper.EndStreaming();
					swApi.Stop();
					if (!string.IsNullOrEmpty(streamError))
					{
						InformationManager.DisplayMessage(new InformationMessage("[API连接失败] " + streamError, new Color(1f, 0.3f, 0.3f)));
						throw new Exception(streamError);
					}
					string result = CleanAIResponse(streamResult ?? "");
					string rawResult = result;
					if (!patienceExhausted && AIConfigHandler.RewardEnabled && RewardSystemBehavior.Instance != null && targetHero != null)
					{
						if (!string.IsNullOrEmpty(rawResult) && (rawResult.IndexOf("[ACTION:GIVE_GOLD:", StringComparison.OrdinalIgnoreCase) >= 0 || rawResult.IndexOf("[ACTION:GIVE_ITEM:", StringComparison.OrdinalIgnoreCase) >= 0) && _isRewardSessionActive && _rewardSessionHeroId == (targetHero?.StringId ?? ""))
						{
							_rewardSessionNoKeywordRounds = 0;
						}
						RewardSystemBehavior.Instance.ApplyRewardTags(targetHero, Hero.MainHero, ref result);
						RomanceSystemBehavior.Instance?.ApplyMarriageTags(targetHero, Hero.MainHero, ref result);
					}
					if (patienceExhausted && string.IsNullOrEmpty(streamError))
					{
						result = StripActionTags(result);
						if (string.IsNullOrWhiteSpace(result))
						{
							result = patienceExhaustedReply ?? string.Empty;
						}
					}
					if (!patienceExhausted && string.IsNullOrEmpty(streamError))
					{
						ApplyPatienceFromDirectResponseExternal(targetHero, ref result);
					}
					try
					{
						string badgeAfter = BuildDirectNamePatienceBadgeForExternal(targetHero);
						ConversationHelper.SetNameSuffix(badgeAfter);
					}
					catch
					{
					}
					Logger.Log("Logic", "[AI Response] NPC=" + npcName + " RAW=\n" + rawResult + "\nFINAL=\n" + result + "\n");
					int actionTagCount;
					try
					{
						actionTagCount = Regex.Matches(rawResult ?? "", "\\[ACTION:[^\\]]+\\]").Count;
					}
					catch
					{
						actionTagCount = 0;
					}
					Logger.Obs("API", "complete", new Dictionary<string, object>
					{
						["mode"] = "direct_stream",
						["ok"] = string.IsNullOrEmpty(streamError),
						["error"] = streamError ?? "",
						["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["chunkCount"] = chunkCount,
						["resultLen"] = (result ?? "").Length,
						["actionTagCount"] = actionTagCount
					});
					Logger.Metric("api.direct.stream", string.IsNullOrEmpty(streamError), swApi.Elapsed.TotalMilliseconds);
					sw.Stop();
					Logger.Obs("DirectChat", "round_done", new Dictionary<string, object>
					{
						["elapsedMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["responseLen"] = (result ?? "").Length
					});
					Logger.Metric("round.direct.total", ok: true, sw.Elapsed.TotalMilliseconds);
					_aiResponseText = result;
					AppendDialogueHistory(targetHero, input, result, extraFact);
					InformationManager.DisplayMessage(new InformationMessage(npcName + " 已准备好回应。", new Color(0f, 1f, 0f)));
					await Task.Delay(300);
					bool hasDuelTag = _aiResponseText != null && _aiResponseText.Contains("[ACTION:DUEL]");
					if (hasDuelTag)
					{
						try
						{
							_suppressAutoClickUntilUtcTicks = DateTime.UtcNow.AddSeconds(30.0).Ticks;
						}
						catch
						{
						}
					}
					CheckDuelAndAutoExit(ref _aiResponseText, isQualified);
					_aiResponseText = StripActionTags(_aiResponseText);
					try
					{
						string ttsVoice = GetNpcVoiceId(targetHero);
						if (string.IsNullOrWhiteSpace(ttsVoice))
						{
							ttsVoice = VoiceMapper.ResolveVoiceId(targetHero);
						}
						TtsEngine.Instance.SpeakAsync(_aiResponseText, -1, -1f, -1, ttsVoice);
					}
					catch
					{
					}
					if (!hasDuelTag)
					{
						SimulateLeftClick();
					}
				}
				catch (Exception ex)
				{
					_aiResponseText = "（NPC 似乎走神了...）";
					Logger.Log("Logic", "[ERROR] StartAiConversation 异常: " + ex.ToString());
					Logger.Obs("DirectChat", "error", new Dictionary<string, object>
					{
						["message"] = ex.Message,
						["type"] = ex.GetType().Name
					});
					Logger.Metric("round.direct.total", ok: false, sw.Elapsed.TotalMilliseconds);
					await Task.Delay(300);
					SimulateLeftClick();
				}
			}
		});
	}

	private string GetHeroDescriptionSafe(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (hero.IsPrisoner)
			{
				string text = hero.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					stringBuilder.Append(" 你目前是" + text + "的囚犯，被关押着，失去了自由。");
				}
				else
				{
					stringBuilder.Append(" 你目前是囚犯，被关押着，失去了自由。");
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private void CheckDuelAndAutoExit(ref string aiResponse, bool isQualified)
	{
		if (string.IsNullOrEmpty(aiResponse))
		{
			return;
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null && _isDuelSessionActive && _duelSessionHeroId == oneToOneConversationHero.StringId)
			{
				DuelBehavior.TryCacheDuelStakeFromText(oneToOneConversationHero, ref aiResponse);
			}
		}
		catch
		{
		}
		if (aiResponse.Contains("[ACTION:DUEL]"))
		{
			Hero oneToOneConversationHero2 = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero2 != null)
			{
				DuelBehavior.TryCacheDuelAfterLinesFromText(oneToOneConversationHero2, ref aiResponse);
				DuelBehavior.TryCacheDuelStakeFromText(oneToOneConversationHero2, ref aiResponse);
			}
			aiResponse = aiResponse.Replace("[ACTION:DUEL]", "").Trim();
			_isDuelSessionActive = false;
			_duelSessionHeroId = null;
			_duelSessionNoKeywordRounds = 0;
			if (isQualified && oneToOneConversationHero2 != null)
			{
				try
				{
					DuelBehavior.SetNextDuelRiskWarningEnabled(_nextDuelRiskWarningByLiteral);
				}
				catch
				{
				}
				_nextDuelRiskWarningByLiteral = true;
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive && Mission.Current != null && DuelBehavior.Instance != null)
				{
					DuelBehavior.Instance.StartDuelViaAI(oneToOneConversationHero2);
				}
				else
				{
					DuelBehavior.PrepareDuel(oneToOneConversationHero2, 10f);
				}
			}
			return;
		}
		Task.Run(async delegate
		{
			await Task.Delay(4000);
			try
			{
				if (DateTime.UtcNow.Ticks < _suppressAutoClickUntilUtcTicks)
				{
					return;
				}
			}
			catch
			{
			}
			SimulateLeftClick();
		});
	}

	private List<DialogueDay> LoadDialogueHistory(Hero hero)
	{
		if (hero == null)
		{
			return new List<DialogueDay>();
		}
		if (_dialogueHistory == null)
		{
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return new List<DialogueDay>();
		}
		if (_dialogueHistory.TryGetValue(stringId, out var value) && value != null)
		{
			return value;
		}
		return new List<DialogueDay>();
	}

	private void SaveDialogueHistory(Hero hero, List<DialogueDay> records)
	{
		if (hero != null && records != null)
		{
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string stringId = hero.StringId;
			if (!string.IsNullOrEmpty(stringId))
			{
				_dialogueHistory[stringId] = records;
			}
		}
	}

	public static void AppendExternalDialogueHistory(Hero hero, string playerText, string aiText, string extraFact)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(hero, playerText, aiText, extraFact);
		}
		catch
		{
		}
	}

	private void AppendDialogueHistory(Hero hero, string playerText, string aiText, string extraFact)
	{
		if (hero == null || (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact)))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			string text = hero.Name?.ToString() ?? "NPC";
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (!string.IsNullOrWhiteSpace(playerText))
			{
				dialogueDay.Lines.Add("玩家: " + playerText);
			}
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				string text2 = extraFact.Trim();
				if (text2.StartsWith("[玩家行为补充]", StringComparison.Ordinal) || text2.StartsWith("[NPC行为补充]", StringComparison.Ordinal))
				{
					dialogueDay.Lines.Add(text2);
				}
				else
				{
					dialogueDay.Lines.Add("[玩家行为补充] " + text2);
				}
			}
			if (!string.IsNullOrWhiteSpace(aiText))
			{
				dialogueDay.Lines.Add(text + ": " + aiText);
			}
			List<(int, string, string)> list2 = new List<(int, string, string)>();
			foreach (DialogueDay item in list)
			{
				if (item.Lines == null)
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						list2.Add((item.GameDayIndex, item.GameDate, line));
					}
				}
			}
			if (list2.Count > 260)
			{
				list2 = list2.Skip(list2.Count - 260).ToList();
			}
			List<DialogueDay> list3 = new List<DialogueDay>();
			foreach (var entry in list2)
			{
				DialogueDay dialogueDay2 = list3.FirstOrDefault((DialogueDay x) => x.GameDayIndex == entry.Item1);
				if (dialogueDay2 == null)
				{
					dialogueDay2 = new DialogueDay
					{
						GameDayIndex = entry.Item1,
						GameDate = entry.Item2
					};
					list3.Add(dialogueDay2);
				}
				dialogueDay2.Lines.Add(entry.Item3);
			}
			SaveDialogueHistory(hero, list3);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 追加失败: " + ex.Message);
		}
	}

	public static string BuildHistoryContextForExternal(Hero hero, int maxLines = 20, string currentInput = null)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildHistoryContext(hero, maxLines, currentInput);
		}
		catch
		{
			return "";
		}
	}

	public static void AppendExternalNpcFact(Hero hero, string factText)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			string text = (factText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = (hero.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "NPC";
			}
			AppendExternalDialogueHistory(hero, null, null, "[NPC行为补充] " + text2 + ": " + text);
		}
		catch
		{
		}
	}

	public static void AppendExternalPlayerFact(Hero hero, string factText)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			string text = (factText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			AppendExternalDialogueHistory(hero, null, null, "[玩家行为补充] " + text);
		}
		catch
		{
		}
	}

	private static void AppendPlayerExtraFactLine(StringBuilder sb, string extraFact)
	{
		if (sb == null)
		{
			return;
		}
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (text.StartsWith("[玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[NPC行为补充]", StringComparison.Ordinal))
			{
				sb.AppendLine(text);
			}
			else
			{
				sb.AppendLine("[玩家行为补充] " + text);
			}
		}
	}

	private string BuildGuardrailSemanticContext(Hero hero, string extraFact)
	{
		try
		{
			List<string> list = new List<string>();
			if (hero != null)
			{
				List<DialogueDay> list2 = LoadDialogueHistory(hero);
				if (list2 != null && list2.Count > 0)
				{
					List<string> list3 = new List<string>();
					foreach (DialogueDay item in list2)
					{
						if (item == null || item.Lines == null)
						{
							continue;
						}
						for (int i = 0; i < item.Lines.Count; i++)
						{
							string text = (item.Lines[i] ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text))
							{
								list3.Add(text);
							}
						}
					}
					int num = Math.Min(6, list3.Count);
					for (int j = list3.Count - num; j < list3.Count; j++)
					{
						if (j >= 0 && j < list3.Count)
						{
							string text2 = list3[j];
							if (text2.Length > 120)
							{
								text2 = text2.Substring(0, 120);
							}
							list.Add(text2);
						}
					}
				}
			}
			string text3 = (extraFact ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				if (text3.Length > 180)
				{
					text3 = text3.Substring(0, 180);
				}
				string text5 = text3.Replace("\r", " ").Replace("\n", " ");
				if (text5.StartsWith("[玩家行为补充]", StringComparison.Ordinal) || text5.StartsWith("[NPC行为补充]", StringComparison.Ordinal))
				{
					list.Add(text5);
				}
				else
				{
					list.Add("[玩家行为补充] " + text5);
				}
			}
			if (list.Count <= 0)
			{
				return "";
			}
			string text4 = string.Join("\n", list);
			if (text4.Length > 700)
			{
				text4 = text4.Substring(text4.Length - 700);
			}
			return text4;
		}
		catch
		{
			return "";
		}
	}

	private void ResetKingdomServiceRuleSession()
	{
		_isKingdomServiceRuleSessionActive = false;
		_kingdomServiceRuleHeroId = null;
		_kingdomServiceRuleNoHitRounds = 0;
		_kingdomServiceRuleCachedBlock = null;
	}

	private void ResetMarriageRuleSession()
	{
		_isMarriageRuleSessionActive = false;
		_marriageRuleHeroId = null;
		_marriageRuleNoHitRounds = 0;
		_marriageRuleCachedBlock = null;
	}

	private static bool TryExtractExtraRuleBlock(string allInstructions, string ruleId, out string block)
	{
		block = "";
		try
		{
			string text = allInstructions ?? "";
			string text2 = "【附加规则:" + (ruleId ?? "").Trim() + "】";
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			int num = text.IndexOf(text2, StringComparison.OrdinalIgnoreCase);
			if (num < 0)
			{
				return false;
			}
			int num2 = text.IndexOf("【附加规则:", num + text2.Length, StringComparison.OrdinalIgnoreCase);
			block = ((num2 > num) ? text.Substring(num, num2 - num) : text.Substring(num)).Trim();
			return !string.IsNullOrWhiteSpace(block);
		}
		catch
		{
			block = "";
			return false;
		}
	}

	private static string ResolveTargetKingdomIdForRules(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		try
		{
			string text = (kingdomIdOverride ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.ToLowerInvariant();
			}
			string text2 = (targetHero?.Clan?.Kingdom?.StringId ?? targetHero?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.ToLowerInvariant();
			}
			Hero heroObject = targetCharacter?.HeroObject;
			string text3 = (heroObject?.Clan?.Kingdom?.StringId ?? heroObject?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.ToLowerInvariant();
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private string BuildExtraRuleInstructionsWithSticky(string input, Hero targetHero, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null)
	{
		string text = "";
		string targetKingdomId = ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		AIConfigHandler.SetGuardrailRuntimeTargetKingdom(targetKingdomId);
		string text2 = targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "";
		AIConfigHandler.SetGuardrailRuntimeTargetHero(text2);
		try
		{
			text = AIConfigHandler.BuildMatchedExtraRuleInstructions(input, 4, hasAnyHero);
		}
		finally
		{
			AIConfigHandler.SetGuardrailRuntimeTargetKingdom("");
			AIConfigHandler.SetGuardrailRuntimeTargetHero("");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			ResetKingdomServiceRuleSession();
			ResetMarriageRuleSession();
			return text;
		}
		bool flag = TryExtractExtraRuleBlock(text, "kingdom_service", out var block);
		if (flag)
		{
			_isKingdomServiceRuleSessionActive = true;
			_kingdomServiceRuleHeroId = text2;
			_kingdomServiceRuleNoHitRounds = 0;
			_kingdomServiceRuleCachedBlock = block;
		}
		bool flag2 = TryExtractExtraRuleBlock(text, "marriage", out var block2);
		if (flag2)
		{
			_isMarriageRuleSessionActive = true;
			_marriageRuleHeroId = text2;
			_marriageRuleNoHitRounds = 0;
			_marriageRuleCachedBlock = block2;
		}
		if (flag || flag2)
		{
			return text;
		}
		if (_isKingdomServiceRuleSessionActive && _kingdomServiceRuleHeroId == text2)
		{
			_kingdomServiceRuleNoHitRounds++;
			if (_kingdomServiceRuleNoHitRounds >= 2)
			{
				Logger.Log("Logic", $"[RuleSticky] kingdom_service 连续 {_kingdomServiceRuleNoHitRounds} 轮未命中，结束延续会话。");
				ResetKingdomServiceRuleSession();
				return text;
			}
			if (!string.IsNullOrWhiteSpace(_kingdomServiceRuleCachedBlock))
			{
				if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("【附加规则:kingdom_service】", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return text;
				}
				string text3 = "【会话延续】你与玩家仍在讨论势力效力，本轮继续按该规则判断。";
				string text4 = _kingdomServiceRuleCachedBlock + Environment.NewLine + text3;
				return string.IsNullOrWhiteSpace(text) ? text4 : (text.TrimEnd() + Environment.NewLine + text4);
			}
		}
		else if (_isKingdomServiceRuleSessionActive && _kingdomServiceRuleHeroId != text2)
		{
			ResetKingdomServiceRuleSession();
		}
		if (_isMarriageRuleSessionActive && _marriageRuleHeroId == text2)
		{
			_marriageRuleNoHitRounds++;
			if (_marriageRuleNoHitRounds >= 2)
			{
				Logger.Log("Logic", $"[RuleSticky] marriage 连续 {_marriageRuleNoHitRounds} 轮未命中，结束延续会话。");
				ResetMarriageRuleSession();
				return text;
			}
			if (!string.IsNullOrWhiteSpace(_marriageRuleCachedBlock))
			{
				if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("【附加规则:marriage】", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return text;
				}
				string text5 = "【会话延续】你与玩家仍在讨论婚姻事宜，本轮继续按该规则判断。";
				string text6 = _marriageRuleCachedBlock + Environment.NewLine + text5;
				return string.IsNullOrWhiteSpace(text) ? text6 : (text.TrimEnd() + Environment.NewLine + text6);
			}
		}
		else if (_isMarriageRuleSessionActive && _marriageRuleHeroId != text2)
		{
			ResetMarriageRuleSession();
		}
		return text;
	}

	private static void AppendRuleBlock(StringBuilder sb, string ruleId, string body)
	{
		if (sb != null)
		{
			string text = (ruleId ?? "").Trim();
			string value = (body ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
			{
				sb.AppendLine("【附加规则:" + text + "】");
				sb.AppendLine(value);
			}
		}
	}

	private string BuildTriggeredRuleInstructions(string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (useDuelContext)
			{
				if (!hasAnyHero)
				{
					string duelNonHeroInstruction = AIConfigHandler.DuelNonHeroInstruction;
					if (!string.IsNullOrWhiteSpace(duelNonHeroInstruction))
					{
						AppendRuleBlock(stringBuilder, "duel", duelNonHeroInstruction);
					}
				}
				else
				{
					string value = AIConfigHandler.DuelInstruction;
					if (string.IsNullOrWhiteSpace(value))
					{
						value = "受到侮辱可发起决斗。";
					}
					if (isQualified)
					{
						StringBuilder stringBuilder2 = new StringBuilder();
						stringBuilder2.AppendLine(value);
						stringBuilder2.AppendLine("如果你决定同意决斗并使用 [ACTION:DUEL]，请在同一条回复末尾额外输出两条隐藏标签（不会展示给玩家，用于战后喊话）：");
						stringBuilder2.AppendLine("[ACTION:DUEL_LINE_WIN:你获胜后要喊的一句话]");
						stringBuilder2.AppendLine("[ACTION:DUEL_LINE_LOSE:你战败后要喊的一句话]");
						stringBuilder2.Append("两句话都必须是纯口语一句话；不要写解释；不要包含括号/星号；不要出现 ] 字符。");
						AppendRuleBlock(stringBuilder, "duel", stringBuilder2.ToString());
					}
					else
					{
						AppendRuleBlock(stringBuilder, "duel", $"玩家触发了决斗相关话题，但等级({playerTier})过低。请拒绝决斗并羞辱其不自量力。严禁使用决斗标签。");
					}
				}
			}
			if (AIConfigHandler.RewardEnabled && useRewardContext)
			{
				string text = (hasAnyHero ? AIConfigHandler.RewardInstruction : AIConfigHandler.RewardNonHeroInstruction);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = AIConfigHandler.RewardInstruction;
				}
				AppendRuleBlock(stringBuilder, "reward", text);
				if (AIConfigHandler.DuelStakeEnabled && _isRewardSessionActive && _rewardSessionFromDuel && _rewardSessionHeroId == (targetHero?.StringId ?? ""))
				{
					string body = (_rewardSessionPlayerWonLastDuel ? AIConfigHandler.DuelStakePlayerWinInstruction : AIConfigHandler.DuelStakeNpcWinInstruction);
					AppendRuleBlock(stringBuilder, "duel_stake", body);
				}
			}
			if (AIConfigHandler.LoanEnabled && isLoanContext)
			{
				string text2 = (hasAnyHero ? AIConfigHandler.LoanInstruction : AIConfigHandler.LoanNonHeroInstruction);
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = AIConfigHandler.LoanInstruction;
				}
				AppendRuleBlock(stringBuilder, "loan", text2);
			}
			if (AIConfigHandler.SurroundingsEnabled && isSurroundingsContext)
			{
				AppendRuleBlock(stringBuilder, "surroundings", AIConfigHandler.SurroundingsInstruction);
			}
			string text3 = BuildExtraRuleInstructionsWithSticky(input, targetHero, hasAnyHero, targetCharacter, kingdomIdOverride);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine(text3.Trim());
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	public static List<Hero> GetDevEditableHeroListForExternal()
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new List<Hero>();
			}
			List<Hero> list = myBehavior.BuildDevEditableHeroList();
			return list ?? new List<Hero>();
		}
		catch
		{
			return new List<Hero>();
		}
	}

	public static void GetNpcPersonaForExternal(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior != null && hero != null)
			{
				myBehavior.GetNpcPersonaStrings(hero, out personality, out background);
			}
		}
		catch
		{
		}
	}

	public static async Task EnsureNpcPersonaGeneratedForExternalAsync(Hero hero)
	{
		try
		{
			MyBehavior inst = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (inst != null && hero != null)
			{
				await inst.EnsureNpcPersonaGeneratedAsync(hero);
			}
		}
		catch
		{
		}
	}

	public static string BuildCurrentDateFactForExternal()
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildCurrentDateFactForPrompt() ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static ShoutPromptContext BuildShoutPromptContextForExternal(Hero targetHero, string input, string extraFact, string cultureIdOverride = null, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new ShoutPromptContext
				{
					Extras = "",
					UseDuelContext = false,
					UseRewardContext = false,
					IsLoanContext = false,
					IsQualified = true
				};
			}
			return myBehavior.BuildShoutPromptContextForExternalInternal(targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride);
		}
		catch
		{
			return new ShoutPromptContext
			{
				Extras = "",
				UseDuelContext = false,
				UseRewardContext = false,
				IsLoanContext = false,
				IsQualified = true
			};
		}
	}

	private ShoutPromptContext BuildShoutPromptContextForExternalInternal(Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null)
	{
		ShoutPromptContext shoutPromptContext = new ShoutPromptContext
		{
			Extras = "",
			UseDuelContext = false,
			UseRewardContext = false,
			IsLoanContext = false,
			IsQualified = true
		};
		if (string.IsNullOrWhiteSpace(input))
		{
			return shoutPromptContext;
		}
		AIConfigHandler.SetGuardrailSemanticContext(BuildGuardrailSemanticContext(targetHero, extraFact));
		string text = ((!string.IsNullOrEmpty(cultureIdOverride)) ? cultureIdOverride : (targetHero?.Culture?.StringId ?? "neutral"));
		int num = _cachedPlayerClanTier;
		if (num <= 0)
		{
			try
			{
				num = Clan.PlayerClan?.Tier ?? 0;
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					num = (Hero.MainHero?.Clan?.Tier).GetValueOrDefault();
				}
				catch
				{
				}
			}
		}
		int num2 = DuelSettings.GetSettings()?.MinimumClanTier ?? 0;
		bool isQualified = num >= num2;
		List<string> duelTriggerKeywords = AIConfigHandler.DuelTriggerKeywords;
		bool flag = false;
		string matchedKeyword = "";
		float score = 0f;
		flag = AIConfigHandler.IsGuardrailSemanticHit(input, "duel", AIConfigHandler.DuelInstruction, duelTriggerKeywords, out matchedKeyword, out score);
		bool flag2 = targetHero != null && flag;
		if (targetHero != null)
		{
			string stringId = targetHero.StringId;
			if (flag)
			{
				_isDuelSessionActive = true;
				_duelSessionHeroId = stringId;
				_duelSessionNoKeywordRounds = 0;
			}
			else if (_isDuelSessionActive && _duelSessionHeroId == stringId)
			{
				_duelSessionNoKeywordRounds++;
				if (_duelSessionNoKeywordRounds >= 2)
				{
					_isDuelSessionActive = false;
					_duelSessionHeroId = null;
					_duelSessionNoKeywordRounds = 0;
					flag2 = false;
				}
				else
				{
					flag2 = true;
				}
			}
			else if (_isDuelSessionActive && _duelSessionHeroId != stringId)
			{
				_isDuelSessionActive = false;
				_duelSessionHeroId = null;
				_duelSessionNoKeywordRounds = 0;
			}
		}
		List<string> rewardTriggerKeywords = AIConfigHandler.RewardTriggerKeywords;
		bool flag3 = false;
		string matchedKeyword2 = "";
		float score2 = 0f;
		if (AIConfigHandler.RewardEnabled)
		{
			flag3 = AIConfigHandler.IsGuardrailSemanticHit(input, "reward", AIConfigHandler.RewardInstruction, rewardTriggerKeywords, out matchedKeyword2, out score2);
		}
		List<string> loanTriggerKeywords = AIConfigHandler.LoanTriggerKeywords;
		bool flag4 = false;
		string matchedKeyword3 = "";
		float score3 = 0f;
		if (AIConfigHandler.LoanEnabled)
		{
			flag4 = AIConfigHandler.IsGuardrailSemanticHit(input, "loan", AIConfigHandler.LoanInstruction, loanTriggerKeywords, out matchedKeyword3, out score3);
		}
		List<string> surroundingsTriggerKeywords = AIConfigHandler.SurroundingsTriggerKeywords;
		bool flag5 = false;
		string matchedKeyword4 = "";
		float score4 = 0f;
		if (AIConfigHandler.SurroundingsEnabled)
		{
			flag5 = AIConfigHandler.IsGuardrailSemanticHit(input, "surroundings", AIConfigHandler.SurroundingsInstruction, surroundingsTriggerKeywords, out matchedKeyword4, out score4);
		}
		string guardrailRuleInstruction = AIConfigHandler.GetGuardrailRuleInstruction("kingdom_service");
		List<string> guardrailRuleKeywords = AIConfigHandler.GetGuardrailRuleKeywords("kingdom_service");
		string matchedKeyword5;
		float score5;
		bool flag6 = AIConfigHandler.IsGuardrailSemanticHit(input, "kingdom_service", guardrailRuleInstruction, guardrailRuleKeywords, out matchedKeyword5, out score5);
		string guardrailMarriageInstruction = AIConfigHandler.GetGuardrailRuleInstruction("marriage");
		List<string> guardrailMarriageKeywords = AIConfigHandler.GetGuardrailRuleKeywords("marriage");
		string matchedKeyword6;
		float score6;
		bool marriageHit = AIConfigHandler.IsGuardrailSemanticHit(input, "marriage", guardrailMarriageInstruction, guardrailMarriageKeywords, out matchedKeyword6, out score6);
		bool flag7 = flag3;
		bool flag8 = flag4;
		string value = "";
		if (!flag2 && !flag7 && !flag8 && !flag5)
		{
			value = AIConfigHandler.BuildGuardrailClarificationHint(input, flag, score, flag3, score2, flag4, score3, flag5, score4);
		}
		string text2 = (string.IsNullOrWhiteSpace(matchedKeyword) ? "" : $"{matchedKeyword}@{score:0.00}");
		string text3 = (string.IsNullOrWhiteSpace(matchedKeyword2) ? "" : $"{matchedKeyword2}@{score2:0.00}");
		string text4 = (string.IsNullOrWhiteSpace(matchedKeyword3) ? "" : $"{matchedKeyword3}@{score3:0.00}");
		string text5 = (string.IsNullOrWhiteSpace(matchedKeyword4) ? "" : $"{matchedKeyword4}@{score4:0.00}");
		string text6 = (string.IsNullOrWhiteSpace(matchedKeyword5) ? "" : $"{matchedKeyword5}@{score5:0.00}");
		string text8 = (string.IsNullOrWhiteSpace(matchedKeyword6) ? "" : $"{matchedKeyword6}@{score6:0.00}");
		string text7 = targetHero?.Name?.ToString() ?? "某人";
		Logger.Log("Logic", $"[SemanticTrigger-Shout] DuelHit={flag} [{text2}] StickyDuel={_isDuelSessionActive} RewardHit={flag3} [{text3}] LoanHit={flag4} [{text4}] StickyReward={_isRewardSessionActive} SurroundingsHit={flag5} [{text5}] KingdomServiceHit={flag6} [{text6}] StickyKingdomService={_isKingdomServiceRuleSessionActive} MarriageHit={marriageHit} [{text8}] StickyMarriage={_isMarriageRuleSessionActive} Input='{input}' NPC='{text7}'");
		if (AIConfigHandler.RewardEnabled && targetHero != null)
		{
			string stringId2 = targetHero.StringId;
			if (flag3)
			{
				_isRewardSessionActive = true;
				_rewardSessionHeroId = stringId2;
				_rewardSessionNoKeywordRounds = 0;
				_rewardSessionFromDuel = false;
				_rewardSessionPlayerWonLastDuel = false;
			}
			else if (_isRewardSessionActive && _rewardSessionHeroId == stringId2)
			{
				_rewardSessionNoKeywordRounds++;
				if (_rewardSessionNoKeywordRounds >= 2)
				{
					_isRewardSessionActive = false;
					_rewardSessionHeroId = null;
					_rewardSessionNoKeywordRounds = 0;
					_rewardSessionFromDuel = false;
					_rewardSessionPlayerWonLastDuel = false;
				}
				else
				{
					flag7 = true;
				}
			}
			else if (_isRewardSessionActive && _rewardSessionHeroId != stringId2)
			{
				_isRewardSessionActive = false;
				_rewardSessionHeroId = null;
				_rewardSessionNoKeywordRounds = 0;
				_rewardSessionFromDuel = false;
				_rewardSessionPlayerWonLastDuel = false;
			}
		}
		if (AIConfigHandler.LoanEnabled && targetHero != null)
		{
			string stringId3 = targetHero.StringId;
			if (flag4)
			{
				_isLoanSessionActive = true;
				_loanSessionHeroId = stringId3;
				_loanSessionNoKeywordRounds = 0;
			}
			else if (_isLoanSessionActive && _loanSessionHeroId == stringId3)
			{
				_loanSessionNoKeywordRounds++;
				if (_loanSessionNoKeywordRounds >= 2)
				{
					_isLoanSessionActive = false;
					_loanSessionHeroId = null;
					_loanSessionNoKeywordRounds = 0;
				}
				else
				{
					flag8 = true;
				}
			}
			else if (_isLoanSessionActive && _loanSessionHeroId != stringId3)
			{
				_isLoanSessionActive = false;
				_loanSessionHeroId = null;
				_loanSessionNoKeywordRounds = 0;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		string value2 = BuildCurrentDateFactForPrompt();
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.AppendLine(value2);
		}
		if (targetHero != null)
		{
			string value3 = BuildNpcPersonaContext(targetHero);
			if (!string.IsNullOrEmpty(value3))
			{
				stringBuilder.AppendLine(value3);
			}
		}
		string loreContext = "";
		string loreCtxSource = "none";
		if (targetHero != null)
		{
			loreContext = AIConfigHandler.GetLoreContext(input, targetHero);
			loreCtxSource = "hero";
		}
		else if (targetCharacter != null)
		{
			loreContext = AIConfigHandler.GetLoreContext(input, targetCharacter, kingdomIdOverride);
			loreCtxSource = "character";
		}
		if (!string.IsNullOrEmpty(loreContext))
		{
			stringBuilder.AppendLine(loreContext);
		}
		try
		{
			Logger.Log("LoreMatch", $"shout_prompt_lore_ctx source={loreCtxSource} heroId={(targetHero?.StringId ?? "null")} charId={(targetCharacter?.StringId ?? "null")} kingdomIdOverride={(kingdomIdOverride ?? "")}");
		}
		catch
		{
		}
		PersistLoreToDialogueHistory(targetHero, loreContext);
		if (RewardSystemBehavior.Instance != null && targetHero != null)
		{
			if (flag8)
			{
				string value4 = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
				if (!string.IsNullOrEmpty(value4))
				{
					stringBuilder.AppendLine(value4);
				}
				string value5 = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
				if (!string.IsNullOrEmpty(value5))
				{
					stringBuilder.AppendLine(value5);
				}
			}
			string value6 = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
			if (!string.IsNullOrEmpty(value6))
			{
				stringBuilder.AppendLine(value6);
			}
		}
		if (targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out var playerWon))
		{
			if (playerWon)
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中输给了玩家。请在态度和言语中体现这一点，并认真考虑履行你在决斗前约定的赌注或补偿。");
				if (AIConfigHandler.RewardEnabled)
				{
					string stringId4 = targetHero.StringId;
					_isRewardSessionActive = true;
					_rewardSessionHeroId = stringId4;
					_rewardSessionNoKeywordRounds = 0;
					_rewardSessionFromDuel = true;
					_rewardSessionPlayerWonLastDuel = true;
					flag7 = true;
				}
			}
			else
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中打败了玩家。你可以据此调整对玩家的态度，或提醒玩家履行之前约定的赌注。");
				if (AIConfigHandler.RewardEnabled)
				{
					string stringId5 = targetHero.StringId;
					_isRewardSessionActive = true;
					_rewardSessionHeroId = stringId5;
					_rewardSessionNoKeywordRounds = 0;
					_rewardSessionFromDuel = true;
					_rewardSessionPlayerWonLastDuel = false;
					flag7 = true;
				}
			}
		}
		if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
		{
			if (_recentlyDefeatedByPlayer.Contains(targetHero.StringId))
			{
				stringBuilder.AppendLine("【原版战斗结果】你刚刚在一场战斗中被玩家击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
			}
			if (_recentlyReleasedPrisoners.Contains(targetHero.StringId))
			{
				stringBuilder.AppendLine("【释放通知】你之前被玩家俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
			}
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(value);
		}
		if (flag5)
		{
			bool flag9 = false;
			CampaignVec2 pos;
			try
			{
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
				{
					flag9 = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
				}
				else
				{
					pos = MobileParty.MainParty.Position;
					flag9 = pos.IsValid();
				}
			}
			catch
			{
				flag9 = false;
				pos = default(CampaignVec2);
			}
			if (flag9)
			{
				string value7 = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos);
				if (!string.IsNullOrWhiteSpace(value7))
				{
					stringBuilder.AppendLine(value7);
				}
			}
		}
		string value8 = BuildTriggeredRuleInstructions(input, targetHero, flag2, isQualified, num, flag7, flag8, flag5, hasAnyHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(value8))
		{
			stringBuilder.AppendLine(value8);
		}
		bool includeTradePricing = flag7 || flag8 || flag2;
		bool includeMarriageCandidates = targetHero != null && (marriageHit || (_isMarriageRuleSessionActive && _marriageRuleHeroId == targetHero.StringId));
		bool flag10 = num >= 2;
		bool includeRuleGatedFields = flag10;
		string value9 = BuildPlayerIdentityInfoForPrompt(Hero.MainHero, includeRuleGatedFields, includeTradePricing, includeMarriageCandidates, targetHero);
		if (!string.IsNullOrWhiteSpace(value9))
		{
			stringBuilder.AppendLine(value9);
		}
		string value10 = BuildNpcIdentityInfoForPrompt(targetHero, includeTradePricing, includeMarriageCandidates);
		if (!string.IsNullOrWhiteSpace(value10))
		{
			stringBuilder.AppendLine(value10);
		}
		shoutPromptContext.Extras = stringBuilder.ToString();
		shoutPromptContext.UseDuelContext = flag2;
		shoutPromptContext.UseRewardContext = flag7;
		shoutPromptContext.IsLoanContext = flag8;
		shoutPromptContext.IsQualified = isQualified;
		return shoutPromptContext;
	}

	public static void AppendExternalLoreHistory(Hero hero, string loreText)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendLoreToHistory(hero, loreText);
		}
		catch
		{
		}
	}

	private void PersistLoreToDialogueHistory(Hero hero, string loreContext)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			string text = (loreContext ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			AppendLoreToHistory(hero, text);
			try
			{
				string text2 = (hero?.StringId ?? "").Trim();
				string text3 = text.Replace("\r", "").Replace("\n", "\\n");
				if (text3.Length > 200)
				{
					text3 = text3.Substring(0, 200);
				}
				Logger.Log("LoreHistory", "append hero=" + text2 + " preview=" + text3);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void AppendLoreToHistory(Hero hero, string loreText)
	{
		if (hero == null)
		{
			return;
		}
		loreText = (loreText ?? "").Trim();
		if (string.IsNullOrEmpty(loreText))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d != null && d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (dialogueDay.Lines == null)
			{
				dialogueDay.Lines = new List<string>();
			}
			string text = loreText.Replace("\r", "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			bool flag = text.IndexOf("【触发相关话题/背景】", StringComparison.OrdinalIgnoreCase) >= 0;
			bool flag2 = text.IndexOf("【玩家触发了（", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("【以下是关于（", StringComparison.OrdinalIgnoreCase) >= 0;
			if (!flag && !flag2)
			{
				string text2 = "";
				try
				{
					text2 = (hero?.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "该NPC";
				}
				text = "【以下是关于（相关）的背景知识，" + text2 + "可酌情参考，但不要假设玩家提起过此话题】\n" + text;
			}
			string text3 = text;
			if (dialogueDay.Lines.Count > 0)
			{
				string a = dialogueDay.Lines[dialogueDay.Lines.Count - 1] ?? "";
				if (string.Equals(a, text3, StringComparison.Ordinal))
				{
					return;
				}
			}
			dialogueDay.Lines.Add(text3);
			int num = 0;
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
			while (num > 260)
			{
				DialogueDay dialogueDay2 = list.FirstOrDefault((DialogueDay d) => d != null && d.Lines != null && d.Lines.Count > 0);
				if (dialogueDay2 == null)
				{
					break;
				}
				dialogueDay2.Lines.RemoveAt(0);
				num--;
				if (dialogueDay2.Lines.Count == 0 && dialogueDay2.GameDayIndex != dayIndex)
				{
					list.Remove(dialogueDay2);
				}
			}
			SaveDialogueHistory(hero, list);
		}
		catch
		{
		}
	}

	private static bool IsPlayerTurnStartLine(string line)
	{
		string text = (line ?? "").TrimStart();
		return text.StartsWith("玩家:", StringComparison.Ordinal);
	}

	private static bool IsSystemFactLine(string line)
	{
		string text = (line ?? "").TrimStart();
		return text.StartsWith("[玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[NPC行为补充]", StringComparison.Ordinal);
	}

	private static bool IsLoreInjectionHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("【玩家触发了（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【触发相关话题/背景】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.IndexOf("玩家说的话让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		if (text.IndexOf("应当知晓以下信息", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private static bool ContainsStructuredSignal(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("债务id") || text2.Contains("欠款") || text2.Contains("还款") || text2.Contains("赊账") || text2.Contains("借款") || text2.Contains("第纳尔") || text2.Contains("价格") || text2.Contains("交易") || text2.Contains("决斗") || text2.Contains("赌注") || text2.Contains("[action:") || text2.Contains("guideprice=") || text2.Contains("|") || text2.Contains("id:"))
		{
			return true;
		}
		foreach (char c in text)
		{
			if (c >= '0' && c <= '9')
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRecallIntentQuery(string query)
	{
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		return text2.Contains("以前") || text2.Contains("之前") || text2.Contains("上次") || text2.Contains("上回") || text2.Contains("那次") || text2.Contains("当时") || text2.Contains("还记得") || text2.Contains("你记得") || text2.Contains("你忘了") || text2.Contains("曾经") || text2.Contains("过去") || text2.Contains("前些天") || text2.Contains("前阵子") || text2.Contains("old") || text2.Contains("before") || text2.Contains("last time") || text2.Contains("remember");
	}

	private static bool ContainsLiteralKeywordHit(string input, List<string> keywords, out string matchedKeyword)
	{
		matchedKeyword = "";
		string text = (input ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text) || keywords == null || keywords.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < keywords.Count; i++)
		{
			string text2 = (keywords[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string value = text2.ToLowerInvariant();
				if (text.Contains(value))
				{
					matchedKeyword = text2;
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsTermChar(char c)
	{
		return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= '\u4E00' && c <= '\u9FFF');
	}

	private static List<string> ExtractQueryTerms(string query)
	{
		List<string> terms = new List<string>();
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return terms;
		}
		StringBuilder cur = new StringBuilder();
		Action action = delegate
		{
			if (cur.Length < 2)
			{
				cur.Clear();
			}
			else
			{
				string item = cur.ToString();
				if (!terms.Contains(item))
				{
					terms.Add(item);
				}
				cur.Clear();
			}
		};
		foreach (char c in text)
		{
			if (IsTermChar(c))
			{
				cur.Append(c);
			}
			else
			{
				action();
			}
		}
		action();
		if (terms.Count > 24)
		{
			terms = terms.Take(24).ToList();
		}
		return terms;
	}

	private static double ComputeLexicalOverlapScore(string text, List<string> terms)
	{
		if (string.IsNullOrWhiteSpace(text) || terms == null || terms.Count <= 0)
		{
			return 0.0;
		}
		int num = 0;
		for (int i = 0; i < terms.Count; i++)
		{
			string value = terms[i];
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num++;
			}
		}
		if (num <= 0)
		{
			return 0.0;
		}
		double num2 = (double)num / (double)Math.Max(1, terms.Count);
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > 1.0)
		{
			num2 = 1.0;
		}
		return num2;
	}

	private static string StripSpeakerPrefixForRecall(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.StartsWith("- ", StringComparison.Ordinal))
		{
			text = text.Substring(2).TrimStart();
		}
		if (text.StartsWith("—— ", StringComparison.Ordinal))
		{
			return "";
		}
		if (text.StartsWith("[玩家行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[玩家行为补充]".Length).Trim();
		}
		if (text.StartsWith("[NPC行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[NPC行为补充]".Length).Trim();
		}
		int num = text.IndexOf(':');
		if (num > 0 && num < 20)
		{
			return text.Substring(num + 1).Trim();
		}
		return text;
	}

	private static string TrimRecallSnippet(string text, int maxChars = 80)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (text2.Length <= maxChars)
		{
			return text2;
		}
		return text2.Substring(0, maxChars).TrimEnd('，', '。', '；', '、', ',', ';', '.') + "…";
	}

	private static List<string> BuildRecallToneLines(List<string> rawArchiveLines, int maxItems = 12)
	{
		List<string> list = new List<string>();
		if (rawArchiveLines == null || rawArchiveLines.Count == 0 || maxItems <= 0)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string[] array = new string[3] { "你隐约记得那次闲聊提到：", "你回想起之前似乎说过：", "你依稀记得当时聊到过：" };
		string[] array2 = new string[3] { "你记得玩家当时提到过：", "你回想起玩家曾说过：", "你依稀记得玩家提过：" };
		string[] array3 = new string[3] { "你记得当时谈到过一件具体事项：", "你回想起之前提过一件具体事项：", "你依稀记得还讨论过一件具体事项：" };
		string text = "";
		int num = 0;
		for (int i = 0; i < rawArchiveLines.Count; i++)
		{
			string text2 = (rawArchiveLines[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("—— ", StringComparison.Ordinal) && text2.EndsWith(" ——", StringComparison.Ordinal))
			{
				text = text2;
				continue;
			}
			if (num >= maxItems)
			{
				break;
			}
			string text3 = StripSpeakerPrefixForRecall(text2);
			text3 = TrimRecallSnippet(text3);
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				string text4 = (IsSystemFactLine(text2) ? ("你清楚记得一条已发生的事实：" + text3) : (IsPlayerTurnStartLine(text2) ? (array2[num % array2.Length] + text3) : ((!ContainsStructuredSignal(text2)) ? (array[num % array.Length] + text3) : (array3[num % array3.Length] + text3))));
				string text5 = (string.IsNullOrWhiteSpace(text) ? "—— 日期未知 ——" : text);
				if (list.Count <= 0 || !string.Equals(list[list.Count - 1], text5, StringComparison.Ordinal))
				{
					list.Add(text5);
				}
				list.Add("- " + text4);
				num++;
			}
		}
		return list;
	}

	private static List<string> BuildRenderedHistoryLines(List<HistoryLineEntry> entries)
	{
		List<string> list = new List<string>();
		if (entries == null || entries.Count == 0)
		{
			return list;
		}
		int num = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			HistoryLineEntry historyLineEntry = entries[i];
			if (historyLineEntry != null && !string.IsNullOrWhiteSpace(historyLineEntry.Line))
			{
				if (historyLineEntry.Day != 0 && historyLineEntry.Day != num)
				{
					num = historyLineEntry.Day;
					string text = ((!string.IsNullOrWhiteSpace(historyLineEntry.Date)) ? historyLineEntry.Date : $"第 {historyLineEntry.Day} 日");
					list.Add("—— " + text + " ——");
				}
				list.Add(historyLineEntry.Line);
			}
		}
		return list;
	}

	private static List<string> TakeTailByCharBudget(List<string> lines, int budget)
	{
		List<string> list = new List<string>();
		if (lines == null || lines.Count == 0 || budget <= 0)
		{
			return list;
		}
		int num = 0;
		for (int num2 = lines.Count - 1; num2 >= 0; num2--)
		{
			string text = lines[num2] ?? "";
			if (text.Length > 0)
			{
				int num3 = text.Length + 1;
				if (num + num3 > budget && list.Count > 0)
				{
					break;
				}
				list.Add(text);
				num += num3;
			}
		}
		list.Reverse();
		return list;
	}

	private static string BuildHistoryQueryText(List<HistoryLineEntry> recent)
	{
		if (recent == null || recent.Count == 0)
		{
			return "";
		}
		int num = Math.Min(8, recent.Count);
		IEnumerable<string> values = from x in recent.Skip(recent.Count - num)
			select x?.Line ?? "" into x
			where !string.IsNullOrWhiteSpace(x)
			select x;
		return string.Join("\n", values);
	}

	private static bool IsCurrentInputPlayerLine(string line, string currentInput)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(currentInput))
		{
			return false;
		}
		string text = line.Trim();
		if (text.StartsWith("玩家:", StringComparison.Ordinal))
		{
			text = text.Substring("玩家:".Length).Trim();
		}
		else
		{
			if (!text.StartsWith("玩家：", StringComparison.Ordinal))
			{
				return false;
			}
			text = text.Substring("玩家：".Length).Trim();
		}
		string b = currentInput.Trim();
		return string.Equals(text, b, StringComparison.Ordinal);
	}

	private List<ArchiveHit> FindRelevantArchiveHits(List<HistoryLineEntry> older, string query, int topK, out bool onnxUsed)
	{
		onnxUsed = false;
		List<ArchiveHit> list = new List<ArchiveHit>();
		if (older == null || older.Count == 0 || topK <= 0)
		{
			return list;
		}
		List<string> terms = ExtractQueryTerms(query);
		List<HistoryLineEntry> list2 = older.Where((HistoryLineEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Line)).ToList();
		if (list2.Count <= 0)
		{
			return list;
		}
		int num = 260;
		if (list2.Count > num)
		{
			list2 = list2.Skip(list2.Count - num).ToList();
		}
		int count = list2.Count;
		List<(HistoryLineEntry, double)> list3 = new List<(HistoryLineEntry, double)>();
		for (int num2 = 0; num2 < count; num2++)
		{
			HistoryLineEntry historyLineEntry = list2[num2];
			string text = historyLineEntry.Line ?? "";
			double num3 = ((count <= 1) ? 1.0 : ((double)num2 / (double)(count - 1)));
			double num4 = (IsSystemFactLine(text) ? 1.0 : (ContainsStructuredSignal(text) ? 0.78 : 0.35));
			double num5 = ComputeLexicalOverlapScore(text, terms);
			double item = 0.6 * num4 + 0.3 * num3 + 0.1 * num5;
			list3.Add((historyLineEntry, item));
		}
		float[] vector = null;
		bool flag = !string.IsNullOrWhiteSpace(query) && query.Trim().Length >= 2;
		bool flag2 = false;
		OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
		if (flag && instance != null && instance.IsAvailable)
		{
			flag2 = instance.TryGetEmbedding(query, out vector) && vector != null && vector.Length != 0;
		}
		onnxUsed = flag2;
		int num6 = 120;
		if (!flag2)
		{
			list3 = list3.OrderByDescending<(HistoryLineEntry, double), double>(((HistoryLineEntry Entry, double BaseScore) x) => x.BaseScore).Take(num6).ToList();
		}
		else if (list3.Count > num6)
		{
			int num7 = Math.Max(1, num6 / 2);
			HashSet<int> topByBase = (from x in list3.OrderByDescending<(HistoryLineEntry, double), double>(((HistoryLineEntry Entry, double BaseScore) x) => x.BaseScore).Take(num7)
				select x.Item1.Index).ToHashSet();
			HashSet<int> latest = (from x in list3.OrderByDescending<(HistoryLineEntry, double), int>(((HistoryLineEntry Entry, double BaseScore) x) => x.Item1.Index).Take(num6 - num7)
				select x.Item1.Index).ToHashSet();
			list3 = list3.Where<(HistoryLineEntry, double)>(((HistoryLineEntry Entry, double BaseScore) x) => topByBase.Contains(x.Item1.Index) || latest.Contains(x.Item1.Index)).ToList();
		}
		if (list3.Count <= 0)
		{
			return list;
		}
		foreach (var item2 in list3)
		{
			double num8 = item2.Item2;
			if (flag2)
			{
				string text2 = (item2.Item1.Line ?? "").Trim();
				if (text2.Length > 200)
				{
					text2 = text2.Substring(0, 200);
				}
				if (!string.IsNullOrWhiteSpace(text2) && instance.TryGetEmbedding(text2, out var vector2) && vector2 != null && vector2.Length != 0)
				{
					int num9 = Math.Min(vector.Length, vector2.Length);
					double num10 = 0.0;
					for (int num11 = 0; num11 < num9; num11++)
					{
						num10 += (double)vector[num11] * (double)vector2[num11];
					}
					double num12 = (num10 + 1.0) * 0.5;
					if (num12 < 0.0)
					{
						num12 = 0.0;
					}
					if (num12 > 1.0)
					{
						num12 = 1.0;
					}
					num8 = num12 * 0.9 + num8 * 0.1;
				}
			}
			list.Add(new ArchiveHit
			{
				Entry = item2.Item1,
				Score = num8
			});
		}
		list = (from x in list
			orderby x.Score descending, (x.Entry != null) ? x.Entry.Index : (-1) descending
			select x).ToList();
		List<ArchiveHit> list4 = new List<ArchiveHit>();
		HashSet<int> hashSet = new HashSet<int>();
		for (int num13 = 0; num13 < list.Count; num13++)
		{
			ArchiveHit archiveHit = list[num13];
			int num14 = archiveHit?.Entry?.Index ?? int.MinValue;
			if (num14 != int.MinValue && !hashSet.Contains(num14))
			{
				list4.Add(archiveHit);
				hashSet.Add(num14 - 1);
				hashSet.Add(num14);
				hashSet.Add(num14 + 1);
				if (list4.Count >= topK)
				{
					break;
				}
			}
		}
		return list4;
	}

	private string BuildHistoryContext(Hero hero, int maxLines = 0, string currentInput = null)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			int num = ((maxLines <= 0) ? GetRecentDialogueTurnsFromSettings() : ClampRecentDialogueTurns(maxLines));
			List<DialogueDay> list = LoadDialogueHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			int num2 = (int)CampaignTime.Now.ToDays;
			List<HistoryLineEntry> list2 = new List<HistoryLineEntry>();
			int num3 = 0;
			foreach (DialogueDay item in list)
			{
				if (item == null || item.Lines == null || (item.GameDayIndex != 0 && item.GameDayIndex > num2))
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					if (!string.IsNullOrWhiteSpace(line) && !IsLoreInjectionHistoryLine(line))
					{
						list2.Add(new HistoryLineEntry
						{
							Day = item.GameDayIndex,
							Date = item.GameDate,
							Line = line.Trim(),
							Index = num3++
						});
					}
				}
			}
			if (list2.Count == 0)
			{
				return "";
			}
			if (!string.IsNullOrWhiteSpace(currentInput))
			{
				int num4 = list2.Count - 1;
				if (num4 >= 0 && IsCurrentInputPlayerLine(list2[num4].Line, currentInput))
				{
					list2.RemoveAt(num4);
				}
			}
			if (list2.Count == 0)
			{
				return "";
			}
			int count = 0;
			int num5 = 0;
			bool flag = false;
			for (int num6 = list2.Count - 1; num6 >= 0; num6--)
			{
				if (IsPlayerTurnStartLine(list2[num6].Line))
				{
					num5++;
					if (num5 >= num)
					{
						count = num6;
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				count = Math.Max(0, list2.Count - num * 3);
			}
			List<HistoryLineEntry> list3 = list2.Skip(count).ToList();
			List<HistoryLineEntry> list4 = list2.Take(count).ToList();
			string text = (string.IsNullOrWhiteSpace(currentInput) ? BuildHistoryQueryText(list3) : currentInput.Trim());
			int historyArchiveTopNFromSettings = GetHistoryArchiveTopNFromSettings();
			bool onnxUsed = false;
			List<ArchiveHit> list5 = FindRelevantArchiveHits(list4, text, historyArchiveTopNFromSettings, out onnxUsed);
			List<string> list6 = BuildRenderedHistoryLines(list3);
			List<string> list7 = new List<string>();
			if (list5 != null && list5.Count > 0)
			{
				List<HistoryLineEntry> list8 = new List<HistoryLineEntry>();
				HashSet<int> hashSet = new HashSet<int>();
				foreach (ArchiveHit item2 in list5)
				{
					HistoryLineEntry historyLineEntry = item2?.Entry;
					if (historyLineEntry != null && hashSet.Add(historyLineEntry.Index))
					{
						list8.Add(historyLineEntry);
					}
				}
				list8 = (from e in list8
					orderby e.Day, e.Index
					select e).ToList();
				Dictionary<int, HistoryLineEntry> dictionary = new Dictionary<int, HistoryLineEntry>();
				for (int num7 = 0; num7 < list4.Count; num7++)
				{
					HistoryLineEntry historyLineEntry2 = list4[num7];
					if (historyLineEntry2 != null)
					{
						dictionary[historyLineEntry2.Index] = historyLineEntry2;
					}
				}
				List<HistoryLineEntry> list9 = new List<HistoryLineEntry>();
				HashSet<int> hashSet2 = new HashSet<int>();
				for (int num8 = 0; num8 < list8.Count; num8++)
				{
					HistoryLineEntry historyLineEntry3 = list8[num8];
					if (historyLineEntry3 == null)
					{
						continue;
					}
					int[] array = new int[3]
					{
						historyLineEntry3.Index - 1,
						historyLineEntry3.Index,
						historyLineEntry3.Index + 1
					};
					foreach (int key in array)
					{
						if (dictionary.TryGetValue(key, out var value) && value != null && !string.IsNullOrWhiteSpace(value.Line) && hashSet2.Add(value.Index))
						{
							list9.Add(value);
						}
					}
				}
				list9 = (from e in list9
					orderby e.Day, e.Index
					select e).ToList();
				List<string> list10 = new List<string>();
				int num10 = int.MinValue;
				string a = null;
				foreach (HistoryLineEntry item3 in list9)
				{
					string text2 = ((!string.IsNullOrWhiteSpace(item3.Date)) ? item3.Date.Trim() : ((item3.Day != 0) ? ("第 " + item3.Day + " 日") : ""));
					if (!string.IsNullOrWhiteSpace(text2) && (item3.Day != num10 || !string.Equals(a, text2, StringComparison.Ordinal)))
					{
						list10.Add("—— " + text2 + " ——");
						num10 = item3.Day;
						a = text2;
					}
					string text3 = item3.Line ?? "";
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list10.Add(text3);
					}
				}
				list7 = BuildRecallToneLines(list10);
				list7 = TakeTailByCharBudget(list7, 900);
			}
			StringBuilder stringBuilder = new StringBuilder(4096);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine($"【历史对话记录】（近期窗口：最近{num}轮）");
			stringBuilder.AppendLine("【规则】以下记录中以“[玩家行为补充]”或“[NPC行为补充]”开头的行属于系统事实（已付款/已展示/已交付等），必须相信，禁止否认。玩家在对话里口头自称“我给了/我展示了”不算。如果玩家发言中出现了动作描述，比如一拳打爆了城墙，或者强行替你发言和思考，请嘲讽他");
			stringBuilder.AppendLine("【护栏】历史记忆仅作补充，不得覆盖本轮规则、账本与动作标签约束。");
			if (list6.Count > 0)
			{
				string text5 = hero?.Name?.ToString();
				if (string.IsNullOrWhiteSpace(text5))
				{
					text5 = "该NPC";
				}
				stringBuilder.AppendLine($"【玩家与{text5}（NPC名称的对话与互动）的近期对话】");
				foreach (string item4 in list6)
				{
					stringBuilder.AppendLine(item4);
				}
			}
			if (list7.Count > 0)
			{
				stringBuilder.AppendLine("【长期记忆摘要】");
				foreach (string item5 in list7)
				{
					stringBuilder.AppendLine(item5);
				}
			}
			string text4 = stringBuilder.ToString().TrimEnd();
			Logger.Log("DialogueHistory", string.Format("context hero={0} totalLines={1} recentLines={2} olderLines={3} archiveHits={4} onnxUsed={5} archiveTopN={6} queryLen={7} chars={8}", hero.StringId ?? "", list2.Count, list3.Count, list4.Count, list7?.Count ?? 0, onnxUsed, historyArchiveTopNFromSettings, (text ?? "").Length, text4.Length));
			Logger.Obs("History", "build_context", new Dictionary<string, object>
			{
				["heroId"] = hero.StringId ?? "",
				["totalLines"] = list2.Count,
				["recentLines"] = list3.Count,
				["olderLines"] = list4.Count,
				["archiveHits"] = list7?.Count ?? 0,
				["onnxUsed"] = onnxUsed,
				["archiveTopN"] = historyArchiveTopNFromSettings,
				["queryLen"] = (text ?? "").Length,
				["chars"] = text4.Length
			});
			Logger.Metric("history.build_context");
			return text4;
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 构建上下文失败: " + ex.Message);
			Logger.Obs("History", "build_context_error", new Dictionary<string, object>
			{
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("history.build_context", ok: false);
			return "";
		}
	}

	private string GetCurrentLocationInfoSafe()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				string encounterMeetingLocationInfoOverride = LordEncounterBehavior.EncounterMeetingLocationInfoOverride;
				encounterMeetingLocationInfoOverride = (encounterMeetingLocationInfoOverride ?? "").Trim();
				if (!string.IsNullOrEmpty(encounterMeetingLocationInfoOverride))
				{
					return encounterMeetingLocationInfoOverride;
				}
			}
			string text = "";
			try
			{
				text = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			}
			catch
			{
				text = "";
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (text.StartsWith("位于 ", StringComparison.Ordinal) || text.StartsWith("靠近 ", StringComparison.Ordinal))
				{
					return "你" + text + "。";
				}
				return "你位于 " + text.TrimEnd('。', '.', ' ') + "。";
			}
			if (Settlement.CurrentSettlement != null)
			{
				return $"你位于 {Settlement.CurrentSettlement.Name}。";
			}
			if (MobileParty.MainParty != null)
			{
				return "你身处野外。";
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private void SimulateLeftClick()
	{
		mouse_event(2, 0, 0, 0, 0);
		Thread.Sleep(30);
		mouse_event(4, 0, 0, 0, 0);
	}

	private async Task<string> CallUniversalApi(string sys, string user)
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				return "错误：请检查 MCM 设置。";
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			var body = new
			{
				model = settings.ModelName,
				messages = new[]
				{
					new
					{
						role = "system",
						content = sys
					},
					new
					{
						role = "user",
						content = user
					}
				},
				max_tokens = 4096,
				stream = true,
				temperature = 0.8
			};
			string jsonBody = JsonConvert.SerializeObject(body);
			StringBuilder httpLog = new StringBuilder();
			httpLog.AppendLine("[HTTP] 请求发送到:");
			httpLog.AppendLine("  Url: " + effectiveApiUrl);
			if (!string.Equals(effectiveApiUrl, settings.ApiUrl, StringComparison.Ordinal))
			{
				httpLog.AppendLine("  Note: 已自动补全 /v1/chat/completions 尾缀");
			}
			httpLog.AppendLine("  Model: " + settings.ModelName);
			httpLog.AppendLine("  MaxTokens: 4096, Stream: true, Temperature: 0.8");
			httpLog.AppendLine("  SystemPrompt:");
			httpLog.AppendLine(sys);
			httpLog.AppendLine("  UserInput:");
			httpLog.AppendLine(user);
			Logger.Log("Logic", httpLog.ToString());
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request, (HttpCompletionOption)1);
				try
				{
					string statusLine = (int)response.StatusCode + " " + response.ReasonPhrase;
					Logger.Log("Logic", "[HTTP] 响应状态: " + statusLine);
					response.EnsureSuccessStatusCode();
					using Stream stream = await response.Content.ReadAsStreamAsync();
					if (stream == null)
					{
						Logger.Log("Logic", "[HTTP] 响应流为空。");
						return "错误: 响应流为空";
					}
					using StreamReader reader = new StreamReader(stream);
					StringBuilder fullContent = new StringBuilder();
					while (true)
					{
						string text;
						string line = (text = await reader.ReadLineAsync());
						if (text == null)
						{
							break;
						}
						if (!string.IsNullOrWhiteSpace(line) && !(line == "data: [DONE]") && line.StartsWith("data: "))
						{
							try
							{
								JObject json = JObject.Parse(line.Substring(6));
								string delta = json["choices"]?[0]?["delta"]?["content"]?.ToString() ?? "";
								fullContent.Append(delta);
							}
							catch (Exception ex)
							{
								Exception parseEx = ex;
								Logger.Log("Logic", "[HTTP] 流解析异常: " + parseEx.ToString());
							}
						}
					}
					string raw = fullContent.ToString();
					Logger.Log("Logic", "[HTTP] 流式原始内容=\n" + raw);
					return CleanAIResponse(raw);
				}
				finally
				{
					((IDisposable)response)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)request)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("Logic", "[ERROR] CallUniversalApi 异常: " + ex2.ToString());
			return "错误: " + ex2.Message;
		}
	}

	private static string StripActionTags(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Regex.Replace(text, "\\[ACTION:[^\\]]*\\]", "").Trim();
	}

	private string CleanAIResponse(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		input = Regex.Replace(input, "<think>.*?</think>", "", RegexOptions.Singleline);
		input = input.Replace("**", "").Replace("#", "").Replace("`", "");
		return input.Trim();
	}

	private static float GetNowCampaignDay()
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

	private static int ClampInt(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static float ClampFloat(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static int GetRelationWithPlayerSafe(Hero hero)
	{
		try
		{
			if (hero == null || Hero.MainHero == null)
			{
				return 0;
			}
			return hero.GetRelation(Hero.MainHero);
		}
		catch
		{
			return 0;
		}
	}

	private static int ComputePatienceMaxFromRelation(int relation)
	{
		double a = ((relation >= 0) ? (30.0 + (double)relation * 0.5) : (30.0 + (double)relation * 0.18));
		return ClampInt((int)Math.Round(a), 10, 80);
	}

	private static int ToTenLevelIndexByRatio(float current, int max)
	{
		if (max <= 0)
		{
			return 1;
		}
		double num = ClampFloat(current / (float)max, 0f, 1f);
		int num2 = (int)Math.Ceiling(num * 10.0);
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	private static int ToTenLevelIndexByRelation(int relation)
	{
		double num = ((double)ClampInt(relation, -100, 100) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	private static string GetPatienceLevelText(float current, int max)
	{
		int num = ToTenLevelIndexByRatio(current, max);
		return PatienceLevelTexts[num - 1];
	}

	private static string GetRelationLevelText(int relation)
	{
		int num = ToTenLevelIndexByRelation(relation);
		return RelationLevelTexts[num - 1];
	}

	private static int GetRelationLevelIndex(int relation)
	{
		return ToTenLevelIndexByRelation(relation);
	}

	private static void FillTrustSnapshot(ref PatienceSnapshot snap, Hero hero)
	{
		snap.Trust = 0;
		snap.PublicTrust = 0;
		snap.PrivateLove = 0;
		snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0);
		if (hero == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		try
		{
			int publicTrust = RewardSystemBehavior.Instance.GetPublicTrust(hero);
			int trust = (snap.Trust = RewardSystemBehavior.Instance.GetEffectiveTrust(hero));
			snap.PublicTrust = publicTrust;
			snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(trust);
			snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(publicTrust);
			int privateLove = (snap.PrivateLove = (RomanceSystemBehavior.Instance?.GetPrivateLove(hero)).GetValueOrDefault());
			snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(privateLove);
		}
		catch
		{
		}
	}

	private static string BuildCompactStateLine(PatienceSnapshot snap)
	{
		int num = (int)Math.Round(snap.Current);
		return $"P(耐心)={num}/{snap.Max}({snap.PatienceLevel}) | R(家族关系)={snap.Relation}({snap.RelationLevel}) | T(综合信任)={snap.Trust}({snap.TrustLevel}) | L(私人关系)={snap.PrivateLove}({snap.PrivateLoveLevel})";
	}

	private static string BuildHeroPatienceKey(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	private static string BuildUnnamedPatienceKey(string unnamedKey, string npcName)
	{
		string text = (unnamedKey ?? "").Trim().ToLower();
		if (!string.IsNullOrEmpty(text))
		{
			return "unnamed:" + text;
		}
		string text2 = (npcName ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text2))
		{
			return "";
		}
		return "name:" + text2;
	}

	private PatienceState GetOrCreateStateUnsafe(string key, int maxPatience, float nowDay)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		if (!_patienceStates.TryGetValue(key, out var value) || value == null)
		{
			value = new PatienceState
			{
				Value = maxPatience,
				LastDay = nowDay,
				NoInterestRounds = 0
			};
			_patienceStates[key] = value;
		}
		return value;
	}

	private void RecoverPatienceUnsafe(PatienceState state, int maxPatience, float nowDay)
	{
		if (state == null)
		{
			return;
		}
		if (nowDay > state.LastDay)
		{
			float num = nowDay - state.LastDay;
			if (num > 0f)
			{
				state.Value += num * 4f;
				state.LastDay = nowDay;
				if (state.NoInterestRounds > 0)
				{
					int num2 = (int)Math.Floor(num);
					if (num2 > 0)
					{
						state.NoInterestRounds = Math.Max(0, state.NoInterestRounds - num2);
					}
				}
			}
		}
		state.Value = ClampFloat(state.Value, 0f, maxPatience);
		if (state.Value > 0.01f && state.ExhaustedRefusalCount > 0)
		{
			state.ExhaustedRefusalCount = 0;
		}
	}

	private PatienceSnapshot GetHeroPatienceSnapshot(Hero hero)
	{
		PatienceSnapshot snap = new PatienceSnapshot
		{
			Key = "",
			DisplayName = (hero?.Name?.ToString() ?? "NPC"),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = "一般",
			RelationLevel = "中立",
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (hero == null)
		{
			return snap;
		}
		string text = BuildHeroPatienceKey(hero);
		if (string.IsNullOrEmpty(text))
		{
			return snap;
		}
		int relationWithPlayerSafe = GetRelationWithPlayerSafe(hero);
		int num = ComputePatienceMaxFromRelation(relationWithPlayerSafe);
		float nowCampaignDay = GetNowCampaignDay();
		float value;
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(text, num, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, num, nowCampaignDay);
			value = orCreateStateUnsafe.Value;
		}
		snap.Key = text;
		snap.Relation = relationWithPlayerSafe;
		snap.Max = num;
		snap.Current = value;
		snap.PatienceLevel = GetPatienceLevelText(value, num);
		snap.RelationLevel = GetRelationLevelText(relationWithPlayerSafe);
		FillTrustSnapshot(ref snap, hero);
		return snap;
	}

	private PatienceSnapshot GetUnnamedPatienceSnapshot(string unnamedKey, string npcName)
	{
		string text = BuildUnnamedPatienceKey(unnamedKey, npcName);
		PatienceSnapshot result = new PatienceSnapshot
		{
			Key = text,
			DisplayName = (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = GetPatienceLevelText(30f, 30),
			RelationLevel = GetRelationLevelText(0),
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (string.IsNullOrWhiteSpace(text))
		{
			return result;
		}
		float nowCampaignDay = GetNowCampaignDay();
		float value;
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(text, 30, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, 30, nowCampaignDay);
			value = orCreateStateUnsafe.Value;
		}
		result.Current = value;
		result.PatienceLevel = GetPatienceLevelText(value, 30);
		return result;
	}

	private static PatienceMood ParseMoodToken(string token)
	{
		string text = (token ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			return PatienceMood.Neutral;
		}
		switch (text)
		{
		case "delighted":
		case "very_happy":
		case "thrilled":
		case "heartwarmed":
		case "heart_warmed":
		case "affectionate":
		case "fond":
		case "sweet":
			return PatienceMood.Delighted;
		default:
			if (!(text == "鎰夊揩"))
			{
				switch (text)
				{
				default:
					if (!(text == "鐢熸皵"))
					{
						if (text == "bored" || text == "boring" || text == "鏃犺亰")
						{
							return PatienceMood.Bored;
						}
						return PatienceMood.Neutral;
					}
					goto case "annoyed";
				case "annoyed":
				case "angry":
				case "upset":
				case "irritated":
				case "displeased":
				case "涓嶆偊":
					return PatienceMood.Annoyed;
				}
			}
			goto case "joy";
		case "joy":
		case "happy":
		case "positive":
		case "amused":
		case "friendly":
		case "鍠滄偊":
			return PatienceMood.Joy;
		}
	}

	private static PatienceMood ExtractMoodAndStripTag(ref string text)
	{
		PatienceMood result = PatienceMood.Neutral;
		string text2 = text ?? "";
		MatchCollection matchCollection = MoodTagRegex.Matches(text2);
		if (matchCollection != null && matchCollection.Count > 0)
		{
			string value = matchCollection[matchCollection.Count - 1].Groups[1].Value;
			result = ParseMoodToken(value);
			text2 = MoodTagRegex.Replace(text2, "");
		}
		text = (text2 ?? "").Trim();
		return result;
	}

	private static int ComputePatienceDelta(PatienceMood mood, ref int noInterestRounds)
	{
		int num;
		switch (mood)
		{
		case PatienceMood.Delighted:
			num = 2;
			noInterestRounds = Math.Max(0, noInterestRounds - 3);
			break;
		case PatienceMood.Joy:
			num = 1;
			noInterestRounds = Math.Max(0, noInterestRounds - 2);
			break;
		case PatienceMood.Annoyed:
			num = -3;
			noInterestRounds++;
			break;
		case PatienceMood.Bored:
			num = -2;
			noInterestRounds++;
			break;
		default:
			num = -1;
			noInterestRounds++;
			break;
		}
		if (mood != PatienceMood.Joy && mood != PatienceMood.Delighted && noInterestRounds >= 3)
		{
			num--;
		}
		return num;
	}

	private static int ComputeNativeRelationDelta(PatienceMood mood, int currentRelation)
	{
		switch (mood)
		{
		case PatienceMood.Delighted:
			if (currentRelation >= 95)
			{
				return 0;
			}
			return 1;
		case PatienceMood.Annoyed:
			if (currentRelation <= -95)
			{
				return 0;
			}
			return -1;
		default:
			return 0;
		}
	}

	private static int ComputePrivateLoveDelta(PatienceMood mood)
	{
		switch (mood)
		{
		case PatienceMood.Delighted:
			return 2;
		case PatienceMood.Joy:
			return 1;
		case PatienceMood.Bored:
			return -1;
		case PatienceMood.Annoyed:
			return -2;
		default:
			return 0;
		}
	}

	private bool ApplyPatienceDeltaInternal(string key, int maxPatience, PatienceMood mood, out int before, out int after)
	{
		before = 0;
		after = 0;
		if (string.IsNullOrWhiteSpace(key) || maxPatience <= 0)
		{
			return false;
		}
		float nowCampaignDay = GetNowCampaignDay();
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(key, maxPatience, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, maxPatience, nowCampaignDay);
			int num = (int)Math.Round(orCreateStateUnsafe.Value);
			int num2 = ComputePatienceDelta(mood, ref orCreateStateUnsafe.NoInterestRounds);
			orCreateStateUnsafe.Value = ClampFloat(orCreateStateUnsafe.Value + (float)num2, 0f, maxPatience);
			orCreateStateUnsafe.LastDay = nowCampaignDay;
			int num3 = (int)Math.Round(orCreateStateUnsafe.Value);
			before = num;
			after = num3;
			return num > 0 && num3 <= 0;
		}
	}

	private static string BuildExhaustedReply(string npcName, int relation, int refusalCount = 1)
	{
		string text = (string.IsNullOrWhiteSpace(npcName) ? "我" : npcName);
		if (refusalCount < 1)
		{
			refusalCount = 1;
		}
		if (relation >= 40)
		{
			string[] array = new string[3]
			{
				text + "压低声音：今天我确实有些乏了，改天再聊。",
				text + "轻叹：我现在没有余力应付长谈，先到这里吧。",
				text + "摇头道：等我缓一缓，再听你说。"
			};
			return array[(refusalCount - 1) % array.Length];
		}
		if (relation <= -20)
		{
			string[] array2 = new string[3]
			{
				text + "皱眉道：我眼下有要事，没空陪你闲扯，退下。",
				text + "冷声道：军务在身，今天到此为止。",
				text + "摆手打断：我还要处理别的事，改日再说。"
			};
			return array2[(refusalCount - 1) % array2.Length];
		}
		string[] array3 = new string[3]
		{
			text + "摆摆手：我这边还有事，今天就先到这。",
			text + "皱了皱眉：行程紧，改日再谈。",
			text + "侧过身：先到这里，我得去处理别的事。"
		};
		return array3[(refusalCount - 1) % array3.Length];
	}

	private static string BuildPatiencePromptText(PatienceSnapshot snap)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【4.四值状态】");
		stringBuilder.AppendLine(BuildCompactStateLine(snap));
		stringBuilder.AppendLine("规则：情绪标签优先影响私人关系 L，并在最强正负情绪下顺带影响原生关系 R。DELIGHTED=L+2 且 R+1；JOY=L+1；NEUTRAL=0；BORED=L-1；ANNOYED=L-2 且 R-1。回复末尾必须且只能追加一个 [ACTION:MOOD:DELIGHTED/JOY/NEUTRAL/BORED/ANNOYED]；耐心低时更不耐烦，耐心归零后仍可继续对话，但继续交流会额外降低关系。");
		return stringBuilder.ToString();
	}

	private string BuildDirectPatiencePrompt(Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
		exhausted = false;
		exhaustedReply = "";
		if (targetHero == null)
		{
			return "";
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(targetHero);
		if (heroPatienceSnapshot.Current <= 0.01f)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(BuildPatiencePromptText(heroPatienceSnapshot).TrimEnd());
			stringBuilder.AppendLine("【耐心状态】你当前耐心已归零。仍可继续对话，但应明显更谨慎或更不耐烦。");
			stringBuilder.AppendLine("继续交流会额外损害你与玩家的关系（由系统结算，不要在文本中直说数值）。");
			stringBuilder.AppendLine("除 [ACTION:MOOD:...] 外，不要输出任何 ACTION 标签。");
			return stringBuilder.ToString();
		}
		return BuildPatiencePromptText(heroPatienceSnapshot);
	}

	private void ApplyPatienceFromHeroResponse(Hero targetHero, ref string aiResponse, bool directConversation)
	{
		if (targetHero == null)
		{
			string text = aiResponse ?? "";
			ExtractMoodAndStripTag(ref text);
			aiResponse = text;
			return;
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(targetHero);
		PatienceMood patienceMood = ExtractMoodAndStripTag(ref aiResponse);
		int before;
		int after;
		bool flag = ApplyPatienceDeltaInternal(heroPatienceSnapshot.Key, heroPatienceSnapshot.Max, patienceMood, out before, out after);
		int relation = heroPatienceSnapshot.Relation;
		int privateLove = heroPatienceSnapshot.PrivateLove;
		int num = ComputeNativeRelationDelta(patienceMood, relation);
		int num2 = ComputePrivateLoveDelta(patienceMood);
		bool flag2 = before <= 0;
		if (flag2)
		{
			num--;
		}
		int num3 = relation;
		if (num != 0)
		{
			try
			{
				if (Hero.MainHero != null && targetHero != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, targetHero, num);
					num3 = GetRelationWithPlayerSafe(targetHero);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Patience", "[WARN] apply relation delta failed: " + ex.Message);
			}
		}
		if (num2 != 0)
		{
			RomanceSystemBehavior.Instance?.AdjustPrivateLove(targetHero, num2, "mood_tag");
		}
		int num4 = RomanceSystemBehavior.Instance?.GetPrivateLove(targetHero) ?? privateLove;
		try
		{
			Logger.Log("Patience", $"hero={targetHero.StringId} mood={patienceMood} value={before}->{after}/{heroPatienceSnapshot.Max} relation={relation}->{num3} delta={num} privateLove={privateLove}->{num4} deltaLove={num2}");
			Logger.Obs("Patience", "hero_update", new Dictionary<string, object>
			{
				["heroId"] = targetHero.StringId ?? "",
				["mood"] = patienceMood.ToString(),
				["patienceBefore"] = before,
				["patienceAfter"] = after,
				["patienceMax"] = heroPatienceSnapshot.Max,
				["relationBefore"] = relation,
				["relationAfter"] = num3,
				["relationDelta"] = num,
				["privateLoveBefore"] = privateLove,
				["privateLoveAfter"] = num4,
				["privateLoveDelta"] = num2,
				["continuedAtZero"] = flag2,
				["directConversation"] = directConversation
			});
			Logger.Metric("patience.hero_update");
		}
		catch
		{
		}
		if (!flag)
		{
			return;
		}
		try
		{
			Logger.Log("Patience", "hero=" + targetHero.StringId + " reached_zero=true defer_exhausted_to_next_round=" + (directConversation ? "direct" : "scene"));
		}
		catch
		{
		}
	}

	private void ApplyPatienceFromUnnamedResponse(string unnamedKey, string npcName, ref string aiResponse)
	{
		PatienceSnapshot unnamedPatienceSnapshot = GetUnnamedPatienceSnapshot(unnamedKey, npcName);
		PatienceMood patienceMood = ExtractMoodAndStripTag(ref aiResponse);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return;
		}
		int before;
		int after;
		bool flag = ApplyPatienceDeltaInternal(unnamedPatienceSnapshot.Key, unnamedPatienceSnapshot.Max, patienceMood, out before, out after);
		try
		{
			Logger.Log("Patience", $"unnamed={unnamedPatienceSnapshot.Key} mood={patienceMood} value={before}->{after}/{unnamedPatienceSnapshot.Max}");
			Logger.Obs("Patience", "unnamed_update", new Dictionary<string, object>
			{
				["key"] = unnamedPatienceSnapshot.Key ?? "",
				["mood"] = patienceMood.ToString(),
				["patienceBefore"] = before,
				["patienceAfter"] = after,
				["patienceMax"] = unnamedPatienceSnapshot.Max
			});
			Logger.Metric("patience.unnamed_update");
		}
		catch
		{
		}
		if (!flag)
		{
			return;
		}
		try
		{
			Logger.Log("Patience", "unnamed=" + unnamedPatienceSnapshot.Key + " reached_zero=true defer_exhausted_to_next_round=scene");
		}
		catch
		{
		}
	}

	private bool TryGetHeroSceneStatus(Hero hero, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		if (hero == null)
		{
			return false;
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(hero);
		int num = (int)Math.Round(heroPatienceSnapshot.Current);
		statusLine = "- " + heroPatienceSnapshot.DisplayName + ": " + BuildCompactStateLine(heroPatienceSnapshot);
		if (num <= 0)
		{
			statusLine += "，耐心已归零（可继续交流，但会额外降低关系）";
		}
		return true;
	}

	private bool TryGetUnnamedSceneStatus(string unnamedKey, string npcName, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		PatienceSnapshot unnamedPatienceSnapshot = GetUnnamedPatienceSnapshot(unnamedKey, npcName);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return false;
		}
		int num = (int)Math.Round(unnamedPatienceSnapshot.Current);
		statusLine = $"- {unnamedPatienceSnapshot.DisplayName}: P(耐心)={num}/{unnamedPatienceSnapshot.Max}({unnamedPatienceSnapshot.PatienceLevel}) | R(家族关系)=0(中立) | T(综合信任)=0(中性观望) | L(私人关系)=0({RomanceSystemBehavior.GetPrivateLoveLevelText(0)})";
		if (num <= 0)
		{
			statusLine += "，耐心已归零（可继续交流，但会额外降低关系）";
		}
		return true;
	}

	private static string BuildScenePatienceInstruction()
	{
		return "【耐心标签要求】：你必须在你回复的末尾加入且只能加入一个合适的标签。强烈被打动/明显心动：[ACTION:MOOD:DELIGHTED]；喜悦友好：[ACTION:MOOD:JOY]；平淡：[ACTION:MOOD:NEUTRAL]；无聊冷淡：[ACTION:MOOD:BORED]；恼火反感：[ACTION:MOOD:ANNOYED]。并始终按四值状态中的 P/R/T/L 来决定语气与合作度。";
	}

	private void SyncPatienceData(IDataStore dataStore)
	{
		if (_patienceStates == null)
		{
			_patienceStates = new Dictionary<string, PatienceState>();
		}
		if (_patienceStorage == null)
		{
			_patienceStorage = new Dictionary<string, string>();
		}
		if (dataStore == null)
		{
			return;
		}
		try
		{
			if (dataStore.IsSaving)
			{
				lock (_patienceLock)
				{
					_patienceStorage.Clear();
					foreach (KeyValuePair<string, PatienceState> patienceState in _patienceStates)
					{
						if (!string.IsNullOrWhiteSpace(patienceState.Key) && patienceState.Value != null)
						{
							try
							{
								PatienceStateSaveModel value = new PatienceStateSaveModel
								{
									Value = patienceState.Value.Value,
									LastDay = patienceState.Value.LastDay,
									NoInterestRounds = patienceState.Value.NoInterestRounds,
									ExhaustedRefusalCount = patienceState.Value.ExhaustedRefusalCount
								};
								_patienceStorage[patienceState.Key] = JsonConvert.SerializeObject(value);
							}
							catch
							{
							}
						}
					}
				}
				dataStore.SyncData("_patienceStates_v1", ref _patienceStorage);
				return;
			}
			lock (_patienceLock)
			{
				_patienceStates.Clear();
				_patienceStorage.Clear();
			}
			dataStore.SyncData("_patienceStates_v1", ref _patienceStorage);
			if (_patienceStorage == null)
			{
				return;
			}
			lock (_patienceLock)
			{
				foreach (KeyValuePair<string, string> item in _patienceStorage)
				{
					if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
					{
						continue;
					}
					try
					{
						PatienceStateSaveModel patienceStateSaveModel = JsonConvert.DeserializeObject<PatienceStateSaveModel>(item.Value);
						if (patienceStateSaveModel != null)
						{
							_patienceStates[item.Key] = new PatienceState
							{
								Value = patienceStateSaveModel.Value,
								LastDay = patienceStateSaveModel.LastDay,
								NoInterestRounds = patienceStateSaveModel.NoInterestRounds,
								ExhaustedRefusalCount = patienceStateSaveModel.ExhaustedRefusalCount
							};
						}
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patience", "[ERROR] SyncPatienceData failed: " + ex.Message);
			lock (_patienceLock)
			{
				_patienceStates = new Dictionary<string, PatienceState>();
				_patienceStorage = new Dictionary<string, string>();
			}
		}
	}

	public static string BuildDirectPatiencePromptForExternal(Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
		exhausted = false;
		exhaustedReply = "";
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildDirectPatiencePrompt(targetHero, out exhausted, out exhaustedReply);
		}
		catch
		{
			exhausted = false;
			exhaustedReply = "";
			return "";
		}
	}

	public static void ApplyPatienceFromDirectResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: true);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneHeroResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: false);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneUnnamedResponseExternal(string unnamedKey, string npcName, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromUnnamedResponse(unnamedKey, npcName, ref aiResponse);
		}
		catch
		{
		}
	}

	public static bool TryGetSceneHeroPatienceStatusForExternal(Hero hero, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetHeroSceneStatus(hero, out statusLine, out canSpeak) ?? false;
		}
		catch
		{
			statusLine = "";
			canSpeak = true;
			return false;
		}
	}

	public static bool TryGetSceneUnnamedPatienceStatusForExternal(string unnamedKey, string npcName, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetUnnamedSceneStatus(unnamedKey, npcName, out statusLine, out canSpeak) ?? false;
		}
		catch
		{
			statusLine = "";
			canSpeak = true;
			return false;
		}
	}

	public static string GetScenePatienceInstructionForExternal()
	{
		return BuildScenePatienceInstruction();
	}

	public static string BuildDirectNamePatienceBadgeForExternal(Hero targetHero)
	{
		return "";
	}

	public static string BuildScenePatienceBadgeForHeroExternal(Hero hero)
	{
		return "";
	}

	public static string BuildScenePatienceBadgeForUnnamedExternal(string unnamedKey, string npcName)
	{
		return "";
	}

	private void DevRootMenuInit(MenuCallbackArgs args)
	{
		try
		{
			args.MenuTitle = new TextObject("开发者工具");
		}
		catch
		{
		}
	}

	private bool DevRootEntryCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
	}

	private void DevRootEntryConsequence(MenuCallbackArgs args)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableDevEditHistory)
		{
			InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
		}
		else
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
	}

	private bool DevRootSubOptionCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
	}

	private void DevRootHeroOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevHeroNpcMenu();
	}

	private void DevRootNonHeroOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevUnnamedPersonaMenu();
	}

	private void DevRootKnowledgeOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevKnowledgeMenu();
	}

	private void DevRootAllOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevAllDataMenu();
	}

	private void DevRootVoiceMappingOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevVoiceMappingMenu();
	}

	private bool DevRootBackCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void DevRootBackConsequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("town");
	}

	private void OpenDevHeroNpcMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit_hero", "编辑 HeroNPC（历史/赊账/个性背景）…", null));
		list.Add(new InquiryElement("single_ie", "单个 HeroNPC 导入/导出…", null));
		list.Add(new InquiryElement("export_hero_all", "全量导出（HeroNPC：历史+赊账+个性背景，选文件夹）", null));
		list.Add(new InquiryElement("import_hero_all", "全量导入（HeroNPC：历史+赊账+个性背景，选文件夹）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("HeroNPC 编辑/导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevHeroNpcMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHeroNpcMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_hero":
				OpenDevTownEditorHeroSelection();
				break;
			case "single_ie":
				OpenDevSingleNpcHeroSelection();
				break;
			case "export_hero_all":
				OpenExportFolderPicker("全量导出（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			case "import_hero_all":
				OpenImportFolderPicker("全量导入（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			}
		}
	}

	private void OpenDevAllDataMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_all", "导出（全部，选择文件夹）", null));
		list.Add(new InquiryElement("import_all", "导入（全部，选择文件夹）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("全部导出/导入", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevAllDataMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevAllDataMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			string text = selected[0].Identifier as string;
			if (text == "export_all")
			{
				OpenExportFolderPicker("导出（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
			else if (text == "import_all")
			{
				OpenImportFolderPicker("导入（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
		}
	}

	private void OpenDevTownEditorHeroSelection()
	{
		_devEditingHero = null;
		_devHistorySearchQuery = string.Empty;
		_devEditableHeroes = BuildDevEditableHeroList();
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有任何可编辑的NPC（尚未产生对话历史或欠款）。"));
			OpenDevHeroNpcMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		for (int i = 0; i < _devEditableHeroes.Count; i++)
		{
			Hero hero = _devEditableHeroes[i];
			string text = hero.Name?.ToString() ?? "NPC";
			string text2 = hero.StringId ?? "";
			string title = (string.IsNullOrEmpty(text2) ? text : (text + " (ID=" + text2 + ")"));
			list.Add(new InquiryElement(hero, title, null));
		}
		string descriptionText = "请选择要编辑的 NPC。";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - 选择NPC", descriptionText, list, isExitShown: true, 0, 1, "确定", "返回", OnDevHeroSelected, delegate
		{
			OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay.Lines.Count)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的对话行。"));
			OpenDevHistoryLineSelection(npc, dayIndex);
			return;
		}
		string value = dialogueDay.Lines[lineIndex] ?? "";
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text2 = npc.Name?.ToString() ?? "NPC";
		string titleText = "编辑对话行 - " + text2;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("当前日期: " + text);
		stringBuilder.AppendLine("原内容：");
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("请输入新的内容。留空则删除该行。");
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "取消", delegate(string input)
		{
			List<DialogueDay> source = LoadDialogueHistory(npc);
			DialogueDay dialogueDay2 = source.FirstOrDefault((DialogueDay x) => x.GameDayIndex == dayIndex);
			if (dialogueDay2 == null || dialogueDay2.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay2.Lines.Count)
			{
				OpenDevHistoryLineSelection(npc, dayIndex);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(input))
				{
					dialogueDay2.Lines.RemoveAt(lineIndex);
				}
				else
				{
					dialogueDay2.Lines[lineIndex] = input;
				}
				source = source.Where((DialogueDay x) => x != null && x.Lines != null && x.Lines.Count > 0).ToList();
				SaveDialogueHistory(npc, source);
				InformationManager.DisplayMessage(new InformationMessage("对话行已更新."));
				OpenDevHistoryLineSelection(npc, dayIndex);
			}
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		}));
	}

	private List<Hero> BuildDevEditableHeroList()
	{
		HashSet<string> hashSet = new HashSet<string>();
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null)
			{
				try
				{
					MBReadOnlyList<Hero> notables = currentSettlement.Notables;
					if (notables != null)
					{
						foreach (Hero item in notables)
						{
							if (item != null && !string.IsNullOrEmpty(item.StringId))
							{
								hashSet.Add(item.StringId);
							}
						}
					}
				}
				catch
				{
				}
				try
				{
					Hero hero = currentSettlement.OwnerClan?.Leader;
					if (hero != null && !string.IsNullOrEmpty(hero.StringId))
					{
						hashSet.Add(hero.StringId);
					}
				}
				catch
				{
				}
				try
				{
					Hero hero2 = currentSettlement.Town?.Governor;
					if (hero2 != null && !string.IsNullOrEmpty(hero2.StringId))
					{
						hashSet.Add(hero2.StringId);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		if (_dialogueHistory != null)
		{
			foreach (KeyValuePair<string, List<DialogueDay>> item2 in _dialogueHistory)
			{
				if (!string.IsNullOrEmpty(item2.Key))
				{
					hashSet.Add(item2.Key);
				}
			}
		}
		if (_npcPersonaProfiles != null)
		{
			foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
			{
				if (!string.IsNullOrEmpty(npcPersonaProfile.Key))
				{
					hashSet.Add(npcPersonaProfile.Key);
				}
			}
		}
		if (RewardSystemBehavior.Instance != null)
		{
			List<string> allDebtorHeroIds = RewardSystemBehavior.Instance.GetAllDebtorHeroIds();
			if (allDebtorHeroIds != null)
			{
				foreach (string item3 in allDebtorHeroIds)
				{
					if (!string.IsNullOrEmpty(item3))
					{
						hashSet.Add(item3);
					}
				}
			}
		}
		List<Hero> list = new List<Hero>();
		foreach (string id in hashSet)
		{
			Hero hero3 = Hero.FindFirst((Hero h) => h != null && h.StringId == id);
			if (hero3 != null)
			{
				list.Add(hero3);
			}
		}
		if (list.Count > 1)
		{
			list = list.OrderBy((Hero h) => h.Name?.ToString()).ToList();
		}
		return list;
	}

	private void OnDevHeroSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (!(selected[0].Identifier is Hero devEditingHero))
		{
			OpenDevHeroNpcMenu();
			return;
		}
		_devEditingHero = devEditingHero;
		_devHistorySearchQuery = string.Empty;
		ShowDevEditInquiry(_devEditingHero);
	}

	private void ShowDevEditInquiry(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine(" - 编辑对话历史");
			stringBuilder.AppendLine(" - 编辑赊账/欠款");
			stringBuilder.AppendLine(" - 编辑角色个性/历史背景");
			stringBuilder.AppendLine(" - 切换 NPC");
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("edit_history", "编辑对话历史", null));
			list.Add(new InquiryElement("edit_debt", "编辑赊账/欠款", null));
			list.Add(new InquiryElement("edit_persona", "编辑角色个性/历史背景", null));
			list.Add(new InquiryElement("switch_npc", "切换 NPC", null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", OnDevNpcMainMenuSelected, delegate
			{
				OpenDevHeroNpcMenu();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	private void OpenDevSetDebtGoldSimple(Hero npc)
	{
		if (npc == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		RewardSystemBehavior.Instance.GetDebtSnapshot(npc, out var owedGold, out var items);
		string text = npc.Name?.ToString() ?? "NPC";
		string text2 = "当前金币欠款: " + owedGold + "。\n输入新的金币欠款数值（允许为 0）：";
		InformationManager.ShowTextInquiry(new TextInquiryData("设置金币欠款 - " + text, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
			int result;
			if (string.IsNullOrWhiteSpace(input))
			{
				OpenDevDebtMenu(npc);
			}
			else if (!int.TryParse(input, out result) || result < 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的非负整数."));
				OpenDevSetDebtGoldSimple(npc);
			}
			else
			{
				RewardSystemBehavior.Instance.SetDebt(npc, result, items);
				InformationManager.DisplayMessage(new InformationMessage("已更新金币欠款为 " + result + "。"));
				OpenDevDebtMenu(npc);
			}
		}, delegate
		{
			OpenDevDebtMenu(npc);
		}));
	}

	private void OpenDevDebtMenu(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = "请选择要执行的操作：";
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				string text2 = RewardSystemBehavior.Instance.BuildDebtEditorSummary(npc, 10);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					descriptionText = text2;
				}
			}
		}
		catch
		{
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("set_gold", "设置金币欠款", null));
		list.Add(new InquiryElement("clear_debt", "清空所有欠款", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑赊账/欠款 - " + text, descriptionText, list, isExitShown: true, 0, 1, "执行", "返回", OnDevDebtMenuSelected, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevDebtMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ShowDevEditInquiry(devEditingHero);
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "set_gold")
		{
			OpenDevSetDebtGoldSimple(devEditingHero);
		}
		else if (text == "clear_debt")
		{
			if (RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.SetDebt(devEditingHero, 0, new Dictionary<string, int>());
				InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的所有欠款."));
			}
			OpenDevDebtMenu(devEditingHero);
		}
		else
		{
			ShowDevEditInquiry(devEditingHero);
		}
	}

	private void OnDevNpcMainMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero != null && selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_history":
				OpenDevHistoryDateSelection(devEditingHero);
				break;
			case "edit_debt":
				OpenDevDebtMenu(devEditingHero);
				break;
			case "edit_persona":
				OpenDevPersonaMenu(devEditingHero);
				break;
			case "switch_npc":
				OpenDevTownEditorHeroSelection();
				break;
			}
		}
	}

	private void OpenDevPersonaMenu(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			GetNpcPersonaStrings(npc, out var personality, out var background);
			bool flag = !string.IsNullOrWhiteSpace(personality);
			bool flag2 = !string.IsNullOrWhiteSpace(background);
			string npcVoiceId = GetNpcVoiceId(npc);
			bool flag3 = !string.IsNullOrWhiteSpace(npcVoiceId);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine("当前个性：" + (flag ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前历史背景：" + (flag2 ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前音色ID：" + (flag3 ? npcVoiceId : "未设置（使用VoiceMapping自动分配）"));
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("set_personality", "设置/修改个性", null));
			list.Add(new InquiryElement("set_background", "设置/修改历史背景", null));
			list.Add(new InquiryElement("set_voice", "设置/修改音色ID", null));
			list.Add(new InquiryElement("clear_persona", "清空个性、历史背景与音色ID", null));
			list.Add(new InquiryElement("back", "返回", null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑个性/历史背景 - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "执行", "返回", OnDevPersonaMenuSelected, delegate
			{
				ShowDevEditInquiry(npc);
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	private void OnDevPersonaMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ShowDevEditInquiry(devEditingHero);
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "set_personality":
			OpenDevSetPersonality(devEditingHero);
			break;
		case "set_background":
			OpenDevSetBackground(devEditingHero);
			break;
		case "set_voice":
			OpenDevSetVoiceId(devEditingHero);
			break;
		case "clear_persona":
			SaveNpcPersonaProfile(devEditingHero, new NpcPersonaProfile());
			InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的个性、历史背景与音色ID."));
			OpenDevPersonaMenu(devEditingHero);
			break;
		default:
			ShowDevEditInquiry(devEditingHero);
			break;
		}
	}

	private static string BuildDevHistoryPreview(string line, int maxLen = 56)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return "（空）";
		}
		StringBuilder stringBuilder = new StringBuilder(line.Length);
		bool flag = false;
		foreach (char c in line)
		{
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		string text = stringBuilder.ToString().Trim();
		if (text.Length <= maxLen)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxLen)) + "...";
	}

	private void OpenDevHistorySearchInput(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string soundEventPath = _devHistorySearchQuery ?? string.Empty;
		InformationManager.ShowTextInquiry(new TextInquiryData("搜索对话历史 - " + text, "输入关键词，按内容模糊匹配该 NPC 的全部对话历史。\n留空将不改变当前搜索。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
		{
			if (!string.IsNullOrWhiteSpace(input))
			{
				_devHistorySearchQuery = input.Trim();
			}
			OpenDevHistoryDateSelection(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, shouldInputBeObfuscated: false, null, soundEventPath));
	}

	private void ConfirmDevClearAllDialogueHistory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = LoadDialogueHistory(npc);
		int num = 0;
		if (list != null)
		{
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
		}
		string arg = npc.Name?.ToString() ?? "NPC";
		string text = $"将删除 {arg} 的全部对话历史（共 {num} 条）。\n此操作不可撤销，是否继续？";
		InformationManager.ShowInquiry(new InquiryData("确认删除对话历史", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认删除", "取消", delegate
		{
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string text2 = npc.StringId ?? string.Empty;
			if (!string.IsNullOrEmpty(text2))
			{
				_dialogueHistory.Remove(text2);
			}
			_devHistorySearchQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage("已删除该NPC的全部对话历史。"));
			ShowDevEditInquiry(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}));
	}

	private void OpenDevHistoryDateSelection(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			ShowDevEditInquiry(npc);
			return;
		}
		list = list.OrderBy((DialogueDay d) => d.GameDayIndex).ToList();
		string text = (_devHistorySearchQuery ?? string.Empty).Trim();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__search__", "搜索对话历史", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement("__clear_search__", "清空搜索", null));
		}
		list2.Add(new InquiryElement("__clear_all__", "一键删除该NPC全部对话历史", null));
		list2.Add(new InquiryElement("back", "返回", null));
		if (string.IsNullOrWhiteSpace(text))
		{
			foreach (DialogueDay item in list)
			{
				if (item != null && item.Lines != null && item.Lines.Count != 0)
				{
					string text2 = ((!string.IsNullOrEmpty(item.GameDate)) ? item.GameDate : $"第 {item.GameDayIndex} 日");
					string title = text2 + $" (共 {item.Lines.Count} 条)";
					list2.Add(new InquiryElement(item.GameDayIndex, title, null));
				}
			}
		}
		else
		{
			string value = text.ToLowerInvariant();
			foreach (DialogueDay item2 in list)
			{
				if (item2 == null || item2.Lines == null || item2.Lines.Count == 0)
				{
					continue;
				}
				string arg = ((!string.IsNullOrEmpty(item2.GameDate)) ? item2.GameDate : $"第 {item2.GameDayIndex} 日");
				for (int num = 0; num < item2.Lines.Count; num++)
				{
					string text3 = item2.Lines[num] ?? string.Empty;
					if (text3.ToLowerInvariant().Contains(value))
					{
						string arg2 = BuildDevHistoryPreview(text3);
						string title2 = $"{arg} / 第{num + 1}条: {arg2}";
						list2.Add(new InquiryElement(new Tuple<int, int>(item2.GameDayIndex, num), title2, null));
					}
				}
			}
		}
		string text4 = npc.Name?.ToString() ?? "NPC";
		string text5 = "请选择要查看/编辑的日期。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + "\n当前搜索：" + BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text5 += "\n\n没有匹配结果。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话历史 - " + text4, text5, list2, isExitShown: true, 0, 1, "下一步", "返回", OnDevHistoryDateSelected, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHistoryDateSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				ShowDevEditInquiry(devEditingHero);
				return;
			case "__search__":
				OpenDevHistorySearchInput(devEditingHero);
				return;
			case "__clear_search__":
				_devHistorySearchQuery = string.Empty;
				OpenDevHistoryDateSelection(devEditingHero);
				return;
			case "__clear_all__":
				ConfirmDevClearAllDialogueHistory(devEditingHero);
				return;
			}
		}
		if (selected[0].Identifier is Tuple<int, int> tuple)
		{
			OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			return;
		}
		if (!(selected[0].Identifier is int))
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		int dayIndex = (int)selected[0].Identifier;
		OpenDevHistoryLineSelection(devEditingHero, dayIndex);
	}

	private void OpenDevHistoryLineSelection(Hero npc, int dayIndex)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || dialogueDay.Lines.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该日期下没有对话行。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回日期列表", null));
		list2.Add(new InquiryElement("search", "搜索该NPC历史", null));
		for (int num = 0; num < dialogueDay.Lines.Count; num++)
		{
			string line = dialogueDay.Lines[num] ?? "";
			string arg = BuildDevHistoryPreview(line);
			string title = $"{num + 1}. {arg}";
			list2.Add(new InquiryElement(new Tuple<int, int>(dayIndex, num), title, null));
		}
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text2 = npc.Name?.ToString() ?? "NPC";
		string text3 = "请选择要编辑的对话行。";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话行 - " + text2, text3 + "\n当前日期: " + text, list2, isExitShown: true, 0, 1, "编辑", "返回", OnDevHistoryLineSelected, delegate
		{
			OpenDevHistoryDateSelection(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHistoryLineSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero != null)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text2 && text2 == "search")
			{
				OpenDevHistorySearchInput(devEditingHero);
			}
			else if (!(selected[0].Identifier is Tuple<int, int> tuple))
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else
			{
				OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			}
		}
	}

	private void OpenDevSetPersonality(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前个性：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(personality) ? "（未设置）" : personality);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的个性描述（留空=清空）：");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置个性 - " + text, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.Personality = (input ?? "").Trim();
				npcPersonaProfile.Background = (background ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				InformationManager.DisplayMessage(new InformationMessage("个性已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			}));
		}
	}

	private void OpenDevSetVoiceId(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string npcVoiceId = GetNpcVoiceId(npc);
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前音色ID：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(npcVoiceId) ? "（未设置 - 使用VoiceMapping自动分配）" : npcVoiceId);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的音色ID（留空=清空，将使用VoiceMapping自动分配）：");
			stringBuilder.AppendLine("提示：音色ID取决于您使用的TTS平台。");
			stringBuilder.AppendLine("  火山引擎例如: BV001_streaming");
			stringBuilder.AppendLine("  GPT-SoVITS例如: 参考音频文件名");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置音色ID - " + text, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.VoiceId = (input ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				string information = (string.IsNullOrWhiteSpace(input) ? "音色ID已清空（将使用VoiceMapping自动分配）." : ("音色ID已更新为: " + input.Trim()));
				InformationManager.DisplayMessage(new InformationMessage(information));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			}, shouldInputBeObfuscated: false, null, npcVoiceId ?? ""));
		}
	}

	private void OpenDevSetBackground(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前历史背景：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(background) ? "（未设置）" : background);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的历史背景描述（留空=清空）：");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置历史背景 - " + text, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.Personality = (personality ?? "").Trim();
				npcPersonaProfile.Background = (input ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				InformationManager.DisplayMessage(new InformationMessage("历史背景已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			}));
		}
	}

	private void OpenDevKnowledgeMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_rule", "导出（单个知识条目，从列表选择）", null));
		list.Add(new InquiryElement("single_import_rule", "导入（单个知识条目，从文件夹选择）", null));
		list.Add(new InquiryElement("export_knowledge", "全量导出（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("import_knowledge", "全量导入（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("edit_knowledge", "编辑 Knowledge 条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("Knowledge 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevKnowledgeMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevKnowledgeMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "edit_knowledge":
			if (KnowledgeLibraryBehavior.Instance != null)
			{
				KnowledgeLibraryBehavior.Instance.OpenEditorMenu(delegate
				{
					OpenDevKnowledgeMenu();
				});
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("KnowledgeLibraryBehavior 未初始化，无法打开知识编辑。"));
				OpenDevKnowledgeMenu();
			}
			break;
		case "single_export_rule":
			OpenDevKnowledgeSingleExportSelection();
			break;
		case "single_import_rule":
			_devPendingKnowledgeSingleImportPicked = false;
			_devPendingKnowledgeSingleImportFolderName = null;
			OpenFolderPickerWithCallback("导入（单个知识条目）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				_devPendingKnowledgeSingleImportPicked = true;
				_devPendingKnowledgeSingleImportFolderName = folderName;
			}, delegate
			{
				if (!_devPendingKnowledgeSingleImportPicked)
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					string devPendingKnowledgeSingleImportFolderName = _devPendingKnowledgeSingleImportFolderName;
					_devPendingKnowledgeSingleImportPicked = false;
					_devPendingKnowledgeSingleImportFolderName = null;
					OpenDevKnowledgeSingleImportSelection(devPendingKnowledgeSingleImportFolderName);
				}
			});
			break;
		case "export_knowledge":
			OpenExportFolderPicker("全量导出（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		case "import_knowledge":
			OpenImportFolderPicker("全量导入（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		}
	}

	private void OpenDevKnowledgeSingleExportSelection()
	{
		List<KnowledgeLibraryBehavior.RuleIndexItem> list = (KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>())?.GetRuleIndexItemsForDev(400);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KnowledgeLibraryBehavior.RuleIndexItem item in list)
		{
			string text = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				string title = (item?.Label ?? text).Trim();
				list2.Add(new InquiryElement(text, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个知识条目 - 选择条目", "请选择要导出的知识条目：", list2, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string rid = selected[0].Identifier as string;
				if (rid == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					rid = (rid ?? "").Trim();
					if (string.IsNullOrEmpty(rid))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						OpenFolderPickerWithCallback("导出（单个知识条目）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							ExportSingleKnowledgeRuleData(folderName, rid);
						}, OpenDevKnowledgeMenu);
					}
				}
			}
		}, delegate
		{
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevKnowledgeSingleImportSelection(string folderName)
	{
		List<string> knowledgeRuleIdsFromImportFolderForDev = GetKnowledgeRuleIdsFromImportFolderForDev(folderName, 400);
		if (knowledgeRuleIdsFromImportFolderForDev == null || knowledgeRuleIdsFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in knowledgeRuleIdsFromImportFolderForDev)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个知识条目 - 选择条目", "请选择要导入的知识条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						ImportSingleKnowledgeRuleData(folderName, text2);
						OpenDevKnowledgeMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_unnamed", "导出（单个未命名NPC，从列表选择）", null));
		list.Add(new InquiryElement("single_import_unnamed", "导入（单个未命名NPC，从文件夹选择）", null));
		list.Add(new InquiryElement("export_unnamed", "全量导出（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("import_unnamed", "全量导入（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("edit_unnamed", "编辑未命名NPC条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("未命名NPC 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevUnnamedPersonaMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevUnnamedPersonaMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "single_export_unnamed":
			OpenDevUnnamedPersonaSingleExportSelection();
			break;
		case "single_import_unnamed":
			_devPendingUnnamedSingleImportPicked = false;
			_devPendingUnnamedSingleImportFolderName = null;
			OpenFolderPickerWithCallback("导入（单个未命名NPC）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				_devPendingUnnamedSingleImportPicked = true;
				_devPendingUnnamedSingleImportFolderName = folderName;
			}, delegate
			{
				if (!_devPendingUnnamedSingleImportPicked)
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					string devPendingUnnamedSingleImportFolderName = _devPendingUnnamedSingleImportFolderName;
					_devPendingUnnamedSingleImportPicked = false;
					_devPendingUnnamedSingleImportFolderName = null;
					OpenDevUnnamedPersonaSingleImportSelection(devPendingUnnamedSingleImportFolderName);
				}
			});
			break;
		case "export_unnamed":
			OpenExportFolderPicker("全量导出（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "import_unnamed":
			OpenImportFolderPicker("全量导入（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "edit_unnamed":
			OpenDevUnnamedPersonaIndexSelection();
			break;
		}
	}

	private void OpenDevUnnamedPersonaSingleExportSelection()
	{
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string text = (item.Key ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = (string.IsNullOrWhiteSpace(item.Label) ? text : item.Label);
					list.Add(new InquiryElement(text, text2 + " (Key=" + text + ")", null));
				}
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个未命名NPC - 选择条目", "请选择要导出的条目：", list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string k = selected[0].Identifier as string;
				if (k == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					k = (k ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(k))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						OpenFolderPickerWithCallback("导出（单个未命名NPC）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							ExportSingleUnnamedPersonaData(folderName, k);
						}, OpenDevUnnamedPersonaMenu);
					}
				}
			}
		}, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaSingleImportSelection(string folderName)
	{
		List<string> unnamedPersonaKeysFromImportFolderForDev = GetUnnamedPersonaKeysFromImportFolderForDev(folderName, 400);
		if (unnamedPersonaKeysFromImportFolderForDev == null || unnamedPersonaKeysFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in unnamedPersonaKeysFromImportFolderForDev)
		{
			string text = (item ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个未命名NPC - 选择条目", "请选择要导入的条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						ImportSingleUnnamedPersonaData(folderName, text2);
						OpenDevUnnamedPersonaMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaIndexSelection()
	{
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string title = (string.IsNullOrWhiteSpace(item.Label) ? item.Key : item.Label);
				list.Add(new InquiryElement(item.Key, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑未命名NPC - 选择条目", "选择一个条目进行编辑：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevUnnamedPersonaIndexSelected, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevUnnamedPersonaIndexSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevUnnamedPersonaMenu();
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "back")
		{
			OpenDevUnnamedPersonaMenu();
		}
		else
		{
			OpenDevUnnamedPersonaEdit(text);
		}
	}

	private void OpenDevUnnamedPersonaEdit(string key)
	{
		string k = (key ?? "").Trim();
		if (string.IsNullOrEmpty(k))
		{
			OpenDevUnnamedPersonaIndexSelection();
			return;
		}
		string personality = "";
		string background = "";
		try
		{
			ShoutUtils.TryGetUnnamedPersonaByKey(k, out personality, out background);
		}
		catch
		{
		}
		string text = (personality ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			text = (background ?? "").Trim();
		}
		string text2 = text.Replace("\r", "").Replace("\n", " ");
		if (text2.Length > 240)
		{
			text2 = text2.Substring(0, 240);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Key: " + k);
		stringBuilder.AppendLine("当前描述预览：");
		stringBuilder.AppendLine(string.IsNullOrWhiteSpace(text2) ? "（空）" : text2);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("请输入新的“描述”（留空=清空）：");
		InformationManager.ShowTextInquiry(new TextInquiryData("编辑未命名NPC - 描述", stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			string personality2 = (input ?? "").Trim();
			ShoutUtils.SaveUnnamedPersonaByKey(k, personality2, "");
			InformationManager.DisplayMessage(new InformationMessage("已保存：" + k));
			OpenDevUnnamedPersonaIndexSelection();
		}, delegate
		{
			OpenDevUnnamedPersonaIndexSelection();
		}));
	}

	private void OpenDevSingleNpcHeroSelection()
	{
		List<Hero> list = BuildDevEditableHeroList() ?? new List<Hero>();
		try
		{
			if (Hero.MainHero != null && !list.Contains(Hero.MainHero))
			{
				list.Insert(0, Hero.MainHero);
			}
		}
		catch
		{
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		list2.Add(new InquiryElement("pick_from_export", "从导出文件夹选择NPC…", null));
		list2.Add(new InquiryElement("manual_id", "手动输入 HeroId…", null));
		foreach (Hero item in list)
		{
			if (item != null)
			{
				string text = (item.StringId ?? "").Trim();
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = item.Name?.ToString() ?? "NPC";
					list2.Add(new InquiryElement(item, text2 + " (ID=" + text + ")", null));
				}
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - 选择NPC", "可从当前存档列表选择，也可从旧导出文件夹里读取 JSON 后选择。", list2, isExitShown: true, 0, 1, "进入", "返回", OnDevSingleNpcHeroSelected, delegate
		{
			OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevSingleNpcHeroSelectionFromExportFolder(string folderName)
	{
		string text = (folderName ?? "").Trim();
		string text2 = null;
		try
		{
			if (!string.IsNullOrEmpty(text) && Path.IsPathRooted(text) && Directory.Exists(text))
			{
				text2 = Path.GetFullPath(text);
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text2))
		{
			text2 = ResolveImportFolderPath(folderName);
		}
		if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到导出目录。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		Dictionary<string, string> idToName = new Dictionary<string, string>(StringComparer.Ordinal);
		List<string> list = new List<string>
		{
			Path.Combine(text2, "personality_background"),
			Path.Combine(text2, "dialogue_history"),
			Path.Combine(text2, "debt")
		};
		foreach (string item in list)
		{
			try
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] files = Directory.GetFiles(item, "*.json", SearchOption.TopDirectoryOnly);
				foreach (string text3 in files)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (string.IsNullOrWhiteSpace(text4))
					{
						continue;
					}
					string value = "";
					try
					{
						string text5 = Path.GetFileNameWithoutExtension(text3) ?? "";
						int num = text5.IndexOf("__", StringComparison.Ordinal);
						if (num >= 0 && num + 2 < text5.Length)
						{
							value = (text5.Substring(num + 2) ?? "").Trim();
						}
					}
					catch
					{
					}
					if (!idToName.ContainsKey(text4))
					{
						idToName[text4] = value;
					}
					else if (string.IsNullOrWhiteSpace(idToName[text4]) && !string.IsNullOrWhiteSpace(value))
					{
						idToName[text4] = value;
					}
				}
			}
			catch
			{
			}
		}
		if (idToName.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该导出文件夹中没有可识别的 NPC JSON。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KeyValuePair<string, string> item2 in idToName.OrderBy((KeyValuePair<string, string> k) => k.Key))
		{
			string text6 = (item2.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text6))
			{
				string text7 = (item2.Value ?? "").Trim();
				string title = (string.IsNullOrEmpty(text7) ? (text6 ?? "") : (text7 + " (ID=" + text6 + ")"));
				list2.Add(new InquiryElement(text6, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("从导出文件夹选择 NPC", "目录：\n" + text2 + "\n\n请选择要导入/导出的 NPC：", list2, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevSingleNpcHeroSelection();
			}
			else
			{
				string text8 = selected[0].Identifier as string;
				if (text8 == "back")
				{
					OpenDevSingleNpcHeroSelection();
				}
				else
				{
					text8 = (text8 ?? "").Trim();
					if (string.IsNullOrEmpty(text8))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						_devOpsHeroId = text8;
						string value2 = "";
						try
						{
							idToName.TryGetValue(text8, out value2);
						}
						catch
						{
							value2 = "";
						}
						if (string.IsNullOrWhiteSpace(value2))
						{
							value2 = text8;
						}
						_devOpsHeroName = value2;
						OpenDevSingleNpcOpsMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevSingleNpcHeroSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				OpenDevHeroNpcMenu();
				return;
			case "pick_from_export":
				OpenFolderPickerWithCallback("单个 HeroNPC - 选择导入文件夹", isExport: false, delegate(string folderName)
				{
					OpenDevSingleNpcHeroSelectionFromExportFolder(folderName);
				}, OpenDevSingleNpcHeroSelection);
				return;
			case "manual_id":
				InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 HeroId", "请输入 HeroId（例如：lord_... / wanderer_...）。\n\n说明：这里不会创建新的游戏角色，只是把导入/导出数据写入到该 HeroId 对应的存档数据里；只有当游戏里存在/将来出现同 ID 的 Hero 时，这些数据才会被使用。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
				{
					string manualHeroId = (input ?? "").Trim();
					if (string.IsNullOrEmpty(manualHeroId))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						_devOpsHeroId = manualHeroId;
						string devOpsHeroName = manualHeroId;
						try
						{
							string text3 = Hero.FindFirst((Hero x) => x != null && x.StringId == manualHeroId)?.Name?.ToString() ?? "";
							if (!string.IsNullOrWhiteSpace(text3))
							{
								devOpsHeroName = text3;
							}
						}
						catch
						{
						}
						_devOpsHeroName = devOpsHeroName;
						OpenDevSingleNpcOpsMenu();
					}
				}, delegate
				{
					OpenDevSingleNpcHeroSelection();
				}));
				return;
			}
		}
		if (!(selected[0].Identifier is Hero hero))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (hero.StringId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
			OpenDevSingleNpcHeroSelection();
		}
		else
		{
			_devOpsHeroId = text2;
			_devOpsHeroName = hero.Name?.ToString() ?? "NPC";
			OpenDevSingleNpcOpsMenu();
		}
	}

	private void OpenDevSingleNpcOpsMenu()
	{
		string text = (_devOpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (string.IsNullOrEmpty(_devOpsHeroName) ? "NPC" : _devOpsHeroName);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_persona", "导出（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("import_persona", "导入（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("export_history", "导出（对话历史，选择文件夹）", null));
		list.Add(new InquiryElement("import_history", "导入（对话历史，选择文件夹）", null));
		list.Add(new InquiryElement("export_debt", "导出（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("import_debt", "导入（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("change_hero", "切换NPC", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - " + text2 + " (ID=" + text + ")", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevSingleNpcOpsSelected, delegate
		{
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevSingleNpcOpsSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text = selected[0].Identifier as string;
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (_devOpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text3 = "单个 HeroNPC - " + (string.IsNullOrEmpty(_devOpsHeroName) ? "NPC" : _devOpsHeroName);
		switch (text)
		{
		case "back":
			OpenDevSingleNpcHeroSelection();
			break;
		case "change_hero":
			OpenDevSingleNpcHeroSelection();
			break;
		case "export_persona":
			OpenFolderPicker(text3 + " - 导出（个性/背景）", isExport: true, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_persona":
			OpenFolderPicker(text3 + " - 导入（个性/背景）", isExport: false, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_history":
			OpenFolderPicker(text3 + " - 导出（对话历史）", isExport: true, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_history":
			OpenFolderPicker(text3 + " - 导入（对话历史）", isExport: false, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_debt":
			OpenFolderPicker(text3 + " - 导出（欠款）", isExport: true, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_debt":
			OpenFolderPicker(text3 + " - 导入（欠款）", isExport: false, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		}
	}

	private static string GetVoiceGroupShortName(string key)
	{
		return (key ?? "").ToLowerInvariant() switch
		{
			"male_young" => "青男", 
			"male_middle" => "中男", 
			"male_old" => "老男", 
			"female_young" => "青女", 
			"female_middle" => "中女", 
			"female_old" => "老女", 
			_ => key ?? "", 
		};
	}

	private void OpenDevVoiceMappingMenu()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组数量概览（详细ID请进分组查看）");
		stringBuilder.AppendLine("------------------------------");
		int num = 0;
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			int num2 = VoiceMapper.GetVoicesForGroup(text)?.Count ?? 0;
			num += num2;
			stringBuilder.AppendLine($"  {GetVoiceGroupShortName(text)}: {num2}");
		}
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		stringBuilder.AppendLine("------------------------------");
		stringBuilder.AppendLine("兜底: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "未设置" : fallbackVoice));
		stringBuilder.AppendLine($"总ID: {num}");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add_voice", "添加声音ID到分组", null));
		list.Add(new InquiryElement("remove_voice", "删除分组中的声音ID", null));
		list.Add(new InquiryElement("set_fallback", "设置全局兜底声音(fallback)", null));
		list.Add(new InquiryElement("export_voice_map", "导出映射（选文件夹）", null));
		list.Add(new InquiryElement("import_voice_map", "导入映射（选文件夹）", null));
		list.Add(new InquiryElement("reload", "重新加载配置文件", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("声音映射管理", stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", OnDevVoiceMappingMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevVoiceMappingMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "add_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: true);
				break;
			case "remove_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: false);
				break;
			case "set_fallback":
				OpenDevVoiceMappingSetFallback();
				break;
			case "export_voice_map":
				OpenExportFolderPicker("导出（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "import_voice_map":
				OpenImportFolderPicker("导入（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "reload":
				VoiceMapper.ReloadConfig();
				InformationManager.DisplayMessage(new InformationMessage("已重新加载 VoiceMapping.json"));
				OpenDevVoiceMappingMenu();
				break;
			}
		}
	}

	private void OpenDevVoiceMappingSelectGroup(bool isAdd)
	{
		string titleText = (isAdd ? "添加声音ID - 选择分组" : "删除声音ID - 选择分组");
		string descriptionText = (isAdd ? "选择分组后输入要添加的声音ID。" : "选择要删除声音ID的分组：");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(text);
			string title = $"{GetVoiceGroupShortName(text)} ({voicesForGroup.Count})";
			if (isAdd || voicesForGroup.Count != 0)
			{
				list.Add(new InquiryElement(text, title, null));
			}
		}
		if (!isAdd && list.Count <= 1)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有任何声音ID可删除。"));
			OpenDevVoiceMappingMenu();
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevVoiceMappingMenu();
				}
				else if (isAdd)
				{
					OpenDevVoiceMappingAddVoice(text2);
				}
				else
				{
					OpenDevVoiceMappingRemoveVoice(text2);
				}
			}
		}, delegate
		{
			OpenDevVoiceMappingMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevVoiceMappingAddVoice(string groupKey)
	{
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组: " + displayName);
		stringBuilder.AppendLine($"当前数量: {voicesForGroup.Count}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("请输入要添加的声音ID：");
		stringBuilder.AppendLine("（与MCM中的TTS声音ID一致）");
		InformationManager.ShowTextInquiry(new TextInquiryData("添加声音ID - " + displayName, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "添加", "返回", delegate(string input)
		{
			string text = (input ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				OpenDevVoiceMappingSelectGroup(isAdd: true);
			}
			else
			{
				VoiceMapper.AddVoiceToGroup(groupKey, text);
				InformationManager.DisplayMessage(new InformationMessage("已添加声音 \"" + text + "\" 到 [" + displayName + "]"));
				OpenDevVoiceMappingMenu();
			}
		}, delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: true);
		}));
	}

	private void OpenDevVoiceMappingRemoveVoice(string groupKey)
	{
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		if (voicesForGroup.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("[" + displayName + "] 分组没有声音ID可删除。"));
			OpenDevVoiceMappingSelectGroup(isAdd: false);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("clear_all", "⚠ 清空该分组所有声音", null));
		for (int i = 0; i < voicesForGroup.Count; i++)
		{
			list.Add(new InquiryElement(voicesForGroup[i], $"{i + 1}. {voicesForGroup[i]}", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除声音ID - " + displayName, $"分组: {displayName}\n当前共 {voicesForGroup.Count} 个声音ID。\n选择要删除的声音：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingSelectGroup(isAdd: false);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "back")
				{
					OpenDevVoiceMappingSelectGroup(isAdd: false);
				}
				else if (text == "clear_all")
				{
					List<string> voicesForGroup2 = VoiceMapper.GetVoicesForGroup(groupKey);
					foreach (string item in voicesForGroup2)
					{
						VoiceMapper.RemoveVoiceFromGroup(groupKey, item);
					}
					InformationManager.DisplayMessage(new InformationMessage("已清空 [" + displayName + "] 的所有声音ID"));
					OpenDevVoiceMappingMenu();
				}
				else
				{
					VoiceMapper.RemoveVoiceFromGroup(groupKey, text);
					InformationManager.DisplayMessage(new InformationMessage("已删除声音 \"" + text + "\" from [" + displayName + "]"));
					List<string> voicesForGroup3 = VoiceMapper.GetVoicesForGroup(groupKey);
					if (voicesForGroup3.Count > 0)
					{
						OpenDevVoiceMappingRemoveVoice(groupKey);
					}
					else
					{
						OpenDevVoiceMappingMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: false);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevVoiceMappingSetFallback()
	{
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("全局兜底声音(fallback)：当某个分组没有配置声音ID时，使用此声音。");
		stringBuilder.AppendLine("当前: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "（未设置，最终回退到MCM全局设置）" : fallbackVoice));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("输入新的 fallback 声音ID（留空=清除）：");
		InformationManager.ShowTextInquiry(new TextInquiryData("设置全局兜底声音(fallback)", stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			string text = (input ?? "").Trim();
			VoiceMapper.SetFallbackVoice(text);
			string information = (string.IsNullOrWhiteSpace(text) ? "fallback 已清除" : ("fallback 已设置为: " + text));
			InformationManager.DisplayMessage(new InformationMessage(information));
			OpenDevVoiceMappingMenu();
		}, delegate
		{
			OpenDevVoiceMappingMenu();
		}, shouldInputBeObfuscated: false, null, fallbackVoice ?? ""));
	}

	private void ReturnToDevRootMenu()
	{
		try
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
		catch
		{
		}
	}

	private void OpenExportFolderPicker(string title, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport: true, scope, ReturnToDevRootMenu, null);
	}

	private void OpenExportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		OpenFolderPicker(title, isExport: true, scope, onReturn, null);
	}

	private void OpenImportFolderPicker(string title, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport: false, scope, ReturnToDevRootMenu, null);
	}

	private void OpenImportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		OpenFolderPicker(title, isExport: false, scope, onReturn, null);
	}

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport, scope, ReturnToDevRootMenu, null);
	}

	private void OpenFolderPickerWithCallback(string title, bool isExport, Action<string> onSelectedFolder, Action onReturn)
	{
		if (onSelectedFolder == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = ReturnToDevRootMenu;
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		Directory.CreateDirectory(playerExportsRootPath);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", isExport ? "手动输入文件夹名…" : "手动输入文件夹名/路径…", null));
		if (!isExport)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择目标文件夹（可覆盖已有）。" : "选择来源文件夹。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名/路径", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出；也可输入完整路径（文件夹或 .json 文件）。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						onSelectedFolder(input);
						onReturn();
					}, delegate
					{
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					onSelectedFolder("");
					onReturn();
				}
				else
				{
					onSelectedFolder(text);
					onReturn();
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void ShowDuplicateImportInquiry(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel)
	{
		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
				};
			}
			if (onSkipDuplicates == null)
			{
				onSkipDuplicates = delegate
				{
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", "覆盖导入", null),
				new InquiryElement("__skip__", "只导入非重复信息", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
			{
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__skip__")
				{
					onSkipDuplicates();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}

	private static bool IsDirectoryNonEmpty(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir))
			{
				return false;
			}
			if (!Directory.Exists(dir))
			{
				return false;
			}
			return Directory.EnumerateFileSystemEntries(dir).Any();
		}
		catch
		{
			return false;
		}
	}

	private void ShowOverwriteExportInquiry(string title, string text, Action onOverwrite, Action onNewFolder, Action onCancel)
	{
		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
				};
			}
			if (onNewFolder == null)
			{
				onNewFolder = delegate
				{
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", "覆盖导出", null),
				new InquiryElement("__new__", "改用新文件夹（自动）", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
			{
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__new__")
				{
					onNewFolder();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope, Action onReturn, string heroId)
	{
		if (onReturn == null)
		{
			onReturn = ReturnToDevRootMenu;
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		Directory.CreateDirectory(playerExportsRootPath);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", "手动输入文件夹名…", null));
		if (!isExport)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择要导出的目标文件夹（可覆盖已有）。" : "选择要导入的来源文件夹。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						if (string.IsNullOrEmpty(heroId))
						{
							ResolveAndRunExportImport(isExport, scope, input);
						}
						else
						{
							ResolveAndRunExportImportForHero(isExport, scope, input, heroId);
						}
						onReturn();
					}, delegate
					{
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport: false, scope, "");
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport: false, scope, "", heroId);
					}
					onReturn();
				}
				else
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport, scope, text);
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport, scope, text, heroId);
					}
					onReturn();
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void ResolveAndRunExportImport(bool isExport, ExportImportScope scope, string folderName)
	{
		if (isExport)
		{
			Action run = delegate
			{
				if (scope == ExportImportScope.All)
				{
					ExportAllData(folderName);
				}
				else if (scope == ExportImportScope.HeroNpcAll)
				{
					ExportHeroNpcAllData(folderName);
				}
				else if (scope == ExportImportScope.PersonalityBackground)
				{
					ExportPersonaData(folderName);
				}
				else if (scope == ExportImportScope.UnnamedPersona)
				{
					ExportUnnamedPersonaData(folderName);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					ExportDialogueHistoryData(folderName);
				}
				else if (scope == ExportImportScope.Debt)
				{
					ExportDebtData(folderName);
				}
				else if (scope == ExportImportScope.Knowledge)
				{
					ExportKnowledgeData(folderName);
				}
				else if (scope == ExportImportScope.VoiceMapping)
				{
					ExportVoiceMappingData(folderName);
				}
			};
			string value = SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath = GetPlayerExportsRootPath();
				Directory.CreateDirectory(playerExportsRootPath);
				string path = ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
					{
						run();
					}, delegate
					{
						ResolveAndRunExportImport(isExport: true, scope, "");
					}, delegate
					{
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.All)
		{
			ImportAllData(folderName);
		}
		else if (scope == ExportImportScope.HeroNpcAll)
		{
			ImportHeroNpcAllData(folderName);
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			ImportPersonaData(folderName);
		}
		else if (scope == ExportImportScope.UnnamedPersona)
		{
			ImportUnnamedPersonaData(folderName);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			ImportDialogueHistoryData(folderName);
		}
		else if (scope == ExportImportScope.Debt)
		{
			ImportDebtData(folderName);
		}
		else if (scope == ExportImportScope.Knowledge)
		{
			ImportKnowledgeData(folderName);
		}
		else if (scope == ExportImportScope.VoiceMapping)
		{
			ImportVoiceMappingData(folderName);
		}
	}

	private void ResolveAndRunExportImportForHero(bool isExport, ExportImportScope scope, string folderName, string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
		}
		else if (isExport)
		{
			Action run = delegate
			{
				if (scope == ExportImportScope.PersonalityBackground)
				{
					ExportSingleNpcPersonaData(folderName, id);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					ExportSingleNpcDialogueHistoryData(folderName, id);
				}
				else if (scope == ExportImportScope.Debt)
				{
					ExportSingleNpcDebtData(folderName, id);
				}
			};
			string value = SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath = GetPlayerExportsRootPath();
				Directory.CreateDirectory(playerExportsRootPath);
				string path = ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
					{
						run();
					}, delegate
					{
						ResolveAndRunExportImportForHero(isExport: true, scope, "", id);
					}, delegate
					{
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			ImportSingleNpcPersonaData(folderName, id);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			ImportSingleNpcDialogueHistoryData(folderName, id);
		}
		else if (scope == ExportImportScope.Debt)
		{
			ImportSingleNpcDebtData(folderName, id);
		}
	}

	private void ExportSingleNpcPersonaData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			NpcPersonaProfile value = null;
			if (_npcPersonaProfiles != null)
			{
				_npcPersonaProfiles.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new NpcPersonaProfile();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text2);
			List<DialogueDay> value = null;
			if (_dialogueHistory != null)
			{
				_dialogueHistory.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new List<DialogueDay>();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			RewardSystemBehavior.DebtExportEntry value = null;
			(RewardSystemBehavior.Instance?.ExportDebtEntries())?.TryGetValue(heroId, out value);
			if (value == null)
			{
				value = new RewardSystemBehavior.DebtExportEntry();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "debt");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "debt", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
					return;
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> all = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				all.Remove(heroId);
				rs.ImportDebtEntries(all);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				return;
			}
			RewardSystemBehavior.DebtExportEntry entry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text3);
			bool flag = entry != null;
			bool flag2 = all.ContainsKey(heroId);
			Action action = delegate
			{
				if (entry == null)
				{
					all.Remove(heroId);
				}
				else
				{
					all[heroId] = entry;
				}
				rs.ImportDebtEntries(all);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "检测到该 NPC 已存在欠款记录：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcPersonaData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "personality_background");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "personality_background", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			NpcPersonaProfile prof = ReadJson<NpcPersonaProfile>(text3);
			if (_npcPersonaProfiles == null)
			{
				_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
			}
			bool flag = prof != null;
			bool flag2 = false;
			try
			{
				flag2 = _npcPersonaProfiles.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (prof == null)
				{
					_npcPersonaProfiles.Remove(heroId);
				}
				else
				{
					_npcPersonaProfiles[heroId] = prof;
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "检测到该 NPC 已存在个性/背景：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "dialogue_history");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "dialogue_history", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 dialogue_history 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			bool flag = list != null;
			bool flag2 = false;
			try
			{
				flag2 = _dialogueHistory.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (list == null)
				{
					_dialogueHistory.Remove(heroId);
				}
				else
				{
					_dialogueHistory[heroId] = list;
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 对话历史", "检测到该 NPC 已存在对话历史：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportHeroNpcAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			ClearJsonFiles(text3);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, BuildNpcDataFileName(item.Key));
						WriteJson(path3, item.Value);
					}
				}
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			ClearJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, BuildNpcDataFileName(item2.Key));
						WriteJson(path4, item2.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportHeroNpcAllData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(importDir, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, List<DialogueDay>> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					string text2 = TryParseHeroIdFromNpcFileName(text);
					if (!string.IsNullOrEmpty(text2))
					{
						NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							pbNew[text2] = npcPersonaProfile;
						}
					}
				}
				num2 = pbNew.Count;
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_dialogueHistory != null)
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _dialogueHistory.ContainsKey(key2))
						{
							num3++;
						}
					}
				}
			}
			string path3 = Path.Combine(importDir, "debt");
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = TryParseHeroIdFromNpcFileName(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (_npcPersonaProfiles == null)
						{
							_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
						}
						foreach (KeyValuePair<string, NpcPersonaProfile> item in pbNew)
						{
							if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
							{
								_npcPersonaProfiles.Remove(item.Key);
								_npcPersonaProfiles[item.Key] = item.Value;
							}
						}
					}
					if (dhNew != null)
					{
						if (_dialogueHistory == null)
						{
							_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
						}
						foreach (KeyValuePair<string, List<DialogueDay>> item2 in dhNew)
						{
							if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
							{
								_dialogueHistory.Remove(item2.Key);
								_dialogueHistory[item2.Key] = item2.Value;
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
						foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item3 in debtNew)
						{
							if (!string.IsNullOrEmpty(item3.Key) && item3.Value != null)
							{
								dictionary.Remove(item3.Key);
								dictionary[item3.Key] = item3.Value;
							}
						}
						rs.ImportDebtEntries(dictionary);
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (_npcPersonaProfiles == null)
						{
							_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
						}
						foreach (KeyValuePair<string, NpcPersonaProfile> item4 in pbNew)
						{
							if (!string.IsNullOrEmpty(item4.Key) && item4.Value != null && !_npcPersonaProfiles.ContainsKey(item4.Key))
							{
								_npcPersonaProfiles[item4.Key] = item4.Value;
							}
						}
					}
					if (dhNew != null)
					{
						if (_dialogueHistory == null)
						{
							_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
						}
						foreach (KeyValuePair<string, List<DialogueDay>> item5 in dhNew)
						{
							if (!string.IsNullOrEmpty(item5.Key) && item5.Value != null && !_dialogueHistory.ContainsKey(item5.Key))
							{
								_dialogueHistory[item5.Key] = item5.Value;
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
						foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item6 in debtNew)
						{
							if (!string.IsNullOrEmpty(item6.Key) && item6.Value != null && !dictionary.ContainsKey(item6.Key))
							{
								dictionary[item6.Key] = item6.Value;
							}
						}
						rs.ImportDebtEntries(dictionary);
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				}
			};
			if (flag)
			{
				string text7 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - HeroNPC 全量导入", text7, action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			ClearJsonFiles(text3);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, BuildNpcDataFileName(item.Key));
						WriteJson(path3, item.Value);
					}
				}
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			ClearJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, BuildNpcDataFileName(item2.Key));
						WriteJson(path4, item2.Value);
					}
				}
			}
			ExportKnowledgeToDir(text);
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			string text5 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text5);
			ClearJsonFiles(text5);
			string path5 = Path.Combine(text5, "VoiceMapping.json");
			string text6 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "{}";
			}
			File.WriteAllText(path5, text6, Encoding.UTF8);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportUnnamedPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDialogueHistoryData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(item.Key));
						WriteJson(path2, item.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDebtData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dictionary)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(item.Key));
						WriteJson(path2, item.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportKnowledgeData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			ExportKnowledgeToDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportKnowledgeToDir(string exportDir)
	{
		try
		{
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior == null)
				{
					return;
				}
				string value = knowledgeLibraryBehavior.ExportRulesJson();
				KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value));
				if (knowledgeFile?.Rules == null || knowledgeFile.Rules.Count <= 0)
				{
					return;
				}
				string text = Path.Combine(exportDir, "knowledge", "rules");
				Directory.CreateDirectory(text);
				ClearJsonFiles(text);
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text2 = (rule?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text2) || !hashSet.Add(text2))
					{
						continue;
					}
					rule.Id = text2;
					string text3 = "";
					try
					{
						text3 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
					}
					catch
					{
						text3 = "";
					}
					string text4 = text2;
					char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
					foreach (char oldChar in invalidFileNameChars)
					{
						text4 = text4.Replace(oldChar, '_');
					}
					text4 = (text4 ?? "").Trim();
					if (text4.Length > 80)
					{
						text4 = text4.Substring(0, 80);
					}
					if (string.IsNullOrEmpty(text4))
					{
						text4 = "rule";
					}
					string text5 = text3;
					char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
					foreach (char oldChar2 in invalidFileNameChars2)
					{
						text5 = text5.Replace(oldChar2, '_');
					}
					text5 = (text5 ?? "").Trim();
					if (text5.Length > 35)
					{
						text5 = text5.Substring(0, 35);
					}
					string text6 = text4;
					if (!string.IsNullOrEmpty(text5))
					{
						text6 = text6 + "__" + text5;
					}
					string path = Path.Combine(text, text6 + ".json");
					if (File.Exists(path))
					{
						for (int num3 = 2; num3 <= 999; num3++)
						{
							string text7 = Path.Combine(text, text6 + "__" + num3 + ".json");
							if (!File.Exists(text7))
							{
								path = text7;
								break;
							}
						}
					}
					string contents = JsonConvert.SerializeObject(rule, Formatting.Indented);
					File.WriteAllText(path, contents, Encoding.UTF8);
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void ExportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：RuleId 为空。"));
				return;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			string text2 = knowledgeLibraryBehavior.ExportSingleRuleJson(text, pretty: true);
			if (string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该知识条目：" + text));
				return;
			}
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text3 = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text3);
			string text4 = Path.Combine(text3, "knowledge", "rules");
			Directory.CreateDirectory(text4);
			KnowledgeLibraryBehavior.LoreRule loreRule = null;
			try
			{
				loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2);
			}
			catch
			{
				loreRule = null;
			}
			string text5 = "";
			try
			{
				text5 = loreRule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text5 = "";
			}
			string text6 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text6 = text6.Replace(oldChar, '_');
			}
			text6 = (text6 ?? "").Trim();
			if (text6.Length > 80)
			{
				text6 = text6.Substring(0, 80);
			}
			if (string.IsNullOrEmpty(text6))
			{
				text6 = "rule";
			}
			string text7 = text5;
			char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
			foreach (char oldChar2 in invalidFileNameChars2)
			{
				text7 = text7.Replace(oldChar2, '_');
			}
			text7 = (text7 ?? "").Trim();
			if (text7.Length > 35)
			{
				text7 = text7.Substring(0, 35);
			}
			string text8 = text6;
			if (!string.IsNullOrEmpty(text7))
			{
				text8 = text8 + "__" + text7;
			}
			string path2 = Path.Combine(text4, text8 + ".json");
			File.WriteAllText(path2, text2, Encoding.UTF8);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text3));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RuleId 为空。"));
				return;
			}
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			KnowledgeLibraryBehavior kb = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (kb == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			string dir = Path.Combine(importDir, "knowledge", "rules");
			string dir2 = Path.Combine(importDir, "knowledge", "single_rules");
			string dir3 = Path.Combine(importDir, "knowledge");
			string text = FindKnowledgeRuleJsonById(dir, id);
			if (string.IsNullOrEmpty(text))
			{
				text = FindKnowledgeRuleJsonById(dir2, id);
			}
			if (string.IsNullOrEmpty(text))
			{
				text = FindKnowledgeRuleJsonById(dir3, id);
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该知识条目的导出文件：" + id));
				return;
			}
			string value = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件为空。"));
				return;
			}
			KnowledgeLibraryBehavior.LoreRule rule = null;
			try
			{
				rule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
			}
			catch
			{
				rule = null;
			}
			if (rule == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text2 = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, id, StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件中的 Id 不匹配（文件Id=" + text2 + "）。"));
				return;
			}
			if (string.IsNullOrEmpty(text2))
			{
				rule.Id = id;
			}
			bool flag = false;
			try
			{
				flag = !string.IsNullOrWhiteSpace(kb.ExportSingleRuleJson(id));
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None)))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：写入规则失败。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None), overwrite: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + id));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "检测到相同 RuleId：" + id + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private static string NormalizeKeywordForCompare(string keyword)
	{
		try
		{
			string text = (keyword ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			foreach (char c in text)
			{
				if (char.IsWhiteSpace(c))
				{
					if (!flag)
					{
						stringBuilder.Append(' ');
					}
					flag = true;
				}
				else
				{
					stringBuilder.Append(c);
					flag = false;
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)
	{
		if (rule?.Keywords == null || rule.Keywords.Count <= 0)
		{
			yield break;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string k in rule.Keywords)
		{
			string kk = NormalizeKeywordForCompare(k);
			if (!string.IsNullOrEmpty(kk) && seen.Add(kk))
			{
				yield return kk;
			}
		}
	}

	private static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string importDir)
	{
		try
		{
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text = (rule?.Id ?? "").Trim();
					if (!string.IsNullOrEmpty(text))
					{
						rule.Id = text;
						if (hashSet.Add(text))
						{
							list.Add(rule);
						}
					}
				}
			}
			string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
			if (File.Exists(path))
			{
				try
				{
					string value = File.ReadAllText(path, Encoding.UTF8);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
					if (knowledgeFile2?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
						{
							string text2 = (rule2?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2))
							{
								rule2.Id = text2;
								if (hashSet.Add(text2))
								{
									list.Add(rule2);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			return list;
		}
		catch
		{
			return new List<KnowledgeLibraryBehavior.LoreRule>();
		}
	}

	private static bool ValidateKnowledgeKeywordsForSingleRuleImport(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			if (kb == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (rule == null)
			{
				error = "导入失败：规则为空。";
				return false;
			}
			string text = (rule.Id ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				error = "导入失败：RuleId 为空。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					string text2 = NormalizeKeywordForCompare(keyword);
					if (!string.IsNullOrEmpty(text2))
					{
						if (dictionary.ContainsKey(text2))
						{
							error = "导入失败：导入规则中存在重复关键词（" + text2 + "）。";
							return false;
						}
						dictionary[text2] = text;
					}
				}
			}
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value = kb.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile.Rules)
				{
					string text3 = (rule2?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text3) || (overwriteExisting && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					foreach (string item in GetKnowledgeKeywordsForCompare(rule2))
					{
						if (dictionary2.TryGetValue(item, out var value2) && !string.Equals(value2, text3, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item] = text3;
					}
				}
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				if (dictionary2.TryGetValue(item2.Key, out var value3) && !string.Equals(value3, text, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item2.Key + "）。当前存档中该关键词属于规则 " + value3 + "，导入规则为 " + text + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	private static bool ValidateKnowledgeKeywordsForImport(string importDir, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			List<KnowledgeLibraryBehavior.LoreRule> list = LoadKnowledgeRulesFromImportDir(importDir);
			if (list == null || list.Count <= 0)
			{
				error = "导入失败：导入目录中未找到可导入的知识规则文件。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item in list)
			{
				string text = (item?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				foreach (string item2 in GetKnowledgeKeywordsForCompare(item))
				{
					if (dictionary.TryGetValue(item2, out var value) && !string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
					{
						error = "导入失败：导入文件夹中存在重复关键词（" + item2 + "），分别属于规则 " + value + " 与 " + text + "。";
						return false;
					}
					dictionary[item2] = text;
				}
			}
			HashSet<string> existingIds = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			List<KnowledgeLibraryBehavior.LoreRule> list2 = list;
			if (!overwriteExisting)
			{
				list2 = list.Where((KnowledgeLibraryBehavior.LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id) && !existingIds.Contains((r.Id ?? "").Trim())).ToList();
			}
			HashSet<string> hashSet = (overwriteExisting ? new HashSet<string>(from r in list
				where r != null
				select (r.Id ?? "").Trim() into x
				where !string.IsNullOrEmpty(x)
				select x, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value2 = knowledgeLibraryBehavior.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value2))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value2);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text2 = (rule?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text2) || hashSet.Contains(text2))
					{
						continue;
					}
					foreach (string item3 in GetKnowledgeKeywordsForCompare(rule))
					{
						if (dictionary2.TryGetValue(item3, out var value3) && !string.Equals(value3, text2, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item3 + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item3] = text2;
					}
				}
			}
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item4 in list2)
			{
				string value4 = (item4?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(value4))
				{
					continue;
				}
				foreach (string item5 in GetKnowledgeKeywordsForCompare(item4))
				{
					dictionary3[item5] = value4;
				}
			}
			foreach (KeyValuePair<string, string> item6 in dictionary3)
			{
				if (dictionary2.TryGetValue(item6.Key, out var value5) && !string.Equals(value5, item6.Value, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item6.Key + "）。当前存档中该关键词属于规则 " + value5 + "，导入规则为 " + item6.Value + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	private static KnowledgeLibraryBehavior.KnowledgeFile TryLoadKnowledgeRulesFromRuleFiles(string importDir)
	{
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				return null;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "knowledge", "rules"),
				Path.Combine(importDir, "knowledge", "single_rules"),
				Path.Combine(importDir, "knowledge")
			};
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					try
					{
						string a = Path.GetFileName(path) ?? "";
						if (string.Equals(a, "AIConfig.json", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "KnowledgeRules.json", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (string.IsNullOrWhiteSpace(value))
						{
							continue;
						}
						KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
						string text = (loreRule?.Id ?? "").Trim();
						if (!string.IsNullOrEmpty(text))
						{
							loreRule.Id = text;
							if (hashSet.Add(text))
							{
								list.Add(loreRule);
							}
						}
					}
					catch
					{
					}
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return new KnowledgeLibraryBehavior.KnowledgeFile
			{
				Version = 1,
				Rules = list
			};
		}
		catch
		{
			return null;
		}
	}

	private static string FindKnowledgeRuleJsonById(string dir, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
					if (loreRule != null)
					{
						string a = (loreRule.Id ?? "").Trim();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private string TryGetUnnamedPersonaKeyFromImportFile(string file)
	{
		string text = null;
		try
		{
			text = (ReadJson<UnnamedPersonaSingleJson>(file)?.Key ?? "").Trim().ToLower();
		}
		catch
		{
			text = null;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				string text2 = Path.GetFileNameWithoutExtension(file) ?? "";
				int num = text2.LastIndexOf("__", StringComparison.Ordinal);
				if (num > 0)
				{
					text2 = text2.Substring(0, num);
				}
				text = (text2 ?? "").Trim().ToLower();
			}
			catch
			{
				text = null;
			}
		}
		return (text ?? "").Trim().ToLower();
	}

	private bool ValidateUnnamedPersonaKeysForImport(string importDir, out string error)
	{
		error = "";
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				error = "导入失败：找不到导出目录。";
				return false;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "unnamed_persona"),
				importDir
			};
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string file in array2)
				{
					string text = TryGetUnnamedPersonaKeyFromImportFile(file);
					if (!string.IsNullOrEmpty(text))
					{
						if (!hashSet.Add(text))
						{
							error = "导入失败：导入文件夹中存在重复 Key（" + text + "）。";
							return false;
						}
						if (ShoutUtils.HasUnnamedPersonaKey(text))
						{
							error = "导入失败：Key 冲突（" + text + "）。当前游戏已存在该 Key，禁止导入覆盖。";
							return false;
						}
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：Key 校验异常：" + ex.Message;
			return false;
		}
	}

	private void ExportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：Key 为空。"));
				return;
			}
			string personality = "";
			string background = "";
			bool flag = false;
			try
			{
				flag = ShoutUtils.TryGetUnnamedPersonaByKey(text, out personality, out background);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该未命名NPC条目：" + text));
				return;
			}
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text2 = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text2);
			string text3 = Path.Combine(text2, "unnamed_persona");
			Directory.CreateDirectory(text3);
			string text4 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text4 = text4.Replace(oldChar, '_');
			}
			text4 = text4.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text4.Contains("__"))
			{
				text4 = text4.Replace("__", "_");
			}
			text4 = (text4 ?? "").Trim();
			if (text4.Length > 120)
			{
				text4 = text4.Substring(0, 120);
			}
			if (string.IsNullOrEmpty(text4))
			{
				text4 = "unnamed";
			}
			string path2 = Path.Combine(text3, text4 + ".json");
			WriteJson(path2, new UnnamedPersonaSingleJson
			{
				Key = text,
				Personality = (personality ?? "").Trim(),
				Background = (background ?? "").Trim()
			});
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：Key 为空。"));
				return;
			}
			string text2 = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string dir = Path.Combine(text2, "unnamed_persona");
			string dir2 = text2;
			string text3 = FindUnnamedPersonaJsonByKey(dir, text);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindUnnamedPersonaJsonByKey(dir2, text);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该未命名NPC条目的导出文件：" + text));
				return;
			}
			UnnamedPersonaSingleJson unnamedPersonaSingleJson = ReadJson<UnnamedPersonaSingleJson>(text3);
			if (unnamedPersonaSingleJson == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text4 = (unnamedPersonaSingleJson.Personality ?? "").Trim();
			string text5 = (unnamedPersonaSingleJson.Background ?? "").Trim();
			bool flag = !string.IsNullOrEmpty(text4) || !string.IsNullOrEmpty(text5);
			bool flag2 = false;
			try
			{
				flag2 = ShoutUtils.HasUnnamedPersonaKey(text);
			}
			catch
			{
				flag2 = false;
			}
			if (flag && flag2)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：检测到重复 Key（" + text + "），当前游戏已存在该条目，禁止导入覆盖。"));
				return;
			}
			ShoutUtils.SaveUnnamedPersonaByKey(text, text4, text5);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private static string FindUnnamedPersonaJsonByKey(string dir, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = text2.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text2.Contains("__"))
			{
				text2 = text2.Replace("__", "_");
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					UnnamedPersonaSingleJson unnamedPersonaSingleJson = JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value);
					if (unnamedPersonaSingleJson != null)
					{
						string a = (unnamedPersonaSingleJson.Key ?? "").Trim().ToLower();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private List<string> GetKnowledgeRuleIdsFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "knowledge", "rules"),
				Path.Combine(text, "knowledge", "single_rules"),
				Path.Combine(text, "knowledge")
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value)?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private List<string> GetUnnamedPersonaKeysFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "unnamed_persona"),
				text
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value)?.Key ?? "").Trim().ToLower();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private void ImportPersonaData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "personality_background");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, NpcPersonaProfile> dict = new Dictionary<string, NpcPersonaProfile>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
					if (npcPersonaProfile != null)
					{
						dict[text2] = npcPersonaProfile;
					}
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (_npcPersonaProfiles == null)
				{
					_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, NpcPersonaProfile> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						_npcPersonaProfiles.Remove(item.Key);
						_npcPersonaProfiles[item.Key] = item.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (_npcPersonaProfiles == null)
				{
					_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, NpcPersonaProfile> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !_npcPersonaProfiles.ContainsKey(item2.Key))
					{
						_npcPersonaProfiles[item2.Key] = item2.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDialogueHistoryData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "dialogue_history");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 dialogue_history 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, List<DialogueDay>> dict = new Dictionary<string, List<DialogueDay>>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					List<DialogueDay> list = ReadJson<List<DialogueDay>>(text);
					if (list != null)
					{
						dict[text2] = list;
					}
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_dialogueHistory != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _dialogueHistory.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (_dialogueHistory == null)
				{
					_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
				}
				foreach (KeyValuePair<string, List<DialogueDay>> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						_dialogueHistory.Remove(item.Key);
						_dialogueHistory[item.Key] = item.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (_dialogueHistory == null)
				{
					_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
				}
				foreach (KeyValuePair<string, List<DialogueDay>> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !_dialogueHistory.ContainsKey(item2.Key))
					{
						_dialogueHistory[item2.Key] = item2.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 对话历史", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDebtData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "debt");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
				return;
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dict = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text);
					if (debtExportEntry != null)
					{
						dict[text2] = debtExportEntry;
					}
				}
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> exist = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					if (!string.IsNullOrEmpty(key) && exist.ContainsKey(key))
					{
						num++;
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = exist ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						dictionary.Remove(item.Key);
						dictionary[item.Key] = item.Value;
					}
				}
				rs.ImportDebtEntries(dictionary);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = exist;
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !dictionary.ContainsKey(item2.Key))
					{
						dictionary[item2.Key] = item2.Value;
					}
				}
				rs.ImportDebtEntries(dictionary);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportUnnamedPersonaData(string folderName)
	{
		try
		{
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			ShoutUtils.ImportUnnamedPersonaFromDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportKnowledgeData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			int num = 0;
			int num2 = 0;
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text))
							{
								hashSet2.Add(text);
							}
						}
					}
					string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet2.Count <= 0 && File.Exists(path))
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text2 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text2))
								{
									hashSet2.Add(text2);
								}
							}
						}
					}
					num2 = hashSet2.Count;
					foreach (string item in hashSet2)
					{
						if (hashSet.Contains(item))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
				num2 = 0;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				}
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "导入的 Knowledge 规则与当前游戏存在重复 RuleId。\n重复：" + num + " / 总计：" + num2 + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportVoiceMappingData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			string path2 = Path.Combine(text2, "VoiceMapping.json");
			string text3 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "{}";
			}
			File.WriteAllText(path2, text3, Encoding.UTF8);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportVoiceMappingData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string text = null;
			string text2 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
			string text3 = Path.Combine(importDir, "VoiceMapping.json");
			if (File.Exists(text2))
			{
				text = text2;
			}
			else if (File.Exists(text3))
			{
				text = text3;
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 voice_mapping/VoiceMapping.json。"));
				return;
			}
			string json = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(json))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：VoiceMapping.json 为空。"));
				return;
			}
			bool flag = false;
			try
			{
				flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage(VoiceMapper.ImportMappingJson(json) ? ("导入完成：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage(VoiceMapper.ImportMappingJson(json, overwriteExisting: false) ? ("导入完成（已合并）：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - VoiceMapping", "当前已存在声音映射。\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportAllData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, List<DialogueDay>> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			string vmJson = null;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					string text2 = TryParseHeroIdFromNpcFileName(text);
					if (!string.IsNullOrEmpty(text2))
					{
						NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							pbNew[text2] = npcPersonaProfile;
						}
					}
				}
				num2 = pbNew.Count;
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_dialogueHistory != null)
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _dialogueHistory.ContainsKey(key2))
						{
							num3++;
						}
					}
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			string path3 = Path.Combine(importDir, "debt");
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = TryParseHeroIdFromNpcFileName(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
			try
			{
				List<string> source = new List<string>
				{
					Path.Combine(importDir, "unnamed_persona"),
					importDir
				};
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string item in source.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					if (!Directory.Exists(item))
					{
						continue;
					}
					string[] array4 = null;
					try
					{
						array4 = Directory.GetFiles(item, "*.json");
					}
					catch
					{
						array4 = null;
					}
					if (array4 == null)
					{
						continue;
					}
					string[] array5 = array4;
					foreach (string path4 in array5)
					{
						string text7 = null;
						try
						{
							text7 = (ReadJson<UnnamedPersonaSingleJson>(path4)?.Key ?? "").Trim().ToLower();
						}
						catch
						{
							text7 = null;
						}
						if (string.IsNullOrWhiteSpace(text7))
						{
							string text8 = Path.GetFileNameWithoutExtension(path4) ?? "";
							int num13 = text8.LastIndexOf("__", StringComparison.Ordinal);
							if (num13 > 0)
							{
								text8 = text8.Substring(0, num13);
							}
							text7 = (text8 ?? "").Trim().ToLower();
						}
						if (!string.IsNullOrEmpty(text7) && hashSet.Add(text7))
						{
							num8++;
							if (ShoutUtils.HasUnnamedPersonaKey(text7))
							{
								num7++;
							}
						}
					}
				}
			}
			catch
			{
				num7 = 0;
				num8 = 0;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet2 = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text9 = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text9))
							{
								hashSet3.Add(text9);
							}
						}
					}
					string path5 = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet3.Count <= 0 && File.Exists(path5))
					{
						string value = File.ReadAllText(path5, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text10 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text10))
								{
									hashSet3.Add(text10);
								}
							}
						}
					}
					num10 = hashSet3.Count;
					foreach (string item2 in hashSet3)
					{
						if (hashSet2.Contains(item2))
						{
							num9++;
						}
					}
				}
			}
			catch
			{
				num9 = 0;
				num10 = 0;
			}
			try
			{
				string path6 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
				if (!File.Exists(path6))
				{
					path6 = Path.Combine(importDir, "VoiceMapping.json");
				}
				if (File.Exists(path6))
				{
					string text11 = File.ReadAllText(path6, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(text11))
					{
						vmJson = text11;
						num12 = 1;
						bool flag = false;
						try
						{
							flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
						}
						catch
						{
							flag = false;
						}
						if (flag)
						{
							num11 = 1;
						}
					}
				}
			}
			catch
			{
				num11 = 0;
				num12 = 0;
				vmJson = null;
			}
			bool flag2 = num + num3 + num5 + num7 + num9 + num11 > 0;
			Action action = delegate
			{
				if (pbNew != null)
				{
					if (_npcPersonaProfiles == null)
					{
						_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
					}
					foreach (KeyValuePair<string, NpcPersonaProfile> item3 in pbNew)
					{
						if (!string.IsNullOrEmpty(item3.Key) && item3.Value != null)
						{
							_npcPersonaProfiles.Remove(item3.Key);
							_npcPersonaProfiles[item3.Key] = item3.Value;
						}
					}
				}
				if (dhNew != null)
				{
					if (_dialogueHistory == null)
					{
						_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
					}
					foreach (KeyValuePair<string, List<DialogueDay>> item4 in dhNew)
					{
						if (!string.IsNullOrEmpty(item4.Key) && item4.Value != null)
						{
							_dialogueHistory.Remove(item4.Key);
							_dialogueHistory[item4.Key] = item4.Value;
						}
					}
				}
				if (debtNew != null && rs != null)
				{
					Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
					foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item5 in debtNew)
					{
						if (!string.IsNullOrEmpty(item5.Key) && item5.Value != null)
						{
							dictionary.Remove(item5.Key);
							dictionary[item5.Key] = item5.Value;
						}
					}
					rs.ImportDebtEntries(dictionary);
				}
				if (!string.IsNullOrWhiteSpace(vmJson) && !VoiceMapper.ImportMappingJson(vmJson))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
				ShoutUtils.ImportUnnamedPersonaFromDir(importDir);
				ImportKnowledgeFromDir(importDir, overwriteExisting: true);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (pbNew != null)
				{
					if (_npcPersonaProfiles == null)
					{
						_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
					}
					foreach (KeyValuePair<string, NpcPersonaProfile> item6 in pbNew)
					{
						if (!string.IsNullOrEmpty(item6.Key) && item6.Value != null && !_npcPersonaProfiles.ContainsKey(item6.Key))
						{
							_npcPersonaProfiles[item6.Key] = item6.Value;
						}
					}
				}
				if (dhNew != null)
				{
					if (_dialogueHistory == null)
					{
						_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
					}
					foreach (KeyValuePair<string, List<DialogueDay>> item7 in dhNew)
					{
						if (!string.IsNullOrEmpty(item7.Key) && item7.Value != null && !_dialogueHistory.ContainsKey(item7.Key))
						{
							_dialogueHistory[item7.Key] = item7.Value;
						}
					}
				}
				if (debtNew != null && rs != null)
				{
					Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
					foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item8 in debtNew)
					{
						if (!string.IsNullOrEmpty(item8.Key) && item8.Value != null && !dictionary.ContainsKey(item8.Key))
						{
							dictionary[item8.Key] = item8.Value;
						}
					}
					rs.ImportDebtEntries(dictionary);
				}
				if (!string.IsNullOrWhiteSpace(vmJson) && !VoiceMapper.ImportMappingJson(vmJson, overwriteExisting: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
				ShoutUtils.ImportUnnamedPersonaFromDir(importDir, overwriteExisting: false);
				ImportKnowledgeFromDir(importDir, overwriteExisting: false);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (flag2)
			{
				string text12 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n未命名NPC：" + num7 + "/" + num8 + "\nKnowledge：" + num9 + "/" + num10 + "\n声音映射：" + num11 + "/" + num12 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - 全部导入", text12, action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private bool ImportKnowledgeFromDir(string importDir)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting: true);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
	{
		try
		{
			bool result = false;
			string text = Path.Combine(importDir, "knowledge", "AIConfig.json");
			if (File.Exists(text))
			{
				string moduleRootPath = GetModuleRootPath();
				string text2 = Path.Combine(moduleRootPath, "ModuleData", "AIConfig.json");
				string text3 = Path.Combine(moduleRootPath, "AIConfig.json");
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text2));
					if (overwriteExisting || !File.Exists(text2))
					{
						File.Copy(text, text2, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					if (overwriteExisting && File.Exists(text3))
					{
						File.Copy(text, text3, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					AIConfigHandler.ReloadConfig();
				}
				catch
				{
				}
				result = true;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile != null)
					{
						string json = JsonConvert.SerializeObject(knowledgeFile, Formatting.None);
						knowledgeLibraryBehavior.ImportRulesJson(json, overwriteExisting);
						result = true;
					}
					else
					{
						string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
						if (File.Exists(path))
						{
							string json2 = File.ReadAllText(path);
							knowledgeLibraryBehavior.ImportRulesJson(json2, overwriteExisting);
							result = true;
						}
					}
				}
			}
			catch
			{
			}
			return result;
		}
		catch
		{
			return false;
		}
	}

	private static string FindNpcJsonByHeroId(string dir, string heroId)
	{
		try
		{
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text = (heroId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			string result = null;
			DateTime dateTime = DateTime.MinValue;
			string[] files = Directory.GetFiles(dir, "*.json");
			string[] array = files;
			foreach (string text2 in array)
			{
				string text3 = TryParseHeroIdFromNpcFileName(text2);
				if (!(text3 != text))
				{
					DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text2);
					if (lastWriteTimeUtc > dateTime)
					{
						result = text2;
						dateTime = lastWriteTimeUtc;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildNpcDataFileName(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			id = "unknown";
		}
		string text = "";
		try
		{
			text = Hero.FindFirst((Hero x) => x != null && x.StringId == id)?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		string text2 = id + "__" + text;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text2 = text2.Replace(oldChar, '_');
		}
		return text2 + ".json";
	}

	private static string TryParseHeroIdFromNpcFileName(string filePath)
	{
		try
		{
			string text = Path.GetFileNameWithoutExtension(filePath) ?? "";
			int num = text.IndexOf("__", StringComparison.Ordinal);
			if (num <= 0)
			{
				return null;
			}
			string text2 = text.Substring(0, num);
			text2 = (text2 ?? "").Trim();
			return string.IsNullOrEmpty(text2) ? null : text2;
		}
		catch
		{
			return null;
		}
	}

	private static string GetPlayerExportsRootPath()
	{
		string moduleRootPath = GetModuleRootPath();
		return Path.Combine(moduleRootPath, "PlayerExports");
	}

	private static string GetModuleRootPath()
	{
		try
		{
			string location = typeof(SubModule).Assembly.Location;
			string text = (string.IsNullOrEmpty(location) ? "" : Path.GetDirectoryName(Path.GetFullPath(location)));
			DirectoryInfo directoryInfo = (string.IsNullOrEmpty(text) ? null : new DirectoryInfo(text));
			while (directoryInfo != null && directoryInfo.Exists)
			{
				if (File.Exists(Path.Combine(directoryInfo.FullName, "SubModule.xml")))
				{
					return directoryInfo.FullName;
				}
				directoryInfo = directoryInfo.Parent;
			}
		}
		catch
		{
		}
		try
		{
			return Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		catch
		{
			return "";
		}
	}

	private static string SanitizeFolderName(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Trim();
	}

	private static void WriteJson(string path, object obj)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
		string contents = JsonConvert.SerializeObject(obj, Formatting.Indented);
		File.WriteAllText(path, contents, Encoding.UTF8);
	}

	private static T ReadJson<T>(string path) where T : class
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string value = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(value);
		}
		catch
		{
			return null;
		}
	}

	private static string FindLatestExportFolder(string root)
	{
		try
		{
			if (!Directory.Exists(root))
			{
				return null;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(root);
			return (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).FirstOrDefault()?.FullName;
		}
		catch
		{
			return null;
		}
	}

	private static void ClearJsonFiles(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
			{
				return;
			}
			string[] files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private string ResolveExportFolderName(string folderName)
	{
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			text = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		}
		return text;
	}

	private string ResolveImportFolderPath(string folderName)
	{
		string text = (folderName ?? "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				if (Path.IsPathRooted(text))
				{
					string fullPath = Path.GetFullPath(text);
					if (Directory.Exists(fullPath))
					{
						return fullPath;
					}
				}
			}
			catch
			{
			}
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text2 = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text2))
		{
			return FindLatestExportFolder(playerExportsRootPath);
		}
		return Path.Combine(playerExportsRootPath, text2);
	}

	private bool IsPersonaReadyForFixedLayer(Hero hero, out bool inFlight)
	{
		inFlight = false;
		if (hero == null)
		{
			return true;
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return true;
		}
		lock (_npcPersonaAutoGenLock)
		{
			if (_npcPersonaAutoGenInFlight.Contains(stringId))
			{
				inFlight = true;
				return false;
			}
		}
		return true;
	}

	private string BuildDirectFixedLayerCacheKey(Hero hero, string cultureId, string basePrompt, string guardrail, string characterDescription, string personaContext, bool personaReady)
	{
		if (!personaReady)
		{
			return null;
		}
		string text = hero?.StringId ?? "_nohero_";
		using SHA256 sHA = SHA256.Create();
		string s = text + "|" + (cultureId ?? "") + "|" + (basePrompt ?? "") + "|" + (guardrail ?? "") + "|" + (characterDescription ?? "") + "|" + (personaContext ?? "");
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
		StringBuilder stringBuilder = new StringBuilder("direct_", 39);
		for (int i = 0; i < 16; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
