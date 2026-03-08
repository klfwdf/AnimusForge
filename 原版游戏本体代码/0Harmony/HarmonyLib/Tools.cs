using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x020001C7 RID: 455
	internal class Tools
	{
		// Token: 0x060007FF RID: 2047 RVA: 0x0001A8F0 File Offset: 0x00018AF0
		internal static Tools.TypeAndName TypColonName(string typeColonName)
		{
			if (typeColonName == null)
			{
				throw new ArgumentNullException("typeColonName");
			}
			string[] parts = typeColonName.Split(new char[] { ':' });
			if (parts.Length != 2)
			{
				throw new ArgumentException(" must be specified as 'Namespace.Type1.Type2:MemberName", "typeColonName");
			}
			return new Tools.TypeAndName
			{
				type = AccessTools.TypeByName(parts[0]),
				name = parts[1]
			};
		}

		// Token: 0x06000800 RID: 2048 RVA: 0x0001A958 File Offset: 0x00018B58
		internal static void ValidateFieldType<F>(FieldInfo fieldInfo)
		{
			Type returnType = typeof(F);
			Type fieldType = fieldInfo.FieldType;
			if (returnType == fieldType)
			{
				return;
			}
			if (fieldType.IsEnum)
			{
				Type underlyingType = Enum.GetUnderlyingType(fieldType);
				if (returnType != underlyingType)
				{
					string str = "FieldRefAccess return type must be the same as FieldType or ";
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 1);
					defaultInterpolatedStringHandler.AppendLiteral("FieldType's underlying integral type (");
					defaultInterpolatedStringHandler.AppendFormatted<Type>(underlyingType);
					defaultInterpolatedStringHandler.AppendLiteral(") for enum types");
					throw new ArgumentException(str + defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			else
			{
				if (fieldType.IsValueType)
				{
					throw new ArgumentException("FieldRefAccess return type must be the same as FieldType for value types");
				}
				if (!returnType.IsAssignableFrom(fieldType))
				{
					throw new ArgumentException("FieldRefAccess return type must be assignable from FieldType for reference types");
				}
			}
		}

		// Token: 0x06000801 RID: 2049 RVA: 0x0001AA04 File Offset: 0x00018C04
		internal static AccessTools.FieldRef<T, F> FieldRefAccess<T, F>(FieldInfo fieldInfo, bool needCastclass)
		{
			Tools.ValidateFieldType<F>(fieldInfo);
			Type delegateInstanceType = typeof(T);
			Type declaringType = fieldInfo.DeclaringType;
			DynamicMethodDefinition dm = new DynamicMethodDefinition("__refget_" + delegateInstanceType.Name + "_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { delegateInstanceType });
			ILGenerator il = dm.GetILGenerator();
			if (fieldInfo.IsStatic)
			{
				il.Emit(OpCodes.Ldsflda, fieldInfo);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				if (needCastclass)
				{
					il.Emit(OpCodes.Castclass, declaringType);
				}
				il.Emit(OpCodes.Ldflda, fieldInfo);
			}
			il.Emit(OpCodes.Ret);
			return dm.Generate().CreateDelegate<AccessTools.FieldRef<T, F>>();
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x0001AABC File Offset: 0x00018CBC
		internal static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<T, F>(FieldInfo fieldInfo) where T : struct
		{
			Tools.ValidateFieldType<F>(fieldInfo);
			DynamicMethodDefinition dm = new DynamicMethodDefinition("__refget_" + typeof(T).Name + "_struct_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { typeof(T).MakeByRefType() });
			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldflda, fieldInfo);
			il.Emit(OpCodes.Ret);
			return dm.Generate().CreateDelegate<AccessTools.StructFieldRef<T, F>>();
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x0001AB54 File Offset: 0x00018D54
		internal static AccessTools.FieldRef<F> StaticFieldRefAccess<F>(FieldInfo fieldInfo)
		{
			if (!fieldInfo.IsStatic)
			{
				throw new ArgumentException("Field must be static");
			}
			Tools.ValidateFieldType<F>(fieldInfo);
			string str = "__refget_";
			Type declaringType = fieldInfo.DeclaringType;
			DynamicMethodDefinition dm = new DynamicMethodDefinition(str + (((declaringType != null) ? declaringType.Name : null) ?? "null") + "_static_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), Array.Empty<Type>());
			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldsflda, fieldInfo);
			il.Emit(OpCodes.Ret);
			return dm.Generate().CreateDelegate<AccessTools.FieldRef<F>>();
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x0001ABF0 File Offset: 0x00018DF0
		internal static FieldInfo GetInstanceField(Type type, string fieldName)
		{
			FieldInfo fieldInfo = AccessTools.Field(type, fieldName);
			if (fieldInfo == null)
			{
				throw new MissingFieldException(type.Name, fieldName);
			}
			if (fieldInfo.IsStatic)
			{
				throw new ArgumentException("Field must not be static");
			}
			return fieldInfo;
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x0001AC2C File Offset: 0x00018E2C
		internal static bool FieldRefNeedsClasscast(Type delegateInstanceType, Type declaringType)
		{
			bool needCastclass = false;
			if (delegateInstanceType != declaringType)
			{
				needCastclass = delegateInstanceType.IsAssignableFrom(declaringType);
				if (!needCastclass && !declaringType.IsAssignableFrom(delegateInstanceType))
				{
					throw new ArgumentException("FieldDeclaringType must be assignable from or to T (FieldRefAccess instance type) - \"instanceOfT is FieldDeclaringType\" must be possible");
				}
			}
			return needCastclass;
		}

		// Token: 0x06000806 RID: 2054 RVA: 0x0001AC64 File Offset: 0x00018E64
		internal static void ValidateStructField<T, F>(FieldInfo fieldInfo) where T : struct
		{
			if (fieldInfo.IsStatic)
			{
				throw new ArgumentException("Field must not be static");
			}
			if (fieldInfo.DeclaringType != typeof(T))
			{
				throw new ArgumentException("FieldDeclaringType must be T (StructFieldRefAccess instance type)");
			}
		}

		// Token: 0x040002BC RID: 700
		internal static readonly bool isWindows = Environment.OSVersion.Platform.Equals(PlatformID.Win32NT);

		// Token: 0x020001C8 RID: 456
		internal struct TypeAndName
		{
			// Token: 0x040002BD RID: 701
			internal Type type;

			// Token: 0x040002BE RID: 702
			internal string name;
		}
	}
}
