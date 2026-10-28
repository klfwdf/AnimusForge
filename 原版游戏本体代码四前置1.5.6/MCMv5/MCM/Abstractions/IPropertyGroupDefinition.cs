namespace MCM.Abstractions;

public interface IPropertyGroupDefinition
{
	string GroupName { get; }

	int GroupOrder { get; }
}
