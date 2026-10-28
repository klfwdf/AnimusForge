namespace TaleWorlds.MountAndBlade.View.MissionViews;

[DefaultView]
public abstract class MissionCheatView : MissionView
{
	public abstract bool GetIsCheatsAvailable();

	public abstract void InitializeScreen();

	public abstract void FinalizeScreen();
}
