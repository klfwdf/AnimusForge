namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionBoolWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionBool, IPropertyDefinitionBase
{
	public PropertyDefinitionBoolWrapper(object @object)
		: base(@object)
	{
	}
}
