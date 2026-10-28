namespace MCM.Abstractions;

public interface IPropertyDefinitionBase
{
	string DisplayName { get; }

	int Order { get; }

	bool RequireRestart { get; }

	string HintText { get; }
}
