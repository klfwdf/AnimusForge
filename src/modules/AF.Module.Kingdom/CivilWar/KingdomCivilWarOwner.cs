using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Main-thread owner for the v2 civil-war state machine. Event handlers only add points;
// all rolls, membership changes and game actions happen in the weekly maintenance slice.
internal sealed class KingdomCivilWarOwner
{
	internal const int PanelPageSize = 8;
	private const int MaxHistory = 8;
	private const int MaxClanGrievance = 100;
	private const string FactionPrefix = "civilwar-faction-";
	private readonly KingdomCivilWarStorage _storage = new KingdomCivilWarStorage();
	private readonly Dictionary<string, int> _oppositionLoyaltyBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _openWarKingdoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	internal KingdomCivilWarStorage Storage { get { return _storage; } }

	internal void Replace(KingdomCivilWarStorage loaded)
	{
		_storage.Kingdoms.Clear();
		_oppositionLoyaltyBySettlement.Clear();
		_openWarKingdoms.Clear();
		if (loaded == null || loaded.Kingdoms == null) return;
		foreach (KeyValuePair<string, KingdomCivilWarKingdomState> pair in loaded.Kingdoms)
		{
			KingdomCivilWarKingdomState state = Sanitize(pair.Value);
			if (state == null) continue;
			_storage.Kingdoms[state.KingdomId] = state;
			if (state.Stage == KingdomCivilWarStage.OpenWar)
			{
				_openWarKingdoms.Add(state.KingdomId);
				IndexOppositionSettlements(state);
			}
		}
	}

