using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class MetaDataExtensions
{
	public static string GetUniqueGameId(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("UniqueGameId", out var value))
		{
			return "";
		}
		return value;
	}

	public static int GetMainHeroLevel(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainHeroLevel", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static float GetMainPartyFood(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainPartyFood", out var value))
		{
			return 0f;
		}
		return float.Parse(value);
	}

	public static int GetMainHeroGold(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainHeroGold", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static float GetClanInfluence(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("ClanInfluence", out var value))
		{
			return 0f;
		}
		return float.Parse(value);
	}

	public static int GetClanFiefs(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("ClanFiefs", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static int GetMainPartyShipCount(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainPartyShipCount", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static int GetMainPartyHealthyMemberCount(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainPartyHealthyMemberCount", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static int GetMainPartyPrisonerMemberCount(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainPartyPrisonerMemberCount", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static int GetMainPartyWoundedMemberCount(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainPartyWoundedMemberCount", out var value))
		{
			return 0;
		}
		return int.Parse(value);
	}

	public static string GetClanBannerCode(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("ClanBannerCode", out var value))
		{
			return "";
		}
		return value;
	}

	public static string GetCharacterName(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("CharacterName", out var value))
		{
			return "";
		}
		return value;
	}

	public static string GetCharacterVisualCode(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("MainHeroVisual", out var value))
		{
			return "";
		}
		return value;
	}

	public static double GetDayLong(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("DayLong", out var value))
		{
			return 0.0;
		}
		return double.Parse(value);
	}

	public static bool GetIronmanMode(this MetaData metaData)
	{
		if (metaData != null && metaData.TryGetValue("IronmanMode", out var value) && int.TryParse(value, out var result))
		{
			return result == 1;
		}
		return false;
	}

	public static int GetPlayerHealthPercentage(this MetaData metaData)
	{
		if (metaData == null || !metaData.TryGetValue("HealthPercentage", out var value) || !int.TryParse(value, out var result))
		{
			return 100;
		}
		return result;
	}
}
