using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// The field camp mission has no CampaignMissionComponent, so the campaign
/// mission-started/ended events never fire there. The camp calls these two entries
/// itself (once each per camp mission) so scene shout gets its mission behaviour,
/// the overhead speech-bubble view and a fresh per-mission history session, exactly
/// as in towns and villages.
/// </summary>
public partial class ShoutBehavior
{
	internal static void NotifyFieldCampMissionStartedForExternal(Mission mission)
	{
		ShoutBehavior shout = CurrentInstance;
		if (shout == null || mission == null || mission.GetMissionBehavior<ShoutMissionBehavior>() != null)
		{
			return;
		}
		shout.OnMissionStarted(mission);
	}

	internal static void NotifyFieldCampMissionEndedForExternal(Mission mission)
	{
		CurrentInstance?.OnMissionEnded(mission);
	}
}
