using System;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
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
            RexLog.Info("Vengeance claimed the AnimusForge-hosted feature.");
        }
        catch
        {
            ExecutionContinuation.EscortedHeroesProvider = null;
            VengeanceIntegration.ReleaseEmbeddedHost();
            throw;
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

        RichExecutionApi.InitializeDefaults();
        ExecutionSitePresetStore.EnsureBuiltInPresetsForAllMethods();
        starter.AddModel(new ScopedExecutionRelationModel());
        starter.AddBehavior(new RichExecutions.Campaign.RichExecutionCampaignBehavior(
            RichExecutionApi.Service,
            RichExecutionApi.Methods,
            RichExecutionApi.Charges));
        RexLog.Info("Registered embedded Vengeance campaign behavior.");
    }

    internal static void Shutdown()
    {
        if (!VengeanceIntegration.IsEmbeddedHostActive)
        {
            return;
        }

        ExecutionAddressLlm.Unregister();
        ExecutionContinuation.EscortedHeroesProvider = null;
        VengeanceIntegration.ReleaseEmbeddedHost();
        RexLog.Info("Released the AnimusForge-hosted Vengeance feature.");
    }
}
