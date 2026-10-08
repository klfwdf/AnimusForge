using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;

namespace AnimusForge;

internal sealed class WeeklyHeroFact
{
	internal string Id = "";
	internal string ClanId = "";
	internal string KingdomId = "";
	internal string SpouseId = "";
	internal string FatherId = "";
	internal string MotherId = "";
	internal bool IsFemale;
}

internal sealed class WeeklyAggregateGameFacts
{
	internal readonly Func<string, string> HeroDisplay;
	internal readonly Func<string, string> ClanDisplay;
	internal readonly Func<string, string> KingdomDisplay;
	internal readonly Func<string, string> SettlementDisplay;
	internal readonly Func<string, string> SettlementName;
	internal readonly Func<string, string> SettlementNameWithType;
	internal readonly Func<string, WeeklyHeroFact> Hero;

	internal WeeklyAggregateGameFacts(Func<string, string> heroDisplay, Func<string, string> clanDisplay,
		Func<string, string> kingdomDisplay, Func<string, string> settlementDisplay,
		Func<string, string> settlementName, Func<string, string> settlementNameWithType,
		Func<string, WeeklyHeroFact> hero)
	{
		HeroDisplay = heroDisplay ?? throw new ArgumentNullException(nameof(heroDisplay));
		ClanDisplay = clanDisplay ?? throw new ArgumentNullException(nameof(clanDisplay));
		KingdomDisplay = kingdomDisplay ?? throw new ArgumentNullException(nameof(kingdomDisplay));
		SettlementDisplay = settlementDisplay ?? throw new ArgumentNullException(nameof(settlementDisplay));
		SettlementName = settlementName ?? throw new ArgumentNullException(nameof(settlementName));
		SettlementNameWithType = settlementNameWithType ?? throw new ArgumentNullException(nameof(settlementNameWithType));
		Hero = hero ?? throw new ArgumentNullException(nameof(hero));
	}
}

// Called only during synchronous main-thread material preparation. No game object is retained.
internal sealed class WeeklyAggregateEventLineOwner
{
internal static List<string> ResolveNames(IEnumerable<string> ids, Func<string,string> resolve)
	{
		return (ids ?? Enumerable.Empty<string>()).Select(resolve).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private readonly WeeklyAggregateGameFacts _facts;
	internal WeeklyAggregateEventLineOwner(WeeklyAggregateGameFacts facts) { _facts = facts ?? throw new ArgumentNullException(nameof(facts)); }
	private static bool SameId(string left, string right) => !string.IsNullOrWhiteSpace(left) && string.Equals(left.Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	private string ResolveHeroDisplay(string id) => _facts.HeroDisplay(id);
	private string ResolveClanDisplay(string id) => _facts.ClanDisplay(id);
	private string ResolveKingdomDisplay(string id) => _facts.KingdomDisplay(id);
	private string ResolveSettlementDisplay(string id) => _facts.SettlementDisplay(id);

	internal string Render(List<EventMaterialReference> materials)
	{
		if (materials == null || materials.Count == 0)
		{
			return "";
		}
		string text = WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(materials[0]);
		switch (text)
		{
		case "strategic_shift":
			return BuildWeeklyPromptAggregateClanChangeLine(materials);
		case "settlement_change":
			return BuildWeeklyPromptAggregateSettlementOwnerChangeLine(materials);
		case "death":
			return BuildWeeklyPromptAggregateDeathLine(materials);
		case "captivity":
			return BuildWeeklyPromptAggregatePrisonerLine(materials, released: false);
		case "release":
			return BuildWeeklyPromptAggregatePrisonerLine(materials, released: true);
		case "decision":
			return BuildWeeklyPromptAggregateDecisionLine(materials);
		case "army":
			return BuildWeeklyPromptAggregateArmyLine(materials);
		case "siege":
			return BuildWeeklyPromptAggregateSiegeLine(materials);
		case "battle":
			return BuildWeeklyPromptAggregateBattleLine(materials);
		case "movement":
			return BuildWeeklyPromptAggregateMovementLine(materials);
		default:
			return BuildWeeklyPromptAggregateGenericLine(materials);
		}
	}

	private string BuildWeeklyPromptAggregateBattleLine(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		List<EventMaterialReference> list2 = list.Where((EventMaterialReference x) => string.Equals((x.ActionKind ?? "").Trim(), "map_event", StringComparison.OrdinalIgnoreCase)).ToList();
		if (list2.Count == 0)
		{
			list2 = list;
		}
		EventMaterialReference eventMaterialReference = list2.FirstOrDefault((EventMaterialReference x) => GetWeeklyPromptBattleMaterialWon(x) == true) ?? list2.FirstOrDefault() ?? list[0];
		string text2 = BuildWeeklyPromptAggregateBattleLocationText(eventMaterialReference);
		List<EventMaterialReference> winners = list2.Where((EventMaterialReference x) => GetWeeklyPromptBattleMaterialWon(x) == true).ToList();
		List<EventMaterialReference> losers = list2.Where((EventMaterialReference x) => GetWeeklyPromptBattleMaterialWon(x) == false).ToList();
		List<string> fields = new List<string>();
		fields.Add("事件=战场交锋");
		if (!string.IsNullOrWhiteSpace(text2))
		{
			fields.Add("地点=" + text2);
		}
		if (winners.Count > 0 || losers.Count > 0)
		{
			fields.Add("结果=" + BuildWeeklyPromptBattleOutcomeField(winners, losers));
		}
		string text3 = BuildWeeklyPromptBattleSideField("胜方", winners, losers);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			fields.Add(text3);
		}
		string text4 = BuildWeeklyPromptBattleSideField("败方", losers, winners);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			fields.Add(text4);
		}
		string text5 = BuildWeeklyPromptBattleTroopField(winners, losers);
		if (!string.IsNullOrWhiteSpace(text5))
		{
			fields.Add(text5);
		}
		string text6 = BuildWeeklyPromptBattleCasualtyField(winners, losers);
		if (!string.IsNullOrWhiteSpace(text6))
		{
			fields.Add(text6);
		}
		string text7 = BuildWeeklyPromptBattleStandoutField(winners, losers);
		if (!string.IsNullOrWhiteSpace(text7))
		{
			fields.Add(text7);
		}
		return string.Join("|", fields);
	}

	private bool? GetWeeklyPromptBattleMaterialWon(EventMaterialReference material)
	{
		if (material == null)
		{
			return null;
		}
		string text = (material.SnapshotText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (text.IndexOf("败给了", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("失利", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("遭遇了失利", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
			if (text.IndexOf("击败了", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("获胜", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("击退了", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("得手", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return material.Won;
	}

	private string BuildWeeklyPromptBattleOutcomeField(List<EventMaterialReference> winners, List<EventMaterialReference> losers)
	{
		string text = BuildWeeklyPromptBattleSideShortName(winners, losers);
		string text2 = BuildWeeklyPromptBattleSideShortName(losers, winners);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return text + "击败" + text2;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text + "取得胜利";
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2 + "遭遇失利";
		}
		return "胜负已分";
	}

	private string BuildWeeklyPromptBattleSideShortName(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = BuildWeeklyPromptBattleSideKingdomNames(ownMaterials, oppositeMaterials);
		if (list.Count > 0)
		{
			return string.Join("、", list.Take(2));
		}
		List<string> list2 = BuildWeeklyPromptBattleSideClanNames(ownMaterials, oppositeMaterials);
		if (list2.Count > 0)
		{
			return string.Join("、", list2.Take(2));
		}
		List<string> list3 = BuildWeeklyPromptBattleSideHeroNames(ownMaterials, oppositeMaterials);
		if (list3.Count > 0)
		{
			return string.Join("、", list3.Take(2));
		}
		List<string> list4 = BuildWeeklyPromptBattleSidePartyNames(ownMaterials, oppositeMaterials);
		return (list4.Count > 0) ? string.Join("、", list4.Take(2)) : "";
	}

	private string BuildWeeklyPromptBattleSideField(string label, List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = new List<string>();
		List<string> list2 = BuildWeeklyPromptBattleSideKingdomNames(ownMaterials, oppositeMaterials);
		if (list2.Count > 0)
		{
			list.Add("王国=" + string.Join("、", list2));
		}
		List<string> list3 = BuildWeeklyPromptBattleSideClanNames(ownMaterials, oppositeMaterials);
		if (list3.Count > 0)
		{
			list.Add("家族=" + string.Join("、", list3));
		}
		List<string> list4 = BuildWeeklyPromptBattleSideHeroNames(ownMaterials, oppositeMaterials);
		if (list4.Count > 0)
		{
			list.Add("人物=" + string.Join("、", list4.Take(8)));
		}
		List<string> list5 = BuildWeeklyPromptBattleSidePartyNames(ownMaterials, oppositeMaterials);
		if (list5.Count > 0)
		{
			list.Add("非领主部队=" + string.Join("、", list5.Take(8)));
		}
		if (list.Count == 0)
		{
			return "";
		}
		return label + "：" + string.Join("；", list);
	}

	private string BuildWeeklyPromptBattleStandoutField(List<EventMaterialReference> winners, List<EventMaterialReference> losers)
	{
		List<string> list = new List<string>();
		AppendWeeklyPromptBattleStandouts(list, "胜方", winners);
		AppendWeeklyPromptBattleStandouts(list, "败方", losers);
		if (list.Count == 0)
		{
			string text = BuildWeeklyPromptBattleFallbackStandoutText("胜方", winners, losers);
			if (!string.IsNullOrWhiteSpace(text))
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, "胜方：" + text);
			}
			text = BuildWeeklyPromptBattleFallbackStandoutText("败方", losers, winners);
			if (!string.IsNullOrWhiteSpace(text))
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, "败方：" + text);
			}
		}
		return (list.Count > 0) ? ("亮眼表现=" + string.Join("；", list)) : "";
	}

	private void AppendWeeklyPromptBattleStandouts(List<string> output, string sideLabel, List<EventMaterialReference> materials)
	{
		if (output == null)
		{
			return;
		}
		foreach (string item in ExtractWeeklyPromptBattleStandoutTexts(materials, sideLabel))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(output, sideLabel + "：" + item);
		}
	}

	private IEnumerable<string> ExtractWeeklyPromptBattleStandoutTexts(List<EventMaterialReference> materials, string sideLabel)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = ExtractWeeklyPromptBattleStandoutText(item?.SnapshotText, sideLabel);
			if (!string.IsNullOrWhiteSpace(text))
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, text);
			}
		}
		return list;
	}

