namespace MCM.Abstractions.Base.Global;

public sealed class EmptyGlobalSettings : GlobalSettings<EmptyGlobalSettings>
{
	public override string Id => "empty_v1";

	public override string DisplayName => "Empty Global Settings";
}
