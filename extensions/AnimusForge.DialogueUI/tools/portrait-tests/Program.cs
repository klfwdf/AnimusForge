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
        Check(BodyProperties.Encodes == 1 && Equipment.Encodes == 1 && banner.Encodes == 1 && MBBodyProperties.Calls == 0,
            "Unchanged portrait does not re-encode appearance or recalculate render resolution from body height");
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
        Check(widget.Race == 2 && widget.IsFemale && widget.CustomRenderScale == 1.35f && MBBodyProperties.Calls == 0,
            "Race and sex update without changing render-texture quality");
        banner.Edit(); portrait.Apply(widget, agent, character);
        Check(widget.BannerCodeText == "banner-1" && banner.Encodes == 2, "In-place banner edit reaches portrait");
        var guard = new CharacterObject { StringId = "second_guard", Culture = new Culture { Color = 12, Color2 = 34 } };
        portrait.Apply(widget, null, guard);
        Check(widget.CharStringId == "second_guard" && widget.BodyProperties == "77" && widget.BannerCodeText == ""
            && widget.ArmorColor1 == 12 && widget.ArmorColor2 == 34, "No-agent map NPC replaces old face and clears banner");
        Check(!portrait.Apply(widget, null, null) && !portrait.Apply(null, agent, character), "Missing portrait inputs are safe");
        var map = new MapPortraitAppearance { Character = guard, Body = new BodyProperties(901), Equipment = new Equipment(),
            Race = 7, Female = true, Color1 = 1234, Color2 = 5678, Banner = null };
        map.Equipment[5] = new EquipmentElement(88); // Native encounter's hood, absent from troop default.
        string encounterEquipment = map.Equipment.CalculateEquipmentCode();
        portrait.Apply(widget, null, guard, map);
        Check(widget.EquipmentCode == encounterEquipment && widget.BodyProperties == "901", "Map portrait uses encounter hood and face, not troop defaults");
        Check(widget.Race == 7 && widget.IsFemale && widget.ArmorColor1 == 1234 && widget.ArmorColor2 == 5678,
            "Map portrait uses actual native race, sex and clothing colors");
        int mapEquipmentEncodes = Equipment.Encodes;
        for (int i = 0; i < 120; i++) portrait.Apply(widget, null, guard, map);
        Check(Equipment.Encodes == mapEquipmentEncodes, "Captured map appearance remains cached");
        map = new MapPortraitAppearance { Character = guard, Body = new BodyProperties(902), Equipment = new Equipment() };
        map.Equipment[5] = new EquipmentElement(99);
        portrait.Apply(widget, null, guard, map);
        Check(widget.BodyProperties == "902" && widget.EquipmentCode != encounterEquipment, "Same troop in a new encounter replaces old individual");
        CheckFraming();
        CameraTests.Run(Check);
        Console.WriteLine($"PASS: {_checks} portrait assertions; unchanged refreshes reuse appearance; eye framing covers height/scale variation.");
    }

    private static void CheckFraming()
    {
        // Independent perspective projection back into the clipped UI. These cover small/large
        // bodies and vertically displaced eyes, rather than asserting implementation constants.
        foreach (float scale in new[] { 0.55f, 0.85f, 1f, 1.15f, 1.5f })
        foreach (float eyeHeight in new[] { 0.9f, 1.55f, 1.85f, 2.4f })
        {
            Check(PortraitFraming.TryGetCameraOffsets(scale, out float distance, out float offset), "Valid body has framing");
            float cameraHeight = eyeHeight - offset;
            float Project(float z) => (0.5f - (z - cameraHeight) / (2f * distance * (float)Math.Tan(Math.PI / 8))) * 630f - 134f;
            Check(Math.Abs(Project(eyeHeight) - 68f) < 0.001f, "Eyes align across heights and scales");
            Check(Project(eyeHeight + 0.18f * scale) > 8f, "Head and ordinary headwear retain top margin in closer crop");
            Check(Project(eyeHeight - 0.13f * scale) < 125f, "Chin stays above the nameplate");
            Check(Project(eyeHeight - 0.30f * scale) < 169f, "Neck and upper shoulder line remain within the closer crop");
        }
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Check(!PortraitFraming.TryGetCameraOffsets(invalid, out _, out _), "Invalid model scale cannot corrupt camera");
    }
}
