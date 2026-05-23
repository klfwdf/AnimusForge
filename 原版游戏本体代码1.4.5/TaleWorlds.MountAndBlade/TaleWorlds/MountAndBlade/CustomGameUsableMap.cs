using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.MountAndBlade;

public class CustomGameUsableMap
{
	public string Map { get; private set; }

	public bool IsCompatibleWithAllGameTypes { get; private set; }

	public List<string> CompatibleGameTypes { get; private set; }

	public CustomGameUsableMap(string map, bool isCompatibleWithAllGameTypes, List<string> compatibleGameTypes)
	{
		Map = map;
		IsCompatibleWithAllGameTypes = isCompatibleWithAllGameTypes;
		CompatibleGameTypes = compatibleGameTypes;
	}

	public override bool Equals(object obj)
	{
		if (obj is CustomGameUsableMap customGameUsableMap)
		{
			if (customGameUsableMap.Map != Map || customGameUsableMap.IsCompatibleWithAllGameTypes != IsCompatibleWithAllGameTypes)
			{
				return false;
			}
			if ((CompatibleGameTypes == null || CompatibleGameTypes.Count == 0) && (customGameUsableMap.CompatibleGameTypes == null || customGameUsableMap.CompatibleGameTypes.Count == 0))
			{
				return true;
			}
			if (CompatibleGameTypes == null || CompatibleGameTypes.Count == 0 || customGameUsableMap.CompatibleGameTypes == null || customGameUsableMap.CompatibleGameTypes.Count == 0)
			{
				return false;
			}
			return CompatibleGameTypes.SequenceEqual(customGameUsableMap.CompatibleGameTypes);
		}
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return (((((Map != null) ? Map.GetHashCode() : 0) * 397) ^ IsCompatibleWithAllGameTypes.GetHashCode()) * 397) ^ ((CompatibleGameTypes != null) ? CompatibleGameTypes.GetHashCode() : 0);
	}
}
