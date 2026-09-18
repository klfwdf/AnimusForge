using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// No native engine calls: check the actual compiled capture boundary and disk handoff.
public static class SceneCaptureAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }

    private static IEnumerable<Tuple<OpCode, object>> Instructions(MethodBase method)
    {
        byte[] code = method.GetMethodBody() == null ? null : method.GetMethodBody().GetILAsByteArray();
        if (code == null) yield break;
        var map = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
        for (int i = 0; i < code.Length;)
        {
            ushort key = code[i++];
            if (key == 0xfe) key = (ushort)(0xfe00 | code[i++]);
            OpCode op = map[key];
            int size;
            object operand = null;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(code, i); break;
                default: size = 4; break;
            }
            if (op.OperandType == OperandType.InlineMethod)
                operand = method.Module.ResolveMethod(BitConverter.ToInt32(code, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            else if (op.OperandType == OperandType.InlineField)
                operand = method.Module.ResolveField(BitConverter.ToInt32(code, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            else if (op.OperandType == OperandType.InlineString)
                operand = method.Module.ResolveString(BitConverter.ToInt32(code, i));
            yield return Tuple.Create(op, operand);
            i += size;
        }
    }

    private static bool Calls(MethodBase method, string name)
    { return Instructions(method).Any(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == name); }

    private static MethodInfo MoveNext(MethodInfo asyncMethod)
    {
        var state = (AsyncStateMachineAttribute)asyncMethod.GetCustomAttributes(typeof(AsyncStateMachineAttribute), false).Single();
        return state.StateMachineType.GetMethod("MoveNext", All);
    }

    // Follow the actual compiler-generated lambdas referenced by the orchestrator.
    // Do not collect unrelated legacy screenshot helpers merely because they share a partial class.
    private static List<Tuple<OpCode, object>> CaptureInstructions(MethodInfo capture)
    {
        var result = Instructions(MoveNext(capture)).ToList();
        var pending = new Queue<MethodBase>(result.Select(i => i.Item2).OfType<MethodBase>()
            .Where(m => m.Module == capture.Module && m.Name.Contains("<CaptureConversationSceneReferencesAsync>")));
        var visited = new HashSet<MethodBase>();
        while (pending.Count > 0)
        {
            MethodBase method = pending.Dequeue();
            if (!visited.Add(method)) continue;
            var code = Instructions(method).ToList();
            result.AddRange(code);
            foreach (var called in code.Select(i => i.Item2).OfType<MethodBase>())
                if (called.Module == capture.Module && called.Name.Contains("<CaptureConversationSceneReferencesAsync>")) pending.Enqueue(called);
        }
        return result;
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        var helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        var capture = helper.GetMethod("CaptureConversationSceneReferencesAsync", All);
        var code = CaptureInstructions(capture);
        var calls = code.Select(i => i.Item2).OfType<MethodBase>().ToList();
        var renderer = assembly.GetType("AnimusForge.Illustrator.Engine.IsolatedPanoramaRenderer", true);
        var projection = assembly.GetType("AnimusForge.Illustrator.Engine.PanoramaProjection", true);
        Check(helper.GetNestedType("PanoramaSession", BindingFlags.NonPublic) == null, "old shared-Mission panorama session is absent");
        Check(calls.Any(m => m.Name == "CreatePanoramaSnapshotAsync") && calls.Any(m => m.DeclaringType == renderer && m.Name == "Create"),
            "compiled generation path creates a private environment snapshot and its renderer");
        Check(calls.Any(m => m.DeclaringType == projection && m.Name == "BuildCameraFrames") && code.Any(i => i.Item1 == OpCodes.Ldc_I4_6),
            "compiled generation builds camera frames and iterates six exports");
        Check(new[] { "SelectFace", "get_IsReady", "RequestExport", "StopExport" }.All(name => calls.Any(m => m.DeclaringType == renderer && m.Name == name)),
            "real generation selects each face, waits for readiness, requests export and stops it");
        Check(calls.Any(m => m.Name == "ReadPanoramaFaceAsync") && calls.Any(m => m.DeclaringType == projection && m.Name == "Compose"),
            "completed face files flow into the production panorama compositor");
        Check(!calls.Any(m => m.Name == "set_CustomCamera" || m.Name == "SetCameraFrame" || m.Name == "set_Frame" || m.Name == "set_IsVisible" || m.Name == "AddLayer" || m.Name == "RemoveLayer"),
            "orchestration does not move live cameras or hide or move interface layers");
        Check(!calls.Any(m => m.Name == "SetScene" || m.Name == "CreateSceneView" || (m.DeclaringType.Name == "Scene" && (m.Name.StartsWith("Set") || m.Name == "Tick" || m.Name == "ClearAll"))),
            "orchestration never binds the live Mission.Scene to an auxiliary renderer");
        var rendererCalls = renderer.GetMethods(All | BindingFlags.DeclaredOnly).SelectMany(Instructions).Select(i => i.Item2).OfType<MethodBase>().ToList();
        Check(!rendererCalls.Any(m => m.DeclaringType.Name == "Mission" || m.DeclaringType.Name == "MissionScreen"),
            "renderer obtains no Mission scene or combat camera");
        Check(Calls(renderer.GetMethod("Paint", All), "SetCamera") && Calls(renderer.GetMethod("Paint", All), "Painted") &&
            Calls(renderer.GetProperty("IsReady", All).GetGetMethod(true), "get_Ready"),
            "readiness is wired to actual tableau paint callbacks and camera resubmission");
        Check(!rendererCalls.Any(m => m.Name == "GetPixelData" || m.Name == "SaveToFile" || m.Name == "ReleaseImmediately" || m.Name == "Tick"),
            "renderer uses no unsafe CPU texture readback or manual scene tick");
        Check(calls.Any(m => m.Name == "CreateCapturedSceneReferences") && calls.Any(m => m.Name == "AddRange") &&
            code.Any(i => i.Item2 is string && ((string)i.Item2).Contains("360×180")),
            "presented screenshot is appended to the real composed panorama rather than replacing it");

        var cancelMethod = helper.GetMethod("CancelIsolatedPanorama", All);
        Check(Calls(helper.GetMethod("CancelActiveStage", All), "CancelIsolatedPanorama"), "active-stage cancellation reaches isolated capture cleanup");
        Check(Calls(cancelMethod, "Restore") && Calls(cancelMethod, "DisposeUnrendered") &&
            !Calls(cancelMethod, "PostCritical"), "shutdown retires pending snapshot and active renderer synchronously");
        var runtime = assembly.GetType("AnimusForge.Illustrator.Core.IllustratorRuntime", true);
        Check(Calls(runtime.GetMethod("Reset", All), "CancelActiveStage") && Calls(runtime.GetMethod("Shutdown", All), "Reset"),
            "Reset and Shutdown retain the cancellation chain even when application ticks stop");
        var fieldWrites = code.Where(i => i.Item1 == OpCodes.Stsfld).Select(i => i.Item2).OfType<FieldInfo>().Select(f => f.Name).ToArray();
        Check(fieldWrites.Contains("_activeIsolatedPanorama") && fieldWrites.Contains("_pendingPanoramaSnapshot"),
            "renderer handoff updates both tracked ownership fields in the dispatched creation action");

        var wrapper = helper.GetMethod("CreateCapturedSceneReferences", All);
        var empty = ((System.Collections.IEnumerable)wrapper.Invoke(null, new object[] { null })).Cast<object>().ToArray();
        Check(empty.Length == 0, "missing calibration image produces no invented scene reference");
        var single = ((System.Collections.IEnumerable)wrapper.Invoke(null, new object[] { "actual-image-bytes" })).Cast<object>().ToArray();
        Type reference = single.Single().GetType();
        Check(single.Length == 1 && (string)reference.GetProperty("Base64Image").GetValue(single[0]) == "actual-image-bytes" &&
            reference.GetProperty("Kind").GetValue(single[0]).ToString() == "Scene", "calibration wrapper preserves image identity and ordinary Scene kind");
        string label = (string)reference.GetProperty("Label").GetValue(single[0]);
        Check(label.Contains("单张") && label.Contains("不是全景") && label.Contains("灰色"), "calibration label discloses its limited view and pixel UI mask");

        var sceneLock = (SemaphoreSlim)helper.GetField("SceneCaptureLock", All).GetValue(null);
        var stageLock = (SemaphoreSlim)helper.GetField("_stageLock", All).GetValue(null);
        int sceneCount = sceneLock.CurrentCount, stageCount = stageLock.CurrentCount;
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            var task = (Task)capture.Invoke(null, new object[] { cancelled.Token });
            bool stopped = false;
            try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { stopped = true; }
            Check(stopped && sceneLock.CurrentCount == sceneCount && stageLock.CurrentCount == stageCount,
                "pre-cancelled capture invokes no native work and consumes neither semaphore");
            Check(helper.GetField("_pendingPanoramaSnapshot", All).GetValue(null) == null && helper.GetField("_activeIsolatedPanorama", All).GetValue(null) == null,
                "pre-cancellation leaves no snapshot or renderer ownership behind");
        }
        Check(sceneLock.Wait(0), "test acquires the scene gate without invoking game work");
        try
        {
            using (var queued = new CancellationTokenSource())
            {
                var task = (Task)capture.Invoke(null, new object[] { queued.Token });
                Check(!task.IsCompleted, "another capture waits on the scene gate before reading live context");
                queued.Cancel();
                bool stopped = false;
                try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { stopped = true; }
                Check(stopped && sceneLock.CurrentCount == 0 && stageLock.CurrentCount == stageCount,
                    "cancelling a queued capture neither steals nor releases another request's gate");
            }
        }
        finally { sceneLock.Release(); }

        string folder = Path.Combine(Path.GetDirectoryName(dllPath), "scene-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "reference.png");
        try
        {
            using (var bitmap = new System.Drawing.Bitmap(24, 18))
            {
                using (var drawing = System.Drawing.Graphics.FromImage(bitmap)) drawing.Clear(System.Drawing.Color.FromArgb(255, 170, 36, 64));
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            var read = helper.GetMethod("ReadPanoramaFaceAsync", All);
            var task = (Task<byte[]>)read.Invoke(null, new object[] { path, CancellationToken.None });
            byte[] result = task.GetAwaiter().GetResult();
            using (var stream = new MemoryStream(result))
            using (var bitmap = new System.Drawing.Bitmap(stream))
                Check(bitmap.Width == 24 && bitmap.GetPixel(1, 1).R == 170 && bitmap.GetPixel(1, 1).B == 64, "completed scene PNG preserves encoded dimensions and colors");
            File.Move(path, path + ".png");
            var appended = (Task<byte[]>)read.Invoke(null, new object[] { path, CancellationToken.None });
            Check(appended.GetAwaiter().GetResult().Length > 0, "native auto-appended PNG suffix is accepted without a directory scan");
            using (var cancel = new CancellationTokenSource())
            {
                cancel.Cancel();
                var pending = (Task<byte[]>)read.Invoke(null, new object[] { Path.Combine(folder, "missing.png"), cancel.Token });
                bool stopped = false;
                try { pending.GetAwaiter().GetResult(); } catch (OperationCanceledException) { stopped = true; }
                Check(stopped, "cancelled capture stops waiting for native export");
            }
            var missing = (Task<byte[]>)read.Invoke(null, new object[] { Path.Combine(folder, "missing.png"), CancellationToken.None });
            Check(missing.GetAwaiter().GetResult() == null, "missing render output times out instead of inventing a panorama");
        }
        finally { File.Delete(path); File.Delete(path + ".png"); Directory.Delete(folder); }
        Console.WriteLine("TOTAL " + checks + " PASS / 0 FAIL (no GPU or live mission exercised)");
    }
}
