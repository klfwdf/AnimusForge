using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Illustrator.Context;
using TaleWorlds.CampaignSystem;

// Linked production extractor and policy, with fake game and native appearance services. No GPU/network.
static class Program
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static void Main()
    {
        var player = new Hero { StringId = "player", Name = "加尼密诺斯" };
        var king = new Hero { StringId = "king", Name = "德泰尔", IsFactionLeader = true };
        var other = new Hero { StringId = "other", Name = "卢孔" };
        Hero.AllAliveHeroes.AddRange(new[] { player, king, other });
        var plan = new WorldBulletinIllustrationPlan { Title = "政变", DateText = "1084年夏季9日", Facts = "加尼密诺斯在加伦发动政变，制伏德泰尔。双方无人阵亡。" };
        plan.Participants.Add(new() { HeroId = "player", Name = "加尼密诺斯", Role = "政变发动者" });
        plan.Participants.Add(new() { HeroId = "king", Name = "德泰尔", Role = "原国王" });
        var context = WeeklyReportContextExtractor.ExtractFromPlan(plan);
        Check(context.Characters.Select(p => p.HeroId).SequenceEqual(new[] { "player", "king" }), "both sides resolved by ID in event order");
        Check(context.Characters.All(p => p.Profile?.Appearance != null), "each person gets own frozen appearance");
        Check(!ReferenceEquals(context.Characters[0].Profile.Appearance, context.Characters[1].Profile.Appearance), "appearance objects are not shared");
        string facts = context.BuildHardFacts();
        Check(facts.Contains("角色：原国王") && facts.Contains("双方无人阵亡") && facts.Contains("已选定事件"), "historical role, outcome and frozen event preserved");
        Check(context.Snapshot.ReportDateLabel == plan.DateText, "plan date overrides current campaign date");
        plan.Participants[1].HeroId = "missing";
        var missing = WeeklyReportContextExtractor.ExtractFromPlan(plan);
        Check(missing.Characters[1].Hero == null && missing.Characters[1].Profile == null, "missing ID never substitutes same-name different hero");
        var legacy = WeeklyReportContextExtractor.ExtractFromWeeklyReport("旧刊", "", "【大事件】加尼密诺斯制伏德泰尔。\n【其他消息】卢孔被俘。");
        Check(legacy.Characters.Count == 2 && legacy.Characters.All(p => p.HeroId != "other"), "old issue resolves multiple main-story names, excludes minors");
        Hero.DeadOrDisabledHeroes.Add(king);
        Check(WeeklyReportContextExtractor.ExtractFromPlan(plan).Characters.Count == 2, "duplicate hero collections do not duplicate references");
        for (int i = 0; i < 8; i++) plan.Participants.Add(new() { HeroId = "extra" + i });
        Check(WeeklyReportContextExtractor.ExtractFromPlan(plan).Characters.Count == 4, "portrait work bounded to four participants");
        Check(context.BuildArtDirection().Contains("不能画成贵宾陪同出游") && context.BuildArtDirection().Contains("不得新增伤亡"), "dynamic action direction reaches fallback without changing casualties");
        Console.WriteLine("PASS " + checks + " linked-source participant/context checks; fake game objects, no GPU/network.");
    }
}
namespace AnimusForge { internal class WorldBulletinLayout { } }
namespace TaleWorlds.Core { }
namespace TaleWorlds.Library { public static class Debug { public static void Print(string s) { } } }
namespace TaleWorlds.CampaignSystem
{
    public class Hero
    {
        public static List<Hero> AllAliveHeroes = new(), DeadOrDisabledHeroes = new();
        public string StringId, Name; public bool IsFactionLeader; public object BattleEquipment = new(); public Clan Clan;
    }
    public class Clan { public Banner Banner; public Kingdom Kingdom; }
    public class Kingdom { public Banner Banner; }
    public class Banner { public string BannerCode; }
    public class CampaignTime { public static CampaignTime Now = new(); public int GetSeasonOfYear => 0; public int GetYear => 1090; public int GetDayOfSeason => 1; }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Settlement { public static List<Settlement> All = new(); public string Name; }
}
namespace AnimusForge.Illustrator.Context
{
    public class CharacterAppearanceSnapshot { public static CharacterAppearanceSnapshot FromHero(Hero hero, object equipment) => new(); }
    public class HeroVisualProfile { public string CurrentStateDetail; public CharacterAppearanceSnapshot Appearance; public string BuildVisualSummary() => "视觉资料"; public string BuildDirectorOnlyFacts() => "背景"; }
    public static class HeroVisualExtractor { public static HeroVisualProfile Extract(Hero hero, bool useCivilian, CharacterAppearanceSnapshot appearance) => new() { Appearance = appearance }; }
    public class EnvironmentVisualProfile { public string ConflictStatus, SpecificLocation; public string BuildHardFactsSummary() => ""; public string BuildDirectorOnlyFacts() => ""; }
    public static class EnvironmentVisualExtractor { public static EnvironmentVisualProfile Extract(TaleWorlds.CampaignSystem.Settlements.Settlement settlement, bool eventAnchored, string eventDateLabel) => new(); }
}
