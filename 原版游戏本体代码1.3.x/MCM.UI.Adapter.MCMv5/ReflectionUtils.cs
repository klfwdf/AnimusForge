using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MCM.UI.Adapter.MCMv5
{
	// Token: 0x02000007 RID: 7
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ReflectionUtils
	{
		// Token: 0x06000007 RID: 7 RVA: 0x000020AC File Offset: 0x000002AC
		public static bool ImplementsOrImplementsEquivalent(Type type, [Nullable(2)] string fullBaseTypeName, bool includeBase = true)
		{
			bool flag = fullBaseTypeName == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				for (Type typeToCheck = (includeBase ? type : type.BaseType); typeToCheck != null; typeToCheck = typeToCheck.BaseType)
				{
					string fullName = typeToCheck.FullName;
					bool flag2 = fullName != null && fullName.EndsWith(fullBaseTypeName, StringComparison.Ordinal);
					if (flag2)
					{
						return true;
					}
				}
				result = type.GetInterfaces().Any((Type x) => (includeBase || type != x) && string.Equals(x.FullName, fullBaseTypeName, StringComparison.Ordinal));
			}
			return result;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000215C File Offset: 0x0000035C
		public static bool ImplementsEquivalentInterface(Type type, [Nullable(2)] string fullBaseTypeName)
		{
			bool flag = fullBaseTypeName == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				Func<Type, bool> <>9__0;
				for (Type typeToCheck = type; typeToCheck != null; typeToCheck = typeToCheck.BaseType)
				{
					IEnumerable<Type> interfaces = type.GetInterfaces();
					Func<Type, bool> predicate;
					if ((predicate = <>9__0) == null)
					{
						predicate = (<>9__0 = delegate(Type x)
						{
							string fullName = x.FullName;
							return fullName != null && fullName.EndsWith(fullBaseTypeName, StringComparison.Ordinal);
						});
					}
					bool flag2 = interfaces.Any(predicate);
					if (flag2)
					{
						return true;
					}
				}
				result = false;
			}
			return result;
		}
	}
}
