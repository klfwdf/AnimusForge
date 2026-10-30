using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

// Uses the native quest journal for durable delivery observations, not a parallel quest ledger.
internal static class HorseQuestDeliveryOwner
{
    internal const string DeliveredKey = "AF_ISSUE_HorseDelivered";
    internal const string PaidKey = "AF_ISSUE_HorsePaymentObserved";
    private static QuestBase PaymentConfirmedQuest;
    internal static int ConfirmedPayment(QuestBase quest) => ReferenceEquals(PaymentConfirmedQuest, quest) ? Observed(quest, PaidKey) : 0;
    internal static readonly Type QuestType = typeof(IssueBase).Assembly.GetType("TaleWorlds.CampaignSystem.Issues.LordNeedsHorsesIssueBehavior+LordNeedsHorsesIssueQuest", true);
    internal static readonly FieldInfo RequiredField = AccessTools.Field(QuestType, "_numMountsToBeDelivered");
    internal static readonly FieldInfo MountField = AccessTools.Field(QuestType, "_mountObjectToBeDelivered");
    internal static readonly FieldInfo RewardField = AccessTools.Field(typeof(QuestBase), "RewardGold");

    internal static bool TryGet(Hero giver, out QuestBase quest)
    {
        var issue = giver?.Issue;
        quest = issue?.IssueQuest;
        return giver != null && issue != null && issue.IssueOwner == giver && !issue.IsSolvingWithAlternative
            && quest != null && quest.QuestGiver == giver && quest.IsOngoing && QuestType.IsInstanceOfType(quest);
    }
    internal static int Required(QuestBase quest) => (int)RequiredField.GetValue(quest);
    internal static ItemObject Mount(QuestBase quest) => (ItemObject)MountField.GetValue(quest);
    internal static JournalLog Find(QuestBase quest, string key) => quest.JournalEntries.FirstOrDefault(x => x?.TaskName?.GetID() == key);
    internal static int Observed(QuestBase quest, string key) => Math.Max(0, Find(quest, key)?.CurrentProgress ?? 0);
    internal static int Remaining(int required, int delivered) => Math.Max(0, required - Math.Min(required, Math.Max(0, delivered)));
    internal static bool IsReady(QuestBase quest) => Remaining(Required(quest), Observed(quest, DeliveredKey)) <= (MobileParty.MainParty?.ItemRoster.GetItemNumber(Mount(quest)) ?? 0);

    private static void AddObservation(QuestBase quest, string key, int amount, int limit)
    {
        if (amount <= 0 || limit <= 0) return;
        JournalLog log = Find(quest, key);
        int total = (int)Math.Min(limit, (long)Observed(quest, key) + amount);
        if (log == null)
        {
            string fallback = key == DeliveredKey ? "Mounts already handed over" : "Denars received before quest settlement";
            var title = new TextObject("{=" + key + "}" + fallback);
            var text = new TextObject("{=AF_ISSUE_HorseObservation}Recorded actual handover with {QUEST_GIVER.LINK}.");
            StringHelpers.SetCharacterProperties("QUEST_GIVER", quest.QuestGiver.CharacterObject, text);
            quest.AddDiscreteLog(text, title, total, limit);
        }
        else log.UpdateCurrentProgress(total);
    }
    internal static void RecordGiven(Hero giver, string itemId, int amount)
    {
        if (TryGet(giver, out var quest) && string.Equals(Mount(quest)?.StringId, itemId, StringComparison.Ordinal))
            AddObservation(quest, DeliveredKey, amount, Required(quest));
    }
    internal static void RecordPayment(Hero giver, Hero receiver, int actual)
    {
        if (receiver == Hero.MainHero && TryGet(giver, out var quest) && Observed(quest, DeliveredKey) > 0)
            AddObservation(quest, PaidKey, actual, quest.RewardGold);
    }
    internal static string Evidence(QuestBase quest) => IssuePromptTexts.Format("AF_ISSUE_HorseEvidence",
        "Required mount: {ITEM} (id={ID}); required: {REQUIRED}; already handed over to this quest giver: {DELIVERED}; current player stock: {STOCK}; observed payments from the giver after delivery: {PAID}. These are actual observations, not player claims. Completion confirms those payments as quest remuneration; ask for clarification if they were unrelated. Native settlement transfers only the remaining mounts and pays only the remaining base reward.",
        ("ITEM", Mount(quest).Name), ("ID", Mount(quest).StringId), ("REQUIRED", Required(quest)),
        ("DELIVERED", Observed(quest, DeliveredKey)), ("STOCK", MobileParty.MainParty?.ItemRoster.GetItemNumber(Mount(quest)) ?? 0), ("PAID", Observed(quest, PaidKey)));

    internal static bool TryComplete(Hero giver)
    {
        if (!TryGet(giver, out var quest) || !IsReady(quest)) return false;
        var previous = PaymentConfirmedQuest;
        PaymentConfirmedQuest = quest;
        try { quest.CompleteQuestWithSuccess(); return quest.IsFinalized; }
        finally { PaymentConfirmedQuest = previous; }
    }
}

// Scope the adjustment to native completion itself, including native UI turn-in.
[HarmonyPatch]
internal static class HorseQuestSettlementPatch
{
    internal sealed class SettlementState
    {
        internal int Required, Reward, JournalCount;
    }
    static MethodBase TargetMethod() => AccessTools.DeclaredMethod(HorseQuestDeliveryOwner.QuestType, "OnCompleteWithSuccess");
    static void Prefix(QuestBase __instance, out SettlementState __state)
    {
        __state = new SettlementState { Required = HorseQuestDeliveryOwner.Required(__instance), Reward = __instance.RewardGold, JournalCount = __instance.JournalEntries.Count };
        HorseQuestDeliveryOwner.RequiredField.SetValue(__instance, HorseQuestDeliveryOwner.Remaining(__state.Required, HorseQuestDeliveryOwner.Observed(__instance, HorseQuestDeliveryOwner.DeliveredKey)));
        HorseQuestDeliveryOwner.RewardField.SetValue(__instance, HorseQuestDeliveryOwner.Remaining(__state.Reward, HorseQuestDeliveryOwner.ConfirmedPayment(__instance)));
    }
    static Exception Finalizer(QuestBase __instance, SettlementState __state, Exception __exception)
    {
        if (__state != null)
        {
            HorseQuestDeliveryOwner.RequiredField.SetValue(__instance, __state.Required);
            HorseQuestDeliveryOwner.RewardField.SetValue(__instance, __state.Reward);
            // Native success prose reports total delivery, while the reward number remains the actual final payout.
            for (int i = __state.JournalCount; i < __instance.JournalEntries.Count; i++)
                __instance.JournalEntries[i].LogText?.SetTextVariable("MOUNT_COUNT", __state.Required);
        }
        return __exception;
    }
}
