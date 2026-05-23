using System;
using System.Xml;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class BasicCultureObject : MBObjectBase
{
	public TextObject Name { get; private set; }

	public bool IsMainCulture { get; private set; }

	public bool IsBandit { get; private set; }

	public bool CanHaveSettlement { get; private set; }

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public uint ClothAlternativeColor { get; private set; }

	public uint ClothAlternativeColor2 { get; private set; }

	public uint BackgroundColor1 { get; private set; }

	public uint ForegroundColor1 { get; private set; }

	public uint BackgroundColor2 { get; private set; }

	public uint ForegroundColor2 { get; private set; }

	public string EncounterBackgroundMesh { get; set; }

	public Banner Banner { get; private set; }

	public override string ToString()
	{
		return Name.ToString();
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		Name = new TextObject(node.Attributes["name"].Value);
		Color = ((node.Attributes["color"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["color"].Value, 16));
		Color2 = ((node.Attributes["color2"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["color2"].Value, 16));
		ClothAlternativeColor = ((node.Attributes["cloth_alternative_color1"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["cloth_alternative_color1"].Value, 16));
		ClothAlternativeColor2 = ((node.Attributes["cloth_alternative_color2"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["cloth_alternative_color2"].Value, 16));
		BackgroundColor1 = ((node.Attributes["banner_background_color1"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["banner_background_color1"].Value, 16));
		ForegroundColor1 = ((node.Attributes["banner_foreground_color1"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["banner_foreground_color1"].Value, 16));
		BackgroundColor2 = ((node.Attributes["banner_background_color2"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["banner_background_color2"].Value, 16));
		ForegroundColor2 = ((node.Attributes["banner_foreground_color2"] == null) ? uint.MaxValue : Convert.ToUInt32(node.Attributes["banner_foreground_color2"].Value, 16));
		IsMainCulture = node.Attributes["is_main_culture"] != null && Convert.ToBoolean(node.Attributes["is_main_culture"].Value);
		EncounterBackgroundMesh = ((node.Attributes["encounter_background_mesh"] == null) ? null : node.Attributes["encounter_background_mesh"].Value);
		Banner = ((node.Attributes["faction_banner_key"] == null) ? new Banner() : new Banner(node.Attributes["faction_banner_key"].Value));
		IsBandit = false;
		IsBandit = node.Attributes["is_bandit"] != null && Convert.ToBoolean(node.Attributes["is_bandit"].Value);
		CanHaveSettlement = false;
		CanHaveSettlement = node.Attributes["can_have_settlement"] != null && Convert.ToBoolean(node.Attributes["can_have_settlement"].Value);
	}
}
