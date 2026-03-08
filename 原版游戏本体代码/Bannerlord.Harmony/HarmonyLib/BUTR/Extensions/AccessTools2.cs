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
	// Token: 0x0200003E RID: 62
	[NullableContext(1)]
	[Nullable(0)]
	internal static class AccessTools2
	{
		// Token: 0x06000342 RID: 834 RVA: 0x0000B9A0 File Offset: 0x00009BA0
		[return: Nullable(2)]
		public static ConstructorInfo DeclaredConstructor(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			ConstructorInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredConstructor: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = parameters == null;
				if (flag2)
				{
					parameters = Type.EmptyTypes;
				}
				BindingFlags flags = (searchForStatic ? (AccessTools.allDeclared & ~BindingFlags.Instance) : (AccessTools.allDeclared & ~BindingFlags.Static));
				result = type.GetConstructor(flags, null, parameters, new ParameterModifier[0]);
			}
			return result;
		}

		// Token: 0x06000343 RID: 835 RVA: 0x0000BA08 File Offset: 0x00009C08
		[return: Nullable(2)]
		public static ConstructorInfo Constructor(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			ConstructorInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.ConstructorInfo: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = parameters == null;
				if (flag2)
				{
					parameters = Type.EmptyTypes;
				}
				BindingFlags flags = (searchForStatic ? (AccessTools.all & ~BindingFlags.Instance) : (AccessTools.all & ~BindingFlags.Static));
				result = AccessTools2.FindIncludingBaseTypes<ConstructorInfo>(type, (Type t) => t.GetConstructor(flags, null, parameters, new ParameterModifier[0]));
			}
			return result;
		}

		// Token: 0x06000344 RID: 836 RVA: 0x0000BA8C File Offset: 0x00009C8C
		[return: Nullable(2)]
		public static ConstructorInfo DeclaredConstructor(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			bool flag = string.IsNullOrWhiteSpace(typeString);
			ConstructorInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
				}
				result = null;
			}
			else
			{
				Type type = AccessTools2.TypeByName(typeString, logErrorInTrace);
				bool flag2 = type == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					result = AccessTools2.DeclaredConstructor(type, parameters, searchForStatic, logErrorInTrace);
				}
			}
			return result;
		}

		// Token: 0x06000345 RID: 837 RVA: 0x0000BADC File Offset: 0x00009CDC
		[return: Nullable(2)]
		public static ConstructorInfo Constructor(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
		{
			bool flag = string.IsNullOrWhiteSpace(typeString);
			ConstructorInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
				}
				result = null;
			}
			else
			{
				Type type = AccessTools2.TypeByName(typeString, logErrorInTrace);
				bool flag2 = type == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					result = AccessTools2.Constructor(type, parameters, searchForStatic, logErrorInTrace);
				}
			}
			return result;
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0000BB2C File Offset: 0x00009D2C
		[return: Nullable(2)]
		public static TDelegate GetDeclaredConstructorDelegate<[Nullable(0)] TDelegate>(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(type, parameters, false, logErrorInTrace);
			return (constructorInfo != null) ? AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0000BB58 File Offset: 0x00009D58
		[return: Nullable(2)]
		public static TDelegate GetConstructorDelegate<[Nullable(0)] TDelegate>(Type type, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.Constructor(type, parameters, false, logErrorInTrace);
			return (constructorInfo != null) ? AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000348 RID: 840 RVA: 0x0000BB84 File Offset: 0x00009D84
		[return: Nullable(2)]
		public static TDelegate GetDeclaredConstructorDelegate<[Nullable(0)] TDelegate>(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.DeclaredConstructor(typeString, parameters, false, logErrorInTrace);
			return (constructorInfo != null) ? AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000349 RID: 841 RVA: 0x0000BBB0 File Offset: 0x00009DB0
		[return: Nullable(2)]
		public static TDelegate GetConstructorDelegate<[Nullable(0)] TDelegate>(string typeString, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			ConstructorInfo constructorInfo = AccessTools2.Constructor(typeString, parameters, false, logErrorInTrace);
			return (constructorInfo != null) ? AccessTools2.GetDelegate<TDelegate>(constructorInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034A RID: 842 RVA: 0x0000BBDC File Offset: 0x00009DDC
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x0000BC0C File Offset: 0x00009E0C
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034C RID: 844 RVA: 0x0000BC3C File Offset: 0x00009E3C
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034D RID: 845 RVA: 0x0000BC70 File Offset: 0x00009E70
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = ((propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0000BCA4 File Offset: 0x00009EA4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(type, name, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0000BCD0 File Offset: 0x00009ED0
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(type, name, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000350 RID: 848 RVA: 0x0000BCFC File Offset: 0x00009EFC
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(type, name, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000351 RID: 849 RVA: 0x0000BD28 File Offset: 0x00009F28
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(type, name, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0000BD54 File Offset: 0x00009F54
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(type, method, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000353 RID: 851 RVA: 0x0000BD80 File Offset: 0x00009F80
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(type, method, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000354 RID: 852 RVA: 0x0000BDAC File Offset: 0x00009FAC
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(type, method, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000355 RID: 853 RVA: 0x0000BDD8 File Offset: 0x00009FD8
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(type, method, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000356 RID: 854 RVA: 0x0000BE04 File Offset: 0x0000A004
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000357 RID: 855 RVA: 0x0000BE30 File Offset: 0x0000A030
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0000BE5C File Offset: 0x0000A05C
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000359 RID: 857 RVA: 0x0000BE88 File Offset: 0x0000A088
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600035A RID: 858 RVA: 0x0000BEB4 File Offset: 0x0000A0B4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600035B RID: 859 RVA: 0x0000BEE0 File Offset: 0x0000A0E0
		[return: Nullable(2)]
		public static TDelegate GetDeclaredPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600035C RID: 860 RVA: 0x0000BF0C File Offset: 0x0000A10C
		[return: Nullable(2)]
		public static TDelegate GetPropertyGetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertyGetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600035D RID: 861 RVA: 0x0000BF38 File Offset: 0x0000A138
		[return: Nullable(2)]
		public static TDelegate GetPropertySetterDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.PropertySetter(typeColonPropertyName, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600035E RID: 862 RVA: 0x0000BF64 File Offset: 0x0000A164
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(ConstructorInfo constructorInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			bool flag = constructorInfo == null;
			TDelegate result;
			if (flag)
			{
				result = default(TDelegate);
			}
			else
			{
				MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
				bool flag2 = delegateInvoke == null;
				if (flag2)
				{
					result = default(TDelegate);
				}
				else
				{
					bool flag3 = !delegateInvoke.ReturnType.IsAssignableFrom(constructorInfo.DeclaringType);
					if (flag3)
					{
						result = default(TDelegate);
					}
					else
					{
						ParameterInfo[] delegateParameters = delegateInvoke.GetParameters();
						ParameterInfo[] constructorParameters = constructorInfo.GetParameters();
						bool flag4 = delegateParameters.Length - constructorParameters.Length != 0 && !AccessTools2.ParametersAreEqual(delegateParameters, constructorParameters);
						if (flag4)
						{
							result = default(TDelegate);
						}
						else
						{
							ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
							List<ParameterExpression> returnParameters = delegateParameters.Select(delegate(ParameterInfo pi, int i)
							{
								Type parameterType = pi.ParameterType;
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(1, 1);
								defaultInterpolatedStringHandler2.AppendLiteral("p");
								defaultInterpolatedStringHandler2.AppendFormatted<int>(i);
								return Expression.Parameter(parameterType, defaultInterpolatedStringHandler2.ToStringAndClear());
							}).ToList<ParameterExpression>();
							List<Expression> inputParameters = returnParameters.Select(delegate(ParameterExpression pe, int i)
							{
								bool flag5 = pe.IsByRef || pe.Type.Equals(constructorParameters[i].ParameterType);
								Expression result2;
								if (flag5)
								{
									result2 = pe;
								}
								else
								{
									result2 = Expression.Convert(pe, constructorParameters[i].ParameterType);
								}
								return result2;
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
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 2);
									defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.GetDelegate<");
									defaultInterpolatedStringHandler.AppendFormatted(typeof(TDelegate).FullName);
									defaultInterpolatedStringHandler.AppendLiteral(">: Error while compiling lambds expression '");
									defaultInterpolatedStringHandler.AppendFormatted<Exception>(ex);
									defaultInterpolatedStringHandler.AppendLiteral("'");
									Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
								}
								result = default(TDelegate);
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600035F RID: 863 RVA: 0x0000C14C File Offset: 0x0000A34C
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			bool flag = methodInfo == null;
			TDelegate result;
			if (flag)
			{
				result = default(TDelegate);
			}
			else
			{
				MethodInfo delegateInvoke = typeof(TDelegate).GetMethod("Invoke");
				bool flag2 = delegateInvoke == null;
				if (flag2)
				{
					result = default(TDelegate);
				}
				else
				{
					bool areEnums = delegateInvoke.ReturnType.IsEnum || methodInfo.ReturnType.IsEnum;
					bool areNumeric = delegateInvoke.ReturnType.IsNumeric() || methodInfo.ReturnType.IsNumeric();
					bool flag3 = !areEnums && !areNumeric && !delegateInvoke.ReturnType.IsAssignableFrom(methodInfo.ReturnType);
					if (flag3)
					{
						result = default(TDelegate);
					}
					else
					{
						ParameterInfo[] delegateParameters = delegateInvoke.GetParameters();
						ParameterInfo[] methodParameters = methodInfo.GetParameters();
						bool hasSameParameters = delegateParameters.Length - methodParameters.Length == 0 && AccessTools2.ParametersAreEqual(delegateParameters, methodParameters);
						bool hasInstance = instance != null;
						bool hasInstanceType = delegateParameters.Length - methodParameters.Length == 1 && (delegateParameters[0].ParameterType.IsAssignableFrom(methodInfo.DeclaringType) || methodInfo.DeclaringType.IsAssignableFrom(delegateParameters[0].ParameterType));
						bool flag4 = !hasInstance && !hasInstanceType && !methodInfo.IsStatic;
						if (flag4)
						{
							result = default(TDelegate);
						}
						else
						{
							bool flag5 = hasInstance && methodInfo.IsStatic;
							if (flag5)
							{
								result = default(TDelegate);
							}
							else
							{
								bool flag6 = hasInstance && !methodInfo.IsStatic && !methodInfo.DeclaringType.IsAssignableFrom(instance.GetType());
								if (flag6)
								{
									result = default(TDelegate);
								}
								else
								{
									bool flag7 = hasSameParameters && hasInstanceType;
									if (flag7)
									{
										result = default(TDelegate);
									}
									else
									{
										bool flag8 = hasInstance && (hasInstanceType || !hasSameParameters);
										if (flag8)
										{
											result = default(TDelegate);
										}
										else
										{
											bool flag9 = hasInstanceType && (hasInstance || hasSameParameters);
											if (flag9)
											{
												result = default(TDelegate);
											}
											else
											{
												bool flag10 = !hasInstanceType && !hasInstance && !hasSameParameters;
												if (flag10)
												{
													result = default(TDelegate);
												}
												else
												{
													ParameterExpression instanceParameter = (hasInstanceType ? Expression.Parameter(delegateParameters[0].ParameterType, "instance") : null);
													List<ParameterExpression> returnParameters = delegateParameters.Skip(hasInstanceType ? 1 : 0).Select(delegate(ParameterInfo pi, int i)
													{
														Type parameterType = pi.ParameterType;
														DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(1, 1);
														defaultInterpolatedStringHandler2.AppendLiteral("p");
														defaultInterpolatedStringHandler2.AppendFormatted<int>(i);
														return Expression.Parameter(parameterType, defaultInterpolatedStringHandler2.ToStringAndClear());
													}).ToList<ParameterExpression>();
													List<Expression> inputParameters = returnParameters.Select(delegate(ParameterExpression pe, int i)
													{
														bool flag12 = pe.IsByRef || pe.Type.Equals(methodParameters[i].ParameterType);
														Expression result2;
														if (flag12)
														{
															result2 = pe;
														}
														else
														{
															result2 = Expression.Convert(pe, methodParameters[i].ParameterType);
														}
														return result2;
													}).ToList<Expression>();
													MethodCallExpression call = (hasInstance ? (instance.GetType().Equals(methodInfo.DeclaringType) ? Expression.Call(Expression.Constant(instance), methodInfo, inputParameters) : Expression.Call(Expression.Convert(Expression.Constant(instance), instance.GetType()), methodInfo, inputParameters)) : (hasSameParameters ? Expression.Call(methodInfo, inputParameters) : (hasInstanceType ? (instanceParameter.Type.Equals(methodInfo.DeclaringType) ? Expression.Call(instanceParameter, methodInfo, inputParameters) : Expression.Call(Expression.Convert(instanceParameter, methodInfo.DeclaringType), methodInfo, inputParameters)) : null)));
													bool flag11 = call == null;
													if (flag11)
													{
														result = default(TDelegate);
													}
													else
													{
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
																DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 2);
																defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.GetDelegate<");
																defaultInterpolatedStringHandler.AppendFormatted(typeof(TDelegate).FullName);
																defaultInterpolatedStringHandler.AppendLiteral(">: Error while compiling lambds expression '");
																defaultInterpolatedStringHandler.AppendFormatted<Exception>(ex);
																defaultInterpolatedStringHandler.AppendLiteral("'");
																Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
															}
															result = default(TDelegate);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000360 RID: 864 RVA: 0x0000C594 File Offset: 0x0000A794
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(null, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000361 RID: 865 RVA: 0x0000C59E File Offset: 0x0000A79E
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
		}

		// Token: 0x06000362 RID: 866 RVA: 0x0000C5A7 File Offset: 0x0000A7A7
		public static bool IsNumeric(this Type myType)
		{
			return AccessTools2.NumericTypes.Contains(myType);
		}

		// Token: 0x06000363 RID: 867 RVA: 0x0000C5B4 File Offset: 0x0000A7B4
		private static bool ParametersAreEqual(ParameterInfo[] delegateParameters, ParameterInfo[] methodParameters)
		{
			bool flag = delegateParameters.Length - methodParameters.Length == 0;
			bool result;
			if (flag)
			{
				for (int i = 0; i < methodParameters.Length; i++)
				{
					bool flag2 = delegateParameters[i].ParameterType.IsByRef != methodParameters[i].ParameterType.IsByRef;
					if (flag2)
					{
						return false;
					}
					bool areEnums = delegateParameters[i].ParameterType.IsEnum || methodParameters[i].ParameterType.IsEnum;
					bool areNumeric = delegateParameters[i].ParameterType.IsNumeric() || methodParameters[i].ParameterType.IsNumeric();
					bool flag3 = !areEnums && !areNumeric && !delegateParameters[i].ParameterType.IsAssignableFrom(methodParameters[i].ParameterType);
					if (flag3)
					{
						return false;
					}
				}
				result = true;
			}
			else
			{
				bool flag4 = delegateParameters.Length - methodParameters.Length == 1;
				if (flag4)
				{
					for (int j = 0; j < methodParameters.Length; j++)
					{
						bool flag5 = delegateParameters[j + 1].ParameterType.IsByRef != methodParameters[j].ParameterType.IsByRef;
						if (flag5)
						{
							return false;
						}
						bool areEnums2 = delegateParameters[j + 1].ParameterType.IsEnum || methodParameters[j].ParameterType.IsEnum;
						bool areNumeric2 = delegateParameters[j + 1].ParameterType.IsNumeric() || methodParameters[j].ParameterType.IsNumeric();
						bool flag6 = !areEnums2 && !areNumeric2 && !delegateParameters[j + 1].ParameterType.IsAssignableFrom(methodParameters[j].ParameterType);
						if (flag6)
						{
							return false;
						}
					}
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000364 RID: 868 RVA: 0x0000C782 File Offset: 0x0000A982
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate, [Nullable(2)] TInstance>(TInstance instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			return AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
		}

		// Token: 0x06000365 RID: 869 RVA: 0x0000C794 File Offset: 0x0000A994
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegateObjectInstance<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000366 RID: 870 RVA: 0x0000C7C4 File Offset: 0x0000A9C4
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x0000C7F4 File Offset: 0x0000A9F4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegateObjectInstance<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000368 RID: 872 RVA: 0x0000C820 File Offset: 0x0000AA20
		[return: Nullable(2)]
		public static TDelegate GetDelegateObjectInstance<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000369 RID: 873 RVA: 0x0000C84C File Offset: 0x0000AA4C
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600036A RID: 874 RVA: 0x0000C87C File Offset: 0x0000AA7C
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600036B RID: 875 RVA: 0x0000C8AC File Offset: 0x0000AAAC
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600036C RID: 876 RVA: 0x0000C8D8 File Offset: 0x0000AAD8
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>(string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x0600036D RID: 877 RVA: 0x0000C904 File Offset: 0x0000AB04
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

		// Token: 0x0600036E RID: 878 RVA: 0x0000C948 File Offset: 0x0000AB48
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

		// Token: 0x0600036F RID: 879 RVA: 0x0000C98C File Offset: 0x0000AB8C
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000370 RID: 880 RVA: 0x0000C9C0 File Offset: 0x0000ABC0
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, Type type, string method, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(type, method, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000371 RID: 881 RVA: 0x0000C9F4 File Offset: 0x0000ABF4
		[return: Nullable(2)]
		public static TDelegate GetDeclaredDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000372 RID: 882 RVA: 0x0000CA24 File Offset: 0x0000AC24
		[return: Nullable(2)]
		public static TDelegate GetDelegate<[Nullable(0)] TDelegate>([Nullable(2)] object instance, string typeSemicolonMethod, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
		{
			MethodInfo methodInfo = AccessTools2.Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
			return (methodInfo != null) ? AccessTools2.GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace) : default(TDelegate);
		}

		// Token: 0x06000373 RID: 883 RVA: 0x0000CA54 File Offset: 0x0000AC54
		[return: Nullable(2)]
		public static FieldInfo DeclaredField(Type type, string name, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			FieldInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredField: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.DeclaredField: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					FieldInfo fieldInfo = type.GetField(name, AccessTools.allDeclared);
					bool flag3 = fieldInfo == null;
					if (flag3)
					{
						if (logErrorInTrace)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(72, 2);
							defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.DeclaredField: Could not find field for type '");
							defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
							defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
							defaultInterpolatedStringHandler2.AppendFormatted(name);
							defaultInterpolatedStringHandler2.AppendLiteral("'");
							Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
						}
						result = null;
					}
					else
					{
						result = fieldInfo;
					}
				}
			}
			return result;
		}

		// Token: 0x06000374 RID: 884 RVA: 0x0000CB4C File Offset: 0x0000AD4C
		[return: Nullable(2)]
		public static FieldInfo Field(Type type, string name, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			FieldInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.Field: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					FieldInfo fieldInfo = AccessTools2.FindIncludingBaseTypes<FieldInfo>(type, (Type t) => t.GetField(name, AccessTools.all));
					bool flag3 = fieldInfo == null && logErrorInTrace;
					if (flag3)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(64, 2);
						defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.Field: Could not find field for type '");
						defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
						defaultInterpolatedStringHandler2.AppendFormatted(name);
						defaultInterpolatedStringHandler2.AppendLiteral("'");
						Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
					}
					result = fieldInfo;
				}
			}
			return result;
		}

		// Token: 0x06000375 RID: 885 RVA: 0x0000CC5C File Offset: 0x0000AE5C
		[return: Nullable(2)]
		public static FieldInfo DeclaredField(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace);
			FieldInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.DeclaredField(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0000CCAC File Offset: 0x0000AEAC
		[return: Nullable(2)]
		public static FieldInfo Field(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace);
			FieldInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.Field(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000377 RID: 887 RVA: 0x0000CCFC File Offset: 0x0000AEFC
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, F> FieldRefAccess<[Nullable(2)] F>(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace);
			AccessTools.FieldRef<object, F> result;
			if (flag)
			{
				Trace.TraceError("AccessTools2.FieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
				result = null;
			}
			else
			{
				result = AccessTools2.FieldRefAccess<F>(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000378 RID: 888 RVA: 0x0000CD44 File Offset: 0x0000AF44
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, [Nullable(2)] F>(string fieldName, bool logErrorInTrace = true) where T : class
		{
			bool flag = fieldName == null;
			AccessTools.FieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				FieldInfo field = AccessTools2.GetInstanceField(typeof(T), fieldName, logErrorInTrace);
				bool flag2 = field == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					result = AccessTools2.FieldRefAccessInternal<T, F>(field, false, logErrorInTrace);
				}
			}
			return result;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x0000CD88 File Offset: 0x0000AF88
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, F> FieldRefAccess<[Nullable(2)] F>(Type type, string fieldName, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			AccessTools.FieldRef<object, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = fieldName == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
					bool flag3 = fieldInfo == null;
					if (flag3)
					{
						result = null;
					}
					else
					{
						Type declaringType;
						bool flag4;
						if (!fieldInfo.IsStatic)
						{
							declaringType = fieldInfo.DeclaringType;
							flag4 = declaringType != null;
						}
						else
						{
							flag4 = false;
						}
						bool flag5 = flag4;
						if (flag5)
						{
							bool isValueType = declaringType.IsValueType;
							if (isValueType)
							{
								if (logErrorInTrace)
								{
									Trace.TraceError("AccessTools2.FieldRefAccess<object, " + typeof(F).FullName + ">: FieldDeclaringType must be a class");
								}
								result = null;
							}
							else
							{
								result = AccessTools2.FieldRefAccessInternal<object, F>(fieldInfo, true, logErrorInTrace);
							}
						}
						else
						{
							result = null;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600037A RID: 890 RVA: 0x0000CE34 File Offset: 0x0000B034
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, [Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : class
		{
			bool flag = fieldInfo == null;
			AccessTools.FieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				Type declaringType;
				bool flag2;
				if (!fieldInfo.IsStatic)
				{
					declaringType = fieldInfo.DeclaringType;
					flag2 = declaringType != null;
				}
				else
				{
					flag2 = false;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					bool isValueType = declaringType.IsValueType;
					if (isValueType)
					{
						if (logErrorInTrace)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
							defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.FieldRefAccess<");
							defaultInterpolatedStringHandler.AppendFormatted(typeof(T).FullName);
							defaultInterpolatedStringHandler.AppendLiteral(", ");
							defaultInterpolatedStringHandler.AppendFormatted(typeof(F).FullName);
							defaultInterpolatedStringHandler.AppendLiteral(">: FieldDeclaringType must be a class");
							Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
						}
						result = null;
					}
					else
					{
						bool? flag4 = AccessTools2.FieldRefNeedsClasscast(typeof(T), declaringType, logErrorInTrace);
						bool needCastclass;
						int num;
						if (flag4 != null)
						{
							needCastclass = flag4.GetValueOrDefault();
							num = 1;
						}
						else
						{
							num = 0;
						}
						bool flag5 = num == 0;
						if (flag5)
						{
							result = null;
						}
						else
						{
							result = AccessTools2.FieldRefAccessInternal<T, F>(fieldInfo, needCastclass, logErrorInTrace);
						}
					}
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x0600037B RID: 891 RVA: 0x0000CF3C File Offset: 0x0000B13C
		[return: Nullable(new byte[] { 2, 1, 1 })]
		private static AccessTools.FieldRef<T, F> FieldRefAccessInternal<T, [Nullable(2)] F>(FieldInfo fieldInfo, bool needCastclass, bool logErrorInTrace = true) where T : class
		{
			bool flag = !AccessTools2.Helper.IsValid(logErrorInTrace);
			AccessTools.FieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool isStatic = fieldInfo.IsStatic;
				if (isStatic)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.FieldRefAccessInternal<");
						defaultInterpolatedStringHandler.AppendFormatted(typeof(T).FullName);
						defaultInterpolatedStringHandler.AppendLiteral(", ");
						defaultInterpolatedStringHandler.AppendFormatted(typeof(F).FullName);
						defaultInterpolatedStringHandler.AppendLiteral(">: Field must not be static");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					bool flag2 = !AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace);
					if (flag2)
					{
						result = null;
					}
					else
					{
						Type delegateInstanceType = typeof(T);
						Type declaringType = fieldInfo.DeclaringType;
						AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create("__refget_" + delegateInstanceType.Name + "_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { delegateInstanceType });
						AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
						AccessTools2.ILGeneratorHandle il;
						int num;
						if (ilgeneratorHandle != null)
						{
							il = ilgeneratorHandle.GetValueOrDefault();
							num = 1;
						}
						else
						{
							num = 0;
						}
						bool flag3 = num == 0;
						if (flag3)
						{
							result = null;
						}
						else
						{
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
							result = obj as AccessTools.FieldRef<T, F>;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600037C RID: 892 RVA: 0x0000D114 File Offset: 0x0000B314
		private static bool? FieldRefNeedsClasscast(Type delegateInstanceType, Type declaringType, bool logErrorInTrace = true)
		{
			bool needCastclass = false;
			bool flag = delegateInstanceType != declaringType;
			if (flag)
			{
				needCastclass = delegateInstanceType.IsAssignableFrom(declaringType);
				bool flag2 = !needCastclass && !declaringType.IsAssignableFrom(delegateInstanceType);
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(216, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.FieldRefNeedsClasscast: FieldDeclaringType must be assignable from or to T (FieldRefAccess instance type) - 'instanceOfT is FieldDeclaringType' must be possible, delegateInstanceType '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(delegateInstanceType);
						defaultInterpolatedStringHandler.AppendLiteral("', declaringType '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(declaringType);
						defaultInterpolatedStringHandler.AppendLiteral("'");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					return null;
				}
			}
			return new bool?(needCastclass);
		}

		// Token: 0x0600037D RID: 893 RVA: 0x0000D1C3 File Offset: 0x0000B3C3
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> FieldRefAccess<[Nullable(2)] TField>(FieldInfo fieldInfo)
		{
			return (fieldInfo == null) ? null : AccessTools.FieldRefAccess<object, TField>(fieldInfo);
		}

		// Token: 0x0600037E RID: 894 RVA: 0x0000D1D4 File Offset: 0x0000B3D4
		[return: Nullable(2)]
		public static MethodInfo DeclaredMethod(Type type, string name, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			MethodInfo result2;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredMethod: 'type' is null");
				}
				result2 = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.DeclaredMethod: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result2 = null;
				}
				else
				{
					bool flag3 = parameters == null;
					MethodInfo result;
					if (flag3)
					{
						try
						{
							result = type.GetMethod(name, AccessTools.allDeclared);
						}
						catch (AmbiguousMatchException ex)
						{
							result = type.GetMethod(name, AccessTools.allDeclared, null, Type.EmptyTypes, new ParameterModifier[0]);
							bool flag4 = result == null;
							if (flag4)
							{
								if (logErrorInTrace)
								{
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(90, 4);
									defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.DeclaredMethod: Ambiguous match for type '");
									defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
									defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
									defaultInterpolatedStringHandler2.AppendFormatted(name);
									defaultInterpolatedStringHandler2.AppendLiteral("' and parameters '");
									defaultInterpolatedStringHandler2.AppendFormatted((parameters != null) ? parameters.Description() : null);
									defaultInterpolatedStringHandler2.AppendLiteral("', '");
									defaultInterpolatedStringHandler2.AppendFormatted<AmbiguousMatchException>(ex);
									defaultInterpolatedStringHandler2.AppendLiteral("'");
									Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
								}
								return null;
							}
						}
					}
					else
					{
						result = type.GetMethod(name, AccessTools.allDeclared, null, parameters, new ParameterModifier[0]);
					}
					bool flag5 = result == null;
					if (flag5)
					{
						if (logErrorInTrace)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(92, 3);
							defaultInterpolatedStringHandler3.AppendLiteral("AccessTools2.DeclaredMethod: Could not find method for type '");
							defaultInterpolatedStringHandler3.AppendFormatted<Type>(type);
							defaultInterpolatedStringHandler3.AppendLiteral("' and name '");
							defaultInterpolatedStringHandler3.AppendFormatted(name);
							defaultInterpolatedStringHandler3.AppendLiteral("' and parameters '");
							defaultInterpolatedStringHandler3.AppendFormatted((parameters != null) ? parameters.Description() : null);
							defaultInterpolatedStringHandler3.AppendLiteral("'");
							Trace.TraceError(defaultInterpolatedStringHandler3.ToStringAndClear());
						}
						result2 = null;
					}
					else
					{
						bool flag6 = generics != null;
						if (flag6)
						{
							result = result.MakeGenericMethod(generics);
						}
						result2 = result;
					}
				}
			}
			return result2;
		}

		// Token: 0x0600037F RID: 895 RVA: 0x0000D40C File Offset: 0x0000B60C
		[return: Nullable(2)]
		public static MethodInfo Method(Type type, string name, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			MethodInfo result2;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: 'type' is null");
				}
				result2 = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.Method: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result2 = null;
				}
				else
				{
					bool flag3 = parameters == null;
					MethodInfo result;
					if (flag3)
					{
						try
						{
							result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all));
						}
						catch (AmbiguousMatchException ex)
						{
							result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, Type.EmptyTypes, new ParameterModifier[0]));
							bool flag4 = result == null;
							if (flag4)
							{
								if (logErrorInTrace)
								{
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(82, 4);
									defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.Method: Ambiguous match for type '");
									defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
									defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
									defaultInterpolatedStringHandler2.AppendFormatted(name);
									defaultInterpolatedStringHandler2.AppendLiteral("' and parameters '");
									Type[] parameters2 = parameters;
									defaultInterpolatedStringHandler2.AppendFormatted((parameters2 != null) ? parameters2.Description() : null);
									defaultInterpolatedStringHandler2.AppendLiteral("', '");
									defaultInterpolatedStringHandler2.AppendFormatted<AmbiguousMatchException>(ex);
									defaultInterpolatedStringHandler2.AppendLiteral("'");
									Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
								}
								return null;
							}
						}
					}
					else
					{
						result = AccessTools2.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, parameters, new ParameterModifier[0]));
					}
					bool flag5 = result == null;
					if (flag5)
					{
						if (logErrorInTrace)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(84, 3);
							defaultInterpolatedStringHandler3.AppendLiteral("AccessTools2.Method: Could not find method for type '");
							defaultInterpolatedStringHandler3.AppendFormatted<Type>(type);
							defaultInterpolatedStringHandler3.AppendLiteral("' and name '");
							defaultInterpolatedStringHandler3.AppendFormatted(name);
							defaultInterpolatedStringHandler3.AppendLiteral("' and parameters '");
							Type[] parameters3 = parameters;
							defaultInterpolatedStringHandler3.AppendFormatted((parameters3 != null) ? parameters3.Description() : null);
							defaultInterpolatedStringHandler3.AppendLiteral("'");
							Trace.TraceError(defaultInterpolatedStringHandler3.ToStringAndClear());
						}
						result2 = null;
					}
					else
					{
						bool flag6 = generics != null;
						if (flag6)
						{
							result = result.MakeGenericMethod(generics);
						}
						result2 = result;
					}
				}
			}
			return result2;
		}

		// Token: 0x06000380 RID: 896 RVA: 0x0000D67C File Offset: 0x0000B87C
		[return: Nullable(2)]
		public static MethodInfo DeclaredMethod(string typeColonMethodname, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonMethodname, out type, out name, logErrorInTrace);
			MethodInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.DeclaredMethod(type, name, parameters, generics, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000381 RID: 897 RVA: 0x0000D6D0 File Offset: 0x0000B8D0
		[return: Nullable(2)]
		public static MethodInfo Method(string typeColonMethodname, [Nullable(new byte[] { 2, 1 })] Type[] parameters = null, [Nullable(new byte[] { 2, 1 })] Type[] generics = null, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonMethodname, out type, out name, logErrorInTrace);
			MethodInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.Method(type, name, parameters, generics, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000382 RID: 898 RVA: 0x0000D724 File Offset: 0x0000B924
		[return: Nullable(2)]
		public static PropertyInfo DeclaredProperty(Type type, string name, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			PropertyInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredProperty: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.DeclaredProperty: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					PropertyInfo property = type.GetProperty(name, AccessTools.allDeclared);
					bool flag3 = property == null && logErrorInTrace;
					if (flag3)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(78, 2);
						defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.DeclaredProperty: Could not find property for type '");
						defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
						defaultInterpolatedStringHandler2.AppendFormatted(name);
						defaultInterpolatedStringHandler2.AppendLiteral("'");
						Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
					}
					result = property;
				}
			}
			return result;
		}

		// Token: 0x06000383 RID: 899 RVA: 0x0000D810 File Offset: 0x0000BA10
		[return: Nullable(2)]
		public static PropertyInfo Property(Type type, string name, bool logErrorInTrace = true)
		{
			bool flag = type == null;
			PropertyInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Property: 'type' is null");
				}
				result = null;
			}
			else
			{
				bool flag2 = name == null;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.Property: type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', 'name' is null");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					PropertyInfo property = AccessTools2.FindIncludingBaseTypes<PropertyInfo>(type, (Type t) => t.GetProperty(name, AccessTools.all));
					bool flag3 = property == null && logErrorInTrace;
					if (flag3)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(70, 2);
						defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.Property: Could not find property for type '");
						defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler2.AppendLiteral("' and name '");
						defaultInterpolatedStringHandler2.AppendFormatted(name);
						defaultInterpolatedStringHandler2.AppendLiteral("'");
						Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
					}
					result = property;
				}
			}
			return result;
		}

		// Token: 0x06000384 RID: 900 RVA: 0x0000D91D File Offset: 0x0000BB1D
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertyGetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null;
		}

		// Token: 0x06000385 RID: 901 RVA: 0x0000D934 File Offset: 0x0000BB34
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertySetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null;
		}

		// Token: 0x06000386 RID: 902 RVA: 0x0000D94B File Offset: 0x0000BB4B
		[return: Nullable(2)]
		public static MethodInfo PropertyGetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(type, name, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null;
		}

		// Token: 0x06000387 RID: 903 RVA: 0x0000D962 File Offset: 0x0000BB62
		[return: Nullable(2)]
		public static MethodInfo PropertySetter(Type type, string name, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(type, name, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null;
		}

		// Token: 0x06000388 RID: 904 RVA: 0x0000D97C File Offset: 0x0000BB7C
		[return: Nullable(2)]
		public static PropertyInfo DeclaredProperty(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonPropertyName, out type, out name, logErrorInTrace);
			PropertyInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.DeclaredProperty: Could not find type or property for '" + typeColonPropertyName + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.DeclaredProperty(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000389 RID: 905 RVA: 0x0000D9CC File Offset: 0x0000BBCC
		[return: Nullable(2)]
		public static PropertyInfo Property(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonPropertyName, out type, out name, logErrorInTrace);
			PropertyInfo result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Property: Could not find type or property for '" + typeColonPropertyName + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.Property(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0000DA1B File Offset: 0x0000BC1B
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(typeColonPropertyName, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null;
		}

		// Token: 0x0600038B RID: 907 RVA: 0x0000DA31 File Offset: 0x0000BC31
		[return: Nullable(2)]
		public static MethodInfo DeclaredPropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.DeclaredProperty(typeColonPropertyName, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0000DA47 File Offset: 0x0000BC47
		[return: Nullable(2)]
		public static MethodInfo PropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(typeColonPropertyName, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x0000DA5D File Offset: 0x0000BC5D
		[return: Nullable(2)]
		public static MethodInfo PropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
		{
			PropertyInfo propertyInfo = AccessTools2.Property(typeColonPropertyName, logErrorInTrace);
			return (propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null;
		}

		// Token: 0x0600038E RID: 910 RVA: 0x0000DA74 File Offset: 0x0000BC74
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> StaticFieldRefAccess<[Nullable(2)] TField>(string typeColonFieldname, bool logErrorInTrace = true)
		{
			Type type;
			string name;
			bool flag = !AccessTools2.TryGetComponents(typeColonFieldname, out type, out name, logErrorInTrace);
			AccessTools.FieldRef<TField> result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.StaticFieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
				}
				result = null;
			}
			else
			{
				result = AccessTools2.StaticFieldRefAccess<TField>(type, name, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x0600038F RID: 911 RVA: 0x0000DAC4 File Offset: 0x0000BCC4
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<F> StaticFieldRefAccess<[Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			bool flag = fieldInfo == null;
			AccessTools.FieldRef<F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = AccessTools2.StaticFieldRefAccessInternal<F>(fieldInfo, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000390 RID: 912 RVA: 0x0000DAEC File Offset: 0x0000BCEC
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> StaticFieldRefAccess<[Nullable(2)] TField>(Type type, string fieldName, bool logErrorInTrace = true)
		{
			FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
			bool flag = fieldInfo == null;
			AccessTools.FieldRef<TField> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = AccessTools2.StaticFieldRefAccess<TField>(fieldInfo, logErrorInTrace);
			}
			return result;
		}

		// Token: 0x06000391 RID: 913 RVA: 0x0000DB1C File Offset: 0x0000BD1C
		[return: Nullable(new byte[] { 2, 1 })]
		private static AccessTools.FieldRef<F> StaticFieldRefAccessInternal<[Nullable(2)] F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			bool flag = !AccessTools2.Helper.IsValid(logErrorInTrace);
			AccessTools.FieldRef<F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = !fieldInfo.IsStatic;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.StaticFieldRefAccessInternal<" + typeof(F).FullName + ">: Field must be static");
					}
					result = null;
				}
				else
				{
					bool flag3 = !AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace);
					if (flag3)
					{
						result = null;
					}
					else
					{
						string str = "__refget_";
						Type declaringType = fieldInfo.DeclaringType;
						AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create(str + (((declaringType != null) ? declaringType.Name : null) ?? "null") + "_static_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[0]);
						AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
						AccessTools2.ILGeneratorHandle il;
						int num;
						if (ilgeneratorHandle != null)
						{
							il = ilgeneratorHandle.GetValueOrDefault();
							num = 1;
						}
						else
						{
							num = 0;
						}
						bool flag4 = num == 0;
						if (flag4)
						{
							result = null;
						}
						else
						{
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
							result = obj as AccessTools.FieldRef<F>;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000392 RID: 914 RVA: 0x0000DC84 File Offset: 0x0000BE84
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<T, [Nullable(2)] F>([Nullable(1)] string fieldName, bool logErrorInTrace = true) where T : struct
		{
			bool flag = string.IsNullOrEmpty(fieldName);
			AccessTools.StructFieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				FieldInfo field = AccessTools2.GetInstanceField(typeof(T), fieldName, logErrorInTrace);
				bool flag2 = field == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					result = AccessTools2.StructFieldRefAccessInternal<T, F>(field, logErrorInTrace);
				}
			}
			return result;
		}

		// Token: 0x06000393 RID: 915 RVA: 0x0000DCCC File Offset: 0x0000BECC
		[NullableContext(2)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<[Nullable(0)] T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			bool flag = fieldInfo == null;
			AccessTools.StructFieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = !AccessTools2.ValidateStructField<T, F>(fieldInfo, logErrorInTrace);
				if (flag2)
				{
					result = null;
				}
				else
				{
					result = AccessTools2.StructFieldRefAccessInternal<T, F>(fieldInfo, logErrorInTrace);
				}
			}
			return result;
		}

		// Token: 0x06000394 RID: 916 RVA: 0x0000DD04 File Offset: 0x0000BF04
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		private static AccessTools.StructFieldRef<T, F> StructFieldRefAccessInternal<T, [Nullable(2)] F>([Nullable(1)] FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			bool flag = !AccessTools2.ValidateFieldType<F>(fieldInfo, logErrorInTrace);
			AccessTools.StructFieldRef<T, F> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				AccessTools2.DynamicMethodDefinitionHandle? dm = AccessTools2.DynamicMethodDefinitionHandle.Create("__refget_" + typeof(T).Name + "_struct_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[] { typeof(T).MakeByRefType() });
				AccessTools2.ILGeneratorHandle? ilgeneratorHandle = ((dm != null) ? dm.GetValueOrDefault().GetILGenerator() : null);
				AccessTools2.ILGeneratorHandle il;
				int num;
				if (ilgeneratorHandle != null)
				{
					il = ilgeneratorHandle.GetValueOrDefault();
					num = 1;
				}
				else
				{
					num = 0;
				}
				bool flag2 = num == 0;
				if (flag2)
				{
					result = null;
				}
				else
				{
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
					result = obj as AccessTools.StructFieldRef<T, F>;
				}
			}
			return result;
		}

		// Token: 0x06000395 RID: 917 RVA: 0x0000DE24 File Offset: 0x0000C024
		public static IEnumerable<Assembly> AllAssemblies()
		{
			return from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.FullName.StartsWith("Microsoft.VisualStudio")
				select a;
		}

		// Token: 0x06000396 RID: 918 RVA: 0x0000DE54 File Offset: 0x0000C054
		public static IEnumerable<Type> AllTypes()
		{
			return AccessTools2.AllAssemblies().SelectMany((Assembly a) => AccessTools2.GetTypesFromAssembly(a, true));
		}

		// Token: 0x06000397 RID: 919 RVA: 0x0000DE80 File Offset: 0x0000C080
		public static Type[] GetTypesFromAssembly(Assembly assembly, bool logErrorInTrace = true)
		{
			bool flag = assembly == null;
			Type[] result;
			if (flag)
			{
				result = Type.EmptyTypes;
			}
			else
			{
				try
				{
					result = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.GetTypesFromAssembly: assembly ");
						defaultInterpolatedStringHandler.AppendFormatted<Assembly>(assembly);
						defaultInterpolatedStringHandler.AppendLiteral(" => ");
						defaultInterpolatedStringHandler.AppendFormatted<ReflectionTypeLoadException>(ex);
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = (from type in ex.Types
						where type != null
						select type).ToArray<Type>();
				}
			}
			return result;
		}

		// Token: 0x06000398 RID: 920 RVA: 0x0000DF3C File Offset: 0x0000C13C
		public static Type[] GetTypesFromAssemblyIfValid(Assembly assembly, bool logErrorInTrace = true)
		{
			bool flag = assembly == null;
			Type[] result;
			if (flag)
			{
				result = Type.EmptyTypes;
			}
			else
			{
				try
				{
					result = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.GetTypesFromAssemblyIfValid: assembly ");
						defaultInterpolatedStringHandler.AppendFormatted<Assembly>(assembly);
						defaultInterpolatedStringHandler.AppendLiteral(" => ");
						defaultInterpolatedStringHandler.AppendFormatted<ReflectionTypeLoadException>(ex);
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = Type.EmptyTypes;
				}
			}
			return result;
		}

		// Token: 0x06000399 RID: 921 RVA: 0x0000DFC8 File Offset: 0x0000C1C8
		[return: Nullable(2)]
		public static Type TypeByName(string name, bool logErrorInTrace = true)
		{
			bool flag = string.IsNullOrEmpty(name);
			Type result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.TypeByName: 'name' is null or empty");
				}
				result = null;
			}
			else
			{
				Type type = Type.GetType(name, false);
				bool flag2 = type == null;
				if (flag2)
				{
					type = AccessTools2.AllTypes().FirstOrDefault((Type t) => t.FullName == name);
				}
				bool flag3 = type == null;
				if (flag3)
				{
					type = AccessTools2.AllTypes().FirstOrDefault((Type t) => t.Name == name);
				}
				bool flag4 = type == null && logErrorInTrace;
				if (flag4)
				{
					Trace.TraceError("AccessTools2.TypeByName: Could not find type named '" + name + "'");
				}
				result = type;
			}
			return result;
		}

		// Token: 0x0600039A RID: 922 RVA: 0x0000E088 File Offset: 0x0000C288
		[return: Nullable(2)]
		public static T FindIncludingBaseTypes<T>(Type type, Func<Type, T> func) where T : class
		{
			bool flag = type == null || func == null;
			T result2;
			if (flag)
			{
				result2 = default(T);
			}
			else
			{
				T result;
				for (;;)
				{
					result = func(type);
					bool flag2 = result != null;
					if (flag2)
					{
						break;
					}
					type = type.BaseType;
					bool flag3 = type == null;
					if (flag3)
					{
						goto Block_4;
					}
				}
				return result;
				Block_4:
				result2 = default(T);
			}
			return result2;
		}

		// Token: 0x0600039B RID: 923 RVA: 0x0000E0F4 File Offset: 0x0000C2F4
		[return: Nullable(2)]
		private static FieldInfo GetInstanceField(Type type, string fieldName, bool logErrorInTrace = true)
		{
			FieldInfo fieldInfo = AccessTools2.Field(type, fieldName, logErrorInTrace);
			bool flag = fieldInfo == null;
			FieldInfo result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool isStatic = fieldInfo.IsStatic;
				if (isStatic)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(78, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.GetInstanceField: Field must not be static, type '");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral("', fieldName '");
						defaultInterpolatedStringHandler.AppendFormatted(fieldName);
						defaultInterpolatedStringHandler.AppendLiteral("'");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = null;
				}
				else
				{
					result = fieldInfo;
				}
			}
			return result;
		}

		// Token: 0x0600039C RID: 924 RVA: 0x0000E184 File Offset: 0x0000C384
		[NullableContext(2)]
		private static bool ValidateFieldType<F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
		{
			bool flag = fieldInfo == null;
			bool result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.ValidateFieldType<" + typeof(F).FullName + ">: 'fieldInfo' is null");
				}
				result = false;
			}
			else
			{
				Type returnType = typeof(F);
				Type fieldType = fieldInfo.FieldType;
				bool flag2 = returnType == fieldType;
				if (flag2)
				{
					result = true;
				}
				else
				{
					bool isEnum = fieldType.IsEnum;
					if (isEnum)
					{
						Type underlyingType = Enum.GetUnderlyingType(fieldType);
						bool flag3 = returnType != underlyingType;
						if (flag3)
						{
							if (logErrorInTrace)
							{
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(162, 3);
								defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.ValidateFieldType<");
								defaultInterpolatedStringHandler.AppendFormatted(typeof(F).FullName);
								defaultInterpolatedStringHandler.AppendLiteral(">: FieldRefAccess return type must be the same as FieldType or FieldType's underlying integral type (");
								defaultInterpolatedStringHandler.AppendFormatted<Type>(underlyingType);
								defaultInterpolatedStringHandler.AppendLiteral(") for enum types, fieldInfo '");
								defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
								defaultInterpolatedStringHandler.AppendLiteral("'");
								Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
							}
							return false;
						}
					}
					else
					{
						bool isValueType = fieldType.IsValueType;
						if (isValueType)
						{
							if (logErrorInTrace)
							{
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(120, 2);
								defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.ValidateFieldType<");
								defaultInterpolatedStringHandler2.AppendFormatted(typeof(F).FullName);
								defaultInterpolatedStringHandler2.AppendLiteral(">: FieldRefAccess return type must be the same as FieldType for value types, fieldInfo '");
								defaultInterpolatedStringHandler2.AppendFormatted<FieldInfo>(fieldInfo);
								defaultInterpolatedStringHandler2.AppendLiteral("'");
								Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
							}
							return false;
						}
						bool flag4 = !returnType.IsAssignableFrom(fieldType);
						if (flag4)
						{
							if (logErrorInTrace)
							{
								Trace.TraceError("AccessTools2.ValidateFieldType<" + typeof(F).FullName + ">: FieldRefAccess return type must be assignable from FieldType for reference types");
							}
							return false;
						}
					}
					result = true;
				}
			}
			return result;
		}

		// Token: 0x0600039D RID: 925 RVA: 0x0000E36C File Offset: 0x0000C56C
		[NullableContext(2)]
		private static bool ValidateStructField<[Nullable(0)] T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
		{
			bool flag = fieldInfo == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool isStatic = fieldInfo.IsStatic;
				if (isStatic)
				{
					if (logErrorInTrace)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AccessTools2.ValidateStructField<");
						defaultInterpolatedStringHandler.AppendFormatted(typeof(T).FullName);
						defaultInterpolatedStringHandler.AppendLiteral(", ");
						defaultInterpolatedStringHandler.AppendFormatted(typeof(F).FullName);
						defaultInterpolatedStringHandler.AppendLiteral(">: Field must not be static");
						Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = false;
				}
				else
				{
					bool flag2 = fieldInfo.DeclaringType != typeof(T);
					if (flag2)
					{
						if (logErrorInTrace)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(103, 2);
							defaultInterpolatedStringHandler2.AppendLiteral("AccessTools2.ValidateStructField<");
							defaultInterpolatedStringHandler2.AppendFormatted(typeof(T).FullName);
							defaultInterpolatedStringHandler2.AppendLiteral(", ");
							defaultInterpolatedStringHandler2.AppendFormatted(typeof(F).FullName);
							defaultInterpolatedStringHandler2.AppendLiteral(">: FieldDeclaringType must be T (StructFieldRefAccess instance type)");
							Trace.TraceError(defaultInterpolatedStringHandler2.ToStringAndClear());
						}
						result = false;
					}
					else
					{
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x0600039E RID: 926 RVA: 0x0000E4A4 File Offset: 0x0000C6A4
		[NullableContext(2)]
		private static bool TryGetComponents([Nullable(1)] string typeColonName, out Type type, out string name, bool logErrorInTrace = true)
		{
			bool flag = string.IsNullOrWhiteSpace(typeColonName);
			bool result;
			if (flag)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.TryGetComponents: 'typeColonName' is null or whitespace/empty");
				}
				type = null;
				name = null;
				result = false;
			}
			else
			{
				string[] parts = typeColonName.Split(new char[] { ':' });
				bool flag2 = parts.Length != 2;
				if (flag2)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.TryGetComponents: typeColonName '" + typeColonName + "', name must be specified as 'Namespace.Type1.Type2:Name");
					}
					type = null;
					name = null;
					result = false;
				}
				else
				{
					type = AccessTools2.TypeByName(parts[0], logErrorInTrace);
					name = parts[1];
					result = type != null;
				}
			}
			return result;
		}

		// Token: 0x0400009B RID: 155
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

		// Token: 0x0200008E RID: 142
		[Nullable(0)]
		[ExcludeFromCodeCoverage]
		private readonly struct DynamicMethodDefinitionHandle
		{
			// Token: 0x06000585 RID: 1413 RVA: 0x000152E4 File Offset: 0x000134E4
			public static AccessTools2.DynamicMethodDefinitionHandle? Create(string name, Type returnType, Type[] parameterTypes)
			{
				return (AccessTools2.Helper.DynamicMethodDefinitionCtor == null) ? null : new AccessTools2.DynamicMethodDefinitionHandle?(new AccessTools2.DynamicMethodDefinitionHandle(AccessTools2.Helper.DynamicMethodDefinitionCtor(name, returnType, parameterTypes)));
			}

			// Token: 0x06000586 RID: 1414 RVA: 0x0001531A File Offset: 0x0001351A
			public DynamicMethodDefinitionHandle(object dynamicMethodDefinition)
			{
				this._dynamicMethodDefinition = dynamicMethodDefinition;
			}

			// Token: 0x06000587 RID: 1415 RVA: 0x00015324 File Offset: 0x00013524
			public AccessTools2.ILGeneratorHandle? GetILGenerator()
			{
				return (AccessTools2.Helper.GetILGenerator == null) ? null : new AccessTools2.ILGeneratorHandle?(new AccessTools2.ILGeneratorHandle(AccessTools2.Helper.GetILGenerator(this._dynamicMethodDefinition)));
			}

			// Token: 0x06000588 RID: 1416 RVA: 0x0001535D File Offset: 0x0001355D
			[NullableContext(2)]
			public MethodInfo Generate()
			{
				return (AccessTools2.Helper.Generate == null) ? null : AccessTools2.Helper.Generate(this._dynamicMethodDefinition);
			}

			// Token: 0x0400021A RID: 538
			private readonly object _dynamicMethodDefinition;
		}

		// Token: 0x0200008F RID: 143
		[Nullable(0)]
		[ExcludeFromCodeCoverage]
		private readonly struct ILGeneratorHandle
		{
			// Token: 0x06000589 RID: 1417 RVA: 0x00015379 File Offset: 0x00013579
			public ILGeneratorHandle(object ilGenerator)
			{
				this._ilGenerator = ilGenerator;
			}

			// Token: 0x0600058A RID: 1418 RVA: 0x00015382 File Offset: 0x00013582
			public void Emit(OpCode opcode)
			{
				AccessTools2.Helper.Emit1Delegate emit = AccessTools2.Helper.Emit1;
				if (emit != null)
				{
					emit(this._ilGenerator, opcode);
				}
			}

			// Token: 0x0600058B RID: 1419 RVA: 0x0001539C File Offset: 0x0001359C
			public void Emit(OpCode opcode, FieldInfo field)
			{
				AccessTools2.Helper.Emit2Delegate emit = AccessTools2.Helper.Emit2;
				if (emit != null)
				{
					emit(this._ilGenerator, opcode, field);
				}
			}

			// Token: 0x0600058C RID: 1420 RVA: 0x000153B7 File Offset: 0x000135B7
			public void Emit(OpCode opcode, Type type)
			{
				AccessTools2.Helper.Emit3Delegate emit = AccessTools2.Helper.Emit3;
				if (emit != null)
				{
					emit(this._ilGenerator, opcode, type);
				}
			}

			// Token: 0x0400021B RID: 539
			private readonly object _ilGenerator;
		}

		// Token: 0x02000090 RID: 144
		[NullableContext(0)]
		[ExcludeFromCodeCoverage]
		private static class Helper
		{
			// Token: 0x0600058E RID: 1422 RVA: 0x000154D4 File Offset: 0x000136D4
			public static bool IsValid(bool logErrorInTrace = true)
			{
				bool flag = AccessTools2.Helper.DynamicMethodDefinitionCtor == null;
				bool result;
				if (flag)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.Helper.IsValid: DynamicMethodDefinitionCtor is null");
					}
					result = false;
				}
				else
				{
					bool flag2 = AccessTools2.Helper.GetILGenerator == null;
					if (flag2)
					{
						if (logErrorInTrace)
						{
							Trace.TraceError("AccessTools2.Helper.IsValid: GetILGenerator is null");
						}
						result = false;
					}
					else
					{
						bool flag3 = AccessTools2.Helper.Emit1 == null;
						if (flag3)
						{
							if (logErrorInTrace)
							{
								Trace.TraceError("AccessTools2.Helper.IsValid: Emit1 is null");
							}
							result = false;
						}
						else
						{
							bool flag4 = AccessTools2.Helper.Emit2 == null;
							if (flag4)
							{
								if (logErrorInTrace)
								{
									Trace.TraceError("AccessTools2.Helper.IsValid: Emit2 is null");
								}
								result = false;
							}
							else
							{
								bool flag5 = AccessTools2.Helper.Emit3 == null;
								if (flag5)
								{
									if (logErrorInTrace)
									{
										Trace.TraceError("AccessTools2.Helper.IsValid: Emit3 is null");
									}
									result = false;
								}
								else
								{
									bool flag6 = AccessTools2.Helper.Generate == null;
									if (flag6)
									{
										if (logErrorInTrace)
										{
											Trace.TraceError("AccessTools2.Helper.IsValid: Generate is null");
										}
										result = false;
									}
									else
									{
										result = true;
									}
								}
							}
						}
					}
				}
				return result;
			}

			// Token: 0x0400021C RID: 540
			[Nullable(2)]
			public static readonly AccessTools2.Helper.DynamicMethodDefinitionCtorDelegate DynamicMethodDefinitionCtor = AccessTools2.GetDeclaredConstructorDelegate<AccessTools2.Helper.DynamicMethodDefinitionCtorDelegate>("MonoMod.Utils.DynamicMethodDefinition", new Type[]
			{
				typeof(string),
				typeof(Type),
				typeof(Type[])
			}, true);

			// Token: 0x0400021D RID: 541
			[Nullable(2)]
			public static readonly AccessTools2.Helper.GetILGeneratorDelegate GetILGenerator = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.GetILGeneratorDelegate>("MonoMod.Utils.DynamicMethodDefinition:GetILGenerator", Type.EmptyTypes, null, true);

			// Token: 0x0400021E RID: 542
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit1Delegate Emit1 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit1Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[] { typeof(OpCode) }, null, true);

			// Token: 0x0400021F RID: 543
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit2Delegate Emit2 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit2Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[]
			{
				typeof(OpCode),
				typeof(FieldInfo)
			}, null, true);

			// Token: 0x04000220 RID: 544
			[Nullable(2)]
			public static readonly AccessTools2.Helper.Emit3Delegate Emit3 = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.Emit3Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[]
			{
				typeof(OpCode),
				typeof(Type)
			}, null, true);

			// Token: 0x04000221 RID: 545
			[Nullable(2)]
			public static readonly AccessTools2.Helper.GenerateDelegate Generate = AccessTools2.GetDelegateObjectInstance<AccessTools2.Helper.GenerateDelegate>("MonoMod.Utils.DynamicMethodDefinition:Generate", Type.EmptyTypes, null, true);

			// Token: 0x020000A2 RID: 162
			// (Invoke) Token: 0x060005BB RID: 1467
			public delegate object DynamicMethodDefinitionCtorDelegate(string name, Type returnType, Type[] parameterTypes);

			// Token: 0x020000A3 RID: 163
			// (Invoke) Token: 0x060005BF RID: 1471
			public delegate object GetILGeneratorDelegate(object instance);

			// Token: 0x020000A4 RID: 164
			// (Invoke) Token: 0x060005C3 RID: 1475
			public delegate void Emit1Delegate(object instance, OpCode opcode);

			// Token: 0x020000A5 RID: 165
			// (Invoke) Token: 0x060005C7 RID: 1479
			public delegate void Emit2Delegate(object instance, OpCode opcode, FieldInfo field);

			// Token: 0x020000A6 RID: 166
			// (Invoke) Token: 0x060005CB RID: 1483
			public delegate void Emit3Delegate(object instance, OpCode opcode, Type type);

			// Token: 0x020000A7 RID: 167
			// (Invoke) Token: 0x060005CF RID: 1487
			public delegate MethodInfo GenerateDelegate(object instance);
		}
	}
}
