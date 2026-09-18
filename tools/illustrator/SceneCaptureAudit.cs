using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            yield return Tuple.Create(op, operand);
            i += size;
        }
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        var helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        var session = helper.GetNestedType("PanoramaSession", BindingFlags.NonPublic);
        var calls = session.GetMethods(All).Where(m => m.DeclaringType == session)
            .SelectMany(m => Instructions(m)).Where(i => i.Item2 is MethodBase).Select(i => (MethodBase)i.Item2).ToList();
        Check(!calls.Any(m => m.Name == "set_CustomCamera"), "capture never takes over MissionScreen.CustomCamera");
        Check(!calls.Any(m => m.DeclaringType.FullName.Contains("Widget") || m.Name == "AddLayer" || m.Name == "RemoveLayer"), "capture never hides UI or attaches a visible screen layer");
        Check(!calls.Any(m => m.DeclaringType.Name == "Scene" && (m.Name.StartsWith("Set") || m.Name.Contains("Tick") || m.Name.Contains("Clear") || m.Name == "CreateNewScene")), "borrowed mission scene is never mutated, ticked, or cleared");
        Check(!calls.Any(m => m.Name == "GetPixelData" || m.Name == "SaveToFile" || m.Name == "ReleaseImmediately"), "GPU render target never uses unsafe CPU readback or immediate release");
        Check(calls.Any(m => m.Name == "CreateSceneView") && calls.Any(m => m.Name == "SetRenderTarget") && calls.Any(m => m.Name == "CreateCamera"), "capture renders to an owned target using an independent camera");
        var cleanup = Instructions(session.GetMethod("Restore", All)).ToList();
        int clear = cleanup.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "AddClearTask");
        Check(clear > 0 && cleanup[clear - 1].Item1 == OpCodes.Ldc_I4_1, "retirement clears only the view, never the shared mission scene");
        Check(Instructions(helper.GetMethod("CancelActiveStage", All)).Any(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).DeclaringType == session && ((MethodBase)i.Item2).Name == "Restore"), "Reset/Shutdown cleanup is wired to active offscreen capture");
        var module = assembly.GetType("AnimusForge.Illustrator.SubModule", true);
        var lifetime = module.GetNestedType("SceneCaptureLifetimeBehavior", BindingFlags.NonPublic);
        Check(Instructions(module.GetMethod("OnBeforeMissionBehaviorInitialize", All)).Any(i => i.Item2 is ConstructorInfo && ((MethodBase)i.Item2).DeclaringType == lifetime), "mission initialization installs the offscreen lifetime owner");
        foreach (string callback in new[] { "OnEndMission", "OnRemoveBehavior" })
            Check(Instructions(lifetime.GetMethod(callback, All)).Any(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "CancelSceneCapture"), "view retirement is requested before shared mission resources are freed: " + callback);

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
            var read = helper.GetMethod("ReadSceneExportAsync", All);
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
