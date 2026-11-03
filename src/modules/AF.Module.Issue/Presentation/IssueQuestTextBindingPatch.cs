using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class IssueQuestTextBinding
{
    internal static void Bind(TextObject text, Hero owner)
    {
        if (text == null || owner?.CharacterObject == null) return;
        foreach (string key in new[] { "QUEST_GIVER", "ISSUE_GIVER", "ISSUE_OWNER" })
            if ((text.Value ?? "").Contains("{" + key + ".") && text.Attributes?.ContainsKey(key) != true)
                StringHelpers.SetCharacterProperties(key, owner.CharacterObject, text);
    }
    internal static void BindLogs(IEnumerable<JournalLog> logs, Hero owner)
    {
        if (logs == null) return;
        foreach (var log in logs) { Bind(log?.LogText, owner); Bind(log?.TaskName, owner); }
    }
}

[HarmonyPatch]
internal static class IssueQuestPanelTextBindingPatch
{
    static IEnumerable<MethodBase> TargetMethods() => typeof(QuestItemVM).GetConstructors()
        .Where(c => c.GetParameters().Length > 0 && (c.GetParameters()[0].ParameterType == typeof(QuestBase)
            || c.GetParameters()[0].ParameterType == typeof(IssueBase) || c.GetParameters()[0].ParameterType == typeof(JournalLogEntry)));
    static void Prefix(object[] __args)
    {
        if (__args[0] is QuestBase quest) IssueQuestTextBinding.BindLogs(quest.JournalEntries, quest.QuestGiver);
        else if (__args[0] is IssueBase issue) IssueQuestTextBinding.BindLogs(issue.JournalEntries, issue.IssueOwner);
        else if (__args[0] is JournalLogEntry entry) IssueQuestTextBinding.BindLogs(entry.GetEntries(), entry.RelatedHero);
    }
}

// Vanilla ArmyNeedsSupplies.Description sets QUEST_GIVER globally instead of on the returned text.
[HarmonyPatch]
internal static class ArmySuppliesDescriptionBindingPatch
{
    static MethodBase TargetMethod() => AccessTools.PropertyGetter(typeof(IssueBase).Assembly.GetType(
        "TaleWorlds.CampaignSystem.Issues.ArmyNeedsSuppliesIssueBehavior+ArmyNeedsSuppliesIssue", true), "Description");
    static void Postfix(IssueBase __instance, TextObject __result) => IssueQuestTextBinding.Bind(__result, __instance.IssueOwner);
}
