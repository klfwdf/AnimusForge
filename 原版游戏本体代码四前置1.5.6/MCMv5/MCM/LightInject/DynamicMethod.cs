using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class DynamicMethod
{
	private readonly Type returnType;

	private readonly Type[] parameterTypes;

	private readonly ParameterExpression[] parameters;

	private readonly ILGenerator generator;

	public DynamicMethod(Type returnType, Type[] parameterTypes)
	{
		this.returnType = returnType;
		this.parameterTypes = parameterTypes;
		parameters = parameterTypes.Select(Expression.Parameter).ToArray();
		generator = new ILGenerator(parameters);
	}

	public Delegate CreateDelegate(Type delegateType)
	{
		LambdaExpression lambdaExpression = Expression.Lambda(delegateType, generator.CurrentExpression, parameters);
		return lambdaExpression.Compile();
	}

	public Delegate CreateDelegate(Type delegateType, object target)
	{
		Type delegateType2 = Expression.GetDelegateType(parameterTypes.Concat(new Type[1] { returnType }).ToArray());
		LambdaExpression expression = Expression.Lambda(delegateType2, generator.CurrentExpression, tailCall: true, parameters);
		Expression[] arguments = new Expression[1] { Expression.Constant(target) }.Concat(parameters.Cast<Expression>().Skip(1)).ToArray();
		InvocationExpression body = Expression.Invoke(expression, arguments);
		LambdaExpression lambdaExpression = Expression.Lambda(delegateType, body, parameters.Skip(1));
		return lambdaExpression.Compile();
	}

	public ILGenerator GetILGenerator()
	{
		return generator;
	}
}
