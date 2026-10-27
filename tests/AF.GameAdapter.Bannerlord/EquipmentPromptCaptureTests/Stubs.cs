using System;
using System.Collections.Generic;
using System.Linq;

enum EquipmentIndex { WeaponItemBeginSlot = 0, Weapon1, Weapon2, Weapon3, ExtraWeaponSlot,
    NumAllWeaponSlots = 5, Head = 5, Body, Leg, Gloves, Cape }
sealed class ItemObject { internal string Name; internal ItemObject(string name) { Name = name; } }
struct EquipmentElement { internal ItemObject Item; }
sealed class Equipment
{
    internal readonly EquipmentElement[] Slots = new EquipmentElement[12];
    internal EquipmentElement this[EquipmentIndex slot] { get => Slots[(int)slot]; }
}
sealed class MissionEquipment
{
    internal readonly EquipmentElement[] Slots = new EquipmentElement[5];
    internal int Reads;
    internal EquipmentElement this[EquipmentIndex slot] { get { Reads++; return Slots[(int)slot]; } }
}
sealed class Agent
{
    internal static Agent Main;
    internal int Index;
    internal bool Active = true;
    internal Equipment SpawnEquipment = new Equipment();
    internal MissionEquipment Equipment = new MissionEquipment();
    internal bool IsActive() => Active;
}
sealed class Hero { internal static Hero MainHero = new Hero(); }
sealed class NpcDataPacket { internal int AgentIndex; }
sealed class Mission
{
    internal static Mission Current;
    internal List<Agent> Agents = new List<Agent>();
    internal bool DoesMissionRequireCivilianEquipment;
}
sealed class Settlement { internal static Settlement CurrentSettlement; internal bool IsVillage; }
sealed class MobileParty { internal static MobileParty MainParty; internal Settlement CurrentSettlement; }
static class AdapterPromptCaptureTexts { internal static string PcapText(string key) => key; }
static class PromptComposerTexts { internal static string Get(string key) => key; }
static class PersonaIntroTextRules
{
    internal static string GetEquipmentContextLabelForPrompt(bool civilian) => civilian ? "civilian" : "battle";
    internal static List<string> BuildEquipmentSummaryItemLinesForPrompt(Dictionary<string,int> counts,
        Dictionary<string,string> names, int max) => names.Values.Take(max).ToList();
    internal static string ComposeEquipmentSummary(string label, List<string> worn, List<string> weapons)
        => label + ":" + string.Join(",", worn.Concat(weapons));
}
internal static partial class Readers
{
    static bool IsWeaponEquipmentIndexForPrompt(EquipmentIndex slot) => (int)slot >= 0 && (int)slot < 4;
    static void AddEquipmentSummaryItemForPrompt(Dictionary<string,int> counts, Dictionary<string,string> names,
        EquipmentIndex slot, ItemObject item) { counts[item.Name] = 1; names[item.Name] = item.Name; }
}
