using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Map;
using WorldBulletinDeathSnapshot=AnimusForge.MyBehavior.WorldBulletinDeathSnapshot;
using TaleWorlds.CampaignSystem.Actions;using TaleWorlds.CampaignSystem.MapEvents;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
namespace AnimusForge;
// Live event and sliced missed-event capture share the existing record/material authorities.
internal sealed class WorldBulletinEventCaptureAdapter
{
 internal readonly Dictionary<string,WorldBulletinDeathSnapshot> DeathSnapshots=new Dictionary<string,WorldBulletinDeathSnapshot>(StringComparer.Ordinal);
 private readonly CampaignMaterialRecordOwner _materials;
 private readonly Func<CampaignCharacterRecordCaptureAdapter> _record;
 private readonly Action _markAll;
 private readonly Func<KingdomStabilityGameAdapter> _stability;
 private readonly Func<WorldBulletinStateOwner> _bulletin;
 internal WorldBulletinFocus Focus;
 internal int FocusDay=-1;
 internal string FocusOwnKingdomId="";
 internal WorldBulletinEventCaptureAdapter(CampaignMaterialRecordOwner materials, Func<CampaignCharacterRecordCaptureAdapter> record, Action markAll, Func<KingdomStabilityGameAdapter> stability=null,Func<WorldBulletinStateOwner> bulletin=null) { _materials=materials;_record=record;_markAll=markAll;_stability=stability;_bulletin=bulletin; }
internal List<Kingdom> _missedStrategicWorldEventMaintenanceKingdoms;
internal HashSet<string> _missedStrategicWorldEventMaintenanceStableKeys;
internal int _missedStrategicWorldEventMaintenanceCursor;
internal void TryRecordMissedStrategicWorldEvents()
	{
		try
		{
			HashSet<string> existingStableKeys = _materials.BuildEventSourceMaterialStableKeySet();
			foreach (Kingdom kingdom in EventEditorProjection.GetDevEditableKingdoms())
			{
				if (kingdom == null || !kingdom.IsEliminated)
				{
					continue;
				}
				string kingdomId = GetKingdomId(kingdom);
				string stableKey = NpcActionLedger.NormalizeStableKey("kingdom_destroyed:" + kingdomId, "");
				if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(stableKey) || existingStableKeys.Contains(stableKey))
				{
					continue;
				}
				RecordKingdomDestroyedMaterial(kingdom, "daily_scan");
				existingStableKeys.Add(stableKey);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] TryRecordMissedStrategicWorldEvents: " + ex.Message);
		}
	}

internal bool ProcessMissedStrategicWorldEventsSlice()
	{
		try
		{
			if (_missedStrategicWorldEventMaintenanceKingdoms == null)
			{
				_missedStrategicWorldEventMaintenanceKingdoms = EventEditorProjection.GetDevEditableKingdoms();
				_missedStrategicWorldEventMaintenanceStableKeys = _materials.BuildEventSourceMaterialStableKeySet();
				_missedStrategicWorldEventMaintenanceCursor = 0;
			}
			if (_missedStrategicWorldEventMaintenanceCursor < _missedStrategicWorldEventMaintenanceKingdoms.Count)
			{
				Kingdom kingdom = _missedStrategicWorldEventMaintenanceKingdoms[_missedStrategicWorldEventMaintenanceCursor++];
				string kingdomId = GetKingdomId(kingdom);
				string stableKey = NpcActionLedger.NormalizeStableKey("kingdom_destroyed:" + kingdomId, "");
				if (kingdom != null && kingdom.IsEliminated && !string.IsNullOrWhiteSpace(stableKey) && !_missedStrategicWorldEventMaintenanceStableKeys.Contains(stableKey))
				{
					RecordKingdomDestroyedMaterial(kingdom, "daily_scan");
					_missedStrategicWorldEventMaintenanceStableKeys.Add(stableKey);
				}
				return false;
			}
			ResetMissedStrategicWorldEventMaintenance();
			return true;
		}
		catch (Exception ex)
		{
			ResetMissedStrategicWorldEventMaintenance();
			Logger.Log("EventMaterial", "[ERROR] deferred strategic world event scan failed: " + ex.Message);
			return true;
		}
	}

internal void ResetMissedStrategicWorldEventMaintenance()
	{
		_missedStrategicWorldEventMaintenanceKingdoms = null;
		_missedStrategicWorldEventMaintenanceStableKeys = null;
		_missedStrategicWorldEventMaintenanceCursor = 0;
	}

internal void RecordKingdomDestroyedMaterial(Kingdom destroyedKingdom, string source)
	{
		if (destroyedKingdom == null)
		{
			return;
		}
		string kingdomId = GetKingdomId(destroyedKingdom);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return;
		}
		string kingdomDisplayName = GetKingdomDisplayName(destroyedKingdom, "某个王国");
		string text = BuildKingdomDestroyedSnapshotText(destroyedKingdom, GetLastKingdomRulingClan(destroyedKingdom));
		string stableKey = "kingdom_destroyed:" + kingdomId;
		_record().RecordEventSourceMaterial("kingdom_destroyed", "王国覆灭 - " + kingdomDisplayName, text, stableKey, kingdomId, "", includeInWorld: true, includeInKingdom: true);
		Logger.Log("EventMaterial", "[KINGDOM_DESTROYED] source=" + (source ?? "") + " kingdom=" + kingdomId + " name=" + kingdomDisplayName);
	}

