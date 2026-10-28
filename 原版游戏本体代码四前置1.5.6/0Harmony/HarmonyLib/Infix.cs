using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib;

internal class Infix
{
	internal Patch patch;

	internal MethodInfo OuterMethod => patch.PatchMethod;

	internal MethodBase InnerMethod => patch.innerMethod.Method;

	internal int[] Positions => patch.innerMethod.positions;

	internal Infix(Patch patch)
	{
		this.patch = patch;
	}

	internal bool Matches(MethodBase method, int index, int total)
	{
		if (method != InnerMethod)
		{
			return false;
		}
		if (Positions.Length == 0)
		{
			return true;
		}
		int[] positions = Positions;
		foreach (int num in positions)
		{
			if (num > 0 && num == index)
			{
				return true;
			}
			if (num < 0 && index == total + num + 1)
			{
				return true;
			}
		}
		return false;
	}

	internal IEnumerable<CodeInstruction> Apply(MethodCreatorConfig config, bool isPrefix)
	{
		_ = config;
		yield return Code.Nop[isPrefix ? "inner-prefix" : "inner-postfix", null];
	}
}
