using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;

namespace Bannerlord.UIExtenderEx.ResourceManager;

public static class BrushFactoryManager
{
	private delegate Brush LoadBrushFromDelegate(object instance, XmlNode brushNode);

	private static readonly Dictionary<string, Brush> CustomBrushes = new Dictionary<string, Brush>();

	private static readonly LoadBrushFromDelegate? LoadBrushFrom = AccessTools2.GetDeclaredDelegate<LoadBrushFromDelegate>(typeof(BrushFactory), "LoadBrushFrom");

	public static IEnumerable<Brush> Create(XmlDocument xmlDocument)
	{
		foreach (XmlNode childNode in xmlDocument.SelectSingleNode("Brushes").ChildNodes)
		{
			Brush brush = LoadBrushFrom?.Invoke(UIResourceManager.BrushFactory, childNode);
			if (brush != null)
			{
				yield return brush;
			}
		}
	}

	public static void Register(IEnumerable<Brush> brushes)
	{
		foreach (Brush brush in brushes)
		{
			CustomBrushes[brush.Name] = brush;
		}
	}

	public static void CreateAndRegister(XmlDocument xmlDocument)
	{
		Register(Create(xmlDocument));
	}

	internal static void Patch(Harmony harmony)
	{
		harmony.Patch(AccessTools2.DeclaredPropertyGetter(typeof(BrushFactory), "Brushes"), null, new HarmonyMethod(typeof(BrushFactoryManager), "GetBrushesPostfix"));
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(BrushFactory), "GetBrush"), new HarmonyMethod(typeof(BrushFactoryManager), "GetBrushPrefix"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.ConstantDefinition:GetValue"), null, null, AccessTools2.DeclaredMethod(typeof(BrushFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetExtensions:SetWidgetAttributeFromString"), null, null, AccessTools2.DeclaredMethod(typeof(BrushFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.UIContext:GetBrush"), null, null, AccessTools2.DeclaredMethod(typeof(BrushFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetExtensions:ConvertObject"), null, null, AccessTools2.DeclaredMethod(typeof(BrushFactoryManager), "BlankTranspiler"));
		harmony.TryPatch(AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.GauntletUI.Widgets.BoolBrushChangerBrushWidget:OnBooleanUpdated"), null, null, AccessTools2.DeclaredMethod(typeof(BrushFactoryManager), "BlankTranspiler"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void GetBrushesPostfix(ref IEnumerable<Brush> __result)
	{
		__result = __result.Concat(CustomBrushes.Values);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool GetBrushPrefix(string name, IReadOnlyDictionary<string, Brush> ____brushes, ref Brush __result)
	{
		if (____brushes.ContainsKey(name) || !CustomBrushes.ContainsKey(name))
		{
			return true;
		}
		Brush brush = CustomBrushes[name];
		if (brush != null)
		{
			__result = brush;
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