	internal bool HasTrackedKingdom(Kingdom kingdom)
	{
		KingdomCivilWarKingdomState state;
		return kingdom != null && _storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) && state != null && state.Faction != null
			&& (state.Stage == KingdomCivilWarStage.FactionFormed || state.Stage == KingdomCivilWarStage.Ultimatum || state.Stage == KingdomCivilWarStage.OpenWar);
	}

	internal bool IsInOpenCivilWar(string kingdomId)
	{
		return !string.IsNullOrWhiteSpace(kingdomId) && _openWarKingdoms.Contains(kingdomId.Trim());
	}

	internal int GetOppositionLoyaltyDelta(Settlement settlement)
	{
		string id = settlement == null ? "" : settlement.StringId;
		int value;
		return !string.IsNullOrWhiteSpace(id) && _oppositionLoyaltyBySettlement.TryGetValue(id, out value) ? value : 0;
	}

	// Called by CampaignEvents. Points are keyed by source, so the same event cannot be
	// counted twice by a replayed callback. This method never scans campaign history.
	internal void AddGrievance(Kingdom kingdom, string sourceId, IEnumerable<Clan> clans, float points, int week, string text)
	{
		if (kingdom == null || !DuelSettings.IsCivilWarFactionsEnabled() || (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed())) return;
		CivilWarGrievanceSourceDef source = CivilWarCatalog.FindSource(sourceId);
		if (source == null || points <= 0f) return;
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, week);
		foreach (Clan clan in (clans ?? Enumerable.Empty<Clan>()).Where(x => x != null && x.Kingdom == kingdom && IsPoliticalClan(x)))
		{
			KingdomCivilWarClanState record = GetOrCreateClan(state, clan, week);
			float current;
			if (!record.Grievance.TryGetValue(source.Id, out current)) current = 0f;
			record.Grievance[source.Id] = CivilWarRules.Clamp(current + points, 0f, MaxClanGrievance);
		}
		if (!string.IsNullOrWhiteSpace(text)) AddHistory(state, week, text);
	}

	internal void RecordPolicyImposed(Kingdom kingdom, string policyId, int week, string text)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(policyId)) return;
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, week);
		state.LastImposedPolicyId = policyId.Trim();
		AddGrievance(kingdom, "policy_imposed", CivilWarWorld.Vassals(kingdom), 8f, week, text);
	}

	internal void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> ignoredRecentEvents)
	{
		if (kingdom == null || kingdom.IsEliminated || weekIndex <= 0 || !DuelSettings.IsCivilWarFactionsEnabled()) return;
		if (CivilWarWorld.IsPlayerRuled(kingdom) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) return;
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		KingdomCivilWarKingdomState state = GetOrCreate(kingdom, weekIndex);
		if (state.LastAdvancedWeek >= weekIndex) return;
		DecayAndRefresh(kingdom, state, weekIndex);
		state.LastAdvancedWeek = weekIndex;
		if (state.Stage == KingdomCivilWarStage.Cooldown)
		{
			if (weekIndex < state.CooldownUntilWeek) { SaveSummary(state, kingdom); return; }
			state.Stage = KingdomCivilWarStage.Discontent;
			state.Faction = null;
			state.StageWeek = weekIndex;
		}
		try
		{
			if (state.Faction == null)
				TryFormFaction(kingdom, state, weekIndex, stability, tuning);
			else if (state.Stage == KingdomCivilWarStage.FactionFormed || state.Stage == KingdomCivilWarStage.Ultimatum)
				AdvanceUltimatum(kingdom, state, weekIndex, stability, tuning, adjustStability);
			else if (state.Stage == KingdomCivilWarStage.OpenWar)
				AdvanceOpenWar(kingdom, state, weekIndex, tuning, adjustStability);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[ERROR] weekly owner failed kingdom=" + kingdom.StringId + " error=" + ex);
		}
		SaveSummary(state, kingdom);
	}

	private void TryFormFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, int stability, CivilWarTuning tuning)
	{
		if (week < state.CooldownUntilWeek) return;
		Clan leader = CivilWarWorld.Vassals(kingdom).OrderByDescending(x => TotalGrievance(GetOrCreateClan(state, x, week))).FirstOrDefault();
		if (leader == null) return;
		KingdomCivilWarClanState leaderState = GetOrCreateClan(state, leader, week);
		float grievance = TotalGrievance(leaderState);
		if (grievance < tuning.DiscontentThreshold) { state.Stage = KingdomCivilWarStage.Discontent; return; }
		CivilWarRoll roll = CivilWarRules.Roll("form_faction", CivilWarCatalog.FormFaction, BuildFeatures(kingdom, state, leader, stability), 1f, tuning, RandomFloat);
		state.LastMaxGrievance = grievance;
		if (!roll.Passed) { AddHistory(state, week, "不满达到阈值，但成派判定未通过（" + roll.Chance.ToString("0.00") + "）"); return; }
		List<CivilWarDemandDef> eligible = CivilWarCatalog.ValidDemands.Where(x => IsDemandEligible(x, kingdom, leader, leaderState, state)).ToList();
		CivilWarDemandDef demand = CivilWarDecisions.PickDemand(eligible, leaderState.Grievance, tuning, RandomFloat);
		if (demand == null) return;
		KingdomCivilWarFactionState faction = new KingdomCivilWarFactionState
		{
			Id = FactionPrefix + kingdom.StringId + "-" + (++state.FactionSerial), DemandId = demand.Id,
			LeaderClanId = leader.StringId ?? "", CreatedWeek = week, UltimatumWeek = week + tuning.UltimatumDelayWeeks,
			TargetId = ResolveTargetId(demand, kingdom, state), TargetName = ResolveTargetName(demand, kingdom, state), WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(demand)
		};
		if (demand.Target != CivilWarDemandTarget.None && string.IsNullOrWhiteSpace(faction.TargetId)) return;
		state.Faction = faction;
		state.Stage = KingdomCivilWarStage.FactionFormed;
		leaderState.Side = KingdomCivilWarSide.Opposition;
		leaderState.SideSinceWeek = week;
		AddHistory(state, week, CivilWarWorld.ClanName(leader) + "成立“" + FormatDemand(demand, faction.TargetName) + "”");
		WriteMaterial(kingdom, week, "反对派成立：" + FormatDemand(demand, faction.TargetName));
		WriteFact(kingdom, leader.Leader, "civil_war:faction:" + faction.Id, "成立" + FormatDemand(demand, faction.TargetName));
	}

	private void AdvanceUltimatum(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, int stability, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		KingdomCivilWarFactionState faction = state.Faction;
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction == null ? "" : faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction == null ? "" : faction.LeaderClanId);
		if (faction == null || demand == null || leader == null || leader.Kingdom != kingdom || leader.Leader == null || !leader.Leader.IsAlive)
		{
			Dissolve(kingdom, state, week, tuning, "诉求目标或领袖已失效");
			return;
		}
		RefreshMembership(kingdom, state, week, tuning, stability);
		if (week < faction.UltimatumWeek) return;
		state.Stage = KingdomCivilWarStage.Ultimatum;
		bool playerKing = CivilWarWorld.IsPlayerRuled(kingdom);
		if (playerKing && !faction.PlayerAnswerPending)
		{
			faction.PlayerAnswerPending = true;
			faction.PlayerPrompted = false;
			faction.PlayerAnswerDeadlineWeek = week + tuning.PlayerAnswerWeeks;
			AddHistory(state, week, "最后通牒已送达玩家国王，等待明确答复");
			return;
		}
		if (playerKing && faction.PlayerAnswerPending && week < faction.PlayerAnswerDeadlineWeek) return;
		CivilWarUltimatumResult ruling = playerKing
			? CivilWarDecisions.RollRefusal(demand, BuildFeatures(kingdom, state, leader, stability), faction.Refusals, tuning, RandomFloat)
			: CivilWarDecisions.RuleOnUltimatum(demand, BuildFeatures(kingdom, state, leader, stability), faction.Refusals, tuning, RandomFloat);
		ApplyRuling(kingdom, state, faction, demand, leader, week, stability, tuning, adjustStability, ruling);
	}

	private void ApplyRuling(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarDemandDef demand, Clan leader, int week, int stability, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, CivilWarUltimatumResult ruling)
	{
		faction.LastRuling = ruling.Ruling.ToString() + " " + CivilWarWorld.Limit(ruling.Log, 220);
		faction.LastEscalateChance = ruling.EscalateRoll.Chance;
		faction.PlayerAnswerPending = false;
		if (ruling.Ruling == CivilWarRuling.Accept)
		{
			CivilWarEffectContext ctx = BuildContext(kingdom, state, faction, demand, leader, week);
			if (CivilWarEffects.TryApply(demand.AcceptEffectId, ctx, out string reason))
			{
				AddHistory(state, week, "国王接受诉求：" + string.Join("；", ctx.Notes));
				FinishFaction(kingdom, state, week, tuning, adjustStability, 4);
			}
			else { AddHistory(state, week, "接受诉求失败：" + reason); RefuseAndReschedule(state, faction, week, tuning); }
			return;
		}
		if (ruling.Ruling == CivilWarRuling.Defer)
		{
			faction.UltimatumWeek = week + tuning.UltimatumDelayWeeks;
			AddHistory(state, week, "国王暂缓答复，最后通牒延后");
			return;
		}
		faction.Refusals++;
		AddGrievanceToFaction(state, faction, "demand_refused", 20f);
		if (ruling.ConvertToUsurp)
		{
			CivilWarDemandDef usurp = CivilWarCatalog.FindDemand(CivilWarCatalog.UsurpDemandId);
			if (usurp != null) { faction.DemandId = usurp.Id; faction.WarGoal = (int)CivilWarWarGoal.Usurp; faction.TargetId = ""; faction.TargetName = "王位"; demand = usurp; }
		}
		if (ruling.Escalate || (demand.WarGoal != CivilWarWarGoal.None && faction.Refusals >= 1))
		{
			OpenWar(kingdom, state, faction, leader, week, stability, tuning, adjustStability);
			return;
		}
		RefuseAndReschedule(state, faction, week, tuning);
		AddHistory(state, week, "国王拒绝诉求，但升级判定未通过");
	}

	private void OpenWar(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, Clan leader, int week, int stability, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			RefuseAndReschedule(state, faction, week, tuning);
			AddHistory(state, week, "玩家王国的稳定度叛乱免疫阻止了内战升级");
			return;
		}
		state.Stage = KingdomCivilWarStage.OpenWar;
		state.StageWeek = week;
		_openWarKingdoms.Add(kingdom.StringId);
		adjustStability?.Invoke(kingdom, -8);
		TeamModuleServices.CivilWar.ApplyPrestigeDelta(kingdom.StringId, -10, "内战爆发");
		List<Clan> followers = state.Clans.Values.Where(x => x.Side == KingdomCivilWarSide.Opposition).Select(x => CivilWarWorld.FindClan(x.ClanId)).Where(x => x != null && x != leader && x != Clan.PlayerClan).ToList();
		faction.WarClanIds = followers.Select(x => x.StringId).Concat(new[] { leader.StringId }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		faction.WarRequestWeek = week;
		faction.LastFactionPower = FactionPower(kingdom, faction);
		if (CivilWarWorld.IsPlayerRuled(kingdom) && string.Equals(state.PlayerSide, "opposition", StringComparison.OrdinalIgnoreCase)) faction.PlayerFollowPending = true;
		Host.QueueRebellion(kingdom, leader, followers, faction.Id, true);
		IndexOppositionSettlements(state);
		AddHistory(state, week, "内战爆发，已请求建立临时叛军王国");
		WriteMaterial(kingdom, week, "内战爆发：" + CivilWarWorld.ClanName(leader) + "领导反对派起兵");
	}

	private void AdvanceOpenWar(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		KingdomCivilWarFactionState faction = state.Faction;
		Clan leader = CivilWarWorld.FindClan(faction == null ? "" : faction.LeaderClanId);
		if (faction == null || leader == null) { EndCooldown(kingdom, state, week, tuning); return; }
		Kingdom rebel = CivilWarWorld.FindKingdom(faction.RebelKingdomId);
		float crownPower = CivilWarWorld.Strength(kingdom);
		float rebelPower = CivilWarWorld.Strength(rebel);
		faction.LastFactionPower = crownPower <= 0f ? 0f : CivilWarRules.Clamp(rebelPower / Math.Max(1f, crownPower + rebelPower), 0f, 1f);
		faction.LastWarScore = rebelPower <= 0f && crownPower <= 0f ? 0f : CivilWarRules.Clamp((rebelPower - crownPower) / Math.Max(1f, rebelPower + crownPower), -1f, 1f);
		int elapsed = Math.Max(0, week - faction.WarStartWeek);
		if (!CivilWarWorld.IsAlive(rebel) && faction.WarRequestWeek > 0 && week <= faction.WarRequestWeek + tuning.WarRequestTimeoutWeeks)
		{
			AddHistory(state, week, "叛军王国尚未完成建立，等待宿主回调");
			return;
		}
		if (elapsed < tuning.MinWarWeeks) return;
		Dictionary<string, float> features = BuildFeatures(kingdom, state, leader, 50);
		features[ CivilWarFeature.WarScore ] = faction.LastWarScore;
		features[ CivilWarFeature.WarScoreAbs ] = Math.Abs(faction.LastWarScore);
		features[ CivilWarFeature.WarProgress ] = elapsed / (float)Math.Max(1, tuning.MaxWarWeeks);
		CivilWarRoll endRoll = CivilWarRules.Roll("war_end", CivilWarCatalog.WarEnds, features, elapsed >= tuning.MaxWarWeeks ? 2f : 1f, tuning, RandomFloat);
		if (elapsed >= tuning.MaxWarWeeks || endRoll.Passed) ResolveWar(kingdom, state, week, tuning, adjustStability);
	}

	// Called by MyBehavior after a real temporary rebel kingdom is created.
	internal void OnRebelKingdomCreated(string factionId, Kingdom rebelKingdom, int week)
	{
		if (!DuelSettings.IsCivilWarFactionsEnabled() || string.IsNullOrWhiteSpace(factionId) || rebelKingdom == null) return;
		foreach (KingdomCivilWarKingdomState state in _storage.Kingdoms.Values)
		{
			KingdomCivilWarFactionState faction = state.Faction;
			if (faction == null || !string.Equals(faction.Id, factionId, StringComparison.OrdinalIgnoreCase) || state.Stage != KingdomCivilWarStage.OpenWar) continue;
			if (!string.IsNullOrWhiteSpace(faction.RebelKingdomId)) return;
			faction.RebelKingdomId = rebelKingdom.StringId ?? "";
			faction.WarStartWeek = Math.Max(week, faction.WarRequestWeek);
			faction.RebelFortShareAtStart = CivilWarWorld.FortificationCount(rebelKingdom) / (float)Math.Max(1, CivilWarWorld.FortificationCount(CivilWarWorld.FindKingdom(state.KingdomId)));
			AddHistory(state, week, "叛军王国建立：" + CivilWarWorld.KingdomName(rebelKingdom));
			return;
		}
	}

	internal bool TryJoinPlayer(Kingdom kingdom, KingdomCivilWarSide side, out string message)
	{
		message = "";
		if (kingdom == null || Clan.PlayerClan == null || Clan.PlayerClan.Kingdom != kingdom) { message = "你不属于这个王国。"; return false; }
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom)) { message = "该王国受稳定度叛乱免疫保护。"; return false; }
		KingdomCivilWarKingdomState state;
		if (!_storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) || state.Faction == null) { message = "派系尚未成形。"; return false; }
		if (side == KingdomCivilWarSide.Middle) { message = "玩家只能加入王室派或反对派。"; return false; }
		state.PlayerSide = side == KingdomCivilWarSide.Opposition ? "opposition" : "crown";
		KingdomCivilWarClanState player = GetOrCreateClan(state, Clan.PlayerClan, CivilWarWorld.CurrentWeek());
		player.Side = side; player.SideSinceWeek = CivilWarWorld.CurrentWeek();
		message = "玩家家族已加入" + (side == KingdomCivilWarSide.Opposition ? "反对派" : "王室派") + "。";
		AddHistory(state, CivilWarWorld.CurrentWeek(), message);
		return true;
	}

	internal bool TryRecruitClan(Hero recruiter, Clan target, Kingdom kingdom, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state;
		if (recruiter == null || target == null || kingdom == null || target.Kingdom != kingdom || recruiter.Clan == null || recruiter.Clan.Kingdom != kingdom || target == Clan.PlayerClan || target == kingdom.RulingClan || !_storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) || state.Faction == null) { message = "当前对象不满足派系招募条件。"; return false; }
		KingdomCivilWarClanState source;
		if (!state.Clans.TryGetValue(recruiter.Clan.StringId ?? "", out source) || source.Side == KingdomCivilWarSide.Middle) { message = "招募者没有明确派系。"; return false; }
		KingdomCivilWarClanState record = GetOrCreateClan(state, target, CivilWarWorld.CurrentWeek());
		record.Side = source.Side; record.SideSinceWeek = CivilWarWorld.CurrentWeek();
		if (source.Side == KingdomCivilWarSide.Opposition) AddGrievanceToFaction(state, state.Faction, "demand_refused", 8f);
		message = CivilWarWorld.ClanName(target) + "加入了" + (source.Side == KingdomCivilWarSide.Opposition ? "反对派" : "王室派") + "。";
		AddHistory(state, CivilWarWorld.CurrentWeek(), message);
		return true;
	}

	internal bool TryDetonate(Kingdom kingdom, int week, bool manual, Func<Kingdom, int, int> adjustStability, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state;
		if (kingdom == null || PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom) || !_storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) || state.Faction == null) { message = "当前王国不能引爆内战。"; return false; }
		if (!manual && state.LastMaxGrievance < DuelSettings.BuildCivilWarTuning().DiscontentThreshold) { message = "不满未达到引爆阈值。"; return false; }
		Clan leader = CivilWarWorld.FindClan(state.Faction.LeaderClanId);
		if (leader == null) { message = "反对派领袖已失效。"; return false; }
		OpenWar(kingdom, state, state.Faction, leader, week, MyBehavior.GetKingdomStabilityValueForExternal(kingdom), DuelSettings.BuildCivilWarTuning(), (k, d) => adjustStability?.Invoke(k, d));
		message = "内战已引爆。";
		return true;
	}

	internal bool TryAnswerPlayerUltimatum(Kingdom kingdom, bool accept, out string message)
	{
		message = "";
		KingdomCivilWarKingdomState state;
		if (kingdom == null || !_storage.Kingdoms.TryGetValue(kingdom.StringId ?? "", out state) || state.Faction == null || !state.Faction.PlayerAnswerPending) { message = "当前没有待答复的最后通牒。"; return false; }
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(state.Faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(state.Faction.LeaderClanId);
		if (accept && demand != null && leader != null)
		{
			CivilWarEffectContext ctx = BuildContext(kingdom, state, state.Faction, demand, leader, CivilWarWorld.CurrentWeek());
			if (CivilWarEffects.TryApply(demand.AcceptEffectId, ctx, out string reason)) { FinishFaction(kingdom, state, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), null, 4); message = "已接受最后通牒。"; return true; }
			message = "接受诉求失败：" + reason; return false;
		}
		state.Faction.PlayerAnswerDeadlineWeek = CivilWarWorld.CurrentWeek();
		message = "已拒绝最后通牒，系统将在周推进中判定是否升级。";
		return true;
	}

	internal IReadOnlyList<KingdomCivilWarKingdomState> ListForPanel()
	{
		return _storage.Kingdoms.Values.Where(x => x != null && x.Stage != KingdomCivilWarStage.None && x.Stage != KingdomCivilWarStage.Cooldown).OrderBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase).ToList();
	}

	internal static CivilWarPanelKingdom ToPanel(KingdomCivilWarKingdomState state)
	{
		Kingdom kingdom = CivilWarWorld.FindKingdom(state == null ? "" : state.KingdomId);
		CivilWarPanelKingdom panel = new CivilWarPanelKingdom { Name = CivilWarWorld.KingdomName(kingdom), Stage = StageName(state == null ? KingdomCivilWarStage.None : state.Stage), GrievanceText = (state == null ? 0f : state.LastMaxGrievance).ToString("0") + "/100", GrievanceBar = Math.Max(0, Math.Min(220, (int)((state == null ? 0f : state.LastMaxGrievance) * 220f / 100f))) };
		if (state == null || state.Faction == null) return panel;
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(state.Faction.DemandId);
		panel.Factions.Add(new CivilWarPanelFaction { Name = CivilWarWorld.ClanName(CivilWarWorld.FindClan(state.Faction.LeaderClanId)), Demand = demand == null ? state.Faction.DemandId : FormatDemand(demand, state.Faction.TargetName), Members = string.Join("、", state.Faction.WarClanIds.Take(6).Select(x => CivilWarWorld.ClanName(CivilWarWorld.FindClan(x)))), Color = demand == null ? "#6B3A78FF" : demand.Color, GrievanceBar = Math.Max(8, Math.Min(180, (int)(state.LastMaxGrievance * 180f / 100f))), Satisfied = state.Stage == KingdomCivilWarStage.Cooldown });
		return panel;
	}

	private void RefreshMembership(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, int stability)
	{
		foreach (Clan clan in CivilWarWorld.Vassals(kingdom))
		{
			KingdomCivilWarClanState record = GetOrCreateClan(state, clan, week);
			if (record.Side == KingdomCivilWarSide.Middle)
			{
				Dictionary<string, float> f = BuildFeatures(kingdom, state, clan, stability);
				if (CivilWarRules.Roll("join:" + clan.StringId, CivilWarCatalog.JoinOpposition, f, 1f, tuning, RandomFloat).Passed) { record.Side = KingdomCivilWarSide.Opposition; record.SideSinceWeek = week; }
			}
			else if (record.Side == KingdomCivilWarSide.Opposition && week - record.SideSinceWeek >= tuning.SideLockWeeks && CivilWarRules.Roll("leave:" + clan.StringId, CivilWarCatalog.LeaveOpposition, BuildFeatures(kingdom, state, clan, stability), 1f, tuning, RandomFloat).Passed) record.Side = KingdomCivilWarSide.Middle;
		}
	}

	private void ResolveWar(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability)
	{
		KingdomCivilWarFactionState faction = state.Faction;
		CivilWarDemandDef demand = CivilWarCatalog.FindDemand(faction.DemandId);
		Clan leader = CivilWarWorld.FindClan(faction.LeaderClanId);
		Dictionary<string, float> features = BuildFeatures(kingdom, state, leader, 50);
		features[CivilWarFeature.WarScore] = faction.LastWarScore;
		features[CivilWarFeature.WarScoreAbs] = Math.Abs(faction.LastWarScore);
		features[CivilWarFeature.WarProgress] = CivilWarRules.Clamp((week - faction.WarStartWeek) / (float)Math.Max(1, tuning.MaxWarWeeks), 0f, 1f);
		string log;
		CivilWarOutcomeDef outcome = CivilWarDecisions.PickOutcome(CivilWarCatalog.EffectiveWarGoal(demand), features, tuning, RandomFloat, out log);
		CivilWarEffectContext ctx = BuildContext(kingdom, state, faction, demand, leader, week);
		string reason = "无可用结局";
		bool applied = outcome != null && CivilWarEffects.TryApply(outcome.EffectId, ctx, out reason);
		AddHistory(state, week, applied ? outcome.Name + "：" + string.Join("；", ctx.Notes) : "内战结算失败：" + (reason ?? "无可用结局"));
		WriteMaterial(kingdom, week, applied ? string.Join("；", ctx.Notes) : "内战结算失败：" + reason);
		adjustStability?.Invoke(kingdom, applied && outcome.RebelsWon ? -4 : 6);
		FinishFaction(kingdom, state, week, tuning, adjustStability, 0);
	}

	private void FinishFaction(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, Action<Kingdom, int> adjustStability, int stabilityDelta)
	{
		if (stabilityDelta != 0) adjustStability?.Invoke(kingdom, stabilityDelta);
		state.Stage = KingdomCivilWarStage.Cooldown;
		state.StageWeek = week;
		state.CooldownUntilWeek = week + tuning.CooldownWeeks;
		state.Faction = null;
		state.PlayerSide = "";
		_openWarKingdoms.Remove(kingdom.StringId);
		ClearWarMarks(kingdom.StringId);
	}

	private void EndCooldown(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning)
	{
		if (state.Faction != null && week <= state.Faction.WarRequestWeek + tuning.WarRequestTimeoutWeeks) return;
		FinishFaction(kingdom, state, week, tuning, null, 0);
	}

	private void RefuseAndReschedule(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, int week, CivilWarTuning tuning)
	{
		faction.UltimatumWeek = week + tuning.UltimatumDelayWeeks;
		state.Stage = KingdomCivilWarStage.FactionFormed;
	}

	private void Dissolve(Kingdom kingdom, KingdomCivilWarKingdomState state, int week, CivilWarTuning tuning, string reason)
	{
		AddHistory(state, week, reason);
		FinishFaction(kingdom, state, week, tuning, null, 0);
	}

	private CivilWarEffectContext BuildContext(Kingdom kingdom, KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, CivilWarDemandDef demand, Clan leader, int week)
	{
		return new CivilWarEffectContext { Kingdom = kingdom, State = state, Faction = faction, Demand = demand, LeaderClan = leader, RebelKingdom = CivilWarWorld.FindKingdom(faction == null ? "" : faction.RebelKingdomId), Week = week, Host = Host };
	}

	private Dictionary<string, float> BuildFeatures(Kingdom kingdom, KingdomCivilWarKingdomState state, Clan clan, int stability)
	{
		KingdomCivilWarFactionState faction = state == null ? null : state.Faction;
		Clan leader = CivilWarWorld.FindClan(faction == null ? "" : faction.LeaderClanId) ?? clan;
		float total = state == null ? 0f : state.Clans.Values.Sum(TotalGrievance);
		float clanGrievance = clan == null || state == null ? 0f : TotalGrievance(GetOrCreateClan(state, clan, CivilWarWorld.CurrentWeek())) / 100f;
		float relationKing = CivilWarWorld.Relation(clan == null ? null : clan.Leader, kingdom == null ? null : kingdom.Leader) / 100f;
		float relationLeader = CivilWarWorld.Relation(clan == null ? null : clan.Leader, leader == null ? null : leader.Leader) / 100f;
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.Ordinal)
		{
			[ CivilWarFeature.KingMercy ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Mercy), [ CivilWarFeature.KingAuthoritarian ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Authoritarian), [ CivilWarFeature.KingEgalitarian ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Egalitarian), [ CivilWarFeature.KingValor ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Valor), [ CivilWarFeature.KingGenerosity ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Generosity), [ CivilWarFeature.KingHonor ] = CivilWarWorld.Trait(kingdom == null ? null : kingdom.Leader, DefaultTraits.Honor),
			[ CivilWarFeature.LeaderValor ] = CivilWarWorld.Trait(leader == null ? null : leader.Leader, DefaultTraits.Valor), [ CivilWarFeature.LeaderCalculating ] = CivilWarWorld.Trait(leader == null ? null : leader.Leader, DefaultTraits.Calculating), [ CivilWarFeature.LeaderHonor ] = CivilWarWorld.Trait(leader == null ? null : leader.Leader, DefaultTraits.Honor), [ CivilWarFeature.LeaderMercy ] = CivilWarWorld.Trait(leader == null ? null : leader.Leader, DefaultTraits.Mercy),
			[ CivilWarFeature.Instability ] = CivilWarRules.Clamp((50f - stability) / 50f, 0f, 1f), [ CivilWarFeature.WarLoad ] = CivilWarRules.Clamp(CivilWarWorld.KingdomWarCount(kingdom) / 5f, 0f, 1f), [ CivilWarFeature.KingdomGrievance ] = CivilWarRules.Clamp(total / 100f, 0f, 1f), [ CivilWarFeature.FactionPower ] = FactionPower(kingdom, faction), [ CivilWarFeature.FactionGrievance ] = faction == null || state == null ? 0f : CivilWarRules.Clamp(TotalFactionGrievance(state, faction) / 100f, 0f, 1f), [ CivilWarFeature.Refusals ] = faction == null ? 0f : CivilWarRules.Clamp(faction.Refusals / 4f, 0f, 1f),
			[ CivilWarFeature.ClanGrievance ] = clanGrievance, [ CivilWarFeature.RelationGap ] = CivilWarRules.Clamp(relationLeader - relationKing, -1f, 1f), [ CivilWarFeature.RelationToKing ] = CivilWarRules.Clamp(relationKing, -1f, 1f), [ CivilWarFeature.BloodShy ] = CivilWarRules.Clamp(Math.Max(CivilWarWorld.Trait(clan == null ? null : clan.Leader, DefaultTraits.Honor), CivilWarWorld.Trait(clan == null ? null : clan.Leader, DefaultTraits.Mercy)), 0f, 1f), [ CivilWarFeature.One ] = 1f
		};
		return result;
	}

	private static bool IsDemandEligible(CivilWarDemandDef demand, Kingdom kingdom, Clan leader, KingdomCivilWarClanState leaderState, KingdomCivilWarKingdomState state)
	{
		if (demand == null || leader == null || leaderState == null || CivilWarWorld.FortificationCount(leader) < demand.MinFortifications || CivilWarWorld.Relation(leader.Leader, kingdom.Leader) > demand.MaxLeaderRelationToKing) return false;
		return demand.Target == CivilWarDemandTarget.None || !string.IsNullOrWhiteSpace(ResolveTargetId(demand, kingdom, state));
	}

	private static string ResolveTargetId(CivilWarDemandDef demand, Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		if (demand == null || kingdom == null) return "";
		if (demand.Target == CivilWarDemandTarget.EnemyKingdom) return CivilWarWorld.StrongestEnemy(kingdom, "")?.StringId ?? "";
		if (demand.Target == CivilWarDemandTarget.ImposedPolicy) return state?.LastImposedPolicyId ?? "";
		return "";
	}

	private static string ResolveTargetName(CivilWarDemandDef demand, Kingdom kingdom, KingdomCivilWarKingdomState state)
	{
		if (demand == null) return "";
		if (demand.Target == CivilWarDemandTarget.EnemyKingdom) return CivilWarWorld.KingdomName(CivilWarWorld.StrongestEnemy(kingdom, ""));
		if (demand.Target == CivilWarDemandTarget.ImposedPolicy) return state == null ? "现行政策" : "现行政策";
		return "";
	}

	private static string FormatDemand(CivilWarDemandDef demand, string target)
	{
		return (demand == null ? "未命名诉求" : demand.Text).Replace("{target}", string.IsNullOrWhiteSpace(target) ? "目标" : target);
	}

	private KingdomCivilWarKingdomState GetOrCreate(Kingdom kingdom, int week)
	{
		string id = kingdom.StringId ?? "";
		KingdomCivilWarKingdomState state;
		if (!_storage.Kingdoms.TryGetValue(id, out state)) { state = new KingdomCivilWarKingdomState { KingdomId = id, Stage = KingdomCivilWarStage.Discontent, StageWeek = week }; _storage.Kingdoms[id] = state; }
		if (state.Clans == null) state.Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		return state;
	}

	private static KingdomCivilWarClanState GetOrCreateClan(KingdomCivilWarKingdomState state, Clan clan, int week)
	{
		if (state == null || clan == null || string.IsNullOrWhiteSpace(clan.StringId)) return new KingdomCivilWarClanState();
		KingdomCivilWarClanState record;
		if (!state.Clans.TryGetValue(clan.StringId, out record) || record == null) { record = new KingdomCivilWarClanState { ClanId = clan.StringId, Side = KingdomCivilWarSide.Middle, SideSinceWeek = week }; state.Clans[clan.StringId] = record; }
		if (record.Grievance == null) record.Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
		return record;
	}

	private static void AddGrievanceToFaction(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction, string sourceId, float points)
	{
		if (state == null || faction == null) return;
		KingdomCivilWarClanState record;
		if (state.Clans.TryGetValue(faction.LeaderClanId, out record)) { float current; if (!record.Grievance.TryGetValue(sourceId, out current)) current = 0f; record.Grievance[sourceId] = CivilWarRules.Clamp(current + points, 0f, MaxClanGrievance); }
	}

	private static float TotalGrievance(KingdomCivilWarClanState state) { return state == null || state.Grievance == null ? 0f : state.Grievance.Values.Sum(x => CivilWarRules.Clamp(x, 0f, MaxClanGrievance)); }
	private static float TotalFactionGrievance(KingdomCivilWarKingdomState state, KingdomCivilWarFactionState faction) { KingdomCivilWarClanState x; return state != null && faction != null && state.Clans.TryGetValue(faction.LeaderClanId, out x) ? TotalGrievance(x) : 0f; }
	private static float FactionPower(Kingdom kingdom, KingdomCivilWarFactionState faction)
	{
		if (kingdom == null || faction == null) return 0f;
		float rebel = 0f;
		foreach (string id in faction.WarClanIds ?? new List<string>()) rebel += CivilWarWorld.Strength(CivilWarWorld.FindClan(id));
		return CivilWarRules.Clamp(rebel / Math.Max(1f, CivilWarWorld.Strength(kingdom)), 0f, 1f);
	}

	private static void DecayAndRefresh(Kingdom kingdom, KingdomCivilWarKingdomState state, int week)
	{
		foreach (KingdomCivilWarClanState clan in state.Clans.Values)
			foreach (string sourceId in clan.Grievance.Keys.ToList()) { CivilWarGrievanceSourceDef source = CivilWarCatalog.FindSource(sourceId); clan.Grievance[sourceId] = CivilWarRules.Clamp(clan.Grievance[sourceId] * (1f - (source == null ? 0.15f : source.DecayPerWeek)), 0f, MaxClanGrievance); }
		foreach (Clan clan in CivilWarWorld.LandedClans(kingdom)) GetOrCreateClan(state, clan, week);
		state.LastMaxGrievance = state.Clans.Values.Select(TotalGrievance).DefaultIfEmpty(0f).Max();
	}

	private static void SaveSummary(KingdomCivilWarKingdomState state, Kingdom kingdom)
	{
		if (state == null) return;
		state.LastMaxGrievance = state.Clans.Values.Select(TotalGrievance).DefaultIfEmpty(0f).Max();
	}

	private static void AddHistory(KingdomCivilWarKingdomState state, int week, string text)
	{
		if (state == null || string.IsNullOrWhiteSpace(text)) return;
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		state.History.Add(new KingdomCivilWarHistoryEntry { Week = week, Text = CivilWarWorld.Limit(text, 240) });
		while (state.History.Count > MaxHistory) state.History.RemoveAt(0);
	}

	private static void WriteMaterial(Kingdom kingdom, int week, string text) { CivilWarCampaignBehavior.RecordMaterial(kingdom, week, CivilWarWorld.Limit(text, 400)); }
	private static void WriteFact(Kingdom kingdom, Hero hero, string key, string text) { if (hero != null) MyBehavior.RecordNpcActionForExternal(hero, text, key, "civil_war", true, true, kingdom == null ? null : kingdom.Leader, null, CivilWarWorld.KingdomName(kingdom), false, null); }

	private void IndexOppositionSettlements(KingdomCivilWarKingdomState state)
	{
		Kingdom kingdom = CivilWarWorld.FindKingdom(state.KingdomId);
		if (kingdom == null || state.Clans == null) return;
		foreach (KingdomCivilWarClanState clanState in state.Clans.Values.Where(x => x.Side == KingdomCivilWarSide.Opposition))
		{
			Clan clan = CivilWarWorld.FindClan(clanState.ClanId);
			if (clan == null || clan.Settlements == null) continue;
			foreach (Settlement settlement in clan.Settlements.Where(x => x != null && (x.IsTown || x.IsCastle))) _oppositionLoyaltyBySettlement[settlement.StringId] = -1;
		}
	}

	private void ClearWarMarks(string kingdomId)
	{
		foreach (string id in _oppositionLoyaltyBySettlement.Keys.ToList()) if (CivilWarWorld.FindClanOwnerKingdom(id) == kingdomId) _oppositionLoyaltyBySettlement.Remove(id);
	}

	private static bool IsPoliticalClan(Clan clan) { return clan != null && !clan.IsEliminated && !clan.IsBanditFaction && !clan.IsMinorFaction && !clan.IsUnderMercenaryService && !clan.IsClanTypeMercenary; }
	private static float RandomFloat() { return MBRandom.RandomFloat; }
	private static string StageName(KingdomCivilWarStage stage) { switch (stage) { case KingdomCivilWarStage.Discontent: return "不满积累"; case KingdomCivilWarStage.FactionFormed: return "派系成形"; case KingdomCivilWarStage.Ultimatum: return "最后通牒"; case KingdomCivilWarStage.OpenWar: return "内战"; case KingdomCivilWarStage.Cooldown: return "冷却"; default: return "无"; } }

	private static KingdomCivilWarKingdomState Sanitize(KingdomCivilWarKingdomState state)
	{
		if (state == null || string.IsNullOrWhiteSpace(state.KingdomId)) return null;
		state.KingdomId = state.KingdomId.Trim();
		if (state.Clans == null) state.Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
		if (state.History == null) state.History = new List<KingdomCivilWarHistoryEntry>();
		foreach (KingdomCivilWarClanState clan in state.Clans.Values) { if (clan.Grievance == null) clan.Grievance = new Dictionary<string, float>(StringComparer.Ordinal); foreach (string key in clan.Grievance.Keys.ToList()) clan.Grievance[key] = CivilWarRules.Clamp(clan.Grievance[key], 0f, MaxClanGrievance); }
		if (state.Faction != null && CivilWarCatalog.FindDemand(state.Faction.DemandId) == null) { Logger.Log("KingdomCivilWar", "[WARN] dropped unknown demand " + state.Faction.DemandId); state.Faction = null; state.Stage = KingdomCivilWarStage.Discontent; }
		return state;
	}

	private static readonly ICivilWarHost Host = new CivilWarHost();
	private sealed class CivilWarHost : ICivilWarHost
	{
		public void AdjustStability(Kingdom kingdom, int delta, string reason) { MyBehavior.TryAdjustKingdomStabilityForExternal(kingdom, delta, reason, out _, out _); }
		public int GetStability(Kingdom kingdom) { return MyBehavior.GetKingdomStabilityValueForExternal(kingdom); }
		public bool DiscontinueLandlessKingdom(Kingdom kingdom, string reason) { return MyBehavior.TryDiscontinueLandlessKingdomForExternal(kingdom, reason); }
		public void QueueRebellion(Kingdom kingdom, Clan leader, List<Clan> followers, string factionId, bool startNow) { MyBehavior.QueueCivilWarRebellionForExternal(kingdom, leader, followers, factionId, startNow); }
		public void ApplyPrestige(Kingdom kingdom, int delta, string reason) { TeamModuleServices.CivilWar.ApplyPrestigeDelta(kingdom == null ? "" : kingdom.StringId, delta, reason); }
		public void RecordMaterial(Kingdom kingdom, int week, string text) { WriteMaterial(kingdom, week, text); }
		public void RecordFact(Kingdom kingdom, Hero hero, string key, string text) { WriteFact(kingdom, hero, key, text); }
	}
}
