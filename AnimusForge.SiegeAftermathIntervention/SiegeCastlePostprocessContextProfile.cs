using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeCastlePostprocessContextProfile
{
    public static string Build(SiegeCastlePostprocessContextFacts facts)
        => Build(facts, false);

    public static string Build(SiegeCastlePostprocessContextFacts facts, bool isEnglish)
    {
        facts ??= SiegeCastlePostprocessContextFacts.Empty;
        bool hasCompoundAuthorization = SiegeCastleCompoundDispositionPlanProfile.TryBuild(
            facts.PlayerText,
            out SiegeCastleCompoundDispositionPlan compoundPlan);
        StringBuilder sb = new StringBuilder();
        if (facts.IsWitnessReaction)
        {
            if (isEnglish)
            {
                return sb.Append("[CASTLE DISPOSITION WITNESS REACTION POSTPROCESS] This turn is an allied soldier speaking freely about the recent \u0027")
                    .Append(SiegeCastleSoldierReactionProfile.DescribeConcernAction(facts.ReactionToAction, true))
                    .Append("\u0027, not directly responding to a new player command. Output the castle soldier discontent tag only when the spoken line clearly expresses disgust, concern, sympathy, discipline doubts, cultural conflict, or discontent with the commander\u0027s actions; agreement, neutral repetition, ordinary inquiries, or suggestions must not output. This tag only registers pending morale appeasement and must not settle or propose any new prisoner disposition, nor trigger a next round of witness reactions.")
                    .ToString();
            }
            return sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_2f2602f65a47"))
                .Append(SiegeCastleSoldierReactionProfile.DescribeConcernAction(facts.ReactionToAction))
                .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_f203926a096d"))
                .ToString();
        }

        if (isEnglish)
        {
            sb.Append("[CASTLE DISPOSITION POSTPROCESS FACTS] Settlement=")
                .Append(string.IsNullOrWhiteSpace(facts.CastleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleNameEn : facts.CastleName.Trim())
                .Append("; Speaker Role=")
                .Append(SiegeCastleActionSpeakerRoleProfile.Describe(facts.SpeakerRole, true))
                .Append("; Direct Player Response=")
                .Append(facts.ReplyIsDirectPlayerResponse ? "Yes" : "No")
                .Append("; Remaining Regular Prisoners=")
                .Append(facts.RemainingRegularPrisoners)
                .Append("; Recruited Regular Prisoners=")
                .Append(facts.RecruitedRegularPrisoners)
                .Append("; Slaughtered Regular Prisoners=")
                .Append(facts.SlaughteredRegularPrisoners)
                .Append("; Allied Morale Appeasement Required=")
                .Append(facts.SoldierAppeasementRequired ? "Yes" : "No")
                .Append("; Appeasement Completed=")
                .Append(facts.SoldierAppeasementApplied ? "Yes" : "No")
                .Append("; Speaker Pending Proposal=")
                .Append(SiegeCastlePrisonerDispositionKindProfile.Describe(facts.PendingProposalForSpeaker, true))
                .Append("; Speaker Prisoner Trust=")
                .Append(facts.SpeakerTrust)
                .Append(facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord
                    ? "; Target Final Disposition="
                    : "; Regular Prisoner Staged Disposition=")
                .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? "None" : facts.TerminalActionForTarget.ToString())
                .Append("; Player Exact Words: \u0022")
                .Append((facts.PlayerText ?? string.Empty).Trim())
                .Append("\u0022; Compound Partition=")
                .Append(hasCompoundAuthorization ? DescribeCompoundPlan(compoundPlan, true) : "None")
                .Append(". Every candidate action tag supplied is only a legal capability for this role and real-time state, not code-level trigger confirmation. The AF action postprocess AI is the semantic judge: decide tags from full player utterance, latest NPC reply, proposals, and scene memory; never rely on keywords or output merely because a tag exists. Do not output disposition tags on questions, assumptions, paraphrasing, negation, refusal, mind-changing, chatter, or ambiguity. When allied soldiers or regular prisoners propose options, output only the corresponding proposal tag; proposals log pending status and never execute immediately. When the player clearly commands or approves, output the disposition tag. Each regular prisoner tag registers designated count/troop tier; unstated picks randomly at runtime. Compound orders partition distinct groups. Escort soldiers obey but cannot alter disposition type; voluntary branches require regular prisoners themselves to respond and meet trust threshold. Appeasement tags are judged from full dialogue semantics. At most one castle disposition tag per reply unless multi-group partitions are explicitly specified.");

            if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord)
            {
                SiegeCastleLordRecruitmentBranch playerTextBranch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                    facts.SpeakerIsClanLeader,
                    facts.PlayerHasKingdom,
                    facts.PlayerRulesKingdom,
                    facts.PlayerText);
                sb.Append(" [LORD RECRUITMENT BRANCH] Clan Leader=").Append(facts.SpeakerIsClanLeader ? "Yes" : "No")
                    .Append("; Player Has Kingdom=").Append(facts.PlayerHasKingdom ? "Yes" : "No")
                    .Append("; Player Rules Kingdom=").Append(facts.PlayerRulesKingdom ? "Yes" : "No")
                    .Append("; Player Text Recognized Branch=").Append(SiegeCastleLordRecruitmentBranchProfile.Describe(playerTextBranch, true)).Append(". ");
                if (!facts.SpeakerIsClanLeader)
                {
                    sb.Append("Non-clan leader recruitment requires choosing between introducing clan leader or becoming a companion; output recruitment tag only when complete dialogue specifies a branch. ");
                }
                sb.Append("Lord sale is an independent individual disposition: output only when the player explicitly orders handing the lord to a ransom broker; pricing and consequences follow native tavern ransom flow, never common prisoner sales. ");
                sb.Append("Lord execution is judged semantically: output when the player explicitly orders execution/slaughter and the reply acknowledges it; questions, doubts, insults, or threats do not output. Tag opens native execution confirmation and cannot bypass player confirmation. ");
                sb.Append(" [LORD DUEL FACT] ")
                    .Append(SiegeCastleLordDuelProfile.BuildEquipmentFact(
                        facts.PlayerIsMounted,
                        facts.PlayerCarriesRangedWeapon,
                        facts.PlayerWieldsRangedWeapon,
                        true))
                    .Append("The lord may refuse or set conditions due to mounted player or ranged weapons; conditional replies are not consent. Output duel tag when the lord explicitly accepts challenge now without conditions. ");
            }

            if (facts.SpeakerCultureMatchesCastle)
            {
                sb.Append("Allied soldier shares culture with castle; may express complex doubt or sympathy, but still obeys the player without bypassing response and appeasement gates. ");
            }

            return sb.ToString();
        }

        sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_a089de3bdfca"))
            .Append(string.IsNullOrWhiteSpace(facts.CastleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleName : facts.CastleName.Trim())
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_653dca86d317"))
            .Append(SiegeCastleActionSpeakerRoleProfile.Describe(facts.SpeakerRole))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_b1a77d1403ee"))
            .Append(facts.ReplyIsDirectPlayerResponse ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_6cb96b5ae161"))
            .Append(facts.RemainingRegularPrisoners)
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_36ae63544046"))
            .Append(facts.RecruitedRegularPrisoners)
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_03bee63ce31d"))
            .Append(facts.SlaughteredRegularPrisoners)
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_55936797bed1"))
            .Append(facts.SoldierAppeasementRequired ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_7f338b1b8ab2"))
            .Append(facts.SoldierAppeasementApplied ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_42ef13944d10"))
            .Append(SiegeCastlePrisonerDispositionKindProfile.Describe(facts.PendingProposalForSpeaker))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_7325732af81d"))
            .Append(facts.SpeakerTrust)
            .Append(facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord
                ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_db2b039a6f63")
                : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_8fee467261c6"))
            .Append(facts.TerminalActionForTarget == SiegeCastleActionKind.Unknown ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_7409a608060f") : facts.TerminalActionForTarget.ToString())
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_b68dcfbaa0f9"))
            .Append((facts.PlayerText ?? string.Empty).Trim())
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_7bf8e3d9fda3"))
            .Append(hasCompoundAuthorization ? DescribeCompoundPlan(compoundPlan) : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_52e83fa75e7e"))
            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_f373e742b3f8"));

        if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.CapturedLord)
        {
            SiegeCastleLordRecruitmentBranch playerTextBranch = SiegeCastleLordRecruitmentBranchProfile.Resolve(
                facts.SpeakerIsClanLeader,
                facts.PlayerHasKingdom,
                facts.PlayerRulesKingdom,
                facts.PlayerText);
            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_e03b51246f99")).Append(facts.SpeakerIsClanLeader ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
                .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_cc945007de1b")).Append(facts.PlayerHasKingdom ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
                .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_541d766c068e")).Append(facts.PlayerRulesKingdom ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"))
                .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_92f5f2920e31")).Append(SiegeCastleLordRecruitmentBranchProfile.Describe(playerTextBranch)).Append("。");
            if (!facts.SpeakerIsClanLeader)
            {
                sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_62fcc9a7ccbf"));
            }
            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_fd67cc678c50"));
            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_6a3b8d1485e9"));
            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_5222e78013a6"))
                .Append(SiegeCastleLordDuelProfile.BuildEquipmentFact(
                    facts.PlayerIsMounted,
                    facts.PlayerCarriesRangedWeapon,
                    facts.PlayerWieldsRangedWeapon))
                .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_5aa6d9f54516"));
        }

        if (facts.SpeakerCultureMatchesCastle)
        {
            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_7d12f3048876"));
        }

        return sb.ToString();
    }

    private static string DescribeCompoundPlan(SiegeCastleCompoundDispositionPlan plan)
        => DescribeCompoundPlan(plan, false);

    private static string DescribeCompoundPlan(SiegeCastleCompoundDispositionPlan plan, bool isEnglish)
    {
        StringBuilder sb = new StringBuilder(isEnglish ? "Plan: " : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_context_profile_c1d515230cc7"));
        for (int i = 0; i < plan.Steps.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(isEnglish ? ", " : "、");
            }
            sb.Append(SiegeCastlePrisonerDispositionKindProfile.Describe(plan.Steps[i].Disposition, isEnglish));
        }
        return sb.Append(isEnglish ? ";" : "）").ToString();
    }
}

public sealed class SiegeCastlePostprocessContextFacts
{
    public SiegeCastlePostprocessContextFacts(
        string castleName,
        SiegeCastleActionSpeakerRole speakerRole,
        bool replyIsDirectPlayerResponse,
        int remainingRegularPrisoners,
        int recruitedRegularPrisoners,
        int slaughteredRegularPrisoners,
        bool soldierAppeasementRequired,
        bool soldierAppeasementApplied,
        bool speakerCultureMatchesCastle,
        string playerText = null,
        SiegeCastlePrisonerDispositionKind pendingProposalForSpeaker = SiegeCastlePrisonerDispositionKind.None,
        int speakerTrust = SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust,
        SiegeCastleActionKind terminalActionForTarget = SiegeCastleActionKind.Unknown,
        bool speakerIsClanLeader = false,
        bool playerHasKingdom = false,
        bool playerRulesKingdom = false,
        bool isWitnessReaction = false,
        SiegeCastleActionKind reactionToAction = SiegeCastleActionKind.Unknown,
        bool playerIsMounted = false,
        bool playerCarriesRangedWeapon = false,
        bool playerWieldsRangedWeapon = false)
    {
        CastleName = castleName ?? string.Empty;
        SpeakerRole = speakerRole;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        RemainingRegularPrisoners = ClampCount(remainingRegularPrisoners);
        RecruitedRegularPrisoners = ClampCount(recruitedRegularPrisoners);
        SlaughteredRegularPrisoners = ClampCount(slaughteredRegularPrisoners);
        SoldierAppeasementRequired = soldierAppeasementRequired;
        SoldierAppeasementApplied = soldierAppeasementApplied;
        SpeakerCultureMatchesCastle = speakerCultureMatchesCastle;
        PlayerText = playerText ?? string.Empty;
        PendingProposalForSpeaker = pendingProposalForSpeaker;
        SpeakerTrust = SiegeCastlePrisonerTrustProfile.Clamp(speakerTrust);
        TerminalActionForTarget = terminalActionForTarget;
        SpeakerIsClanLeader = speakerIsClanLeader;
        PlayerHasKingdom = playerHasKingdom;
        PlayerRulesKingdom = playerRulesKingdom;
        IsWitnessReaction = isWitnessReaction;
        ReactionToAction = reactionToAction;
        PlayerIsMounted = playerIsMounted;
        PlayerCarriesRangedWeapon = playerCarriesRangedWeapon;
        PlayerWieldsRangedWeapon = playerWieldsRangedWeapon;
    }

    public static SiegeCastlePostprocessContextFacts Empty => new SiegeCastlePostprocessContextFacts(
        string.Empty,
        SiegeCastleActionSpeakerRole.Unknown,
        replyIsDirectPlayerResponse: false,
        remainingRegularPrisoners: 0,
        recruitedRegularPrisoners: 0,
        slaughteredRegularPrisoners: 0,
        soldierAppeasementRequired: false,
        soldierAppeasementApplied: false,
        speakerCultureMatchesCastle: false,
        playerText: string.Empty,
        pendingProposalForSpeaker: SiegeCastlePrisonerDispositionKind.None);

    public string CastleName { get; }

    public SiegeCastleActionSpeakerRole SpeakerRole { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public int RemainingRegularPrisoners { get; }

    public int RecruitedRegularPrisoners { get; }

    public int SlaughteredRegularPrisoners { get; }

    public bool SoldierAppeasementRequired { get; }

    public bool SoldierAppeasementApplied { get; }

    public bool SpeakerCultureMatchesCastle { get; }

    public string PlayerText { get; }

    public SiegeCastlePrisonerDispositionKind PendingProposalForSpeaker { get; }

    public int SpeakerTrust { get; }

    public SiegeCastleActionKind TerminalActionForTarget { get; }

    public bool SpeakerIsClanLeader { get; }

    public bool PlayerHasKingdom { get; }

    public bool PlayerRulesKingdom { get; }

    public bool IsWitnessReaction { get; }

    public SiegeCastleActionKind ReactionToAction { get; }

    public bool PlayerIsMounted { get; }

    public bool PlayerCarriesRangedWeapon { get; }

    public bool PlayerWieldsRangedWeapon { get; }

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
