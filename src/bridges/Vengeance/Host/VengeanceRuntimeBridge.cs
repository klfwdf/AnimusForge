using System;
using System.Collections.Generic;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Thin AnimusForge host for the shared Vengeance source. The standalone module
/// sees the embedded claim and skips its own registration.
/// </summary>
internal static class VengeanceRuntimeBridge
{
    internal static void Initialize()
    {
        if (!VengeanceIntegration.TryClaimEmbeddedHost())
        {
            Logger.Log("Vengeance", "Embedded Vengeance was not claimed; standalone registration remains unchanged.");
            return;
        }

        try
        {
            ExecutionContinuation.EscortedHeroesProvider = NoblePrisonerEscortBehavior.GetEscortedHeroesForExecution;
            ExecutionAddressLlm.Register();
            SubscribeExecutionMemoryFacts();
            RexLog.Info("Vengeance claimed the AnimusForge-hosted feature.");
        }
        catch
        {
            UnsubscribeExecutionMemoryFacts();
            ExecutionContinuation.EscortedHeroesProvider = null;
            VengeanceIntegration.ReleaseEmbeddedHost();
            throw;
        }
    }

    // A public-execution death reaches AF only through vanilla HeroKilledEvent, which
    // carries no method or charge. DeathCommitting is raised synchronously right before
    // KillCharacterAction.ApplyByExecution, so the facts are parked per victim for the
    // HeroKilled handlers and dropped on Completed/Cancelled. One entry per execution.
    private static readonly object ExecutionFactsSync = new();
    private static readonly Dictionary<string, VengeanceExecutionFacts> PendingExecutionFacts = new(StringComparer.Ordinal);

    internal static VengeanceExecutionFacts TryGetPendingExecutionFacts(Hero victim)
    {
        string id = victim?.StringId;
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        lock (ExecutionFactsSync)
        {
            return PendingExecutionFacts.Count > 0 && PendingExecutionFacts.TryGetValue(id, out var facts) ? facts : null;
        }
    }

    private static void SubscribeExecutionMemoryFacts()
    {
        UnsubscribeExecutionMemoryFacts();
        ExecutionSpeechDirector.LineShown = (request, sequence, cue, speaker, witnesses) =>
            Campaign.Current?.GetCampaignBehavior<MyBehavior>()?.RecordExecutionSpeech(request, sequence, cue, speaker, witnesses);
        RichExecutionEvents.ExecutionDeathCommitting += OnExecutionDeathCommitting;
        RichExecutionEvents.ExecutionCompleted += OnExecutionCompleted;
        RichExecutionEvents.ExecutionCancelled += OnExecutionCancelled;
    }

    private static void UnsubscribeExecutionMemoryFacts()
    {
        ExecutionSpeechDirector.LineShown = null;
        RichExecutionEvents.ExecutionDeathCommitting -= OnExecutionDeathCommitting;
        RichExecutionEvents.ExecutionCompleted -= OnExecutionCompleted;
        RichExecutionEvents.ExecutionCancelled -= OnExecutionCancelled;
        lock (ExecutionFactsSync)
        {
            PendingExecutionFacts.Clear();
        }
    }

    private static void OnExecutionDeathCommitting(object sender, ExecutionDeathCommittingEventArgs args)
    {
        string id = args?.Request?.Victim?.StringId;
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        VengeanceExecutionFacts facts = VengeanceExecutionFacts.From(args.Request, args.Actor);
        lock (ExecutionFactsSync)
        {
            PendingExecutionFacts[id] = facts;
        }
    }

    private static void OnExecutionCompleted(object sender, ExecutionCompletedEventArgs args)
    {
        try { Campaign.Current?.GetCampaignBehavior<MyBehavior>()?.CompleteExecutionTranscript(args?.Outcome?.Request,
            args?.Outcome?.Success == true && args.Outcome.DeathCommitted, args?.Outcome?.Actor ?? ExecutionActor.Undecided); }
        finally { ForgetExecutionFacts(args?.Outcome?.Request?.Victim); }
    }

    private static void OnExecutionCancelled(object sender, ExecutionCancelledEventArgs args)
    {
        try { Campaign.Current?.GetCampaignBehavior<MyBehavior>()?.CompleteExecutionTranscript(args?.Request, false); }
        finally { ForgetExecutionFacts(args?.Request?.Victim); }
    }

    private static void ForgetExecutionFacts(Hero victim)
    {
        string id = victim?.StringId;
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        lock (ExecutionFactsSync)
        {
            PendingExecutionFacts.Remove(id);
        }
    }

    internal static void TryInjectMission(Mission mission)
    {
        if (!VengeanceIntegration.IsEmbeddedHostActive ||
            !VengeanceIntegration.IsEnabled ||
            !RichExecutionApi.IsInitialized ||
            !ExecutionSessionCoordinator.TryClaimForMission(mission, out var session))
        {
            return;
        }

        try
        {
            ExecutionSessionCoordinator.InjectClaimedSession(mission, session);
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not inject the hosted public-execution mission behaviors.", exception);
            ExecutionSessionCoordinator.CancelPending(
                ExecutionFailureReason.ScenePlacementFailed,
                new TextObject(
                    "{=REX_Error_Mission_Inject}The execution scene could not be initialized safely. Nothing was spent and the prisoner lives."));
        }
    }

