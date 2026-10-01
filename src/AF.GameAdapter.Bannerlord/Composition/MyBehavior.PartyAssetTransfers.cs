using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public partial class MyBehavior
{
	public static PartyBase ResolvePartyTransferCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
		=> PartyAssetTransferBannerlordAdapter.ResolvePartyTransferCounterpartyForExternal(targetHero, targetCharacter, targetAgentIndex);

	public static bool IsWildernessNonHeroPartyTransferEligibleForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
		=> PartyAssetTransferBannerlordAdapter.IsWildernessNonHeroPartyTransferEligibleForExternal(targetHero, targetCharacter, targetAgentIndex);

	private static bool IsPartyTransferRuleEligible(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
		=> PartyAssetTransferBannerlordAdapter.IsPartyTransferRuleEligible(targetHero, targetCharacter, targetAgentIndex);

	public static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
		=> PartyAssetTransferBannerlordAdapter.BuildPartyTransferPromptEntriesForExternal(targetHero, targetCharacter, targetAgentIndex);

	public static bool IsSettlementTransferLeaderEligibleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		=> PartyAssetTransferBannerlordAdapter.IsSettlementTransferLeaderEligibleForExternal(targetHero, targetCharacter);

	public static string GetSettlementTransferAssetIdForExternal(SettlementTransferPromptEntry entry)
		=> PartyAssetTransferBannerlordAdapter.GetSettlementTransferAssetIdForExternal(entry);

	public static bool IsSettlementTransferEntryValidForExternal(SettlementTransferPromptEntry entry)
		=> PartyAssetTransferBannerlordAdapter.IsSettlementTransferEntryValidForExternal(entry);

	public static string GetSettlementTransferAssetDisplayNameForExternal(SettlementTransferPromptEntry entry)
		=> PartyAssetTransferBannerlordAdapter.GetSettlementTransferAssetDisplayNameForExternal(entry);

	public static bool LooksLikeFixedAssetTransferIdForExternal(string assetToken)
		=> PartyAssetTransferBannerlordAdapter.LooksLikeFixedAssetTransferIdForExternal(assetToken);

	public static bool TryResolveFixedAssetTransferEntryByIdForExternal(string assetToken, out SettlementTransferPromptEntry entry)
		=> PartyAssetTransferBannerlordAdapter.TryResolveFixedAssetTransferEntryByIdForExternal(assetToken, out entry);

	public static List<SettlementTransferPromptEntry> BuildSettlementTransferPromptEntriesForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		=> PartyAssetTransferBannerlordAdapter.BuildSettlementTransferPromptEntriesForExternal(targetHero, targetCharacter);

	public static SettlementTransferPromptEntry ResolveSettlementTransferEntryForExternal(Hero targetHero, CharacterObject targetCharacter, string directionToken, string settlementToken)
		=> PartyAssetTransferBannerlordAdapter.ResolveSettlementTransferEntryForExternal(targetHero, targetCharacter, directionToken, settlementToken);

	public static Settlement ResolveSettlementTransferSettlementForExternal(Hero targetHero, CharacterObject targetCharacter, string directionToken, string settlementToken)
		=> PartyAssetTransferBannerlordAdapter.ResolveSettlementTransferSettlementForExternal(targetHero, targetCharacter, directionToken, settlementToken);

	public static string BuildSettlementTransferRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		=> PartyAssetTransferBannerlordAdapter.BuildSettlementTransferRuntimeInstructionForExternal(targetHero, targetCharacter);

	public static string GetPartyTransferPrisonerSourceLabelForExternal(PartyTransferPromptEntry entry)
		=> PartyAssetTransferBannerlordAdapter.GetPartyTransferPrisonerSourceLabelForExternal(entry);

	internal static long CalculateSettlementTransferTotalValueForExternal(IEnumerable<SettlementTransferPromptEntry> entries)
		=> PartyAssetTransferBannerlordAdapter.CalculateSettlementTransferTotalValueForExternal(entries);

	internal static long CalculatePartyTransferTotalValueForExternal(IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner)
		=> PartyAssetTransferBannerlordAdapter.CalculatePartyTransferTotalValueForExternal(entries, isPrisoner);

	public static string GetPartyTransferTroopTypeLabelForExternal(CharacterObject character)
		=> PartyAssetTransferBannerlordAdapter.GetPartyTransferTroopTypeLabelForExternal(character);

	public static bool IsPartyTransferLordEligibleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		=> PartyAssetTransferBannerlordAdapter.IsPartyTransferLordEligibleForExternal(targetHero, targetCharacter);

	public static string BuildPartyTransferRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
		=> PartyAssetTransferBannerlordAdapter.BuildPartyTransferRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);

	public static bool TryApplyPartyTransferTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications)
		=> PartyAssetTransferBannerlordAdapter.TryApplyPartyTransferTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out generatedFacts, out notifications);

	public static int TransferPlayerPartyEntryToCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
		=> PartyAssetTransferBannerlordAdapter.TransferPlayerPartyEntryToCounterpartyForExternal(targetHero, targetCharacter, targetAgentIndex, entry, amount);
    internal static PartyTransferEffectResult TransferPlayerPartyEntryWithObservedEffects(Hero targetHero,
        CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
        => PartyAssetTransferBannerlordAdapter.TransferPlayerPartyEntryWithObservedEffects(targetHero, targetCharacter, targetAgentIndex, entry, amount);
}
