using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

namespace AnimusForge.Illustrator.Context
{
    public sealed class CharacterAppearanceSnapshot
    {
        public string EquipmentCode { get; }
        public string BodyProperties { get; }
        public string BannerCode { get; }
        public uint Color1 { get; }
        public uint Color2 { get; }
        public int Race { get; }
        public bool IsFemale { get; }
        public CharacterAppearanceSnapshot(string equipment, string body, string banner, uint color1, uint color2, int race, bool female)
        { EquipmentCode = equipment; BodyProperties = body; BannerCode = banner; Color1 = color1; Color2 = color2; Race = race; IsFemale = female; }

        internal static Equipment VisibleEquipment(Equipment source)
        {
            if (source == null) return null;
            var visible = new Equipment(source);
            for (int i = 0; i < 12; i++)
            {
                var element = source[i];
                if (element.CosmeticItem != null)
                    visible[i] = new EquipmentElement(element.CosmeticItem, element.ItemModifier);
            }
            return visible;
        }

        internal static CharacterAppearanceSnapshot FromAgent(Agent agent, Equipment equipment)
        {
            var character = agent.Character as CharacterObject;
            return new CharacterAppearanceSnapshot(VisibleEquipment(equipment)?.CalculateEquipmentCode(), agent.BodyPropertiesValue.ToString(),
                agent.Origin?.Banner?.BannerCode, agent.ClothingColor1, agent.ClothingColor2, character?.Race ?? 0, character?.IsFemale == true);
        }

        internal static CharacterAppearanceSnapshot FromHero(Hero hero, Equipment equipment)
        {
            if (hero == null || equipment == null) return null;
            var visible = VisibleEquipment(equipment);
            return new CharacterAppearanceSnapshot(visible.CalculateEquipmentCode(), hero.CharacterObject.GetBodyProperties(visible, -1).ToString(),
                hero.ClanBanner?.BannerCode, hero.MapFaction?.Color ?? 0, hero.MapFaction?.Color2 ?? 0, hero.CharacterObject.Race, hero.IsFemale);
        }

        internal static CharacterAppearanceSnapshot FromTableau(CharacterTableauWidget widget)
            => new CharacterAppearanceSnapshot(widget.EquipmentCode, widget.BodyProperties, widget.BannerCodeText,
                widget.ArmorColor1, widget.ArmorColor2, widget.Race, widget.IsFemale);
    }
}
