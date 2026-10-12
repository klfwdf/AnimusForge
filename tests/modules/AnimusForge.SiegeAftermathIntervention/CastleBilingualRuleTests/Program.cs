using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;

internal static class Program
{
    private static int count;
    private static void Check(bool value, string label) { count++; if (!value) throw new Exception(label); }
    private static void Main()
    {
        var all = new Dictionary<string, string>();
        foreach (SiegeCastleActionSpeakerRole role in Enum.GetValues(typeof(SiegeCastleActionSpeakerRole)))
        foreach (int trust in new[] { -5, 5, 8, 10, 100 })
        foreach (bool witness in new[] { false, true })
        {
            var facts = new SiegeCastlePostprocessRuleFacts(role, true, 200, true, false, "release all", SiegeCastlePrisonerDispositionKind.None, trust, isWitnessReaction:witness, reactionToAction:SiegeCastleActionKind.ReleasePrisoners);
            var cn = SiegeCastlePostprocessRuleCatalog.GetAvailableRules(facts, false);
            var en = SiegeCastlePostprocessRuleCatalog.GetAvailableRules(facts, true);
            Check(cn.Select(x=>x.Tag).SequenceEqual(en.Select(x=>x.Tag)), "same role/trust/witness eligibility and order");
            foreach (var rule in en)
            {
                Check(!Regex.IsMatch(rule.Description, @"[\u4e00-\u9fff]"), "English rule text: " + rule.Tag);
                Check(rule.Description.Contains("complete player input") && rule.Description.Contains("do not trigger it by fixed keywords"), "semantic contract: " + rule.Tag);
                all[rule.Tag]=rule.Description;
            }
        }
        Check(all.Count==26, "all 26 actual public candidate rules exercised");
        string[] staged = { SiegeCastleActionTagCatalog.ReleasePrisonersTag, SiegeCastleActionTagCatalog.SellPrisonersTag, SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag, SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag, SiegeCastleActionTagCatalog.LaborPrisonersForcedTag, SiegeCastleActionTagCatalog.LaborPrisonersVoluntaryTag, SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag, SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag, SiegeCastleActionTagCatalog.InstructorPrisonersForcedTag, SiegeCastleActionTagCatalog.InstructorPrisonersVoluntaryTag };
        foreach(string tag in staged)
        {
            string text=all[tag];
            Check(text.Contains("random quantity") && text.Contains("troops randomly") && text.Contains("currently unassigned"), "allocation semantics: "+tag);
            Check(text.Contains("multiple different") && text.Contains("group order") && text.Contains("must not clear other groups") && text.Contains("only when the player explicitly"), "multi-group reset semantics: "+tag);
        }
        Check(all[SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag].Contains("actual number is not halved") && all[SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag].Contains("50%") && all[SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag].Contains("1.5"), "forced recruitment ratios and headcount");
        Check(all[SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag].Contains("actual number is not halved") && all[SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag].Contains("half the corresponding town"), "voluntary recruitment ratios and headcount");
        Check(!all.Values.Any(x=>x.Contains("fixed 90") || x.Contains("fixed 60") || x.Contains("Requires trust >= 10")), "no invented morale or shared trust threshold");
        Check(all[SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag].Contains("200") && all[SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag].Contains("+25%") && all[SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag].Contains("+6.25%"), "forced repair numeric contract");
        Check(all[SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag].Contains("200") && all[SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag].Contains("+50%") && all[SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag].Contains("linearly"), "voluntary repair numeric contract");
        foreach(string text in new[] { "cancel release", "do not slaughter them", "don't execute them", "don’t execute them", "do not repair the castle", "do not train recruits", "不要杀了他们", "不要释放", "取消", "不同意" })
            Check(!SiegeCastlePlayerAuthorizationPolicy.Evaluate(text, SiegeCastlePrisonerDispositionKind.None).IsAuthorized, "negative denied: "+text);
        foreach(string text in new[] { "release all", "释放全部", "release 50 prisoners", "批准" })
            Check(SiegeCastlePlayerAuthorizationPolicy.Evaluate(text, SiegeCastlePrisonerDispositionKind.Release).IsAuthorized, "positive preserved: "+text);
        Check(!SiegeCastlePrisonerAllocationProfile.RequestsAllAvailable("small group"), "small is not all");
        Check(!SiegeCastlePrisonerAllocationProfile.RequestsAllAvailable("recall 50 prisoners"), "recall is not all");
        Check(SiegeCastlePrisonerAllocationProfile.RequestsAllAvailable("release all") && SiegeCastlePrisonerAllocationProfile.RequestsAllAvailable("释放全部"), "English and Chinese all");
        Check(SiegeCastlePrisonerAllocationProfile.IsPlanResetRequested("start over") && SiegeCastlePrisonerAllocationProfile.IsPlanResetRequested("全部重来"), "English and Chinese reset");
        Check(SiegeCastlePrisonerAllocationProfile.RequestsEliteTroops("elite veterans") && SiegeCastlePrisonerAllocationProfile.RequestsEliteTroops("精锐"), "English and Chinese elite");
        Check(SiegeCastlePrisonerAllocationProfile.RequestsLowTierTroops("low-tier recruits") && SiegeCastlePrisonerAllocationProfile.RequestsLowTierTroops("低阶"), "English and Chinese low tier");
        Check(SiegeCastlePrisonerAllocationProfile.TryParseExplicitCount("release 50 Prisoners", out int number) && number==50, "capitalized English quantity");
        Check(SiegeCastlePrisonerTrustProfile.VoluntaryRecruitThreshold==10 && SiegeCastlePrisonerTrustProfile.VoluntaryLaborThreshold==5 && SiegeCastlePrisonerTrustProfile.VoluntaryInstructorThreshold==8, "gameplay thresholds untouched");
        Console.WriteLine("PASS GCCZ castle bilingual actual-source checks="+count+" rules="+all.Count+" live=NOT_RUN");
    }
}
