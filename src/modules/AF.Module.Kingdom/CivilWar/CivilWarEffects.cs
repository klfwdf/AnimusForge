using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

// Host services owned by MyBehavior (stability, rebel kingdom lifecycle, memory). Implemented by the adapter.
internal interface ICivilWarHost
{
	void AdjustStability(Kingdom kingdom, int delta, string reason);
	// Player clan defects from its kingdom to the civil-war rebel kingdom (follow prompt). True when it moved.
	bool MovePlayerToRebels(Clan player, Kingdom home, Kingdom rebel);
	int GetStability(Kingdom kingdom);
	bool DiscontinueLandlessKingdom(Kingdom kingdom, string reason);
	// Independent clans left over from a settled war make peace with everyone they are not permanently at war with.
	void MakeClansPeaceful(IEnumerable<Clan> clans, string reason);
	void QueueRebellion(Kingdom kingdom, Clan leader, List<Clan> followers, string factionId, bool startNow);
	void ApplyPrestige(Kingdom kingdom, int delta, string reason);
	void RecordMaterial(Kingdom kingdom, int week, string text);
	void RecordFact(Kingdom kingdom, Hero hero, string key, string text);
}

// One applied-effect note: the original text stays verbatim in Notes for history, save receipts and
// material, while the stable code plus values let the notification render the same fact in the active
// language. Notes must never be dropped, only described.
internal struct CivilWarNoteProjection
{
	internal string Code;
	internal string Text;
	internal string Arg;
	internal string Arg2;
}

internal sealed class CivilWarEffectContext
{
	internal Kingdom Kingdom;
	internal KingdomCivilWarKingdomState State;
	internal KingdomCivilWarFactionState Faction;
	internal CivilWarDemandDef Demand;
	internal Clan LeaderClan;
	internal Kingdom RebelKingdom;
	internal int Week;
	internal ICivilWarHost Host;
	internal bool RetryableFailure;
	internal readonly List<string> Notes = new List<string>();

	// Display identity of a refused or failed effect: a stable code plus at most two dynamic values.
	// The raw reason text stays the stored fact; these fields only let the notice render the same fact
	// in the active language. An empty code means this build cannot describe the outcome, and the
	// notice then keeps the original reason text instead of a guessed summary.
	internal string FailureCode = "";
	internal string FailureArg = "";
	internal string FailureArg2 = "";

	// Display identity of each note, index-aligned with Notes. A note added straight to Notes by an
	// effect outside this file has no projection and keeps its original text.
	internal readonly List<CivilWarNoteProjection> NoteProjections = new List<CivilWarNoteProjection>();

	// Records one applied-effect note: the original text stays verbatim in Notes, and the code plus
	// values let the notification render the same fact in the active language.
	internal void Note(string code, string text, string arg = "", string arg2 = "")
	{
		Notes.Add(text);
		NoteProjections.Add(new CivilWarNoteProjection { Code = code ?? "", Text = text ?? "", Arg = arg ?? "", Arg2 = arg2 ?? "" });
	}

	internal IEnumerable<Clan> FactionClans()
	{
		if (State?.Clans == null) yield break;
		foreach (KingdomCivilWarClanState clan in State.Clans.Values)
		{
			if (clan?.Side != KingdomCivilWarSide.Opposition || Faction == null || !string.Equals(clan.FactionId, Faction.Id, StringComparison.OrdinalIgnoreCase)) continue;
			Clan found = CivilWarWorld.FindClan(clan.ClanId);
			if (found != null && !found.IsEliminated) yield return found;
		}
	}

	internal IEnumerable<Clan> WarClans()
	{
		foreach (string id in Faction?.WarClanIds ?? new List<string>())
		{
			Clan found = CivilWarWorld.FindClan(id);
			if (found != null && !found.IsEliminated) yield return found;
		}
	}
}

// One class per effect id. Add an effect: new class + constant in CivilWarEffectIds + one Register line.
internal interface ICivilWarEffect
{
	string Id { get; }
	bool CanApply(CivilWarEffectContext ctx, out string reason);
	void Apply(CivilWarEffectContext ctx);
}

// Only reconciliation operations with an observable postcondition may request an automatic retry.
internal sealed class CivilWarRetryableEffectException : Exception
{
	// Stable identity of the failure for display. Empty when the throw site has no description yet:
	// the notice then keeps the original message rather than guessing a translation.
	internal readonly string Code;
	internal readonly string Arg;
	internal readonly string Arg2;

	internal CivilWarRetryableEffectException(string message) : base(message)
	{
		Code = "";
		Arg = "";
		Arg2 = "";
	}

