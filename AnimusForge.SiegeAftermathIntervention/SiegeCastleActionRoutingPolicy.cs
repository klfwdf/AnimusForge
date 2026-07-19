using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Hard role/direct-reply/state gate for castle actions selected by AF's native action
/// postprocessor. The AI is authoritative for semantic intent and consent; this policy
/// never reinterprets a selected tag through a second keyword list.
/// </summary>
public static class SiegeCastleActionRoutingPolicy
{
    public static SiegeCastleActionRoutingDecision Evaluate(SiegeCastleActionRoutingFacts facts)
    {
        facts ??= new SiegeCastleActionRoutingFacts(
            string.Empty,
            SiegeCastleActionSpeakerRole.Unknown,
            replyIsDirectPlayerResponse: false,
            remainingRegularPrisoners: 0,
            soldierAppeasementRequired: false,
            soldierAppeasementApplied: false);

        IReadOnlyList<SiegeCastleActionKind> actions = SiegeCastleActionTagCatalog.ExtractKinds(facts.RawActionText);
        if (actions.Count == 0)
        {
            return Block(hasRecognizedAction: false, SiegeCastleActionKind.Unknown, "no_castle_action");
        }

        if (actions.Count != 1)
        {
            return Block(hasRecognizedAction: true, SiegeCastleActionKind.Unknown, "multiple_castle_actions");
        }

        SiegeCastleActionKind action = actions[0];
        if (action == SiegeCastleActionKind.SoldierDiscontent)
        {
            return facts.IsWitnessReaction
                && facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
                && SiegeCastleSoldierReactionProfile.CanReactTo(facts.ReactionToAction)
                ? Allow(action, "allied_witness_expressed_discontent")
                : Block(hasRecognizedAction: true, action, "castle_soldier_witness_reaction_required");
        }
        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return Block(hasRecognizedAction: true, action, "direct_player_response_required");
        }

        if (SiegeCastleActionKindProfile.IsProposal(action))
        {
            if (facts.RemainingRegularPrisoners <= 0)
            {
                return Block(hasRecognizedAction: true, action, "no_regular_prisoners_remaining");
            }

            bool validProposer = facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
                || (facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner
                    && SiegeCastleActionKindProfile.CanRegularPrisonerPropose(action));
            if (!validProposer)
            {
                return Block(hasRecognizedAction: true, action, "soldier_proposal_role_required");
            }

            return Allow(action, "castle_ai_proposal_tag_role_state_valid");
        }

        if (action == SiegeCastleActionKind.AppeaseSoldiers)
        {
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.AlliedSoldier)
            {
                return Block(hasRecognizedAction: true, action, "allied_soldier_response_required");
            }

            if (!facts.SoldierAppeasementRequired)
            {
                return Block(hasRecognizedAction: true, action, "soldier_appeasement_not_required");
            }

            if (facts.SoldierAppeasementApplied)
            {
                return Block(hasRecognizedAction: true, action, "soldier_appeasement_already_applied");
            }

