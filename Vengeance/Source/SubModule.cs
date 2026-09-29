using RichExecutions.Campaign;
using RichExecutions.Customization;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions;

public sealed class SubModule : MBSubModuleBase
{
    private bool _embeddedHostOwnsSession;

    private static bool IsHostedByAnimusForge()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, "AnimusForge", StringComparison.Ordinal)) continue;
            PropertyInfo property = assembly.GetType("AnimusForge.IntegratedModuleHost")?.GetProperty("OwnsVengeance", BindingFlags.Public | BindingFlags.Static);
            if (property == null) return false;
            try { return property.GetValue(null) is true; }
            catch (Exception exception)
            {
                RexLog.Warning("Could not read the AnimusForge execution claim; standalone registration disabled: " + exception.Message);
                return true;
            }
        }
        return false;
    }

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        if (VengeanceIntegration.IsEmbeddedHostActive || IsHostedByAnimusForge() || !VengeanceIntegration.IsEnabled)
        {
            RexLog.Info(
                "Standalone Vengeance registration skipped because the AnimusForge-hosted feature is active.");
            return;
        }
#if BANNERLORD_1_4_8
        RexLog.Info("SubModule loaded for Bannerlord 1.4.8.");
#else
        RexLog.Info("SubModule loaded for Bannerlord 1.4.7.");
#endif
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);
        _embeddedHostOwnsSession = IsHostedByAnimusForge();

        if (!VengeanceIntegration.IsEnabled ||
            VengeanceIntegration.IsEmbeddedHostActive ||
            _embeddedHostOwnsSession ||
            game.GameType is not BannerlordCampaign ||
            gameStarterObject is not CampaignGameStarter starter)
        {
            return;
        }

        RichExecutionApi.InitializeDefaults();
        ExecutionSitePresetStore.EnsureBuiltInPresetsForAllMethods();
        starter.AddModel(new ScopedExecutionRelationModel());
        starter.AddBehavior(new RichExecutionCampaignBehavior(
            RichExecutionApi.Service,
            RichExecutionApi.Methods,
            RichExecutionApi.Charges));
        RexLog.Info(
            "Registered the campaign public-execution behavior and kingdom-scoped execution relation model.");
    }

    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        base.OnMissionBehaviorInitialize(mission);

        if (!VengeanceIntegration.IsEnabled ||
            VengeanceIntegration.IsEmbeddedHostActive ||
            _embeddedHostOwnsSession ||
            IsHostedByAnimusForge() ||
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
            RexLog.Error("Could not inject the public-execution mission behaviors.", exception);
            ExecutionSessionCoordinator.CancelPending(
                ExecutionFailureReason.ScenePlacementFailed,
                new TextObject(
                    "{=REX_Error_Mission_Inject}The execution scene could not be initialized safely. Nothing was spent and the prisoner lives."));
        }
    }
}
