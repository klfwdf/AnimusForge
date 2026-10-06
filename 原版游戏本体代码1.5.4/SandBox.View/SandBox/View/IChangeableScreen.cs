namespace SandBox.View;

public interface IChangeableScreen
{
	bool AnyUnsavedChanges();

	bool CanChangesBeApplied();

	void ApplyChanges();

	void ResetChanges();
}
