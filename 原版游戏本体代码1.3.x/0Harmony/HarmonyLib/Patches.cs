using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HarmonyLib
{
	/// <summary>A group of patches</summary>
	// Token: 0x02000094 RID: 148
	public class Patches
	{
		/// <summary>Gets all owners (Harmony IDs) or all known patches</summary>
		/// <value>The patch owners</value>
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060002D0 RID: 720 RVA: 0x00010630 File Offset: 0x0000E830
		public ReadOnlyCollection<string> Owners
		{
			get
			{
				HashSet<string> result = new HashSet<string>();
				result.UnionWith(from p in this.Prefixes
					select p.owner);
				result.UnionWith(from p in this.Postfixes
					select p.owner);
				result.UnionWith(from p in this.Transpilers
					select p.owner);
				result.UnionWith(from p in this.Finalizers
					select p.owner);
				result.UnionWith(from p in this.InnerPrefixes
					select p.owner);
				result.UnionWith(from p in this.InnerPostfixes
					select p.owner);
				return result.ToList<string>().AsReadOnly();
			}
		}

		/// <summary>Creates a group of patches</summary>
		/// <param name="prefixes">An array of prefixes as <see cref="T:HarmonyLib.Patch" /></param>
		/// <param name="postfixes">An array of postfixes as <see cref="T:HarmonyLib.Patch" /></param>
		/// <param name="transpilers">An array of transpileres as <see cref="T:HarmonyLib.Patch" /></param>
		/// <param name="finalizers">An array of finalizeres as <see cref="T:HarmonyLib.Patch" /></param>
		/// <param name="innerprefixes">An array of inner prefixes as <see cref="T:HarmonyLib.Patch" /></param>
		/// <param name="innerpostfixes">An array of inner postfixes as <see cref="T:HarmonyLib.Patch" /></param>
		// Token: 0x060002D1 RID: 721 RVA: 0x00010770 File Offset: 0x0000E970
		public Patches(Patch[] prefixes, Patch[] postfixes, Patch[] transpilers, Patch[] finalizers, Patch[] innerprefixes, Patch[] innerpostfixes)
		{
			if (prefixes == null)
			{
				prefixes = Array.Empty<Patch>();
			}
			if (postfixes == null)
			{
				postfixes = Array.Empty<Patch>();
			}
			if (transpilers == null)
			{
				transpilers = Array.Empty<Patch>();
			}
			if (finalizers == null)
			{
				finalizers = Array.Empty<Patch>();
			}
			if (innerprefixes == null)
			{
				innerprefixes = Array.Empty<Patch>();
			}
			if (innerpostfixes == null)
			{
				innerpostfixes = Array.Empty<Patch>();
			}
			this.Prefixes = prefixes.ToList<Patch>().AsReadOnly();
			this.Postfixes = postfixes.ToList<Patch>().AsReadOnly();
			this.Transpilers = transpilers.ToList<Patch>().AsReadOnly();
			this.Finalizers = finalizers.ToList<Patch>().AsReadOnly();
			this.InnerPrefixes = innerprefixes.ToList<Patch>().AsReadOnly();
			this.InnerPostfixes = innerpostfixes.ToList<Patch>().AsReadOnly();
		}

		/// <summary>A collection of prefix <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E3 RID: 483
		public readonly ReadOnlyCollection<Patch> Prefixes;

		/// <summary>A collection of postfix <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E4 RID: 484
		public readonly ReadOnlyCollection<Patch> Postfixes;

		/// <summary>A collection of transpiler <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E5 RID: 485
		public readonly ReadOnlyCollection<Patch> Transpilers;

		/// <summary>A collection of finalizer <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E6 RID: 486
		public readonly ReadOnlyCollection<Patch> Finalizers;

		/// <summary>A collection of inner prefix <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E7 RID: 487
		public readonly ReadOnlyCollection<Patch> InnerPrefixes;

		/// <summary>A collection of inner postfix <see cref="T:HarmonyLib.Patch" /></summary>
		// Token: 0x040001E8 RID: 488
		public readonly ReadOnlyCollection<Patch> InnerPostfixes;
	}
}
