using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Screens;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public abstract class MissionView : MissionBehavior
{
	public int ViewOrderPriority;

	public MissionScreen MissionScreen { get; internal set; }

	public IInputContext Input => MissionScreen.SceneLayer.Input;

	protected bool IsViewSuspended { get; private set; }

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public bool IsFinalized { get; internal set; }

	public virtual void OnMissionScreenTick(float dt)
	{
	}

	public virtual bool OnEscape()
	{
		return false;
	}

	public virtual bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return true;
	}

	public virtual bool IsPhotoModeAllowed()
	{
		return true;
	}

	public virtual void OnFocusChangeOnGameWindow(bool focusGained)
	{
	}

	public virtual void OnSceneRenderingStarted()
	{
	}

	public virtual void OnMissionScreenInitialize()
	{
	}

	public virtual void OnMissionScreenFinalize()
	{
	}

	public virtual void OnMissionScreenActivate()
	{
	}

	public virtual void OnMissionScreenDeactivate()
	{
	}

	public virtual bool UpdateOverridenCamera(float dt)
	{
		return false;
	}

	public virtual bool IsReady()
	{
		return true;
	}

	public virtual void OnPhotoModeActivated()
	{
	}

	public virtual void OnPhotoModeDeactivated()
	{
	}

	public virtual void OnConversationBegin()
	{
	}

	public virtual void OnConversationEnd()
	{
	}

	protected virtual void OnSuspendView()
	{
	}

	protected virtual void OnResumeView()
	{
	}

	public virtual void OnDeploymentPlanMade(Team team, bool isFirstPlan)
	{
	}

	public void SuspendView()
	{
		OnSuspendView();
		IsViewSuspended = true;
	}

	public void ResumeView()
	{
		OnResumeView();
		IsViewSuspended = false;
	}

	public sealed override void OnEndMissionInternal()
	{
		OnEndMission();
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
	}
}
