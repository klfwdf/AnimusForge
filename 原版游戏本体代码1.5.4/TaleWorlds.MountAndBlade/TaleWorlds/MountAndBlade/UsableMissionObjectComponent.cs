using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public abstract class UsableMissionObjectComponent
{
	protected internal virtual void OnAdded(Scene scene)
	{
	}

	protected internal virtual void OnRemoved()
	{
	}

	protected internal virtual void OnFocusGain(Agent userAgent)
	{
	}

	protected internal virtual void OnFocusLose(Agent userAgent)
	{
	}

	public virtual bool IsOnTickRequired()
	{
		return false;
	}

	protected internal virtual void OnTick(float dt)
	{
	}

	protected internal virtual void OnEditorTick(float dt)
	{
	}

	protected internal virtual void OnEditorValidate()
	{
	}

	protected internal virtual void OnUse(Agent userAgent)
	{
	}

	protected internal virtual void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
	}

	protected internal virtual void OnMissionReset()
	{
	}

	protected internal virtual void OnMissionObjectDisabled()
	{
	}
}
