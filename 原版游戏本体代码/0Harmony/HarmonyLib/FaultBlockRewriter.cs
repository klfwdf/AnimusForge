using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HarmonyLib
{
	// Token: 0x0200001D RID: 29
	internal class FaultBlockRewriter
	{
		// Token: 0x06000094 RID: 148 RVA: 0x000047BC File Offset: 0x000029BC
		private static int FindMatchingBeginException(List<CodeInstruction> rewritten)
		{
			int i = rewritten.Count - 1;
			int depth = 0;
			while (i >= 0)
			{
				if (rewritten[i].HasBlock(ExceptionBlockType.EndExceptionBlock))
				{
					depth++;
				}
				if (rewritten[i].HasBlock(ExceptionBlockType.BeginExceptionBlock))
				{
					if (depth == 0)
					{
						return i;
					}
					depth--;
				}
				i--;
			}
			return -1;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000480C File Offset: 0x00002A0C
		private static int FindMatchingEndException(List<CodeInstruction> source, int start)
		{
			int i = start;
			int depth = 0;
			while (i < source.Count)
			{
				if (source[i].HasBlock(ExceptionBlockType.BeginExceptionBlock))
				{
					depth++;
				}
				if (source[i].HasBlock(ExceptionBlockType.EndExceptionBlock))
				{
					if (depth == 0)
					{
						return i;
					}
					depth--;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00004858 File Offset: 0x00002A58
		private static CodeInstruction CloneWithoutFaultMarker(CodeInstruction original)
		{
			CodeInstruction copy = new CodeInstruction(original);
			copy.blocks.RemoveAll((ExceptionBlock b) => b.blockType == ExceptionBlockType.BeginFaultBlock);
			return copy;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00004898 File Offset: 0x00002A98
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
			int i = 0;
			List<CodeInstruction> rewritten = new List<CodeInstruction>(instructions.Count * 2);
			while (i < instructions.Count)
			{
				CodeInstruction cur = instructions[i];
				if (!cur.HasBlock(ExceptionBlockType.BeginFaultBlock))
				{
					rewritten.Add(new CodeInstruction(cur));
					i++;
				}
				else
				{
					int beginExceptionIdx = FaultBlockRewriter.FindMatchingBeginException(rewritten);
					int endExceptionIdx = FaultBlockRewriter.FindMatchingEndException(instructions, i + 1);
					if (beginExceptionIdx < 0 || endExceptionIdx < 0)
					{
						throw new InvalidOperationException("Unbalanced exception markers – cannot rewrite.");
					}
					List<CodeInstruction> faultBody = new List<CodeInstruction>();
					for (int j = i; j < endExceptionIdx; j++)
					{
						faultBody.Add(FaultBlockRewriter.CloneWithoutFaultMarker(instructions[j]));
					}
					i = endExceptionIdx + 1;
					LocalBuilder failedLocal = generator.DeclareLocal(typeof(bool));
					Label skipFault = generator.DefineLabel();
					rewritten.AddRange(new <>z__ReadOnlyArray<CodeInstruction>(new CodeInstruction[]
					{
						Code.Nop.WithBlocks(new ExceptionBlock[]
						{
							new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(object))
						}),
						Code.Pop,
						Code.Ldc_I4_1,
						Code.Stloc[failedLocal.LocalIndex, null],
						Code.Rethrow,
						Code.Nop.WithBlocks(new ExceptionBlock[]
						{
							new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null)
						}),
						Code.Ldloc[failedLocal.LocalIndex, null],
						Code.Brfalse_S[skipFault, null],
						Code.Nop.WithLabels(new Label[] { skipFault }),
						Code.Nop.WithBlocks(new ExceptionBlock[]
						{
							new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null)
						})
					}));
				}
			}
			return rewritten;
		}
	}
}
