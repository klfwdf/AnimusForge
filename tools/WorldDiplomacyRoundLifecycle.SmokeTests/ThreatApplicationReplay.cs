using System;
using System.Collections.Generic;
using AnimusForge;
using Newtonsoft.Json;

internal static class ThreatApplicationReplay
{
    internal static void Run()
    {
        var pressure = new List<(string Source, string Target, int Delta, string Intent)>();
        var apology = new WorldDiplomacyDocument
            { Intent = "apology", AuthorKingdomId = "a", TargetKingdomId = "b", Title = "apology" };
        Action<string, string, int, string, string> add = (a, b, delta, _, intent) =>
            pressure.Add((a, b, delta, intent));
        WorldDiplomacyThreatApplication.ApplyPressure(apology, () => (true, "a", "b"), add);
        Test.True(pressure.Count == 2 && pressure[0] == ("a", "b", -16, "apology")
            && pressure[1] == ("b", "a", -8, "apology"),
            "formal apology applies both directional pressure reductions once");
        pressure.Clear();
        apology.Intent = "concession";
        WorldDiplomacyThreatApplication.ApplyPressure(apology, () => (true, "a", "b"), add);
        Test.True(pressure.Count == 2 && pressure[0].Delta == -22 && pressure[1].Delta == -11,
            "formal concession retains the larger directional reductions");
        pressure.Clear();
        apology.MechanicalResult = "rejected";
        WorldDiplomacyThreatApplication.ApplyPressure(apology, () => (true, "a", "b"), add);
        apology.MechanicalResult = "";
        WorldDiplomacyThreatApplication.ApplyPressure(apology, () => (false, "a", "b"), add);
        Test.True(pressure.Count == 0, "failed documents and invalid live kingdoms do not alter pressure");

        var threat = new WorldDiplomacyThreat
        {
            ThreatId = "t", IssuerKingdomId = "issuer", TargetKingdomId = "target",
            Stage = "warning", StageDocumentId = "warning", TargetDecision = "pending"
        };
        var storage = new WorldDiplomacyStorage();
        storage.DiplomaticThreats.Add(threat);
        var document = new WorldDiplomacyDocument
        {
            DocumentId = "decision", RoundId = "round", Intent = "apology",
            PresentedThreatDocumentIds = new List<string> { "warning" }
        };
        WorldDiplomacyThreatApplication.RecordTargetDecision(storage, document, "target", "issuer",
            document.Intent, () => 17, _ => { });
        Test.True(threat.TargetDecision == "noncomplied" && threat.TargetDecisionDocumentId == "decision"
            && threat.TargetDecisionRoundId == "round" && threat.TargetDecisionDay == 17
            && threat.NonComplianceEvents.Count == 1,
            "first noncompliant declaration records the decision and durable event");
        WorldDiplomacyThreatApplication.RecordTargetDecision(storage, document, "target", "issuer",
            document.Intent, () => 18, _ => { });
        Test.True(threat.NonComplianceEvents.Count == 1 && threat.TargetDecisionDay == 17,
            "replayed declaration does not duplicate the noncompliance event");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage));
        Test.True(restored?.DiplomaticThreats[0].NonComplianceEvents.Count == 1,
            "target decision survives canonical storage serialization");
    }
}
