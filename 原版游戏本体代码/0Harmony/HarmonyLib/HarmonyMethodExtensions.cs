using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	/// <summary>Annotation extensions</summary>
	// Token: 0x02000089 RID: 137
	public static class HarmonyMethodExtensions
	{
		// Token: 0x06000291 RID: 657 RVA: 0x0000F128 File Offset: 0x0000D328
		internal static void SetValue(Traverse trv, string name, object val)
		{
			if (val == null)
			{
				return;
			}
			Traverse fld = trv.Field(name);
			if (name == "methodType" || name == "reversePatchType")
			{
				Type enumType = Nullable.GetUnderlyingType(fld.GetValueType());
				val = Enum.ToObject(enumType, (int)val);
			}
			fld.SetValue(val);
		}

		/// <summary>Copies annotation information</summary>
		/// <param name="from">The source <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		/// <param name="to">The destination <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		// Token: 0x06000292 RID: 658 RVA: 0x0000F180 File Offset: 0x0000D380
		public static void CopyTo(this HarmonyMethod from, HarmonyMethod to)
		{
			if (to == null)
			{
				return;
			}
			Traverse fromTrv = Traverse.Create(from);
			Traverse toTrv = Traverse.Create(to);
			HarmonyMethod.HarmonyFields().ForEach(delegate(string f)
			{
				object val = fromTrv.Field(f).GetValue();
				if (val != null)
				{
					HarmonyMethodExtensions.SetValue(toTrv, f, val);
				}
			});
		}

		/// <summary>Clones an annotation</summary>
		/// <param name="original">The <see cref="T:HarmonyLib.HarmonyMethod" /> to clone</param>
		/// <returns>A copied <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000293 RID: 659 RVA: 0x0000F1C8 File Offset: 0x0000D3C8
		public static HarmonyMethod Clone(this HarmonyMethod original)
		{
			HarmonyMethod result = new HarmonyMethod();
			original.CopyTo(result);
			return result;
		}

		/// <summary>Merges annotations</summary>
		/// <param name="master">The master <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		/// <param name="detail">The detail <see cref="T:HarmonyLib.HarmonyMethod" /></param>
		/// <returns>A new, merged <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000294 RID: 660 RVA: 0x0000F1E4 File Offset: 0x0000D3E4
		public static HarmonyMethod Merge(this HarmonyMethod master, HarmonyMethod detail)
		{
			if (detail == null)
			{
				return master;
			}
			HarmonyMethod result = new HarmonyMethod();
			Traverse resultTrv = Traverse.Create(result);
			Traverse masterTrv = Traverse.Create(master);
			Traverse detailTrv = Traverse.Create(detail);
			HarmonyMethod.HarmonyFields().ForEach(delegate(string f)
			{
				object baseValue = masterTrv.Field(f).GetValue();
				object detailValue = detailTrv.Field(f).GetValue();
				if (f != "priority")
				{
					HarmonyMethodExtensions.SetValue(resultTrv, f, detailValue ?? baseValue);
					return;
				}
				int baseInt = (int)baseValue;
				int detailInt = (int)detailValue;
				int priority = Math.Max(baseInt, detailInt);
				if (baseInt == -1 && detailInt != -1)
				{
					priority = detailInt;
				}
				if (baseInt != -1 && detailInt == -1)
				{
					priority = baseInt;
				}
				HarmonyMethodExtensions.SetValue(resultTrv, f, priority);
			});
			return result;
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000F240 File Offset: 0x0000D440
		private static HarmonyMethod GetHarmonyMethodInfo(object attribute)
		{
			FieldInfo f_info = attribute.GetType().GetField("info", AccessTools.all);
			if (f_info == null)
			{
				return null;
			}
			if (f_info.FieldType.FullName != PatchTools.harmonyMethodFullName)
			{
				return null;
			}
			object info = f_info.GetValue(attribute);
			return AccessTools.MakeDeepCopy<HarmonyMethod>(info);
		}

		/// <summary>Gets all annotations on a class/type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>A list of all <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000296 RID: 662 RVA: 0x0000F290 File Offset: 0x0000D490
		public static List<HarmonyMethod> GetFromType(Type type)
		{
			IEnumerable<object> customAttributes = type.GetCustomAttributes(true);
			Func<object, HarmonyMethod> selector;
			if ((selector = HarmonyMethodExtensions.<>O.<0>__GetHarmonyMethodInfo) == null)
			{
				selector = (HarmonyMethodExtensions.<>O.<0>__GetHarmonyMethodInfo = new Func<object, HarmonyMethod>(HarmonyMethodExtensions.GetHarmonyMethodInfo));
			}
			return (from info in customAttributes.Select(selector)
				where info != null
				select info).ToList<HarmonyMethod>();
		}

		/// <summary>Gets merged annotations on a class/type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>The merged <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000297 RID: 663 RVA: 0x0000F2ED File Offset: 0x0000D4ED
		public static HarmonyMethod GetMergedFromType(Type type)
		{
			return HarmonyMethod.Merge(HarmonyMethodExtensions.GetFromType(type));
		}

		/// <summary>Gets all annotations on a method</summary>
		/// <param name="method">The method/constructor</param>
		/// <returns>A list of <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000298 RID: 664 RVA: 0x0000F2FC File Offset: 0x0000D4FC
		public static List<HarmonyMethod> GetFromMethod(MethodBase method)
		{
			IEnumerable<object> customAttributes = method.GetCustomAttributes(true);
			Func<object, HarmonyMethod> selector;
			if ((selector = HarmonyMethodExtensions.<>O.<0>__GetHarmonyMethodInfo) == null)
			{
				selector = (HarmonyMethodExtensions.<>O.<0>__GetHarmonyMethodInfo = new Func<object, HarmonyMethod>(HarmonyMethodExtensions.GetHarmonyMethodInfo));
			}
			return (from info in customAttributes.Select(selector)
				where info != null
				select info).ToList<HarmonyMethod>();
		}

		/// <summary>Gets merged annotations on a method</summary>
		/// <param name="method">The method/constructor</param>
		/// <returns>The merged <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000299 RID: 665 RVA: 0x0000F359 File Offset: 0x0000D559
		public static HarmonyMethod GetMergedFromMethod(MethodBase method)
		{
			return HarmonyMethod.Merge(HarmonyMethodExtensions.GetFromMethod(method));
		}

		// Token: 0x0200008A RID: 138
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x040001B9 RID: 441
			public static Func<object, HarmonyMethod> <0>__GetHarmonyMethodInfo;
		}
	}
}
