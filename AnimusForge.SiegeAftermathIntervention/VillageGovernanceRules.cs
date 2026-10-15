using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

// Runtime-neutral rules shared by the standalone GCCZ module and the AF adapter.
public static class VillageGovernanceRules
{
    public const int AssistanceMaximum = 6;
    public const int AidCooldownDays = 7;
    public const int AidDurationDays = 21;
    public const int CultureCost = 10000;
    public const int CultureDays = 84;
    public static int CultureDuration(bool purge, bool playerCulture) => purge ? 0 : playerCulture ? 42 : CultureDays;
    public static bool CanConfirmCulture(int expectedSpeaker, int actualSpeaker, int replyTurn, int questionTurn)
        => expectedSpeaker >= 0 && expectedSpeaker == actualSpeaker && questionTurn > 0 && replyTurn > questionTurn;
    public static bool CanGovern(bool sameFaction, bool ownsVillage, bool nationalRuler)
        => sameFaction && (ownsVillage || nationalRuler);
    public static int OwnerPenalty(VillageDispositionActionKind kind)
        => kind == VillageDispositionActionKind.Destroy ? -50
            : kind == VillageDispositionActionKind.MassacreAndSeize ? -20 : -10;
    public static int RulerPenalty(VillageDispositionActionKind kind)
        => kind == VillageDispositionActionKind.Destroy || kind == VillageDispositionActionKind.MassacreAndSeize ? -10 : 0;
    public static bool IsDestructive(VillageDispositionActionKind kind)
        => kind == VillageDispositionActionKind.Destroy || kind == VillageDispositionActionKind.MassacreAndSeize;
    public static bool IsDisposition(VillageDispositionActionKind kind)
        => kind != VillageDispositionActionKind.GatherVillagers && kind != VillageDispositionActionKind.VillagerApproval
            && kind != VillageDispositionActionKind.StopPlunder;
    public static int AidPercent(int mask)
    {
        int count = 0;
        for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) count++;
        return count == 4 ? 50 : count >= 2 ? 20 : 0;
    }
    public static int AidAssistance(int before, int after)
        => (AidPercent(after) >= 20 ? 1 : 0) + (AidPercent(after) == 50 ? 1 : 0)
            - (AidPercent(before) >= 20 ? 1 : 0) - (AidPercent(before) == 50 ? 1 : 0);
}

public sealed class VillageDepartureDecision
{
    public bool HasDisposition { get; private set; }
    public bool ExitRequested { get; private set; }
    public void Reset() { HasDisposition = false; ExitRequested = false; }
    public void MarkDisposition() => HasDisposition = true;
    // Called only from the player's leave request, never generic OnEndMission/load teardown.
    public bool RequestDefaultPlunder(bool active, bool playerAlive)
    {
        if (!active || !playerAlive || ExitRequested) return false;
        ExitRequested = true;
        return !HasDisposition;
    }
}

public sealed class FriendlyVillageAction
{
    internal FriendlyVillageAction(string name, string english, VillageDispositionActionKind? economic = null, int gift = -1)
    { Name = name; English = english; Economic = economic; Gift = gift; }
    public string Name { get; }
    public string English { get; }
    public VillageDispositionActionKind? Economic { get; }
    public int Gift { get; }
    public string Tag => "[VILLAGE_ACTION:" + Name + "]";
}

public static class FriendlyVillageActionCatalog
{
    private static readonly Regex Tags = new Regex(@"\[VILLAGE_ACTION:(?<name>[^\]\r\n]*)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly IReadOnlyList<FriendlyVillageAction> All = new[]
    {
        new FriendlyVillageAction("召集村民", "Gather villagers", VillageDispositionActionKind.GatherVillagers),
        new FriendlyVillageAction("约束军纪", "Restrain troops"),
        new FriendlyVillageAction("停止行动", "Stop operations", VillageDispositionActionKind.StopPlunder),
        new FriendlyVillageAction("宽恕", "Pardon", VillageDispositionActionKind.Mercy),
        new FriendlyVillageAction("赈济金钱", "Donate money", gift: 0),
        new FriendlyVillageAction("赈济粮食", "Donate food", gift: 1),
        new FriendlyVillageAction("援助物资", "Donate goods", gift: 2),
        new FriendlyVillageAction("补充牲畜", "Donate livestock", gift: 3),
        new FriendlyVillageAction("村民自愿入伍", "Voluntary enlistment", VillageDispositionActionKind.VolunteerFollowers),
        new FriendlyVillageAction("自愿提供金钱", "Voluntary money", VillageDispositionActionKind.VoluntaryRansom),
        new FriendlyVillageAction("自愿给予粮食", "Voluntary food", VillageDispositionActionKind.VoluntaryFood),
        new FriendlyVillageAction("自愿给予物资", "Voluntary goods", VillageDispositionActionKind.VoluntaryGoods),
        new FriendlyVillageAction("自愿给予牲畜", "Voluntary livestock", VillageDispositionActionKind.VoluntaryLivestock),
        new FriendlyVillageAction("强征钱财", "Forced levy of money", VillageDispositionActionKind.ForcedRansom),
        new FriendlyVillageAction("强征粮食", "Forced levy of food", VillageDispositionActionKind.ForcedFood),
        new FriendlyVillageAction("强征物资", "Forced levy of goods", VillageDispositionActionKind.ForcedGoods),
        new FriendlyVillageAction("强征牲畜", "Forced levy of livestock", VillageDispositionActionKind.ForcedLivestock),
        new FriendlyVillageAction("强行征兵", "Forced recruitment", VillageDispositionActionKind.ForcedRecruitment),
        new FriendlyVillageAction("掠夺村庄", "Plunder the village", VillageDispositionActionKind.Plunder),
        new FriendlyVillageAction("抓取奴隶", "Enslave peasants", VillageDispositionActionKind.Enslave),
        new FriendlyVillageAction("屠村", "Massacre the village", VillageDispositionActionKind.MassacreAndSeize),
        new FriendlyVillageAction("纯粹毁坏", "Devastate the village", VillageDispositionActionKind.Destroy),
        new FriendlyVillageAction("文化改造", "Cultural reform"),
        new FriendlyVillageAction("认可游说", "Accept sincere persuasion", VillageDispositionActionKind.VillagerApproval),
        new FriendlyVillageAction("核对文化方案", "Ask for confirmation of the selected culture proposal"),
    };
    private static readonly Dictionary<string, FriendlyVillageAction> ByName = All.ToDictionary(a => a.Name, StringComparer.Ordinal);
    public static IReadOnlyList<FriendlyVillageAction> Extract(string text)
        => ExtractInvocations(text).Select(a => a.Action).Distinct().ToList();

    public static IReadOnlyList<FriendlyVillageActionInvocation> ExtractInvocations(string text)
    {
        var result = new List<FriendlyVillageActionInvocation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Tags.Matches(text ?? ""))
        {
            string body = match.Groups["name"].Value;
            if (body.Length > 512) continue;
            var fields = body.Split('|');
            if (!ByName.TryGetValue(fields[0].Trim(), out var action)) continue;
            if (!FriendlyVillageActionInvocation.TryCreate(action, fields.Skip(1), out var invocation)) continue;
            if (seen.Add(invocation.Tag)) result.Add(invocation);
        }
        return result;
    }

    public static string NormalizeInvocations(string text, ISet<string> allowedBaseTags)
        => string.Join("\n", ExtractInvocations(text).Where(a => allowedBaseTags.Contains(a.Action.Tag)).Select(a => a.Tag));
    public static string RemoveTags(string text) => Tags.Replace(text ?? "", "").Trim();
}
