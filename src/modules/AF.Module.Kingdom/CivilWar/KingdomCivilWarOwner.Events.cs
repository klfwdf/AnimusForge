using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
	private readonly Queue<string> _politicalQueue = new Queue<string>();
	private readonly Dictionary<string, HashSet<string>> _politicalDirty = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
	private IEnumerator<bool> _politicalWork;
	private int _lastPoliticalDay = -1;
	private readonly Queue<string> _decayQueue = new Queue<string>();
	private readonly Dictionary<string, int> _decayDue = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private IEnumerator<bool> _decayWork;
	internal int LastBatchRecords { get; private set; }

	private static void MigratePoliticalDays(KingdomCivilWarKingdomState s)
	{
		if (s.CooldownUntilDay < 0) s.CooldownUntilDay = Math.Max(0, s.CooldownUntilWeek * 7);
		if (s.AftermathUntilDay < 0) s.AftermathUntilDay = Math.Max(0, s.AftermathUntilWeek * 7);
		if (s.NoPeaceUntilDay < 0) s.NoPeaceUntilDay = Math.Max(0, s.NoPeaceUntilWeek * 7);
		foreach (var f in s.Factions)
		{
			if (f.UltimatumDay < 0) f.UltimatumDay = Math.Max(0, f.UltimatumWeek * 7);
			if (f.AnswerDeadlineDay < 0) f.AnswerDeadlineDay = Math.Max(0, f.PlayerAnswerDeadlineWeek * 7);
			if (f.WarRequestDay < 0) f.WarRequestDay = Math.Max(0, f.WarRequestWeek * 7);
			if (f.WarStartDay < 0) f.WarStartDay = Math.Max(0, f.WarStartWeek * 7);
			if (f.LastDemandDay < 0) f.LastDemandDay = Math.Max(0, f.CreatedWeek * 7);
			f.DemandLocked |= f.Stage == KingdomCivilWarStage.Ultimatum || f.PlayerAnswerPending || f.Refusals > 0 || f.PendingResponse != null;
		}
	}

	private Kingdom PlayerPoliticalKingdom()
	{
		var current = Clan.PlayerClan?.Kingdom;
		if (current == null) return null;
		foreach (string crownId in _openWarKingdoms)
			if (_storage.Kingdoms.TryGetValue(crownId, out var s) && s.Factions.Any(f => f.RebelKingdomId == current.StringId && (f.LeaderClanId == Clan.PlayerClan.StringId || f.WarClanIds.Contains(Clan.PlayerClan.StringId)))) return CivilWarWorld.FindKingdom(crownId);
		return current;
	}

	internal void NotifyPoliticalChange(Kingdom kingdom, string sourceId)
	{
		if (kingdom == null || !DuelSettings.IsCivilWarFactionsEnabled()) return;
		if (IsActiveRebelKingdom(kingdom))
		{
			foreach (string id in _openWarKingdoms)
				if (_storage.Kingdoms.TryGetValue(id, out var crown) && crown.Factions.Any(x => x.RebelKingdomId == kingdom.StringId)) NotifyPoliticalChange(CivilWarWorld.FindKingdom(id), sourceId);
			return;
		}
		if (!_politicalDirty.TryGetValue(kingdom.StringId, out var sources))
		{
			sources = new HashSet<string>(StringComparer.Ordinal); _politicalDirty.Add(kingdom.StringId, sources); _politicalQueue.Enqueue(kingdom.StringId);
		}
		sources.Add(sourceId ?? "structure"); _revision++;
	}

	// Called by the campaign main-thread tick; empty queues do not allocate or scan the world.
	internal void ProcessPending()
	{
		LastBatchRecords = 0;
		if (_politicalWork == null && _politicalQueue.Count == 0 && _decayWork == null && _decayQueue.Count == 0) return;
		long started = Stopwatch.GetTimestamp();
		while (LastBatchRecords < 32)
		{
			if (_decayWork == null && _decayQueue.Count > 0)
			{
				string decayId = _decayQueue.Dequeue(); int decayDay = _decayDue[decayId]; _decayDue.Remove(decayId);
				if (_storage.Kingdoms.TryGetValue(decayId, out var decayState)) _decayWork = DecayPoliticalRecords(decayState, decayDay).GetEnumerator();
			}
			if (_decayWork != null)
			{
				try { if (!_decayWork.MoveNext()) { _decayWork.Dispose(); _decayWork = null; } }
				catch (Exception ex) { _decayWork?.Dispose(); _decayWork = null; Logger.Log("KingdomCivilWar", "decay work failed: " + ex.Message); }
				LastBatchRecords++;
				if ((Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency >= 1d) break;
				continue;
			}
			if (_politicalWork == null)
			{
				if (_politicalQueue.Count == 0) break;
				string id = _politicalQueue.Dequeue();
				if (!_politicalDirty.TryGetValue(id, out var sources)) continue;
				_politicalDirty.Remove(id);
				_politicalWork = EvaluatePoliticalKingdom(id, sources).GetEnumerator();
			}
			try { if (!_politicalWork.MoveNext()) { _politicalWork.Dispose(); _politicalWork = null; } }
			catch (Exception ex) { _politicalWork?.Dispose(); _politicalWork = null; Logger.Log("KingdomCivilWar", "political work retained for next event: " + ex); }
			LastBatchRecords++;
			if ((Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency >= 1d) break;
		}
	}

	private IEnumerable<bool> DecayPoliticalRecords(KingdomCivilWarKingdomState s, int day)
	{
		var k = CivilWarWorld.FindKingdom(s.KingdomId); float max = 0;
		foreach (var record in s.Clans.Values.ToArray())
		{
			DecayPoliticalClan(k, s, record, day); max = Math.Max(max, TotalGrievance(record)); yield return true;
		}
		s.LastMaxGrievance = max; s.LastGrievanceDecayDay = day; Revision++;
	}

	private void DecayPoliticalClan(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarClanState record, int day)
	{
		int previous = record.LastDecayDay < 0 ? s.LastGrievanceDecayDay : record.LastDecayDay;
		if (previous == day) return;
		record.LastDecayDay = day;
		if (previous < 0 || previous > day || !CivilWarWorld.IsAlive(k) || !DuelSettings.IsCivilWarFactionsEnabled()
			|| (CivilWarWorld.IsPlayerRuled(k) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) || IsActiveRebelKingdom(k)) return;
		_decaySourceKeys.Clear(); _decaySourceKeys.AddRange(record.Grievance.Keys);
		foreach (string source in _decaySourceKeys)
		{
			double retention = CivilWarCatalog.FindSource(source)?.DailyRetention ?? UnknownSourceDailyRetention;
			record.Grievance[source] = CivilWarRules.Clamp((float)(record.Grievance[source] * (day - previous == 1 ? retention : Math.Pow(retention, day - previous))), 0, MaxClanGrievance);
		}
		UpdatePoliticalGrievanceCache(record);
	}

	private static void UpdatePoliticalGrievanceCache(KingdomCivilWarClanState record)
	{
		float value = TotalGrievance(record), delta = value - record.CachedGrievance; record.CachedGrievance = value;
		if (record.Owner != null) record.Owner.CachedGrievance += delta;
		if (record.CachedFaction != null)
		{
			record.CachedFaction.GrievanceSum += delta;
			record.CachedFaction.Grievance = record.CachedFaction.GrievanceSum / Math.Max(1, record.CachedFaction.CachedMemberCount);
		}
	}

	private IEnumerable<bool> EvaluatePoliticalKingdom(string id, HashSet<string> sources)
	{
		Kingdom k = CivilWarWorld.FindKingdom(id);
		if (k != null && k.IsEliminated)
		{
			var ended = Find(k);
			if (ended != null) foreach (var f in ended.Factions.ToArray())
			{
				PublishPoliticalResult(k, ended, f, CivilWarWorld.FindClan(f.LeaderClanId), f.Id + ":war:resolved", "原王国已经消亡，派系及其内战流程终止；现存家族与王国归属保持实际状态。");
				FinishFaction(k, ended, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), null, 0); yield return true;
			}
			DropKingdom(id); Revision++; yield break;
		}
		if (!CivilWarWorld.IsAlive(k) || !DuelSettings.IsCivilWarFactionsEnabled() || IsActiveRebelKingdom(k) || (CivilWarWorld.IsPlayerRuled(k) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed())) yield break;
		int day = CivilWarWorld.CurrentDay(), week = CivilWarWorld.CurrentWeek();
		var s = GetOrCreate(k, week); var tuning = DuelSettings.BuildCivilWarTuning();
		MigratePoliticalDays(s);
		// Native clan collection can change between ticks; ids are snapshotted once per coalesced kingdom event.
		var clans = k.Clans.ToArray();
		Clan formationLeader = null; float formationGrievance = -1;
		foreach (var clan in clans)
		{
			if (PoliticalClan(clan, k))
			{
				var record = GetOrCreateClan(s, clan, week);
				EvaluatePoliticalMembership(k, s, clan, record, day, tuning);
				if (clan != k.RulingClan && clan != Clan.PlayerClan && record.Side == KingdomCivilWarSide.Middle && TotalGrievance(record) > formationGrievance)
				{ formationLeader = clan; formationGrievance = TotalGrievance(record); }
			}
			yield return true;
		}
		foreach (var record in s.Clans.Values.ToArray())
		{
			var clan = CivilWarWorld.FindClan(record.ClanId); var own = FactionOfClan(s, clan);
			if (record.Side != KingdomCivilWarSide.Middle && (own == null || IsPreWar(own)) && !PoliticalClan(clan, k)) { record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; }
			yield return true;
		}
		foreach (bool ignored in RebuildPoliticalCaches(s)) yield return true;
		foreach (var f in s.Factions.ToArray())
		{
			if (!s.Factions.Contains(f) || f.ResolutionNeedsReview) continue;
			if (IsPreWar(f) && ResolveInvalidPoliticalTarget(k, s, f)) { yield return true; continue; }
			EnsurePoliticalLeader(k, s, f);
			if (!s.Factions.Contains(f)) continue;
			f.LastFactionPower = FactionPower(k, s, f);
			if (f.PendingResponse != null)
			{
				if (f.PendingResponse.LeaderClanId != f.LeaderClanId || f.PendingResponse.RulerClanId != k.RulingClan?.StringId)
				{
					if (f.PendingResponse.Accepted) { f.ResolutionNeedsReview = true; f.ResolutionError = "已接受协议的当事方发生变化，保留执行进度待核查。"; }
					else { f.PendingResponse = null; AddHistory(s, week, "交涉当事方发生变化，未接受的旧提议已撤回。"); }
					f.Version++; yield return true; continue;
				}
				if (f.PendingResponse.Accepted)
				{
					try
					{
						string operation = f.PendingResponse.OperationId;
						var completed = CompletePoliticalResponse(k, s, f, f.PendingResponse, true);
						if (completed.Status == CivilWarActionStatus.Applied) PublishPoliticalResult(k, s, f, k.RulingClan, operation + ":settled", completed.Message);
					}
					catch (CivilWarRetryableEffectException ex) { f.ResolutionError = ex.Message; }
					catch (Exception ex) { f.ResolutionNeedsReview = true; f.ResolutionError = ex.Message; }
				}
				else if (day >= f.PendingResponse.DeadlineDay)
				{
					var r = new CivilWarActionRequest { OperationId = f.PendingResponse.OperationId + ":timeout", KingdomId = k.StringId, FactionId = f.Id, Action = CivilWarAction.Respond, Accept = false };
					// A fixed timeout operation id resolves expired saved replies without moving their deadline.
					Execute(r, CivilWarWorld.FindClan(f.LeaderClanId));
				}
				yield return true; continue;
			}
			if (!string.IsNullOrEmpty(f.PendingConcessionOperationId))
			{
				var r = new CivilWarActionRequest { OperationId = f.PendingConcessionOperationId, KingdomId = k.StringId, FactionId = f.Id, Action = CivilWarAction.Concede };
				var quote = Quote(r, k.RulingClan);
				if (quote.Allowed)
				{
					try { var result = ExecutePoliticalAction(r, k.RulingClan, k, s, f, quote, false); if (result.Status == CivilWarActionStatus.Applied) PublishPoliticalResult(k, s, f, k.RulingClan, r.OperationId + ":settled", result.Message); }
					catch (CivilWarRetryableEffectException ex) { f.ResolutionError = ex.Message; }
					catch (Exception ex) { f.ResolutionNeedsReview = true; f.ResolutionError = ex.Message; }
				}
				yield return true; continue;
			}
			if (f.Stage == KingdomCivilWarStage.OpenWar) AdvanceOpenWar(k, s, f, week, tuning, HostStability);
			else
			{
				if (!f.PlayerFounded && !f.DemandLocked && day >= f.LastDemandDay + 7 && sources.Any(x => x != "daily" && x != "weekly")) RefreshPoliticalDemand(k, s, f, day);
				bool trigger = f.EscalationPending;
				if (trigger && (f.LastEscalationDay < 0 || day >= f.LastEscalationDay + 7) && CanStartPoliticalWar(k, s, f))
				{
					f.LastEscalationDay = day; f.EscalationPending = false;
					if (RandomFloat() < EscalationChance(k, s, f)) OpenWar(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), week, tuning, HostStability);
				}
				if (IsPreWar(f)) AdvanceUltimatum(k, s, f, week, Host.GetStability(k), tuning, HostStability);
			}
			f.Version++; yield return true;
		}
		if (sources.Any(x => x != "daily" && x != "weekly")) s.FormationDueDay = s.FormationAttemptDay < 0 ? day : Math.Max(day, s.FormationAttemptDay + 7);
		if (s.FormationDueDay >= 0 && day >= s.FormationDueDay && day >= s.CooldownUntilDay)
		{
			s.FormationAttemptDay = day; s.FormationDueDay = day + 7;
			if (formationLeader != null) TryFormFaction(k, s, week, Host.GetStability(k), tuning, formationLeader);
		}
		foreach (bool ignored in RebuildPoliticalCaches(s)) yield return true; Revision++;
	}

	private void EvaluatePoliticalMembership(Kingdom k, KingdomCivilWarKingdomState s, Clan clan, KingdomCivilWarClanState record, int day, CivilWarTuning tuning)
	{
		if (clan == Clan.PlayerClan || clan == k.RulingClan || record.Side == KingdomCivilWarSide.Crown || s.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar || x.LeaderClanId == clan.StringId)
			|| day < record.MembershipEvaluationDay + 7 || _storage.ClanExitUntilDay.TryGetValue(clan.StringId, out int until) && day < until) return;
		record.MembershipEvaluationDay = day;
		var own = FactionOfClan(s, clan);
		if (own != null)
		{
			if (day < (record.SideSinceDay < 0 ? record.SideSinceWeek * 7 : record.SideSinceDay) + tuning.SideLockWeeks * 7) return;
			if (CivilWarRules.Roll("event_leave", CivilWarCatalog.LeaveOpposition, BuildFeatures(k, s, own, clan, Host.GetStability(k)), CivilWarAftermathRules.LeaveFactor(CurrentAftermath(s, day / 7)), tuning, RandomFloat).Passed)
			{ record.Side = KingdomCivilWarSide.Middle; record.FactionId = ""; record.SideSinceWeek = day / 7; record.SideSinceDay = CivilWarWorld.CurrentDay(); }
			return;
		}
		var open = s.Factions.Where(IsPreWar).ToList(); if (open.Count == 0) return;
		int index = CivilWarFactionRules.PickFactionForClan(open.Select(x => DemandAffinity(CivilWarCatalog.FindDemand(x.DemandId), record) + RelationAffinity(clan, x)).ToList(), tuning, RandomFloat);
		if (index < 0) return;
		var target = open[index];
		if (CivilWarRules.Roll("event_join", CivilWarCatalog.JoinOpposition, BuildFeatures(k, s, target, clan, Host.GetStability(k)), 1, tuning, RandomFloat).Passed)
		{ record.Side = KingdomCivilWarSide.Opposition; record.FactionId = target.Id; record.SideSinceWeek = day / 7; record.SideSinceDay = CivilWarWorld.CurrentDay(); }
	}

	private bool ResolveInvalidPoliticalTarget(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f)
	{
		var d = CivilWarCatalog.FindDemand(f.DemandId);
		if (d == null) return false;
		string reason = null;
		if (d.Id == CivilWarCatalog.UsurpDemandId && k.RulingClan?.StringId == f.LeaderClanId) reason = "派系领袖已经继位，宣权诉求已满足。";
		if (d.Target == CivilWarDemandTarget.ImposedPolicy && !k.ActivePolicies.Any(x => x.StringId == f.TargetId)) reason = "目标政策已撤销，诉求已满足。";
		if (d.Target == CivilWarDemandTarget.EnemyKingdom)
		{
			var target = CivilWarWorld.FindKingdom(f.TargetId);
			if (!CivilWarWorld.IsAlive(target)) reason = "目标王国已失效，关闭旧诉求。";
			else if (!k.IsAtWarWith(target)) reason = d.Id == "make_peace" ? "双方已议和，诉求已满足。" : "战争已经结束，继续战争的旧诉求失效。";
		}
		if (reason == null) return false;
		PublishPoliticalResult(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), f.Id + ":target-closed", reason);
		Dissolve(k, s, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), reason); return true;
	}

	private void RefreshPoliticalDemand(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, int day)
	{
		var leader = CivilWarWorld.FindClan(f.LeaderClanId); var record = GetOrCreateClan(s, leader, CivilWarWorld.CurrentWeek());
		var current = CivilWarCatalog.FindDemand(f.DemandId);
		var candidate = CivilWarCatalog.ValidDemands.Where(x => IsDemandEligible(x, k, leader, s) && !s.Factions.Any(other => other != f && other.DemandId == x.Id))
			.OrderByDescending(x => DemandAffinity(x, record)).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
		f.LastDemandDay = day;
		if (candidate == null || candidate.Id == f.DemandId || DemandAffinity(candidate, record) <= DemandAffinity(current, record) * 1.25f) return;
		f.DemandId = candidate.Id; f.TargetId = ResolveTargetId(candidate, k, s); f.TargetName = ResolveTargetName(candidate, k, s); f.WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(candidate);
		PublishPoliticalResult(k, s, f, leader, f.Id + ":demand:" + day, "派系调整诉求为：" + FormatDemand(candidate, f.TargetName));
	}

	private bool TryAiGovernance(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f)
	{
		var features = BuildFeatures(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), Host.GetStability(k));
		var tuning = DuelSettings.BuildCivilWarTuning();
		var offers = new List<CivilWarActionRequest>();
		for (int tier = 1; tier <= 3; tier++) for (int influence = 0; influence <= 1; influence++)
		{
			var offer = new CivilWarActionRequest { KingdomId = k.StringId, FactionId = f.Id, Action = CivilWarAction.Negotiate, OfferTier = tier, OfferInfluence = influence == 1 };
			if (Quote(offer, k.RulingClan).Allowed) offers.Add(offer);
		}
		var best = offers.OrderByDescending(x => CivilWarPoliticalRules.NegotiationChance(features, x.OfferTier, tuning)).ThenBy(x => x.OfferTier).ThenBy(x => x.OfferInfluence).FirstOrDefault();
		var actions = new List<CivilWarActionRequest>(); var weights = new List<float>();
		foreach (var action in new[] { CivilWarAction.Negotiate, CivilWarAction.Suppress, CivilWarAction.ForceDissolve, CivilWarAction.Concede })
		{
			var request = action == CivilWarAction.Negotiate ? best : new CivilWarActionRequest { KingdomId = k.StringId, FactionId = f.Id, Action = action };
			if (request == null || !Quote(request, k.RulingClan).Allowed) continue;
			actions.Add(request);
			weights.Add(CivilWarPoliticalRules.GovernanceWeight(action == CivilWarAction.Negotiate ? "negotiate" : action == CivilWarAction.Suppress ? "suppress" : action == CivilWarAction.ForceDissolve ? "dissolve" : "concede", features, CivilWarCatalog.FindDemand(f.DemandId)));
		}
		if (actions.Count == 0 || weights.Sum() <= 0) return false;
		var selected = actions[CivilWarRules.PickWeighted(weights, tuning, RandomFloat)];
		selected.OperationId = "ai:" + f.Id + ":" + CivilWarWorld.CurrentDay() + ":" + selected.Action;
		Execute(selected, k.RulingClan); return true;
	}
}
