using System;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyLib
{
	/// <summary>A helper class for fast access to getters and setters</summary>
	// Token: 0x02000008 RID: 8
	public static class FastAccess
	{
		/// <summary>Creates an instantiation delegate</summary>
		/// <typeparam name="T">Type that constructor creates</typeparam>
		/// <returns>The new instantiation delegate</returns>
		// Token: 0x06000012 RID: 18 RVA: 0x000021DC File Offset: 0x000003DC
		public static InstantiationHandler<T> CreateInstantiationHandler<T>()
		{
			ConstructorInfo constructorInfo = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null);
			if (constructorInfo == null)
			{
				throw new ApplicationException(string.Format("The type {0} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).", typeof(T)));
			}
			DynamicMethodDefinition dynamicMethod = new DynamicMethodDefinition("InstantiateObject_" + typeof(T).Name, typeof(T), null);
			ILGenerator generator = dynamicMethod.GetILGenerator();
			generator.Emit(OpCodes.Newobj, constructorInfo);
			generator.Emit(OpCodes.Ret);
			return dynamicMethod.Generate().CreateDelegate<InstantiationHandler<T>>();
		}

		/// <summary>Creates an getter delegate for a property</summary>
		/// <typeparam name="T">Type that getter reads property from</typeparam>
		/// <typeparam name="S">Type of the property that gets accessed</typeparam>
		/// <param name="propertyInfo">The property</param>
		/// <returns>The new getter delegate</returns>
		// Token: 0x06000013 RID: 19 RVA: 0x00002274 File Offset: 0x00000474
		[Obsolete("Use AccessTools.MethodDelegate<Func<T, S>>(PropertyInfo.GetGetMethod(true))")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static GetterHandler<T, S> CreateGetterHandler<T, S>(PropertyInfo propertyInfo)
		{
			MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
			DynamicMethodDefinition dynamicGet = FastAccess.CreateGetDynamicMethod<T, S>(propertyInfo.DeclaringType);
			ILGenerator getGenerator = dynamicGet.GetILGenerator();
			getGenerator.Emit(OpCodes.Ldarg_0);
			getGenerator.Emit(OpCodes.Call, getMethodInfo);
			getGenerator.Emit(OpCodes.Ret);
			return dynamicGet.Generate().CreateDelegate<GetterHandler<T, S>>();
		}

		/// <summary>Creates an getter delegate for a field</summary>
		/// <typeparam name="T">Type that getter reads field from</typeparam>
		/// <typeparam name="S">Type of the field that gets accessed</typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The new getter delegate</returns>
		// Token: 0x06000014 RID: 20 RVA: 0x000022CC File Offset: 0x000004CC
		[Obsolete("Use AccessTools.FieldRefAccess<T, S>(fieldInfo)")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static GetterHandler<T, S> CreateGetterHandler<T, S>(FieldInfo fieldInfo)
		{
			DynamicMethodDefinition dynamicGet = FastAccess.CreateGetDynamicMethod<T, S>(fieldInfo.DeclaringType);
			ILGenerator getGenerator = dynamicGet.GetILGenerator();
			getGenerator.Emit(OpCodes.Ldarg_0);
			getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
			getGenerator.Emit(OpCodes.Ret);
			return dynamicGet.Generate().CreateDelegate<GetterHandler<T, S>>();
		}

		/// <summary>Creates an getter delegate for a field (with a list of possible field names)</summary>
		/// <typeparam name="T">Type that getter reads field/property from</typeparam>
		/// <typeparam name="S">Type of the field/property that gets accessed</typeparam>
		/// <param name="names">A list of possible field names</param>
		/// <returns>The new getter delegate</returns>
		// Token: 0x06000015 RID: 21 RVA: 0x0000231C File Offset: 0x0000051C
		[Obsolete("Use AccessTools.FieldRefAccess<T, S>(name) for fields and AccessTools.MethodDelegate<Func<T, S>>(AccessTools.PropertyGetter(typeof(T), name)) for properties")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static GetterHandler<T, S> CreateFieldGetter<T, S>(params string[] names)
		{
			foreach (string name in names)
			{
				FieldInfo field = typeof(T).GetField(name, AccessTools.all);
				if (field != null)
				{
					return FastAccess.CreateGetterHandler<T, S>(field);
				}
				PropertyInfo property = typeof(T).GetProperty(name, AccessTools.all);
				if (property != null)
				{
					return FastAccess.CreateGetterHandler<T, S>(property);
				}
			}
			return null;
		}

		/// <summary>Creates an setter delegate</summary>
		/// <typeparam name="T">Type that setter assigns property value to</typeparam>
		/// <typeparam name="S">Type of the property that gets assigned</typeparam>
		/// <param name="propertyInfo">The property</param>
		/// <returns>The new setter delegate</returns>
		// Token: 0x06000016 RID: 22 RVA: 0x00002384 File Offset: 0x00000584
		[Obsolete("Use AccessTools.MethodDelegate<Action<T, S>>(PropertyInfo.GetSetMethod(true))")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static SetterHandler<T, S> CreateSetterHandler<T, S>(PropertyInfo propertyInfo)
		{
			MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
			DynamicMethodDefinition dynamicSet = FastAccess.CreateSetDynamicMethod<T, S>(propertyInfo.DeclaringType);
			ILGenerator setGenerator = dynamicSet.GetILGenerator();
			setGenerator.Emit(OpCodes.Ldarg_0);
			setGenerator.Emit(OpCodes.Ldarg_1);
			setGenerator.Emit(OpCodes.Call, setMethodInfo);
			setGenerator.Emit(OpCodes.Ret);
			return dynamicSet.Generate().CreateDelegate<SetterHandler<T, S>>();
		}

		/// <summary>Creates an setter delegate for a field</summary>
		/// <typeparam name="T">Type that setter assigns field value to</typeparam>
		/// <typeparam name="S">Type of the field that gets assigned</typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The new getter delegate</returns>
		// Token: 0x06000017 RID: 23 RVA: 0x000023E4 File Offset: 0x000005E4
		[Obsolete("Use AccessTools.FieldRefAccess<T, S>(fieldInfo)")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static SetterHandler<T, S> CreateSetterHandler<T, S>(FieldInfo fieldInfo)
		{
			DynamicMethodDefinition dynamicSet = FastAccess.CreateSetDynamicMethod<T, S>(fieldInfo.DeclaringType);
			ILGenerator setGenerator = dynamicSet.GetILGenerator();
			setGenerator.Emit(OpCodes.Ldarg_0);
			setGenerator.Emit(OpCodes.Ldarg_1);
			setGenerator.Emit(OpCodes.Stfld, fieldInfo);
			setGenerator.Emit(OpCodes.Ret);
			return dynamicSet.Generate().CreateDelegate<SetterHandler<T, S>>();
		}

		// Token: 0x06000018 RID: 24 RVA: 0x0000243C File Offset: 0x0000063C
		private static DynamicMethodDefinition CreateGetDynamicMethod<T, S>(Type type)
		{
			return new DynamicMethodDefinition("DynamicGet_" + type.Name, typeof(S), new Type[] { typeof(T) });
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002470 File Offset: 0x00000670
		private static DynamicMethodDefinition CreateSetDynamicMethod<T, S>(Type type)
		{
			return new DynamicMethodDefinition("DynamicSet_" + type.Name, typeof(void), new Type[]
			{
				typeof(T),
				typeof(S)
			});
		}
	}
}
