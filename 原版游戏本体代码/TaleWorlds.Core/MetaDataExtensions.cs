using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000B7 RID: 183
	public static class MetaDataExtensions
	{
		// Token: 0x0600098A RID: 2442 RVA: 0x0001F298 File Offset: 0x0001D498
		public static DateTime GetCreationTime(this MetaData metaData)
		{
			string text = ((metaData != null) ? metaData["CreationTime"] : null);
			if (text != null)
			{
				DateTime result;
				if (DateTime.TryParse(text, out result))
				{
					return result;
				}
				long ticks;
				if (long.TryParse(text, out ticks))
				{
					return new DateTime(ticks);
				}
			}
			return DateTime.MinValue;
		}

		// Token: 0x0600098B RID: 2443 RVA: 0x0001F2DC File Offset: 0x0001D4DC
		public static string[] GetModules(this MetaData metaData)
		{
			string text;
			if (metaData == null || !metaData.TryGetValue("Modules", out text))
			{
				return new string[0];
			}
			return text.Split(new char[] { ';' });
		}

		// Token: 0x0600098C RID: 2444 RVA: 0x0001F314 File Offset: 0x0001D514
		public static ApplicationVersion GetModuleVersion(this MetaData metaData, string moduleName)
		{
			string key = "Module_" + moduleName;
			string versionAsString;
			if (metaData != null && metaData.TryGetValue(key, out versionAsString))
			{
				try
				{
					return ApplicationVersion.FromString(versionAsString, 0);
				}
				catch (Exception ex)
				{
					Debug.FailedAssert(ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MetaDataExtensions.cs", "GetModuleVersion", 45);
				}
			}
			return ApplicationVersion.Empty;
		}
	}
}
