using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008C6 RID: 2246
	[NullableContext(1)]
	[Nullable(0)]
	internal class GenericMethodInstantiationComparer : IEqualityComparer<MethodBase>
	{
		// Token: 0x06002E91 RID: 11921 RVA: 0x000A02BB File Offset: 0x0009E4BB
		public GenericMethodInstantiationComparer()
			: this(new GenericTypeInstantiationComparer())
		{
		}

		// Token: 0x06002E92 RID: 11922 RVA: 0x000A02C8 File Offset: 0x0009E4C8
		public GenericMethodInstantiationComparer(IEqualityComparer<Type> typeComparer)
		{
			this.genericTypeComparer = typeComparer;
		}

		// Token: 0x06002E93 RID: 11923 RVA: 0x000A02D8 File Offset: 0x0009E4D8
		[NullableContext(2)]
		public bool Equals(MethodBase x, MethodBase y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			bool flag;
			if (!x.IsGenericMethod || x.ContainsGenericParameters)
			{
				Type declaringType = x.DeclaringType;
				flag = declaringType != null && declaringType.IsGenericType;
			}
			else
			{
				flag = true;
			}
			bool xGeneric = flag;
			bool flag2;
			if (!y.IsGenericMethod || y.ContainsGenericParameters)
			{
				Type declaringType2 = y.DeclaringType;
				flag2 = declaringType2 != null && declaringType2.IsGenericType;
			}
			else
			{
				flag2 = true;
			}
			bool yGeneric = flag2;
			if (xGeneric != yGeneric)
			{
				return false;
			}
			if (!xGeneric)
			{
				return x.Equals(y);
			}
			if (!this.genericTypeComparer.Equals(x.DeclaringType, y.DeclaringType))
			{
				return false;
			}
			MethodInfo xi = x as MethodInfo;
			MethodBase xDef;
			if (xi != null)
			{
				xDef = xi.GetActualGenericMethodDefinition();
			}
			else
			{
				xDef = x.GetUnfilledMethodOnGenericType();
			}
			MethodInfo yi = y as MethodInfo;
			MethodBase yDef;
			if (yi != null)
			{
				yDef = yi.GetActualGenericMethodDefinition();
			}
			else
			{
				yDef = y.GetUnfilledMethodOnGenericType();
			}
			if (!xDef.Equals(yDef))
			{
				return false;
			}
			if (xDef.Name != yDef.Name)
			{
				return false;
			}
			ParameterInfo[] xParams = x.GetParameters();
			ParameterInfo[] yParams = y.GetParameters();
			if (xParams.Length != yParams.Length)
			{
				return false;
			}
			ParameterInfo[] xDefParams = xDef.GetParameters();
			for (int i = 0; i < xParams.Length; i++)
			{
				Type xType = xParams[i].ParameterType;
				Type yType = yParams[i].ParameterType;
				if (xDefParams[i].ParameterType.IsGenericParameter)
				{
					if (!xType.IsValueType)
					{
						xType = GenericMethodInstantiationComparer.CannonicalFillType ?? typeof(object);
					}
					if (!yType.IsValueType)
					{
						yType = GenericMethodInstantiationComparer.CannonicalFillType ?? typeof(object);
					}
				}
				if (!this.genericTypeComparer.Equals(xType, yType))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002E94 RID: 11924 RVA: 0x000A047C File Offset: 0x0009E67C
		public int GetHashCode(MethodBase obj)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(obj, "obj");
			if (!obj.IsGenericMethod || obj.ContainsGenericParameters)
			{
				Type declaringType = obj.DeclaringType;
				if (declaringType == null || !declaringType.IsGenericType)
				{
					return obj.GetHashCode();
				}
			}
			int code = -559038737;
			if (obj.DeclaringType != null)
			{
				code ^= obj.DeclaringType.Assembly.GetHashCode();
				code ^= this.genericTypeComparer.GetHashCode(obj.DeclaringType);
			}
			code ^= obj.Name.GetHashCode(StringComparison.Ordinal);
			ParameterInfo[] parameters = obj.GetParameters();
			int paramCount = parameters.Length;
			paramCount ^= paramCount << 4;
			paramCount ^= paramCount << 8;
			paramCount ^= paramCount << 16;
			code ^= paramCount;
			if (obj.IsGenericMethod)
			{
				Type[] typeArgs = obj.GetGenericArguments();
				for (int i = 0; i < typeArgs.Length; i++)
				{
					int offs = i % 32;
					Type type = typeArgs[i];
					int num;
					if (!type.IsValueType)
					{
						Type cannonicalFillType = GenericMethodInstantiationComparer.CannonicalFillType;
						num = ((cannonicalFillType != null) ? cannonicalFillType.GetHashCode() : 1431655765);
					}
					else
					{
						num = this.genericTypeComparer.GetHashCode(type);
					}
					int typeCode = num;
					typeCode = (typeCode << offs) | (typeCode >> 32 - offs);
					code ^= typeCode;
				}
			}
			MethodInfo info = obj as MethodInfo;
			MethodBase definition;
			if (info != null)
			{
				definition = info.GetActualGenericMethodDefinition();
			}
			else
			{
				definition = obj.GetUnfilledMethodOnGenericType();
			}
			ParameterInfo[] definitionParams = definition.GetParameters();
			for (int j = 0; j < parameters.Length; j++)
			{
				int offs2 = j % 32;
				Type type2 = parameters[j].ParameterType;
				int typeCode2 = this.genericTypeComparer.GetHashCode(type2);
				if (definitionParams[j].ParameterType.IsGenericParameter && !type2.IsValueType)
				{
					Type cannonicalFillType2 = GenericMethodInstantiationComparer.CannonicalFillType;
					typeCode2 = ((cannonicalFillType2 != null) ? cannonicalFillType2.GetHashCode() : 1431655765);
				}
				typeCode2 = (typeCode2 >> offs2) | (typeCode2 << 32 - offs2);
				code ^= typeCode2;
			}
			return code;
		}

		// Token: 0x04003B3C RID: 15164
		[Nullable(2)]
		internal static Type CannonicalFillType = typeof(object).Assembly.GetType("System.__Canon");

		// Token: 0x04003B3D RID: 15165
		private readonly IEqualityComparer<Type> genericTypeComparer;
	}
}
