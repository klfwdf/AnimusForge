using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

internal sealed partial class KingdomCivilWarOwner
{
	private long _revision;
	private bool _hasPoliticalResponse;
	internal event Action StateChanged;
	internal long Revision { get => _revision; private set { _revision = value; StateChanged?.Invoke(); } }
	private static bool PoliticalClan(Clan c, Kingdom k) => k != null && CivilWarWorld.IsPoliticalClan(c)
        && c.Kingdom == k && c.Leader != null && c.Leader.IsAlive;
	private static bool IsGovernance(CivilWarAction a) => a == CivilWarAction.Suppress || a == CivilWarAction.Negotiate || a == CivilWarAction.Concede || a == CivilWarAction.ForceDissolve;
	private static KingdomCivilWarFactionState ActionFaction(CivilWarActionRequest r, KingdomCivilWarKingdomState s, Clan actor)
		=> r.Action == CivilWarAction.Leave ? FactionOfClan(s, actor) : r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.Found ? null : s?.Factions.FirstOrDefault(x => x.Id == r.FactionId);
	internal bool TryTakePoliticalResponse(out CivilWarActionRequest request, out string text)
	{
		request = null; text = "";
		if (!_hasPoliticalResponse || !DuelSettings.IsCivilWarFactionsEnabled()) return false;
		foreach (var s in _storage.Kingdoms.Values) foreach (var f in s.Factions)
		{
			var p = f.PendingResponse;
			if (p == null || p.Prompted || p.Accepted || f.LeaderClanId != Clan.PlayerClan?.StringId || f.ResolutionNeedsReview) continue;
			p.Prompted = true;
			request = new CivilWarActionRequest { OperationId = Guid.NewGuid().ToString("N"), KingdomId = s.KingdomId, FactionId = f.Id, Action = CivilWarAction.Respond, Version = f.Version };
			text = p.Dissolve ? "王室命令解散派系。服从将解散，抗命将起兵。" : "王室提出以 " + (p.Gold > 0 ? p.Gold + " 金币" : p.Influence + " 影响力") + " 换取撤回诉求；接受后解散派系。";
			text += "\n答复期限：第 " + p.DeadlineDay + " 天；超时视为" + (p.Dissolve ? "抗命。" : "拒绝。"); return true;
		}
		_hasPoliticalResponse = false; return false;
	}

	internal List<CivilWarFoundingOption> GetFoundingOptions(string kingdomId, Clan actor)
	{
		var result = new List<CivilWarFoundingOption>();
		Kingdom kingdom = CivilWarWorld.FindKingdom(kingdomId);
		if (!PoliticalClan(actor, kingdom) || actor == kingdom.RulingClan) return result;
		var state = Find(kingdom);
		foreach (var demand in CivilWarCatalog.ValidDemands)
		{
			if (CivilWarWorld.FortificationCount(actor) < demand.MinFortifications || actor != Clan.PlayerClan && state?.Factions.Any(x => x.DemandId == demand.Id) == true) continue;
			if (demand.Target == CivilWarDemandTarget.None) result.Add(new CivilWarFoundingOption { DemandId = demand.Id, Text = demand.Text });
			else if (demand.Target == CivilWarDemandTarget.ImposedPolicy)
			{
				foreach (var policy in kingdom.ActivePolicies) result.Add(new CivilWarFoundingOption { DemandId = demand.Id, TargetId = policy.StringId, Text = FormatDemand(demand, policy.Name.ToString()) });
			}
			else foreach (var target in kingdom.FactionsAtWarWith.OfType<Kingdom>().Where(x => !x.IsEliminated && !IsCivilWarPair(kingdom, x)))
				result.Add(new CivilWarFoundingOption { DemandId = demand.Id, TargetId = target.StringId, Text = FormatDemand(demand, CivilWarWorld.KingdomName(target)) });
		}
		return result;
	}

