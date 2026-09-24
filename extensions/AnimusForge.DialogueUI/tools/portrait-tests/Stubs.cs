// Headless fixtures for the actual linked portrait updater. No game renderer is exercised.
using System;
namespace TaleWorlds.CampaignSystem.ViewModelCollection { }
namespace TaleWorlds.Core.ViewModelCollection
{
    public class CharacterViewModel { public enum StanceTypes { None, EmphasizeFace } }
}
namespace TaleWorlds.Core
{
    public readonly record struct BodyProperties(int Key)
    {
        public static int Encodes;
        public override string ToString() { Encodes++; return Key.ToString(); }
    }
    public readonly record struct EquipmentElement(int Item, int Modifier = 0)
    {
        public bool IsEqualTo(EquipmentElement other) => this == other;
    }
    public class Equipment
    {
        public const int EquipmentSlotLength = 12;
        public static int Encodes;
        private readonly EquipmentElement[] _slots = new EquipmentElement[EquipmentSlotLength];
        public EquipmentElement this[int i] { get => _slots[i]; set => _slots[i] = value; }
        public string CalculateEquipmentCode() { Encodes++; return string.Join("|", _slots); }
    }
    public class Banner
    {
        private string _cached;
        public int Encodes;
        public int Version;
        public string BannerCode => _cached ??= Serialize();
        public string Serialize() { Encodes++; return "banner-" + Version; }
        public void Edit() { Version++; _cached = null; }
    }
}
namespace TaleWorlds.CampaignSystem
{
    using TaleWorlds.Core;
    public class CharacterObject
    {
        public Hero HeroObject;
        public Equipment Equipment = new Equipment();
        public int Race;
        public bool IsFemale;
        public string StringId = "guard";
        public Culture Culture;
        public BodyProperties GetBodyProperties(Equipment equipment) => new BodyProperties(77);
    }
    public class Hero { public BodyProperties BodyProperties; public Clan Clan; }
    public class Clan { public Banner Banner; }
    public class Culture { public uint Color; public uint Color2; }
}
namespace TaleWorlds.MountAndBlade
{
    using TaleWorlds.Core;
    public class Agent
    {
        public BodyProperties BodyPropertiesValue;
        public Equipment SpawnEquipment;
        public bool IsFemale;
        public uint ClothingColor1;
        public uint ClothingColor2;
        // A portrait cannot use a scene action; fail the fixture if this regression returns.
        public object GetCurrentAction(int channel) => throw new Exception("Scene animation sampled");
    }
    public static class MBBodyProperties
    {
        public static int Calls;
        public static float GetScaleFromKey(int race, int sex, BodyProperties properties) { Calls++; return 1; }
    }
}
namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets
{
    public class CharacterTableauWidget
    {
        public bool IsVisible, DoNotAcceptEvents, IsFemale, IsEquipmentAnimActive;
        public int StanceIndex, Race, LeftHandWieldedEquipmentIndex, RightHandWieldedEquipmentIndex;
        public float PositionYOffset, CustomRenderScale;
        public string CharStringId, BodyProperties, EquipmentCode, BannerCodeText, IdleAction;
        public uint ArmorColor1, ArmorColor2;
    }
}
namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiRuntime
    {
        public static void LogOnce(string key, string text) => throw new Exception(text);
    }
}
