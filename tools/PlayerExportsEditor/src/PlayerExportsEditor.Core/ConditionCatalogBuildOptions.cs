namespace PlayerExportsEditor.Core;

public sealed class ConditionCatalogBuildOptions
{
    public bool IncludePackagedCatalog { get; init; } = true;

    public bool IncludeLooseModuleData { get; init; } = true;

    public bool OfficialModulesOnly { get; init; }

    public IReadOnlyList<string> ExtraModulesRoots { get; init; } = Array.Empty<string>();
}
