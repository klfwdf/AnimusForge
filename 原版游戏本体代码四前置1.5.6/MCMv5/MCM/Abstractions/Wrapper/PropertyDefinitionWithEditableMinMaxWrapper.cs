namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithEditableMinMaxWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithEditableMinMax
{
	public decimal EditableMinValue { get; }

	public decimal EditableMaxValue { get; }

	public PropertyDefinitionWithEditableMinMaxWrapper(object @object)
		: base(@object)
	{
		EditableMinValue = (@object.GetType().GetProperty("EditableMinValue")?.GetValue(@object) as decimal?).GetValueOrDefault();
		EditableMaxValue = (@object.GetType().GetProperty("EditableMaxValue")?.GetValue(@object) as decimal?).GetValueOrDefault();
	}
}
