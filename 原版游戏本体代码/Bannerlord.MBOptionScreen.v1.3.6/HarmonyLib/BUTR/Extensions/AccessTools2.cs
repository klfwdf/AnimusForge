using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000068 RID: 104
	[NullableContext(1)]
	[Nullable(0)]
	internal static class AccessTools2
	{
		// Token: 0x060003D4 RID: 980 RVA: 0x0000E2F4 File Offset: 0x0000C4F4
		[return: Nullable(2)]
		public static ConstructorInfo DeclaredConstructor(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredConstructor: 'type' is null");
				}
				return null;
			}
			if (parameters == null)
			{
				parameters = Type.EmptyTypes;
			}
			BindingFlags flags = (searchForStatic ? (AccessTools.allDeclared & ~BindingFlags.Instance) : (AccessTools.allDeclared & ~BindingFlags.Static));
			return type.GetConstructor(flags, null, parameters, new ParameterModifier[0]);
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x0000E344 File Offset: 0x0000C544
		[return: Nullable(2)]
		public static ConstructorInfo Constructor(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.ConstructorInfo: 'type' is null");
				}
				return null;
			}
			if (parameters == null)
			{
				parameters = Type.EmptyTypes;
			}
			BindingFlags flags = (searchForStatic ? (AccessTools.all & ~BindingFlags.Instance) : (AccessTools.all & ~BindingFlags.Static));
			return AccessTools2.FindIncludingBaseTypes<ConstructorInfo>(type, (Type t) => t.GetConstructor(flags, null, parameters, new ParameterModifier[0]));
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0000E3B0 File Offset: 0x0000C5B0
		[return: Nullable(2)]
		public static ConstructorInfo DeclaredConstructor(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			if (string.IsNullOrWhiteSpace(typeString))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
				}
				return null;
			}
			Type type = AccessTools2.TypeByName(typeString, logErrorInTrace);
			if (type == null)
			{
				return null;
			}
			return AccessTools2.DeclaredConstructor(type, parameters, searchForStatic, logErrorInTrace);
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0000E3EC File Offset: 0x0000C5EC
		[return: Nullable(2)]
		public static ConstructorInfo Constructor(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			if (string.IsNullOrWhiteSpace(typeString))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
				}
				return null;
			}
			Type type = AccessTools2.TypeByName(typeString, logErrorInTrace);
			if (type == null)
			{
				return null;
			}
			return AccessTools2.Constructor(type, parameters, searchForStatic, logErrorInTrace);
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x0000E428 File Offset: 0x0000C628
		[return: Nullable(2)]
		public static TDelegate GetDeclaredConstructorDelegate<[Nullable(0)] TDelegate>(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(type, parameters, false, logErrorInTrace);
			if (constructorInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x0000E454 File Offset: 0x0000C654
		[return: Nullable(2)]
		public static TDelegate GetConstructorDelegate<[Nullable(0)] TDelegate>(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.Constructor(type, parameters, false, logErrorInTrace);
			if (constructorInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x0000E480 File Offset: 0x0000C680
		[return: Nullable(2)]
		public static TDelegate GetDeclaredConstructorDelegate<[Nullable(0)] TDelegate>(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(typeString, parameters, false, logErrorInTrace);
			if (constructorInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
		}

		// Token: 0x060003DB RID: 987 RVA: 0x0000E4AC File Offset: 0x0000C6AC
		[return: Nullable(2)]
		public static TDelegate GetConstructorDelegate<[Nullable(0)] TDelegate>(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.Constructor(typeString, parameters, false, logErrorInTrace);
			if (constructorInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
		}

		// Token: 0x060003DC RID: 988 RVA: 0x0000E4D8 File Offset: 0x0000C6D8
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003DD RID: 989 RVA: 0x0000E508 File Offset: 0x0000C708
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003DE RID: 990 RVA: 0x0000E538 File Offset: 0x0000C738
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003DF RID: 991 RVA: 0x0000E568 File Offset: 0x0000C768
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x0000E598 File Offset: 0x0000C798
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(type, name, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x0000E5C4 File Offset: 0x0000C7C4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(type, name, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0000E5F0 File Offset: 0x0000C7F0
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(type, name, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0000E61C File Offset: 0x0000C81C
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(type, name, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0000E648 File Offset: 0x0000C848
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(type, method, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0000E674 File Offset: 0x0000C874
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(type, method, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x0000E6A0 File Offset: 0x0000C8A0
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(type, method, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x0000E6CC File Offset: 0x0000C8CC
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(type, method, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x0000E6F8 File Offset: 0x0000C8F8
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x0000E724 File Offset: 0x0000C924
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x0000E750 File Offset: 0x0000C950
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x0000E77C File Offset: 0x0000C97C
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003EC RID: 1004 RVA: 0x0000E7A8 File Offset: 0x0000C9A8
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003ED RID: 1005 RVA: 0x0000E7D4 File Offset: 0x0000C9D4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x0000E800 File Offset: 0x0000CA00
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x0000E82C File Offset: 0x0000CA2C
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(typeColonPropertyName, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x0000E858 File Offset: 0x0000CA58
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(ConstructorInfo constructorInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			TDelegate result;
			if (constructorInfo == null)
			{
				result = default(TDelegate);
				return result;
			}
			MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
			if (delegateInvoke == null)
			{
				result = default(TDelegate);
				return result;
			}
			if (!delegateInvoke.ReturnType.IsAssignableFrom(constructorInfo.DeclaringType))
			{
				result = default(TDelegate);
				return result;
			}
			ParameterInfo[] delegateParameters = delegateInvoke.GetParameters();
			ParameterInfo[] constructorParameters = constructorInfo.GetParameters();
			if (delegateParameters.Length - constructorParameters.Length != 0 && !AccessTools2.ParametersAreEqual(delegateParameters, constructorParameters))
			{
				result = default(TDelegate);
				return result;
			}
			ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
			List<ParameterExpression> returnParameters = delegateParameters.Select((ParameterInfo pi, int i) => Expression.Parameter(pi.ParameterType, string.Format("p{0}", i))).ToList<ParameterExpression>();
			List<Expression> inputParameters = returnParameters.Select(delegate(ParameterExpression pe, int i)
			{
				if (pe.IsByRef || pe.Type.Equals(constructorParameters[i].ParameterType))
				{
					return pe;
				}
				return Expression.Convert(pe, constructorParameters[i].ParameterType);
			}).ToList<Expression>();
			Expression @new = Expression.New(constructorInfo, inputParameters);
			UnaryExpression body = Expression.Convert(@new, delegateInvoke.ReturnType);
			try
			{
				result = Expression.Lambda<TDelegate>(body, returnParameters).Compile();
			}
			catch (Exception ex)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.GetDelegate<{0}>: Error while compiling lambds expression '{1}'", typeof(TDelegate).FullName, ex));
				}
				result = default(TDelegate);
			}
			return result;
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x0000E9C0 File Offset: 0x0000CBC0
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			TDelegate result;
			if (methodInfo == null)
			{
				result = default(TDelegate);
				return result;
			}
			MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
			if (delegateInvoke == null)
			{
				result = default(TDelegate);
				return result;
			}
			bool areEnums = delegateInvoke.ReturnType.IsEnum || methodInfo.ReturnType.IsEnum;
			bool areNumeric = delegateInvoke.ReturnType.IsNumeric() || methodInfo.ReturnType.IsNumeric();
			if (!areEnums && !areNumeric && !delegateInvoke.ReturnType.IsAssignableFrom(methodInfo.ReturnType))
			{
				result = default(TDelegate);
				return result;
			}
			ParameterInfo[] delegateParameters = delegateInvoke.GetParameters();
			ParameterInfo[] methodParameters = methodInfo.GetParameters();
			bool hasSameParameters = delegateParameters.Length - methodParameters.Length == 0 && AccessTools2.ParametersAreEqual(delegateParameters, methodParameters);
			bool hasInstance = instance != null;
			bool hasInstanceType = delegateParameters.Length - methodParameters.Length == 1 && (delegateParameters[0].ParameterType.IsAssignableFrom(methodInfo.DeclaringType) || methodInfo.DeclaringType.IsAssignableFrom(delegateParameters[0].ParameterType));
			if (!hasInstance && !hasInstanceType && !methodInfo.IsStatic)
			{
				result = default(TDelegate);
				return result;
			}
			if (hasInstance && methodInfo.IsStatic)
			{
				result = default(TDelegate);
				return result;
			}
			if (hasInstance && !methodInfo.IsStatic && !methodInfo.DeclaringType.IsAssignableFrom(instance.GetType()))
			{
				result = default(TDelegate);
				return result;
			}
			if (hasSameParameters && hasInstanceType)
			{
				result = default(TDelegate);
				return result;
			}
			if (hasInstance && (hasInstanceType || !hasSameParameters))
			{
				result = default(TDelegate);
				return result;
			}
			if (hasInstanceType && (hasInstance || hasSameParameters))
			{
				result = default(TDelegate);
				return result;
			}
			if (!hasInstanceType && !hasInstance && !hasSameParameters)
			{
				result = default(TDelegate);
				return result;
			}
			ParameterExpression instanceParameter = (hasInstanceType ? Expression.Parameter(delegateParameters[0].ParameterType, "instance") : null);
			List<ParameterExpression> returnParameters = delegateParameters.Skip((hasInstanceType > false) ? 1 : 0).Select((ParameterInfo pi, int i) => Expression.Parameter(pi.ParameterType, string.Format("p{0}", i))).ToList<ParameterExpression>();
			List<Expression> inputParameters = returnParameters.Select(delegate(ParameterExpression pe, int i)
			{
				if (pe.IsByRef || pe.Type.Equals(methodParameters[i].ParameterType))
				{
					return pe;
				}
				return Expression.Convert(pe, methodParameters[i].ParameterType);
			}).ToList<Expression>();
			MethodCallExpression call = (hasInstance ? (instance.GetType().Equals(methodInfo.DeclaringType) ? Expression.Call(Expression.Constant(instance), methodInfo, inputParameters) : Expression.Call(Expression.Convert(Expression.Constant(instance), instance.GetType()), methodInfo, inputParameters)) : (hasSameParameters ? Expression.Call(methodInfo, inputParameters) : (hasInstanceType ? (instanceParameter.Type.Equals(methodInfo.DeclaringType) ? Expression.Call(instanceParameter, methodInfo, inputParameters) : Expression.Call(Expression.Convert(instanceParameter, methodInfo.DeclaringType), methodInfo, inputParameters)) : null)));
			if (call == null)
			{
				result = default(TDelegate);
				return result;
			}
			UnaryExpression body = Expression.Convert(call, delegateInvoke.ReturnType);
			try
			{
				Expression body2 = body;
				IEnumerable<ParameterExpression> parameters;
				if (!hasInstanceType)
				{
					IEnumerable<ParameterExpression> enumerable = returnParameters;
					parameters = enumerable;
				}
				else
				{
					parameters = new List<ParameterExpression> { instanceParameter }.Concat(returnParameters);
				}
				result = Expression.Lambda<TDelegate>(body2, parameters).Compile();
			}
			catch (Exception ex)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.GetDelegate<{0}>: Error while compiling lambds expression '{1}'", typeof(TDelegate).FullName, ex));
				}
				result = default(TDelegate);
			}
			return result;
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x0000ED20 File Offset: 0x0000CF20
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(null, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x0000ED2A File Offset: 0x0000CF2A
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x0000ED33 File Offset: 0x0000CF33
		public static bool IsNumeric(this Type myType)
		{
			return AccessTools2.NumericTypes.Contains(myType);
		}

		// Token: 0x060003F5 RID: 1013 RVA: 0x0000ED40 File Offset: 0x0000CF40
		private static bool ParametersAreEqual(ParameterInfo[] delegateParameters, ParameterInfo[] methodParameters)
		{
			if (delegateParameters.Length - methodParameters.Length == 0)
			{
				for (int i = 0; i < methodParameters.Length; i++)
				{
					if (delegateParameters[i].ParameterType.IsByRef != methodParameters[i].ParameterType.IsByRef)
					{
						return false;
					}
					bool areEnums = delegateParameters[i].ParameterType.IsEnum || methodParameters[i].ParameterType.IsEnum;
					bool areNumeric = delegateParameters[i].ParameterType.IsNumeric() || methodParameters[i].ParameterType.IsNumeric();
					if (!areEnums && !areNumeric && !delegateParameters[i].ParameterType.IsAssignableFrom(methodParameters[i].ParameterType))
					{
						return false;
					}
				}
				return true;
			}
			if (delegateParameters.Length - methodParameters.Length == 1)
			{
				for (int j = 0; j < methodParameters.Length; j++)
				{
					if (delegateParameters[j + 1].ParameterType.IsByRef != methodParameters[j].ParameterType.IsByRef)
					{
						return false;
					}
					bool areEnums2 = delegateParameters[j + 1].ParameterType.IsEnum || methodParameters[j].ParameterType.IsEnum;
					bool areNumeric2 = delegateParameters[j + 1].ParameterType.IsNumeric() || methodParameters[j].ParameterType.IsNumeric();
					if (!areEnums2 && !areNumeric2 && !delegateParameters[j + 1].ParameterType.IsAssignableFrom(methodParameters[j].ParameterType))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060003F6 RID: 1014 RVA: 0x0000EE99 File Offset: 0x0000D099
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate, [Nullable(2)] TInstance>(TInstance instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F7 RID: 1015 RVA: 0x0000EEA8 File Offset: 0x0000D0A8
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegateObjectInstance<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x0000EED8 File Offset: 0x0000D0D8
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x0000EF08 File Offset: 0x0000D108
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegateObjectInstance<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FA RID: 1018 RVA: 0x0000EF34 File Offset: 0x0000D134
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x0000EF60 File Offset: 0x0000D160
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0000EF90 File Offset: 0x0000D190
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0000EFC0 File Offset: 0x0000D1C0
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0000EFEC File Offset: 0x0000D1EC
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0000F018 File Offset: 0x0000D218
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate, [Nullable(2)] TInstance>(TInstance instance, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			if (instance != null)
			{
				MethodInfo methodInfo = AccessTools2.DeclaredMethod(instance.GetType(), method, parameters, generics, logErrorInTrace);
				if (methodInfo != null)
				{
					return AccessTools2.GetDelegate<TDelegate, TInstance>(instance, methodInfo, logErrorInTrace);
				}
			}
			return default(TDelegate);
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0000F05C File Offset: 0x0000D25C
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate, [Nullable(2)] TInstance>(TInstance instance, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			if (instance != null)
			{
				MethodInfo methodInfo = AccessTools2.Method(instance.GetType(), method, parameters, generics, logErrorInTrace);
				if (methodInfo != null)
				{
					return AccessTools2.GetDelegate<TDelegate, TInstance>(instance, methodInfo, logErrorInTrace);
				}
			}
			return default(TDelegate);
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0000F0A0 File Offset: 0x0000D2A0
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0000F0D0 File Offset: 0x0000D2D0
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0000F100 File Offset: 0x0000D300
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0000F130 File Offset: 0x0000D330
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			if (methodInfo == null)
			{
				return default(TDelegate);
			}
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0000F160 File Offset: 0x0000D360
		[return: Nullable(2)]
		public static FieldInfo DeclaredField(Type type, string name, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredField: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.DeclaredField: type '{0}', 'name' is null", type));
				}
				return null;
			}
			FieldInfo fieldInfo = type.GetField(name, AccessTools.allDeclared);
			if (fieldInfo == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.DeclaredField: Could not find field for type '{0}' and name '{1}'", type, name));
				}
				return null;
			}
			return fieldInfo;
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0000F1C0 File Offset: 0x0000D3C0
		[return: Nullable(2)]
		public static FieldInfo Field(Type type, string name, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.Field: type '{0}', 'name' is null", type));
				}
				return null;
			}
			FieldInfo fieldInfo = AccessTools2.FindIncludingBaseTypes<FieldInfo>(type, (Type t) => t.GetField(name, AccessTools.all));
			if (fieldInfo == null && logErrorInTrace)
			{
				Trace.TraceError(string.Format("AccessTools2.Field: Could not find field for type '{0}' and name '{1}'", type, name));
			}
			return fieldInfo;
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0000F23C File Offset: 0x0000D43C
		[return: Nullable(2)]
		public static FieldInfo DeclaredField(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
				}
				return null;
			}
			return AccessTools2.DeclaredField(type, name, logErrorInTrace);
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0000F278 File Offset: 0x0000D478
		[return: Nullable(2)]
		public static FieldInfo Field(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
				}
				return null;
			}
			return AccessTools2.Field(type, name, logErrorInTrace);
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0000F2B4 File Offset: 0x0000D4B4
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, F> FieldRefAccess<[Nullable(2)] F>(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace))
			{
				Trace.TraceError("AccessTools2.FieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
				return null;
			}
			return AccessTools2.FieldRefAccess<F>(type, name, logErrorInTrace);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0000F2F0 File Offset: 0x0000D4F0
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, [Nullable(2)] F>(string fieldName, bool logErrorInTrace = true) where T : class
		{
			if (fieldName == null)
			{
				return null;
			}
			FieldInfo field = AccessTools2.GetInstanceField(typeof(T), fieldName, logErrorInTrace);
			if (field == null)
			{
				return null;
			}
			return AccessTools2.FieldRefAccessInternal<T, F>(field, false, logErrorInTrace);
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0000F324 File Offset: 0x0000D524
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, F> FieldRefAccess<[Nullable(2)] F>(Type type, string fieldName, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				return null;
			}
			if (fieldName == null)
			{
				return null;
			}
			FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
			if (fieldInfo == null)
			{
				return null;
			}
			if (!fieldInfo.IsStatic)
			{
				Type declaringType = fieldInfo.DeclaringType;
				if (declaringType != null)
				{
					if (declaringType.IsValueType)
					{
						if (logErrorInTrace)
						{
							Trace.TraceError("AccessTools2.FieldRefAccess<object, " + typeof(F).FullName + ">: FieldDeclaringType must be a class");
						}
						return null;
					}
					return AccessTools2.FieldRefAccessInternal<object, F>(fieldInfo, true, logErrorInTrace);
				}
			}
			return null;
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0000F398 File Offset: 0x0000D598
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, [Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : class
		{
			if (fieldInfo == null)
			{
				return null;
			}
			if (!fieldInfo.IsStatic)
			{
				Type declaringType = fieldInfo.DeclaringType;
				if (declaringType != null)
				{
					if (declaringType.IsValueType)
					{
						if (logErrorInTrace)
						{
							Trace.TraceError(string.Concat(new string[]
							{
								"AccessTools2.FieldRefAccess<",
								typeof(T).FullName,
								", ",
								typeof(F).FullName,
								">: FieldDeclaringType must be a class"
							}));
						}
						return null;
					}
					bool? flag = AccessTools2.FieldRefNeedsClasscast(typeof(T), declaringType, logErrorInTrace);
					if (flag != null)
					{
						bool needCastclass = flag.GetValueOrDefault();
						return AccessTools2.FieldRefAccessInternal<T, F>(fieldInfo, needCastclass, logErrorInTrace);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x0000F44C File Offset: 0x0000D64C
		[return: Nullable(new byte[] { 2, 1, 1 })]
		private static AccessTools.FieldRef<T, F> FieldRefAccessInternal<T, [Nullable(2)] F>(FieldInfo fieldInfo, bool needCastclass, bool logErrorInTrace = true) where T : class
		{
			if (!AccessTools2.Helper.IsValid(logErrorInTrace))
			{
				return null;
			}
			if (fieldInfo.IsStatic)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Concat(new string[]
					{
						"AccessTools2.FieldRefAccessInternal<",
						typeof(T).FullName,
						", ",
						typeof(F).FullName,
						">: Field must not be static"
					}));
				}
				return null;
			}
			if (!AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace))
			{
				return null;
			}
			Type delegateInstanceType = typeof(T);
			Type declaringType = fieldInfo.DeclaringType;
			AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create("__refget_" + delegateInstanceType.Name + "_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { delegateInstanceType });
			AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
			if (ilgeneratorHandle != null)
			{
				AccessTools2.ILGeneratorHandle il = ilgeneratorHandle.GetValueOrDefault();
				il.Emit(OpCodes.Ldarg_0);
				if (needCastclass)
				{
					il.Emit(OpCodes.Castclass, declaringType);
				}
				il.Emit(OpCodes.Ldflda, fieldInfo);
				il.Emit(OpCodes.Ret);
				object obj;
				if (dm == null)
				{
					obj = null;
				}
				else
				{
					MethodInfo methodInfo = dm.GetValueOrDefault().Generate();
					obj = ((methodInfo != null) ? methodInfo.CreateDelegate(typeof(AccessTools.FieldRef<T, F>)) : null);
				}
				return obj as AccessTools.FieldRef<T, F>;
			}
			return null;
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x0000F5BC File Offset: 0x0000D7BC
		private static bool? FieldRefNeedsClasscast(Type delegateInstanceType, Type declaringType, bool logErrorInTrace = true)
		{
			bool needCastclass = false;
			if (delegateInstanceType != declaringType)
			{
				needCastclass = delegateInstanceType.IsAssignableFrom(declaringType);
				if (!needCastclass && !declaringType.IsAssignableFrom(delegateInstanceType))
				{
					if (logErrorInTrace)
					{
						Trace.TraceError(string.Format("AccessTools2.FieldRefNeedsClasscast: FieldDeclaringType must be assignable from or to T (FieldRefAccess instance type) - 'instanceOfT is FieldDeclaringType' must be possible, delegateInstanceType '{0}', declaringType '{1}'", delegateInstanceType, declaringType));
					}
					return null;
				}
			}
			return new bool?(needCastclass);
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x0000F60C File Offset: 0x0000D80C
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> FieldRefAccess<[Nullable(2)] TField>(FieldInfo fieldInfo)
		{
			if (fieldInfo != null)
			{
				return AccessTools.FieldRefAccess<object, TField>(fieldInfo);
			}
			return null;
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x0000F61C File Offset: 0x0000D81C
		[return: Nullable(2)]
		public static MethodInfo DeclaredMethod(Type type, string name, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredMethod: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.DeclaredMethod: type '{0}', 'name' is null", type));
				}
				return null;
			}
			MethodInfo result;
			if (parameters == null)
			{
				try
				{
					result = type.GetMethod(name, AccessTools.allDeclared);
					goto IL_AA;
				}
				catch (AmbiguousMatchException ex)
				{
					result = type.GetMethod(name, AccessTools.allDeclared, null, Type.EmptyTypes, new ParameterModifier[0]);
					if (result == null)
					{
						if (logErrorInTrace)
						{
							Trace.TraceError(string.Format("AccessTools2.DeclaredMethod: Ambiguous match for type '{0}' and name '{1}' and parameters '{2}', '{3}'", new object[]
							{
								type,
								name,
								(parameters != null) ? parameters.Description() : null,
								ex
							}));
						}
						return null;
					}
					goto IL_AA;
				}
			}
			result = type.GetMethod(name, AccessTools.allDeclared, null, parameters, new ParameterModifier[0]);
			IL_AA:
			if (result == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.DeclaredMethod: Could not find method for type '{0}' and name '{1}' and parameters '{2}'", type, name, (parameters != null) ? parameters.Description() : null));
				}
				return null;
			}
			if (generics != null)
			{
				result = result.MakeGenericMethod(generics);
			}
			return result;
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x0000F718 File Offset: 0x0000D918
		[return: Nullable(2)]
		public static MethodInfo Method(Type type, string name, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.Method: type '{0}', 'name' is null", type));
				}
				return null;
			}
			MethodInfo result;
			if (parameters == null)
			{
				try
				{
					result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all));
					goto IL_D1;
				}
				catch (AmbiguousMatchException ex)
				{
					result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, Type.EmptyTypes, new ParameterModifier[0]));
					if (result == null)
					{
						if (logErrorInTrace)
						{
							string format = "AccessTools2.Method: Ambiguous match for type '{0}' and name '{1}' and parameters '{2}', '{3}'";
							object[] array = new object[4];
							array[0] = type;
							array[1] = name;
							int num = 2;
							Type[] parameters2 = parameters;
							array[num] = ((parameters2 != null) ? parameters2.Description() : null);
							array[3] = ex;
							Trace.TraceError(string.Format(format, array));
						}
						return null;
					}
					goto IL_D1;
				}
			}
			result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, parameters, new ParameterModifier[0]));
			IL_D1:
			if (result == null)
			{
				if (logErrorInTrace)
				{
					string format2 = "AccessTools2.Method: Could not find method for type '{0}' and name '{1}' and parameters '{2}'";
					object name2 = name;
					Type[] parameters3 = parameters;
					Trace.TraceError(string.Format(format2, type, name2, (parameters3 != null) ? parameters3.Description() : null));
				}
				return null;
			}
			if (generics != null)
			{
				result = result.MakeGenericMethod(generics);
			}
			return result;
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x0000F848 File Offset: 0x0000DA48
		[return: Nullable(2)]
		public static MethodInfo DeclaredMethod(string typeColonMethodname, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonMethodname, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
				}
				return null;
			}
			return AccessTools2.DeclaredMethod(type, name, parameters, generics, logErrorInTrace);
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x0000F888 File Offset: 0x0000DA88
		[return: Nullable(2)]
		public static MethodInfo Method(string typeColonMethodname, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonMethodname, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
				}
				return null;
			}
			return AccessTools2.Method(type, name, parameters, generics, logErrorInTrace);
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x0000F8C8 File Offset: 0x0000DAC8
		[return: Nullable(2)]
		public static PropertyInfo DeclaredProperty(Type type, string name, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredProperty: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.DeclaredProperty: type '{0}', 'name' is null", type));
				}
				return null;
			}
			PropertyInfo property = type.GetProperty(name, AccessTools.allDeclared);
			if (property == null && logErrorInTrace)
			{
				Trace.TraceError(string.Format("AccessTools2.DeclaredProperty: Could not find property for type '{0}' and name '{1}'", type, name));
			}
			return property;
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x0000F928 File Offset: 0x0000DB28
		[return: Nullable(2)]
		public static PropertyInfo Property(Type type, string name, bool logErrorInTrace = true)
		{
			if (type == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Property: 'type' is null");
				}
				return null;
			}
			if (name == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.Property: type '{0}', 'name' is null", type));
				}
				return null;
			}
			PropertyInfo property = AccessTools2.FindIncludingBaseTypes<PropertyInfo>(type, (Type t) => t.GetProperty(name, AccessTools.all));
			if (property == null && logErrorInTrace)
			{
				Trace.TraceError(string.Format("AccessTools2.Property: Could not find property for type '{0}' and name '{1}'", type, name));
			}
			return property;
		}

		// Token: 0x06000416 RID: 1046 RVA: 0x0000F9A3 File Offset: 0x0000DBA3
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertyGetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x0000F9B9 File Offset: 0x0000DBB9
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertySetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x0000F9CF File Offset: 0x0000DBCF
		[return: Nullable(2)]
		public static MethodInfo PropertyGetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(type, name, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x0000F9E5 File Offset: 0x0000DBE5
		[return: Nullable(2)]
		public static MethodInfo PropertySetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(type, name, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x0000F9FC File Offset: 0x0000DBFC
		[return: Nullable(2)]
		public static PropertyInfo DeclaredProperty(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonPropertyName, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredProperty: Could not find type or property for '" + typeColonPropertyName + "'");
				}
				return null;
			}
			return AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x0000FA38 File Offset: 0x0000DC38
		[return: Nullable(2)]
		public static PropertyInfo Property(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonPropertyName, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Property: Could not find type or property for '" + typeColonPropertyName + "'");
				}
				return null;
			}
			return AccessTools2.Property(type, name, logErrorInTrace);
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x0000FA74 File Offset: 0x0000DC74
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(typeColonPropertyName, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x0000FA89 File Offset: 0x0000DC89
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(typeColonPropertyName, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x0000FA9E File Offset: 0x0000DC9E
		[return: Nullable(2)]
		public static MethodInfo PropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(typeColonPropertyName, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x0000FAB3 File Offset: 0x0000DCB3
		[return: Nullable(2)]
		public static MethodInfo PropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(typeColonPropertyName, logErrorInTrace);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		// Token: 0x06000420 RID: 1056 RVA: 0x0000FAC8 File Offset: 0x0000DCC8
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> StaticFieldRefAccess<[Nullable(2)] TField>(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			if (!AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.StaticFieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
				}
				return null;
			}
			return AccessTools2.StaticFieldRefAccess<TField>(type, name, logErrorInTrace);
		}

		// Token: 0x06000421 RID: 1057 RVA: 0x0000FB04 File Offset: 0x0000DD04
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<F> StaticFieldRefAccess<[Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			if (fieldInfo == null)
			{
				return null;
			}
			return AccessTools2.StaticFieldRefAccessInternal<F>(fieldInfo, logErrorInTrace);
		}

		// Token: 0x06000422 RID: 1058 RVA: 0x0000FB14 File Offset: 0x0000DD14
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> StaticFieldRefAccess<[Nullable(2)] TField>(Type type, string fieldName, bool logErrorInTrace = true)
		{
			FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
			if (fieldInfo == null)
			{
				return null;
			}
			return AccessTools2.StaticFieldRefAccess<TField>(fieldInfo, logErrorInTrace);
		}

		// Token: 0x06000423 RID: 1059 RVA: 0x0000FB38 File Offset: 0x0000DD38
		[return: Nullable(new byte[] { 2, 1 })]
		private static AccessTools.FieldRef<F> StaticFieldRefAccessInternal<[Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			if (!AccessTools2.Helper.IsValid(logErrorInTrace))
			{
				return null;
			}
			if (!fieldInfo.IsStatic)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.StaticFieldRefAccessInternal<" + typeof(F).FullName + ">: Field must be static");
				}
				return null;
			}
			if (!AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace))
			{
				return null;
			}
			string str = "__refget_";
			Type declaringType = fieldInfo.DeclaringType;
			AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create(str + (((declaringType != null) ? declaringType.Name : null) ?? "null") + "_static_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[0]);
			AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
			if (ilgeneratorHandle != null)
			{
				AccessTools2.ILGeneratorHandle il = ilgeneratorHandle.GetValueOrDefault();
				il.Emit(OpCodes.Ldsflda, fieldInfo);
				il.Emit(OpCodes.Ret);
				object obj;
				if (dm == null)
				{
					obj = null;
				}
				else
				{
					MethodInfo methodInfo = dm.GetValueOrDefault().Generate();
					obj = ((methodInfo != null) ? methodInfo.CreateDelegate(typeof(AccessTools.FieldRef<F>)) : null);
				}
				return obj as AccessTools.FieldRef<F>;
			}
			return null;
		}

		// Token: 0x06000424 RID: 1060 RVA: 0x0000FC60 File Offset: 0x0000DE60
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<T, [Nullable(2)] F>([Nullable(1)] string fieldName, bool logErrorInTrace = true) where T : struct
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				return null;
			}
			FieldInfo field = AccessTools2.GetInstanceField(typeof(T), fieldName, logErrorInTrace);
			if (field == null)
			{
				return null;
			}
			return AccessTools2.StructFieldRefAccessInternal<T, F>(field, logErrorInTrace);
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x0000FC95 File Offset: 0x0000DE95
		[NullableContext(2)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<[Nullable(0)] T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			if (fieldInfo == null)
			{
				return null;
			}
			if (!AccessTools2.ValidateStructField<T, F>(fieldInfo, logErrorInTrace))
			{
				return null;
			}
			return AccessTools2.StructFieldRefAccessInternal<T, F>(fieldInfo, logErrorInTrace);
		}

		// Token: 0x06000426 RID: 1062 RVA: 0x0000FCB0 File Offset: 0x0000DEB0
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		private static AccessTools.StructFieldRef<T, F> StructFieldRefAccessInternal<T, [Nullable(2)] F>([Nullable(1)] FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			if (!AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace))
			{
				return null;
			}
			AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create("__refget_" + typeof(T).Name + "_struct_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { typeof(T).MakeByRefType() });
			AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
			if (ilgeneratorHandle != null)
			{
				AccessTools2.ILGeneratorHandle il = ilgeneratorHandle.GetValueOrDefault();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldflda, fieldInfo);
				il.Emit(OpCodes.Ret);
				object obj;
				if (dm == null)
				{
					obj = null;
				}
				else
				{
					MethodInfo methodInfo = dm.GetValueOrDefault().Generate();
					obj = ((methodInfo != null) ? methodInfo.CreateDelegate(typeof(AccessTools.StructFieldRef<T, F>)) : null);
				}
				return obj as AccessTools.StructFieldRef<T, F>;
			}
			return null;
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x0000FDAF File Offset: 0x0000DFAF
		public static IEnumerable<Assembly> AllAssemblies()
		{
			return from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.FullName.StartsWith("Microsoft.VisualStudio")
				select a;
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x0000FDDF File Offset: 0x0000DFDF
		public static IEnumerable<Type> AllTypes()
		{
			return AccessTools2.AllAssemblies().SelectMany((Assembly a) => AccessTools2.GetTypesFromAssembly(a, true));
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x0000FE0C File Offset: 0x0000E00C
		public static Type[] GetTypesFromAssembly(Assembly assembly, bool logErrorInTrace = true)
		{
			if (assembly == null)
			{
				return Type.EmptyTypes;
			}
			Type[] result;
			try
			{
				result = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.GetTypesFromAssembly: assembly {0} => {1}", assembly, ex));
				}
				result = (from type in ex.Types
					where type != null
					select type).ToArray<Type>();
			}
			return result;
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x0000FE84 File Offset: 0x0000E084
		public static Type[] GetTypesFromAssemblyIfValid(Assembly assembly, bool logErrorInTrace = true)
		{
			if (assembly == null)
			{
				return Type.EmptyTypes;
			}
			Type[] result;
			try
			{
				result = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.GetTypesFromAssemblyIfValid: assembly {0} => {1}", assembly, ex));
				}
				result = Type.EmptyTypes;
			}
			return result;
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x0000FED4 File Offset: 0x0000E0D4
		[return: Nullable(2)]
		public static Type TypeByName(string name, bool logErrorInTrace = true)
		{
			if (string.IsNullOrEmpty(name))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.TypeByName: 'name' is null or empty");
				}
				return null;
			}
			Type type = Type.GetType(name, false);
			if (type == null)
			{
				type = AccessTools2.AllTypes().FirstOrDefault((Type t) => t.FullName == name);
			}
			if (type == null)
			{
				type = AccessTools2.AllTypes().FirstOrDefault((Type t) => t.Name == name);
			}
			if (type == null && logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.TypeByName: Could not find type named '" + name + "'");
			}
			return type;
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x0000FF70 File Offset: 0x0000E170
		[return: Nullable(2)]
		public static T FindIncludingBaseTypes<T>(Type type, Func<Type, T> func) where T : class
		{
			if (type == null || func == null)
			{
				return default(T);
			}
			T result;
			for (;;)
			{
				result = func(type);
				if (result != null)
				{
					break;
				}
				type = type.BaseType;
				if (type == null)
				{
					goto Block_3;
				}
			}
			return result;
			Block_3:
			return default(T);
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x0000FFB4 File Offset: 0x0000E1B4
		[return: Nullable(2)]
		private static FieldInfo GetInstanceField(Type type, string fieldName, bool logErrorInTrace = true)
		{
			FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
			if (fieldInfo == null)
			{
				return null;
			}
			if (fieldInfo.IsStatic)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Format("AccessTools2.GetInstanceField: Field must not be static, type '{0}', fieldName '{1}'", type, fieldName));
				}
				return null;
			}
			return fieldInfo;
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x0000FFF0 File Offset: 0x0000E1F0
		[NullableContext(2)]
		private static bool ValidateFieldType<F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			if (fieldInfo == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.ValidateFieldType<" + typeof(F).FullName + ">: 'fieldInfo' is null");
				}
				return false;
			}
			Type returnType = typeof(F);
			Type fieldType = fieldInfo.FieldType;
			if (returnType == fieldType)
			{
				return true;
			}
			if (fieldType.IsEnum)
			{
				Type underlyingType = Enum.GetUnderlyingType(fieldType);
				if (returnType != underlyingType)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError(string.Format("AccessTools2.ValidateFieldType<{0}>: FieldRefAccess return type must be the same as FieldType or FieldType's underlying integral type ({1}) for enum types, fieldInfo '{2}'", typeof(F).FullName, underlyingType, fieldInfo));
					}
					return false;
				}
			}
			else
			{
				if (fieldType.IsValueType)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError(string.Format("AccessTools2.ValidateFieldType<{0}>: FieldRefAccess return type must be the same as FieldType for value types, fieldInfo '{1}'", typeof(F).FullName, fieldInfo));
					}
					return false;
				}
				if (!returnType.IsAssignableFrom(fieldType))
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.ValidateFieldType<" + typeof(F).FullName + ">: FieldRefAccess return type must be assignable from FieldType for reference types");
					}
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x000100E4 File Offset: 0x0000E2E4
		[NullableContext(2)]
		private static bool ValidateStructField<[Nullable(0)] T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			if (fieldInfo == null)
			{
				return false;
			}
			if (fieldInfo.IsStatic)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Concat(new string[]
					{
						"AccessTools2.ValidateStructField<",
						typeof(T).FullName,
						", ",
						typeof(F).FullName,
						">: Field must not be static"
					}));
				}
				return false;
			}
			if (fieldInfo.DeclaringType != typeof(T))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError(string.Concat(new string[]
					{
						"AccessTools2.ValidateStructField<",
						typeof(T).FullName,
						", ",
						typeof(F).FullName,
						">: FieldDeclaringType must be T (StructFieldRefAccess instance type)"
					}));
				}
				return false;
			}
			return true;
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x000101B8 File Offset: 0x0000E3B8
		[NullableContext(2)]
		private static bool TryGetComponents([Nullable(1)] string typeColonName, out Type type, out string name, bool logErrorInTrace = true)
		{
			if (string.IsNullOrWhiteSpace(typeColonName))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.TryGetComponents: 'typeColonName' is null or whitespace/empty");
				}
				type = null;
				name = null;
				return false;
			}
			string[] parts = typeColonName.Split(new char[] { ':' });
			if (parts.Length != 2)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.TryGetComponents: typeColonName '" + typeColonName + "', name must be specified as 'Namespace.Type1.Type2:Name");
				}
				type = null;
				name = null;
				return false;
			}
			type = AccessTools2.TypeByName(parts[0], logErrorInTrace);
			name = parts[1];
			return type != null;
		}

		// Token: 0x04000155 RID: 341
		private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
		{
			typeof(long),
			typeof(ulong),
			typeof(int),
			typeof(uint),
			typeof(short),
			typeof(ushort),
			typeof(byte),
			typeof(sbyte)
		};

		// Token: 0x020000EF RID: 239
		[Nullable(0)]
		[ExcludeFromCodeCoverage]
		private readonly struct DynamicMethodDefinitionHandle
		{
			// Token: 0x06000696 RID: 1686 RVA: 0x00016494 File Offset: 0x00014694
			public static AccessTools2.DynamicMethodDefinitionHandle? Create(string name, Type returnType, Type[] parameterTypes)
			{
				if (AccessTools2.Helper.DynamicMethodDefinitionCtor != null)
				{
					return new AccessTools2.DynamicMethodDefinitionHandle?(new AccessTools2.DynamicMethodDefinitionHandle(AccessTools2.Helper.DynamicMethodDefinitionCtor(name, returnType, parameterTypes)));
				}
				return null;
			}

			// Token: 0x06000697 RID: 1687 RVA: 0x000164C9 File Offset: 0x000146C9
			public DynamicMethodDefinitionHandle(object dynamicMethodDefinition)
			{
				this._dynamicMethodDefinition = dynamicMethodDefinition;
			}

			// Token: 0x06000698 RID: 1688 RVA: 0x000164D4 File Offset: 0x000146D4
			public AccessTools2.ILGeneratorHandle? GetILGenerator()
			{
				if (AccessTools2.Helper.GetILGenerator != null)
				{
					return new AccessTools2.ILGeneratorHandle?(new AccessTools2.ILGeneratorHandle(AccessTools2.Helper.GetILGenerator(this._dynamicMethodDefinition)));
				}
				return null;
			}

			// Token: 0x06000699 RID: 1689 RVA: 0x0001650C File Offset: 0x0001470C
			[NullableContext(2)]
			public MethodInfo Generate()
			{
				if (AccessTools2.Helper.Generate != null)
				{
					return AccessTools2.Helper.Generate(this._dynamicMethodDefinition);
				}
				return null;
			}

			// Token: 0x040002C7 RID: 711
			private readonly object _dynamicMethodDefinition;
		}

		// Token: 0x020000F0 RID: 240
		[Nullable(0)]
		[ExcludeFromCodeCoverage]
		private readonly struct ILGeneratorHandle
		{
			// Token: 0x0600069A RID: 1690 RVA: 0x00016527 File Offset: 0x00014727
			public ILGeneratorHandle(object ilGenerator)
			{
				this._ilGenerator = ilGenerator;
			}

			// Token: 0x0600069B RID: 1691 RVA: 0x00016530 File Offset: 0x00014730
			public void Emit(OpCode opcode)
			{
				AccessTools2.Helper.Emit1Delegate emit = AccessTools2.Helper.Emit1;
				if (emit == null)
				{
					return;
				}
				emit(this._ilGenerator, opcode);
			}

			// Token: 0x0600069C RID: 1692 RVA: 0x00016548 File Offset: 0x00014748
			public void Emit(OpCode opcode, FieldInfo field)
			{
				AccessTools2.Helper.Emit2Delegate emit = AccessTools2.Helper.Emit2;
				if (emit == null)
				{
					return;
				}
				emit(this._ilGenerator, opcode, field);
			}

			// Token: 0x0600069D RID: 1693 RVA: 0x00016561 File Offset: 0x00014761
			public void Emit(OpCode opcode, Type type)
			{
				AccessTools2.Helper.Emit3Delegate emit = AccessTools2.Helper.Emit3;
				if (emit == null)
				{
					return;
				}
				emit(this._ilGenerator, opcode, type);
			}

			// Token: 0x040002C8 RID: 712
			private readonly object _ilGenerator;
		}

		// Token: 0x020000F1 RID: 241
		[NullableContext(0)]
		[ExcludeFromCodeCoverage]
		private static class Helper
		{
			// Token: 0x0600069F RID: 1695 RVA: 0x00016678 File Offset: 0x00014878
			public static bool IsValid(bool logErrorInTrace = true)
			{
				if (AccessTools2.Helper.DynamicMethodDefinitionCtor == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: DynamicMethodDefinitionCtor is null");
					}
					return false;
				}
				if (AccessTools2.Helper.GetILGenerator == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: GetILGenerator is null");
					}
					return false;
				}
				if (AccessTools2.Helper.Emit1 == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: Emit1 is null");
					}
					return false;
				}
				if (AccessTools2.Helper.Emit2 == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: Emit2 is null");
					}
					return false;
				}
				if (AccessTools2.Helper.Emit3 == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: Emit3 is null");
					}
					return false;
				}
				if (AccessTools2.Helper.Generate == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: Generate is null");
					}
					return false;
				}
				return true;
			}

			// Token: 0x040002C9 RID: 713
			[Nullable(2)]
			public static readonly AccessTools2.Helper.DynamicMethodDefinitionCtorDelegate DynamicMethodDefinitionCtor = AccessTools2.GetDeclaredConstructorDelegate<AccessTools2.Helper.DynamicMethodDefinitionCtorDelegate>("MonoMod.Utils.DynamicMethodDefinition", new Type[]
			{
				typeof(string),
				typeof(Type),
				typeof(Type[])
			}, true);

			// Token: 0x040002CA RID: 714
			[Nullable(2)]
			public static readonly AccessTools2.Helper.GetILGeneratorDelegate GetILGenerator = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.GetILGeneratorDelegate>("MonoMod.Utils.DynamicMethodDefinition:GetILGenerator", Type.EmptyTypes, null, true);

			// Token: 0x040002CB RID: 715
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit1Delegate Emit1 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit1Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[] { typeof(OpCode) }, null, true);

			// Token: 0x040002CC RID: 716
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit2Delegate Emit2 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit2Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[]
			{
				typeof(OpCode),
				typeof(FieldInfo)
			}, null, true);

			// Token: 0x040002CD RID: 717
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit3Delegate Emit3 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit3Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[]
			{
				typeof(OpCode),
				typeof(Type)
			}, null, true);

			// Token: 0x040002CE RID: 718
			[Nullable(2)]
			public static readonly AccessTools2.Helper.GenerateDelegate Generate = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.GenerateDelegate>("MonoMod.Utils.DynamicMethodDefinition:Generate", Type.EmptyTypes, null, true);

			// Token: 0x02000103 RID: 259
			// (Invoke) Token: 0x060006CC RID: 1740
			public delegate object DynamicMethodDefinitionCtorDelegate(string name, Type returnType, Type[] parameterTypes);

			// Token: 0x02000104 RID: 260
			// (Invoke) Token: 0x060006D0 RID: 1744
			public delegate object GetILGeneratorDelegate(object instance);

			// Token: 0x02000105 RID: 261
			// (Invoke) Token: 0x060006D4 RID: 1748
			public delegate void Emit1Delegate(object instance, OpCode opcode);

			// Token: 0x02000106 RID: 262
			// (Invoke) Token: 0x060006D8 RID: 1752
			public delegate void Emit2Delegate(object instance, OpCode opcode, FieldInfo field);

			// Token: 0x02000107 RID: 263
			// (Invoke) Token: 0x060006DC RID: 1756
			public delegate void Emit3Delegate(object instance, OpCode opcode, Type type);

			// Token: 0x02000108 RID: 264
			// (Invoke) Token: 0x060006E0 RID: 1760
			public delegate MethodInfo GenerateDelegate(object instance);
		}
	}
}