	internal CivilWarRetryableEffectException(string message, string code, string arg = "", string arg2 = "") : base(message)
	{
		Code = code ?? "";
		Arg = arg ?? "";
		Arg2 = arg2 ?? "";
	}
}

// A non-retryable effect failure: the operation must not be replayed and needs human review. It carries
// the same display identity as the retryable case, so the notice can describe the cause in the active
// language while the raw message stays the stored fact.
internal sealed class CivilWarReviewRequiredException : Exception
{
	internal readonly string Code;
	internal readonly string Arg;
	internal readonly string Arg2;

	internal CivilWarReviewRequiredException(string code, string message, string arg = "", string arg2 = "") : base(message)
	{
		Code = code ?? "";
		Arg = arg ?? "";
		Arg2 = arg2 ?? "";
	}
}

internal static class CivilWarEffects
{
	private static readonly Dictionary<string, ICivilWarEffect> Registry = new Dictionary<string, ICivilWarEffect>(StringComparer.Ordinal);

	static CivilWarEffects()
	{
		Register(new MakePeaceWithTargetEffect());
		Register(new PledgeWarOnTargetEffect());
		Register(new RevokeTargetPolicyEffect());
		Register(new PayRedressEffect());
		Register(new GrantPrivilegesEffect());
		Register(new AbdicateEffect());
		Register(new UsurpThroneEffect());
		Register(new RestoreCoupDynastyEffect());
		Register(new SecedeEffect());
		Register(new ConcessionEffect());
		Register(new CrownVictoryEffect());
	}

	internal static ICollection<string> Ids => Registry.Keys;

	internal static ICivilWarEffect Find(string id) => id != null && Registry.TryGetValue(id, out ICivilWarEffect effect) ? effect : null;

	// Records the display identity of a refusal and returns false, so a CanApply body stays a one-liner.
	internal static bool Refuse(CivilWarEffectContext ctx, string code, string text, out string reason, string arg = "", string arg2 = "")
	{
		reason = text;
		ctx.FailureCode = code;
		ctx.FailureArg = arg;
		ctx.FailureArg2 = arg2;
		return false;
	}

	// Applies an effect with a guard; returns false (and a reason) instead of throwing into the weekly tick.
	// The reason text is the stored fact. FailureCode/Arg/Arg2 only describe that fact for the localized
	// notice and stay empty when this build cannot describe it, so the notice then keeps the raw reason.
	internal static bool TryApply(string id, CivilWarEffectContext ctx, out string reason)
	{
		reason = "";
		ctx.RetryableFailure = false;
		ctx.FailureCode = "";
		ctx.FailureArg = "";
		ctx.FailureArg2 = "";
		ICivilWarEffect effect = Find(id);
		if (effect == null)
		{
			ctx.FailureCode = "UnknownEffect";
			ctx.FailureArg = id ?? "";
			reason = "unknown effect " + id;
			return false;
		}
		try
		{
			if (!effect.CanApply(ctx, out reason)) { ctx.RetryableFailure = true; return false; }
			effect.Apply(ctx);
			return true;
		}
		catch (CivilWarRetryableEffectException ex)
		{
			reason = ex.Message;
			ctx.RetryableFailure = true;
			ctx.FailureCode = ex.Code;
			ctx.FailureArg = ex.Arg;
			ctx.FailureArg2 = ex.Arg2;
			return false;
		}
		catch (Exception ex)
		{
			reason = "effect " + id + " failed: " + ex.Message;
			Logger.Log("KingdomCivilWar", "[ERROR] " + reason);
			// Only an exception that describes itself gets a projection. An unexpected error keeps its
			// original message so the notice never hides the detail behind a guessed translation.
			CivilWarReviewRequiredException described = ex as CivilWarReviewRequiredException;
			if (described != null)
			{
				ctx.FailureCode = described.Code;
				ctx.FailureArg = described.Arg;
				ctx.FailureArg2 = described.Arg2;
			}
			return false;
		}
	}

	private static void Register(ICivilWarEffect effect) => Registry[effect.Id] = effect;