internal static string BuildKingdomDestroyedSnapshotText(Kingdom destroyedKingdom, Clan rulingClan)
	{
		string kingdomDisplayName = GetKingdomDisplayName(destroyedKingdom, "某个王国");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(kingdomDisplayName).Append("已经覆灭。这是世界格局级事件，周报必须记录为该王国政治实体的终结。");
		if (rulingClan != null)
		{
			stringBuilder.Append(" 覆灭时的执政家族是").Append(GetClanDisplayName(rulingClan)).Append("。");
			if (rulingClan.Leader != null)
			{
				stringBuilder.Append(" 末代统治者是").Append(GetHeroDisplayName(rulingClan.Leader)).Append("。");
			}
		}
		List<string> clans = new List<string>();
		try
		{
			clans = Clan.All.Where((Clan x) => x != null && x.Kingdom == destroyedKingdom).Select(GetClanDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
		}
		catch
		{
		}
		if (clans.Count > 0)
		{
			stringBuilder.Append(" 仍可识别的相关家族包括：").Append(string.Join("、", clans)).Append("。");
		}
		List<string> settlements = new List<string>();
		try
		{
			settlements = Settlement.All.Where((Settlement x) => x != null && x.MapFaction == destroyedKingdom && x.IsFortification).Select(GetSettlementDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
		}
		catch
		{
		}
		if (settlements.Count > 0)
		{
			stringBuilder.Append(" 覆灭时仍归属该王国的要塞记录包括：").Append(string.Join("、", settlements)).Append("。");
		}
		else
		{
			stringBuilder.Append(" 覆灭时本模组未再检测到该王国掌控的城镇或城堡。");
		}
		return stringBuilder.ToString().Trim();
	}

internal void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		_markAll();
		try
		{
			RecordKingdomDestroyedMaterial(destroyedKingdom, "event");
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnKingdomDestroyed: " + ex.Message);
		}
	}

internal WorldBulletinFocus GetWorldBulletinFocus()
	{
		try
		{
			string own = GetKingdomId(Clan.PlayerClan?.Kingdom);
			int day = GetCurrentGameDayIndexSafe();
			if (Focus == null || day != FocusDay || !string.Equals(own, FocusOwnKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				List<string> nearest = GetKingdomIdsByPlayerProximity(EventEditorProjection.GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select(k => GetKingdomId(k)))
					.Where(x => !string.IsNullOrWhiteSpace(x)).Take(3).ToList();
				Focus = new WorldBulletinFocus
				{
					PlayerKingdomId = own.Length > 0 ? own : nearest.FirstOrDefault() ?? "",
					NearbyKingdomIds = new HashSet<string>(nearest, StringComparer.OrdinalIgnoreCase)
				};
				FocusDay = day;
				FocusOwnKingdomId = own;
			}
			return Focus;
		}
		catch
		{
			return new WorldBulletinFocus();
		}
	}

internal List<string> BuildWorldBulletinKingdomContext(WorldBulletinSelection selection)
	{
		List<string> lines = new List<string>();
		try
		{
			List<string> ids = selection.MajorFacts.SelectMany(e => e.KingdomIds ?? new List<string>())
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
			List<Kingdom> all = EventEditorProjection.GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).ToList();
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
				bool stabilityEnabled = DuelSettings.IsKingdomStabilityAndRebellionEnabled();
                string line = WorldBulletinCampaignMaterialPolicy.KingdomContextLine(GetKingdomDisplayName(kingdom), GetHeroDisplayName(kingdom.Leader), towns, castles, stabilityEnabled,
                    stabilityEnabled ? _stability().GetKingdomStabilityValue(kingdom) : 0, enemies);
				lines.Add(line);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[WARN] kingdom context skipped: " + ex.Message);
		}
		return lines;
	}

internal WorldBulletinPromptFacts CaptureWorldBulletinPromptFacts(WorldBulletinSelection selection,WorldBulletinFocus focus) { Kingdom home=FindKingdomById(focus.PlayerKingdomId);return new WorldBulletinPromptFacts {ScopeLine="快报视角：天下大事，但以"+(home!=null?GetKingdomDisplayName(home):"玩家所在地区")+"读者关心的角度组织；与玩家本人或该国相关的事实优先交代",Date=GetCurrentGameDateTextSafe(),KingdomContext=BuildWorldBulletinKingdomContext(selection)}; }

internal void OnWorldBulletinWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		try
		{
			if (!(faction1 is Kingdom k1) || !(faction2 is Kingdom k2))
			{
				return;
			}
			// Inherited at founding; the founding fact already carries the story.
			if (detail == DeclareWarAction.DeclareWarDetail.CausedByKingdomCreation)
			{
				return;
			}
			string sentence = WorldBulletinCampaignMaterialPolicy.WarSentence(GetKingdomDisplayName(k1), GetKingdomDisplayName(k2));
			string detailText = WorldBulletinCampaignMaterialPolicy.WarDetail(GetHeroDisplayName(k1.Leader), GetHeroDisplayName(k2.Leader), WorldBulletinCampaignMaterialPolicy.WarReason(detail.ToString()));
			CaptureWorldBulletinEvent("war_declared", "war:" + GetKingdomId(k1) + ":" + GetKingdomId(k2) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("war_declared"), sentence, false,
				"diplomacy:" + WorldBulletinPairKey(k1, k2), detailText, BulletinParticipants((k1.Leader, "宣战方君主，未证实亲临现场"), (k2.Leader, "被宣战方君主，未证实亲临现场")), GetKingdomId(k1), GetKingdomId(k2));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] war declared: " + ex.Message);
		}
	}

