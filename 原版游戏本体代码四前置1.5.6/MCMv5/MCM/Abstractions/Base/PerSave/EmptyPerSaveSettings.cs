namespace MCM.Abstractions.Base.PerSave;

public sealed class EmptyPerSaveSettings : PerSaveSettings<EmptyPerSaveSettings>
{
	public override string Id => "empty_persave_v1";

	public override string DisplayName => "Empty PerSave Settings";
}
