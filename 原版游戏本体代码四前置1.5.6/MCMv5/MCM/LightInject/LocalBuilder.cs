using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class LocalBuilder
{
	public ParameterExpression Variable { get; private set; }

	public Type LocalType { get; private set; }

	public int LocalIndex { get; private set; }

	public LocalBuilder(Type type, int localIndex)
	{
		Variable = Expression.Parameter(type);
		LocalType = type;
		LocalIndex = localIndex;
	}
}