	internal CivilWarActionQuote Quote(CivilWarActionRequest r, Clan actor)
	{
		var q = new CivilWarActionQuote();
		if (r == null) { q.Reason = "操作不存在。"; return q; }
		Kingdom k = CivilWarWorld.FindKingdom(r.KingdomId);
		var s = Find(k);
		var f = ActionFaction(r, s, actor);
		q.Version = f?.Version ?? 0;
		if (!DuelSettings.IsCivilWarFactionsEnabled() || !CivilWarWorld.IsAlive(k)) q.Reason = "内战功能未启用或王国已失效。";
		else if (CivilWarWorld.IsPlayerRuled(k) && !DuelSettings.IsCivilWarPlayerKingdomFactionsAllowed()) q.Reason = "设置已关闭玩家王国派系。";
		else if (actor?.Leader == null || !actor.Leader.IsAlive) q.Reason = "家族领袖不具备资格。";
		else if (r.Version >= 0 && r.Version != q.Version) q.Reason = "派系状态已变化，请重新确认。";
		else if (f?.ResolutionNeedsReview == true) q.Reason = "先前操作结果待核查，暂不能继续操作。";
		else if (r.Action == CivilWarAction.Respond)
		{
			if (f?.PendingResponse == null || f.LeaderClanId != actor.StringId || f.PendingResponse.LeaderClanId != actor.StringId) q.Reason = "没有需要本家族回应的事项。";
			else if (IsPreWar(f) && !PoliticalClan(actor, k) || f.Stage == KingdomCivilWarStage.OpenWar && actor.Kingdom?.StringId != f.RebelKingdomId) q.Reason = "家族归属已变化，不能回应原协议。";
			else if (f.PendingResponse.Accepted) q.Reason = "协议已接受并进入结算，不能重复答复或撤回；等待安全重试。";
			else if (r.Accept == false && f.PendingResponse.Dissolve && actor == Clan.PlayerClan && r.OperationId != f.PendingResponse.OperationId + ":timeout" && !PlayerDetonationAllowed(k, actor, out string responseReason)) q.Reason = responseReason;
			else if (CivilWarWorld.CurrentDay() > f.PendingResponse.DeadlineDay && (r.Accept || r.OperationId != f.PendingResponse.OperationId + ":timeout")) q.Reason = "回应期限已过。";
			q.Consequence = f?.PendingResponse?.Dissolve == true ? "服从将解散派系；抗命将起兵。" : "接受补偿将撤回诉求并解散派系。";
		}
		else if (IsGovernance(r.Action))
		{
			if (actor != k.RulingClan || actor.Kingdom != k) q.Reason = "只有原王国的统治家族能执行此操作。";
			else if (f == null) q.Reason = "请选择仍然存在的派系。";
			else if (CivilWarWorld.FindClan(f.LeaderClanId)?.Leader?.IsAlive != true) q.Reason = "派系领袖已变化，请等待继任处理。";
			else if (f.PendingResponse != null) q.Reason = "正在等待派系回应。";
			else if (f.Stage == KingdomCivilWarStage.OpenWar && (r.Action == CivilWarAction.Suppress || r.Action == CivilWarAction.ForceDissolve)) q.Reason = "建国请求开始后不能压制或行政解散。";
			else if (f.Stage == KingdomCivilWarStage.OpenWar && string.IsNullOrWhiteSpace(f.RebelKingdomId)) q.Reason = "叛军正在建国，请等待建国完成。";
			else if (r.Action != CivilWarAction.Concede && CivilWarWorld.CurrentDay() < f.GovernanceUntilDay) { q.CooldownUntilDay = f.GovernanceUntilDay; q.Reason = "政治行动冷却至第 " + f.GovernanceUntilDay + " 天。"; }
			if (r.Action == CivilWarAction.Suppress) { q.Influence = CivilWarPoliticalRules.SuppressCost; q.Consequence = "成员失去最多20影响力；领袖不满+7.5，其余+4；与国王关系−5。首次压制延后期限7天。"; }
			if (r.Action == CivilWarAction.ForceDissolve)
			{
				q.Influence = CivilWarPoliticalRules.DissolveCost; q.Consequence = "成员不满+7.5、与国王关系−10；服从则解散，抗命则起兵。";
				if (q.Reason.Length == 0 && !CanStartPoliticalWar(k, s, f, IsPlayerLed(f))) q.Reason = "国内/世界战争上限或玩家王国免疫阻止抗命起兵，不能下达解散令。";
			}
			if (r.Action == CivilWarAction.Negotiate)
			{
				if (r.OfferTier < 1 || r.OfferTier > 3) q.Reason = "补偿档位无效。";
				else if (r.OfferInfluence) q.Influence = CompensationOffer(k, s, f, r.OfferTier, true);
				else q.Gold = CompensationOffer(k, s, f, r.OfferTier, false);
				q.Consequence = "接受后转移补偿、撤回诉求并解散；拒绝不收费但消耗7天冷却。战中先议和归国。";
			}
			if (r.Action == CivilWarAction.Concede) q.Consequence = "完整兑现当前诉求；宣权将退位，独立战争将承认独立。";
			if (q.Reason.Length == 0 && (actor.Influence < q.Influence || actor.Leader.Gold < q.Gold)) q.Reason = "统治家族资源不足。";
		}
		else
		{
			KingdomCivilWarClanState member = null;
			s?.Clans.TryGetValue(actor?.StringId ?? "", out member);
			var own = FactionOfClan(s, actor);
			bool same = r.Action == CivilWarAction.JoinCrown && member?.Side == KingdomCivilWarSide.Crown
				|| r.Action == CivilWarAction.JoinOpposition && own != null && own == f;
			if (!PoliticalClan(actor, k)) q.Reason = "必须是本国正式封臣。";
			else if (actor == k.RulingClan) q.Reason = "国王不能自行加入、退出或建立反对派。";
			else if (own?.Stage == KingdomCivilWarStage.OpenWar || (r.Action != CivilWarAction.Detonate && s?.Factions.Any(x => x.Stage == KingdomCivilWarStage.OpenWar) == true)) q.Reason = "建国请求或内战期间，成员管理已锁定。";
			else if (own?.ResolutionNeedsReview == true) q.Reason = "已有派系操作结果待核查，暂不能改变成员或派系。";
			else if (r.Action == CivilWarAction.ChangeDemand)
			{
				if (actor != Clan.PlayerClan || f == null || f != own || f.LeaderClanId != actor.StringId || !IsPreWar(f)) q.Reason = "只能更改自己领导的战前派系诉求。";
				else if (f.PendingResponse != null || !string.IsNullOrEmpty(f.PendingConcessionOperationId)) q.Reason = "王室交涉或协议结算中，暂不能更改诉求。";
				else if (f.DemandId == r.DemandId && f.TargetId == r.TargetId) q.Reason = "诉求及目标未改变，无需重新评估成员。";
				else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = "新诉求或目标不合法，或缺少所需领地。";
				q.Consequence = "更改自己派系的诉求，并立即重评每个其他成员是否继续参与；退出者回到中立，不额外扣关系，不招募外部家族，不触发起兵。";
			}
			else if (r.Action == CivilWarAction.DissolveOwn)
			{
				if (actor != Clan.PlayerClan || f == null || f != own || f.LeaderClanId != actor.StringId || !IsPreWar(f)) q.Reason = "只能主动解散自己领导的战前派系。";
				else if (f.PendingResponse?.Accepted == true || !string.IsNullOrEmpty(f.PendingConcessionOperationId)) q.Reason = "已接受的协议仍在结算，不能解散。";
				q.Consequence = "解散自己领导的派系，与每个其他成员家族关系−10；保留不满，不延长已有王国冷却。";
			}
			else if (r.Action == CivilWarAction.Found && actor == Clan.PlayerClan)
			{
				int until = Math.Max(s?.CooldownUntilDay ?? 0, (s?.CooldownUntilWeek ?? 0) * 7);
				if (CivilWarWorld.CurrentDay() < until) { q.CooldownUntilDay = until; q.Reason = "王国战后冷却至第 " + until + " 天，玩家创建派系也须等待。"; }
				else if (own?.PendingResponse?.Accepted == true || !string.IsNullOrEmpty(own?.PendingConcessionOperationId)) q.Reason = "已有协议正在结算，不能改建派系。";
				else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = "诉求或目标不合法，或缺少所需领地。";
				q.Consequence = "创建派系，与统治者关系−10；若已有阵营，先退出并承担成员关系损失，自己的旧派系会解散。玩家领袖只会手动起兵。";
			}
			else if (r.Action == CivilWarAction.Leave)
			{
				if (member == null || member.Side == KingdomCivilWarSide.Middle) q.Reason = "当前已是中立。";
				q.Consequence = "与原成员领袖关系−10，与派系领袖−20（不叠加）；7天内不能加入或建立派系。";
			}
			else if (r.Action == CivilWarAction.Detonate)
			{
				if (f == null || own != f || !IsPreWar(f)) q.Reason = "只能要求自己的战前派系起兵。";
				else if (!CanStartPoliticalWar(k, s, f, actor == Clan.PlayerClan)) q.Reason = "国内/世界战争上限或叛乱免疫阻止起兵。";
				else if (actor == Clan.PlayerClan && !PlayerDetonationAllowed(k, actor, out string strengthReason)) q.Reason = strengthReason;
				else if (f.LeaderClanId != actor.StringId && CivilWarWorld.CurrentDay() < f.ProposalUntilDay) q.Reason = "起兵建议被拒后须等待7天。";
				q.Consequence = f?.LeaderClanId == actor.StringId ? "立即进入起兵建国流程。" : "向领袖提议；拒绝后7天内不能再次建议。";
			}
			else if (!same)
			{
				if (member != null && member.Side != KingdomCivilWarSide.Middle) q.Reason = "须先退出当前阵营，承担关系损失及7天冷却。";
				else if (_storage.ClanExitUntilDay.TryGetValue(actor.StringId, out int until) && CivilWarWorld.CurrentDay() < until) { q.CooldownUntilDay = until; q.Reason = "退出冷却至第 " + until + " 天，换国不能绕过。"; }
				else if (r.Action == CivilWarAction.JoinOpposition && !IsPreWar(f)) q.Reason = "请选择可加入的战前派系。";
				else if (r.Action == CivilWarAction.Found)
				{
					if (!CivilWarFactionRules.CanFormFaction(s?.Factions.Count ?? 0, CivilWarWorld.CurrentWeek(), s?.CooldownUntilWeek ?? 0, DuelSettings.BuildCivilWarTuning()) || (s?.CooldownUntilDay ?? 0) > CivilWarWorld.CurrentDay()) q.Reason = "派系名额已满或王国仍在冷却。";
					else if (!GetFoundingOptions(k.StringId, actor).Any(x => x.DemandId == r.DemandId && x.TargetId == r.TargetId)) q.Reason = "诉求或目标不合法，或缺少所需领地。";
				}
				else if (r.Action != CivilWarAction.JoinCrown && r.Action != CivilWarAction.JoinOpposition) q.Reason = "不支持此操作。";
			}
		}
		q.Allowed = q.Reason.Length == 0;
		return q;
	}