	// ------------------------------------------------------------ shared war helpers
	// Peace first, then bring clans home (followers before leader), then retire the empty rebel kingdom.
	internal static void ReturnRebels(CivilWarEffectContext ctx, IEnumerable<Clan> clans, int relationToKing, bool includePlayer)
	{
		if (ctx.Faction.ResolutionReturnCompleted) return;
		Kingdom home = ctx.Kingdom;
		Kingdom rebel = ctx.RebelKingdom;
		if (!CivilWarWorld.IsAlive(home)) throw new CivilWarRetryableEffectException("原王国已不存在，无法归国", "HomeKingdomGone");
		if (CivilWarWorld.IsAlive(rebel)) RequirePeace(home, rebel, "civil_war_return");
		Clan leader = ctx.LeaderClan;
		List<Clan> ordered = clans.Where(x => x != null && !x.IsEliminated).Distinct().OrderBy(x => x == leader ? 1 : 0).ToList();
		bool returnFailed = false;
		foreach (Clan clan in ordered)
		{
			try
			{
				if (clan.Kingdom == home || clan == Clan.PlayerClan && !includePlayer) continue;
				if (CivilWarWorld.IsAlive(rebel) && clan.Kingdom == rebel)
					ChangeKingdomAction.ApplyByJoinToKingdomByDefection(clan, rebel, home, default(CampaignTime), showNotification: true);
				else if (clan.Kingdom == null)
					ChangeKingdomAction.ApplyByJoinToKingdom(clan, home, default(CampaignTime), showNotification: true);
				else continue;
				if (clan.Kingdom != home) { returnFailed = true; continue; }
				CivilWarWorld.ChangeRelation(clan.Leader, home.Leader, relationToKing);
			}
			catch (Exception ex)
			{
				// Defection can throw from an event after changing the clan's kingdom. Re-read the
				// postcondition next week; clans already home are skipped, including their relation delta.
				returnFailed = true;
				Logger.Log("KingdomCivilWar", "[WARN] return rebel clan failed clan=" + clan.StringId + " error=" + ex.Message);
			}
		}
		if (returnFailed) throw new CivilWarRetryableEffectException("部分叛军家族尚未确认归国", "RebelClansNotHome");
		if (CivilWarWorld.IsAlive(rebel))
		{
			if (ctx.Host == null || !ctx.Host.DiscontinueLandlessKingdom(rebel, "civil_war_return:" + ctx.Faction.Id))
				throw new CivilWarRetryableEffectException("叛军王国尚未解散", "RebelKingdomNotDissolved");
			if (CivilWarWorld.IsAlive(rebel)) throw new CivilWarRetryableEffectException("叛军王国解散尚未确认", "RebelKingdomDissolveUnconfirmed");
			ctx.Note("RebelKingdomDissolved", CivilWarWorld.KingdomName(rebel) + "随叛军归国而解散", CivilWarWorld.KingdomName(rebel));
		}
		ctx.Faction.ResolutionReturnCompleted = true;
	}

	internal static void RequirePeace(Kingdom first, Kingdom second, string reason)
	{
		CivilWarWorld.MakePeaceSafe(first, second, reason);
		if (CivilWarWorld.IsAlive(first) && CivilWarWorld.IsAlive(second) && first.IsAtWarWith(second))
			throw new CivilWarRetryableEffectException("议和尚未生效：" + reason, "PeaceNotInEffect", reason);
	}
}

internal sealed class MakePeaceWithTargetEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.MakePeaceWithTarget;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction?.TargetId);
		if (CivilWarWorld.IsAlive(target) && ctx.Kingdom.IsAtWarWith(target)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "PeaceTargetNotAtWar", "议和对象已不在交战中", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction.TargetId);
		CivilWarEffects.RequirePeace(ctx.Kingdom, target, "civil_war_make_peace");
		ctx.Note("PeaceMade", CivilWarWorld.KingdomName(ctx.Kingdom) + "与" + CivilWarWorld.KingdomName(target) + "议和", CivilWarWorld.KingdomName(ctx.Kingdom), CivilWarWorld.KingdomName(target));
	}
}

internal sealed class PledgeWarOnTargetEffect : ICivilWarEffect
{
	internal const int PledgeWeeks = 8;

	public string Id => CivilWarEffectIds.PledgeWarOnTarget;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction?.TargetId);
		if (CivilWarWorld.IsAlive(target)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "WarTargetGone", "作战对象已不存在", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		ctx.State.NoPeaceTargetId = ctx.Faction.TargetId;
		ctx.State.NoPeaceUntilWeek = ctx.Week + PledgeWeeks;
		ctx.State.NoPeaceUntilDay = CivilWarWorld.CurrentDay() + PledgeWeeks * 7;
		ctx.State.NoPeaceClanIds = ctx.FactionClans().Select(x => x.StringId).ToList();
		ctx.Note("PledgeNoPeace", "国王承诺 " + PledgeWeeks + " 周内不与" + ctx.Faction.TargetName + "议和", PledgeWeeks.ToString(), ctx.Faction.TargetName);
	}
}

