using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Real DLL UI callback -> fresh capture -> queued request, followed by a detached
// provider result at the production commit boundary. No provider or game writes.
internal static class WeeklyReportRecoveryReplay
{
    private const BindingFlags M = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    internal static void Run(Assembly af)
    {
        Type hostType = af.GetType("AnimusForge.MyBehavior", true);
        Type Nested(string name) => hostType.GetNestedType(name, BindingFlags.NonPublic);
        object New(string name) => Activator.CreateInstance(Nested(name), true);
        object Get(object o, string name) => o.GetType().GetField(name, M).GetValue(o);
        void Set(object o, string name, object value) => o.GetType().GetField(name, M).SetValue(o, value);
        object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, M).Invoke(o, args);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Weekly recovery: " + label); }
        object host = RuntimeHelpers.GetUninitializedObject(hostType);
        Type revisionType = af.GetType("AnimusForge.WeeklyReportMaterialRevisionOwner", true);
        object revisions = Activator.CreateInstance(revisionType, true);
        Set(host, "_weeklyReportMaterialRevisions", revisions);
        object snapshot = Call(revisions, "Capture", 0, 6);
        IList records = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Nested("EventRecordEntry")));
        Set(host, "_eventRecordEntries", records);
        Set(host, "_eventWorldOpeningSummary", "fresh opening material");
        object Queue(string context, Type result, string complete, string field)
        {
            Type ct = Nested(context);
            Type qt = af.GetType("AnimusForge.WeeklyReportCommitQueueOwner`2", true).MakeGenericType(ct, result);
            Delegate callback = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(ct, result), hostType.GetMethod(complete, M));
            Delegate canceled = Expression.Lambda(typeof(Func<>).MakeGenericType(result), Expression.Default(result)).Compile();
            object queue = Activator.CreateInstance(qt, M, null, new object[] { callback, canceled }, null);
            Set(host, field, queue);
            return queue;
        }
        Type resultType = Nested("WeeklyReportGenerationResult");
        object queue = Queue("PendingWeeklyReportCommitContext", resultType, "CompletePendingWeeklyReportCommit", "_weeklyReportCommitQueue");
        object prompts = Queue("PendingWeeklyPromptPreparationContext", Nested("WeeklyPromptPreparationResult"), "CompletePendingWeeklyPromptPreparation", "_weeklyPromptPreparationQueue");
        object world = New("WeeklyEventMaterialPreviewGroup"); Set(world, "GroupKind", "world");
        object kingdom = New("WeeklyEventMaterialPreviewGroup"); Set(kingdom, "GroupKind", "kingdom"); Set(kingdom, "KingdomId", "k1");
        object winner = New("EventRecordEntry");
        Set(winner, "EventId", "weekly_report:kingdom:1:k1"); Set(winner, "EventKind", "kingdom");
        Set(winner, "ScopeKingdomId", "k1"); Set(winner, "WeekIndex", 1); Set(winner, "Summary", "existing winner"); records.Add(winner);
        object Context(IList groups, bool success, object source)
        {
            object context = New("PendingWeeklyReportCommitContext");
            Set(context, "WeekIndex", 1); Set(context, "StartDay", 0); Set(context, "EndDay", 6);
            Set(context, "DisplayLabel", "recovery replay"); Set(context, "Groups", groups); Set(context, "SourceSnapshot", source);
            var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (object group in groups) states[(string)Get(group, "GroupKind") == "world" ? "world" : "kingdom:k1"] = null;
            Set(context, "CapturedRecordStates", states);
            object batch = New("WeeklyReportBatchRequest"); Set(batch, "Groups", groups);
            object result = New("WeeklyReportBatchRequestResult"); Set(result, "Success", success);
            foreach (object group in groups)
            {
                string id = (string)Get(group, "GroupKind") == "world" ? "world" : "kingdom:k1";
                if (!success && id == "world") { ((IList)Get(result, "MissingReportIds")).Add(id); continue; }
                object block = New("WeeklyReportBatchBlockResult"); Set(block, "ReportId", id); Set(block, "Parsed", true);
                Set(block, "Title", "recovered title"); Set(block, "Report", new string('r', 4096)); Set(block, "ShortSummary", "recovered summary");
                ((IList)Get(result, "Blocks")).Add(block);
            }
            object execution = New("WeeklyReportBatchExecutionResult"); Set(execution, "Batch", batch); Set(execution, "Result", result);
            ((IList)Get(context, "Executions")).Add(execution);
            Set(context, "CompletionSource", Activator.CreateInstance(typeof(TaskCompletionSource<>).MakeGenericType(resultType), TaskCreationOptions.RunContinuationsAsynchronously));
            return context;
        }
        IList Groups(params object[] items)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Nested("WeeklyEventMaterialPreviewGroup")));
            foreach (object item in items) list.Add(item);
            return list;
        }
        object Commit(object context)
        {
            Check((bool)Call(host, "ProcessPendingWeeklyReportCommitContext", context, Stopwatch.GetTimestamp(), 10000.0), "commit finishes");
            object completion = Get(context, "CompletionSource");
            Task task = (Task)completion.GetType().GetProperty("Task").GetValue(completion);
            Check(task.IsCompletedSuccessfully, "waiter completed");
            return task.GetType().GetProperty("Result").GetValue(task);
        }
        FieldInfo instance = hostType.GetField("<Instance>k__BackingField", M);
        object previous = instance.GetValue(null);
        Type info = Assembly.Load("TaleWorlds.Library").GetType("TaleWorlds.Library.InformationManager", true);
        EventInfo inquiryEvent = info.GetEvents(M).Single(e => e.EventHandlerType.GetMethod("Invoke").GetParameters().Any(p => p.ParameterType.Name == "InquiryData"));
        object inquiry = null;
        Action<object> capture = value => inquiry = value;
        var parameters = inquiryEvent.EventHandlerType.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        Delegate observer = Expression.Lambda(inquiryEvent.EventHandlerType,
            Expression.Invoke(Expression.Constant(capture), Expression.Convert(parameters.Single(p => p.Type.Name == "InquiryData"), typeof(object))), parameters).Compile();
        inquiryEvent.AddEventHandler(null, observer);
        instance.SetValue(null, host);
        try
        {
            object partial = Context(Groups(world, kingdom), false, snapshot);
            object partialResult = Commit(partial);
            Check((int)Get(partialResult, "SuccessCount") == 1 && (int)Get(partialResult, "FailureCount") == 1, "partial winner and failed world");
            object retry = Get(partialResult, "RetryContext"); Set(retry, "RequiresFreshMaterials", true); Set(host, "_weeklyReportRetryContext", retry);
            Call(host, "ShowWeeklyReportFailurePopup", true);
            Check(inquiry != null && !(bool)prompts.GetType().GetProperty("HasPending", M).GetValue(prompts), "popup alone does not start retry");
            Action affirmative = (Action)(inquiry.GetType().GetProperty("AffirmativeAction", M)?.GetValue(inquiry)
                ?? inquiry.GetType().GetField("AffirmativeAction", M)?.GetValue(inquiry));
            Check(affirmative != null, "real affirmative callback captured");
            affirmative();
            object fresh = Get(host, "_weeklyReportRetryContext");
            Check(!ReferenceEquals(fresh, retry) && !(bool)Get(fresh, "RequiresFreshMaterials"), "explicit action replaces stale context");
            IList freshGroups = (IList)Get(fresh, "Groups");
            Check(freshGroups.Count == 1 && (string)Get(freshGroups[0], "GroupKind") == "world", "only failed group recaptured");
            Check((bool)prompts.GetType().GetProperty("HasPending", M).GetValue(prompts), "real retry stops at main-thread prompt queue before network");
            affirmative();
            Check(ReferenceEquals(fresh, Get(host, "_weeklyReportRetryContext")), "duplicate old UI callback cannot replace active request");
            // Cancel the queued request without pumping network; supply its detached result at the actual commit boundary.
            Set(host, "_weeklyReportManualRetryVersion", (int)Get(host, "_weeklyReportManualRetryVersion") + 1);
            Call(prompts, "CancelAll");
            IList materials = (IList)Get(freshGroups[0], "Materials");
            for (int index = 0; index < 96; index++)
            {
                object material = New("EventMaterialReference");
                Set(material, "MaterialType", "fixture"); Set(material, "Label", "material-" + index);
                Set(material, "SnapshotText", new string('m', 2048)); materials.Add(material);
            }
            object recovered = Context(freshGroups, true, Call(revisions, "Capture", 0, 6));
            object freshMap = hostType.GetMethod("BuildWeeklyReportGroupMap", M).Invoke(null, new object[] { freshGroups });
            Set(recovered, "CapturedRecordStates", Call(host, "CaptureWeeklyReportCommitRecordStates", freshMap, 1));
            object recoveredResult = Commit(recovered);
            Check((bool)Get(recoveredResult, "Completed") && (int)Get(recoveredResult, "SuccessCount") == 1 && records.Count == 2, "fresh result committed once");
            object published = records.Cast<object>().Single(r => (string)Get(r, "EventKind") == "world");
            Check(((string)Get(published, "Summary")).Length == 4096 && (string)Get(winner, "Summary") == "existing winner", "full text retained and old winner unchanged");
            Check(((IList)Get(published, "Materials")).Count == 97, "all 97 materials survive staged commit");
            long publishedRevision = (long)Get(host, "_publishedWorldWeeklyHistoryRevision");
            Commit(recovered);
            Check(records.Count == 2 && (long)Get(host, "_publishedWorldWeeklyHistoryRevision") == publishedRevision
                && ReferenceEquals(published, records.Cast<object>().Single(r => (string)Get(r, "EventKind") == "world")), "duplicate completion cannot republish product");
            for (int week = 2; week <= 6; week++)
            {
                object backlog = Context(freshGroups, true, Call(revisions, "Capture", 0, 6));
                Set(backlog, "WeekIndex", week);
                Set(backlog, "CapturedRecordStates", Call(host, "CaptureWeeklyReportCommitRecordStates", freshMap, week));
                Check(!(bool)Call(host, "ProcessPendingWeeklyReportCommitContext", backlog, 1L, 0.001)
                    && (int)Get(backlog, "ExecutionIndex") == 0, "exhausted time budget preserves cursor");
                Call(queue, "Enqueue", backlog);
            }
            int ticks = 0;
            while ((bool)queue.GetType().GetProperty("HasPending", M).GetValue(queue) && ticks++ < 1000)
                Call(host, "ProcessPendingWeeklyReportCommits");
            Check(ticks < 1000 && records.Count == 7, "five queued contexts drain without lost records");
            long snapshotChars = materials.Cast<object>().Sum(m => (long)((string)Get(m, "SnapshotText") ?? "").Length);
            Console.WriteLine("PASS WeeklyReportRecoveryReplay partial/UI-explicit-fresh/old-callback/real-commit/duplicate/backlog=5 recordsPerContext=1 materialsPerRecord=97 reportChars=4096 snapshotChars=" + snapshotChars + " pumpCalls=" + ticks + " provider=NOT_RUN live=NOT_RUN frameTime=NOT_RUN");
        }
        finally
        {
            Call(prompts, "CancelAll");
            inquiryEvent.RemoveEventHandler(null, observer);
            instance.SetValue(null, previous);
        }
    }
}
