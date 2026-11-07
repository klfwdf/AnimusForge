using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Camp issues (field-camp-v1 step 4): judged once when the camp is pitched, at most
/// three, worst first. Each issue is stored as "kind|subject|subject2|state".
/// Only fixed issue kinds exist; the LLM voices them and picks the resolution tag,
/// C# validates and applies the bounded effect.
/// </summary>
internal static class FieldCampIssues
{
	internal const int MaxIssues = 3;

	internal enum Kind
	{
		UnpaidWages,
		Hunger,
		ManyWounded,
		RecentDefeat,
		NoRest,
		LowMorale,
		CultureFriction,
		CompanionFeud,
		Petition
	}

	internal enum State
	{
		Open,
		Encouraged,
		Granted,
		Refused,
		Disciplined,
		Ignored,
		Withdrawn
	}

	internal enum Resolution
	{
		Encourage,
		Grant,
		Refuse,
		Discipline
	}

	internal sealed class Issue
	{
		internal Kind Kind;
		internal string Subject = "";
		internal string Subject2 = "";
		internal State State;

		internal bool IsOpen => State == State.Open;

		internal string Encode() => Kind + "|" + Subject + "|" + Subject2 + "|" + State;

		internal static Issue Decode(string text)
		{
			string[] parts = (text ?? "").Split('|');
			if (parts.Length != 4 || !Enum.TryParse(parts[0], out Kind kind) || !Enum.TryParse(parts[3], out State state))
			{
				return null;
			}
			return new Issue { Kind = kind, Subject = parts[1], Subject2 = parts[2], State = state };
		}

		/// <summary>Companion issues target heroes; troop issues target a troop type.</summary>
		internal bool IsCompanionIssue => Kind == Kind.CompanionFeud;
	}

	private const int NoRestDays = 7;
	private const int DefeatMemoryDays = 7;

	/// <summary>Judges the camp's issues once; suppressed kinds are skipped.</summary>
	internal static List<Issue> Judge(MobileParty main, double nowHours, double lastRestHours, double lastDefeatHours, ICollection<string> suppressed, Func<Hero, Hero, int, bool> canRaiseCompanionFeud = null)
	{
		var found = new List<(int Severity, Issue Issue)>();
		TroopRoster roster = main.MemberRoster;
		int men = roster.TotalManCount;
		CharacterObject largest = LargestRegularTroop(roster);
		string largestId = largest?.StringId ?? "";

		void Add(int severity, Kind kind, string subject = "", string subject2 = "")
		{
			if (suppressed != null && suppressed.Contains(kind.ToString()))
			{
				return;
			}
			found.Add((severity, new Issue { Kind = kind, Subject = subject ?? "", Subject2 = subject2 ?? "" }));
		}

		if (main.HasUnpaidWages > 0f || (Hero.MainHero?.Gold ?? 0) < main.TotalWage * 7)
		{
			Add(90, Kind.UnpaidWages, largestId);
		}
		float foodDays = 99f;
		try
		{
			foodDays = main.GetNumDaysForFoodToLast();
		}
		catch
		{
		}
		if (foodDays < 3f)
		{
			Add(85, Kind.Hunger, largestId);
		}
		if (men > 0 && roster.TotalWounded > men / 4)
		{
			Add(70, Kind.ManyWounded, MostWoundedTroop(roster)?.StringId ?? largestId);
		}
		if (lastDefeatHours >= 0 && nowHours - lastDefeatHours < DefeatMemoryDays * 24.0)
		{
			Add(65, Kind.RecentDefeat, largestId);
		}
		if (lastRestHours >= 0 && nowHours - lastRestHours > NoRestDays * 24.0)
		{
			Add(55, Kind.NoRest, largestId);
		}
		if (main.Morale < 30f)
		{
			Add(60, Kind.LowMorale, largestId);
		}
		if (TryFindCultureFriction(roster, out CharacterObject a, out CharacterObject b))
		{
			Add(40, Kind.CultureFriction, a.StringId, b.StringId);
		}
		if (TryFindCompanionFeud(roster, canRaiseCompanionFeud, out Hero first, out Hero second))
		{
			Add(45, Kind.CompanionFeud, first.StringId, second.StringId);
		}
		found.Sort((x, y) => y.Severity.CompareTo(x.Severity));
		var result = new List<Issue>();
		for (int i = 0; i < found.Count && result.Count < MaxIssues; i++)
		{
			result.Add(found[i].Issue);
		}
		// A healthy camp still has something to say: a petition for battle or reward.
		if (result.Count == 0 && largest != null && (suppressed == null || !suppressed.Contains(Kind.Petition.ToString())))
		{
			result.Add(new Issue { Kind = Kind.Petition, Subject = largestId });
		}
		return result;
	}

