namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Stable machine-readable role context shared by the main reply and postprocess prompts.
/// Localized prompt resources explain the role codes to the language model.
/// </summary>
public static class TownDialogueRoleContextProfile
{
    public const string Marker = "[GCCZ_TOWN_ROLE]";

    public const string PersistentPersonalMemoryScope = "persistent_personal";

    public const string SceneLocalMemoryScope = "scene_local";

    public const string NoPersonalMemoryScope = "none";

    public static string Build(TownDialogueRole role)
    {
        return Marker
            + " role=" + role
            + "; memory_scope=" + ResolveMemoryScope(role)
            + ";";
    }

    public static string ResolveMemoryScope(TownDialogueRole role)
    {
        if (TownDialogueRoleClassifier.UsesPersistentPersonalMemory(role))
        {
            return PersistentPersonalMemoryScope;
        }

        if (TownDialogueRoleClassifier.UsesSceneLocalMemory(role))
        {
            return SceneLocalMemoryScope;
        }

        return NoPersonalMemoryScope;
    }
}
