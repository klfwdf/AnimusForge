using System;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class ShieldDamageEffect : MPPerkEffect
{
	private enum BlockType
	{
		Any,
		CorrectSide,
		WrongSide
	}

	protected static string StringType = "ShieldDamage";

	private float _value;

	private BlockType _blockType;

	protected ShieldDamageEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\ShieldDamageEffect.cs", "Deserialize", 31);
		}
		string text2 = node?.Attributes?["block_type"]?.Value;
		_blockType = BlockType.Any;
		if (text2 != null && !Enum.TryParse<BlockType>(text2, ignoreCase: true, out _blockType))
		{
			_blockType = BlockType.Any;
			Debug.FailedAssert("provided 'block_type' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\ShieldDamageEffect.cs", "Deserialize", 39);
		}
	}

	public override float GetShieldDamage(bool isCorrectSideBlock)
	{
		switch (_blockType)
		{
		case BlockType.Any:
			return _value;
		case BlockType.CorrectSide:
			if (!isCorrectSideBlock)
			{
				return 0f;
			}
			return _value;
		case BlockType.WrongSide:
			if (!isCorrectSideBlock)
			{
				return _value;
			}
			return 0f;
		default:
			return 0f;
		}
	}
}
