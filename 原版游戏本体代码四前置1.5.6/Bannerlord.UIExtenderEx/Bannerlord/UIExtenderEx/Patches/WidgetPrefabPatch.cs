using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.UIExtenderEx.Utils;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace Bannerlord.UIExtenderEx.Patches;

internal static class WidgetPrefabPatch
{
	public static void Patch(Harmony harmony)
	{
		harmony.Patch(AccessTools2.DeclaredMethod(typeof(WidgetPrefab), "LoadFrom"), null, null, new HarmonyMethod(typeof(WidgetPrefabPatch), "WidgetPrefab_LoadFrom_Transpiler"));
		harmony.CreateReversePatcher(AccessTools2.DeclaredMethod(typeof(WidgetPrefab), "LoadFrom"), new HarmonyMethod(typeof(WidgetPrefabPatch), "LoadFromDocument")).Patch();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IEnumerable<CodeInstruction> WidgetPrefab_LoadFrom_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
	{
		List<CodeInstruction> instructionsList = instructions.ToList();
		ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(typeof(WidgetPrefab));
		if ((object)constructorInfo == null)
		{
			return ReturnDefault("WidgetPrefab constructor not found");
		}
		MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeof(WidgetPrefabPatch), "ProcessMovie");
		if ((object)methodInfo == null)
		{
			return ReturnDefault("WidgetPrefabPatch:ProcessMovie not found");
		}
		LocalVariableInfo localVariableInfo = (method.GetMethodBody()?.LocalVariables)?.FirstOrDefault((LocalVariableInfo x) => x.LocalType == typeof(WidgetPrefab));
		if (localVariableInfo == null)
		{
			return ReturnDefault("Local not found");
		}
		int num = -1;
		for (int num2 = 0; num2 < instructionsList.Count - 2; num2++)
		{
			if (!(instructionsList[num2].opcode != OpCodes.Newobj) && object.Equals(instructionsList[num2].operand, constructorInfo) && instructionsList[num2 + 1].IsStloc())
			{
				num = num2;
				break;
			}
		}
		if (num == -1)
		{
			return ReturnDefault("Pattern not found");
		}
		instructionsList.InsertRange(num + 1, new List<CodeInstruction>
		{
			new CodeInstruction(OpCodes.Ldarg_2),
			new CodeInstruction(OpCodes.Ldloc_0),
			new CodeInstruction(OpCodes.Call, methodInfo)
		});
		return instructionsList.AsEnumerable();
		[MethodImpl(MethodImplOptions.NoInlining)]
		IEnumerable<CodeInstruction> ReturnDefault(string place)
		{
			MessageUtils.DisplayUserWarning("Failed to patch WidgetPrefab.LoadFrom! {0}", place);
			return instructionsList.AsEnumerable();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ProcessMovie(string path, XmlDocument document)
	{
		foreach (UIExtenderRuntime allRuntime in UIExtender.GetAllRuntimes())
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
			allRuntime.PrefabComponent.ProcessMovieIfNeeded(fileNameWithoutExtension, document);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static WidgetPrefab LoadFromDocument(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, string path, XmlDocument document)
	{
		Transpiler(null);
		prefabExtensionContext.AddExtension(null);
		widgetAttributeContext.RegisterKeyType(null);
		path.Do(null);
		document.Validate(null);
		return null;
		[MethodImpl(MethodImplOptions.NoInlining)]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			IEnumerable<CodeInstruction> returnNull = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldnull),
				new CodeInstruction(OpCodes.Ret)
			}.AsEnumerable();
			ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(typeof(WidgetPrefab));
			if ((object)constructorInfo == null)
			{
				return ReturnDefault("WidgetPrefab constructor not found");
			}
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeof(WidgetPrefab), "LoadFrom");
			LocalVariableInfo localVariableInfo = (methodInfo.GetMethodBody()?.LocalVariables)?.FirstOrDefault((LocalVariableInfo x) => x.LocalType == typeof(XmlDocument));
			if (localVariableInfo == null)
			{
				return returnNull;
			}
			int num = -1;
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				if (list[num2].opcode == OpCodes.Newobj && object.Equals(list[num2].operand, constructorInfo))
				{
					num = num2;
				}
			}
			if (num == -1)
			{
				return returnNull;
			}
			for (int num3 = 0; num3 < num; num3++)
			{
				list[num3] = new CodeInstruction(OpCodes.Nop);
			}
			list[num - 2] = new CodeInstruction(OpCodes.Ldarg_S, 3);
			list[num - 1] = new CodeInstruction(OpCodes.Stloc_S, localVariableInfo.LocalIndex);
			return list.AsEnumerable();
			[MethodImpl(MethodImplOptions.NoInlining)]
			IEnumerable<CodeInstruction> ReturnDefault(string place)
			{
				MessageUtils.DisplayUserWarning("Failed to patch WidgetPrefab:LoadFrom.Transpiler! {0}", place);
				return returnNull;
			}
		}
	}
}
