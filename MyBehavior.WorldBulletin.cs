using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

// Instant bulletin mode: campaign events are scored locally on arrival (O(1) append, capped list),
// the hourly tick advances one collect window, and each publish issues one LLM request.
// Kingdoms get a local template brief once per week (no LLM); NPCs read live facts instead.
public partial class MyBehavior
{
	private const string WorldBulletinStorageKey = "_af_worldBulletin_v1";

	private const string WorldBulletinBulletinIdMarker = ":bulletin:";

	private WorldBulletinSaveState _worldBulletinState;

	private readonly ConcurrentQueue<Action> _worldBulletinMainThreadActions = new ConcurrentQueue<Action>();

	private bool _worldBulletinInFlight;

	// Rank captured in BeforeHeroKilledEvent; consumed by the matching HeroKilledEvent in the same call stack.
	private readonly Dictionary<string, WorldBulletinDeathSnapshot> _worldBulletinDeathSnapshots = new Dictionary<string, WorldBulletinDeathSnapshot>(StringComparer.Ordinal);

	// Unreadable raw JSON is carried verbatim, then retained inside any newly initialized state.
	private string _worldBulletinCorruptRaw;

	// Id of the newest published bulletin, for the per-prompt NPC lookup (MyBehavior.WorldBulletinNpc.cs).
	private string _worldBulletinLatestEventId = "";

	private int _worldBulletinLastPruneDay = -1;

	private WorldBulletinFocus _worldBulletinFocus;

	private int _worldBulletinFocusDay = -1;

	private string _worldBulletinFocusOwnKingdomId = "";

