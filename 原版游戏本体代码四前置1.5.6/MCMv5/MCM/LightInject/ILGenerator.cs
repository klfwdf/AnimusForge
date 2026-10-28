using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ILGenerator
{
	private readonly ParameterExpression[] parameters;

	private readonly Stack<Expression> stack = new Stack<Expression>();

	private readonly List<LocalBuilder> locals = new List<LocalBuilder>();

	private readonly List<Expression> expressions = new List<Expression>();

	public Expression CurrentExpression
	{
		get
		{
			List<ParameterExpression> variables = locals.Select((LocalBuilder l) => l.Variable).ToList();
			List<Expression> list = new List<Expression>(expressions) { stack.Peek() };
			return Expression.Block(variables, list);
		}
	}

	public ILGenerator(ParameterExpression[] parameters)
	{
		this.parameters = parameters;
	}

	public void Emit(OpCode code, ConstructorInfo constructor)
	{
		if (code == OpCodes.Newobj)
		{
			int numberOfElements = constructor.GetParameters().Length;
			NewExpression item = Expression.New(constructor, Pop(numberOfElements));
			stack.Push(item);
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code)
	{
		if (code == OpCodes.Ldarg_0)
		{
			stack.Push(parameters[0]);
		}
		else if (code == OpCodes.Ldarg_1)
		{
			stack.Push(parameters[1]);
		}
		else if (code == OpCodes.Ldarg_2)
		{
			stack.Push(parameters[2]);
		}
		else if (code == OpCodes.Ldarg_3)
		{
			stack.Push(parameters[3]);
		}
		else if (code == OpCodes.Ldloc_0)
		{
			stack.Push(locals[0].Variable);
		}
		else if (code == OpCodes.Ldloc_1)
		{
			stack.Push(locals[1].Variable);
		}
		else if (code == OpCodes.Ldloc_2)
		{
			stack.Push(locals[2].Variable);
		}
		else if (code == OpCodes.Ldloc_3)
		{
			stack.Push(locals[3].Variable);
		}
		else if (code == OpCodes.Stloc_0)
		{
			Expression right = stack.Pop();
			BinaryExpression item = Expression.Assign(locals[0].Variable, right);
			expressions.Add(item);
		}
		else if (code == OpCodes.Stloc_1)
		{
			Expression right2 = stack.Pop();
			BinaryExpression item2 = Expression.Assign(locals[1].Variable, right2);
			expressions.Add(item2);
		}
		else if (code == OpCodes.Stloc_2)
		{
			Expression right3 = stack.Pop();
			BinaryExpression item3 = Expression.Assign(locals[2].Variable, right3);
			expressions.Add(item3);
		}
		else if (code == OpCodes.Stloc_3)
		{
			Expression right4 = stack.Pop();
			BinaryExpression item4 = Expression.Assign(locals[3].Variable, right4);
			expressions.Add(item4);
		}
		else if (code == OpCodes.Ldelem_Ref)
		{
			Expression[] array = new Expression[1] { stack.Pop() };
			for (int i = 0; i < array.Length; i++)
			{
				array[0] = Expression.Convert(array[i], typeof(int));
			}
			Expression array2 = stack.Pop();
			stack.Push(Expression.ArrayAccess(array2, array));
		}
		else if (code == OpCodes.Ldlen)
		{
			Expression array3 = stack.Pop();
			stack.Push(Expression.ArrayLength(array3));
		}
		else if (code == OpCodes.Conv_I4)
		{
			stack.Push(Expression.Convert(stack.Pop(), typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_0)
		{
			stack.Push(Expression.Constant(0, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_1)
		{
			stack.Push(Expression.Constant(1, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_2)
		{
			stack.Push(Expression.Constant(2, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_3)
		{
			stack.Push(Expression.Constant(3, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_4)
		{
			stack.Push(Expression.Constant(4, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_5)
		{
			stack.Push(Expression.Constant(5, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_6)
		{
			stack.Push(Expression.Constant(6, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_7)
		{
			stack.Push(Expression.Constant(7, typeof(int)));
		}
		else if (code == OpCodes.Ldc_I4_8)
		{
			stack.Push(Expression.Constant(8, typeof(int)));
		}
		else if (code == OpCodes.Sub)
		{
			Expression right5 = stack.Pop();
			Expression left = stack.Pop();
			stack.Push(Expression.Subtract(left, right5));
		}
		else if (!(code == OpCodes.Ret))
		{
			if (!(code == OpCodes.Ldnull))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Push(Expression.Constant(null));
		}
	}

	public void Emit(OpCode code, LocalBuilder localBuilder)
	{
		if (code == OpCodes.Stloc)
		{
			Expression right = stack.Pop();
			BinaryExpression item = Expression.Assign(localBuilder.Variable, right);
			expressions.Add(item);
			return;
		}
		if (code == OpCodes.Ldloc)
		{
			stack.Push(localBuilder.Variable);
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, int arg)
	{
		if (code == OpCodes.Ldc_I4)
		{
			stack.Push(Expression.Constant(arg, typeof(int)));
			return;
		}
		if (code == OpCodes.Ldarg)
		{
			stack.Push(parameters[arg]);
			return;
		}
		if (code == OpCodes.Ldloc)
		{
			stack.Push(locals[arg].Variable);
			return;
		}
		if (code == OpCodes.Stloc)
		{
			Expression right = stack.Pop();
			BinaryExpression item = Expression.Assign(locals[arg].Variable, right);
			expressions.Add(item);
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, long arg)
	{
		if (code == OpCodes.Ldc_I8)
		{
			stack.Push(Expression.Constant(arg, typeof(long)));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void PushConstantValue(object arg, Type type)
	{
		stack.Push(Expression.Constant(arg, type));
	}

	public void Emit(OpCode code, sbyte arg)
	{
		if (code == OpCodes.Ldc_I4_S)
		{
			stack.Push(Expression.Constant((int)arg, typeof(int)));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, byte arg)
	{
		if (code == OpCodes.Ldloc_S)
		{
			stack.Push(locals[arg].Variable);
			return;
		}
		if (code == OpCodes.Ldarg_S)
		{
			stack.Push(parameters[arg]);
			return;
		}
		if (code == OpCodes.Stloc_S)
		{
			Expression right = stack.Pop();
			BinaryExpression item = Expression.Assign(locals[arg].Variable, right);
			expressions.Add(item);
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public void Emit(OpCode code, string arg)
	{
		if (code == OpCodes.Ldstr)
		{
			stack.Push(Expression.Constant(arg, typeof(string)));
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	public LocalBuilder DeclareLocal(Type type)
	{
		LocalBuilder localBuilder = new LocalBuilder(type, locals.Count);
		locals.Add(localBuilder);
		return localBuilder;
	}

	public void Emit(OpCode code, Type type)
	{
		if (code == OpCodes.Newarr)
		{
			stack.Push(Expression.NewArrayBounds(type, Pop(1)));
		}
		else if (code == OpCodes.Stelem)
		{
			Expression right = stack.Pop();
			Expression expression = stack.Pop();
			Expression array = stack.Pop();
			IndexExpression left = Expression.ArrayAccess(array, expression);
			BinaryExpression item = Expression.Assign(left, right);
			expressions.Add(item);
		}
		else if (code == OpCodes.Castclass)
		{
			stack.Push(Expression.Convert(stack.Pop(), type));
		}
		else if (code == OpCodes.Box)
		{
			stack.Push(Expression.Convert(stack.Pop(), typeof(object)));
		}
		else
		{
			if (!(code == OpCodes.Unbox_Any))
			{
				throw new NotSupportedException(code.ToString());
			}
			stack.Push(Expression.Convert(stack.Pop(), type));
		}
	}

	public void Emit(OpCode code, MethodInfo methodInfo)
	{
		if (code == OpCodes.Callvirt || code == OpCodes.Call)
		{
			int numberOfElements = methodInfo.GetParameters().Length;
			Expression[] arguments = Pop(numberOfElements);
			MethodCallExpression item;
			if (!methodInfo.IsStatic)
			{
				Expression instance = stack.Pop();
				item = Expression.Call(instance, methodInfo, arguments);
			}
			else
			{
				item = Expression.Call(null, methodInfo, arguments);
			}
			if (methodInfo.ReturnType == typeof(void))
			{
				expressions.Add(item);
			}
			else
			{
				stack.Push(item);
			}
			return;
		}
		throw new NotSupportedException(code.ToString());
	}

	private Expression[] Pop(int numberOfElements)
	{
		Expression[] array = new Expression[numberOfElements];
		for (int i = 0; i < numberOfElements; i++)
		{
			array[i] = stack.Pop();
		}
		return Enumerable.Reverse(array).ToArray();
	}
}
