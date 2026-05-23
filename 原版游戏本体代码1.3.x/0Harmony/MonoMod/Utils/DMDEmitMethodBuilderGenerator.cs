using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x0200088C RID: 2188
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class DMDEmitMethodBuilderGenerator : DMDGenerator<DMDEmitMethodBuilderGenerator>
	{
		// Token: 0x06002CD9 RID: 11481 RVA: 0x00095F00 File Offset: 0x00094100
		protected override MethodInfo GenerateCore(DynamicMethodDefinition dmd, [Nullable(2)] object context)
		{
			TypeBuilder typeBuilder = context as TypeBuilder;
			MethodBuilder method = DMDEmitMethodBuilderGenerator.GenerateMethodBuilder(dmd, typeBuilder);
			typeBuilder = (TypeBuilder)method.DeclaringType;
			Type type = typeBuilder.CreateType();
			object dumpToVal;
			if (!string.IsNullOrEmpty(Switches.TryGetSwitchValue("DMDDumpTo", out dumpToVal) ? (dumpToVal as string) : null))
			{
				string path = method.Module.FullyQualifiedName;
				string name = Path.GetFileName(path);
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				((AssemblyBuilder)typeBuilder.Assembly).Save(name);
			}
			return type.GetMethod(method.Name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x06002CDA RID: 11482 RVA: 0x00095FB0 File Offset: 0x000941B0
		public static MethodBuilder GenerateMethodBuilder(DynamicMethodDefinition dmd, [Nullable(2)] TypeBuilder typeBuilder)
		{
			Helpers.ThrowIfArgumentNull<DynamicMethodDefinition>(dmd, "dmd");
			MethodBase orig = dmd.OriginalMethod;
			MethodDefinition def = dmd.Definition;
			if (typeBuilder == null)
			{
				object dumpToVal;
				string dumpDir = (Switches.TryGetSwitchValue("DMDDumpTo", out dumpToVal) ? (dumpToVal as string) : null);
				if (string.IsNullOrEmpty(dumpDir))
				{
					dumpDir = null;
				}
				else
				{
					dumpDir = Path.GetFullPath(dumpDir);
				}
				bool collect = string.IsNullOrEmpty(dumpDir) && DMDEmitMethodBuilderGenerator._MBCanRunAndCollect;
				AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName
				{
					Name = dmd.GetDumpName("MethodBuilder")
				}, collect ? AssemblyBuilderAccess.RunAndCollect : AssemblyBuilderAccess.RunAndSave, dumpDir);
				ab.SetCustomAttribute(new CustomAttributeBuilder(DynamicMethodDefinition.c_UnverifiableCodeAttribute, new object[0]));
				if (dmd.Debug)
				{
					ab.SetCustomAttribute(new CustomAttributeBuilder(DynamicMethodDefinition.c_DebuggableAttribute, new object[] { DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations }));
				}
				ModuleBuilder moduleBuilder = ab.DefineDynamicModule(ab.GetName().Name + ".dll", ab.GetName().Name + ".dll", dmd.Debug);
				FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(6, 2);
				formatInterpolatedStringHandler.AppendLiteral("DMD<");
				formatInterpolatedStringHandler.AppendFormatted<MethodBase>(orig);
				formatInterpolatedStringHandler.AppendLiteral(">?");
				formatInterpolatedStringHandler.AppendFormatted<int>(dmd.GetHashCode());
				typeBuilder = moduleBuilder.DefineType(DebugFormatter.Format(ref formatInterpolatedStringHandler), System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed);
			}
			Type[] argTypes;
			Type[][] argTypesModReq;
			Type[][] argTypesModOpt;
			if (orig != null)
			{
				ParameterInfo[] args = orig.GetParameters();
				int offs = 0;
				if (!orig.IsStatic)
				{
					offs++;
					argTypes = new Type[args.Length + 1];
					argTypesModReq = new Type[args.Length + 1][];
					argTypesModOpt = new Type[args.Length + 1][];
					argTypes[0] = orig.GetThisParamType();
					argTypesModReq[0] = Type.EmptyTypes;
					argTypesModOpt[0] = Type.EmptyTypes;
				}
				else
				{
					argTypes = new Type[args.Length];
					argTypesModReq = new Type[args.Length][];
					argTypesModOpt = new Type[args.Length][];
				}
				for (int i = 0; i < args.Length; i++)
				{
					argTypes[i + offs] = args[i].ParameterType;
					argTypesModReq[i + offs] = args[i].GetRequiredCustomModifiers();
					argTypesModOpt[i + offs] = args[i].GetOptionalCustomModifiers();
				}
			}
			else
			{
				int offs2 = 0;
				if (def.HasThis)
				{
					offs2++;
					argTypes = new Type[def.Parameters.Count + 1];
					argTypesModReq = new Type[def.Parameters.Count + 1][];
					argTypesModOpt = new Type[def.Parameters.Count + 1][];
					Type type = def.DeclaringType.ResolveReflection();
					if (type.IsValueType)
					{
						type = type.MakeByRefType();
					}
					argTypes[0] = type;
					argTypesModReq[0] = Type.EmptyTypes;
					argTypesModOpt[0] = Type.EmptyTypes;
				}
				else
				{
					argTypes = new Type[def.Parameters.Count];
					argTypesModReq = new Type[def.Parameters.Count][];
					argTypesModOpt = new Type[def.Parameters.Count][];
				}
				List<Type> modReq = new List<Type>();
				List<Type> modOpt = new List<Type>();
				for (int j = 0; j < def.Parameters.Count; j++)
				{
					Type paramType;
					Type[] paramTypeModReq;
					Type[] paramTypeModOpt;
					_DMDEmit.ResolveWithModifiers(def.Parameters[j].ParameterType, out paramType, out paramTypeModReq, out paramTypeModOpt, modReq, modOpt);
					argTypes[j + offs2] = paramType;
					argTypesModReq[j + offs2] = paramTypeModReq;
					argTypesModOpt[j + offs2] = paramTypeModOpt;
				}
			}
			Type returnType;
			Type[] returnTypeModReq;
			Type[] returnTypeModOpt;
			_DMDEmit.ResolveWithModifiers(def.ReturnType, out returnType, out returnTypeModReq, out returnTypeModOpt, null, null);
			TypeBuilder typeBuilder2 = typeBuilder;
			string name;
			if ((name = dmd.Name) == null)
			{
				name = (((orig != null) ? orig.Name : null) ?? def.Name).Replace('.', '_');
			}
			MethodBuilder mb = typeBuilder2.DefineMethod(name, System.Reflection.MethodAttributes.FamANDAssem | System.Reflection.MethodAttributes.Family | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig, CallingConventions.Standard, returnType, returnTypeModReq, returnTypeModOpt, argTypes, argTypesModReq, argTypesModOpt);
			ILGenerator il = mb.GetILGenerator();
			_DMDEmit.Generate(dmd, mb, il);
			return mb;
		}

		// Token: 0x04003A8A RID: 14986
		private static readonly bool _MBCanRunAndCollect = Enum.IsDefined(typeof(AssemblyBuilderAccess), "RunAndCollect");
	}
}
