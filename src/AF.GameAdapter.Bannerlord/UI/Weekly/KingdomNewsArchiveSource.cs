using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Projects recent diplomatic declarations and published policies into the terminal "王国近况" country lists,
// next to the existing bulletin / recent / weekly archive entries. It runs only when the page opens or reloads;
// nothing is scanned per frame and no persisted record is modified.
internal static class KingdomNewsArchiveSource
{
	internal const string DiplomacyKind = "diplomacy";
	internal const string PolicyKind = "policy";
	private const string AllCountriesName = "全部王国";
	private const int MaxDiplomacyEntries = 240;
	private const int MaxPolicyEntries = 240;
	private const int BodyCharacterLimit = 8000;

	internal static List<MyBehavior.WeeklyReportBrowserCountryData> Merge(List<MyBehavior.WeeklyReportBrowserCountryData> archive)
	{
		List<MyBehavior.WeeklyReportBrowserCountryData> countries = archive ?? new List<MyBehavior.WeeklyReportBrowserCountryData>();
		countries.RemoveAll(c => c == null);
		MyBehavior.WeeklyReportBrowserCountryData world = countries.FirstOrDefault(c => c.IsWorld);
		if (world == null)
		{
			world = new MyBehavior.WeeklyReportBrowserCountryData { CountryId = "world", DisplayName = AllCountriesName, IsWorld = true };
			countries.Insert(0, world);
		}
		else
		{
			world.DisplayName = AllCountriesName;
		}
		Dictionary<string, MyBehavior.WeeklyReportBrowserCountryData> byId = new Dictionary<string, MyBehavior.WeeklyReportBrowserCountryData>(StringComparer.OrdinalIgnoreCase);
		foreach (MyBehavior.WeeklyReportBrowserCountryData country in countries)
		{
			string id = (country.CountryId ?? "").Trim();
			if (id.Length > 0 && !byId.ContainsKey(id)) byId[id] = country;
		}

		void Add(MyBehavior.WeeklyReportBrowserEntryData entry, IEnumerable<KeyValuePair<string, string>> related)
		{
			world.Reports.Add(entry);
			foreach (KeyValuePair<string, string> pair in related)
			{
				string id = (pair.Key ?? "").Trim();
				if (id.Length == 0) continue;
				if (!byId.TryGetValue(id, out MyBehavior.WeeklyReportBrowserCountryData country))
				{
					country = new MyBehavior.WeeklyReportBrowserCountryData { CountryId = id, DisplayName = FirstNonEmpty(pair.Value, id) };
					countries.Add(country);
					byId[id] = country;
				}
				if (!ReferenceEquals(country, world) && !country.Reports.Contains(entry)) country.Reports.Add(entry);
			}
		}

		try
		{
			foreach (WorldDiplomacyTimelineDocument document in DiplomacyPresentationBridge.GetRecentDocumentsOrEmpty(MaxDiplomacyEntries))
			{
				if (document == null) continue;
				Add(BuildDiplomacyEntry(document), DiplomacyCountries(document));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TerminalKingdomNews", "[WARN] diplomacy source failed: " + ex.Message);
		}

		try
		{
			foreach (PolicyRecordPresentationData policy in CustomPolicyBehavior.GetPolicyRecordPresentationSnapshot()
				.Where(x => x != null).OrderByDescending(x => x.Day).Take(MaxPolicyEntries))
			{
				Add(BuildPolicyEntry(policy), new[] { new KeyValuePair<string, string>(policy.KingdomId, policy.KingdomName) });
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TerminalKingdomNews", "[WARN] policy source failed: " + ex.Message);
		}
		return countries;
	}

	private static MyBehavior.WeeklyReportBrowserEntryData BuildDiplomacyEntry(WorldDiplomacyTimelineDocument document)
	{
		string label = document.IsResponse ? "外交回应" : document.RequiresResponse ? "外交照会" : document.ChangedDiplomaticState ? "外交结果" : "外交宣言";
		string author = FirstNonEmpty(document.AuthorKingdomName, document.AuthorKingdomId, "未知国家");
		string target = string.Join("、", DiplomacyCountries(document)
			.Where(p => !string.Equals(p.Key, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
			.Select(p => FirstNonEmpty(p.Value, p.Key)).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(4));
		string body = Limit(document.Body);
		if (!string.IsNullOrWhiteSpace(document.ImpactText)) body += "\n\n外交影响：" + document.ImpactText.Trim();
		string id = "diplomacy:" + FirstNonEmpty(document.DocumentId, document.CreatedUtcTicks.ToString(CultureInfo.InvariantCulture));
		int day = Math.Max(0, document.Day);
		return new MyBehavior.WeeklyReportBrowserEntryData
		{
			EventId = id,
			ArchiveKind = DiplomacyKind,
			OpenTargetId = id,
			KindLabelText = label + " · " + author + (target.Length == 0 ? "" : " → " + target),
			WeekIndex = day / 7,
			Title = FirstNonEmpty(document.Title, label),
			BodyText = body.Length == 0 ? "（该外交消息暂无正文。）" : body,
			CreatedDate = FirstNonEmpty(document.GameDate, FormatDay(day)),
			CreatedDay = day,
			TagText = "",
			HasFullReport = true
		};
	}

	private static MyBehavior.WeeklyReportBrowserEntryData BuildPolicyEntry(PolicyRecordPresentationData policy)
	{
		string scope = PolicyScopeLabel(policy.ScopeKind);
		string id = "policy:" + FirstNonEmpty(policy.HistoryKey, policy.RecordId, policy.TitleText);
		string body = Limit(policy.BodyText);
		if (!string.IsNullOrWhiteSpace(policy.ImpactText)) body += "\n\n政策影响：" + policy.ImpactText.Trim();
		int day = Math.Max(0, policy.Day);
		return new MyBehavior.WeeklyReportBrowserEntryData
		{
			EventId = id,
			ArchiveKind = PolicyKind,
			OpenTargetId = id,
			KindLabelText = scope + (string.IsNullOrWhiteSpace(policy.StatusText) ? "" : " · " + policy.StatusText.Trim())
				+ (string.IsNullOrWhiteSpace(policy.KingdomName) ? "" : " · " + policy.KingdomName.Trim()),
			WeekIndex = day / 7,
			Title = FirstNonEmpty(policy.TitleText, scope),
			BodyText = body.Length == 0 ? "（该政策暂无正文。）" : body,
			CreatedDate = FirstNonEmpty(policy.DateText, FormatDay(day)),
			CreatedDay = day,
			TagText = "",
			HasFullReport = true
		};
	}

	private static IEnumerable<KeyValuePair<string, string>> DiplomacyCountries(WorldDiplomacyTimelineDocument document)
	{
		yield return new KeyValuePair<string, string>(document.AuthorKingdomId, document.AuthorKingdomName);
		yield return new KeyValuePair<string, string>(document.TargetKingdomId, document.TargetKingdomName);
		foreach (WorldDiplomacyTimelineCountryReference target in document.ActionTargets)
			yield return new KeyValuePair<string, string>(target.CountryId, target.CountryName);
	}

	private static string PolicyScopeLabel(string scopeKind)
	{
		if (string.Equals(scopeKind, "kingdom", StringComparison.OrdinalIgnoreCase)) return "王国政策";
		if (string.Equals(scopeKind, "local", StringComparison.OrdinalIgnoreCase)) return "地方政策";
		if (string.Equals(scopeKind, "vassal", StringComparison.OrdinalIgnoreCase)) return "附庸政策";
		return "政策";
	}

	private static string Limit(string text)
	{
		string normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return normalized.Length <= BodyCharacterLimit ? normalized : normalized.Substring(0, BodyCharacterLimit) + "…";
	}

	private static string FormatDay(int day) => day > 0 ? "第" + day.ToString(CultureInfo.InvariantCulture) + "天" : "未知日期";

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
			if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
		return "";
	}
}
