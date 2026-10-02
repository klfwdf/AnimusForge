using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// Executes the production sequencing and appearance handoff without constructing a native stage.
// This is not GPU validation of the native EmphasizeFace framing or helmet/hair meshes.
public static class HeadDetailAudit
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }

    private static Task Collect(Type helper, Func<bool, CancellationToken, Task<string>> capture, CancellationToken token)
    { return (Task)helper.GetMethod("CollectCharacterPortraitReferencesAsync", Static).Invoke(null, new object[] { capture, token }); }
    private static object Complete(Task task)
    { task.GetAwaiter().GetResult(); return task.GetType().GetProperty("Result").GetValue(task); }
    private static string Text(object value, string name)
    { return (string)value.GetType().GetProperty(name).GetValue(value); }
    private static void ExpectCancelled(Task task, string label)
    {
        try { Complete(task); throw new Exception("Expected cancellation: " + label); }
        catch (OperationCanceledException) { Check(true, label); }
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        var helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        var references = assembly.GetType("AnimusForge.Illustrator.Engine.CharacterPortraitReferences", true);
        Check((int)helper.GetField("PortraitCaptureTimeoutMs", Static).GetRawConstantValue() == 12000, "portrait wall-clock budget covers low-frame-rate native warmup");
        var cutoff = helper.GetMethod("ShouldStopStageForFrameBudget", Static);
        Check(!(bool)cutoff.Invoke(null, new object[] { true, 1440, 240 }), "high-FPS portrait is not stopped by unrelated 240-frame cap");
        Check((bool)cutoff.Invoke(null, new object[] { false, 240, 240 }), "banner frame cutoff remains unchanged");
        var classify = helper.GetMethod("ClassifyPortraitCaptureFailure", Static);
        Check((string)classify.Invoke(null, new object[] { false, 0, 20, 0L, null }) == "portrait.provider_warmup_timeout", "failure identifies provider not receiving UI updates");
        Check((string)classify.Invoke(null, new object[] { false, 21, 20, 0L, "portrait.export_request_failed" }) == "portrait.export_request_failed", "failure identifies native export request rejection");
        Check((string)classify.Invoke(null, new object[] { true, 21, 20, 0L, null }) == "portrait.export_file_missing", "failure identifies native PNG never arriving");
        Check((string)classify.Invoke(null, new object[] { true, 21, 20, 128L, null }) == "portrait.export_file_unstable", "failure identifies PNG still changing");
        Check(references.GetProperty("FullBody").SetMethod == null && references.GetProperty("HeadDetail").SetMethod == null,
            "reference pair is immutable after capture");

        var nativeVm = Assembly.Load("TaleWorlds.Core.ViewModelCollection");
        var nativeStance = nativeVm.GetType("TaleWorlds.Core.ViewModelCollection.CharacterViewModel+StanceTypes", true);
        Check((int)helper.GetField("HeadDetailPortraitStance", Static).GetRawConstantValue() == Convert.ToInt32(Enum.Parse(nativeStance, "EmphasizeFace")),
            "head detail selects the native EmphasizeFace camera rather than image enlargement");
        Check((int)helper.GetField("FullBodyPortraitStance", Static).GetRawConstantValue() == Convert.ToInt32(Enum.Parse(nativeStance, "None")),
            "full-body camera preserves the existing native stance");

        var bodyDone = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<bool>();
        Task single = Collect(helper, (head, token) =>
        {
            lock (calls) calls.Add(head);
            if (head) throw new Exception("FAIL redundant head capture was invoked");
            return bodyDone.Task;
        }, CancellationToken.None);
        Check(calls.Count == 1 && !calls[0] && !single.IsCompleted, "single-reference collection waits for the full-body export");
        bodyDone.SetResult("full-native-jpeg");
        var pair = Complete(single);
        Check(Text(pair, "FullBody") == "full-native-jpeg" && Text(pair, "HeadDetail") == null, "automatic reference returns full-body only while preserving legacy DTO shape");
        Check(calls.SequenceEqual(new[] { false }), "exactly one native camera is rendered per character");

        int failedBodyCalls = 0;
        pair = Complete(Collect(helper, (head, token) => { failedBodyCalls++; return Task.FromResult<string>(null); }, CancellationToken.None));
        Check(failedBodyCalls == 1 && Text(pair, "FullBody") == null && Text(pair, "HeadDetail") == null, "failed full-body export does not launch another native stage");
        pair = Complete(Collect(helper, (head, token) => { if (head) throw new Exception("head must not run"); return Task.FromResult("full"); }, CancellationToken.None));
        Check(Text(pair, "FullBody") == "full" && Text(pair, "HeadDetail") == null, "obsolete head work cannot delay or fail a successful full-body capture");
        bool bodyError = false;
        try { Complete(Collect(helper, (head, token) => { throw new InvalidOperationException("offline body failure"); }, CancellationToken.None)); }
        catch (InvalidOperationException) { bodyError = true; }
        Check(bodyError, "required full-body failure is not turned into a successful reference");

        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel(); int starts = 0;
            ExpectCancelled(Collect(helper, (head, token) => { starts++; return Task.FromResult("unused"); }, cancelled.Token), "pre-cancellation stops reference collection");
            Check(starts == 0, "pre-cancelled request creates no stages");
        }
        using (var afterBody = new CancellationTokenSource())
        {
            int starts = 0;
            ExpectCancelled(Collect(helper, (head, token) => { starts++; afterBody.Cancel(); return Task.FromResult("full"); }, afterBody.Token), "cancellation after body capture rejects late delivery");
            Check(starts == 1, "cancelled capture never starts a second view");
        }
        using (var during = new CancellationTokenSource())
        {
            ExpectCancelled(Collect(helper, (head, token) => { during.Cancel(); token.ThrowIfCancellationRequested(); return Task.FromResult("unused"); }, during.Token), "native capture cancellation is never converted into success");
        }

        var snapshotType = assembly.GetType("AnimusForge.Illustrator.Context.CharacterAppearanceSnapshot", true);
        var snapshot = Activator.CreateInstance(snapshotType, new object[] { "complete-equipment-with-helmet", "actual-face-and-hair", "actual-banner", 0xFFAA0011u, 0xFF2233DDu, 7, true });
        var equipmentOverride = helper.GetMethod("WithPortraitEquipmentOverride", Static);
        var same = equipmentOverride.Invoke(null, new object[] { snapshot, null });
        Check(ReferenceEquals(same, snapshot), "no override shares exactly the same immutable appearance");
        var edited = equipmentOverride.Invoke(null, new object[] { snapshot, "portrait-equipment-with-helmet" });
        Check(Text(edited, "EquipmentCode") == "portrait-equipment-with-helmet" && Text(snapshot, "EquipmentCode") == "complete-equipment-with-helmet",
            "portrait equipment override does not mutate the original snapshot");
        foreach (string name in new[] { "BodyProperties", "BannerCode", "Color1", "Color2", "Race", "IsFemale" })
            Check(object.Equals(snapshotType.GetProperty(name).GetValue(snapshot), snapshotType.GetProperty(name).GetValue(edited)),
                "equipment override retains " + name);

        foreach (string subject in new[] { "Hero", "Character" })
        {
            var oldApi = helper.GetMethod("Extract" + subject + "PortraitOffscreenAsync", Static);
            var newApi = helper.GetMethod("Extract" + subject + "PortraitReferencesAsync", Static);
            Check(oldApi.ReturnType == typeof(Task<string>), subject + " legacy string API remains available");
            Check(newApi.ReturnType.GetGenericArguments().Single() == references, subject + " typed API retains the compatible reference DTO");
            Check(oldApi.GetParameters().Select(p => p.Name + ":" + p.ParameterType.FullName)
                .SequenceEqual(newApi.GetParameters().Select(p => p.Name + ":" + p.ParameterType.FullName)),
                subject + " typed API retains original parameter contract");
        }
        Console.WriteLine("SINGLE PORTRAIT / LEGACY SHAPE AUDIT: " + checks + " PASS / 0 FAIL (no GPU/live-game acceptance)");
    }
}
