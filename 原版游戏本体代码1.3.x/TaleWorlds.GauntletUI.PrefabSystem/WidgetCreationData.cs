using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000017 RID: 23
	public class WidgetCreationData
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00002F85 File Offset: 0x00001185
		// (set) Token: 0x0600008A RID: 138 RVA: 0x00002F8D File Offset: 0x0000118D
		public Widget Parent { get; private set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00002F96 File Offset: 0x00001196
		// (set) Token: 0x0600008C RID: 140 RVA: 0x00002F9E File Offset: 0x0000119E
		public UIContext Context { get; private set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00002FA7 File Offset: 0x000011A7
		// (set) Token: 0x0600008E RID: 142 RVA: 0x00002FAF File Offset: 0x000011AF
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00002FB8 File Offset: 0x000011B8
		public BrushFactory BrushFactory
		{
			get
			{
				return this.Context.BrushFactory;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000090 RID: 144 RVA: 0x00002FC5 File Offset: 0x000011C5
		public SpriteData SpriteData
		{
			get
			{
				return this.Context.SpriteData;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00002FD2 File Offset: 0x000011D2
		public PrefabExtensionContext PrefabExtensionContext
		{
			get
			{
				return this.WidgetFactory.PrefabExtensionContext;
			}
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00002FDF File Offset: 0x000011DF
		public WidgetCreationData(UIContext context, WidgetFactory widgetFactory, Widget parent)
		{
			this.Context = context;
			this.WidgetFactory = widgetFactory;
			this.Parent = parent;
			this._extensionData = new Dictionary<string, object>();
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00003007 File Offset: 0x00001207
		public WidgetCreationData(UIContext context, WidgetFactory widgetFactory)
		{
			this.Context = context;
			this.WidgetFactory = widgetFactory;
			this.Parent = null;
			this._extensionData = new Dictionary<string, object>();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00003030 File Offset: 0x00001230
		public WidgetCreationData(WidgetCreationData widgetCreationData, WidgetInstantiationResult parentResult)
		{
			this.Context = widgetCreationData.Context;
			this.WidgetFactory = widgetCreationData.WidgetFactory;
			this.Parent = parentResult.Widget;
			this._extensionData = new Dictionary<string, object>();
			foreach (KeyValuePair<string, object> keyValuePair in widgetCreationData._extensionData)
			{
				this._extensionData.Add(keyValuePair.Key, keyValuePair.Value);
			}
			foreach (WidgetInstantiationResultExtensionData widgetInstantiationResultExtensionData in parentResult.ExtensionDatas)
			{
				if (widgetInstantiationResultExtensionData.PassToChildWidgetCreation)
				{
					this.AddExtensionData(widgetInstantiationResultExtensionData.Name, widgetInstantiationResultExtensionData.Data);
				}
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000311C File Offset: 0x0000131C
		public void AddExtensionData(string name, object data)
		{
			if (this._extensionData.ContainsKey(name))
			{
				this._extensionData[name] = data;
				return;
			}
			this._extensionData.Add(name, data);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00003148 File Offset: 0x00001348
		public T GetExtensionData<T>(string name) where T : class
		{
			if (this._extensionData.ContainsKey(name))
			{
				return this._extensionData[name] as T;
			}
			return default(T);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003183 File Offset: 0x00001383
		public void AddExtensionData(object data)
		{
			this.AddExtensionData(data.GetType().Name, data);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003197 File Offset: 0x00001397
		public T GetExtensionData<T>() where T : class
		{
			return this.GetExtensionData<T>(typeof(T).Name);
		}

		// Token: 0x04000032 RID: 50
		private Dictionary<string, object> _extensionData;
	}
}
