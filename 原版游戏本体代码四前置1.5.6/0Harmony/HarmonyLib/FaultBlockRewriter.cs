using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class FaultBlockRewriter
{
	private static int FindMatchingBeginException(List<CodeInstruction> rewritten)
	{
		int num = rewritten.Count - 1;
		int num2 = 0;
		while (num >= 0)
		{
			if (rewritten[num].HasBlock(ExceptionBlockType.EndExceptionBlock))
			{
				num2++;
			}
			if (rewritten[num].HasBlock(ExceptionBlockType.BeginExceptionBlock))
			{
				if (num2 == 0)
				{
					return num;
				}
				num2--;
			}
			num--;
		}
		return -1;
	}

	private static int FindMatchingEndException(List<CodeInstruction> source, int start)
	{
		int i = start;
		int num = 0;
		for (; i < source.Count; i++)
		{
			if (source[i].HasBlock(ExceptionBlockType.BeginExceptionBlock))
			{
				num++;
			}
			if (source[i].HasBlock(ExceptionBlockType.EndExceptionBlock))
			{
				if (num == 0)
				{
					return i;
				}
				num--;
			}
		}
		return -1;
	}

	private static CodeInstruction CloneWithoutFaultMarker(CodeInstruction original)
	{
		CodeInstruction codeInstruction = new CodeInstruction(original);
		codeInstruction.blocks.RemoveAll((ExceptionBlock b) => b.blockType == ExceptionBlockType.BeginFaultBlock);
		return codeInstruction;
	}

	internal static List<CodeInstruction> Rewrite(List<CodeInstruction> instructions, ILGenerator generator)
	{
		if (instructions == null)
		{
			throw new ArgumentNullException("instructions");
		}
		if (generator == null)
		{
			throw new ArgumentNullException("generator");
		}
		int num = 0;
		List<CodeInstruction> list = new List<CodeInstruction>(instructions.Count * 2);
		while (num < instructions.Count)
		{
			CodeInstruction codeInstruction = instructions[num];
			if (!codeInstruction.HasBlock(ExceptionBlockType.BeginFaultBlock))
			{
				list.Add(new CodeInstruction(codeInstruction));
				num++;
				continue;
			}
			int num2 = FindMatchingBeginException(list);
			int num3 = FindMatchingEndException(instructions, num + 1);
			if (num2 < 0 || num3 < 0)
			{
				throw new InvalidOperationException("Unbalanced exception markers – cannot rewrite.");
			}
			List<CodeInstruction> list2 = new List<CodeInstruction>();
			for (int i = num; i < num3; i++)
			{
				list2.Add(CloneWithoutFaultMarker(instructions[i]));
			}
			num = num3 + 1;
			LocalBuilder localBuilder = generator.DeclareLocal(typeof(bool));
			Label label = generator.DefineLabel();
			list.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>(new CodeInstruction[10]
			{
				Code.Nop.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(object))),
				Code.Pop,
				Code.Ldc_I4_1,
				Code.Stloc[localBuilder.LocalIndex, null],
				Code.Rethrow,
				Code.Nop.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock)),
				Code.Ldloc[localBuilder.LocalIndex, null],
				Code.Brfalse_S[label, null],
				Code.Nop.WithLabels(label),
				Code.Nop.WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock))
			}));
		}
		return list;
	}
}
