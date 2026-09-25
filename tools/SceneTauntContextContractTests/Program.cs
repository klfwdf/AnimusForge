using AnimusForge.Refactor.Modules;

static void Check(bool expected, ScenePeaceConflictContext facts, string label)
{
    bool actual = ScenePeaceConflictContextOwner.CanInitialize(in facts);
    if (actual != expected) throw new InvalidOperationException(label + ": expected=" + expected + " actual=" + actual);
    Console.WriteLine("PASS " + label);
}

static ScenePeaceConflictContext Facts(
    bool mission = true, bool settlement = true, bool encounter = true,
    bool location = true, bool sameSettlement = true, bool battle = false,
    bool siegeHandler = false, bool battleTeam = false, bool battleMode = false,
    bool underSiege = false, string locationId = "center")
    => new ScenePeaceConflictContext(mission, settlement, encounter, location,
        sameSettlement, battle, siegeHandler, battleTeam, battleMode, underSiege, locationId);

Check(true, Facts(), "peace center allowed");
Check(true, Facts(locationId: "village_center"), "village center allowed");
Check(true, Facts(locationId: "lordshall"), "lord hall allowed");
Check(true, Facts(locationId: "tavern"), "tavern allowed");
Check(true, Facts(locationId: "alley"), "native alley allowed");
Check(true, Facts(locationId: "prison"), "dungeon allowed");
Check(true, Facts(locationId: "port"), "port allowed");
Check(false, Facts(mission: false), "missing Mission denied");
Check(false, Facts(settlement: false), "missing settlement denied");
Check(false, Facts(encounter: false), "missing LocationEncounter denied");
Check(false, Facts(location: false), "missing Campaign location denied");
Check(false, Facts(sameSettlement: false), "mismatched settlement denied");
Check(false, Facts(battle: true), "active map/encounter battle denied");
Check(false, Facts(siegeHandler: true), "siege handler denied");
Check(false, Facts(battleTeam: true), "siege/sally/field team type denied");
Check(false, Facts(battleMode: true), "deployment/stealth/duel mode denied");
Check(false, Facts(underSiege: true), "besieged settlement denied");
Check(false, Facts(locationId: "arena"), "arena denied");
Check(false, Facts(locationId: "TRAINING_FIELD"), "training field denied");
Check(false, Facts(locationId: " "), "unknown location denied");
Console.WriteLine("20/20 scene Taunt context cases passed; game Mission order NOT_RUN");
