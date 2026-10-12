namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free SETS policy for an AI-proposed or player-ordered massacre inside an owned or ruler-attached settlement.
/// AF/SETS adapters still own live mission combat, agent selection, team changes, and shout dispatch.
/// </summary>
public static class SetsOwnedSettlementMassacreProfile
{
    public const int MaxAlliedAttackers = 100;

    public const string RequestActionTag = "[ACTION:SETS_REQUEST_MASSACRE]";

    public const string StartActionTag = "[ACTION:SETS_START_MASSACRE]";

    public const string StopActionTag = "[ACTION:SETS_STOP_MASSACRE]";

    public const string CancelRequestActionTag = "[ACTION:SETS_CANCEL_MASSACRE_REQUEST]";

    public const string RequestSource = "sets_owned_settlement_ai_massacre_request";

    public const string StartSource = "sets_owned_settlement_shout_massacre_start";

    public const string StopSource = "sets_owned_settlement_shout_massacre_stop";

    public const string CancelRequestSource = "sets_owned_settlement_massacre_request_cancel";

    public const uint PendingMessageColor = 0xFFFFD27Fu;

    public const uint StartMessageColor = 0xFFFF7777u;

    public const uint StopMessageColor = 0xFFB6F7A8u;

    public static bool ShouldOfferRequestRule(bool sceneActive, bool massacreActive, bool anyRequestPending, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && !anyRequestPending && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferStartRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferCancelRequestRule(bool sceneActive, bool massacreActive, bool requestPendingForSpeaker, bool speakerIsSelectedFollower)
    {
        return sceneActive && !massacreActive && requestPendingForSpeaker && speakerIsSelectedFollower;
    }

    public static bool ShouldOfferStopRule(bool sceneActive, bool massacreActive, bool speakerIsSelectedFollower)
    {
        return sceneActive && massacreActive && speakerIsSelectedFollower;
    }

    public static string BuildRuntimeInstruction(bool massacreActive, bool requestPending)
    {
        return BuildRuntimeInstruction(SetsSettlementSceneKind.Town, massacreActive, requestPending, isEnglish: false);
    }

    public static string BuildRuntimeInstruction(bool massacreActive, bool requestPending, bool isEnglish)
    {
        return BuildRuntimeInstruction(SetsSettlementSceneKind.Town, massacreActive, requestPending, isEnglish);
    }

    public static string BuildRuntimeInstruction(SetsSettlementSceneKind kind, bool massacreActive, bool requestPending)
    {
        return BuildRuntimeInstruction(kind, massacreActive, requestPending, isEnglish: false);
    }

    public static string BuildRuntimeInstruction(SetsSettlementSceneKind kind, bool massacreActive, bool requestPending, bool isEnglish)
        => BuildRuntimeInstruction(kind, massacreActive, requestPending, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildRuntimeInstruction(SetsSettlementSceneKind kind, bool massacreActive, bool requestPending, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, text.IsEnglish);
        string template = massacreActive ? text.SetsMassacreActivePromptTemplate : (requestPending ? text.SetsMassacreProposalPromptTemplate : text.SetsMassacreRuleDescription);
        return TownPromptTextCatalog.FormatTemplate(template, ("noun", noun), ("noun_upper", noun.ToUpperInvariant()));
    }

    public static string BuildRequestRuleDescription()
    {
        return BuildRequestRuleDescription(SetsSettlementSceneKind.Town, isEnglish: false);
    }

    public static string BuildRequestRuleDescription(bool isEnglish)
    {
        return BuildRequestRuleDescription(SetsSettlementSceneKind.Town, isEnglish);
    }

    public static string BuildRequestRuleDescription(SetsSettlementSceneKind kind)
    {
        return BuildRequestRuleDescription(kind, isEnglish: false);
    }

    public static string BuildRequestRuleDescription(SetsSettlementSceneKind kind, bool isEnglish)
        => BuildRequestRuleDescription(kind, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildRequestRuleDescription(SetsSettlementSceneKind kind, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        bool isEnglish = text.IsEnglish;
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);

        return text.GetRuntimeText("Sets_BuildRequestRuleDescription_001") + noun + text.GetRuntimeText("Sets_BuildRequestRuleDescription_002");


    }

    public static string BuildStartRuleDescription(bool requestPendingForSpeaker)
    {
        return BuildStartRuleDescription(SetsSettlementSceneKind.Town, requestPendingForSpeaker, isEnglish: false);
    }

    public static string BuildStartRuleDescription(bool requestPendingForSpeaker, bool isEnglish)
    {
        return BuildStartRuleDescription(SetsSettlementSceneKind.Town, requestPendingForSpeaker, isEnglish);
    }

    public static string BuildStartRuleDescription(SetsSettlementSceneKind kind, bool requestPendingForSpeaker)
    {
        return BuildStartRuleDescription(kind, requestPendingForSpeaker, isEnglish: false);
    }

    public static string BuildStartRuleDescription(SetsSettlementSceneKind kind, bool requestPendingForSpeaker, bool isEnglish)
        => BuildStartRuleDescription(kind, requestPendingForSpeaker, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildStartRuleDescription(SetsSettlementSceneKind kind, bool requestPendingForSpeaker, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        bool isEnglish = text.IsEnglish;
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);

        return requestPendingForSpeaker
            ? text.GetRuntimeText("Sets_BuildStartRuleDescription_001")
            : text.GetRuntimeText("Sets_BuildStartRuleDescription_002") + noun + text.GetRuntimeText("Sets_BuildStartRuleDescription_003");


    }

    public static string BuildCancelRequestRuleDescription()
    {
        return BuildCancelRequestRuleDescription(isEnglish: false);
    }

    public static string BuildCancelRequestRuleDescription(bool isEnglish)
        => BuildCancelRequestRuleDescription(TownPromptCatalogResources.Get(isEnglish));

    public static string BuildCancelRequestRuleDescription(TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);

        return text.GetRuntimeText("Sets_BuildCancelRequestRuleDescription_001");


    }

