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
	internal CivilWarRetryableEffectException(string message) : base(message) { }
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

	// Applies an effect with a guard; returns false (and a reason) instead of throwing into the weekly tick.
	internal static bool TryApply(string id, CivilWarEffectContext ctx, out string reason)
	{
		reason = "";
		ctx.RetryableFailure = false;
		ICivilWarEffect effect = Find(id);
		if (effect == null) { reason = "unknown effect " + id; return false; }
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
			return false;
		}
		catch (Exception ex)
		{
			reason = "effect " + id + " failed: " + ex.Message;
			Logger.Log("KingdomCivilWar", "[ERROR] " + reason);
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
		if (!CivilWarWorld.IsAlive(home)) throw new CivilWarRetryableEffectException("原王国已不存在，无法归国");
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
		if (returnFailed) throw new CivilWarRetryableEffectException("部分叛军家族尚未确认归国");
		if (CivilWarWorld.IsAlive(rebel))
		{
			if (ctx.Host == null || !ctx.Host.DiscontinueLandlessKingdom(rebel, "civil_war_return:" + ctx.Faction.Id))
				throw new CivilWarRetryableEffectException("叛军王国尚未解散");
			if (CivilWarWorld.IsAlive(rebel)) throw new CivilWarRetryableEffectException("叛军王国解散尚未确认");
			ctx.Notes.Add(CivilWarWorld.KingdomName(rebel) + "随叛军归国而解散");
		}
		ctx.Faction.ResolutionReturnCompleted = true;
	}

	internal static void RequirePeace(Kingdom first, Kingdom second, string reason)
	{
		CivilWarWorld.MakePeaceSafe(first, second, reason);
		if (CivilWarWorld.IsAlive(first) && CivilWarWorld.IsAlive(second) && first.IsAtWarWith(second))
			throw new CivilWarRetryableEffectException("议和尚未生效：" + reason);
	}
}

internal sealed class MakePeaceWithTargetEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.MakePeaceWithTarget;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction?.TargetId);
		reason = CivilWarWorld.IsAlive(target) && ctx.Kingdom.IsAtWarWith(target) ? "" : "议和对象已不在交战中";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction.TargetId);
		CivilWarEffects.RequirePeace(ctx.Kingdom, target, "civil_war_make_peace");
		ctx.Notes.Add(CivilWarWorld.KingdomName(ctx.Kingdom) + "与" + CivilWarWorld.KingdomName(target) + "议和");
	}
}

internal sealed class PledgeWarOnTargetEffect : ICivilWarEffect
{
	internal const int PledgeWeeks = 8;

	public string Id => CivilWarEffectIds.PledgeWarOnTarget;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		Kingdom target = CivilWarWorld.FindKingdom(ctx.Faction?.TargetId);
		reason = CivilWarWorld.IsAlive(target) ? "" : "作战对象已不存在";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		ctx.State.NoPeaceTargetId = ctx.Faction.TargetId;
		ctx.State.NoPeaceUntilWeek = ctx.Week + PledgeWeeks;
		ctx.State.NoPeaceUntilDay = CivilWarWorld.CurrentDay() + PledgeWeeks * 7;
		ctx.State.NoPeaceClanIds = ctx.FactionClans().Select(x => x.StringId).ToList();
		ctx.Notes.Add("国王承诺 " + PledgeWeeks + " 周内不与" + ctx.Faction.TargetName + "议和");
	}
}

internal sealed class RevokeTargetPolicyEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.RevokeTargetPolicy;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		PolicyObject policy = Find(ctx.Faction?.TargetId);
		reason = policy != null && ctx.Kingdom.ActivePolicies.Contains(policy) ? "" : "该政策已不再生效";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		PolicyObject policy = Find(ctx.Faction.TargetId);
		ctx.Kingdom.RemovePolicy(policy);
		ctx.Notes.Add("废除政策" + (policy.Name?.ToString() ?? ctx.Faction.TargetName));
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
		reason = ctx.Kingdom?.Leader != null && ctx.LeaderClan?.Leader != null ? "" : "赔偿双方不存在";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		Hero king = ctx.Kingdom.Leader;
		Hero leader = ctx.LeaderClan.Leader;
		int amount = Math.Min(Math.Max(0, king.Gold / 5), 3000 + 1500 * Math.Max(0, ctx.LeaderClan.Tier));
		if (amount > 0) GiveGoldAction.ApplyBetweenCharacters(king, leader, amount, disableNotification: true);
		foreach (Clan clan in ctx.FactionClans()) CivilWarWorld.ChangeRelation(clan.Leader, king, clan == ctx.LeaderClan ? 10 : 5);
		ctx.Notes.Add("国王向" + CivilWarWorld.ClanName(ctx.LeaderClan) + "赔偿 " + amount + " 第纳尔");
	}
}

