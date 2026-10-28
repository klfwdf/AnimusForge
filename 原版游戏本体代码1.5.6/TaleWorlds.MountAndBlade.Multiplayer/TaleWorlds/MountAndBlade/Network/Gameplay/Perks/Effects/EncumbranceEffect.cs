using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class EncumbranceEffect : MPPerkEffect
{
	protected static string StringType = "Encumbrance";

	private bool _isOnBody;

	private float _value;

	protected EncumbranceEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\EncumbranceEffect.cs", "Deserialize", 23);
		}
		_isOnBody = (node?.Attributes?["is_on_body"]?.Value)?.ToLower() == "true";
	}

	public override void OnUpdate(Agent agent, bool newState)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		agent?.UpdateAgentProperties();
	}

	public override float GetEncumbrance(bool isOnBody)
	{
		if (isOnBody != _isOnBody)
		{
			return 0f;
		}
		return _value;
	}
}
