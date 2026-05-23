using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000059 RID: 89
	[NullableContext(1)]
	[Nullable(0)]
	internal class ExpressionReflectionDelegateFactory : ReflectionDelegateFactory
	{
		// Token: 0x170000BE RID: 190
		// (get) Token: 0x0600051D RID: 1309 RVA: 0x00015E84 File Offset: 0x00014084
		internal static ReflectionDelegateFactory Instance
		{
			get
			{
				return ExpressionReflectionDelegateFactory._instance;
			}
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x00015E8C File Offset: 0x0001408C
		public override ObjectConstructor<object> CreateParameterizedConstructor(MethodBase method)
		{
			ValidationUtils.ArgumentNotNull(method, "method");
			Type typeFromHandle = typeof(object);
			ParameterExpression parameterExpression = Expression.Parameter(typeof(object[]), "args");
			Expression body = this.BuildMethodCall(method, typeFromHandle, null, parameterExpression);
			return (ObjectConstructor<object>)Expression.Lambda(typeof(ObjectConstructor<object>), body, new ParameterExpression[] { parameterExpression }).Compile();
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x00015EF4 File Offset: 0x000140F4
		[return: Nullable(new byte[] { 1, 1, 2 })]
		public override MethodCall<T, object> CreateMethodCall<[Nullable(2)] T>(MethodBase method)
		{
			ValidationUtils.ArgumentNotNull(method, "method");
			Type typeFromHandle = typeof(object);
			ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, "target");
			ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object[]), "args");
			Expression body = this.BuildMethodCall(method, typeFromHandle, parameterExpression, parameterExpression2);
			return (MethodCall<T, object>)Expression.Lambda(typeof(MethodCall<T, object>), body, new ParameterExpression[] { parameterExpression, parameterExpression2 }).Compile();
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00015F6C File Offset: 0x0001416C
		private Expression BuildMethodCall(MethodBase method, Type type, [Nullable(2)] ParameterExpression targetParameterExpression, ParameterExpression argsParameterExpression)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Expression[] array;
			IList<ExpressionReflectionDelegateFactory.ByRefParameter> list;
			if (parameters.Length == 0)
			{
				array = CollectionUtils.ArrayEmpty<Expression>();
				list = CollectionUtils.ArrayEmpty<ExpressionReflectionDelegateFactory.ByRefParameter>();
			}
			else
			{
				array = new Expression[parameters.Length];
				list = new List<ExpressionReflectionDelegateFactory.ByRefParameter>();
				for (int i = 0; i < parameters.Length; i++)
				{
					ParameterInfo parameterInfo = parameters[i];
					Type type2 = parameterInfo.ParameterType;
					bool flag = false;
					if (type2.IsByRef)
					{
						type2 = type2.GetElementType();
						flag = true;
					}
					Expression index = Expression.Constant(i);
					Expression expression = Expression.ArrayIndex(argsParameterExpression, index);
					Expression expression2 = this.EnsureCastExpression(expression, type2, !flag);
					if (flag)
					{
						ParameterExpression parameterExpression = Expression.Variable(type2);
						list.Add(new ExpressionReflectionDelegateFactory.ByRefParameter(expression2, parameterExpression, parameterInfo.IsOut));
						expression2 = parameterExpression;
					}
					array[i] = expression2;
				}
			}
			Expression expression3;
			if (method.IsConstructor)
			{
				expression3 = Expression.New((ConstructorInfo)method, array);
			}
			else if (method.IsStatic)
			{
				expression3 = Expression.Call((MethodInfo)method, array);
			}
			else
			{
				expression3 = Expression.Call(this.EnsureCastExpression(targetParameterExpression, method.DeclaringType, false), (MethodInfo)method, array);
			}
			MethodInfo methodInfo = method as MethodInfo;
			if (methodInfo != null)
			{
				if (methodInfo.ReturnType != typeof(void))
				{
					expression3 = this.EnsureCastExpression(expression3, type, false);
				}
				else
				{
					expression3 = Expression.Block(expression3, Expression.Constant(null));
				}
			}
			else
			{
				expression3 = this.EnsureCastExpression(expression3, type, false);
			}
			if (list.Count > 0)
			{
				IList<ParameterExpression> list2 = new List<ParameterExpression>();
				IList<Expression> list3 = new List<Expression>();
				foreach (ExpressionReflectionDelegateFactory.ByRefParameter byRefParameter in list)
				{
					if (!byRefParameter.IsOut)
					{
						list3.Add(Expression.Assign(byRefParameter.Variable, byRefParameter.Value));
					}
					list2.Add(byRefParameter.Variable);
				}
				list3.Add(expression3);
				expression3 = Expression.Block(list2, list3);
			}
			return expression3;
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00016164 File Offset: 0x00014364
		public override Func<T> CreateDefaultConstructor<[Nullable(2)] T>(Type type)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			if (type.IsAbstract())
			{
				return () => (T)((object)Activator.CreateInstance(type));
			}
			Func<T> result;
			try
			{
				Type typeFromHandle = typeof(T);
				Expression expression = Expression.New(type);
				expression = this.EnsureCastExpression(expression, typeFromHandle, false);
				result = (Func<T>)Expression.Lambda(typeof(Func<T>), expression, new ParameterExpression[0]).Compile();
			}
			catch
			{
				result = () => (T)((object)Activator.CreateInstance(type));
			}
			return result;
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00016210 File Offset: 0x00014410
		[return: Nullable(new byte[] { 1, 1, 2 })]
		public override Func<T, object> CreateGet<[Nullable(2)] T>(PropertyInfo propertyInfo)
		{
			ValidationUtils.ArgumentNotNull(propertyInfo, "propertyInfo");
			Type typeFromHandle = typeof(T);
			Type typeFromHandle2 = typeof(object);
			ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, "instance");
			MethodInfo getMethod = propertyInfo.GetGetMethod(true);
			if (getMethod == null)
			{
				throw new ArgumentException("Property does not have a getter.");
			}
			Expression expression;
			if (getMethod.IsStatic)
			{
				expression = Expression.MakeMemberAccess(null, propertyInfo);
			}
			else
			{
				expression = Expression.MakeMemberAccess(this.EnsureCastExpression(parameterExpression, propertyInfo.DeclaringType, false), propertyInfo);
			}
			expression = this.EnsureCastExpression(expression, typeFromHandle2, false);
			return (Func<T, object>)Expression.Lambda(typeof(Func<T, object>), expression, new ParameterExpression[] { parameterExpression }).Compile();
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x000162B8 File Offset: 0x000144B8
		[return: Nullable(new byte[] { 1, 1, 2 })]
		public override Func<T, object> CreateGet<[Nullable(2)] T>(FieldInfo fieldInfo)
		{
			ValidationUtils.ArgumentNotNull(fieldInfo, "fieldInfo");
			ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "source");
			Expression expression;
			if (fieldInfo.IsStatic)
			{
				expression = Expression.Field(null, fieldInfo);
			}
			else
			{
				expression = Expression.Field(this.EnsureCastExpression(parameterExpression, fieldInfo.DeclaringType, false), fieldInfo);
			}
			expression = this.EnsureCastExpression(expression, typeof(object), false);
			return Expression.Lambda<Func<T, object>>(expression, new ParameterExpression[] { parameterExpression }).Compile();
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x00016334 File Offset: 0x00014534
		[return: Nullable(new byte[] { 1, 1, 2 })]
		public override Action<T, object> CreateSet<[Nullable(2)] T>(FieldInfo fieldInfo)
		{
			ValidationUtils.ArgumentNotNull(fieldInfo, "fieldInfo");
			if (fieldInfo.DeclaringType.IsValueType() || fieldInfo.IsInitOnly)
			{
				return LateBoundReflectionDelegateFactory.Instance.CreateSet<T>(fieldInfo);
			}
			ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "source");
			ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object), "value");
			Expression expression;
			if (fieldInfo.IsStatic)
			{
				expression = Expression.Field(null, fieldInfo);
			}
			else
			{
				expression = Expression.Field(this.EnsureCastExpression(parameterExpression, fieldInfo.DeclaringType, false), fieldInfo);
			}
			Expression right = this.EnsureCastExpression(parameterExpression2, expression.Type, false);
			BinaryExpression body = Expression.Assign(expression, right);
			return (Action<T, object>)Expression.Lambda(typeof(Action<T, object>), body, new ParameterExpression[] { parameterExpression, parameterExpression2 }).Compile();
		}

		// Token: 0x06000525 RID: 1317 RVA: 0x00016400 File Offset: 0x00014600
		[return: Nullable(new byte[] { 1, 1, 2 })]
		public override Action<T, object> CreateSet<[Nullable(2)] T>(PropertyInfo propertyInfo)
		{
			ValidationUtils.ArgumentNotNull(propertyInfo, "propertyInfo");
			if (propertyInfo.DeclaringType.IsValueType())
			{
				return LateBoundReflectionDelegateFactory.Instance.CreateSet<T>(propertyInfo);
			}
			Type typeFromHandle = typeof(T);
			Type typeFromHandle2 = typeof(object);
			ParameterExpression parameterExpression = Expression.Parameter(typeFromHandle, "instance");
			ParameterExpression parameterExpression2 = Expression.Parameter(typeFromHandle2, "value");
			Expression expression = this.EnsureCastExpression(parameterExpression2, propertyInfo.PropertyType, false);
			MethodInfo setMethod = propertyInfo.GetSetMethod(true);
			if (setMethod == null)
			{
				throw new ArgumentException("Property does not have a setter.");
			}
			Expression body;
			if (setMethod.IsStatic)
			{
				body = Expression.Call(setMethod, expression);
			}
			else
			{
				body = Expression.Call(this.EnsureCastExpression(parameterExpression, propertyInfo.DeclaringType, false), setMethod, new Expression[] { expression });
			}
			return (Action<T, object>)Expression.Lambda(typeof(Action<T, object>), body, new ParameterExpression[] { parameterExpression, parameterExpression2 }).Compile();
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x000164EC File Offset: 0x000146EC
		private Expression EnsureCastExpression(Expression expression, Type targetType, bool allowWidening = false)
		{
			Type type = expression.Type;
			if (type == targetType || (!type.IsValueType() && targetType.IsAssignableFrom(type)))
			{
				return expression;
			}
			if (targetType.IsValueType())
			{
				Expression expression2 = Expression.Unbox(expression, targetType);
				if (allowWidening && targetType.IsPrimitive())
				{
					MethodInfo method = typeof(Convert).GetMethod("To" + targetType.Name, new Type[] { typeof(object) });
					if (method != null)
					{
						expression2 = Expression.Condition(Expression.TypeIs(expression, targetType), expression2, Expression.Call(method, expression));
					}
				}
				return Expression.Condition(Expression.Equal(expression, Expression.Constant(null, typeof(object))), Expression.Default(targetType), expression2);
			}
			return Expression.Convert(expression, targetType);
		}

		// Token: 0x040001E5 RID: 485
		private static readonly ExpressionReflectionDelegateFactory _instance = new ExpressionReflectionDelegateFactory();

		// Token: 0x0200017F RID: 383
		[Nullable(0)]
		private class ByRefParameter
		{
			// Token: 0x06000EDF RID: 3807 RVA: 0x0004242A File Offset: 0x0004062A
			public ByRefParameter(Expression value, ParameterExpression variable, bool isOut)
			{
				this.Value = value;
				this.Variable = variable;
				this.IsOut = isOut;
			}

			// Token: 0x040006D4 RID: 1748
			public Expression Value;

			// Token: 0x040006D5 RID: 1749
			public ParameterExpression Variable;

			// Token: 0x040006D6 RID: 1750
			public bool IsOut;
		}
	}
}
