using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge.Illustrator.Context
{
    internal enum ConversationEntryKind
    {
        Unknown,
        /// <summary>Hostile party met on the map, battle not started.</summary>
        PreBattleParley,
        /// <summary>Non-hostile party met on the map.</summary>
        PartyMeeting,
        /// <summary>Battle is over and captives are being freed or taken.</summary>
        PostBattlePrisoners,
        /// <summary>Battle is over, other result-flow conversation.</summary>
        PostBattle,
        /// <summary>Started from the player's party screen with a member of the player's party.</summary>
        OwnPartyMember,
        /// <summary>Started from the player's party screen with a prisoner held by the player's party.</summary>
        OwnPartyPrisoner
    }

    internal sealed class ConversationEntry
    {
        internal ConversationEntryKind Kind { get; }
        /// <summary>One hard-fact sentence for the director, without trailing punctuation. Empty when unknown.</summary>
        internal string Fact { get; }

        internal ConversationEntry(ConversationEntryKind kind, string fact)
        { Kind = kind; Fact = fact ?? string.Empty; }

        internal static readonly ConversationEntry Unknown = new ConversationEntry(ConversationEntryKind.Unknown, string.Empty);
    }

    /// <summary>
    /// Tells how the current conversation was entered, so a field conversation is not always drawn as a
    /// pre-battle parley. Runs once per context extraction on the game thread: a few property reads,
    /// at most two roster lookups, no scan and no reflection.
    /// </summary>
    internal static class ConversationEntryClassifier
    {
        internal static ConversationEntry Classify(Hero partnerHero, CharacterObject partnerChar, bool hostile, bool inSettlement, bool mapConversation)
        {
            try
            {
                ConversationContext context = ConversationContext.Default;
                try { context = Campaign.Current?.CurrentConversationContext ?? ConversationContext.Default; } catch { }

                bool postBattle = context == ConversationContext.CapturedLord || context == ConversationContext.FreeOrCapturePrisonerHero;
                PlayerEncounter encounter = null;
                try { encounter = Campaign.Current?.PlayerEncounter; } catch { }
                PlayerEncounterState state = default(PlayerEncounterState);
                bool hasState = false;
                if (encounter != null)
                {
                    try { state = encounter.EncounterState; hasState = true; } catch { }
                }
                if (!postBattle && hasState) postBattle = IsResultState(state);
                if (!postBattle)
                {
                    try
                    {
                        var mapEvent = TaleWorlds.CampaignSystem.MapEvents.MapEvent.PlayerMapEvent;
                        postBattle = mapEvent != null && (mapEvent.HasWinner || mapEvent.IsFinalized);
                    }
                    catch { }
                }

                if (postBattle)
                {
                    if (context == ConversationContext.CapturedLord || context == ConversationContext.FreeOrCapturePrisonerHero)
                        return new ConversationEntry(ConversationEntryKind.PostBattlePrisoners,
                            IllustratorText.Get("ConversationEntry_PostBattlePrisoners"));
                    return new ConversationEntry(ConversationEntryKind.PostBattle,
                        IllustratorText.Get("ConversationEntry_PostBattle"));
                }

                // A live encounter that has not started a battle: party met on the map.
                if (encounter != null && hasState && (state == PlayerEncounterState.Begin || state == PlayerEncounterState.Wait))
                {
                    return hostile
                        ? new ConversationEntry(ConversationEntryKind.PreBattleParley, IllustratorText.Get("ConversationEntry_PreBattleParley"))
                        : new ConversationEntry(ConversationEntryKind.PartyMeeting, IllustratorText.Get("ConversationEntry_PartyMeeting"));
                }

                // Party-screen talks are map conversations without any PlayerEncounter.
                if (mapConversation && !inSettlement && encounter == null)
                {
                    var mainParty = MobileParty.MainParty;
                    if (mainParty != null)
                    {
                        bool prisoner = false, member = false;
                        if (partnerHero != null)
                        {
                            prisoner = partnerHero.IsPrisoner && partnerHero.PartyBelongedToAsPrisoner == PartyBase.MainParty;
                            member = !prisoner && partnerHero.PartyBelongedTo == mainParty;
                        }
                        else if (partnerChar != null)
                        {
                            prisoner = mainParty.PrisonRoster != null && mainParty.PrisonRoster.FindIndexOfTroop(partnerChar) >= 0;
                            member = !prisoner && mainParty.MemberRoster != null && mainParty.MemberRoster.FindIndexOfTroop(partnerChar) >= 0;
                        }
                        if (prisoner)
                            return new ConversationEntry(ConversationEntryKind.OwnPartyPrisoner,
                                IllustratorText.Get("ConversationEntry_OwnPartyPrisoner"));
                        if (member)
                            return new ConversationEntry(ConversationEntryKind.OwnPartyMember,
                                IllustratorText.Get("ConversationEntry_OwnPartyMember"));
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[Illustrator] Conversation entry classification skipped: " + ex.GetType().Name);
            }
            return ConversationEntry.Unknown;
        }

        // Same result states as the host's post-battle flow check.
        private static bool IsResultState(PlayerEncounterState state)
        {
            switch (state)
            {
                case PlayerEncounterState.PrepareResults:
                case PlayerEncounterState.ApplyResults:
                case PlayerEncounterState.PlayerVictory:
                case PlayerEncounterState.PlayerTotalDefeat:
                case PlayerEncounterState.CaptureHeroes:
                case PlayerEncounterState.FreeHeroes:
                case PlayerEncounterState.LootParty:
                case PlayerEncounterState.LootInventory:
                case PlayerEncounterState.End:
                    return true;
                default:
                    return false;
            }
        }
    }
}
