using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000482 RID: 1154
	internal static class TypeExtensions
	{
		// Token: 0x060019AF RID: 6575 RVA: 0x0005464C File Offset: 0x0005284C
		[NullableContext(1)]
		public static bool IsByRefLike(this Type type)
		{
			ThrowHelper.ThrowIfArgumentNull(type, ExceptionArgument.type);
			if (type == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.type);
			}
			object[] customAttributes = type.GetCustomAttributes(false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				if (customAttributes[i].GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
				{
					return true;
				}
			}
			return false;
		}
	}
}
