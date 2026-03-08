using System;
using System.Collections.Generic;
using System.IO;

namespace TaleWorlds.Library
{
	// Token: 0x02000087 RID: 135
	public class ResourceDepot
	{
		// Token: 0x14000015 RID: 21
		// (add) Token: 0x060004DE RID: 1246 RVA: 0x00011D6C File Offset: 0x0000FF6C
		// (remove) Token: 0x060004DF RID: 1247 RVA: 0x00011DA4 File Offset: 0x0000FFA4
		public event ResourceChangeEvent OnResourceChange;

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060004E0 RID: 1248 RVA: 0x00011DD9 File Offset: 0x0000FFD9
		public MBReadOnlyList<ResourceDepotLocation> ResourceLocations
		{
			get
			{
				return this._resourceLocations;
			}
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x00011DE1 File Offset: 0x0000FFE1
		public ResourceDepot()
		{
			this._resourceLocations = new MBList<ResourceDepotLocation>();
			this._files = new Dictionary<string, ResourceDepotFile>();
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x00011E00 File Offset: 0x00010000
		public void AddLocation(string basePath, string location)
		{
			basePath = basePath.Replace('\\', '/');
			location = location.Replace('\\', '/');
			ResourceDepotLocation item = new ResourceDepotLocation(basePath, location, basePath + location);
			this._resourceLocations.Add(item);
		}

		// Token: 0x060004E3 RID: 1251 RVA: 0x00011E40 File Offset: 0x00010040
		public void CollectResources()
		{
			this._files.Clear();
			foreach (ResourceDepotLocation resourceDepotLocation in this._resourceLocations)
			{
				Debug.Print("ResourceDepot:CollectResources: " + resourceDepotLocation.FullPath + "\n", 0, Debug.DebugColor.White, 17592186044416UL);
				string fullPath = resourceDepotLocation.FullPath;
				foreach (string text in Directory.GetFiles(resourceDepotLocation.BasePath + resourceDepotLocation.Path, "*", SearchOption.AllDirectories))
				{
					text = text.Replace('\\', '/');
					string text2 = text.Replace('\\', '/').Substring(fullPath.Length);
					string key = text2.ToLower();
					ResourceDepotFile value = new ResourceDepotFile(resourceDepotLocation, text2, text);
					if (this._files.ContainsKey(key))
					{
						this._files[key] = value;
					}
					else
					{
						this._files.Add(key, value);
					}
				}
			}
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x00011F68 File Offset: 0x00010168
		public string[] GetFiles(string subDirectory, string extension, bool excludeSubContents = false)
		{
			string value = extension.ToLower();
			List<string> list = new List<string>();
			foreach (ResourceDepotFile resourceDepotFile in this._files.Values)
			{
				string text = (resourceDepotFile.BasePath + resourceDepotFile.Location + subDirectory).Replace('\\', '/').ToLower();
				string fullPath = resourceDepotFile.FullPath;
				string fullPathLowerCase = resourceDepotFile.FullPathLowerCase;
				bool flag = (!excludeSubContents && fullPathLowerCase.StartsWith(text)) || (excludeSubContents && string.Equals(Directory.GetParent(text).FullName, text, StringComparison.CurrentCultureIgnoreCase));
				bool flag2 = fullPathLowerCase.EndsWith(value, StringComparison.OrdinalIgnoreCase);
				if (flag && flag2)
				{
					list.Add(fullPath);
				}
			}
			return list.ToArray();
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x00012044 File Offset: 0x00010244
		public string GetFilePath(string file)
		{
			file = file.Replace('\\', '/');
			return this._files[file.ToLower()].FullPath;
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x00012068 File Offset: 0x00010268
		public IEnumerable<string> GetFilesEndingWith(string fileEndName)
		{
			fileEndName = fileEndName.Replace('\\', '/');
			foreach (KeyValuePair<string, ResourceDepotFile> keyValuePair in this._files)
			{
				if (keyValuePair.Key.EndsWith(fileEndName.ToLower()))
				{
					yield return keyValuePair.Value.FullPath;
				}
			}
			Dictionary<string, ResourceDepotFile>.Enumerator enumerator = default(Dictionary<string, ResourceDepotFile>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x00012080 File Offset: 0x00010280
		public void StartWatchingChangesInDepot()
		{
			foreach (ResourceDepotLocation resourceDepotLocation in this._resourceLocations)
			{
				resourceDepotLocation.StartWatchingChanges(new FileSystemEventHandler(this.OnAnyChangeInDepotLocations), new RenamedEventHandler(this.OnAnyRenameInDepotLocations));
			}
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x000120E8 File Offset: 0x000102E8
		public void StopWatchingChangesInDepot()
		{
			foreach (ResourceDepotLocation resourceDepotLocation in this._resourceLocations)
			{
				resourceDepotLocation.StopWatchingChanges();
			}
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x00012138 File Offset: 0x00010338
		private void OnAnyChangeInDepotLocations(object source, FileSystemEventArgs e)
		{
			this._isThereAnyUnhandledChange = true;
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x00012141 File Offset: 0x00010341
		private void OnAnyRenameInDepotLocations(object source, RenamedEventArgs e)
		{
			this._isThereAnyUnhandledChange = true;
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001214A File Offset: 0x0001034A
		public void CheckForChanges()
		{
			if (this._isThereAnyUnhandledChange)
			{
				this.CollectResources();
				ResourceChangeEvent onResourceChange = this.OnResourceChange;
				if (onResourceChange != null)
				{
					onResourceChange();
				}
				this._isThereAnyUnhandledChange = false;
			}
		}

		// Token: 0x04000178 RID: 376
		private readonly MBList<ResourceDepotLocation> _resourceLocations;

		// Token: 0x04000179 RID: 377
		private readonly Dictionary<string, ResourceDepotFile> _files;

		// Token: 0x0400017A RID: 378
		private bool _isThereAnyUnhandledChange;
	}
}
