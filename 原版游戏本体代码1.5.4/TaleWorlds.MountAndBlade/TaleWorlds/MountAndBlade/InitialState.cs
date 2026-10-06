using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class InitialState : GameState
{
	public override bool IsMusicMenuState => true;

	public event OnInitialMenuOptionInvokedDelegate OnInitialMenuOptionInvoked;

	public event OnGameContentUpdatedDelegate OnGameContentUpdated;

	protected override void OnActivate()
	{
		base.OnActivate();
		MBMusicManager.Current?.UnpauseMusicManagerSystem();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
	}

	public void OnExecutedInitialStateOption(InitialStateOption target)
	{
		this.OnInitialMenuOptionInvoked?.Invoke(target);
	}

	public void RefreshContentState()
	{
		this.OnGameContentUpdated?.Invoke();
	}
}
