using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

internal static class Program
{
	private static int _failures;

	private static void Check(bool condition, string name)
	{
		Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
		if (!condition)
		{
			_failures++;
		}
	}

	private static WorldBulletinEvent Ev(string key, string kind, double hour, int score, string sentence, string group, bool player, params string[] kingdoms)
	{
		return new WorldBulletinEvent { Key = key, Kind = kind, Hour = hour, Day = (int)(hour / 24), Score = score, Sentence = sentence, Group = group, InvolvesPlayer = player, KingdomIds = kingdoms.ToList() };
	}

	private static int Main()
	{
		var focus = new WorldBulletinFocus { PlayerKingdomId = "vlandia", NearbyKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "vlandia", "battania", "sturgia" } };
		var events = new List<WorldBulletinEvent>
		{
			Ev("killed:a", "lord_killed", 100, 50, "甲被玩家处决。", "execution:main_hero:4", true, "empire_s", "vlandia"),
			Ev("killed:b", "lord_killed", 105, 50, "乙被玩家处决。", "execution:main_hero:4", true, "empire_s", "vlandia"),
			Ev("raid:1", "raid", 101, 25, "村一遭劫掠。", "raid:4:battania>vlandia", false, "vlandia", "battania"),
			Ev("raid:2", "raid", 102, 25, "村二遭劫掠。", "raid:4:battania>vlandia", false, "vlandia", "battania"),
			Ev("raid:3", "raid", 103, 25, "村三遭劫掠。", "raid:4:battania>vlandia", false, "vlandia", "battania"),
			Ev("fief", "fief_grant", 104, 20, "某城授予某人。", "fief_grant:vlandia:4", false, "vlandia"),
			Ev("war:x", "war_declared", 106, 70, "南帝国向北帝国宣战。", "diplomacy:empire_n|empire_s", false, "empire_s", "empire_n"),
			Ev("raid:far", "raid", 107, 25, "远方村遭劫掠。", "raid:4:aserai>khuzait", false, "aserai", "khuzait"),
			Ev("cap:near", "lord_captured", 108, 35, "斯特吉亚某领主被俘。", "clash:4:sturgia|nord", false, "sturgia", "nord")
		};

		// Scoring leans toward the player: own deeds +30, home +20, neighbours +10.
		Check(WorldBulletinPolicy.FocusScore(events[0], focus) == 100, "player execution in home kingdom = 50+30+20");
		Check(WorldBulletinPolicy.FocusScore(events[8], focus) == 45, "neighbour capture = 35+10");
		Check(WorldBulletinPolicy.FocusScore(events[6], focus) == 70, "far war keeps base score");

