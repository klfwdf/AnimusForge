using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class AssertionConditionAttribute : Attribute
{
	public AssertionConditionType ConditionType { get; private set; }

	public AssertionConditionAttribute(AssertionConditionType conditionType)
	{
		ConditionType = conditionType;
	}
}
