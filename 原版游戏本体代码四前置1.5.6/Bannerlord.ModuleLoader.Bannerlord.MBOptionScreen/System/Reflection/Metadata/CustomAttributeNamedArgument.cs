namespace System.Reflection.Metadata;

internal readonly struct CustomAttributeNamedArgument<TType>
{
	public string? Name { get; }

	public CustomAttributeNamedArgumentKind Kind { get; }

	public TType Type { get; }

	public object? Value { get; }

	public CustomAttributeNamedArgument(string? name, CustomAttributeNamedArgumentKind kind, TType type, object? value)
	{
		Name = name;
		Kind = kind;
		Type = type;
		Value = value;
	}
}