    public static string BuildStopRuleDescription()
    {
        return BuildStopRuleDescription(isEnglish: false);
    }

    public static string BuildStopRuleDescription(bool isEnglish)
        => BuildStopRuleDescription(TownPromptCatalogResources.Get(isEnglish));

    public static string BuildStopRuleDescription(TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);

        return text.GetRuntimeText("Sets_BuildStopRuleDescription_001");


    }

    public static string BuildStartedMessage(int attackerCount, int targetCount)
    {
        return BuildStartedMessage(SetsSettlementSceneKind.Town, attackerCount, targetCount, isEnglish: false);
    }

    public static string BuildStartedMessage(int attackerCount, int targetCount, bool isEnglish)
    {
        return BuildStartedMessage(SetsSettlementSceneKind.Town, attackerCount, targetCount, isEnglish);
    }

    public static string BuildStartedMessage(SetsSettlementSceneKind kind, int attackerCount, int targetCount)
    {
        return BuildStartedMessage(kind, attackerCount, targetCount, isEnglish: false);
    }

    public static string BuildStartedMessage(SetsSettlementSceneKind kind, int attackerCount, int targetCount, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        int attackers = attackerCount < 0 ? 0 : attackerCount;
        int targets = targetCount < 0 ? 0 : targetCount;
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);

        return text.GetRuntimeText("Sets_BuildStartedMessage_001") + attackers + text.GetRuntimeText("Sets_BuildStartedMessage_002") + targets + text.GetRuntimeText("Sets_BuildStartedMessage_003") + noun + text.GetRuntimeText("Sets_BuildStartedMessage_004");

    }

    public static string BuildPendingRequestMessage(string speakerName)
    {
        return BuildPendingRequestMessage(SetsSettlementSceneKind.Town, speakerName, isEnglish: false);
    }

    public static string BuildPendingRequestMessage(string speakerName, bool isEnglish)
    {
        return BuildPendingRequestMessage(SetsSettlementSceneKind.Town, speakerName, isEnglish);
    }

    public static string BuildPendingRequestMessage(SetsSettlementSceneKind kind, string speakerName)
    {
        return BuildPendingRequestMessage(kind, speakerName, isEnglish: false);
    }

    public static string BuildPendingRequestMessage(SetsSettlementSceneKind kind, string speakerName, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        string resolvedName = string.IsNullOrWhiteSpace(speakerName)
            ? text.GetRuntimeText("SetsEscortName")
            : speakerName.Trim();
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);

        return text.GetRuntimeText("Sets_BuildPendingRequestMessage_001") + resolvedName + text.GetRuntimeText("Sets_BuildPendingRequestMessage_002") + noun + text.GetRuntimeText("Sets_BuildPendingRequestMessage_003");

    }

    public static string BuildCancelledRequestMessage()
    {
        return BuildCancelledRequestMessage(isEnglish: false);
    }

    public static string BuildCancelledRequestMessage(bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);

        return text.GetRuntimeText("Sets_BuildCancelledRequestMessage_001");

    }

    public static string BuildStoppedMessage(int survivingTargetCount)
    {
        return BuildStoppedMessage(survivingTargetCount, isEnglish: false);
    }

    public static string BuildStoppedMessage(int survivingTargetCount, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        int survivors = survivingTargetCount < 0 ? 0 : survivingTargetCount;

        return text.GetRuntimeText("Sets_BuildStoppedMessage_001") + survivors + text.GetRuntimeText("Sets_BuildStoppedMessage_002");

    }

    public static string BuildCompletedMessage()
    {
        return BuildCompletedMessage(SetsSettlementSceneKind.Town, isEnglish: false);
    }

    public static string BuildCompletedMessage(bool isEnglish)
    {
        return BuildCompletedMessage(SetsSettlementSceneKind.Town, isEnglish);
    }

    public static string BuildCompletedMessage(SetsSettlementSceneKind kind)
    {
        return BuildCompletedMessage(kind, isEnglish: false);
    }

    public static string BuildCompletedMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);

        return text.GetRuntimeText("Sets_BuildCompletedMessage_001") + noun + text.GetRuntimeText("Sets_BuildCompletedMessage_002");

    }
}
