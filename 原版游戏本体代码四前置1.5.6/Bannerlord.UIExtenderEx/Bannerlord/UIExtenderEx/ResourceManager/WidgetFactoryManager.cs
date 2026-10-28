using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.UIExtenderEx.Patches;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace Bannerlord.UIExtenderEx.ResourceManager;

public static class WidgetFactoryManager
{
	private delegate void ReloadDelegate();

	private delegate Widget WidgetConstructor(UIContext uiContext);

	private static readonly ReloadDelegate? Reload = AccessTools2.GetDeclaredDelegate<ReloadDelegate>(typeof(WidgetInfo), "Reload") ?? AccessTools2.GetDeclaredDelegate<ReloadDelegate>(typeof(WidgetInfo), "Refresh");

	private static readonly AccessTools.FieldRef<WidgetFactory, IDictionary>? _liveCustomTypes = AccessTools2.FieldRefAccess<WidgetFactory, IDictionary>("_liveCustomTypes");

	private static readonly ConcurrentDictionary<Type, WidgetConstructor?> WidgetConstructors = new ConcurrentDictionary<Type, WidgetConstructor>();

	private static readonly Dictionary<string, Func<WidgetPrefab?>> CustomTypes = new Dictionary<string, Func<WidgetPrefab>>();

	private static readonly Dictionary<string, Type> BuiltinTypes = new Dictionary<string, Type>();

	private static readonly Dictionary<string, WidgetPrefab> LiveCustomTypes = new Dictionary<string, WidgetPrefab>();

	private static readonly Dictionary<string, int> LiveInstanceTracker = new Dictionary<string, int>();

	public static WidgetPrefab? Create(XmlDocument doc)
	{
		return WidgetPrefabPatch.LoadFromDocument(UIResourceManager.WidgetFactory.PrefabExtensionContext, UIResourceManager.WidgetFactory.WidgetAttributeContext, string.Empty, doc);
	}

	public static WidgetPrefab? Create(string name, XmlDocument doc)
	{
		return WidgetPrefabPatch.LoadFromDocument(UIResourceManager.WidgetFactory.PrefabExtensionContext, UIResourceManager.WidgetFactory.WidgetAttributeContext, name, doc);
	}

	public static void Register(Type widgetType)
	{
		if (Reload != null)
		{
			BuiltinTypes[widgetType.Name] = widgetType;
			Reload();
		}
	}

	public static void Register(string name, Func<WidgetPrefab?> create)
	{
		CustomTypes.Add(name, create);
	}

	public static void CreateAndRegister(string name, XmlDocument xmlDocument)
	{
		Register(name, () => Create(name + ".xml", xmlDocument));
	}

	public static void Patch(Harmony harmony)
	{
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(WidgetFactory), "GetCustomType"), new HarmonyMethod(typeof(WidgetFactoryManager), "GetCustomTypePrefix"));
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(WidgetFactory), "CreateBuiltinWidget"), new HarmonyMethod(typeof(WidgetFactoryManager), "CreateBuiltinWidgetPrefix"));
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(WidgetFactory), "GetWidgetTypes"), null, new HarmonyMethod(typeof(WidgetFactoryManager), "GetWidgetTypesPostfix"));
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(WidgetFactory), "IsCustomType"), new HarmonyMethod(typeof(WidgetFactoryManager), "IsCustomTypePrefix"));
		harmony.TryPatch(AccessTools2.DeclaredMethod(typeof(WidgetFactory), "OnUnload"), AccessTools2.DeclaredMethod(typeof(WidgetFactoryManager), "OnUnloadPrefix"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetTemplate:CreateWidgets"), null, null, AccessTools2.DeclaredMethod(typeof(WidgetFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetTemplate:OnRelease"), null, null, AccessTools2.DeclaredMethod(typeof(WidgetFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.Data.GauntletMovie:LoadMovie"), null, null, AccessTools2.DeclaredMethod(typeof(WidgetFactoryManager), "BlankTranspiler"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void GetWidgetTypesPostfix(ref IEnumerable<string> __result)
	{
		__result = __result.Concat(BuiltinTypes.Keys).Concat(CustomTypes.Keys);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool CreateBuiltinWidgetPrefix(UIContext context, string typeName, ref object? __result)
	{
		if (!BuiltinTypes.TryGetValue(typeName, out Type value))
		{
			return true;
		}
		WidgetConstructor orAdd = WidgetConstructors.GetOrAdd(value, (Type x) => AccessTools2.GetDeclaredConstructorDelegate<WidgetConstructor>(x, new Type[1] { typeof(UIContext) }));
		if (orAdd == null)
		{
			return true;
		}
		__result = orAdd(context);
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsCustomTypePrefix(string typeName, ref bool __result)
	{
		if (!CustomTypes.ContainsKey(typeName))
		{
			return true;
		}
		__result = true;
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool GetCustomTypePrefix(WidgetFactory __instance, string typeName, ref WidgetPrefab __result)
	{
		AccessTools.FieldRef<WidgetFactory, IDictionary>? liveCustomTypes = _liveCustomTypes;
		IDictionary dictionary = ((liveCustomTypes != null) ? liveCustomTypes(__instance) : null);
		if ((dictionary != null && dictionary.Contains(typeName)) || !CustomTypes.ContainsKey(typeName))
		{
			return true;
		}
		if (LiveCustomTypes.TryGetValue(typeName, out WidgetPrefab value))
		{
			LiveInstanceTracker[typeName]++;
			__result = value;
			return false;
		}
		WidgetPrefab widgetPrefab = CustomTypes[typeName]?.Invoke();
		if (widgetPrefab != null)
		{
			LiveCustomTypes.Add(typeName, widgetPrefab);
			LiveInstanceTracker[typeName] = 1;
			__result = widgetPrefab;
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool OnUnloadPrefix(string typeName)
	{
		if (LiveCustomTypes.ContainsKey(typeName))
		{
			LiveInstanceTracker[typeName]--;
			if (LiveInstanceTracker[typeName] == 0)
			{
				LiveCustomTypes.Remove(typeName);
				LiveInstanceTracker.Remove(typeName);
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IEnumerable<CodeInstruction> BlankTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return instructions;
	}
}
