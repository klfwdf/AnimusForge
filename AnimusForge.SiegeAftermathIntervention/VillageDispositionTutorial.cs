namespace AnimusForge.SiegeAftermathIntervention;
public static class VillageDispositionTutorial
{
    public static string Text => string.Join("\n\n", GcczVillageCastleTutorial.Village(false));
    public static string EnglishText => string.Join("\n\n", GcczVillageCastleTutorial.Village(true));
}
