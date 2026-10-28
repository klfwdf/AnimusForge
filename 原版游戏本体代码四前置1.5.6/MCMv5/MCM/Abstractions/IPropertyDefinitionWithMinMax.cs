namespace MCM.Abstractions;

public interface IPropertyDefinitionWithMinMax
{
	decimal MinValue { get; }

	decimal MaxValue { get; }
}
