using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MonoMod.Utils
{
	// Token: 0x0200087E RID: 2174
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class DMDCecilGenerator : DMDGenerator<DMDCecilGenerator>
	{
		// Token: 0x06002CAB RID: 11435 RVA: 0x00093770 File Offset: 0x00091970
		protected override MethodInfo GenerateCore(DynamicMethodDefinition dmd, [Nullable(2)] object context)
		{
			DMDCecilGenerator.<>c__DisplayClass0_0 CS$<>8__locals1 = new DMDCecilGenerator.<>c__DisplayClass0_0();
			DMDCecilGenerator.<>c__DisplayClass0_0 CS$<>8__locals2 = CS$<>8__locals1;
			MethodDefinition definition = dmd.Definition;
			if (definition == null)
			{
				throw new InvalidOperationException();
			}
			CS$<>8__locals2.def = definition;
			TypeDefinition typeDef = context as TypeDefinition;
			bool moduleIsTemporary = false;
			CS$<>8__locals1.module = ((typeDef != null) ? typeDef.Module : null);
			HashSet<string> accessChecksIgnored = null;
			MethodInfo result;
			try
			{
				if (typeDef == null || CS$<>8__locals1.module == null)
				{
					moduleIsTemporary = true;
					string name = dmd.GetDumpName("Cecil");
					CS$<>8__locals1.module = ModuleDefinition.CreateModule(name, new ModuleParameters
					{
						Kind = ModuleKind.Dll,
						ReflectionImporterProvider = MMReflectionImporter.ProviderNoDefault
					});
					accessChecksIgnored = new HashSet<string>();
					CS$<>8__locals1.module.Assembly.CustomAttributes.Add(new CustomAttribute(CS$<>8__locals1.module.ImportReference(DynamicMethodDefinition.c_UnverifiableCodeAttribute)));
					if (dmd.Debug)
					{
						CustomAttribute caDebug = new CustomAttribute(CS$<>8__locals1.module.ImportReference(DynamicMethodDefinition.c_DebuggableAttribute));
						caDebug.ConstructorArguments.Add(new CustomAttributeArgument(CS$<>8__locals1.module.ImportReference(typeof(DebuggableAttribute.DebuggingModes)), DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations));
						CS$<>8__locals1.module.Assembly.CustomAttributes.Add(caDebug);
					}
					string @namespace = "";
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 2);
					defaultInterpolatedStringHandler.AppendLiteral("DMD<");
					MethodBase originalMethod = dmd.OriginalMethod;
					string value;
					if (originalMethod == null)
					{
						value = null;
					}
					else
					{
						string name3 = originalMethod.Name;
						value = ((name3 != null) ? name3.Replace('.', '_') : null);
					}
					defaultInterpolatedStringHandler.AppendFormatted(value);
					defaultInterpolatedStringHandler.AppendLiteral(">?");
					defaultInterpolatedStringHandler.AppendFormatted<int>(this.GetHashCode());
					typeDef = new TypeDefinition(@namespace, defaultInterpolatedStringHandler.ToStringAndClear(), Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed)
					{
						BaseType = CS$<>8__locals1.module.TypeSystem.Object
					};
					CS$<>8__locals1.module.Types.Add(typeDef);
				}
				CS$<>8__locals1.clone = null;
				new TypeReference("System.Runtime.CompilerServices", "IsVolatile", CS$<>8__locals1.module, CS$<>8__locals1.module.TypeSystem.CoreLibrary);
				Relinker relinker = delegate(IMetadataTokenProvider mtp, [Nullable(2)] IGenericParameterProvider ctx)
				{
					if (mtp == CS$<>8__locals1.def)
					{
						return CS$<>8__locals1.clone;
					}
					MethodReference mr = mtp as MethodReference;
					if (mr != null && mr.FullName == CS$<>8__locals1.def.FullName && mr.DeclaringType.FullName == CS$<>8__locals1.def.DeclaringType.FullName && mr.DeclaringType.Scope.Name == CS$<>8__locals1.def.DeclaringType.Scope.Name)
					{
						return CS$<>8__locals1.clone;
					}
					return CS$<>8__locals1.module.ImportReference(mtp);
				};
				CS$<>8__locals1.clone = new MethodDefinition(dmd.Name ?? ("_" + CS$<>8__locals1.def.Name.Replace('.', '_')), CS$<>8__locals1.def.Attributes, CS$<>8__locals1.module.TypeSystem.Void)
				{
					MethodReturnType = CS$<>8__locals1.def.MethodReturnType,
					Attributes = (Mono.Cecil.MethodAttributes.FamANDAssem | Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig),
					ImplAttributes = Mono.Cecil.MethodImplAttributes.IL,
					DeclaringType = typeDef,
					NoInlining = true
				};
				foreach (ParameterDefinition param in CS$<>8__locals1.def.Parameters)
				{
					CS$<>8__locals1.clone.Parameters.Add(param.Clone().Relink(relinker, CS$<>8__locals1.clone));
				}
				CS$<>8__locals1.clone.ReturnType = CS$<>8__locals1.def.ReturnType.Relink(relinker, CS$<>8__locals1.clone);
				typeDef.Methods.Add(CS$<>8__locals1.clone);
				CS$<>8__locals1.clone.HasThis = CS$<>8__locals1.def.HasThis;
				Mono.Cecil.Cil.MethodBody body = (CS$<>8__locals1.clone.Body = CS$<>8__locals1.def.Body.Clone(CS$<>8__locals1.clone));
				foreach (VariableDefinition variableDefinition in CS$<>8__locals1.clone.Body.Variables)
				{
					variableDefinition.VariableType = variableDefinition.VariableType.Relink(relinker, CS$<>8__locals1.clone);
				}
				foreach (ExceptionHandler handler in CS$<>8__locals1.clone.Body.ExceptionHandlers)
				{
					if (handler.CatchType != null)
					{
						handler.CatchType = handler.CatchType.Relink(relinker, CS$<>8__locals1.clone);
					}
				}
				for (int instri = 0; instri < body.Instructions.Count; instri++)
				{
					Instruction instruction = body.Instructions[instri];
					object operand = instruction.Operand;
					ParameterDefinition param2 = operand as ParameterDefinition;
					if (param2 != null)
					{
						operand = CS$<>8__locals1.clone.Parameters[param2.Index];
					}
					else
					{
						IMetadataTokenProvider mtp2 = operand as IMetadataTokenProvider;
						if (mtp2 != null)
						{
							operand = mtp2.Relink(relinker, CS$<>8__locals1.clone);
						}
					}
					DynamicMethodReference dynamicMethodReference = operand as DynamicMethodReference;
					if (accessChecksIgnored != null)
					{
						MemberReference mref = operand as MemberReference;
						if (mref != null)
						{
							TypeReference typeReference = mref as TypeReference;
							IMetadataScope asmRef = ((typeReference != null) ? typeReference.Scope : null) ?? mref.DeclaringType.Scope;
							if (!accessChecksIgnored.Contains(asmRef.Name))
							{
								CustomAttribute caAccess = new CustomAttribute(CS$<>8__locals1.module.ImportReference(DynamicMethodDefinition.c_IgnoresAccessChecksToAttribute));
								caAccess.ConstructorArguments.Add(new CustomAttributeArgument(CS$<>8__locals1.module.ImportReference(typeof(DebuggableAttribute.DebuggingModes)), asmRef.Name));
								CS$<>8__locals1.module.Assembly.CustomAttributes.Add(caAccess);
								accessChecksIgnored.Add(asmRef.Name);
							}
						}
					}
					instruction.Operand = operand;
				}
				CS$<>8__locals1.clone.HasThis = false;
				if (CS$<>8__locals1.def.HasThis)
				{
					TypeReference type = CS$<>8__locals1.def.DeclaringType;
					if (type.IsValueType)
					{
						type = new ByReferenceType(type);
					}
					CS$<>8__locals1.clone.Parameters.Insert(0, new ParameterDefinition("<>_this", Mono.Cecil.ParameterAttributes.None, type.Relink(relinker, CS$<>8__locals1.clone)));
				}
				object dumpToVal;
				string envDmdDump = (Switches.TryGetSwitchValue("DMDDumpTo", out dumpToVal) ? (dumpToVal as string) : null);
				if (!string.IsNullOrEmpty(envDmdDump))
				{
					string dir = Path.GetFullPath(envDmdDump);
					string name2 = CS$<>8__locals1.module.Name + ".dll";
					string path = Path.Combine(dir, name2);
					dir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					if (File.Exists(path))
					{
						File.Delete(path);
					}
					using (Stream fileStream = File.OpenWrite(path))
					{
						CS$<>8__locals1.module.Write(fileStream);
					}
				}
				MethodInfo method = ReflectionHelper.Load(CS$<>8__locals1.module).GetType(typeDef.FullName.Replace("+", "\\+", StringComparison.Ordinal), false, false).GetMethod(CS$<>8__locals1.clone.Name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					throw new InvalidOperationException("Could not find generated method");
				}
				result = method;
			}
			finally
			{
				if (moduleIsTemporary)
				{
					CS$<>8__locals1.module.Dispose();
				}
				CS$<>8__locals1.module = null;
			}
			return result;
		}
	}
}
