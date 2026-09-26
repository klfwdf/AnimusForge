using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace AnimusForge;

// Single love table and ephemeral marriage-topic context. Live eligibility/mutation stays in host.
internal sealed class RomanceRelationshipOwner
{
    internal Dictionary<string, int> PrivateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _context = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    internal void SetContext(string speaker, bool enabled)
    {
        string key = (speaker ?? "").Trim();
        if (string.IsNullOrWhiteSpace(key)) return;
        if (enabled) _context[key] = true;
        else _context.TryRemove(key, out _);
    }

    internal bool ConsumeContext(string speaker)
    {
        string key = (speaker ?? "").Trim();
        return !string.IsNullOrWhiteSpace(key) && _context.TryRemove(key, out bool enabled) && enabled;
    }

    internal void ClearContext() => _context.Clear();

    internal int GetLove(string key)
    {
        PrivateLove ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return PrivateLove.TryGetValue(key, out int value) ? ClampLove(value) : 0;
    }

    internal int SetLove(string key, int value)
    {
        PrivateLove ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int next = ClampLove(value);
        if (next == 0) PrivateLove.Remove(key);
        else PrivateLove[key] = next;
        return next;
    }

    internal void NormalizeLove()
    {
		List<string> list = PrivateLove.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string key = list[i];
			if (string.IsNullOrWhiteSpace(key))
			{
				PrivateLove.Remove(key);
				continue;
			}
			int value = ClampLove(PrivateLove[key]);
			if (value == 0)
			{
				PrivateLove.Remove(key);
			}
			else
			{
				PrivateLove[key] = value;
			}
		}
    }

    internal static bool IsCandidateAge(float age, int maximum, bool authority)
        => !(age < 18f || (age > (float)maximum && !authority));

    internal static bool IsAgeGapAllowed(float left, float right, int maximum)
        => !(Math.Abs(left - right) > (float)maximum);

	internal static int ClampLove(int value)
	{
		if (value < -100)
		{
			value = -100;
		}
		if (value > 100)
		{
			value = 100;
		}
		return value;
	}

	internal static int ToLoveLevelIndex(int value)
	{
		double num = ((double)ClampLove(value) + 100.0) / 200.0;
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

	internal static string GetMarriageRuntimeInstructionState(bool hasPlayer, bool hasTarget, bool pairAvailable, bool hasClan, bool isLeader, bool canElope, int tierDiff, int clanRelation, int effectiveTrust, int formalThreshold)
	{
		if (!hasPlayer)
		{
			return "no_player_hero";
		}
		if (!hasTarget)
		{
			return "no_target";
		}
		if (!pairAvailable)
		{
			return "unavailable";
		}
		if (!hasClan)
		{
			return "clanless_path";
		}
		if (isLeader)
		{
			return "leader_path";
		}
		return "member_path";
	}

	internal static string GetMarriageRuntimeConstraintState(bool hasPlayer, bool hasTarget, bool pairAvailable, bool hasClan, bool isLeader, bool canElope, int tierDiff, int clanRelation, int effectiveTrust, int formalThreshold)
	{
		if (!hasPlayer)
		{
			return "no_player_hero";
		}
		if (!hasTarget)
		{
			return "no_target";
		}
		if (!pairAvailable)
		{
			return "unavailable";
		}
		if (!hasClan)
		{
			return (canElope ? "clanless_elope_ready" : "clanless_elope_blocked");
		}
		if (isLeader)
		{
			if (tierDiff <= -3)
			{
				return "leader_blocked_tier_gap";
			}
			if (tierDiff == -2)
			{
				return "leader_need_heavy_brideprice";
			}
			if (tierDiff == -1)
			{
				return ((clanRelation >= 20 && effectiveTrust >= 20) ? "leader_need_brideprice_ready" : "leader_need_brideprice_blocked");
			}
			if (tierDiff >= 2)
			{
				return ((clanRelation >= formalThreshold && effectiveTrust >= formalThreshold) ? "leader_offer_brideprice_major" : "leader_standard_blocked");
			}
			if (tierDiff == 1)
			{
				return ((clanRelation >= formalThreshold && effectiveTrust >= formalThreshold) ? "leader_offer_brideprice_minor" : "leader_standard_blocked");
			}
			return ((clanRelation >= formalThreshold && effectiveTrust >= formalThreshold) ? "leader_standard_ready" : "leader_standard_blocked");
		}
		return (canElope ? "member_redirect_elope_ready" : "member_redirect_elope_blocked");
	}
}
