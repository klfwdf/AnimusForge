namespace TaleWorlds.MountAndBlade;

public class OverrideStrikeAndDeathActionDuringUsageComponent : UsableMissionObjectComponent
{
	private readonly ActionIndexCache _strikeAction = ActionIndexCache.act_none;

	private readonly ActionIndexCache _deathAction = ActionIndexCache.act_none;

	public OverrideStrikeAndDeathActionDuringUsageComponent(in ActionIndexCache strikeAction, in ActionIndexCache deathAction)
	{
		_strikeAction = strikeAction;
		_deathAction = deathAction;
	}

	protected internal override void OnUse(Agent userAgent)
	{
		userAgent.SetOverridenStrikeAndDeathAction(in _strikeAction, in _deathAction);
	}

	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		userAgent.SetOverridenStrikeAndDeathAction(in ActionIndexCache.act_none, in ActionIndexCache.act_none);
	}
}
