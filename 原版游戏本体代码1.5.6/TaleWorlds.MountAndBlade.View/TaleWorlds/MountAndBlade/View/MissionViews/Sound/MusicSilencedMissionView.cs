namespace TaleWorlds.MountAndBlade.View.MissionViews.Sound;

public class MusicSilencedMissionView : MissionView, IMusicHandler
{
	bool IMusicHandler.IsPausable => true;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.OnSilencedMusicHandlerInit(this);
	}

	public override void OnMissionScreenFinalize()
	{
		MBMusicManager.Current.OnSilencedMusicHandlerFinalize();
	}

	void IMusicHandler.OnUpdated(float dt)
	{
	}
}
