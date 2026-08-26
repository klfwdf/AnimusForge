using AnimusForge.SceneActions.Core;
using AnimusForge.XihaiAction;
using System;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class SceneActionsIntegrationBoundary
{
    internal const string ImportedStandaloneVersion = "v1.1.0";

    internal const bool RuntimeIntegrationEnabled = true;

    private static bool _runtimeInitialized;

    internal static bool TryValidateCoreContract(out string reason)
    {
        if (SceneActionFrameworkV1.LogicalActions.Count != 8 ||
            SceneActionFrameworkV2.LogicalActions.Count != 16 ||
            SceneActionFrameworkV3.LogicalActions.Count != 24 ||
            SceneActionFrameworkV4.LogicalActions.Count != 27)
        {
            reason = "SceneActions contract counts are not 8/16/24/27.";
            return false;
        }

        reason = "SceneActions V1-V4 core contract is available in AnimusForge.";
        return true;
    }

    internal static void InitializeRuntime()
    {
        if (_runtimeInitialized)
        {
            return;
        }

        string moduleRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
        SceneActionsLog.InitializeForModuleRoot(moduleRoot);
        try
        {
            if (!TryValidateCoreContract(out string contractReason))
            {
                SceneActionsLog.Warning("INTEGRATION", contractReason);
                return;
            }

            SceneActionsRuntimeHost.Initialize(moduleRoot);
            BattleSpeechRuntimeHost.Initialize(SceneActionsRuntimeHost.ModuleRoot);
            if (!SceneActionsAfBridgeHost.TryInstall(out string bridgeReason))
            {
                throw new InvalidOperationException("AF bridge unavailable: " + bridgeReason);
            }

            _runtimeInitialized = true;
            SceneActionsLog.Info(
                "INTEGRATION",
                "SceneActions runtime integrated into AnimusForge. " + contractReason +
                " Bridge=" + SceneActionsAfBridgeHost.ActiveBridgeId + ". " + bridgeReason);
        }
        catch (Exception ex)
        {
            ShutdownRuntime();
            SceneActionsLog.Error("INTEGRATION", "SceneActions initialization failed closed.", ex);
        }
    }

    internal static void RefreshMcmOverrides()
    {
        if (!_runtimeInitialized)
        {
            return;
        }

        try
        {
            if (SceneActionsRuntimeHost.IsInitialized &&
                !SceneActionsRuntimeHost.RefreshMcmOverrides(out string sceneActionsMcmError))
            {
                SceneActionsLog.Warning("SCENE_ACTIONS_MCM", sceneActionsMcmError);
            }
            if (BattleSpeechRuntimeHost.IsInitialized &&
                !BattleSpeechRuntimeHost.RefreshMcmOverrides(out string battleSpeechMcmError))
            {
                SceneActionsLog.Warning("BATTLE_SPEECH_MCM", battleSpeechMcmError);
            }
        }
        catch (Exception ex)
        {
            SceneActionsLog.Error("MCM", "Integrated SceneActions MCM refresh failed closed.", ex);
        }
    }
    internal static void RegisterBeforeMissionInitialization(Mission mission)
    {
        if (!_runtimeInitialized || !SceneActionsRuntimeHost.IsInitialized || mission == null)
        {
            return;
        }

        try
        {
            RefreshMcmOverrides();

            if (mission.GetMissionBehavior<SceneActionsMissionBehavior>() == null)
            {
                mission.AddMissionBehavior(new SceneActionsMissionBehavior());
            }
            if (BattleSpeechRuntimeHost.IsInitialized &&
                BattleSpeechRuntimeHost.ConfigurationValid &&
                mission.GetMissionBehavior<BattleSpeechMissionBehavior>() == null)
            {
                mission.AddMissionBehavior(new BattleSpeechMissionBehavior());
            }
            if (BattleSpeechRuntimeHost.IsInitialized &&
                BattleSpeechRuntimeHost.ConfigurationValid &&
                BattleSpeechRuntimeHost.PerformanceConfigurationValid &&
                mission.GetMissionBehavior<BattleSpeechPerformanceMissionBehavior>() == null)
            {
                mission.AddMissionBehavior(new BattleSpeechPerformanceMissionBehavior());
            }
        }
        catch (Exception ex)
        {
            SceneActionsLog.Error(
                "INTEGRATION",
                "Mission behavior registration failed closed.",
                ex);
        }
    }

    internal static void VerifyMissionInitialization(Mission mission)
    {
        if (!_runtimeInitialized || mission == null)
        {
            return;
        }

        SceneActionsMissionBehavior actions = mission.GetMissionBehavior<SceneActionsMissionBehavior>();
        if (actions == null || !actions.IsSessionActive)
        {
            SceneActionsLog.Warning("INTEGRATION", "SceneActions Mission behavior is inactive.");
            return;
        }

        if (BattleSpeechRuntimeHost.ConfigurationValid)
        {
            BattleSpeechMissionBehavior speech =
                mission.GetMissionBehavior<BattleSpeechMissionBehavior>();
            if (speech == null || !speech.IsSessionActive)
            {
                SceneActionsLog.Warning("INTEGRATION", "Battle speech Mission behavior is inactive.");
                return;
            }
        }
        SceneActionsLog.Info("INTEGRATION", "Mission behavior initialization verified.");
    }

    internal static void ShutdownRuntime()
    {
        try
        {
            SceneActionsAfBridgeHost.Uninstall();
            BattleSpeechRuntimeHost.Shutdown();
            SceneActionsRuntimeHost.Shutdown();
        }
        catch (Exception ex)
        {
            SceneActionsLog.Error("INTEGRATION", "SceneActions shutdown failed.", ex);
        }
        finally
        {
            _runtimeInitialized = false;
        }
    }
}