	private static CharacterObject LargestRegularTroop(TroopRoster roster)
	{
		CharacterObject best = null;
		int bestCount = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement e = roster.GetElementCopyAtIndex(i);
			if (e.Character != null && !e.Character.IsHero && e.Number > bestCount)
			{
				best = e.Character;
				bestCount = e.Number;
			}
		}
		return best;
	}

	private static CharacterObject MostWoundedTroop(TroopRoster roster)
	{
		CharacterObject best = null;
		int bestCount = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement e = roster.GetElementCopyAtIndex(i);
			if (e.Character != null && !e.Character.IsHero && e.WoundedNumber > bestCount)
			{
				best = e.Character;
				bestCount = e.WoundedNumber;
			}
		}
		return best;
	}

	internal const int FeudRelation = -10;
	internal const int FeudRenewalRelationLoss = 5;

	internal static string CompanionPairKey(string first, string second)
		=> string.CompareOrdinal(first, second) <= 0 ? first + "|" + second : second + "|" + first;

	internal static bool CanRaiseCompanionFeud(int relation, int? handledRelation)
		=> relation <= FeudRelation && (!handledRelation.HasValue || relation <= handledRelation.Value - FeudRenewalRelationLoss);

	/// <summary>Worst eligible pair by actual relation; traits alone are not evidence of a feud. Once per camp.</summary>
	private static bool TryFindCompanionFeud(TroopRoster roster, Func<Hero, Hero, int, bool> canRaise, out Hero first, out Hero second)
	{
		first = null;
		second = null;
		var heroes = new List<Hero>();
		for (int i = 0; i < roster.Count; i++)
		{
			Hero hero = roster.GetElementCopyAtIndex(i).Character?.HeroObject;
			if (hero != null && hero != Hero.MainHero && hero.IsAlive && !hero.IsPrisoner)
			{
				heroes.Add(hero);
			}
		}
		int worst = 0;
		for (int a = 0; a < heroes.Count; a++)
		{
			for (int b = a + 1; b < heroes.Count; b++)
			{
				int score;
				try
				{
					int relation = heroes[a].GetRelation(heroes[b]);
					if (!CanRaiseCompanionFeud(relation, null) || canRaise?.Invoke(heroes[a], heroes[b], relation) == false) continue;
					score = relation;
				}
				catch
				{
					continue;
				}
				if (score < worst)
				{
					worst = score;
					first = heroes[a];
					second = heroes[b];
				}
			}
		}
		return first != null;
	}

	/// <summary>Two cultures each holding at least 30% of the regulars (threshold tuned in play).</summary>
	private static bool TryFindCultureFriction(TroopRoster roster, out CharacterObject first, out CharacterObject second)
	{
		first = null;
		second = null;
		var byCulture = new Dictionary<string, (int Count, CharacterObject Top, int TopCount)>(StringComparer.Ordinal);
		int total = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement e = roster.GetElementCopyAtIndex(i);
			string culture = e.Character?.Culture?.StringId;
			if (e.Character == null || e.Character.IsHero || string.IsNullOrEmpty(culture) || e.Number <= 0)
			{
				continue;
			}
			total += e.Number;
			byCulture.TryGetValue(culture, out var entry);
			entry.Count += e.Number;
			if (e.Number > entry.TopCount)
			{
				entry.Top = e.Character;
				entry.TopCount = e.Number;
			}
			byCulture[culture] = entry;
		}
		if (total < 20)
		{
			return false;
		}
		foreach (var entry in byCulture.Values)
		{
			if (entry.Count * 10 < total * 3)
			{
				continue;
			}
			if (first == null)
			{
				first = entry.Top;
			}
			else
			{
				second = entry.Top;
				return true;
			}
		}
		return false;
	}
}
