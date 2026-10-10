using System;
using System.Threading;

namespace AnimusForge;

public partial class DuelSettings
{
    // Packaged defaults follow the CN/EN content package. User overrides still go through
    // the existing layered store; defaults are read at most once per process, never per tick.
    private static readonly Lazy<string> PackagedKingdomRebellionSystemPromptDefault = new Lazy<string>(
        () => ReadPackagedPromptDefault(KingdomRebellionSystemPromptJsonFileName, "You are a chronicler tasked with naming rebel realms in the current world setting.\nYour task is to generate a realm name and encyclopedia biography for a fledgling realm that has just broken away from its former parent kingdom, based on the provided material.\nNaming guidelines:\n1. The name must fit the medieval fantasy style of the current setting; do not use modern political terminology.\n2. Strive to derive the name from the original kingdom's identity or the rebellion's cause, rather than specific locations or clan names. If the original kingdom was an Empire, do not reuse the 'Empire' title or claim imperial supremacy.\n3. The formal name should feel natural, dignified, and suitable for an encyclopedia entry; the short name should be concise for map and UI display.\n4. Avoid duplicate names with existing realms.\n5. The encyclopedia biography should mirror native encyclopedia style: concise, objective, and summarizing the founding context.\n6. Output only the specified fields without commentary."),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static string DefaultKingdomRebellionSystemPrompt => PackagedKingdomRebellionSystemPromptDefault.Value;

    private static readonly Lazy<string> PackagedWeeklyReportWritingRequirementsDefault = new Lazy<string>(
        () => ReadPackagedPromptDefault(WeeklyReportWritingRequirementsJsonFileName, "1. Must cover key changes in the material for this week or issue, without mechanically repeating each item; combine similar information.\n2. Only the full [REPORT] body must be divided sequentially into three sections: [Military Affairs], [Diplomatic Affairs], [Domestic & Fief Affairs]. Write a concise paragraph for each; if material is lacking, state 'No notable changes recorded this week', without omitting sections. Short reports and [SHORT] must NOT be split into sections; write a single compact factual summary.\n3. Military Affairs include battles, sieges, troop movements, captives, triumphs/defeats, and border threats. Diplomatic Affairs include declarations of war, peace, alliances, truces, treaties, and relations between vassals and realms. Domestic & Fief Affairs include fief ownership, security, loyalty, prosperity, food, governance, and populace shifts.\n4. International events must be framed strictly from the perspective of the report's subject realm; never misattribute foreign events as domestic.\n5. Never fabricate core facts unsupported by the source material.\n6. Synthesize fragmented material into situational insights; distill voluminous material into several main narrative threads.\n7. Adopt the tone of a chronicle, political gazette, or noble gazetteer: clear, fluent, ornate, epic, with high literary sophistication and evocative diction; do not format as bulleted lists.\n8. Do not format as novel dialogue.\n9. Never use system jargon, field names, StableKeys, raw material tags, or developer notes.\n10. Describe changes through qualitative prose rather than raw numeric values.\n11. Settlement ownership changes must respect the source context: if transferred via trade, purchase, or non-siege cession, never describe it as stormed, captured, or taken by siege; place siege seizures under Military Affairs, and trade/diplomatic transfers under Diplomatic or Domestic Affairs.\n12. Avoid default native continent names; refer generally to 'the continent', 'the realm', or specific kingdoms and towns.\n13. Military victories generally enhance realm stability, while defeats reduce it (only when supported by context).\n14. Keep titles concise, body text complete, and short summaries tightly packed with factual anchors for downstream NPC prompt injection.\n15. When events are quiet, faithfully reflect the tranquil status quo without forcing dramatic plot twists.\n16. If battlefield engagements involve villagers, caravans, patrols, militia, or bandits, specify the troop type without sensationalizing it into a noble triumph."),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static string DefaultWeeklyReportWritingRequirements => PackagedWeeklyReportWritingRequirementsDefault.Value;

    private static readonly Lazy<string> PackagedDailyMemoryCompressionWritingRequirementsDefault = new Lazy<string>(
        () => ReadPackagedPromptDefault(DailyMemoryCompressionWritingRequirementsJsonFileName, "SUMMARY must retain key motivations, promises, conflicts, relationship shifts, transactions, quests, and emotional trajectories."),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static string DefaultDailyMemoryCompressionWritingRequirements => PackagedDailyMemoryCompressionWritingRequirementsDefault.Value;

    private static readonly Lazy<string> PackagedMemoryOverviewCompressionWritingRequirementsDefault = new Lazy<string>(
        () => ReadPackagedPromptDefault(MemoryOverviewCompressionWritingRequirementsJsonFileName, "May be organized by timeline or theme, using concise language; do not list items mechanically or repeat the same facts."),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static string DefaultMemoryOverviewCompressionWritingRequirements => PackagedMemoryOverviewCompressionWritingRequirementsDefault.Value;

    private static readonly Lazy<string> PackagedMajorActionCompressionWritingRequirementsDefault = new Lazy<string>(
        () => ReadPackagedPromptDefault(MajorActionCompressionWritingRequirementsJsonFileName, "Write an easily understood summary. Unimportant, repetitive, or convoluted details may be omitted."),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static string DefaultMajorActionCompressionWritingRequirements => PackagedMajorActionCompressionWritingRequirementsDefault.Value;

    private static string ReadPackagedPromptDefault(string fileName, string englishFallback)
    {
        try
        {
            string directory = GetPackagedCustomPromptTextStoreDirectory();
            if (!string.IsNullOrWhiteSpace(directory))
            {
                string path = GetCustomPromptTextFilePath(directory, fileName);
                if (TryReadCustomPromptTextJsonFile(path, null, englishFallback, out string text))
                    return text;
            }
        }
        catch
        {
            // Path discovery may be unavailable in a headless host. Keep the full fallback.
        }
        return englishFallback;
    }
}
