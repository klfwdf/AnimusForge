using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using MonoMod.Utils.Cil;

namespace MonoMod.Utils
{
	// Token: 0x02000880 RID: 2176
	[NullableContext(1)]
	[Nullable(0)]
	internal static class _DMDEmit
	{
		// Token: 0x06002CAF RID: 11439 RVA: 0x00093F2C File Offset: 0x0009212C
		private static MethodBuilder _CreateMethodProxy(MethodBuilder context, MethodInfo target)
		{
			TypeBuilder typeBuilder = (TypeBuilder)context.DeclaringType;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
			defaultInterpolatedStringHandler.AppendLiteral(".dmdproxy<");
			defaultInterpolatedStringHandler.AppendFormatted(target.Name.Replace('.', '_'));
			defaultInterpolatedStringHandler.AppendLiteral(">?");
			defaultInterpolatedStringHandler.AppendFormatted<int>(target.GetHashCode());
			string name = defaultInterpolatedStringHandler.ToStringAndClear();
			Type[] args = (from param in target.GetParameters()
				select param.ParameterType).ToArray<Type>();
			MethodBuilder mb = typeBuilder.DefineMethod(name, System.Reflection.MethodAttributes.Private | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig, CallingConventions.Standard, target.ReturnType, args);
			ILGenerator il = mb.GetILGenerator();
			DynamicReferenceCell dynamicReferenceCell;
			il.EmitNewTypedReference(target, out dynamicReferenceCell);
			il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, args.Length);
			il.Emit(System.Reflection.Emit.OpCodes.Newarr, typeof(object));
			for (int i = 0; i < args.Length; i++)
			{
				il.Emit(System.Reflection.Emit.OpCodes.Dup);
				il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, i);
				il.Emit(System.Reflection.Emit.OpCodes.Ldarg, i);
				Type argType = args[i];
				if (argType.IsByRef)
				{
					argType = argType.GetElementType() ?? argType;
				}
				if (argType.IsValueType)
				{
					il.Emit(System.Reflection.Emit.OpCodes.Box, argType);
				}
				il.Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
			}
			il.Emit(System.Reflection.Emit.OpCodes.Callvirt, _DMDEmit.m_MethodBase_InvokeSimple);
			if (target.ReturnType == typeof(void))
			{
				il.Emit(System.Reflection.Emit.OpCodes.Pop);
			}
			else if (target.ReturnType.IsValueType)
			{
				il.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, target.ReturnType);
			}
			il.Emit(System.Reflection.Emit.OpCodes.Ret);
			return mb;
		}

		// Token: 0x06002CB0 RID: 11440 RVA: 0x000940E8 File Offset: 0x000922E8
		static _DMDEmit()
		{
			MethodInfo methodInfo = _DMDEmit.mDynamicMethod_AddRef;
			_DMDEmit.DynamicMethod_AddRef = ((methodInfo != null) ? methodInfo.CreateDelegate<Func<DynamicMethod, object, int>>() : null);
			_DMDEmit.TRuntimeILGenerator = Type.GetType("System.Reflection.Emit.RuntimeILGenerator");
			MethodInfo ilgen_EnsureCapacity;
			if ((ilgen_EnsureCapacity = typeof(ILGenerator).GetMethod("EnsureCapacity", BindingFlags.Instance | BindingFlags.NonPublic)) == null)
			{
				Type truntimeILGenerator = _DMDEmit.TRuntimeILGenerator;
				ilgen_EnsureCapacity = ((truntimeILGenerator != null) ? truntimeILGenerator.GetMethod("EnsureCapacity", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			}
			_DMDEmit._ILGen_EnsureCapacity = ilgen_EnsureCapacity;
			MethodInfo ilgen_PutInteger;
			if ((ilgen_PutInteger = typeof(ILGenerator).GetMethod("PutInteger4", BindingFlags.Instance | BindingFlags.NonPublic)) == null)
			{
				Type truntimeILGenerator2 = _DMDEmit.TRuntimeILGenerator;
				ilgen_PutInteger = ((truntimeILGenerator2 != null) ? truntimeILGenerator2.GetMethod("PutInteger4", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			}
			_DMDEmit._ILGen_PutInteger4 = ilgen_PutInteger;
			MethodInfo ilgen_InternalEmit;
			if ((ilgen_InternalEmit = typeof(ILGenerator).GetMethod("InternalEmit", BindingFlags.Instance | BindingFlags.NonPublic)) == null)
			{
				Type truntimeILGenerator3 = _DMDEmit.TRuntimeILGenerator;
				ilgen_InternalEmit = ((truntimeILGenerator3 != null) ? truntimeILGenerator3.GetMethod("InternalEmit", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			}
			_DMDEmit._ILGen_InternalEmit = ilgen_InternalEmit;
			MethodInfo ilgen_UpdateStackSize;
			if ((ilgen_UpdateStackSize = typeof(ILGenerator).GetMethod("UpdateStackSize", BindingFlags.Instance | BindingFlags.NonPublic)) == null)
			{
				Type truntimeILGenerator4 = _DMDEmit.TRuntimeILGenerator;
				ilgen_UpdateStackSize = ((truntimeILGenerator4 != null) ? truntimeILGenerator4.GetMethod("UpdateStackSize", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			}
			_DMDEmit._ILGen_UpdateStackSize = ilgen_UpdateStackSize;
			Type type = typeof(ILGenerator).Assembly.GetType("System.Reflection.Emit.DynamicILGenerator");
			_DMDEmit.f_DynILGen_m_scope = ((type != null) ? type.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			Type type2 = typeof(ILGenerator).Assembly.GetType("System.Reflection.Emit.DynamicScope");
			_DMDEmit.f_DynScope_m_tokens = ((type2 != null) ? type2.GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic) : null);
			_DMDEmit.CorElementTypes = new Type[]
			{
				null,
				typeof(void),
				typeof(bool),
				typeof(char),
				typeof(sbyte),
				typeof(byte),
				typeof(short),
				typeof(ushort),
				typeof(int),
				typeof(uint),
				typeof(long),
				typeof(ulong),
				typeof(float),
				typeof(double),
				typeof(string),
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				typeof(IntPtr),
				typeof(UIntPtr),
				null,
				null,
				typeof(object)
			};
			_DMDEmit.callSiteEmitter = ((_DMDEmit.DynamicMethod_AddRef != null) ? new _DMDEmit.MonoCallSiteEmitter() : new _DMDEmit.NetCallSiteEmitter());
			FieldInfo[] fields = typeof(System.Reflection.Emit.OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int i = 0; i < fields.Length; i++)
			{
				System.Reflection.Emit.OpCode reflOpCode = (System.Reflection.Emit.OpCode)fields[i].GetValue(null);
				_DMDEmit._ReflOpCodes[reflOpCode.Value] = reflOpCode;
			}
			fields = typeof(Mono.Cecil.Cil.OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int i = 0; i < fields.Length; i++)
			{
				Mono.Cecil.Cil.OpCode cecilOpCode = (Mono.Cecil.Cil.OpCode)fields[i].GetValue(null);
				_DMDEmit._CecilOpCodes[cecilOpCode.Value] = cecilOpCode;
			}
		}

		// Token: 0x06002CB1 RID: 11441 RVA: 0x000944B8 File Offset: 0x000926B8
		public static void Generate(DynamicMethodDefinition dmd, MethodBase _mb, ILGenerator il)
		{
			MethodDefinition definition = dmd.Definition;
			if (definition == null)
			{
				throw new InvalidOperationException();
			}
			MethodDefinition def = definition;
			DynamicMethod dm = _mb as DynamicMethod;
			MethodBuilder mb = _mb as MethodBuilder;
			MethodBuilder mb3 = mb;
			ModuleBuilder moduleBuilder = ((mb3 != null) ? mb3.Module : null) as ModuleBuilder;
			MethodBuilder mb2 = mb;
			TypeBuilder typeBuilder = ((mb2 != null) ? mb2.DeclaringType : null) as TypeBuilder;
			AssemblyBuilder assemblyBuilder = ((typeBuilder != null) ? typeBuilder.Assembly : null) as AssemblyBuilder;
			HashSet<Assembly> accessChecksIgnored = null;
			if (mb != null)
			{
				accessChecksIgnored = new HashSet<Assembly>();
			}
			MethodDebugInformation defInfo = (dmd.Debug ? def.DebugInformation : null);
			if (dm != null)
			{
				foreach (ParameterDefinition param in def.Parameters)
				{
					dm.DefineParameter(param.Index + 1, (System.Reflection.ParameterAttributes)param.Attributes, param.Name);
				}
			}
			if (mb != null)
			{
				foreach (ParameterDefinition param2 in def.Parameters)
				{
					mb.DefineParameter(param2.Index + 1, (System.Reflection.ParameterAttributes)param2.Attributes, param2.Name);
				}
			}
			LocalBuilder[] locals = def.Body.Variables.Select(delegate(VariableDefinition var)
			{
				LocalBuilder local = il.DeclareLocal(var.VariableType.ResolveReflection(), var.IsPinned);
				string name;
				if (mb != null && defInfo != null && defInfo.TryGetName(var, out name))
				{
					local.SetLocalSymInfo(name);
				}
				return local;
			}).ToArray<LocalBuilder>();
			Dictionary<Instruction, Label> labelMap = new Dictionary<Instruction, Label>();
			foreach (Instruction instr in def.Body.Instructions)
			{
				Instruction[] targets = instr.Operand as Instruction[];
				if (targets != null)
				{
					foreach (Instruction target5 in targets)
					{
						if (!labelMap.ContainsKey(target5))
						{
							labelMap[target5] = il.DefineLabel();
						}
					}
				}
				else
				{
					Instruction target2 = instr.Operand as Instruction;
					if (target2 != null && !labelMap.ContainsKey(target2))
					{
						labelMap[target2] = il.DefineLabel();
					}
				}
			}
			Dictionary<Document, ISymbolDocumentWriter> infoDocCache = ((mb == null) ? null : new Dictionary<Document, ISymbolDocumentWriter>());
			int paramOffs = ((def.HasThis > false) ? 1 : 0);
			new object[2];
			bool checkTryEndEarly = false;
			Func<Instruction, Label> <>9__1;
			foreach (Instruction instr2 in def.Body.Instructions)
			{
				Label label;
				if (labelMap.TryGetValue(instr2, out label))
				{
					il.MarkLabel(label);
				}
				MethodDebugInformation defInfo2 = defInfo;
				SequencePoint instrInfo = ((defInfo2 != null) ? defInfo2.GetSequencePoint(instr2) : null);
				if (mb != null && instrInfo != null && infoDocCache != null && moduleBuilder != null)
				{
					ISymbolDocumentWriter infoDoc;
					if (!infoDocCache.TryGetValue(instrInfo.Document, out infoDoc))
					{
						infoDoc = (infoDocCache[instrInfo.Document] = moduleBuilder.DefineDocument(instrInfo.Document.Url, instrInfo.Document.LanguageGuid, instrInfo.Document.LanguageVendorGuid, instrInfo.Document.TypeGuid));
					}
					il.MarkSequencePoint(infoDoc, instrInfo.StartLine, instrInfo.StartColumn, instrInfo.EndLine, instrInfo.EndColumn);
				}
				foreach (Mono.Cecil.Cil.ExceptionHandler handler in def.Body.ExceptionHandlers)
				{
					if (checkTryEndEarly && handler.HandlerEnd == instr2)
					{
						il.EndExceptionBlock();
					}
					if (handler.TryStart == instr2)
					{
						il.BeginExceptionBlock();
					}
					else if (handler.FilterStart == instr2)
					{
						il.BeginExceptFilterBlock();
					}
					else if (handler.HandlerStart == instr2)
					{
						switch (handler.HandlerType)
						{
						case ExceptionHandlerType.Catch:
							il.BeginCatchBlock(handler.CatchType.ResolveReflection());
							break;
						case ExceptionHandlerType.Filter:
							il.BeginCatchBlock(null);
							break;
						case ExceptionHandlerType.Finally:
							il.BeginFinallyBlock();
							break;
						case ExceptionHandlerType.Fault:
							il.BeginFaultBlock();
							break;
						}
					}
					if (handler.HandlerStart == instr2.Next)
					{
						ExceptionHandlerType handlerType = handler.HandlerType;
						if (handlerType != ExceptionHandlerType.Filter)
						{
							if (handlerType == ExceptionHandlerType.Finally)
							{
								if (instr2.OpCode == Mono.Cecil.Cil.OpCodes.Endfinally)
								{
									goto IL_8A3;
								}
							}
						}
						else if (instr2.OpCode == Mono.Cecil.Cil.OpCodes.Endfilter)
						{
							goto IL_8A3;
						}
					}
				}
				if (instr2.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineNone)
				{
					il.Emit(_DMDEmit._ReflOpCodes[instr2.OpCode.Value]);
				}
				else
				{
					Mono.Cecil.Cil.OpCode opcode = instr2.OpCode;
					object operand = instr2.Operand;
					Instruction[] targets2 = operand as Instruction[];
					if (targets2 != null)
					{
						IEnumerable<Instruction> source = targets2;
						Func<Instruction, Label> selector;
						if ((selector = <>9__1) == null)
						{
							selector = (<>9__1 = (Instruction target) => labelMap[target]);
						}
						operand = source.Select(selector).ToArray<Label>();
						opcode = opcode.ToLongOp();
					}
					else
					{
						Instruction target3 = operand as Instruction;
						if (target3 != null)
						{
							operand = labelMap[target3];
							opcode = opcode.ToLongOp();
						}
						else
						{
							VariableDefinition var2 = operand as VariableDefinition;
							if (var2 != null)
							{
								operand = locals[var2.Index];
							}
							else
							{
								ParameterDefinition param3 = operand as ParameterDefinition;
								if (param3 != null)
								{
									operand = param3.Index + paramOffs;
								}
								else
								{
									MemberReference mref = operand as MemberReference;
									if (mref != null)
									{
										MemberInfo member = ((mref == def) ? _mb : mref.ResolveReflection());
										operand = member;
										if (mb != null && member != null)
										{
											Module module = member.Module;
											if (module == null)
											{
												continue;
											}
											Assembly asm = module.Assembly;
											if (asm != null && accessChecksIgnored != null && assemblyBuilder != null && !accessChecksIgnored.Contains(asm))
											{
												assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(DynamicMethodDefinition.c_IgnoresAccessChecksToAttribute, new object[] { asm.GetName().Name }));
												accessChecksIgnored.Add(asm);
											}
										}
									}
									else
									{
										Mono.Cecil.CallSite csite = operand as Mono.Cecil.CallSite;
										if (csite != null)
										{
											if (dm != null)
											{
												_DMDEmit._EmitCallSite(dm, il, _DMDEmit._ReflOpCodes[opcode.Value], csite);
												continue;
											}
											if (mb == null)
											{
												throw new NotSupportedException();
											}
											operand = csite.ResolveReflection(mb.Module);
										}
									}
								}
							}
						}
					}
					if (mb != null)
					{
						MethodBase called = operand as MethodBase;
						if (called != null && called.DeclaringType == null)
						{
							if (!(opcode == Mono.Cecil.Cil.OpCodes.Call))
							{
								throw new NotSupportedException("Unsupported global method operand on opcode " + opcode.Name);
							}
							MethodInfo target4 = called as MethodInfo;
							if (target4 != null && target4.IsDynamicMethod())
							{
								operand = _DMDEmit._CreateMethodProxy(mb, target4);
							}
							else
							{
								IntPtr ptr = called.GetLdftnPointer();
								if (IntPtr.Size == 4)
								{
									il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, (int)ptr);
								}
								else
								{
									il.Emit(System.Reflection.Emit.OpCodes.Ldc_I8, (long)ptr);
								}
								il.Emit(System.Reflection.Emit.OpCodes.Conv_I);
								opcode = Mono.Cecil.Cil.OpCodes.Calli;
								operand = ((MethodReference)instr2.Operand).ResolveReflectionSignature(mb.Module);
							}
						}
					}
					if (operand == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Unexpected null in ");
						defaultInterpolatedStringHandler.AppendFormatted<MethodDefinition>(def);
						defaultInterpolatedStringHandler.AppendLiteral(" @ ");
						defaultInterpolatedStringHandler.AppendFormatted<Instruction>(instr2);
						throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					il.DynEmit(_DMDEmit._ReflOpCodes[opcode.Value], operand);
				}
				if (!checkTryEndEarly)
				{
					using (Collection<Mono.Cecil.Cil.ExceptionHandler>.Enumerator enumerator3 = def.Body.ExceptionHandlers.GetEnumerator())
					{
						while (enumerator3.MoveNext())
						{
							if (enumerator3.Current.HandlerEnd == instr2.Next)
							{
								il.EndExceptionBlock();
							}
						}
					}
				}
				checkTryEndEarly = false;
				continue;
				IL_8A3:
				checkTryEndEarly = true;
			}
		}

		// Token: 0x06002CB2 RID: 11442 RVA: 0x00094E1C File Offset: 0x0009301C
		public static void ResolveWithModifiers(TypeReference typeRef, out Type type, out Type[] typeModReq, out Type[] typeModOpt, [Nullable(new byte[] { 2, 1 })] List<Type> modReq = null, [Nullable(new byte[] { 2, 1 })] List<Type> modOpt = null)
		{
			if (modReq == null)
			{
				modReq = new List<Type>();
			}
			else
			{
				modReq.Clear();
			}
			if (modOpt == null)
			{
				modOpt = new List<Type>();
			}
			else
			{
				modOpt.Clear();
			}
			TypeReference mod = typeRef;
			for (;;)
			{
				TypeSpecification modSpec = mod as TypeSpecification;
				if (modSpec == null)
				{
					break;
				}
				RequiredModifierType paramTypeModReq = mod as RequiredModifierType;
				if (paramTypeModReq == null)
				{
					OptionalModifierType paramTypeOptReq = mod as OptionalModifierType;
					if (paramTypeOptReq != null)
					{
						modOpt.Add(paramTypeOptReq.ModifierType.ResolveReflection());
					}
				}
				else
				{
					modReq.Add(paramTypeModReq.ModifierType.ResolveReflection());
				}
				mod = modSpec.ElementType;
			}
			type = typeRef.ResolveReflection();
			typeModReq = modReq.ToArray();
			typeModOpt = modOpt.ToArray();
		}

		// Token: 0x06002CB3 RID: 11443 RVA: 0x00094EBC File Offset: 0x000930BC
		internal static void _EmitCallSite(DynamicMethod dm, ILGenerator il, System.Reflection.Emit.OpCode opcode, Mono.Cecil.CallSite csite)
		{
			_DMDEmit.callSiteEmitter.EmitCallSite(dm, il, opcode, csite);
		}

		// Token: 0x04003A63 RID: 14947
		private static readonly MethodInfo m_MethodBase_InvokeSimple = typeof(MethodBase).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public, null, new Type[]
		{
			typeof(object),
			typeof(object[])
		}, null);

		// Token: 0x04003A64 RID: 14948
		private static readonly Dictionary<short, System.Reflection.Emit.OpCode> _ReflOpCodes = new Dictionary<short, System.Reflection.Emit.OpCode>();

		// Token: 0x04003A65 RID: 14949
		private static readonly Dictionary<short, Mono.Cecil.Cil.OpCode> _CecilOpCodes = new Dictionary<short, Mono.Cecil.Cil.OpCode>();

		// Token: 0x04003A66 RID: 14950
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_make_room = typeof(ILGenerator).GetMethod("make_room", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04003A67 RID: 14951
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_emit_int = typeof(ILGenerator).GetMethod("emit_int", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04003A68 RID: 14952
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_ll_emit = typeof(ILGenerator).GetMethod("ll_emit", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04003A69 RID: 14953
		[Nullable(2)]
		private static readonly MethodInfo mDynamicMethod_AddRef = typeof(DynamicMethod).GetMethod("AddRef", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(object) }, null);

		// Token: 0x04003A6A RID: 14954
		[Nullable(new byte[] { 2, 1, 2 })]
		private static readonly Func<DynamicMethod, object, int> DynamicMethod_AddRef;

		// Token: 0x04003A6B RID: 14955
		[Nullable(2)]
		private static readonly Type TRuntimeILGenerator;

		// Token: 0x04003A6C RID: 14956
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_EnsureCapacity;

		// Token: 0x04003A6D RID: 14957
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_PutInteger4;

		// Token: 0x04003A6E RID: 14958
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_InternalEmit;

		// Token: 0x04003A6F RID: 14959
		[Nullable(2)]
		private static readonly MethodInfo _ILGen_UpdateStackSize;

		// Token: 0x04003A70 RID: 14960
		[Nullable(2)]
		private static readonly FieldInfo f_DynILGen_m_scope;

		// Token: 0x04003A71 RID: 14961
		[Nullable(2)]
		private static readonly FieldInfo f_DynScope_m_tokens;

		// Token: 0x04003A72 RID: 14962
		[Nullable(new byte[] { 1, 2 })]
		private static readonly Type[] CorElementTypes;

		// Token: 0x04003A73 RID: 14963
		private static readonly _DMDEmit.CallSiteEmitter callSiteEmitter;

		// Token: 0x02000881 RID: 2177
		[Nullable(0)]
		private abstract class TokenCreator
		{
			// Token: 0x06002CB4 RID: 11444
			public abstract int GetTokenForType(Type type);

			// Token: 0x06002CB5 RID: 11445
			public abstract int GetTokenForSig(byte[] sig);
		}

		// Token: 0x02000882 RID: 2178
		[Nullable(0)]
		private sealed class NetTokenCreator : _DMDEmit.TokenCreator
		{
			// Token: 0x06002CB7 RID: 11447 RVA: 0x00094ECC File Offset: 0x000930CC
			public NetTokenCreator(ILGenerator il)
			{
				Helpers.Assert(_DMDEmit.f_DynScope_m_tokens != null, null, "f_DynScope_m_tokens is not null");
				Helpers.Assert(_DMDEmit.f_DynILGen_m_scope != null, null, "f_DynILGen_m_scope is not null");
				List<object> list = (List<object>)_DMDEmit.f_DynScope_m_tokens.GetValue(_DMDEmit.f_DynILGen_m_scope.GetValue(il));
				Helpers.Assert(list != null, "DynamicMethod object list is null!", "list is not null");
				this.tokens = list;
			}

			// Token: 0x06002CB8 RID: 11448 RVA: 0x00094F40 File Offset: 0x00093140
			public override int GetTokenForType(Type type)
			{
				this.tokens.Add(type.TypeHandle);
				return (this.tokens.Count - 1) | 33554432;
			}

			// Token: 0x06002CB9 RID: 11449 RVA: 0x00094F6B File Offset: 0x0009316B
			public override int GetTokenForSig(byte[] sig)
			{
				this.tokens.Add(sig);
				return (this.tokens.Count - 1) | 285212672;
			}

			// Token: 0x04003A74 RID: 14964
			private readonly List<object> tokens;
		}

		// Token: 0x02000883 RID: 2179
		[Nullable(0)]
		private sealed class MonoTokenCreator : _DMDEmit.TokenCreator
		{
			// Token: 0x06002CBA RID: 11450 RVA: 0x00094F8C File Offset: 0x0009318C
			public MonoTokenCreator(DynamicMethod dm)
			{
				Helpers.Assert(_DMDEmit.DynamicMethod_AddRef != null, null, "DynamicMethod_AddRef is not null");
				this.addRef = _DMDEmit.DynamicMethod_AddRef;
				this.dm = dm;
			}

			// Token: 0x06002CBB RID: 11451 RVA: 0x00094FBC File Offset: 0x000931BC
			public override int GetTokenForType(Type type)
			{
				return this.addRef(this.dm, type);
			}

			// Token: 0x06002CBC RID: 11452 RVA: 0x00094FBC File Offset: 0x000931BC
			public override int GetTokenForSig(byte[] sig)
			{
				return this.addRef(this.dm, sig);
			}

			// Token: 0x04003A75 RID: 14965
			private readonly DynamicMethod dm;

			// Token: 0x04003A76 RID: 14966
			[Nullable(new byte[] { 1, 1, 2 })]
			private readonly Func<DynamicMethod, object, int> addRef;
		}

		// Token: 0x02000884 RID: 2180
		[NullableContext(0)]
		private abstract class CallSiteEmitter
		{
			// Token: 0x06002CBD RID: 11453
			[NullableContext(1)]
			public abstract void EmitCallSite(DynamicMethod dm, ILGenerator il, System.Reflection.Emit.OpCode opcode, Mono.Cecil.CallSite csite);
		}

		// Token: 0x02000885 RID: 2181
		[NullableContext(0)]
		private sealed class NetCallSiteEmitter : _DMDEmit.CallSiteEmitter
		{
			// Token: 0x06002CBF RID: 11455 RVA: 0x00094FD0 File Offset: 0x000931D0
			[NullableContext(1)]
			public override void EmitCallSite(DynamicMethod dm, ILGenerator il, System.Reflection.Emit.OpCode opcode, Mono.Cecil.CallSite csite)
			{
				_DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 CS$<>8__locals1;
				CS$<>8__locals1.tokenCreator = ((_DMDEmit.DynamicMethod_AddRef != null) ? new _DMDEmit.MonoTokenCreator(dm) : new _DMDEmit.NetTokenCreator(il));
				CS$<>8__locals1.signature = new byte[32];
				CS$<>8__locals1.currSig = 0;
				int sizeLoc = -1;
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1((int)(csite.CallingConvention | (csite.HasThis ? ((MethodCallingConvention)32) : MethodCallingConvention.Default) | (csite.ExplicitThis ? ((MethodCallingConvention)64) : MethodCallingConvention.Default)), ref CS$<>8__locals1);
				int currSig = CS$<>8__locals1.currSig;
				CS$<>8__locals1.currSig = currSig + 1;
				sizeLoc = currSig;
				List<Type> modReq = new List<Type>();
				List<Type> modOpt = new List<Type>();
				Type returnType;
				Type[] returnTypeModReq;
				Type[] returnTypeModOpt;
				_DMDEmit.ResolveWithModifiers(csite.ReturnType, out returnType, out returnTypeModReq, out returnTypeModOpt, modReq, modOpt);
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddArgument|0_0(returnType, returnTypeModReq, returnTypeModOpt, ref CS$<>8__locals1);
				foreach (ParameterDefinition parameterDefinition in csite.Parameters)
				{
					if (parameterDefinition.ParameterType.IsSentinel)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(65, ref CS$<>8__locals1);
					}
					if (parameterDefinition.ParameterType.IsPinned)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(69, ref CS$<>8__locals1);
					}
					Type paramType;
					Type[] paramTypeModReq;
					Type[] paramTypeModOpt;
					_DMDEmit.ResolveWithModifiers(parameterDefinition.ParameterType, out paramType, out paramTypeModReq, out paramTypeModOpt, modReq, modOpt);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddArgument|0_0(paramType, paramTypeModReq, paramTypeModOpt, ref CS$<>8__locals1);
				}
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(0, ref CS$<>8__locals1);
				int currSigHolder = CS$<>8__locals1.currSig;
				int newSigSize;
				if (csite.Parameters.Count < 128)
				{
					newSigSize = 1;
				}
				else if (csite.Parameters.Count < 16384)
				{
					newSigSize = 2;
				}
				else
				{
					newSigSize = 4;
				}
				byte[] temp = new byte[CS$<>8__locals1.currSig + newSigSize - 1];
				temp[0] = CS$<>8__locals1.signature[0];
				Buffer.BlockCopy(CS$<>8__locals1.signature, sizeLoc + 1, temp, sizeLoc + newSigSize, currSigHolder - (sizeLoc + 1));
				CS$<>8__locals1.signature = temp;
				CS$<>8__locals1.currSig = sizeLoc;
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(csite.Parameters.Count, ref CS$<>8__locals1);
				CS$<>8__locals1.currSig = currSigHolder + (newSigSize - 1);
				if (CS$<>8__locals1.signature.Length > CS$<>8__locals1.currSig)
				{
					temp = new byte[CS$<>8__locals1.currSig];
					Array.Copy(CS$<>8__locals1.signature, temp, CS$<>8__locals1.currSig);
					CS$<>8__locals1.signature = temp;
				}
				if (_DMDEmit._ILGen_emit_int != null)
				{
					_DMDEmit._ILGen_make_room.Invoke(il, new object[] { 6 });
					_DMDEmit._ILGen_ll_emit.Invoke(il, new object[] { opcode });
					_DMDEmit._ILGen_emit_int.Invoke(il, new object[] { CS$<>8__locals1.tokenCreator.GetTokenForSig(CS$<>8__locals1.signature) });
					return;
				}
				_DMDEmit._ILGen_EnsureCapacity.Invoke(il, new object[] { 7 });
				_DMDEmit._ILGen_InternalEmit.Invoke(il, new object[] { opcode });
				if (opcode.StackBehaviourPop == System.Reflection.Emit.StackBehaviour.Varpop)
				{
					_DMDEmit._ILGen_UpdateStackSize.Invoke(il, new object[]
					{
						opcode,
						-csite.Parameters.Count - 1
					});
				}
				_DMDEmit._ILGen_PutInteger4.Invoke(il, new object[] { CS$<>8__locals1.tokenCreator.GetTokenForSig(CS$<>8__locals1.signature) });
			}

			// Token: 0x06002CC1 RID: 11457 RVA: 0x00095308 File Offset: 0x00093508
			[NullableContext(1)]
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddArgument|0_0(Type clsArgument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_3)
			{
				if (optionalCustomModifiers != null)
				{
					foreach (Type t in optionalCustomModifiers)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__InternalAddTypeToken|0_5(A_3.tokenCreator.GetTokenForType(t), 32, ref A_3);
					}
				}
				if (requiredCustomModifiers != null)
				{
					foreach (Type t2 in requiredCustomModifiers)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__InternalAddTypeToken|0_5(A_3.tokenCreator.GetTokenForType(t2), 31, ref A_3);
					}
				}
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelper|0_6(clsArgument, ref A_3);
			}

			// Token: 0x06002CC2 RID: 11458 RVA: 0x00095374 File Offset: 0x00093574
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddData|0_1(int data, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_1)
			{
				if (A_1.currSig + 4 > A_1.signature.Length)
				{
					A_1.signature = _DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__ExpandArray|0_2(A_1.signature, -1);
				}
				if (data <= 127)
				{
					byte[] signature = A_1.signature;
					int currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature[currSig] = (byte)(data & 255);
					return;
				}
				if (data <= 16383)
				{
					byte[] signature2 = A_1.signature;
					int currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature2[currSig] = (byte)((data >> 8) | 128);
					byte[] signature3 = A_1.signature;
					currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature3[currSig] = (byte)(data & 255);
					return;
				}
				if (data <= 536870911)
				{
					byte[] signature4 = A_1.signature;
					int currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature4[currSig] = (byte)((data >> 24) | 192);
					byte[] signature5 = A_1.signature;
					currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature5[currSig] = (byte)((data >> 16) & 255);
					byte[] signature6 = A_1.signature;
					currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature6[currSig] = (byte)((data >> 8) & 255);
					byte[] signature7 = A_1.signature;
					currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature7[currSig] = (byte)(data & 255);
					return;
				}
				throw new ArgumentException("Integer or token was too large to be encoded.");
			}

			// Token: 0x06002CC3 RID: 11459 RVA: 0x000954B4 File Offset: 0x000936B4
			[NullableContext(1)]
			[CompilerGenerated]
			internal static byte[] <EmitCallSite>g__ExpandArray|0_2(byte[] inArray, int requiredLength = -1)
			{
				if (requiredLength < inArray.Length)
				{
					requiredLength = inArray.Length * 2;
				}
				byte[] outArray = new byte[requiredLength];
				Buffer.BlockCopy(inArray, 0, outArray, 0, inArray.Length);
				return outArray;
			}

			// Token: 0x06002CC4 RID: 11460 RVA: 0x000954E4 File Offset: 0x000936E4
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddElementType|0_3(byte cvt, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_1)
			{
				if (A_1.currSig + 1 > A_1.signature.Length)
				{
					A_1.signature = _DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__ExpandArray|0_2(A_1.signature, -1);
				}
				byte[] signature = A_1.signature;
				int currSig = A_1.currSig;
				A_1.currSig = currSig + 1;
				signature[currSig] = cvt;
			}

			// Token: 0x06002CC5 RID: 11461 RVA: 0x00095530 File Offset: 0x00093730
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddToken|0_4(int token, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_1)
			{
				int rid = token & 16777215;
				int type = token & -16777216;
				if (rid > 67108863)
				{
					throw new ArgumentException("Integer or token was too large to be encoded.");
				}
				rid <<= 2;
				if (type == 16777216)
				{
					rid |= 1;
				}
				else if (type == 452984832)
				{
					rid |= 2;
				}
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(rid, ref A_1);
			}

			// Token: 0x06002CC6 RID: 11462 RVA: 0x00095585 File Offset: 0x00093785
			[CompilerGenerated]
			internal static void <EmitCallSite>g__InternalAddTypeToken|0_5(int clsToken, byte CorType, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_2)
			{
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(CorType, ref A_2);
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddToken|0_4(clsToken, ref A_2);
			}

			// Token: 0x06002CC7 RID: 11463 RVA: 0x00095595 File Offset: 0x00093795
			[NullableContext(1)]
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddOneArgTypeHelper|0_6(Type clsArgument, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_1)
			{
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelperWorker|0_7(clsArgument, false, ref A_1);
			}

			// Token: 0x06002CC8 RID: 11464 RVA: 0x000955A0 File Offset: 0x000937A0
			[NullableContext(1)]
			[CompilerGenerated]
			internal static void <EmitCallSite>g__AddOneArgTypeHelperWorker|0_7(Type clsArgument, bool lastWasGenericInst, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_2)
			{
				if (clsArgument.IsGenericType && (!clsArgument.IsGenericTypeDefinition || !lastWasGenericInst))
				{
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(21, ref A_2);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelperWorker|0_7(clsArgument.GetGenericTypeDefinition(), true, ref A_2);
					Type[] genericArguments = clsArgument.GetGenericArguments();
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(genericArguments.Length, ref A_2);
					Type[] array = genericArguments;
					for (int k = 0; k < array.Length; k++)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelper|0_6(array[k], ref A_2);
					}
					return;
				}
				if (clsArgument.IsByRef)
				{
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(16, ref A_2);
					clsArgument = clsArgument.GetElementType() ?? clsArgument;
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelper|0_6(clsArgument, ref A_2);
					return;
				}
				if (clsArgument.IsPointer)
				{
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(15, ref A_2);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelper|0_6(clsArgument.GetElementType() ?? clsArgument, ref A_2);
					return;
				}
				if (clsArgument.IsArray)
				{
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(20, ref A_2);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddOneArgTypeHelper|0_6(clsArgument.GetElementType() ?? clsArgument, ref A_2);
					int rank = clsArgument.GetArrayRank();
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(rank, ref A_2);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(0, ref A_2);
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(rank, ref A_2);
					for (int i = 0; i < rank; i++)
					{
						_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddData|0_1(0, ref A_2);
					}
					return;
				}
				byte type = 0;
				for (int j = 0; j < _DMDEmit.CorElementTypes.Length; j++)
				{
					if (clsArgument == _DMDEmit.CorElementTypes[j])
					{
						type = (byte)j;
						break;
					}
				}
				if (type == 0)
				{
					if (clsArgument == typeof(object))
					{
						type = 28;
					}
					else if (clsArgument.IsValueType)
					{
						type = 17;
					}
					else
					{
						type = 18;
					}
				}
				if (type <= 14 || type == 22 || type == 24 || type == 25 || type == 28)
				{
					_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(type, ref A_2);
					return;
				}
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__InternalAddRuntimeType|0_8(clsArgument, ref A_2);
			}

			// Token: 0x06002CC9 RID: 11465 RVA: 0x00095724 File Offset: 0x00093924
			[NullableContext(1)]
			[CompilerGenerated]
			internal unsafe static void <EmitCallSite>g__InternalAddRuntimeType|0_8(Type type, ref _DMDEmit.NetCallSiteEmitter.<>c__DisplayClass0_0 A_1)
			{
				_DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__AddElementType|0_3(33, ref A_1);
				IntPtr handle = type.TypeHandle.Value;
				if (A_1.currSig + sizeof(void*) > A_1.signature.Length)
				{
					A_1.signature = _DMDEmit.NetCallSiteEmitter.<EmitCallSite>g__ExpandArray|0_2(A_1.signature, -1);
				}
				byte* phandle = (byte*)(&handle);
				for (int i = 0; i < sizeof(void*); i++)
				{
					byte[] signature = A_1.signature;
					int currSig = A_1.currSig;
					A_1.currSig = currSig + 1;
					signature[currSig] = phandle[i];
				}
			}
		}

		// Token: 0x02000887 RID: 2183
		[Nullable(0)]
		private sealed class MonoCallSiteEmitter : _DMDEmit.CallSiteEmitter
		{
			// Token: 0x06002CCA RID: 11466 RVA: 0x000957A8 File Offset: 0x000939A8
			public MonoCallSiteEmitter()
			{
				FieldInfo callConv = typeof(SignatureHelper).GetField("callConv", BindingFlags.Instance | BindingFlags.NonPublic);
				FieldInfo unmanagedCallConv = typeof(SignatureHelper).GetField("unmanagedCallConv", BindingFlags.Instance | BindingFlags.NonPublic);
				FieldInfo arguments = typeof(SignatureHelper).GetField("arguments", BindingFlags.Instance | BindingFlags.NonPublic);
				FieldInfo modreqs = typeof(SignatureHelper).GetField("modreqs", BindingFlags.Instance | BindingFlags.NonPublic);
				FieldInfo modopts = typeof(SignatureHelper).GetField("modopts", BindingFlags.Instance | BindingFlags.NonPublic);
				Helpers.Assert(callConv != null, null, "callConv is not null");
				Helpers.Assert(unmanagedCallConv != null, null, "unmanagedCallConv is not null");
				Helpers.Assert(arguments != null, null, "arguments is not null");
				Helpers.Assert(modreqs != null, null, "modreqs is not null");
				Helpers.Assert(modopts != null, null, "modopts is not null");
				this.SigHelper_callConv = callConv;
				this.SigHelper_unmanagedCallConv = unmanagedCallConv;
				this.SigHelper_arguments = arguments;
				this.SigHelper_modreqs = modreqs;
				this.SigHelper_modopts = modopts;
			}

			// Token: 0x06002CCB RID: 11467 RVA: 0x000958A0 File Offset: 0x00093AA0
			public override void EmitCallSite(DynamicMethod dm, ILGenerator il, System.Reflection.Emit.OpCode opcode, Mono.Cecil.CallSite csite)
			{
				List<Type> modReq = new List<Type>();
				List<Type> modOpt = new List<Type>();
				Type rawRetType;
				Type[] array;
				Type[] array2;
				_DMDEmit.ResolveWithModifiers(csite.ReturnType, out rawRetType, out array, out array2, modReq, modOpt);
				SignatureHelper sigHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.Standard, rawRetType);
				Type[] arguments = new Type[csite.Parameters.Count];
				Type[][] modreqs = new Type[csite.Parameters.Count][];
				Type[][] modopts = new Type[csite.Parameters.Count][];
				CallingConventions callingConventions;
				if (csite.CallingConvention == MethodCallingConvention.VarArg)
				{
					callingConventions = CallingConventions.VarArgs;
				}
				else
				{
					callingConventions = CallingConventions.Standard;
				}
				CallingConventions managedCallConv = callingConventions;
				if (csite.HasThis)
				{
					managedCallConv |= CallingConventions.HasThis;
				}
				if (csite.ExplicitThis)
				{
					managedCallConv |= CallingConventions.ExplicitThis;
				}
				CallingConvention callingConvention;
				switch (csite.CallingConvention)
				{
				case MethodCallingConvention.C:
					callingConvention = CallingConvention.Cdecl;
					break;
				case MethodCallingConvention.StdCall:
					callingConvention = CallingConvention.StdCall;
					break;
				case MethodCallingConvention.ThisCall:
					callingConvention = CallingConvention.ThisCall;
					break;
				case MethodCallingConvention.FastCall:
					callingConvention = CallingConvention.FastCall;
					break;
				default:
					callingConvention = (CallingConvention)0;
					break;
				}
				CallingConvention unmanagedCallConv = callingConvention;
				for (int i = 0; i < csite.Parameters.Count; i++)
				{
					_DMDEmit.ResolveWithModifiers(csite.Parameters[i].ParameterType, out arguments[i], out modreqs[i], out modopts[i], modReq, modOpt);
				}
				this.SigHelper_callConv.SetValue(sigHelper, managedCallConv);
				this.SigHelper_unmanagedCallConv.SetValue(sigHelper, unmanagedCallConv);
				this.SigHelper_arguments.SetValue(sigHelper, arguments);
				this.SigHelper_modreqs.SetValue(sigHelper, modreqs);
				this.SigHelper_modopts.SetValue(sigHelper, modopts);
				_DMDEmit._ILGen_make_room.Invoke(il, new object[] { 6 });
				_DMDEmit._ILGen_ll_emit.Invoke(il, new object[] { opcode });
				_DMDEmit._ILGen_emit_int.Invoke(il, new object[] { _DMDEmit.DynamicMethod_AddRef(dm, sigHelper) });
			}

			// Token: 0x04003A7A RID: 14970
			private FieldInfo SigHelper_callConv;

			// Token: 0x04003A7B RID: 14971
			private FieldInfo SigHelper_unmanagedCallConv;

			// Token: 0x04003A7C RID: 14972
			private FieldInfo SigHelper_arguments;

			// Token: 0x04003A7D RID: 14973
			private FieldInfo SigHelper_modreqs;

			// Token: 0x04003A7E RID: 14974
			private FieldInfo SigHelper_modopts;
		}
	}
}
