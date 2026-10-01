using System;
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
		Try("Illustrator", global::AnimusForge.Illustrator.SubModule.Start);
		Try("DialogueUI", global::AnimusForge.DialogueUI.SubModule.Start);
		Try("Coup", global::AnimusForge.Coup.SubModule.Start);
	}

	public static void InstallDialoguePresentation()
	{
		Try("DialogueUI", global::AnimusForge.DialogueUI.SubModule.InstallPresentation);
	}

	public static void RegisterCampaign(IGameStarter starterObject)
	{
		Try("Illustrator", () => global::AnimusForge.Illustrator.SubModule.RegisterCampaign(starterObject));
		Try("Coup", () => global::AnimusForge.Coup.SubModule.RegisterCampaign(starterObject));
	}

	public static void Tick(float dt)
	{
		global::AnimusForge.DialogueUI.SubModule.Tick(dt);
		global::AnimusForge.Illustrator.SubModule.Tick(dt);
		global::AnimusForge.Coup.SubModule.Tick(dt);
	}

	public static void Shutdown()
	{
		Try("DialogueUI", global::AnimusForge.DialogueUI.SubModule.Shutdown);
		Try("Illustrator", global::AnimusForge.Illustrator.SubModule.Shutdown);
		Try("Coup", global::AnimusForge.Coup.SubModule.Shutdown);
	}

	private static void Try(string name, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log(name, "Integrated module call failed: " + ex);
			}
			catch
			{
			}
		}
	}
}
