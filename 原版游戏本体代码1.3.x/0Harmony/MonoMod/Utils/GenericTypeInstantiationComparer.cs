using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008C7 RID: 2247
	[NullableContext(2)]
	[Nullable(0)]
	internal class GenericTypeInstantiationComparer : IEqualityComparer<Type>
	{
		// Token: 0x06002E96 RID: 11926 RVA: 0x000A066C File Offset: 0x0009E86C
		public bool Equals(Type x, Type y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			bool xGeneric = x.IsGenericType;
			bool yGeneric = y.IsGenericType;
			if (xGeneric != yGeneric)
			{
				return false;
			}
			if (!xGeneric)
			{
				return x.Equals(y);
			}
			Type genericTypeDefinition = x.GetGenericTypeDefinition();
			Type yDef = y.GetGenericTypeDefinition();
			if (!genericTypeDefinition.Equals(yDef))
			{
				return false;
			}
			Type[] xGenArgs = x.GetGenericArguments();
			Type[] yGenArgs = y.GetGenericArguments();
			if (xGenArgs.Length != yGenArgs.Length)
			{
				return false;
			}
			for (int i = 0; i < xGenArgs.Length; i++)
			{
				Type xArg = xGenArgs[i];
				Type yArg = yGenArgs[i];
				if (!xArg.IsValueType)
				{
					xArg = GenericTypeInstantiationComparer.CannonicalFillType ?? typeof(object);
				}
				if (!yArg.IsValueType)
				{
					yArg = GenericTypeInstantiationComparer.CannonicalFillType ?? typeof(object);
				}
				if (!this.Equals(xArg, yArg))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002E97 RID: 11927 RVA: 0x000A0744 File Offset: 0x0009E944
		[NullableContext(1)]
		public int GetHashCode(Type obj)
		{
			Helpers.ThrowIfArgumentNull<Type>(obj, "obj");
			if (!obj.IsGenericType)
			{
				return obj.GetHashCode();
			}
			int code = -559038737;
			code ^= obj.Assembly.GetHashCode();
			code ^= (code << 16) | (code >> 16);
			if (obj.Namespace != null)
			{
				code ^= obj.Namespace.GetHashCode(StringComparison.Ordinal);
			}
			code ^= obj.Name.GetHashCode(StringComparison.Ordinal);
			Type[] genericParams = obj.GetGenericArguments();
			for (int i = 0; i < genericParams.Length; i++)
			{
				int offs = i % 8 * 4;
				Type param = genericParams[i];
				int num;
				if (!param.IsValueType)
				{
					Type cannonicalFillType = GenericTypeInstantiationComparer.CannonicalFillType;
					num = ((cannonicalFillType != null) ? cannonicalFillType.GetHashCode() : (-1717986919));
				}
				else
				{
					num = this.GetHashCode(param);
				}
				int typeCode = num;
				code ^= (typeCode << offs) | (typeCode >> 32 - offs);
			}
			return code;
		}

		// Token: 0x04003B3E RID: 15166
		private static Type CannonicalFillType = GenericMethodInstantiationComparer.CannonicalFillType;
	}
}
