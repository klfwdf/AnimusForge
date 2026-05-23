using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.KillFeed;

public class MultiplayerGeneralKillFeedItemWidget : Widget
{
	private const float FadeInTime = 0.15f;

	private const float StayTime = 3.5f;

	private const float FadeOutTime = 1f;

	private float _speedModifier = 1f;

	private bool _initialized;

	private float CurrentAlpha => base.AlphaFactor;

	public float TimeSinceCreation { get; private set; }

	public MultiplayerGeneralKillFeedItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			this.SetGlobalAlphaRecursively(0f);
			_initialized = true;
		}
		TimeSinceCreation += dt * _speedModifier;
		if (TimeSinceCreation <= 0.15f)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(CurrentAlpha, 1f, TimeSinceCreation / 0.15f));
		}
		else if (TimeSinceCreation - 0.15f <= 3.5f)
		{
			this.SetGlobalAlphaRecursively(1f);
		}
		else if (TimeSinceCreation - 3.65f <= 1f)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(CurrentAlpha, 0f, (TimeSinceCreation - 3.65f) / 1f));
			if (CurrentAlpha <= 0.1f)
			{
				EventFired("OnRemove");
			}
		}
		else
		{
			EventFired("OnRemove");
		}
	}

	public void SetSpeedModifier(float newSpeed)
	{
		if (newSpeed > _speedModifier)
		{
			_speedModifier = newSpeed;
		}
	}
}