	private bool CanStartPoliticalWar(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, bool playerAuthorized = false)
	{
		CivilWarTuning tuning = DuelSettings.BuildCivilWarTuning();
		return !PlayerKingdomRebellionImmunity.ShouldProtectKingdom(k)
			&& CivilWarFactionRules.CanOpenWar(s != null && OtherFactionAtWar(s, f), tuning)
			&& CivilWarFactionRules.HasWorldWarSlot(WorldOpenWarCount, tuning, playerAuthorized);
	}
	// Quoted on demand (panel open / AI governance), never per tick.
	private static int CompensationOffer(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, int tier, bool influence)
	{
		int leaderTier = CivilWarWorld.FindClan(f.LeaderClanId)?.Tier ?? 0;
		float power = IsPreWar(f) ? FactionPower(k, s, f) : f.LastFactionPower;
		float grievance = CivilWarRules.Clamp(f.Grievance / 100f, 0f, 1f);
		string key = f.Id + ":" + CivilWarWorld.CurrentWeek() + ":" + (influence ? "i" : "g");
		return influence ? CivilWarPoliticalRules.InfluenceOffer(tier, leaderTier, power, grievance, key) : CivilWarPoliticalRules.GoldOffer(tier, leaderTier, power, grievance, key);
	}
	private static string Fingerprint(CivilWarActionRequest r, Clan actor) => string.Join("|", actor?.StringId, r.KingdomId, r.FactionId, r.Action, r.DemandId, r.TargetId, r.OfferTier, r.OfferInfluence, r.Accept);
	internal CivilWarActionResult Execute(CivilWarActionRequest r, Clan actor, bool leaderAgreed = false)
	{
		if (r == null || string.IsNullOrWhiteSpace(r.OperationId)) return new CivilWarActionResult { Status = CivilWarActionStatus.Rejected, Message = "操作缺少身份。" };
		string fingerprint = Fingerprint(r, actor);
		if (_storage.Operations.TryGetValue(r.OperationId, out var previous))
			return new CivilWarActionResult { FactionId = previous.FactionId, Status = previous.Fingerprint != fingerprint || previous.InProgress ? CivilWarActionStatus.PartialFailure : (CivilWarActionStatus)previous.Status,
				Message = previous.Fingerprint != fingerprint ? "操作身份冲突。" : previous.InProgress ? "先前操作中断，保留状态待核查，不能重复执行。" : previous.Message };
		var quote = Quote(r, actor);
		if (!quote.Allowed) return new CivilWarActionResult { Status = CivilWarActionStatus.Rejected, Message = quote.Reason };
		var receipt = new CivilWarOperation { Fingerprint = fingerprint, FactionId = r.FactionId, InProgress = true };
		_storage.Operations.Add(r.OperationId, receipt);
		Kingdom k = CivilWarWorld.FindKingdom(r.KingdomId);
		var s = GetOrCreate(k, CivilWarWorld.CurrentWeek());
		var f = ActionFaction(r, s, actor);
		// Only Found can leave/dissolve an old side before creating a different faction. This list is action-local,
		// filled by the existing relation loops; it never becomes persistent faction membership or tick work.
		List<Hero> affectedParticipants = r.Action == CivilWarAction.Found ? new List<Hero>() : null;
		CivilWarActionResult result;
		try
		{
			result = ExecutePoliticalAction(r, actor, k, s, f, quote, leaderAgreed, affectedParticipants);
			receipt.Status = (int)result.Status; receipt.Message = result.Message; receipt.FactionId = result.FactionId;
			receipt.InProgress = false;
			if (result.Status == CivilWarActionStatus.Applied)
			{
				PublishPoliticalResult(k, s, f, actor, r.OperationId, result.Message, affectedParticipants);
				receipt.FactsWritten = true;
			}
		}
		catch (CivilWarRetryableEffectException ex)
		{
			if (f != null) { f.ResolutionError = ex.Message; f.ResolutionNeedsReview = false; }
			receipt.InProgress = false; receipt.Status = (int)CivilWarActionStatus.PartialFailure;
			receipt.Message = "协议尚未完成，将重试已确认安全的步骤：" + ex.Message;
			result = new CivilWarActionResult { Status = CivilWarActionStatus.PartialFailure, Message = receipt.Message, FactionId = f?.Id ?? "" };
		}
		catch (Exception ex)
		{
			if (f != null) { f.ResolutionNeedsReview = true; f.ResolutionError = ex.Message; }
			receipt.Message = "操作未完整确认，保留派系等待核查：" + ex.Message;
			result = new CivilWarActionResult { Status = CivilWarActionStatus.PartialFailure, Message = receipt.Message, FactionId = f?.Id ?? "" };
			Logger.Log("KingdomCivilWar", receipt.Message);
		}
		if (f != null) f.Version++;
		SaveSummary(s);
		if (r.Action == CivilWarAction.Suppress) NotifyPoliticalChange(k, "royal_suppression");
		if (result.Status == CivilWarActionStatus.Applied && (r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.JoinOpposition || r.Action == CivilWarAction.Leave || r.Action == CivilWarAction.Found || r.Action == CivilWarAction.DissolveOwn || r.Action == CivilWarAction.ChangeDemand)) NotifyPoliticalChange(k, "membership");
		Revision++;
		return result;
	}

