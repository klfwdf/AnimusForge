using System;
using System.Reflection;
using System.Security;
using System.StubHelpers;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A0C RID: 2572
	internal static class ICustomPropertyProviderImpl
	{
		// Token: 0x06006584 RID: 25988 RVA: 0x00159590 File Offset: 0x00157790
		internal static ICustomProperty CreateProperty(object target, string propertyName)
		{
			PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (property == null)
			{
				return null;
			}
			return new CustomPropertyImpl(property);
		}

		// Token: 0x06006585 RID: 25989 RVA: 0x001595C0 File Offset: 0x001577C0
		[SecurityCritical]
		internal unsafe static ICustomProperty CreateIndexedProperty(object target, string propertyName, TypeNameNative* pIndexedParamType)
		{
			Type indexedParamType = null;
			SystemTypeMarshaler.ConvertToManaged(pIndexedParamType, ref indexedParamType);
			return ICustomPropertyProviderImpl.CreateIndexedProperty(target, propertyName, indexedParamType);
		}

		// Token: 0x06006586 RID: 25990 RVA: 0x001595E0 File Offset: 0x001577E0
		internal static ICustomProperty CreateIndexedProperty(object target, string propertyName, Type indexedParamType)
		{
			PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, null, new Type[] { indexedParamType }, null);
			if (property == null)
			{
				return null;
			}
			return new CustomPropertyImpl(property);
		}

		// Token: 0x06006587 RID: 25991 RVA: 0x0015961A File Offset: 0x0015781A
		[SecurityCritical]
		internal unsafe static void GetType(object target, TypeNameNative* pIndexedParamType)
		{
			SystemTypeMarshaler.ConvertToNative(target.GetType(), pIndexedParamType);
		}
	}
}
