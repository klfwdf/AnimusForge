#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib.BUTR.Extensions;

internal static class AccessTools2
{
	[ExcludeFromCodeCoverage]
	private readonly struct DynamicMethodDefinitionHandle
	{
		private readonly object _dynamicMethodDefinition;

		public static DynamicMethodDefinitionHandle? Create(string name, Type returnType, Type[] parameterTypes)
		{
			if (Helper.DynamicMethodDefinitionCtor != null)
			{
				return new DynamicMethodDefinitionHandle(Helper.DynamicMethodDefinitionCtor(name, returnType, parameterTypes));
			}
			return null;
		}

		public DynamicMethodDefinitionHandle(object dynamicMethodDefinition)
		{
			_dynamicMethodDefinition = dynamicMethodDefinition;
		}

		public ILGeneratorHandle? GetILGenerator()
		{
			if (Helper.GetILGenerator != null)
			{
				return new ILGeneratorHandle(Helper.GetILGenerator(_dynamicMethodDefinition));
			}
			return null;
		}

		public MethodInfo? Generate()
		{
			if (Helper.Generate != null)
			{
				return Helper.Generate(_dynamicMethodDefinition);
			}
			return null;
		}
	}

	[ExcludeFromCodeCoverage]
	private readonly struct ILGeneratorHandle(object ilGenerator)
	{
		private readonly object _ilGenerator = ilGenerator;

		public void Emit(OpCode opcode)
		{
			Helper.Emit1?.Invoke(_ilGenerator, opcode);
		}

		public void Emit(OpCode opcode, FieldInfo field)
		{
			Helper.Emit2?.Invoke(_ilGenerator, opcode, field);
		}

		public void Emit(OpCode opcode, Type type)
		{
			Helper.Emit3?.Invoke(_ilGenerator, opcode, type);
		}
	}

	[ExcludeFromCodeCoverage]
	private static class Helper
	{
		public delegate object DynamicMethodDefinitionCtorDelegate(string name, Type returnType, Type[] parameterTypes);

		public delegate object GetILGeneratorDelegate(object instance);

		public delegate void Emit1Delegate(object instance, OpCode opcode);

		public delegate void Emit2Delegate(object instance, OpCode opcode, FieldInfo field);

		public delegate void Emit3Delegate(object instance, OpCode opcode, Type type);

		public delegate MethodInfo GenerateDelegate(object instance);

		public static readonly DynamicMethodDefinitionCtorDelegate? DynamicMethodDefinitionCtor;

		public static readonly GetILGeneratorDelegate? GetILGenerator;

		public static readonly Emit1Delegate? Emit1;

		public static readonly Emit2Delegate? Emit2;

		public static readonly Emit3Delegate? Emit3;

		public static readonly GenerateDelegate? Generate;