	private CivilWarActionResult ExecutePoliticalAction(CivilWarActionRequest r, Clan actor, Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, CivilWarActionQuote q, bool leaderAgreed, ICollection<Hero> affectedParticipants = null)
	{
		int day = CivilWarWorld.CurrentDay(), week = CivilWarWorld.CurrentWeek();
		var tuning = DuelSettings.BuildCivilWarTuning();
		var member = GetOrCreateClan(s, actor, week);
		var result = new CivilWarActionResult { Status = CivilWarActionStatus.Applied, FactionId = f?.Id ?? "" };
		if (r.Action == CivilWarAction.JoinCrown || r.Action == CivilWarAction.JoinOpposition)
		{
			var side = r.Action == CivilWarAction.JoinCrown ? KingdomCivilWarSide.Crown : KingdomCivilWarSide.Opposition;
			if (member.Side == side && (side == KingdomCivilWarSide.Crown || member.FactionId == f?.Id)) { result.Status = CivilWarActionStatus.Rejected; result.Message = "已在当前阵营，无需重复加入。"; return result; }
			member.Side = side; member.FactionId = side == KingdomCivilWarSide.Crown ? "" : f.Id; member.SideSinceWeek = week; member.SideSinceDay = CivilWarWorld.CurrentDay();
			if (actor == Clan.PlayerClan) s.PlayerSide = side == KingdomCivilWarSide.Crown ? "crown" : "opposition";
			result.Message = CivilWarWorld.ClanName(actor) + "加入" + (side == KingdomCivilWarSide.Crown ? "王室阵营" : FactionName(f, CivilWarCatalog.FindDemand(f.DemandId))) + "。";
		}
		else if (r.Action == CivilWarAction.Leave)
		{
			DetachPoliticalSide(k, s, actor, member, week, day);
			result.Message = CivilWarWorld.ClanName(actor) + "退出阵营，七天内保持中立。";
		}
		else if (r.Action == CivilWarAction.Found)
		{
			var previous = FactionOfClan(s, actor);
			string departure = "";
			if (previous?.LeaderClanId == actor.StringId && actor == Clan.PlayerClan)
			{
				departure = "解散" + FactionName(previous, CivilWarCatalog.FindDemand(previous.DemandId)) + "后，";
				DissolvePlayerFaction(k, s, previous, actor, week, tuning, affectedParticipants);
			}
			else if (member.Side != KingdomCivilWarSide.Middle)
			{
				departure = "退出" + (previous == null ? "王室阵营" : FactionName(previous, CivilWarCatalog.FindDemand(previous.DemandId))) + "后，";
				DetachPoliticalSide(k, s, actor, member, week, day, affectedParticipants);
			}
			var demand = CivilWarCatalog.FindDemand(r.DemandId);
			f = new KingdomCivilWarFactionState { Id = FactionPrefix + k.StringId + "-" + (++s.FactionSerial), DemandId = demand.Id, TargetId = r.TargetId,
				TargetName = demand.Target == CivilWarDemandTarget.EnemyKingdom ? CivilWarWorld.KingdomName(CivilWarWorld.FindKingdom(r.TargetId)) : k.ActivePolicies.FirstOrDefault(x => x.StringId == r.TargetId)?.Name?.ToString() ?? "",
				LeaderClanId = actor.StringId, PlayerFounded = actor == Clan.PlayerClan, CreatedWeek = week, StageWeek = week, UltimatumWeek = week + tuning.UltimatumDelayWeeks,
				UltimatumDay = day + tuning.UltimatumDelayWeeks * 7, LastDemandDay = day, WarGoal = (int)CivilWarCatalog.EffectiveWarGoal(demand), Grievance = TotalGrievance(member) };
			s.Factions.Add(f); member.Side = KingdomCivilWarSide.Opposition; member.FactionId = f.Id; member.SideSinceWeek = week; member.SideSinceDay = CivilWarWorld.CurrentDay();
			if (actor == Clan.PlayerClan) s.PlayerSide = "opposition";
			ApplyFoundingRelationLoss(k, actor);
			result.FactionId = f.Id; result.Message = CivilWarWorld.ClanName(actor) + departure + "建立派系，诉求：" + FormatDemand(demand, f.TargetName) + "。";
		}
		else if (r.Action == CivilWarAction.ChangeDemand)
		{
			result.Message = ChangePlayerFactionDemand(k, s, f, r, week, tuning);
		}
		else if (r.Action == CivilWarAction.DissolveOwn)
		{
			DissolvePlayerFaction(k, s, f, actor, week, tuning);
			result.Message = "你解散了自己的派系，与其他成员家族关系−10；不满与已有王国冷却保留。";
		}
		else if (r.Action == CivilWarAction.Detonate)
		{
			var leader = CivilWarWorld.FindClan(f.LeaderClanId);
			if (leader != actor && !leaderAgreed && RandomFloat() >= EscalationChance(k, s, f))
			{
				f.ProposalUntilDay = day + CivilWarPoliticalRules.ActionDays; result.Status = CivilWarActionStatus.Rejected; result.Message = "领袖拒绝起兵建议，七天后可再提议。"; return result;
			}
			f.RebellionOperationId = r.OperationId;
			OpenWar(k, s, f, leader, week, tuning, HostStability, playerAuthorized: actor == Clan.PlayerClan);
			result.Status = CivilWarActionStatus.AwaitingKingdom; result.Message = "起兵命令已受理，等待叛军建国。";
		}
		else if (r.Action == CivilWarAction.Suppress)
		{
			ChangeClanInfluenceAction.Apply(actor, -q.Influence); f.GovernanceUntilDay = day + CivilWarPoliticalRules.ActionDays;
			foreach (var record in Members(s, f).ToList())
			{
				var clan = CivilWarWorld.FindClan(record.ClanId); if (clan == null) continue;
				ChangeClanInfluenceAction.Apply(clan, -Math.Min(CivilWarPoliticalRules.SuppressInfluenceLoss, Math.Max(0, clan.Influence)));
				AddPoints(record, "royal_suppression", clan.StringId == f.LeaderClanId ? CivilWarPoliticalRules.SuppressLeaderGrievance : CivilWarPoliticalRules.SuppressMemberGrievance);
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, actor.Leader, -CivilWarPoliticalRules.SuppressRelationLoss, false);
			}
			var candidate = Members(s, f).Where(x => x.ClanId != f.LeaderClanId && x.ClanId != Clan.PlayerClan?.StringId)
				.OrderBy(TotalGrievance).ThenBy(x => x.ClanId, StringComparer.Ordinal).FirstOrDefault();
			if (candidate != null && CivilWarRules.Roll("suppression_leave", CivilWarCatalog.LeaveOpposition,
				BuildFeatures(k, s, f, CivilWarWorld.FindClan(candidate.ClanId), Host.GetStability(k)), CivilWarPoliticalRules.SuppressLeaveMultiplier, tuning, RandomFloat).Passed)
			{ candidate.Side = KingdomCivilWarSide.Middle; candidate.FactionId = ""; candidate.SideSinceWeek = week; candidate.SideSinceDay = CivilWarWorld.CurrentDay(); }
			if (!f.SuppressionExtended) { f.SuppressionExtended = true; f.UltimatumDay = Math.Max(day, f.UltimatumDay < 0 ? f.UltimatumWeek * 7 : f.UltimatumDay) + 7; if (f.PlayerAnswerPending) f.AnswerDeadlineDay = Math.Max(day, f.AnswerDeadlineDay < 0 ? f.PlayerAnswerDeadlineWeek * 7 : f.AnswerDeadlineDay) + 7; }
			result.Message = "王室对" + FactionName(f, CivilWarCatalog.FindDemand(f.DemandId)) + "实施政治压制。";
		}
		else if (r.Action == CivilWarAction.Respond)
		{
			var pending = f.PendingResponse;
			pending.ResponseOperationId = r.OperationId;
			if (k.RulingClan?.StringId != pending.RulerClanId) { f.PendingResponse = null; result.Status = CivilWarActionStatus.Rejected; result.Message = "统治家族已更换，原提议失效。"; return result; }
			return CompletePoliticalResponse(k, s, f, pending, r.Accept, playerAuthorized: actor == Clan.PlayerClan && r.OperationId != pending.OperationId + ":timeout");
		}
		else if (r.Action == CivilWarAction.Negotiate || r.Action == CivilWarAction.ForceDissolve)
		{
			bool dissolve = r.Action == CivilWarAction.ForceDissolve;
			f.GovernanceUntilDay = day + CivilWarPoliticalRules.ActionDays; f.DemandLocked = true;
			if (dissolve)
			{
				ChangeClanInfluenceAction.Apply(actor, -q.Influence);
				foreach (var record in Members(s, f)) { AddPoints(record, "royal_suppression", CivilWarPoliticalRules.DissolveGrievance); var clan = CivilWarWorld.FindClan(record.ClanId); if (clan?.Leader != null) ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, actor.Leader, -CivilWarPoliticalRules.DissolveRelationLoss, false); }
			}
			var pending = new CivilWarPendingResponse { OperationId = r.OperationId, RulerClanId = actor.StringId, LeaderClanId = f.LeaderClanId, Dissolve = dissolve, Gold = q.Gold, Influence = dissolve ? 0 : q.Influence, DeadlineDay = day + CivilWarPoliticalRules.ReplyDays };
			f.PendingResponse = pending;
			if (f.LeaderClanId == Clan.PlayerClan?.StringId)
			{ _hasPoliticalResponse = true; result.Status = CivilWarActionStatus.AwaitingPlayer; result.Message = dissolve ? "解散命令已送达，等待玩家三日内回应。" : "补偿提议已送达，等待玩家三日内回应。"; return result; }
			bool accept = dissolve ? RandomFloat() >= EscalationChance(k, s, f) : RandomFloat() < CivilWarPoliticalRules.NegotiationChance(BuildFeatures(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), Host.GetStability(k)), r.OfferTier, tuning);
			return CompletePoliticalResponse(k, s, f, pending, accept);
		}
		else if (r.Action == CivilWarAction.Concede)
		{
			if (string.IsNullOrEmpty(f.PendingConcessionOperationId)) { f.PendingConcessionRulerId = actor.StringId; f.PendingConcessionLeaderId = f.LeaderClanId; }
			if (f.PendingConcessionRulerId != actor.StringId || f.PendingConcessionLeaderId != f.LeaderClanId) throw new InvalidOperationException("妥协期间王室或派系领袖已变化，原协议需核查。");
			f.PendingConcessionOperationId = r.OperationId;
			var demand = CivilWarCatalog.FindDemand(f.DemandId);
			var ctx = BuildContext(k, s, f, demand, CivilWarWorld.FindClan(f.LeaderClanId), week);
			string effect = demand.AcceptEffectId;
			if (f.Stage == KingdomCivilWarStage.OpenWar)
			{
				if (CivilWarCatalog.EffectiveWarGoal(demand) == CivilWarWarGoal.Secede) effect = CivilWarEffectIds.Secede;
				else if (CivilWarCatalog.EffectiveWarGoal(demand) == CivilWarWarGoal.Usurp) effect = CivilWarEffectIds.UsurpThrone;
				else CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
			}
			if (!CivilWarEffects.TryApply(effect, ctx, out string reason)) { f.ResolutionError = reason; f.ResolutionNeedsReview = !ctx.RetryableFailure; result.Status = CivilWarActionStatus.PartialFailure; result.Message = "妥协尚未兑现，派系保留：" + reason; return result; }
			result.Message = "国王完整兑现" + FactionName(f, demand) + "的诉求：" + string.Join("；", ctx.Notes);
			f.PendingConcessionOperationId = "";
			FinishFaction(k, s, f, week, tuning, HostStability, 4);
		}
		return result;
	}

	private float EscalationChance(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f)
	{
		var demand = CivilWarCatalog.FindDemand(f.DemandId);
		return CivilWarRules.Shape((demand?.Escalate?.Raw(BuildFeatures(k, s, f, CivilWarWorld.FindClan(f.LeaderClanId), Host.GetStability(k))) ?? 0)
			* (demand?.EscalationScale ?? 1) * CivilWarAftermathRules.EscalationFactor(CurrentAftermath(s, CivilWarWorld.CurrentWeek())), DuelSettings.BuildCivilWarTuning());
	}

	private CivilWarActionResult CompletePoliticalResponse(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, CivilWarPendingResponse p, bool accept, bool playerAuthorized = false)
	{
		var result = new CivilWarActionResult { FactionId = f.Id, Status = CivilWarActionStatus.Applied };
		var crown = CivilWarWorld.FindClan(p.RulerClanId); var leader = CivilWarWorld.FindClan(p.LeaderClanId);
		if (crown != k.RulingClan || p.LeaderClanId != f.LeaderClanId) throw new InvalidOperationException("协议双方身份已变化，原协议需核查。");
		if (!accept)
		{
			f.PendingResponse = null;
			if (p.Dissolve && !CanStartPoliticalWar(k, s, f, playerAuthorized)) { result.Status = CivilWarActionStatus.Rejected; result.Message = "战争资格已变化，原解散命令失效，派系保留。"; }
			else if (p.Dissolve && IsPlayerLed(f) && !playerAuthorized) { RefuseAndReschedule(f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning()); result.Status = CivilWarActionStatus.Rejected; result.Message = "解散令超时，派系保留；玩家领袖须手动决定起兵。"; }
			else if (p.Dissolve) { OpenWar(k, s, f, leader, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), HostStability, playerAuthorized); result.Status = CivilWarActionStatus.AwaitingKingdom; result.Message = "派系抗命，进入起兵流程。"; }
			else { result.Status = CivilWarActionStatus.Rejected; result.Message = "派系拒绝补偿，未扣除资源。"; }
			return result;
		}
		if (!p.Dissolve)
		{
			if (crown?.Leader == null || leader?.Leader == null || crown.Leader.Gold < p.Gold || crown.Influence < p.Influence)
			{ f.PendingResponse = null; result.Status = CivilWarActionStatus.Rejected; result.Message = "补偿方已无法支付，协议失效。"; return result; }
			p.Accepted = true;
			if (f.Stage == KingdomCivilWarStage.OpenWar) CivilWarEffects.ReturnRebels(BuildContext(k, s, f, CivilWarCatalog.FindDemand(f.DemandId), leader, CivilWarWorld.CurrentWeek()), f.WarClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null), 0, true);
			if (p.Gold > 0) GiveGoldAction.ApplyBetweenCharacters(crown.Leader, leader.Leader, p.Gold, disableNotification: true);
			if (p.Influence > 0) { ChangeClanInfluenceAction.Apply(crown, -p.Influence); ChangeClanInfluenceAction.Apply(leader, p.Influence); }
		}
		f.PendingResponse = null;
		result.Message = p.Dissolve ? "派系服从解散命令，不满仍然保留。" : "派系接受补偿并撤回诉求，组织解散。";
		FinishFaction(k, s, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), HostStability, 0);
		foreach (string id in new[] { p.OperationId, p.ResponseOperationId })
			if (_storage.Operations.TryGetValue(id ?? "", out var receipt)) { receipt.Status = (int)result.Status; receipt.Message = result.Message; receipt.InProgress = false; }
		return result;
	}

	private void EnsurePoliticalLeader(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, bool startCooldown = true)
	{
		if (!IsPreWar(f)) return;
		var leader = CivilWarWorld.FindClan(f.LeaderClanId);
		if (PoliticalClan(leader, k) && leader != k.RulingClan && FactionOfClan(s, leader) == f) return;
		var next = Members(s, f).Select(x => CivilWarWorld.FindClan(x.ClanId)).Where(x => PoliticalClan(x, k) && x != k.RulingClan)
			.OrderByDescending(CivilWarWorld.Strength).ThenBy(x => x.StringId, StringComparer.Ordinal).FirstOrDefault();
		if (next == null) Dissolve(k, s, f, CivilWarWorld.CurrentWeek(), DuelSettings.BuildCivilWarTuning(), "派系无合格继任者，解散。", startCooldown);
		else { f.LeaderClanId = next.StringId; f.Version++; AddHistory(s, CivilWarWorld.CurrentWeek(), CivilWarWorld.ClanName(next) + "继任派系领袖。"); }
	}

	private void PublishPoliticalResult(Kingdom k, KingdomCivilWarKingdomState s, KingdomCivilWarFactionState f, Clan actor, string operationId, string text, IEnumerable<Hero> affectedParticipants = null)
	{
		AddHistory(s, CivilWarWorld.CurrentWeek(), text);
		MyBehavior.RecordCivilWarPoliticalResult(k, "civil_war:result:" + operationId, text, f != null || operationId.Contains(":war:"),
			actor?.Leader, CivilWarWorld.FindClan(f?.LeaderClanId)?.Leader);
		var heroes = new HashSet<Hero>();
		if (affectedParticipants != null)
			foreach (Hero hero in affectedParticipants) if (hero != null) heroes.Add(hero);
		if (actor?.Leader != null) heroes.Add(actor.Leader);
		if (k.Leader != null) heroes.Add(k.Leader);
		var leader = CivilWarWorld.FindClan(f?.LeaderClanId); if (leader?.Leader != null) heroes.Add(leader.Leader);
		if (f != null) foreach (var id in f.WarClanIds.Concat(f.LastParticipantIds).Concat(Members(s, f).Select(x => x.ClanId))) { var clan = CivilWarWorld.FindClan(id); if (clan?.Leader != null) heroes.Add(clan.Leader); }
		foreach (var hero in heroes) WriteFact(k, hero, "civil_war:action:" + operationId + ":" + hero.StringId, text);
	}
}
