namespace Bannerlord.ModuleManager;

internal sealed record ModuleSorterOptions
{
	public bool SkipOptionals { get; set; }

	public bool SkipExternalDependencies { get; set; }

	public ModuleSorterOptions()
	{
	}

	public ModuleSorterOptions(bool skipOptionals, bool skipExternalDependencies)
	{
		SkipOptionals = skipOptionals;
		SkipExternalDependencies = skipExternalDependencies;
	}
}
