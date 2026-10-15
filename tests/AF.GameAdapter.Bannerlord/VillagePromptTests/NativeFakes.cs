using AnimusForge.SiegeAftermathIntervention;
namespace TaleWorlds.CampaignSystem { public sealed class Hero {} public sealed class CharacterObject {public Hero HeroObject;} }
namespace TaleWorlds.MountAndBlade {public sealed class Agent {public object Character;} }
namespace AnimusForge {
public sealed partial class VillageAftermathBehavior {
    public static bool Active, Foreign, Live;
    public static int BuildCalls;
    public static TaleWorlds.MountAndBlade.Agent Speaker;
    public static VillageDispositionSpeakerRole Role;
    private static bool _hostileDispositionActive => Foreign;
    private static bool IsActive() => Active;
    private static object ResolveActiveVillage() => new object();
    private static VillageDispositionSpeakerRole ResolveHostileSpeakerRole(int index, out TaleWorlds.MountAndBlade.Agent agent) {agent=Speaker; return Live && index==7 ? Role : VillageDispositionSpeakerRole.Unknown;}
    private static string BuildHostileDialoguePrompt(object village, TaleWorlds.CampaignSystem.Hero hero, TaleWorlds.CampaignSystem.CharacterObject c,int index) {BuildCalls++;return "foreign-main";}
    private static string BuildFriendlyPrompt(object village, TaleWorlds.CampaignSystem.Hero hero, TaleWorlds.CampaignSystem.CharacterObject c,int index) {BuildCalls++;return "friendly-main";}
}
}