            SiegeCastleDirectActionAuthorizationDecision appeasementAuthorization =
                SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText);
            return appeasementAuthorization.IsAuthorized
                ? Allow(action, appeasementAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, appeasementAuthorization.ReasonCode);
        }

        if (SiegeCastleActionKindProfile.IsLordProcess(action))
        {
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.CapturedLord)
            {
                return Block(hasRecognizedAction: true, action, "captured_lord_response_required");
            }
            if (facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                return Block(hasRecognizedAction: true, action, "captured_lord_already_resolved");
            }
            if (facts.IsActionAlreadyApplied(action))
            {
                return Block(hasRecognizedAction: true, action, "castle_lord_process_already_applied");
            }
            SiegeCastleDirectActionAuthorizationDecision lordProcessAuthorization =
                SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText);
            return lordProcessAuthorization.IsAuthorized
                ? Allow(action, lordProcessAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, lordProcessAuthorization.ReasonCode);
        }

        if (SiegeCastleActionKindProfile.IsProcess(action))
        {
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.AlliedSoldier
                && facts.SpeakerRole != SiegeCastleActionSpeakerRole.RegularPrisoner
                && facts.SpeakerRole != SiegeCastleActionSpeakerRole.CapturedLord)
            {
                return Block(hasRecognizedAction: true, action, "prisoner_response_required");
            }
            if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner
                && facts.RemainingRegularPrisoners <= 0)
            {
                return Block(hasRecognizedAction: true, action, "no_regular_prisoners_remaining");
            }
            if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord
                && facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                return Block(hasRecognizedAction: true, action, "target_already_terminally_resolved");
            }
            if (facts.IsActionAlreadyApplied(action))
            {
                return Block(hasRecognizedAction: true, action, "castle_process_already_applied");
            }
            SiegeCastleDirectActionAuthorizationDecision processAuthorization =
                SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText);
            return processAuthorization.IsAuthorized
                ? Allow(action, processAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, processAuthorization.ReasonCode);
        }

        if (SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action))
        {
            bool alliedMayExecute = facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
                && SiegeCastleActionKindProfile.CanAlliedSoldierExecute(action);
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.RegularPrisoner && !alliedMayExecute)
            {
                return Block(hasRecognizedAction: true, action, "regular_prisoner_response_required");
            }
            if (facts.RemainingRegularPrisoners <= 0)
            {
                return Block(hasRecognizedAction: true, action, "no_regular_prisoners_remaining");
            }
            if (SiegeCastleActionKindProfile.IsVoluntary(action)
                && (facts.SpeakerRole != SiegeCastleActionSpeakerRole.RegularPrisoner
                    || !SiegeCastlePrisonerTrustProfile.MeetsVoluntaryThreshold(action, facts.SpeakerTrust)))
            {
                return Block(hasRecognizedAction: true, action, "voluntary_trust_threshold_not_met");
            }
            SiegeCastleDirectActionAuthorizationDecision terminalAuthorization =
                SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText);
            return terminalAuthorization.IsAuthorized
                ? Allow(action, terminalAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, terminalAuthorization.ReasonCode);
        }

        if (SiegeCastleActionKindProfile.IsLordTerminal(action))
        {
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.CapturedLord)
            {
                return Block(hasRecognizedAction: true, action, "captured_lord_response_required");
            }
            if (facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)
            {
                return Block(hasRecognizedAction: true, action, "captured_lord_already_resolved");
            }
            if (action == SiegeCastleActionKind.RecruitLord)
            {
                SiegeCastleLordRecruitmentBranch branch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                    facts.SpeakerIsClanLeader,
                    facts.PlayerHasKingdom,
                    facts.PlayerRulesKingdom,
                    facts.PlayerText + "\n" + facts.SpeakerReplyText);
                if (branch == SiegeCastleLordRecruitmentBranch.Unknown)
                {
                    return Block(hasRecognizedAction: true, action, "lord_recruitment_branch_required");
                }
            }
            SiegeCastleDirectActionAuthorizationDecision lordAuthorization =
                SiegeCastleDirectActionAuthorizationPolicy.Evaluate(action, facts.PlayerText);
            return lordAuthorization.IsAuthorized
                ? Allow(action, lordAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, lordAuthorization.ReasonCode);
        }

        return Block(hasRecognizedAction: true, action, "unsupported_castle_action");
    }

    private static SiegeCastleActionRoutingDecision Allow(SiegeCastleActionKind action, string reasonCode)
    {
        return new SiegeCastleActionRoutingDecision(true, true, action, reasonCode);
    }

    private static SiegeCastleActionRoutingDecision Block(bool hasRecognizedAction, SiegeCastleActionKind action, string reasonCode)
    {
        return new SiegeCastleActionRoutingDecision(hasRecognizedAction, false, action, reasonCode);
    }
}
