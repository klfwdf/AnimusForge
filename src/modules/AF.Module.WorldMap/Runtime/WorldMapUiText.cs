using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing WorldMap command text. Runtime protocol, AFEF facts and logs
/// remain owned by their callers; this helper only formats visible notices.
/// </summary>
internal static class WorldMapUiText
{
	private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				text.SetTextVariable(name, value?.ToString() ?? "");
			}
		}
		return text.ToString();
	}

	public static string GoToSettlement(string actor, string settlement, int days, bool holdUntilSpecified)
		=> holdUntilSpecified
			? Text("AF_WM_GoToSettlementUntil", "{ACTOR} is heading to {SETTLEMENT}; it will stay there until the specified deadline.", ("ACTOR", actor), ("SETTLEMENT", settlement))
			: Text("AF_WM_GoToSettlement", "{ACTOR} is heading to {SETTLEMENT} and will stay there for {DAYS} days.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("DAYS", days));

	public static string Patrol(string actor, string settlement, int days)
		=> Text("AF_WM_Patrol", "{ACTOR} is heading near {SETTLEMENT} and will patrol for {DAYS} days.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("DAYS", days));

	public static string SettlementAttack(string actor, string settlement, string action, int days, string mode)
	{
		string localizedAction = string.Equals(action, "raid", StringComparison.OrdinalIgnoreCase)
			? Text("AF_WM_ActionRaid", "raid")
			: string.Equals(action, "besiege", StringComparison.OrdinalIgnoreCase)
				? Text("AF_WM_ActionBesiege", "besiege")
				: action;
		return Text("AF_WM_SettlementAttack", "{ACTOR} is moving toward {SETTLEMENT} to {ACTION}; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("SETTLEMENT", settlement), ("ACTION", localizedAction), ("DAYS", days), ("MODE", mode));
	}

	public static string WaitForHero(string actor, string settlement, string hero)
		=> Text("AF_WM_WaitForHero", "{ACTOR} is heading outside {SETTLEMENT} and waiting for {HERO} to leave before attacking.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("HERO", hero));

	public static string TrackHero(string actor, string hero, int days, string mode)
		=> Text("AF_WM_TrackHero", "{ACTOR} is tracking {HERO}'s party to attack; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("HERO", hero), ("DAYS", days), ("MODE", mode));

	public static string TrackParty(string actor, string party, int days, string mode)
		=> Text("AF_WM_TrackParty", "{ACTOR} is tracking {PARTY} to attack; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("PARTY", party), ("DAYS", days), ("MODE", mode));

	public static string Merge(string hero, bool foreignGuest)
		=> foreignGuest
			? Text("AF_WM_MergeForeign", "{HERO} is returning to the player's party and will merge as a guest force while keeping the original clan identity.", ("HERO", hero))
			: Text("AF_WM_Merge", "{HERO} is returning to the player's party to regroup and transfer troops.", ("HERO", hero));

	public static string Result(string id, string fallback, params (string Name, object Value)[] variables)
		=> Text(id, fallback, variables);
}
