using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	/// <summary>A helper class to retrieve reflection info for non-private methods</summary>
	// Token: 0x020001C6 RID: 454
	public static class SymbolExtensions
	{
		/// <summary>Given a lambda expression that calls a method, returns the method info</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>The method in the lambda expression</returns>
		// Token: 0x060007FB RID: 2043 RVA: 0x0001A843 File Offset: 0x00018A43
		public static MethodInfo GetMethodInfo(Expression<Action> expression)
		{
			return SymbolExtensions.GetMethodInfo(expression);
		}

		/// <summary>Given a lambda expression that calls a method, returns the method info</summary>
		/// <typeparam name="T">The generic type</typeparam>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>The method in the lambda expression</returns>
		// Token: 0x060007FC RID: 2044 RVA: 0x0001A843 File Offset: 0x00018A43
		public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
		{
			return SymbolExtensions.GetMethodInfo(expression);
		}

		/// <summary>Given a lambda expression that calls a method, returns the method info</summary>
		/// <typeparam name="T">The generic type</typeparam>
		/// <typeparam name="TResult">The generic result type</typeparam>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>The method in the lambda expression</returns>
		// Token: 0x060007FD RID: 2045 RVA: 0x0001A843 File Offset: 0x00018A43
		public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression)
		{
			return SymbolExtensions.GetMethodInfo(expression);
		}

		/// <summary>Given a lambda expression that calls a method, returns the method info</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>The method in the lambda expression</returns>
		// Token: 0x060007FE RID: 2046 RVA: 0x0001A84C File Offset: 0x00018A4C
		public static MethodInfo GetMethodInfo(LambdaExpression expression)
		{
			MethodCallExpression outermostExpression = expression.Body as MethodCallExpression;
			if (outermostExpression == null)
			{
				UnaryExpression ue = expression.Body as UnaryExpression;
				if (ue != null)
				{
					MethodCallExpression me = ue.Operand as MethodCallExpression;
					if (me != null)
					{
						ConstantExpression ce = me.Object as ConstantExpression;
						if (ce != null)
						{
							MethodInfo mi = ce.Value as MethodInfo;
							if (mi != null)
							{
								return mi;
							}
						}
					}
				}
				throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
			}
			MethodInfo method = outermostExpression.Method;
			if (method == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Cannot find method for expression ");
				defaultInterpolatedStringHandler.AppendFormatted<LambdaExpression>(expression);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return method;
		}
	}
}
