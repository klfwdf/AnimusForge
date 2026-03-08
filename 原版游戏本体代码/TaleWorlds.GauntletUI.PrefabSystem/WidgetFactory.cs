using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000019 RID: 25
	public class WidgetFactory
	{
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600009F RID: 159 RVA: 0x000036B6 File Offset: 0x000018B6
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x000036BE File Offset: 0x000018BE
		public PrefabExtensionContext PrefabExtensionContext { get; private set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x000036C7 File Offset: 0x000018C7
		// (set) Token: 0x060000A2 RID: 162 RVA: 0x000036CF File Offset: 0x000018CF
		public WidgetAttributeContext WidgetAttributeContext { get; private set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000A3 RID: 163 RVA: 0x000036D8 File Offset: 0x000018D8
		// (set) Token: 0x060000A4 RID: 164 RVA: 0x000036E0 File Offset: 0x000018E0
		public GeneratedPrefabContext GeneratedPrefabContext { get; private set; }

		// Token: 0x060000A5 RID: 165 RVA: 0x000036EC File Offset: 0x000018EC
		public WidgetFactory(ResourceDepot resourceDepot, string resourceFolder)
		{
			this._resourceDepot = resourceDepot;
			this._resourceDepot.OnResourceChange += this.OnResourceChange;
			this._resourceFolder = resourceFolder;
			this._builtinTypes = new Dictionary<string, Type>();
			this._liveCustomTypes = new Dictionary<string, CustomWidgetType>();
			this._customTypePaths = new Dictionary<string, string>();
			this._liveInstanceTracker = new Dictionary<string, int>();
			this.PrefabExtensionContext = new PrefabExtensionContext();
			this.WidgetAttributeContext = new WidgetAttributeContext();
			this.GeneratedPrefabContext = new GeneratedPrefabContext();
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00003771 File Offset: 0x00001971
		private void OnResourceChange()
		{
			this.CheckForUpdates();
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000377C File Offset: 0x0000197C
		public void Initialize(List<string> assemblyOrder = null)
		{
			foreach (PrefabExtension prefabExtension in this.PrefabExtensionContext.PrefabExtensions)
			{
				prefabExtension.RegisterAttributeTypes(this.WidgetAttributeContext);
			}
			WidgetInfo[] widgetInfos = WidgetInfo.GetWidgetInfos();
			for (int i = 0; i < widgetInfos.Length; i++)
			{
				Type type = widgetInfos[i].Type;
				bool flag = true;
				if (this._builtinTypes.ContainsKey(type.Name) && assemblyOrder != null)
				{
					flag = assemblyOrder.IndexOf(type.Assembly.GetName().Name + ".dll") > assemblyOrder.IndexOf(this._builtinTypes[type.Name].Assembly.GetName().Name + ".dll");
				}
				if (flag)
				{
					this._builtinTypes[type.Name] = type;
				}
			}
			foreach (KeyValuePair<string, string> keyValuePair in this.GetPrefabNamesAndPathsFromCurrentPath())
			{
				this.AddCustomType(keyValuePair.Key, keyValuePair.Value);
			}
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000038C8 File Offset: 0x00001AC8
		private Dictionary<string, string> GetPrefabNamesAndPathsFromCurrentPath()
		{
			string[] files = this._resourceDepot.GetFiles(this._resourceFolder, ".xml", false);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (string text in files)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				string text2 = text.Substring(0, text.LastIndexOf('/') + 1);
				if (!dictionary.ContainsKey(fileNameWithoutExtension))
				{
					dictionary.Add(fileNameWithoutExtension, text2);
				}
				else
				{
					Debug.FailedAssert(string.Concat(new string[]
					{
						"This prefab has already been added: ",
						fileNameWithoutExtension,
						". Previous Directory: ",
						dictionary[fileNameWithoutExtension],
						" | New Directory: ",
						text2
					}), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetFactory.cs", "GetPrefabNamesAndPathsFromCurrentPath", 96);
					dictionary[fileNameWithoutExtension] = text2;
				}
			}
			return dictionary;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00003990 File Offset: 0x00001B90
		public void AddCustomType(string name, string path)
		{
			this._customTypePaths.Add(name, path);
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000399F File Offset: 0x00001B9F
		public IEnumerable<string> GetPrefabNames()
		{
			return this._customTypePaths.Keys;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x000039AC File Offset: 0x00001BAC
		public IEnumerable<string> GetWidgetTypes()
		{
			return this._builtinTypes.Keys.Concat(this._customTypePaths.Keys);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000039C9 File Offset: 0x00001BC9
		public bool IsBuiltinType(string name)
		{
			return this._builtinTypes.ContainsKey(name);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000039D7 File Offset: 0x00001BD7
		public Type GetBuiltinType(string name)
		{
			return this._builtinTypes[name];
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000039E5 File Offset: 0x00001BE5
		public bool IsCustomType(string typeName)
		{
			return this._customTypePaths.ContainsKey(typeName);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000039F4 File Offset: 0x00001BF4
		public string GetCustomTypePath(string name)
		{
			string result;
			if (this._customTypePaths.TryGetValue(name, out result))
			{
				return result;
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetFactory.cs", "GetCustomTypePath", 141);
			return "";
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00003A34 File Offset: 0x00001C34
		public Widget CreateBuiltinWidget(UIContext context, string typeName)
		{
			Type type;
			Widget result;
			if (this._builtinTypes.TryGetValue(typeName, out type))
			{
				result = (Widget)type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, new Type[] { typeof(UIContext) }, null).InvokeWithLog(new object[] { context });
			}
			else
			{
				result = new Widget(context);
				Debug.FailedAssert("builtin widget type not found in CreateBuiltinWidget(" + typeName + ")", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetFactory.cs", "CreateBuiltinWidget", 162);
			}
			return result;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00003AB8 File Offset: 0x00001CB8
		public WidgetPrefab GetCustomType(string typeName)
		{
			CustomWidgetType customWidgetType;
			if (this._liveCustomTypes.TryGetValue(typeName, out customWidgetType))
			{
				Dictionary<string, int> liveInstanceTracker = this._liveInstanceTracker;
				int num = liveInstanceTracker[typeName];
				liveInstanceTracker[typeName] = num + 1;
				return customWidgetType.WidgetPrefab;
			}
			string resourcesPath;
			if (this._customTypePaths.TryGetValue(typeName, out resourcesPath))
			{
				CustomWidgetType customWidgetType2 = new CustomWidgetType(this, resourcesPath, typeName);
				this._liveCustomTypes[typeName] = customWidgetType2;
				this._liveInstanceTracker[typeName] = 1;
				return customWidgetType2.WidgetPrefab;
			}
			Debug.FailedAssert("Couldn't find Custom Widget type: " + typeName, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.PrefabSystem\\WidgetFactory.cs", "GetCustomType", 185);
			return null;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00003B54 File Offset: 0x00001D54
		public void OnUnload(string typeName)
		{
			if (this._liveCustomTypes.ContainsKey(typeName))
			{
				Dictionary<string, int> liveInstanceTracker = this._liveInstanceTracker;
				int num = liveInstanceTracker[typeName];
				liveInstanceTracker[typeName] = num - 1;
				if (this._liveInstanceTracker[typeName] == 0)
				{
					this._liveCustomTypes.Remove(typeName);
					this._liveInstanceTracker.Remove(typeName);
				}
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00003BB0 File Offset: 0x00001DB0
		public void CheckForUpdates()
		{
			bool flag = false;
			Dictionary<string, string> prefabNamesAndPathsFromCurrentPath = this.GetPrefabNamesAndPathsFromCurrentPath();
			foreach (KeyValuePair<string, string> keyValuePair in prefabNamesAndPathsFromCurrentPath)
			{
				if (!this._customTypePaths.ContainsKey(keyValuePair.Key))
				{
					this.AddCustomType(keyValuePair.Key, keyValuePair.Value);
				}
			}
			List<string> list = null;
			foreach (string text in this._customTypePaths.Keys)
			{
				if (!prefabNamesAndPathsFromCurrentPath.ContainsKey(text))
				{
					if (list == null)
					{
						list = new List<string>();
					}
					list.Add(text);
					flag = true;
				}
			}
			if (list != null)
			{
				foreach (string key in list)
				{
					this._customTypePaths.Remove(key);
				}
			}
			foreach (CustomWidgetType customWidgetType in this._liveCustomTypes.Values)
			{
				flag = flag || customWidgetType.CheckForUpdate();
			}
			if (flag)
			{
				Action prefabChange = this.PrefabChange;
				if (prefabChange == null)
				{
					return;
				}
				prefabChange();
			}
		}

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060000B4 RID: 180 RVA: 0x00003D38 File Offset: 0x00001F38
		// (remove) Token: 0x060000B5 RID: 181 RVA: 0x00003D70 File Offset: 0x00001F70
		public event Action PrefabChange;

		// Token: 0x04000033 RID: 51
		private Dictionary<string, Type> _builtinTypes;

		// Token: 0x04000034 RID: 52
		private Dictionary<string, string> _customTypePaths;

		// Token: 0x04000035 RID: 53
		private Dictionary<string, CustomWidgetType> _liveCustomTypes;

		// Token: 0x04000036 RID: 54
		private Dictionary<string, int> _liveInstanceTracker;

		// Token: 0x04000037 RID: 55
		private ResourceDepot _resourceDepot;

		// Token: 0x04000038 RID: 56
		private readonly string _resourceFolder;
	}
}
