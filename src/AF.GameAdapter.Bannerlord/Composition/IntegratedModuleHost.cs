using System;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Core;

namespace AnimusForge;

// Illustrator, Dialogue UI, and Coup run inside AnimusForge.dll.
// Their old module classes stay in the separate extension assemblies only as a no-op
// when these flags are visible, so a leftover launcher module does not patch twice.
public static class IntegratedModuleHost
{
	public static bool OwnsIllustrator => true;

	public static bool OwnsDialogueUi => true;

	public static bool OwnsCoup => true;

	public static bool OwnsVengeance => global::RichExecutions.Core.VengeanceIntegration.IsEmbeddedHostActive;

	public static void Start()
	{
		TryStart("Illustrator", HostedExtensionCatalog.Illustrator, global::AnimusForge.Illustrator.SubModule.TryStart);
		TryStart("DialogueUI", HostedExtensionCatalog.DialogueUi, global::AnimusForge.DialogueUI.SubModule.TryStart, awaitingPresentation: true);
		TryStart("Coup", HostedExtensionCatalog.Coup, global::AnimusForge.Coup.SubModule.TryStart);
	}

	public static void InstallDialoguePresentation()
	{
		TryStart("DialogueUI", HostedExtensionCatalog.DialogueUi, global::AnimusForge.DialogueUI.SubModule.TryInstallPresentation);
	}

	public static void RegisterCampaign(IGameStarter starterObject)
	{
		TryCampaign("Illustrator", HostedExtensionCatalog.Illustrator,
            () => global::AnimusForge.Illustrator.SubModule.RegisterCampaign(starterObject),
            starterObject is TaleWorlds.CampaignSystem.CampaignGameStarter);
		TryCampaign("Coup", HostedExtensionCatalog.Coup,
            () => global::AnimusForge.Coup.SubModule.RegisterCampaign(starterObject),
            starterObject is TaleWorlds.CampaignSystem.CampaignGameStarter);
	}

	public static void Tick(float dt)
	{
		global::AnimusForge.DialogueUI.SubModule.Tick(dt);
		global::AnimusForge.Illustrator.SubModule.Tick(dt);
		global::AnimusForge.Coup.SubModule.Tick(dt);
	}

	public static void Shutdown()
	{
		TryShutdown("DialogueUI", HostedExtensionCatalog.DialogueUi, global::AnimusForge.DialogueUI.SubModule.Shutdown);
		TryShutdown("Illustrator", HostedExtensionCatalog.Illustrator, global::AnimusForge.Illustrator.SubModule.Shutdown);
		TryShutdown("Coup", HostedExtensionCatalog.Coup, global::AnimusForge.Coup.SubModule.Shutdown);
	}

    private static void TryStart(string name, string moduleId, Func<bool> start, bool awaitingPresentation = false)
    {
        try
        {
            bool success = start();
            ModuleFrameworkRuntime.ReportHostedExtensionState(moduleId,
                success ? (awaitingPresentation ? InternalModuleRuntimeState.NotInitialized : InternalModuleRuntimeState.Ready)
                    : InternalModuleRuntimeState.Unavailable,
                success ? (awaitingPresentation ? "module.awaiting_presentation" : "module.host_started")
                    : "module.host_unavailable");
        }
        catch (Exception ex)
        {
            ModuleFrameworkRuntime.ReportHostedExtensionState(moduleId, InternalModuleRuntimeState.Failed,
                "module.host_start_failed");
            LogFailure(name, ex);
        }
    }

    private static void TryCampaign(string name, string moduleId, Action register, bool isCampaign)
    {
        try
        {
            register(); // Preserve the original owner and callback order, including non-campaign no-ops.
            if (isCampaign)
                ModuleFrameworkRuntime.ReportHostedExtensionState(moduleId, InternalModuleRuntimeState.Ready,
                    "module.campaign_registered", requireStarted: true);
        }
        catch (Exception ex)
        {
            ModuleFrameworkRuntime.ReportHostedExtensionState(moduleId, InternalModuleRuntimeState.Failed,
                "module.campaign_registration_failed", requireStarted: true);
            LogFailure(name, ex);
        }
    }

    private static void TryShutdown(string name, string moduleId, Action shutdown)
    {
        try { shutdown(); }
        catch (Exception ex) { LogFailure(name, ex); }
        finally
        {
            ModuleFrameworkRuntime.ReportHostedExtensionState(moduleId, InternalModuleRuntimeState.Unavailable,
                "module.host_stopped");
        }
    }

    private static void LogFailure(string name, Exception ex)
    {
        try { Logger.Log(name, "Integrated module call failed: " + ex); }
        catch { }
    }

}
