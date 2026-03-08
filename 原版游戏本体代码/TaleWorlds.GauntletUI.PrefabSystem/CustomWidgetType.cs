using System;
using System.IO;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000004 RID: 4
	public class CustomWidgetType
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600001C RID: 28 RVA: 0x00002480 File Offset: 0x00000680
		public WidgetTemplate WidgetTemplate
		{
			get
			{
				return this.WidgetPrefab.RootTemplate;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600001D RID: 29 RVA: 0x0000248D File Offset: 0x0000068D
		// (set) Token: 0x0600001E RID: 30 RVA: 0x00002495 File Offset: 0x00000695
		public WidgetPrefab WidgetPrefab { get; private set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600001F RID: 31 RVA: 0x0000249E File Offset: 0x0000069E
		// (set) Token: 0x06000020 RID: 32 RVA: 0x000024A6 File Offset: 0x000006A6
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000021 RID: 33 RVA: 0x000024AF File Offset: 0x000006AF
		// (set) Token: 0x06000022 RID: 34 RVA: 0x000024B7 File Offset: 0x000006B7
		public string Name { get; private set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000023 RID: 35 RVA: 0x000024C0 File Offset: 0x000006C0
		public string FullPath
		{
			get
			{
				return this._resourcesPath + this.Name + ".xml";
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000024D8 File Offset: 0x000006D8
		public CustomWidgetType(WidgetFactory widgetFactory, string resourcesPath, string name)
		{
			this._resourcesPath = resourcesPath;
			this.Name = name;
			this.WidgetFactory = widgetFactory;
			this.Load();
			this._lastWriteTime = File.GetLastWriteTime(this.FullPath);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002518 File Offset: 0x00000718
		public bool CheckForUpdate()
		{
			DateTime lastWriteTime = File.GetLastWriteTime(this.FullPath);
			if (this._lastWriteTime != lastWriteTime)
			{
				try
				{
					this.Load();
					this._lastWriteTime = lastWriteTime;
					return true;
				}
				catch
				{
				}
				return false;
			}
			return false;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002568 File Offset: 0x00000768
		private void Load()
		{
			this.WidgetPrefab = WidgetPrefab.LoadFrom(this.WidgetFactory.PrefabExtensionContext, this.WidgetFactory.WidgetAttributeContext, this.FullPath);
		}

		// Token: 0x04000017 RID: 23
		private DateTime _lastWriteTime = DateTime.MinValue;

		// Token: 0x04000018 RID: 24
		private string _resourcesPath;
	}
}