internal void OnWorldBulletinMakePeace(IFaction side1, IFaction side2, MakePeaceAction.MakePeaceDetail detail)
	{
		try
		{
			if (!(side1 is Kingdom k1) || !(side2 is Kingdom k2))
			{
				return;
			}
			// AF ends a new rebel kingdom's inherited wars right at founding; that is bookkeeping, not a peace treaty.
			if (MyBehavior.QuietPeaceActive)
			{
				return;
			}
			string sentence = WorldBulletinCampaignMaterialPolicy.PeaceSentence(GetKingdomDisplayName(k1), GetKingdomDisplayName(k2));
			string detailText = WorldBulletinCampaignMaterialPolicy.PeaceDetail(GetKingdomDisplayName(k1), GetHeroDisplayName(k1.Leader), GetKingdomDisplayName(k2), GetHeroDisplayName(k2.Leader), detail == MakePeaceAction.MakePeaceDetail.ByKingdomDecision);
			if (CaptureWorldBulletinEvent("peace_made", "peace:" + GetKingdomId(k1) + ":" + GetKingdomId(k2) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("peace_made"), sentence, false,
				"diplomacy:" + WorldBulletinPairKey(k1, k2), detailText, BulletinParticipants((k1.Leader, "议和一方君主，未证实亲临现场"), (k2.Leader, "议和另一方君主，未证实亲临现场")), GetKingdomId(k1), GetKingdomId(k2)))
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

internal void OnWorldBulletinSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
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
			string ownerDetail = WorldBulletinCampaignMaterialPolicy.OwnerDetail(settlement.IsTown, oldOwner != null, GetHeroDisplayName(oldOwner), WorldBulletinHeroTitle(oldOwner), newOwner != null, GetHeroDisplayName(newOwner), WorldBulletinHeroTitle(newOwner));
			if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
			{
				Hero actor = capturerHero ?? newOwner;
				string sentence = WorldBulletinCampaignMaterialPolicy.SiegeSentence(GetHeroDisplayName(actor), name, oldName, newName);
			if (CaptureWorldBulletinEvent("settlement_siege", key, WorldBulletinCampaignMaterialPolicy.SiegePriority(settlement.IsTown), sentence, involvesPlayer, settlementGroup, ownerDetail + "；攻城统帅：" + GetHeroDisplayName(actor), BulletinParticipants((actor, "攻城统帅"), (oldOwner, "原领主，未证实在场"), (newOwner, "新领主，未证实在场")), newId, oldId))
				{
					ApplyWorldBulletinStability(newId, WorldBulletinCampaignMaterialPolicy.SiegeStability(settlement.IsTown, true));
					ApplyWorldBulletinStability(oldId, WorldBulletinCampaignMaterialPolicy.SiegeStability(settlement.IsTown, false));
				}
				return;
			}
			string label = CampaignCharacterRecordCaptureAdapter.GetSettlementOwnerChangeDetailLabel(detail);
			if (WorldBulletinCampaignMaterialPolicy.SameKingdom(newId, oldId))
			{
				string grant = WorldBulletinCampaignMaterialPolicy.GrantSentence(name, GetHeroDisplayName(newOwner), label);
				CaptureWorldBulletinEvent("fief_grant", key, WorldBulletinCampaignMaterialPolicy.EventPriority("fief_grant"), grant, involvesPlayer, "fief_grant:" + newId + ":" + GetCurrentGameDayIndexSafe(), ownerDetail, BulletinParticipants((newOwner, "获授封地者")), newId);
				return;
			}
			string transfer = WorldBulletinCampaignMaterialPolicy.TransferSentence(name, oldName, newName, label);
			CaptureWorldBulletinEvent("settlement_transfer", key, WorldBulletinCampaignMaterialPolicy.EventPriority("settlement_transfer"), transfer, involvesPlayer, settlementGroup, ownerDetail, BulletinParticipants((newOwner, "新领主"), (oldOwner, "原领主")), newId, oldId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] settlement owner changed: " + ex.Message);
		}
	}

internal void OnWorldBulletinHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
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
			string who = WorldBulletinCampaignMaterialPolicy.CapturedWho(ruler, GetKingdomDisplayName(kingdom), GetHeroDisplayName(prisoner), GetKingdomDisplayName(kingdom, GetClanDisplayName(prisoner.Clan) + "家族"));
			string sentence = WorldBulletinCampaignMaterialPolicy.CapturedSentence(who, captor != null, GetHeroDisplayName(captor));
			string kingdomId = GetKingdomId(kingdom);
			// Same group as the day's clashes between these realms, so a battle and its captives read as one story.
			string group = "clash:" + GetCurrentGameDayIndexSafe() + ":" + WorldBulletinPairKey(captor?.MapFaction, prisoner.MapFaction);
			string detailText = WorldBulletinCampaignMaterialPolicy.CapturedDetail(WorldBulletinHeroTitle(prisoner), captor != null, GetHeroDisplayName(captor), WorldBulletinHeroTitle(captor));
			if (CaptureWorldBulletinEvent(ruler ? "ruler_captured" : "lord_captured", "captured:" + GetHeroId(prisoner) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.CapturedPriority(ruler), sentence, IsWorldBulletinPlayerHero(captor),
				group, detailText, BulletinParticipants((prisoner, "被俘者"), (captor, "俘获方")), kingdomId, GetKingdomId(captor?.Clan?.Kingdom)))
			{
				ApplyWorldBulletinStability(kingdomId, WorldBulletinCampaignMaterialPolicy.CapturedStability(ruler));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] prisoner taken: " + ex.Message);
		}
	}

internal void OnWorldBulletinKingdomDestroyed(Kingdom kingdom)
	{
		try
		{
			if (kingdom == null)
			{
				return;
			}
			Hero ruler = GetLastKingdomRulingClan(kingdom)?.Leader;
			CaptureWorldBulletinEvent("kingdom_destroyed", "kingdom_destroyed:" + GetKingdomId(kingdom), WorldBulletinCampaignMaterialPolicy.EventPriority("kingdom_destroyed"), WorldBulletinCampaignMaterialPolicy.DestroyedSentence(GetKingdomDisplayName(kingdom)), false,
				"realm:" + GetKingdomId(kingdom), WorldBulletinCampaignMaterialPolicy.DestroyedDetail(GetHeroDisplayName(ruler)), BulletinParticipants((ruler, "事件相关君主，是否亲临现场依事实")), GetKingdomId(kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] kingdom destroyed: " + ex.Message);
		}
		finally
		{
			// The weekly material listener registers earlier and has already read the cached clan.
			if (kingdom != null) _discontinuedKingdomRulingClans.Remove(kingdom);
		}
	}

internal void OnWorldBulletinClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		try
		{
			if (clan == null || CivilWarRebellionExecuting)
			{
				return;
			}
			bool involvesPlayer = clan == Clan.PlayerClan;
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion && oldKingdom != null)
			{
				string sentence = WorldBulletinCampaignMaterialPolicy.RebellionSentence(GetClanDisplayName(clan), GetKingdomDisplayName(oldKingdom));
				if (CaptureWorldBulletinEvent("kingdom_rebellion", "rebellion:" + GetClanId(clan) + ":" + GetKingdomId(oldKingdom), WorldBulletinCampaignMaterialPolicy.EventPriority("kingdom_rebellion"), sentence, involvesPlayer,
					"realm:" + GetKingdomId(oldKingdom), WorldBulletinCampaignMaterialPolicy.RebellionDetail(GetHeroDisplayName(clan.Leader), GetHeroDisplayName(oldKingdom.Leader)), BulletinParticipants((clan.Leader, "反叛家族族长"), (oldKingdom.Leader, "原王国君主，未证实在场")), GetKingdomId(oldKingdom)))
				{
					ApplyWorldBulletinStability(GetKingdomId(oldKingdom), -8);
				}
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom && newKingdom != null)
			{
				string sentence = WorldBulletinCampaignMaterialPolicy.CreatedSentence(GetClanDisplayName(clan), GetKingdomDisplayName(newKingdom));
				CaptureWorldBulletinEvent("kingdom_created", "kingdom_created:" + GetKingdomId(newKingdom), WorldBulletinCampaignMaterialPolicy.EventPriority("kingdom_created"), sentence, involvesPlayer,
					"realm:" + GetKingdomId(oldKingdom ?? newKingdom), WorldBulletinCampaignMaterialPolicy.CreatedDetail(GetHeroDisplayName(clan.Leader), oldKingdom != null, GetKingdomDisplayName(oldKingdom)), BulletinParticipants((clan.Leader, "开国者")), GetKingdomId(newKingdom), GetKingdomId(oldKingdom));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] clan changed kingdom: " + ex.Message);
		}
	}

