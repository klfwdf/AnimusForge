using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.DamageFeed;

public class MissionAgentDamageFeedItemWidget : Widget
{
	private float _speedModifier = 1f;

	private bool _isInitialized;

	private bool _isShown;

	public float FadeInTime { get; set; } = 0.1f;

	public float StayTime { get; set; } = 1.5f;

	public float FadeOutTime { get; set; } = 0.3f;

	public float TimeSinceCreation { get; private set; }

	public MissionAgentDamageFeedItemWidget(UIContext context)
		: base(context)
	{
	}

	public void ShowFeed()
	{
		_isShown = true;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_isInitialized)
		{
			this.SetGlobalAlphaRecursively(0f);
			_isInitialized = true;
		}
		if (!_isShown)
		{
			return;
		}
		TimeSinceCreation += dt * _speedModifier;
		if (TimeSinceCreation <= FadeInTime)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(base.AlphaFactor, 1f, TimeSinceCreation / FadeInTime));
		}
		else if (TimeSinceCreation - FadeInTime <= StayTime)
		{
			this.SetGlobalAlphaRecursively(1f);
		}
		else if (TimeSinceCreation - (FadeInTime + StayTime) <= FadeOutTime)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(base.AlphaFactor, 0f, (TimeSinceCreation - (FadeInTime + StayTime)) / FadeOutTime));
			if (base.AlphaFactor <= 0.1f)
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
