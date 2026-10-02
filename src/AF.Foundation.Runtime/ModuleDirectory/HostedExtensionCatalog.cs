using System;

namespace AnimusForge.Refactor.Modules;

// Metadata only. No second gameplay owner, discovery, tick, or persistent state.
internal static class HostedExtensionCatalog
{
    internal const string Illustrator = "af.extension.illustrator";
    internal const string DialogueUi = "af.extension.dialogue_ui";
    internal const string Coup = "af.extension.coup";
    internal const string Vengeance = "af.extension.vengeance";

    internal static bool RequiresHostConfirmation(string moduleId)
        => moduleId == Illustrator || moduleId == DialogueUi || moduleId == Coup || moduleId == Vengeance;

    internal static void Register(InternalModuleDirectory directory)
    {
        RegisterHost(directory, Illustrator);
        RegisterHost(directory, DialogueUi);
        RegisterHost(directory, Coup);
        RegisterHost(directory, Vengeance);
    }

    private static void RegisterHost(InternalModuleDirectory directory, string moduleId)
    {
        var definition = new InternalModuleDefinition(moduleId, 1,
            new[] { new InternalCapabilityDefinition(moduleId + ".host", 1) });
        if (!directory.TryRegister(definition, out string reason))
            throw new InvalidOperationException(reason);
    }
}
