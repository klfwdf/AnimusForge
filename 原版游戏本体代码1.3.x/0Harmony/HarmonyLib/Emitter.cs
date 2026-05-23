using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;

namespace HarmonyLib
{
	// Token: 0x0200001B RID: 27
	internal class Emitter
	{
		// Token: 0x0600006E RID: 110 RVA: 0x00003C3E File Offset: 0x00001E3E
		internal Emitter(ILGenerator il)
		{
			this.iLGenerator = il;
			this.il = il.GetProxiedShim<CecilILGenerator>();
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00003C64 File Offset: 0x00001E64
		internal Dictionary<int, CodeInstruction> GetInstructions()
		{
			return this.instructions;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00003C6C File Offset: 0x00001E6C
		internal void AddInstruction(System.Reflection.Emit.OpCode opcode, object operand = null)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, operand));
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00003C86 File Offset: 0x00001E86
		internal int CurrentPos()
		{
			return this.il.ILOffset;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00003C93 File Offset: 0x00001E93
		internal static string CodePos(int offset)
		{
			return string.Format("IL_{0:X4}: ", offset);
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00003CA5 File Offset: 0x00001EA5
		internal string CodePos()
		{
			return Emitter.CodePos(this.CurrentPos());
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00003CB2 File Offset: 0x00001EB2
		internal IEnumerable<VariableDefinition> Variables()
		{
			return this.il.IL.Body.Variables;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00003CCC File Offset: 0x00001ECC
		internal static string FormatOperand(object argument)
		{
			if (argument == null)
			{
				return "NULL";
			}
			Type type = argument.GetType();
			MethodBase method = argument as MethodBase;
			if (method != null)
			{
				return method.FullDescription();
			}
			FieldInfo field = argument as FieldInfo;
			if (field != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 3);
				defaultInterpolatedStringHandler.AppendFormatted(field.FieldType.FullDescription());
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(field.DeclaringType.FullDescription());
				defaultInterpolatedStringHandler.AppendLiteral("::");
				defaultInterpolatedStringHandler.AppendFormatted(field.Name);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			if (type == typeof(Label))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(5, 1);
				defaultInterpolatedStringHandler2.AppendLiteral("Label");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(((Label)argument).GetHashCode());
				return defaultInterpolatedStringHandler2.ToStringAndClear();
			}
			if (type == typeof(Label[]))
			{
				return "Labels" + string.Join(",", (from l in (Label[])argument
					select l.GetHashCode().ToString()).ToArray<string>());
			}
			if (type == typeof(LocalBuilder))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(3, 2);
				defaultInterpolatedStringHandler3.AppendFormatted<int>(((LocalBuilder)argument).LocalIndex);
				defaultInterpolatedStringHandler3.AppendLiteral(" (");
				defaultInterpolatedStringHandler3.AppendFormatted<Type>(((LocalBuilder)argument).LocalType);
				defaultInterpolatedStringHandler3.AppendLiteral(")");
				return defaultInterpolatedStringHandler3.ToStringAndClear();
			}
			if (type == typeof(string))
			{
				return argument.ToString().ToLiteral("\"");
			}
			return argument.ToString().Trim();
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003E8C File Offset: 0x0000208C
		internal LocalBuilder DeclareLocalVariable(Type type, bool isReturnValue = false)
		{
			if (type.IsByRef)
			{
				if (isReturnValue)
				{
					LocalBuilder v = this.il.DeclareLocal(type);
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_1);
					this.Emit(System.Reflection.Emit.OpCodes.Newarr, type.GetElementType());
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_0);
					this.Emit(System.Reflection.Emit.OpCodes.Ldelema, type.GetElementType());
					this.Emit(System.Reflection.Emit.OpCodes.Stloc, v);
					return v;
				}
				type = type.GetElementType();
			}
			if (type.IsEnum)
			{
				type = Enum.GetUnderlyingType(type);
			}
			if (AccessTools.IsClass(type))
			{
				LocalBuilder v2 = this.il.DeclareLocal(type);
				this.Emit(System.Reflection.Emit.OpCodes.Ldnull);
				this.Emit(System.Reflection.Emit.OpCodes.Stloc, v2);
				return v2;
			}
			if (AccessTools.IsStruct(type))
			{
				LocalBuilder v3 = this.il.DeclareLocal(type);
				this.Emit(System.Reflection.Emit.OpCodes.Ldloca, v3);
				this.Emit(System.Reflection.Emit.OpCodes.Initobj, type);
				return v3;
			}
			if (AccessTools.IsValue(type))
			{
				LocalBuilder v4 = this.il.DeclareLocal(type);
				if (type == typeof(float))
				{
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_R4, 0f);
				}
				else if (type == typeof(double))
				{
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_R8, 0.0);
				}
				else if (type == typeof(long) || type == typeof(ulong))
				{
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I8, 0L);
				}
				else
				{
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, 0);
				}
				this.Emit(System.Reflection.Emit.OpCodes.Stloc, v4);
				return v4;
			}
			return null;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00004020 File Offset: 0x00002220
		internal void InitializeOutParameter(int argIndex, Type type)
		{
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			this.Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex);
			if (AccessTools.IsStruct(type))
			{
				this.Emit(System.Reflection.Emit.OpCodes.Initobj, type);
				return;
			}
			if (!AccessTools.IsValue(type))
			{
				this.Emit(System.Reflection.Emit.OpCodes.Ldnull);
				this.Emit(System.Reflection.Emit.OpCodes.Stind_Ref);
				return;
			}
			if (type == typeof(float))
			{
				this.Emit(System.Reflection.Emit.OpCodes.Ldc_R4, 0f);
				this.Emit(System.Reflection.Emit.OpCodes.Stind_R4);
				return;
			}
			if (type == typeof(double))
			{
				this.Emit(System.Reflection.Emit.OpCodes.Ldc_R8, 0.0);
				this.Emit(System.Reflection.Emit.OpCodes.Stind_R8);
				return;
			}
			if (type == typeof(long))
			{
				this.Emit(System.Reflection.Emit.OpCodes.Ldc_I8, 0L);
				this.Emit(System.Reflection.Emit.OpCodes.Stind_I8);
				return;
			}
			this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, 0);
			this.Emit(System.Reflection.Emit.OpCodes.Stind_I4);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00004124 File Offset: 0x00002324
		internal void PrepareArgumentArray(MethodBase original)
		{
			ParameterInfo[] parameters = original.GetParameters();
			int i = 0;
			foreach (ParameterInfo pInfo in parameters)
			{
				int argIndex = i++ + ((!original.IsStatic) ? 1 : 0);
				if (pInfo.IsOut || pInfo.IsRetval)
				{
					this.InitializeOutParameter(argIndex, pInfo.ParameterType);
				}
			}
			this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, parameters.Length);
			this.Emit(System.Reflection.Emit.OpCodes.Newarr, typeof(object));
			i = 0;
			int arrayIdx = 0;
			foreach (ParameterInfo pInfo2 in parameters)
			{
				int argIndex2 = i++ + ((!original.IsStatic) ? 1 : 0);
				Type pType = pInfo2.ParameterType;
				bool paramByRef = pType.IsByRef;
				if (paramByRef)
				{
					pType = pType.GetElementType();
				}
				this.Emit(System.Reflection.Emit.OpCodes.Dup);
				this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, arrayIdx++);
				this.Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex2);
				if (paramByRef)
				{
					if (AccessTools.IsStruct(pType))
					{
						this.Emit(System.Reflection.Emit.OpCodes.Ldobj, pType);
					}
					else
					{
						this.Emit(MethodPatcherTools.LoadIndOpCodeFor(pType));
					}
				}
				if (pType.IsValueType)
				{
					this.Emit(System.Reflection.Emit.OpCodes.Box, pType);
				}
				this.Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00004274 File Offset: 0x00002474
		internal void RestoreArgumentArray(MethodBase original, LocalBuilderState localState)
		{
			ParameterInfo[] parameters = original.GetParameters();
			int i = 0;
			int arrayIdx = 0;
			foreach (ParameterInfo pInfo in parameters)
			{
				int argIndex = i++ + ((!original.IsStatic) ? 1 : 0);
				Type pType = pInfo.ParameterType;
				if (pType.IsByRef)
				{
					pType = pType.GetElementType();
					this.Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex);
					this.Emit(System.Reflection.Emit.OpCodes.Ldloc, localState["__args"]);
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, arrayIdx);
					this.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
					if (pType.IsValueType)
					{
						this.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, pType);
						if (AccessTools.IsStruct(pType))
						{
							this.Emit(System.Reflection.Emit.OpCodes.Stobj, pType);
						}
						else
						{
							this.Emit(MethodPatcherTools.StoreIndOpCodeFor(pType));
						}
					}
					else
					{
						this.Emit(System.Reflection.Emit.OpCodes.Castclass, pType);
						this.Emit(System.Reflection.Emit.OpCodes.Stind_Ref);
					}
				}
				else
				{
					this.Emit(System.Reflection.Emit.OpCodes.Ldloc, localState["__args"]);
					this.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, arrayIdx);
					this.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
					if (pType.IsValueType)
					{
						this.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, pType);
					}
					else
					{
						this.Emit(System.Reflection.Emit.OpCodes.Castclass, pType);
					}
					this.Emit(System.Reflection.Emit.OpCodes.Starg, argIndex);
				}
				arrayIdx++;
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x000043D2 File Offset: 0x000025D2
		internal void MarkLabel(Label label)
		{
			this.il.MarkLabel(label);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000043E0 File Offset: 0x000025E0
		internal void MarkBlockBefore(ExceptionBlock block, out Label? label)
		{
			label = null;
			switch (block.blockType)
			{
			case ExceptionBlockType.BeginExceptionBlock:
				label = new Label?(this.il.BeginExceptionBlock());
				return;
			case ExceptionBlockType.BeginCatchBlock:
				this.il.BeginCatchBlock(block.catchType);
				return;
			case ExceptionBlockType.BeginExceptFilterBlock:
				this.il.BeginExceptFilterBlock();
				return;
			case ExceptionBlockType.BeginFaultBlock:
				this.il.BeginFaultBlock();
				return;
			case ExceptionBlockType.BeginFinallyBlock:
				this.il.BeginFinallyBlock();
				return;
			default:
				return;
			}
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004464 File Offset: 0x00002664
		internal void MarkBlockAfter(ExceptionBlock block)
		{
			ExceptionBlockType blockType = block.blockType;
			if (blockType == ExceptionBlockType.EndExceptionBlock)
			{
				this.il.EndExceptionBlock();
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00004487 File Offset: 0x00002687
		internal void Emit(System.Reflection.Emit.OpCode opcode)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, null));
			this.il.Emit(opcode);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000044AD File Offset: 0x000026AD
		internal void Emit(System.Reflection.Emit.OpCode opcode, LocalBuilder local)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, local));
			this.il.Emit(opcode, local);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000044D4 File Offset: 0x000026D4
		internal void Emit(System.Reflection.Emit.OpCode opcode, FieldInfo field)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, field));
			this.il.Emit(opcode, field);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000044FB File Offset: 0x000026FB
		internal void Emit(System.Reflection.Emit.OpCode opcode, Label[] labels)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, labels));
			this.il.Emit(opcode, labels);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00004522 File Offset: 0x00002722
		internal void Emit(System.Reflection.Emit.OpCode opcode, Label label)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, label));
			this.il.Emit(opcode, label);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x0000454E File Offset: 0x0000274E
		internal void Emit(System.Reflection.Emit.OpCode opcode, string str)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, str));
			this.il.Emit(opcode, str);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00004575 File Offset: 0x00002775
		internal void Emit(System.Reflection.Emit.OpCode opcode, float arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000045A1 File Offset: 0x000027A1
		internal void Emit(System.Reflection.Emit.OpCode opcode, byte arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000045CD File Offset: 0x000027CD
		internal void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000045F9 File Offset: 0x000027F9
		internal void Emit(System.Reflection.Emit.OpCode opcode, double arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00004625 File Offset: 0x00002825
		internal void Emit(System.Reflection.Emit.OpCode opcode, int arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004651 File Offset: 0x00002851
		internal void Emit(System.Reflection.Emit.OpCode opcode, MethodInfo meth)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, meth));
			this.il.Emit(opcode, meth);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004678 File Offset: 0x00002878
		internal void Emit(System.Reflection.Emit.OpCode opcode, short arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x000046A4 File Offset: 0x000028A4
		internal void Emit(System.Reflection.Emit.OpCode opcode, SignatureHelper signature)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, signature));
			this.il.Emit(opcode, signature);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x000046CB File Offset: 0x000028CB
		internal void Emit(System.Reflection.Emit.OpCode opcode, ConstructorInfo con)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, con));
			this.il.Emit(opcode, con);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000046F2 File Offset: 0x000028F2
		internal void Emit(System.Reflection.Emit.OpCode opcode, Type cls)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, cls));
			this.il.Emit(opcode, cls);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004719 File Offset: 0x00002919
		internal void Emit(System.Reflection.Emit.OpCode opcode, long arg)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, arg));
			this.il.Emit(opcode, arg);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00004745 File Offset: 0x00002945
		internal void Emit(System.Reflection.Emit.OpCode opcode, ICallSiteGenerator operand)
		{
			this.il.Emit(opcode, operand);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004754 File Offset: 0x00002954
		internal void EmitCall(System.Reflection.Emit.OpCode opcode, MethodInfo methodInfo)
		{
			this.instructions.Add(this.CurrentPos(), new CodeInstruction(opcode, methodInfo));
			this.il.EmitCall(opcode, methodInfo, null);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x0000477C File Offset: 0x0000297C
		internal void DynEmit(System.Reflection.Emit.OpCode opcode, object operand)
		{
			this.iLGenerator.DynEmit(opcode, operand);
		}

		// Token: 0x04000043 RID: 67
		private readonly ILGenerator iLGenerator;

		// Token: 0x04000044 RID: 68
		private readonly CecilILGenerator il;

		// Token: 0x04000045 RID: 69
		private readonly Dictionary<int, CodeInstruction> instructions = new Dictionary<int, CodeInstruction>();
	}
}
