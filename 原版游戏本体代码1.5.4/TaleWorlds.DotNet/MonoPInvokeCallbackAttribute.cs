using System;

public class MonoPInvokeCallbackAttribute : Attribute
{
	public Type Type;

	public MonoPInvokeCallbackAttribute(Type type)
	{
		Type = type;
	}
}
