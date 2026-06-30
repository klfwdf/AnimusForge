using System;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace AnimusForge;

[HarmonyPatch(typeof(Kingdom), nameof(Kingdom.ChangeKingdomName))]
internal static class Patch_PlayerKingdomNameChange_RecordMaterials
{
	private const string ActionKind = "player_kingdom_rename";

	private static void Prefix(Kingdom __instance, out RenameState __state)
	{
		__state = RenameState.Capture(__instance);
	}

	private static void Postfix(Kingdom __instance, TextObject name, TextObject informalName, RenameState __state)
	{
		try
		{
			if (!__state.ShouldRecord || __instance == null || !IsPlayerRuledKingdom(__instance))
			{
				return;
			}
			string newName = CleanDisplayText(name?.ToString() ?? __instance.Name?.ToString());
			string newInformalName = CleanDisplayText(informalName?.ToString() ?? __instance.InformalName?.ToString());
			if (string.IsNullOrWhiteSpace(newName) || string.Equals(__state.OldName, newName, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			int day = GetCurrentGameDayIndexSafe();
			string gameDate = GetCurrentGameDateTextSafe();
			string stableKey = "player_kingdom_rename:" + (__state.KingdomId ?? "").Trim() + ":" + day + ":" + NormalizeKeyPart(__state.OldName) + ":" + NormalizeKeyPart(newName);
			string locationText = string.IsNullOrWhiteSpace(newInformalName) ? newName : newInformalName;
			string recentText = LimitText("你将王国由「" + __state.OldName + "」更名为「" + newName + "」，以新国号重塑王权名义。", 160);
			string majorText = LimitText("以国王身份将旧国号「" + __state.OldName + "」改为「" + newName + "」，公开重塑玩家王国的政治名号。", 180);
			PlayerNotorietyBehavior.RecordPlayerActionForExternal(
				recentText,
				stableKey + ":recent",
				ActionKind,
				isMajor: false,
				day,
				gameDate,
				0,
				"",
				"",
				locationText,
				Hero.MainHero?.Culture?.StringId ?? "",
				__instance.Culture?.StringId ?? "",
				"",
				won: null);
			PlayerNotorietyBehavior.RecordPlayerHistoryMaterialForExternal(
				majorText,
				stableKey + ":history",
				ActionKind,
				day,
				gameDate,
				Hero.MainHero?.Culture?.StringId ?? "",
				__instance.Culture?.StringId ?? "",
				"");
			MyBehavior.RecordPlayerKingdomRenameWeeklyMaterialForExternal(
				__state.OldName,
				newName,
				__state.OldInformalName,
				newInformalName,
				__state.KingdomId,
				day,
				gameDate,
				stableKey);
			Logger.Log("PlayerKingdomRename", "recorded kingdom rename old=" + __state.OldName + " new=" + newName + " kingdom=" + (__state.KingdomId ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerKingdomRename", "record failed: " + ex.Message);
		}
	}

	private static bool IsPlayerRuledKingdom(Kingdom kingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (kingdom == null || playerClan == null)
			{
				return false;
			}
			return kingdom == playerClan.Kingdom && (kingdom.RulingClan == playerClan || kingdom.Leader == Hero.MainHero);
		}
		catch
		{
			return false;
		}
	}

	private static int GetCurrentGameDayIndexSafe()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static string GetCurrentGameDateTextSafe()
	{
		try
		{
			string text = CampaignTime.Now.ToString();
			return string.IsNullOrWhiteSpace(text) ? ("第 " + GetCurrentGameDayIndexSafe() + " 日") : text.Trim();
		}
		catch
		{
			return "第 " + GetCurrentGameDayIndexSafe() + " 日";
		}
	}

	private static string CleanDisplayText(string text)
	{
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		return text;
	}

	private static string LimitText(string text, int maxChars)
	{
		text = CleanDisplayText(text);
		if (string.IsNullOrWhiteSpace(text) || maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
	}

	private static string NormalizeKeyPart(string text)
	{
		string clean = CleanDisplayText(text).ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(clean))
		{
			return "empty";
		}
		StringBuilder sb = new StringBuilder(clean.Length);
		foreach (char c in clean)
		{
			if (char.IsLetterOrDigit(c))
			{
				sb.Append(c);
			}
			else if (sb.Length == 0 || sb[sb.Length - 1] != '_')
			{
				sb.Append('_');
			}
		}
		string result = sb.ToString().Trim('_');
		if (string.IsNullOrWhiteSpace(result))
		{
			result = "name";
		}
		return result.Length <= 48 ? result : result.Substring(0, 48);
	}

	private sealed class RenameState
	{
		public bool ShouldRecord;
		public string KingdomId = "";
		public string OldName = "";
		public string OldInformalName = "";

		public static RenameState Capture(Kingdom kingdom)
		{
			RenameState state = new RenameState();
			try
			{
				if (!IsPlayerRuledKingdom(kingdom))
				{
					return state;
				}
				state.KingdomId = (kingdom.StringId ?? "").Trim();
				state.OldName = CleanDisplayText(kingdom.Name?.ToString());
				state.OldInformalName = CleanDisplayText(kingdom.InformalName?.ToString());
				state.ShouldRecord = !string.IsNullOrWhiteSpace(state.OldName);
				return state;
			}
			catch
			{
				return state;
			}
		}
	}
}
