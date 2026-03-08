using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MonoMod.Utils.Cil
{
	// Token: 0x020008FC RID: 2300
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ILGeneratorShim
	{
		// Token: 0x17000867 RID: 2151
		// (get) Token: 0x0600304E RID: 12366
		public abstract int ILOffset { get; }

		// Token: 0x0600304F RID: 12367
		public abstract void BeginCatchBlock(Type exceptionType);

		// Token: 0x06003050 RID: 12368
		public abstract void BeginExceptFilterBlock();

		// Token: 0x06003051 RID: 12369
		public abstract Label BeginExceptionBlock();

		// Token: 0x06003052 RID: 12370
		public abstract void BeginFaultBlock();

		// Token: 0x06003053 RID: 12371
		public abstract void BeginFinallyBlock();

		// Token: 0x06003054 RID: 12372
		public abstract void BeginScope();

		// Token: 0x06003055 RID: 12373
		public abstract LocalBuilder DeclareLocal(Type localType);

		// Token: 0x06003056 RID: 12374
		public abstract LocalBuilder DeclareLocal(Type localType, bool pinned);

		// Token: 0x06003057 RID: 12375
		public abstract Label DefineLabel();

		// Token: 0x06003058 RID: 12376
		public abstract void Emit(System.Reflection.Emit.OpCode opcode);

		// Token: 0x06003059 RID: 12377
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, byte arg);

		// Token: 0x0600305A RID: 12378
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, double arg);

		// Token: 0x0600305B RID: 12379
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, short arg);

		// Token: 0x0600305C RID: 12380
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, int arg);

		// Token: 0x0600305D RID: 12381
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, long arg);

		// Token: 0x0600305E RID: 12382
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, ConstructorInfo con);

		// Token: 0x0600305F RID: 12383
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, Label label);

		// Token: 0x06003060 RID: 12384
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, Label[] labels);

		// Token: 0x06003061 RID: 12385
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, LocalBuilder local);

		// Token: 0x06003062 RID: 12386
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, SignatureHelper signature);

		// Token: 0x06003063 RID: 12387
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, FieldInfo field);

		// Token: 0x06003064 RID: 12388
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, MethodInfo meth);

		// Token: 0x06003065 RID: 12389
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg);

		// Token: 0x06003066 RID: 12390
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, float arg);

		// Token: 0x06003067 RID: 12391
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, string str);

		// Token: 0x06003068 RID: 12392
		public abstract void Emit(System.Reflection.Emit.OpCode opcode, Type cls);

		// Token: 0x06003069 RID: 12393
		public abstract void EmitCall(System.Reflection.Emit.OpCode opcode, MethodInfo methodInfo, [Nullable(new byte[] { 2, 1 })] Type[] optionalParameterTypes);

		// Token: 0x0600306A RID: 12394
		[NullableContext(2)]
		public abstract void EmitCalli(System.Reflection.Emit.OpCode opcode, CallingConventions callingConvention, Type returnType, [Nullable(new byte[] { 2, 1 })] Type[] parameterTypes, [Nullable(new byte[] { 2, 1 })] Type[] optionalParameterTypes);

		// Token: 0x0600306B RID: 12395
		[NullableContext(2)]
		public abstract void EmitCalli(System.Reflection.Emit.OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, [Nullable(new byte[] { 2, 1 })] Type[] parameterTypes);

		// Token: 0x0600306C RID: 12396
		public abstract void EmitWriteLine(LocalBuilder localBuilder);

		// Token: 0x0600306D RID: 12397
		public abstract void EmitWriteLine(FieldInfo fld);

		// Token: 0x0600306E RID: 12398
		public abstract void EmitWriteLine(string value);

		// Token: 0x0600306F RID: 12399
		public abstract void EndExceptionBlock();

		// Token: 0x06003070 RID: 12400
		public abstract void EndScope();

		// Token: 0x06003071 RID: 12401
		public abstract void MarkLabel(Label loc);

		// Token: 0x06003072 RID: 12402
		public abstract void ThrowException(Type excType);

		// Token: 0x06003073 RID: 12403
		public abstract void UsingNamespace(string usingNamespace);

		// Token: 0x06003074 RID: 12404 RVA: 0x000A6C9F File Offset: 0x000A4E9F
		public ILGenerator GetProxy()
		{
			return (ILGenerator)ILGeneratorShim.ILGeneratorBuilder.GenerateProxy().MakeGenericType(new Type[] { base.GetType() }).GetConstructors()[0].Invoke(new object[] { this });
		}

		// Token: 0x06003075 RID: 12405 RVA: 0x000A6CD5 File Offset: 0x000A4ED5
		public static Type GetProxyType<[Nullable(0)] TShim>() where TShim : ILGeneratorShim
		{
			return ILGeneratorShim.GetProxyType(typeof(TShim));
		}

		// Token: 0x06003076 RID: 12406 RVA: 0x000A6CE6 File Offset: 0x000A4EE6
		public static Type GetProxyType(Type tShim)
		{
			return ILGeneratorShim.GenericProxyType.MakeGenericType(new Type[] { tShim });
		}

		// Token: 0x17000868 RID: 2152
		// (get) Token: 0x06003077 RID: 12407 RVA: 0x000A6CFC File Offset: 0x000A4EFC
		public static Type GenericProxyType
		{
			get
			{
				return ILGeneratorShim.ILGeneratorBuilder.GenerateProxy();
			}
		}

		// Token: 0x020008FD RID: 2301
		[Nullable(0)]
		internal static class ILGeneratorBuilder
		{
			// Token: 0x06003079 RID: 12409 RVA: 0x000A6D04 File Offset: 0x000A4F04
			public static Type GenerateProxy()
			{
				if (ILGeneratorShim.ILGeneratorBuilder.ProxyType != null)
				{
					return ILGeneratorShim.ILGeneratorBuilder.ProxyType;
				}
				Type t_ILGenerator = typeof(ILGenerator);
				Type t_ILGeneratorProxyTarget = typeof(ILGeneratorShim);
				Assembly asm;
				using (ModuleDefinition module = ModuleDefinition.CreateModule("MonoMod.Utils.Cil.ILGeneratorProxy", new ModuleParameters
				{
					Kind = ModuleKind.Dll,
					ReflectionImporterProvider = MMReflectionImporter.Provider
				}))
				{
					CustomAttribute ca_IACTA = new CustomAttribute(module.ImportReference(DynamicMethodDefinition.c_IgnoresAccessChecksToAttribute));
					ca_IACTA.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, typeof(ILGeneratorShim).Assembly.GetName().Name));
					module.Assembly.CustomAttributes.Add(ca_IACTA);
					TypeDefinition type = new TypeDefinition("MonoMod.Utils.Cil", "ILGeneratorProxy", Mono.Cecil.TypeAttributes.Public)
					{
						BaseType = module.ImportReference(t_ILGenerator)
					};
					module.Types.Add(type);
					TypeReference tr_ILGeneratorProxyTarget = module.ImportReference(t_ILGeneratorProxyTarget);
					GenericParameter g_TTarget = new GenericParameter("TTarget", type);
					g_TTarget.Constraints.Add(new GenericParameterConstraint(tr_ILGeneratorProxyTarget));
					type.GenericParameters.Add(g_TTarget);
					FieldDefinition fd_Target = new FieldDefinition("Target", Mono.Cecil.FieldAttributes.Public, g_TTarget);
					type.Fields.Add(fd_Target);
					FieldReference fr_Target = new FieldReference("Target", g_TTarget, new GenericInstanceType(type)
					{
						GenericArguments = { g_TTarget }
					});
					MethodDefinition ctor = new MethodDefinition(".ctor", Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void);
					ctor.Parameters.Add(new ParameterDefinition(g_TTarget));
					type.Methods.Add(ctor);
					ILProcessor il = ctor.Body.GetILProcessor();
					il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
					il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
					il.Emit(Mono.Cecil.Cil.OpCodes.Stfld, fr_Target);
					il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
					foreach (MethodInfo orig in t_ILGenerator.GetMethods(BindingFlags.Instance | BindingFlags.Public))
					{
						MethodInfo target = t_ILGeneratorProxyTarget.GetMethod(orig.Name, (from p in orig.GetParameters()
							select p.ParameterType).ToArray<Type>());
						if (!(target == null))
						{
							MethodDefinition proxy = new MethodDefinition(orig.Name, Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.Virtual | Mono.Cecil.MethodAttributes.HideBySig, module.ImportReference(orig.ReturnType))
							{
								HasThis = true
							};
							foreach (ParameterInfo param in orig.GetParameters())
							{
								proxy.Parameters.Add(new ParameterDefinition(module.ImportReference(param.ParameterType)));
							}
							type.Methods.Add(proxy);
							il = proxy.Body.GetILProcessor();
							il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
							il.Emit(Mono.Cecil.Cil.OpCodes.Ldfld, fr_Target);
							foreach (ParameterDefinition param2 in proxy.Parameters)
							{
								il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, param2);
							}
							il.Emit(target.IsVirtual ? Mono.Cecil.Cil.OpCodes.Callvirt : Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(target));
							il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
						}
					}
					asm = ReflectionHelper.Load(module);
					asm.SetMonoCorlibInternal(true);
				}
				ResolveEventHandler mmcResolver = delegate(object asmSender, ResolveEventArgs asmArgs)
				{
					if (new AssemblyName(asmArgs.Name).Name == typeof(ILGeneratorShim.ILGeneratorBuilder).Assembly.GetName().Name)
					{
						return typeof(ILGeneratorShim.ILGeneratorBuilder).Assembly;
					}
					return null;
				};
				AppDomain.CurrentDomain.AssemblyResolve += mmcResolver;
				try
				{
					ILGeneratorShim.ILGeneratorBuilder.ProxyType = asm.GetType("MonoMod.Utils.Cil.ILGeneratorProxy");
				}
				finally
				{
					AppDomain.CurrentDomain.AssemblyResolve -= mmcResolver;
				}
				if (ILGeneratorShim.ILGeneratorBuilder.ProxyType == null)
				{
					StringBuilder builder = new StringBuilder();
					builder.Append("Couldn't find ILGeneratorShim proxy \"").Append("MonoMod.Utils.Cil.ILGeneratorProxy").Append("\" in autogenerated \"")
						.Append(asm.FullName)
						.AppendLine("\"");
					Type[] types;
					Exception[] exceptions;
					try
					{
						types = asm.GetTypes();
						exceptions = null;
					}
					catch (ReflectionTypeLoadException e)
					{
						types = e.Types;
						exceptions = new Exception[e.LoaderExceptions.Length + 1];
						exceptions[0] = e;
						for (int i = 0; i < e.LoaderExceptions.Length; i++)
						{
							exceptions[i + 1] = e.LoaderExceptions[i];
						}
					}
					builder.AppendLine("Listing all types in autogenerated assembly:");
					foreach (Type type2 in types)
					{
						builder.AppendLine(((type2 != null) ? type2.FullName : null) ?? "<NULL>");
					}
					if (exceptions != null && exceptions.Length != 0)
					{
						builder.AppendLine("Listing all exceptions:");
						for (int j = 0; j < exceptions.Length; j++)
						{
							StringBuilder stringBuilder = builder.Append('#').Append(j).Append(": ");
							Exception ex = exceptions[j];
							stringBuilder.AppendLine(((ex != null) ? ex.ToString() : null) ?? "NULL");
						}
					}
					throw new InvalidOperationException(builder.ToString());
				}
				return ILGeneratorShim.ILGeneratorBuilder.ProxyType;
			}

			// Token: 0x04003C04 RID: 15364
			public const string Namespace = "MonoMod.Utils.Cil";

			// Token: 0x04003C05 RID: 15365
			public const string Name = "ILGeneratorProxy";

			// Token: 0x04003C06 RID: 15366
			public const string FullName = "MonoMod.Utils.Cil.ILGeneratorProxy";

			// Token: 0x04003C07 RID: 15367
			public const string TargetName = "Target";

			// Token: 0x04003C08 RID: 15368
			[Nullable(2)]
			private static Type ProxyType;
		}
	}
}