		static Helper()
		{
			DynamicMethodDefinitionCtor = GetDeclaredConstructorDelegate<DynamicMethodDefinitionCtorDelegate>("MonoMod.Utils.DynamicMethodDefinition", new Type[3]
			{
				typeof(string),
				typeof(Type),
				typeof(Type[])
			});
			GetILGenerator = GetDelegateObjectInstance<GetILGeneratorDelegate>("MonoMod.Utils.DynamicMethodDefinition:GetILGenerator", Type.EmptyTypes);
			Emit1 = GetDelegateObjectInstance<Emit1Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[1] { typeof(OpCode) });
			Emit2 = GetDelegateObjectInstance<Emit2Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[2]
			{
				typeof(OpCode),
				typeof(FieldInfo)
			});
			Emit3 = GetDelegateObjectInstance<Emit3Delegate>("System.Reflection.Emit.ILGenerator:Emit", new Type[2]
			{
				typeof(OpCode),
				typeof(Type)
			});
			Generate = GetDelegateObjectInstance<GenerateDelegate>("MonoMod.Utils.DynamicMethodDefinition:Generate", Type.EmptyTypes);
		}

		public static bool IsValid(bool logErrorInTrace = true)
		{
			if (DynamicMethodDefinitionCtor == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: DynamicMethodDefinitionCtor is null");
				}
				return false;
			}
			if (GetILGenerator == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: GetILGenerator is null");
				}
				return false;
			}
			if (Emit1 == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: Emit1 is null");
				}
				return false;
			}
			if (Emit2 == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: Emit2 is null");
				}
				return false;
			}
			if (Emit3 == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: Emit3 is null");
				}
				return false;
			}
			if (Generate == null)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError("AccessTools2.Helper.IsValid: Generate is null");
				}
				return false;
			}
			return true;
		}
	}

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

	public static ConstructorInfo? DeclaredConstructor(Type type, Type[]? parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
		BindingFlags bindingAttr = (searchForStatic ? (AccessTools.allDeclared & ~BindingFlags.Instance) : (AccessTools.allDeclared & ~BindingFlags.Static));
		return type.GetConstructor(bindingAttr, null, parameters, new ParameterModifier[0]);
	}

	public static ConstructorInfo? Constructor(Type type, Type[]? parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
		return FindIncludingBaseTypes(type, (Type t) => t.GetConstructor(flags, null, parameters, new ParameterModifier[0]));
	}

	public static ConstructorInfo? DeclaredConstructor(string typeString, Type[]? parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
	{
		if (string.IsNullOrWhiteSpace(typeString))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
			}
			return null;
		}
		Type type = TypeByName(typeString, logErrorInTrace);
		if ((object)type == null)
		{
			return null;
		}
		return DeclaredConstructor(type, parameters, searchForStatic, logErrorInTrace);
	}

	public static ConstructorInfo? Constructor(string typeString, Type[]? parameters = null, bool searchForStatic = false, bool logErrorInTrace = true)
	{
		if (string.IsNullOrWhiteSpace(typeString))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Constructor: 'typeString' is null or whitespace/empty");
			}
			return null;
		}
		Type type = TypeByName(typeString, logErrorInTrace);
		if ((object)type == null)
		{
			return null;
		}
		return Constructor(type, parameters, searchForStatic, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredConstructorDelegate<TDelegate>(Type type, Type[]? parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		ConstructorInfo constructorInfo = DeclaredConstructor(type, parameters, searchForStatic: false, logErrorInTrace);
		if ((object)constructorInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
	}

	public static TDelegate? GetConstructorDelegate<TDelegate>(Type type, Type[]? parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		ConstructorInfo constructorInfo = Constructor(type, parameters, searchForStatic: false, logErrorInTrace);
		if ((object)constructorInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredConstructorDelegate<TDelegate>(string typeString, Type[]? parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		ConstructorInfo constructorInfo = DeclaredConstructor(typeString, parameters, searchForStatic: false, logErrorInTrace);
		if ((object)constructorInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
	}

	public static TDelegate? GetConstructorDelegate<TDelegate>(string typeString, Type[]? parameters = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		ConstructorInfo constructorInfo = Constructor(typeString, parameters, searchForStatic: false, logErrorInTrace);
		if ((object)constructorInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(constructorInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = propertyInfo?.GetGetMethod(nonPublic: true);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = propertyInfo?.GetSetMethod(nonPublic: true);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(object? instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = propertyInfo?.GetGetMethod(nonPublic: true);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(object? instance, PropertyInfo propertyInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = propertyInfo?.GetSetMethod(nonPublic: true);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertyGetterDelegate<TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertyGetter(type, name, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertySetterDelegate<TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertySetter(type, name, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertyGetter(type, name, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(Type type, string name, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertySetter(type, name, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertyGetterDelegate<TDelegate>(object? instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertyGetter(type, method, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertySetterDelegate<TDelegate>(object? instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertySetter(type, method, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(object? instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertyGetter(type, method, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(object? instance, Type type, string method, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertySetter(type, method, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertyGetterDelegate<TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertySetterDelegate<TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertyGetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertySetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertyGetterDelegate<TDelegate>(object? instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertyGetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredPropertySetterDelegate<TDelegate>(object? instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredPropertySetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertyGetterDelegate<TDelegate>(object? instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertyGetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetPropertySetterDelegate<TDelegate>(object? instance, string typeColonPropertyName, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = PropertySetter(typeColonPropertyName, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegate<TDelegate>(ConstructorInfo constructorInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		if ((object)constructorInfo == null)
		{
			return null;
		}
		MethodInfo method = typeof(TDelegate).GetMethod("Invoke");
		if ((object)method == null)
		{
			return null;
		}
		if (!method.ReturnType.IsAssignableFrom(constructorInfo.DeclaringType))
		{
			return null;
		}
		ParameterInfo[] parameters = method.GetParameters();
		ParameterInfo[] constructorParameters = constructorInfo.GetParameters();
		if (parameters.Length - constructorParameters.Length != 0 && !ParametersAreEqual(parameters, constructorParameters))
		{
			return null;
		}
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "instance");
		List<ParameterExpression> list = parameters.Select((ParameterInfo pi, int i) => Expression.Parameter(pi.ParameterType, $"p{i}")).ToList();
		List<Expression> arguments = list.Select((ParameterExpression pe, int i) => (pe.IsByRef || pe.Type.Equals(constructorParameters[i].ParameterType)) ? ((Expression)pe) : ((Expression)Expression.Convert(pe, constructorParameters[i].ParameterType))).ToList();
		Expression expression = Expression.New(constructorInfo, arguments);
		UnaryExpression body = Expression.Convert(expression, method.ReturnType);
		try
		{
			return Expression.Lambda<TDelegate>(body, list).Compile();
		}
		catch (Exception value)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.GetDelegate<{typeof(TDelegate).FullName}>: Error while compiling lambds expression '{value}'");
			}
			return null;
		}
	}

	public static TDelegate? GetDelegate<TDelegate>(object? instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		if ((object)methodInfo == null)
		{
			return null;
		}
		MethodInfo method = typeof(TDelegate).GetMethod("Invoke");
		if ((object)method == null)
		{
			return null;
		}
		bool flag = method.ReturnType.IsEnum || methodInfo.ReturnType.IsEnum;
		bool flag2 = method.ReturnType.IsNumeric() || methodInfo.ReturnType.IsNumeric();
		if (!flag && !flag2 && !method.ReturnType.IsAssignableFrom(methodInfo.ReturnType))
		{
			return null;
		}
		ParameterInfo[] parameters = method.GetParameters();
		ParameterInfo[] methodParameters = methodInfo.GetParameters();
		bool flag3 = parameters.Length - methodParameters.Length == 0 && ParametersAreEqual(parameters, methodParameters);
		bool flag4 = instance != null;
		bool flag5 = parameters.Length - methodParameters.Length == 1 && (parameters[0].ParameterType.IsAssignableFrom(methodInfo.DeclaringType) || methodInfo.DeclaringType.IsAssignableFrom(parameters[0].ParameterType));
		if (!flag4 && !flag5 && !methodInfo.IsStatic)
		{
			return null;
		}
		if (flag4 && methodInfo.IsStatic)
		{
			return null;
		}
		if (flag4 && !methodInfo.IsStatic && !methodInfo.DeclaringType.IsAssignableFrom(instance.GetType()))
		{
			return null;
		}
		if (flag3 & flag5)
		{
			return null;
		}
		if (flag4 && (flag5 || !flag3))
		{
			return null;
		}
		if (flag5 && (flag4 | flag3))
		{
			return null;
		}
		if (!flag5 && !flag4 && !flag3)
		{
			return null;
		}
		ParameterExpression parameterExpression = (flag5 ? Expression.Parameter(parameters[0].ParameterType, "instance") : null);
		List<ParameterExpression> list = parameters.Skip(flag5 ? 1 : 0).Select((ParameterInfo pi, int i) => Expression.Parameter(pi.ParameterType, $"p{i}")).ToList();
		List<Expression> arguments = list.Select((ParameterExpression pe, int i) => (pe.IsByRef || pe.Type.Equals(methodParameters[i].ParameterType)) ? ((Expression)pe) : ((Expression)Expression.Convert(pe, methodParameters[i].ParameterType))).ToList();
		MethodCallExpression methodCallExpression = ((!flag4) ? (flag3 ? Expression.Call(methodInfo, arguments) : ((!flag5) ? null : (parameterExpression.Type.Equals(methodInfo.DeclaringType) ? Expression.Call(parameterExpression, methodInfo, arguments) : Expression.Call(Expression.Convert(parameterExpression, methodInfo.DeclaringType), methodInfo, arguments)))) : (instance.GetType().Equals(methodInfo.DeclaringType) ? Expression.Call(Expression.Constant(instance), methodInfo, arguments) : Expression.Call(Expression.Convert(Expression.Constant(instance), instance.GetType()), methodInfo, arguments)));
		if (methodCallExpression == null)
		{
			return null;
		}
		UnaryExpression body = Expression.Convert(methodCallExpression, method.ReturnType);
		try
		{
			IEnumerable<ParameterExpression> parameters2;
			if (!flag5)
			{
				IEnumerable<ParameterExpression> enumerable = list;
				parameters2 = enumerable;
			}
			else
			{
				parameters2 = new List<ParameterExpression> { parameterExpression }.Concat(list);
			}
			return Expression.Lambda<TDelegate>(body, parameters2).Compile();
		}
		catch (Exception value)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.GetDelegate<{typeof(TDelegate).FullName}>: Error while compiling lambds expression '{value}'");
			}
			return null;
		}
	}

	public static TDelegate? GetDelegate<TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		return GetDelegate<TDelegate>(null, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegateObjectInstance<TDelegate>(MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static bool IsNumeric(this Type myType)
	{
		return NumericTypes.Contains(myType);
	}

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
				bool flag = delegateParameters[i].ParameterType.IsEnum || methodParameters[i].ParameterType.IsEnum;
				bool flag2 = delegateParameters[i].ParameterType.IsNumeric() || methodParameters[i].ParameterType.IsNumeric();
				if (!flag && !flag2 && !delegateParameters[i].ParameterType.IsAssignableFrom(methodParameters[i].ParameterType))
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
				bool flag3 = delegateParameters[j + 1].ParameterType.IsEnum || methodParameters[j].ParameterType.IsEnum;
				bool flag4 = delegateParameters[j + 1].ParameterType.IsNumeric() || methodParameters[j].ParameterType.IsNumeric();
				if (!flag3 && !flag4 && !delegateParameters[j + 1].ParameterType.IsAssignableFrom(methodParameters[j].ParameterType))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static TDelegate? GetDelegate<TDelegate, TInstance>(TInstance instance, MethodInfo methodInfo, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegateObjectInstance<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegateObjectInstance<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegateObjectInstance<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegateObjectInstance<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegateObjectInstance<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegate<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegate<TDelegate>(Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegate<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegate<TDelegate>(string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegate<TDelegate, TInstance>(TInstance instance, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		if (instance != null)
		{
			MethodInfo methodInfo = DeclaredMethod(instance.GetType(), method, parameters, generics, logErrorInTrace);
			if ((object)methodInfo != null)
			{
				return GetDelegate<TDelegate, TInstance>(instance, methodInfo, logErrorInTrace);
			}
		}
		return null;
	}

	public static TDelegate? GetDelegate<TDelegate, TInstance>(TInstance instance, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		if (instance != null)
		{
			MethodInfo methodInfo = Method(instance.GetType(), method, parameters, generics, logErrorInTrace);
			if ((object)methodInfo != null)
			{
				return GetDelegate<TDelegate, TInstance>(instance, methodInfo, logErrorInTrace);
			}
		}
		return null;
	}

	public static TDelegate? GetDeclaredDelegate<TDelegate>(object? instance, Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegate<TDelegate>(object? instance, Type type, string method, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(type, method, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDeclaredDelegate<TDelegate>(object? instance, string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = DeclaredMethod(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static TDelegate? GetDelegate<TDelegate>(object? instance, string typeSemicolonMethod, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true) where TDelegate : Delegate
	{
		MethodInfo methodInfo = Method(typeSemicolonMethod, parameters, generics, logErrorInTrace);
		if ((object)methodInfo == null)
		{
			return null;
		}
		return GetDelegate<TDelegate>(instance, methodInfo, logErrorInTrace);
	}

	public static FieldInfo? DeclaredField(Type type, string name, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.DeclaredField: type '{type}', 'name' is null");
			}
			return null;
		}
		FieldInfo field = type.GetField(name, AccessTools.allDeclared);
		if ((object)field == null)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.DeclaredField: Could not find field for type '{type}' and name '{name}'");
			}
			return null;
		}
		return field;
	}

	public static FieldInfo? Field(Type type, string name, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.Field: type '{type}', 'name' is null");
			}
			return null;
		}
		FieldInfo fieldInfo = FindIncludingBaseTypes(type, (Type t) => t.GetField(name, AccessTools.all));
		if (((object)fieldInfo == null) & logErrorInTrace)
		{
			Trace.TraceError($"AccessTools2.Field: Could not find field for type '{type}' and name '{name}'");
		}
		return fieldInfo;
	}

	public static FieldInfo? DeclaredField(string typeColonFieldname, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonFieldname, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
			}
			return null;
		}
		return DeclaredField(type, name, logErrorInTrace);
	}

	public static FieldInfo? Field(string typeColonFieldname, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonFieldname, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Field: Could not find type or field for '" + typeColonFieldname + "'");
			}
			return null;
		}
		return Field(type, name, logErrorInTrace);
	}

	public static AccessTools.FieldRef<object, F>? FieldRefAccess<F>(string typeColonFieldname, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonFieldname, out Type type, out string name, logErrorInTrace))
		{
			Trace.TraceError("AccessTools2.FieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
			return null;
		}
		return FieldRefAccess<F>(type, name, logErrorInTrace);
	}

	public static AccessTools.FieldRef<T, F>? FieldRefAccess<T, F>(string fieldName, bool logErrorInTrace = true) where T : class
	{
		if (fieldName == null)
		{
			return null;
		}
		FieldInfo instanceField = GetInstanceField(typeof(T), fieldName, logErrorInTrace);
		if ((object)instanceField == null)
		{
			return null;
		}
		return FieldRefAccessInternal<T, F>(instanceField, needCastclass: false, logErrorInTrace);
	}

	public static AccessTools.FieldRef<object, F>? FieldRefAccess<F>(Type type, string fieldName, bool logErrorInTrace = true)
	{
		if ((object)type == null)
		{
			return null;
		}
		if (fieldName == null)
		{
			return null;
		}
		FieldInfo fieldInfo = Field(type, fieldName, logErrorInTrace);
		if ((object)fieldInfo == null)
		{
			return null;
		}
		if (!fieldInfo.IsStatic)
		{
			Type declaringType = fieldInfo.DeclaringType;
			if ((object)declaringType != null)
			{
				if (declaringType.IsValueType)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError("AccessTools2.FieldRefAccess<object, " + typeof(F).FullName + ">: FieldDeclaringType must be a class");
					}
					return null;
				}
				return FieldRefAccessInternal<object, F>(fieldInfo, needCastclass: true, logErrorInTrace);
			}
		}
		return null;
	}

	public static AccessTools.FieldRef<T, F>? FieldRefAccess<T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : class
	{
		if ((object)fieldInfo == null)
		{
			return null;
		}
		if (!fieldInfo.IsStatic)
		{
			Type declaringType = fieldInfo.DeclaringType;
			if ((object)declaringType != null)
			{
				if (declaringType.IsValueType)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError($"AccessTools2.FieldRefAccess<{typeof(T).FullName}, {typeof(F).FullName}>: FieldDeclaringType must be a class");
					}
					return null;
				}
				bool? flag = FieldRefNeedsClasscast(typeof(T), declaringType, logErrorInTrace);
				if (flag.HasValue)
				{
					bool valueOrDefault = flag == true;
					return FieldRefAccessInternal<T, F>(fieldInfo, valueOrDefault, logErrorInTrace);
				}
				return null;
			}
		}
		return null;
	}

	private static AccessTools.FieldRef<T, F>? FieldRefAccessInternal<T, F>(FieldInfo fieldInfo, bool needCastclass, bool logErrorInTrace = true) where T : class
	{
		if (!Helper.IsValid(logErrorInTrace))
		{
			return null;
		}
		if (fieldInfo.IsStatic)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.FieldRefAccessInternal<{typeof(T).FullName}, {typeof(F).FullName}>: Field must not be static");
			}
			return null;
		}
		if (!ValidateFieldType<F>(fieldInfo, logErrorInTrace))
		{
			return null;
		}
		Type typeFromHandle = typeof(T);
		Type declaringType = fieldInfo.DeclaringType;
		DynamicMethodDefinitionHandle? dynamicMethodDefinitionHandle = DynamicMethodDefinitionHandle.Create("__refget_" + typeFromHandle.Name + "_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[1] { typeFromHandle });
		ILGeneratorHandle? iLGeneratorHandle = dynamicMethodDefinitionHandle?.GetILGenerator();
		if (iLGeneratorHandle.HasValue)
		{
			ILGeneratorHandle valueOrDefault = iLGeneratorHandle.GetValueOrDefault();
			valueOrDefault.Emit(OpCodes.Ldarg_0);
			if (needCastclass)
			{
				valueOrDefault.Emit(OpCodes.Castclass, declaringType);
			}
			valueOrDefault.Emit(OpCodes.Ldflda, fieldInfo);
			valueOrDefault.Emit(OpCodes.Ret);
			return dynamicMethodDefinitionHandle?.Generate()?.CreateDelegate(typeof(AccessTools.FieldRef<T, F>)) as AccessTools.FieldRef<T, F>;
		}
		return null;
	}

	private static bool? FieldRefNeedsClasscast(Type delegateInstanceType, Type declaringType, bool logErrorInTrace = true)
	{
		bool flag = false;
		if (delegateInstanceType != declaringType)
		{
			flag = delegateInstanceType.IsAssignableFrom(declaringType);
			if (!flag && !declaringType.IsAssignableFrom(delegateInstanceType))
			{
				if (logErrorInTrace)
				{
					Trace.TraceError($"AccessTools2.FieldRefNeedsClasscast: FieldDeclaringType must be assignable from or to T (FieldRefAccess instance type) - 'instanceOfT is FieldDeclaringType' must be possible, delegateInstanceType '{delegateInstanceType}', declaringType '{declaringType}'");
				}
				return null;
			}
		}
		return flag;
	}

	public static AccessTools.FieldRef<object, TField>? FieldRefAccess<TField>(FieldInfo fieldInfo)
	{
		if ((object)fieldInfo != null)
		{
			return AccessTools.FieldRefAccess<object, TField>(fieldInfo);
		}
		return null;
	}

	public static MethodInfo? DeclaredMethod(Type type, string name, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.DeclaredMethod: type '{type}', 'name' is null");
			}
			return null;
		}
		MethodInfo methodInfo;
		if (parameters == null)
		{
			try
			{
				methodInfo = type.GetMethod(name, AccessTools.allDeclared);
			}
			catch (AmbiguousMatchException value)
			{
				methodInfo = type.GetMethod(name, AccessTools.allDeclared, null, Type.EmptyTypes, new ParameterModifier[0]);
				if ((object)methodInfo == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError($"AccessTools2.DeclaredMethod: Ambiguous match for type '{type}' and name '{name}' and parameters '{parameters?.Description()}', '{value}'");
					}
					return null;
				}
			}
		}
		else
		{
			methodInfo = type.GetMethod(name, AccessTools.allDeclared, null, parameters, new ParameterModifier[0]);
		}
		if ((object)methodInfo == null)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.DeclaredMethod: Could not find method for type '{type}' and name '{name}' and parameters '{parameters?.Description()}'");
			}
			return null;
		}
		if (generics != null)
		{
			methodInfo = methodInfo.MakeGenericMethod(generics);
		}
		return methodInfo;
	}

	public static MethodInfo? Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.Method: type '{type}', 'name' is null");
			}
			return null;
		}
		MethodInfo methodInfo;
		if (parameters == null)
		{
			try
			{
				methodInfo = FindIncludingBaseTypes(type, (Type t) => t.GetMethod(name, AccessTools.all));
			}
			catch (AmbiguousMatchException value)
			{
				methodInfo = FindIncludingBaseTypes(type, (Type t) => t.GetMethod(name, AccessTools.all, null, Type.EmptyTypes, new ParameterModifier[0]));
				if ((object)methodInfo == null)
				{
					if (logErrorInTrace)
					{
						Trace.TraceError($"AccessTools2.Method: Ambiguous match for type '{type}' and name '{name}' and parameters '{parameters?.Description()}', '{value}'");
					}
					return null;
				}
			}
		}
		else
		{
			methodInfo = FindIncludingBaseTypes(type, (Type t) => t.GetMethod(name, AccessTools.all, null, parameters, new ParameterModifier[0]));
		}
		if ((object)methodInfo == null)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.Method: Could not find method for type '{type}' and name '{name}' and parameters '{parameters?.Description()}'");
			}
			return null;
		}
		if (generics != null)
		{
			methodInfo = methodInfo.MakeGenericMethod(generics);
		}
		return methodInfo;
	}

	public static MethodInfo? DeclaredMethod(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonMethodname, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
			}
			return null;
		}
		return DeclaredMethod(type, name, parameters, generics, logErrorInTrace);
	}

	public static MethodInfo? Method(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonMethodname, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Method: Could not find type or property for '" + typeColonMethodname + "'");
			}
			return null;
		}
		return Method(type, name, parameters, generics, logErrorInTrace);
	}

	public static PropertyInfo? DeclaredProperty(Type type, string name, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.DeclaredProperty: type '{type}', 'name' is null");
			}
			return null;
		}
		PropertyInfo property = type.GetProperty(name, AccessTools.allDeclared);
		if (((object)property == null) & logErrorInTrace)
		{
			Trace.TraceError($"AccessTools2.DeclaredProperty: Could not find property for type '{type}' and name '{name}'");
		}
		return property;
	}

	public static PropertyInfo? Property(Type type, string name, bool logErrorInTrace = true)
	{
		if ((object)type == null)
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
				Trace.TraceError($"AccessTools2.Property: type '{type}', 'name' is null");
			}
			return null;
		}
		PropertyInfo propertyInfo = FindIncludingBaseTypes(type, (Type t) => t.GetProperty(name, AccessTools.all));
		if (((object)propertyInfo == null) & logErrorInTrace)
		{
			Trace.TraceError($"AccessTools2.Property: Could not find property for type '{type}' and name '{name}'");
		}
		return propertyInfo;
	}

	public static MethodInfo? DeclaredPropertyGetter(Type type, string name, bool logErrorInTrace = true)
	{
		return DeclaredProperty(type, name, logErrorInTrace)?.GetGetMethod(nonPublic: true);
	}

	public static MethodInfo? DeclaredPropertySetter(Type type, string name, bool logErrorInTrace = true)
	{
		return DeclaredProperty(type, name, logErrorInTrace)?.GetSetMethod(nonPublic: true);
	}

	public static MethodInfo? PropertyGetter(Type type, string name, bool logErrorInTrace = true)
	{
		return Property(type, name, logErrorInTrace)?.GetGetMethod(nonPublic: true);
	}

	public static MethodInfo? PropertySetter(Type type, string name, bool logErrorInTrace = true)
	{
		return Property(type, name, logErrorInTrace)?.GetSetMethod(nonPublic: true);
	}

	public static PropertyInfo? DeclaredProperty(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonPropertyName, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.DeclaredProperty: Could not find type or property for '" + typeColonPropertyName + "'");
			}
			return null;
		}
		return DeclaredProperty(type, name, logErrorInTrace);
	}

	public static PropertyInfo? Property(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonPropertyName, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.Property: Could not find type or property for '" + typeColonPropertyName + "'");
			}
			return null;
		}
		return Property(type, name, logErrorInTrace);
	}

	public static MethodInfo? DeclaredPropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		return DeclaredProperty(typeColonPropertyName, logErrorInTrace)?.GetSetMethod(nonPublic: true);
	}

	public static MethodInfo? DeclaredPropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		return DeclaredProperty(typeColonPropertyName, logErrorInTrace)?.GetGetMethod(nonPublic: true);
	}

	public static MethodInfo? PropertyGetter(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		return Property(typeColonPropertyName, logErrorInTrace)?.GetGetMethod(nonPublic: true);
	}

	public static MethodInfo? PropertySetter(string typeColonPropertyName, bool logErrorInTrace = true)
	{
		return Property(typeColonPropertyName, logErrorInTrace)?.GetSetMethod(nonPublic: true);
	}

	public static AccessTools.FieldRef<TField>? StaticFieldRefAccess<TField>(string typeColonFieldname, bool logErrorInTrace = true)
	{
		if (!TryGetComponents(typeColonFieldname, out Type type, out string name, logErrorInTrace))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.StaticFieldRefAccess: Could not find type or field for '" + typeColonFieldname + "'");
			}
			return null;
		}
		return StaticFieldRefAccess<TField>(type, name, logErrorInTrace);
	}

	public static AccessTools.FieldRef<F>? StaticFieldRefAccess<F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
	{
		if ((object)fieldInfo == null)
		{
			return null;
		}
		return StaticFieldRefAccessInternal<F>(fieldInfo, logErrorInTrace);
	}

	public static AccessTools.FieldRef<TField>? StaticFieldRefAccess<TField>(Type type, string fieldName, bool logErrorInTrace = true)
	{
		FieldInfo fieldInfo = Field(type, fieldName, logErrorInTrace);
		if ((object)fieldInfo == null)
		{
			return null;
		}
		return StaticFieldRefAccess<TField>(fieldInfo, logErrorInTrace);
	}

	private static AccessTools.FieldRef<F>? StaticFieldRefAccessInternal<F>(FieldInfo fieldInfo, bool logErrorInTrace = true)
	{
		if (!Helper.IsValid(logErrorInTrace))
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
		if (!ValidateFieldType<F>(fieldInfo, logErrorInTrace))
		{
			return null;
		}
		DynamicMethodDefinitionHandle? dynamicMethodDefinitionHandle = DynamicMethodDefinitionHandle.Create("__refget_" + (fieldInfo.DeclaringType?.Name ?? "null") + "_static_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[0]);
		ILGeneratorHandle? iLGeneratorHandle = dynamicMethodDefinitionHandle?.GetILGenerator();
		if (iLGeneratorHandle.HasValue)
		{
			ILGeneratorHandle valueOrDefault = iLGeneratorHandle.GetValueOrDefault();
			valueOrDefault.Emit(OpCodes.Ldsflda, fieldInfo);
			valueOrDefault.Emit(OpCodes.Ret);
			return dynamicMethodDefinitionHandle?.Generate()?.CreateDelegate(typeof(AccessTools.FieldRef<F>)) as AccessTools.FieldRef<F>;
		}
		return null;
	}

	public static AccessTools.StructFieldRef<T, F>? StructFieldRefAccess<T, F>(string fieldName, bool logErrorInTrace = true) where T : struct
	{
		if (string.IsNullOrEmpty(fieldName))
		{
			return null;
		}
		FieldInfo instanceField = GetInstanceField(typeof(T), fieldName, logErrorInTrace);
		if ((object)instanceField == null)
		{
			return null;
		}
		return StructFieldRefAccessInternal<T, F>(instanceField, logErrorInTrace);
	}

	public static AccessTools.StructFieldRef<T, F>? StructFieldRefAccess<T, F>(FieldInfo? fieldInfo, bool logErrorInTrace = true) where T : struct
	{
		if ((object)fieldInfo == null)
		{
			return null;
		}
		if (!ValidateStructField<T, F>(fieldInfo, logErrorInTrace))
		{
			return null;
		}
		return StructFieldRefAccessInternal<T, F>(fieldInfo, logErrorInTrace);
	}

	private static AccessTools.StructFieldRef<T, F>? StructFieldRefAccessInternal<T, F>(FieldInfo fieldInfo, bool logErrorInTrace = true) where T : struct
	{
		if (!ValidateFieldType<F>(fieldInfo, logErrorInTrace))
		{
			return null;
		}
		DynamicMethodDefinitionHandle? dynamicMethodDefinitionHandle = DynamicMethodDefinitionHandle.Create("__refget_" + typeof(T).Name + "_struct_fi_" + fieldInfo.Name, typeof(F).MakeByRefType(), new Type[1] { typeof(T).MakeByRefType() });
		ILGeneratorHandle? iLGeneratorHandle = dynamicMethodDefinitionHandle?.GetILGenerator();
		if (iLGeneratorHandle.HasValue)
		{
			ILGeneratorHandle valueOrDefault = iLGeneratorHandle.GetValueOrDefault();
			valueOrDefault.Emit(OpCodes.Ldarg_0);
			valueOrDefault.Emit(OpCodes.Ldflda, fieldInfo);
			valueOrDefault.Emit(OpCodes.Ret);
			return dynamicMethodDefinitionHandle?.Generate()?.CreateDelegate(typeof(AccessTools.StructFieldRef<T, F>)) as AccessTools.StructFieldRef<T, F>;
		}
		return null;
	}

	public static IEnumerable<Assembly> AllAssemblies()
	{
		return from a in AppDomain.CurrentDomain.GetAssemblies()
			where !a.FullName.StartsWith("Microsoft.VisualStudio")
			select a;
	}

	public static IEnumerable<Type> AllTypes()
	{
		return AllAssemblies().SelectMany((Assembly a) => GetTypesFromAssembly(a));
	}

	public static Type[] GetTypesFromAssembly(Assembly assembly, bool logErrorInTrace = true)
	{
		if ((object)assembly == null)
		{
			return Type.EmptyTypes;
		}
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.GetTypesFromAssembly: assembly {assembly} => {ex}");
			}
			return ex.Types.Where((Type type) => (object)type != null).ToArray();
		}
	}

	public static Type[] GetTypesFromAssemblyIfValid(Assembly assembly, bool logErrorInTrace = true)
	{
		if ((object)assembly == null)
		{
			return Type.EmptyTypes;
		}
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException value)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.GetTypesFromAssemblyIfValid: assembly {assembly} => {value}");
			}
			return Type.EmptyTypes;
		}
	}

	public static Type? TypeByName(string name, bool logErrorInTrace = true)
	{
		if (string.IsNullOrEmpty(name))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.TypeByName: 'name' is null or empty");
			}
			return null;
		}
		Type type = Type.GetType(name, throwOnError: false);
		if ((object)type == null)
		{
			type = AllTypes().FirstOrDefault((Type t) => t.FullName == name);
		}
		if ((object)type == null)
		{
			type = AllTypes().FirstOrDefault((Type t) => t.Name == name);
		}
		if (((object)type == null) & logErrorInTrace)
		{
			Trace.TraceError("AccessTools2.TypeByName: Could not find type named '" + name + "'");
		}
		return type;
	}

	public static T? FindIncludingBaseTypes<T>(Type type, Func<Type, T> func) where T : class
	{
		if ((object)type == null || func == null)
		{
			return null;
		}
		do
		{
			T val = func(type);
			if (val != null)
			{
				return val;
			}
			type = type.BaseType;
		}
		while ((object)type != null);
		return null;
	}

	private static FieldInfo? GetInstanceField(Type type, string fieldName, bool logErrorInTrace = true)
	{
		FieldInfo fieldInfo = Field(type, fieldName, logErrorInTrace);
		if ((object)fieldInfo == null)
		{
			return null;
		}
		if (fieldInfo.IsStatic)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.GetInstanceField: Field must not be static, type '{type}', fieldName '{fieldName}'");
			}
			return null;
		}
		return fieldInfo;
	}

	private static bool ValidateFieldType<F>(FieldInfo? fieldInfo, bool logErrorInTrace = true)
	{
		if ((object)fieldInfo == null)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.ValidateFieldType<" + typeof(F).FullName + ">: 'fieldInfo' is null");
			}
			return false;
		}
		Type typeFromHandle = typeof(F);
		Type fieldType = fieldInfo.FieldType;
		if (typeFromHandle == fieldType)
		{
			return true;
		}
		if (fieldType.IsEnum)
		{
			Type underlyingType = Enum.GetUnderlyingType(fieldType);
			if (typeFromHandle != underlyingType)
			{
				if (logErrorInTrace)
				{
					Trace.TraceError($"AccessTools2.ValidateFieldType<{typeof(F).FullName}>: FieldRefAccess return type must be the same as FieldType or FieldType's underlying integral type ({underlyingType}) for enum types, fieldInfo '{fieldInfo}'");
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
					Trace.TraceError($"AccessTools2.ValidateFieldType<{typeof(F).FullName}>: FieldRefAccess return type must be the same as FieldType for value types, fieldInfo '{fieldInfo}'");
				}
				return false;
			}
			if (!typeFromHandle.IsAssignableFrom(fieldType))
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

	private static bool ValidateStructField<T, F>(FieldInfo? fieldInfo, bool logErrorInTrace = true) where T : struct
	{
		if ((object)fieldInfo == null)
		{
			return false;
		}
		if (fieldInfo.IsStatic)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.ValidateStructField<{typeof(T).FullName}, {typeof(F).FullName}>: Field must not be static");
			}
			return false;
		}
		if (fieldInfo.DeclaringType != typeof(T))
		{
			if (logErrorInTrace)
			{
				Trace.TraceError($"AccessTools2.ValidateStructField<{typeof(T).FullName}, {typeof(F).FullName}>: FieldDeclaringType must be T (StructFieldRefAccess instance type)");
			}
			return false;
		}
		return true;
	}

	private static bool TryGetComponents(string typeColonName, out Type? type, out string? name, bool logErrorInTrace = true)
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
		string[] array = typeColonName.Split(':');
		if (array.Length != 2)
		{
			if (logErrorInTrace)
			{
				Trace.TraceError("AccessTools2.TryGetComponents: typeColonName '" + typeColonName + "', name must be specified as 'Namespace.Type1.Type2:Name");
			}
			type = null;
			name = null;
			return false;
		}
		type = TypeByName(array[0], logErrorInTrace);
		name = array[1];
		return (object)type != null;
	}
}
