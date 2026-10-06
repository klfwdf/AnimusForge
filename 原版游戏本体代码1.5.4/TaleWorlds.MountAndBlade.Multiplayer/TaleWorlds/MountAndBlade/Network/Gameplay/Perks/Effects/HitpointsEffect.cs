using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class HitpointsEffect : MPOnSpawnPerkEffect
{
	protected static string StringType = "HitpointsOnSpawn";

	private float _value;

	protected HitpointsEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\HitpointsEffect.cs", "Deserialize", 20);
		}
	}

	public override float GetHitpoints(bool isPlayer)
	{
		if (EffectTarget == Target.Any || (isPlayer ? (EffectTarget == Target.Player) : (EffectTarget == Target.Troops)))
		{
			return _value;
		}
		return 0f;
	}
}
