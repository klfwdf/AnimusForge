using System.Text;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;

static class Test
{
    private static int _assertions;

    internal static void Equal<T>(T expected, T actual, string message)
    {
        _assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
        }
    }

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static int Assertions => _assertions;
}

sealed class StubQuery : IWorldDiplomacyTimelineRevisionQuery
{
    private readonly Func<WorldDiplomacyTimelineRevisionResult> _query;

    internal StubQuery(Func<WorldDiplomacyTimelineRevisionResult> query)
    {
        _query = query;
    }

    public WorldDiplomacyTimelineRevisionResult Query() => _query();
}

sealed class CountingQuery : IWorldDiplomacyTimelineRevisionQuery
{
    private long _revision;

    public WorldDiplomacyTimelineRevisionResult Query()
    {
        return WorldDiplomacyTimelineRevisionResult.Available(++_revision);
    }
}

sealed class StubRevisionSource : IWorldDiplomacyTimelineRevisionSource
{
    internal bool Available;
    internal long Revision;
    internal bool Throw;

    public bool TryRead(out long revision)
    {
        if (Throw) throw new InvalidOperationException("source failure");
        revision = Revision;
        return Available;
    }
}

sealed class StubDocumentQuery : IWorldDiplomacyTimelineDocumentQuery
{
    private readonly Func<int, WorldDiplomacyTimelineDocumentsResult> _query;

    internal StubDocumentQuery(Func<int, WorldDiplomacyTimelineDocumentsResult> query)
    {
        _query = query;
    }

    internal int LastMaxCount { get; private set; }

    public WorldDiplomacyTimelineDocumentsResult Query(int maxCount)
    {
        LastMaxCount = maxCount;
        return _query(maxCount);
    }
}

sealed class StubDocumentReadCommand : IWorldDiplomacyDocumentReadCommand
{
    private readonly Func<string, WorldDiplomacyDocumentReadResult> _markRead;

    internal StubDocumentReadCommand(Func<string, WorldDiplomacyDocumentReadResult> markRead)
    {
        _markRead = markRead;
    }

    internal string LastDocumentId { get; private set; } = string.Empty;

    public WorldDiplomacyDocumentReadResult MarkRead(string documentId)
    {
        LastDocumentId = documentId;
        return _markRead(documentId);
    }
}

internal static class Program
{
    private static int Main()
    {
        VerifyResultContract();
        VerifyFacadeMapping();
        VerifyModuleApplicationQuery();
        VerifyHotPathAllocation();
        VerifyDocumentContract();
        VerifyDocumentFacadeMapping();
        VerifyDocumentReadCommand();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy timeline query smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyResultContract()
    {
        WorldDiplomacyTimelineRevisionResult defaultResult = default;
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Unavailable, defaultResult.Status, "default status is fail-safe");
        Test.True(!defaultResult.IsAvailable, "default result is unavailable");
        Test.Equal(0L, defaultResult.Revision, "default revision");

        WorldDiplomacyTimelineRevisionResult available = WorldDiplomacyTimelineRevisionResult.Available(42L);
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Available, available.Status, "available status");
        Test.True(available.IsAvailable, "available flag");
        Test.Equal(42L, available.Revision, "available revision");

        WorldDiplomacyTimelineRevisionResult negative = WorldDiplomacyTimelineRevisionResult.Available(-7L);
        Test.True(negative.IsAvailable, "negative source revision remains an available read");
        Test.Equal(0L, negative.Revision, "negative source revision is clamped");

