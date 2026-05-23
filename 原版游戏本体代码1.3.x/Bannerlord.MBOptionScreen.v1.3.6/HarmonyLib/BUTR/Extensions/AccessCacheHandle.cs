using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000067 RID: 103
	[ExcludeFromCodeCoverage]
	internal readonly struct AccessCacheHandle
	{
		// Token: 0x060003CF RID: 975 RVA: 0x0000E258 File Offset: 0x0000C458
		public static AccessCacheHandle? Create()
		{
			AccessCacheHandle.AccessCacheCtorDelegate accessCacheCtorMethod = AccessCacheHandle.AccessCacheCtorMethod;
			object accessCache = ((accessCacheCtorMethod != null) ? accessCacheCtorMethod() : null);
			if (accessCache == null)
			{
				return null;
			}
			return new AccessCacheHandle?(new AccessCacheHandle(accessCache));
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0000E28F File Offset: 0x0000C48F
		[NullableContext(1)]
		private AccessCacheHandle(object accessCache)
		{
			this._accessCache = accessCache;
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000E298 File Offset: 0x0000C498
		[NullableContext(1)]
		[return: Nullable(2)]
		public FieldInfo GetFieldInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetFieldInfoDelegate getFieldInfoMethod = AccessCacheHandle.GetFieldInfoMethod;
			if (getFieldInfoMethod == null)
			{
				return null;
			}
			return getFieldInfoMethod(this._accessCache, type, name, memberType, declaredOnly);
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0000E2B5 File Offset: 0x0000C4B5
		[NullableContext(1)]
		[return: Nullable(2)]
		public PropertyInfo GetPropertyInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetPropertyInfoDelegate getPropertyInfoMethod = AccessCacheHandle.GetPropertyInfoMethod;
			if (getPropertyInfoMethod == null)
			{
				return null;
			}
			return getPropertyInfoMethod(this._accessCache, type, name, memberType, declaredOnly);
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x0000E2D2 File Offset: 0x0000C4D2
		[NullableContext(1)]
		[return: Nullable(2)]
		public MethodBase GetMethodInfo(Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetMethodInfoDelegate getMethodInfoMethod = AccessCacheHandle.GetMethodInfoMethod;
			if (getMethodInfoMethod == null)
			{
				return null;
			}
			return getMethodInfoMethod(this._accessCache, type, name, arguments, memberType, declaredOnly);
		}

		// Token: 0x0400014F RID: 335
		[Nullable(1)]
		private static readonly Type Blank = typeof(Harmony);

		// Token: 0x04000150 RID: 336
		[Nullable(2)]
		private static readonly AccessCacheHandle.AccessCacheCtorDelegate AccessCacheCtorMethod = AccessTools2.GetDeclaredConstructorDelegate<AccessCacheHandle.AccessCacheCtorDelegate>("HarmonyLib.AccessCache", null, true);

		// Token: 0x04000151 RID: 337
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetFieldInfoDelegate GetFieldInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetFieldInfoDelegate>("HarmonyLib.AccessCache:GetFieldInfo", null, null, true);

		// Token: 0x04000152 RID: 338
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetPropertyInfoDelegate GetPropertyInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetPropertyInfoDelegate>("HarmonyLib.AccessCache:GetPropertyInfo", null, null, true);

		// Token: 0x04000153 RID: 339
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetMethodInfoDelegate GetMethodInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetMethodInfoDelegate>("HarmonyLib.AccessCache:GetMethodInfo", null, null, true);

		// Token: 0x04000154 RID: 340
		[Nullable(1)]
		private readonly object _accessCache;

		// Token: 0x020000EA RID: 234
		internal enum MemberType
		{
			// Token: 0x040002C4 RID: 708
			Any,
			// Token: 0x040002C5 RID: 709
			Static,
			// Token: 0x040002C6 RID: 710
			Instance
		}

		// Token: 0x020000EB RID: 235
		// (Invoke) Token: 0x06000687 RID: 1671
		private delegate object AccessCacheCtorDelegate();

		// Token: 0x020000EC RID: 236
		// (Invoke) Token: 0x0600068B RID: 1675
		private delegate FieldInfo GetFieldInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x020000ED RID: 237
		// (Invoke) Token: 0x0600068F RID: 1679
		private delegate PropertyInfo GetPropertyInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x020000EE RID: 238
		// (Invoke) Token: 0x06000693 RID: 1683
		private delegate MethodBase GetMethodInfoDelegate(object instance, Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);
	}
}