internal void OnWorldBulletinRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
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
			string sentence = WorldBulletinCampaignMaterialPolicy.RaidSentence(settlement.Name?.ToString(), GetHeroDisplayName(raider), MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(raider.MapFaction, "敌军"));
			// A realm's raids on one day collapse into a single "等N起" line.
			string owner = settlement.Village?.Bound != null ? GetSettlementDisplayName(settlement.Village.Bound) : "";
			if (CaptureWorldBulletinEvent("raid", "raid:" + GetSettlementId(settlement) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("raid"), sentence, IsWorldBulletinPlayerHero(raider),
				"raid:" + GetCurrentGameDayIndexSafe() + ":" + GetKingdomId(raider.MapFaction) + ">" + victimId, WorldBulletinCampaignMaterialPolicy.RaidDetail(owner), BulletinParticipants((raider, "劫掠方统帅")), victimId, GetKingdomId(raider.MapFaction)))
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
			CaptureWorldBulletinEvent("civil_war", "civil_war:" + (stableKey ?? GetKingdomId(kingdom)), WorldBulletinCampaignMaterialPolicy.EventPriority("civil_war"), WorldBulletinCampaignMaterialPolicy.CivilWarSentence(GetKingdomDisplayName(kingdom)), Clan.PlayerClan?.Kingdom == kingdom,
				"realm:" + GetKingdomId(kingdom), WorldBulletinCampaignMaterialPolicy.CivilWarDetail(GetHeroDisplayName(kingdom.Leader)), BulletinParticipants((kingdom.Leader, "事件相关君主，是否亲临现场依事实")), GetKingdomId(kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] civil war: " + ex.Message);
		}
	}

internal static bool IsWorldBulletinPlayerHero(Hero hero)
	{
		return hero != null && (hero == Hero.MainHero || (hero.Clan != null && hero.Clan == Clan.PlayerClan));
	}

internal static WorldBulletinParticipant[] BulletinParticipants(params (Hero Hero, string Role)[] people)
	{
		return people.Where(x => x.Hero != null).GroupBy(x => x.Hero.StringId).Take(4)
			.Select(x => new WorldBulletinParticipant { HeroId = x.Key, Name = x.First().Hero.Name?.ToString() ?? "", Role = x.First().Role }).ToArray();
	}

internal static string WorldBulletinPairKey(IFaction a, IFaction b)
	{
		string x = GetKingdomId(a);
		string y = GetKingdomId(b);
		return WorldBulletinCampaignMaterialPolicy.PairKey(x, y);
	}

internal static string WorldBulletinHeroTitle(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		if (IsWorldBulletinRuler(hero))
		{
			return WorldBulletinCampaignMaterialPolicy.HeroTitle(true, GetKingdomDisplayName(hero.Clan?.Kingdom), "", false);
		}
		string clan = GetClanDisplayName(hero.Clan);
		string kingdom = hero.Clan?.Kingdom != null ? GetKingdomDisplayName(hero.Clan.Kingdom) : "";
		bool clanLeader = hero.Clan != null && hero.Clan.Leader == hero;
		return WorldBulletinCampaignMaterialPolicy.HeroTitle(false, kingdom, clan, clanLeader);
	}

internal static bool IsWorldBulletinRuler(Hero hero)
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
 private bool CaptureWorldBulletinEvent(string kind,string key,int score,string sentence,bool involvesPlayer,string group,string detail,WorldBulletinParticipant[] participants,params string[] kingdoms) => _bulletin().CaptureWorldBulletinEvent(kind,key,score,sentence,involvesPlayer,group,detail,participants,kingdoms);
 private void ApplyWorldBulletinStability(string kingdomId,int delta) => _stability().ApplyWorldBulletinStability(kingdomId,delta,_bulletin,WorldBulletinStateOwner.IsWorldBulletinPublishingEnabled);

