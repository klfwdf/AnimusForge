using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class DrivenPropertyEffect : MPPerkEffect
{
	protected static string StringType = "DrivenProperty";

	private DrivenProperty _drivenProperty;

	private float _value;

	private bool _isRatio;

	protected DrivenPropertyEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		_isRatio = (node?.Attributes?["is_ratio"]?.Value)?.ToLower() == "true";
		if (!Enum.TryParse<DrivenProperty>(node?.Attributes?["driven_property"]?.Value, ignoreCase: true, out _drivenProperty))
		{
			Debug.FailedAssert("provided 'driven_property' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\DrivenPropertyEffect.cs", "Deserialize", 28);
		}
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\DrivenPropertyEffect.cs", "Deserialize", 35);
		}
	}

	public override void OnUpdate(Agent agent, bool newState)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		agent?.UpdateAgentProperties();
	}

	public override float GetDrivenPropertyBonus(DrivenProperty drivenProperty, float baseValue)
	{
		if (drivenProperty != _drivenProperty)
		{
			return 0f;
		}
		if (!_isRatio)
		{
			return _value;
		}
		return baseValue * _value;
	}
}
