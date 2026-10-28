namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class MissionConversationView : MissionView
{
	public static MissionConversationView Current => Mission.Current.GetMissionBehavior<MissionConversationView>();
}
