using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>A reverse patcher</summary>
	// Token: 0x0200009F RID: 159
	public class ReversePatcher
	{
		/// <summary>Creates a reverse patcher</summary>
		/// <param name="instance">The Harmony instance</param>
		/// <param name="original">The original method/constructor</param>
		/// <param name="standin">Your stand-in stub method as <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		// Token: 0x0600032F RID: 815 RVA: 0x000115D9 File Offset: 0x0000F7D9
		public ReversePatcher(Harmony instance, MethodBase original, HarmonyMethod standin)
		{
			this.instance = instance;
			this.original = original;
			this.standin = standin;
		}

		/// <summary>Applies the patch</summary>
		/// <param name="type">The type of patch, see <see cref="T:HarmonyLib.HarmonyReversePatchType" /></param>
		/// <returns>The generated replacement method</returns>
		// Token: 0x06000330 RID: 816 RVA: 0x000115F8 File Offset: 0x0000F7F8
		public MethodInfo Patch(HarmonyReversePatchType type = HarmonyReversePatchType.Original)
		{
			if (this.original == null)
			{
				throw new NullReferenceException("Null method for " + this.instance.Id);
			}
			this.standin.reversePatchType = new HarmonyReversePatchType?(type);
			MethodInfo transpiler = ReversePatcher.GetTranspiler(this.standin.method);
			return PatchFunctions.ReversePatch(this.standin, this.original, transpiler);
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0001165C File Offset: 0x0000F85C
		internal static MethodInfo GetTranspiler(MethodInfo method)
		{
			string methodName = method.Name;
			Type type = method.DeclaringType;
			List<MethodInfo> methods = AccessTools.GetDeclaredMethods(type);
			Type ici = typeof(IEnumerable<CodeInstruction>);
			return methods.FirstOrDefault((MethodInfo m) => !(m.ReturnType != ici) && m.Name.StartsWith("<" + methodName + ">"));
		}

		// Token: 0x04000222 RID: 546
		private readonly Harmony instance;

		// Token: 0x04000223 RID: 547
		private readonly MethodBase original;

		// Token: 0x04000224 RID: 548
		private readonly HarmonyMethod standin;
	}
}
