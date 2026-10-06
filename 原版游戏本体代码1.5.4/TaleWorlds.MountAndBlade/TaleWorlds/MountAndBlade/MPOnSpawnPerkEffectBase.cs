using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MPOnSpawnPerkEffectBase : MPPerkEffectBase, IOnSpawnPerkEffect
{
	protected enum Target
	{
		Player,
		Troops,
		Any
	}

	protected Target EffectTarget;

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["target"]?.Value;
		EffectTarget = Target.Any;
		if (text != null && !Enum.TryParse<Target>(text, ignoreCase: true, out EffectTarget))
		{
			EffectTarget = Target.Any;
			Debug.FailedAssert("provided 'target' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\Perks\\MPOnSpawnPerkEffectBase.cs", "Deserialize", 38);
		}
	}

	public virtual float GetTroopCountMultiplier()
	{
		return 0f;
	}

	public virtual int GetExtraTroopCount()
	{
		return 0;
	}

	public virtual List<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isPlayer, List<(EquipmentIndex, EquipmentElement)> alternativeEquipments, bool getAll = false)
	{
		return alternativeEquipments;
	}

	public virtual float GetDrivenPropertyBonusOnSpawn(bool isPlayer, DrivenProperty drivenProperty, float baseValue)
	{
		return 0f;
	}

	public virtual float GetHitpoints(bool isPlayer)
	{
		return 0f;
	}
}
