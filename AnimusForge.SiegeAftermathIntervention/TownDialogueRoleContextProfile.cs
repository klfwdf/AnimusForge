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

    public static string Build(TownDialogueRole role)
    {
        TownDialogueRole normalizedRole = TownDialogueRoleClassifier.NormalizeForRuntime(role);
        return Marker
            + " role=" + normalizedRole
            + "; memory_scope=" + ResolveMemoryScope(normalizedRole)
            + ";";
    }

    public static string ResolveMemoryScope(TownDialogueRole role)
    {
        TownDialogueMemoryScope scope = TownDialogueMemoryPolicy.ResolveScope(role);
        if (scope == TownDialogueMemoryScope.PersistentPersonal)
        {
            return PersistentPersonalMemoryScope;
        }

        return SceneLocalMemoryScope;
    }
}
