using System;
using TaleWorlds.SaveSystem;

namespace NavalDLC;

public static class MetaDataExtensions
{
	public static bool HasNavalDLC(this MetaData metaData)
	{
		bool result = false;
		if (metaData != null && metaData.TryGetValue("Modules", out var value))
		{
			string[] array = value.Split(new char[1] { ';' });
			for (int i = 0; i < array.Length; i++)
			{
				if (string.Equals(array[i], "NavalDLC", StringComparison.OrdinalIgnoreCase))
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}
}
