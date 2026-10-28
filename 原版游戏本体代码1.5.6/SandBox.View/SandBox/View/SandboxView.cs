using TaleWorlds.ScreenSystem;

namespace SandBox.View;

public abstract class SandboxView
{
	public bool IsFinalized { get; protected set; }

	public ScreenLayer Layer { get; protected set; }

	protected internal virtual void OnActivate()
	{
	}

	protected internal virtual void OnDeactivate()
	{
	}

	protected internal virtual void OnInitialize()
	{
	}

	protected internal virtual void OnFinalize()
	{
		IsFinalized = true;
	}

	protected internal virtual void OnFrameTick(float dt)
	{
	}
}
