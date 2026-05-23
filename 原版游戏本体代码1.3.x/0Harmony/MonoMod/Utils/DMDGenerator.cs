using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x0200087D RID: 2173
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class DMDGenerator<[Nullable(0)] TSelf> : IDMDGenerator where TSelf : DMDGenerator<TSelf>, new()
	{
		// Token: 0x06002CA6 RID: 11430
		protected abstract MethodInfo GenerateCore(DynamicMethodDefinition dmd, [Nullable(2)] object context);

		// Token: 0x06002CA7 RID: 11431 RVA: 0x000936E3 File Offset: 0x000918E3
		MethodInfo IDMDGenerator.Generate(DynamicMethodDefinition dmd, [Nullable(2)] object context)
		{
			return DMDGenerator<TSelf>.Postbuild(this.GenerateCore(dmd, context));
		}

		// Token: 0x06002CA8 RID: 11432 RVA: 0x000936F2 File Offset: 0x000918F2
		public static MethodInfo Generate(DynamicMethodDefinition dmd, [Nullable(2)] object context = null)
		{
			TSelf tself;
			if ((tself = DMDGenerator<TSelf>.Instance) == null)
			{
				tself = (DMDGenerator<TSelf>.Instance = Activator.CreateInstance<TSelf>());
			}
			return DMDGenerator<TSelf>.Postbuild(tself.GenerateCore(dmd, context));
		}

		// Token: 0x06002CA9 RID: 11433 RVA: 0x00093720 File Offset: 0x00091920
		internal static MethodInfo Postbuild(MethodInfo mi)
		{
			if (PlatformDetection.Runtime == RuntimeKind.Mono && !(mi is DynamicMethod) && mi.DeclaringType != null)
			{
				Module module = mi.Module;
				if (module == null)
				{
					return mi;
				}
				Assembly asm = module.Assembly;
				if (asm.GetType() == null)
				{
					return mi;
				}
				asm.SetMonoCorlibInternal(true);
			}
			return mi;
		}

		// Token: 0x04003A5F RID: 14943
		[Nullable(2)]
		private static TSelf Instance;
	}
}