internal void OnWorldBulletinBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null || victim == Hero.MainHero || !victim.IsLord || !WorldBulletinStateOwner.IsWorldBulletinEnabled())
			{
				return;
			}
			DeathSnapshots[GetHeroId(victim)] = new WorldBulletinDeathSnapshot
			{
				Hero = victim,
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

internal void OnWorldBulletinHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null || victim == Hero.MainHero || !victim.IsLord)
			{
				return;
			}
			string victimId = GetHeroId(victim);
			DeathSnapshots.TryGetValue(victimId, out WorldBulletinDeathSnapshot snapshot);
			DeathSnapshots.Remove(victimId);
			bool ruler = snapshot?.Ruler ?? IsWorldBulletinRuler(victim);
			Kingdom kingdom = (snapshot != null ? FindKingdomById(snapshot.KingdomId) : null) ?? victim.Clan?.Kingdom;
			string victimTitle = snapshot?.Title ?? WorldBulletinHeroTitle(victim);
            string deathDetail = detail.ToString();
            string who = WorldBulletinCampaignMaterialPolicy.KilledWho(ruler,
                kingdom != null || ruler ? GetKingdomDisplayName(kingdom) : "", GetClanDisplayName(victim.Clan), GetHeroDisplayName(victim));
            bool natural = WorldBulletinCampaignMaterialPolicy.IsNaturalDeath(deathDetail);
            string killerName = killer != null ? GetHeroDisplayName(killer) : "";
            VengeanceExecutionFacts executionFacts = CampaignCharacterRecordCaptureAdapter.ResolvePublicExecutionFacts(victim, detail);
            bool execution = executionFacts != null || CampaignCharacterRecordCaptureAdapter.IsExecutionKillDetail(detail);
            var material = new WorldBulletinDeathMaterialCapture {
                Who = who, VictimTitle = victimTitle, Age = (int)victim.Age,
                HasKiller = killer != null, KillerName = killerName, KillerTitle = killer != null ? WorldBulletinHeroTitle(killer) : "",
                Detail = deathDetail, Execution = execution, HasPublicExecutionFacts = executionFacts != null,
                Venue = executionFacts != null ? CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionVenueText(executionFacts) : "",
                Method = executionFacts != null ? CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionMethodText(executionFacts) : "",
                Charge = executionFacts != null ? CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionChargeText(executionFacts) : "",
                ToneLabel = executionFacts?.ToneLabel, LegitimacyLabel = executionFacts?.LegitimacyLabel,
                PlayerStruck = executionFacts?.PlayerStruck ?? false,
                ExecutionPlace = execution && executionFacts == null ? CampaignCharacterRecordCaptureAdapter.ResolveHeroExecutionLocationText(CampaignCharacterRecordCaptureAdapter.ResolveHeroExecutionSettlement(victim, killer), victim, killer) : ""
            };
            string sentence = WorldBulletinCampaignMaterialPolicy.DeathSentence(material);
            int score = WorldBulletinCampaignMaterialPolicy.DeathPriority(ruler, natural);
            string kind = WorldBulletinCampaignMaterialPolicy.DeathKind(ruler);
            string kingdomId = GetKingdomId(kingdom);
            string group = WorldBulletinCampaignMaterialPolicy.DeathGroup(execution, deathDetail, GetHeroId(killer), GetHeroId(victim), GetCurrentGameDayIndexSafe(),
                deathDetail == "DiedInBattle" ? WorldBulletinPairKey(killer?.MapFaction, victim.MapFaction) : "");
            string detailText = WorldBulletinCampaignMaterialPolicy.DeathDetail(material);
			if (CaptureWorldBulletinEvent(kind, "killed:" + victimId, score, sentence, IsWorldBulletinPlayerHero(killer) || IsWorldBulletinPlayerHero(victim), group, detailText, BulletinParticipants((victim, "死者"), (natural ? null : killer, "致死方，是否亲自行刑依事实")), kingdomId, GetKingdomId(killer?.Clan?.Kingdom)))
			{
				ApplyWorldBulletinStability(kingdomId, WorldBulletinCampaignMaterialPolicy.DeathStability(ruler, natural));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] hero killed: " + ex.Message);
		}
	}

internal void OnWorldBulletinMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.HasWinner || mapEvent.IsHideoutBattle || CampaignBattleRecordCaptureAdapter.IsBanditOrMonsterMapEvent(mapEvent))
			{
				return;
			}
			MapEventSide winner = mapEvent.WinningSide == BattleSideEnum.Attacker ? mapEvent.AttackerSide : mapEvent.DefenderSide;
			MapEventSide loser = CampaignBattleRecordCaptureAdapter.GetMapEventDefeatedSide(mapEvent);
			if (winner == null || loser == null)
			{
				return;
			}
			bool involvesPlayer = mapEvent.IsPlayerMapEvent;
			bool winnerLord = CampaignBattleRecordCaptureAdapter.ShouldMentionBattleHero(winner.LeaderParty?.LeaderHero);
			bool loserLord = CampaignBattleRecordCaptureAdapter.ShouldMentionBattleHero(loser.LeaderParty?.LeaderHero);
			if (!involvesPlayer && !winnerLord && !loserLord)
			{
				return;
			}
			int troops = CampaignBattleRecordCaptureAdapter.GetMapEventTroopCount(mapEvent);
			bool sallyOut = mapEvent.IsSallyOut || mapEvent.IsSiegeOutside;
			bool siege = mapEvent.IsSiegeAssault || sallyOut;
			// Raid fights are covered by the raid event; a lord running down villagers or caravans is not news.
			bool lordVsLord = winnerLord && loserLord;
			if (!WorldBulletinCampaignMaterialPolicy.ShouldIncludeBattle(involvesPlayer, winnerLord, loserLord, mapEvent.IsRaid, siege, troops, 500))
			{
				return;
			}
			int score = WorldBulletinCampaignMaterialPolicy.BattlePriority(troops, 500, lordVsLord, siege);
			string location = CampaignBattleRecordCaptureAdapter.GetMapEventLocationLabel(mapEvent);
			string winnerFaction = GetFactionDisplayName(winner.MapFaction, "一方");
			string loserFaction = GetFactionDisplayName(loser.MapFaction, "另一方");
			string sentence = WorldBulletinCampaignMaterialPolicy.BattleSentence(location, sallyOut, siege, CampaignBattleRecordCaptureAdapter.GetPrimaryOtherSideLabel(winner), winnerFaction, CampaignBattleRecordCaptureAdapter.GetPrimaryOtherSideLabel(loser), loserFaction, troops);
			string winnerId = GetKingdomId(winner.MapFaction);
			string loserId = GetKingdomId(loser.MapFaction);
			string group = siege && mapEvent.MapEventSettlement != null
				? "siege:" + GetSettlementId(mapEvent.MapEventSettlement)
				: "clash:" + GetCurrentGameDayIndexSafe() + ":" + WorldBulletinPairKey(winner.MapFaction, loser.MapFaction);
			string detailText = WorldBulletinCampaignMaterialPolicy.BattleDetail(CampaignBattleRecordCaptureAdapter.GetMapEventSideCommittedTroopCount(winner), CampaignBattleRecordCaptureAdapter.BuildMapEventCasualtyText(winner), CampaignBattleRecordCaptureAdapter.GetMapEventSideCommittedTroopCount(loser), CampaignBattleRecordCaptureAdapter.BuildMapEventCasualtyText(loser), sallyOut, winnerLord, winnerLord ? WorldBulletinHeroTitle(winner.LeaderParty.LeaderHero) : "", loserLord, loserLord ? WorldBulletinHeroTitle(loser.LeaderParty.LeaderHero) : "");
			if (CaptureWorldBulletinEvent(sallyOut ? "sally_out_battle" : siege ? "siege_battle" : "battle", "battle:" + CampaignBattleRecordCaptureAdapter.BuildMapEventStableKey(mapEvent, location), score, sentence, involvesPlayer, group, detailText, BulletinParticipants((winner.LeaderParty?.LeaderHero, "胜方统帅"), (loser.LeaderParty?.LeaderHero, "败方统帅")), winnerId, loserId))
			{
				int swing = WorldBulletinCampaignMaterialPolicy.BattleStability(troops, 500);
				ApplyWorldBulletinStability(winnerId, swing);
				ApplyWorldBulletinStability(loserId, -swing);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] map event ended: " + ex.Message);
		}
	}

