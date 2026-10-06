using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerPreloadHelper : MissionNetwork
{
	public override List<EquipmentElement> GetExtraEquipmentElementsForCharacter(BasicCharacterObject character, bool getAllEquipments = false)
	{
		List<EquipmentElement> list = new List<EquipmentElement>();
		foreach (List<IReadOnlyPerkObject> item in MultiplayerClassDivisions.GetAllPerksForHeroClass(MultiplayerClassDivisions.GetMPHeroClassForCharacter(character)))
		{
			List<(EquipmentIndex, EquipmentElement)> list2 = null;
			foreach (IReadOnlyPerkObject item2 in item)
			{
				int num = list2?.Count ?? 0;
				list2 = item2.GetAlternativeEquipments(isWarmup: false, isPlayer: true, list2, getAllEquipments);
				int num2 = list2?.Count ?? 0;
				for (int i = num; i < num2; i++)
				{
					list.Add(list2[i].Item2);
				}
			}
		}
		return list;
	}
}
