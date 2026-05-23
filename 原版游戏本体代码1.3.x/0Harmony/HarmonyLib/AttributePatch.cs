using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x02000050 RID: 80
	internal class AttributePatch
	{
		// Token: 0x06000197 RID: 407 RVA: 0x0000C158 File Offset: 0x0000A358
		internal static AttributePatch Create(MethodInfo patch)
		{
			if (patch == null)
			{
				throw new NullReferenceException("Patch method cannot be null");
			}
			object[] allAttributes = patch.GetCustomAttributes(true);
			string methodName = patch.Name;
			HarmonyPatchType? type = AttributePatch.GetPatchType(methodName, allAttributes);
			if (type == null)
			{
				return null;
			}
			if (type.GetValueOrDefault() != HarmonyPatchType.ReversePatch && !patch.IsStatic)
			{
				throw new ArgumentException("Patch method " + patch.FullDescription() + " must be static");
			}
			IEnumerable<object> source = (from attr in allAttributes
				where attr.GetType().BaseType.FullName == PatchTools.harmonyAttributeFullName
				select attr).Select(delegate(object attr)
			{
				FieldInfo f_info = AccessTools.Field(attr.GetType(), "info");
				return f_info.GetValue(attr);
			});
			Func<object, HarmonyMethod> selector;
			if ((selector = AttributePatch.<>O.<0>__MakeDeepCopy) == null)
			{
				selector = (AttributePatch.<>O.<0>__MakeDeepCopy = new Func<object, HarmonyMethod>(AccessTools.MakeDeepCopy<HarmonyMethod>));
			}
			List<HarmonyMethod> list = source.Select(selector).ToList<HarmonyMethod>();
			HarmonyMethod info = HarmonyMethod.Merge(list);
			info.method = patch;
			return new AttributePatch
			{
				info = info,
				type = type
			};
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0000C258 File Offset: 0x0000A458
		private static HarmonyPatchType? GetPatchType(string methodName, object[] allAttributes)
		{
			HashSet<string> harmonyAttributes = new HashSet<string>(from attr in allAttributes
				select attr.GetType().FullName into name
				where name.StartsWith("Harmony")
				select name);
			HarmonyPatchType? type = null;
			foreach (HarmonyPatchType patchType in AttributePatch.allPatchTypes)
			{
				string name2 = patchType.ToString();
				if (name2 == methodName || harmonyAttributes.Contains("HarmonyLib.Harmony" + name2))
				{
					type = new HarmonyPatchType?(patchType);
					break;
				}
			}
			return type;
		}

		// Token: 0x04000119 RID: 281
		private static readonly HarmonyPatchType[] allPatchTypes = new HarmonyPatchType[]
		{
			HarmonyPatchType.Prefix,
			HarmonyPatchType.Postfix,
			HarmonyPatchType.Transpiler,
			HarmonyPatchType.Finalizer,
			HarmonyPatchType.ReversePatch,
			HarmonyPatchType.InnerPrefix,
			HarmonyPatchType.InnerPostfix
		};

		// Token: 0x0400011A RID: 282
		internal HarmonyMethod info;

		// Token: 0x0400011B RID: 283
		internal HarmonyPatchType? type;

		// Token: 0x02000051 RID: 81
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x0400011C RID: 284
			public static Func<object, HarmonyMethod> <0>__MakeDeepCopy;
		}
	}
}
