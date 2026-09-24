using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

namespace AnimusForge.DialogueUI.Native;

/// <summary>
/// Copies the active conversation agent into the native CharacterTableau renderer.
/// The renderer owns a private tableau scene (it never changes the mission agent or
/// the mission camera), while the model and equipment are sampled from the speaker.
/// Scene combat/upper-body actions must never be replayed as a tableau idle action.
/// This is deliberately a small typed bridge for the overlay;
/// it does not search missions or use reflection.
/// </summary>
internal sealed class LiveSpeakerPortrait
{
    private readonly EquipmentElement[] _equipmentSlots = new EquipmentElement[Equipment.EquipmentSlotLength];
    private bool _hasBody;
    private bool _hasEquipment;
    private BodyProperties _body;
    private int _race;
    private bool _female;
    private string _bodyText;
    private string _equipmentCode;
    private float _renderScale;

    public bool Apply(CharacterTableauWidget tableau, Agent speaker, CharacterObject character)
    {
        if (tableau == null || character == null)
            return false;

        try
        {
            BodyProperties bodyProperties = speaker != null
                ? speaker.BodyPropertiesValue
                : character.HeroObject != null
                    ? character.HeroObject.BodyProperties
                    : character.GetBodyProperties(character.Equipment);
            bool female = speaker != null ? speaker.IsFemale : character.IsFemale;
            if (!_hasBody || bodyProperties != _body || character.Race != _race || female != _female)
            {
                string bodyText = bodyProperties.ToString();
                float scale = GetHeadLockedRenderScale(bodyProperties, character.Race, female);
                _body = bodyProperties;
                _race = character.Race;
                _female = female;
                _bodyText = bodyText;
                _renderScale = scale;
                _hasBody = true;
            }
            Equipment equipment = speaker?.SpawnEquipment ?? character.Equipment;
            bool equipmentChanged = !_hasEquipment;
            // Compare the fixed set of slots, including in-place edits; allocate/encode only on change.
            if (!equipmentChanged)
                for (int i = 0; i < _equipmentSlots.Length; i++)
                    if (!_equipmentSlots[i].IsEqualTo(equipment[i])) { equipmentChanged = true; break; }
            if (equipmentChanged)
            {
                _equipmentCode = equipment.CalculateEquipmentCode();
                for (int i = 0; i < _equipmentSlots.Length; i++) _equipmentSlots[i] = equipment[i];
                _hasEquipment = true;
            }
            tableau.IsVisible = true;
            tableau.DoNotAcceptEvents = true;
            // EmphasizeFace is the native head camera stance.  The additional
            // offset keeps the eyes above the lower wooden mask in our circular
            // viewport while the render scale below normalizes body-height
            // variation between NPCs.
            tableau.StanceIndex = (int)CharacterViewModel.StanceTypes.EmphasizeFace;
            tableau.PositionYOffset = -134f;
            tableau.CustomRenderScale = _renderScale;
            tableau.CharStringId = character.StringId ?? string.Empty;
            tableau.BodyProperties = _bodyText;
            tableau.IsFemale = female;
            tableau.Race = character.Race;
            tableau.EquipmentCode = _equipmentCode;
            tableau.ArmorColor1 = speaker != null ? speaker.ClothingColor1 : character.Culture?.Color ?? 0;
            tableau.ArmorColor2 = speaker != null ? speaker.ClothingColor2 : character.Culture?.Color2 ?? 0;
            // BannerCode is the engine's invalidated cache; Serialize would rebuild it every sample.
            tableau.BannerCodeText = character.HeroObject?.Clan?.Banner?.BannerCode ?? string.Empty;

            // A guard's channel-1 weapon/attack animation is an additive scene action, not a
            // complete portrait stance. Use the native tableau idle with both hands unwielded.
            tableau.IsEquipmentAnimActive = false;
            tableau.LeftHandWieldedEquipmentIndex = -1;
            tableau.RightHandWieldedEquipmentIndex = -1;
            tableau.IdleAction = "act_inventory_idle_start";

            return true;
        }
        catch (Exception ex)
        {
            DialogueUiRuntime.LogOnce("portrait-update", "Live speaker portrait update failed: " + ex.GetType().Name + ": " + ex.Message);
            return false;
        }
    }

    internal static float GetHeadLockedRenderScale(BodyProperties bodyProperties, int race, bool isFemale)
    {
        const float baseRenderScale = 1.35f;
        try
        {
            // FaceGen's scale key is the same height factor used by the native
            // tableau.  Applying its reciprocal keeps the head at a stable size
            // without touching the mission agent or the global camera.
            float bodyScale = MBBodyProperties.GetScaleFromKey(race, isFemale ? 1 : 0, bodyProperties);
            if (bodyScale > 0.01f)
            {
                float normalized = baseRenderScale / bodyScale;
                if (normalized < 1.18f) return 1.18f;
                if (normalized > 1.52f) return 1.52f;
                return normalized;
            }
        }
        catch
        {
            // Some custom races do not expose a FaceGen scale key; keep the
            // safe native scale for those characters.
        }
        return baseRenderScale;
    }

}
