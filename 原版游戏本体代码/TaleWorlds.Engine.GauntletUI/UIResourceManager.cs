using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x0200000B RID: 11
	public static class UIResourceManager
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000063 RID: 99 RVA: 0x000033E7 File Offset: 0x000015E7
		// (set) Token: 0x06000064 RID: 100 RVA: 0x000033EE File Offset: 0x000015EE
		public static ResourceDepot ResourceDepot { get; private set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000065 RID: 101 RVA: 0x000033F6 File Offset: 0x000015F6
		// (set) Token: 0x06000066 RID: 102 RVA: 0x000033FD File Offset: 0x000015FD
		public static WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00003405 File Offset: 0x00001605
		// (set) Token: 0x06000068 RID: 104 RVA: 0x0000340C File Offset: 0x0000160C
		public static SpriteData SpriteData { get; private set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000069 RID: 105 RVA: 0x00003414 File Offset: 0x00001614
		// (set) Token: 0x0600006A RID: 106 RVA: 0x0000341B File Offset: 0x0000161B
		public static BrushFactory BrushFactory { get; private set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600006B RID: 107 RVA: 0x00003423 File Offset: 0x00001623
		// (set) Token: 0x0600006C RID: 108 RVA: 0x0000342A File Offset: 0x0000162A
		public static FontFactory FontFactory { get; private set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006D RID: 109 RVA: 0x00003432 File Offset: 0x00001632
		// (set) Token: 0x0600006E RID: 110 RVA: 0x00003439 File Offset: 0x00001639
		public static TwoDimensionEngineResourceContext ResourceContext { get; private set; } = new TwoDimensionEngineResourceContext();

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00003441 File Offset: 0x00001641
		private static bool _uiDebugMode
		{
			get
			{
				return UIConfig.DebugModeEnabled || NativeConfig.GetUIDebugMode;
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00003460 File Offset: 0x00001660
		public static void Refresh()
		{
			List<string> assemblyOrder;
			UIResourceManager.RefreshResourceDepot(out assemblyOrder);
			UIResourceManager.RefreshWidgetFactory(assemblyOrder);
			UIResourceManager.RefreshSpriteData();
			UIResourceManager.RefreshFontFactory();
			UIResourceManager.RefreshBrushFactory();
		}

		// Token: 0x06000072 RID: 114 RVA: 0x0000348C File Offset: 0x0000168C
		public static SpriteCategory GetSpriteCategory(string spriteCategoryName)
		{
			if (UIResourceManager.SpriteData == null)
			{
				Debug.FailedAssert("Trying to get sprite category but sprite data was not initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\UIResourceManager.cs", "GetSpriteCategory", 54);
				return null;
			}
			SpriteCategory result;
			if (UIResourceManager.SpriteData.SpriteCategories.TryGetValue(spriteCategoryName, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000034CF File Offset: 0x000016CF
		public static SpriteCategory LoadSpriteCategory(string spriteCategoryName)
		{
			SpriteCategory spriteCategory = UIResourceManager.GetSpriteCategory(spriteCategoryName);
			spriteCategory.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
			return spriteCategory;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000034E8 File Offset: 0x000016E8
		public static void Update()
		{
			if (UIResourceManager._latestUIDebugModeState != UIResourceManager._uiDebugMode)
			{
				if (UIResourceManager._uiDebugMode)
				{
					UIResourceManager.ResourceDepot.StartWatchingChangesInDepot();
				}
				else
				{
					UIResourceManager.ResourceDepot.StopWatchingChangesInDepot();
				}
				UIResourceManager._latestUIDebugModeState = UIResourceManager._uiDebugMode;
			}
			if (UIResourceManager._uiDebugMode)
			{
				UIResourceManager.ResourceDepot.CheckForChanges();
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00003539 File Offset: 0x00001739
		public static void OnLanguageChange(string newLanguageCode)
		{
			UIResourceManager.FontFactory.OnLanguageChange(newLanguageCode);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003546 File Offset: 0x00001746
		public static void Clear()
		{
			UIResourceManager.WidgetFactory = null;
			UIResourceManager.SpriteData = null;
			UIResourceManager.BrushFactory = null;
			UIResourceManager.FontFactory = null;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003560 File Offset: 0x00001760
		private static void RefreshResourceDepot(out List<string> assemblyOrder)
		{
			if (UIResourceManager._uiDebugMode && UIResourceManager.ResourceDepot != null)
			{
				UIResourceManager.ResourceDepot.StopWatchingChangesInDepot();
			}
			UIResourceManager.ResourceDepot = new ResourceDepot();
			UIResourceManager.ResourceDepot.AddLocation(BasePath.Name, "GUI/GauntletUI/");
			assemblyOrder = new List<string>();
			foreach (ModuleInfo moduleInfo in ModuleHelper.GetModules(null))
			{
				string folderPath = moduleInfo.FolderPath;
				if (Directory.Exists(folderPath + "/GUI/"))
				{
					UIResourceManager.ResourceDepot.AddLocation(folderPath, "/GUI/");
				}
				foreach (SubModuleInfo subModuleInfo in moduleInfo.SubModules)
				{
					if (subModuleInfo != null && subModuleInfo.DLLExists && !string.IsNullOrEmpty(subModuleInfo.DLLName))
					{
						assemblyOrder.Add(subModuleInfo.DLLName);
					}
				}
			}
			UIResourceManager.ResourceDepot.CollectResources();
			if (UIResourceManager._uiDebugMode)
			{
				UIResourceManager.ResourceDepot.StartWatchingChangesInDepot();
			}
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00003694 File Offset: 0x00001894
		private static void RefreshWidgetFactory(List<string> assemblyOrder)
		{
			UIResourceManager.WidgetFactory = new WidgetFactory(UIResourceManager.ResourceDepot, "Prefabs");
			UIResourceManager.WidgetFactory.PrefabExtensionContext.AddExtension(new PrefabDatabindingExtension());
			UIResourceManager.WidgetFactory.Initialize(assemblyOrder);
			UIResourceManager.WidgetFactory.GeneratedPrefabContext.CollectPrefabs();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000036E3 File Offset: 0x000018E3
		private static void RefreshSpriteData()
		{
			if (UIResourceManager.SpriteData == null)
			{
				UIResourceManager.SpriteData = new SpriteData("SpriteData");
				UIResourceManager.SpriteData.Load(UIResourceManager.ResourceDepot);
				return;
			}
			UIResourceManager.SpriteData.Reload(UIResourceManager.ResourceDepot, UIResourceManager.ResourceContext);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000371F File Offset: 0x0000191F
		private static void RefreshFontFactory()
		{
			UIResourceManager.FontFactory = new FontFactory(UIResourceManager.ResourceDepot);
			UIResourceManager.FontFactory.LoadAllFonts(UIResourceManager.SpriteData);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x0000373F File Offset: 0x0000193F
		private static void RefreshBrushFactory()
		{
			UIResourceManager.BrushFactory = new BrushFactory(UIResourceManager.ResourceDepot, "Brushes", UIResourceManager.SpriteData, UIResourceManager.FontFactory);
			UIResourceManager.BrushFactory.Initialize();
		}

		// Token: 0x04000017 RID: 23
		private static bool _latestUIDebugModeState;
	}
}
