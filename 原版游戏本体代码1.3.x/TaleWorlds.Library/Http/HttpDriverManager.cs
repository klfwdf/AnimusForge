using System;
using System.Collections.Concurrent;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000AF RID: 175
	public static class HttpDriverManager
	{
		// Token: 0x06000691 RID: 1681 RVA: 0x00016CA5 File Offset: 0x00014EA5
		public static void AddHttpDriver(string name, IHttpDriver driver)
		{
			if (HttpDriverManager._httpDrivers.Count == 0)
			{
				HttpDriverManager._defaultHttpDriver = name;
			}
			HttpDriverManager._httpDrivers[name] = driver;
		}

		// Token: 0x06000692 RID: 1682 RVA: 0x00016CC5 File Offset: 0x00014EC5
		public static void SetDefault(string name)
		{
			if (HttpDriverManager.GetHttpDriver(name) != null)
			{
				HttpDriverManager._defaultHttpDriver = name;
			}
		}

		// Token: 0x06000693 RID: 1683 RVA: 0x00016CD8 File Offset: 0x00014ED8
		public static IHttpDriver GetHttpDriver(string name)
		{
			IHttpDriver httpDriver;
			HttpDriverManager._httpDrivers.TryGetValue(name, out httpDriver);
			if (httpDriver == null)
			{
				Debug.Print("HTTP driver not found:" + (name ?? "not set"), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return httpDriver;
		}

		// Token: 0x06000694 RID: 1684 RVA: 0x00016D1C File Offset: 0x00014F1C
		public static IHttpDriver GetDefaultHttpDriver()
		{
			if (HttpDriverManager._defaultHttpDriver == null)
			{
				HttpDriverManager.AddHttpDriver("DotNet", new DotNetHttpDriver());
			}
			return HttpDriverManager.GetHttpDriver(HttpDriverManager._defaultHttpDriver);
		}

		// Token: 0x040001F7 RID: 503
		private static ConcurrentDictionary<string, IHttpDriver> _httpDrivers = new ConcurrentDictionary<string, IHttpDriver>();

		// Token: 0x040001F8 RID: 504
		private static string _defaultHttpDriver;
	}
}