	private string ExtractWeeklyPromptBattleStandoutText(string text, string sideLabel)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		int num = text2.IndexOf(" ：", StringComparison.Ordinal);
		int markerLength = 2;
		if (num < 0)
		{
			num = text2.IndexOf(" :", StringComparison.Ordinal);
		}
		if (num < 0)
		{
			return "";
		}
		num += markerLength;
		while (num < text2.Length && text2[num] == ' ')
		{
			num++;
		}
		int num2 = text2.IndexOf('；', num);
		if (num2 < 0)
		{
			num2 = text2.IndexOf(';', num);
		}
		if (num2 < 0)
		{
			num2 = text2.Length;
		}
		if (num2 <= num)
		{
			return "";
		}
		string text3 = text2.Substring(num, num2 - num).Trim(' ', '。', '；', ';');
		if (string.IsNullOrWhiteSpace(text3))
		{
			return "";
		}
		text3 = Regex.Replace(text3, "\\s+", " ").Trim();
		if (!string.IsNullOrWhiteSpace(sideLabel))
		{
			text3 = text3.Replace("我方", sideLabel.Trim());
		}
		return text3;
	}

	private string BuildWeeklyPromptBattleFallbackStandoutText(string sideLabel, List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<EventMaterialReference> list = (ownMaterials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		string text = BuildWeeklyPromptBattleStandoutActorText(list, oppositeMaterials, sideLabel);
		string text2 = BuildWeeklyPromptBattleSideTroopText(list, oppositeMaterials);
		string text3 = BuildWeeklyPromptBattleOppositeTroopText(list, oppositeMaterials);
		string text4 = BuildWeeklyPromptBattleSideCasualtyText(list, oppositeMaterials);
		string text5 = BuildWeeklyPromptBattleOppositeCasualtyText(list, oppositeMaterials);
		bool flag = TryParseWeeklyPromptBattleCasualtyTotal(text4, out var ownCasualtyTotal);
		bool flag2 = TryParseWeeklyPromptBattleCasualtyTotal(text5, out var oppositeCasualtyTotal);
		bool flag3 = TryParseWeeklyPromptBattleTroopTotal(text2, out var ownTroops);
		bool flag4 = TryParseWeeklyPromptBattleTroopTotal(text3, out var oppositeTroops);
		List<string> list2 = new List<string>();
		if (flag3 && flag4 && ownTroops > 0 && oppositeTroops > 0)
		{
			list2.Add("投入兵力" + ownTroops + "人对" + oppositeTroops + "人");
		}
		else if (flag3 && ownTroops > 0)
		{
			list2.Add("投入兵力" + ownTroops + "人");
		}
		if (flag && flag2)
		{
			if (string.Equals((sideLabel ?? "").Trim(), "胜方", StringComparison.OrdinalIgnoreCase))
			{
				if (ownCasualtyTotal <= 0 && oppositeCasualtyTotal > 0)
				{
					list2.Add("未承受可确认伤亡，造成败方" + oppositeCasualtyTotal + "人伤亡");
				}
				else
				{
					list2.Add("以" + ownCasualtyTotal + "人损失造成败方" + oppositeCasualtyTotal + "人伤亡");
				}
			}
			else if (oppositeCasualtyTotal > 0)
			{
				list2.Add("虽遭失利，仍造成胜方" + oppositeCasualtyTotal + "人伤亡");
			}
		}
		if (list2.Count == 0)
		{
			return "";
		}
		return text + string.Join("，", list2);
	}

	private string BuildWeeklyPromptBattleStandoutActorText(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials, string fallbackLabel)
	{
		List<string> list = BuildWeeklyPromptBattleSideHeroNames(ownMaterials, oppositeMaterials);
		string text = list.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x) && x.IndexOf("（统帅）", StringComparison.OrdinalIgnoreCase) >= 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = list.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim() + "部队";
		}
		List<string> list2 = BuildWeeklyPromptBattleSidePartyNames(ownMaterials, oppositeMaterials);
		text = list2.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = BuildWeeklyPromptBattleSideShortName(ownMaterials, oppositeMaterials);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim() + "部队";
		}
		return string.IsNullOrWhiteSpace(fallbackLabel) ? "该方部队" : (fallbackLabel.Trim() + "部队");
	}

	private string BuildWeeklyPromptBattleSideTroopText(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		string text = ExtractWeeklyPromptBattleTroopText(ownMaterials, ownSide: true);
		return string.IsNullOrWhiteSpace(text) ? ExtractWeeklyPromptBattleTroopText(oppositeMaterials, ownSide: false) : text;
	}

	private string BuildWeeklyPromptBattleOppositeTroopText(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		string text = ExtractWeeklyPromptBattleTroopText(oppositeMaterials, ownSide: true);
		return string.IsNullOrWhiteSpace(text) ? ExtractWeeklyPromptBattleTroopText(ownMaterials, ownSide: false) : text;
	}

	private string BuildWeeklyPromptBattleSideCasualtyText(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		string text = ExtractWeeklyPromptBattleCasualtyText(ownMaterials, ownSide: true);
		return string.IsNullOrWhiteSpace(text) ? ExtractWeeklyPromptBattleCasualtyText(oppositeMaterials, ownSide: false) : text;
	}

	private string BuildWeeklyPromptBattleOppositeCasualtyText(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		string text = ExtractWeeklyPromptBattleCasualtyText(oppositeMaterials, ownSide: true);
		return string.IsNullOrWhiteSpace(text) ? ExtractWeeklyPromptBattleCasualtyText(ownMaterials, ownSide: false) : text;
	}

	private bool TryParseWeeklyPromptBattleTroopTotal(string text, out int total)
	{
		total = 0;
		Match match = Regex.Match(text ?? "", "(?<value>\\d+)\\s*人", RegexOptions.IgnoreCase);
		return match.Success && int.TryParse(match.Groups["value"]?.Value ?? "", out total);
	}

	private bool TryParseWeeklyPromptBattleCasualtyTotal(string text, out int total)
	{
		total = 0;
		Match match = Regex.Match(text ?? "", "阵亡\\s*(?<dead>\\d+)\\s*[、,，]\\s*负伤\\s*(?<wounded>\\d+)", RegexOptions.IgnoreCase);
		if (!match.Success || !int.TryParse(match.Groups["dead"]?.Value ?? "", out var dead) || !int.TryParse(match.Groups["wounded"]?.Value ?? "", out var wounded))
		{
			return false;
		}
		total = Math.Max(0, dead) + Math.Max(0, wounded);
		return true;
	}

	private List<string> BuildWeeklyPromptBattleSideKingdomNames(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = ResolveKingdomNames(CollectMaterialIds(ownMaterials, (EventMaterialReference x) => new string[1] { x.ActorKingdomId }, null));
		if (list.Count > 0)
		{
			return list;
		}
		return ResolveKingdomNames(CollectMaterialIds(oppositeMaterials, (EventMaterialReference x) => new string[1] { x.TargetKingdomId }, null));
	}

	private List<string> BuildWeeklyPromptBattleSideClanNames(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = ResolveClanNames(CollectMaterialIds(ownMaterials, (EventMaterialReference x) => new string[1] { x.ActorClanId }, null));
		if (list.Count > 0)
		{
			return list;
		}
		return ResolveClanNames(CollectMaterialIds(oppositeMaterials, (EventMaterialReference x) => new string[1] { x.TargetClanId }, null));
	}

	private List<string> BuildWeeklyPromptBattleSideHeroNames(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = new List<string>();
		foreach (string item in ResolveHeroNames(CollectMaterialIds(ownMaterials, (EventMaterialReference x) => new string[1] { x.ActorHeroId }, null)))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(list, item);
		}
		foreach (string item2 in ExtractWeeklyPromptBattleHeroNames(ownMaterials, "我方领主"))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(list, item2);
		}
		if (list.Count > 0)
		{
			return list;
		}
		foreach (string item3 in ResolveHeroNames(CollectMaterialIds(oppositeMaterials, (EventMaterialReference x) => new string[1] { x.TargetHeroId }, null)).Concat(ExtractWeeklyPromptBattleHeroNames(oppositeMaterials, "敌方领主")))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(list, item3);
		}
		return list;
	}

	private List<string> BuildWeeklyPromptBattleSidePartyNames(List<EventMaterialReference> ownMaterials, List<EventMaterialReference> oppositeMaterials)
	{
		List<string> list = new List<string>();
		foreach (string item in ExtractWeeklyPromptBattlePartyNames(ownMaterials, "我方非领主部队"))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(list, item);
		}
		if (list.Count > 0)
		{
			return list;
		}
		foreach (string item2 in ExtractWeeklyPromptBattlePartyNames(oppositeMaterials, "敌方非领主部队"))
		{
			WeeklyMaterialAggregationOwner.AddUniqueId(list, item2);
		}
		return list;
	}

	private IEnumerable<string> ExtractWeeklyPromptBattlePartyNames(List<EventMaterialReference> materials, string marker)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = ExtractWeeklyPromptBattleMarkedText(item?.SnapshotText, marker);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			foreach (string item2 in text.Split(new char[5] { '、', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string text2 = item2.Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					WeeklyMaterialAggregationOwner.AddUniqueId(list, text2);
				}
			}
		}
		return list;
	}

	private IEnumerable<string> ExtractWeeklyPromptBattleHeroNames(List<EventMaterialReference> materials, string marker)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = ExtractWeeklyPromptBattleMarkedText(item?.SnapshotText, marker);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			foreach (string item2 in text.Split(new char[5] { '、', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string text2 = item2.Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					WeeklyMaterialAggregationOwner.AddUniqueId(list, text2);
				}
			}
		}
		return list;
	}

	private string BuildWeeklyPromptBattleTroopField(List<EventMaterialReference> winners, List<EventMaterialReference> losers)
	{
		string text = ExtractWeeklyPromptBattleTroopText(winners, ownSide: true);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ExtractWeeklyPromptBattleTroopText(losers, ownSide: false);
		}
		string text2 = ExtractWeeklyPromptBattleTroopText(losers, ownSide: true);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = ExtractWeeklyPromptBattleTroopText(winners, ownSide: false);
		}
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return "战前投入兵力=胜方" + (string.IsNullOrWhiteSpace(text) ? "不详" : text) + "；败方" + (string.IsNullOrWhiteSpace(text2) ? "不详" : text2);
	}

	private string BuildWeeklyPromptBattleCasualtyField(List<EventMaterialReference> winners, List<EventMaterialReference> losers)
	{
		string text = ExtractWeeklyPromptBattleCasualtyText(winners, ownSide: true);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ExtractWeeklyPromptBattleCasualtyText(losers, ownSide: false);
		}
		string text2 = ExtractWeeklyPromptBattleCasualtyText(losers, ownSide: true);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = ExtractWeeklyPromptBattleCasualtyText(winners, ownSide: false);
		}
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return "伤亡=胜方" + (string.IsNullOrWhiteSpace(text) ? "不详" : text) + "；败方" + (string.IsNullOrWhiteSpace(text2) ? "不详" : text2);
	}

	private string ExtractWeeklyPromptBattleTroopText(List<EventMaterialReference> materials, bool ownSide)
	{
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = ExtractWeeklyPromptBattleTroopText(item?.SnapshotText, ownSide);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		return "";
	}

	private string ExtractWeeklyPromptBattleTroopText(string text, bool ownSide)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		Match match = Regex.Match(text2, "战前投入兵力：我方(?<own>[^；。]+)；敌方(?<enemy>[^。；]+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return "";
		}
		string value = ownSide ? (match.Groups["own"]?.Value ?? "") : (match.Groups["enemy"]?.Value ?? "");
		return (value ?? "").Trim();
	}

	private string ExtractWeeklyPromptBattleCasualtyText(List<EventMaterialReference> materials, bool ownSide)
	{
		string marker = ownSide ? "我方死伤" : "敌方死伤";
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = ExtractWeeklyPromptBattleMarkedText(item?.SnapshotText, marker);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		return "";
	}

	private string ExtractWeeklyPromptBattleMarkedText(string text, string marker)
	{
		string text2 = (text ?? "").Trim();
		string text3 = (marker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
		{
			return "";
		}
		int num = text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		num += text3.Length;
		while (num < text2.Length && (text2[num] == '：' || text2[num] == ':' || text2[num] == ' '))
		{
			num++;
		}
		int num2 = text2.Length;
		foreach (char value in new char[4] { '。', '；', ';', '\n' })
		{
			int num3 = text2.IndexOf(value, num);
			if (num3 >= num && num3 < num2)
			{
				num2 = num3;
			}
		}
		if (num2 <= num)
		{
			return "";
		}
		return text2.Substring(num, num2 - num).Trim();
	}

	private string BuildWeeklyPromptAggregateGenericLine(List<EventMaterialReference> materials)
	{
		EventMaterialReference eventMaterialReference = materials.FirstOrDefault((EventMaterialReference x) => x != null);
		if (eventMaterialReference == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		AppendWeeklyPromptAggregateField(list, "人物归属", BuildWeeklyPromptAggregateHeroAffiliationValues(materials));
		list.Add("事件=" + BuildWeeklyPromptAggregateActionLabel(materials));
		AppendWeeklyPromptAggregateField(list, "人物", ResolveHeroNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[3] { x.ActorHeroId, x.TargetHeroId, x.HeroId }, delegate(EventMaterialReference x)
		{
			return x.RelatedHeroIds;
		})));
		AppendWeeklyPromptAggregateField(list, "家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[4] { x.ActorClanId, x.TargetClanId, x.SettlementOwnerClanId, x.PreviousSettlementOwnerClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[5] { x.ActorKingdomId, x.TargetKingdomId, x.KingdomId, x.SettlementOwnerKingdomId, x.PreviousSettlementOwnerKingdomId }, delegate(EventMaterialReference x)
		{
			return x.RelatedKingdomIds;
		})));
		AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateDeathLine(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		Dictionary<string, List<EventMaterialReference>> dictionary = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list)
		{
			string text = ResolveWeeklyPromptAggregateDeathVictimHeroId(item);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(item);
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new List<EventMaterialReference>();
				dictionary[text] = value;
			}
			value.Add(item);
		}
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, List<EventMaterialReference>> item2 in dictionary)
		{
			List<EventMaterialReference> value2 = item2.Value;
			EventMaterialReference eventMaterialReference = value2.FirstOrDefault((EventMaterialReference x) => x != null);
			if (eventMaterialReference == null)
			{
				continue;
			}
			string text2 = ResolveWeeklyPromptAggregateDeathVictimHeroId(eventMaterialReference);
			if (string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(item2.Key) && item2.Key.IndexOf('|') < 0)
			{
				text2 = item2.Key;
			}
			string text3 = (value2.Select((EventMaterialReference x) => (x.TargetClanId ?? "").Trim()).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "").Trim();
			string text4 = (value2.Select((EventMaterialReference x) => (x.TargetKingdomId ?? "").Trim()).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				WeeklyHeroFact heroFact = _facts.Hero(text2);
				if (string.IsNullOrWhiteSpace(text3))
				{
					text3 = (heroFact?.ClanId ?? "").Trim();
				}
				if (string.IsNullOrWhiteSpace(text4))
				{
					text4 = (heroFact?.KingdomId ?? "").Trim();
				}
			}
			string text5 = BuildWeeklyPromptAggregateDeathSubjectText(text2, text3, text4);
			if (string.IsNullOrWhiteSpace(text5))
			{
				text5 = "某家族成员";
			}
			string text6 = BuildWeeklyPromptAggregateDeathRelationText(text2, ResolveWeeklyPromptAggregateDeathRelationHeroId(value2, text2));
			string text7 = value2.Select((EventMaterialReference x) => ParseWeeklyPromptHeroDeathReason(x?.ActionStableKey)).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
			if (string.IsNullOrWhiteSpace(text7))
			{
				text7 = "不明原因";
			}
			string text8 = string.IsNullOrWhiteSpace(text6) ? (text5 + "，因为" + text7 + "去世了。") : (text5 + "，" + text6 + "，因为" + text7 + "去世了。");
			if (!list2.Contains(text8))
			{
				list2.Add(text8);
			}
		}
		if (list2.Count == 0)
		{
			return BuildWeeklyPromptAggregateGenericLine(materials);
		}
		return "事件=人物死亡：" + string.Join("；", list2);
	}

	private string BuildWeeklyPromptAggregateArmyLine(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		string text = BuildWeeklyPromptAggregateActionLabel(list);
		List<string> list2 = list.Select((EventMaterialReference x) => ((x.ActionKind ?? "").Trim().ToLowerInvariant())).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		string text2 = (list2.Count == 1) ? list2[0] : "";
		string text3 = BuildWeeklyPromptAggregateSiegeParticipantsText(list);
		if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
		{
			List<string> list3 = ResolveHeroNames(list.Select((EventMaterialReference x) => x.TargetHeroId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
			string text4 = "";
			switch (text2)
			{
			case "army_disperse":
				text4 = text3 + "解散了军团";
				break;
			case "army_create":
				text4 = text3 + "组建了新的军团";
				break;
			case "army_gather":
				text4 = text3 + "开始集结军团";
				break;
			case "army_join":
				text4 = ((list3.Count == 1) ? (text3 + "加入了" + list3[0] + "的军团") : (text3 + "加入了军团"));
				break;
			case "army_leave":
				text4 = ((list3.Count == 1) ? (text3 + "离开了" + list3[0] + "的军团") : (text3 + "离开了军团"));
				break;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return "事件=" + text + "：" + text4;
			}
		}
		List<string> list5 = new List<string>();
		foreach (EventMaterialReference item in list)
		{
			string text5 = ((item.ActionKind ?? "").Trim().ToLowerInvariant());
			string text6 = BuildWeeklyPromptAggregateActorAffiliationText(item);
			string text7 = ResolveHeroDisplay(item.TargetHeroId);
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "某领主";
			}
			string text8 = "";
			switch (text5)
			{
			case "army_disperse":
				text8 = text6 + "解散了他的军团";
				break;
			case "army_create":
				text8 = text6 + "组建了新的军团";
				break;
			case "army_gather":
				text8 = text6 + "开始集结军团";
				break;
			case "army_join":
				text8 = string.IsNullOrWhiteSpace(text7) ? (text6 + "加入了军团") : (text6 + "加入了" + text7 + "的军团");
				break;
			case "army_leave":
				text8 = string.IsNullOrWhiteSpace(text7) ? (text6 + "离开了军团") : (text6 + "离开了" + text7 + "的军团");
				break;
			}
			if (string.IsNullOrWhiteSpace(text8))
			{
				string text9 = (item.SnapshotText ?? item.Label ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text9))
				{
					text8 = text6 + "：" + text9;
				}
			}
			if (!string.IsNullOrWhiteSpace(text8) && !list5.Contains(text8))
			{
				list5.Add(text8);
			}
		}
		if (list5.Count == 0)
		{
			return BuildWeeklyPromptAggregateGenericLine(materials);
		}
		return "事件=" + text + "：" + string.Join("；", list5);
	}

	private string ResolveWeeklyPromptAggregateDeathVictimHeroId(EventMaterialReference item)
	{
		if (string.Equals((item?.ActionKind ?? "").Trim(), "hero_executed_victim", StringComparison.OrdinalIgnoreCase))
		{
			string actorHeroId = (item?.ActorHeroId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(actorHeroId))
			{
				return actorHeroId;
			}
		}
		string text = (item?.TargetHeroId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (item?.ActionStableKey ?? "").Trim();
		string[] array = text2.Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length >= 3 && string.Equals((array[0] ?? "").Trim(), "hero_killed", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = (array[1] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3;
			}
		}
		return (item?.ActorHeroId ?? "").Trim();
	}

	private string BuildWeeklyPromptAggregateDeathSubjectText(string heroId, string clanId, string kingdomId)
	{
		string text = ResolveHeroDisplay(heroId);
		string text2 = ResolveClanDisplay(clanId);
		string text3 = ResolveKingdomDisplay(kingdomId);
		if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text))
		{
			return text3 + "的" + text2 + "家族的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text))
		{
			return text2 + "家族的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text))
		{
			return text3 + "的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text2))
		{
			return text3 + "的" + text2 + "家族成员";
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2 + "家族成员";
		}
		if (!string.IsNullOrWhiteSpace(text3))
		{
			return text3 + "的成员";
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateDeathRelationText(string victimHeroId, string fallbackRelationHeroId)
	{
		WeeklyHeroFact victim = _facts.Hero(victimHeroId);
		WeeklyHeroFact other = _facts.Hero(fallbackRelationHeroId);
		if (victim == null || other == null || string.Equals(victim.Id, other.Id, StringComparison.OrdinalIgnoreCase)) return "";
		string name = ResolveHeroDisplay(other.Id);
		if (string.IsNullOrWhiteSpace(name)) return "";
		if (SameId(victim.SpouseId, other.Id) || SameId(other.SpouseId, victim.Id)) return name + "的" + (victim.IsFemale ? "妻子" : "丈夫");
		if (SameId(victim.FatherId, other.Id) || SameId(victim.MotherId, other.Id)) return name + "的" + (victim.IsFemale ? "女儿" : "儿子");
		if (SameId(other.FatherId, victim.Id) || SameId(other.MotherId, victim.Id)) return name + "的" + (victim.IsFemale ? "母亲" : "父亲");
		if (SameId(victim.FatherId, other.FatherId) || SameId(victim.MotherId, other.MotherId)) return name + "的" + (victim.IsFemale ? "姐妹" : "兄弟");
		if (SameId(victim.ClanId, other.ClanId)) return name + "的家族成员";
		return "";
	}

	private string ResolveWeeklyPromptAggregateDeathRelationHeroId(List<EventMaterialReference> materials, string victimHeroId)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		string text = list.Where((EventMaterialReference x) => string.Equals((x.ActionKind ?? "").Trim(), "clan_member_killed", StringComparison.OrdinalIgnoreCase)).Select((EventMaterialReference x) => (x.ActorHeroId ?? "").Trim()).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, (victimHeroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return list.Select((EventMaterialReference x) => (x.ActorHeroId ?? "").Trim()).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, (victimHeroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ?? "";
	}

	private string ParseWeeklyPromptHeroDeathReason(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 3)
		{
			return "";
		}
		if (!string.Equals((array[0] ?? "").Trim(), "hero_killed", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return TranslateWeeklyPromptHeroDeathDetail((array[2] ?? "").Trim());
	}

	private string TranslateWeeklyPromptHeroDeathDetail(string detail)
	{
		switch ((detail ?? "").Trim())
		{
		case "Murdered":
			return "遭遇谋杀";
		case "DiedInLabor":
			return "分娩事故";
		case "DiedOfOldAge":
			return "寿终正寝";
		case "DiedInBattle":
			return "战死沙场";
		case "WoundedInBattle":
			return "重伤不治";
		case "Executed":
		case "ExecutionAfterMapEvent":
			return "被处决";
		default:
			return string.IsNullOrWhiteSpace(detail) ? "" : detail;
		}
	}

	private string BuildWeeklyPromptAggregateMovementLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=近期行军动向");
		AppendWeeklyPromptAggregateField(list, "动向明细", BuildWeeklyPromptAggregateMovementDetailValues(materials));
		AppendWeeklyPromptAggregateField(list, "涉及地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateDecisionLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=王国决议");
		AppendWeeklyPromptAggregateField(list, "议题明细", BuildWeeklyPromptAggregateDetailValues(materials));
		AppendWeeklyPromptAggregateField(list, "相关人物归属", BuildWeeklyPromptAggregateHeroAffiliationValues(materials));
		AppendWeeklyPromptAggregateField(list, "相关家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[3] { x.ActorClanId, x.TargetClanId, x.SettlementOwnerClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "相关王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[4] { x.ActorKingdomId, x.TargetKingdomId, x.KingdomId, x.SettlementOwnerKingdomId }, delegate(EventMaterialReference x)
		{
			return x.RelatedKingdomIds;
		})));
		AppendWeeklyPromptAggregateField(list, "涉及地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateSiegeLine(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		string text = BuildWeeklyPromptAggregateActionLabel(list);
		string text2 = ((list[0].ActionKind ?? "").Trim().ToLowerInvariant());
		string text3 = BuildWeeklyPromptAggregateSiegeParticipantsText(list);
		string text4 = BuildWeeklyPromptAggregateSiegeTargetKingdomText(list);
		string text5 = BuildWeeklyPromptAggregateSiegeSettlementText(list);
		string text6 = string.IsNullOrWhiteSpace(text3) ? "相关部队" : text3;
		string text7 = string.IsNullOrWhiteSpace(text5) ? "目标定居点" : text5;
		string text8 = string.IsNullOrWhiteSpace(text4) ? text7 : (text4 + "的" + text7);
		string text9;
		switch (text2)
		{
		case "siege_join":
			text9 = text6 + "加入了对" + text8 + "的围攻";
			break;
		case "siege_leave":
			text9 = text6 + "结束了对" + text8 + "的围攻";
			break;
		case "siege_start_attack":
			text9 = text6 + "对" + text8 + "发起了围攻";
			break;
		case "siege_start_defend":
			text9 = text6 + "加入了" + text7 + "的守城";
			break;
		case "siege_end_attack":
			text9 = text6 + "结束了对" + text8 + "的围攻";
			break;
		case "siege_end_defend":
			text9 = text6 + "结束了在" + text7 + "的守城";
			break;
		default:
		{
			List<string> list2 = list.Where((EventMaterialReference x) => x != null && x.ActionKind == "siege_complete").Select(delegate(EventMaterialReference x)
			{
				if (!string.IsNullOrWhiteSpace(x.SnapshotText))
				{
					if (x.SnapshotText.IndexOf("获胜", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return "获胜";
					}
					if (x.SnapshotText.IndexOf("失利", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return "失利";
					}
				}
				return "";
			}).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			text9 = text6 + "围绕" + text7 + "进行了围城行动";
			if (list2.Count > 0)
			{
				text9 += "，结果为" + string.Join("、", list2);
			}
			break;
		}
		}
		return "事件=" + text + "：" + text9;
	}

	private string BuildWeeklyPromptAggregateSiegeParticipantsText(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		List<string> list2 = new List<string>();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list)
		{
			string text = (item.ActorKingdomId ?? "").Trim();
			string text2 = (item.ActorClanId ?? "").Trim();
			string text3 = ResolveHeroDisplay(!string.IsNullOrWhiteSpace(item.ActorHeroId) ? item.ActorHeroId : item.HeroId);
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			WeeklyMaterialAggregationOwner.AddUniqueId(list2, text);
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new List<string>();
				dictionary[text] = value;
			}
			WeeklyMaterialAggregationOwner.AddUniqueId(value, text2);
			string key = text + "|" + text2;
			if (!dictionary2.TryGetValue(key, out var value2))
			{
				value2 = new List<string>();
				dictionary2[key] = value2;
			}
			WeeklyMaterialAggregationOwner.AddUniqueId(value2, text3);
		}
		List<string> list3 = new List<string>();
		foreach (string item2 in list2)
		{
			List<string> list4 = dictionary.ContainsKey(item2) ? dictionary[item2] : new List<string>();
			List<string> list5 = new List<string>();
			foreach (string item3 in list4)
			{
				string key2 = item2 + "|" + item3;
				List<string> list6 = dictionary2.ContainsKey(key2) ? dictionary2[key2] : new List<string>();
				if (list6.Count == 0)
				{
					continue;
				}
				string text4 = ResolveClanDisplay(item3);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					list5.Add(text4 + "家族的" + string.Join("、", list6));
				}
				else
				{
					list5.Add(string.Join("、", list6));
				}
			}
			if (list5.Count == 0)
			{
				continue;
			}
			string text5 = ResolveKingdomDisplay(item2);
			if (!string.IsNullOrWhiteSpace(text5))
			{
				list3.Add(text5 + "所属的" + JoinWithYiJi(list5));
			}
			else
			{
				list3.Add(JoinWithYiJi(list5));
			}
		}
		return JoinWithYiJi(list3);
	}

	private string BuildWeeklyPromptAggregateSiegeTargetKingdomText(List<EventMaterialReference> materials)
	{
		List<string> list = ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.TargetKingdomId, x.SettlementOwnerKingdomId }, null));
		if (list.Count == 0)
		{
			return "";
		}
		return string.Join("、", list);
	}

	private string BuildWeeklyPromptAggregateSiegeSettlementText(List<EventMaterialReference> materials)
	{
		string id = (materials ?? new List<EventMaterialReference>()).Where(x => x != null).Select(x => (x.SettlementId ?? "").Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
		return string.IsNullOrWhiteSpace(id) ? "" : _facts.SettlementName(id);
	}

	private string JoinWithYiJi(List<string> parts)
	{
		List<string> list = (parts ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		if (list.Count == 2)
		{
			return list[0] + "和" + list[1];
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.Join("、", list.Take(list.Count - 1)));
		stringBuilder.Append("，还有");
		stringBuilder.Append(list[list.Count - 1]);
		return stringBuilder.ToString();
	}


	private string BuildWeeklyPromptAggregatePrisonerLine(List<EventMaterialReference> materials, bool released)
	{
		List<string> list = new List<string>();
		list.Add("事件=" + (released ? "人物获释" : "人物被俘"));
		List<string> pairOrder = new List<string>();
		Dictionary<string, string> captorHeroByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> prisonerHeroByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> captorClanByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> prisonerClanByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> captorKingdomByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> prisonerKingdomByPair = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> settlementIdsByPair = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> releaseDetailsByPair = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		void setIfEmpty(Dictionary<string, string> map, string key, string value)
		{
			if (!map.ContainsKey(key) || string.IsNullOrWhiteSpace(map[key]))
			{
				map[key] = (value ?? "").Trim();
			}
		}

		void ensurePair(string pairKey)
		{
			if (!pairOrder.Contains(pairKey))
			{
				pairOrder.Add(pairKey);
			}
			if (!settlementIdsByPair.ContainsKey(pairKey))
			{
				settlementIdsByPair[pairKey] = new List<string>();
			}
			if (!releaseDetailsByPair.ContainsKey(pairKey))
			{
				releaseDetailsByPair[pairKey] = new List<string>();
			}
		}

		void mergePair(EventMaterialReference item, string captorHeroId, string prisonerHeroId, string captorClanId, string prisonerClanId, string captorKingdomId, string prisonerKingdomId)
		{
			string text2 = (captorHeroId ?? "").Trim();
			string text3 = (prisonerHeroId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2) && string.IsNullOrWhiteSpace(text3))
			{
				return;
			}
			string pairKey = text2 + "->" + text3;
			ensurePair(pairKey);
			setIfEmpty(captorHeroByPair, pairKey, text2);
			setIfEmpty(prisonerHeroByPair, pairKey, text3);
			setIfEmpty(captorClanByPair, pairKey, captorClanId);
			setIfEmpty(prisonerClanByPair, pairKey, prisonerClanId);
			setIfEmpty(captorKingdomByPair, pairKey, captorKingdomId);
			setIfEmpty(prisonerKingdomByPair, pairKey, prisonerKingdomId);
			if (item != null)
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(settlementIdsByPair[pairKey], item.SettlementId);
				if (released)
				{
					WeeklyMaterialAggregationOwner.AddUniqueId(releaseDetailsByPair[pairKey], ParseWeeklyPromptReleaseDetail(item.ActionStableKey));
				}
			}
		}

		foreach (EventMaterialReference item in materials.Where((EventMaterialReference x) => x != null))
		{
			string text = (item.ActionKind ?? "").Trim().ToLowerInvariant();
			if (!released)
			{
				string text2;
				string text3;
				if (TryParseWeeklyPromptPrisonerPair(item.ActionStableKey, out text2, out text3) && text.StartsWith("prisoner_taken_", StringComparison.OrdinalIgnoreCase))
				{
					if (text == "prisoner_taken_captor")
					{
						mergePair(item, text2, text3, item.ActorClanId, item.TargetClanId, item.ActorKingdomId, item.TargetKingdomId);
						continue;
					}
					if (text == "prisoner_taken_prisoner")
					{
						mergePair(item, text2, text3, item.TargetClanId, item.ActorClanId, item.TargetKingdomId, item.ActorKingdomId);
						continue;
					}
				}
			}
			else
			{
				string text4;
				string text5;
				if (TryParseWeeklyPromptPrisonerPair(item.ActionStableKey, out text4, out text5) && text.StartsWith("prisoner_released_", StringComparison.OrdinalIgnoreCase))
				{
					if (text == "prisoner_released_captor")
					{
						mergePair(item, text4, text5, item.TargetClanId, item.ActorClanId, item.TargetKingdomId, item.ActorKingdomId);
						continue;
					}
					if (text == "prisoner_released_prisoner")
					{
						mergePair(item, text4, text5, item.TargetClanId, item.ActorClanId, item.TargetKingdomId, item.ActorKingdomId);
						continue;
					}
				}
			}

			if (!released)
			{
				if (text == "prisoner_taken_captor")
				{
					mergePair(item, item.ActorHeroId, item.TargetHeroId, item.ActorClanId, item.TargetClanId, item.ActorKingdomId, item.TargetKingdomId);
				}
				else if (text == "prisoner_taken_prisoner")
				{
					mergePair(item, item.TargetHeroId, item.ActorHeroId, item.TargetClanId, item.ActorClanId, item.TargetKingdomId, item.ActorKingdomId);
				}
			}
			else if (text == "prisoner_released_captor" || text == "prisoner_released_prisoner")
			{
				mergePair(item, item.TargetHeroId, item.ActorHeroId, item.TargetClanId, item.ActorClanId, item.TargetKingdomId, item.ActorKingdomId);
			}
		}

		List<string> list2 = new List<string>();
		foreach (string item2 in pairOrder)
		{
			string text6 = captorHeroByPair.ContainsKey(item2) ? captorHeroByPair[item2] : "";
			string text7 = prisonerHeroByPair.ContainsKey(item2) ? prisonerHeroByPair[item2] : "";
			string text8 = BuildWeeklyPromptAggregateHumanAffiliationText(text6, captorClanByPair.ContainsKey(item2) ? captorClanByPair[item2] : "", captorKingdomByPair.ContainsKey(item2) ? captorKingdomByPair[item2] : "");
			string text9 = BuildWeeklyPromptAggregateHumanAffiliationText(text7, prisonerClanByPair.ContainsKey(item2) ? prisonerClanByPair[item2] : "", prisonerKingdomByPair.ContainsKey(item2) ? prisonerKingdomByPair[item2] : "");
			string text10 = string.IsNullOrWhiteSpace(text8) ? "未知势力" : text8;
			string text11 = string.IsNullOrWhiteSpace(text9) ? "目标人物" : text9;
			List<string> list3 = ResolveSettlementNames(settlementIdsByPair.ContainsKey(item2) ? settlementIdsByPair[item2] : new List<string>());
			List<string> list4 = released ? releaseDetailsByPair[item2].Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>();
			List<string> list5 = new List<string>();
			if (list3.Count > 0)
			{
				list5.Add("地点：" + string.Join("、", list3));
			}
			if (released && list4.Count > 0)
			{
				list5.Add("方式：" + string.Join("、", list4));
			}
			string text12 = (!released) ? (text11 + "被" + text10 + "俘虏") : (text11 + "被" + text10 + "从俘虏状态中释放");
			if (list5.Count > 0)
			{
				text12 += "（" + string.Join("；", list5) + "）";
			}
			list2.Add(text12);
		}
		if (list2.Count > 0)
		{
			return "事件=" + (released ? "人物获释" : "人物被俘") + "：" + string.Join("；", list2);
		}
		else
		{
			AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		}
		return string.Join("|", list);
	}

	internal static bool TryParseWeeklyPromptPrisonerPair(string stableKey, out string captorHeroId, out string prisonerHeroId)
	{
		captorHeroId = "";
		prisonerHeroId = "";
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 3)
		{
			return false;
		}
		if (!string.Equals((array[0] ?? "").Trim(), "prisoner_released", StringComparison.OrdinalIgnoreCase) && !string.Equals((array[0] ?? "").Trim(), "prisoner_taken", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		captorHeroId = (array[1] ?? "").Trim();
		prisonerHeroId = (array[2] ?? "").Trim();
		return !string.IsNullOrWhiteSpace(captorHeroId) || !string.IsNullOrWhiteSpace(prisonerHeroId);
	}

	private string BuildWeeklyPromptAggregateHumanAffiliationText(string heroId, string clanId, string kingdomId)
	{
		string text = ResolveHeroDisplay(heroId);
		string text2 = ResolveClanDisplay(clanId);
		string text3 = ResolveKingdomDisplay(kingdomId);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
		{
			return text3 + "所属的" + text2 + "家族的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return text2 + "家族的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text3))
		{
			return text3 + "所属的" + text;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
		{
			return text3 + "所属的" + text2 + "家族";
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2 + "家族";
		}
		if (!string.IsNullOrWhiteSpace(text3))
		{
			return text3;
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateSettlementOwnerChangeLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=定居点易主");
		AppendWeeklyPromptAggregateField(list, "定居点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		List<string> list5 = new List<string>();
		List<string> list6 = new List<string>();
		foreach (EventMaterialReference item in materials.Where((EventMaterialReference x) => x != null))
		{
			string text = (item.ActionKind ?? "").Trim().ToLowerInvariant();
			if (text == "settlement_owner_changed_gain")
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list2, item.ActorHeroId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list3, item.TargetHeroId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list5, item.ActorKingdomId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list6, item.TargetKingdomId);
			}
			else if (text == "settlement_owner_changed_loss")
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list2, item.TargetHeroId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list3, item.ActorHeroId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list5, item.TargetKingdomId);
				WeeklyMaterialAggregationOwner.AddUniqueId(list6, item.ActorKingdomId);
			}
			else if (text == "settlement_owner_changed_capture")
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list4, item.ActorHeroId);
			}
		}
		AppendWeeklyPromptAggregateField(list, "新所有者", ResolveHeroNames(list2));
		AppendWeeklyPromptAggregateField(list, "失去者", ResolveHeroNames(list3));
		AppendWeeklyPromptAggregateField(list, "促成者", ResolveHeroNames(list4));
		AppendWeeklyPromptAggregateField(list, "新所有者王国", ResolveKingdomNames(list5));
		AppendWeeklyPromptAggregateField(list, "失去者王国", ResolveKingdomNames(list6));
		List<string> list7 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptSettlementChangeDetail(x?.ActionStableKey)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		AppendWeeklyPromptAggregateField(list, "方式", list7);
		List<string> list8 = BuildWeeklyPromptSettlementOwnerChangeNatureLabels(list7);
		AppendWeeklyPromptAggregateField(list, "事实约束", list8);
		return string.Join("|", list);
	}

	private List<string> BuildWeeklyPromptSettlementOwnerChangeNatureLabels(List<string> detailLabels)
	{
		List<string> list = new List<string>();
		foreach (string item in detailLabels ?? new List<string>())
		{
			string text = (item ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.IndexOf("交易", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("买卖", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("易物", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("非攻城", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, "这是交易/买卖导致的和平移交，不是攻城夺取");
				continue;
			}
			if (text.IndexOf("围城", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, "这是围城或攻城导致的军事易主");
			}
		}
		return list;
	}

	private string BuildWeeklyPromptAggregateClanChangeLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		bool flag = (materials ?? new List<EventMaterialReference>()).Any((EventMaterialReference x) => string.Equals((x?.ActionKind ?? "").Trim(), "clan_rebellion", StringComparison.OrdinalIgnoreCase) || (x?.SourceActionKinds ?? new List<string>()).Any((string y) => string.Equals((y ?? "").Trim(), "clan_rebellion", StringComparison.OrdinalIgnoreCase)));
		list.Add(flag ? "事件=家族叛乱" : "事件=家族归属变更");
		AppendWeeklyPromptAggregateField(list, "家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.ActorClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "成员", ResolveHeroNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.ActorHeroId, x.HeroId }, delegate(EventMaterialReference x)
		{
			return x.RelatedHeroIds;
		})));
		List<string> list2 = materials.Select((EventMaterialReference x) => WeeklyPromptMaterialOwner.ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.oldKingdomId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> list3 = materials.Select((EventMaterialReference x) => WeeklyPromptMaterialOwner.ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.newKingdomId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> list4 = materials.Select((EventMaterialReference x) => WeeklyPromptMaterialOwner.ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.detailLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		AppendWeeklyPromptAggregateField(list, "原王国", ResolveKingdomNames(list2));
		AppendWeeklyPromptAggregateField(list, "新王国", ResolveKingdomNames(list3));
		AppendWeeklyPromptAggregateField(list, "方式", list4);
		if (flag)
		{
			AppendWeeklyPromptAggregateField(list, "事实约束", new List<string> { "这是NPC家族主动脱离旧王国并发动叛乱，不是普通换阵营" });
		}
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateActionLabel(List<EventMaterialReference> materials)
	{
		List<string> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).Select((EventMaterialReference x) => TranslateNpcActionKindForPrompt(x.ActionKind)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count > 0)
		{
			return string.Join("、", list);
		}
		string text = (materials?.FirstOrDefault()?.ActionKind ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "未分类行动" : text;
	}

	private void AppendWeeklyPromptAggregateField(List<string> fields, string name, List<string> values)
	{
		if (fields == null || values == null)
		{
			return;
		}
		List<string> list = values.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return;
		}
		fields.Add(name + "=" + string.Join("、", list));
	}

	private List<string> BuildWeeklyPromptAggregateHeroAffiliationValues(List<EventMaterialReference> materials)
	{
		List<Tuple<string, string, string>> list = new List<Tuple<string, string, string>>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			if (item == null)
			{
				continue;
			}
			AddWeeklyPromptAggregateAffiliationEntry(list, item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
			AddWeeklyPromptAggregateAffiliationEntry(list, item.TargetHeroId, item.TargetClanId, item.TargetKingdomId);
			AddWeeklyPromptAggregateAffiliationEntry(list, item.HeroId, "", item.KingdomId);
			foreach (string relatedHeroId in item.RelatedHeroIds ?? new List<string>())
			{
				AddWeeklyPromptAggregateAffiliationEntry(list, relatedHeroId, "", "");
			}
		}
		return BuildWeeklyPromptAggregateAffiliationValues(list);
	}

	private List<string> BuildWeeklyPromptAggregateAffiliationValues(IEnumerable<Tuple<string, string, string>> entries)
	{
		List<string> list = new List<string>();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (Tuple<string, string, string> item in entries ?? Enumerable.Empty<Tuple<string, string, string>>())
		{
			string text = (item?.Item1 ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			WeeklyMaterialAggregationOwner.AddUniqueId(list, text);
			WeeklyHeroFact heroFact = _facts.Hero(text);
			string item2 = !string.IsNullOrWhiteSpace(item?.Item2) ? item.Item2 : (heroFact?.ClanId ?? "");
			string item3 = !string.IsNullOrWhiteSpace(item?.Item3) ? item.Item3 : (heroFact?.KingdomId ?? "");
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new List<string>();
				dictionary[text] = value;
			}
			if (!dictionary2.TryGetValue(text, out var value2))
			{
				value2 = new List<string>();
				dictionary2[text] = value2;
			}
			WeeklyMaterialAggregationOwner.AddUniqueId(value, item2);
			WeeklyMaterialAggregationOwner.AddUniqueId(value2, item3);
		}
		return list.Select(delegate(string heroId)
		{
			string text = ResolveHeroDisplay(heroId);
			List<string> list2 = dictionary.ContainsKey(heroId) ? ResolveClanNames(dictionary[heroId]) : new List<string>();
			List<string> list3 = dictionary2.ContainsKey(heroId) ? ResolveKingdomNames(dictionary2[heroId]) : new List<string>();
			List<string> list4 = new List<string>();
			if (list2.Count > 0)
			{
				list4.Add("家族=" + string.Join("、", list2));
			}
			if (list3.Count > 0)
			{
				list4.Add("王国=" + string.Join("、", list3));
			}
			return (list4.Count == 0) ? text : (text + "(" + string.Join(";", list4) + ")");
		}).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
	}

	private void AddWeeklyPromptAggregateAffiliationEntry(List<Tuple<string, string, string>> entries, string heroId, string clanId, string kingdomId)
	{
		string item = (heroId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(item))
		{
			entries?.Add(Tuple.Create(item, (clanId ?? "").Trim(), (kingdomId ?? "").Trim()));
		}
	}

	private List<string> BuildWeeklyPromptAggregateMovementDetailValues(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		List<EventMaterialReference> list2 = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		Dictionary<string, List<EventMaterialReference>> dictionary = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list2)
		{
			if (IsMovementDefendMaterial(item) && !string.IsNullOrWhiteSpace(item.SettlementId))
			{
				string key = ((item.SettlementId ?? "").Trim()) + "|" + ((item.SettlementOwnerClanId ?? "").Trim()) + "|" + ((item.SettlementOwnerKingdomId ?? "").Trim());
				if (!dictionary.TryGetValue(key, out var value))
				{
					value = new List<EventMaterialReference>();
					dictionary[key] = value;
				}
				value.Add(item);
			}
		}
		foreach (List<EventMaterialReference> item2 in dictionary.Values)
		{
			EventMaterialReference eventMaterialReference = item2.FirstOrDefault((EventMaterialReference x) => x != null);
			if (eventMaterialReference == null)
			{
				continue;
			}
			string text = BuildWeeklyPromptAggregateSiegeParticipantsText(item2);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "守军";
			}
			string text2 = BuildWeeklyPromptAggregateSettlementOwnerFactionText(eventMaterialReference);
			string text3 = BuildWeeklyPromptAggregateSettlementNameWithType(eventMaterialReference.SettlementId);
			string text4 = "";
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
			{
				text4 = text + "正在守备" + text2 + "的" + text3 + "。";
			}
			else if (!string.IsNullOrWhiteSpace(text3))
			{
				text4 = text + "正在守备" + text3 + "。";
			}
			else
			{
				text4 = text + "正在守备当地要地。";
			}
			if (!list.Contains(text4))
			{
				list.Add(text4);
			}
		}
		HashSet<EventMaterialReference> hashSet = new HashSet<EventMaterialReference>();
		Dictionary<string, List<EventMaterialReference>> dictionary2 = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item3 in list2)
		{
			if (IsMovementArmySiegeMaterial(item3))
			{
				string text5 = BuildWeeklyPromptAggregateMovementArmySiegeGroupKey(item3);
				if (!dictionary2.TryGetValue(text5, out var value2))
				{
					value2 = new List<EventMaterialReference>();
					dictionary2[text5] = value2;
				}
				value2.Add(item3);
			}
		}
		foreach (List<EventMaterialReference> item4 in dictionary2.Values)
		{
			string text6 = BuildWeeklyPromptAggregateMovementArmySiegeLine(item4);
			if (!string.IsNullOrWhiteSpace(text6) && !list.Contains(text6))
			{
				list.Add(text6);
				foreach (EventMaterialReference item5 in item4)
				{
					hashSet.Add(item5);
				}
			}
		}
		Dictionary<string, List<EventMaterialReference>> dictionary3 = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item6 in list2)
		{
			if (item6 != null && !hashSet.Contains(item6) && IsMovementRaidMaterial(item6))
			{
				string text7 = BuildWeeklyPromptAggregateMovementRaidGroupKey(item6);
				if (!dictionary3.TryGetValue(text7, out var value3))
				{
					value3 = new List<EventMaterialReference>();
					dictionary3[text7] = value3;
				}
				value3.Add(item6);
			}
		}
		foreach (List<EventMaterialReference> item7 in dictionary3.Values)
		{
			EventMaterialReference eventMaterialReference2 = item7.FirstOrDefault((EventMaterialReference x) => x != null);
			if (eventMaterialReference2 == null)
			{
				continue;
			}
			string text8 = BuildWeeklyPromptAggregateSiegeParticipantsText(item7).Replace("所属的", "的");
			if (string.IsNullOrWhiteSpace(text8))
			{
				text8 = BuildWeeklyPromptAggregateActorAffiliationText(eventMaterialReference2);
			}
			if (string.IsNullOrWhiteSpace(text8))
			{
				text8 = "某势力";
			}
			string text9 = BuildWeeklyPromptAggregateRaidTargetText(eventMaterialReference2);
			string text10 = text8 + "最近在袭扰" + (string.IsNullOrWhiteSpace(text9) ? "某地" : text9) + "。";
			if (!list.Contains(text10))
			{
				list.Add(text10);
			}
			foreach (EventMaterialReference item8 in item7)
			{
				hashSet.Add(item8);
			}
		}
		foreach (EventMaterialReference item9 in list2)
		{
			if (item9 == null)
			{
				continue;
			}
			if (hashSet.Contains(item9))
			{
				continue;
			}
			if (IsMovementDefendMaterial(item9) && !string.IsNullOrWhiteSpace(item9.SettlementId))
			{
				continue;
			}
			string text11 = (item9.SnapshotText ?? item9.Label ?? "").Trim();
			if (IsMovementRaidMaterial(item9))
			{
				string text12 = BuildWeeklyPromptAggregateActorAffiliationText(item9);
				string text13 = BuildWeeklyPromptAggregateRaidTargetText(item9);
				string text14 = string.IsNullOrWhiteSpace(text12) ? "某势力" : text12;
				string text15 = text14 + "最近在袭扰" + (string.IsNullOrWhiteSpace(text13) ? "某地" : text13) + "。";
				if (!list.Contains(text15))
				{
					list.Add(text15);
				}
				continue;
			}
			string text16 = BuildWeeklyPromptAggregateActorAffiliationText(item9);
			string text17 = BuildWeeklyPromptAggregateMovementActionText(item9, text11);
			if (string.IsNullOrWhiteSpace(text17))
			{
				continue;
			}
			if (IsMovementArmyLeaderMaterial(item9))
			{
				string text18 = string.IsNullOrWhiteSpace(text16) ? text17 : (text16.Replace("所属的", "的") + text17);
				if (!list.Contains(text18))
				{
					list.Add(text18);
				}
				continue;
			}
			string text19 = string.IsNullOrWhiteSpace(text16) ? text17 : (text16 + "：" + text17);
			if (!list.Contains(text19))
			{
				list.Add(text19);
			}
		}
		return list;
	}

	private bool IsMovementDefendMaterial(EventMaterialReference item)
	{
		string text = (item?.ActionStableKey ?? "").Trim();
		if (text.IndexOf("DefendSettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (item?.SnapshotText ?? item?.Label ?? "").Trim();
		return text2.IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool IsMovementRaidMaterial(EventMaterialReference item)
	{
		if (item == null || string.IsNullOrWhiteSpace(item.SettlementId))
		{
			return false;
		}
		string text = (item.ActionStableKey ?? "").Trim();
		if (text.IndexOf("RaidSettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (!((item.ActionKind ?? "").Trim().Equals("daily_behavior", StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		string text2 = (item.SnapshotText ?? item.Label ?? "").Trim();
		return text2.IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("劫掠", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("掠夺", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("掠", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private string BuildWeeklyPromptAggregateMovementRaidGroupKey(EventMaterialReference item)
	{
		string text = BuildWeeklyPromptAggregateRaidTargetText(item);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (item?.SettlementId ?? "").Trim().ToLowerInvariant();
		}
		return ((item?.ActorKingdomId ?? "").Trim().ToLowerInvariant()) + "|" + ((item?.ActorClanId ?? "").Trim().ToLowerInvariant()) + "|" + text;
	}

	private bool IsMovementArmySiegeMaterial(EventMaterialReference item)
	{
		if (item == null)
		{
			return false;
		}
		string text = ((item.ActionKind ?? "").Trim().ToLowerInvariant());
		if (text != "daily_behavior")
		{
			return false;
		}
		string text2 = (item.ActionStableKey ?? "").Trim();
		string text3 = (item.SnapshotText ?? item.Label ?? "").Trim();
		bool flag = text2.IndexOf("daily_behavior:army:", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("军团", StringComparison.OrdinalIgnoreCase) >= 0;
		if (!flag)
		{
			return false;
		}
		if (text2.IndexOf("BesiegeSettlement", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("AssaultSettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return text3.IndexOf("围攻", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("强攻", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private string BuildWeeklyPromptAggregateMovementArmySiegeGroupKey(EventMaterialReference item)
	{
		string text = WeeklyMaterialAggregationOwner.NormalizeWeeklyPromptAggregateStableKey(item?.ActionStableKey);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = ResolveMovementArmySiegeActionVerb(item, (item?.SnapshotText ?? item?.Label ?? "").Trim());
		return ((item?.SettlementId ?? "").Trim().ToLowerInvariant()) + "|" + ((item?.SettlementOwnerClanId ?? "").Trim().ToLowerInvariant()) + "|" + ((item?.SettlementOwnerKingdomId ?? "").Trim().ToLowerInvariant()) + "|" + (text2 ?? "").Trim().ToLowerInvariant();
	}

	private string BuildWeeklyPromptAggregateMovementArmySiegeLine(List<EventMaterialReference> materials)
	{
		List<EventMaterialReference> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return "";
		}
		EventMaterialReference eventMaterialReference = list.FirstOrDefault(IsMovementArmyLeaderMaterial);
		bool flag = eventMaterialReference != null;
		if (eventMaterialReference == null)
		{
			eventMaterialReference = list[0];
		}
		string text = (eventMaterialReference.SnapshotText ?? eventMaterialReference.Label ?? "").Trim();
		string text2 = BuildWeeklyPromptAggregateMovementActionText(eventMaterialReference, text);
		string text3 = ResolveMovementArmySiegeActionVerb(eventMaterialReference, text2);
		if (string.IsNullOrWhiteSpace(text3))
		{
			return "";
		}
		string text4 = ExtractMovementArmyNameFromActionText(text2);
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = "军团";
		}
		string text5 = BuildWeeklyPromptAggregateMovementArmyTargetText(eventMaterialReference);
		if (string.IsNullOrWhiteSpace(text5))
		{
			text5 = "目标据点";
		}
		if (flag)
		{
			string text6 = BuildWeeklyPromptAggregateActorAffiliationText(eventMaterialReference);
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "某领主";
			}
			string text7 = text6 + "最近正率领" + text4 + text3 + text5 + "。";
			List<EventMaterialReference> list2 = list.Where((EventMaterialReference x) => x != eventMaterialReference).ToList();
			string text8 = BuildWeeklyPromptAggregateSiegeParticipantsText(list2);
			if (!string.IsNullOrWhiteSpace(text8))
			{
				text7 = TrimTrailingSentencePunctuation(text7) + "，军团的成员有" + text8 + "。";
			}
			return text7;
		}
		string text9 = BuildWeeklyPromptAggregateSiegeParticipantsText(list);
		if (string.IsNullOrWhiteSpace(text9))
		{
			text9 = "相关部队";
		}
		return text9 + "最近正随" + text4 + text3 + text5 + "。";
	}

	private bool IsMovementArmyLeaderMaterial(EventMaterialReference item)
	{
		string text = BuildWeeklyPromptAggregateMovementActionText(item, (item?.SnapshotText ?? item?.Label ?? "").Trim());
		return text.IndexOf("率领", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private string ResolveMovementArmySiegeActionVerb(EventMaterialReference item, string actionText)
	{
		string text = (item?.ActionStableKey ?? "").Trim();
		if (text.IndexOf("BesiegeSettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "围攻";
		}
		if (text.IndexOf("AssaultSettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "强攻";
		}
		string text2 = (actionText ?? "").Trim();
		if (text2.IndexOf("围攻", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "围攻";
		}
		if (text2.IndexOf("强攻", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "强攻";
		}
		return "";
	}

	private string ExtractMovementArmyNameFromActionText(string actionText)
	{
		string text = (actionText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf("率领", StringComparison.OrdinalIgnoreCase);
		int num2 = 2;
		if (num < 0)
		{
			num = text.IndexOf("随", StringComparison.OrdinalIgnoreCase);
			num2 = 1;
		}
		if (num < 0)
		{
			return "";
		}
		int num3 = num + num2;
		if (num3 >= text.Length)
		{
			return "";
		}
		int num4 = int.MaxValue;
		foreach (string item in new string[5] { "围攻", "强攻", "守备", "前往", "在" })
		{
			int num5 = text.IndexOf(item, num3, StringComparison.OrdinalIgnoreCase);
			if (num5 > num3 && num5 < num4)
			{
				num4 = num5;
			}
		}
		if (num4 == int.MaxValue || num4 <= num3)
		{
			return "";
		}
		return text.Substring(num3, num4 - num3).Trim().Trim('，', '。', '、', ',', ' ');
	}

	private string BuildWeeklyPromptAggregateMovementArmyTargetText(EventMaterialReference item)
	{
		string text = BuildWeeklyPromptAggregateSettlementOwnerFactionText(item).Replace("所属的", "的");
		string text2 = ResolveSettlementDisplay(item?.SettlementId);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return text + "的" + text2;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text + "的据点";
		}
		return "";
	}

	private string TrimTrailingSentencePunctuation(string text)
	{
		string text2 = (text ?? "").Trim();
		while (text2.EndsWith("。", StringComparison.Ordinal) || text2.EndsWith("！", StringComparison.Ordinal) || text2.EndsWith("？", StringComparison.Ordinal))
		{
			text2 = text2.Substring(0, text2.Length - 1).TrimEnd();
		}
		return text2;
	}

	private string BuildWeeklyPromptAggregateActorAffiliationText(EventMaterialReference item)
	{
		string text = BuildWeeklyPromptAggregateSingleAffiliationValue(item?.ActorHeroId, item?.ActorClanId, item?.ActorKingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = ResolveHeroDisplay(!string.IsNullOrWhiteSpace(item?.ActorHeroId) ? item.ActorHeroId : (!string.IsNullOrWhiteSpace(item?.HeroId) ? item.HeroId : item?.TargetHeroId));
		return (text2 ?? "").Trim();
	}

	private string BuildWeeklyPromptAggregateMovementActionText(EventMaterialReference item, string rawText)
	{
		string text = (rawText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf("；其所属家族是", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			text = text.Substring(0, num).Trim();
		}
		string text2 = ResolveHeroDisplay(!string.IsNullOrWhiteSpace(item?.ActorHeroId) ? item.ActorHeroId : (!string.IsNullOrWhiteSpace(item?.HeroId) ? item.HeroId : item?.TargetHeroId));
		if (!string.IsNullOrWhiteSpace(text2))
		{
			if (text.StartsWith(text2 + "：", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring((text2 + "：").Length).Trim();
			}
			else if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(text2.Length).TrimStart('：', ':', '，', ',', ' ');
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			string text3 = ResolveSettlementDisplay(item?.SettlementId);
			text = (!string.IsNullOrWhiteSpace(text3)) ? ("在" + text3 + "一带行动。") : "在前线行动。";
		}
		if (!(text.EndsWith("。") || text.EndsWith("！") || text.EndsWith("？")))
		{
			text += "。";
		}
		return text;
	}

	private string BuildWeeklyPromptAggregateSettlementOwnerText(EventMaterialReference item)
	{
		string text = ResolveHeroDisplay(item?.SettlementOwnerHeroId);
		string text2 = ResolveClanDisplay(item?.SettlementOwnerClanId);
		string text3 = ResolveKingdomDisplay(item?.SettlementOwnerKingdomId);
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("家族=" + text2);
		}
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add("王国=" + text3);
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return (list.Count == 0) ? text : (text + "(" + string.Join(";", list) + ")");
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return string.IsNullOrWhiteSpace(text3) ? text2 : (text2 + "(王国=" + text3 + ")");
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateRaidTargetText(EventMaterialReference item)
	{
		string text = BuildWeeklyPromptAggregateHumanAffiliationText(item?.SettlementOwnerHeroId, item?.SettlementOwnerClanId, item?.SettlementOwnerKingdomId);
		string text2 = BuildWeeklyPromptAggregateSettlementNameWithType(item?.SettlementId);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return text + "的" + text2;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text + "的领地";
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateSettlementOwnerFactionText(EventMaterialReference item)
	{
		string text = ResolveClanDisplay(item?.SettlementOwnerClanId);
		string text2 = ResolveKingdomDisplay(item?.SettlementOwnerKingdomId);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			return text2 + "所属的" + text + "家族";
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text + "家族";
		}
		return text2;
	}

	private string BuildWeeklyPromptAggregateSettlementNameWithType(string settlementId)
	{
		string id = (settlementId ?? "").Trim();
		return string.IsNullOrWhiteSpace(id) ? "" : _facts.SettlementNameWithType(id);
	}

	private string BuildWeeklyPromptAggregateBattleLocationText(EventMaterialReference item)
	{
		string text = (item?.LocationText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = BuildWeeklyPromptAggregateSettlementNameWithType(item?.SettlementId);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateBattleOutcomeText(EventMaterialReference item)
	{
		if (item?.Won.HasValue == true)
		{
			return item.Won.Value ? "取得了胜利" : "遭遇了失利";
		}
		string text = (item?.SnapshotText ?? item?.Label ?? "").Trim();
		if (text.IndexOf("获胜", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("击败", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("战胜", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "取得了胜利";
		}
		if (text.IndexOf("失利", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("战败", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("败给", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "遭遇了失利";
		}
		return "";
	}

	private string BuildWeeklyPromptAggregateSingleAffiliationValue(string heroId, string clanId, string kingdomId)
	{
		return BuildWeeklyPromptAggregateAffiliationValues(new List<Tuple<string, string, string>> { Tuple.Create((heroId ?? "").Trim(), (clanId ?? "").Trim(), (kingdomId ?? "").Trim()) }).FirstOrDefault() ?? "";
	}

	private List<string> BuildWeeklyPromptAggregateDetailValues(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = (item?.SnapshotText ?? item?.Label ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private List<string> CollectMaterialIds(List<EventMaterialReference> materials, Func<EventMaterialReference, IEnumerable<string>> singleSelector, Func<EventMaterialReference, IEnumerable<string>> listSelector)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			if (item == null)
			{
				continue;
			}
			if (singleSelector != null)
			{
				foreach (string item2 in singleSelector(item) ?? Enumerable.Empty<string>())
				{
					WeeklyMaterialAggregationOwner.AddUniqueId(list, item2);
				}
			}
			if (listSelector == null)
			{
				continue;
			}
			foreach (string item3 in listSelector(item) ?? Enumerable.Empty<string>())
			{
				WeeklyMaterialAggregationOwner.AddUniqueId(list, item3);
			}
		}
		return list;
	}

	private List<string> ResolveHeroNames(IEnumerable<string> heroIds) => ResolveNames(heroIds, ResolveHeroDisplay);

	private List<string> ResolveClanNames(IEnumerable<string> clanIds) => ResolveNames(clanIds, ResolveClanDisplay);

	private List<string> ResolveKingdomNames(IEnumerable<string> kingdomIds) => ResolveNames(kingdomIds, ResolveKingdomDisplay);

	private List<string> ResolveSettlementNames(IEnumerable<string> settlementIds)
	{
		return (settlementIds ?? Enumerable.Empty<string>()).Select(ResolveSettlementDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}





	internal static string TranslateNpcActionKindForPrompt(string actionKind)
	{
		switch ((actionKind ?? "").Trim().ToLowerInvariant())
		{
		case "army_create":
			return "组建军团的行动";
		case "army_gather":
			return "军团集结行动";
		case "army_disperse":
			return "军团解散行动";
		case "army_join":
			return "加入军团的行动";
		case "army_leave":
			return "离开军团的行动";
		case "siege_start_attack":
			return "参与围攻的行动";
		case "siege_start_defend":
			return "参与守城的行动";
		case "siege_end_attack":
			return "围城结束后的攻方行动";
		case "siege_end_defend":
			return "围城结束后的守方行动";
		case "siege_join":
			return "加入围城的行动";
		case "siege_leave":
			return "离开围城的行动";
		case "siege_complete":
			return "围城结果事件";
		case "daily_behavior":
			return "近期行军动向";
		case "map_event":
			return "战场交锋";
		case "map_event_aftermath":
			return "战后余波";
		case "tournament_finished":
			return "竞技大会胜出事件";
		case "duel_result":
			return "正式决斗结果";
		case "marriage":
			return "联姻事件";
		case "clan_changed_kingdom":
			return "家族更换效忠对象的事件";
		case "clan_defected":
			return "家族叛逃事件";
		case "clan_rebellion":
			return "家族叛乱事件";
		case "kingdom_decision_concluded":
			return "王国决议事件";
		case "bilateral_diplomacy_pending":
			return "双边外交复议进展";
		case "bilateral_diplomacy_outcome":
			return "双边外交最终结果";
		case "ruling_clan_changed":
			return "执政家族变更事件";
		case "settlement_owner_changed_gain":
			return "定居点归属增加事件";
		case "settlement_owner_changed_loss":
			return "定居点归属失去事件";
		case "settlement_owner_changed_capture":
			return "定居点易主事件";
		case "hero_killed":
			return "英雄死亡事件";
		case "hero_executed_victim":
			return "被处决事件";
		case "clan_member_killed":
			return "家族成员死亡事件";
		case "prisoner_taken_captor":
			return "俘获领主事件";
		case "prisoner_taken_prisoner":
			return "被俘事件";
		case "prisoner_released_captor":
			return "囚犯获释事件";
		case "prisoner_released_prisoner":
			return "结束囚禁事件";
		case "birth":
			return "家族新生事件";
		case "clan_leader_changed":
			return "家族族长更替事件";
		default:
			return "";
		}
	}

	private static string ParseWeeklyPromptSettlementChangeDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 5)
		{
			return "";
		}
		switch ((array[array.Length - 1] ?? "").Trim())
		{
		case "BySiege":
			return "围城";
		case "ByBarter":
			return "交易/买卖移交（非攻城）";
		case "ByLeaveFaction":
			return "脱离王国";
		case "ByKingDecision":
			return "王国决议";
		case "ByGift":
			return "赠与";
		case "ByRebellion":
			return "叛乱";
		case "ByClanDestruction":
			return "家族覆灭";
		default:
			return (array[array.Length - 1] ?? "").Trim();
		}
	}

	private static string ParseWeeklyPromptReleaseDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 4)
		{
			return "";
		}
		int num = array.Length - 1;
		while (num >= 0)
		{
			string text = (array[num] ?? "").Trim();
			if (!string.Equals(text, "prisoner", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "captor", StringComparison.OrdinalIgnoreCase))
			{
				break;
			}
			num--;
		}
		if (num < 3)
		{
			return "";
		}
		switch ((array[num] ?? "").Trim())
		{
		case "Ransom":
			return "通过赎金获释";
		case "ReleasedByChoice":
			return "被主动释放";
		case "ReleasedAfterPeace":
			return "因议和获释";
		case "ReleasedAfterEscape":
			return "成功逃脱";
		case "ReleasedAfterBattle":
			return "战后获释";
		case "ReleasedByCompensation":
			return "补偿后获释";
		case "Death":
			return "囚禁中死亡";
		default:
			return (array[num] ?? "").Trim();
		}
	}
}
