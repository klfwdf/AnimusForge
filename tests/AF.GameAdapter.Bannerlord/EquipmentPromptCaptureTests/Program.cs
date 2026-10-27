using System;

static class Program
{
    static int checks, boundsExceptions;
    static void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
    static Agent Reset()
    {
        var agent = new Agent { Index = 7 };
        Agent.Main = agent;
        Mission.Current = new Mission();
        Mission.Current.Agents.Add(agent);
        return agent;
    }
    static int Main()
    {
        AppDomain.CurrentDomain.FirstChanceException += (_, e) => {
            if (e.Exception is IndexOutOfRangeException) boundsExceptions++;
        };
        try
        {
            var agent = Reset();
            for (int i = 5; i <= 9; i++)
                Check(Readers.TryGetAgentEquipmentItemForPrompt(Hero.MainHero, (EquipmentIndex)i) == null,
                    "empty armor returns no item " + i);
            Check(agent.Equipment.Reads == 0, "empty armor never reads weapon storage");
            var armor = new ItemObject("armor");
            agent.SpawnEquipment.Slots[6].Item = armor;
            Check(Readers.TryGetAgentEquipmentItemForPrompt(Hero.MainHero, EquipmentIndex.Body) == armor,
                "spawn armor retained");
            for (int i = 0; i < 5; i++)
            {
                var weapon = new ItemObject("weapon" + i);
                agent.Equipment.Slots[i].Item = weapon;
                Check(Readers.TryGetAgentEquipmentItemForPrompt(Hero.MainHero, (EquipmentIndex)i) == weapon,
                    "live weapon fallback retained " + i);
            }
            var spawnWeapon = new ItemObject("spawn weapon");
            agent.SpawnEquipment.Slots[0].Item = spawnWeapon;
            Check(Readers.TryGetAgentEquipmentItemForPrompt(Hero.MainHero, EquipmentIndex.WeaponItemBeginSlot)
                == spawnWeapon, "spawn weapon retains precedence");
            agent.Active = false;
            Check(Readers.TryGetAgentEquipmentItemForPrompt(Hero.MainHero, EquipmentIndex.Body) == null,
                "inactive agent rejected");
            agent = Reset();
            var npc = new NpcDataPacket { AgentIndex = agent.Index };
            Check(Readers.BuildNonHeroEquipmentSummaryForPrompt(npc) == "battle:", "empty NPC equipment");
            Check(agent.Equipment.Reads == 4, "NPC fallback reads exactly four requested weapon slots");
            agent.SpawnEquipment.Slots[6].Item = armor;
            agent.Equipment.Slots[1].Item = new ItemObject("sword");
            Check(Readers.BuildNonHeroEquipmentSummaryForPrompt(npc) == "battle:armor,sword",
                "NPC armor and live fallback weapon retained");
            Mission.Current.DoesMissionRequireCivilianEquipment = true;
            Check(Readers.BuildNonHeroEquipmentSummaryForPrompt(npc) == "civilian:armor,sword",
                "civilian equipment context retained");
            Check(Readers.BuildNonHeroEquipmentSummaryForPrompt(null) == "Project_Unknown", "null NPC");
            Check(boundsExceptions == 0, "no caught or uncaught array bounds exceptions: " + boundsExceptions);
            Console.WriteLine("PASS " + checks + " checks; bounds exceptions=" + boundsExceptions);
            return 0;
        }
        catch (Exception e) { Console.WriteLine("FAIL " + e.Message + "; bounds exceptions=" + boundsExceptions); return 1; }
    }
}
