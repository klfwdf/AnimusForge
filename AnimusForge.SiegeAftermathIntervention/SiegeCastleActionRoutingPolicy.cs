using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Hard role/direct-reply gate for castle actions. Prompt wording may guide the AI, but this policy is authoritative.
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
        if (!facts.ReplyIsDirectPlayerResponse)
        {
            return Block(hasRecognizedAction: true, action, "direct_player_response_required");
        }

        SiegeCastlePrisonerDispositionKind disposition = SiegeCastlePrisonerDispositionKindProfile.FromAction(action);
        SiegeCastlePlayerAuthorizationDecision authorization = SiegeCastlePlayerAuthorizationPolicy.Evaluate(
            facts.PlayerText,
            facts.PendingProposalForSpeaker);

        if (SiegeCastleActionKindProfile.IsProposal(action))
        {
            if (facts.RemainingRegularPrisoners <= 0)
            {
                return Block(hasRecognizedAction: true, action, "no_regular_prisoners_remaining");
            }

            bool validProposer = facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier
                || (action == SiegeCastleActionKind.ProposeRecruitPrisoners
                    && facts.SpeakerRole == SiegeCastleActionSpeakerRole.RegularPrisoner);
            if (!validProposer)
            {
                return Block(hasRecognizedAction: true, action, "soldier_proposal_role_required");
            }

            if (authorization.IsAuthorized)
            {
                return Block(
                    hasRecognizedAction: true,
                    action,
                    authorization.Disposition == disposition
                        ? "player_already_authorized_disposition"
                        : "player_authorized_different_disposition");
            }

            return authorization.ReasonCode == "player_rejected_or_cancelled"
                ? Block(hasRecognizedAction: true, action, "player_rejected_proposal")
                : Allow(action, "soldier_proposal_record_only");
        }

        if (SiegeCastleActionKindProfile.IsPrisonerDispositionSettlement(action))
        {
            if (facts.SpeakerRole != SiegeCastleActionSpeakerRole.RegularPrisoner
                && facts.SpeakerRole != SiegeCastleActionSpeakerRole.AlliedSoldier)
            {
                return Block(hasRecognizedAction: true, action, "soldier_response_required");
            }

            if (!authorization.IsAuthorized || authorization.Disposition != disposition)
            {
                return Block(hasRecognizedAction: true, action, "player_authorization_required");
            }

            return facts.RemainingRegularPrisoners > 0
                ? Allow(action, authorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, "no_regular_prisoners_remaining");
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

            SiegeCastleSoldierAppeasementAuthorizationDecision appeasementAuthorization =
                SiegeCastleSoldierAppeasementAuthorizationPolicy.Evaluate(facts.PlayerText);
            if (!appeasementAuthorization.IsAuthorized)
            {
                return Block(hasRecognizedAction: true, action, appeasementAuthorization.ReasonCode);
            }

            return !facts.SoldierAppeasementApplied
                ? Allow(action, appeasementAuthorization.ReasonCode)
                : Block(hasRecognizedAction: true, action, "soldier_appeasement_already_applied");
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
