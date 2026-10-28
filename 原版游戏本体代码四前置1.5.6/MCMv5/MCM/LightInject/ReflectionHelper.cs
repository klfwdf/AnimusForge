using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ReflectionHelper
{
	private static readonly Lazy<ThreadSafeDictionary<Type, MethodInfo>> GetInstanceWithParametersMethods;

	static ReflectionHelper()
	{
		GetInstanceWithParametersMethods = CreateLazyGetInstanceWithParametersMethods();
	}

	public static MethodInfo GetGetInstanceWithParametersMethod(Type serviceType)
	{
		return GetInstanceWithParametersMethods.Value.GetOrAdd(serviceType, CreateGetInstanceWithParametersMethod);
	}

	public static Delegate CreateGetNamedInstanceWithParametersDelegate(IServiceFactory factory, Type delegateType, string serviceName)
	{
		Type[] genericTypeArguments = delegateType.GetTypeInfo().GenericTypeArguments;
		MethodInfo methodInfo = typeof(ReflectionHelper).GetTypeInfo().DeclaredMethods.Single((MethodInfo m) => m.GetGenericArguments().Length == genericTypeArguments.Length && m.Name == "CreateGenericGetNamedParameterizedInstanceDelegate");
		MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(genericTypeArguments);
		return (Delegate)methodInfo2.Invoke(null, new object[2] { factory, serviceName });
	}

	private static Lazy<ThreadSafeDictionary<Type, MethodInfo>> CreateLazyGetInstanceWithParametersMethods()
	{
		return new Lazy<ThreadSafeDictionary<Type, MethodInfo>>(() => new ThreadSafeDictionary<Type, MethodInfo>());
	}

	private static MethodInfo CreateGetInstanceWithParametersMethod(Type serviceType)
	{
		Type[] genericTypeArguments = serviceType.GetTypeInfo().GenericTypeArguments;
		MethodInfo methodInfo = typeof(ServiceFactoryExtensions).GetTypeInfo().DeclaredMethods.Single((MethodInfo m) => m.Name == "GetInstance" && m.GetGenericArguments().Length == genericTypeArguments.Length && m.GetParameters().All((ParameterInfo p) => p.Name != "serviceName"));
		return methodInfo.MakeGenericMethod(genericTypeArguments);
	}

	private static Func<TArg, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg, TService>(IServiceFactory factory, string serviceName)
	{
		return (TArg arg) => factory.GetInstance<TArg, TService>(arg, serviceName);
	}

	private static Func<TArg1, TArg2, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TService>(IServiceFactory factory, string serviceName)
	{
		return (TArg1 arg1, TArg2 arg2) => factory.GetInstance<TArg1, TArg2, TService>(arg1, arg2, serviceName);
	}

	private static Func<TArg1, TArg2, TArg3, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TArg3, TService>(IServiceFactory factory, string serviceName)
	{
		return (TArg1 arg1, TArg2 arg2, TArg3 arg3) => factory.GetInstance<TArg1, TArg2, TArg3, TService>(arg1, arg2, arg3, serviceName);
	}

	private static Func<TArg1, TArg2, TArg3, TArg4, TService> CreateGenericGetNamedParameterizedInstanceDelegate<TArg1, TArg2, TArg3, TArg4, TService>(IServiceFactory factory, string serviceName)
	{
		return (TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => factory.GetInstance<TArg1, TArg2, TArg3, TArg4, TService>(arg1, arg2, arg3, arg4, serviceName);
	}
}
