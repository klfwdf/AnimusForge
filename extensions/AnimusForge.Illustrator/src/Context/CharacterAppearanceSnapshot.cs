using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
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

        internal static CharacterAppearanceSnapshot FromCharacter(CharacterObject character, Equipment equipment)
        {
            if (character == null || equipment == null) return null;
            var visible = VisibleEquipment(equipment);
            string bodyProperties = character.GetBodyProperties(visible, -1).ToString();
            var party = MobileParty.ConversationParty?.Party ?? PlayerEncounter.EncounteredParty;
            var banner = character.HeroObject?.ClanBanner?.BannerCode
                         ?? party?.Banner?.BannerCode
                         ?? string.Empty;
            uint color1 = character.HeroObject?.MapFaction?.Color
                          ?? party?.MapFaction?.Color
                          ?? (character.Culture != null ? character.Culture.Color : 0);
            uint color2 = character.HeroObject?.MapFaction?.Color2
                          ?? party?.MapFaction?.Color2
                          ?? (character.Culture != null ? character.Culture.Color2 : 0);

            return new CharacterAppearanceSnapshot(visible.CalculateEquipmentCode(), bodyProperties,
                banner, color1, color2, character.Race, character.IsFemale);
        }

        internal static CharacterAppearanceSnapshot FromTableau(CharacterTableauWidget widget)
            => new CharacterAppearanceSnapshot(widget.EquipmentCode, widget.BodyProperties, widget.BannerCodeText,
                widget.ArmorColor1, widget.ArmorColor2, widget.Race, widget.IsFemale);
    }
}
