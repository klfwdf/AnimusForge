using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            SceneActionsLog.Initialize(typeof(SubModule).Assembly.Location);

            try
            {
                SceneActionsRuntimeHost.Initialize();
                BattleSpeechRuntimeHost.Initialize(SceneActionsRuntimeHost.ModuleRoot);
                if (SceneActionsAfBridgeHost.TryInstall(out string reason))
                {
                    SceneActionsLog.Info(
                        "BOOT",
                        "AF bridge installed: " +
                        SceneActionsAfBridgeHost.ActiveBridgeId + ". " + reason);
                    InformationManager.DisplayMessage(new InformationMessage(
                        SceneActionsText.Loaded().ToString(),
                        Colors.Green));
                }
                else
                {
                    SceneActionsLog.Warning("BOOT", "Action input disabled: " + reason);
                    BattleSpeechRuntimeHost.Shutdown();
                    SceneActionsRuntimeHost.Shutdown();
                    InformationManager.DisplayMessage(new InformationMessage(
                        SceneActionsText.Disabled().ToString(),
                        Colors.Yellow));
                }
            }
            catch (Exception ex)
            {
                SceneActionsAfBridgeHost.Uninstall();
                BattleSpeechRuntimeHost.Shutdown();
                SceneActionsRuntimeHost.Shutdown();
                SceneActionsLog.Error("BOOT", "Module initialization failed.", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    SceneActionsText.InitializationFailed(ex.Message).ToString(),
                    Colors.Red));
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            if (!SceneActionsRuntimeHost.IsInitialized || mission == null)
            {
                return;
            }

            try
            {
                if (BattleSpeechRuntimeHost.IsInitialized &&
                    !BattleSpeechRuntimeHost.RefreshMcmOverrides(out string mcmError))
                {
                    SceneActionsLog.Warning(
                        "BATTLE_SPEECH_MCM",
                        "Mission registration skipped invalid MCM overrides. " + mcmError);
                }
                if (mission.GetMissionBehavior<SceneActionsMissionBehavior>() == null)
                {
                    mission.AddMissionBehavior(new SceneActionsMissionBehavior());
                    SceneActionsLog.Info(
                        "SESSION",
                        "SceneActions behavior registered before engine behavior initialization.");
                }
                else
                {
                    SceneActionsLog.Warning(
                        "SESSION",
                        "SceneActions behavior was already registered before engine initialization.");
                }

                if (BattleSpeechRuntimeHost.IsInitialized &&
                    BattleSpeechRuntimeHost.ConfigurationValid &&
                    BattleSpeechRuntimeHost.Settings.Enabled &&
                    mission.GetMissionBehavior<BattleSpeechMissionBehavior>() == null)
                {
                    mission.AddMissionBehavior(new BattleSpeechMissionBehavior());
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH",
                        "Battle speech behavior registered before engine behavior initialization.");
                }
                if (BattleSpeechRuntimeHost.IsInitialized &&
                    BattleSpeechRuntimeHost.ConfigurationValid &&
                    BattleSpeechRuntimeHost.Settings.Enabled &&
                    BattleSpeechRuntimeHost.PerformanceConfigurationValid &&
                    BattleSpeechRuntimeHost.PerformanceSettings.Enabled &&
                    mission.GetMissionBehavior<BattleSpeechPerformanceMissionBehavior>() == null)
                {
                    mission.AddMissionBehavior(new BattleSpeechPerformanceMissionBehavior());
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_PERFORMANCE",
                        "Battle speech performance behavior registered before engine behavior initialization.");
                }
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "SESSION",
                    "Mission behavior pre-initialization registration failed closed.",
                    ex);
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            if (!SceneActionsRuntimeHost.IsInitialized || mission == null)
            {
                return;
            }

            try
            {
                SceneActionsMissionBehavior behavior =
                    mission.GetMissionBehavior<SceneActionsMissionBehavior>();
                if (behavior == null || !behavior.IsSessionActive)
                {
                    SceneActionsLog.Error(
                        "SESSION",
                        "SceneActions behavior is unavailable after initialization; this Mission remains fail-closed.");
                    return;
                }

                if (BattleSpeechRuntimeHost.IsInitialized &&
                    BattleSpeechRuntimeHost.ConfigurationValid &&
                    BattleSpeechRuntimeHost.Settings.Enabled)
                {
                    BattleSpeechMissionBehavior battleSpeech =
                        mission.GetMissionBehavior<BattleSpeechMissionBehavior>();
                    if (battleSpeech == null || !battleSpeech.IsSessionActive)
                    {
                        SceneActionsLog.Error(
                            "BATTLE_SPEECH",
                            "Battle speech behavior did not activate; this Mission remains fail-closed for speeches.");
                        return;
                    }
                    if (BattleSpeechRuntimeHost.PerformanceConfigurationValid &&
                        BattleSpeechRuntimeHost.PerformanceSettings.Enabled)
                    {
                        BattleSpeechPerformanceMissionBehavior performance =
                            mission.GetMissionBehavior<BattleSpeechPerformanceMissionBehavior>();
                        if (performance == null || !performance.IsSessionActive)
                        {
                            SceneActionsLog.Error(
                                "BATTLE_SPEECH_PERFORMANCE",
                                "Battle speech performance behavior did not activate; performance remains fail-closed.");
                            return;
                        }
                    }
                }

                SceneActionsLog.Info(
                    "SESSION",
                    "Mission behavior post-initialization verification passed.");
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "SESSION",
                    "Mission behavior post-initialization verification failed closed.",
                    ex);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                SceneActionsAfBridgeHost.Uninstall();
                BattleSpeechRuntimeHost.Shutdown();
                SceneActionsRuntimeHost.Shutdown();
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("BOOT", "Module shutdown failed.", ex);
            }

            base.OnSubModuleUnloaded();
        }
    }
}
