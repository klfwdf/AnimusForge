using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000011 RID: 17
	[ExcludeFromCodeCoverage]
	internal readonly struct AccessCacheHandle
	{
		// Token: 0x0600002F RID: 47 RVA: 0x00002A3C File Offset: 0x00000C3C
		public static AccessCacheHandle? Create()
		{
			AccessCacheHandle.AccessCacheCtorDelegate accessCacheCtorMethod = AccessCacheHandle.AccessCacheCtorMethod;
			object accessCache = ((accessCacheCtorMethod != null) ? accessCacheCtorMethod() : null);
			return (accessCache != null) ? new AccessCacheHandle?(new AccessCacheHandle(accessCache)) : null;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002A79 File Offset: 0x00000C79
		[NullableContext(1)]
		private AccessCacheHandle(object accessCache)
		{
			this._accessCache = accessCache;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002A82 File Offset: 0x00000C82
		[NullableContext(1)]
		[return: Nullable(2)]
		public FieldInfo GetFieldInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetFieldInfoDelegate getFieldInfoMethod = AccessCacheHandle.GetFieldInfoMethod;
			return (getFieldInfoMethod != null) ? getFieldInfoMethod(this._accessCache, type, name, memberType, declaredOnly) : null;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002AA0 File Offset: 0x00000CA0
		[NullableContext(1)]
		[return: Nullable(2)]
		public PropertyInfo GetPropertyInfo(Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetPropertyInfoDelegate getPropertyInfoMethod = AccessCacheHandle.GetPropertyInfoMethod;
			return (getPropertyInfoMethod != null) ? getPropertyInfoMethod(this._accessCache, type, name, memberType, declaredOnly) : null;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002ABE File Offset: 0x00000CBE
		[NullableContext(1)]
		[return: Nullable(2)]
		public MethodBase GetMethodInfo(Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false)
		{
			AccessCacheHandle.GetMethodInfoDelegate getMethodInfoMethod = AccessCacheHandle.GetMethodInfoMethod;
			return (getMethodInfoMethod != null) ? getMethodInfoMethod(this._accessCache, type, name, arguments, memberType, declaredOnly) : null;
		}

		// Token: 0x0400000A RID: 10
		[Nullable(1)]
		private static readonly Type Blank = typeof(Harmony);

		// Token: 0x0400000B RID: 11
		[Nullable(2)]
		private static readonly AccessCacheHandle.AccessCacheCtorDelegate AccessCacheCtorMethod = AccessTools2.GetDeclaredConstructorDelegate<AccessCacheHandle.AccessCacheCtorDelegate>("HarmonyLib.AccessCache", null, true);

		// Token: 0x0400000C RID: 12
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetFieldInfoDelegate GetFieldInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetFieldInfoDelegate>("HarmonyLib.AccessCache:GetFieldInfo", null, null, true);

		// Token: 0x0400000D RID: 13
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetPropertyInfoDelegate GetPropertyInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetPropertyInfoDelegate>("HarmonyLib.AccessCache:GetPropertyInfo", null, null, true);

		// Token: 0x0400000E RID: 14
		[Nullable(2)]
		private static readonly AccessCacheHandle.GetMethodInfoDelegate GetMethodInfoMethod = AccessTools2.GetDelegateObjectInstance<AccessCacheHandle.GetMethodInfoDelegate>("HarmonyLib.AccessCache:GetMethodInfo", null, null, true);

		// Token: 0x0400000F RID: 15
		[Nullable(1)]
		private readonly object _accessCache;

		// Token: 0x0200002F RID: 47
		internal enum MemberType
		{
			// Token: 0x0400006D RID: 109
			Any,
			// Token: 0x0400006E RID: 110
			Static,
			// Token: 0x0400006F RID: 111
			Instance
		}

		// Token: 0x02000030 RID: 48
		// (Invoke) Token: 0x06000159 RID: 345
		private delegate object AccessCacheCtorDelegate();

		// Token: 0x02000031 RID: 49
		// (Invoke) Token: 0x0600015D RID: 349
		private delegate FieldInfo GetFieldInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x02000032 RID: 50
		// (Invoke) Token: 0x06000161 RID: 353
		private delegate PropertyInfo GetPropertyInfoDelegate(object instance, Type type, string name, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);

		// Token: 0x02000033 RID: 51
		// (Invoke) Token: 0x06000165 RID: 357
		private delegate MethodBase GetMethodInfoDelegate(object instance, Type type, string name, Type[] arguments, AccessCacheHandle.MemberType memberType = AccessCacheHandle.MemberType.Any, bool declaredOnly = false);
	}
}