// ---------- diplomacy, succession and realm events ----------
	// All handlers below run on rare campaign callbacks; each does O(1) work plus at most one army/clan fief walk.

	// Ruler, or a child of the ruler inside the ruling clan.
	private static bool IsWorldBulletinRoyal(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		if (IsWorldBulletinRuler(hero))
		{
			return true;
		}
		Kingdom kingdom = hero.Clan?.Kingdom;
		Hero ruler = kingdom?.Leader;
		return ruler != null && hero.Clan == kingdom.RulingClan && (hero.Father == ruler || hero.Mother == ruler);
	}

	// A ruler inside KillCharacterAction is still alive when vanilla hands the crown on.
	private Hero FindDyingWorldBulletinRuler(string kingdomId)
	{
		foreach (WorldBulletinDeathSnapshot snapshot in DeathSnapshots.Values)
		{
			if (snapshot != null && snapshot.Ruler && snapshot.Hero?.IsAlive == true && string.Equals(snapshot.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase))
			{
				return snapshot.Hero;
			}
		}
		return null;
	}

	internal void OnWorldBulletinAllianceStarted(Kingdom k1, Kingdom k2) => CaptureWorldBulletinAlliance(k1, k2, true);

	internal void OnWorldBulletinAllianceEnded(Kingdom k1, Kingdom k2) => CaptureWorldBulletinAlliance(k1, k2, false);

	private void CaptureWorldBulletinAlliance(Kingdom k1, Kingdom k2, bool formed)
	{
		try
		{
			// An alliance that ends because a kingdom fell belongs to the kingdom's destruction story.
			if (k1 == null || k2 == null || k1.IsEliminated || k2.IsEliminated)
			{
				return;
			}
			string kind = formed ? "alliance_formed" : "alliance_ended";
			string first = GetKingdomDisplayName(k1);
			string second = GetKingdomDisplayName(k2);
			string sentence = formed ? WorldBulletinCampaignMaterialPolicy.AllianceFormedSentence(first, second) : WorldBulletinCampaignMaterialPolicy.AllianceEndedSentence(first, second);
			CaptureWorldBulletinEvent(kind, kind + ":" + WorldBulletinPairKey(k1, k2) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority(kind), sentence, false,
				"diplomacy:" + WorldBulletinPairKey(k1, k2), WorldBulletinCampaignMaterialPolicy.AllianceDetail(first, GetHeroDisplayName(k1.Leader), second, GetHeroDisplayName(k2.Leader)),
				BulletinParticipants((k1.Leader, "盟约一方君主，未证实亲临现场"), (k2.Leader, "盟约另一方君主，未证实亲临现场")), GetKingdomId(k1), GetKingdomId(k2));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] alliance: " + ex.Message);
		}
	}

	// Set by flows that publish their own throne-change story (Coup), so the generic succession fact is skipped.
	internal static bool SuppressWorldBulletinRulerChange;

	internal void OnWorldBulletinRulingClanChanged(Kingdom kingdom, Clan eventRulingClan)
	{
		try
		{
			Clan newClan = kingdom?.RulingClan;
			Hero ruler = newClan?.Leader;
			// At founding the clan rules before it has joined; kingdom_created already reports that.
			if (SuppressWorldBulletinRulerChange || kingdom == null || kingdom.IsEliminated || ruler == null || newClan.Kingdom != kingdom)
			{
				return;
			}
			string kingdomId = GetKingdomId(kingdom);
			// 1.3 passes the new clan and 1.4.5 the old one; a dying ruler is the reliable predecessor in both.
			Hero dying = FindDyingWorldBulletinRuler(kingdomId);
			Hero previous = dying ?? (eventRulingClan != null && eventRulingClan != newClan ? eventRulingClan.Leader : null);
			if (previous == ruler)
			{
				previous = null;
			}
			// Vanilla queues a king election and seats a random clan meanwhile; the election result is the real coronation.
			bool interim = dying != null && kingdom.UnresolvedDecisions.Any(d => d is KingSelectionKingdomDecision);
			string sentence = WorldBulletinCampaignMaterialPolicy.RulerChangedSentence(GetKingdomDisplayName(kingdom), GetHeroDisplayName(ruler), GetClanDisplayName(newClan), interim);
			string detail = WorldBulletinCampaignMaterialPolicy.RulerChangedDetail(previous != null, GetHeroDisplayName(previous), GetClanDisplayName(previous?.Clan), dying != null);
			CaptureWorldBulletinEvent("ruler_changed", "ruler_changed:" + kingdomId + ":" + GetHeroId(ruler) + ":" + GetCurrentGameDayIndexSafe() + (interim ? ":interim" : ""), WorldBulletinCampaignMaterialPolicy.RulerChangedPriority(interim), sentence,
				IsWorldBulletinPlayerHero(ruler) || IsWorldBulletinPlayerHero(previous), "realm:" + kingdomId, detail,
				BulletinParticipants((ruler, interim ? "暂掌王位者" : "新君主"), (previous, dying != null ? "前任君主，已在本次变故中身故" : "前任君主")), kingdomId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] ruling clan changed: " + ex.Message);
		}
	}

	internal void OnWorldBulletinHeroesMarried(Hero first, Hero second, bool showNotification)
	{
		try
		{
			if (first == null || second == null)
			{
				return;
			}
			bool royal = IsWorldBulletinRoyal(first) || IsWorldBulletinRoyal(second);
			bool clanLeader = (first.IsLord && first.Clan?.Leader == first) || (second.IsLord && second.Clan?.Leader == second);
			if (!royal && !clanLeader)
			{
				return;
			}
			string pair = string.CompareOrdinal(GetHeroId(first), GetHeroId(second)) <= 0 ? GetHeroId(first) + "|" + GetHeroId(second) : GetHeroId(second) + "|" + GetHeroId(first);
			string sentence = WorldBulletinCampaignMaterialPolicy.MarriageSentence(GetHeroDisplayName(first), WorldBulletinHeroTitle(first), GetHeroDisplayName(second), WorldBulletinHeroTitle(second));
			CaptureWorldBulletinEvent(royal ? "royal_marriage" : "noble_marriage", "marriage:" + pair, WorldBulletinCampaignMaterialPolicy.MarriagePriority(royal), sentence,
				IsWorldBulletinPlayerHero(first) || IsWorldBulletinPlayerHero(second), "marriage:" + pair, "",
				BulletinParticipants((first, "成婚一方"), (second, "成婚另一方")), GetKingdomId(first.Clan?.Kingdom), GetKingdomId(second.Clan?.Kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] heroes married: " + ex.Message);
		}
	}

	internal void OnWorldBulletinClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		try
		{
			// Annexation reports the whole merger once; each transferred clan is not separate news.
			if (clan == null || newKingdom == null || KingdomAnnexationBehavior.IsAnnexationInProgress)
			{
				return;
			}
			List<string> fiefs = clan.Fiefs.Where(x => x?.Settlement != null).Select(x => GetSettlementDisplayName(x.Settlement)).ToList();
			if (fiefs.Count == 0)
			{
				return;
			}
			string sentence = WorldBulletinCampaignMaterialPolicy.DefectionSentence(GetClanDisplayName(clan), fiefs.Count, oldKingdom != null, GetKingdomDisplayName(oldKingdom), GetKingdomDisplayName(newKingdom));
			// Shares the realm group, so a civil war or rebellion and its defections read as one story.
			CaptureWorldBulletinEvent("clan_defection", "defection:" + GetClanId(clan) + ":" + GetKingdomId(newKingdom) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("clan_defection"), sentence,
				clan == Clan.PlayerClan, "realm:" + GetKingdomId(oldKingdom ?? newKingdom), WorldBulletinCampaignMaterialPolicy.DefectionDetail(GetHeroDisplayName(clan.Leader), string.Join("、", fiefs.Take(4)) + (fiefs.Count > 4 ? "等" : "")),
				BulletinParticipants((clan.Leader, "改投家族族长"), (newKingdom.Leader, "接纳方君主，未证实在场")), GetKingdomId(newKingdom), GetKingdomId(oldKingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] clan defected: " + ex.Message);
		}
	}

	internal void OnWorldBulletinClanDestroyed(Clan clan)
	{
		try
		{
			// Fires before deactivation, so the kingdom and last leader are still readable.
			if (clan == null || clan == Clan.PlayerClan || !clan.IsNoble || clan.IsBanditFaction || clan.IsRebelClan)
			{
				return;
			}
			Kingdom kingdom = clan.Kingdom;
			string kingdomId = GetKingdomId(kingdom);
			string clanId = GetClanId(clan);
			CaptureWorldBulletinEvent("clan_destroyed", "clan_destroyed:" + clanId, WorldBulletinCampaignMaterialPolicy.EventPriority("clan_destroyed"),
				WorldBulletinCampaignMaterialPolicy.ClanDestroyedSentence(GetClanDisplayName(clan), kingdom != null, GetKingdomDisplayName(kingdom)), false,
				kingdom != null ? "realm:" + kingdomId : "clan_destroyed:" + clanId, WorldBulletinCampaignMaterialPolicy.ClanDestroyedDetail(clan.Leader != null ? GetHeroDisplayName(clan.Leader) : ""),
				BulletinParticipants((clan.Leader, "末任族长")), kingdomId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] clan destroyed: " + ex.Message);
		}
	}

	internal void OnWorldBulletinArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		try
		{
			Hero leader = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			Kingdom kingdom = army?.Kingdom;
			if (kingdom == null || !IsWorldBulletinRoyal(leader))
			{
				return;
			}
			// Parties already called to the army count, not only the ones that have arrived.
			int troops = 0;
			foreach (MobileParty party in army.Parties)
			{
				troops += party?.MemberRoster?.TotalManCount ?? 0;
			}
			if (troops < WorldBulletinCampaignMaterialPolicy.ArmyGatheredTroopThreshold)
			{
				return;
			}
			string place = CampaignBattleRecordCaptureAdapter.ResolveGatheringPointLabel(gatheringPoint, army.LeaderParty);
			string kingdomId = GetKingdomId(kingdom);
			// The player's own muster is not news to the player; it stays a supporting line.
			string sentence = WorldBulletinCampaignMaterialPolicy.ArmyGatheredSentence(GetKingdomDisplayName(kingdom), IsWorldBulletinRuler(leader) ? "君主" : "王储", GetHeroDisplayName(leader), troops, place == "集结地" ? "" : place);
			CaptureWorldBulletinEvent("army_gathered", "army:" + GetHeroId(leader) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("army_gathered"), sentence,
				false, "army:" + kingdomId + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.ArmyGatheredDetail(army.Parties.Count),
				BulletinParticipants((leader, "统兵者")), kingdomId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] army gathered: " + ex.Message);
		}
	}

	internal void OnWorldBulletinTownRebelliousStateChanged(Town town, bool rebellious)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			if (!rebellious || settlement == null)
			{
				return;
			}
			Hero owner = settlement.OwnerClan?.Leader;
			string settlementId = GetSettlementId(settlement);
			// Loyalty hovering at the threshold can flip daily; one unrest line per town per week.
			CaptureWorldBulletinEvent("town_unrest", "town_unrest:" + settlementId + ":" + (GetCurrentGameDayIndexSafe() / 7), WorldBulletinCampaignMaterialPolicy.EventPriority("town_unrest"),
				WorldBulletinCampaignMaterialPolicy.TownUnrestSentence(GetSettlementDisplayName(settlement), owner != null ? GetHeroDisplayName(owner) : ""), IsWorldBulletinPlayerHero(owner),
				"siege:" + settlementId, "", BulletinParticipants((owner, "城镇领主，未证实在场")), GetKingdomId(settlement.MapFaction));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] town unrest: " + ex.Message);
		}
	}

	internal void OnWorldBulletinTownRebellion(Settlement settlement, Clan oldOwnerClan)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			// Vanilla has already handed the town to the rebel clan; the owner-change fact shares this siege group.
			Clan rebels = settlement.OwnerClan != oldOwnerClan ? settlement.OwnerClan : null;
			Hero oldOwner = oldOwnerClan?.Leader;
			string settlementId = GetSettlementId(settlement);
			string sentence = WorldBulletinCampaignMaterialPolicy.TownRebellionSentence(GetSettlementDisplayName(settlement), GetClanDisplayName(oldOwnerClan) + "家族", rebels != null ? GetClanDisplayName(rebels) : "起义者");
			CaptureWorldBulletinEvent("town_rebellion", "town_rebellion:" + settlementId + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority("town_rebellion"), sentence,
				IsWorldBulletinPlayerHero(oldOwner), "siege:" + settlementId, "", BulletinParticipants((rebels?.Leader, "起义首领"), (oldOwner, "原领主，未证实在场")), GetKingdomId(oldOwnerClan?.Kingdom));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] town rebellion: " + ex.Message);
		}
	}

	// Called by KingdomAnnexationBehavior after the annexed kingdom is dissolved.
	internal void CaptureWorldBulletinAnnexation(Kingdom receiving, Kingdom annexed, Hero formerRuler, int transferredClans)
	{
		try
		{
			if (receiving == null || annexed == null)
			{
				return;
			}
			string annexedId = GetKingdomId(annexed);
			// Same realm group as the kingdom_destroyed fact the dissolution just produced.
			CaptureWorldBulletinEvent("kingdom_annexed", "kingdom_annexed:" + annexedId, WorldBulletinCampaignMaterialPolicy.EventPriority("kingdom_annexed"),
				WorldBulletinCampaignMaterialPolicy.AnnexedSentence(GetKingdomDisplayName(receiving), GetKingdomDisplayName(annexed)), IsWorldBulletinPlayerHero(receiving.Leader),
				"realm:" + annexedId, WorldBulletinCampaignMaterialPolicy.AnnexedDetail(GetHeroDisplayName(receiving.Leader), GetHeroDisplayName(formerRuler), transferredClans),
				BulletinParticipants((receiving.Leader, "吞并方君主"), (formerRuler, "被吞并国末代君主")), GetKingdomId(receiving), annexedId);
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] annexation: " + ex.Message);
		}
	}

	// Called by the vassalage owners when a treaty is signed or broken.
	internal void CaptureWorldBulletinVassalage(Kingdom suzerain, Kingdom vassal, string typeName, bool established)
	{
		try
		{
			if (suzerain == null || vassal == null)
			{
				return;
			}
			string kind = established ? "vassalage_established" : "vassalage_ended";
			string suzerainName = GetKingdomDisplayName(suzerain);
			string vassalName = GetKingdomDisplayName(vassal);
			string sentence = established ? WorldBulletinCampaignMaterialPolicy.VassalageEstablishedSentence(suzerainName, vassalName, typeName) : WorldBulletinCampaignMaterialPolicy.VassalageEndedSentence(suzerainName, vassalName, typeName);
			CaptureWorldBulletinEvent(kind, kind + ":" + GetKingdomId(suzerain) + ">" + GetKingdomId(vassal) + ":" + GetCurrentGameDayIndexSafe(), WorldBulletinCampaignMaterialPolicy.EventPriority(kind), sentence, false,
				"diplomacy:" + WorldBulletinPairKey(suzerain, vassal), WorldBulletinCampaignMaterialPolicy.VassalageDetail(GetHeroDisplayName(suzerain.Leader), GetHeroDisplayName(vassal.Leader)),
				BulletinParticipants((suzerain.Leader, "宗主国君主，未证实亲临现场"), (vassal.Leader, "臣属国君主，未证实亲临现场")), GetKingdomId(suzerain), GetKingdomId(vassal));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletin", "[ERROR] vassalage: " + ex.Message);
		}
	}


