using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x0200002F RID: 47
	internal class MethodBodyReader
	{
		// Token: 0x060000E8 RID: 232 RVA: 0x00006168 File Offset: 0x00004368
		internal static List<ILInstruction> GetInstructions(ILGenerator generator, MethodBase method)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			MethodBodyReader reader = new MethodBodyReader(method, generator);
			reader.DeclareVariables(null);
			reader.GenerateInstructions();
			return reader.ilInstructions;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x000061A0 File Offset: 0x000043A0
		internal MethodBodyReader(MethodBase method, ILGenerator generator)
		{
			this.generator = generator;
			this.method = method;
			this.module = method.Module;
			MethodBody body = method.GetMethodBody();
			int? num;
			if (body == null)
			{
				num = null;
			}
			else
			{
				byte[] ilasByteArray = body.GetILAsByteArray();
				num = ((ilasByteArray != null) ? new int?(ilasByteArray.Length) : null);
			}
			int? num2 = num;
			if (num2.GetValueOrDefault() == 0)
			{
				this.ilBytes = new ByteBuffer(Array.Empty<byte>());
				this.ilInstructions = new List<ILInstruction>();
			}
			else
			{
				byte[] bytes = body.GetILAsByteArray();
				if (bytes == null)
				{
					throw new ArgumentException("Can not get IL bytes of method " + method.FullDescription());
				}
				this.ilBytes = new ByteBuffer(bytes);
				this.ilInstructions = new List<ILInstruction>((bytes.Length + 1) / 2);
			}
			Type type = method.DeclaringType;
			if (type != null && type.IsGenericType)
			{
				try
				{
					this.typeArguments = type.GetGenericArguments();
				}
				catch
				{
					this.typeArguments = null;
				}
			}
			if (method.IsGenericMethod)
			{
				try
				{
					this.methodArguments = method.GetGenericArguments();
				}
				catch
				{
					this.methodArguments = null;
				}
			}
			if (!method.IsStatic)
			{
				this.this_parameter = new MethodBodyReader.ThisParameter(method);
			}
			this.parameters = method.GetParameters();
			List<LocalVariableInfo> list;
			if (body == null)
			{
				list = null;
			}
			else
			{
				IList<LocalVariableInfo> list2 = body.LocalVariables;
				list = ((list2 != null) ? list2.ToList<LocalVariableInfo>() : null);
			}
			this.localVariables = list ?? new List<LocalVariableInfo>();
			this.exceptions = ((body != null) ? body.ExceptionHandlingClauses : null) ?? new List<ExceptionHandlingClause>();
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000632C File Offset: 0x0000452C
		internal void SetDebugging(bool debug)
		{
			this.debug = debug;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00006338 File Offset: 0x00004538
		internal void GenerateInstructions()
		{
			while (this.ilBytes.position < this.ilBytes.buffer.Length)
			{
				int loc = this.ilBytes.position;
				ILInstruction instruction = new ILInstruction(this.ReadOpCode(), null)
				{
					offset = loc
				};
				this.ReadOperand(instruction);
				this.ilInstructions.Add(instruction);
			}
			this.HandleNativeMethod();
			this.ResolveBranches();
			this.ParseExceptions();
		}

		// Token: 0x060000EC RID: 236 RVA: 0x000063A8 File Offset: 0x000045A8
		internal void HandleNativeMethod()
		{
			MethodInfo methodInfo = this.method as MethodInfo;
			if (methodInfo == null)
			{
				return;
			}
			if (methodInfo.ReflectedType != null)
			{
				return;
			}
			DllImportAttribute dllAttribute = methodInfo.GetCustomAttributes(false).OfType<DllImportAttribute>().FirstOrDefault<DllImportAttribute>();
			if (dllAttribute == null)
			{
				return;
			}
			string[] paramTypes = (from p in methodInfo.GetParameters()
				select p.ParameterType.FullName ?? p.ParameterType.Name).ToArray<string>();
			string paramSignature = string.Join("_", paramTypes);
			string paramHash = ((paramSignature.Length > 0) ? paramSignature.GetHashCode().ToString("X") : "0");
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
			Type declaringType = methodInfo.DeclaringType;
			defaultInterpolatedStringHandler.AppendFormatted((((declaringType != null) ? declaringType.FullName : null) ?? "").Replace(".", "_"));
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(methodInfo.Name);
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(paramHash);
			string name = defaultInterpolatedStringHandler.ToStringAndClear();
			AssemblyName assemblyName = new AssemblyName(name);
			AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule(assemblyName.Name);
			TypeBuilder typeBuilder = dynamicModule.DefineType("NativeMethodHolder", TypeAttributes.Public | TypeAttributes.UnicodeClass);
			MethodBuilder methodBuilder = typeBuilder.DefinePInvokeMethod(methodInfo.Name, dllAttribute.Value, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.PinvokeImpl, CallingConventions.Standard, methodInfo.ReturnType, (from x in methodInfo.GetParameters()
				select x.ParameterType).ToArray<Type>(), dllAttribute.CallingConvention, dllAttribute.CharSet);
			methodBuilder.SetImplementationFlags(methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);
			Type type = typeBuilder.CreateType();
			MethodInfo proxyMethod = type.GetMethod(methodInfo.Name);
			int argCount = this.method.GetParameters().Length;
			for (int i = 0; i < argCount; i++)
			{
				this.ilInstructions.Add(new ILInstruction(OpCodes.Ldarg, i)
				{
					offset = 0
				});
			}
			this.ilInstructions.Add(new ILInstruction(OpCodes.Call, proxyMethod)
			{
				offset = argCount
			});
			this.ilInstructions.Add(new ILInstruction(OpCodes.Ret, null)
			{
				offset = argCount + 5
			});
		}

		// Token: 0x060000ED RID: 237 RVA: 0x000065F7 File Offset: 0x000047F7
		internal void DeclareVariables(LocalBuilder[] existingVariables)
		{
			if (this.generator == null)
			{
				return;
			}
			if (existingVariables != null)
			{
				this.variables = existingVariables;
				return;
			}
			this.variables = (from lvi in this.localVariables
				select this.generator.DeclareLocal(lvi.LocalType, lvi.IsPinned)).ToArray<LocalBuilder>();
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00006630 File Offset: 0x00004830
		private void ResolveBranches()
		{
			foreach (ILInstruction ilInstruction in this.ilInstructions)
			{
				OperandType operandType = ilInstruction.opcode.OperandType;
				if (operandType != OperandType.InlineBrTarget)
				{
					if (operandType == OperandType.InlineSwitch)
					{
						int[] offsets = (int[])ilInstruction.operand;
						ILInstruction[] branches = new ILInstruction[offsets.Length];
						for (int i = 0; i < offsets.Length; i++)
						{
							branches[i] = this.GetInstruction(offsets[i], false);
						}
						ilInstruction.operand = branches;
						continue;
					}
					if (operandType != OperandType.ShortInlineBrTarget)
					{
						continue;
					}
				}
				ilInstruction.operand = this.GetInstruction((int)ilInstruction.operand, false);
			}
		}

		// Token: 0x060000EF RID: 239 RVA: 0x000066F4 File Offset: 0x000048F4
		private void ParseExceptions()
		{
			foreach (ExceptionHandlingClause exception in this.exceptions)
			{
				int try_start = exception.TryOffset;
				int handler_start = exception.HandlerOffset;
				int handler_end = exception.HandlerOffset + exception.HandlerLength - 1;
				ILInstruction instr = this.GetInstruction(try_start, false);
				instr.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null));
				ILInstruction instr2 = this.GetInstruction(handler_end, true);
				instr2.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null));
				switch (exception.Flags)
				{
				case ExceptionHandlingClauseOptions.Clause:
				{
					ILInstruction instr3 = this.GetInstruction(handler_start, false);
					instr3.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, exception.CatchType));
					break;
				}
				case ExceptionHandlingClauseOptions.Filter:
				{
					ILInstruction instr4 = this.GetInstruction(exception.FilterOffset, false);
					instr4.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptFilterBlock, null));
					break;
				}
				case ExceptionHandlingClauseOptions.Finally:
				{
					ILInstruction instr5 = this.GetInstruction(handler_start, false);
					instr5.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
					break;
				}
				case ExceptionHandlingClauseOptions.Fault:
				{
					ILInstruction instr6 = this.GetInstruction(handler_start, false);
					instr6.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock, null));
					break;
				}
				}
			}
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00006854 File Offset: 0x00004A54
		private bool EndsInDeadCode(List<CodeInstruction> list)
		{
			int i = list.Count;
			if (i < 2 || list.Last<CodeInstruction>().opcode != OpCodes.Throw)
			{
				return false;
			}
			return list.GetRange(0, i - 1).All((CodeInstruction code) => code.opcode != OpCodes.Ret);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x000068B4 File Offset: 0x00004AB4
		internal List<CodeInstruction> FinalizeILCodes(List<MethodInfo> transpilers, bool stripLastReturn, out bool hasReturnCode, out bool methodEndsInDeadCode, List<Label> endLabels)
		{
			hasReturnCode = false;
			methodEndsInDeadCode = false;
			if (this.generator == null)
			{
				return null;
			}
			foreach (ILInstruction ilInstruction in this.ilInstructions)
			{
				OperandType operandType = ilInstruction.opcode.OperandType;
				if (operandType != OperandType.InlineBrTarget)
				{
					if (operandType != OperandType.InlineSwitch)
					{
						if (operandType != OperandType.ShortInlineBrTarget)
						{
							continue;
						}
					}
					else
					{
						ILInstruction[] targets = ilInstruction.operand as ILInstruction[];
						if (targets != null)
						{
							List<Label> labels = new List<Label>();
							foreach (ILInstruction target in targets)
							{
								Label label = this.generator.DefineLabel();
								target.labels.Add(label);
								labels.Add(label);
							}
							ilInstruction.argument = labels.ToArray();
							continue;
						}
						continue;
					}
				}
				ILInstruction target2 = ilInstruction.operand as ILInstruction;
				if (target2 != null)
				{
					Label label2 = this.generator.DefineLabel();
					target2.labels.Add(label2);
					ilInstruction.argument = label2;
				}
			}
			CodeTranspiler codeTranspiler = new CodeTranspiler(this.ilInstructions);
			transpilers.Do(new Action<MethodInfo>(codeTranspiler.Add));
			List<CodeInstruction> codeInstructions = codeTranspiler.GetResult(this.generator, this.method);
			hasReturnCode = codeInstructions.Any((CodeInstruction code) => code.opcode == OpCodes.Ret);
			methodEndsInDeadCode = this.EndsInDeadCode(codeInstructions);
			while (stripLastReturn)
			{
				CodeInstruction lastInstruction = codeInstructions.LastOrDefault<CodeInstruction>();
				if (lastInstruction == null || lastInstruction.opcode != OpCodes.Ret)
				{
					break;
				}
				if (endLabels != null)
				{
					endLabels.AddRange(lastInstruction.labels);
				}
				codeInstructions.RemoveAt(codeInstructions.Count - 1);
			}
			return codeInstructions;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00006A88 File Offset: 0x00004C88
		private static void GetMemberInfoValue(MemberInfo info, out object result)
		{
			result = null;
			MemberTypes memberType = info.MemberType;
			if (memberType <= MemberTypes.Method)
			{
				switch (memberType)
				{
				case MemberTypes.Constructor:
					result = (ConstructorInfo)info;
					return;
				case MemberTypes.Event:
					result = (EventInfo)info;
					return;
				case MemberTypes.Constructor | MemberTypes.Event:
					break;
				case MemberTypes.Field:
					result = (FieldInfo)info;
					return;
				default:
					if (memberType != MemberTypes.Method)
					{
						return;
					}
					result = (MethodInfo)info;
					return;
				}
			}
			else if (memberType != MemberTypes.Property)
			{
				if (memberType != MemberTypes.TypeInfo && memberType != MemberTypes.NestedType)
				{
					return;
				}
				result = (Type)info;
				return;
			}
			else
			{
				result = (PropertyInfo)info;
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00006B08 File Offset: 0x00004D08
		private void ReadOperand(ILInstruction instruction)
		{
			switch (instruction.opcode.OperandType)
			{
			case OperandType.InlineBrTarget:
			{
				int val = this.ilBytes.ReadInt32();
				instruction.operand = val + this.ilBytes.position;
				return;
			}
			case OperandType.InlineField:
			{
				int val2 = this.ilBytes.ReadInt32();
				instruction.operand = this.module.ResolveField(val2, this.typeArguments, this.methodArguments);
				Type declaringType = ((MemberInfo)instruction.operand).DeclaringType;
				if (declaringType != null)
				{
					declaringType.FixReflectionCacheAuto();
				}
				instruction.argument = (FieldInfo)instruction.operand;
				return;
			}
			case OperandType.InlineI:
			{
				int val3 = this.ilBytes.ReadInt32();
				instruction.operand = val3;
				instruction.argument = (int)instruction.operand;
				return;
			}
			case OperandType.InlineI8:
			{
				long val4 = this.ilBytes.ReadInt64();
				instruction.operand = val4;
				instruction.argument = (long)instruction.operand;
				return;
			}
			case OperandType.InlineMethod:
			{
				int val5 = this.ilBytes.ReadInt32();
				instruction.operand = this.module.ResolveMethod(val5, this.typeArguments, this.methodArguments);
				Type declaringType2 = ((MemberInfo)instruction.operand).DeclaringType;
				if (declaringType2 != null)
				{
					declaringType2.FixReflectionCacheAuto();
				}
				if (instruction.operand is ConstructorInfo)
				{
					instruction.argument = (ConstructorInfo)instruction.operand;
					return;
				}
				instruction.argument = (MethodInfo)instruction.operand;
				return;
			}
			case OperandType.InlineNone:
				instruction.argument = null;
				return;
			case OperandType.InlineR:
			{
				double val6 = this.ilBytes.ReadDouble();
				instruction.operand = val6;
				instruction.argument = (double)instruction.operand;
				return;
			}
			case OperandType.InlineSig:
			{
				int val7 = this.ilBytes.ReadInt32();
				byte[] bytes = this.module.ResolveSignature(val7);
				InlineSignature signature = InlineSignatureParser.ImportCallSite(this.module, bytes);
				instruction.operand = signature;
				instruction.argument = signature;
				return;
			}
			case OperandType.InlineString:
			{
				int val8 = this.ilBytes.ReadInt32();
				instruction.operand = this.module.ResolveString(val8);
				instruction.argument = (string)instruction.operand;
				return;
			}
			case OperandType.InlineSwitch:
			{
				int length = this.ilBytes.ReadInt32();
				int base_offset = this.ilBytes.position + 4 * length;
				int[] branches = new int[length];
				for (int i = 0; i < length; i++)
				{
					branches[i] = this.ilBytes.ReadInt32() + base_offset;
				}
				instruction.operand = branches;
				return;
			}
			case OperandType.InlineTok:
			{
				int val9 = this.ilBytes.ReadInt32();
				instruction.operand = this.module.ResolveMember(val9, this.typeArguments, this.methodArguments);
				Type declaringType3 = ((MemberInfo)instruction.operand).DeclaringType;
				if (declaringType3 != null)
				{
					declaringType3.FixReflectionCacheAuto();
				}
				MethodBodyReader.GetMemberInfoValue((MemberInfo)instruction.operand, out instruction.argument);
				return;
			}
			case OperandType.InlineType:
			{
				int val10 = this.ilBytes.ReadInt32();
				instruction.operand = this.module.ResolveType(val10, this.typeArguments, this.methodArguments);
				((Type)instruction.operand).FixReflectionCacheAuto();
				instruction.argument = (Type)instruction.operand;
				return;
			}
			case OperandType.InlineVar:
			{
				short idx = this.ilBytes.ReadInt16();
				if (!MethodBodyReader.TargetsLocalVariable(instruction.opcode))
				{
					instruction.operand = this.GetParameter((int)idx);
					instruction.argument = idx;
					return;
				}
				LocalVariableInfo lvi = this.GetLocalVariable((int)idx);
				if (lvi == null)
				{
					instruction.argument = idx;
					return;
				}
				instruction.operand = lvi;
				LocalBuilder[] array = this.variables;
				instruction.argument = ((array != null) ? array[lvi.LocalIndex] : null) ?? lvi;
				return;
			}
			case OperandType.ShortInlineBrTarget:
			{
				sbyte val11 = (sbyte)this.ilBytes.ReadByte();
				instruction.operand = (int)val11 + this.ilBytes.position;
				return;
			}
			case OperandType.ShortInlineI:
			{
				if (instruction.opcode == OpCodes.Ldc_I4_S)
				{
					sbyte sb = (sbyte)this.ilBytes.ReadByte();
					instruction.operand = sb;
					instruction.argument = (sbyte)instruction.operand;
					return;
				}
				byte b = this.ilBytes.ReadByte();
				instruction.operand = b;
				instruction.argument = (byte)instruction.operand;
				return;
			}
			case OperandType.ShortInlineR:
			{
				float val12 = this.ilBytes.ReadSingle();
				instruction.operand = val12;
				instruction.argument = (float)instruction.operand;
				return;
			}
			case OperandType.ShortInlineVar:
			{
				byte idx2 = this.ilBytes.ReadByte();
				if (!MethodBodyReader.TargetsLocalVariable(instruction.opcode))
				{
					instruction.operand = this.GetParameter((int)idx2);
					instruction.argument = idx2;
					return;
				}
				LocalVariableInfo lvi2 = this.GetLocalVariable((int)idx2);
				if (lvi2 == null)
				{
					instruction.argument = idx2;
					return;
				}
				instruction.operand = lvi2;
				LocalBuilder[] array2 = this.variables;
				instruction.argument = ((array2 != null) ? array2[lvi2.LocalIndex] : null) ?? lvi2;
				return;
			}
			}
			throw new NotSupportedException();
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000703C File Offset: 0x0000523C
		private ILInstruction GetInstruction(int offset, bool isEndOfInstruction)
		{
			if (offset < 0)
			{
				string paramName = "offset";
				object actualValue = offset;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Instruction offset ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(offset);
				defaultInterpolatedStringHandler.AppendLiteral(" is less than 0");
				throw new ArgumentOutOfRangeException(paramName, actualValue, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int lastInstructionIndex = this.ilInstructions.Count - 1;
			ILInstruction instruction = this.ilInstructions[lastInstructionIndex];
			if (offset > instruction.offset + instruction.GetSize() - 1)
			{
				string paramName2 = "offset";
				object actualValue2 = offset;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(47, 2);
				defaultInterpolatedStringHandler2.AppendLiteral("Instruction offset ");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(offset);
				defaultInterpolatedStringHandler2.AppendLiteral(" is outside valid range 0 - ");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(instruction.offset + instruction.GetSize() - 1);
				throw new ArgumentOutOfRangeException(paramName2, actualValue2, defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			int min = 0;
			int max = lastInstructionIndex;
			while (min <= max)
			{
				int mid = min + (max - min) / 2;
				instruction = this.ilInstructions[mid];
				if (isEndOfInstruction)
				{
					if (offset == instruction.offset + instruction.GetSize() - 1)
					{
						return instruction;
					}
				}
				else if (offset == instruction.offset)
				{
					return instruction;
				}
				if (offset < instruction.offset)
				{
					max = mid - 1;
				}
				else
				{
					min = mid + 1;
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(28, 1);
			defaultInterpolatedStringHandler3.AppendLiteral("Cannot find instruction for ");
			defaultInterpolatedStringHandler3.AppendFormatted<int>(offset, "X4");
			throw new Exception(defaultInterpolatedStringHandler3.ToStringAndClear());
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000719B File Offset: 0x0000539B
		private static bool TargetsLocalVariable(OpCode opcode)
		{
			return opcode.Name.Contains("loc");
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000071AE File Offset: 0x000053AE
		private LocalVariableInfo GetLocalVariable(int index)
		{
			List<LocalVariableInfo> list = this.localVariables;
			if (list == null)
			{
				return null;
			}
			return list[index];
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x000071C2 File Offset: 0x000053C2
		private ParameterInfo GetParameter(int index)
		{
			if (index == 0)
			{
				return this.this_parameter;
			}
			return this.parameters[index - 1];
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x000071D8 File Offset: 0x000053D8
		private OpCode ReadOpCode()
		{
			byte op = this.ilBytes.ReadByte();
			if (op == 254)
			{
				return MethodBodyReader.two_bytes_opcodes[(int)this.ilBytes.ReadByte()];
			}
			return MethodBodyReader.one_byte_opcodes[(int)op];
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000721C File Offset: 0x0000541C
		[MethodImpl(MethodImplOptions.Synchronized)]
		static MethodBodyReader()
		{
			FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo field in fields)
			{
				OpCode opcode = (OpCode)field.GetValue(null);
				if (opcode.OpCodeType != OpCodeType.Nternal)
				{
					if (opcode.Size == 1)
					{
						MethodBodyReader.one_byte_opcodes[(int)opcode.Value] = opcode;
					}
					else
					{
						MethodBodyReader.two_bytes_opcodes[(int)(opcode.Value & 255)] = opcode;
					}
				}
			}
		}

		// Token: 0x0400008A RID: 138
		private readonly ILGenerator generator;

		// Token: 0x0400008B RID: 139
		private readonly MethodBase method;

		// Token: 0x0400008C RID: 140
		private bool debug;

		// Token: 0x0400008D RID: 141
		private readonly Module module;

		// Token: 0x0400008E RID: 142
		private readonly Type[] typeArguments;

		// Token: 0x0400008F RID: 143
		private readonly Type[] methodArguments;

		// Token: 0x04000090 RID: 144
		private readonly ByteBuffer ilBytes;

		// Token: 0x04000091 RID: 145
		private readonly ParameterInfo this_parameter;

		// Token: 0x04000092 RID: 146
		private readonly ParameterInfo[] parameters;

		// Token: 0x04000093 RID: 147
		private readonly IList<ExceptionHandlingClause> exceptions;

		// Token: 0x04000094 RID: 148
		private readonly List<ILInstruction> ilInstructions;

		// Token: 0x04000095 RID: 149
		private readonly List<LocalVariableInfo> localVariables;

		// Token: 0x04000096 RID: 150
		private LocalBuilder[] variables;

		// Token: 0x04000097 RID: 151
		private static readonly OpCode[] one_byte_opcodes = new OpCode[225];

		// Token: 0x04000098 RID: 152
		private static readonly OpCode[] two_bytes_opcodes = new OpCode[31];

		// Token: 0x02000030 RID: 48
		private class ThisParameter : ParameterInfo
		{
			// Token: 0x060000FB RID: 251 RVA: 0x000072D3 File Offset: 0x000054D3
			internal ThisParameter(MethodBase method)
			{
				this.MemberImpl = method;
				this.ClassImpl = method.DeclaringType;
				this.NameImpl = "this";
				this.PositionImpl = -1;
			}
		}
	}
}
