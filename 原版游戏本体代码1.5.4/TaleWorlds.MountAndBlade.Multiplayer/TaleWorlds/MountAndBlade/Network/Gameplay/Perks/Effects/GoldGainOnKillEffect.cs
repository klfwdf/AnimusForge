using System;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class GoldGainOnKillEffect : MPPerkEffect
{
	private enum EnemyValue
	{
		Any,
		Higher,
		Lower
	}

	protected static string StringType = "GoldGainOnKill";

	private int _value;

	private EnemyValue _enemyValue;

	protected GoldGainOnKillEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !int.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\GoldGainOnKillEffect.cs", "Deserialize", 31);
		}
		string text2 = node?.Attributes?["enemy_value"]?.Value;
		_enemyValue = EnemyValue.Any;
		if (text2 != null && !Enum.TryParse<EnemyValue>(text2, ignoreCase: true, out _enemyValue))
		{
			_enemyValue = EnemyValue.Any;
			Debug.FailedAssert("provided 'enemy_value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\GoldGainOnKillEffect.cs", "Deserialize", 39);
		}
	}

	public override int GetGoldOnKill(float attackerValue, float victimValue)
	{
		switch (_enemyValue)
		{
		case EnemyValue.Any:
			return _value;
		case EnemyValue.Higher:
			if (!(victimValue > attackerValue))
			{
				return 0;
			}
			return _value;
		case EnemyValue.Lower:
			if (!(victimValue < attackerValue))
			{
				return 0;
			}
			return _value;
		default:
			return 0;
		}
	}
}
