using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	/// <summary>A wrapper around a method to use it as a patch (for example a Prefix)</summary>
	// Token: 0x02000084 RID: 132
	public class HarmonyMethod
	{
		/// <summary>Default constructor</summary>
		// Token: 0x0600027B RID: 635 RVA: 0x0000ECBE File Offset: 0x0000CEBE
		public HarmonyMethod()
		{
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000ECD0 File Offset: 0x0000CED0
		private void ImportMethod(MethodInfo theMethod)
		{
			this.method = theMethod;
			if (this.method != null)
			{
				List<HarmonyMethod> infos = HarmonyMethodExtensions.GetFromMethod(this.method);
				if (infos != null)
				{
					HarmonyMethod.Merge(infos).CopyTo(this);
				}
			}
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="method">The original method</param>
		// Token: 0x0600027D RID: 637 RVA: 0x0000ED07 File Offset: 0x0000CF07
		public HarmonyMethod(MethodInfo method)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			this.ImportMethod(method);
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="delegate">The original method</param>
		// Token: 0x0600027E RID: 638 RVA: 0x0000ED2B File Offset: 0x0000CF2B
		public HarmonyMethod(Delegate @delegate)
			: this(@delegate.Method)
		{
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="method">The original method</param>
		/// <param name="priority">The patch <see cref="T:HarmonyLib.Priority" /></param>
		/// <param name="before">A list of harmony IDs that should come after this patch</param>
		/// <param name="after">A list of harmony IDs that should come before this patch</param>
		/// <param name="debug">Set to true to generate debug output</param>
		// Token: 0x0600027F RID: 639 RVA: 0x0000ED3C File Offset: 0x0000CF3C
		public HarmonyMethod(MethodInfo method, int priority = -1, string[] before = null, string[] after = null, bool? debug = null)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			this.ImportMethod(method);
			this.priority = priority;
			this.before = before;
			this.after = after;
			this.debug = debug;
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="delegate">The original method</param>
		/// <param name="priority">The patch <see cref="T:HarmonyLib.Priority" /></param>
		/// <param name="before">A list of harmony IDs that should come after this patch</param>
		/// <param name="after">A list of harmony IDs that should come before this patch</param>
		/// <param name="debug">Set to true to generate debug output</param>
		// Token: 0x06000280 RID: 640 RVA: 0x0000ED89 File Offset: 0x0000CF89
		public HarmonyMethod(Delegate @delegate, int priority = -1, string[] before = null, string[] after = null, bool? debug = null)
			: this(@delegate.Method, priority, before, after, debug)
		{
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="methodType">The patch class/type</param>
		/// <param name="methodName">The patch method name</param>
		/// <param name="argumentTypes">The optional argument types of the patch method (for overloaded methods)</param>
		// Token: 0x06000281 RID: 641 RVA: 0x0000EDA0 File Offset: 0x0000CFA0
		public HarmonyMethod(Type methodType, string methodName, Type[] argumentTypes = null)
		{
			MethodInfo result = AccessTools.Method(methodType, methodName, argumentTypes, null);
			if (result == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Cannot not find method for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(methodType);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(methodName);
				defaultInterpolatedStringHandler.AppendLiteral(" and parameters ");
				defaultInterpolatedStringHandler.AppendFormatted((argumentTypes != null) ? argumentTypes.Description() : null);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			this.ImportMethod(result);
		}

		/// <summary>Gets the names of all internal patch info fields</summary>
		/// <returns>A list of field names</returns>
		// Token: 0x06000282 RID: 642 RVA: 0x0000EE2C File Offset: 0x0000D02C
		public static List<string> HarmonyFields()
		{
			return (from s in AccessTools.GetFieldNames(typeof(HarmonyMethod))
				where s != "method"
				select s).ToList<string>();
		}

		/// <summary>Merges annotations</summary>
		/// <param name="attributes">The list of <see cref="T:HarmonyLib.HarmonyMethod" /> to merge</param>
		/// <returns>The merged <see cref="T:HarmonyLib.HarmonyMethod" /></returns>
		// Token: 0x06000283 RID: 643 RVA: 0x0000EE68 File Offset: 0x0000D068
		public static HarmonyMethod Merge(List<HarmonyMethod> attributes)
		{
			HarmonyMethod result = new HarmonyMethod();
			if (attributes == null || attributes.Count == 0)
			{
				return result;
			}
			Traverse resultTrv = Traverse.Create(result);
			attributes.ForEach(delegate(HarmonyMethod attribute)
			{
				Traverse trv = Traverse.Create(attribute);
				HarmonyMethod.HarmonyFields().ForEach(delegate(string f)
				{
					object val = trv.Field(f).GetValue();
					if (val != null && (f != "priority" || (int)val != -1))
					{
						HarmonyMethodExtensions.SetValue(resultTrv, f, val);
					}
				});
			});
			return result;
		}

		/// <summary>Returns a string that represents the annotation</summary>
		/// <returns>A string representation</returns>
		// Token: 0x06000284 RID: 644 RVA: 0x0000EEB0 File Offset: 0x0000D0B0
		public override string ToString()
		{
			string result = "";
			Traverse trv = Traverse.Create(this);
			HarmonyMethod.HarmonyFields().ForEach(delegate(string f)
			{
				string result;
				if (result.Length > 0)
				{
					result += ", ";
				}
				result = result;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
				defaultInterpolatedStringHandler.AppendFormatted(f);
				defaultInterpolatedStringHandler.AppendLiteral("=");
				defaultInterpolatedStringHandler.AppendFormatted<object>(trv.Field(f).GetValue());
				result += defaultInterpolatedStringHandler.ToStringAndClear();
			});
			return "HarmonyMethod[" + result + "]";
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0000EF08 File Offset: 0x0000D108
		internal string Description()
		{
			string cName = ((this.declaringType != null) ? this.declaringType.FullName : "undefined");
			string mName = this.methodName ?? "undefined";
			string tName = ((this.methodType != null) ? this.methodType.Value.ToString() : "undefined");
			string aName = ((this.argumentTypes != null) ? this.argumentTypes.Description() : "undefined");
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 4);
			defaultInterpolatedStringHandler.AppendLiteral("(class=");
			defaultInterpolatedStringHandler.AppendFormatted(cName);
			defaultInterpolatedStringHandler.AppendLiteral(", methodname=");
			defaultInterpolatedStringHandler.AppendFormatted(mName);
			defaultInterpolatedStringHandler.AppendLiteral(", type=");
			defaultInterpolatedStringHandler.AppendFormatted(tName);
			defaultInterpolatedStringHandler.AppendLiteral(", args=");
			defaultInterpolatedStringHandler.AppendFormatted(aName);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="method">The original method</param>
		// Token: 0x06000286 RID: 646 RVA: 0x0000EFF7 File Offset: 0x0000D1F7
		public static implicit operator HarmonyMethod(MethodInfo method)
		{
			return new HarmonyMethod(method);
		}

		/// <summary>Creates a patch from a given method</summary>
		/// <param name="delegate">The original method</param>
		// Token: 0x06000287 RID: 647 RVA: 0x0000EFFF File Offset: 0x0000D1FF
		public static implicit operator HarmonyMethod(Delegate @delegate)
		{
			return new HarmonyMethod(@delegate);
		}

		/// <summary>The original method</summary>
		// Token: 0x040001A6 RID: 422
		public MethodInfo method;

		/// <summary>Patch Category</summary>
		// Token: 0x040001A7 RID: 423
		public string category;

		/// <summary>Class/type declaring this patch</summary>
		// Token: 0x040001A8 RID: 424
		public Type declaringType;

		/// <summary>Patch method name</summary>
		// Token: 0x040001A9 RID: 425
		public string methodName;

		/// <summary>Optional patch <see cref="T:HarmonyLib.MethodType" /></summary>
		// Token: 0x040001AA RID: 426
		public MethodType? methodType;

		/// <summary>Array of argument types of the patch method</summary>
		// Token: 0x040001AB RID: 427
		public Type[] argumentTypes;

		/// <summary>
		///     <see cref="T:HarmonyLib.Priority" /> of the patch</summary>
		// Token: 0x040001AC RID: 428
		public int priority = -1;

		/// <summary>Install this patch before patches with these Harmony IDs</summary>
		// Token: 0x040001AD RID: 429
		public string[] before;

		/// <summary>Install this patch after patches with these Harmony IDs</summary>
		// Token: 0x040001AE RID: 430
		public string[] after;

		/// <summary>Reverse patch type, see <see cref="T:HarmonyLib.HarmonyReversePatchType" /></summary>
		// Token: 0x040001AF RID: 431
		public HarmonyReversePatchType? reversePatchType;

		/// <summary>Create debug output for this patch</summary>
		// Token: 0x040001B0 RID: 432
		public bool? debug;

		/// <summary>Whether to use <see cref="F:HarmonyLib.MethodDispatchType.Call" /> (<c>true</c>) or <see cref="F:HarmonyLib.MethodDispatchType.VirtualCall" /> (<c>false</c>) mechanics
		/// for <see cref="T:HarmonyLib.HarmonyDelegate" />-attributed delegate</summary>
		// Token: 0x040001B1 RID: 433
		public bool nonVirtualDelegate;
	}
}
