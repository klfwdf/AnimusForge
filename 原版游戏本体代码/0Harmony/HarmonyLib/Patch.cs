using System;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib
{
	/// <summary>A serializable patch</summary>
	// Token: 0x02000090 RID: 144
	[Serializable]
	public class Patch : IComparable
	{
		/// <summary>The method of the static patch method</summary>
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060002A9 RID: 681 RVA: 0x0000F53F File Offset: 0x0000D73F
		// (set) Token: 0x060002AA RID: 682 RVA: 0x0000F568 File Offset: 0x0000D768
		public MethodInfo PatchMethod
		{
			get
			{
				if (this.patchMethod == null)
				{
					this.patchMethod = AccessTools.GetMethodByModuleAndToken(this.moduleGUID, this.methodToken);
				}
				return this.patchMethod;
			}
			set
			{
				this.patchMethod = value;
				this.methodToken = this.patchMethod.MetadataToken;
				this.moduleGUID = this.patchMethod.Module.ModuleVersionId.ToString();
			}
		}

		/// <summary>Creates a patch</summary>
		/// <param name="patch">The method of the patch</param>
		/// <param name="index">Zero-based index</param>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="priority">The priority, see <see cref="T:HarmonyLib.Priority" /></param>
		/// <param name="before">A list of Harmony IDs for patches that should run after this patch</param>
		/// <param name="after">A list of Harmony IDs for patches that should run before this patch</param>
		/// <param name="debug">A flag that will log the replacement method via <see cref="T:HarmonyLib.FileLog" /> every time this patch is used to build the replacement, even in the future</param>
		// Token: 0x060002AB RID: 683 RVA: 0x0000F5B4 File Offset: 0x0000D7B4
		public Patch(MethodInfo patch, int index, string owner, int priority, string[] before, string[] after, bool debug)
		{
			if (patch is DynamicMethod)
			{
				throw new Exception("Cannot directly reference dynamic method \"" + patch.FullDescription() + "\" in Harmony. Use a factory method instead that will return the dynamic method.");
			}
			this.index = index;
			this.owner = owner;
			this.priority = ((priority == -1) ? 400 : priority);
			this.before = before ?? Array.Empty<string>();
			this.after = after ?? Array.Empty<string>();
			this.debug = debug;
			this.PatchMethod = patch;
		}

		/// <summary>Creates a patch</summary>
		/// <param name="method">The method of the patch</param>
		/// <param name="index">Zero-based index</param>
		/// <param name="owner">An owner (Harmony ID)</param>
		// Token: 0x060002AC RID: 684 RVA: 0x0000F63D File Offset: 0x0000D83D
		public Patch(HarmonyMethod method, int index, string owner)
			: this(method.method, index, owner, method.priority, method.before, method.after, method.debug.GetValueOrDefault())
		{
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0000F66C File Offset: 0x0000D86C
		internal Patch(int index, string owner, int priority, string[] before, string[] after, bool debug, int methodToken, string moduleGUID)
		{
			this.index = index;
			this.owner = owner;
			this.priority = ((priority == -1) ? 400 : priority);
			this.before = before ?? Array.Empty<string>();
			this.after = after ?? Array.Empty<string>();
			this.debug = debug;
			this.methodToken = methodToken;
			this.moduleGUID = moduleGUID;
		}

		/// <summary>Get the patch method or a DynamicMethod if original patch method is a patch factory</summary>
		/// <param name="original">The original method/constructor</param>
		/// <returns>The method of the patch</returns>
		// Token: 0x060002AE RID: 686 RVA: 0x0000F6DC File Offset: 0x0000D8DC
		public MethodInfo GetMethod(MethodBase original)
		{
			MethodInfo method = this.PatchMethod;
			if (method.ReturnType != typeof(DynamicMethod) && method.ReturnType != typeof(MethodInfo))
			{
				return method;
			}
			if (!method.IsStatic)
			{
				return method;
			}
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length != 1)
			{
				return method;
			}
			if (parameters[0].ParameterType != typeof(MethodBase))
			{
				return method;
			}
			return method.Invoke(null, new object[] { original }) as MethodInfo;
		}

		/// <summary>Determines whether patches are equal</summary>
		/// <param name="obj">The other patch</param>
		/// <returns>true if equal</returns>
		// Token: 0x060002AF RID: 687 RVA: 0x0000F76A File Offset: 0x0000D96A
		public override bool Equals(object obj)
		{
			return obj != null && obj is Patch && this.PatchMethod == ((Patch)obj).PatchMethod;
		}

		/// <summary>Determines how patches sort</summary>
		/// <param name="obj">The other patch</param>
		/// <returns>integer to define sort order (-1, 0, 1)</returns>
		// Token: 0x060002B0 RID: 688 RVA: 0x0000F78F File Offset: 0x0000D98F
		public int CompareTo(object obj)
		{
			return PatchInfoSerialization.PriorityComparer(obj, this.index, this.priority);
		}

		/// <summary>Hash function</summary>
		/// <returns>A hash code</returns>
		// Token: 0x060002B1 RID: 689 RVA: 0x0000F7A3 File Offset: 0x0000D9A3
		public override int GetHashCode()
		{
			return this.PatchMethod.GetHashCode();
		}

		/// <summary>Zero-based index</summary>
		// Token: 0x040001C8 RID: 456
		public readonly int index;

		/// <summary>The owner (Harmony ID)</summary>
		// Token: 0x040001C9 RID: 457
		public readonly string owner;

		/// <summary>The priority, see <see cref="T:HarmonyLib.Priority" /></summary>
		// Token: 0x040001CA RID: 458
		public readonly int priority;

		/// <summary>Keep this patch before the patches indicated in the list of Harmony IDs</summary>
		// Token: 0x040001CB RID: 459
		public readonly string[] before;

		/// <summary>Keep this patch after the patches indicated in the list of Harmony IDs</summary>
		// Token: 0x040001CC RID: 460
		public readonly string[] after;

		/// <summary>A flag that will log the replacement method via <see cref="T:HarmonyLib.FileLog" /> every time this patch is used to build the replacement, even in the future</summary>
		// Token: 0x040001CD RID: 461
		public readonly bool debug;

		// Token: 0x040001CE RID: 462
		[NonSerialized]
		private MethodInfo patchMethod;

		// Token: 0x040001CF RID: 463
		private int methodToken;

		// Token: 0x040001D0 RID: 464
		private string moduleGUID;

		/// <summary>For an infix patch, this defines the inner method that we will apply the patch to</summary>
		// Token: 0x040001D1 RID: 465
		public readonly InnerMethod innerMethod;
	}
}