private readonly Dictionary<Kingdom, Clan> _discontinuedKingdomRulingClans = new Dictionary<Kingdom, Clan>();
internal static bool CivilWarRebellionExecuting;
internal void RememberDiscontinuedKingdomRuler(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction && oldKingdom?.RulingClan != null)
		{
			_discontinuedKingdomRulingClans[oldKingdom] = oldKingdom.RulingClan;
		}
	}
internal Clan GetLastKingdomRulingClan(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return null;
		}
		return kingdom.RulingClan ?? (_discontinuedKingdomRulingClans.TryGetValue(kingdom, out Clan clan) ? clan : null);
	}
internal void CaptureCivilNewsMaterial(string kind, string stableKey, string label, string snapshot,
        string kingdomId, string actorKingdomId, string actorHeroId, int day, bool worldCopy)
    {
        if (kind != "ruler_policy" && kind != "player_vassal_policy"
            && kind != "noble_gathering" && kind != "tournament_finished") return;
        if (!WorldBulletinStateOwner.IsWorldBulletinEnabled() || day != GetCurrentGameDayIndexSafe() || string.IsNullOrWhiteSpace(stableKey)) return;
        string newsKind = kind == "player_vassal_policy" ? "ruler_policy" : kind;
        string sentence = WorldBulletinPolicy.Truncate(snapshot, 650);
        string detail = "以已记录事实为准，不推断额外效果。";
        try
        {
            _bulletin().CaptureCivilNewsMaterial(newsKind, "material:" + day + ":" + stableKey,
                sentence, detail, actorHeroId == Hero.MainHero?.StringId, kingdomId, actorKingdomId);
        }
        catch (Exception ex) { Logger.Log("WorldBulletin", "[WARN] civil material capture: " + ex.Message); }
    }
}
