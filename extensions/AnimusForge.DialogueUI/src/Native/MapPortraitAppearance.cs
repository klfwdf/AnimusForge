using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge.DialogueUI.Native;

// Managed appearance only: never retain the map scene, camera, or native AgentVisuals.
internal sealed class MapPortraitAppearance
{
    internal CharacterObject Character;
    internal BodyProperties Body;
    internal Equipment Equipment;
    internal int Race;
    internal bool Female;
    internal uint Color1;
    internal uint Color2;
    internal Banner Banner;
}
