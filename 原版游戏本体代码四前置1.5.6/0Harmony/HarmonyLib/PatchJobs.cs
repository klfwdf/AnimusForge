using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

internal class PatchJobs<T>
{
	internal class Job
	{
		internal MethodBase original;

		internal T replacement;

		internal List<HarmonyMethod> prefixes = new List<HarmonyMethod>();

		internal List<HarmonyMethod> postfixes = new List<HarmonyMethod>();

		internal List<HarmonyMethod> transpilers = new List<HarmonyMethod>();

		internal List<HarmonyMethod> finalizers = new List<HarmonyMethod>();

		internal List<HarmonyMethod> innerprefixes = new List<HarmonyMethod>();

		internal List<HarmonyMethod> innerpostfixes = new List<HarmonyMethod>();

		internal void AddPatch(AttributePatch patch)
		{
			HarmonyPatchType? type = patch.type;
			if (type.HasValue)
			{
				switch (type.GetValueOrDefault())
				{
				case HarmonyPatchType.Prefix:
					prefixes.Add(patch.info);
					break;
				case HarmonyPatchType.Postfix:
					postfixes.Add(patch.info);
					break;
				case HarmonyPatchType.Transpiler:
					transpilers.Add(patch.info);
					break;
				case HarmonyPatchType.Finalizer:
					finalizers.Add(patch.info);
					break;
				case HarmonyPatchType.InnerPrefix:
					innerprefixes.Add(patch.info);
					break;
				case HarmonyPatchType.InnerPostfix:
					innerpostfixes.Add(patch.info);
					break;
				case HarmonyPatchType.ReversePatch:
					break;
				}
			}
		}
	}

	internal Dictionary<MethodBase, Job> state = new Dictionary<MethodBase, Job>();

	internal Job GetJob(MethodBase method)
	{
		if ((object)method == null)
		{
			return null;
		}
		if (!state.TryGetValue(method, out var value))
		{
			value = new Job
			{
				original = method
			};
			state[method] = value;
		}
		return value;
	}

	internal List<Job> GetJobs()
	{
		return state.Values.Where((Job job) => job.prefixes.Count + job.postfixes.Count + job.transpilers.Count + job.finalizers.Count + job.innerprefixes.Count + job.innerpostfixes.Count > 0).ToList();
	}

	internal List<T> GetReplacements()
	{
		return state.Values.Select((Job job) => job.replacement).ToList();
	}
}
