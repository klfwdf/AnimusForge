using System;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x02000170 RID: 368
	public static class MetaDataExtensions
	{
		// Token: 0x06001B02 RID: 6914 RVA: 0x0008ADC0 File Offset: 0x00088FC0
		public static string GetUniqueGameId(this MetaData metaData)
		{
			string result;
			if (metaData == null || !metaData.TryGetValue("UniqueGameId", out result))
			{
				return "";
			}
			return result;
		}

		// Token: 0x06001B03 RID: 6915 RVA: 0x0008ADE8 File Offset: 0x00088FE8
		public static int GetMainHeroLevel(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainHeroLevel", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B04 RID: 6916 RVA: 0x0008AE10 File Offset: 0x00089010
		public static float GetMainPartyFood(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainPartyFood", out s))
			{
				return 0f;
			}
			return float.Parse(s);
		}

		// Token: 0x06001B05 RID: 6917 RVA: 0x0008AE3C File Offset: 0x0008903C
		public static int GetMainHeroGold(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainHeroGold", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B06 RID: 6918 RVA: 0x0008AE64 File Offset: 0x00089064
		public static float GetClanInfluence(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("ClanInfluence", out s))
			{
				return 0f;
			}
			return float.Parse(s);
		}

		// Token: 0x06001B07 RID: 6919 RVA: 0x0008AE90 File Offset: 0x00089090
		public static int GetClanFiefs(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("ClanFiefs", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B08 RID: 6920 RVA: 0x0008AEB8 File Offset: 0x000890B8
		public static int GetMainPartyHealthyMemberCount(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainPartyHealthyMemberCount", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B09 RID: 6921 RVA: 0x0008AEE0 File Offset: 0x000890E0
		public static int GetMainPartyPrisonerMemberCount(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainPartyPrisonerMemberCount", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B0A RID: 6922 RVA: 0x0008AF08 File Offset: 0x00089108
		public static int GetMainPartyWoundedMemberCount(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("MainPartyWoundedMemberCount", out s))
			{
				return 0;
			}
			return int.Parse(s);
		}

		// Token: 0x06001B0B RID: 6923 RVA: 0x0008AF30 File Offset: 0x00089130
		public static string GetClanBannerCode(this MetaData metaData)
		{
			string result;
			if (metaData == null || !metaData.TryGetValue("ClanBannerCode", out result))
			{
				return "";
			}
			return result;
		}

		// Token: 0x06001B0C RID: 6924 RVA: 0x0008AF58 File Offset: 0x00089158
		public static string GetCharacterName(this MetaData metaData)
		{
			string result;
			if (metaData == null || !metaData.TryGetValue("CharacterName", out result))
			{
				return "";
			}
			return result;
		}

		// Token: 0x06001B0D RID: 6925 RVA: 0x0008AF80 File Offset: 0x00089180
		public static string GetCharacterVisualCode(this MetaData metaData)
		{
			string result;
			if (metaData == null || !metaData.TryGetValue("MainHeroVisual", out result))
			{
				return "";
			}
			return result;
		}

		// Token: 0x06001B0E RID: 6926 RVA: 0x0008AFA8 File Offset: 0x000891A8
		public static double GetDayLong(this MetaData metaData)
		{
			string s;
			if (metaData == null || !metaData.TryGetValue("DayLong", out s))
			{
				return 0.0;
			}
			return double.Parse(s);
		}

		// Token: 0x06001B0F RID: 6927 RVA: 0x0008AFD8 File Offset: 0x000891D8
		public static bool GetIronmanMode(this MetaData metaData)
		{
			string s;
			int num;
			return metaData != null && metaData.TryGetValue("IronmanMode", out s) && int.TryParse(s, out num) && num == 1;
		}

		// Token: 0x06001B10 RID: 6928 RVA: 0x0008B008 File Offset: 0x00089208
		public static int GetPlayerHealthPercentage(this MetaData metaData)
		{
			string s;
			int result;
			if (metaData == null || !metaData.TryGetValue("HealthPercentage", out s) || !int.TryParse(s, out result))
			{
				return 100;
			}
			return result;
		}
	}
}