        WorldDiplomacyTimelineRevisionResult unavailable = WorldDiplomacyTimelineRevisionResult.Unavailable();
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Unavailable, unavailable.Status, "unavailable status");
        Test.True(!unavailable.IsAvailable, "unavailable flag");
        Test.Equal(0L, unavailable.Revision, "unavailable revision");

        WorldDiplomacyTimelineRevisionResult failed = WorldDiplomacyTimelineRevisionResult.Failed();
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Failed, failed.Status, "failed status");
        Test.True(!failed.IsAvailable, "failed flag");
        Test.Equal(0L, failed.Revision, "failed revision");
    }

    private static void VerifyFacadeMapping()
    {
        WorldDiplomacyTimelineRevisionFacade available = new(
            new StubQuery(() => WorldDiplomacyTimelineRevisionResult.Available(73L)));
        Test.Equal(73L, available.GetRevisionOrZero(), "available facade revision");
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Available, available.Query().Status, "available facade status");

        WorldDiplomacyTimelineRevisionFacade unavailable = new(
            new StubQuery(WorldDiplomacyTimelineRevisionResult.Unavailable));
        Test.Equal(0L, unavailable.GetRevisionOrZero(), "unavailable facade fallback");
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Unavailable, unavailable.Query().Status, "unavailable facade status");

        WorldDiplomacyTimelineRevisionFacade failed = new(
            new StubQuery(WorldDiplomacyTimelineRevisionResult.Failed));
        Test.Equal(0L, failed.GetRevisionOrZero(), "failed facade fallback");
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Failed, failed.Query().Status, "failed facade status");

        WorldDiplomacyTimelineRevisionFacade throwing = new(
            new StubQuery(() => throw new InvalidOperationException("fixture")));
        Test.Equal(0L, throwing.GetRevisionOrZero(), "throwing query fallback");
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Failed, throwing.Query().Status, "throwing query status");
    }

    private static void VerifyModuleApplicationQuery()
    {
        StubRevisionSource source = new() { Available = true, Revision = 73L };
        Test.Equal(73L, WorldDiplomacyTimelineRevisionApplication.Query(source).Revision,
            "module query reads the current scalar snapshot");
        source.Available = false;
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Unavailable,
            WorldDiplomacyTimelineRevisionApplication.Query(source).Status, "missing owner remains unavailable");
        source.Throw = true;
        Test.Equal(WorldDiplomacyTimelineRevisionStatus.Failed,
            WorldDiplomacyTimelineRevisionApplication.Query(source).Status, "source failure remains failed");
        source.Throw = false;
        source.Available = true;
        for (int i = 0; i < 1000; i++) WorldDiplomacyTimelineRevisionApplication.Query(source);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100000; i++) WorldDiplomacyTimelineRevisionApplication.Query(source);
        Test.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before,
            "module revision query allocates nothing while polling");
    }

    private static void VerifyHotPathAllocation()
    {
        CountingQuery query = new();
        WorldDiplomacyTimelineRevisionFacade facade = new(query);
        for (int i = 0; i < 1000; i++)
        {
            facade.GetRevisionOrZero();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        long last = 0L;
        for (int i = 0; i < 100000; i++)
        {
            last = facade.GetRevisionOrZero();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Test.Equal(101000L, last, "hot-path revision progression");
        Test.Equal(0L, allocated, "hot-path facade allocation");
    }

    private static void VerifyDocumentContract()
    {
        List<WorldDiplomacyTimelineCountryReference> targets = new()
        {
            new WorldDiplomacyTimelineCountryReference("empire", "帝国")
        };
        WorldDiplomacyTimelineDocument document = CreateDocument(targets);
        targets.Add(new WorldDiplomacyTimelineCountryReference("aserai", "阿塞莱"));

        Test.Equal("doc-1", document.DocumentId, "document id projection");
        Test.Equal("author", document.AuthorKingdomId, "author id projection");
        Test.Equal("target", document.TargetKingdomId, "target id projection");
        Test.Equal("正文", document.Body, "body projection");
        Test.Equal("影响", document.ImpactText, "impact projection");
        Test.Equal(1, document.ActionTargets.Count, "action targets are copied");

        List<WorldDiplomacyTimelineDocument> source = new() { document };
        WorldDiplomacyTimelineDocumentsResult available = WorldDiplomacyTimelineDocumentsResult.Available(source);
        source.Clear();
        Test.Equal(WorldDiplomacyTimelineDocumentsStatus.Available, available.Status, "documents available status");
        Test.True(available.IsAvailable, "documents available flag");
        Test.Equal(1, available.Documents.Count, "available documents are copied");

        WorldDiplomacyTimelineDocumentsResult defaultResult = default;
        Test.Equal(WorldDiplomacyTimelineDocumentsStatus.Unavailable, defaultResult.Status, "documents default status");
        Test.Equal(0, defaultResult.Documents.Count, "documents default collection");
        Test.Equal(0, WorldDiplomacyTimelineDocumentsResult.Unavailable().Documents.Count, "unavailable collection");
        Test.Equal(0, WorldDiplomacyTimelineDocumentsResult.Failed().Documents.Count, "failed collection");
    }

    private static void VerifyDocumentFacadeMapping()
    {
        WorldDiplomacyTimelineDocument document = CreateDocument(Array.Empty<WorldDiplomacyTimelineCountryReference>());
        StubDocumentQuery query = new(_ => WorldDiplomacyTimelineDocumentsResult.Available(new[] { document }));
        WorldDiplomacyTimelineDocumentQueryFacade facade = new(query);

        Test.Equal(1, facade.Query(0).Documents.Count, "minimum bounded query result");
        Test.Equal(1, query.LastMaxCount, "minimum bounded query count");
        Test.Equal(1, facade.Query(5000).Documents.Count, "maximum bounded query result");
        Test.Equal(WorldDiplomacyTimelineDocumentQueryFacade.MaximumDocumentCount, query.LastMaxCount, "maximum bounded query count");

        WorldDiplomacyTimelineDocumentQueryFacade unavailable = new(
            new StubDocumentQuery(_ => WorldDiplomacyTimelineDocumentsResult.Unavailable()));
        Test.Equal(WorldDiplomacyTimelineDocumentsStatus.Unavailable, unavailable.Query(40).Status, "document unavailable status");
        Test.Equal(0, unavailable.Query(40).Documents.Count, "document unavailable fallback");

        WorldDiplomacyTimelineDocumentQueryFacade throwing = new(
            new StubDocumentQuery(_ => throw new InvalidOperationException("fixture")));
        Test.Equal(WorldDiplomacyTimelineDocumentsStatus.Failed, throwing.Query(40).Status, "document throwing status");
        Test.Equal(0, throwing.Query(40).Documents.Count, "document throwing fallback");
    }

    private static WorldDiplomacyTimelineDocument CreateDocument(
        IEnumerable<WorldDiplomacyTimelineCountryReference> actionTargets)
    {
        return new WorldDiplomacyTimelineDocument(
            "doc-1", "author", "作者国", "target", "目标国", "标题", "正文", "第3天",
            3, 123L, false, true, false, false, "影响", actionTargets);
    }

    private static void VerifyDocumentReadCommand()
    {
        Test.Equal(WorldDiplomacyDocumentReadStatus.InvalidDocumentId,
            default(WorldDiplomacyDocumentReadResult).Status,
            "document-read default status");
        Test.True(!default(WorldDiplomacyDocumentReadResult).IsApplied,
            "document-read default is not applied");

        StubDocumentReadCommand command = new(_ => WorldDiplomacyDocumentReadResult.Applied());
        WorldDiplomacyDocumentReadCommandFacade facade = new(command);
        WorldDiplomacyDocumentReadResult applied = facade.MarkRead("doc-1");
        Test.Equal(WorldDiplomacyDocumentReadStatus.Applied, applied.Status, "document-read applied status");
        Test.True(applied.IsApplied, "document-read applied flag");
        Test.True(facade.MarkReadOrFalse("doc-2"), "document-read bool mapping");
        Test.Equal("doc-2", command.LastDocumentId, "document-read id delegation");

        foreach ((WorldDiplomacyDocumentReadResult result, WorldDiplomacyDocumentReadStatus status) in new[]
        {
            (WorldDiplomacyDocumentReadResult.InvalidDocumentId(), WorldDiplomacyDocumentReadStatus.InvalidDocumentId),
            (WorldDiplomacyDocumentReadResult.NotFound(), WorldDiplomacyDocumentReadStatus.NotFound),
            (WorldDiplomacyDocumentReadResult.Unavailable(), WorldDiplomacyDocumentReadStatus.Unavailable),
            (WorldDiplomacyDocumentReadResult.Failed(), WorldDiplomacyDocumentReadStatus.Failed)
        })
        {
            WorldDiplomacyDocumentReadCommandFacade mapped = new(new StubDocumentReadCommand(_ => result));
            Test.Equal(status, mapped.MarkRead("doc-x").Status, "document-read non-applied status");
            Test.True(!mapped.MarkReadOrFalse("doc-x"), "document-read non-applied bool mapping");
        }

        WorldDiplomacyDocumentReadCommandFacade throwing = new(
            new StubDocumentReadCommand(_ => throw new InvalidOperationException("fixture")));
        Test.Equal(WorldDiplomacyDocumentReadStatus.Failed, throwing.MarkRead("doc-x").Status,
            "document-read throwing status");
        Test.True(!throwing.MarkReadOrFalse("doc-x"), "document-read throwing bool mapping");
    }

    private static void VerifySourceBoundary()
    {
        string contractPath = FindRepositoryFile("Refactor", "Contracts", "WorldDiplomacyTimelineQueryContracts.cs");
        string facadePath = FindRepositoryFile("Refactor", "Modules", "WorldDiplomacyTimelineRevisionFacade.cs");
        string documentContractPath = FindRepositoryFile("Refactor", "Contracts", "WorldDiplomacyTimelineDocumentQueryContracts.cs");
        string documentFacadePath = FindRepositoryFile("Refactor", "Modules", "WorldDiplomacyTimelineDocumentQueryFacade.cs");
        string readContractPath = FindRepositoryFile("Refactor", "Contracts", "WorldDiplomacyDocumentReadCommandContracts.cs");
        string readFacadePath = FindRepositoryFile("Refactor", "Modules", "WorldDiplomacyDocumentReadCommandFacade.cs");
        string adapterPath = FindRepositoryFile("Refactor", "Adapters", "WorldDiplomacyTimelineRevisionQueryAdapter.cs");
        string documentAdapterPath = FindRepositoryFile("Refactor", "Adapters", "WorldDiplomacyTimelineDocumentQueryAdapter.cs");
        string readAdapterPath = FindRepositoryFile("Refactor", "Adapters", "WorldDiplomacyDocumentReadCommandAdapter.cs");
        string hostPath = FindRepositoryFile("Refactor", "Modules", "WorldDiplomacyTimelineQueryHost.cs");
        string uiPath = FindRepositoryFile("WorldMessageTimelineUi.cs");
        string behaviorPath = FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs");
        string moduleAdapterPath = FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs");
        string modulePath = FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyModule.cs");
        string servicesPath = FindRepositoryFile("src/bridges/Diplomacy/DiplomacyModuleServices.cs");
        string campaignPath = FindRepositoryFile("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string startupPath = FindRepositoryFile("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");

        string contract = File.ReadAllText(contractPath, Encoding.UTF8);
        string facade = File.ReadAllText(facadePath, Encoding.UTF8);
        string documentContract = File.ReadAllText(documentContractPath, Encoding.UTF8);
        string documentFacade = File.ReadAllText(documentFacadePath, Encoding.UTF8);
        string readContract = File.ReadAllText(readContractPath, Encoding.UTF8);
        string readFacade = File.ReadAllText(readFacadePath, Encoding.UTF8);
        string adapter = File.ReadAllText(adapterPath, Encoding.UTF8);
        string documentAdapter = File.ReadAllText(documentAdapterPath, Encoding.UTF8);
        string readAdapter = File.ReadAllText(readAdapterPath, Encoding.UTF8);
        string host = File.ReadAllText(hostPath, Encoding.UTF8);
        string ui = File.ReadAllText(uiPath, Encoding.UTF8);
        string behavior = File.ReadAllText(behaviorPath, Encoding.UTF8);
        string moduleAdapter = File.ReadAllText(moduleAdapterPath, Encoding.UTF8);
        string module = File.ReadAllText(modulePath, Encoding.UTF8);
        string timelineApplication = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTimelineApplication.cs"), Encoding.UTF8);
        string services = File.ReadAllText(servicesPath, Encoding.UTF8);
        string campaign = File.ReadAllText(campaignPath, Encoding.UTF8);
        string startup = File.ReadAllText(startupPath, Encoding.UTF8);
        string queries = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPresentationQueries.cs"));
        string inbox = File.ReadAllText(FindRepositoryFile("WorldEvents", "WorldEventInbox.cs"), Encoding.UTF8);

        foreach (string source in new[] { contract, facade, documentContract, documentFacade, readContract, readFacade })
        {
            Test.True(!source.Contains("TaleWorlds", StringComparison.Ordinal), "pure query source must not reference TaleWorlds");
            Test.True(!source.Contains("WorldMessageTimelineUi", StringComparison.Ordinal), "pure query source must not reference UI");
        }
        Test.True(adapter.Contains("DiplomacyModuleServices.World.QueryTimelineRevision()", StringComparison.Ordinal),
            "legacy adapter must delegate to the current diplomacy owner");
        Test.True(moduleAdapter.Contains("WorldDiplomacyTimelineRevisionApplication.Query(TimelineRevisionSource)", StringComparison.Ordinal)
                  && moduleAdapter.Contains("WorldDiplomacyBehavior.TryGetTimelineRevisionSnapshot(out revision)", StringComparison.Ordinal)
                  && !moduleAdapter.Contains("WorldDiplomacyBehavior.QueryWorldMessageTimelineRevision(", StringComparison.Ordinal)
                  && !behavior.Contains("QueryWorldMessageTimelineRevision(", StringComparison.Ordinal),
            "real revision caller must enter Application; legacy Behavior may only supply a scalar snapshot");
        Test.True(services.Contains("DiplomacyModule Module { get; }", StringComparison.Ordinal)
                  && module.Contains("internal IDiplomacyConversationPort Conversation", StringComparison.Ordinal)
                  && module.Contains("internal IWorldDiplomacyModulePort World", StringComparison.Ordinal)
                  && module.Contains("internal IDiplomacyPolicyObservationPort Policy", StringComparison.Ordinal)
                  && campaign.Contains("DiplomacyModuleServices.Register(campaignGameStarter)", StringComparison.Ordinal)
                  && startup.Contains("DiplomacyModuleServices.RegisterPatches(harmony)", StringComparison.Ordinal),
            "AF lifecycle and capability callers must enter the single diplomacy module through the bridge");
        Test.True(host.Contains("new WorldDiplomacyTimelineRevisionQueryAdapter()", StringComparison.Ordinal)
                  && host.Contains("RevisionFacade.GetRevisionOrZero()", StringComparison.Ordinal),
            "query host must compose the legacy adapter behind the facade once");
        Test.True(!documentContract.Contains("WorldDiplomacyDocument", StringComparison.Ordinal),
            "timeline document contract must not expose persisted document DTOs");
        Test.True(documentAdapter.Contains("DiplomacyModuleServices.World.QueryTimelineDocuments(maxCount)", StringComparison.Ordinal)
                  && moduleAdapter.Contains("WorldDiplomacyTimelineApplication.QueryDocuments(TimelineSource, maxCount)", StringComparison.Ordinal)
                  && timelineApplication.Contains("WorldDiplomacyPresentationQueries.Timeline(storage, maxCount)", StringComparison.Ordinal)
                  && !behavior.Contains("QueryTimelineDocuments(int maxCount)", StringComparison.Ordinal)
                  && queries.Contains("document.IsPlayerAuthored || document.IsReadyForPublication", StringComparison.Ordinal)
                  && queries.Contains("BuildImpactText(document)", StringComparison.Ordinal),
            "document adapter must preserve the bounded source, visibility filter, and standing impact formatter");
        Test.True(host.Contains("new WorldDiplomacyTimelineDocumentQueryAdapter()", StringComparison.Ordinal)
                  && host.Contains("DocumentFacade.Query(maxCount).Documents", StringComparison.Ordinal),
            "query host must compose the document adapter behind the facade once");
        Test.True(readAdapter.Contains("cleanId = (documentId ?? string.Empty).Trim()", StringComparison.Ordinal)
                  && readAdapter.Contains("cleanId.StartsWith(TimelinePrefix, StringComparison.OrdinalIgnoreCase)", StringComparison.Ordinal)
                  && readAdapter.Contains("cleanId = cleanId.Substring(TimelinePrefix.Length)", StringComparison.Ordinal),
            "document-read adapter must preserve exact trim and diplomacy-prefix normalization");
        Test.True(readAdapter.Contains("DiplomacyModuleServices.World.TryMarkDocumentRead", StringComparison.Ordinal)
                  && readAdapter.Contains("WorldDiplomacyDocumentReadResult.Unavailable()", StringComparison.Ordinal)
                  && readAdapter.Contains("WorldDiplomacyDocumentReadResult.NotFound()", StringComparison.Ordinal),
            "document-read adapter must preserve owner availability and lookup failure boundaries");
        Test.True(host.Contains("new WorldDiplomacyDocumentReadCommandAdapter()", StringComparison.Ordinal)
                  && host.Contains("DocumentReadFacade.MarkReadOrFalse(documentId)", StringComparison.Ordinal),
            "query host must compose the document-read adapter behind the facade once");
        Test.True(ui.Contains("DiplomacyRevision = WorldDiplomacyTimelineQueryHost.GetRevisionOrZero()", StringComparison.Ordinal),
            "timeline UI must use the query host");
        Test.True(!ui.Contains("DiplomacyRevision = WorldDiplomacyBehavior.GetWorldMessageTimelineRevisionForExternal()", StringComparison.Ordinal),
            "timeline UI must not call the legacy behavior directly");
        Test.True(ui.Contains("WorldDiplomacyTimelineQueryHost.GetRecentDocumentsOrEmpty(MaxDiplomacySourceEntries)", StringComparison.Ordinal),
            "timeline UI must use the bounded document query host");
        Test.True(!ui.Contains("IEnumerable<WorldDiplomacyDocument> documents", StringComparison.Ordinal),
            "timeline UI must not consume persisted diplomacy documents");
        Test.True(ui.Contains("WorldDiplomacyTimelineQueryHost.MarkDocumentRead(selected.ReadSourceId)", StringComparison.Ordinal)
                  && !ui.Contains("WorldDiplomacyBehavior.MarkDocumentReadForExternal(selected.ReadSourceId)", StringComparison.Ordinal),
            "timeline UI must use the document-read command host");
        Test.True(inbox.Contains("WorldDiplomacyTimelineQueryHost.MarkDocumentRead(selected.EventId)", StringComparison.Ordinal)
                  && !inbox.Contains("WorldDiplomacyBehavior.MarkDocumentReadForExternal(selected.EventId)", StringComparison.Ordinal),
            "world-event inbox must use the document-read command host");
        Test.True(behavior.Contains("public static long GetWorldMessageTimelineRevisionForExternal()", StringComparison.Ordinal)
                  && behavior.Contains("return WorldDiplomacyTimelineQueryHost.GetRevisionOrZero();", StringComparison.Ordinal),
            "legacy static surface must remain and delegate through the facade");
        Test.True(behavior.Contains("public static List<WorldDiplomacyDocument> GetRecentDocumentsForExternal", StringComparison.Ordinal)
                  && behavior.Contains("internal static bool TryGetRecentDocumentsForTimelineQuery", StringComparison.Ordinal),
            "legacy document surface must remain beside the explicit adapter query boundary");
        Test.True(behavior.Contains("public static bool MarkDocumentReadForExternal(string documentId)", StringComparison.Ordinal)
                  && behavior.Contains("return WorldDiplomacyTimelineQueryHost.MarkDocumentRead(documentId);", StringComparison.Ordinal)
                  && moduleAdapter.Contains("WorldDiplomacyTimelineApplication.MarkRead(TimelineSource, documentId, out ownerAvailable)", StringComparison.Ordinal)
                  && timelineApplication.Contains("WorldDiplomacyPresentationQueries.MarkRead(", StringComparison.Ordinal)
                  && !behavior.Contains("TryMarkDocumentReadForCommand", StringComparison.Ordinal)
                  && queries.Contains("document.IsRead = true;", StringComparison.Ordinal),
            "legacy document-read surface must delegate while Application owns the mutation");
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}