internal sealed class RevokeTargetPolicyEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.RevokeTargetPolicy;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		PolicyObject policy = Find(ctx.Faction?.TargetId);
		if (policy != null && ctx.Kingdom.ActivePolicies.Contains(policy)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "PolicyNotInEffect", "该政策已不再生效", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		PolicyObject policy = Find(ctx.Faction.TargetId);
		ctx.Kingdom.RemovePolicy(policy);
		ctx.Note("PolicyRevoked", "废除政策" + (policy.Name?.ToString() ?? ctx.Faction.TargetName), policy.Name?.ToString() ?? ctx.Faction.TargetName);
	}

	private static PolicyObject Find(string id)
	{
		if (string.IsNullOrWhiteSpace(id)) return null;
		try { return MBObjectManager.Instance?.GetObject<PolicyObject>(id); }
		catch { return null; }
	}
}

internal sealed class PayRedressEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.PayRedress;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (ctx.Kingdom?.Leader != null && ctx.LeaderClan?.Leader != null) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "RedressPartiesGone", "赔偿双方不存在", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		Hero king = ctx.Kingdom.Leader;
		Hero leader = ctx.LeaderClan.Leader;
		int members = ctx.FactionClans().Count();
		int amount = CivilWarPoliticalRules.RedressGold(ctx.LeaderClan.Tier, CivilWarRules.Clamp((ctx.Faction?.Grievance ?? 0f) / 100f, 0f, 1f), members, king.Gold, (ctx.Faction?.Id ?? "") + ":" + ctx.Week + ":redress");
		if (amount > 0) GiveGoldAction.ApplyBetweenCharacters(king, leader, amount, disableNotification: true);
		foreach (Clan clan in ctx.FactionClans()) CivilWarWorld.ChangeRelation(clan.Leader, king, clan == ctx.LeaderClan ? 10 : 5);
		ctx.Note("RedressPaid", "国王向" + CivilWarWorld.ClanName(ctx.LeaderClan) + "赔偿 " + amount + " 第纳尔", CivilWarWorld.ClanName(ctx.LeaderClan), amount.ToString());
	}
}

internal sealed class GrantPrivilegesEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.GrantPrivileges;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (ctx.Kingdom?.Leader != null) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "KingGone", "国王不存在", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		foreach (Clan clan in ctx.FactionClans())
		{
			CivilWarWorld.ChangeRelation(clan.Leader, ctx.Kingdom.Leader, 8);
			// Privileges float with the clan's standing (tier) and a stable per-faction jitter instead of a flat grant.
			float grant = (clan == ctx.LeaderClan ? 40f + 12f * Math.Max(0, clan.Tier) : 15f + 5f * Math.Max(0, clan.Tier))
				* CivilWarPoliticalRules.Jitter((ctx.Faction?.Id ?? "") + ":" + ctx.Week + ":" + clan.StringId);
			try { ChangeClanInfluenceAction.Apply(clan, (float)Math.Round(grant)); }
			catch (Exception ex) { Logger.Log("KingdomCivilWar", "[WARN] influence grant failed: " + ex.Message); throw; }
		}
		ctx.Note("PrivilegesGranted", "国王承认" + CivilWarWorld.ClanName(ctx.LeaderClan) + "等家族的封地特权", CivilWarWorld.ClanName(ctx.LeaderClan));
	}
}

internal sealed class AbdicateEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Abdicate;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (ctx.LeaderClan != null && ctx.LeaderClan.Kingdom == ctx.Kingdom && ctx.LeaderClan.Leader?.IsAlive == true) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "HeirNotInKingdom", "继位者不在本国", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		ChangeRulingClanAction.Apply(ctx.Kingdom, ctx.LeaderClan);
		ctx.Note("Abdicated", "国王退位，" + CivilWarWorld.ClanName(ctx.LeaderClan) + "继位", CivilWarWorld.ClanName(ctx.LeaderClan));
	}
}

internal sealed class UsurpThroneEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.UsurpThrone;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (ctx.LeaderClan != null && !ctx.LeaderClan.IsEliminated && ctx.LeaderClan.Leader?.IsAlive == true) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "RebelLeaderGone", "叛军领袖已不在", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
		if (ctx.LeaderClan.Kingdom == ctx.Kingdom)
		{
			Clan old = ctx.Kingdom.RulingClan;
			ChangeRulingClanAction.Apply(ctx.Kingdom, ctx.LeaderClan);
			if (ctx.Kingdom.RulingClan != ctx.LeaderClan) throw new CivilWarRetryableEffectException("王位转移尚未生效", "ThroneTransferNotInEffect");
			CivilWarWorld.ChangeRelation(old?.Leader, ctx.LeaderClan.Leader, -30);
			ctx.Note("ThroneSeized", CivilWarWorld.ClanName(ctx.LeaderClan) + "夺取王位", CivilWarWorld.ClanName(ctx.LeaderClan));
		}
		else throw new CivilWarRetryableEffectException("叛军领袖未能回到本国，夺位尚未完成", "RebelLeaderNotHome");
	}
}