	internal static bool IsWorldBulletinEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.UseWorldBulletin ?? true;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsWorldBulletinPublishingEnabled()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			return settings == null || (settings.UseWorldBulletin && settings.AutoGenerateWeeklyReports);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsWorldBulletinEventId(string eventId)
	{
		return (eventId ?? "").IndexOf(WorldBulletinBulletinIdMarker, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private WorldBulletinSaveState EnsureWorldBulletinState()
	{
		if (_worldBulletinState == null)
		{
			int day = GetCurrentGameDayIndexSafe();
			_worldBulletinState = new WorldBulletinSaveState
			{
				TrackingStartDay = day,
				LastKingdomWeek = day / 7,
				PreservedUnreadableState = _worldBulletinCorruptRaw
			};
		}
		_worldBulletinState.Events ??= new List<WorldBulletinEvent>();
		_worldBulletinState.World ??= new WorldBulletinScopeState();
		_worldBulletinState.Player ??= new WorldBulletinScopeState();
		_worldBulletinState.WeeklyStability ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		return _worldBulletinState;
	}

	private void RegisterWorldBulletinEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnWorldBulletinHourlyTick);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWorldBulletinWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnWorldBulletinMakePeace);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnWorldBulletinSettlementOwnerChanged);
		CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnWorldBulletinBeforeHeroKilled);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnWorldBulletinHeroKilled);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnWorldBulletinHeroPrisonerTaken);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnWorldBulletinMapEventEnded);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnWorldBulletinKingdomDestroyed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnWorldBulletinClanChangedKingdom);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnWorldBulletinRaidCompleted);
	}

	// ---------- event capture ----------

	private static double GetCurrentCampaignHourSafe()
	{
		try
		{
			return CampaignTime.Now.ToHours;
		}
		catch
		{
			return GetCurrentGameDayIndexSafe() * 24.0;
		}
	}

	// Home kingdom (the player's, else the nearest, like the legacy report) plus the 3 nearest kingdoms.
	// The proximity scan walks Settlement.All, so the focus is cached for one campaign day;
	// a change of the player's own kingdom refreshes it at once.
	private WorldBulletinFocus GetWorldBulletinFocus()
	{
		try
		{
			string own = GetKingdomId(Clan.PlayerClan?.Kingdom);
			int day = GetCurrentGameDayIndexSafe();
			if (_worldBulletinFocus == null || day != _worldBulletinFocusDay || !string.Equals(own, _worldBulletinFocusOwnKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				List<string> nearest = GetKingdomIdsByPlayerProximity(GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select(k => GetKingdomId(k)))
					.Where(x => !string.IsNullOrWhiteSpace(x)).Take(3).ToList();
				_worldBulletinFocus = new WorldBulletinFocus
				{
					PlayerKingdomId = own.Length > 0 ? own : nearest.FirstOrDefault() ?? "",
					NearbyKingdomIds = new HashSet<string>(nearest, StringComparer.OrdinalIgnoreCase)
				};
				_worldBulletinFocusDay = day;
				_worldBulletinFocusOwnKingdomId = own;
			}
			return _worldBulletinFocus;
		}
		catch
		{
			return new WorldBulletinFocus();
		}
	}

	private static bool IsWorldBulletinPlayerHero(Hero hero)
	{
		return hero != null && (hero == Hero.MainHero || (hero.Clan != null && hero.Clan == Clan.PlayerClan));
	}

	// Returns true only for a newly recorded fact, so callers apply stability once per fact.
	private bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, params string[] kingdomIds)
	{
		if (!IsWorldBulletinEnabled())
		{
			return false;
		}
		string text = (PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(sentence) ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string normalizedKey = (key ?? "").Trim();
		if (text.Length == 0 || normalizedKey.Length == 0)
		{
			return false;
		}
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		List<WorldBulletinEvent> events = state.Events;
		// Duplicate callbacks for one fact arrive back to back; only the recent tail is checked.
		for (int i = events.Count - 1, checkedCount = 0; i >= 0 && checkedCount < 64; i--, checkedCount++)
		{
			if (string.Equals(events[i]?.Key, normalizedKey, StringComparison.Ordinal))
			{
				return false;
			}
		}
		double now = GetCurrentCampaignHourSafe();
		WorldBulletinEvent e = new WorldBulletinEvent
		{
			Key = normalizedKey,
			Kind = kind ?? "",
			Day = GetCurrentGameDayIndexSafe(),
			Hour = now,
			Score = score,
			Sentence = text,
			Group = (group ?? "").Trim(),
			Detail = WorldBulletinPolicy.Truncate((PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(detail) ?? "").Replace("\r", " ").Replace("\n", " "), 220),
			InvolvesPlayer = involvesPlayer,
			KingdomIds = (kingdomIds ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
		};
		events.Add(e);
		if (events.Count > WorldBulletinPolicy.MaxEvents)
		{
			events.RemoveAt(0);
		}
		if (WorldBulletinPolicy.IsTrigger(e, GetWorldBulletinFocus()) && !WorldBulletinPolicy.TryOpenWindow(state.World, e.Hour, now))
		{
			state.World.PendingTrigger = true;
		}
		Logger.Log("WorldBulletin", "[Capture] kind=" + e.Kind + " score=" + score + " player=" + involvesPlayer + " group=" + e.Group + " key=" + normalizedKey);
		return true;
	}

	// Bulletin stability replaces the legacy weekly STAB tags, so it runs only when bulletins actually publish;
	// with auto weekly reports off, stability stays as untouched as it was before.
	private void ApplyWorldBulletinStability(string kingdomId, int delta)
	{
		if (delta == 0 || !IsWorldBulletinPublishingEnabled() || !DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			return;
		}
		Kingdom kingdom = FindKingdomById(kingdomId);
		if (kingdom == null || kingdom.IsEliminated)
		{
			return;
		}
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		string key = (GetCurrentGameDayIndexSafe() / 7).ToString(CultureInfo.InvariantCulture) + "|" + GetKingdomId(kingdom);
		state.WeeklyStability.TryGetValue(key, out int currentTotal);
		int applied = WorldBulletinPolicy.ClampWeeklyStability(currentTotal, delta, out int newTotal);
		if (applied == 0)
		{
			return;
		}
		state.WeeklyStability[key] = newTotal;
		// Value only: the daily maintenance slice (ApplyKingdomStabilityRelationAdjustments) reconciles relations,
		// so a battle does not walk every clan hero twice.
		KingdomStability.Set(GetKingdomId(kingdom), GetKingdomStabilityValue(kingdom) + applied);
	}

	private void OnWorldBulletinWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		try
		{
			if (!(faction1 is Kingdom k1) || !(faction2 is Kingdom k2))
			{
				return;
			}
			string sentence = GetKingdomDisplayName(k1) + "向" + GetKingdomDisplayName(k2) + "宣战，两国进入战争状态。";
			string detailText = "宣战方君主：" + GetHeroDisplayName(k1.Leader) + "；被宣战方君主：" + GetHeroDisplayName(k2.Leader) + "；宣战缘由：" + GetWorldBulletinWarReason(detail);
			CaptureWorldBulletinEvent("war_declared", "war:" + GetKingdomId(k1) + ":" + GetKingdomId(k2) + ":" + GetCurrentGameDayIndexSafe(), 70, sentence, false,
				"diplomacy:" + WorldBulletinPairKey(k1, k2), detailText, GetKingdomId(k1), GetKingdomId(k2));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] war declared: " + ex.Message);
		}
	}

	private void OnWorldBulletinMakePeace(IFaction side1, IFaction side2, MakePeaceAction.MakePeaceDetail detail)
	{
		try
		{
			if (!(side1 is Kingdom k1) || !(side2 is Kingdom k2))
			{
				return;
			}
			string sentence = GetKingdomDisplayName(k1) + "与" + GetKingdomDisplayName(k2) + "停战议和，双方结束战争状态。";
			string detailText = GetKingdomDisplayName(k1) + "君主：" + GetHeroDisplayName(k1.Leader) + "；" + GetKingdomDisplayName(k2) + "君主：" + GetHeroDisplayName(k2.Leader) + "；议和方式：" + (detail == MakePeaceAction.MakePeaceDetail.ByKingdomDecision ? "王国议会决议" : "双方议定");
			if (CaptureWorldBulletinEvent("peace_made", "peace:" + GetKingdomId(k1) + ":" + GetKingdomId(k2) + ":" + GetCurrentGameDayIndexSafe(), 60, sentence, false,
				"diplomacy:" + WorldBulletinPairKey(k1, k2), detailText, GetKingdomId(k1), GetKingdomId(k2)))
			{
				ApplyWorldBulletinStability(GetKingdomId(k1), 2);
				ApplyWorldBulletinStability(GetKingdomId(k2), 2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] make peace: " + ex.Message);
		}
	}

	private static string GetWorldBulletinWarReason(DeclareWarAction.DeclareWarDetail detail)
	{
		switch (detail)
		{
		case DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility:
			return "玩家一方的敌对行为";
		case DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision:
			return "王国议会决议";
		case DeclareWarAction.DeclareWarDetail.CausedByRebellion:
			return "叛乱";
		case DeclareWarAction.DeclareWarDetail.CausedByCrimeRatingChange:
			return "犯罪恶名";
		case DeclareWarAction.DeclareWarDetail.CausedByKingdomCreation:
			return "新王国建立";
		case DeclareWarAction.DeclareWarDetail.CausedByClaimOnThrone:
			return "争夺王位";
		case DeclareWarAction.DeclareWarDetail.CausedByCallToWarAgreement:
			return "盟约参战";
		default:
			return "未载明";
		}
	}

	// Order-independent, so A-vs-B war and B-vs-A peace land in one story.
	private static string WorldBulletinPairKey(IFaction a, IFaction b)
	{
		string x = GetKingdomId(a);
		string y = GetKingdomId(b);
		return string.CompareOrdinal(x, y) <= 0 ? x + "|" + y : y + "|" + x;
	}

	private static string WorldBulletinHeroTitle(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		if (IsWorldBulletinRuler(hero))
		{
			return GetKingdomDisplayName(hero.Clan?.Kingdom) + "君主";
		}
		string clan = GetClanDisplayName(hero.Clan);
		string kingdom = hero.Clan?.Kingdom != null ? GetKingdomDisplayName(hero.Clan.Kingdom) : "";
		bool clanLeader = hero.Clan != null && hero.Clan.Leader == hero;
		return kingdom + clan + "家族" + (clanLeader ? "族长" : "成员");
	}

	private void OnWorldBulletinSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		try
		{
			if (settlement == null || settlement.IsVillage || (!settlement.IsTown && !settlement.IsCastle))
			{
				return;
			}
			Kingdom newKingdom = newOwner?.Clan?.Kingdom;
			Kingdom oldKingdom = oldOwner?.Clan?.Kingdom;
			string newId = GetKingdomId(newKingdom);
			string oldId = GetKingdomId(oldKingdom);
			string name = GetSettlementDisplayName(settlement);
			bool involvesPlayer = IsWorldBulletinPlayerHero(newOwner) || IsWorldBulletinPlayerHero(oldOwner) || IsWorldBulletinPlayerHero(capturerHero);
			string key = "settlement:" + GetSettlementId(settlement) + ":" + GetHeroId(newOwner) + ":" + GetCurrentGameDayIndexSafe();
			string oldName = GetKingdomDisplayName(oldKingdom, GetClanDisplayName(oldOwner?.Clan) + "家族");
			string newName = GetKingdomDisplayName(newKingdom, GetClanDisplayName(newOwner?.Clan) + "家族");
			string settlementGroup = "siege:" + GetSettlementId(settlement);
			string ownerDetail = (settlement.IsTown ? "城镇" : "城堡")
				+ (oldOwner != null ? "；原领主：" + GetHeroDisplayName(oldOwner) + "（" + WorldBulletinHeroTitle(oldOwner) + "）" : "")
				+ (newOwner != null ? "；新领主：" + GetHeroDisplayName(newOwner) + "（" + WorldBulletinHeroTitle(newOwner) + "）" : "");
			if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
			{
				Hero actor = capturerHero ?? newOwner;
				string sentence = GetHeroDisplayName(actor) + "攻陷" + name + "，该地由" + oldName + "转归" + newName + "。";
			if (CaptureWorldBulletinEvent("settlement_siege", key, settlement.IsTown ? 65 : 50, sentence, involvesPlayer, settlementGroup, ownerDetail + "；攻城统帅：" + GetHeroDisplayName(actor), newId, oldId))
				{
					ApplyWorldBulletinStability(newId, settlement.IsTown ? 4 : 2);
					ApplyWorldBulletinStability(oldId, settlement.IsTown ? -6 : -3);
				}
				return;
			}
			string label = GetSettlementOwnerChangeDetailLabel(detail);
			if (!string.IsNullOrEmpty(newId) && string.Equals(newId, oldId, StringComparison.OrdinalIgnoreCase))
			{
				string grant = name + "被授予" + GetHeroDisplayName(newOwner) + "（方式：" + label + "）。";
				CaptureWorldBulletinEvent("fief_grant", key, 20, grant, involvesPlayer, "fief_grant:" + newId + ":" + GetCurrentGameDayIndexSafe(), ownerDetail, newId);
				return;
			}
			string transfer = name + "以“" + label + "”的方式由" + oldName + "转归" + newName + "，并非攻城夺取。";
			CaptureWorldBulletinEvent("settlement_transfer", key, 30, transfer, involvesPlayer, settlementGroup, ownerDetail, newId, oldId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] settlement owner changed: " + ex.Message);
		}
	}

	private static bool IsWorldBulletinRuler(Hero hero)
	{
		try
		{
			Kingdom kingdom = hero?.Clan?.Kingdom;
			return kingdom != null && (kingdom.Leader == hero || (kingdom.RulingClan == hero.Clan && hero.Clan.Leader == hero));
		}
		catch
		{
			return false;
		}
	}

	// KillCharacterAction hands the clan and throne to the heir (and may detach a heirless clan from its kingdom)
	// before HeroKilledEvent fires, in both 1.3.x and 1.4.x. Rank and realm are therefore read here, one event earlier.
	private void OnWorldBulletinBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null || victim == Hero.MainHero || !victim.IsLord || !IsWorldBulletinEnabled())
			{
				return;
			}
			_worldBulletinDeathSnapshots[GetHeroId(victim)] = new WorldBulletinDeathSnapshot
			{
				Ruler = IsWorldBulletinRuler(victim),
				KingdomId = GetKingdomId(victim.Clan?.Kingdom),
				Title = WorldBulletinHeroTitle(victim)
			};
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] before hero killed: " + ex.Message);
		}
	}

	private sealed class WorldBulletinDeathSnapshot
	{
		public bool Ruler;

		public string KingdomId = "";

		public string Title = "";
	}

	private void OnWorldBulletinHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null || victim == Hero.MainHero || !victim.IsLord)
			{
				return;
			}
			string victimId = GetHeroId(victim);
			_worldBulletinDeathSnapshots.TryGetValue(victimId, out WorldBulletinDeathSnapshot snapshot);
			_worldBulletinDeathSnapshots.Remove(victimId);
			bool ruler = snapshot?.Ruler ?? IsWorldBulletinRuler(victim);
			Kingdom kingdom = (snapshot != null ? FindKingdomById(snapshot.KingdomId) : null) ?? victim.Clan?.Kingdom;
			string victimTitle = snapshot?.Title ?? WorldBulletinHeroTitle(victim);
			string who = ruler
				? GetKingdomDisplayName(kingdom) + "的君主" + GetHeroDisplayName(victim)
				: (kingdom != null ? GetKingdomDisplayName(kingdom) : "") + GetClanDisplayName(victim.Clan) + "家族的" + GetHeroDisplayName(victim);
			bool natural = detail == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge || detail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor;
			string killerName = killer != null ? GetHeroDisplayName(killer) : "";
			string sentence;
			VengeanceExecutionFacts executionFacts = ResolvePublicExecutionFacts(victim, detail);
			if (executionFacts != null)
			{
				sentence = who + BuildPublicExecutionVenueText(executionFacts) + (killer != null ? "被" + killerName : "被") + BuildPublicExecutionMethodText(executionFacts) + BuildPublicExecutionChargeText(executionFacts) + "。";
			}
			else if (IsExecutionKillDetail(detail))
			{
				sentence = who + (killer != null ? "被" + killerName + "处决。" : "被处决。");
			}
			else if (detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
			{
				sentence = who + "战死沙场" + (killer != null ? "，死于" + killerName + "之手。" : "。");
			}
			else if (detail == KillCharacterAction.KillCharacterActionDetail.Murdered)
			{
				sentence = who + (killer != null ? "遭" + killerName + "谋杀。" : "遭人谋杀。");
			}
			else if (detail == KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge)
			{
				sentence = who + "寿终离世。";
			}
			else
			{
				sentence = who + "离世。";
			}
			int score = ruler ? (natural ? 70 : 90) : (natural ? 25 : 50);
			string kind = ruler ? "ruler_killed" : "lord_killed";
			string kingdomId = GetKingdomId(kingdom);
			// One executioner's or murderer's victims form one story; a battle death joins that day's clash between the realms.
			bool execution = executionFacts != null || IsExecutionKillDetail(detail);
			string group = execution ? "execution:" + GetHeroId(killer) + ":" + GetCurrentGameDayIndexSafe()
				: detail == KillCharacterAction.KillCharacterActionDetail.Murdered ? "murder:" + GetHeroId(killer)
				: detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle ? "clash:" + GetCurrentGameDayIndexSafe() + ":" + WorldBulletinPairKey(killer?.MapFaction, victim.MapFaction)
				: "death:" + GetHeroId(victim);
			StringBuilder detailText = new StringBuilder();
			detailText.Append("死者身份：").Append(victimTitle).Append("，年约").Append((int)victim.Age).Append("岁");
			if (killer != null)
			{
				detailText.Append("；行事者：").Append(killerName).Append("（").Append(WorldBulletinHeroTitle(killer)).Append("）");
			}
			if (executionFacts != null)
			{
				detailText.Append("；审判：").Append(executionFacts.ToneLabel).Append("，").Append(executionFacts.LegitimacyLabel)
					.Append(executionFacts.PlayerStruck ? "，由执行者亲自行刑" : "，由行刑人行刑");
			}
			else if (execution)
			{
				string place = ResolveHeroExecutionLocationText(ResolveHeroExecutionSettlement(victim, killer), victim, killer);
				if (!string.IsNullOrWhiteSpace(place))
				{
					detailText.Append("；地点：").Append(place);
				}
			}
			if (CaptureWorldBulletinEvent(kind, "killed:" + victimId, score, sentence, IsWorldBulletinPlayerHero(killer) || IsWorldBulletinPlayerHero(victim), group, detailText.ToString(), kingdomId, GetKingdomId(killer?.Clan?.Kingdom)))
			{
				ApplyWorldBulletinStability(kingdomId, ruler ? -8 : (natural ? 0 : -2));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] hero killed: " + ex.Message);
		}
	}

	private void OnWorldBulletinHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		try
		{
			if (prisoner == null || prisoner == Hero.MainHero || !prisoner.IsLord)
			{
				return;
			}
			Hero captor = capturer?.LeaderHero ?? capturer?.MobileParty?.LeaderHero;
			bool ruler = IsWorldBulletinRuler(prisoner);
			Kingdom kingdom = prisoner.Clan?.Kingdom;
			string who = ruler ? GetKingdomDisplayName(kingdom) + "的君主" + GetHeroDisplayName(prisoner) : GetHeroDisplayName(prisoner) + "（" + GetKingdomDisplayName(kingdom, GetClanDisplayName(prisoner.Clan) + "家族") + "）";
			string sentence = who + (captor != null ? "被" + GetHeroDisplayName(captor) + "俘虏。" : "被敌方俘虏。");
			string kingdomId = GetKingdomId(kingdom);
			// Same group as the day's clashes between these realms, so a battle and its captives read as one story.
			string group = "clash:" + GetCurrentGameDayIndexSafe() + ":" + WorldBulletinPairKey(captor?.MapFaction, prisoner.MapFaction);
			string detailText = "被俘者身份：" + WorldBulletinHeroTitle(prisoner) + (captor != null ? "；俘获者：" + GetHeroDisplayName(captor) + "（" + WorldBulletinHeroTitle(captor) + "）" : "");
			if (CaptureWorldBulletinEvent(ruler ? "ruler_captured" : "lord_captured", "captured:" + GetHeroId(prisoner) + ":" + GetCurrentGameDayIndexSafe(), ruler ? 65 : 35, sentence, IsWorldBulletinPlayerHero(captor),
				group, detailText, kingdomId, GetKingdomId(captor?.Clan?.Kingdom)))
			{
				ApplyWorldBulletinStability(kingdomId, ruler ? -4 : -1);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] prisoner taken: " + ex.Message);
		}
	}

	private void OnWorldBulletinMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.HasWinner || mapEvent.IsHideoutBattle || IsBanditOrMonsterMapEvent(mapEvent))
			{
				return;
			}
			MapEventSide winner = mapEvent.WinningSide == BattleSideEnum.Attacker ? mapEvent.AttackerSide : mapEvent.DefenderSide;
			MapEventSide loser = GetMapEventDefeatedSide(mapEvent);
			if (winner == null || loser == null)
			{
				return;
			}
			bool involvesPlayer = mapEvent.IsPlayerMapEvent;
			bool winnerLord = ShouldMentionBattleHero(winner.LeaderParty?.LeaderHero);
			bool loserLord = ShouldMentionBattleHero(loser.LeaderParty?.LeaderHero);
			if (!involvesPlayer && !winnerLord && !loserLord)
			{
				return;
			}
			int troops = GetMapEventTroopCount(mapEvent);
			bool siege = mapEvent.IsSiegeAssault;
			// Raid fights are covered by the raid event; a lord running down villagers or caravans is not news.
			bool lordVsLord = winnerLord && loserLord;
			if (!involvesPlayer && (mapEvent.IsRaid || (!siege && !lordVsLord && troops <= MajorNpcBattleTroopThreshold)))
			{
				return;
			}
			int score = (troops >= 1000 ? 55 : (troops > MajorNpcBattleTroopThreshold ? 40 : (lordVsLord ? 30 : 20))) + (siege ? 10 : 0);
			string location = GetMapEventLocationLabel(mapEvent);
			string winnerFaction = GetFactionDisplayName(winner.MapFaction, "一方");
			string loserFaction = GetFactionDisplayName(loser.MapFaction, "另一方");
			string sentence = location + (siege ? "攻城战：" : "一战：") + GetPrimaryOtherSideLabel(winner) + "（" + winnerFaction + "）击败" + GetPrimaryOtherSideLabel(loser) + "（" + loserFaction + "）"
				+ (troops > 0 ? "，双方约" + troops + "人参战。" : "。");
			string winnerId = GetKingdomId(winner.MapFaction);
			string loserId = GetKingdomId(loser.MapFaction);
			string group = siege && mapEvent.MapEventSettlement != null
				? "siege:" + GetSettlementId(mapEvent.MapEventSettlement)
				: "clash:" + GetCurrentGameDayIndexSafe() + ":" + WorldBulletinPairKey(winner.MapFaction, loser.MapFaction);
			string detailText = "胜方" + GetMapEventSideCommittedTroopCount(winner) + "人，" + BuildMapEventCasualtyText(winner)
				+ "；败方" + GetMapEventSideCommittedTroopCount(loser) + "人，" + BuildMapEventCasualtyText(loser)
				+ (winnerLord ? "；胜方统帅：" + WorldBulletinHeroTitle(winner.LeaderParty.LeaderHero) : "")
				+ (loserLord ? "；败方统帅：" + WorldBulletinHeroTitle(loser.LeaderParty.LeaderHero) : "");
			if (CaptureWorldBulletinEvent(siege ? "siege_battle" : "battle", "battle:" + BuildMapEventStableKey(mapEvent, location), score, sentence, involvesPlayer, group, detailText, winnerId, loserId))
			{
				int swing = troops > MajorNpcBattleTroopThreshold ? 2 : 1;
				ApplyWorldBulletinStability(winnerId, swing);
				ApplyWorldBulletinStability(loserId, -swing);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] map event ended: " + ex.Message);
		}
	}

	private void OnWorldBulletinKingdomDestroyed(Kingdom kingdom)
	{
		try
		{
			if (kingdom == null)
			{
				return;
			}
			CaptureWorldBulletinEvent("kingdom_destroyed", "kingdom_destroyed:" + GetKingdomId(kingdom), 100, GetKingdomDisplayName(kingdom) + "已经覆灭，这个王国不复存在。", false,
				"realm:" + GetKingdomId(kingdom), "末代君主：" + GetHeroDisplayName(kingdom.Leader), GetKingdomId(kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] kingdom destroyed: " + ex.Message);
		}
	}

	private void OnWorldBulletinClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		try
		{
			if (clan == null)
			{
				return;
			}
			bool involvesPlayer = clan == Clan.PlayerClan;
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion && oldKingdom != null)
			{
				string sentence = GetClanDisplayName(clan) + "家族举兵反叛，脱离了" + GetKingdomDisplayName(oldKingdom) + "。";
				if (CaptureWorldBulletinEvent("kingdom_rebellion", "rebellion:" + GetClanId(clan) + ":" + GetKingdomId(oldKingdom), 70, sentence, involvesPlayer,
					"realm:" + GetKingdomId(oldKingdom), "叛乱家族族长：" + GetHeroDisplayName(clan.Leader) + "；原王国君主：" + GetHeroDisplayName(oldKingdom.Leader), GetKingdomId(oldKingdom)))
				{
					ApplyWorldBulletinStability(GetKingdomId(oldKingdom), -8);
				}
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom && newKingdom != null)
			{
				string sentence = GetClanDisplayName(clan) + "家族建立了新王国" + GetKingdomDisplayName(newKingdom) + "。";
				CaptureWorldBulletinEvent("kingdom_created", "kingdom_created:" + GetKingdomId(newKingdom), 75, sentence, involvesPlayer,
					"realm:" + GetKingdomId(oldKingdom ?? newKingdom), "开国者：" + GetHeroDisplayName(clan.Leader) + (oldKingdom != null ? "；此前效忠：" + GetKingdomDisplayName(oldKingdom) : ""), GetKingdomId(newKingdom), GetKingdomId(oldKingdom));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] clan changed kingdom: " + ex.Message);
		}
	}

	private void OnWorldBulletinRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			if (winnerSide != BattleSideEnum.Attacker)
			{
				return;
			}
			Settlement settlement = raidEvent?.MapEventSettlement;
			Hero raider = raidEvent?.AttackerSide?.LeaderParty?.LeaderHero;
			if (settlement == null || raider == null || !raider.IsLord)
			{
				return;
			}
			string victimId = GetKingdomId(settlement.MapFaction);
			string sentence = settlement.Name + "村遭" + GetHeroDisplayName(raider) + "（" + GetFactionDisplayName(raider.MapFaction, "敌军") + "）劫掠得手。";
			// A realm's raids on one day collapse into a single "等N起" line.
			string owner = settlement.Village?.Bound != null ? GetSettlementDisplayName(settlement.Village.Bound) : "";
			if (CaptureWorldBulletinEvent("raid", "raid:" + GetSettlementId(settlement) + ":" + GetCurrentGameDayIndexSafe(), 25, sentence, IsWorldBulletinPlayerHero(raider),
				"raid:" + GetCurrentGameDayIndexSafe() + ":" + GetKingdomId(raider.MapFaction) + ">" + victimId, owner.Length > 0 ? "该村隶属：" + owner : "", victimId, GetKingdomId(raider.MapFaction)))
			{
				ApplyWorldBulletinStability(victimId, -1);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] raid completed: " + ex.Message);
		}
	}

	internal void CaptureWorldBulletinCivilWar(string kingdomId, string stableKey)
	{
		try
		{
			Kingdom kingdom = FindKingdomById(kingdomId);
			if (kingdom == null)
			{
				return;
			}
			CaptureWorldBulletinEvent("civil_war", "civil_war:" + (stableKey ?? GetKingdomId(kingdom)), 80, GetKingdomDisplayName(kingdom) + "爆发内战，国内各家族兵戎相见。", Clan.PlayerClan?.Kingdom == kingdom,
				"realm:" + GetKingdomId(kingdom), "在位君主：" + GetHeroDisplayName(kingdom.Leader), GetKingdomId(kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] civil war: " + ex.Message);
		}
	}

	// ---------- hourly scheduling ----------

	private void OnWorldBulletinHourlyTick()
	{
		if (!IsWorldBulletinEnabled())
		{
			return;
		}
		try
		{
			WorldBulletinSaveState state = EnsureWorldBulletinState();
			int day = GetCurrentGameDayIndexSafe();
			// Keep the legacy cursor current, so switching back to weekly reports resumes at this week
			// instead of replaying every week the bulletin covered.
			_lastAutoGeneratedWeeklyReportWeek = Math.Max(_lastAutoGeneratedWeeklyReportWeek, day / 7);
			if (day != _worldBulletinLastPruneDay)
			{
				_worldBulletinLastPruneDay = day;
				WorldBulletinPolicy.Prune(state.Events, day);
				PruneWorldBulletinWeeklyStability(state, day / 7);
			}
			if (!IsWorldBulletinPublishingEnabled())
			{
				return;
			}
			AdvanceWorldBulletinScope(state, GetCurrentCampaignHourSafe());
			WriteWorldBulletinKingdomBriefs(state, day);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] hourly tick: " + ex);
		}
	}

	private static void PruneWorldBulletinWeeklyStability(WorldBulletinSaveState state, int currentWeek)
	{
		foreach (string key in state.WeeklyStability.Keys.ToList())
		{
			int bar = key.IndexOf('|');
			if (bar <= 0 || !int.TryParse(key.Substring(0, bar), NumberStyles.Integer, CultureInfo.InvariantCulture, out int week) || week < currentWeek - 1)
			{
				state.WeeklyStability.Remove(key);
			}
		}
	}

	private void AdvanceWorldBulletinScope(WorldBulletinSaveState state, double now)
	{
		if (_worldBulletinInFlight)
		{
			return;
		}
		WorldBulletinScopeState scope = state.World;
		WorldBulletinFocus focus = GetWorldBulletinFocus();
		if (scope.WindowEndHour < 0)
		{
			if (!scope.PendingTrigger || now < scope.CooldownUntilHour)
			{
				return;
			}
			scope.PendingTrigger = false;
			double triggerHour = WorldBulletinPolicy.FindPendingTriggerHour(state.Events, scope, focus, now);
			if (triggerHour >= 0)
			{
				WorldBulletinPolicy.TryOpenWindow(scope, triggerHour, now);
			}
			return;
		}
		if (!WorldBulletinPolicy.IsWindowDue(scope, now))
		{
			return;
		}
		WorldBulletinSelection selection = WorldBulletinPolicy.Select(state.Events, scope, focus, now);
		if (selection?.Major == null)
		{
			WorldBulletinPolicy.AbandonWindow(scope);
			return;
		}
		long generation = SaveRuntimeGuard.CaptureGeneration();
		Kingdom home = FindKingdomById(focus.PlayerKingdomId);
		string scopeLine = "快报视角：天下大事，但以" + (home != null ? GetKingdomDisplayName(home) : "玩家所在地区") + "读者关心的角度组织；与玩家本人或该国相关的事实优先交代";
		WorldBulletinText template = WorldBulletinPolicy.BuildTemplate(selection);
		string userPrompt = WorldBulletinPolicy.BuildUserPrompt(scopeLine, GetCurrentGameDateTextSafe(), selection, BuildWorldBulletinKingdomContext(selection));
		string systemPrompt = WorldBulletinPolicy.BuildSystemPrompt(selection.MajorFacts.Count, selection.Minors.Count);
		Logger.Log("WorldBulletin", "[Select] home=" + focus.PlayerKingdomId + " majorFacts=" + selection.MajorFacts.Count + " minors=" + selection.Minors.Count + " minorEvents=" + selection.Minors.Sum(x => x.Events.Count));
		// Set last: if anything above throws, the flag stays clear and the next tick retries instead of blocking forever.
		_worldBulletinInFlight = true;
		_ = RunWorldBulletinRequestAsync(scope.WindowEndHour, generation, selection, template, systemPrompt, userPrompt);
	}

	// Main thread, once per publish: at most four kingdoms, each checked against the others for war.
	private List<string> BuildWorldBulletinKingdomContext(WorldBulletinSelection selection)
	{
		List<string> lines = new List<string>();
		try
		{
			List<string> ids = selection.MajorFacts.SelectMany(e => e.KingdomIds ?? new List<string>())
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
			List<Kingdom> all = GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).ToList();
			foreach (string id in ids)
			{
				Kingdom kingdom = FindKingdomById(id);
				if (kingdom == null || kingdom.IsEliminated)
				{
					continue;
				}
				int towns = 0;
				int castles = 0;
				foreach (Settlement settlement in kingdom.Settlements)
				{
					if (settlement.IsTown)
					{
						towns++;
					}
					else if (settlement.IsCastle)
					{
						castles++;
					}
				}
				List<string> enemies = all.Where(k => k != kingdom && kingdom.IsAtWarWith(k)).Select(k => GetKingdomDisplayName(k)).Take(4).ToList();
				string line = GetKingdomDisplayName(kingdom) + "：君主" + GetHeroDisplayName(kingdom.Leader) + "，城镇" + towns + "座、城堡" + castles + "座"
					+ (DuelSettings.IsKingdomStabilityAndRebellionEnabled() ? "，稳定度" + GetKingdomStabilityValue(kingdom) : "")
					+ "，" + (enemies.Count > 0 ? "正与" + string.Join("、", enemies) + "交战" : "眼下没有战事");
				lines.Add(line);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[WARN] kingdom context skipped: " + ex.Message);
		}
		return lines;
	}

	private async Task RunWorldBulletinRequestAsync(double windowEndHour, long generation, WorldBulletinSelection selection, WorldBulletinText template, string systemPrompt, string userPrompt)
	{
		WorldBulletinText result = null;
		try
		{
			ApiCallResult api = await CallWeeklyReportApiDetailed(systemPrompt, userPrompt).ConfigureAwait(false);
			if (api != null && api.Success)
			{
				result = WorldBulletinPolicy.ParseResponse(api.Content, selection.Minors.Count);
				if (result == null)
				{
					Logger.Log("WorldBulletin", "[WARN] response unparsable, using template. raw=" + WorldBulletinPolicy.Truncate(api.Content, 200));
				}
			}
			else
			{
				Logger.Log("WorldBulletin", "[WARN] request failed, using template: " + (api?.ErrorMessage ?? "null"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[WARN] request threw, using template: " + ex.Message);
		}
		_worldBulletinMainThreadActions.Enqueue(delegate
		{
			CompleteWorldBulletin(windowEndHour, generation, selection, template, result);
		});
	}

	private void ProcessWorldBulletinMainThreadActions()
	{
		int processed = 0;
		while (processed < 4 && _worldBulletinMainThreadActions.TryDequeue(out Action action))
		{
			processed++;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("WorldBulletin", "[ERROR] main-thread action failed: " + ex);
			}
		}
	}

	private void CompleteWorldBulletin(double windowEndHour, long generation, WorldBulletinSelection selection, WorldBulletinText template, WorldBulletinText generated)
	{
		if (SaveRuntimeGuard.IsStale(generation, "world_bulletin_complete"))
		{
			return;
		}
		_worldBulletinInFlight = false;
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		WorldBulletinScopeState scope = state.World;
		if (Math.Abs(scope.WindowEndHour - windowEndHour) > 0.001)
		{
			return;
		}
		// Switched off (or auto reports disabled) while the request was out: close the window, publish nothing.
		if (!IsWorldBulletinPublishingEnabled())
		{
			WorldBulletinPolicy.AbandonWindow(scope);
			Logger.Log("WorldBulletin", "[Publish] skipped: bulletin publishing turned off during the request");
			return;
		}
		try
		{
			PublishWorldBulletin(scope, selection, template, generated);
		}
		catch (Exception ex)
		{
			// Never leave the window due after a failure, or every hourly tick would re-request the LLM.
			if (Math.Abs(scope.WindowEndHour - windowEndHour) <= 0.001)
			{
				WorldBulletinPolicy.AbandonWindow(scope);
			}
			Logger.Log("WorldBulletin", "[ERROR] publish failed, window closed: " + ex);
		}
	}

	private void PublishWorldBulletin(WorldBulletinScopeState scope, WorldBulletinSelection selection, WorldBulletinText template, WorldBulletinText generated)
	{
		WorldBulletinText text = generated ?? template;
		string title = string.IsNullOrWhiteSpace(text.Title) ? template.Title : text.Title;
		int polishedMinors = 0;
		List<string> minors = generated != null ? WorldBulletinPolicy.MergeMinors(generated.Minors, selection.Minors, out polishedMinors) : template.Minors;
		if (generated != null && polishedMinors < selection.Minors.Count)
		{
			Logger.Log("WorldBulletin", "[WARN] model returned " + polishedMinors + "/" + selection.Minors.Count + " minors; template lines fill the rest");
		}
		string body = WorldBulletinPolicy.BuildBody(text.Major, minors);
		string shortText = !string.IsNullOrWhiteSpace(text.Short) ? WorldBulletinPolicy.Truncate(text.Short, 140) : template.Short;
		double now = GetCurrentCampaignHourSafe();
		WorldBulletinPolicy.CompletePublish(scope, now, selection.Major.Key);
		int day = GetCurrentGameDayIndexSafe();
		string seq = scope.Sequence.ToString(CultureInfo.InvariantCulture);
		// The single bulletin: world-kind record (diplomacy history and the NPC "world" layer read it), and it pops.
		string eventId = "weekly_report:world" + WorldBulletinBulletinIdMarker + seq + ":" + day;
		UpsertWorldBulletinRecord(eventId, "world", "", title, shortText, body, day);
		_worldBulletinLatestEventId = eventId;
		RecordWorldBulletinLayout(eventId, selection);
		QueueWeeklyReportMapNotice(eventId);
		Logger.Log("WorldBulletin", "[Publish] id=" + eventId + " llm=" + (generated != null) + " major=" + selection.Major.Key + " majorFacts=" + selection.MajorFacts.Count + " minors=" + polishedMinors + "/" + selection.Minors.Count + " majorChars=" + (text.Major ?? "").Length);
	}

	private void UpsertWorldBulletinRecord(string eventId, string eventKind, string scopeKingdomId, string title, string shortSummary, string summary, int day)
	{
		_eventRecordEntries ??= new List<EventRecordEntry>();
		EventRecordEntry entry = FindWeeklyReportRecordById(eventId);
		string previous = BuildPublishedWorldWeeklyProductState(entry);
		if (entry == null)
		{
			entry = new EventRecordEntry { EventId = eventId };
			_eventRecordEntries.Add(entry);
		}
		entry.EventKind = eventKind;
		entry.ScopeKingdomId = scopeKingdomId ?? "";
		entry.WeekIndex = Math.Max(0, day / 7);
		entry.Title = NeutralizeWeeklyReportScenarioName(title);
		entry.Summary = NeutralizeWeeklyReportScenarioName(summary);
		entry.ShortSummary = BuildFallbackWeeklyReportShortSummary(shortSummary);
		if (string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			entry.ShortSummary = BuildFallbackWeeklyReportShortSummary(entry.Summary);
		}
		entry.TagText = "";
		entry.PromptText = "";
		entry.CreatedDay = day;
		entry.CreatedDate = GetCurrentGameDateTextSafe();
		entry.Materials = new List<EventMaterialReference>();
		NotifyPublishedWorldWeeklyProductChanged(previous, entry);
		NotifyWorldMessageWeeklyTimelineChanged();
	}

	// ---------- weekly kingdom briefs ----------

	// Local template only: an archive line per kingdom for the browser and the stability history.
	// NPCs read live facts (MyBehavior.WorldBulletinNpc.cs), so no LLM polish is spent here.
	private void WriteWorldBulletinKingdomBriefs(WorldBulletinSaveState state, int day)
	{
		int week = day / 7;
		if (week < 1 || week <= state.LastKingdomWeek)
		{
			return;
		}
		state.LastKingdomWeek = week;
		int startDay = week * 7 - 7;
		bool fullWindow = state.TrackingStartDay >= 0 && state.TrackingStartDay <= startDay;
		int written = 0;
		foreach (Kingdom kingdom in GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport))
		{
			string kingdomId = GetKingdomId(kingdom);
			List<WorldBulletinEvent> facts = state.Events.Where(e => e != null && e.Day >= startDay && WorldBulletinPolicy.InvolvesKingdom(e, kingdomId)).ToList();
			if (facts.Count == 0 && !fullWindow)
			{
				continue;
			}
			string name = GetKingdomDisplayName(kingdom);
			string template = WorldBulletinPolicy.BuildKingdomTemplate(name, facts);
			string eventId = "weekly_report:kingdom:" + week + ":" + kingdomId + ":brief";
			UpsertWorldBulletinRecord(eventId, "kingdom", kingdomId, name + "第" + week + "周局势提要", template, template, day);
			written++;
		}
		Logger.Log("WorldBulletin", "[KingdomBrief] week=" + week + " written=" + written);
	}

	// ---------- persistence / lifecycle ----------

	private void SyncWorldBulletinData(IDataStore dataStore)
	{
		try
		{
			if (dataStore.IsSaving)
			{
				// Preserve unreadable input even after an hourly tick or a new fact initializes state.
				// New facts remain saveable; the recovery payload survives subsequent successful loads.
				if (_worldBulletinState != null && _worldBulletinCorruptRaw != null)
				{
					_worldBulletinState.PreservedUnreadableState = _worldBulletinCorruptRaw;
				}
				string json = _worldBulletinState != null ? JsonConvert.SerializeObject(_worldBulletinState) : (_worldBulletinCorruptRaw ?? "");
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, WorldBulletinStorageKey, json, "WorldBulletin");
				return;
			}
			_worldBulletinCorruptRaw = null;
			string loaded = CampaignSaveChunkHelper.LoadChunkedString(dataStore, WorldBulletinStorageKey, "WorldBulletin") ?? "";
			try
			{
				_worldBulletinState = string.IsNullOrWhiteSpace(loaded) ? null : JsonConvert.DeserializeObject<WorldBulletinSaveState>(loaded);
			}
			catch (Exception ex)
			{
				_worldBulletinState = null;
				_worldBulletinCorruptRaw = loaded;
				Logger.Log("WorldBulletin", "[ERROR] saved state unreadable, preserved raw (" + loaded.Length + " chars) key=" + WorldBulletinStorageKey + ": " + ex.Message);
			}
			ResetWorldBulletinTransientState();
		}
		catch (Exception ex)
		{
			// Save-path failures keep the in-memory state; only the load path may reset it.
			if (!dataStore.IsSaving)
			{
				_worldBulletinState = null;
				ResetWorldBulletinTransientState();
			}
			Logger.Log("WorldBulletin", "[ERROR] SyncData isolated (saving=" + dataStore.IsSaving + "): " + ex.Message);
		}
	}

	private void ResetWorldBulletinTransientState()
	{
		_worldBulletinInFlight = false;
		_worldBulletinLastPruneDay = -1;
		_worldBulletinFocus = null;
		_worldBulletinFocusDay = -1;
		_worldBulletinFocusOwnKingdomId = "";
		_worldBulletinDeathSnapshots.Clear();
		_worldBulletinLatestEventId = "";
		_worldBulletinCachedRecords = null;
		_worldBulletinCachedRecordCount = -1;
		_worldBulletinCachedRecordIndex = -1;
		while (_worldBulletinMainThreadActions.TryDequeue(out _))
		{
		}
	}

	private void ResetWorldBulletinForRuntime(string reason)
	{
		ResetWorldBulletinTransientState();
		// A new campaign never runs a load SyncData, so the previous campaign's state must not leak into it.
		if (string.Equals(reason, "new_game_created", StringComparison.Ordinal))
		{
			_worldBulletinState = null;
			_worldBulletinCorruptRaw = null;
		}
	}
}
