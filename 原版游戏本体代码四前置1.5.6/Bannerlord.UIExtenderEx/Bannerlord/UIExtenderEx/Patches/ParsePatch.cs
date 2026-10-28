using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Utils;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace Bannerlord.UIExtenderEx.Patches;

internal static class ParsePatch
{
	public static void Patch(Harmony harmony)
	{
		MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeof(ConstantDefinition), "GetValue");
		if ((object)methodInfo != null)
		{
			harmony.Patch(methodInfo, null, null, new HarmonyMethod(typeof(ParsePatch), "ConstantDefinition_GetValue_Transpiler"));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IEnumerable<CodeInstruction> ConstantDefinition_GetValue_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionsList = instructions.ToList();
		MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeof(decimal), "Parse", new Type[1] { typeof(string) });
		if ((object)methodInfo == null)
		{
			return ReturnDefault("decimal.Parse(string) method not found");
		}
		MethodInfo methodInfo2 = AccessTools2.DeclaredMethod(typeof(decimal), "Parse", new Type[2]
		{
			typeof(string),
			typeof(IFormatProvider)
		});
		if ((object)methodInfo2 == null)
		{
			return ReturnDefault("decimal.Parse(string, IFormatProvider) method not found");
		}
		MethodInfo methodInfo3 = AccessTools2.DeclaredPropertyGetter(typeof(CultureInfo), "InvariantCulture");
		if ((object)methodInfo3 == null)
		{
			return ReturnDefault("CultureInfo.InvariantCulture getter not found");
		}
		bool flag = false;
		for (int i = 0; i < instructionsList.Count; i++)
		{
			if (instructionsList[i].opcode == OpCodes.Call && object.Equals(instructionsList[i].operand, methodInfo))
			{
				instructionsList.Insert(i, new CodeInstruction(OpCodes.Call, methodInfo3));
				i++;
				instructionsList[i] = new CodeInstruction(OpCodes.Call, methodInfo2);
				flag = true;
			}
		}
		if (!flag)
		{
			return ReturnDefault("decimal.Parse(string) call not found in method");
		}
		return instructionsList.AsEnumerable();
		[MethodImpl(MethodImplOptions.NoInlining)]
		IEnumerable<CodeInstruction> ReturnDefault(string place)
		{
			MessageUtils.DisplayUserWarning("Failed to patch ConstantDefinition.GetValue! {0}", place);
			return instructionsList.AsEnumerable();
		}
	}
}
