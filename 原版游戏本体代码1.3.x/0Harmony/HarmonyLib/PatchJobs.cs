using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x0200004D RID: 77
	internal class PatchJobs<T>
	{
		// Token: 0x0600018D RID: 397 RVA: 0x0000BF20 File Offset: 0x0000A120
		internal PatchJobs<T>.Job GetJob(MethodBase method)
		{
			if (method == null)
			{
				return null;
			}
			PatchJobs<T>.Job job;
			if (!this.state.TryGetValue(method, out job))
			{
				job = new PatchJobs<T>.Job
				{
					original = method
				};
				this.state[method] = job;
			}
			return job;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x0000BF5D File Offset: 0x0000A15D
		internal List<PatchJobs<T>.Job> GetJobs()
		{
			return (from job in this.state.Values
				where job.prefixes.Count + job.postfixes.Count + job.transpilers.Count + job.finalizers.Count + job.innerprefixes.Count + job.innerpostfixes.Count > 0
				select job).ToList<PatchJobs<T>.Job>();
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000BF93 File Offset: 0x0000A193
		internal List<T> GetReplacements()
		{
			return (from job in this.state.Values
				select job.replacement).ToList<T>();
		}

		// Token: 0x0400010D RID: 269
		internal Dictionary<MethodBase, PatchJobs<T>.Job> state = new Dictionary<MethodBase, PatchJobs<T>.Job>();

		// Token: 0x0200004E RID: 78
		internal class Job
		{
			// Token: 0x06000191 RID: 401 RVA: 0x0000BFDC File Offset: 0x0000A1DC
			internal void AddPatch(AttributePatch patch)
			{
				HarmonyPatchType? type = patch.type;
				if (type != null)
				{
					switch (type.GetValueOrDefault())
					{
					case HarmonyPatchType.Prefix:
						this.prefixes.Add(patch.info);
						return;
					case HarmonyPatchType.Postfix:
						this.postfixes.Add(patch.info);
						return;
					case HarmonyPatchType.Transpiler:
						this.transpilers.Add(patch.info);
						return;
					case HarmonyPatchType.Finalizer:
						this.finalizers.Add(patch.info);
						return;
					case HarmonyPatchType.ReversePatch:
						break;
					case HarmonyPatchType.InnerPrefix:
						this.innerprefixes.Add(patch.info);
						return;
					case HarmonyPatchType.InnerPostfix:
						this.innerpostfixes.Add(patch.info);
						break;
					default:
						return;
					}
				}
			}

			// Token: 0x0400010E RID: 270
			internal MethodBase original;

			// Token: 0x0400010F RID: 271
			internal T replacement;

			// Token: 0x04000110 RID: 272
			internal List<HarmonyMethod> prefixes = new List<HarmonyMethod>();

			// Token: 0x04000111 RID: 273
			internal List<HarmonyMethod> postfixes = new List<HarmonyMethod>();

			// Token: 0x04000112 RID: 274
			internal List<HarmonyMethod> transpilers = new List<HarmonyMethod>();

			// Token: 0x04000113 RID: 275
			internal List<HarmonyMethod> finalizers = new List<HarmonyMethod>();

			// Token: 0x04000114 RID: 276
			internal List<HarmonyMethod> innerprefixes = new List<HarmonyMethod>();

			// Token: 0x04000115 RID: 277
			internal List<HarmonyMethod> innerpostfixes = new List<HarmonyMethod>();
		}
	}
}
