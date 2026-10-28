using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
[BaseTypeRequired(typeof(Attribute))]
public sealed class BaseTypeRequiredAttribute : Attribute
{
	public Type[] BaseTypes { get; private set; }

	public BaseTypeRequiredAttribute(Type baseType)
	{
		BaseTypes = new Type[1] { baseType };
	}
}
