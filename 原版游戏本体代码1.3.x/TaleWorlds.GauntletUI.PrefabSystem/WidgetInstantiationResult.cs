using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200001A RID: 26
	public class WidgetInstantiationResult
	{
		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x00003DA5 File Offset: 0x00001FA5
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00003DAD File Offset: 0x00001FAD
		public Widget Widget { get; private set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00003DB6 File Offset: 0x00001FB6
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x00003DBE File Offset: 0x00001FBE
		public WidgetTemplate Template { get; private set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00003DC7 File Offset: 0x00001FC7
		// (set) Token: 0x060000BB RID: 187 RVA: 0x00003DCF File Offset: 0x00001FCF
		public WidgetInstantiationResult CustomWidgetInstantiationData { get; private set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00003DD8 File Offset: 0x00001FD8
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00003DE0 File Offset: 0x00001FE0
		public List<WidgetInstantiationResult> Children { get; private set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00003DE9 File Offset: 0x00001FE9
		internal IEnumerable<WidgetInstantiationResultExtensionData> ExtensionDatas
		{
			get
			{
				return this._entensionData.Values;
			}
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00003DF6 File Offset: 0x00001FF6
		public WidgetInstantiationResult(Widget widget, WidgetTemplate widgetTemplate, WidgetInstantiationResult customWidgetInstantiationData)
		{
			this.CustomWidgetInstantiationData = customWidgetInstantiationData;
			this.Widget = widget;
			this.Template = widgetTemplate;
			this.Children = new List<WidgetInstantiationResult>();
			this._entensionData = new Dictionary<string, WidgetInstantiationResultExtensionData>();
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00003E2C File Offset: 0x0000202C
		public void AddExtensionData(string name, object data, bool passToChildWidgetCreation = false)
		{
			WidgetInstantiationResultExtensionData value = default(WidgetInstantiationResultExtensionData);
			value.Name = name;
			value.Data = data;
			value.PassToChildWidgetCreation = passToChildWidgetCreation;
			this._entensionData.Add(name, value);
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00003E68 File Offset: 0x00002068
		public T GetExtensionData<T>(string name)
		{
			return (T)((object)this._entensionData[name].Data);
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00003E8E File Offset: 0x0000208E
		internal WidgetInstantiationResultExtensionData GetExtensionData(string name)
		{
			return this._entensionData[name];
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00003E9C File Offset: 0x0000209C
		public void AddExtensionData(object data, bool passToChildWidgetCreation = false)
		{
			this.AddExtensionData(data.GetType().Name, data, passToChildWidgetCreation);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00003EB1 File Offset: 0x000020B1
		public T GetExtensionData<T>() where T : class
		{
			return this.GetExtensionData<T>(typeof(T).Name);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00003EC8 File Offset: 0x000020C8
		public WidgetInstantiationResult(Widget widget, WidgetTemplate widgetTemplate)
			: this(widget, widgetTemplate, null)
		{
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00003ED3 File Offset: 0x000020D3
		public WidgetInstantiationResult GetLogicalOrDefaultChildrenLocation()
		{
			return WidgetInstantiationResult.GetLogicalOrDefaultChildrenLocation(this, true);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00003EDC File Offset: 0x000020DC
		private static WidgetInstantiationResult GetLogicalOrDefaultChildrenLocation(WidgetInstantiationResult data, bool isRoot)
		{
			if (isRoot)
			{
				foreach (WidgetInstantiationResult widgetInstantiationResult in data.CustomWidgetInstantiationData.Children)
				{
					if (widgetInstantiationResult.Template.LogicalChildrenLocation)
					{
						return widgetInstantiationResult;
					}
				}
				foreach (WidgetInstantiationResult data2 in data.CustomWidgetInstantiationData.Children)
				{
					WidgetInstantiationResult logicalOrDefaultChildrenLocation = WidgetInstantiationResult.GetLogicalOrDefaultChildrenLocation(data2, false);
					if (logicalOrDefaultChildrenLocation != null)
					{
						return logicalOrDefaultChildrenLocation;
					}
				}
				return data;
			}
			foreach (WidgetInstantiationResult widgetInstantiationResult2 in data.Children)
			{
				if (widgetInstantiationResult2.Template.LogicalChildrenLocation)
				{
					return widgetInstantiationResult2;
				}
			}
			foreach (WidgetInstantiationResult data3 in data.Children)
			{
				WidgetInstantiationResult logicalOrDefaultChildrenLocation2 = WidgetInstantiationResult.GetLogicalOrDefaultChildrenLocation(data3, false);
				if (logicalOrDefaultChildrenLocation2 != null)
				{
					return logicalOrDefaultChildrenLocation2;
				}
			}
			return null;
		}

		// Token: 0x04000041 RID: 65
		private Dictionary<string, WidgetInstantiationResultExtensionData> _entensionData;
	}
}
