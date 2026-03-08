using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000020 RID: 32
	public static class TypeExtensions
	{
		// Token: 0x0600015E RID: 350 RVA: 0x000095A4 File Offset: 0x000077A4
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
