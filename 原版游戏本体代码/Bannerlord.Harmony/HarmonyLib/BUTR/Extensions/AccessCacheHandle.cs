using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x0200003D RID: 61
	[ExcludeFromCodeCoverage]
	internal readonly struct AccessCacheHandle
	{
		// Token: 0x0600033D RID: 829 RVA: 0x0000B8FC File Offset: 0x00009AFC
		public static AccessCacheHandle? Create()
		{
			AccessCacheHandle.AccessCacheCtorDelegate accessCacheCtorMethod = AccessCacheHandle.AccessCacheCtorMethod;
			object accessCache = ((accessCacheCtorMethod != null) ? accessCacheCtorMethod() : null);
			return (accessCache != null) ? new AccessCacheHandle?(new AccessCacheHandle(accessCache)) : null;
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0000B939 File Offset: 0x00009B39
		[NullableContext(1)]
		private AccessCacheHandle(object accessCache)
		{
			this._accessCache = accessCache;
		}

		// Token: 0x0600033F RID: 831 RVA: 0x0000B942 File Offset: 0x00009B42
		[NullableContext(1)]
		[return: Nullable(2)]
		public FieldInfo GetFieldInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetFieldInfoDelegate getFieldInfoMethod = AccessCacheHandle.GetFieldInfoMethod;
			return (getFieldInfoMethod != null) ? getFieldInfoMethod(this._accessCache, type, name, memberType, declaredOnly) : null;
		}

		// Token: 0x06000340 RID: 832 RVA: 0x0000B960 File Offset: 0x00009B60
		[NullableContext(1)]
		[return: Nullable(2)]
		public PropertyInfo GetPropertyInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetPropertyInfoDelegate getPropertyInfoMethod = AccessCacheHandle.GetPropertyInfoMethod;
			return (getPropertyInfoMethod != null) ? getPropertyInfoMethod(this._accessCache, type, name, memberType, declaredOnly) : null;
		}

		// Token: 0x06000341 RID: 833 RVA: 0x0000B97E File Offset: 0x00009B7E
		[NullableContext(1)]
		[return: Nullable(2)]
		public MethodBase GetMethodInfo(Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetMethodInfoDelegate getMethodInfoMethod = AccessCacheHandle.GetMethodInfoMethod;
			return (getMethodInfoMethod != null) ? getMethodInfoMethod(this._accessCache, type, name, arguments, memberType, declaredOnly) : null;
		}

		// Token: 0x04000095 RID: 149
		[Nullable(1)]
		private static readonly Type Blank = typeof(Harmony);

		// Token: 0x04000096 RID: 150
		[Nullable(2)]
		private static readonly AccessCacheHandle.AccessCacheCtorDelegate AccessCacheCtorMethod = AccessTools2.GetDeclaredConstructorDelegate<AccessCacheHandle.AccessCacheCtorDelegate>("HarmonyLib.AccessCache", null, true);

		// Token: 0x04000097 RID: 151
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetFieldInfoDelegate GetFieldInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetFieldInfoDelegate>("HarmonyLib.AccessCache:GetFieldInfo", null, null, true);

		// Token: 0x04000098 RID: 152
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetPropertyInfoDelegate GetPropertyInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetPropertyInfoDelegate>("HarmonyLib.AccessCache:GetPropertyInfo", null, null, true);

		// Token: 0x04000099 RID: 153
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetMethodInfoDelegate GetMethodInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetMethodInfoDelegate>("HarmonyLib.AccessCache:GetMethodInfo", null, null, true);

		// Token: 0x0400009A RID: 154
		[Nullable(1)]
		private readonly object _accessCache;

		// Token: 0x02000089 RID: 137
		internal enum MemberType
		{
			// Token: 0x04000217 RID: 535
			Any,
			// Token: 0x04000218 RID: 536
			Static,
			// Token: 0x04000219 RID: 537
			Instance
		}

		// Token: 0x0200008A RID: 138
		// (Invoke) Token: 0x06000576 RID: 1398
		private delegate object AccessCacheCtorDelegate();

		// Token: 0x0200008B RID: 139
		// (Invoke) Token: 0x0600057A RID: 1402
		private delegate FieldInfo GetFieldInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x0200008C RID: 140
		// (Invoke) Token: 0x0600057E RID: 1406
		private delegate PropertyInfo GetPropertyInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x0200008D RID: 141
		// (Invoke) Token: 0x06000582 RID: 1410
		private delegate MethodBase GetMethodInfoDelegate(object instance, Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);
	}
}
