namespace MCM.Abstractions.Wrapper;

public sealed class AttributePropertyDefinitionGroupToggleWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionGroupToggle, IPropertyDefinitionBase
{
	public bool IsToggle { get; } = true;

	public AttributePropertyDefinitionGroupToggleWrapper(object @object)
		: base(@object)
	{
	}
}
