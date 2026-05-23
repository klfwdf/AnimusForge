using System;
using System.IO;

namespace TaleWorlds.Library
{
	// Token: 0x02000089 RID: 137
	public class ResourceDepotLocation
	{
		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060004F7 RID: 1271 RVA: 0x000121F9 File Offset: 0x000103F9
		// (set) Token: 0x060004F8 RID: 1272 RVA: 0x00012201 File Offset: 0x00010401
		public string BasePath { get; private set; }

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060004F9 RID: 1273 RVA: 0x0001220A File Offset: 0x0001040A
		// (set) Token: 0x060004FA RID: 1274 RVA: 0x00012212 File Offset: 0x00010412
		public string Path { get; private set; }

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060004FB RID: 1275 RVA: 0x0001221B File Offset: 0x0001041B
		// (set) Token: 0x060004FC RID: 1276 RVA: 0x00012223 File Offset: 0x00010423
		public string FullPath { get; private set; }

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060004FD RID: 1277 RVA: 0x0001222C File Offset: 0x0001042C
		// (set) Token: 0x060004FE RID: 1278 RVA: 0x00012234 File Offset: 0x00010434
		public FileSystemWatcher Watcher { get; private set; }

		// Token: 0x060004FF RID: 1279 RVA: 0x0001223D File Offset: 0x0001043D
		public ResourceDepotLocation(string basePath, string path, string fullPath)
		{
			this.BasePath = basePath;
			this.Path = path;
			this.FullPath = fullPath;
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x0001225C File Offset: 0x0001045C
		public void StartWatchingChanges(FileSystemEventHandler onChangeEvent, RenamedEventHandler onRenameEvent)
		{
			this.Watcher = new FileSystemWatcher
			{
				Path = this.FullPath,
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime),
				Filter = "*.*",
				IncludeSubdirectories = true,
				EnableRaisingEvents = true
			};
			this.Watcher.Changed += onChangeEvent;
			this.Watcher.Created += onChangeEvent;
			this.Watcher.Deleted += onChangeEvent;
			this.Watcher.Renamed += onRenameEvent;
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x000122D1 File Offset: 0x000104D1
		public void StopWatchingChanges()
		{
			this.Watcher.Dispose();
		}
	}
}