		// One bulletin: both executions merge; the foreign war joins as a headline ("与此同时").
		WorldBulletinSelection s = WorldBulletinPolicy.Select(events, new WorldBulletinScopeState { WindowEndHour = 130 }, focus, 130);
		Check(s != null && s.Major.Key == "killed:a", "lead is the player's earlier execution");
		Check(s.MajorFacts.Select(e => e.Key).Contains("killed:b"), "second execution merged into major");
		Check(s.MajorFacts.Select(e => e.Key).Contains("war:x"), "world headline merged into the one bulletin");
		Check(s.Minors.Any(m => m.Events.Count == WorldBulletinPolicy.MaxMinorGroupSentences && !m.Sentence.Contains("另有")), "home raids collapse only explicitly reported facts into one minor");
		Check(!s.Minors.SelectMany(m => m.Events).Any(e => e.Key == "fief"), "score-20 fief grant dropped");
		Check(!s.Minors.SelectMany(m => m.Events).Any(e => e.Key == "raid:far"), "far-away raid below world bar dropped");
		var enoughNews = WorldBulletinPolicy.Select(events.Concat(new[] { Ev("cap:home", "lord_captured", 109, 35, "本国领主被俘。", "clash:4:vlandia|aserai", false, "vlandia", "aserai") }).ToList(), new WorldBulletinScopeState { WindowEndHour = 130 }, focus, 130);
		Check(WorldBulletinPolicy.HasEnoughMinorNews(enoughNews), "two distinct minor groups allow generation");
		Check(!WorldBulletinPolicy.HasEnoughMinorNews(s), "below-threshold foreign capture does not pad minor count");
		Check(!WorldBulletinPolicy.HasEnoughMinorNews(new WorldBulletinSelection { Major = s.Major, Minors = new() { s.Minors[0] } }), "three raids in one group still count as one minor");
		Check(!WorldBulletinPolicy.HasEnoughMinorNews(new WorldBulletinSelection { Major = s.Major }), "zero minors wait without generating");
		s.Major.Participants.Add(new WorldBulletinParticipant { HeroId = "a", Name = "甲", Role = "死者" });
		s.Major.Participants.Add(new WorldBulletinParticipant { HeroId = "player", Name = "玩家", Role = "行刑方" });
		s.WindowFacts.Single(f => f.Key == events[1].Key).Participants.Add(new WorldBulletinParticipant { HeroId = "b", Name = "乙", Role = "死者" });
		s.WindowFacts.Single(f => f.Key == events[6].Key).Participants.Add(new WorldBulletinParticipant { HeroId = "unrelated", Name = "外部君主", Role = "另一战事君主" });
		var art = WorldBulletinPolicy.BuildIllustrationPlan(s, "selection:1", "1084年夏季8日");
		Check(art.Participants.Select(p => p.HeroId).SequenceEqual(new[] { "a", "player", "b" }), "art preserves all core participants but excludes unrelated headline");
		Check(art.Facts.Contains("甲被玩家处决") && art.Facts.Contains("乙被玩家处决") && !art.Facts.Contains("宣战") && !art.Facts.Contains("村一"), "art freezes only same-story facts");
		s.Major.Participants[0].Role = "已改写";
		Check(art.Participants[0].Role == "死者", "plan clones event-time role independently of source mutations");
		var filledMinors = WorldBulletinPolicy.MergeMinors(new List<string>(), enoughNews.Minors, out int filledCount);
		Check(filledMinors.Count >= 2 && filledMinors.All(m => !string.IsNullOrWhiteSpace(m)) && filledCount == 0, "missing generated minors filled with real selected facts");

		// Bonuses alone never make a headline: a home fief grant (20+20) must not trigger.
		Check(!WorldBulletinPolicy.IsTrigger(events[5], focus), "home fief grant is not a trigger");
		Check(WorldBulletinPolicy.IsTrigger(events[0], focus), "player execution is a trigger");

		// NPC regional layer: local, per kingdom, strongest first, one line per story group.
		List<WorldBulletinEvent> vlandia = WorldBulletinPolicy.RecentKingdomFacts(events, "vlandia", 5, 3);
		Check(vlandia.Count == 3 && vlandia[0].Key == "killed:b" && vlandia[2].Key == "fief", "kingdom facts: score first, newest breaks ties");
		Check(vlandia.Count(e => e.Group.StartsWith("execution")) == 1 && vlandia.Count(e => e.Group.StartsWith("raid")) == 1, "one fact per story group");
		string brief = WorldBulletinPolicy.BuildNpcBriefBlock(new List<KeyValuePair<string, string>> { new("vlandia", "瓦兰迪亚"), new("aserai", "阿塞莱"), new("empire_w", "西帝国") }, events, 5);
		Check(brief.StartsWith("【近期三个王国发生的事】") && brief.Contains("瓦兰迪亚：") && brief.Contains("阿塞莱：") && !brief.Contains("西帝国"), "brief block keeps legacy header, skips kingdoms with no news");
		Check(brief.Contains("昨日") || brief.Contains("日前") || brief.Contains("今日"), "brief lines carry relative dates");

