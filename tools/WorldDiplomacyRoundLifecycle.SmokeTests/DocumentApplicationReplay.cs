using System;
using System.IO;
using System.Linq;
using AnimusForge;
using Newtonsoft.Json;

internal static class DocumentApplicationReplay
{
    internal static void Run()
    {
        var created = WorldDiplomacyDocumentApplication.Create(new WorldDiplomacyDocumentApplication.CreationSnapshot
        {
            DocumentId = "doc", ExchangeId = "round", AuthorKingdomId = "author",
            AuthorKingdomName = "Author", AuthorRulerId = "ruler", AuthorRulerName = "Ruler",
            TargetKingdomId = "target", TargetKingdomName = "Target", HasTarget = true,
            Title = "", Body = "  The declaration.  ", Origin = "game", Day = 12,
            GameDate = "date", CreatedUtcTicks = 12345,
            IsPlayerAuthored = true, IsResponse = true
        });
        Test.True(created.DocumentId == "doc" && created.ExchangeId == "round" && created.RoundId == "round"
                && created.AuthorKingdomId == "author" && created.AuthorRulerId == "ruler"
                && created.TargetKingdomId == "target" && created.AddressedKingdomIds.Single() == "target",
            "document creation preserves stable IDs, round identity and initial addressee");
        Test.True(created.Title == "外交宣言" && created.Body == "The declaration."
                && created.Day == 12 && created.GameDate == "date" && created.CreatedUtcTicks == 12345,
            "document creation keeps canonical title/body normalization and captured game time");
        Test.True(created.IsPlayerAuthored && created.IsResponse && created.IsRead,
            "player-authored response starts read without changing authored or response identity");
        var noTarget = WorldDiplomacyDocumentApplication.Create(new WorldDiplomacyDocumentApplication.CreationSnapshot
        {
            DocumentId = "untargeted", HasTarget = false, TargetKingdomId = "",
            TargetKingdomName = "stale", IsPlayerAuthored = false
        });
        Test.True(noTarget.TargetKingdomName == "" && noTarget.AddressedKingdomIds.Count == 0
                && !noTarget.IsRead && noTarget.ExchangeId == "" && noTarget.RoundId == "",
            "untargeted NPC document has no inherited addressee or player read flag");

        var storage = new WorldDiplomacyStorage();
        int revision = 0;
        Action advance = () => revision++;
        WorldDiplomacyDocumentApplication.Add(storage, created, 2, advance);
        WorldDiplomacyDocumentApplication.Add(storage, null, 2, advance);
        WorldDiplomacyDocumentApplication.Add(storage, new WorldDiplomacyDocument(), 2, advance);
        Test.True(storage.Documents.Count == 1 && revision == 1,
            "only a valid canonical document advances the timeline revision");
        var replacement = new WorldDiplomacyDocument
            { DocumentId = "DOC", Day = 13, CreatedUtcTicks = 12346, Title = "replacement" };
        WorldDiplomacyDocumentApplication.Add(storage, replacement, 2, advance);
        Test.True(storage.Documents.Count == 1 && ReferenceEquals(storage.Documents[0], replacement) && revision == 2,
            "same document ID replaces its old record case-insensitively without a second writable copy");
        WorldDiplomacyDocumentApplication.Add(storage,
            new WorldDiplomacyDocument { DocumentId = "second", Day = 14, CreatedUtcTicks = 12347 }, 2, advance);
        WorldDiplomacyDocumentApplication.Add(storage,
            new WorldDiplomacyDocument { DocumentId = "third", Day = 15, CreatedUtcTicks = 12348 }, 2, advance);
        Test.True(storage.Documents.Count == 2 && storage.Documents.All(x => x.DocumentId != "DOC")
                && revision == 4,
            "archive retention keeps the latest two ordinary records and advances once per committed write");
        string saved = JsonConvert.SerializeObject(storage);
        Test.True(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)) == saved,
            "canonical document list and identities survive a real JSON round trip");

        var multi = new WorldDiplomacyDocument
        {
            SourceDocumentId = "original", Actions = new System.Collections.Generic.List<WorldDiplomacyDocumentAction>
            {
                new WorldDiplomacyDocumentAction { ActionId = "one", TargetKingdomId = "a", Intent = "declare_war",
                    RespondingToThreatDocumentId = "warning", RequiresResponse = true },
                new WorldDiplomacyDocumentAction { ActionId = "two", TargetKingdomId = "b", Intent = "statement" }
            }
        };
        WorldDiplomacyDocumentApplication.BeginAction(multi, multi.Actions[0], "a");
        Test.True(multi.ProcessingActionId == "one" && multi.AddressedKingdomIds.Single() == "a",
            "first action exposes only its own target to mechanical execution");
        multi.ChangedDiplomaticState = true;
        multi.MechanicalResult = "applied";
        WorldDiplomacyDocumentApplication.CaptureActionResult(multi, multi.Actions[0]);
        WorldDiplomacyDocumentApplication.BeginAction(multi, multi.Actions[1], "b");
        Test.True(!multi.ChangedDiplomaticState && multi.MechanicalResult == ""
            && multi.Actions[0].ChangedDiplomaticState && multi.Actions[0].MechanicalResult == "applied",
            "each action starts with fresh mechanism state while the previous result remains durable");
        WorldDiplomacyDocumentApplication.CaptureActionResult(multi, multi.Actions[1]);
        WorldDiplomacyDocumentApplication.SealActions(multi,
            new System.Collections.Generic.List<string> { "a", "b" }, "original");
        Test.True(multi.ProcessingActionId == "" && multi.AddressedKingdomIds.Count == 2
            && multi.SourceDocumentId == "warning" && multi.ChangedDiplomaticState
            && multi.RequiresResponse,
            "sealed document restores all addressees, source precedence, and aggregate action state");

        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for document source boundary");
        string owner = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDocumentApplication.cs"));
        string host = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"));
        string executor = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDocumentExecutionApplication.cs"));
        Test.True(!owner.Contains("TaleWorlds", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyDocumentApplication.Create(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyDocumentApplication.Add(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(", StringComparison.Ordinal)
                && executor.Contains("WorldDiplomacyDocumentApplication.BeginAction(", StringComparison.Ordinal)
                && executor.Contains("WorldDiplomacyDocumentApplication.SealActions(", StringComparison.Ordinal),
            "real document callers use one game-free canonical creation and write owner");
    }
}
