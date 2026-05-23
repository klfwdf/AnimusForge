using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace MonoMod.Utils.Cil
{
	// Token: 0x020008F7 RID: 2295
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class CecilILGenerator : ILGeneratorShim
	{
		// Token: 0x06003010 RID: 12304 RVA: 0x000A5DA0 File Offset: 0x000A3FA0
		unsafe static CecilILGenerator()
		{
			FieldInfo[] fields = typeof(Mono.Cecil.Cil.OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int j = 0; j < fields.Length; j++)
			{
				Mono.Cecil.Cil.OpCode cecilOpCode = (Mono.Cecil.Cil.OpCode)fields[j].GetValue(null);
				CecilILGenerator._MCCOpCodes[cecilOpCode.Value] = cecilOpCode;
			}
			Label i = default(Label);
			*Unsafe.As<Label, int>(ref i) = -1;
			CecilILGenerator.NullLabel = i;
		}

		// Token: 0x17000865 RID: 2149
		// (get) Token: 0x06003011 RID: 12305 RVA: 0x000A5E94 File Offset: 0x000A4094
		public ILProcessor IL { get; }

		// Token: 0x06003012 RID: 12306 RVA: 0x000A5E9C File Offset: 0x000A409C
		public CecilILGenerator(ILProcessor il)
		{
			this.IL = il;
		}

		// Token: 0x06003013 RID: 12307 RVA: 0x000A5EED File Offset: 0x000A40ED
		private static Mono.Cecil.Cil.OpCode _(System.Reflection.Emit.OpCode opcode)
		{
			return CecilILGenerator._MCCOpCodes[opcode.Value];
		}

		// Token: 0x06003014 RID: 12308 RVA: 0x000A5F00 File Offset: 0x000A4100
		[NullableContext(2)]
		private CecilILGenerator.LabelInfo _(Label handle)
		{
			CecilILGenerator.LabelInfo labelInfo;
			if (!this._LabelInfos.TryGetValue(handle, out labelInfo))
			{
				return null;
			}
			return labelInfo;
		}

		// Token: 0x06003015 RID: 12309 RVA: 0x000A5F20 File Offset: 0x000A4120
		private VariableDefinition _(LocalBuilder handle)
		{
			return this._Variables[handle];
		}

		// Token: 0x06003016 RID: 12310 RVA: 0x000A5F2E File Offset: 0x000A412E
		private TypeReference _(Type info)
		{
			return this.IL.Body.Method.Module.ImportReference(info);
		}

		// Token: 0x06003017 RID: 12311 RVA: 0x000A5F4B File Offset: 0x000A414B
		private FieldReference _(FieldInfo info)
		{
			return this.IL.Body.Method.Module.ImportReference(info);
		}

		// Token: 0x06003018 RID: 12312 RVA: 0x000A5F68 File Offset: 0x000A4168
		private MethodReference _(MethodBase info)
		{
			return this.IL.Body.Method.Module.ImportReference(info);
		}

		// Token: 0x17000866 RID: 2150
		// (get) Token: 0x06003019 RID: 12313 RVA: 0x000A5F85 File Offset: 0x000A4185
		public override int ILOffset
		{
			get
			{
				return this._ILOffset;
			}
		}

		// Token: 0x0600301A RID: 12314 RVA: 0x000A5F90 File Offset: 0x000A4190
		private Instruction ProcessLabels(Instruction ins)
		{
			if (this._LabelsToMark.Count != 0)
			{
				foreach (CecilILGenerator.LabelInfo labelInfo in this._LabelsToMark)
				{
					foreach (Instruction insToFix in labelInfo.Branches)
					{
						object operand = insToFix.Operand;
						if (!(operand is Instruction))
						{
							Instruction[] instrsOperand = operand as Instruction[];
							if (instrsOperand != null)
							{
								for (int i = 0; i < instrsOperand.Length; i++)
								{
									if (instrsOperand[i] == labelInfo.Instruction)
									{
										instrsOperand[i] = ins;
										break;
									}
								}
							}
						}
						else
						{
							insToFix.Operand = ins;
						}
					}
					labelInfo.Emitted = true;
					labelInfo.Instruction = ins;
				}
				this._LabelsToMark.Clear();
			}
			if (this._ExceptionHandlersToMark.Count != 0)
			{
				foreach (CecilILGenerator.LabelledExceptionHandler exHandler in this._ExceptionHandlersToMark)
				{
					Collection<Mono.Cecil.Cil.ExceptionHandler> exceptionHandlers = this.IL.Body.ExceptionHandlers;
					Mono.Cecil.Cil.ExceptionHandler exceptionHandler = new Mono.Cecil.Cil.ExceptionHandler(exHandler.HandlerType);
					CecilILGenerator.LabelInfo labelInfo2 = this._(exHandler.TryStart);
					exceptionHandler.TryStart = ((labelInfo2 != null) ? labelInfo2.Instruction : null);
					CecilILGenerator.LabelInfo labelInfo3 = this._(exHandler.TryEnd);
					exceptionHandler.TryEnd = ((labelInfo3 != null) ? labelInfo3.Instruction : null);
					CecilILGenerator.LabelInfo labelInfo4 = this._(exHandler.HandlerStart);
					exceptionHandler.HandlerStart = ((labelInfo4 != null) ? labelInfo4.Instruction : null);
					CecilILGenerator.LabelInfo labelInfo5 = this._(exHandler.HandlerEnd);
					exceptionHandler.HandlerEnd = ((labelInfo5 != null) ? labelInfo5.Instruction : null);
					CecilILGenerator.LabelInfo labelInfo6 = this._(exHandler.FilterStart);
					exceptionHandler.FilterStart = ((labelInfo6 != null) ? labelInfo6.Instruction : null);
					exceptionHandler.CatchType = exHandler.ExceptionType;
					exceptionHandlers.Add(exceptionHandler);
				}
				this._ExceptionHandlersToMark.Clear();
			}
			return ins;
		}

		// Token: 0x0600301B RID: 12315 RVA: 0x000A61C0 File Offset: 0x000A43C0
		public unsafe override Label DefineLabel()
		{
			Label handle = default(Label);
			ref int ptr = ref *(int*)(&handle);
			int num = this.labelCounter;
			this.labelCounter = num + 1;
			ptr = num;
			this._LabelInfos[handle] = new CecilILGenerator.LabelInfo();
			return handle;
		}

		// Token: 0x0600301C RID: 12316 RVA: 0x000A61FC File Offset: 0x000A43FC
		public override void MarkLabel(Label loc)
		{
			CecilILGenerator.LabelInfo labelInfo;
			if (!this._LabelInfos.TryGetValue(loc, out labelInfo) || labelInfo.Emitted)
			{
				return;
			}
			this._LabelsToMark.Add(labelInfo);
		}

		// Token: 0x0600301D RID: 12317 RVA: 0x000A622E File Offset: 0x000A442E
		public override LocalBuilder DeclareLocal(Type localType)
		{
			return this.DeclareLocal(localType, false);
		}

		// Token: 0x0600301E RID: 12318 RVA: 0x000A6238 File Offset: 0x000A4438
		public override LocalBuilder DeclareLocal(Type localType, bool pinned)
		{
			int index = this.IL.Body.Variables.Count;
			object obj;
			if (CecilILGenerator.c_LocalBuilder_params != 4)
			{
				if (CecilILGenerator.c_LocalBuilder_params != 3)
				{
					if (CecilILGenerator.c_LocalBuilder_params != 2)
					{
						if (CecilILGenerator.c_LocalBuilder_params != 0)
						{
							throw new NotSupportedException();
						}
						obj = CecilILGenerator.c_LocalBuilder.Invoke(ArrayEx.Empty<object>());
					}
					else
					{
						ConstructorInfo constructorInfo = CecilILGenerator.c_LocalBuilder;
						object[] array = new object[2];
						array[0] = localType;
						obj = constructorInfo.Invoke(array);
					}
				}
				else
				{
					ConstructorInfo constructorInfo2 = CecilILGenerator.c_LocalBuilder;
					object[] array2 = new object[3];
					array2[0] = index;
					array2[1] = localType;
					obj = constructorInfo2.Invoke(array2);
				}
			}
			else
			{
				obj = CecilILGenerator.c_LocalBuilder.Invoke(new object[] { index, localType, null, pinned });
			}
			LocalBuilder handle = (LocalBuilder)obj;
			FieldInfo fieldInfo = CecilILGenerator.f_LocalBuilder_position;
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(handle, (ushort)index);
			}
			FieldInfo fieldInfo2 = CecilILGenerator.f_LocalBuilder_is_pinned;
			if (fieldInfo2 != null)
			{
				fieldInfo2.SetValue(handle, pinned);
			}
			TypeReference typeRef = this._(localType);
			if (pinned)
			{
				typeRef = new PinnedType(typeRef);
			}
			VariableDefinition def = new VariableDefinition(typeRef);
			this.IL.Body.Variables.Add(def);
			this._Variables[handle] = def;
			return handle;
		}

		// Token: 0x0600301F RID: 12319 RVA: 0x000A635E File Offset: 0x000A455E
		private void Emit(Instruction ins)
		{
			ins.Offset = this._ILOffset;
			this._ILOffset += ins.GetSize();
			this.IL.Append(this.ProcessLabels(ins));
		}

		// Token: 0x06003020 RID: 12320 RVA: 0x000A6391 File Offset: 0x000A4591
		public override void Emit(System.Reflection.Emit.OpCode opcode)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode)));
		}

		// Token: 0x06003021 RID: 12321 RVA: 0x000A63AA File Offset: 0x000A45AA
		public override void Emit(System.Reflection.Emit.OpCode opcode, byte arg)
		{
			if (opcode.OperandType == System.Reflection.Emit.OperandType.ShortInlineVar || opcode.OperandType == System.Reflection.Emit.OperandType.InlineVar)
			{
				this._EmitInlineVar(CecilILGenerator._(opcode), (int)arg);
				return;
			}
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003022 RID: 12322 RVA: 0x000A63E8 File Offset: 0x000A45E8
		public override void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg)
		{
			if (opcode.OperandType == System.Reflection.Emit.OperandType.ShortInlineVar || opcode.OperandType == System.Reflection.Emit.OperandType.InlineVar)
			{
				this._EmitInlineVar(CecilILGenerator._(opcode), (int)arg);
				return;
			}
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003023 RID: 12323 RVA: 0x000A6426 File Offset: 0x000A4626
		public override void Emit(System.Reflection.Emit.OpCode opcode, short arg)
		{
			if (opcode.OperandType == System.Reflection.Emit.OperandType.ShortInlineVar || opcode.OperandType == System.Reflection.Emit.OperandType.InlineVar)
			{
				this._EmitInlineVar(CecilILGenerator._(opcode), (int)arg);
				return;
			}
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), (int)arg));
		}

		// Token: 0x06003024 RID: 12324 RVA: 0x000A6464 File Offset: 0x000A4664
		public override void Emit(System.Reflection.Emit.OpCode opcode, int arg)
		{
			if (opcode.OperandType == System.Reflection.Emit.OperandType.ShortInlineVar || opcode.OperandType == System.Reflection.Emit.OperandType.InlineVar)
			{
				this._EmitInlineVar(CecilILGenerator._(opcode), arg);
				return;
			}
			string name = opcode.Name;
			if (name != null && name.EndsWith(".s", StringComparison.Ordinal))
			{
				this.Emit(this.IL.Create(CecilILGenerator._(opcode), (sbyte)arg));
				return;
			}
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003025 RID: 12325 RVA: 0x000A64E2 File Offset: 0x000A46E2
		public override void Emit(System.Reflection.Emit.OpCode opcode, long arg)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003026 RID: 12326 RVA: 0x000A64FC File Offset: 0x000A46FC
		public override void Emit(System.Reflection.Emit.OpCode opcode, float arg)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003027 RID: 12327 RVA: 0x000A6516 File Offset: 0x000A4716
		public override void Emit(System.Reflection.Emit.OpCode opcode, double arg)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), arg));
		}

		// Token: 0x06003028 RID: 12328 RVA: 0x000A6530 File Offset: 0x000A4730
		public override void Emit(System.Reflection.Emit.OpCode opcode, string str)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), str));
		}

		// Token: 0x06003029 RID: 12329 RVA: 0x000A654A File Offset: 0x000A474A
		public override void Emit(System.Reflection.Emit.OpCode opcode, Type cls)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(cls)));
		}

		// Token: 0x0600302A RID: 12330 RVA: 0x000A656A File Offset: 0x000A476A
		public override void Emit(System.Reflection.Emit.OpCode opcode, FieldInfo field)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(field)));
		}

		// Token: 0x0600302B RID: 12331 RVA: 0x000A658A File Offset: 0x000A478A
		public override void Emit(System.Reflection.Emit.OpCode opcode, ConstructorInfo con)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(con)));
		}

		// Token: 0x0600302C RID: 12332 RVA: 0x000A658A File Offset: 0x000A478A
		public override void Emit(System.Reflection.Emit.OpCode opcode, MethodInfo meth)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(meth)));
		}

		// Token: 0x0600302D RID: 12333 RVA: 0x000A65AC File Offset: 0x000A47AC
		public override void Emit(System.Reflection.Emit.OpCode opcode, Label label)
		{
			CecilILGenerator.LabelInfo labelInfo = this._(label);
			Instruction ins = this.IL.Create(CecilILGenerator._(opcode), this._(label).Instruction);
			labelInfo.Branches.Add(ins);
			this.Emit(this.ProcessLabels(ins));
		}

		// Token: 0x0600302E RID: 12334 RVA: 0x000A65F8 File Offset: 0x000A47F8
		public override void Emit(System.Reflection.Emit.OpCode opcode, Label[] labels)
		{
			CecilILGenerator.LabelInfo[] labelInfos = (from x in labels.Distinct<Label>().Select(new Func<Label, CecilILGenerator.LabelInfo>(this._))
				where x != null
				select x).ToArray<CecilILGenerator.LabelInfo>();
			Instruction ins = this.IL.Create(CecilILGenerator._(opcode), (from labelInfo in labelInfos
				select labelInfo.Instruction).ToArray<Instruction>());
			CecilILGenerator.LabelInfo[] array = labelInfos;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Branches.Add(ins);
			}
			this.Emit(this.ProcessLabels(ins));
		}

		// Token: 0x0600302F RID: 12335 RVA: 0x000A66AD File Offset: 0x000A48AD
		public override void Emit(System.Reflection.Emit.OpCode opcode, LocalBuilder local)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(local)));
		}

		// Token: 0x06003030 RID: 12336 RVA: 0x000A66CD File Offset: 0x000A48CD
		public override void Emit(System.Reflection.Emit.OpCode opcode, SignatureHelper signature)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this.IL.Body.Method.Module.ImportCallSite(signature)));
		}

		// Token: 0x06003031 RID: 12337 RVA: 0x000A6701 File Offset: 0x000A4901
		public void Emit(System.Reflection.Emit.OpCode opcode, ICallSiteGenerator signature)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this.IL.Body.Method.Module.ImportCallSite(signature)));
		}

		// Token: 0x06003032 RID: 12338 RVA: 0x000A6738 File Offset: 0x000A4938
		private void _EmitInlineVar(Mono.Cecil.Cil.OpCode opcode, int index)
		{
			switch (opcode.OperandType)
			{
			case Mono.Cecil.Cil.OperandType.InlineVar:
			case Mono.Cecil.Cil.OperandType.ShortInlineVar:
				this.Emit(this.IL.Create(opcode, this.IL.Body.Variables[index]));
				return;
			case Mono.Cecil.Cil.OperandType.InlineArg:
			case Mono.Cecil.Cil.OperandType.ShortInlineArg:
				this.Emit(this.IL.Create(opcode, this.IL.Body.Method.Parameters[index]));
				return;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Unsupported SRE InlineVar -> Cecil ");
			defaultInterpolatedStringHandler.AppendFormatted<Mono.Cecil.Cil.OperandType>(opcode.OperandType);
			defaultInterpolatedStringHandler.AppendLiteral(" for ");
			defaultInterpolatedStringHandler.AppendFormatted<Mono.Cecil.Cil.OpCode>(opcode);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(index);
			throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x06003033 RID: 12339 RVA: 0x000A658A File Offset: 0x000A478A
		public override void EmitCall(System.Reflection.Emit.OpCode opcode, MethodInfo methodInfo, [Nullable(new byte[] { 2, 1 })] Type[] optionalParameterTypes)
		{
			this.Emit(this.IL.Create(CecilILGenerator._(opcode), this._(methodInfo)));
		}

		// Token: 0x06003034 RID: 12340 RVA: 0x00003BBE File Offset: 0x00001DBE
		[NullableContext(2)]
		public override void EmitCalli(System.Reflection.Emit.OpCode opcode, CallingConventions callingConvention, Type returnType, [Nullable(new byte[] { 2, 1 })] Type[] parameterTypes, [Nullable(new byte[] { 2, 1 })] Type[] optionalParameterTypes)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06003035 RID: 12341 RVA: 0x00003BBE File Offset: 0x00001DBE
		[NullableContext(2)]
		public override void EmitCalli(System.Reflection.Emit.OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, [Nullable(new byte[] { 2, 1 })] Type[] parameterTypes)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06003036 RID: 12342 RVA: 0x000A6824 File Offset: 0x000A4A24
		public override void EmitWriteLine(FieldInfo fld)
		{
			if (fld.IsStatic)
			{
				this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Ldsfld, this._(fld)));
			}
			else
			{
				this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0));
				this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Ldfld, this._(fld)));
			}
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Call, this._(typeof(Console).GetMethod("WriteLine", new Type[] { fld.FieldType }))));
		}

		// Token: 0x06003037 RID: 12343 RVA: 0x000A68CC File Offset: 0x000A4ACC
		public override void EmitWriteLine(LocalBuilder localBuilder)
		{
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Ldloc, this._(localBuilder)));
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Call, this._(typeof(Console).GetMethod("WriteLine", new Type[] { localBuilder.LocalType }))));
		}

		// Token: 0x06003038 RID: 12344 RVA: 0x000A6938 File Offset: 0x000A4B38
		public override void EmitWriteLine(string value)
		{
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Ldstr, value));
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Call, this._(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }))));
		}

		// Token: 0x06003039 RID: 12345 RVA: 0x000A69A0 File Offset: 0x000A4BA0
		public override void ThrowException(Type excType)
		{
			ILProcessor il = this.IL;
			Mono.Cecil.Cil.OpCode newobj = Mono.Cecil.Cil.OpCodes.Newobj;
			ConstructorInfo constructor = excType.GetConstructor(Type.EmptyTypes);
			if (constructor == null)
			{
				throw new InvalidOperationException("No default constructor");
			}
			this.Emit(il.Create(newobj, this._(constructor)));
			this.Emit(this.IL.Create(Mono.Cecil.Cil.OpCodes.Throw));
		}

		// Token: 0x0600303A RID: 12346 RVA: 0x000A69FC File Offset: 0x000A4BFC
		public override Label BeginExceptionBlock()
		{
			CecilILGenerator.ExceptionHandlerChain chain = new CecilILGenerator.ExceptionHandlerChain(this);
			this._ExceptionHandlers.Push(chain);
			return chain.SkipAll;
		}

		// Token: 0x0600303B RID: 12347 RVA: 0x000A6A22 File Offset: 0x000A4C22
		public override void BeginCatchBlock(Type exceptionType)
		{
			this._ExceptionHandlers.Peek().BeginHandler(ExceptionHandlerType.Catch).ExceptionType = ((exceptionType == null) ? null : this._(exceptionType));
		}

		// Token: 0x0600303C RID: 12348 RVA: 0x000A6A47 File Offset: 0x000A4C47
		public override void BeginExceptFilterBlock()
		{
			this._ExceptionHandlers.Peek().BeginHandler(ExceptionHandlerType.Filter);
		}

		// Token: 0x0600303D RID: 12349 RVA: 0x000A6A5B File Offset: 0x000A4C5B
		public override void BeginFaultBlock()
		{
			this._ExceptionHandlers.Peek().BeginHandler(ExceptionHandlerType.Fault);
		}

		// Token: 0x0600303E RID: 12350 RVA: 0x000A6A6F File Offset: 0x000A4C6F
		public override void BeginFinallyBlock()
		{
			this._ExceptionHandlers.Peek().BeginHandler(ExceptionHandlerType.Finally);
		}

		// Token: 0x0600303F RID: 12351 RVA: 0x000A6A83 File Offset: 0x000A4C83
		public override void EndExceptionBlock()
		{
			this._ExceptionHandlers.Pop().End();
		}

		// Token: 0x06003040 RID: 12352 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void BeginScope()
		{
		}

		// Token: 0x06003041 RID: 12353 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void EndScope()
		{
		}

		// Token: 0x06003042 RID: 12354 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void UsingNamespace(string usingNamespace)
		{
		}

		// Token: 0x04003BE2 RID: 15330
		private static readonly Type t_LocalBuilder = Type.GetType("System.Reflection.Emit.RuntimeLocalBuilder") ?? typeof(LocalBuilder);

		// Token: 0x04003BE3 RID: 15331
		private static readonly ConstructorInfo c_LocalBuilder = (from c in CecilILGenerator.t_LocalBuilder.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			orderby c.GetParameters().Length descending
			select c).First<ConstructorInfo>();

		// Token: 0x04003BE4 RID: 15332
		[Nullable(2)]
		private static readonly FieldInfo f_LocalBuilder_position = CecilILGenerator.t_LocalBuilder.GetField("position", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04003BE5 RID: 15333
		[Nullable(2)]
		private static readonly FieldInfo f_LocalBuilder_is_pinned = CecilILGenerator.t_LocalBuilder.GetField("is_pinned", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04003BE6 RID: 15334
		private static int c_LocalBuilder_params = CecilILGenerator.c_LocalBuilder.GetParameters().Length;

		// Token: 0x04003BE7 RID: 15335
		private static readonly Dictionary<short, Mono.Cecil.Cil.OpCode> _MCCOpCodes = new Dictionary<short, Mono.Cecil.Cil.OpCode>();

		// Token: 0x04003BE8 RID: 15336
		private static Label NullLabel;

		// Token: 0x04003BEA RID: 15338
		private readonly Dictionary<Label, CecilILGenerator.LabelInfo> _LabelInfos = new Dictionary<Label, CecilILGenerator.LabelInfo>();

		// Token: 0x04003BEB RID: 15339
		private readonly List<CecilILGenerator.LabelInfo> _LabelsToMark = new List<CecilILGenerator.LabelInfo>();

		// Token: 0x04003BEC RID: 15340
		private readonly List<CecilILGenerator.LabelledExceptionHandler> _ExceptionHandlersToMark = new List<CecilILGenerator.LabelledExceptionHandler>();

		// Token: 0x04003BED RID: 15341
		private readonly Dictionary<LocalBuilder, VariableDefinition> _Variables = new Dictionary<LocalBuilder, VariableDefinition>();

		// Token: 0x04003BEE RID: 15342
		private readonly Stack<CecilILGenerator.ExceptionHandlerChain> _ExceptionHandlers = new Stack<CecilILGenerator.ExceptionHandlerChain>();

		// Token: 0x04003BEF RID: 15343
		private int labelCounter;

		// Token: 0x04003BF0 RID: 15344
		private int _ILOffset;

		// Token: 0x020008F8 RID: 2296
		[Nullable(0)]
		private class LabelInfo
		{
			// Token: 0x04003BF1 RID: 15345
			public bool Emitted;

			// Token: 0x04003BF2 RID: 15346
			public Instruction Instruction = Instruction.Create(Mono.Cecil.Cil.OpCodes.Nop);

			// Token: 0x04003BF3 RID: 15347
			public readonly List<Instruction> Branches = new List<Instruction>();
		}

		// Token: 0x020008F9 RID: 2297
		[NullableContext(0)]
		private class LabelledExceptionHandler
		{
			// Token: 0x04003BF4 RID: 15348
			public Label TryStart = CecilILGenerator.NullLabel;

			// Token: 0x04003BF5 RID: 15349
			public Label TryEnd = CecilILGenerator.NullLabel;

			// Token: 0x04003BF6 RID: 15350
			public Label HandlerStart = CecilILGenerator.NullLabel;

			// Token: 0x04003BF7 RID: 15351
			public Label HandlerEnd = CecilILGenerator.NullLabel;

			// Token: 0x04003BF8 RID: 15352
			public Label FilterStart = CecilILGenerator.NullLabel;

			// Token: 0x04003BF9 RID: 15353
			public ExceptionHandlerType HandlerType;

			// Token: 0x04003BFA RID: 15354
			[Nullable(2)]
			public TypeReference ExceptionType;
		}

		// Token: 0x020008FA RID: 2298
		[Nullable(0)]
		private class ExceptionHandlerChain
		{
			// Token: 0x06003045 RID: 12357 RVA: 0x000A6AF7 File Offset: 0x000A4CF7
			public ExceptionHandlerChain(CecilILGenerator il)
			{
				this.IL = il;
				this._Start = il.DefineLabel();
				il.MarkLabel(this._Start);
				this.SkipAll = il.DefineLabel();
			}

			// Token: 0x06003046 RID: 12358 RVA: 0x000A6B2C File Offset: 0x000A4D2C
			public CecilILGenerator.LabelledExceptionHandler BeginHandler(ExceptionHandlerType type)
			{
				CecilILGenerator.LabelledExceptionHandler prev = (this._Prev = this._Handler);
				if (prev != null)
				{
					this.EndHandler(prev);
				}
				this.IL.Emit(System.Reflection.Emit.OpCodes.Leave, this._SkipHandler = this.IL.DefineLabel());
				Label handlerStart = this.IL.DefineLabel();
				this.IL.MarkLabel(handlerStart);
				CecilILGenerator.LabelledExceptionHandler labelledExceptionHandler = new CecilILGenerator.LabelledExceptionHandler();
				labelledExceptionHandler.TryStart = this._Start;
				labelledExceptionHandler.TryEnd = handlerStart;
				labelledExceptionHandler.HandlerType = type;
				labelledExceptionHandler.HandlerEnd = this._SkipHandler;
				CecilILGenerator.LabelledExceptionHandler labelledExceptionHandler2 = labelledExceptionHandler;
				this._Handler = labelledExceptionHandler;
				CecilILGenerator.LabelledExceptionHandler next = labelledExceptionHandler2;
				if (type == ExceptionHandlerType.Filter)
				{
					next.FilterStart = handlerStart;
				}
				else
				{
					next.HandlerStart = handlerStart;
				}
				return next;
			}

			// Token: 0x06003047 RID: 12359 RVA: 0x000A6BDC File Offset: 0x000A4DDC
			public void EndHandler(CecilILGenerator.LabelledExceptionHandler handler)
			{
				Label skip = this._SkipHandler;
				ExceptionHandlerType handlerType = handler.HandlerType;
				if (handlerType != ExceptionHandlerType.Filter)
				{
					if (handlerType != ExceptionHandlerType.Finally)
					{
						this.IL.Emit(System.Reflection.Emit.OpCodes.Leave, skip);
					}
					else
					{
						this.IL.Emit(System.Reflection.Emit.OpCodes.Endfinally);
					}
				}
				else
				{
					this.IL.Emit(System.Reflection.Emit.OpCodes.Endfilter);
				}
				this.IL.MarkLabel(skip);
				this.IL._ExceptionHandlersToMark.Add(handler);
			}

			// Token: 0x06003048 RID: 12360 RVA: 0x000A6C53 File Offset: 0x000A4E53
			public void End()
			{
				CecilILGenerator.LabelledExceptionHandler handler = this._Handler;
				if (handler == null)
				{
					throw new InvalidOperationException("Cannot end when there is no current handler!");
				}
				this.EndHandler(handler);
				this.IL.MarkLabel(this.SkipAll);
			}

			// Token: 0x04003BFB RID: 15355
			private readonly CecilILGenerator IL;

			// Token: 0x04003BFC RID: 15356
			private readonly Label _Start;

			// Token: 0x04003BFD RID: 15357
			public readonly Label SkipAll;

			// Token: 0x04003BFE RID: 15358
			private Label _SkipHandler;

			// Token: 0x04003BFF RID: 15359
			[Nullable(2)]
			private CecilILGenerator.LabelledExceptionHandler _Prev;

			// Token: 0x04003C00 RID: 15360
			[Nullable(2)]
			private CecilILGenerator.LabelledExceptionHandler _Handler;
		}
	}
}
