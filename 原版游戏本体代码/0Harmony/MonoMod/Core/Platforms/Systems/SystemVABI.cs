using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Systems
{
	// Token: 0x02000523 RID: 1315
	[NullableContext(1)]
	[Nullable(0)]
	internal static class SystemVABI
	{
		// Token: 0x06001D84 RID: 7556 RVA: 0x0005F634 File Offset: 0x0005D834
		public static TypeClassification ClassifyAMD64(Type type, bool isReturn)
		{
			int totalSize = type.GetManagedSize();
			if (totalSize > 16)
			{
				if (totalSize > 32)
				{
					if (!isReturn)
					{
						return TypeClassification.OnStack;
					}
					return TypeClassification.ByReference;
				}
				else if (true)
				{
					if (!isReturn)
					{
						return TypeClassification.OnStack;
					}
					return TypeClassification.ByReference;
				}
			}
			return TypeClassification.InRegister;
		}

		// Token: 0x06001D85 RID: 7557 RVA: 0x0005F664 File Offset: 0x0005D864
		public static TypeClassification ClassifyARM64(Type type, bool isReturn)
		{
			int totalSize = type.GetManagedSize();
			if (totalSize > 16)
			{
				if (totalSize > 32)
				{
					if (!isReturn)
					{
						return TypeClassification.OnStack;
					}
					return TypeClassification.ByReference;
				}
				else if (SystemVABI.AnyFieldsNotFloat(type))
				{
					if (!isReturn)
					{
						return TypeClassification.OnStack;
					}
					return TypeClassification.ByReference;
				}
			}
			return TypeClassification.InRegister;
		}

		// Token: 0x06001D86 RID: 7558 RVA: 0x0005F699 File Offset: 0x0005D899
		private static bool AnyFieldsNotFloat(Type type)
		{
			return SystemVABI.SysVIsMemoryCache.GetValue(type2, delegate(Type type)
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				for (int i = 0; i < fields.Length; i++)
				{
					Type fieldType = fields[i].FieldType;
					if (fieldType != null && !fieldType.IsPrimitive && fieldType.IsValueType && SystemVABI.AnyFieldsNotFloat(fieldType))
					{
						return SystemVABI.SBTrue;
					}
					TypeCode typeCode = Type.GetTypeCode(fieldType);
					if (typeCode != TypeCode.Single && typeCode != TypeCode.Double)
					{
						return SystemVABI.SBTrue;
					}
				}
				return SystemVABI.SBFalse;
			}).Value;
		}

		// Token: 0x04001226 RID: 4646
		private static readonly ConditionalWeakTable<Type, StrongBox<bool>> SysVIsMemoryCache = new ConditionalWeakTable<Type, StrongBox<bool>>();

		// Token: 0x04001227 RID: 4647
		private static readonly StrongBox<bool> SBTrue = new StrongBox<bool>(true);

		// Token: 0x04001228 RID: 4648
		private static readonly StrongBox<bool> SBFalse = new StrongBox<bool>(false);
	}
}
