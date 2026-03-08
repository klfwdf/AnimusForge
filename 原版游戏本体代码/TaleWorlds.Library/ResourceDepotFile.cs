using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000088 RID: 136
	public class ResourceDepotFile
	{
		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060004EC RID: 1260 RVA: 0x00012172 File Offset: 0x00010372
		// (set) Token: 0x060004ED RID: 1261 RVA: 0x0001217A File Offset: 0x0001037A
		public ResourceDepotLocation ResourceDepotLocation { get; private set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060004EE RID: 1262 RVA: 0x00012183 File Offset: 0x00010383
		public string BasePath
		{
			get
			{
				return this.ResourceDepotLocation.BasePath;
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060004EF RID: 1263 RVA: 0x00012190 File Offset: 0x00010390
		public string Location
		{
			get
			{
				return this.ResourceDepotLocation.Path;
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060004F0 RID: 1264 RVA: 0x0001219D File Offset: 0x0001039D
		// (set) Token: 0x060004F1 RID: 1265 RVA: 0x000121A5 File Offset: 0x000103A5
		public string FileName { get; private set; }

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060004F2 RID: 1266 RVA: 0x000121AE File Offset: 0x000103AE
		// (set) Token: 0x060004F3 RID: 1267 RVA: 0x000121B6 File Offset: 0x000103B6
		public string FullPath { get; private set; }

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060004F4 RID: 1268 RVA: 0x000121BF File Offset: 0x000103BF
		// (set) Token: 0x060004F5 RID: 1269 RVA: 0x000121C7 File Offset: 0x000103C7
		public string FullPathLowerCase { get; private set; }

		// Token: 0x060004F6 RID: 1270 RVA: 0x000121D0 File Offset: 0x000103D0
		public ResourceDepotFile(ResourceDepotLocation resourceDepotLocation, string fileName, string fullPath)
		{
			this.ResourceDepotLocation = resourceDepotLocation;
			this.FileName = fileName;
			this.FullPath = fullPath;
			this.FullPathLowerCase = fullPath.ToLower();
		}
	}
}
