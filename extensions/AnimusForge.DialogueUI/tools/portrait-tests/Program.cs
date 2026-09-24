using System;
using AnimusForge.DialogueUI.Native;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

static class Program
{
    private static int _checks;
    private static void Check(bool value, string description)
    {
        if (!value) throw new Exception(description);
        _checks++;
    }
    static void Main()
    {
        var banner = new Banner();
        var character = new CharacterObject { HeroObject = new Hero { Clan = new Clan { Banner = banner } } };
        var agent = new Agent { SpawnEquipment = new Equipment(), BodyPropertiesValue = new BodyProperties(3) };
        agent.SpawnEquipment[0] = new EquipmentElement(9);
        var widget = new CharacterTableauWidget { IdleAction = "act_attack", IsEquipmentAnimActive = true };
        var portrait = new LiveSpeakerPortrait();
        Check(portrait.Apply(widget, agent, character), "First portrait initializes");
        string firstCode = widget.EquipmentCode;
        for (int i = 0; i < 120; i++) Check(portrait.Apply(widget, agent, character), "Repeated appearance succeeds");
        Check(BodyProperties.Encodes == 1 && Equipment.Encodes == 1 && banner.Encodes == 1 && MBBodyProperties.Calls == 1,
            "Unchanged portrait does not re-encode body, equipment or banner or recalculate scale");
        Check(widget.IdleAction == "act_inventory_idle_start" && !widget.IsEquipmentAnimActive
            && widget.LeftHandWieldedEquipmentIndex == -1 && widget.RightHandWieldedEquipmentIndex == -1,
            "Armed guard uses a neutral, unwielded tableau stance");
        agent.SpawnEquipment[0] = new EquipmentElement(9, 2);
        portrait.Apply(widget, agent, character);
        Check(Equipment.Encodes == 2 && widget.EquipmentCode != firstCode, "In-place equipment modifier edit invalidates cache");
        var identical = new Equipment(); identical[0] = new EquipmentElement(9, 2);
        agent.SpawnEquipment = identical;
        portrait.Apply(widget, agent, character);
        Check(Equipment.Encodes == 2, "Equivalent replacement equipment reuses code");
        agent.BodyPropertiesValue = new BodyProperties(4);
        portrait.Apply(widget, agent, character);
        Check(widget.BodyProperties == "4" && BodyProperties.Encodes == 2, "Face edit invalidates body cache");
        character.Race = 2; agent.IsFemale = true;
        portrait.Apply(widget, agent, character);
        Check(widget.Race == 2 && widget.IsFemale && MBBodyProperties.Calls == 3, "Race and sex refresh scale");
        banner.Edit(); portrait.Apply(widget, agent, character);
        Check(widget.BannerCodeText == "banner-1" && banner.Encodes == 2, "In-place banner edit reaches portrait");
        var guard = new CharacterObject { StringId = "second_guard", Culture = new Culture { Color = 12, Color2 = 34 } };
        portrait.Apply(widget, null, guard);
        Check(widget.CharStringId == "second_guard" && widget.BodyProperties == "77" && widget.BannerCodeText == ""
            && widget.ArmorColor1 == 12 && widget.ArmorColor2 == 34, "No-agent map NPC replaces old face and clears banner");
        Check(!portrait.Apply(widget, null, null) && !portrait.Apply(null, agent, character), "Missing portrait inputs are safe");
        Console.WriteLine($"PASS: {_checks} portrait assertions; first 120 unchanged refreshes performed one body/equipment/banner encode each.");
    }
}
