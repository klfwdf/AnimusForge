namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionTextWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionText, IPropertyDefinitionBase
{
	public PropertyDefinitionTextWrapper(object @object)
		: base(@object)
	{
	}
}
