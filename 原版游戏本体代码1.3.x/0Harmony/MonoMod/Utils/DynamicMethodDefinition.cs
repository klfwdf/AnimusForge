using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils.Cil;

namespace MonoMod.Utils
{
	// Token: 0x0200089A RID: 2202
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class DynamicMethodDefinition : IDisposable
	{
		// Token: 0x06002D2B RID: 11563 RVA: 0x00097284 File Offset: 0x00095484
		private static void _InitCopier()
		{
			DynamicMethodDefinition._CecilOpCodes1X = new Mono.Cecil.Cil.OpCode[225];
			DynamicMethodDefinition._CecilOpCodes2X = new Mono.Cecil.Cil.OpCode[31];
			FieldInfo[] fields = typeof(Mono.Cecil.Cil.OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int i = 0; i < fields.Length; i++)
			{
				Mono.Cecil.Cil.OpCode opcode = (Mono.Cecil.Cil.OpCode)fields[i].GetValue(null);
				if (opcode.OpCodeType != Mono.Cecil.Cil.OpCodeType.Nternal)
				{
					if (opcode.Size == 1)
					{
						DynamicMethodDefinition._CecilOpCodes1X[(int)opcode.Value] = opcode;
					}
					else
					{
						DynamicMethodDefinition._CecilOpCodes2X[(int)(opcode.Value & 255)] = opcode;
					}
				}
			}
		}

		// Token: 0x06002D2C RID: 11564 RVA: 0x0009731C File Offset: 0x0009551C
		private static void _CopyMethodToDefinition(MethodBase from, MethodDefinition into)
		{
			DynamicMethodDefinition.<>c__DisplayClass3_0 CS$<>8__locals1 = new DynamicMethodDefinition.<>c__DisplayClass3_0();
			CS$<>8__locals1.into = into;
			CS$<>8__locals1.moduleFrom = from.Module;
			System.Reflection.MethodBody methodBody = from.GetMethodBody();
			if (methodBody == null)
			{
				throw new NotSupportedException("Body-less method");
			}
			System.Reflection.MethodBody bodyFrom = methodBody;
			byte[] ilasByteArray = bodyFrom.GetILAsByteArray();
			if (ilasByteArray == null)
			{
				throw new InvalidOperationException();
			}
			byte[] data = ilasByteArray;
			CS$<>8__locals1.moduleTo = CS$<>8__locals1.into.Module;
			CS$<>8__locals1.bodyTo = CS$<>8__locals1.into.Body;
			CS$<>8__locals1.bodyTo.GetILProcessor();
			CS$<>8__locals1.typeArguments = null;
			Type declaringType = from.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				CS$<>8__locals1.typeArguments = from.DeclaringType.GetGenericArguments();
			}
			CS$<>8__locals1.methodArguments = null;
			if (from.IsGenericMethod)
			{
				CS$<>8__locals1.methodArguments = from.GetGenericArguments();
			}
			foreach (LocalVariableInfo info in bodyFrom.LocalVariables)
			{
				TypeReference type = CS$<>8__locals1.moduleTo.ImportReference(info.LocalType);
				if (info.IsPinned)
				{
					type = new PinnedType(type);
				}
				CS$<>8__locals1.bodyTo.Variables.Add(new VariableDefinition(type));
			}
			using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
			{
				Instruction prev = null;
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					int offset = (int)reader.BaseStream.Position;
					Instruction instr = Instruction.Create(Mono.Cecil.Cil.OpCodes.Nop);
					byte op = reader.ReadByte();
					instr.OpCode = ((op != 254) ? DynamicMethodDefinition._CecilOpCodes1X[(int)op] : DynamicMethodDefinition._CecilOpCodes2X[(int)reader.ReadByte()]);
					instr.Offset = offset;
					if (prev != null)
					{
						prev.Next = instr;
					}
					instr.Previous = prev;
					CS$<>8__locals1.<_CopyMethodToDefinition>g__ReadOperand|0(reader, instr);
					CS$<>8__locals1.bodyTo.Instructions.Add(instr);
					prev = instr;
				}
			}
			foreach (Instruction instr2 in CS$<>8__locals1.bodyTo.Instructions)
			{
				Mono.Cecil.Cil.OperandType operandType = instr2.OpCode.OperandType;
				if (operandType != Mono.Cecil.Cil.OperandType.InlineBrTarget)
				{
					if (operandType == Mono.Cecil.Cil.OperandType.InlineSwitch)
					{
						int[] offsets = (int[])instr2.Operand;
						Instruction[] targets = new Instruction[offsets.Length];
						for (int i = 0; i < offsets.Length; i++)
						{
							targets[i] = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(offsets[i]);
						}
						instr2.Operand = targets;
						continue;
					}
					if (operandType != Mono.Cecil.Cil.OperandType.ShortInlineBrTarget)
					{
						continue;
					}
				}
				instr2.Operand = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2((int)instr2.Operand);
			}
			foreach (ExceptionHandlingClause clause in bodyFrom.ExceptionHandlingClauses)
			{
				Mono.Cecil.Cil.ExceptionHandler handler = new Mono.Cecil.Cil.ExceptionHandler((ExceptionHandlerType)clause.Flags);
				CS$<>8__locals1.bodyTo.ExceptionHandlers.Add(handler);
				handler.TryStart = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(clause.TryOffset);
				handler.TryEnd = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(clause.TryOffset + clause.TryLength);
				handler.FilterStart = ((handler.HandlerType != ExceptionHandlerType.Filter) ? null : CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(clause.FilterOffset));
				handler.HandlerStart = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(clause.HandlerOffset);
				handler.HandlerEnd = CS$<>8__locals1.<_CopyMethodToDefinition>g__GetInstruction|2(clause.HandlerOffset + clause.HandlerLength);
				handler.CatchType = ((handler.HandlerType != ExceptionHandlerType.Catch) ? null : ((clause.CatchType == null) ? null : CS$<>8__locals1.moduleTo.ImportReference(clause.CatchType)));
			}
		}

		// Token: 0x06002D2D RID: 11565 RVA: 0x0009771C File Offset: 0x0009591C
		static DynamicMethodDefinition()
		{
			bool preferCecil;
			if (PlatformDetection.Runtime != RuntimeKind.Mono || DynamicMethodDefinition._IsNewMonoSRE || DynamicMethodDefinition._IsOldMonoSRE)
			{
				if (PlatformDetection.Runtime != RuntimeKind.Mono)
				{
					Type type = typeof(ILGenerator).Assembly.GetType("System.Reflection.Emit.DynamicILGenerator");
					preferCecil = ((type != null) ? type.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic) : null) == null;
				}
				else
				{
					preferCecil = false;
				}
			}
			else
			{
				preferCecil = true;
			}
			DynamicMethodDefinition._PreferCecil = preferCecil;
			DynamicMethodDefinition.c_DebuggableAttribute = typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
			DynamicMethodDefinition.c_UnverifiableCodeAttribute = typeof(UnverifiableCodeAttribute).GetConstructor(ArrayEx.Empty<Type>());
			DynamicMethodDefinition.c_IgnoresAccessChecksToAttribute = typeof(IgnoresAccessChecksToAttribute).GetConstructor(new Type[] { typeof(string) });
			DynamicMethodDefinition.t__IDMDGenerator = typeof(IDMDGenerator);
			DynamicMethodDefinition._DMDGeneratorCache = new ConcurrentDictionary<string, IDMDGenerator>();
			DynamicMethodDefinition._InitCopier();
		}

		// Token: 0x17000839 RID: 2105
		// (get) Token: 0x06002D2E RID: 11566 RVA: 0x00097874 File Offset: 0x00095A74
		public static bool IsDynamicILAvailable
		{
			get
			{
				return !DynamicMethodDefinition._PreferCecil;
			}
		}

		// Token: 0x1700083A RID: 2106
		// (get) Token: 0x06002D2F RID: 11567 RVA: 0x0009787E File Offset: 0x00095A7E
		[Nullable(2)]
		public MethodBase OriginalMethod
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x1700083B RID: 2107
		// (get) Token: 0x06002D30 RID: 11568 RVA: 0x00097886 File Offset: 0x00095A86
		public MethodDefinition Definition { get; }

		// Token: 0x1700083C RID: 2108
		// (get) Token: 0x06002D31 RID: 11569 RVA: 0x0009788E File Offset: 0x00095A8E
		public ModuleDefinition Module { get; }

		// Token: 0x1700083D RID: 2109
		// (get) Token: 0x06002D32 RID: 11570 RVA: 0x00097896 File Offset: 0x00095A96
		[Nullable(2)]
		public string Name
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x1700083E RID: 2110
		// (get) Token: 0x06002D33 RID: 11571 RVA: 0x0009789E File Offset: 0x00095A9E
		// (set) Token: 0x06002D34 RID: 11572 RVA: 0x000978A6 File Offset: 0x00095AA6
		public bool Debug { get; set; }

		// Token: 0x06002D35 RID: 11573 RVA: 0x000978B0 File Offset: 0x00095AB0
		private static bool GetDefaultDebugValue()
		{
			bool value;
			return Switches.TryGetSwitchEnabled("DMDDebug", out value) && value;
		}

		// Token: 0x06002D36 RID: 11574 RVA: 0x000978CC File Offset: 0x00095ACC
		public DynamicMethodDefinition(MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			this.OriginalMethod = method;
			this.Debug = DynamicMethodDefinition.GetDefaultDebugValue();
			ModuleDefinition module;
			MethodDefinition definition;
			this.LoadFromMethod(method, out module, out definition);
			this.Module = module;
			this.Definition = definition;
		}

		// Token: 0x06002D37 RID: 11575 RVA: 0x00097920 File Offset: 0x00095B20
		public DynamicMethodDefinition(DynamicMethodDefinition method)
		{
			Helpers.ThrowIfArgumentNull<DynamicMethodDefinition>(method, "method");
			this.OriginalMethod = null;
			this.Debug = DynamicMethodDefinition.GetDefaultDebugValue();
			this.Name = method.Name;
			ModuleDefinition module;
			MethodDefinition definition;
			this.CreateFromDmd(method, out module, out definition);
			this.Module = module;
			this.Definition = definition;
		}

		// Token: 0x06002D38 RID: 11576 RVA: 0x00097980 File Offset: 0x00095B80
		public DynamicMethodDefinition(string name, [Nullable(2)] Type returnType, Type[] parameterTypes)
		{
			Helpers.ThrowIfArgumentNull<string>(name, "name");
			Helpers.ThrowIfArgumentNull<Type[]>(parameterTypes, "parameterTypes");
			this.Name = name;
			this.OriginalMethod = null;
			this.Debug = DynamicMethodDefinition.GetDefaultDebugValue();
			ModuleDefinition module;
			MethodDefinition definition;
			this._CreateDynModule(name, returnType, parameterTypes, out module, out definition);
			this.Module = module;
			this.Definition = definition;
		}

		// Token: 0x06002D39 RID: 11577 RVA: 0x000979E8 File Offset: 0x00095BE8
		[MemberNotNull("Definition")]
		public ILProcessor GetILProcessor()
		{
			if (this.Definition == null)
			{
				throw new InvalidOperationException();
			}
			return this.Definition.Body.GetILProcessor();
		}

		// Token: 0x06002D3A RID: 11578 RVA: 0x00097A08 File Offset: 0x00095C08
		[MemberNotNull("Definition")]
		public ILGenerator GetILGenerator()
		{
			if (this.Definition == null)
			{
				throw new InvalidOperationException();
			}
			return new CecilILGenerator(this.Definition.Body.GetILProcessor()).GetProxy();
		}

		// Token: 0x06002D3B RID: 11579 RVA: 0x00097A34 File Offset: 0x00095C34
		private void _CreateDynModule(string name, [Nullable(2)] Type returnType, Type[] parameterTypes, out ModuleDefinition Module, out MethodDefinition Definition)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 2);
			defaultInterpolatedStringHandler.AppendLiteral("DMD:DynModule<");
			defaultInterpolatedStringHandler.AppendFormatted(name);
			defaultInterpolatedStringHandler.AppendLiteral(">?");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.GetHashCode());
			ModuleDefinition moduleDefinition;
			Module = (moduleDefinition = ModuleDefinition.CreateModule(defaultInterpolatedStringHandler.ToStringAndClear(), new ModuleParameters
			{
				Kind = ModuleKind.Dll,
				ReflectionImporterProvider = MMReflectionImporter.ProviderNoDefault
			}));
			ModuleDefinition module = moduleDefinition;
			string @namespace = "";
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(6, 2);
			defaultInterpolatedStringHandler2.AppendLiteral("DMD<");
			defaultInterpolatedStringHandler2.AppendFormatted(name);
			defaultInterpolatedStringHandler2.AppendLiteral(">?");
			defaultInterpolatedStringHandler2.AppendFormatted<int>(this.GetHashCode());
			TypeDefinition type = new TypeDefinition(@namespace, defaultInterpolatedStringHandler2.ToStringAndClear(), Mono.Cecil.TypeAttributes.Public);
			module.Types.Add(type);
			MethodDefinition methodDefinition;
			Definition = (methodDefinition = new MethodDefinition(name, Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig, (returnType != null) ? module.ImportReference(returnType) : module.TypeSystem.Void));
			MethodDefinition def = methodDefinition;
			foreach (Type paramType in parameterTypes)
			{
				def.Parameters.Add(new ParameterDefinition(module.ImportReference(paramType)));
			}
			type.Methods.Add(def);
		}

		// Token: 0x06002D3C RID: 11580 RVA: 0x00097B6C File Offset: 0x00095D6C
		private void CreateFromDmd(DynamicMethodDefinition src, out ModuleDefinition Module, out MethodDefinition Definition)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 2);
			defaultInterpolatedStringHandler.AppendLiteral("DMD:DynModule<");
			defaultInterpolatedStringHandler.AppendFormatted(src.Name);
			defaultInterpolatedStringHandler.AppendLiteral(">?");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.GetHashCode());
			ModuleDefinition moduleDefinition;
			Module = (moduleDefinition = ModuleDefinition.CreateModule(defaultInterpolatedStringHandler.ToStringAndClear(), new ModuleParameters
			{
				Kind = ModuleKind.Dll,
				ReflectionImporterProvider = MMReflectionImporter.ProviderNoDefault
			}));
			ModuleDefinition module = moduleDefinition;
			string @namespace = "";
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(6, 2);
			defaultInterpolatedStringHandler2.AppendLiteral("DMD<");
			defaultInterpolatedStringHandler2.AppendFormatted(src.Name);
			defaultInterpolatedStringHandler2.AppendLiteral(">?");
			defaultInterpolatedStringHandler2.AppendFormatted<int>(this.GetHashCode());
			TypeDefinition type = new TypeDefinition(@namespace, defaultInterpolatedStringHandler2.ToStringAndClear(), Mono.Cecil.TypeAttributes.Public);
			module.Types.Add(type);
			MethodDefinition def = new MethodDefinition(src.Name, Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig, module.ImportReference(src.Definition.ReturnType));
			type.Methods.Add(def);
			MethodDefinition methodDefinition;
			Definition = (methodDefinition = src.Definition.Clone(def));
			def = methodDefinition;
			def.DeclaringType = type;
		}

		// Token: 0x06002D3D RID: 11581 RVA: 0x00097C84 File Offset: 0x00095E84
		private void LoadFromMethod(MethodBase orig, out ModuleDefinition Module, out MethodDefinition def)
		{
			ParameterInfo[] args = orig.GetParameters();
			int offs = 0;
			Type[] argTypes;
			if (!orig.IsStatic)
			{
				offs++;
				argTypes = new Type[args.Length + 1];
				argTypes[0] = orig.GetThisParamType();
			}
			else
			{
				argTypes = new Type[args.Length];
			}
			for (int i = 0; i < args.Length; i++)
			{
				argTypes[i + offs] = args[i].ParameterType;
			}
			string id = orig.GetID(null, null, true, false, true);
			MethodInfo methodInfo = orig as MethodInfo;
			this._CreateDynModule(id, (methodInfo != null) ? methodInfo.ReturnType : null, argTypes, out Module, out def);
			DynamicMethodDefinition._CopyMethodToDefinition(orig, def);
			if (!orig.IsStatic)
			{
				def.Parameters[0].Name = "this";
			}
			for (int j = 0; j < args.Length; j++)
			{
				def.Parameters[j + offs].Name = args[j].Name;
			}
		}

		// Token: 0x06002D3E RID: 11582 RVA: 0x00097D5D File Offset: 0x00095F5D
		public MethodInfo Generate()
		{
			return this.Generate(null);
		}

		// Token: 0x06002D3F RID: 11583 RVA: 0x00097D68 File Offset: 0x00095F68
		public MethodInfo Generate([Nullable(2)] object context)
		{
			object swValue;
			string dmdType = (Switches.TryGetSwitchValue("DMDType", out swValue) ? (swValue as string) : null);
			if (dmdType != null)
			{
				if (dmdType.Equals("dynamicmethod", StringComparison.OrdinalIgnoreCase) || dmdType.Equals("dm", StringComparison.OrdinalIgnoreCase))
				{
					return DMDGenerator<DMDEmitDynamicMethodGenerator>.Generate(this, context);
				}
				if (dmdType.Equals("cecil", StringComparison.OrdinalIgnoreCase) || dmdType.Equals("md", StringComparison.OrdinalIgnoreCase))
				{
					return DMDGenerator<DMDCecilGenerator>.Generate(this, context);
				}
				if (dmdType.Equals("methodbuilder", StringComparison.OrdinalIgnoreCase) || dmdType.Equals("mb", StringComparison.OrdinalIgnoreCase))
				{
					return DMDGenerator<DMDEmitMethodBuilderGenerator>.Generate(this, context);
				}
			}
			if (dmdType != null)
			{
				Type type = ReflectionHelper.GetType(dmdType);
				if (type != null)
				{
					if (!DynamicMethodDefinition.t__IDMDGenerator.IsCompatible(type))
					{
						throw new ArgumentException("Invalid DMDGenerator type: " + dmdType);
					}
					return DynamicMethodDefinition._DMDGeneratorCache.GetOrAdd(dmdType, (string _) => (IDMDGenerator)Activator.CreateInstance(type)).Generate(this, context);
				}
			}
			if (DynamicMethodDefinition._PreferCecil)
			{
				return DMDGenerator<DMDCecilGenerator>.Generate(this, context);
			}
			if (this.Debug)
			{
				return DMDGenerator<DMDEmitMethodBuilderGenerator>.Generate(this, context);
			}
			if (this.Definition.Body.ExceptionHandlers.Any(delegate(Mono.Cecil.Cil.ExceptionHandler eh)
			{
				ExceptionHandlerType handlerType = eh.HandlerType;
				return handlerType == ExceptionHandlerType.Filter || handlerType == ExceptionHandlerType.Fault;
			}))
			{
				return DMDGenerator<DMDEmitMethodBuilderGenerator>.Generate(this, context);
			}
			return DMDGenerator<DMDEmitDynamicMethodGenerator>.Generate(this, context);
		}

		// Token: 0x06002D40 RID: 11584 RVA: 0x00097EC5 File Offset: 0x000960C5
		public void Dispose()
		{
			if (this.isDisposed)
			{
				return;
			}
			this.isDisposed = true;
			ModuleDefinition module = this.Module;
			if (module == null)
			{
				return;
			}
			module.Dispose();
		}

		// Token: 0x06002D41 RID: 11585 RVA: 0x00097EE8 File Offset: 0x000960E8
		public string GetDumpName(string type)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 2);
			defaultInterpolatedStringHandler.AppendLiteral("DMDASM.");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.GUID.GetHashCode(), "X8");
			defaultInterpolatedStringHandler.AppendFormatted(string.IsNullOrEmpty(type) ? "" : ("." + type));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x04003AAB RID: 15019
		private static Mono.Cecil.Cil.OpCode[] _CecilOpCodes1X = null;

		// Token: 0x04003AAC RID: 15020
		private static Mono.Cecil.Cil.OpCode[] _CecilOpCodes2X = null;

		// Token: 0x04003AAD RID: 15021
		internal static readonly bool _IsNewMonoSRE = PlatformDetection.Runtime == RuntimeKind.Mono && typeof(DynamicMethod).GetField("il_info", BindingFlags.Instance | BindingFlags.NonPublic) != null;

		// Token: 0x04003AAE RID: 15022
		internal static readonly bool _IsOldMonoSRE = PlatformDetection.Runtime == RuntimeKind.Mono && !DynamicMethodDefinition._IsNewMonoSRE && typeof(DynamicMethod).GetField("ilgen", BindingFlags.Instance | BindingFlags.NonPublic) != null;

		// Token: 0x04003AAF RID: 15023
		private static bool _PreferCecil;

		// Token: 0x04003AB0 RID: 15024
		internal static readonly ConstructorInfo c_DebuggableAttribute;

		// Token: 0x04003AB1 RID: 15025
		internal static readonly ConstructorInfo c_UnverifiableCodeAttribute;

		// Token: 0x04003AB2 RID: 15026
		internal static readonly ConstructorInfo c_IgnoresAccessChecksToAttribute;

		// Token: 0x04003AB3 RID: 15027
		internal static readonly Type t__IDMDGenerator;

		// Token: 0x04003AB4 RID: 15028
		internal static readonly ConcurrentDictionary<string, IDMDGenerator> _DMDGeneratorCache;

		// Token: 0x04003ABA RID: 15034
		private Guid GUID = Guid.NewGuid();

		// Token: 0x04003ABB RID: 15035
		private bool isDisposed;

		// Token: 0x0200089B RID: 2203
		[NullableContext(0)]
		private enum TokenResolutionMode
		{
			// Token: 0x04003ABD RID: 15037
			Any,
			// Token: 0x04003ABE RID: 15038
			Type,
			// Token: 0x04003ABF RID: 15039
			Method,
			// Token: 0x04003AC0 RID: 15040
			Field
		}
	}
}
