using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x02000012 RID: 18
	internal class CodeTranspiler
	{
		// Token: 0x06000044 RID: 68 RVA: 0x00002F60 File Offset: 0x00001160
		internal CodeTranspiler(List<ILInstruction> ilInstructions)
		{
			this.codeInstructions = (from ilInstruction in ilInstructions
				select ilInstruction.GetCodeInstruction()).ToList<CodeInstruction>().AsEnumerable<CodeInstruction>();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002FB3 File Offset: 0x000011B3
		internal void Add(MethodInfo transpiler)
		{
			this.transpilers.Add(transpiler);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002FC4 File Offset: 0x000011C4
		internal static object ConvertInstruction(Type type, object instruction, out Dictionary<string, object> unassigned)
		{
			Dictionary<string, object> nonExisting = new Dictionary<string, object>();
			object elementTo = AccessTools.MakeDeepCopy(instruction, type, delegate(string namePath, Traverse trvSrc, Traverse trvDest)
			{
				object value = trvSrc.GetValue();
				if (!trvDest.FieldExists())
				{
					nonExisting[namePath] = value;
					return null;
				}
				if (namePath == "opcode")
				{
					return CodeTranspiler.ReplaceShortJumps((OpCode)value);
				}
				return value;
			}, "");
			unassigned = nonExisting;
			return elementTo;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003004 File Offset: 0x00001204
		internal static bool ShouldAddExceptionInfo(object op, int opIndex, List<object> originalInstructions, List<object> newInstructions, Dictionary<object, Dictionary<string, object>> unassignedValues)
		{
			int originalIndex = originalInstructions.IndexOf(op);
			if (originalIndex == -1)
			{
				return false;
			}
			Dictionary<string, object> unassigned;
			if (!unassignedValues.TryGetValue(op, out unassigned))
			{
				return false;
			}
			object blocksObject;
			if (!unassigned.TryGetValue("blocks", out blocksObject))
			{
				return false;
			}
			List<ExceptionBlock> blocks = blocksObject as List<ExceptionBlock>;
			int dupCount = newInstructions.Count((object instr) => instr == op);
			if (dupCount <= 1)
			{
				return true;
			}
			ExceptionBlock isStartBlock = blocks.FirstOrDefault((ExceptionBlock block) => block.blockType != ExceptionBlockType.EndExceptionBlock);
			ExceptionBlock isEndBlock = blocks.FirstOrDefault((ExceptionBlock block) => block.blockType == ExceptionBlockType.EndExceptionBlock);
			if (isStartBlock != null && isEndBlock == null)
			{
				object pairInstruction = originalInstructions.Skip(originalIndex + 1).FirstOrDefault(delegate(object instr)
				{
					if (!unassignedValues.TryGetValue(instr, out unassigned))
					{
						return false;
					}
					if (!unassigned.TryGetValue("blocks", out blocksObject))
					{
						return false;
					}
					blocks = blocksObject as List<ExceptionBlock>;
					return blocks.Count > 0;
				});
				if (pairInstruction != null)
				{
					int pairStart = originalIndex + 1;
					int pairEnd = pairStart + originalInstructions.Skip(pairStart).ToList<object>().IndexOf(pairInstruction) - 1;
					IEnumerable<object> originalBetweenInstructions = originalInstructions.GetRange(pairStart, pairEnd - pairStart).Intersect(newInstructions);
					pairInstruction = newInstructions.Skip(opIndex + 1).FirstOrDefault(delegate(object instr)
					{
						if (!unassignedValues.TryGetValue(instr, out unassigned))
						{
							return false;
						}
						if (!unassigned.TryGetValue("blocks", out blocksObject))
						{
							return false;
						}
						blocks = blocksObject as List<ExceptionBlock>;
						return blocks.Count > 0;
					});
					if (pairInstruction != null)
					{
						pairStart = opIndex + 1;
						pairEnd = pairStart + newInstructions.Skip(opIndex + 1).ToList<object>().IndexOf(pairInstruction) - 1;
						List<object> newBetweenInstructions = newInstructions.GetRange(pairStart, pairEnd - pairStart);
						List<object> remaining = originalBetweenInstructions.Except(newBetweenInstructions).ToList<object>();
						return remaining.Count == 0;
					}
				}
			}
			if (isStartBlock == null && isEndBlock != null)
			{
				object pairInstruction2 = originalInstructions.GetRange(0, originalIndex).LastOrDefault(delegate(object instr)
				{
					if (!unassignedValues.TryGetValue(instr, out unassigned))
					{
						return false;
					}
					if (!unassigned.TryGetValue("blocks", out blocksObject))
					{
						return false;
					}
					blocks = blocksObject as List<ExceptionBlock>;
					return blocks.Count > 0;
				});
				if (pairInstruction2 != null)
				{
					int pairStart2 = originalInstructions.GetRange(0, originalIndex).LastIndexOf(pairInstruction2);
					int pairEnd2 = originalIndex;
					IEnumerable<object> originalBetweenInstructions2 = originalInstructions.GetRange(pairStart2, pairEnd2 - pairStart2).Intersect(newInstructions);
					pairInstruction2 = newInstructions.GetRange(0, opIndex).LastOrDefault(delegate(object instr)
					{
						if (!unassignedValues.TryGetValue(instr, out unassigned))
						{
							return false;
						}
						if (!unassigned.TryGetValue("blocks", out blocksObject))
						{
							return false;
						}
						blocks = blocksObject as List<ExceptionBlock>;
						return blocks.Count > 0;
					});
					if (pairInstruction2 != null)
					{
						pairStart2 = newInstructions.GetRange(0, opIndex).LastIndexOf(pairInstruction2);
						List<object> newBetweenInstructions2 = newInstructions.GetRange(pairStart2, opIndex - pairStart2);
						IEnumerable<object> remaining2 = originalBetweenInstructions2.Except(newBetweenInstructions2);
						return !remaining2.Any<object>();
					}
				}
			}
			return true;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003270 File Offset: 0x00001470
		internal static IEnumerable ConvertInstructionsAndUnassignedValues(Type type, IEnumerable enumerable, out Dictionary<object, Dictionary<string, object>> unassignedValues)
		{
			Assembly enumerableAssembly = type.GetGenericTypeDefinition().Assembly;
			Type genericListType = enumerableAssembly.GetType(typeof(List<>).FullName);
			Type elementType = type.GetGenericArguments()[0];
			Type genericListTypeWithElement = genericListType.MakeGenericType(new Type[] { elementType });
			Type listType = enumerableAssembly.GetType(genericListTypeWithElement.FullName);
			object list = Activator.CreateInstance(listType);
			MethodInfo listAdd = list.GetType().GetMethod("Add");
			unassignedValues = new Dictionary<object, Dictionary<string, object>>();
			foreach (object op in enumerable)
			{
				Dictionary<string, object> unassigned;
				object elementTo = CodeTranspiler.ConvertInstruction(elementType, op, out unassigned);
				unassignedValues.Add(elementTo, unassigned);
				listAdd.Invoke(list, new object[] { elementTo });
			}
			return list as IEnumerable;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003360 File Offset: 0x00001560
		internal static IEnumerable ConvertToOurInstructions(IEnumerable instructions, Type codeInstructionType, List<object> originalInstructions, Dictionary<object, Dictionary<string, object>> unassignedValues)
		{
			CodeTranspiler.<ConvertToOurInstructions>d__7 <ConvertToOurInstructions>d__ = new CodeTranspiler.<ConvertToOurInstructions>d__7(-2);
			<ConvertToOurInstructions>d__.<>3__instructions = instructions;
			<ConvertToOurInstructions>d__.<>3__codeInstructionType = codeInstructionType;
			<ConvertToOurInstructions>d__.<>3__originalInstructions = originalInstructions;
			<ConvertToOurInstructions>d__.<>3__unassignedValues = unassignedValues;
			return <ConvertToOurInstructions>d__;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003385 File Offset: 0x00001585
		private static bool IsCodeInstructionsParameter(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("IEnumerable", StringComparison.Ordinal);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000033A8 File Offset: 0x000015A8
		internal static IEnumerable ConvertToGeneralInstructions(MethodInfo transpiler, IEnumerable enumerable, out Dictionary<object, Dictionary<string, object>> unassignedValues)
		{
			IEnumerable<Type> source = from p in transpiler.GetParameters()
				select p.ParameterType;
			Func<Type, bool> predicate;
			if ((predicate = CodeTranspiler.<>O.<0>__IsCodeInstructionsParameter) == null)
			{
				predicate = (CodeTranspiler.<>O.<0>__IsCodeInstructionsParameter = new Func<Type, bool>(CodeTranspiler.IsCodeInstructionsParameter));
			}
			Type type = source.FirstOrDefault(predicate);
			if (type == typeof(IEnumerable<CodeInstruction>))
			{
				unassignedValues = null;
				IList<CodeInstruction> result;
				if ((result = enumerable as IList<CodeInstruction>) == null)
				{
					List<CodeInstruction> list = new List<CodeInstruction>();
					list.AddRange((enumerable as IEnumerable<CodeInstruction>) ?? enumerable.Cast<CodeInstruction>());
					result = list;
				}
				return result;
			}
			return CodeTranspiler.ConvertInstructionsAndUnassignedValues(type, enumerable, out unassignedValues);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003448 File Offset: 0x00001648
		internal static List<object> GetTranspilerCallParameters(ILGenerator generator, MethodInfo transpiler, MethodBase method, IEnumerable instructions)
		{
			List<object> parameter = new List<object>();
			(from param in transpiler.GetParameters()
				select param.ParameterType).Do(delegate(Type type)
			{
				if (type.IsAssignableFrom(typeof(ILGenerator)))
				{
					parameter.Add(generator);
					return;
				}
				if (type.IsAssignableFrom(typeof(MethodBase)))
				{
					parameter.Add(method);
					return;
				}
				if (CodeTranspiler.IsCodeInstructionsParameter(type))
				{
					parameter.Add(instructions);
				}
			});
			return parameter;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000034BC File Offset: 0x000016BC
		internal List<CodeInstruction> GetResult(ILGenerator generator, MethodBase method)
		{
			IEnumerable instructions = this.codeInstructions;
			this.transpilers.ForEach(delegate(MethodInfo transpiler)
			{
				Dictionary<object, Dictionary<string, object>> unassignedValues;
				instructions = CodeTranspiler.ConvertToGeneralInstructions(transpiler, instructions, out unassignedValues);
				List<object> originalInstructions = null;
				if (unassignedValues != null)
				{
					originalInstructions = instructions.Cast<object>().ToList<object>();
				}
				List<object> parameter = CodeTranspiler.GetTranspilerCallParameters(generator, transpiler, method, instructions);
				IEnumerable newInstructions = transpiler.Invoke(null, parameter.ToArray()) as IEnumerable;
				if (newInstructions != null)
				{
					instructions = newInstructions;
				}
				if (unassignedValues != null)
				{
					instructions = CodeTranspiler.ConvertToOurInstructions(instructions, typeof(CodeInstruction), originalInstructions, unassignedValues);
				}
			});
			return (instructions as List<CodeInstruction>) ?? instructions.Cast<CodeInstruction>().ToList<CodeInstruction>();
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003520 File Offset: 0x00001720
		private static OpCode ReplaceShortJumps(OpCode opcode)
		{
			foreach (KeyValuePair<OpCode, OpCode> pair in CodeTranspiler.allJumpCodes)
			{
				if (opcode == pair.Key)
				{
					return pair.Value;
				}
			}
			return opcode;
		}

		// Token: 0x0400001E RID: 30
		private readonly IEnumerable<CodeInstruction> codeInstructions;

		// Token: 0x0400001F RID: 31
		private readonly List<MethodInfo> transpilers = new List<MethodInfo>();

		// Token: 0x04000020 RID: 32
		private static readonly Dictionary<OpCode, OpCode> allJumpCodes = new Dictionary<OpCode, OpCode>
		{
			{
				OpCodes.Beq_S,
				OpCodes.Beq
			},
			{
				OpCodes.Bge_S,
				OpCodes.Bge
			},
			{
				OpCodes.Bge_Un_S,
				OpCodes.Bge_Un
			},
			{
				OpCodes.Bgt_S,
				OpCodes.Bgt
			},
			{
				OpCodes.Bgt_Un_S,
				OpCodes.Bgt_Un
			},
			{
				OpCodes.Ble_S,
				OpCodes.Ble
			},
			{
				OpCodes.Ble_Un_S,
				OpCodes.Ble_Un
			},
			{
				OpCodes.Blt_S,
				OpCodes.Blt
			},
			{
				OpCodes.Blt_Un_S,
				OpCodes.Blt_Un
			},
			{
				OpCodes.Bne_Un_S,
				OpCodes.Bne_Un
			},
			{
				OpCodes.Brfalse_S,
				OpCodes.Brfalse
			},
			{
				OpCodes.Brtrue_S,
				OpCodes.Brtrue
			},
			{
				OpCodes.Br_S,
				OpCodes.Br
			},
			{
				OpCodes.Leave_S,
				OpCodes.Leave
			}
		};

		// Token: 0x02000013 RID: 19
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04000021 RID: 33
			public static Func<Type, bool> <0>__IsCodeInstructionsParameter;
		}
	}
}
