namespace TaleWorlds.MountAndBlade.View.MissionViews;

public abstract class MissionBattleUIBaseView : MissionView
{
	public bool IsViewCreated { get; private set; }

	protected abstract void OnCreateView();

	protected abstract void OnDestroyView();

	private void OnEnableView()
	{
		OnCreateView();
		IsViewCreated = true;
	}

	private void OnDisableView()
	{
		OnDestroyView();
		IsViewCreated = false;
	}

	protected abstract override void OnSuspendView();

	protected abstract override void OnResumeView();

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		if (GameNetwork.IsMultiplayer)
		{
			OnEnableView();
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!GameNetwork.IsMultiplayer && !MBCommon.IsPaused)
		{
			if (!IsViewCreated && !BannerlordConfig.HideBattleUI)
			{
				OnEnableView();
			}
			else if (IsViewCreated && BannerlordConfig.HideBattleUI)
			{
				OnDisableView();
			}
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		if (IsViewCreated)
		{
			OnDisableView();
		}
	}
}