internal sealed class SecedeEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Secede;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (CivilWarWorld.IsAlive(ctx.RebelKingdom)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "RebelKingdomGone", "叛军王国已不存在", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		CivilWarEffects.RequirePeace(ctx.Kingdom, ctx.RebelKingdom, "civil_war_secede");
		ctx.Note("Seceded", CivilWarWorld.KingdomName(ctx.RebelKingdom) + "获得承认，脱离" + CivilWarWorld.KingdomName(ctx.Kingdom), CivilWarWorld.KingdomName(ctx.RebelKingdom), CivilWarWorld.KingdomName(ctx.Kingdom));
	}
}

internal sealed class ConcessionEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Concession;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (CivilWarWorld.IsAlive(ctx.Kingdom)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "KingdomGone", "王国已灭亡", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 5, true);
		// Coerce wars were fought for the original demand; usurp/secede compromises fall back to privileges.
		string demandEffect = ctx.Demand?.AcceptEffectId ?? "";
		bool throneOrLand = demandEffect == CivilWarEffectIds.Abdicate || demandEffect == CivilWarEffectIds.GrantPrivileges || demandEffect.Length == 0;
		string effect = throneOrLand ? CivilWarEffectIds.GrantPrivileges : demandEffect;
		// The original demand may already have been fulfilled during the war.
		if (effect == CivilWarEffectIds.MakePeaceWithTarget)
		{
			Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction.TargetId);
			if (!CivilWarWorld.IsAlive(target) || !ctx.Kingdom.IsAtWarWith(target)) { ctx.Note("WarAlreadyEnded", "原诉求的战争已经结束"); return; }
		}
		if (effect == CivilWarEffectIds.RevokeTargetPolicy && !ctx.Kingdom.ActivePolicies.Any(x => x.StringId == ctx.Faction.TargetId))
		{ ctx.Note("PolicyAlreadyRevoked", "原诉求的政策已经废除"); return; }
		if (!CivilWarEffects.TryApply(effect, ctx, out string reason))
		{
			// The inner failure's display identity is forwarded unchanged so the notice describes the concrete
			// cause; when the inner effect could not describe it, the raw text is kept instead of a guess.
			if (ctx.RetryableFailure) throw new CivilWarRetryableEffectException("妥协条款未能兑现：" + reason, ctx.FailureCode, ctx.FailureArg, ctx.FailureArg2);
			throw new CivilWarReviewRequiredException(ctx.FailureCode, "妥协条款执行异常：" + reason, ctx.FailureArg, ctx.FailureArg2);
		}
	}
}

internal sealed class CrownVictoryEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.CrownVictory;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		if (CivilWarWorld.IsAlive(ctx.Kingdom)) { reason = ""; return true; }
		return CivilWarEffects.Refuse(ctx, "KingdomGone", "王国已灭亡", out reason);
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		Clan leader = ctx.LeaderClan;
		// Followers are pardoned with a penalty; the leader clan comes home only to be expelled with its fiefs confiscated.
		// The player (if they followed the rebels) is pardoned with the same penalty rather than left in an empty rebel kingdom.
		CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), -15, true);
		if (leader != null && !leader.IsEliminated && leader.Kingdom == ctx.Kingdom && leader != Clan.PlayerClan)
		{
			try
			{
				ChangeKingdomAction.ApplyByLeaveKingdom(leader, showNotification: true);
				ctx.Note("LeaderExiled", CivilWarWorld.ClanName(leader) + "被逐出王国，封地收归王室", CivilWarWorld.ClanName(leader));
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomCivilWar", "[WARN] exile leader failed: " + ex.Message);
				throw;
			}
			if (leader.Kingdom == ctx.Kingdom) throw new CivilWarRetryableEffectException("叛军领袖尚未被逐出王国", "RebelLeaderNotExiled");
		}
		CivilWarWorld.ChangeRelation(leader?.Leader, ctx.Kingdom.Leader, -25);
		ctx.Note("CrownSuppressed", "王室平定叛乱");
	}
}
