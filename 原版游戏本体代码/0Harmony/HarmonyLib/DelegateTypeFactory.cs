using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	/// <summary>A factory to create delegate types</summary>
	// Token: 0x02000004 RID: 4
	public class DelegateTypeFactory
	{
		/// <summary>Default constructor</summary>
		// Token: 0x06000004 RID: 4 RVA: 0x00002070 File Offset: 0x00000270
		public DelegateTypeFactory()
		{
			DelegateTypeFactory.counter++;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
			defaultInterpolatedStringHandler.AppendLiteral("HarmonyDTFAssembly");
			defaultInterpolatedStringHandler.AppendFormatted<int>(DelegateTypeFactory.counter);
			string name = defaultInterpolatedStringHandler.ToStringAndClear();
			AssemblyBuilder assembly = PatchTools.DefineDynamicAssembly(name);
			AssemblyBuilder assemblyBuilder = assembly;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(16, 1);
			defaultInterpolatedStringHandler2.AppendLiteral("HarmonyDTFModule");
			defaultInterpolatedStringHandler2.AppendFormatted<int>(DelegateTypeFactory.counter);
			this.module = assemblyBuilder.DefineDynamicModule(defaultInterpolatedStringHandler2.ToStringAndClear());
		}

		/// <summary>Creates a delegate type for a method</summary>
		/// <param name="method">The method</param>
		/// <returns>The new delegate type</returns>
		// Token: 0x06000005 RID: 5 RVA: 0x000020F8 File Offset: 0x000002F8
		public Type CreateDelegateType(MethodInfo method)
		{
			TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Sealed;
			ModuleBuilder moduleBuilder = this.module;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
			defaultInterpolatedStringHandler.AppendLiteral("HarmonyDTFType");
			defaultInterpolatedStringHandler.AppendFormatted<int>(DelegateTypeFactory.counter);
			TypeBuilder typeBuilder = moduleBuilder.DefineType(defaultInterpolatedStringHandler.ToStringAndClear(), attr, typeof(MulticastDelegate));
			ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[]
			{
				typeof(object),
				typeof(IntPtr)
			});
			constructor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			ParameterInfo[] parameters = method.GetParameters();
			MethodBuilder invokeMethod = typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig, method.ReturnType, parameters.Types());
			invokeMethod.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
			for (int i = 0; i < parameters.Length; i++)
			{
				invokeMethod.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Name);
			}
			return typeBuilder.CreateType();
		}

		// Token: 0x04000002 RID: 2
		private readonly ModuleBuilder module;

		// Token: 0x04000003 RID: 3
		private static int counter;
	}
}
