using System.Collections.Generic;
using System.IO;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public static class UIResourceManager
{
	private static bool _latestUIDebugModeState;

	public static ResourceDepot ResourceDepot { get; private set; }

	public static WidgetFactory WidgetFactory { get; private set; }

	public static SpriteData SpriteData { get; private set; }

	public static BrushFactory BrushFactory { get; private set; }

	public static FontFactory FontFactory { get; private set; }

	public static TwoDimensionEngineResourceContext ResourceContext { get; private set; }

	private static bool _uiDebugMode
	{
		get
		{
			if (!UIConfig.DebugModeEnabled)
			{
				return NativeConfig.GetUIDebugMode;
			}
			return true;
		}
	}

	static UIResourceManager()
	{
		ResourceContext = new TwoDimensionEngineResourceContext();
	}

	public static void Refresh()
	{
		RefreshResourceDepot(out var assemblyOrder);
		RefreshWidgetFactory(assemblyOrder);
		RefreshSpriteData();
		RefreshFontFactory();
		RefreshBrushFactory();
	}

	public static SpriteCategory GetSpriteCategory(string spriteCategoryName)
	{
		if (SpriteData == null)
		{
			Debug.FailedAssert("Trying to get sprite category but sprite data was not initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\UIResourceManager.cs", "GetSpriteCategory", 54);
			return null;
		}
		if (SpriteData.SpriteCategories.TryGetValue(spriteCategoryName, out var value))
		{
			return value;
		}
		return null;
	}

	public static SpriteCategory LoadSpriteCategory(string spriteCategoryName)
	{
		SpriteCategory spriteCategory = GetSpriteCategory(spriteCategoryName);
		spriteCategory.Load(ResourceContext, ResourceDepot);
		return spriteCategory;
	}

	public static void Update()
	{
		if (_latestUIDebugModeState != _uiDebugMode)
		{
			if (_uiDebugMode)
			{
				ResourceDepot.StartWatchingChangesInDepot();
			}
			else
			{
				ResourceDepot.StopWatchingChangesInDepot();
			}
			_latestUIDebugModeState = _uiDebugMode;
		}
		if (_uiDebugMode)
		{
			ResourceDepot.CheckForChanges();
		}
	}

	public static void OnLanguageChange(string newLanguageCode)
	{
		FontFactory.OnLanguageChange(newLanguageCode);
	}

	public static void Clear()
	{
		WidgetFactory = null;
		SpriteData = null;
		BrushFactory = null;
		FontFactory = null;
	}

	private static void RefreshResourceDepot(out List<string> assemblyOrder)
	{
		if (_uiDebugMode && ResourceDepot != null)
		{
			ResourceDepot.StopWatchingChangesInDepot();
		}
		ResourceDepot = new ResourceDepot();
		ResourceDepot.AddLocation(BasePath.Name, "GUI/GauntletUI/");
		assemblyOrder = new List<string>();
		foreach (ModuleInfo module in ModuleHelper.GetModules())
		{
			string folderPath = module.FolderPath;
			if (Directory.Exists(folderPath + "/GUI/"))
			{
				ResourceDepot.AddLocation(folderPath, "/GUI/");
			}
			foreach (SubModuleInfo subModule in module.SubModules)
			{
				if (subModule != null && subModule.DLLExists && !string.IsNullOrEmpty(subModule.DLLName))
				{
					assemblyOrder.Add(subModule.DLLName);
				}
			}
		}
		ResourceDepot.CollectResources();
		if (_uiDebugMode)
		{
			ResourceDepot.StartWatchingChangesInDepot();
		}
	}

	private static void RefreshWidgetFactory(List<string> assemblyOrder)
	{
		WidgetFactory = new WidgetFactory(ResourceDepot, "Prefabs");
		WidgetFactory.PrefabExtensionContext.AddExtension(new PrefabDatabindingExtension());
		WidgetFactory.Initialize(assemblyOrder);
		WidgetFactory.GeneratedPrefabContext.CollectPrefabs();
	}

	private static void RefreshSpriteData()
	{
		if (SpriteData == null)
		{
			SpriteData = new SpriteData("SpriteData");
			SpriteData.Load(ResourceDepot);
		}
		else
		{
			SpriteData.Reload(ResourceDepot, ResourceContext);
		}
	}

	private static void RefreshFontFactory()
	{
		FontFactory = new FontFactory(ResourceDepot);
		FontFactory.LoadAllFonts(SpriteData);
	}

	private static void RefreshBrushFactory()
	{
		BrushFactory = new BrushFactory(ResourceDepot, "Brushes", SpriteData, FontFactory);
		BrushFactory.Initialize();
	}
}
