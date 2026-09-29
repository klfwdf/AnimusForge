using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

// Production state-machine and IL checks; never creates a Scene, camera, texture or HTTP request.
public static class IsolatedPanoramaRendererAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
    private static object Call(object instance, string method, params object[] args)
    { return instance.GetType().GetMethod(method, All).Invoke(instance, args); }
    private static T Property<T>(object instance, string name)
    { return (T)instance.GetType().GetProperty(name, All).GetValue(instance); }

    private static IEnumerable<Tuple<OpCode, object>> Instructions(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body == null) yield break;
        var bytes = body.GetILAsByteArray();
        var map = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(op => unchecked((ushort)op.Value));
        for (int i = 0; i < bytes.Length;)
        {
            ushort key = bytes[i++];
            if (key == 0xfe) key = (ushort)(0xfe00 | bytes[i++]);
            OpCode op = map[key]; int size; object operand = null;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + BitConverter.ToInt32(bytes, i) * 4; break;
                default: size = 4; break;
            }
            if (op.OperandType == OperandType.InlineMethod)
                operand = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            else if (op.OperandType == OperandType.ShortInlineR)
                operand = BitConverter.ToSingle(bytes, i);
            yield return Tuple.Create(op, operand);
            i += size;
        }
    }

    private static bool Calls(MethodBase method, string name)
    { return Instructions(method).Any(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == name); }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        var renderer = assembly.GetType("AnimusForge.Illustrator.Engine.IsolatedPanoramaRenderer", true);
        var sequenceType = renderer.GetNestedType("FaceSequence", BindingFlags.NonPublic);
        var sequence = Activator.CreateInstance(sequenceType, All, null, new object[] { 6 }, null);
        Check(!Property<bool>(sequence, "Ready"), "new session is not ready before a face is selected");
        Check(!(bool)Call(sequence, "Select", -1) && !(bool)Call(sequence, "Select", 6), "invalid face indices never select a camera");
        Check((bool)Call(sequence, "Select", 0), "valid face can be selected");
        int token = Property<int>(sequence, "Generation");
        Call(sequence, "Painted", token - 1);
        Check(Property<int>(sequence, "PaintCount") == 0, "stale paint callbacks cannot warm a new face");
        Call(sequence, "Painted", token); Call(sequence, "Painted", token);
        Check(!Property<bool>(sequence, "Ready") && !(bool)Call(sequence, "BeginExport", 0), "two paint callbacks are insufficient to export");
        Call(sequence, "Painted", token);
        Check(Property<bool>(sequence, "Ready"), "three genuine paint callbacks make the selected face ready");
        Check(!(bool)Call(sequence, "BeginExport", 1), "export cannot label one camera with another face index");
        Check((bool)Call(sequence, "BeginExport", 0) && Property<int>(sequence, "ExportGeneration") == token,
            "export freezes the current face generation");
        Check(!(bool)Call(sequence, "Select", 1) && !(bool)Call(sequence, "BeginExport", 0),
            "an outstanding PNG blocks camera changes and overlapping exports");
        Call(sequence, "EndExport", false);
        Check(!(bool)Call(sequence, "Select", 1), "missing PNG does not release the export lock");
        Call(sequence, "EndExport", true);
        Check((bool)Call(sequence, "Select", 1) && Property<int>(sequence, "PaintCount") == 0,
            "completed export permits the next face with a fresh warmup");
        Call(sequence, "Painted", token);
        Check(Property<int>(sequence, "PaintCount") == 0, "previous face token stays invalid after switching");
        Call(sequence, "Close"); Call(sequence, "Close");
        Call(sequence, "Painted", Property<int>(sequence, "Generation"));
        Check(Property<bool>(sequence, "Closed") && !(bool)Call(sequence, "Select", 0) &&
            !(bool)Call(sequence, "BeginExport", 1) && Property<int>(sequence, "PaintCount") == 0,
            "retirement is terminal and late callbacks cannot restart a session");

        var retireStep = renderer.GetMethod("RetireStep", All);
        var first = new InvalidOperationException("first offline release failure");
        object[] failed = { (Action)(() => { throw first; }), null };
        retireStep.Invoke(null, failed);
        Check(ReferenceEquals(failed[1], first), "cleanup records native release exceptions instead of escaping");
        int afterFailure = 0;
        object[] remaining = { (Action)(() => { afterFailure++; }), failed[1] };
        retireStep.Invoke(null, remaining);
        Check(afterFailure == 1 && ReferenceEquals(remaining[1], first), "later cleanup still executes after an earlier release failure");
        object[] second = { (Action)(() => { throw new Exception("second failure"); }), failed[1] };
        retireStep.Invoke(null, second);
        Check(ReferenceEquals(second[1], first), "multiple release failures retain the first actionable error");

        var ownedTypes = assembly.GetTypes().Where(t => t == renderer || t.FullName.StartsWith(renderer.FullName + "+", StringComparison.Ordinal));
        var allMethods = ownedTypes.SelectMany(t => t.GetMethods(All | BindingFlags.DeclaredOnly)).ToArray();
        var calls = allMethods.SelectMany(Instructions).Where(i => i.Item2 is MethodBase).Select(i => (MethodBase)i.Item2).ToArray();
        Check(!calls.Any(m => m.DeclaringType.Name == "Mission" || m.DeclaringType.Name == "MissionScreen"),
            "renderer has no live Mission or player camera access");
        Check(!calls.Any(m => m.Name == "Tick" && m.DeclaringType.Name == "Scene"), "renderer never manually ticks a scene");
        Check(!calls.Any(m => new[] { "GetPixelData", "SaveToFile", "ReleaseImmediately", "SetRenderOnDemand" }.Contains(m.Name)),
            "renderer uses no CPU RT readback, immediate release or unverified render-on-demand override");
        var create = renderer.GetMethod("Create", All);
        Check(Calls(create, "AddTableau") && Calls(create, "CreateCamera") && Calls(create, "SetFovHorizontal") && Calls(create, "MarkRendered"),
            "creation uses the native tableau scheduler, private cameras and explicit scene ownership transfer");
        var paint = renderer.GetMethod("Paint", All);
        Check(Calls(paint, "SetCamera") && Calls(paint, "SetScene") && Calls(paint, "Painted"),
            "every actual paint rebinds its own scene and selected camera before recording readiness");
        var paintIl = Instructions(paint).ToList();
        int focused = paintIl.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "SetFocusedShadowmap");
        int ready = paintIl.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "Painted");
        Check(focused >= 3 && focused < ready && paintIl[focused - 3].Item1 == OpCodes.Ldc_I4_1 &&
            object.Equals(paintIl[focused - 1].Item2, 100f) &&
            new[] { "SetRenderWithPostfx", "SetSceneUsesShadows" }.All(name =>
            {
                int index = paintIl.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == name);
                return index > 0 && index < focused && paintIl[index - 1].Item1 == OpCodes.Ldc_I4_1;
            }), "each paint initializes postfx, shadows and focused shadow context before export readiness");
        var createIl = Instructions(create).ToList();
        int shadowScale = createIl.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "SetShadowmapResolutionMultiplier");
        Check(shadowScale > 0 && object.Equals(createIl[shadowScale - 1].Item2, 0.5f),
            "owned view uses half-resolution shadowmaps for the bounded panorama capture");
        Check(Calls(renderer.GetMethod("RequestExport", All), "BeginExport") &&
            Calls(renderer.GetMethod("RequestExport", All), "SetSaveFinalResultToDisk"),
            "native export is guarded by the production face state machine");
        Check(Calls(renderer.GetMethod("StopExport", All), "HasNonEmptyFile") &&
            Calls(renderer.GetMethod("StopExport", All), "EndExport"),
            "stop-export checks the actual output before allowing another face");
        var stop = Instructions(renderer.GetMethod("StopExport", All)).ToList();
        Check(new[] { "SetContinuousRendering", "SetEnable" }.All(name =>
        {
            int index = stop.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == name);
            return index > 0 && stop[index - 1].Item1 == OpCodes.Ldc_I4_0;
        }), "completed export disables GPU rendering during image composition and between faces");
        var restore = renderer.GetMethod("Restore", All);
        Check(Calls(restore, "Close") && Calls(restore, "RetireStep") &&
            Calls(restore, "TrySetResult") && Calls(restore, "TrySetException") &&
            restore.GetMethodBody().ExceptionHandlingClauses.Count(c => c.Flags == ExceptionHandlingClauseOptions.Finally) >= 2,
            "retirement completes its task from finally even when individual native releases fail");
        Check(calls.Any(m => m.Name == "AddClearTask") && calls.Any(m => m.Name == "ReleaseCamera") && calls.Any(m => m.Name == "Release"),
            "cleanup queues native deferred scene retirement and releases owned cameras and texture");
        Check(!calls.Any(m => m.Name == "GetEvent" || m.Name == "RemoveEventHandler"),
            "paint callback is left to native texture release instead of reflective unsubscribe");
        Console.WriteLine("ISOLATED PANORAMA RENDERER AUDIT: " + checks + " PASS / 0 FAIL (no GPU/live-game acceptance)");
    }
}
