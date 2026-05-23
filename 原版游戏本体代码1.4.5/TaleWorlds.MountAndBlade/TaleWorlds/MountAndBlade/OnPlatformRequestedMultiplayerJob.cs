using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

internal class OnPlatformRequestedMultiplayerJob : Job
{
	public override void DoJob(float dt)
	{
		base.DoJob(dt);
		if (MBGameManager.Current != null)
		{
			MBGameManager.Current.OnPlatformRequestedMultiplayer();
		}
		else if (GameStateManager.Current != null && GameStateManager.Current.ActiveState != null)
		{
			GameStateManager.Current.CleanStates();
		}
		base.Finished = true;
	}
}