internal sealed class GrantPrivilegesEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.GrantPrivileges;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = ctx.Kingdom?.Leader != null ? "" : "国王不存在";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		foreach (Clan clan in ctx.FactionClans())
		{
			CivilWarWorld.ChangeRelation(clan.Leader, ctx.Kingdom.Leader, 8);
			try { ChangeClanInfluenceAction.Apply(clan, clan == ctx.LeaderClan ? 80f : 30f); }
			catch (Exception ex) { Logger.Log("KingdomCivilWar", "[WARN] influence grant failed: " + ex.Message); throw; }
		}
		ctx.Notes.Add("国王承认" + CivilWarWorld.ClanName(ctx.LeaderClan) + "等家族的封地特权");
	}
}

internal sealed class AbdicateEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Abdicate;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = ctx.LeaderClan != null && ctx.LeaderClan.Kingdom == ctx.Kingdom && ctx.LeaderClan.Leader?.IsAlive == true ? "" : "继位者不在本国";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		ChangeRulingClanAction.Apply(ctx.Kingdom, ctx.LeaderClan);
		ctx.Notes.Add("国王退位，" + CivilWarWorld.ClanName(ctx.LeaderClan) + "继位");
	}
}

internal sealed class UsurpThroneEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.UsurpThrone;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = ctx.LeaderClan != null && !ctx.LeaderClan.IsEliminated && ctx.LeaderClan.Leader?.IsAlive == true ? "" : "叛军领袖已不在";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		CivilWarEffects.ReturnRebels(ctx, ctx.WarClans(), 0, true);
		if (ctx.LeaderClan.Kingdom == ctx.Kingdom)
		{
			Clan old = ctx.Kingdom.RulingClan;
			ChangeRulingClanAction.Apply(ctx.Kingdom, ctx.LeaderClan);
			if (ctx.Kingdom.RulingClan != ctx.LeaderClan) throw new CivilWarRetryableEffectException("王位转移尚未生效");
			CivilWarWorld.ChangeRelation(old?.Leader, ctx.LeaderClan.Leader, -30);
			ctx.Notes.Add(CivilWarWorld.ClanName(ctx.LeaderClan) + "夺取王位");
		}
		else throw new CivilWarRetryableEffectException("叛军领袖未能回到本国，夺位尚未完成");
	}
}

internal sealed class SecedeEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Secede;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = CivilWarWorld.IsAlive(ctx.RebelKingdom) ? "" : "叛军王国已不存在";
		return reason.Length == 0;
	}

	public void Apply(CivilWarEffectContext ctx)
	{
		CivilWarEffects.RequirePeace(ctx.Kingdom, ctx.RebelKingdom, "civil_war_secede");
		ctx.Notes.Add(CivilWarWorld.KingdomName(ctx.RebelKingdom) + "获得承认，脱离" + CivilWarWorld.KingdomName(ctx.Kingdom));
	}
}

internal sealed class ConcessionEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.Concession;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = CivilWarWorld.IsAlive(ctx.Kingdom) ? "" : "王国已灭亡";
		return reason.Length == 0;
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
			if (!CivilWarWorld.IsAlive(target) || !ctx.Kingdom.IsAtWarWith(target)) { ctx.Notes.Add("原诉求的战争已经结束"); return; }
		}
		if (effect == CivilWarEffectIds.RevokeTargetPolicy && !ctx.Kingdom.ActivePolicies.Any(x => x.StringId == ctx.Faction.TargetId))
		{ ctx.Notes.Add("原诉求的政策已经废除"); return; }
		if (!CivilWarEffects.TryApply(effect, ctx, out string reason))
		{
			if (ctx.RetryableFailure) throw new CivilWarRetryableEffectException("妥协条款未能兑现：" + reason);
			throw new InvalidOperationException("妥协条款执行异常：" + reason);
		}
	}
}

internal sealed class CrownVictoryEffect : ICivilWarEffect
{
	public string Id => CivilWarEffectIds.CrownVictory;

	public bool CanApply(CivilWarEffectContext ctx, out string reason)
	{
		reason = CivilWarWorld.IsAlive(ctx.Kingdom) ? "" : "王国已灭亡";
		return reason.Length == 0;
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
				ctx.Notes.Add(CivilWarWorld.ClanName(leader) + "被逐出王国，封地收归王室");
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomCivilWar", "[WARN] exile leader failed: " + ex.Message);
				throw;
			}
			if (leader.Kingdom == ctx.Kingdom) throw new CivilWarRetryableEffectException("叛军领袖尚未被逐出王国");
		}
		CivilWarWorld.ChangeRelation(leader?.Leader, ctx.Kingdom.Leader, -25);
		ctx.Notes.Add("王室平定叛乱");
	}
}