		events[0].Detail = "死者身份：南帝国某家族族长";
		string detail = WorldBulletinPolicy.BuildNpcDetailBlock(WorldBulletinPolicy.NpcKingdomHeader, "瓦兰迪亚", "vlandia", events, 5);
		Check(detail.Contains("（死者身份：南帝国某家族族长）"), "detail block carries captured details");
		Check(detail.Contains("甲被玩家处决") && detail.Contains("乙被玩家处决") && detail.Contains("村三遭劫掠"), "detail block lists every fact of a story");
		// Scene prompts end the rule section only on the legacy "完整周报" headers.
		Check(detail.StartsWith("【NPC所属王国完整周报】\n标题：瓦兰迪亚"), "detail block keeps the legacy header the scene splitter recognises");
		string world = WorldBulletinPolicy.BuildNpcWorldBlock(events, 5, "血染刑场", "玩家连斩两人", 4);
		Check(world.StartsWith("【世界完整周报】") && world.Contains("南帝国向北帝国宣战") && !world.Contains("远方村") && world.Contains("最新快报《血染刑场》"), "world block: legacy header, headlines only + latest bulletin");
		string worldForSouth = WorldBulletinPolicy.BuildNpcWorldBlock(events, 5, "", "", -1, "empire_s");
		Check(!worldForSouth.Contains("南帝国向北帝国宣战"), "world block skips facts already in the NPC kingdom block");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(null, 100, "旧停战", "甲乙停战", 10) == "", "90-day-old bulletin excluded");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(null, 12, "边界", "消息", 5).Contains("边界"), "seven-day boundary included");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(null, 13, "边界", "消息", 5) == "", "eight-day-old bulletin excluded");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(null, 5, "未知", "消息", -1) == "", "unknown publication day excluded");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(null, 5, "未来", "消息", 6) == "", "future publication day excluded");
		Check(WorldBulletinPolicy.BuildNpcWorldBlock(events, 5, "旧停战", "甲乙停战", -1).Contains("南帝国向北帝国宣战"), "old bulletin filtering preserves recent raw facts");

		// A non-headline that outranks the real trigger by bonus (player raid 25+30+20=75 vs far war 70)
		// must not sink the window: the lead is the strongest trigger.
		var raidOutranks = new List<WorldBulletinEvent>
		{
			Ev("war:far", "war_declared", 200, 70, "阿塞莱向库塞特宣战。", "diplomacy:aserai|khuzait", false, "aserai", "khuzait"),
			Ev("raid:player", "raid", 201, 25, "玩家劫掠某村。", "raid:8:vlandia>battania", true, "battania", "vlandia")
		};
		WorldBulletinSelection r = WorldBulletinPolicy.Select(raidOutranks, new WorldBulletinScopeState { WindowEndHour = 230 }, focus, 230);
		Check(r != null && r.Major.Key == "war:far", "outranking non-trigger does not drop the bulletin");
		Check(r.Minors.Any(m => m.Events.Any(e => e.Key == "raid:player")), "the player's raid still appears as a minor");

		// The lead's whole story (its captives) comes before unrelated headlines when slots run out.
		var crowded = new List<WorldBulletinEvent> { Ev("siege", "settlement_siege", 300, 65, "某城陷落。", "siege:town_x", false, "vlandia", "battania") };
		for (int i = 0; i < 5; i++)
		{
			crowded.Add(Ev("far" + i, "war_declared", 301 + i, 70 - i, "远方第" + i + "场宣战。", "diplomacy:far" + i, false, "khuzait", "aserai" + i));
		}
		crowded.Add(Ev("cap", "lord_captured", 306, 35, "守将被俘。", "siege:town_x", false, "vlandia", "battania"));
		WorldBulletinSelection c = WorldBulletinPolicy.Select(crowded, new WorldBulletinScopeState { WindowEndHour = 330 }, focus, 330);
		Check(c.Major.Key == "siege" && c.MajorFacts.Count == 5 && c.MajorFacts.Any(e => e.Key == "cap"), "lead story's supporting facts precede unrelated headlines");
		Check(WorldBulletinPolicy.BuildNpcBriefBlock(new List<KeyValuePair<string, string>>(), events, 5) == "", "empty kingdom list yields empty block");
		Check(WorldBulletinPolicy.RecentKingdomFacts(events, "vlandia", 20, 3).Count == 0, "facts older than 7 days are not NPC knowledge");

		// Parsing: indexed minors, tags crammed on one line, out-of-order slots.
		WorldBulletinText t = WorldBulletinPolicy.ParseResponse("[TITLE] 血染刑场\n[MAJOR] 第一段。\n第二段。\n[M2] 第二条润色 [M1] 第一条润色\n[SHORT] 摘要", 3);
		Check(t != null && t.Major == "第一段。\n第二段。", "multi-paragraph major kept");
		Check(t.Minors.Count == 3 && t.Minors[0] == "第一条润色" && t.Minors[1] == "第二条润色" && t.Minors[2] == "", "indexed minors land in slots");
		var selected = new List<WorldBulletinMinor> { new() { Sentence = "模板一" }, new() { Sentence = "模板二" }, new() { Sentence = "模板三" } };
		List<string> merged = WorldBulletinPolicy.MergeMinors(t.Minors, selected, out int polished);
		Check(polished == 2 && merged[2] == "模板三", "missing minor falls back to template");
		WorldBulletinText legacy = WorldBulletinPolicy.ParseResponse("[MAJOR] 正文\n[MINOR] 甲\n[MINOR] 乙", 2);
		Check(legacy.Minors[0] == "甲" && legacy.Minors[1] == "乙", "unindexed minors fill in order");

		string user = WorldBulletinPolicy.BuildUserPrompt("快报视角：天下大事", "某日", s, new[] { "瓦兰迪亚：君主某某" });
		Check(user.Contains("1. 甲被玩家处决") && user.Contains("M1. ") && user.Contains("【相关王国现状】"), "user prompt lists facts, context and M1");
		string sys = WorldBulletinPolicy.BuildSystemPrompt(s.MajorFacts.Count, s.Minors.Count);
        Check(sys.Contains("合写成同一篇") && sys.Contains("320到480字") && sys.Contains("[M1]"), "existing length, merged-story and format contract retained");
        Check(sys.Contains("几人对几人") && sys.Contains("不要连续罗列"), "default writing requirements discourage numeric battle recitals");
        var custom = WorldBulletinPolicy.BuildSystemPrompt(1, 2, "测试独立写作风格");
        Check(custom.Contains("测试独立写作风格") && !custom.Contains(WorldBulletinPolicy.DefaultWritingRequirements) && custom.Contains("260到400字"), "custom writing replaces only editable style, retaining existing length");
        var empty = WorldBulletinPolicy.BuildSystemPrompt(1, 2, "");
        Check(!empty.Contains("【快报写作要求】") && empty.Contains("[MAJOR]") && empty.Contains("不得添加"), "explicit empty style keeps factual and output contracts");
		string sallySentence = WorldBulletinCampaignMaterialPolicy.BattleSentence("某城", true, true, "甲军", "瓦兰迪亚", "乙军", "南帝国", 120);
		string sallyDetail = WorldBulletinCampaignMaterialPolicy.BattleDetail(80, "伤亡10人", 40, "伤亡30人", true, true, "瓦兰迪亚伯爵", true, "南帝国伯爵");
		Check(sallySentence.Contains("出城战") && sallySentence.Contains("本次参战部队"), "sally-out material labels only the engaged troops");
        Check(sallySentence.Contains("120") && sallyDetail.Contains("80") && sallyDetail.Contains("40"), "original battle fact template and numeric details retained");
		Check(sallyDetail.Contains("仅记录本场交战") && sallyDetail.Contains("不代表围城军或守军整支军团覆灭"), "sally-out detail preserves the force boundary");
		Check(WorldBulletinPolicy.TitleForKind("sally_out_battle") == "出城战报" && sys.Contains("击败全军"), "sally-out title and anti-exaggeration prompt are explicit");
		WorldBulletinText template = WorldBulletinPolicy.BuildTemplate(s);
		Check(template.Major.Contains("甲被玩家处决") && template.Major.Contains("乙被玩家处决"), "template major keeps all merged facts");

		Console.WriteLine(_failures == 0 ? "ALL PASS" : _failures + " FAILED");
		return _failures == 0 ? 0 : 1;
	}
}
