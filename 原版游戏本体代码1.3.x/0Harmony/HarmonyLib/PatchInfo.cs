using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>Serializable patch information</summary>
	// Token: 0x02000096 RID: 150
	[Serializable]
	public class PatchInfo
	{
		/// <summary>Returns if any of the patches wants debugging turned on</summary>
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060002DA RID: 730 RVA: 0x00010840 File Offset: 0x0000EA40
		public bool Debugging
		{
			get
			{
				if (!this.prefixes.Any((Patch p) => p.debug))
				{
					if (!this.postfixes.Any((Patch p) => p.debug))
					{
						if (!this.transpilers.Any((Patch p) => p.debug))
						{
							if (!this.finalizers.Any((Patch p) => p.debug))
							{
								if (!this.innerprefixes.Any((Patch p) => p.debug))
								{
									return this.innerpostfixes.Any((Patch p) => p.debug);
								}
							}
						}
					}
				}
				return true;
			}
		}

		/// <summary>Adds prefixes</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002DB RID: 731 RVA: 0x0001095E File Offset: 0x0000EB5E
		internal void AddPrefixes(string owner, params HarmonyMethod[] methods)
		{
			this.prefixes = PatchInfo.Add(owner, methods, this.prefixes);
		}

		/// <summary>Adds a prefix</summary>
		// Token: 0x060002DC RID: 732 RVA: 0x00010974 File Offset: 0x0000EB74
		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddPrefix(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddPrefixes(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		/// <summary>Removes prefixes</summary>
		/// <param name="owner">The owner of the prefixes, or <c>*</c> for all</param>
		// Token: 0x060002DD RID: 733 RVA: 0x000109A3 File Offset: 0x0000EBA3
		public void RemovePrefix(string owner)
		{
			this.prefixes = PatchInfo.Remove(owner, this.prefixes);
		}

		/// <summary>Adds postfixes</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002DE RID: 734 RVA: 0x000109B7 File Offset: 0x0000EBB7
		internal void AddPostfixes(string owner, params HarmonyMethod[] methods)
		{
			this.postfixes = PatchInfo.Add(owner, methods, this.postfixes);
		}

		/// <summary>Adds a postfix</summary>
		// Token: 0x060002DF RID: 735 RVA: 0x000109CC File Offset: 0x0000EBCC
		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddPostfix(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddPostfixes(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		/// <summary>Removes postfixes</summary>
		/// <param name="owner">The owner of the postfixes, or <c>*</c> for all</param>
		// Token: 0x060002E0 RID: 736 RVA: 0x000109FB File Offset: 0x0000EBFB
		public void RemovePostfix(string owner)
		{
			this.postfixes = PatchInfo.Remove(owner, this.postfixes);
		}

		/// <summary>Adds transpilers</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002E1 RID: 737 RVA: 0x00010A0F File Offset: 0x0000EC0F
		internal void AddTranspilers(string owner, params HarmonyMethod[] methods)
		{
			this.transpilers = PatchInfo.Add(owner, methods, this.transpilers);
		}

		/// <summary>Adds a transpiler</summary>
		// Token: 0x060002E2 RID: 738 RVA: 0x00010A24 File Offset: 0x0000EC24
		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddTranspiler(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddTranspilers(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		/// <summary>Removes transpilers</summary>
		/// <summary>Removes transpilers</summary>
		/// <param name="owner">The owner of the transpilers, or <c>*</c> for all</param>
		// Token: 0x060002E3 RID: 739 RVA: 0x00010A53 File Offset: 0x0000EC53
		public void RemoveTranspiler(string owner)
		{
			this.transpilers = PatchInfo.Remove(owner, this.transpilers);
		}

		/// <summary>Adds finalizers</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002E4 RID: 740 RVA: 0x00010A67 File Offset: 0x0000EC67
		internal void AddFinalizers(string owner, params HarmonyMethod[] methods)
		{
			this.finalizers = PatchInfo.Add(owner, methods, this.finalizers);
		}

		/// <summary>Adds a finalizer</summary>
		// Token: 0x060002E5 RID: 741 RVA: 0x00010A7C File Offset: 0x0000EC7C
		[Obsolete("This method only exists for backwards compatibility since the class is public.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void AddFinalizer(MethodInfo patch, string owner, int priority, string[] before, string[] after, bool debug)
		{
			this.AddFinalizers(owner, new HarmonyMethod[]
			{
				new HarmonyMethod(patch, priority, before, after, new bool?(debug))
			});
		}

		/// <summary>Removes finalizers</summary>
		/// <param name="owner">The owner of the finalizers, or <c>*</c> for all</param>
		// Token: 0x060002E6 RID: 742 RVA: 0x00010AAB File Offset: 0x0000ECAB
		public void RemoveFinalizer(string owner)
		{
			this.finalizers = PatchInfo.Remove(owner, this.finalizers);
		}

		/// <summary>Adds inner prefixes</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002E7 RID: 743 RVA: 0x00010ABF File Offset: 0x0000ECBF
		internal void AddInnerPrefixes(string owner, params HarmonyMethod[] methods)
		{
			this.innerprefixes = PatchInfo.Add(owner, methods, this.innerprefixes);
		}

		/// <summary>Removes inner prefixes</summary>
		/// <param name="owner">The owner of the inner prefixes, or <c>*</c> for all</param>
		// Token: 0x060002E8 RID: 744 RVA: 0x00010AD4 File Offset: 0x0000ECD4
		public void RemoveInnerPrefix(string owner)
		{
			this.innerprefixes = PatchInfo.Remove(owner, this.innerprefixes);
		}

		/// <summary>Adds inner postfixes</summary>
		/// <param name="owner">An owner (Harmony ID)</param>
		/// <param name="methods">The patch methods</param>
		// Token: 0x060002E9 RID: 745 RVA: 0x00010AE8 File Offset: 0x0000ECE8
		internal void AddInnerPostfixes(string owner, params HarmonyMethod[] methods)
		{
			this.innerpostfixes = PatchInfo.Add(owner, methods, this.innerpostfixes);
		}

		/// <summary>Removes inner postfixes</summary>
		/// <param name="owner">The owner of the inner postfixes, or <c>*</c> for all</param>
		// Token: 0x060002EA RID: 746 RVA: 0x00010AFD File Offset: 0x0000ECFD
		public void RemoveInnerPostfix(string owner)
		{
			this.innerpostfixes = PatchInfo.Remove(owner, this.innerpostfixes);
		}

		/// <summary>Removes a patch using its method</summary>
		/// <param name="patch">The method of the patch to remove</param>
		// Token: 0x060002EB RID: 747 RVA: 0x00010B14 File Offset: 0x0000ED14
		public void RemovePatch(MethodInfo patch)
		{
			this.prefixes = (from p in this.prefixes
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
			this.postfixes = (from p in this.postfixes
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
			this.transpilers = (from p in this.transpilers
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
			this.finalizers = (from p in this.finalizers
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
			this.innerprefixes = (from p in this.innerprefixes
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
			this.innerpostfixes = (from p in this.innerpostfixes
				where p.PatchMethod != patch
				select p).ToArray<Patch>();
		}

		// Token: 0x060002EC RID: 748 RVA: 0x00010BFC File Offset: 0x0000EDFC
		private static Patch[] Add(string owner, HarmonyMethod[] add, Patch[] current)
		{
			if (add.Length == 0)
			{
				return current;
			}
			int initialIndex = current.Length;
			List<Patch> list = new List<Patch>();
			list.AddRange(current);
			list.AddRange((from method in add
				where method != null
				select method).Select((HarmonyMethod method, int i) => new Patch(method, i + initialIndex, owner)));
			return list.ToArray();
		}

		// Token: 0x060002ED RID: 749 RVA: 0x00010C74 File Offset: 0x0000EE74
		private static Patch[] Remove(string owner, Patch[] current)
		{
			if (!(owner == "*"))
			{
				return (from patch in current
					where patch.owner != owner
					select patch).ToArray<Patch>();
			}
			return Array.Empty<Patch>();
		}

		/// <summary>Prefixes as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F0 RID: 496
		public Patch[] prefixes = Array.Empty<Patch>();

		/// <summary>Postfixes as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F1 RID: 497
		public Patch[] postfixes = Array.Empty<Patch>();

		/// <summary>Transpilers as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F2 RID: 498
		public Patch[] transpilers = Array.Empty<Patch>();

		/// <summary>Finalizers as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F3 RID: 499
		public Patch[] finalizers = Array.Empty<Patch>();

		/// <summary>InnerPrefixes as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F4 RID: 500
		public Patch[] innerprefixes = Array.Empty<Patch>();

		/// <summary>InnerPostfixes as an array of <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001F5 RID: 501
		public Patch[] innerpostfixes = Array.Empty<Patch>();

		/// <summary>Number of replacements created</summary>
		// Token: 0x040001F6 RID: 502
		public int VersionCount;
	}
}
