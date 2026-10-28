using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface IOnSpawnPerkEffect
{
	int GetExtraTroopCount();

	List<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isPlayer, List<(EquipmentIndex, EquipmentElement)> alternativeEquipments, bool getAll = false);

	float GetDrivenPropertyBonusOnSpawn(bool isPlayer, DrivenProperty drivenProperty, float baseValue);

	float GetHitpoints(bool isPlayer);
}