    internal static void RegisterCampaign(IGameStarter starterObject)
    {
        if (!VengeanceIntegration.IsEmbeddedHostActive || starterObject is not CampaignGameStarter starter)
        {
            return;
        }

        // A Vengeance failure must not abort the host's campaign registration.
        // Preset files are prepared first; nothing is added to the starter
        // until every fallible step has succeeded.
        try
        {
            RichExecutionApi.InitializeDefaults();
            ExecutionSitePresetStore.EnsureBuiltInPresetsForAllMethods();
        }
        catch (Exception exception)
        {
            RexLog.Error("Embedded Vengeance setup failed; the feature stays off for this campaign.", exception);
            Logger.Log("Vengeance", "Embedded Vengeance setup failed; campaign continues without it: " + exception);
            return;
        }

        try
        {
            starter.AddModel(new ScopedExecutionRelationModel());
            starter.AddBehavior(new RichExecutions.Campaign.RichExecutionCampaignBehavior(
                RichExecutionApi.Service,
                RichExecutionApi.Methods,
                RichExecutionApi.Charges));
            RexLog.Info("Registered embedded Vengeance campaign behavior.");
        }
        catch (Exception exception)
        {
            RexLog.Error("Embedded Vengeance campaign registration failed.", exception);
            Logger.Log("Vengeance", "Embedded Vengeance campaign registration failed: " + exception);
        }
    }

    internal static void Shutdown()
    {
        if (!VengeanceIntegration.IsEmbeddedHostActive)
        {
            return;
        }

        ExecutionAddressLlm.Unregister();
        UnsubscribeExecutionMemoryFacts();
        ExecutionContinuation.EscortedHeroesProvider = null;
        VengeanceIntegration.ReleaseEmbeddedHost();
        RexLog.Info("Released the AnimusForge-hosted Vengeance feature.");
    }
}

/// <summary>
/// Frozen, AF-facing facts of one public execution. Labels are resolved to Chinese
/// by id so memory text does not depend on the game UI language.
/// </summary>
internal sealed class VengeanceExecutionFacts
{
    private VengeanceExecutionFacts(string methodLabel, string chargeLabel, string toneLabel, string legitimacyLabel, bool playerStruck, Settlement venue)
    {
        MethodLabel = methodLabel;
        ChargeLabel = chargeLabel;
        ToneLabel = toneLabel;
        LegitimacyLabel = legitimacyLabel;
        PlayerStruck = playerStruck;
        Venue = venue;
    }

    /// <summary>刑罚方式，例如“火刑”。</summary>
    internal string MethodLabel { get; }
    /// <summary>罪名，例如“叛乱或叛国”。</summary>
    internal string ChargeLabel { get; }
    internal string ToneLabel { get; }
    internal string LegitimacyLabel { get; }
    /// <summary>True when the executor struck the blow; false when the town executioner did.</summary>
    internal bool PlayerStruck { get; }
    internal Settlement Venue { get; }

    internal static VengeanceExecutionFacts From(ExecutionRequest request, ExecutionActor actor) =>
        new(
            ResolveMethodLabel(request.Method),
            ResolveChargeLabel(request.Charge),
            request.Tone switch
            {
                ExecutionTone.Spectacle => "盛大示众",
                ExecutionTone.Terror => "恐怖威慑",
                _ => "司法宣判"
            },
            request.LegitimacyTier switch
            {
                LegitimacyTier.Legal => "合法",
                LegitimacyTier.Disputed => "有争议",
                _ => "缺少合法授权"
            },
            actor == ExecutionActor.Player,
            request.Venue);

    // Mirrors CNs/vengeance_strings-zh-CN.xml; unknown extension ids fall back to the localized name.
    private static string ResolveMethodLabel(ExecutionMethodDefinition method) => (method?.StringId ?? "").Trim().ToLowerInvariant() switch
    {
        ExecutionMethodRules.Beheading => "斩首",
        ExecutionMethodRules.Hanging => "绞刑",
        ExecutionMethodRules.Burning => "火刑",
        ExecutionMethodRules.BreakingWheel => "轮刑",
        ExecutionMethodRules.Impalement => "穿刺刑",
        ExecutionMethodRules.Stoning => "石刑",
        ExecutionMethodRules.CrossbowExecution => "弩决",
        _ => SafeName(() => method?.GetName()?.ToString())
    };

    private static string ResolveChargeLabel(ExecutionChargeDefinition charge) => (charge?.StringId ?? "").Trim().ToLowerInvariant() switch
    {
        "treason" => "叛乱或叛国",
        "raiding_civilians" => "劫掠平民",
        "siege_atrocity" => "围城暴行",
        "banditry" => "盗匪罪",
        "public_enemy" => "王国公敌",
        "personal_revenge" => "私人复仇",
        _ => SafeName(() => charge?.GetName()?.ToString())
    };

    private static string SafeName(Func<string> read)
    {
        try
        {
            return (read() ?? "").Trim();
        }
        catch
        {
            return "";
        }
    }
}
