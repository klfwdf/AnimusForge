using System;
using TaleWorlds.SaveSystem;

namespace StoryMode.Extensions;

public static class MetaDataExtensions
{
	public static bool HasStoryMode(this MetaData metaData)
	{
		bool result = false;
		if (metaData != null && metaData.TryGetValue("Modules", out var value))
		{
			string[] array = value.Split(new char[1] { ';' });
			for (int i = 0; i < array.Length; i++)
			{
				if (string.Equals(array[i], "StoryMode", StringComparison.OrdinalIgnoreCase))
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	public static bool AreAchievementsDisabled(this MetaData metaData)
	{
		if (metaData != null && metaData.TryGetValue("AchievementsDisabled", out var value) && int.TryParse(value, out var result))
		{
			return result == 1;
		}
		return false;
	}
}
